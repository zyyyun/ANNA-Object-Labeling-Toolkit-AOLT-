# Architecture Patterns for GS Certification Improvement

**Project:** AOLT (ANNA Object Labeling Tool)
**Domain:** WinForms desktop labeling application, quality improvement for GS certification
**Researched:** 2026-04-14
**Confidence:** HIGH (based on direct codebase inspection + official .NET docs)

---

## Current State Summary

MainForm.cs is 2,596 lines. It is the dominant class. The code already has a meaningful
layered structure (Services/, Models/, Helpers/) that can be leveraged without touching MainForm.

Current cross-cutting deficiencies mapped to GS certification dimensions:

| Deficiency | GS Dimension | Location |
|---|---|---|
| Debug.WriteLine only, no file log | 유지보수성 | All files |
| Generic `catch (Exception ex)` x 7 | 유지보수성, 신뢰성 | MainForm.cs, Services |
| No audit trail of user actions | 보안성 | MainForm.cs |
| No global unhandled exception handler | 신뢰성 | Program.cs |
| MAC hash stored/verified without salt | 보안성 | (license logic) |
| doubleClickTimer leaks if form closes | 신뢰성 | MainForm.cs line ~69 |
| JSON timestamps use DateTime.Now not frame time | 호환성 | JsonService.cs |

---

## Recommended Architecture for Improvement

The goal is **injection without restructuring**. All improvements follow one of three safe
patterns:

```
Pattern A — New static service, called from existing code
Pattern B — Wrap existing method with try/catch/log
Pattern C — Hook global event in Program.cs
```

Do NOT introduce constructor injection, DI containers, or partial-class splits. The existing
#region discipline inside MainForm.cs is sufficient to locate and scope each change.

---

## Component Additions (No Restructuring Required)

### 1. AppLogger — Static File Logger

**New file:** `Logging/AppLogger.cs`

Single responsibility: write structured log lines to a rotating file in the app's data
directory. Called anywhere via `AppLogger.Info(...)`, `AppLogger.Error(...)`.

```
Logging/
  AppLogger.cs      <- new
```

Design rules:
- Static class with thread-safe lock on write.
- Log file path: `%APPDATA%\AOLT\logs\aolt_YYYYMMDD.log` (rotates daily).
- Format per line: `[ISO8601 timestamp] [LEVEL] [source] message`
- Maximum 10 MB per file; rotate to `.1`, `.2` suffix when exceeded.
- Five levels: DEBUG, INFO, WARN, ERROR, FATAL.
- On write failure: silently ignore (never crash the app due to logging).

This class replaces all `Debug.WriteLine(...)` calls. Existing call sites require only a
one-line substitution. No parameter changes in any existing method signature.

**Where to inject logging (all are one-line replacements):**

| File | Line (approx) | Current | Replace with |
|---|---|---|---|
| MainForm.cs | 328 | `Debug.WriteLine(...)` | `AppLogger.Error("frame-load", ex)` |
| MainForm.cs | 434 | `Debug.WriteLine(...)` | `AppLogger.Error("json-load", ex)` |
| MainForm.cs | 504 | `Debug.WriteLine(...)` | `AppLogger.Error("json-save", ex)` |
| MainForm.cs | 607 | `Debug.WriteLine(...)` | `AppLogger.Error("playback", ex)` |
| MainForm.cs | 673 | `AppLogger.Error("exit-marker", ex)` | same pattern |
| JsonService.cs | 207 | `Debug.WriteLine(...)` | `AppLogger.Warn("json-backup", backupEx)` |
| VideoService.cs | 208 | `Debug.WriteLine(...)` | `AppLogger.Error("frame-load", ex)` |
| VideoService.cs | 472 | `Debug.WriteLine(...)` | `AppLogger.Warn("subtitle-load", ex)` |

**Confidence:** HIGH. Static singleton logger pattern is well-established for WinForms.
Thread-safe file write with lock is standard .NET. No DI required.

---

### 2. AuditLogger — User Action Audit Trail

**New file:** `Logging/AuditLogger.cs`

Separate concern from AppLogger. Records WHO did WHAT and WHEN. Required by GS
certification 보안성 > 책임성 (accountability) subcharacteristic.

```
Logging/
  AppLogger.cs      <- error/system log
  AuditLogger.cs    <- user action log  (new)
```

Design rules:
- Static class backed by a separate file: `%APPDATA%\AOLT\audit\audit_YYYYMMDD.log`
- Format: `[ISO8601] [ACTION] [detail]`
- Must NOT fail silently if disk is full — audit failure is itself a loggable event in AppLogger.
- Immutable append-only; never delete or truncate audit log from within the application.

Action categories to record (each is a one-liner call added at the action site):

| User Action | Method to Instrument | Log Entry |
|---|---|---|
| 영상 파일 로드 | `LoadVideoWithSubtitle()` after success | `VIDEO_LOADED path=...` |
| JSON 내보내기 | `SaveCurrentLabelingData()` after success | `JSON_EXPORTED path=... boxes=N` |
| JSON 로드 | `LoadLabelingData()` after success | `JSON_LOADED path=...` |
| JSON 삭제 | `btnDeleteJson_Click` after confirm | `JSON_DELETED path=...` |
| 바운딩 박스 생성 | end of draw operation | `BOX_CREATED label=... frame=N` |
| 바운딩 박스 삭제 | delete action site | `BOX_DELETED label=... frame=N` |
| Undo/Redo | undo/redo execution | `UNDO` / `REDO` |
| 애플리케이션 시작 | `MainForm_Load` | `APP_STARTED version=...` |
| 애플리케이션 종료 | `MainForm.FormClosing` | `APP_CLOSED` |

This requires adding approximately 10-12 one-liner calls into existing methods. No signature
changes. No restructuring.

**Confidence:** HIGH. Append-only text audit log is the simplest compliant pattern for
desktop applications without a database.

---

### 3. Global Exception Handler — Program.cs

**Modified file:** `Program.cs`

Currently Program.cs is 13 lines. Add three global handlers before `Application.Run()`.

Pattern C applies: hook `Application.ThreadException`, `AppDomain.CurrentDomain.UnhandledException`,
and `Application.SetUnhandledExceptionMode`.

```csharp
// Add to Program.Main(), before Application.Run(new MainForm())
Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
Application.ThreadException += (s, e) => {
    AppLogger.Fatal("unhandled-ui-thread", e.Exception);
    MessageBox.Show("예기치 않은 오류가 발생했습니다. 로그를 확인해주세요.", "오류",
        MessageBoxButtons.OK, MessageBoxIcon.Error);
};
AppDomain.CurrentDomain.UnhandledException += (s, e) => {
    if (e.ExceptionObject is Exception ex)
        AppLogger.Fatal("unhandled-bg-thread", ex);
};
```

This catches any exception that escapes all existing try/catch blocks. It is a safety net,
not a replacement for per-site handling. Total change: 8 lines added to Program.cs.

**Confidence:** HIGH. Official .NET WinForms pattern. Documented at
https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.application.setunhandledexceptionmode

---

### 4. Security Hardening — License Verification

**Modified file:** wherever MAC address verification currently lives (confirm via codebase search)

Current state (inferred from PROJECT.md): MAC address used as license key without salt or
SHA-256. KISA 암호화 가이드 requires SHA-256 or stronger for one-way hashing.

Minimum compliant pattern:
```csharp
// KISA-compliant: SHA-256 + application-specific fixed salt
private static string HashMacAddress(string mac)
{
    const string APP_SALT = "AOLT_GS_2025_IFEZ";  // fixed per deployment
    using var sha256 = SHA256.Create();
    byte[] bytes = Encoding.UTF8.GetBytes(mac.ToUpperInvariant() + APP_SALT);
    byte[] hash = sha256.ComputeHash(bytes);
    return Convert.ToHexString(hash);  // .NET 5+ built-in
}
```

This is a single method replacement. The stored license value must be the SHA-256 output,
not the raw MAC. On validation: hash the current MAC with the same salt and compare.

AuditLogger should record `LICENSE_VALIDATED result=ok/fail` on each startup.

**Note:** This is a drop-in replacement. If the license file stores the raw MAC today, the
license file format must change. Generate new license files after the fix.

**Confidence:** MEDIUM. KISA 암호화 알고리즘 가이드라인 (2022) specifies SHA-256 or stronger
for irreversible hashing. Verified via project constraints in PROJECT.md; actual KISA doc
not directly fetched.

---

### 5. Exception Handling Improvement — Per-Site Specificity

**Modified files:** MainForm.cs (7 sites), Services/*.cs (4 sites)

Do NOT rewrite all at once. Apply incrementally, site by site, in order of risk.

The safe pattern for each existing generic catch:

**Before (all 7 MainForm catch blocks look similar):**
```csharp
catch (Exception ex)
{
    Debug.WriteLine($"[오류] {ex.Message}");
    MessageBox.Show(...);
}
```

**After (example for frame load):**
```csharp
catch (InvalidOperationException ex)
{
    AppLogger.Error("frame-load", ex);
    MessageBox.Show("프레임 로드 실패: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
catch (OutOfMemoryException ex)
{
    AppLogger.Fatal("frame-load-oom", ex);
    MessageBox.Show("메모리 부족으로 프레임을 로드할 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
catch (Exception ex)
{
    AppLogger.Error("frame-load-unexpected", ex);
    MessageBox.Show("예기치 않은 오류: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
```

Key rule: always keep a trailing generic catch as the last resort. Never remove it entirely.
The goal is to add specific cases before it, not to eliminate it.

Priority order for which sites to fix first:

1. `LoadVideoWithSubtitle()` — critical path, video load failure
2. `SaveCurrentLabelingData()` / `btnExportJson_Click` — data loss risk
3. `LoadLabelingData()` — JSON load, OutOfMemoryException already partially handled in JsonService
4. `btnExit_Click` / `SetExitMarkerAndCreateWaypoint()` — waypoint creation failure
5. `btnPlay_Click` — playback, lower risk
6. `timerPlayback_Tick` — background timer, requires thread-safety awareness

**Confidence:** HIGH. Standard .NET exception handling patterns. Microsoft CA1031 code analysis
rule documents this exact recommendation.

---

### 6. Timer Leak Fix — doubleClickTimer Disposal

**Modified file:** MainForm.cs

The `doubleClickTimer` field (System.Threading.Timer, line ~69) must be disposed when the
form closes. Add to `MainForm.FormClosing` or `Dispose(bool)`.

This is a 3-line fix:
```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        doubleClickTimer?.Dispose();
        labelFont?.Dispose();
        // ...existing disposals
    }
    base.Dispose(disposing);
}
```

Alternatively, if FormClosing is already handled, add `doubleClickTimer?.Dispose()` there.

**Confidence:** HIGH. Timer leak is directly observable in the codebase (line 69 field
declaration, no corresponding Dispose call found via codebase inspection).

---

## Improvement Dependency Order

This is the required implementation sequence. Each phase depends on the previous.

```
Phase 1: AppLogger (foundation)
    - All subsequent improvements depend on this
    - Required before any exception handling changes (logging must exist first)

Phase 2: Global Exception Handler in Program.cs
    - Requires AppLogger to be functional
    - Safety net; catch anything Phase 3+ misses during development

Phase 3: Replace Debug.WriteLine calls with AppLogger calls
    - No logic changes, pure substitution
    - Can be done in one sweep across all files

Phase 4: Timer Leak + Dispose fixes
    - Isolated, no dependencies
    - Must be done before audit logging (clean shutdown needed)

Phase 5: AuditLogger (user action trail)
    - Requires AppLogger to already exist (shares infrastructure pattern)
    - Add action-site calls incrementally

Phase 6: Exception Handling Per-Site Specificity
    - Requires AppLogger (logging in catch blocks)
    - Do in priority order from Section 5 above

Phase 7: Security — SHA-256 License Hash
    - Requires AuditLogger (license audit events)
    - Do last; requires regenerating license files
```

Dependency graph:

```
AppLogger
    |
    +-- Global handler (Program.cs)
    |
    +-- Debug.WriteLine replacement
    |
    +-- AuditLogger
    |       |
    |       +-- Security / license hash
    |
    +-- Exception specificity
    |
Timer fix (independent)
```

---

## Where NOT to Inject

Avoid injecting calls into these areas unless explicitly fixing a known defect:

| Area | Reason to Avoid |
|---|---|
| `pictureBoxVideo_Paint` | High-frequency repaint; logging here causes performance regression |
| `timerPlayback_Tick` | 33ms interval; any blocking call causes frame drops |
| `panelTimeline_Paint` | Same as above |
| `CoordinateHelper.*` | Pure math, no side effects, no error conditions |
| `WndProc` override | Win32 message pump; exception here crashes the app silently |

---

## File Structure After Changes

```
AOLT/
  Logging/
    AppLogger.cs          <- NEW: structured file logger, static
    AuditLogger.cs        <- NEW: user action audit trail, static
  Forms/
    MainForm.cs           <- MODIFIED: ~20 call sites (no signature changes)
  Services/
    VideoService.cs       <- MODIFIED: 2 call sites
    JsonService.cs        <- MODIFIED: 1 call site
  Program.cs              <- MODIFIED: +8 lines global handler
  (no other structural changes)
```

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: Introducing ILogger / DI Container
**What:** Refactoring to constructor-injected ILogger throughout MainForm
**Why bad:** Requires changing constructor signature and all instantiation sites; risks
breaking designer-generated code in MainForm.Designer.cs
**Instead:** Static AppLogger class. Not as testable, but zero disruption to existing code.

### Anti-Pattern 2: Splitting MainForm into Partial Classes
**What:** Moving regions into partial class files to reduce perceived size
**Why bad:** Partial classes share the same class scope; it does not improve testability or
maintainability metrics, it only moves code around. Introduces merge risk.
**Instead:** Leave MainForm.cs as-is. GS certification does not penalize file length directly.

### Anti-Pattern 3: Adding Logging Inside Every Helper Method
**What:** Adding AppLogger calls to CoordinateHelper, GetBoxId, etc.
**Why bad:** These methods are called hundreds of times per second during paint events.
File I/O in hot paths causes visible UI stuttering.
**Instead:** Log only at operation boundaries (video load, save, user actions).

### Anti-Pattern 4: Using try/catch inside timerPlayback_Tick
**What:** Wrapping the playback timer tick handler in a broad try/catch that swallows errors
**Why bad:** If the frame load fails silently each tick, the app appears frozen and no
diagnostic is possible.
**Instead:** Let VideoService throw specific exceptions; let the timer's catch log + stop
playback cleanly.

### Anti-Pattern 5: Audit Log to Same File as Error Log
**What:** Using AppLogger for both system errors and user audit trail
**Why bad:** GS 보안성 > 책임성 requires audit records to be tamper-evident and separate
from operational logs.
**Instead:** Separate files. AppLogger for system/error. AuditLogger for user actions.

---

## Scalability Considerations

This is a single-user desktop application. Scalability concerns do not apply to user load.

| Concern | Approach |
|---|---|
| Large video files (>4 GB) | Already handled via OpenCvSharp VideoCapture; no change needed |
| Many bounding boxes (>10,000) | O(n) scan on frame change is a known issue; index by FrameIndex in Phase 1 of perf work |
| Log file growth | Daily rotation + 10 MB cap in AppLogger prevents unbounded growth |
| Audit log on restricted filesystem | Fallback: write to executable directory if APPDATA unavailable |

---

## Sources

- Microsoft .NET official logging docs: https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/overview
- Application.SetUnhandledExceptionMode: https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.application.setunhandledexceptionmode
- CA1031 do not catch general exception: https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1031
- .NET exception best practices: https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions
- WinForms exception handling patterns: https://codejack.com/2024/09/winforms-exception-handling-best-practices/
- NLog WinForms file logging: https://thecodebuzz.com/file-logging-windows-form-application-nlog-net-core/
- SHA-256 in C#: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.sha256
- Singleton logger pattern: https://dotnettutorials.net/lesson/singleton-design-pattern-real-time-example/
- Direct codebase inspection: MainForm.cs, VideoService.cs, JsonService.cs, Program.cs (2026-04-14)
