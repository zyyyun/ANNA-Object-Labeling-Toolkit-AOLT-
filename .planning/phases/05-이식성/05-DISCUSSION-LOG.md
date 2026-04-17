# Phase 5: 이식성 - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-04-17
**Phase:** 05-이식성
**Areas discussed:** Installer, .NET Runtime, FFmpeg, Code signing

---

## Installer Format

| Option | Description | Selected |
|--------|-------------|----------|
| Inno Setup | 무료, 스크립트 기반, 한국 업계 인증에서 널리 사용, 제거 완전함 | ✓ |
| WiX Toolset (MSI) | Microsoft 표준 MSI, 엔터프라이즈 배포에 유리하나 설정 복잡 | |
| Self-contained 단일 exe + zip | .NET Runtime exe 번들, 압축 해제 후 실행. 설치/제거 개념 없음 | |

**User's choice:** Inno Setup
**Notes:** 한국 업계 인증에서 널리 사용되고, 스크립트 자동화와 클린 언인스톨을 모두 지원

---

## .NET Runtime Distribution

| Option | Description | Selected |
|--------|-------------|----------|
| Self-contained 가젯정 | `dotnet publish --self-contained`, Runtime 번들, 파일 크기 +70MB | ✓ |
| Framework-dependent + 화면 안내 | 작은 배포 크기, 미설치 시 MessageBox로 다운로드 URL 안내 | |
| 인스톨러가 Runtime 자동 설치 | 설치 중 .NET 감지 후 없으면 MS 공식 프로그램 실행 | |

**User's choice:** Self-contained 가젯정
**Notes:** Runtime 미설치 문제 원천 해결. PORT-02 자동 충족

---

## FFmpeg Distribution

| Option | Description | Selected |
|--------|-------------|----------|
| 번들 제공 | 인스톨러에 ffmpeg.exe 포함, <InstallDir>/ffmpeg/에 배치 | ✓ |
| 선택적 실행 | FFmpeg 없이도 앱 동작 (자막 추출만 비활성). Phase 4에서 이미 감지+안내 완료 | |
| 다운로드 링크 안내 | FFmpeg 공식 배포 링크를 MessageBox에 표시 | |

**User's choice:** 번들 제공
**Notes:** 사용자가 별도 설치할 필요 없음. 기존 `VideoService.SetupFFmpegPath()` 로직 그대로 동작

---

## Code Signing

| Option | Description | Selected |
|--------|-------------|----------|
| 서명 없음 | GS인증 1등급은 서명 요구 없음. SmartScreen 경고는 수용 | ✓ |
| 자체 서명 (테스트용) | signtool로 자체 서명, 신뢰는 없지만 서명 존재 자체는 표시 | |
| 상용 인증서 구입 안내 | DigiCert 등 CA 서명 권장, 범위 밖, 할인점 없을 수 있음 | |

**User's choice:** 서명 없음
**Notes:** 내부 인증 과정에서 SmartScreen 경고 수용

---

## Claude's Discretion

- Inno Setup 스크립트의 페이지 구성 (환영/라이선스/경로/바로가기/완료)
- Visual C++ Redistributable 감지 및 안내 (OpenCvSharp native DLL 요구 가능성)
- 설치 로그 포맷 및 저장 위치
- 설치 언어 설정 (한국어 기본)

## Deferred Ideas

- 자동 업데이트 시스템 (v2)
- 상용 코드 서명 인증서 (필요 시 별도 마일스톤)
- MSIX 패키징 (Microsoft Store 배포 시)
- Portable zip 버전 (설치 없이 실행)
