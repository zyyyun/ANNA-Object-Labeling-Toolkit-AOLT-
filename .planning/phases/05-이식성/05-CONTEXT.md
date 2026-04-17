# Phase 5: 이식성 - Context

**Gathered:** 2026-04-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 5는 Windows 10/11 환경에서 AOLT의 정상 설치·실행·제거를 보장하는 배포 패키지를 구축한다.

GS인증 1등급 이식성 평가(PORT-01~03) 통과를 위한 설치 자동화 + 의존성 해결 + 클린 언인스톨.

## Out of Scope
- 크로스 플랫폼 (Linux/macOS): WinForms는 Windows 전용
- Windows 7/8 지원: 명시된 평가 환경은 Win10/11
- 상용 코드 서명 인증서 구매
- 자동 업데이트 시스템 (v2 이후)

</domain>

<decisions>
## Implementation Decisions

### Installer Format
- **D-01:** Inno Setup(무료, 스크립트 기반) 사용 — `.iss` 스크립트로 자동화, 한국 업계 인증에서 널리 사용, 클린 언인스톨 지원
- **D-02:** 설치 경로 기본값: `C:\Program Files\ANNA\ASLT` (공식 위치)
- **D-03:** 설치 시 바로가기: 시작 메뉴 + 바탕화면(선택사항 체크박스)

### .NET Runtime Distribution
- **D-04:** **Self-contained** 배포 — `dotnet publish -r win-x64 --self-contained -c Release` 사용
- **D-05:** .NET 8 Runtime 미설치 대응 불필요 — 앱과 함께 런타임 번들됨 (파일 크기 ~70MB)
- **D-06:** 결과: 사용자가 Runtime을 별도 설치하지 않아도 실행 가능 (PORT-02 자동 충족)

### FFmpeg Distribution
- **D-07:** **FFmpeg 번들** — `ffmpeg.exe`를 설치 패키지에 포함, 설치 시 `<InstallDir>\ffmpeg\ffmpeg.exe`에 배치
- **D-08:** 기존 `VideoService.SetupFFmpegPath()` 로직 그대로 동작 — app 폴더의 `/ffmpeg/ffmpeg.exe` 우선 탐색 (이미 구현됨)
- **D-09:** Phase 4에서 구현된 "FFmpeg 미설치 안내 MessageBox"는 fallback으로 유지 (사용자가 설치 후 `ffmpeg.exe`를 삭제한 비정상 상황 대비)

### Code Signing
- **D-10:** **서명 없음** — GS인증 1등급은 코드 서명 요구 없음
- **D-11:** Windows SmartScreen 경고는 내부 인증 과정에서 수용
- **D-12:** 필요 시 향후(v2) 자체 서명 또는 상용 인증서 도입 검토 (현 범위 밖)

### Uninstaller (PORT-03)
- **D-13:** Inno Setup 기본 언인스톨러 사용 — 설치된 모든 파일/폴더 자동 제거
- **D-14:** 레지스트리 엔트리는 최소화 — Inno Setup이 자동 관리하는 `Uninstall\ASLT` 키 외에 커스텀 레지스트리 기록 없음
- **D-15:** 사용자 생성 데이터(JSON 라벨링 파일, 로그 파일) 처리: 기본적으로 **유지** (사용자가 별도 위치에 저장한 라벨링 작업물은 보존), 설치 디렉토리 내 로그 폴더만 제거

### Versioning
- **D-16:** 설치 패키지 버전: `1.0.0` (csproj `Version` 필드와 일치)
- **D-17:** 설치 파일 이름: `ASLT-Setup-v1.0.0.exe`

### Claude's Discretion
- Inno Setup 스크립트의 세부 페이지 구성 (환영/라이선스/설치경로/바로가기/완료)
- 설치 시 Visual C++ Redistributable 필요 여부 자동 감지 (OpenCvSharp native DLL에 필요할 수 있음 — 설치 시 확인)
- 언어 설정: 한국어 기본 + 영어 fallback
- 설치/제거 로그 경로 및 포맷

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Specs
- `.planning/PROJECT.md` — 기술 스택 (WinForms/.NET 8.0 x64), 제약사항
- `.planning/REQUIREMENTS.md` §이식성 — PORT-01, PORT-02, PORT-03 정의
- `.planning/ROADMAP.md` — Phase 5 Goal + Success Criteria

### Existing Code
- `ASLTv1.0.csproj` — 프로젝트 구성 (Version, Product 명칭, PackageReferences)
- `Services/VideoService.cs` §SetupFFmpegPath (line 520-555) — 기존 FFmpeg 탐색 로직
- `Services/VideoService.cs` §IsFFmpegAvailable (Phase 4 추가) — FFmpeg 가용성 플래그

### External Docs (downstream agent가 research 시 참고)
- Inno Setup 공식: https://jrsoftware.org/isinfo.php (ISPP, Pascal scripting)
- `dotnet publish` self-contained: https://learn.microsoft.com/dotnet/core/deploying/
- FFmpeg Windows 공식 빌드: https://www.gyan.dev/ffmpeg/builds/ (essentials/full)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `VideoService.SetupFFmpegPath()`: 앱 폴더 `/ffmpeg/ffmpeg.exe` 우선 탐색 로직 이미 구현 — 번들 FFmpeg에 그대로 활용 가능
- `VideoService.IsFFmpegAvailable` 플래그: Phase 4에서 추가된 가용성 체크 — 설치 완료 후 FFmpeg 제거 시 안내 MessageBox 트리거
- `ASLTv1.0.csproj` Version 1.0.0 / Product 명: 설치 패키지 메타데이터와 일치

### Native Dependencies (설치 패키지 포함 필요)
- OpenCvSharp4 native DLL: `OpenCvSharpExtern.dll`, `opencv_videoio_ffmpeg4110_64.dll`
  - `OpenCvSharp4.runtime.win` NuGet이 자동 복사 → `bin/Release/net8.0-windows/win-x64/publish/`에 포함됨
- System.Drawing.Common runtime: .NET 8 self-contained 배포에 자동 포함
- FFmpeg: **별도 번들 필요** (NuGet으로 자동 포함 안 됨)

### Build/Publish Commands
- 현재 빌드: `dotnet build -c Debug` (Visual Studio default)
- 배포 빌드(신규): `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false`
- 출력 경로: `bin/Release/net8.0-windows/win-x64/publish/`

### Integration Points
- 설치 스크립트(`installer/ASLT-Setup.iss`)에서 `publish/` 폴더의 모든 파일 + `ffmpeg/ffmpeg.exe`를 패키징
- 설치 후 실행 파일: `<InstallDir>\ASLTv1.exe`
- 언인스톨: Inno Setup 자동 생성 `unins000.exe`가 설치된 모든 파일 제거

</code_context>

<specifics>
## Specific Ideas

### 설치 구조
```
<InstallDir>/
├── ASLTv1.exe                          ← self-contained 실행 파일
├── *.dll                                ← .NET Runtime + NuGet 런타임
├── OpenCvSharpExtern.dll               ← OpenCV native
├── opencv_videoio_ffmpeg4110_64.dll    ← OpenCV FFmpeg bridge
├── ffmpeg/
│   └── ffmpeg.exe                       ← 번들 FFmpeg (SRT 추출용)
├── logs/                                ← Serilog 파일 로그 (런타임 생성)
└── unins000.exe                         ← Inno Setup 언인스톨러 (자동 생성)
```

### 설치 파일 명명
- `ASLT-Setup-v1.0.0.exe` (버전 명시)
- 파일 크기 예상: ~80-100MB (self-contained + FFmpeg)

### 언인스톨 시 정리 대상
- 설치 디렉토리 전체 (`<InstallDir>/` 및 하위)
- 시작 메뉴 바로가기
- 바탕화면 바로가기(설치 시 체크한 경우)
- 레지스트리: `HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\ASLT` (Inno Setup 자동 관리)

### 언인스톨 시 유지 대상
- 사용자가 외부에 저장한 COCO JSON 라벨링 파일 (건드리지 않음)
- 사용자 비디오 파일 원본

</specifics>

<deferred>
## Deferred Ideas

### 향후 마일스톤 고려
- **자동 업데이트 시스템** — v2 이상에서 Squirrel.Windows 또는 커스텀 업데이터 도입 검토
- **상용 코드 서명 인증서** — SmartScreen 경고 제거가 필요한 배포 시 DigiCert 등 CA 서명 도입
- **MSIX 패키징** — Microsoft Store 배포가 필요한 경우
- **Portable 버전** — 설치 없이 zip 해제만으로 실행하는 휴대용 버전 (Out of Scope for v1)

</deferred>

---

*Phase: 05-이식성*
*Context gathered: 2026-04-17*
