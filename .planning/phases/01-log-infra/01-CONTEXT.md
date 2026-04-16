# Phase 1: 로그 인프라 - Context

**Gathered:** 2026-04-16
**Status:** Ready for planning

<domain>
## Phase Boundary

애플리케이션 전반에 파일 기반 구조화 로그를 기록하고 감사 추적이 가능하도록 로그 인프라를 구축한다. 기존 Debug.WriteLine() 호출을 구조화 로그로 대체하고, 주요 이벤트에 대한 감사 로그를 기록하며, 개인정보(MAC 주소)가 로그에 기록되지 않도록 보장한다.

</domain>

<decisions>
## Implementation Decisions

### 로그 라이브러리
- **D-01:** Serilog 도입 (Serilog + Serilog.Sinks.File + Serilog.Sinks.Console)
- **D-02:** 기존 Debug.WriteLine() 8개소를 전부 Serilog 호출로 교체 (병행 유지 안 함)
- **D-03:** 로그 레벨 4단계: Debug / Info / Warning / Error

### 감사 로그 범위
- **D-04:** 감사 이벤트 필수 4개만 기록: 앱 시작, 앱 종료, JSON 저장, 라이선스 오류
- **D-05:** 감사 로그는 일반 로그와 동일 파일에 Info 레벨로 기록 (별도 파일 분리 안 함)

### 개인정보 처리
- **D-06:** MAC 주소는 감사 로그에 아예 기록하지 않음 (해싱도 하지 않음)
- **D-07:** 로그에 사용자 식별 정보 미포함 원칙

### 로그 저장 위치 + 로테이션
- **D-08:** 로그 파일 보관 기간: 30일 (초과 시 자동 삭제)
- **D-09:** 날짜별 로테이션: 하루 1개 파일 (예: AOLT-2026-04-16.log)

### Claude's Discretion
- 로그 파일 저장 경로 (실행 폴더/logs/ 또는 %APPDATA% 중 적합한 쪽 선택)
- Serilog 설정 방식 (코드 기반 vs appsettings.json)
- 로그 출력 포맷 (타임스탬프 형식, 메시지 구조)
- 감사 로그 메시지 형식 및 접두어 패턴

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 요구사항
- `.planning/REQUIREMENTS.md` — MAINT-01 (구조화 로그 시스템), SECU-02 (감사 로그), SECU-03 (개인정보 미저장)
- `.planning/ROADMAP.md` §Phase 1 — Success Criteria 4개 항목

### 코드베이스 분석
- `.planning/codebase/CONVENTIONS.md` — 기존 코딩 패턴, 네이밍 규칙, 에러 핸들링 패턴
- `.planning/codebase/STRUCTURE.md` — 디렉토리 구조, Services 계층 패턴

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- 없음 — 현재 로그 인프라 없음. Debug.WriteLine만 사용 중

### Established Patterns
- **Services 패턴**: `VideoService`, `JsonService` — 생성자에서 MainForm에 주입. 새 LogService도 동일 패턴 가능
- **Helpers 패턴**: `CoordinateHelper` — 정적 유틸리티. 로그 헬퍼도 정적 접근 가능
- **에러 핸들링**: try-catch + `[카테고리] 메시지` 포맷의 Debug.WriteLine. Serilog 전환 시 동일 카테고리 유지 가능

### Integration Points
- `Forms/MainForm.cs` — Debug.WriteLine 5곳 (프레임 로드, JSON 로드/저장, 재생, Exit 오류)
- `Services/VideoService.cs` — Debug.WriteLine 2곳 (프레임 로드, 자막 로드 오류)
- `Services/JsonService.cs` — Debug.WriteLine 1곳 (백업 파일 생성 실패)
- `Program.cs` — 앱 시작점. 감사 로그(앱 시작/종료) 기록 위치

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 01-log-infra*
*Context gathered: 2026-04-16*
