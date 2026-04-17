---
phase: 05-이식성
verified: 2026-04-17T00:00:00Z
status: human_needed
score: 3/3 must-haves verified (static) — 2/3 require clean-VM confirmation
re_verification: null
human_verification:
  - test: "Clean Windows 10/11 x64 VM install + launch"
    expected: "ASLT-Setup-v1.0.0.exe installs without additional prerequisites; ASLTv1.exe launches from Start Menu; no '.NET Runtime missing' dialog"
    why_human: "Requires fresh VM without .NET/FFmpeg/VC++ runtimes pre-installed; cannot simulate programmatically from dev machine"
  - test: "Uninstall residue check"
    expected: "After Settings → Apps → Uninstall: C:\\Program Files\\ANNA\\ASLT\\ is empty/gone; Start Menu shortcut removed; HKLM\\...\\Uninstall\\ASLT key removed; user COCO JSON outside install dir untouched"
    why_human: "Requires executing installer + uninstaller on real Windows, inspecting filesystem and registry; no static analysis can confirm runtime uninstaller behavior"
  - test: "Installer build + artifact produced"
    expected: "installer/build.bat produces installer/Output/ASLT-Setup-v1.0.0.exe (~150-200 MB)"
    why_human: "Requires Inno Setup 6 (ISCC.exe) installed on maintainer machine + human-supplied installer/ffmpeg/ffmpeg.exe; per CONTEXT.md packaging is explicitly assigned to human maintainer"
---

# Phase 5: 이식성 Verification Report

**Phase Goal:** 명시된 Windows 환경에서 정상 설치·실행·제거가 보장된다
**Verified:** 2026-04-17
**Status:** human_needed (static code review passes all checks; runtime acceptance requires clean-VM test)
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths (= Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Win10/11 클린 환경에서 설치 후 추가 설정 없이 앱 정상 실행 | ? HUMAN NEEDED | csproj SelfContained=true + RuntimeIdentifier=win-x64 verified; publish/ contains coreclr.dll + hostfxr.dll (bundled runtime); .iss [Files] recurses publish/; but actual clean-VM launch requires human |
| 2 | FFmpeg 또는 .NET Runtime 미설치 시 구체적 안내 | VERIFIED | .NET: self-contained so N/A (runtime bundled, cannot be missing). FFmpeg: MainForm.cs:174-184 shows `MessageBox.Show("FFmpeg가 설치되지 않았습니다...")` with concrete remediation steps when `IsFFmpegAvailable=false`. Installer bundles ffmpeg at {app}/ffmpeg/ffmpeg.exe so fallback only fires if user deletes it. |
| 3 | 제거 후 레지스트리/파일 잔여 없음 | ? HUMAN NEEDED | .iss has [UninstallDelete] for {app}\logs + {app}\ffmpeg + dirifempty {app}; NO custom [Registry] section (Inno auto-manages only Uninstall\ASLT key which it also auto-removes). Static structure is correct; runtime uninstall residue requires VM test. |

**Score:** 1/3 fully verified via static analysis; 2/3 correctly designed but require human VM acceptance per CONTEXT.md packaging delegation.

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `ASLTv1.0.csproj` | Version, Company, Product, SelfContained=true, RuntimeIdentifier=win-x64 | VERIFIED | All 6 fields present (lines 13-24). Version=1.0.0, Company=ANNA, Product=ASLT..., SelfContained=true, RuntimeIdentifier=win-x64, AssemblyVersion+FileVersion+Copyright+AssemblyTitle+Description also added. |
| `installer/ASLT-Setup.iss` | [Setup] w/ AppName/Version/DefaultDirName/OutputBaseFilename, [Files], [Tasks] desktopicon, [UninstallDelete], NO [Registry], NO SignTool | VERIFIED | 72 lines. DefaultDirName={autopf}\ANNA\ASLT (L23). OutputBaseFilename=ASLT-Setup-v1.0.0 (L27). [Files] recurses publish\* + ffmpeg\ffmpeg.exe (L51-55). [Tasks] desktopicon unchecked (L49). [UninstallDelete] logs+ffmpeg+dirifempty (L67-72). Confirmed absent: [Registry], SignTool, [Code]. |
| `installer/build.bat` | dotnet publish + ISCC.exe | VERIFIED | 41 lines. Step 1 checks ffmpeg.exe + ISCC.exe prereqs (L12-19). Step 2 runs dotnet publish with correct flags (L23). Step 3 invokes ISCC (L32). Uses %~dp0 for location independence. |
| `installer/README.md` | Inno Setup explanation, FFmpeg download source, build procedure, clean-VM verification | VERIFIED | 91 lines. Covers prerequisites (.NET 8 SDK, Inno Setup 6, gyan.dev FFmpeg link), Option A/B build workflows, explicit clean-VM 6-step checklist mapped to PORT-01/02/03, post-install file layout, troubleshooting. |
| `.gitignore` | Excludes installer/Output/, publish/, installer/ffmpeg/ | VERIFIED | Lines 4-6 contain `installer/Output/`, `installer/ffmpeg/`, `publish/`. Also bin/ obj/ .claude/ *.user. |
| `bin/Release/net8.0-windows/win-x64/publish/` | Contains ASLTv1.exe + bundled runtime | VERIFIED | 266 files present. ASLTv1.exe verified. coreclr.dll verified present (proves self-contained bundling). OpenCvSharpExtern.dll present (per summary). |

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| ASLT-Setup.iss [Files] | publish/ output | `..\bin\Release\net8.0-windows\win-x64\publish\*` | WIRED | Relative path resolves correctly from installer/.iss parent dir; recursesubdirs flag present. publish/ exists with 266 files. |
| ASLT-Setup.iss [Files] | bundled FFmpeg | `Source: "ffmpeg\ffmpeg.exe"` → `{app}\ffmpeg` | WIRED | Path is relative to .iss file (installer/ffmpeg/ffmpeg.exe — human-supplied); consumed by VideoService.SetupFFmpegPath() at runtime (L557-563 probes {app}/ffmpeg/ffmpeg.exe). |
| build.bat | ASLT-Setup.iss | `%ISCC% "%~dp0ASLT-Setup.iss"` | WIRED | Path resolution correct; prereq check for ISCC.exe path before invocation. |
| MainForm initialization | FFmpeg availability warning | `_videoService.SetupFFmpegPath()` + `!IsFFmpegAvailable` branch → MessageBox | WIRED | MainForm.cs:173-184. Concrete Korean error message with 2 remediation options shown at startup if FFmpeg missing. This satisfies SC #2 fallback (Phase 4 COMP-02 carry-over). |
| Inno uninstaller | filesystem cleanup | [UninstallDelete] types `filesandordirs` + `dirifempty` | WIRED (static) | Covers runtime-created logs/ + ffmpeg/ that wouldn't be tracked by [Files] manifest removal. Default Inno behavior handles manifest-tracked files. Runtime validation pending VM test. |

### Data-Flow Trace (Level 4)

Not applicable — Phase 5 produces build/packaging artifacts, not data-rendering code.

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Self-contained publish produces runnable exe | Inspect publish/ASLTv1.exe + coreclr.dll existence | Both present (266 files total, 250 MB per summary 05-01) | PASS |
| csproj has valid XML + required metadata | Read ASLTv1.0.csproj | All 6 required fields present, XML well-formed | PASS |
| .iss has no forbidden directives | Grep [Registry], SignTool in ASLT-Setup.iss | Neither present | PASS |
| build.bat syntactically correct + idempotent | Read build.bat | Proper %~dp0 usage, errorlevel checks after each step, fail-fast prereq validation | PASS |
| Installer compile (ISCC.exe) | ISCC ASLT-Setup.iss | SKIP — ISCC not installed on executor per CONTEXT.md (packaging = human maintainer task) | SKIP |
| Clean-VM install/run/uninstall | Manual VM test | Deferred to human per Phase 5 CONTEXT.md | SKIP |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| PORT-01 | 05-02 | 명시된 Windows 환경에서 정상 설치·실행·제거 보장 | NEEDS HUMAN | Static: ArchitecturesAllowed=x64 + MinVersion=10.0.17763 + PrivilegesRequired=admin in .iss; clean-VM protocol documented in installer/README.md. Runtime: pending VM test. |
| PORT-02 | 05-01 | 런타임 번들링(.NET 8 + 의존성) 보장 | SATISFIED | SelfContained=true + RuntimeIdentifier=win-x64 in csproj; publish/ contains coreclr.dll + hostfxr.dll (266 files, 250 MB); FFmpeg bundled via .iss [Files] ffmpeg entry. |
| PORT-03 | 05-02 | 제거 시 잔존 파일/레지스트리 없음 | NEEDS HUMAN | Static: [UninstallDelete] covers logs+ffmpeg+dirifempty; NO custom [Registry] writes (Inno only creates+removes its own Uninstall\ASLT key). Runtime: pending VM uninstall check. |

No orphaned requirements — REQUIREMENTS.md lists PORT-01/02/03 for Phase 5, all 3 claimed by plans.

### Anti-Patterns Found

None. Files scanned:
- `ASLTv1.0.csproj` — clean, no TODO/FIXME/placeholder
- `installer/ASLT-Setup.iss` — clean, all directives intentional and documented
- `installer/build.bat` — clean, fail-fast prereq checks, explicit error messages
- `installer/README.md` — clean, accurate maintainer doc

### CONTEXT.md Decision Alignment

| Decision | Implementation | Status |
|----------|----------------|--------|
| D: Inno Setup .iss (NOT WiX/MSIX) | installer/ASLT-Setup.iss present, no .wxs/.msix files | ALIGNED |
| D-02: Install path `{autopf}\ANNA\ASLT` | .iss L23 `DefaultDirName={autopf}\ANNA\ASLT` | ALIGNED |
| D-04/05: Self-contained .NET | csproj SelfContained=true + RuntimeIdentifier=win-x64; coreclr.dll in publish/ | ALIGNED |
| D-07: FFmpeg bundled at `{app}/ffmpeg/ffmpeg.exe` | .iss L55 `DestDir: "{app}\ffmpeg"` + VideoService.cs:557-563 probes this path | ALIGNED |
| D-10: No code signing | .iss has no SignTool directive | ALIGNED |
| D-15: Clean uninstall via [UninstallDelete] | .iss L67-72 filesandordirs + dirifempty | ALIGNED |
| D-14: No custom [Registry] entries | grep confirms no [Registry] section in .iss | ALIGNED |
| D-17: Installer filename `ASLT-Setup-v1.0.0.exe` | .iss L27 `OutputBaseFilename=ASLT-Setup-v{#MyAppVersion}` with MyAppVersion=1.0.0 → `ASLT-Setup-v1.0.0.exe` | ALIGNED |

All 8 locked decisions implemented correctly.

### Human Verification Required

#### 1. Clean-VM Install + Launch (SC #1, PORT-01)

**Test:** Provision fresh Win10 1809+ or Win11 x64 VM with NO .NET Runtime, NO FFmpeg, NO Visual Studio tooling. Copy `ASLT-Setup-v1.0.0.exe` to VM, run installer, accept wizard defaults, launch from Start Menu.
**Expected:** Installer completes without prereq prompts; ASLTv1.exe opens main window; no ".NET Runtime missing" dialog; loading a video with embedded SRT succeeds (confirms bundled FFmpeg works).
**Why human:** Dev machine has .NET 8 SDK + prior runtimes installed; cannot simulate absence. Clean VM is the only valid acceptance environment for PORT-01.

#### 2. Uninstall Residue Check (SC #3, PORT-03)

**Test:** On the same VM, go to Settings → Apps → ASLT → Uninstall. After completion, inspect:
- `C:\Program Files\ANNA\ASLT\` directory (should be gone or empty)
- Start Menu `ANNA\ASLT` group (should be gone)
- Desktop shortcut (gone, if enabled during install)
- `HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\` for `ASLT` or AppId key (should be removed by Inno)
- Any user-saved COCO JSON files OUTSIDE `C:\Program Files\ANNA\ASLT` (should be untouched)
**Expected:** No residue in any of the above locations except user JSON outside install dir.
**Why human:** Requires actually running the uninstaller + registry/filesystem inspection on real Windows.

#### 3. Installer Build Artifact Production

**Test:** On maintainer machine (with .NET 8 SDK + Inno Setup 6 installed + `installer/ffmpeg/ffmpeg.exe` downloaded from gyan.dev essentials build), run `installer\build.bat` from repo root.
**Expected:** Script completes with exit 0; produces `installer\Output\ASLT-Setup-v1.0.0.exe` in expected size range (150–200 MB).
**Why human:** ISCC.exe not installed on executor; FFmpeg binary must be human-supplied per licensing. CONTEXT.md explicitly assigns packaging to human maintainer.

### Gaps Summary

**No blocking static gaps.** All files, metadata, installer directives, wiring, and CONTEXT.md decisions are correctly implemented. The phase goal "명시된 Windows 환경에서 정상 설치·실행·제거가 보장된다" has been built correctly per the plan, but final acceptance of Success Criteria #1 (clean install/run) and #3 (no uninstall residue) requires execution on a clean Windows VM — which is explicitly a human maintainer responsibility per CONTEXT.md D-10 (no auto-sign) and the README §"Verifying the installer" protocol.

Success Criterion #2 (FFmpeg/.NET missing → concrete message) is **fully satisfied statically**: .NET Runtime cannot be missing (self-contained), and FFmpeg absence triggers a specific Korean MessageBox with 2 remediation options (MainForm.cs:177-184) — this is defensive code that activates only if the user deletes the bundled binary.

**Recommendation to orchestrator:** Mark Phase 5 as complete for code-level deliverables; gate final ROADMAP sign-off on clean-VM maintainer test evidence (screenshots + filesystem/registry inspection) attached to the phase record.

---

_Verified: 2026-04-17_
_Verifier: Claude (gsd-verifier)_
