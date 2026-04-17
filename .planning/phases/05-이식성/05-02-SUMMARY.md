---
phase: 05-이식성
plan: 02
subsystem: installer-packaging
tags: [installer, inno-setup, packaging, port-01, port-03, windows, uninstaller]
requires:
  - "self-contained win-x64 publish artifact at bin/Release/net8.0-windows/win-x64/publish/ (from 05-01)"
  - "csproj Publisher/Copyright/FileVersion metadata (from 05-01)"
provides:
  - "installer/ASLT-Setup.iss — Inno Setup 6 script producing ASLT-Setup-v1.0.0.exe"
  - "installer/build.bat — one-command publish + ISCC compile automation"
  - "installer/README.md — maintainer build + clean-VM verification guide"
  - "gitignore entry for installer/ffmpeg/ (human-supplied binary)"
affects:
  - installer/ASLT-Setup.iss
  - installer/build.bat
  - installer/README.md
  - .gitignore
tech-stack:
  added:
    - "Inno Setup 6 (.iss declarative installer script; not a runtime dependency — build-time packaging only)"
  patterns:
    - "Inno Setup default uninstaller with AppId-pinned Uninstall registry key (no custom [Registry] section)"
    - "[UninstallDelete] for runtime-created dirs (logs/, ffmpeg/) that are not in the [Files] manifest"
    - "Relative Source paths from .iss file location (`..\\bin\\Release\\...\\publish`) for repo-portable builds"
    - "Prereq checks in .bat before expensive publish step (fail fast on missing ffmpeg.exe / ISCC.exe)"
key-files:
  created:
    - installer/ASLT-Setup.iss
    - installer/build.bat
    - installer/README.md
  modified:
    - .gitignore
decisions:
  - "AppId pinned to {{B4A2C1F0-8E4D-4A6B-9F3A-ASLT10000001}} so future 1.0.x releases upgrade in place (do NOT regenerate between versions)."
  - "Languages: Korean primary + English fallback (compiler:Languages\\Korean.isl + compiler:Default.isl) — matches user base while giving English speakers a readable wizard."
  - "ArchitecturesAllowed=x64 + MinVersion=10.0.17763 — blocks install on 32-bit or pre-1809 Windows (aligns with PORT-01 target matrix)."
  - "gitignore installer/ffmpeg/ — bundled FFmpeg binary is human-supplied per release (licensing + size); not committed."
  - "No Pascal scripting, no [Registry], no SignTool — installer is pure declarative for easy review and certification audit."
metrics:
  duration: "~5 minutes"
  tasks_completed: 3
  files_created: 3
  files_modified: 1
  completed: "2026-04-17"
---

# Phase 05 Plan 02: 인스톨러 스크립트 Summary

**One-liner:** Inno Setup 6 script + build.bat automation + maintainer README that package the self-contained publish output (from Plan 01) plus bundled FFmpeg into `ASLT-Setup-v1.0.0.exe` with clean install to `C:\Program Files\ANNA\ASLT` and clean uninstall via default Inno uninstaller.

## What Shipped

### 1. `installer/ASLT-Setup.iss` (72 lines)

Inno Setup 6 declarative script with the following directive sections:

| Section            | Purpose                                                                              |
| ------------------ | ------------------------------------------------------------------------------------ |
| `[Setup]`          | AppId (pinned), AppName/Version/Publisher, `DefaultDirName={autopf}\ANNA\ASLT`, `OutputBaseFilename=ASLT-Setup-v1.0.0`, `ArchitecturesAllowed=x64`, `PrivilegesRequired=admin`, `MinVersion=10.0.17763` |
| `[Languages]`      | `korean` (primary) + `english` (fallback)                                           |
| `[Tasks]`          | `desktopicon` (optional, unchecked by default — D-03)                                |
| `[Files]`          | `..\bin\Release\net8.0-windows\win-x64\publish\*` (recursesubdirs) + `ffmpeg\ffmpeg.exe` → `{app}\ffmpeg` |
| `[Icons]`          | Start Menu always (app + uninstaller); Desktop conditional on `Tasks: desktopicon`   |
| `[Run]`            | Optional post-install launch of ASLTv1.exe                                           |
| `[UninstallDelete]` | `filesandordirs` for `{app}\logs` + `{app}\ffmpeg`; `dirifempty` sweep of `{app}`    |

Verified absent: `[Registry]` section, `SignTool=` directive, Pascal `[Code]` block — per CONTEXT.md decisions D-10 and D-14.

### 2. `installer/build.bat` (41 lines)

Three-step automation:

1. **Prereq verify** — Fail with exit code 1 if `installer\ffmpeg\ffmpeg.exe` or `C:\Program Files (x86)\Inno Setup 6\ISCC.exe` missing.
2. **dotnet publish** — `dotnet publish ASLTv1.0.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false` (from repo root via `pushd %REPO_ROOT%`).
3. **ISCC compile** — `ISCC.exe ASLT-Setup.iss` → produces `installer\Output\ASLT-Setup-v1.0.0.exe`.

All path references use `%~dp0` so the script is location-independent.

### 3. `installer/README.md` (90 lines)

Maintainer guide with:

- Prerequisites (.NET 8 SDK, Inno Setup 6, gyan.dev FFmpeg essentials build)
- Option A (build.bat one-command) and Option B (manual publish + ISCC) workflows
- Clean-VM verification protocol (6-step numbered checklist) explicitly mapped to PORT-01, PORT-02, PORT-03 acceptance
- File layout diagram for post-install state
- Troubleshooting section (common ISCC / FFmpeg errors)

### 4. `.gitignore` update

Added `installer/ffmpeg/` so human-supplied FFmpeg binary is never accidentally committed.

## Requirements Coverage

| ID      | Requirement                                          | Coverage                                                                                  |
| ------- | ---------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| PORT-01 | 명시된 Windows 환경에서 정상 설치·실행·제거 보장     | `[Setup]` constraints (x64, Win10 1809+, admin) + clean-VM protocol in README             |
| PORT-03 | 제거 시 잔존 파일/레지스트리 없음                     | Default Inno uninstaller + `[UninstallDelete]` for logs/ffmpeg + no custom [Registry] writes |

PORT-02 (bundled runtime + FFmpeg) is carried from 05-01's self-contained publish, consumed here via `[Files]` recurse entry + `installer/ffmpeg/ffmpeg.exe` → `{app}\ffmpeg\ffmpeg.exe`.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added `.gitignore` entry for `installer/ffmpeg/`**
- **Found during:** Task 3 (README writing)
- **Issue:** README claims `installer/ffmpeg/` is NOT committed to git, but `.gitignore` only had `installer/Output/`. Without the exclude, a future maintainer running `git add installer/` would silently commit the FFmpeg binary, violating the documented policy and bloating the repo.
- **Fix:** Added `installer/ffmpeg/` to `.gitignore` next to the existing `installer/Output/` entry.
- **Files modified:** `.gitignore`
- **Commit:** `6178bc3`

Otherwise: plan executed exactly as written.

## Commits

| Task | Commit   | Message                                                     |
| ---- | -------- | ----------------------------------------------------------- |
| 1    | `04db3ad` | feat(05-02): add Inno Setup installer script                |
| 2    | `d46c071` | feat(05-02): add build.bat publish+ISCC automation          |
| 3    | `6178bc3` | docs(05-02): add installer build + clean-VM verification README |

## Open Items for Human Maintainer

1. **Install Inno Setup 6** from https://jrsoftware.org/isinfo.php (default path expected by build.bat).
2. **Download FFmpeg** (gyan.dev essentials, win-x64) and place `ffmpeg.exe` at `installer\ffmpeg\ffmpeg.exe`.
3. **Run `installer\build.bat`** from repo root → produces `installer\Output\ASLT-Setup-v1.0.0.exe`.
4. **Execute clean-VM verification** per README §"Verifying the installer" — this is the acceptance gate for PORT-01 and PORT-03 in GS certification evidence.

## Compile Attempt (optional)

Not performed — ISCC.exe is not installed on the executor machine (by design; CONTEXT.md explicitly assigns packaging to the human maintainer). Script syntax validated by direct review against Inno Setup 6 reference: all directives are standard (no deprecated/removed identifiers), constants use canonical forms (`{autopf}`, `{autoprograms}`, `{autodesktop}`, `{uninstallexe}`, `{app}`, `{src}`, `{cm:...}`), and the `[Files]` Source paths resolve correctly relative to the .iss file's parent dir.

## Self-Check: PASSED

- `installer/ASLT-Setup.iss` FOUND (72 lines, 7 sections, no [Registry])
- `installer/build.bat` FOUND (41 lines, all prereq checks present)
- `installer/README.md` FOUND (90 lines, PORT-01/02/03 + clean-VM protocol)
- `.gitignore` MODIFIED (installer/ffmpeg/ entry added)
- Commit `04db3ad` FOUND in git log (Task 1)
- Commit `d46c071` FOUND in git log (Task 2)
- Commit `6178bc3` FOUND in git log (Task 3)
