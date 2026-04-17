---
phase: 05-이식성
plan: 01
subsystem: build-pipeline
tags: [build, publish, self-contained, dotnet8, port-02]
requires: []
provides:
  - "self-contained win-x64 publish artifact at bin/Release/net8.0-windows/win-x64/publish/"
  - "csproj publisher/version metadata for Inno Setup consumption (Company, Copyright, FileVersion)"
  - "installer/ directory tracked for Plan 02 Inno Setup script"
affects:
  - ASLTv1.0.csproj
  - .gitignore
  - installer/.gitkeep
tech-stack:
  added: []
  patterns:
    - "Self-contained deployment (SelfContained=true, RuntimeIdentifier=win-x64, PublishSingleFile=false)"
key-files:
  created:
    - installer/.gitkeep
  modified:
    - ASLTv1.0.csproj
    - .gitignore
decisions:
  - "Adopt SelfContained + RuntimeIdentifier csproj defaults so bare `dotnet publish` yields the correct artifact; Plan 02 install scripts still pass flags explicitly for clarity."
  - "Keep Version=1.0.0 unchanged; add AssemblyVersion=1.0.0.0 / FileVersion=1.0.0.0 (4-part) alongside (required by Windows file properties spec)."
  - "Copyright year fixed at 2026 (matches current project date)."
metrics:
  duration: "~4 minutes"
  completed: "2026-04-17"
requirements: [PORT-02]
---

# Phase 05 Plan 01: 이식성 빌드 파이프라인 준비 Summary

Prepared the self-contained win-x64 publish pipeline for AOLT by adding publisher/version metadata to the csproj, extending `.gitignore` to cover installer output, creating the tracked `installer/` directory, and verifying `dotnet publish` produces a complete 250 MB self-contained artifact with bundled .NET 8 runtime and OpenCV natives.

## Completed Tasks

| Task | Name                                                  | Commit    | Files                                             |
| ---- | ----------------------------------------------------- | --------- | ------------------------------------------------- |
| 1    | Add publish metadata to csproj and extend .gitignore  | 950f04f   | ASLTv1.0.csproj, .gitignore, installer/.gitkeep   |
| 2    | Run dotnet publish and verify self-contained output   | 85b8144   | (verification only — no committed artifacts)      |

## Exact csproj Properties Added

Inside the existing `<PropertyGroup>` in `ASLTv1.0.csproj`, after `<Product>`:

```xml
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
<InformationalVersion>1.0.0</InformationalVersion>
<Company>ANNA</Company>
<Authors>ANNA</Authors>
<Copyright>Copyright © ANNA 2026</Copyright>
<AssemblyTitle>ANNA 합성데이터 라벨링 툴킷 (ASLT)</AssemblyTitle>
<Description>ANNA Synthetic data Labeling Toolkit - COCO format annotation tool for traffic video analysis</Description>
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishSingleFile>false</PublishSingleFile>
```

Existing `<Version>1.0.0</Version>` preserved (single occurrence, not duplicated).

## Exact .gitignore Lines

Final `.gitignore` contents (3 new lines added, existing 3 preserved):

```
bin/
obj/
.claude/
installer/Output/
publish/
*.user
```

## Publish Verification

**Command executed:**
```
dotnet publish ASLTv1.0.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
```

**Exit code:** 0 (success)

**Output path:** `bin/Release/net8.0-windows/win-x64/publish/`

**Output statistics:**
- Total files: **266**
- Total size: **250 MB** (well over the 50 MB threshold — confirms self-contained runtime bundled)
- `ASLTv1.exe` size: 147 KB (150,528 bytes)

**Key files verified present:**

| File                                  | Role                                              |
| ------------------------------------- | ------------------------------------------------- |
| `ASLTv1.exe`                          | Main self-contained executable                    |
| `ASLTv1.dll`                          | Managed assembly                                  |
| `coreclr.dll`                         | Bundled .NET 8 runtime (proves self-contained)    |
| `hostfxr.dll`                         | .NET host FX resolver                             |
| `OpenCvSharpExtern.dll`               | OpenCV native bindings                            |
| `opencv_videoio_ffmpeg4110_64.dll`    | OpenCV FFmpeg bridge                              |
| `Serilog.dll`                         | Logging framework                                 |

`ffmpeg/ffmpeg.exe` is **not** in publish output (intentional — Plan 02 installer will bundle it).

## Warnings During Publish

Pre-existing warnings (not introduced by this plan, out of scope per deviation rules):
- ~26× `CS8632` warnings in `Services/JsonService.cs` and `Services/VideoService.cs` — nullable reference type annotations present while `<Nullable>disable</Nullable>` in csproj. Harmless; pre-existing before this plan touched the csproj.
- 1× `CS1998` warning in `Forms/MainForm.cs:845` — async method without await. Pre-existing.

No warnings introduced by the csproj edits.

## Deviations from Plan

None — plan executed exactly as written.

## Installer Size Estimation for Plan 02

Based on 250 MB raw publish output, expect Inno Setup compressed installer to land in the 80–120 MB range (LZMA2 typically achieves ~50-65% compression on .NET self-contained payloads). Plus ~80 MB for bundled `ffmpeg.exe` → total `ASLT-Setup-v1.0.0.exe` likely in the 150–200 MB range.

## Success Criteria Check

- [x] PORT-02 (.NET 8 Runtime bundled) satisfied — `coreclr.dll` + `hostfxr.dll` in publish output
- [x] csproj metadata available for Inno Setup (Company, Copyright, FileVersion)
- [x] `installer/` directory exists and tracked via `.gitkeep`
- [x] `.gitignore` blocks `bin/`, `obj/`, `installer/Output/`, `publish/`, `*.user`
- [x] No C# behavior changes — only build configuration + metadata

## Self-Check: PASSED

Verified:
- `ASLTv1.0.csproj` — AssemblyVersion/FileVersion/Company/Copyright/SelfContained present (grep count 1 each)
- `.gitignore` — `installer/Output/` and `publish/` lines present (grep count 1 each)
- `installer/.gitkeep` — file exists
- `bin/Release/net8.0-windows/win-x64/publish/ASLTv1.exe` — exists, 147 KB
- `bin/Release/net8.0-windows/win-x64/publish/coreclr.dll` — exists
- `bin/Release/net8.0-windows/win-x64/publish/OpenCvSharpExtern.dll` — exists
- `bin/Release/net8.0-windows/win-x64/publish/opencv_videoio_ffmpeg4110_64.dll` — exists
- Commits `950f04f` and `85b8144` in `git log --oneline`
