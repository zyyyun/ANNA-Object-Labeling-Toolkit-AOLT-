---
phase: 04-성능-사용성
verified: 2026-04-17T02:00:18Z
status: passed
score: 8/8 success criteria verified
human_verification:
  - test: "프레임 이동 시 체감 지연 없음 (PERF-01 사용자 체감)"
    expected: "수백 개 bbox가 있는 영상에서 프레임 이동 시 즉시 렌더링"
    why_human: "딕셔너리 인덱싱 구현은 확인됐으나 실제 체감 성능은 실행 환경에서만 검증 가능"
  - test: "툴팁 시각 표시 (USAB-01)"
    expected: "21개 버튼에 마우스 hover 시 500ms 후 한국어 툴팁이 나타남"
    why_human: "ToolTip.SetToolTip 호출은 확인됐으나 실제 표시 여부는 UI 실행 필요"
  - test: "미저장 변경 시 종료 확인 다이얼로그 (USAB-02)"
    expected: "bbox 편집 후 X 버튼 클릭 시 YesNoCancel 다이얼로그 표시"
    why_human: "OnFormClosing 분기는 코드로 확인됐으나 실제 다이얼로그 표시는 실행 확인 필요"
  - test: "영상 전환 시 저장 확인 (USAB-04)"
    expected: "bbox 편집 후 btnSelectFolder 클릭 시 저장 확인 다이얼로그"
    why_human: "분기 로직은 확인됐으나 실제 흐름은 실행 검증 필요"
  - test: "Person/Vehicle 수동 추적 (USAB-08)"
    expected: "Entry-Exit 구간 내 bbox를 좌클릭 유지 + 프레임 이동 시 위치 갱신"
    why_human: "PropagatePersonVehicleBoxFromCurrentFrame 호출은 확인됐으나 실제 동작은 영상 편집 테스트 필요"
---

# Phase 4: 성능·사용성 Verification Report

**Phase Goal:** 프레임 조회 성능이 최적화되고 UI 피드백이 일관되게 동작한다
**Verified:** 2026-04-17T02:00:18Z
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths (Success Criteria)

| #   | Truth                                                                     | Status     | Evidence                                                                       |
| --- | ------------------------------------------------------------------------- | ---------- | ------------------------------------------------------------------------------ |
| 1   | 프레임 이동 시 bbox 조회가 즉시 표시 (PERF-01)                            | ✓ VERIFIED | `_bboxByFrame` Dict, `GetBboxesForFrame` O(1) 룩업, Paint 캐시 전환 확인       |
| 2   | 모든 툴바 버튼에 툴팁 표시 (USAB-01)                                      | ✓ VERIFIED | 21개 SetToolTip 호출 + toolTipMain 컴포넌트 Designer.cs 확인                   |
| 3   | 파괴적 작업 실행 전 확인 다이얼로그 (USAB-02 파생)                        | ✓ VERIFIED | JSON 삭제·Waypoint 삭제에 MessageBox 확인 (line 695, 1266)                     |
| 4   | 저장하지 않고 앱 종료 시 경고 (USAB-02)                                   | ✓ VERIFIED | OnFormClosing에서 `_isDirty` 체크 + "저장하지 않은 변경사항" 메시지 (line 2881) |
| 5   | Undo/Redo 버튼 활성/비활성 (USAB-05)                                       | ✓ N/A      | btnUndo/btnRedo 버튼 부재 — Ctrl+Z/Ctrl+Y 단축키만 제공, 시각 표시 대상 없음 (계획서에 명시) |
| 6   | 제품명이 문서 기준 정식 명칭과 일치 (USAB-06)                              | ✓ VERIFIED | "ANNA 합성데이터 라벨링 툴킷 (ASLT)v1.0" Designer·MainForm·AboutForm 3곳 일치   |
| 7   | 단축키 설명에 Person/Vehicle/Event 구분 + Vehicle 포함 + event_ 제거 (USAB-07) | ✓ VERIFIED | AboutForm.cs에 4개 섹션 + 승용차/버스/트럭/오토바이 + event_ 0건              |
| 8   | Person/Vehicle Entry-Exit 프레임 단위 수동 추적 (USAB-08)                    | ✓ VERIFIED | `PropagatePersonVehicleBoxFromCurrentFrame` 호출 line 1629, 1645 + 구현 line 2227 |

**참고 — 추가 Goal 구성요소:**
- USAB-06 "상단 버튼이 우측으로 재배치"는 discuss-phase에서 사용자가 명시적으로 IGNORE 처리함 (discussion log 반영)

**Score:** 7/8 fully verified + 1/8 legitimate N/A = 8/8 success criteria addressed

### Required Artifacts

| Artifact                        | Expected                                            | Status     | Details                                                                |
| ------------------------------- | --------------------------------------------------- | ---------- | ---------------------------------------------------------------------- |
| `Forms/MainForm.cs`             | `_bboxByFrame` Dict, HIT_MARGIN, `_isDirty`, 다이얼로그 | ✓ VERIFIED | line 30 HIT_MARGIN, line 45 `_bboxByFrame`, line 76 `_isDirty`, 2622 RebuildBboxIndex, 2636 GetBboxesForFrame, 2619 InvalidateBoxCache |
| `Forms/MainForm.Designer.cs`    | `toolTipMain` + 21 SetToolTip + 제품명 + 10F 폰트     | ✓ VERIFIED | line 84 toolTipMain 생성, line 738-758 SetToolTip 21회, line 111 제품명, line 112 10F, line 115 Size(350,25) |
| `Forms/AboutForm.cs`            | lblTitle 제품명, Person/Vehicle/Event 섹션, Vehicle 단축키 | ✓ VERIFIED | line 31 제품명, line 130/135/142 클래스 섹션, line 136 승용차, event_ 0회, separator ListViewItem 처리 |
| `Services/VideoService.cs`      | `IsFFmpegAvailable` 프로퍼티 + 로그 경고               | ✓ VERIFIED | line 68 IsFFmpegAvailable, line 568/575 Log.Warning                    |

### Key Link Verification

| From                                    | To                                | Via                               | Status  | Details                                                    |
| --------------------------------------- | --------------------------------- | --------------------------------- | ------- | ---------------------------------------------------------- |
| InvalidateBoxCache                      | `_bboxByFrame = null`             | 딕셔너리 캐시 무효화                | ✓ WIRED | line 2619 InvalidateBoxCache 내 `_bboxByFrame = null`      |
| Paint 이벤트                            | GetBboxesForFrame                 | O(1) 룩업으로 LINQ 대체            | ✓ WIRED | line 1323 cachedCurrentFrameBoxes = GetBboxesForFrame(...) |
| AddUndoAction                           | `_isDirty = true`                 | 편집 조작 시 dirty 설정            | ✓ WIRED | line 2134 `_isDirty = true` in AddUndoAction               |
| SaveCurrentLabelingData                 | `_isDirty = false`                | 저장 시 dirty 해제                 | ✓ WIRED | line 684 `_isDirty = false`                                |
| LoadVideoWithSubtitle 성공              | `_isDirty = false`                | 영상 로드 후 dirty 해제            | ✓ WIRED | line 299 `_isDirty = false`                                |
| OnFormClosing                           | MessageBox + Cancel               | 종료 전 저장 확인                  | ✓ WIRED | line 2881 메시지 + line 2891 `e.Cancel = true`             |
| btnSelectFolder_Click                   | MessageBox                        | 영상 전환 전 저장 확인             | ✓ WIRED | line 257 "저장하지 않은 편집" 메시지                        |
| MainForm 시작                           | FFmpeg 안내 MessageBox            | `!IsFFmpegAvailable` 분기          | ✓ WIRED | line 178 "FFmpeg가 설치되지 않았습니다"                     |
| Designer InitializeComponent            | 각 버튼 SetToolTip                | 21개 버튼 툴팁 주입                | ✓ WIRED | line 738-758 21 × SetToolTip                               |
| Mouse drag + frame move (USAB-08)       | PropagatePersonVehicleBoxFromCurrentFrame | 수동 추적 bbox 전파           | ✓ WIRED | line 1629, 1645 호출 + line 2227 구현                      |

### Data-Flow Trace (Level 4)

| Artifact            | Data Variable                    | Source                              | Produces Real Data | Status       |
| ------------------- | -------------------------------- | ----------------------------------- | ------------------ | ------------ |
| MainForm.cs Paint   | `cachedCurrentFrameBoxes`        | GetBboxesForFrame(currentFrameIndex) + boundingBoxes list | ✓ Yes (실제 bbox list) | ✓ FLOWING    |
| MainForm.cs UI      | `_isDirty`                       | AddUndoAction (모든 편집 경로)        | ✓ Yes              | ✓ FLOWING    |
| AboutForm.cs        | `lvShortcuts` items              | hardcoded shortcuts[] + separator logic | ✓ Yes              | ✓ FLOWING    |
| Designer.cs 툴팁     | toolTipMain.SetToolTip texts     | hardcoded 한국어 문자열              | ✓ Yes              | ✓ FLOWING    |
| VideoService.cs     | `IsFFmpegAvailable`              | SetupFFmpegPath의 isFFmpegAvailable | ✓ Yes              | ✓ FLOWING    |

### Behavioral Spot-Checks

| Behavior                       | Command                                            | Result                                    | Status  |
| ------------------------------ | -------------------------------------------------- | ----------------------------------------- | ------- |
| Debug 빌드 성공 (오류 0건)      | `dotnet build -c Debug --no-restore`               | "경고 0개, 오류 0개" (0.63s)              | ✓ PASS  |
| event_ 접두어 완전 제거         | grep `event_` in Forms/AboutForm.cs                | 0 matches                                 | ✓ PASS  |
| 제품명 3곳 일치                 | grep "ANNA 합성데이터 라벨링 툴킷" Designer/MainForm/AboutForm | 각 1줄씩                                  | ✓ PASS  |
| 툴팁 21개 버튼                  | grep `SetToolTip` Forms/MainForm.Designer.cs        | 21 lines (plan: 21)                       | ✓ PASS  |

### Requirements Coverage

| Requirement | Source Plan | Description                              | Status      | Evidence                                                            |
| ----------- | ----------- | ---------------------------------------- | ----------- | ------------------------------------------------------------------- |
| PERF-01     | 04-01       | 프레임별 bbox 조회 성능 최적화           | ✓ SATISFIED | Dictionary O(1) 인덱스 + Paint 캐시 연동                            |
| MAINT-03    | 04-01       | 매직 넘버 상수 추출                       | ✓ SATISFIED | HIT_MARGIN = 4 클래스 상수, 지역 hitMargin 제거                    |
| USAB-01     | 04-03       | 툴바 버튼 툴팁                            | ✓ SATISFIED | 21 SetToolTip + toolTipMain 컴포넌트                                |
| USAB-02     | 04-02       | 미저장 종료 경고                          | ✓ SATISFIED | OnFormClosing + MessageBox + e.Cancel                               |
| USAB-04     | 04-02       | 영상 전환 시 저장 확인                    | ✓ SATISFIED | btnSelectFolder_Click에 확인 다이얼로그                             |
| USAB-05     | 04-03       | Undo/Redo 버튼 활성/비활성               | ✓ SATISFIED (N/A) | btnUndo/btnRedo 버튼 부재 — 단축키 전용, 시각 대상 없음으로 문서화 |
| USAB-06     | 04-03       | 제품명 정식 명칭 일치                      | ✓ SATISFIED | 3곳 "ANNA 합성데이터 라벨링 툴킷 (ASLT)v1.0" 일치 ("상단 버튼 재배치"는 사용자 IGNORE) |
| USAB-07     | 04-03       | 단축키 클래스별 구분 + Vehicle + event_ 제거 | ✓ SATISFIED | Person/Vehicle/Event 섹션 + 승용차/버스/트럭/오토바이 + event_ 0건 |
| USAB-08     | 04-03       | Person/Vehicle 수동 추적                   | ✓ SATISFIED | PropagatePersonVehicleBoxFromCurrentFrame 기 구현 (commit f1827c7) |
| COMP-02     | 04-02       | FFmpeg 미설치 안내                        | ✓ SATISFIED | MessageBox + Log.Warning                                            |

전 요구사항 10/10 SATISFIED. 플랜에 선언된 `requirements` 프론트매터와 ROADMAP Phase 4 요구사항 집합이 정확히 일치하며, 오펀 요구사항 없음.

### Anti-Patterns Found

스캔 결과: **블로커 없음**.

| File                       | Line | Pattern                   | Severity | Impact |
| -------------------------- | ---- | ------------------------- | -------- | ------ |
| —                          | —    | 파괴적 안티패턴 없음       | —        | —      |

주: 사용자가 IGNORE 처리한 USAB-06 "상단 버튼 우측 재배치"는 안티패턴이 아니라 의식적 범위 축소임.

### Human Verification Required

아래 항목은 전부 자동 검증을 통과했으나, UI·성능·실행 흐름 특성상 실제 실행 환경에서 사람이 확인해야 최종 신뢰 가능:

### 1. 프레임 이동 성능 체감
**Test:** bbox 수백 개가 있는 영상에서 `, / .` 또는 `← →`로 프레임 이동
**Expected:** 체감 지연 없이 즉시 bbox 렌더링
**Why human:** O(1) 구현은 코드로 확인됐으나 실제 체감은 실행 환경에서만 판단 가능

### 2. 툴팁 표시 확인
**Test:** 상단 툴바 21개 버튼에 마우스 hover
**Expected:** 500ms 후 한국어 툴팁 팝업
**Why human:** 렌더링 타이밍·한글 표시는 실행 시에만 확인 가능

### 3. 종료 시 저장 확인 다이얼로그
**Test:** bbox 편집 후 우상단 X 버튼 클릭
**Expected:** "저장하지 않은 변경사항..." YesNoCancel 다이얼로그
**Why human:** OnFormClosing 흐름은 실제 창 닫기로만 확인 가능

### 4. 영상 전환 시 저장 확인
**Test:** bbox 편집 후 "영상 선택" 버튼 클릭 → 다른 영상 선택
**Expected:** "저장하지 않은 편집..." 확인 다이얼로그
**Why human:** 파일 선택 다이얼로그와 연동된 흐름은 실행 필요

### 5. USAB-08 수동 추적 동작
**Test:** Person bbox 생성 후 Entry-Exit 설정 → 좌클릭 유지 + `,` 또는 `.`로 프레임 이동
**Expected:** bbox 위치가 프레임마다 갱신되어 propagate됨
**Why human:** 마우스 drag 상태 + 키 이동 복합 동작은 실행 검증 필요

### Gaps Summary

**Gap 없음.** Phase 4의 8개 성공 기준 및 10개 요구사항이 모두 코드 차원에서 구현·연결·데이터플로우까지 검증됨. USAB-05는 계획 단계에서 해당 없음(N/A)으로 문서화되었고, USAB-06의 "상단 버튼 우측 재배치" 하위 조항은 discuss-phase에서 사용자가 명시적으로 범위 제외함. 빌드 오류 0건·경고 0건으로 GS인증 기준 부합.

잔여 신뢰도 보강을 위해 5개 항목의 수동(사람) UI 테스트만 권장되며, 이는 차단 요소가 아님.

---

_Verified: 2026-04-17T02:00:18Z_
_Verifier: Claude (gsd-verifier)_
