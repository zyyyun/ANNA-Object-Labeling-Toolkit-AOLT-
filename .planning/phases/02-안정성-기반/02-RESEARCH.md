# Phase 2: 안정성 기반 - Research

**Researched:** 2026-04-16
**Domain:** .NET 8 WinForms reliability — exception handling, resource management, race conditions, null safety
**Confidence:** HIGH

## Summary

Phase 2 addresses six requirements (RELI-01 through RELI-04, PERF-02, PERF-03) focused on making the AOLT application stable under stress. The current codebase has no global exception handler, a known timer disposal leak, no cancellation token for async video loading, inconsistent null checks on `selectedBox`, and an undo/redo stack that defines `MAX_UNDO_STACK = 100` but already implements the enforcement (line 1930-1934 of MainForm.cs).

All fixes are in-place modifications to existing files (Program.cs, MainForm.cs, VideoService.cs). No new libraries are needed. Phase 1's Serilog logging infrastructure (LogService) is already in place, so all error paths can log via `Log.Error()` / `Log.Fatal()`.

**Primary recommendation:** Apply the five fixes in dependency order: (1) global exception handler in Program.cs, (2) CancellationToken in VideoService.LoadVideoAsync and MainForm.LoadVideoWithSubtitle, (3) doubleClickTimer disposal fix, (4) null guard audit on selectedBox/selectedWaypoint, (5) verify and correct undo/redo stack enforcement.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| RELI-01 | 전역 예외 처리기로 처리되지 않은 예외에 의한 비정상 종료 방지 | Global exception handler pattern for Program.cs (Application.ThreadException + AppDomain.UnhandledException) |
| RELI-02 | doubleClickTimer 누수 수정 (Dispose 경로 추가) | Timer leak analysis in MainForm.cs lines 1267-1444; OnFormClosing missing timer disposal |
| RELI-03 | 영상 로드 시 CancellationToken 적용으로 레이스 컨디션 제거 | CancellationTokenSource pattern for VideoService.LoadVideoAsync + MainForm.LoadVideoWithSubtitle |
| RELI-04 | Nullable 필드 접근 시 일관된 null 체크 적용 | Null reference audit: selectedBox accessed without guard at 6+ locations |
| PERF-02 | 영상 전환 시 이전 VideoCapture 리소스 정상 해제 | VideoService.Dispose() already exists; need to ensure MainForm properly stops playback and disposes on video switch |
| PERF-03 | Undo/Redo 스택에 MAX_UNDO_STACK 상한 실제 적용 | AddUndoAction() at line 1930 already checks and trims; verify correctness of trim logic |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Tech stack**: C# .NET 8.0 WinForms only -- no new frameworks or architecture changes
- **Certification**: ISO/IEC 25023 8대 품질 특성 충족 (Phase 2 = 신뢰성 + 성능효율성)
- **Defects**: Critical/High 결함 0건 필수
- **Nullable disabled**: `<Nullable>disable</Nullable>` in csproj -- cannot enable nullable reference types (would be a breaking change across entire codebase)
- **No unit test framework**: Out of scope per REQUIREMENTS.md (GS인증에서 테스트 코드 자체 평가 안함)
- **Korean language**: All user-facing messages must be in Korean

## Standard Stack

No new libraries needed. Phase 2 works entirely with existing dependencies.

### Core (already installed)
| Library | Version | Purpose | Status |
|---------|---------|---------|--------|
| Serilog | (installed Phase 1) | Error/Fatal logging in exception handlers | Available via LogService |
| .NET 8.0 BCL | 8.0 | CancellationTokenSource, IDisposable, Application events | Built-in |
| System.Windows.Forms | 8.0 | Application.ThreadException, SetUnhandledExceptionMode | Built-in |

### No New Packages Required

All six requirements can be satisfied using .NET BCL types already available:
- `System.Threading.CancellationTokenSource` / `CancellationToken`
- `System.Windows.Forms.Application.ThreadException`
- `System.AppDomain.CurrentDomain.UnhandledException`
- `System.Windows.Forms.Application.SetUnhandledExceptionMode`

## Architecture Patterns

### Pattern 1: Global Exception Safety Net (RELI-01)

**What:** Wire three global handlers in Program.cs before Application.Run() to catch any exception that escapes all try/catch blocks.

**When to use:** Every WinForms application that must not show the .NET crash dialog.

**Current state:** Program.cs has `LogService.Initialize()` and `LogService.AuditAppStart()` but NO exception handlers.

**Implementation:**
```csharp
// Program.cs - add before ApplicationConfiguration.Initialize()
Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

Application.ThreadException += (s, e) =>
{
    Log.Fatal(e.Exception, "[FATAL] UI 스레드에서 처리되지 않은 예외 발생");
    MessageBox.Show(
        $"예기치 않은 오류가 발생했습니다.\n\n{e.Exception.Message}\n\n로그 파일을 확인해주세요.",
        "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
};

AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    if (e.ExceptionObject is Exception ex)
    {
        Log.Fatal(ex, "[FATAL] 백그라운드 스레드에서 처리되지 않은 예외 발생");
    }
};
```

**Source:** [Microsoft Learn - Application.SetUnhandledExceptionMode](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.application.setunhandledexceptionmode)

**Confidence:** HIGH -- official .NET WinForms pattern, well-documented.

### Pattern 2: CancellationToken for Async Video Loading (RELI-03)

**What:** Add a `CancellationTokenSource` field to MainForm. Before each `LoadVideoWithSubtitle()` call, cancel the previous CTS, create a new one, and pass the token through to `VideoService.LoadVideoAsync()`.

**Current state:** `LoadVideoAsync()` has no cancellation support. If user clicks two videos rapidly, both loads race to update UI state.

**Implementation outline:**
```csharp
// MainForm.cs - add field
private CancellationTokenSource _videoLoadCts;

// In LoadVideoWithSubtitle (or its caller):
_videoLoadCts?.Cancel();
_videoLoadCts?.Dispose();
_videoLoadCts = new CancellationTokenSource();
var token = _videoLoadCts.Token;

await _videoService.LoadVideoAsync(filePath, token);
token.ThrowIfCancellationRequested();
// ... UI updates only if not cancelled ...
```

```csharp
// VideoService.cs - add parameter
public async Task LoadVideoAsync(string filePath, CancellationToken cancellationToken = default)
{
    // After VideoCapture creation:
    cancellationToken.ThrowIfCancellationRequested();
    // After subtitle loading:
    cancellationToken.ThrowIfCancellationRequested();
}
```

**Key detail:** The catch block in `LoadVideoWithSubtitle` must handle `OperationCanceledException` separately -- log it as Info (not Error), do not show MessageBox.

**Source:** [Microsoft Learn - CancellationTokenSource](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource)

**Confidence:** HIGH -- standard .NET async cancellation pattern.

### Pattern 3: Timer Disposal Fix (RELI-02)

**What:** The `doubleClickTimer` (System.Threading.Timer) is created at 4 locations (lines 1267, 1268) and disposed at 4 locations (lines 1267, 1324, 1375, 1443), but it is NOT disposed in `OnFormClosing`. This means if the form closes while a timer callback is pending, the callback can fire on a disposed form.

**Current OnFormClosing (line 2590-2594):**
```csharp
protected override void OnFormClosing(FormClosingEventArgs e)
{
    base.OnFormClosing(e);
    _videoService?.Dispose();
}
```

**Fix:** Add `doubleClickTimer?.Dispose()` and also dispose other IDisposable fields:
```csharp
protected override void OnFormClosing(FormClosingEventArgs e)
{
    base.OnFormClosing(e);
    doubleClickTimer?.Dispose();
    doubleClickTimer = null;
    _videoLoadCts?.Cancel();
    _videoLoadCts?.Dispose();
    _videoService?.Dispose();
    labelFont?.Dispose();
}
```

**Additional concern:** The `System.Threading.Timer` callback uses `this.Invoke()` (line 1272) without checking `IsDisposed` or `IsHandleCreated`. This can throw `ObjectDisposedException` if the form is closing. Guard with:
```csharp
if (!IsDisposed && IsHandleCreated)
{
    this.Invoke((Action)(() => { ... }));
}
```

**Source:** [Microsoft Learn - Timer.Dispose](https://learn.microsoft.com/en-us/dotnet/api/system.threading.timer.dispose?view=net-8.0)

**Confidence:** HIGH

### Pattern 4: Null Guard Audit (RELI-04)

**What:** With `<Nullable>disable</Nullable>`, the compiler does not warn about null dereferences. Manual audit required.

**Known unguarded `selectedBox` access locations:**
- Line 706-728: `GetBoxId(selectedBox)` and `selectedBox.Label` used inside a block that checks `selectedBox != null` at entry but selectedBox could theoretically be set to null by another code path (though unlikely in single-threaded WinForms)
- Line 1247, 1255: accessed inside `isResizing` block -- selectedBox could be null if cleared between mouse events
- Line 1383, 1389: accessed inside `isDragging` block -- same concern
- Line 1626: `selectedBox.Rectangle = newRect` -- no null check (inside resize handler)
- Line 2494: `selectedBox.Label switch` -- already guarded by null check on line 2492

**Other nullable fields to audit:**
- `selectedWaypoint` -- used in waypoint operations
- `pictureBoxVideo.Image` -- checked in some places but not all
- `_videoService` -- checked via `?.` operator in OnFormClosing but direct calls elsewhere assume non-null

**Fix approach:** Add explicit `if (selectedBox == null) return;` guards at the start of each mouse event handler block that accesses selectedBox. Do NOT enable nullable reference types project-wide.

**Confidence:** HIGH -- straightforward defensive coding.

### Pattern 5: Undo/Redo Stack Enforcement (PERF-03)

**What:** `MAX_UNDO_STACK = 100` is defined at line 28. `AddUndoAction()` at line 1927-1936 already implements the check:

```csharp
private void AddUndoAction(UndoAction action)
{
    undoStack.Push(action);
    if (undoStack.Count > MAX_UNDO_STACK)
    {
        var tempList = undoStack.ToList();
        tempList.RemoveAt(tempList.Count - 1);  // removes bottom (oldest)
        undoStack = new Stack<UndoAction>(tempList.AsEnumerable().Reverse());
    }
    redoStack.Clear();
}
```

**Issue:** The CONCERNS.md says "constant defined but undo/redo operations never check stack size." However, the current code DOES check. The code was likely added after the concern was documented, or the concern was inaccurate. Need to verify this trim logic is correct:

1. `undoStack.ToList()` converts stack to list (top of stack = index 0)
2. `RemoveAt(tempList.Count - 1)` removes the LAST element (bottom of stack = oldest item) -- **CORRECT**
3. `new Stack<UndoAction>(tempList.AsEnumerable().Reverse())` rebuilds stack -- the Reverse + Stack constructor reverses twice, resulting in correct order

**Verification needed:** This implementation is functionally correct but inefficient (O(n) allocation on every push when over limit). For 100 items this is negligible. The planner should verify this code path works correctly but may not need changes.

**Confidence:** HIGH -- code already implements the enforcement.

### Pattern 6: VideoCapture Resource Release on Switch (PERF-02)

**What:** When user opens a new video, the previous VideoCapture must be fully released.

**Current state:** `VideoService.LoadVideoAsync()` lines 98-103 already handle this:
```csharp
if (videoCapture != null)
{
    videoCapture.Release();
    videoCapture.Dispose();
    videoCapture = null;
}
```

**Issue:** The MainForm side does NOT stop playback before loading a new video. `LoadVideoWithSubtitle()` proceeds directly to `_videoService.LoadVideoAsync()`. If playback timer (`timerPlayback`) is still running, it will call `LoadFrame()` on the disposed VideoCapture.

**Fix:** Stop playback at the start of `LoadVideoWithSubtitle()`:
```csharp
private async Task LoadVideoWithSubtitle(string filePath)
{
    // Stop any active playback first
    if (isPlaying)
    {
        isPlaying = false;
        btnPlay.Text = "\u25B6";
        timerPlayback.Stop();
    }
    // ... existing code ...
}
```

**Confidence:** HIGH

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Global exception handling | Custom try/catch wrappers around every method | `Application.SetUnhandledExceptionMode` + `Application.ThreadException` + `AppDomain.UnhandledException` | Official WinForms safety net; one location covers all unhandled exceptions |
| Async cancellation | Boolean flags or manual state checks | `CancellationTokenSource` / `CancellationToken` | Thread-safe, composable, handles all edge cases (disposed CTS, etc.) |
| Timer lifecycle | Manual boolean flags for timer state | `IDisposable` pattern with null-conditional dispose `?.Dispose()` | Prevents double-dispose, handles null case |

## Common Pitfalls

### Pitfall 1: CancellationToken + UI Thread Marshaling
**What goes wrong:** After cancellation, awaited code resumes on the UI thread and tries to update controls. If the form is closing, this throws ObjectDisposedException.
**Why it happens:** `await` in WinForms resumes on the captured SynchronizationContext (UI thread).
**How to avoid:** Always check `token.IsCancellationRequested` or catch `OperationCanceledException` before touching UI controls after an await.
**Warning signs:** Sporadic ObjectDisposedException on rapid file switching or app close during load.

### Pitfall 2: System.Threading.Timer Callback on Thread Pool
**What goes wrong:** The `doubleClickTimer` callback (line 1268) fires on a thread pool thread, not the UI thread. It uses `this.Invoke()` to marshal to UI, but if the form is disposed between the timer firing and the Invoke call, it throws.
**Why it happens:** `System.Threading.Timer` callbacks are not on the UI thread.
**How to avoid:** Guard `this.Invoke()` with `if (!IsDisposed && IsHandleCreated)`. Consider replacing with `System.Windows.Forms.Timer` (fires on UI thread) if the 500ms precision is acceptable.
**Warning signs:** Intermittent `InvalidOperationException` or `ObjectDisposedException` on form close.

### Pitfall 3: Undo Stack Rebuild Inefficiency
**What goes wrong:** Creating a new Stack from a reversed list on every AddUndoAction when over limit causes GC pressure.
**Why it happens:** Stack<T> doesn't expose RemoveAt or TrimExcess for bottom elements.
**How to avoid:** This is acceptable for MAX_UNDO_STACK=100 (negligible cost). Do NOT optimize to a circular buffer unless profiling shows it matters. Keep it simple for GS certification.
**Warning signs:** N/A at current scale.

### Pitfall 4: Multiple Null Check Patterns
**What goes wrong:** Mixing `selectedBox?.Property` with `if (selectedBox != null)` creates inconsistency. Some paths check null, others don't, leading to NullReferenceException in unchecked paths.
**Why it happens:** Nullable reference types are disabled; no compiler warnings for null dereference.
**How to avoid:** Use a consistent pattern: add `if (selectedBox == null) return;` early guard at the TOP of each handler method, then access freely within the method body. Do NOT sprinkle `?.` throughout.
**Warning signs:** NullReferenceException in mouse event handlers when no box is selected.

### Pitfall 5: Disposing VideoCapture While Timer Still Running
**What goes wrong:** MainForm's `timerPlayback` fires `timerPlayback_Tick` which calls `LoadFrame()` which calls `_videoService.LoadFrame()`. If a new video is loading and VideoCapture was just disposed, LoadFrame accesses a disposed object.
**Why it happens:** No playback stop before video switch.
**How to avoid:** Always `timerPlayback.Stop()` and set `isPlaying = false` BEFORE calling `LoadVideoAsync`.
**Warning signs:** ObjectDisposedException or AccessViolationException during rapid video switching.

## Code Examples

### Global Exception Handler (RELI-01)
```csharp
// Source: Microsoft Learn - Application.SetUnhandledExceptionMode
// File: Program.cs
[STAThread]
static void Main()
{
    LogService.Initialize();
    LogService.AuditAppStart();

    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
    Application.ThreadException += (s, e) =>
    {
        Log.Fatal(e.Exception, "[FATAL] UI 스레드 처리되지 않은 예외");
        MessageBox.Show(
            $"예기치 않은 오류가 발생했습니다.\n\n{e.Exception.Message}\n\n로그 파일을 확인해주세요.",
            "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
    };
    AppDomain.CurrentDomain.UnhandledException += (s, e) =>
    {
        if (e.ExceptionObject is Exception ex)
            Log.Fatal(ex, "[FATAL] 백그라운드 스레드 처리되지 않은 예외");
    };

    try
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
    finally
    {
        LogService.AuditAppStop();
        LogService.CloseAndFlush();
    }
}
```

### CancellationToken Video Loading (RELI-03)
```csharp
// File: MainForm.cs
private CancellationTokenSource _videoLoadCts;

private async Task LoadVideoWithSubtitle(string filePath)
{
    // 1. Cancel previous load
    _videoLoadCts?.Cancel();
    _videoLoadCts?.Dispose();
    _videoLoadCts = new CancellationTokenSource();
    var token = _videoLoadCts.Token;

    // 2. Stop playback
    if (isPlaying)
    {
        isPlaying = false;
        btnPlay.Text = "\u25B6";
        timerPlayback.Stop();
    }

    try
    {
        await _videoService.LoadVideoAsync(filePath, token);
        token.ThrowIfCancellationRequested();

        // ... UI updates (frame display, labels, etc.) ...
    }
    catch (OperationCanceledException)
    {
        Log.Information("[영상 로드] 이전 로드 작업이 취소됨: {FilePath}", filePath);
        // Do not show MessageBox for cancellation
    }
    catch (Exception ex)
    {
        Log.Error(ex, "[영상 로드 오류] {Message}", ex.Message);
        MessageBox.Show($"비디오 로드 중 오류가 발생했습니다:\n{ex.Message}",
            "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
```

### Timer Dispose Guard (RELI-02)
```csharp
// File: MainForm.cs - doubleClickTimer callback guard
doubleClickTimer = new System.Threading.Timer((state) =>
{
    if (isWaitingForDoubleClick && !isDragging)
    {
        if (!IsDisposed && IsHandleCreated)
        {
            this.Invoke((Action)(() =>
            {
                if (isWaitingForDoubleClick && selectedBox != null)
                {
                    isDragging = true;
                    isWaitingForDoubleClick = false;
                    pictureBoxVideo.Invalidate();
                }
            }));
        }
    }
}, null, 500, Timeout.Infinite);
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `System.Threading.Timer` for UI delays | `System.Windows.Forms.Timer` or `Task.Delay` | .NET 4.5+ | Eliminates thread marshaling complexity |
| Manual boolean cancellation flags | `CancellationTokenSource` / `CancellationToken` | .NET 4.0+ (mature in .NET 8) | Thread-safe, composable, standard pattern |
| `Debug.WriteLine` for error logging | Serilog structured logging (Phase 1) | Already implemented | Errors persisted to file, queryable |

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None -- no automated test framework in project |
| Config file | None |
| Quick run command | Manual UI testing |
| Full suite command | Manual UI testing |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| RELI-01 | App shows error dialog instead of crashing on unhandled exception | manual-only | N/A -- requires triggering unhandled exception in running app | N/A |
| RELI-02 | doubleClickTimer disposed on form close; no handle leak | manual-only | N/A -- verify via Task Manager handle count after repeated open/close | N/A |
| RELI-03 | Rapid video switching does not corrupt display | manual-only | N/A -- open video A, immediately open video B, verify display shows B correctly | N/A |
| RELI-04 | No NullReferenceException in mouse handlers | manual-only | N/A -- click/drag without selected box, verify no crash | N/A |
| PERF-02 | Memory stable after repeated video open/close | manual-only | N/A -- monitor memory in Task Manager across 10 open/close cycles | N/A |
| PERF-03 | Undo stack stays at 100 max entries | manual-only | N/A -- create 105 annotations, verify first 5 are not undoable | N/A |

**Justification for manual-only:** This is a WinForms desktop application with no test framework (Out of Scope per REQUIREMENTS.md). All reliability requirements involve runtime behavior (exception handling, resource lifecycle, UI thread interaction) that require a running application to verify.

### Sampling Rate
- **Per task commit:** Build succeeds (`dotnet build`)
- **Per wave merge:** Manual smoke test: open video, draw boxes, switch videos, close app
- **Phase gate:** Manual verification of all 5 success criteria before `/gsd:verify-work`

### Wave 0 Gaps
None -- no automated test infrastructure needed (manual testing per project constraints).

## Open Questions

1. **doubleClickTimer: Replace with System.Windows.Forms.Timer?**
   - What we know: Current `System.Threading.Timer` requires `Invoke()` to marshal to UI thread, creating disposal risk.
   - What's unclear: Whether switching to `System.Windows.Forms.Timer` (which fires on UI thread) would change the 500ms double-click detection behavior.
   - Recommendation: Keep `System.Threading.Timer` but add `IsDisposed`/`IsHandleCreated` guards. Replacing the timer type is a functional change that risks introducing new bugs. Minimal-change approach is safer for GS certification.

2. **PERF-03 already implemented?**
   - What we know: `AddUndoAction()` at line 1930 already checks `undoStack.Count > MAX_UNDO_STACK` and trims. The CONCERNS.md claim appears outdated.
   - What's unclear: Whether this code was added recently (after CONCERNS.md was written) or was always there.
   - Recommendation: Verify the trim logic is correct (it appears to be). If correct, mark PERF-03 as already partially addressed -- only need to verify, not rewrite.

## Sources

### Primary (HIGH confidence)
- [Microsoft Learn - Application.SetUnhandledExceptionMode](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.application.setunhandledexceptionmode) -- global exception handling pattern
- [Microsoft Learn - CancellationTokenSource](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource) -- async cancellation pattern
- [Microsoft Learn - Timer.Dispose](https://learn.microsoft.com/en-us/dotnet/api/system.threading.timer.dispose?view=net-8.0) -- timer disposal requirements
- Source code audit: `MainForm.cs` (2598 lines), `VideoService.cs` (592 lines), `Program.cs` (25 lines)

### Secondary (MEDIUM confidence)
- `.planning/codebase/CONCERNS.md` -- known bugs and tech debt catalog
- `.planning/research/ARCHITECTURE.md` -- prior research on global exception handler pattern
- `.planning/research/PITFALLS.md` -- prior research on GS certification reliability pitfalls

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- no new libraries needed, all .NET BCL
- Architecture: HIGH -- all patterns are standard .NET/WinForms, verified against Microsoft docs
- Pitfalls: HIGH -- identified from actual code audit + official documentation

**Research date:** 2026-04-16
**Valid until:** 2026-05-16 (stable -- .NET 8 LTS, no API changes expected)
