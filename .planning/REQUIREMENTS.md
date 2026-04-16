# Requirements: AOLT GS인증 1등급

**Defined:** 2026-04-14
**Core Value:** 모든 라벨링 기능이 GS인증 1등급 기준(ISO/IEC 25023)을 충족하며 결함 없이 동작

## v1 Requirements

GS인증 1등급 통과를 위한 필수 개선 항목. ISO/IEC 25023 8대 품질 특성 + 일반적 요구사항.

### 기능적합성 (Functional Suitability)

- [ ] **FUNC-01**: Vehicle 라벨 드롭다운에서 차량 종류 선택 교체가 정상 동작
- [ ] **FUNC-02**: JSON 내보내기 시 타임스탬프가 실제 프레임 시간 기반으로 정확하게 생성
- [ ] **FUNC-03**: 바운딩 박스 좌표가 이미지 범위를 초과하지 않도록 클램핑 적용
- [ ] **FUNC-04**: COCO 포맷 JSON 출력의 카테고리 ID가 정확하게 매핑
- [ ] **FUNC-05**: 모든 UI 버튼/메뉴가 오류 없이 의도된 기능 수행
- [ ] **FUNC-06**: BBOX 생성 후 ID 사후 지정 시에도 Entry-Exit 구간에서 객체 ID가 일관되게 유지 (Entry-Exit 프레임 간 ID 불일치 시 안내 메시지 표시)
- [ ] **FUNC-07**: 객체 선택 상태에서 ID 변경 단축키(Ctrl+N / Alt+N)가 모든 클래스에서 일관되게 동작
- [ ] **FUNC-08**: 다중 BBOX 상태에서 단축키로 ID 변경 시 선택된 객체만 변경 (전체 일괄 변경 방지)
- [ ] **FUNC-09**: 새 영상 로드 시 이전 작업의 Waypoint·Labels·JSON 상태가 완전히 초기화
- [ ] **FUNC-10**: BBOX 삭제 시 내부 데이터에 즉시 반영되어 화면 상태와 저장 데이터가 일치

### 성능효율성 (Performance Efficiency)

- [ ] **PERF-01**: 프레임별 바운딩 박스 조회를 딕셔너리 인덱싱으로 최적화 (O(n) → O(1))
- [x] **PERF-02**: 영상 전환 시 이전 VideoCapture 리소스 정상 해제
- [x] **PERF-03**: Undo/Redo 스택에 MAX_UNDO_STACK 상한 실제 적용

### 호환성 (Compatibility)

- [ ] **COMP-01**: COCO JSON 출력이 타 ML 도구와 호환되는 정확한 데이터 생성
- [ ] **COMP-02**: FFmpeg 미설치 시 명확한 안내 메시지 제공 (무언 실패 제거)

### 사용성 (Usability)

- [ ] **USAB-01**: 모든 툴바 버튼에 툴팁 제공
- [ ] **USAB-02**: 파괴적 작업(전체 삭제 등) 실행 전 확인 다이얼로그 제공
- [ ] **USAB-03**: 오류 메시지가 구체적이고 해결 방법을 제시
- [ ] **USAB-04**: 저장되지 않은 변경 사항이 있을 때 종료 시 경고
- [ ] **USAB-05**: Undo/Redo 가능 여부를 버튼 활성/비활성으로 시각 표시
- [ ] **USAB-06**: 프로그램 상단 제품명이 문서 기준 정식 명칭("ANNA 합성데이터 라벨링 툴킷 (ASLT)v1.0")과 일치하도록 표기 및 버튼 배치 조정
- [ ] **USAB-07**: 단축키 설명에 클래스별(Person/Vehicle/Event) 적용 범위가 명확히 구분 표기되고, Vehicle 단축키 설명 포함, Event 접두어(event_) 제거
- [ ] **USAB-08**: Person/Vehicle 클래스에서 Entry-Exit 구간 내 프레임 단위 수동 추적(BBOX 위치 갱신) 지원 (Event 클래스와 동일 방식)

### 신뢰성 (Reliability)

- [x] **RELI-01**: 전역 예외 처리기로 처리되지 않은 예외에 의한 비정상 종료 방지
- [x] **RELI-02**: doubleClickTimer 누수 수정 (Dispose 경로 추가)
- [x] **RELI-03**: 영상 로드 시 CancellationToken 적용으로 레이스 컨디션 제거
- [x] **RELI-04**: Nullable 필드 접근 시 일관된 null 체크 적용
- [ ] **RELI-05**: 손상된 JSON/SRT 파일 로드 시 크래시 없이 사용자에게 안내

### 보안성 (Security)

- [ ] **SECU-01**: 라이선스 검증에 SHA-256 + Salt 기반 PBKDF2 해싱 적용 (KISA 가이드)
- [x] **SECU-02**: 파일 기반 감사 로그 — 시작, 종료, 저장, 라이선스 오류 등 주요 이벤트 기록
- [x] **SECU-03**: 감사 로그에 개인정보(MAC 주소 등) 미저장 또는 해싱 처리
- [ ] **SECU-04**: 파일 경로 입력에 대한 경로 트래버설 방지 (Path 정규화 검사)

### 유지보수성 (Maintainability)

- [x] **MAINT-01**: 파일 기반 구조화 로그 시스템 구축 (Serilog, 날짜별 로테이션, 로그 레벨)
- [ ] **MAINT-02**: generic catch(Exception) 블록을 구체적 예외 타입으로 교체
- [ ] **MAINT-03**: 매직 넘버를 명명된 상수로 추출

### 이식성 (Portability)

- [ ] **PORT-01**: Windows 10/11 클린 환경에서 정상 설치 및 실행 보장
- [ ] **PORT-02**: .NET 8 Runtime 및 Visual C++ Redistributable 의존성 확인/안내
- [ ] **PORT-03**: 설치 제거 시 잔여 파일 없는 클린 언인스톨

### 일반적 요구사항 (General Requirements)

- [ ] **DOC-01**: 제품설명서 작성 (버전 명시, 연동 제품 정보 포함)
- [ ] **DOC-02**: 사용자취급설명서 작성 (모든 기능, 입력값 유효 범위, 오류 메시지 기술)
- [ ] **DOC-03**: 프로그램 내 버전 정보와 문서 버전 일치

## v2 Requirements

인증 후 또는 차기 마일스톤 고려 항목.

### 신뢰성 개선

- **RELI-V2-01**: 주기적 자동저장 (5분 간격)
- **RELI-V2-02**: 시작 시 자동저장 백업 파일 감지 및 복구 제안

### 성능 개선

- **PERF-V2-01**: 이전/다음 프레임 슬라이딩 윈도우 캐시
- **PERF-V2-02**: BenchmarkDotNet 기반 성능 측정 보고서 생성

### 기능 개선

- **FUNC-V2-01**: 내보내기 전 어노테이션 유효성 검사 (너무 작은 박스, 범위 밖 좌표 경고)
- **FUNC-V2-02**: JSON 로드 시 카테고리 ID 범위 검증

## Out of Scope

| Feature | Reason |
|---------|--------|
| 새 기능 추가 (배치 처리, 키보드 설정 등) | GS인증은 기존 기능 완성도 평가, 새 기능은 추가 결함 리스크 |
| 크로스 플랫폼 지원 | WinForms는 Windows 전용, GS인증도 명시된 환경만 평가 |
| MVVM 아키텍처 전환 | 대규모 리팩토링은 기능 변경 리스크, 범위 밖 |
| 상용 라이선스 시스템 전면 교체 | 현재 MAC 기반 인증 개선만, 전면 교체 불가 |
| 단위 테스트 프레임워크 도입 | GS인증에서 테스트 코드 자체는 평가하지 않음 |
| 4K 60fps 지원 | 새 기능이며 현재 WinForms 구조로 불가능 |
| 다국어(영어) 지원 | 인증 범위 밖, 기존 한국어 UI 일관성 유지 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| FUNC-01 | Phase 3 | Pending |
| FUNC-02 | Phase 3 | Pending |
| FUNC-03 | Phase 3 | Pending |
| FUNC-04 | Phase 3 | Pending |
| FUNC-05 | Phase 3 | Pending |
| PERF-01 | Phase 4 | Pending |
| PERF-02 | Phase 2 | Complete |
| PERF-03 | Phase 2 | Complete |
| COMP-01 | Phase 3 | Pending |
| COMP-02 | Phase 4 | Pending |
| USAB-01 | Phase 4 | Pending |
| USAB-02 | Phase 4 | Pending |
| USAB-03 | Phase 3 | Pending |
| USAB-04 | Phase 4 | Pending |
| USAB-05 | Phase 4 | Pending |
| RELI-01 | Phase 2 | Complete |
| RELI-02 | Phase 2 | Complete |
| RELI-03 | Phase 2 | Complete |
| RELI-04 | Phase 2 | Complete |
| RELI-05 | Phase 3 | Pending |
| SECU-01 | Phase 3 | Pending |
| SECU-02 | Phase 1 | Complete |
| SECU-03 | Phase 1 | Complete |
| SECU-04 | Phase 3 | Pending |
| MAINT-01 | Phase 1 | Complete |
| MAINT-02 | Phase 3 | Pending |
| MAINT-03 | Phase 4 | Pending |
| PORT-01 | Phase 5 | Pending |
| PORT-02 | Phase 5 | Pending |
| PORT-03 | Phase 5 | Pending |
| DOC-01 | Phase 6 | Pending |
| DOC-02 | Phase 6 | Pending |
| DOC-03 | Phase 6 | Pending |
| FUNC-06 | Phase 3 | Pending |
| FUNC-07 | Phase 3 | Pending |
| FUNC-08 | Phase 3 | Pending |
| FUNC-09 | Phase 3 | Pending |
| FUNC-10 | Phase 3 | Pending |
| USAB-06 | Phase 4 | Pending |
| USAB-07 | Phase 4 | Pending |
| USAB-08 | Phase 4 | Pending |

**Coverage:**
- v1 requirements: 41 total
- Mapped to phases: 41
- Unmapped: 0 ✓

---
*Requirements defined: 2026-04-14*
*Last updated: 2026-04-16 — FUNC-06~10, USAB-06~08 추가 (사용자 버그 리포트 8건 반영)*
