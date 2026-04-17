---
phase: 04-성능-사용성
plan: 01
subsystem: Annotation / Hit-Testing
tags: [performance, maintainability, refactor, MainForm]
requires: []
provides:
  - "Dictionary<int, List<BoundingBox>> _bboxByFrame O(1) index"
  - "GetBboxesForFrame(int) helper"
  - "HIT_MARGIN class constant (hit-testing inflation)"
affects:
  - "Forms/MainForm.cs (paint cache, hit-testing, waypoint propagation, event propagation)"
tech-stack:
  added: []
  patterns:
    - "Lazy-built per-frame dictionary index invalidated via InvalidateBoxCache()"
    - "Class-level constants for repeated hit-testing magic numbers"
key-files:
  created: []
  modified:
    - "Forms/MainForm.cs"
decisions:
  - "Keep range-scan LINQ (b.FrameIndex > x && b.FrameIndex <= y) — dictionary unsuitable for range queries"
  - "Lazy rebuild strategy (null-then-rebuild-on-demand) preferred over eager incremental maintenance to minimize churn across many mutation sites"
metrics:
  duration_min: 1.3
  completed: 2026-04-17
requirements: [PERF-01, MAINT-03]
---

# Phase 04 Plan 01: 딕셔너리 인덱싱 + 매직 넘버 추출 Summary

## One-Liner

프레임별 bbox 조회를 `Dictionary<int, List<BoundingBox>>` 기반 O(1) 룩업으로 전환하고 반복되던 `hitMargin=4` 지역 상수 3곳을 클래스 수준 `HIT_MARGIN` 상수로 통합.

## What Changed

### Task 1 — O(1) Dictionary Indexing (PERF-01)

- Added field `private Dictionary<int, List<BoundingBox>> _bboxByFrame = null;`
- Added helpers:
  - `RebuildBboxIndex()` — builds the per-frame dictionary from `boundingBoxes`
  - `GetBboxesForFrame(int frameIndex)` — lazy-builds if null, returns list or empty
- `InvalidateBoxCache()` expanded from expression-body to include `_bboxByFrame = null;` so all existing mutation paths (AddBox/RemoveBox/Undo/Redo/import/delete/etc.) also invalidate the new index automatically.
- Replaced 9 single-frame `boundingBoxes.Where(b => b.FrameIndex == x ...)` call sites with `GetBboxesForFrame(x).Where(...)`:
  - Entry/Exit waypoint creation (person/vehicle pairs) — 4 sites
  - Paint event cache rebuild
  - `GetBoundingBoxAt`, `HasAnotherHitCandidateAt`, `GetOrderedCandidatesAt`, selection-frame helper — 4 sites
- Range-scan LINQ preserved intentionally at the two waypoint-event propagation sites (`b.FrameIndex > x && b.FrameIndex <= y`) since dictionary lookup cannot answer range queries.

### Task 2 — Magic Number Extraction (MAINT-03)

- Added `private const int HIT_MARGIN = 4;` alongside existing `HANDLE_SIZE`, `MIN_BBOX_SIZE`, `MAX_UNDO_STACK`, `RESIZE_BORDER_WIDTH`.
- Removed three duplicate `const int hitMargin = 4;` locals (in `GetBoundingBoxAt`, `HasAnotherHitCandidateAt`, `GetOrderedCandidatesAt`) and switched the three `r.Inflate(hitMargin, hitMargin)` calls to `r.Inflate(HIT_MARGIN, HIT_MARGIN)`.

## Files Modified

- `Forms/MainForm.cs` — +40 / −16

## Verification

- `dotnet build -c Debug`: **0 errors**, 28 warnings (all pre-existing CS8632/CS1998, none introduced).
- Acceptance grep checks:
  - `_bboxByFrame` occurrences: 7 (field + 6 uses in Rebuild/Invalidate/GetBboxesForFrame)
  - `GetBboxesForFrame` + `RebuildBboxIndex` + `HIT_MARGIN` occurrences: 16
  - `hitMargin` locals remaining: 0
  - `const int hitMargin` remaining: 0

## Deviations from Plan

None — plan executed exactly as written. Both tasks committed together as a single `perf(bbox): ...` commit per the orchestrator instructions.

## Key Decisions

- **Lazy rebuild over incremental maintenance.** The existing `InvalidateBoxCache()` is already called from every bbox mutation path (Undo/Redo, AddBox/RemoveBox, import, delete, frame-move, etc. — ~15 call sites). Hooking into it for `_bboxByFrame = null` gives correct invalidation with zero new coupling, and the rebuild cost is O(n) only on the next read.
- **Preserve range-scan LINQ.** Only `b.FrameIndex == x` single-frame patterns benefit from hash-lookup. Range conditions (`> x && <= y`) at lines ~2177 and ~2413 stay on the List to keep semantics simple.
- **`cachedCurrentFrameBoxes` now aliases the dictionary bucket.** Previously it held a fresh `.ToList()` snapshot; now it references the same `List<BoundingBox>` stored in `_bboxByFrame`. This is safe because `InvalidateBoxCache()` nulls both the dictionary and `lastCachedFrameForPaint` in lockstep on every mutation, so Paint always re-acquires a fresh list through the same invalidation semantics.

## Commits

- `af07ba8` — perf(bbox): O(1) dict indexing + extract magic numbers (PERF-01, MAINT-03)

## Self-Check: PASSED

- Forms/MainForm.cs modified: FOUND
- Commit af07ba8 in git log: FOUND
- Build: 0 errors
- All acceptance criteria grep checks satisfied
