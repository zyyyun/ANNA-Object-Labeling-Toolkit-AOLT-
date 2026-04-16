---
phase: 03-기능-정확성-보안
plan: "04"
subsystem: error-handling
tags: [exception-handling, user-messages, reliability, maintainability]
dependency_graph:
  requires: [03-01, 03-02, 03-03]
  provides: [specific-exception-handling, user-friendly-error-messages]
  affects: [Services/JsonService.cs, Services/VideoService.cs, Forms/MainForm.cs]
tech_stack:
  added: []
  patterns: [specific-catch-before-generic, korean-error-messages-with-solution]
key_files:
  modified:
    - Services/JsonService.cs
    - Services/VideoService.cs
    - Forms/MainForm.cs
decisions:
  - loadPath variable moved outside try block in LoadLabelingDataAsync to allow access in catch blocks
metrics:
  duration: "~10 minutes"
  completed: "2026-04-16"
  tasks_completed: 2
  files_modified: 3
requirements: [RELI-05, USAB-03, MAINT-02]
---

# Phase 03 Plan 04: Exception Handling + User-Friendly Messages Summary

**One-liner:** Replaced generic catch(Exception) with ordered specific exception types and Korean error messages including 해결 방법 (solution) sections across all three service/form files.

## What Was Done

### Task 1: JsonService + VideoService Specific Exception Handling

**JsonService.LoadLabelingDataAsync** — Replaced single `catch (Exception ex)` after OutOfMemoryException with:
- `Newtonsoft.Json.JsonReaderException` — includes line/column position in message
- `Newtonsoft.Json.JsonSerializationException` — COCO format mismatch guidance
- `IOException` — file lock/access guidance
- `Exception` fallback

Also moved `loadPath` variable declaration outside the `try` block so catch blocks can reference the filename in error messages.

**JsonService backup catch** — Replaced single `catch (Exception backupEx)` with:
- `IOException` → Debug.WriteLine + Log.Warning
- `UnauthorizedAccessException` → Debug.WriteLine + Log.Warning
- `Exception` fallback

**JsonService.ExportToJsonExtended** — Added before generic catch:
- `IOException` → throws InvalidOperationException with 해결 방법
- `UnauthorizedAccessException` → throws InvalidOperationException with 해결 방법

**VideoService.LoadFrame** — Added before generic catch:
- `OpenCvSharp.OpenCVException` → Log.Error + return null

**VideoService.LoadSrtFileAsync** — Replaced generic catch with:
- `FormatException` → Log.Warning + subtitleEntries.Clear()
- `IOException` → Log.Warning
- `Exception` fallback

### Task 2: MainForm Specific Exception Handling + User Messages

**LoadVideoWithSubtitle** — After existing `OperationCanceledException`, added before generic catch:
- `IOException` — "파일이 존재하고 다른 프로그램에서 사용 중이지 않은지 확인하세요"
- `OpenCvSharp.OpenCVException` — "MP4(H.264) 형식의 비디오 파일을 사용하세요"

**LoadFrame** — Added `OpenCvSharp.OpenCVException` before generic catch.

**LoadLabelingData** — Added `IOException` before generic catch.

**btnExportJson inner catch** — Replaced with:
- `InvalidOperationException` — shows service-level message
- `IOException` — disk space / write permission guidance
- `Exception` fallback

**btnPlay** — Added `InvalidOperationException` with state reset before generic catch.

**btnExit** — Added `InvalidOperationException` with "Entry 마커를 먼저 설정한 후" guidance before generic catch.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] `loadPath` variable scope fix**
- **Found during:** Task 1
- **Issue:** `loadPath` was declared inside the `try` block in `LoadLabelingDataAsync`, making it inaccessible in the new specific catch blocks that needed to reference `Path.GetFileName(loadPath)`.
- **Fix:** Moved `string loadPath = "";` declaration to just before the `try` block, changed inner assignment from `string loadPath = normalPath;` to `loadPath = normalPath;`.
- **Files modified:** Services/JsonService.cs
- **Commit:** fe7a2c8 (included in main task commit)

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| Task 1 + Task 2 | fe7a2c8 | fix(errors): specific exception handling + user-friendly messages |

## Known Stubs

None — all exception handling is fully wired with real messages.

## Self-Check: PASSED

- Services/JsonService.cs modified with JsonReaderException, JsonSerializationException, IOException catches
- Services/VideoService.cs modified with OpenCVException, FormatException catches
- Forms/MainForm.cs modified with IOException, OpenCVException, InvalidOperationException catches
- Build: 0 errors, 28 warnings (pre-existing nullable warnings, not introduced by this plan)
- Commit fe7a2c8 confirmed present
