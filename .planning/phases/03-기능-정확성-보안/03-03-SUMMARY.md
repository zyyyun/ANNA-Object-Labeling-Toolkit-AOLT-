---
phase: 03-기능-정확성-보안
plan: 03
subsystem: MainForm
tags: [bug-fix, state-reset, delete-unify, shortcut, clamping, FUNC]
dependency_graph:
  requires: [03-01, 03-02]
  provides: [FUNC-01, FUNC-05, FUNC-06, FUNC-07, FUNC-08, FUNC-09, FUNC-10]
  affects: [Forms/MainForm.cs]
tech_stack:
  added: []
  patterns:
    - boundingBoxes.Remove() unified delete path
    - CoordinateHelper.ClampToImage applied at all 4 bbox mutation sites
    - selectedBox.Label-first shortcut dispatch (no cross-label conflicts)
key_files:
  modified:
    - Forms/MainForm.cs
decisions:
  - selectedBox.Label wins over currentSelectedLabel for Ctrl+N shortcut dispatch
  - FindWaypointForBox(b)==waypoint filter added to ChangeBoxIdWithinWaypoint to prevent cross-track ID mutation
  - WASD/draw/drag/resize all clamp via CoordinateHelper.ClampToImage
metrics:
  duration: 8min
  completed: 2026-04-16
  tasks_completed: 3
  files_modified: 1
---

# Phase 03 Plan 03: MainForm Core Bug Fixes Summary

**One-liner:** Fixed 7 functional bugs in MainForm — state reset on video load, delete path unification, vehicle/event shortcut conflict resolution, waypoint scope filter, and bbox clamping at all 4 interaction sites.

## What Was Done

### Task 1: State reset + delete unification (FUNC-09, FUNC-10)

Added complete state reset block in `LoadLabelingData` after existing `.Clear()` calls:
- `undoStack.Clear()`, `redoStack.Clear()`
- `entryFrameIndex = null`, `exitFrameIndex = null`
- `currentMode = DrawMode.Select`
- `currentAssignedId = 1`
- `isDrawing = false`, `isDragging = false`, `isResizing = false`

Changed Delete key handler from `selectedBox.IsDeleted = true` to `boundingBoxes.Remove(selectedBox)` + `InvalidateBoxCache()`, unifying with `btnDeleteLabel_Click` path.

### Task 2: Shortcut key conflicts + ID change scope (FUNC-07, FUNC-08)

Restructured vehicle/event Ctrl+1~N shortcut blocks to check `selectedBox.Label` first:
- If `selectedBox != null && selectedBox.Label == "vehicle"` — vehicle block fires, event block cannot.
- If `selectedBox == null && currentSelectedLabel == "vehicle"` — vehicle block fires (no selected box).
- Same pattern for event, eliminating Ctrl+1~4 conflict when vehicle box selected but event label active.

Added `FindWaypointForBox(b) == waypoint` filter to `ChangeBoxIdWithinWaypoint` LINQ query so ID changes only affect boxes in the same waypoint track, not overlapping tracks with same frame range.

### Task 3: BBox clamping at all interaction paths (FUNC-03)

Applied `CoordinateHelper.ClampToImage(rect, image.Width, image.Height)` at 4 sites:
1. **MouseUp / draw complete** — before `boundingBoxes.Add(drawingBox)`
2. **MouseMove / drag** — after position update from `ViewToImage`
3. **PerformResize** — before `selectedBox.Rectangle = newRect`
4. **WASD keyboard move** — after switch/case position update

## Acceptance Criteria Verification

| Criterion | Result |
|-----------|--------|
| `undoStack.Clear` present | 1 match |
| `redoStack.Clear` present | 2 matches (LoadLabelingData + Redo) |
| `entryFrameIndex = null` >= 2 | 3 matches |
| Delete key uses `boundingBoxes.Remove` | confirmed |
| `InvalidateBoxCache` in Delete handler | confirmed |
| Vehicle shortcut checks `selectedBox.Label == "vehicle"` | confirmed |
| Event shortcut checks `selectedBox.Label == "event"` | confirmed |
| `FindWaypointForBox(b) == waypoint` in ChangeBoxIdWithinWaypoint | confirmed |
| `CoordinateHelper.ClampToImage` >= 4 sites | 4 matches |
| Build errors | 0 errors, 28 warnings (pre-existing) |

## Deviations from Plan

None - plan executed exactly as written.

## Known Stubs

None.

## Self-Check: PASSED

- `Forms/MainForm.cs`: modified (51 insertions, 15 deletions)
- Commit `e9c9b10` exists in git log
