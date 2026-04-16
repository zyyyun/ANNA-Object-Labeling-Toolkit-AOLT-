# Phase 4: 성능 + 사용성 — Context

**Gathered:** 2026-04-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 4는 두 가지 축을 다룬다:
1. **성능 최적화**: 프레임별 bbox 조회를 딕셔너리 인덱싱으로 O(n) → O(1) 전환
2. **UI 피드백 일관성**: 툴팁, 확인 다이얼로그, 미저장 경고, Undo/Redo 시각 상태, 제품명 표기, 단축키 설명 재구성, 수동 추적 기능

USAB-08(수동 추적) 핵심 버그(스냅백)는 이미 discuss-phase 중 MainForm.cs에 직접 수정 완료. 나머지는 planning → execution 단계에서 구현.
</domain>

<decisions>
## Implementation Decisions

### USAB-08: Person/Vehicle 수동 추적 (이미 구현)
- **D-01:** Event 클래스와 동일한 전파 방식 적용: MouseUp 시 현재 프레임에서 ExitFrame까지 bbox 위치 전파 (`PropagatePersonVehicleBoxFromCurrentFrame`)
- **D-02:** 스냅백 버그 원인: `LoadFrame` 내 rebind 로직이 isDragging 중 새 프레임의 옛 위치 박스로 교체 → 수정: `carriedRect`로 현재 위치 유지, 박스 없으면 웨이포인트 범위 내 신규 생성
- **D-03:** resize 경로에도 동일 전파 적용 (MouseUp의 isResizing 분기)
- **구현 상태:** `Forms/MainForm.cs` 수정 완료, 빌드 0 오류 확인

### USAB-06: 제품명 표기
- **D-04:** 정식 명칭: **"ANNA 합성데이터 라벨링 툴킷 (ASLT)v1.0"**
- **D-05:** 변경 대상 2곳:
  1. `Forms/MainForm.Designer.cs` labelTitle.Text + Size 확장 + Font 크기 12pt → 10pt
  2. `Forms/AboutForm.cs` 파란색 lblTitle.Text (현재 "ASLT v1.0", DarkTheme.Accent 색상)
- **D-06:** 영상 로드 시 labelTitle 동적 갱신 (`MainForm.cs` line 279: `$"ASLT v1.0 - {fileName}"`) → `$"ANNA 합성데이터 라벨링 툴킷 (ASLT)v1.0 - {fileName}"` 또는 짧은 형식 `$"ASLT v1.0 - {fileName}"` 유지 (Claude 재량)
- **D-07:** 상단 최소화/최대화/닫기 버튼 배치는 현재 우측 고정 위치 유지

### USAB-04: 미저장 경고
- **D-08:** isDirty 트리거: bbox 추가/삭제/이동 등 UndoAction이 추가되는 모든 조작 후 `_isDirty = true`, Ctrl+S(SaveCurrentLabelingData) 호출 시 `_isDirty = false`
- **D-09:** `OnFormClosing`에서 `_isDirty && 영상 로드됨` 조건으로 MessageBox.Show("저장하지 않은 변경사항이 있습니다. 저장하시겠습니까?", YesNoCancel) 표시
  - Yes → SaveCurrentLabelingData() 후 종료
  - No → 저장 없이 종료
  - Cancel → e.Cancel = true (종료 취소)

### USAB-02: 영상 전환 확인
- **D-10:** 영상 전환 시(`btnOpenVideo_Click`, drag-drop 등 영상 로드 진입점) `_isDirty` 확인:
  - isDirty + 영상 로드됨 → MessageBox.Show("저장하지 않은 편집이 있습니다.", YesNoCancel)
  - Yes → SaveCurrentLabelingData() 후 전환
  - No → 그냥 전환
  - Cancel → 전환 취소

### Claude's Discretion
- **PERF-01**: 딕셔너리 인덱싱 구현 방식 (`Dictionary<int, List<BoundingBox>>` 키=FrameIndex). `InvalidateBoxCache()`가 이미 있으므로 해당 메서드 내에서 dict도 초기화. `GetBboxesForFrame(int frameIndex)`를 헬퍼로 추가해 모든 `boundingBoxes.Where(b => b.FrameIndex == x)` 조회를 대체.
- **USAB-01**: 모든 툴바 버튼에 ToolTip 컴포넌트 추가. 툴팁 텍스트는 기능명으로.
- **USAB-05**: Undo/Redo 버튼이 현재 Designer에 없음 — 단축키(Ctrl+Z, Ctrl+Y)만 있는 것으로 보임. 버튼이 없으면 이 요구사항은 해당 없음으로 처리하거나 menuStrip이 있는지 확인 후 menu item enable/disable로 대체.
- **USAB-07**: AboutForm 단축키 목록 재구성:
  - 섹션별 분리: Person / Vehicle / Event
  - Vehicle Ctrl+1~4 단축키 추가 (현재 없음)
  - Event 항목에서 "event_" 접두어 제거 (예: "event_hazard" → "위험물체")
- **COMP-02**: FFMpegCore 초기화 시 FFmpeg 바이너리 미발견 → 사용자 안내 메시지 표시 (VideoService에 의존)
- **MAINT-03**: 지역 상수 `const int hitMargin = 4` 복수 존재 → 클래스 상수로 추출. 기타 magic number 탐색.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

No external specs — requirements fully captured in decisions above.

### 핵심 파일 (반드시 읽을 것)
- `Forms/MainForm.cs` — 메인 폼 전체 (isDirty, LoadFrame, SaveCurrentLabelingData, AddUndoAction)
- `Forms/MainForm.Designer.cs` — labelTitle 위치/크기/폰트 정의
- `Forms/AboutForm.cs` — 단축키 목록 + lblTitle
- `.planning/REQUIREMENTS.md` — PERF-01, USAB-01~08, COMP-02, MAINT-03 요구사항 원문

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `InvalidateBoxCache()` — bbox 캐시 무효화 진입점. 딕셔너리 인덱스 초기화도 여기서.
- `AddUndoAction(UndoAction)` — 모든 변경 조작의 공통 진입점. isDirty = true 설정 위치.
- `SaveCurrentLabelingData()` — Ctrl+S 저장 진입점. isDirty = false 설정 위치.
- `PropagateEventBoxFromCurrentFrame(BoundingBox)` — Event 전파 패턴. Person/Vehicle 메서드도 동일 구조로 이미 추가됨.
- `PropagatePersonVehicleBoxFromCurrentFrame(BoundingBox)` — **Phase discuss 중 신규 추가** (이미 MainForm.cs에 존재)

### Established Patterns
- 박스 캐시: `cachedCurrentFrameBoxes` (List) + `lastCachedFrameForPaint` (int) — Paint 이벤트 전용
- 박스 조회 패턴: `boundingBoxes.Where(b => b.FrameIndex == x)` — 성능 문제 지점 (PERF-01 대상)
- 에러 처리: Serilog `Log.Warning/Error` + 사용자 MessageBox (Phase 3에서 확립)

### Integration Points
- `timerPlayback_Tick` → `LoadFrame` → rebind (스냅백 수정 완료)
- `pictureBoxVideo_MouseUp` → `PropagatePersonVehicleBoxFromCurrentFrame` (이미 연결됨)
- `OnFormClosing` → isDirty 체크 + 경고 추가 필요
- 영상 로드 진입점 → isDirty 체크 + 확인 추가 필요

</code_context>

<specifics>
## Specific Ideas

- **제품명**: `"ANNA 합성데이터 라벨링 툴킷 (ASLT)v1.0"` — 사용자가 직접 명시
- **USAB-02 확인 메시지 형식**: "저장하지 않은 편집이 있습니다. 저장 후 전환하시겠습니까?" + YesNoCancel (Yes=저장+전환, No=그냥전환, Cancel=취소)
- **USAB-04 종료 경고**: "저장하지 않은 변경사항이 있습니다. 저장하시겠습니까?" + YesNoCancel

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 04-성능-사용성*
*Context gathered: 2026-04-16*
