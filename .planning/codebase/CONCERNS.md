# Codebase Concerns

**Analysis Date:** 2026-04-14

## Tech Debt

**MainForm Size and Complexity:**
- Issue: `MainForm.cs` is 2,596 lines, handling UI rendering, annotation logic, state management, video playback, JSON I/O, and event handling all in one class
- Files: `Forms/MainForm.cs`
- Impact: Difficult to maintain, test, and extend. High risk of introducing bugs when modifying. UI thread blocking potential when handling large datasets.
- Fix approach: Refactor into separate concerns: ViewModel (state/logic), PresentationService (rendering), InputHandler (mouse/keyboard), and keep MainForm as thin view layer.

**Bare Exception Catching:**
- Issue: Multiple locations catch generic `Exception` without specific handling. Examples: `catch (Exception ex)` in form load, playback, and JSON operations.
- Files: `Forms/MainForm.cs` (lines 277, 326, 432, 496, 502, 605, 671), `Services/VideoService.cs` (lines 206, 470), `Services/JsonService.cs` (line 387)
- Impact: Masks actual error types, makes debugging harder. Silent failures in some paths. Cannot distinguish between recoverable and fatal errors.
- Fix approach: Implement specific exception types for each service (VideoLoadException, JsonSerializationException, etc.). Use selective catching with proper error recovery strategies.

**Missing Event Unsubscription:**
- Issue: Multiple event handlers registered in MainForm (lines 165-167, 525-533, 1004-1005, 1449+) but never explicitly unsubscribed, particularly lambda event handlers with captured variables
- Files: `Forms/MainForm.cs`
- Impact: Potential memory leaks if form is recreated. Event handlers holding references to old state. Increased memory footprint over application lifetime.
- Fix approach: Implement proper cleanup in `MainForm_Closing` or use weak event patterns. Track all event subscriptions and unsubscribe them.

**Manual Timer Management:**
- Issue: `doubleClickTimer` is manually created and disposed multiple times (lines 1265, 1322, 1373, 1441). Timer can be created multiple times without ensuring previous instance is disposed.
- Files: `Forms/MainForm.cs`
- Impact: Resource leak if disposal fails or exception occurs between timer creation and disposal. Handles accumulate if exceptions occur during double-click logic.
- Fix approach: Use a single timer instance or implement try-finally pattern. Consider using Task.Delay instead of System.Threading.Timer for UI-safe async operations.

**No Input Validation on Bounding Box Operations:**
- Issue: BoundingBox rectangles created from mouse coordinates without validation. MIN_BBOX_SIZE check exists but no validation for out-of-bounds coordinates.
- Files: `Forms/MainForm.cs` (lines 1354-1358, 2411-2486)
- Impact: Boxes can be created outside video frame bounds. Rendering artifacts. Annotation data integrity issues when exported to JSON.
- Fix approach: Implement coordinate validation in BoundingBox creation and a clamping mechanism in CoordinateHelper.

**Hardcoded Magic Numbers:**
- Issue: Many magic numbers scattered throughout: font sizes (8F, 9F), padding (5, 0, 0, 0), colors (FromArgb values), timing values (500ms, 3000ms)
- Files: `Forms/MainForm.cs` (lines 2442-2476), `Services/VideoService.cs` (line 519)
- Impact: Difficult to maintain visual consistency. Hard to adjust timing without understanding context. Increases bug risk when tweaking UI/timing.
- Fix approach: Centralize constants in a Theme/Config class. Create constants for timing intervals (DOUBLE_CLICK_DELAY_MS = 500).

## Known Bugs

**Race Condition in Async Video Loading:**
- Symptoms: If user rapidly selects different video files, frame display may show wrong video or corrupt display
- Files: `Forms/MainForm.cs` (lines 237-269), `Services/VideoService.cs` (lines 94-146)
- Trigger: Click video file 1, immediately click video file 2 before first load completes
- Workaround: Wait for load to complete before selecting another video
- Root cause: No cancellation token for LoadVideoAsync. Multiple concurrent video loads race to update UI state.
- Fix approach: Implement CancellationToken in LoadVideoAsync. Cancel previous load before starting new one.

**FFmpeg Process Not Disposed:**
- Symptoms: ffmpeg.exe processes may remain in task manager after subtitle extraction
- Files: `Services/VideoService.cs` (lines 505-527)
- Trigger: Call SetupFFmpegPath during app startup; process may not be properly cleaned up
- Root cause: Process.Start() is called but process instance is never stored or disposed
- Fix approach: Store process reference and dispose properly using `using` statement or try-finally.

**Subtitle Time Parsing Silent Failure:**
- Symptoms: Subtitles don't display even though SRT file exists
- Files: `Services/VideoService.cs` (lines 470-473)
- Trigger: Malformed SRT time format (e.g., missing milliseconds component)
- Root cause: ParseSrtTime returns TimeSpan.Zero on parse failure; catch block silently logs to Debug output only
- Fix approach: Add validation for SRT format. Implement fallback or user-facing error notification.

**Undo/Redo Stack Size Not Enforced:**
- Symptoms: Memory usage grows unbounded when making many annotations
- Files: `Forms/MainForm.cs` (line 28: `MAX_UNDO_STACK = 100` constant defined but never used)
- Trigger: Create >100 annotation operations; stack will grow beyond configured limit
- Root cause: Constant defined but undo/redo operations never check stack size
- Fix approach: Implement stack size check in undo/redo addition operations. Pop oldest entries when limit reached.

## Security Considerations

**No Input Sanitization in JSON Export:**
- Risk: User-provided label names and waypoint data written directly to JSON without validation
- Files: `Services/JsonService.cs` (lines 468-560), `Forms/MainForm.cs` (line 49: `currentSelectedLabel`)
- Current mitigation: None explicit
- Recommendations: Validate label names against whitelist. Sanitize any free-text fields before export. Add schema validation on load.

**File Path Traversal Potential:**
- Risk: Video file path is used directly to construct JSON save path without validation
- Files: `Services/JsonService.cs` (lines 114-131, 174-182)
- Current mitigation: Path.GetDirectoryName and Directory.Exists checks provide some protection
- Recommendations: Use Path APIs consistently. Validate that constructed paths remain within expected directories. Add canonical path checks.

**Unencrypted Backup Files:**
- Risk: JSON.backup files created without encryption (line 198-203)
- Files: `Services/JsonService.cs`
- Impact: Sensitive annotation data stored in plaintext backup
- Recommendations: Either delete backups after successful load, or implement encryption for backup files.

## Performance Bottlenecks

**Per-Frame Rendering Complexity:**
- Problem: PictureBox.Paint event renders all bounding boxes, waypoints, and labels for every frame with no culling
- Files: `Forms/MainForm.cs` (paint logic scattered, lines 2400+)
- Cause: No spatial indexing or culling of off-screen objects
- Impact: Slow with >100 boxes per frame. 4K video rendering becomes sluggish.
- Improvement path: Implement object culling based on viewport. Cache rendered overlays between frames. Consider using custom Graphics rendering instead of UI controls.

**JSON File Load Entire in Memory:**
- Problem: Large JSON files (hundreds of annotations) loaded entirely into string before parsing
- Files: `Services/JsonService.cs` (line 220: `string json = streamReader.ReadToEnd()`)
- Cause: No streaming JSON parser implementation
- Impact: Memory spikes with files >50MB. OOM exceptions caught but not gracefully handled.
- Improvement path: Implement streaming JSON deserialization using JsonReader. Process annotations incrementally.

**Bounding Box List Recomputation on Every Paint:**
- Problem: `cachedCurrentFrameBoxes` exists but cache validation logic (line 2403-2404) is non-optimal
- Files: `Forms/MainForm.cs` (lines 79-80, 2403-2408)
- Impact: Unnecessary LINQ filtering on every frame paint
- Improvement path: Implement frame-indexed dictionary of boxes to eliminate filtering overhead.

**No Playback Timing Adjustment:**
- Problem: Video playback timing relies on wall-clock time without frame-skipping or sync mechanism
- Files: `Services/VideoService.cs` (lines 26-28, 180-210)
- Impact: Playback can drift from real-time, especially on slower machines or when UI is busy
- Improvement path: Implement frame-based timing with catchup logic. Measure actual frame load time and adjust presentation accordingly.

## Fragile Areas

**Waypoint and BoundingBox Synchronization:**
- Files: `Forms/MainForm.cs` (lines 317-351), `Models/WaypointMarker.cs`
- Why fragile: Complex bidirectional relationship between WaypointMarker and BoundingBox collections. Deleting a box requires updating waypoints. No invariant enforcement.
- Safe modification: Add transaction-like pattern. Define clear ownership rules (waypoint owns boxes vs. boxes own waypoint). Add validation in ChangeBoxIdWithinWaypoint method.
- Test coverage: No unit tests for relationship consistency. High risk of orphaned waypoints or dangling references.

**Coordinate Transformation (Image vs View):**
- Files: `Helpers/CoordinateHelper.cs`, `Forms/MainForm.cs` (coordinate conversion scattered throughout)
- Why fragile: Complex coordinate system handling between video image space and PictureBox view space. PictureBox zoom and pan state affects calculations.
- Safe modification: Create dedicated CoordinateContext class. Centralize all transforms. Add unit tests with known image/view sizes.
- Test coverage: Manual testing only. Edge cases around zoom=1.0 and pan=(0,0) likely untested.

**Label Category Mapping:**
- Files: `Services/JsonService.cs` (lines 15-30, 54-104)
- Why fragile: Hardcoded category ID ranges (person 1-20, vehicle 21-24, event 25-34) and string-based category lookup
- Safe modification: Extract category definitions to config file. Validate category IDs on load. Add reverse mapping cache.
- Test coverage: No validation that incoming JSON categories match expected ranges.

**Form State During Async Operations:**
- Files: `Forms/MainForm.cs` (lines 237-269, 390-436, 442-506)
- Why fragile: User can close form during async video load or JSON export. No cancellation tokens. Form fields accessed from async callbacks.
- Safe modification: Implement CancellationToken throughout async operations. Validate form state before UI updates in callbacks.
- Test coverage: No tests for concurrent operations or form closure scenarios.

## Scaling Limits

**Single-threaded UI for Video Playback:**
- Current capacity: Smooth playback up to ~30fps at 1080p on modern hardware
- Limit: 4K video or high-speed playback (60fps+) causes frame drops and UI lag
- Scaling path: Move video frame loading to background thread. Implement frame buffering. Use hardware acceleration (DirectX/GPU rendering).

**In-Memory Annotation Storage:**
- Current capacity: ~10,000 bounding boxes across all frames before noticeable slowdown
- Limit: Very large datasets (100,000+ boxes) cause memory pressure and slow JSON export
- Scaling path: Implement lazy-loading for frames. Use memory-mapped files for large datasets. Consider database backend for annotation storage.

**JSON Export Time:**
- Current capacity: <500MB video with annotations exports in <5 seconds on modern hardware
- Limit: Larger videos or complex annotation hierarchies cause long export times with blocked UI
- Scaling path: Run export on background thread with progress reporting. Implement incremental/delta export.

## Dependencies at Risk

**OpenCvSharp4 Version Pinning:**
- Risk: Version 4.11.0 is very recent (2025-05-07). Potential stability issues. DLL version mismatches with runtime.
- Files: `ASLTv1.0.csproj` (lines 18-20)
- Impact: Breaking changes in future versions. DLL load failures on systems without Visual C++ redistributable.
- Migration plan: Pin to stable LTS version or implement version compatibility checks. Add runtime DLL version verification.

**FFMpegCore Partial Integration:**
- Risk: FFmpeg setup is optional but some features depend on it. Graceful degradation is incomplete.
- Files: `Services/VideoService.cs` (lines 499-549), `Forms/MainForm.cs` (line 169)
- Impact: Subtitle extraction and some video processing silently fail if FFmpeg unavailable
- Migration plan: Implement explicit FFmpeg availability checks before operations. Provide clear user feedback. Consider bundled FFmpeg in installer.

**Newtonsoft.Json Dependency:**
- Risk: JSON.NET version 13.0.3 is stable but adding System.Text.Json support would reduce external dependency
- Files: `Services/JsonService.cs` (line 1)
- Current mitigation: Widely used and stable library
- Recommendations: Monitor for security updates. Consider migration path to System.Text.Json in .NET 8+.

## Missing Critical Features

**No Undo/Redo UI Feedback:**
- Problem: Undo/Redo stacks exist but no visual indication of stack depth or undo/redo availability
- Blocks: Users can't tell if undo will have any effect. Stack behavior is invisible.
- Implementation: Add undo/redo buttons with enable/disable state. Show operation count in status bar.

**No Auto-Save:**
- Problem: Annotations lost if application crashes before manual save
- Blocks: Data safety concerns in production use. Users must remember to save frequently.
- Implementation: Implement periodic auto-save to temporary file. Add recovery on startup if crash detected.

**No Annotation Validation Before Export:**
- Problem: Invalid or incomplete annotations exported without warning
- Blocks: Downstream systems may reject malformed JSON
- Implementation: Add pre-export validation. Warn if boxes are too small, outside bounds, or inconsistent.

## Test Coverage Gaps

**No Unit Tests for Core Services:**
- What's not tested: JsonService.LoadLabelingDataAsync behavior with various JSON formats. VideoService frame loading with corrupted files. Coordinate transformations with extreme zoom levels.
- Files: `Services/JsonService.cs`, `Services/VideoService.cs`, `Helpers/CoordinateHelper.cs`
- Risk: JSON parsing bugs go undetected. Coordinate transformation errors only found through manual testing. Video load failures not caught until user encounters them.
- Priority: **High** - These services are critical to application functionality.

**No Integration Tests for Annotation Workflow:**
- What's not tested: Full workflow: load video → draw box → save JSON → reload → verify data integrity
- Files: All files involved in workflow
- Risk: Breaking changes in refactoring not caught. Serialization/deserialization round-trip bugs not detected.
- Priority: **High** - Would catch most regressions.

**No UI Tests for Input Handling:**
- What's not tested: Mouse events, keyboard shortcuts, edge cases (clicking on overlapping boxes, dragging outside bounds, rapid operations)
- Files: `Forms/MainForm.cs` (input handling scattered throughout)
- Risk: UI logic bugs persist. Regression when refactoring input handlers. Race conditions in async operations not caught.
- Priority: **Medium** - Manual testing currently handles most cases, but automation would improve reliability.

**No Performance/Stress Tests:**
- What's not tested: Loading videos with 100+ frames. Drawing 1000+ boxes. Rapid save/load cycles.
- Risk: Performance bottlenecks not identified until user encounters them in production.
- Priority: **Medium** - Would identify scaling limits before they impact users.

## Other Concerns

**Nullable Reference Type Disabled:**
- Issue: `<Nullable>disable</Nullable>` in project file disables null-safety analysis
- Files: `ASLTv1.0.csproj` (line 7)
- Impact: Null reference exceptions possible at runtime. IDE provides no warnings for potential null dereferences.
- Fix approach: Enable nullable reference types. Add null checks and use null-coalescing operators appropriately.

**Hardcoded Language (Korean):**
- Issue: All user-facing strings, error messages, and UI labels are hardcoded in Korean
- Files: Throughout (MainForm, Services, Forms)
- Impact: Application not usable for non-Korean speakers. Maintenance requires Korean language expertise. Localization difficult if needed later.
- Fix approach: Extract strings to resource files. Implement ILocalizationService. Design for multi-language support from start.

**No Application Logging Framework:**
- Issue: Diagnostics use Debug.WriteLine and MessageBox scattered throughout
- Files: Multiple files
- Impact: Production diagnostics difficult. No structured logging. No log rotation or archival. Debug output only visible in debug builds.
- Fix approach: Implement structured logging (Serilog or similar). Add file-based logging with rotation. Implement log levels and filtering.

---

*Concerns audit: 2026-04-14*
