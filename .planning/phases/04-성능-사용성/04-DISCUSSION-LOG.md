# Phase 4: 성능 + 사용성 — Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-16
**Phase:** 04-성능-사용성
**Areas discussed:** USAB-08 수동 추적, USAB-06 제품명·버튼 배치, USAB-04 미저장 경고, USAB-02 확인 다이얼로그

---

## 영역 선택

| Option | Selected |
|--------|----------|
| USAB-08 수동 추적 동작 | ✓ |
| USAB-06 제품명·버튼 배치 | ✓ |
| USAB-04 미저장 경고 범위 | ✓ |
| USAB-02 확인 다이얼로그 범위 | ✓ |

---

## USAB-08: 수동 추적 동작

| Option | Description | Selected |
|--------|-------------|----------|
| Event 클래스와 동일 방식 | 드래그/리사이즈 후 MouseUp 시 현재 프레임→ExitFrame 전파 | ✓ |

**User's choice:** Event 클래스와 동일 방식 적용  
**Notes:** 스냅백 버그(클릭 유지 + 영상 재생 중 박스가 원위치로 돌아오는 현상)도 함께 수정 요청.
원인: `LoadFrame` rebind 시 isDragging 중 옛 위치 박스로 교체 → `carriedRect`로 수정.
**구현 상태:** discuss-phase 중 직접 구현 완료 (MainForm.cs 수정, 빌드 0 오류).

---

## USAB-06: 제품명·버튼 배치

| Option | Description | Selected |
|--------|-------------|----------|
| 전체 명칭 표기 | "ANNA 합성데이터 라벨링 툴킷 (ASLT)v1.0", 폰트 조정 | ✓ |
| 약어 + 버전 유지 | "ASLT v1.0" 그대로 유지 | |
| 두 줄로 분리 | 두 줄 표기 | |

**User's choice:** 전체 명칭 표기  
**Notes:** "정보 창에 있는 파란색 메인 네이밍을 'ANNA 합성데이터 라벨링 툴킷 (ASLT)v1.0'으로 변경" — AboutForm의 lblTitle(DarkTheme.Accent 색상) 포함. 버튼 배치는 현재 그대로 유지.

---

## USAB-04: 미저장 경고

| Option | Description | Selected |
|--------|-------------|----------|
| bbox 조작 후 미저장 (Recommended) | AddUndoAction 시 isDirty=true, Ctrl+S 시 false | ✓ |
| 영상 로드 후 저장 미실시 | Ctrl+S 한 번도 안 눌렀으면 경고 | |
| 경고 없이 자동 저장 | OnFormClosing에서 자동 저장 | |

**User's choice:** bbox 조작 후 미저장 상태에서 종료 시 경고  
**Notes:** YesNoCancel — Yes=저장+종료, No=저장없이종료, Cancel=종료취소.

---

## USAB-02: 확인 다이얼로그 범위

| Option | Description | Selected |
|--------|-------------|----------|
| JSON 삭제 (이미 있음) | 현재 YesNo 있음 — 유지 | |
| Waypoint 삭제 (이미 있음) | 현재 YesNo 있음 — 유지 | |
| 영상 전환 시 미저장 편집 있음 | 새 영상 로드 전 미저장 확인 | ✓ |
| 기존 2개만으로 충분 | | |

**User's choice:** 영상 전환 시 미저장 편집 있으면 확인 다이얼로그  
**Notes:** "저장하지 않은 편집이 있습니다. 저장 후 전환하시겠습니까?" + YesNoCancel

---

## Claude's Discretion

- PERF-01: Dictionary<int, List<BoundingBox>> 인덱싱 — 구현 방식 Claude 재량
- USAB-01: 툴팁 텍스트 내용 — Claude 재량
- USAB-05: Undo/Redo 버튼 존재 여부 확인 후 처리
- USAB-07: AboutForm 단축키 섹션 재구성 (Vehicle 추가, event_ 접두어 제거)
- COMP-02: FFmpeg 미발견 메시지 위치 — Claude 재량
- MAINT-03: magic number 추출 — Claude 재량

## Deferred Ideas

없음

---

*Discussion date: 2026-04-16*
