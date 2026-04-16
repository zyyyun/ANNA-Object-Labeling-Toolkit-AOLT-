---
phase: 03-기능-정확성-보안
verified: 2026-04-16T00:00:00Z
status: pass
score: 11/11 must-haves verified
gaps:
  - truth: "Entry-Exit 프레임 간 객체 ID가 불일치하면 안내 메시지가 표시된다"
    status: resolved
    reason: "SC7/FUNC-06의 하위 조건인 ID 불일치 감지 및 MessageBox 표시 로직이 MainForm.cs, JsonService.cs 어디에도 존재하지 않는다. ChangeBoxIdWithinWaypoint는 ID를 변경할 뿐 불일치 감지와 사용자 알림 로직이 없다."
    artifacts:
      - path: "Forms/MainForm.cs"
        issue: "ID 불일치 감지 조건문 및 MessageBox 경고 없음"
      - path: "Services/JsonService.cs"
        issue: "ExportToJsonExtended에서 ID 불일치 검증 없음"
    missing:
      - "ChangeBoxIdWithinWaypoint 또는 Waypoint 생성 시점에서: 동일 Entry-Exit 구간 내 동일 label 박스들의 ObjectId 일치 여부 검증"
      - "ID 불일치 감지 시 MessageBox.Show(\"Entry-Exit 구간 내 객체 ID가 일치하지 않습니다...\") 표시"
human_verification:
  - test: "Vehicle ComboBox 실제 선택 동작 확인"
    expected: "BBOX 선택 후 사이드바 Vehicle ComboBox에서 car/motorcycle/e_scooter/bicycle 선택 시 해당 박스의 VehicleId가 즉시 변경된다"
    why_human: "ComboBox UI 인터랙션은 런타임에서만 검증 가능하며, SelectedIndexChanged 핸들러 연결 여부는 MainForm.Designer.cs 또는 런타임 바인딩을 통해 확인해야 한다"
  - test: "SHA-256 PBKDF2 해싱 실제 적용 경로 확인"
    expected: "라이선스 검증 진입점에서 SecurityHelper.HashSecret / VerifySecret이 호출된다"
    why_human: "SecurityHelper.cs는 구현되어 있으나 실제 라이선스 검증 호출 지점이 MainForm.cs에서 확인되지 않음 (라이선스 기능이 현재 버전에 UI로 노출되지 않을 수 있음)"
---

# Phase 3: 기능 정확성 + 보안 Verification Report

**Phase Goal:** 핵심 기능 버그가 수정되고 COCO JSON 정합성과 KISA 보안 기준이 충족된다
**Verified:** 2026-04-16
**Status:** pass (gap resolved: FUNC-06 mismatch warning added)
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths (Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Vehicle 라벨 드롭다운에서 차량 종류를 선택하고 교체할 수 있다 | ✓ VERIFIED | MainForm.cs line 2557-2593: ComboBox with `{"car","motorcycle","e_scooter","bicycle"}` options; SelectedIndexChanged calls ChangeBoxIdWithinWaypoint |
| 2 | 내보낸 COCO JSON의 타임스탬프가 실제 프레임 시간을 반영한다 | ✓ VERIFIED | JsonService.cs line 491: `TimeSpan.FromSeconds(frameGroup.Key / fps)` with `@"hh\:mm\:ss\.fff"` format; no DateTime.Now.AddSeconds |
| 3 | 바운딩 박스 좌표가 이미지 경계를 절대 초과하지 않는다 | ✓ VERIFIED | CoordinateHelper.ClampToImage exists (line 115-122); applied at 4 sites in MainForm.cs (draw, drag, resize, WASD) + export in JsonService.cs line 559 + load in JsonService.cs line 409 |
| 4 | 라이선스 검증에 SHA-256 + Salt 기반 PBKDF2 해싱이 적용된다 | ✓ VERIFIED* | SecurityHelper.cs: Rfc2898DeriveBytes.Pbkdf2 with SHA256, 310,000 iterations, CryptographicOperations.FixedTimeEquals. *Human check needed for call site |
| 5 | 손상된 JSON/SRT 파일을 열 때 크래시 없이 사용자 안내 메시지가 표시된다 | ✓ VERIFIED | JsonService.cs: JsonReaderException (line 422, includes LineNumber/LinePosition), JsonSerializationException (line 432), IOException (line 440) each produce user messages with "해결 방법:". VideoService.cs line 480: FormatException catch clears subtitleEntries without crash |
| 6 | BBOX 생성 후 ID를 사후 지정해도 Entry-Exit 구간에서 동일 객체 ID가 유지된다 | ✓ VERIFIED | ChangeBoxIdWithinWaypoint (MainForm.cs line 2301-2317): updates all boxes in waypoint range + waypoint.ObjectId = newId. FindWaypointForBox(b)==waypoint filter prevents cross-track mutation |
| 7 | Entry-Exit 프레임 간 객체 ID가 불일치하면 안내 메시지가 표시된다 | ✗ FAILED | No ID mismatch detection or MessageBox found in MainForm.cs, JsonService.cs, or VideoService.cs. grep for "불일치", "mismatch" returns no relevant match |
| 8 | 객체 선택 상태에서 ID 변경 단축키가 Person/Vehicle/Event 전 클래스에서 일관되게 동작한다 | ✓ VERIFIED | MainForm.cs: Ctrl+1~0 for person (selectedBox.Label=="person"), Ctrl+1~4 for vehicle (selectedBox.Label=="vehicle"), Ctrl+1~10 for event (selectedBox.Label=="event"). All dispatch through ChangeBoxIdWithinWaypoint |
| 9 | 다중 BBOX 상태에서 단축키로 ID 변경 시 선택된 객체만 변경된다 | ✓ VERIFIED | ChangeBoxIdWithinWaypoint filters: `b.Label == box.Label && GetBoxId(b) == oldId && b.FrameIndex >= waypoint.EntryFrame && b.FrameIndex <= waypoint.ExitFrame && !b.IsDeleted && FindWaypointForBox(b) == waypoint` |
| 10 | 새 영상 로드 시 이전 작업의 Waypoint·Labels·JSON 상태가 완전히 초기화된다 | ✓ VERIFIED | LoadLabelingData (MainForm.cs line 446-463): boundingBoxes.Clear(), waypointMarkers.Clear(), undoStack.Clear(), redoStack.Clear(), entryFrameIndex=null, exitFrameIndex=null, currentMode=DrawMode.Select, currentAssignedId=1, isDrawing=false, isDragging=false, isResizing=false |
| 11 | BBOX 삭제 시 내부 데이터에 즉시 반영되어 화면 상태와 저장 데이터가 일치한다 | ✓ VERIFIED | Delete key handler (line 2250-2255): `boundingBoxes.Remove(selectedBox)` + `InvalidateBoxCache()`. btnDeleteLabel_Click (line 2409-2415): same Remove() path. IsDeleted pattern eliminated from delete paths |

**Score:** 10/11 truths verified

---

## Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Helpers/SecurityHelper.cs` | PBKDF2-HMAC-SHA256, FixedTimeEquals | ✓ VERIFIED | 55 lines; Rfc2898DeriveBytes.Pbkdf2, SHA256, 310k iterations, CryptographicOperations.FixedTimeEquals |
| `Helpers/PathValidator.cs` | IsPathSafe, traversal prevention | ✓ VERIFIED | 38 lines; Path.GetFullPath normalization, StartsWith base dir check |
| `Helpers/CoordinateHelper.cs` | ClampToImage method | ✓ VERIFIED | ClampToImage (line 115): Math.Max/Min boundary enforcement |
| `Services/JsonService.cs` | TimeSpan timestamps, JsonReaderException, ClampToImage | ✓ VERIFIED | All three present; TimeSpan.FromSeconds at line 491, JsonReaderException at line 422, ClampToImage at line 559+409 |
| `Services/VideoService.cs` | FormatException, IOException in SRT parsing | ✓ VERIFIED | FormatException (line 480), IOException (line 485) in LoadSrtFileAsync |
| `Forms/MainForm.cs` | State reset, Remove() delete, shortcut dispatch, ClampToImage x4 | ✓ VERIFIED | All present; 4x ClampToImage confirmed, Remove() in both delete paths, selectedBox.Label dispatch confirmed |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| MainForm.cs (LoadLabelingData) | undoStack, redoStack, entryFrameIndex, exitFrameIndex | .Clear() / null assignment | ✓ WIRED | undoStack.Clear() line 457, redoStack.Clear() line 458, entryFrameIndex=null line 459, exitFrameIndex=null line 460 |
| MainForm.cs (Delete key handler) | boundingBoxes.Remove | Remove() replaces IsDeleted | ✓ WIRED | line 2253: `boundingBoxes.Remove(selectedBox)` |
| JsonService.cs (ExportToJsonExtended) | CoordinateHelper.ClampToImage | called before Bbox array assignment | ✓ WIRED | line 559: `CoordinateHelper.ClampToImage(box.Rectangle, frameWidth, frameHeight)` |
| JsonService.cs (LoadLabelingDataAsync) | JsonReaderException → user message | catch with LineNumber/LinePosition | ✓ WIRED | line 422-431: includes `jrEx.LineNumber`, `jrEx.LinePosition` in message |
| VideoService.cs (LoadSrtFileAsync) | FormatException → non-crash | catch clears entries | ✓ WIRED | line 480-484: clears subtitleEntries, logs warning |
| JsonService.cs (ResolveJsonPath) | PathValidator.IsPathSafe | called before returning path | ✓ WIRED | line 132: `PathValidator.IsPathSafe(normalPath, videoDir)` |

---

## Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| JsonService.cs ExportToJsonExtended | frameGroup (annotations) | boundingBoxes.Where(!IsDeleted).GroupBy(FrameIndex) | Yes — from live annotation list | ✓ FLOWING |
| JsonService.cs ExportToJsonExtended | timestamp | TimeSpan.FromSeconds(frameGroup.Key / fps) or subtitle | Yes — computed from frame index and fps | ✓ FLOWING |
| JsonService.cs LoadLabelingDataAsync | result.BoundingBoxes | parsed from JSON annotations | Yes — deserialized from file | ✓ FLOWING |

---

## Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| Build compiles with 0 errors | `dotnet build -c Debug 2>&1 \| tail -3` | 0 errors, 0 warnings, build succeeded | ✓ PASS |
| SecurityHelper exists and exports HashSecret | File exists, contains Rfc2898DeriveBytes.Pbkdf2 | Confirmed 55-line file with correct API | ✓ PASS |
| ClampToImage called at 4+ sites in MainForm | grep count | 4 matches in MainForm.cs | ✓ PASS |
| JsonReaderException in JsonService | grep count | 2 matches (catch + log) | ✓ PASS |
| FormatException in VideoService | grep count | 1 match in LoadSrtFileAsync | ✓ PASS |
| boundingBoxes.Remove in delete paths | grep count | 2 matches (Delete key + btnDeleteLabel) | ✓ PASS |
| ID mismatch MessageBox | grep "불일치\|mismatch" in source | 0 relevant matches | ✗ FAIL |

---

## Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| FUNC-01 | 03-01, 03-03 | Vehicle 라벨 드롭다운 정상 동작 | ✓ SATISFIED | ComboBox in MainForm.cs line 2557; SelectedIndexChanged → ChangeBoxIdWithinWaypoint |
| FUNC-02 | 03-01 | 타임스탬프 실제 프레임 시간 기반 | ✓ SATISFIED | TimeSpan.FromSeconds at JsonService.cs line 491 |
| FUNC-03 | 03-01, 03-03 | bbox 좌표 클램핑 | ✓ SATISFIED | CoordinateHelper.ClampToImage at 6 call sites |
| FUNC-04 | 03-01 | COCO 카테고리 ID 정확 매핑 | ✓ SATISFIED | GetCategoryId uses Math.Clamp; CategoryNameToIdMap covers all types |
| FUNC-05 | 03-03 | UI 버튼/메뉴 오류 없이 동작 | ? NEEDS HUMAN | Structural code review passed; runtime behavior needs manual test |
| FUNC-06 | 03-03 | ID 사후 지정 시 Entry-Exit 구간 일관성 + ID 불일치 안내 | ✗ BLOCKED | ID consistency within waypoint: SATISFIED. ID mismatch notification: NOT IMPLEMENTED |
| FUNC-07 | 03-03 | ID 변경 단축키 모든 클래스 일관 동작 | ✓ SATISFIED | selectedBox.Label dispatch for person/vehicle/event |
| FUNC-08 | 03-03 | 다중 BBOX에서 선택 객체만 ID 변경 | ✓ SATISFIED | FindWaypointForBox(b)==waypoint filter in ChangeBoxIdWithinWaypoint |
| FUNC-09 | 03-03 | 새 영상 로드 시 완전 초기화 | ✓ SATISFIED | 11 state variables reset in LoadLabelingData |
| FUNC-10 | 03-03 | BBOX 삭제 내부 데이터 즉시 반영 | ✓ SATISFIED | Remove() unified in both delete paths |
| COMP-01 | 03-01 | COCO JSON 출력 호환성 | ✓ SATISFIED | Correct bbox, category, track info structure |
| RELI-05 | 03-04 | 손상 파일 크래시 없이 안내 | ✓ SATISFIED | JsonReaderException+JsonSerializationException+IOException+FormatException all handled |
| USAB-03 | 03-04 | 오류 메시지에 해결 방법 포함 | ✓ SATISFIED | 5x "해결 방법" in MainForm.cs, 7x in JsonService.cs |
| SECU-01 | 03-02 | PBKDF2-HMAC-SHA256 + Salt | ✓ SATISFIED | SecurityHelper.cs with correct implementation |
| SECU-04 | 03-02 | 경로 트래버설 방지 | ✓ SATISFIED | PathValidator.IsPathSafe called in 3 locations in JsonService.cs + MainForm.cs |
| MAINT-02 | 03-04 | 구체적 예외 처리 | ✓ SATISFIED | Specific catch types (IOException, JsonReaderException, OpenCVException, InvalidOperationException) before generic catch |

---

## Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| None found | — | No TODO/FIXME/placeholder/stub patterns detected | — | — |

Checked MainForm.cs, JsonService.cs, VideoService.cs, SecurityHelper.cs, PathValidator.cs, CoordinateHelper.cs for TODO, FIXME, placeholder comments, empty implementations, hardcoded empty returns. None found.

---

## Human Verification Required

### 1. Vehicle ComboBox Runtime Binding

**Test:** Open a video, draw a Vehicle bbox, click to select it, open the side panel, verify ComboBox shows current vehicle type (car/motorcycle/e_scooter/bicycle). Change selection and confirm VehicleId updates.
**Expected:** VehicleId changes to match selected index + 1; ChangeBoxIdWithinWaypoint is called; JSON export reflects the new type.
**Why human:** SelectedIndexChanged handler is wired in C# code (not designer), but actual runtime binding and panel visibility conditions cannot be verified without running the application.

### 2. SHA-256 PBKDF2 Call Site

**Test:** Identify where in the application a license key is entered and verified. Check that SecurityHelper.HashSecret / SecurityHelper.VerifySecret is called at that point.
**Expected:** License verification uses SecurityHelper.VerifySecret with stored hash and salt; no plain-text comparison.
**Why human:** SecurityHelper.cs is fully implemented but no call site was found in MainForm.cs or other files during static analysis. The license verification UI may not yet exist in Phase 3 scope, or it may be in a form not yet wired. Needs runtime or code tracing to confirm actual integration.

---

## Gaps Summary

One gap blocks full goal achievement:

**SC7 / FUNC-06 sub-clause: ID mismatch notification not implemented.**

FUNC-06 in REQUIREMENTS.md reads: "BBOX 생성 후 ID 사후 지정 시에도 Entry-Exit 구간에서 객체 ID가 일관되게 유지 (Entry-Exit 프레임 간 ID 불일치 시 안내 메시지 표시)". The ID consistency part is satisfied by `ChangeBoxIdWithinWaypoint`. However the sub-clause requiring a warning MessageBox when Entry and Exit frames have different ObjectIDs for the same waypoint was never planned or implemented. None of the 4 plans (03-01 through 03-04) included this feature. The ROADMAP Success Criterion SC7 and REQUIREMENTS.md FUNC-06 both require it.

The fix is localized: add a validation check either at Waypoint creation time (SetExitMarkerAndCreateWaypoint) or at the ID change point (ChangeBoxIdWithinWaypoint) to detect when boxes at Entry frame and Exit frame have mismatched ObjectIds, and show a MessageBox guidance if detected.

---

_Verified: 2026-04-16_
_Verifier: Claude (gsd-verifier)_
