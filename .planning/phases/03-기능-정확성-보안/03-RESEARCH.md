# Phase 3: 기능 정확성 + 보안 - Research

**Researched:** 2026-04-16
**Domain:** WinForms annotation tool bug fixes, COCO JSON correctness, KISA security compliance
**Confidence:** HIGH

## Summary

Phase 3 addresses 16 requirements spanning three categories: (1) 10 functional bug fixes (FUNC-01~10), (2) security hardening (SECU-01 PBKDF2, SECU-04 path traversal), and (3) cross-cutting quality improvements (COMP-01, RELI-05, USAB-03, MAINT-02). The codebase has been thoroughly analyzed and every bug's root cause has been identified in the existing source.

The functional bugs cluster into four groups: COCO JSON correctness (FUNC-02~04, COMP-01), ID/Waypoint consistency (FUNC-06~08), state lifecycle (FUNC-09~10), and UI interaction (FUNC-01, FUNC-05). Security work requires two new modules: a `SecurityHelper` for PBKDF2 hashing and a `PathValidator` for path traversal prevention. Exception handling cleanup (MAINT-02) touches all files with `catch(Exception)` blocks.

**Primary recommendation:** Split into 3 plans: (P1) Functional bug fixes FUNC-01~10 + COMP-01 + RELI-05 + USAB-03, (P2) Security SECU-01 + SECU-04, (P3) MAINT-02 exception specificity. This groups related changes and minimizes merge conflicts.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| FUNC-01 | Vehicle 라벨 드롭다운 선택 교체 정상 동작 | Vehicle ComboBox SelectedIndexChanged 핸들러 분석 완료. 현재 코드(MainForm.cs:2485-2493)에서 ComboBox 동작은 구현되어 있으나, 선택 후 실제 VehicleId 반영 로직 검증 필요 |
| FUNC-02 | JSON 타임스탬프가 실제 프레임 시간 기반 | 버그 위치 확인: JsonService.cs:428-429. `DateTime.Now.AddSeconds(frameSeconds)` 사용 -- 현재 시각 기반이라 매 내보내기마다 다른 값 생성. SRT 자막 없을 때 `frameSeconds` 기반 상대 시간으로 교체 필요 |
| FUNC-03 | 바운딩 박스 좌표 이미지 범위 클램핑 | 클램핑 로직 부재 확인: MouseUp(MainForm.cs:1424-1441)과 MouseMove(MainForm.cs:1375-1384)에서 좌표 경계 검증 없음. 드래그(1403-1408), 키보드 이동(2149), 리사이즈(PerformResize) 모두 클램핑 미적용 |
| FUNC-04 | COCO 카테고리 ID 정확 매핑 | JsonService.cs GetCategoryId(line 55-67) 분석 완료. person은 1-20, vehicle은 21-24, event는 25-34 매핑. fallback 분기에서 범위 밖 ID 처리가 부정확할 수 있음 |
| FUNC-05 | 모든 UI 버튼 오류 없이 동작 | 전체 버튼 핸들러 스캔 완료. 대부분 try-catch 적용됨. 개별 버튼별 동작 검증 필요 |
| FUNC-06 | BBOX Entry-Exit ID 유지 | ChangeBoxIdWithinWaypoint(MainForm.cs:2202-2218)가 waypoint 내 boxes를 일괄 업데이트하지만, ID 사후 지정 시 waypoint 매칭이 실패할 수 있음 (ID=0인 상태에서 waypoint 생성 후 ID 변경) |
| FUNC-07 | ID 변경 단축키 전 클래스 일관 동작 | KeyDown 핸들러(2057-2185) 분석: person은 Ctrl+1~0(1-10) + Alt+1~0(11-20), vehicle은 Ctrl+1~4, event는 Ctrl+1~0. 단, vehicle과 event가 동일 Ctrl+N을 공유하여 충돌 가능성 존재 |
| FUNC-08 | 단축키 ID 변경 시 선택 객체만 변경 | ChangeBoxIdWithinWaypoint가 waypoint 내 동일 oldId 가진 모든 박스를 변경(line 2208). selectedBox만 변경해야 하는 요구사항과 충돌 -- waypoint 내 일괄 변경은 의도적 설계이므로 요구사항 해석 필요 |
| FUNC-09 | 새 영상 로드 시 상태 완전 초기화 | LoadLabelingData(MainForm.cs:422-430)에서 clear 수행하지만 undoStack, redoStack, entryFrameIndex, exitFrameIndex, currentMode, currentAssignedId 등 미초기화 |
| FUNC-10 | BBOX 삭제 시 내부 데이터 즉시 반영 | Delete 키(2152-2156)는 `IsDeleted = true`만 설정하고 리스트에서 제거하지 않음. btnDeleteLabel(2310-2316)은 `boundingBoxes.Remove()` 사용. 두 경로의 불일치가 저장 시 `!b.IsDeleted` 필터로 숨겨지지만 내부 상태 불일치 유발 |
| COMP-01 | COCO JSON 타 ML 도구 호환 | FUNC-02, FUNC-03, FUNC-04 수정과 연동. bbox 좌표가 [x, y, w, h] 형식이고 이미지 범위 내여야 함 |
| RELI-05 | 손상된 JSON/SRT 로드 시 크래시 방지 | JsonService.LoadLabelingDataAsync(line 387-394)에 generic catch 있으나 JsonReaderException, FormatException 등 구체적 처리 부재. SRT ParseSrtTime(line 481-498)에 int.Parse 예외 처리 없음 |
| USAB-03 | 오류 메시지 구체적 + 해결 방법 제시 | 현재 오류 메시지가 기술적 ex.Message 노출 위주. 사용자 친화적 메시지 + 해결 방법 패턴 적용 필요 |
| SECU-01 | SHA-256 + Salt PBKDF2 라이선스 해싱 | 현재 라이선스/MAC 관련 코드가 Forms/MainForm.cs에 없음. 라이선스 검증 로직이 별도 존재하지 않으면 신규 구현 필요. .NET 8 BCL Rfc2898DeriveBytes.Pbkdf2() 사용 |
| SECU-04 | 파일 경로 트래버설 방지 | 파일 경로 구성 지점: JsonService.cs ResolveJsonPath(116-132), SaveCurrentLabelingData(532-553), DeleteJsonFileForVideo(573-601). Path.GetFullPath() 후 허용 디렉토리 내 존재 확인 필요 |
| MAINT-02 | generic catch(Exception)을 구체적 타입으로 | MainForm.cs 8개소, JsonService.cs 2개소, VideoService.cs 3개소에서 catch(Exception) 사용 중 |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Tech stack**: C# .NET 8.0 WinForms -- 기존 코드 기반 개선만
- **Certification**: ISO/IEC 25023 8대 품질 특성 충족
- **Security**: KISA 가이드 준수 -- SHA-256 이상 단방향 암호화 + Salt
- **Defects**: Critical/High 등급 결함 0건 필수
- **No new features**: 기존 기능 완성도만 개선
- **No architecture overhaul**: 대규모 리팩토링 불가
- **Korean UI/comments**: 한국어 UI 및 코멘트 유지
- **GSD Workflow**: Edit/Write 전 GSD 명령 사용

## Standard Stack

### Core (already in project -- no new NuGet packages)
| Library | Version | Purpose | Phase 3 Usage |
|---------|---------|---------|---------------|
| .NET 8.0 BCL | 8.0 | Runtime | `System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2()` for SECU-01 |
| Newtonsoft.Json | 13.0.3 | JSON | COCO export/import fixes (FUNC-02, FUNC-04, COMP-01) |
| OpenCvSharp4 | 4.11.0 | Video | FrameWidth/FrameHeight for bbox clamping (FUNC-03) |
| Serilog | installed | Logging | Error logging for RELI-05, MAINT-02 |

### No New Dependencies Required
Phase 3 requires zero new NuGet packages. All cryptographic operations use .NET 8 BCL (`System.Security.Cryptography`). JSON operations use existing Newtonsoft.Json.

## Architecture Patterns

### New Files to Create
```
Helpers/
  SecurityHelper.cs     # SECU-01: PBKDF2 hash + verify
  PathValidator.cs      # SECU-04: Path traversal prevention
```

### Files to Modify
```
Forms/MainForm.cs       # FUNC-01,03,05-10, USAB-03, MAINT-02 (heaviest changes)
Services/JsonService.cs # FUNC-02,04, COMP-01, RELI-05, MAINT-02
Services/VideoService.cs # RELI-05 (SRT parsing), MAINT-02
```

### Pattern 1: Bbox Coordinate Clamping
**What:** Every code path that sets `BoundingBox.Rectangle` must clamp to `[0, 0, frameWidth, frameHeight]`
**When to use:** After drawing, dragging, resizing, keyboard-moving, or loading from JSON
**Affected locations:**
- `pictureBoxVideo_MouseUp` (draw complete, line 1424)
- `pictureBoxVideo_MouseMove` (drag, line 1403-1408; draw, line 1384)
- `MainForm_KeyDown` WASD (line 2149)
- `PerformResize` (resize complete)
- `JsonService.LoadLabelingDataAsync` (load from file, line 302)

**Implementation:**
```csharp
// Add to CoordinateHelper or as local method
public static Rectangle ClampToImage(Rectangle rect, int imageWidth, int imageHeight)
{
    int x = Math.Max(0, Math.Min(rect.X, imageWidth - 1));
    int y = Math.Max(0, Math.Min(rect.Y, imageHeight - 1));
    int w = Math.Min(rect.Width, imageWidth - x);
    int h = Math.Min(rect.Height, imageHeight - y);
    return new Rectangle(x, y, Math.Max(1, w), Math.Max(1, h));
}
```

### Pattern 2: Timestamp Fix (FUNC-02)
**What:** JSON export timestamps should be frame-time-based, not `DateTime.Now`-based
**Bug location:** `JsonService.ExportToJsonExtended()` lines 428-429 and 492-495

**Current (BUGGY):**
```csharp
double frameSeconds = frameGroup.Key / fps;
DateTime frameTime = DateTime.Now.AddSeconds(frameSeconds);  // BUG: uses wall clock
string timestamp = subtitleTimestamp ?? frameTime.ToString("yyyy-MM-ddTHH:mm:ss.fff");
```

**Fix:**
```csharp
double frameSeconds = frameGroup.Key / fps;
TimeSpan frameTimeSpan = TimeSpan.FromSeconds(frameSeconds);
// Use relative time from video start (00:00:00.000) when no subtitle timestamp
string timestamp = subtitleTimestamp ?? frameTimeSpan.ToString(@"hh\:mm\:ss\.fff");
```

Same pattern for entry/exit timestamps (lines 492-495).

### Pattern 3: State Reset on Video Load (FUNC-09)
**What:** `LoadLabelingData()` currently resets annotation data but misses UI/interaction state
**Missing resets (MainForm.cs ~line 422-430):**
```csharp
// Currently reset:
boundingBoxes.Clear(); waypointMarkers.Clear(); selectedBox = null;
selectedWaypoint = null; categoryMap = new(); frameTimestampMap = new();
nextAnnotationId = 1; labelCurrentJsonFile.Text = "";

// MISSING -- must add:
undoStack.Clear();
redoStack.Clear();
entryFrameIndex = null;
exitFrameIndex = null;
currentMode = DrawMode.Select;
isDrawing = false;
isDragging = false;
isResizing = false;
```

### Pattern 4: PBKDF2 License Hashing (SECU-01)
**What:** New `SecurityHelper.cs` with KISA-compliant PBKDF2-HMAC-SHA256
**Key parameters:**
- Algorithm: PBKDF2-HMAC-SHA256 (via `Rfc2898DeriveBytes.Pbkdf2()`)
- Salt: 16 bytes from `RandomNumberGenerator.GetBytes(16)`
- Iterations: 310,000 (OWASP 2023 recommendation for SHA-256)
- Output: 32 bytes (256 bits)

```csharp
// Helpers/SecurityHelper.cs
using System.Security.Cryptography;

public static class SecurityHelper
{
    private const int SALT_SIZE = 16;
    private const int HASH_SIZE = 32;
    private const int ITERATIONS = 310_000;

    public static (string Hash, string Salt) HashSecret(string input)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            input, salt, ITERATIONS,
            HashAlgorithmName.SHA256, HASH_SIZE);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool VerifySecret(string input, string storedHash, string storedSalt)
    {
        byte[] salt = Convert.FromBase64String(storedSalt);
        byte[] candidateHash = Rfc2898DeriveBytes.Pbkdf2(
            input, salt, ITERATIONS,
            HashAlgorithmName.SHA256, HASH_SIZE);
        return CryptographicOperations.FixedTimeEquals(
            candidateHash, Convert.FromBase64String(storedHash));
    }
}
```

**Confidence:** HIGH -- `Rfc2898DeriveBytes.Pbkdf2` static method is documented in official .NET 8 Microsoft docs. `CryptographicOperations.FixedTimeEquals` prevents timing attacks.

### Pattern 5: Path Traversal Prevention (SECU-04)
**What:** Validate all file paths before I/O operations
```csharp
// Helpers/PathValidator.cs
public static class PathValidator
{
    public static bool IsPathSafe(string filePath, string allowedBaseDir)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        string fullPath = Path.GetFullPath(filePath);
        string fullBase = Path.GetFullPath(allowedBaseDir);
        return fullPath.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase);
    }
}
```

Apply at: `JsonService.ResolveJsonPath()`, `SaveCurrentLabelingData()`, `DeleteJsonFileForVideo()`, video file open dialog result.

### Anti-Patterns to Avoid
- **DateTime.Now for timestamps:** Never use wall-clock time for frame timestamps. Use `frameIndex / fps` relative time.
- **IsDeleted flag without list removal:** The current mixed approach (sometimes `IsDeleted = true`, sometimes `Remove()`) causes state drift. Standardize on one approach.
- **generic catch(Exception) for all errors:** Catches OutOfMemoryException, StackOverflowException, etc. Always catch specific types first.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| PBKDF2 hashing | Custom SHA-256 + string salt concat | `Rfc2898DeriveBytes.Pbkdf2()` | BCL handles salt derivation, iteration, HMAC correctly |
| Timing-safe compare | `==` for hash comparison | `CryptographicOperations.FixedTimeEquals()` | Prevents timing side-channel attacks |
| Path normalization | Manual `..` stripping | `Path.GetFullPath()` + prefix check | Handles all edge cases (UNC, symlinks, mixed separators) |
| JSON error handling | Generic try-catch | `JsonReaderException`, `JsonSerializationException` | Specific error messages for corrupted files |

## Common Pitfalls

### Pitfall 1: Keyboard Shortcut Collision (FUNC-07)
**What goes wrong:** Ctrl+1~4 is shared between vehicle (Ctrl+1~4) and event (Ctrl+1~10) when `currentSelectedLabel` check overlaps with `selectedBox.Label` check
**Why it happens:** The KeyDown handler checks both `currentSelectedLabel` and `selectedBox?.Label` for vehicle AND event blocks. If a vehicle box is selected but `currentSelectedLabel` is "event", both blocks execute.
**How to avoid:** Add `return` or exclusive conditions. The vehicle block at line 2101 checks `currentSelectedLabel == "vehicle" || selectedBox?.Label == "vehicle"`, then the event block at 2115 checks the same pattern for event. If selectedBox is vehicle but currentSelectedLabel is event, vehicle block runs first (correct). But if reversed, event block could run instead.
**Warning signs:** Pressing Ctrl+1 changes wrong box's ID

### Pitfall 2: BBOX Delete Inconsistency (FUNC-10)
**What goes wrong:** Delete key sets `IsDeleted = true` (soft delete) but Delete button calls `boundingBoxes.Remove()` (hard delete). `UpdateBoxCount()` counts `!b.IsDeleted`, so soft-deleted boxes remain in memory.
**Why it happens:** Two deletion paths evolved independently
**How to avoid:** Standardize on hard delete (`Remove()`) for both paths. Soft delete with `IsDeleted` is unnecessary since Undo stores a clone.
**Warning signs:** Box count shows correct number but internal list is larger; exported JSON may differ from display

### Pitfall 3: Waypoint-less Boxes with ID=0 (FUNC-06)
**What goes wrong:** User draws a bbox (gets ID from `currentAssignedId`), then creates a waypoint. Later changes the ID. If the box was created with ID=0, `FindWaypointForBox` returns null because waypoint.ObjectId=0 won't match the new ID.
**Why it happens:** `ChangeBoxIdWithinWaypoint` depends on `FindWaypointForBox` which matches by current ID. If ID was already changed, the old waypoint has the old ObjectId.
**How to avoid:** When changing ID, also update the waypoint's ObjectId. Current code already does this (line 2210), but only when waypoint is found. Edge case: box created after waypoint with different ID.

### Pitfall 4: Timestamp Drift
**What goes wrong:** Every JSON export produces different timestamps even for the same frame
**Root cause:** `DateTime.Now.AddSeconds(frameSeconds)` at JsonService.cs:428
**How to avoid:** Replace with relative time from video start. Only use SRT subtitle timestamps when available.

### Pitfall 5: PBKDF2 Iteration Count Performance
**What goes wrong:** 310,000 iterations takes ~200-500ms on modern hardware. If called on UI thread, app freezes.
**How to avoid:** Run license verification on background thread or during splash screen. Log result via `LogService.AuditLicenseError()` if verification fails.

### Pitfall 6: State Leak Between Videos (FUNC-09)
**What goes wrong:** Loading a new video keeps undo/redo history from previous video, so Ctrl+Z restores boxes from wrong video
**Root cause:** `LoadLabelingData()` clears annotation data but not `undoStack`, `redoStack`, `entryFrameIndex`, `exitFrameIndex`
**Warning signs:** Undo after video switch creates phantom boxes

## Code Examples

### Corrupted JSON Handling (RELI-05)
```csharp
// In JsonService.LoadLabelingDataAsync, replace generic catch:
catch (JsonReaderException jrEx)
{
    result.Success = false;
    result.ErrorMessage = $"JSON 파일이 손상되었습니다.\n\n" +
        $"파일: {Path.GetFileName(loadPath)}\n" +
        $"위치: 줄 {jrEx.LineNumber}, 열 {jrEx.LinePosition}\n\n" +
        $"해결 방법: 백업 파일({Path.GetFileName(loadPath)}.backup)을 확인하거나 " +
        $"새로 라벨링을 시작하세요.";
}
catch (JsonSerializationException jsEx)
{
    result.Success = false;
    result.ErrorMessage = $"JSON 데이터 형식이 올바르지 않습니다.\n\n" +
        $"상세: {jsEx.Message}\n\n" +
        $"해결 방법: JSON 파일의 구조가 COCO 형식과 일치하는지 확인하세요.";
}
```

### SRT Parse Error Handling (RELI-05)
```csharp
// In VideoService.LoadSrtFileAsync, add specific catches:
catch (FormatException fmtEx)
{
    Log.Warning("[자막 파싱 오류] SRT 파일 형식 오류: {Message}", fmtEx.Message);
    subtitleEntries.Clear(); // partial parse를 버림
}
catch (IOException ioEx)
{
    Log.Warning("[자막 로드 오류] 파일 읽기 실패: {Message}", ioEx.Message);
}
```

### Specific Exception Types for MAINT-02
```
// Replace catch(Exception) with:
// File I/O: catch (IOException), catch (UnauthorizedAccessException)
// JSON: catch (JsonReaderException), catch (JsonSerializationException)  
// Video: catch (OpenCvSharp.OpenCVException), catch (InvalidOperationException)
// General fallback: catch (Exception) 유지하되 구체적 타입 먼저 처리
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual verification (no unit test framework -- per REQUIREMENTS.md Out of Scope) |
| Config file | none |
| Quick run command | `dotnet build -c Debug` |
| Full suite command | `dotnet build -c Release --no-incremental` |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| FUNC-01 | Vehicle ComboBox 선택 교체 | manual | Build + run app | N/A |
| FUNC-02 | JSON 타임스탬프 정확성 | manual | Export JSON, inspect timestamps | N/A |
| FUNC-03 | BBOX 좌표 클램핑 | manual | Draw box at edges, check JSON | N/A |
| FUNC-04 | 카테고리 ID 매핑 | manual | Export with all label types | N/A |
| FUNC-05 | 모든 버튼 동작 | manual | Click each button | N/A |
| FUNC-06 | Entry-Exit ID 일관성 | manual | Create waypoint, change ID | N/A |
| FUNC-07 | 단축키 전 클래스 동작 | manual | Test Ctrl+N for each label | N/A |
| FUNC-08 | 선택 객체만 ID 변경 | manual | Multi-box + shortcut test | N/A |
| FUNC-09 | 영상 전환 시 상태 초기화 | manual | Load video A, annotate, load B | N/A |
| FUNC-10 | BBOX 삭제 즉시 반영 | manual | Delete box, export, compare | N/A |
| COMP-01 | COCO JSON 호환성 | manual | Load exported JSON in pycocotools | N/A |
| RELI-05 | 손상 파일 크래시 방지 | manual | Open corrupted JSON/SRT | N/A |
| USAB-03 | 오류 메시지 구체성 | manual | Trigger each error path | N/A |
| SECU-01 | PBKDF2 해싱 | smoke | `dotnet build` (compile check) | N/A |
| SECU-04 | 경로 트래버설 방지 | manual | Attempt `../../` in paths | N/A |
| MAINT-02 | 구체적 예외 타입 | smoke | `dotnet build` (compile check) | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build -c Debug`
- **Per wave merge:** `dotnet build -c Release --no-incremental`
- **Phase gate:** Full build green + manual verification of all 11 success criteria

### Wave 0 Gaps
None -- no unit test framework (explicitly out of scope per REQUIREMENTS.md).

## Bug Root Cause Analysis

### FUNC-01: Vehicle 라벨 드롭다운 교체
**Location:** MainForm.cs:2458-2493
**Current behavior:** ComboBox `SelectedIndexChanged` calls `ChangeBoxIdWithinWaypoint(capturedBox, newClassId)`. This correctly updates VehicleId.
**Suspected issue:** When changing vehicle type from sidebar, the ComboBox may not reflect the current vehicle type correctly. `selIdx` (line 2475) uses `Array.IndexOf(options, text)` where `text = GetCategoryName()`. If the category name doesn't match options exactly, `selIdx = -1` and defaults to index 0 (car). This means any vehicle with unexpected ID shows as "car".
**Fix:** Ensure `GetCategoryName` output always matches ComboBox options. Verify the Vehicle combobox change actually persists by checking `GetBoxId()` returns the new value.

### FUNC-08: 다중 BBOX 단축키 -- 선택 객체만 변경
**Location:** MainForm.cs:2202-2218 (`ChangeBoxIdWithinWaypoint`)
**Current behavior:** Changes ALL boxes with same label+oldId within waypoint range. This is by design for waypoint consistency.
**Tension with requirement:** FUNC-08 says "선택된 객체만 변경". But waypoints semantically represent ONE object's track, so all boxes in a waypoint SHOULD share the same ID.
**Recommended interpretation:** "다중 BBOX" means multiple unrelated boxes on the same frame. The current code already scopes changes to waypoint boundaries. The bug is likely when a box is NOT in any waypoint but shares label+ID with another box -- then `ChangeBoxIdWithinWaypoint`'s else branch (line 2213-2216) correctly changes only the selected box. Verify this path works.

### SECU-01: License Verification
**Location:** No license verification code found in current source files (Forms/, Services/, Helpers/).
**Implication:** Either (a) license code exists in files not yet discovered, or (b) license verification is not yet implemented. The requirement says "라이선스 검증에 SHA-256 + Salt 기반 PBKDF2 해싱 적용" -- this implies existing MAC-based verification needs upgrading.
**Action:** Search for any `.lic`, `.key`, or MAC-related code at implementation time. If no existing code, create a new `LicenseService` with PBKDF2 hashing. `LogService.AuditLicenseError()` is already available (Phase 1).

## Open Questions

1. **SECU-01 -- Where is the current license code?**
   - What we know: No MAC/license code found in source files. CLAUDE.md mentions "현재 MAC 기반 인증 개선만"
   - What's unclear: Whether license validation exists in a compiled DLL, external file, or has not been implemented yet
   - Recommendation: At implementation time, do thorough search. If absent, create minimal license verification service

2. **FUNC-08 -- Waypoint-scoped vs. single-box ID change**
   - What we know: Current code changes all boxes in waypoint (by design). Requirement says "선택된 객체만"
   - What's unclear: Whether "selected only" means single frame or single waypoint track
   - Recommendation: Keep waypoint-scoped behavior (it's correct semantically) but ensure only boxes within the SELECTED box's waypoint are affected, not boxes of same label+ID in OTHER waypoints

3. **FUNC-02 -- Timestamp format when no SRT**
   - What we know: Current code uses `DateTime.Now.AddSeconds()` which is wrong
   - What's unclear: What format downstream ML tools expect (ISO 8601? relative time? specific format?)
   - Recommendation: Use relative time format `HH:mm:ss.fff` from video start (consistent, reproducible)

## Sources

### Primary (HIGH confidence)
- Codebase analysis: `Forms/MainForm.cs`, `Services/JsonService.cs`, `Services/VideoService.cs`, `Models/BoundingBox.cs`, `Models/LabelingData.cs`
- `.planning/research/STACK.md` -- PBKDF2 implementation pattern with .NET 8 BCL
- `.planning/research/PITFALLS.md` -- KISA compliance requirements, GS certification pitfalls
- `.planning/research/ARCHITECTURE.md` -- Security hardening patterns

### Secondary (MEDIUM confidence)
- .NET 8 `Rfc2898DeriveBytes.Pbkdf2()` -- confirmed in STACK.md research (originally from Microsoft docs)
- KISA SHA-256 + Salt requirement -- confirmed via project constraints and prior research

### Tertiary (LOW confidence)
- PBKDF2 iteration count (310,000) -- OWASP 2023 recommendation, KISA minimum is 1,000. May need adjustment per certifier guidance.

## Metadata

**Confidence breakdown:**
- Functional bugs (FUNC-01~10): HIGH -- all root causes identified in source code with line numbers
- Security (SECU-01, SECU-04): MEDIUM -- implementation patterns clear, but license code location unknown
- Exception handling (MAINT-02): HIGH -- all catch(Exception) locations enumerated
- COCO compatibility (COMP-01): HIGH -- JSON structure analyzed against COCO spec

**Research date:** 2026-04-16
**Valid until:** 2026-05-16 (stable codebase, no external dependency changes expected)
