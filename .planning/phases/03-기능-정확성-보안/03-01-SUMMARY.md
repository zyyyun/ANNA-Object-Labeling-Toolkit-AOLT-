---
phase: 03-기능-정확성-보안
plan: 01
subsystem: json-export
tags: [coco-json, bbox-clamping, timestamps, category-id, func-accuracy]
dependency_graph:
  requires: []
  provides: [ClampToImage, TimeSpan-timestamps, clamped-bbox-export, clamped-bbox-load]
  affects: [Services/JsonService.cs, Helpers/CoordinateHelper.cs, Forms/MainForm.cs]
tech_stack:
  added: []
  patterns: [static-helper, optional-params, math-clamp]
key_files:
  created: []
  modified:
    - Helpers/CoordinateHelper.cs
    - Services/JsonService.cs
    - Forms/MainForm.cs
decisions:
  - Use TimeSpan.ToString(@"hh\:mm\:ss\.fff") for frame-relative timestamps instead of DateTime.Now
  - ClampToImage applied at both export and load for bidirectional correctness
  - LoadLabelingDataAsync gains optional frameWidth/frameHeight params (default 0 = skip clamping)
metrics:
  duration: ~10min
  completed: 2026-04-16
  tasks_completed: 2
  files_modified: 3
---

# Phase 03 Plan 01: COCO JSON 정확성 수정 Summary

**One-liner:** TimeSpan 기반 프레임 상대 타임스탬프, ClampToImage 좌표 클램핑(export+load), Math.Clamp 카테고리 ID fallback으로 COCO JSON 출력 정합성 수정

## What Was Built

COCO JSON 내보내기/로드의 세 가지 정확성 결함을 수정했다:

1. **타임스탬프 수정 (FUNC-02):** `DateTime.Now.AddSeconds(...)` 패턴을 `TimeSpan.FromSeconds(...)`으로 교체하여 프레임 상대 시간 기반 타임스탬프를 생성한다. 프레임 타임스탬프, entry/exit 트랙 타임스탬프 모두 적용.

2. **bbox 클램핑 (FUNC-03, COMP-01):** `CoordinateHelper.ClampToImage(rect, imageWidth, imageHeight)` 정적 메서드를 추가하고, export 시 및 load 시 양방향 클램핑을 적용한다. export에서 `frameWidth`/`frameHeight` 파라미터는 이미 있었으므로 ClampToImage 호출만 추가. load에서 `LoadLabelingDataAsync`에 `int frameWidth = 0, int frameHeight = 0` 선택적 파라미터 추가.

3. **카테고리 ID fallback (FUNC-04):** `Math.Min` 기반 fallback을 `Math.Clamp`로 교체하여 boxId가 0 이하인 경우도 올바른 범위(person 1~20, vehicle 21~24, event 25~34)로 클램핑.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | ClampToImage 헬퍼 추가 + JSON 타임스탬프 수정 | bfcdfcd | Helpers/CoordinateHelper.cs, Services/JsonService.cs |
| 2 | JSON 로드 시 bbox 클램핑 적용 | bfcdfcd | Services/JsonService.cs, Forms/MainForm.cs |

## Verification Results

- `grep -c "ClampToImage" Helpers/CoordinateHelper.cs` → 1
- `grep -c "TimeSpan.FromSeconds" Services/JsonService.cs` → 4
- `grep -c "DateTime.Now.AddSeconds" Services/JsonService.cs` → 0
- `grep -c "CoordinateHelper.ClampToImage" Services/JsonService.cs` → 2
- `grep -c "Math.Clamp" Services/JsonService.cs` → 3
- `dotnet build -c Release --no-incremental` → 0 errors, 28 warnings (all pre-existing)

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

## Self-Check: PASSED

- Helpers/CoordinateHelper.cs: FOUND ClampToImage method
- Services/JsonService.cs: FOUND TimeSpan.FromSeconds (4x), CoordinateHelper.ClampToImage (2x), Math.Clamp (3x), no DateTime.Now.AddSeconds
- Forms/MainForm.cs: FOUND updated LoadLabelingDataAsync call with FrameWidth/FrameHeight
- Commit bfcdfcd: FOUND in git log
