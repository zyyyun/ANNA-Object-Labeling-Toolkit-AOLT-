---
phase: 04-성능-사용성
plan: 03
subsystem: Forms/UI (사용성)
tags: [usability, tooltips, branding, shortcuts, gs-certification]
requires:
  - Forms/MainForm.Designer.cs (existing toolbar buttons)
  - Forms/AboutForm.cs (existing shortcut ListView)
provides:
  - toolTipMain component with 21 SetToolTip bindings
  - Official product name in 3 locations (Designer, dynamic, AboutForm)
  - Class-grouped shortcut list (공통/Person/Vehicle/Event)
affects:
  - Forms/MainForm.Designer.cs
  - Forms/MainForm.cs
  - Forms/AboutForm.cs
tech-stack:
  added: []
  patterns:
    - "Designer-level ToolTip component with per-button SetToolTip"
    - "ListView separator rows via empty-tuple sentinel"
key-files:
  created: []
  modified:
    - Forms/MainForm.Designer.cs
    - Forms/MainForm.cs
    - Forms/AboutForm.cs
decisions:
  - "Vehicle 단축키 표기는 실제 카테고리(car/motorcycle/e_scooter/bicycle)를 따라 승용차/오토바이/전동킥보드/자전거로 표기 — 계획서의 버스/트럭은 실제 코드에 존재하지 않음 (Rule 1)"
  - "USAB-05는 btnUndo/btnRedo UI 버튼이 존재하지 않아 해당없음 처리 — 키보드 단축키(Ctrl+Z/Y)만 제공됨"
  - "USAB-08(수동 추적)은 04-02에서 이미 구현 완료 — 추가 작업 없음"
metrics:
  completed: 2026-04-17
  duration: ~10분
  tasks: 2
  files_modified: 3
requirements:
  - USAB-01
  - USAB-05
  - USAB-06
  - USAB-07
  - USAB-08
---

# Phase 04 Plan 03: 사용성 마무리 (툴팁/제품명/단축키) Summary

사용성 GS인증 요구사항 4건을 마감 처리했다. 툴바 21개 버튼에 한국어 툴팁을 부여하고, 프로그램 제품명을 정식 명칭 "ANNA 합성데이터 라벨링 툴킷 (ASLT)v1.0"으로 3곳 일치시키고, AboutForm 단축키 목록을 Person/Vehicle/Event 클래스별로 재구성했다.

## What Changed

### Task 1 — USAB-01 (툴팁) + USAB-06 (제품명) — Commit `e3c8e91`

**Forms/MainForm.Designer.cs:**
- `System.Windows.Forms.ToolTip toolTipMain` 컴포넌트 추가 (AutoPopDelay=5000, InitialDelay=500, ReshowDelay=100)
- 21개 버튼에 `SetToolTip` 바인딩: btnSelectFolder, btnExportJson, btnDeleteJson, btnAbout, btnMinimize, btnMaximize, btnClose, btnSelectAll, btnEdit, btnPlay, btnRewind, btnForward, btnEntry, btnExit, btnToggleSubtitle, btnLabelPerson, btnLabelVehicle, btnLabelEvent, btnDeleteLabel, btnExportJsonInLabels, btnDeleteEventWaypoint
- `labelTitle.Text`: "ASLT v1.0" → "ANNA 합성데이터 라벨링 툴킷 (ASLT)v1.0"
- `labelTitle.Font`: 12pt Bold → 10pt Bold (긴 제품명 대응)
- `labelTitle.Size`: (150, 25) → (350, 25)

**Forms/MainForm.cs:**
- 영상 로드 직후 동적 갱신 라인(line 310)의 접두어를 "ASLT v1.0 -" → "ANNA 합성데이터 라벨링 툴킷 (ASLT)v1.0 -"로 교체

### Task 2 — USAB-07 (단축키 재구성) + USAB-06 (AboutForm 제품명) — Commit `8a153eb`

**Forms/AboutForm.cs:**
- `lblTitle.Text`: "ASLT v1.0" → "ANNA 합성데이터 라벨링 툴킷 (ASLT)v1.0"
- `lblTitle.Font`: 20pt Bold → 16pt Bold
- 기존 평면 shortcuts 배열을 섹션 구분자(빈 튜플) 방식으로 재구성:
  - **공통** (17행): Space/모드/Entry/Exit/Tab/WASD/Delete/Undo/Redo/Save/프레임 네비/배속/자막/Escape
  - **Person** (3행): F1, F1+Ctrl+1~0 (1~10), F1+Alt+1~0 (11~20)
  - **Vehicle** (5행) — 신규 추가: F2, F2+Ctrl+1~4 (승용차/오토바이/전동킥보드/자전거)
  - **Event** (11행): F3, F3+Ctrl+1~0 (위험물체/사고/파손/화재/무단침입/누수/고장/분실물/쓰러짐/이상행동)
- `event_` 영문 접두어 전체 제거 (한국어 단일 표기)
- foreach 루프에 빈 튜플 감지 → `BackColor = DarkTheme.Background` 구분선 삽입 로직 추가

## Deviations from Plan

### [Rule 1 - Plan Bug] Vehicle 단축키 카테고리 수정

- **Found during:** Task 2 실행 전 사전 확인
- **Issue:** 계획서(04-03-PLAN.md line 200-203)는 Vehicle 카테고리를 "승용차/버스/트럭/오토바이"로 기재했지만, 실제 코드(MainForm.cs line 1077, Services/JsonService.cs line 27, 81-83)의 Vehicle 카테고리는 `car / motorcycle / e_scooter / bicycle` 4종이다. "버스"와 "트럭"은 이 프로젝트에 존재하지 않는다.
- **Fix:** 실제 코드와 일치하도록 "승용차 (car) / 오토바이 (motorcycle) / 전동킥보드 (e_scooter) / 자전거 (bicycle)"로 표기
- **Files modified:** Forms/AboutForm.cs
- **Commit:** 8a153eb

### USAB-05 해당없음 (Not Applicable)

- **근거:** MainForm.Designer.cs 전체에 `btnUndo`, `btnRedo` 식별자가 존재하지 않는다. Undo/Redo 기능은 키보드 단축키(Ctrl+Z, Ctrl+Y, Ctrl+Shift+Z)로만 제공된다.
- **결론:** 버튼이 없으므로 "Undo/Redo 가능 여부에 따라 버튼 활성/비활성 표시" 요구사항의 대상이 없다. 해당없음 처리.
- **참고:** 향후 툴바에 Undo/Redo 버튼을 추가한다면 undoStack/redoStack 크기 감지 기반으로 `btn.Enabled` 토글 로직을 별도 Plan으로 진행해야 한다.

### USAB-08 이미 구현 완료

- 04-02-SUMMARY에서 구현됨 (f1827c7 "fix(usab-08): fix snap-back bug + add Person/Vehicle manual tracking propagation"). 본 Plan에서는 추가 작업 없음.

## Verification Results

- `dotnet build -c Debug` → **0 errors**, 28 warnings (모두 기존 CS8632 nullable/CS1998 async 경고, 본 변경과 무관)
- `grep "toolTipMain" Forms/MainForm.Designer.cs` → 25+ occurrences (선언 + 설정 3줄 + SetToolTip 21줄)
- `grep "SetToolTip" Forms/MainForm.Designer.cs` → 21 occurrences
- `grep "ANNA 합성데이터 라벨링 툴킷" Forms/MainForm.Designer.cs Forms/MainForm.cs Forms/AboutForm.cs` → 3 files, 일치
- `grep "350, 25" Forms/MainForm.Designer.cs` → 1줄 (labelTitle.Size)
- `grep "10F" Forms/MainForm.Designer.cs` → labelTitle Font 확인
- `grep "Person 클래스\|Vehicle 클래스\|Event 클래스" Forms/AboutForm.cs` → 3 섹션 모두 존재
- `grep "승용차" Forms/AboutForm.cs` → 1줄
- `grep "event_" Forms/AboutForm.cs` → **0 occurrences** (접두어 완전 제거)
- `grep "separator" Forms/AboutForm.cs` → 3+ (구분선 로직)

## Requirements Status

| ID      | Status | Notes                                                                      |
| ------- | ------ | -------------------------------------------------------------------------- |
| USAB-01 | ✅ Done | 툴바 21개 버튼에 한국어 툴팁 적용                                            |
| USAB-05 | ⚠ N/A  | Undo/Redo UI 버튼 미존재 — 단축키만 제공됨. 향후 버튼 추가 시 별도 Plan 필요 |
| USAB-06 | ✅ Done | 정식 제품명 3곳(Designer/동적/AboutForm) 일치                              |
| USAB-07 | ✅ Done | Person/Vehicle/Event 클래스별 섹션 재구성 + Vehicle 추가 + event_ 제거     |
| USAB-08 | ✅ Done | 04-02에서 이미 구현 완료 (f1827c7)                                         |

## Commits

- `e3c8e91` — feat(04-03): add toolbar tooltips + official product name (USAB-01, USAB-06)
- `8a153eb` — feat(04-03): class-grouped shortcut list + AboutForm product name (USAB-07)

## Self-Check: PASSED

- [x] Forms/MainForm.Designer.cs toolTipMain 존재
- [x] Forms/MainForm.cs 동적 제품명 갱신 교체
- [x] Forms/AboutForm.cs 섹션 재구성 + event_ 제거
- [x] dotnet build 0 errors
- [x] 3 commits ( e3c8e91, 8a153eb + 이 SUMMARY 메타 커밋 )
