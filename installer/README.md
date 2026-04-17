# ASLT Installer — Build & Package Guide

This directory builds `ASLT-Setup-v1.0.0.exe`, the Windows installer for
**ANNA 합성데이터 라벨링 툴킷 (ASLT) v1.0.0**. Per Phase 5 decisions:

- Installer: **Inno Setup 6** (`.iss` script)
- Deployment: **Self-contained .NET 8 x64** (runtime bundled — no user install)
- FFmpeg: **Bundled** at `<InstallDir>\ffmpeg\ffmpeg.exe`
- Code signing: **None** (SmartScreen warnings accepted for internal cert process)
- Default install path: `C:\Program Files\ANNA\ASLT`

## Prerequisites (one-time setup)

1. **.NET 8 SDK** — https://dotnet.microsoft.com/download/dotnet/8.0
2. **Inno Setup 6** — https://jrsoftware.org/isinfo.php
   Install to default path `C:\Program Files (x86)\Inno Setup 6\`.
3. **FFmpeg binary** — https://www.gyan.dev/ffmpeg/builds/
   - Download: `ffmpeg-release-essentials.zip` (Windows x64)
   - Extract `bin\ffmpeg.exe` → place at `installer\ffmpeg\ffmpeg.exe` in this repo.
   - `installer\ffmpeg\` is NOT committed to git (it is under `installer/` but contains a binary).

## Build & Package (per release)

### Option A — One command (recommended)

From the repo root:

```cmd
installer\build.bat
```

This runs `dotnet publish` + `ISCC.exe` and produces:
`installer\Output\ASLT-Setup-v1.0.0.exe`

### Option B — Manual (for debugging)

```cmd
REM Step 1: Publish self-contained x64 build
dotnet publish ASLTv1.0.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false

REM Step 2: Compile installer
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\ASLT-Setup.iss
```

## Verifying the installer (clean VM test)

This step is **human-only** — no Claude automation.

1. Spin up a clean Windows 10/11 x64 VM (no .NET, no FFmpeg, no Visual C++ tooling).
2. Copy `ASLT-Setup-v1.0.0.exe` to the VM. Run it.
3. Click through wizard → install completes without error.
4. Launch ASLT from Start Menu. Confirm:
   - App window opens
   - Load a video with embedded SRT → SRT extraction succeeds (confirms bundled FFmpeg)
   - No "missing .NET Runtime" prompt (confirms self-contained bundling — PORT-02)
5. Go to Settings → Apps → ASLT → Uninstall.
6. Confirm after uninstall:
   - `C:\Program Files\ANNA\ASLT\` is fully removed
   - Start Menu shortcut gone; Desktop shortcut gone (if enabled during install)
   - Registry key `HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\ASLT` is removed (Inno auto-manages)
   - Any COCO JSON files the user saved OUTSIDE `C:\Program Files\ANNA\ASLT` are untouched (PORT-03)

## What the installer does NOT do

- Sign binaries (SmartScreen warning on first run is expected — click "More info → Run anyway")
- Write custom registry keys (only Inno's auto-managed `Uninstall\ASLT`)
- Install .NET Runtime separately (runtime is bundled)
- Install Visual C++ Redistributable (OpenCvSharp 4.11 natives built against VC runtime already shipped in Win10/11)

## File layout after install

```
C:\Program Files\ANNA\ASLT\
├── ASLTv1.exe                          self-contained entry point
├── *.dll                                bundled .NET + OpenCvSharp + Serilog
├── OpenCvSharpExtern.dll                OpenCV native
├── opencv_videoio_ffmpeg4110_64.dll     OpenCV FFmpeg bridge
├── ffmpeg\ffmpeg.exe                    bundled FFmpeg (SRT extraction)
├── logs\                                 runtime-created by Serilog
└── unins000.exe                          Inno uninstaller (auto-created)
```

## Troubleshooting

- **ISCC error "File not found: ..\bin\Release\net8.0-windows\win-x64\publish\*"**
  Run `dotnet publish` first (or use `build.bat`).
- **"ffmpeg.exe not found"** from `build.bat`
  Download FFmpeg essentials build and place `ffmpeg.exe` in `installer\ffmpeg\`.
- **App reports "FFmpeg not available" after install**
  Verify `C:\Program Files\ANNA\ASLT\ffmpeg\ffmpeg.exe` exists. `VideoService.SetupFFmpegPath()` probes this location first.
