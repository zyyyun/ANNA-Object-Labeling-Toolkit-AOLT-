---
phase: 04-성능-사용성
plan: 02
subsystem: UX / Safety / Compatibility
tags: [usability, compatibility, dirty-flag, dialogs, ffmpeg, MainForm, VideoService]
requires: []
provides:
  - "_isDirty flag lifecycle in MainForm"
  - "Unsaved-changes YesNoCancel confirm on app close"
  - "Unsaved-changes YesNoCancel confirm on video switch"
  - "VideoService.IsFFmpegAvailable public property"
  - "FFmpeg-missing startup MessageBox + warning log"
affects:
  - "Forms/MainForm.cs (fields, AddUndoAction, SaveCurrentLabelingData, btnSelectFolder_Click, LoadVideoWithSubtitle, OnFormClosing, startup flow)"
  - "Services/VideoService.cs (properties, SetupFFmpegPath, ExtractSrtFromVideoAsync)"
tech-stack:
  added: []
  patterns:
    - "Dirty-flag lifecycle: set on every AddUndoAction, cleared on save + on successful video load"
    - "YesNoCancel save-confirm pattern (Yes→save, No→continue, Cancel→abort)"
    - "Startup capability probe with user-facing guidance MessageBox (FFmpeg)"
key-files:
  created:
    - ".planning/phases/04-성능-사용성/04-02-SUMMARY.md"
  modified:
    - "Forms/MainForm.cs"
    - "Services/VideoService.cs"
decisions:
  - "Route dirty-flag through AddUndoAction rather than instrumenting every mutation site — single chokepoint already exists for undo/redo"
  - "Use YesNoCancel (not YesNo) so Cancel truly aborts app-close / video-switch without losing either option"
  - "Show FFmpeg-missing dialog once at startup (not on each SRT extraction attempt) to avoid repeat nagging; still log per-attempt warning"
  - "Reset _isDirty on successful video load (before LoadLabelingData) so the just-loaded JSON state counts as clean baseline"
metrics:
  duration_min: 6
  completed: 2026-04-17
requirements: [USAB-02, USAB-04, COMP-02]
---

# Phase 04 Plan 02: 미저장 변경사항 경고 + FFmpeg 안내 Summary

## One-Liner

`_isDirty` 플래그와 YesNoCancel 저장 확인 다이얼로그(앱 종료/영상 전환)로 편집 손실을 방지하고, FFmpeg 미설치 시 시작 시 안내 MessageBox와 경고 로그를 통해 호환성 이슈를 사용자에게 명확히 노출.

## What Changed

### Task 1 — _isDirty + Save-Confirm Dialogs (USAB-02, USAB-04)

- **Field:** `private bool _isDirty = false;` (UI State region).
- **Set dirty:** `AddUndoAction()` 첫 줄에 `_isDirty = true;` — 모든 편집(bbox add/remove/modify, waypoint, 기타 undo 액션)이 일원화된 진입점을 통하므로 단일 hook 으로 커버.
- **Clear dirty:** `SaveCurrentLabelingData()` 마지막에 `_isDirty = false;` (2곳 — 저장 성공 시, 신규 영상 로드 성공 시).
- **Close guard:** `OnFormClosing` 시작부에 `_isDirty && IsVideoLoaded` 체크. YesNoCancel MessageBox, Yes→`SaveCurrentLabelingData()`, Cancel→`e.Cancel=true; return;`.
- **Switch guard:** `btnSelectFolder_Click` 에서 OpenFileDialog OK 후, `LoadVideoWithSubtitle` 호출 전에 동일 YesNoCancel 체크. Cancel 시 early-return.

### Task 2 — FFmpeg Missing Message (COMP-02)

- **VideoService.cs:**
  - Added `public bool IsFFmpegAvailable => isFFmpegAvailable;`
  - `SetupFFmpegPath()`: 로컬 폴더에도 ffmpeg.exe 없으면 `Log.Warning(...)` with install guidance; outer catch 도 exception 로깅.
  - `ExtractSrtFromVideoAsync()`: 기존 silent early-return 에 `Log.Warning("FFmpeg 미설치로 자막 추출을 건너뜁니다: {VideoPath}", videoPath);` 추가.
- **MainForm.cs:** `_videoService.SetupFFmpegPath()` 직후 `!IsFFmpegAvailable` 분기에서 `Log.Warning` + MessageBox (설치 안내: PATH 추가 vs `ffmpeg/ffmpeg.exe` 배치).

## Key Decisions

- **Single choke-point for dirty tracking** — AddUndoAction 은 이미 모든 undo-able 편집의 통합 진입점이므로 각 편집 핸들러마다 `_isDirty=true` 를 뿌리지 않음. 미래에 undo 없이 state 를 바꾸는 경로가 추가되면 그 자리에 명시적으로 설정해야 함.
- **Baseline clean on load** — 영상 로드 성공 후 즉시 `_isDirty=false` 를 설정해, 뒤이어 호출되는 `LoadLabelingData` 가 내부적으로 collection 변형을 해도 사용자 관점의 "방금 불러온 상태"가 clean 으로 유지됨.
- **Once-at-startup FFmpeg dialog** — 자막 추출 시마다 MessageBox 로 방해하지 않고, 시작 시 1회만 안내. 이후 시도는 로그에만 기록.

## Verification

- `dotnet build --no-restore -c Debug`: **0 errors** (28 warnings, all pre-existing nullable/async-without-await noise — unchanged from baseline).
- Acceptance checks:
  - `grep "private bool _isDirty" Forms/MainForm.cs` → 1 match.
  - `grep -A2 "void AddUndoAction" Forms/MainForm.cs` contains `_isDirty = true`.
  - `grep -c "_isDirty = false" Forms/MainForm.cs` → 3 (SaveCurrentLabelingData, LoadVideoWithSubtitle post-load, declaration initializer).
  - `grep "저장하지 않은 변경사항"` → 1 (OnFormClosing).
  - `grep "저장하지 않은 편집"` → 1 (btnSelectFolder_Click).
  - `grep "e.Cancel = true"` → 1 (OnFormClosing cancel branch).
  - `grep "IsFFmpegAvailable" Services/VideoService.cs` → property present.
  - `grep "FFmpeg를 찾을 수 없습니다" Services/VideoService.cs` → warning log present.
  - `grep "FFmpeg가 설치되지 않았습니다" Forms/MainForm.cs` → user message present.

## Deviations from Plan

None — plan executed exactly as written. The plan specified 3 `_isDirty = false` occurrences; achieved by (1) field declaration initializer, (2) SaveCurrentLabelingData, (3) LoadVideoWithSubtitle. Plan's acceptance check asked "2줄 이상" — satisfied (3).

## Known Stubs

None. All dialog branches are wired to real actions (`SaveCurrentLabelingData`, `e.Cancel=true`, early return).

## Commits

- `fb7f117` feat(04-02): add _isDirty flag with save confirm dialogs (USAB-02, USAB-04)
- `48a5aab` feat(04-02): FFmpeg-missing user message and logging (COMP-02)

## Files Touched

- `Forms/MainForm.cs` — +50 lines (field, 2 dialogs, startup FFmpeg message, isDirty set/clear)
- `Services/VideoService.cs` — +7 lines (property, 2 log warnings)

## Self-Check: PASSED

- FOUND: Forms/MainForm.cs (modified, grep checks pass)
- FOUND: Services/VideoService.cs (modified, grep checks pass)
- FOUND commit fb7f117
- FOUND commit 48a5aab
- Build verified: 0 errors
