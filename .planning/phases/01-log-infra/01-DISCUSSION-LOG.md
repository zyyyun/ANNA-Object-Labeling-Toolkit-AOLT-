# Phase 1: 로그 인프라 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-16
**Phase:** 01-로그 인프라
**Areas discussed:** 로그 라이브러리 선택, 감사 로그 범위 + 개인정보 처리, 로그 저장 위치 + 로테이션 정책

---

## 로그 라이브러리 선택

| Option | Description | Selected |
|--------|-------------|----------|
| Serilog | .NET 생태계 표준. 구조화 로그, 파일 Sink 내장, 날짜별 로테이션 지원. NuGet 패키지 3개만 추가 | ✓ |
| NLog | .NET 레거시 로거. XML 설정 기반, 파일 타겟 내장. 설정이 더 복잡하지만 유연함 | |
| 자체 구현 | 외부 의존성 없이 StreamWriter 기반으로 직접 구현. 최소 패키지이지만 로테이션/레벨 직접 구현 필요 | |

**User's choice:** Serilog (추천)
**Notes:** 없음

---

### 기존 Debug.WriteLine 마이그레이션

| Option | Description | Selected |
|--------|-------------|----------|
| 전부 Serilog로 교체 | 기존 Debug.WriteLine 호출을 모두 Serilog 로그로 대체. 일관된 로그 출력 | ✓ |
| 양쪽 병행 유지 | Debug.WriteLine은 그대로 두고 Serilog를 추가로 호출. 디버그 시 양쪽 확인 가능 | |
| Claude 재량 | Claude가 상황에 맞게 판단 | |

**User's choice:** 전부 Serilog로 교체 (추천)
**Notes:** 없음

---

### 로그 레벨 구성

| Option | Description | Selected |
|--------|-------------|----------|
| 4레벨: Debug/Info/Warning/Error | GS인증 성공 기준에 명시된 4단계 | ✓ |
| 5레벨: +Fatal 추가 | 4레벨 + Fatal(치명적 오류). 비정상 종료 등 구분 가능 | |
| Claude 재량 | Claude가 상황에 맞게 판단 | |

**User's choice:** 4레벨: Debug/Info/Warning/Error (추천)
**Notes:** 없음

---

## 감사 로그 범위 + 개인정보 처리

### 감사 이벤트 범위

| Option | Description | Selected |
|--------|-------------|----------|
| 필수 4개만 | 앱 시작, 앱 종료, JSON 저장, 라이선스 오류. SECU-02 기준 최소 충족 | ✓ |
| 필수 + 영상 로드/내보내기 | 4개 + 영상 파일 로드, JSON 내보내기 성공/실패. 데이터 추적성 강화 | |
| 포괄적 기록 | 위 항목 + 라벨링 작업(바운딩박스 생성/삭제/수정), Waypoint 설정 등 전체 사용자 활동 | |

**User's choice:** 필수 4개만 (추천)
**Notes:** 없음

---

### MAC 주소 개인정보 처리

| Option | Description | Selected |
|--------|-------------|----------|
| SHA-256 해싱 | MAC 주소를 SHA-256으로 해싱하여 로그에 기록. 동일 MAC은 동일 해시로 추적 가능 | |
| 마스킹 (XX:XX:XX:XX:XX:XX) | MAC 주소 일부를 마스킹하여 기록. 간단하지만 추적 불가 | |
| MAC 주소 아예 미기록 | 감사 로그에 MAC 주소 자체를 기록하지 않음. 가장 안전하지만 사용자 식별 불가 | ✓ |

**User's choice:** MAC 주소 아예 미기록
**Notes:** 없음

---

## 로그 저장 위치 + 로테이션 정책

### 로그 파일 저장 위치

| Option | Description | Selected |
|--------|-------------|----------|
| 실행 폴더/logs/ | 앱 실행 경로 하위 logs/ 폴더. 휴대성 좋고 찾기 쉬움 | |
| %APPDATA%/AOLT/logs/ | Windows 표준 앱 데이터 경로. 프로그램 폴더와 분리 | |
| Claude 재량 | Claude가 상황에 맞게 판단 | ✓ |

**User's choice:** Claude 재량
**Notes:** 없음

---

### 로그 보관 기간

| Option | Description | Selected |
|--------|-------------|----------|
| 30일 보관 | 날짜별 파일 생성, 30일 초과 파일 자동 삭제. GS인증 감사 추적에 충분 | ✓ |
| 90일 보관 | 장기 보관. 디스크 사용량 증가 가능성 | |
| 무제한 (삭제 안 함) | 로그 자동 삭제 없음. 사용자가 직접 관리 | |
| Claude 재량 | Claude가 상황에 맞게 판단 | |

**User's choice:** 30일 보관 (추천)
**Notes:** 없음

---

## Claude's Discretion

- 로그 파일 저장 경로 (실행 폴더/logs/ 또는 %APPDATA% 중 적합한 쪽 선택)
- Serilog 설정 방식 (코드 기반 vs appsettings.json)
- 로그 출력 포맷 (타임스탬프 형식, 메시지 구조)
- 감사 로그 메시지 형식 및 접두어 패턴

## Deferred Ideas

None — discussion stayed within phase scope
