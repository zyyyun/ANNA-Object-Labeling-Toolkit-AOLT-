# AOLT - ANNA Object Labeling Tool

## What This Is

AOLT는 영상 내 객체(사람, 차량, 이벤트)에 바운딩 박스를 그리고 COCO 형식 JSON으로 내보내는 Windows 데스크톱 라벨링 도구다. IFEZ 등 내부 연구원/엔지니어가 교통 영상 분석용 학습 데이터를 생성하는 데 사용한다.

## Core Value

모든 라벨링 기능이 GS인증 1등급 기준(ISO/IEC 25023 8대 품질 특성)을 충족하며 결함 없이 정확하게 동작해야 한다.

## Requirements

### Validated

기존 코드베이스에서 이미 구현된 기능:

- ✓ 영상 파일 로드 및 프레임 탐색 — existing
- ✓ 바운딩 박스 그리기/선택/이동/크기 조정 — existing
- ✓ Person/Vehicle/Event 라벨 분류 — existing
- ✓ COCO 형식 JSON 저장/로드 — existing
- ✓ Undo/Redo 기능 — existing
- ✓ Waypoint 마커 관리 — existing
- ✓ SRT 자막 추출 및 표시 — existing
- ✓ 다크 테마 UI — existing
- ✓ 재생 속도 조절 — existing
- ✓ 키보드 단축키 기반 조작 — existing

### Active

GS인증 1등급 통과를 위한 개선 항목:

- [x] 기능 적합성: 모든 기존 기능이 오류 없이 정확하게 동작 (vehicle 드롭다운 교체 불가 등 버그 수정) (Phase 3 완료)
- [ ] 기능 적합성: 모든 UI 컨트롤이 설명서 대로 동작
- [x] 성능 효율성: 프레임 로드 응답 시간 최적화 (바운딩 박스 조회 O(n) → 인덱싱) (Phase 4 완료)
- [x] 성능 효율성: CPU/메모리 사용 효율성 확보 (프레임 캐싱 개선) (Phase 4 완료)
- [x] 호환성: 데이터 교환(JSON) 정확성 보장 (타임스탬프 버그 수정) (Phase 3 완료)
- [x] 사용성: 일관된 UI 조작 방식 및 오류 메시지 제공 (Phase 4 완료)
- [ ] 사용성: 사용자 취급 설명서와 실제 동작 일치
- [x] 신뢰성: 고장 회피 (null 참조, 타이머 누수, 상태 불일치 수정) (Phase 2 완료)
- [ ] 신뢰성: 데이터 손실 방지 (자동 저장 또는 안전한 종료 처리)
- [x] 보안성: KISA 가이드 준수 암호화 적용 (SHA-256 이상) (Phase 3 완료)
- [x] 보안성: 접근 통제 강화 (라이선스 검증 개선) (Phase 3 완료)
- [x] 보안성: 감사 추적 — 사용자 활동 로그 기록 (Phase 1 완료)
- [x] 유지보수성: 구조화된 로그 시스템 구축 (파일 기반 로그) (Phase 1 완료)
- [x] 유지보수성: 예외 처리 체계화 (generic catch → 구체적 예외) (Phase 3 완료)
- [x] 이식성: 명시된 설치 환경에서 정상 설치/실행 보장 (Phase 5 완료 — 클린 VM 수동 검증 필요)

### Out of Scope

- 새 기능 추가 — GS인증은 기존 기능의 완성도 평가, 새 기능은 범위 밖
- 크로스 플랫폼 지원 — Windows 전용 WinForms 앱, GS인증도 명시된 환경만 평가
- 대규모 리팩토링 (MVVM 전환 등) — 기능 변경 없이 품질만 개선
- 상용 라이선스 시스템 — 현재 MAC 기반 인증을 개선하되 전면 교체는 하지 않음
- 테스트 자동화 프레임워크 도입 — 인증 평가에서 테스트 코드 자체는 평가하지 않음

## Context

- **기술 스택**: C# / .NET 8.0 / WinForms / OpenCvSharp4 / FFMpegCore / Newtonsoft.Json
- **코드 규모**: MainForm.cs 약 2,500줄 단일 클래스 + Services/Models/Helpers/Theme 계층
- **현재 상태**: Phase 5.5 완료 — Waypoint 선택 시 Entry/Exit 버튼 프레임 이동 + 영상 로드 중 타임라인 가드. 잠긴 바이너리 기능 완성.
- **코드베이스 맵**: `.planning/codebase/` 에 7개 분석 문서 존재
- **추가된 파일**: `Helpers/SecurityHelper.cs` (PBKDF2-HMAC-SHA256), `Helpers/PathValidator.cs` (경로 트래버설 방지)
- **해결된 주요 문제**:
  - Vehicle 드롭다운 선택 교체 버그 수정
  - JSON 타임스탬프 TimeSpan 기반으로 수정
  - bbox 좌표 클램핑 (6개 경로)
  - 영상 전환 시 상태 완전 초기화
  - Generic exception catch 구체화 + 사용자 친화적 오류 메시지
  - 로그 시스템 없음 (Debug.WriteLine만 사용)
  - 보안: 암호화 없음

## Constraints

- **Tech stack**: C# .NET 8.0 WinForms 유지 — 기존 코드 기반 개선만
- **Certification**: ISO/IEC 25023 8대 품질 특성 모두 충족 필요
- **Security**: KISA 가이드 준수 — SHA-256 이상 단방향 암호화 + Salt
- **Defects**: Critical/High 등급 결함 0건 필수 (Medium 이하 최소화)
- **Documentation**: 제품 설명서 + 사용자 취급 설명서 필요 (코드와 동작 일치)

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 기존 기능만 개선, 새 기능 추가 안 함 | GS인증은 기존 기능 완성도 평가 | — Pending |
| 대규모 아키텍처 리팩토링 제외 | 기능 변경 리스크 최소화 | — Pending |
| KISA 가이드 기반 보안 강화 | GS인증 보안성 탈락 사유 다수 | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd:transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-04-17 after Phase 5.5 completion*
