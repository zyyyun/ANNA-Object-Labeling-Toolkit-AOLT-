# Domain Pitfalls: GS인증 준비 (C# WinForms 데스크톱 앱)

**Domain:** Korean GS (Good Software) certification for desktop labeling tool
**Researched:** 2026-04-14
**Overall Confidence:** MEDIUM — GS certification is a Korean national standard with limited English
documentation. Core requirements drawn from ISO/IEC 25023, KISA official guides, and
practitioner accounts. Specific thresholds (PBKDF2 iteration counts, exact defect scoring) are
not publicly documented in detail; flags for validation are noted.

---

## Critical Pitfalls

Mistakes that cause re-submission cycles or outright failure at the evaluation stage.

---

### Pitfall C-1: Manual–Feature Mismatch (가장 빈번한 탈락 원인)

**What goes wrong:**
The evaluator opens the 사용자 취급 설명서 and performs every described action step-by-step.
If the application behaves differently from what the manual describes — even a sub-menu name,
a keyboard shortcut, or an error message wording — the evaluator records a 기능적합성 결함.
Multiple such findings cascade into a Critical/High defect cluster that blocks certification.

**Why it happens:**
Developers write the manual at the end, from memory, without running through the actual build.
UI labels get renamed, shortcuts change, and error dialogs evolve, but the manual is never
updated. A 2,500-line monolithic form (as in AOLT's MainForm.cs) makes it especially easy for
undocumented behaviour to accumulate.

**Consequences:**
Every mismatch found = a defect entry. A single Critical or High defect requires a full
regression re-test cycle (adds weeks). The certification industry reports average 4.5 re-test
rounds; manual defects are the leading cause.

**Warning signs:**
- Manual written before code is frozen
- Screenshots in manual taken at a different resolution or theme than the actual build
- Feature list in the manual contains items not present in the submitted binary
- Keyboard shortcut table exists in manual but no cross-check against actual key-handling code

**Prevention:**
1. Lock the binary BEFORE writing the manual (or update the manual last, against the locked build).
2. Walk through every manual section with the exact build that will be submitted.
3. For AOLT specifically: verify vehicle dropdown, undo/redo key table, JSON export/import
   workflow, and SRT subtitle behaviour match manual descriptions exactly.
4. Include only features that are confirmed-working; omit or clearly mark beta features.

**Phase to address:** Manual-writing phase (final quality milestone, before submission).

---

### Pitfall C-2: No Encryption on License/Credential Storage (보안성 결함 — 기밀성)

**What goes wrong:**
The GS evaluator checks ISO/IEC 25023 보안성 → 기밀성 (Confidentiality). If the application
stores any authentication credential, license key, or user-identifying value in plaintext
(registry, config file, flat file), this is a 기밀성 결함. KISA guidelines require one-way
hashing with SHA-256 or stronger plus a random Salt for any stored secret.

**Current AOLT state:**
- MAC address stored and compared directly (confirmed in codebase; encryption absent).
- No KISA-compliant hashing applied to any stored value.

**KISA requirements (MEDIUM confidence — from KISA 암호이용활성화 guides):**
- Algorithm: SHA-2 family, minimum SHA-256. SHA-1 is explicitly deprecated.
- Salt: random, per-value, minimum 16 bytes (recommended 32 bytes = same length as SHA-256 output).
- Key stretching: PBKDF2-HMAC-SHA256 is KISA's referenced KDF. OWASP 2023 recommends
  ≥ 600,000 iterations; KISA's own older guides reference 1,000 minimum, but current industry
  practice for GS evaluation typically aligns with NIST/OWASP current recommendations
  (≥ 310,000 for SHA-256). **Flag for validation with the certification body before implementing.**
- C# implementation: `Rfc2898DeriveBytes.Pbkdf2()` in .NET 8 (`System.Security.Cryptography`)
  natively supports SHA256 hash algorithm parameter — no third-party library needed.

**Consequences:**
Plaintext secret storage is a High or Critical security defect. Cannot pass 보안성 evaluation
without remediation.

**Warning signs:**
- Any value compared with `==` against a stored string (MAC, license key, password)
- `File.ReadAllText` / `Registry.GetValue` used to retrieve an auth credential without decryption
- `Debug.WriteLine` printing a credential-adjacent value

**Prevention:**
```csharp
// .NET 8 — KISA-compliant one-way hash with salt
using System.Security.Cryptography;

public static (string Hash, string Salt) HashSecret(string secret)
{
    byte[] saltBytes = RandomNumberGenerator.GetBytes(32); // 256-bit salt
    byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
        secret,
        saltBytes,
        iterations: 310_000,          // validate count with certifier
        HashAlgorithmName.SHA256,
        outputLength: 32);
    return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
}

public static bool VerifySecret(string candidate, string storedHash, string storedSalt)
{
    byte[] saltBytes = Convert.FromBase64String(storedSalt);
    byte[] candidateHash = Rfc2898DeriveBytes.Pbkdf2(
        candidate, saltBytes, 310_000, HashAlgorithmName.SHA256, 32);
    return CryptographicOperations.FixedTimeEquals(
        candidateHash, Convert.FromBase64String(storedHash));
}
```

**Phase to address:** Security hardening phase (early, before feature freeze).

---

### Pitfall C-3: Unhandled Exceptions Causing Visible Crashes (신뢰성 결함)

**What goes wrong:**
The evaluator triggers an unhandled exception (NullReferenceException, IndexOutOfRangeException,
InvalidOperationException from cross-thread UI access) and sees an unformatted .NET crash dialog
or a silent freeze. This is a High 신뢰성 결함 under 결함허용성 (Fault Tolerance). Multiple
crash paths compound into a Critical finding.

**Current AOLT state (from CONCERNS.md):**
- `catch (Exception ex)` swallows specific errors in 7+ locations in MainForm.cs alone.
- Race condition in async video loading (no CancellationToken).
- Form state accessed from async callbacks without null/disposed checks.
- FFmpeg process leaks; subtitle parse failures silently ignored.
- `MAX_UNDO_STACK` constant defined but never enforced — unbounded memory growth.

**Consequences:**
Each visible crash = automatic High/Critical defect. Evaluators specifically stress-test:
rapid file switching, undo/redo at stack boundaries, saving with no project open,
and drag operations that exit the video frame bounds.

**Warning signs:**
- `catch (Exception ex)` with only `Debug.WriteLine` in body
- `async void` event handlers without top-level try/catch
- `Control.Invoke`/`BeginInvoke` calls without `IsDisposed` check
- No global `Application.ThreadException` handler

**Prevention:**
1. Wire `Application.ThreadException` and `AppDomain.UnhandledException` at startup to show a
   friendly Korean error dialog and write to the log file — never let the .NET crash dialog appear.
2. Replace all `catch (Exception ex)` with specific types; only use base `Exception` at true
   top-level boundaries.
3. Add `CancellationToken` to `LoadVideoAsync` and cancel previous token before starting new load.
4. Enforce `MAX_UNDO_STACK = 100` in the undo/redo add path.
5. Guard every `Control.Invoke` with `if (!IsDisposed && IsHandleCreated)`.

**Phase to address:** Bug-fix / reliability phase (before any testing against the manual).

---

### Pitfall C-4: Features Present in Binary but Absent from Manual (기능완전성)

**What goes wrong:**
Evaluators document every observable feature. If the binary includes a feature (e.g., Waypoint
management, SRT subtitle extraction, dark theme) that is not described in the 제품 설명서 or
사용자 취급 설명서, the evaluator may record a 기능완전성 결함: the manual is incomplete.
This is the mirror of C-1.

**Why it happens:**
Teams submit the manual for a subset of features, planning to "add the rest later." Evaluators
are not limited to the manual's table of contents — they probe the UI.

**Warning signs:**
- Menu items, toolbar buttons, or right-click options not listed in the manual
- Keyboard shortcuts that work but are not in the shortcut reference table
- Settings/preferences dialogs not documented

**Prevention:**
Do a reverse walkthrough: open the running application and enumerate every menu item, dialog,
keyboard shortcut, and UI state, then verify each is in the manual. For AOLT: Waypoint panel,
SRT subtitle toggle, playback speed control, and export dialog must all be documented.

**Phase to address:** Manual-writing phase (same as C-1).

---

## Moderate Pitfalls

---

### Pitfall M-1: No Audit Trail (보안성 결함 — 책임성/추적성)

**What goes wrong:**
ISO/IEC 25023 보안성 includes 책임성 (Accountability) and 추적성 (Traceability): the degree
to which user actions and data access can be attributed and traced. An application with no
logging of who did what, when, provides no audit trail. For a tool used in research contexts
(IFEZ), evaluators may flag absence of an activity log as a 책임성 결함.

**Current AOLT state:** No structured logging system; `Debug.WriteLine` only (invisible in
release builds).

**Prevention:**
Implement a file-based structured log. Minimum entries:
- Application start/stop (timestamp, version, user account name)
- File opened/closed (path, frame count)
- Export triggered (output path, annotation count)
- License validation result (pass/fail, no credential value in log)

A simple `TextWriter`-based log rotating by date is sufficient. Serilog is acceptable for .NET 8
but adds a dependency — keep it lightweight for a certifiable build.

**Warning signs:**
- No log file created in `%AppData%` or application directory after a session
- No way to prove who ran the application or what files were processed

**Phase to address:** Logging infrastructure phase (early; other features depend on it).

---

### Pitfall M-2: Bounding Box Out-of-Bounds Export (기능정확성 / 호환성)

**What goes wrong:**
COCO JSON exported with bounding box coordinates outside the image dimensions (x+w > image_width,
y+h > image_height) or with negative values is technically invalid per the COCO schema. An
evaluator checking data exchange correctness (호환성 → 데이터 교환 정확성) will catch this
with a simple validation pass.

**Current AOLT state (from CONCERNS.md):**
- BoundingBox created from mouse coordinates without clamping to video frame bounds.
- Timestamp written as `DateTime.Now` instead of actual frame timestamp.

**Prevention:**
- Clamp box coordinates to `[0, imageWidth-1]` × `[0, imageHeight-1]` at creation and export.
- Export `timestamp` field as frame index × (1 / fps) rounded to milliseconds, not wall-clock time.

**Warning signs:**
- Any bounding box where `x + width > videoWidth` in the exported JSON
- `DateTime.Now.ToString()` appearing in JSON export code

**Phase to address:** Bug-fix phase (JSON correctness milestone).

---

### Pitfall M-3: Installer Fails on Clean Evaluation Environment (이식성 결함)

**What goes wrong:**
GS evaluators test on a clean Windows environment, not the developer's machine. A common failure
mode: the application silently fails because a prerequisite is missing (.NET 8 Desktop Runtime,
Visual C++ Redistributable for OpenCvSharp4, FFmpeg binaries). The installer does not check for
or install these prerequisites.

**Current AOLT state (from CONCERNS.md):**
- OpenCvSharp4 v4.11.0 requires Visual C++ Redistributable; not bundled.
- FFmpeg is optional but subtitle extraction silently fails without it.
- .NET 8 Desktop Runtime must be present; not validated at startup.

**Consequences:**
Application crashes on launch or feature silently fails = 이식성 결함. Evaluator cannot
replicate the developer's environment.

**Prevention:**
1. Use a self-contained .NET 8 publish (`dotnet publish -r win-x64 --self-contained true`) or
   include a prerequisite check at startup.
2. Bundle or prerequisite-check: Visual C++ Redistributable, .NET 8 Desktop Runtime.
3. For FFmpeg: either bundle a minimal FFmpeg build or disable subtitle features gracefully with
   a clear Korean message ("FFmpeg을 찾을 수 없어 자막 기능을 사용할 수 없습니다").
4. Test the installer on a freshly provisioned Windows 10/11 VM with zero developer tools before
   submission.

**Warning signs:**
- Installer script does not include prerequisite bootstrapper chains
- FFmpeg path hardcoded to developer's machine path

**Phase to address:** Installer/packaging phase (final milestone before submission).

---

### Pitfall M-4: Generic Error Messages (사용성 — 학습성/운영성)

**What goes wrong:**
GS 사용성 평가 specifically checks that error messages are "사용자 이해 가능한 내용으로 작성"
(written in user-understandable language, not technical codes). Showing a raw exception type
or stack trace fails 학습성 and 운영성 criteria.

**Current AOLT state:**
- `MessageBox.Show(ex.Message)` calls throughout surface raw .NET exception messages in Korean
  or English technical language.
- Some errors silently swallowed with only `Debug.WriteLine`.

**Prevention:**
- Define a user-facing error message map: `FileNotFoundException` → "파일을 찾을 수 없습니다. 경로를 확인해 주세요."
- Never expose stack traces or CLR type names in message boxes.
- For each catch block that currently shows `ex.Message`, replace with a meaningful Korean sentence.

**Warning signs:**
- `MessageBox.Show(ex.Message)` or `MessageBox.Show(ex.ToString())` in code
- English technical strings ("Object reference not set to an instance of an object") reachable by user

**Phase to address:** Bug-fix / UI polish phase.

---

### Pitfall M-5: Undo/Redo State Invisible to User (사용성 — 운영성)

**What goes wrong:**
GS 운영성 평가 requires "실행 취소 기능 제공" AND that the user can see whether undo/redo is
available. If Ctrl+Z appears to do nothing with no feedback, evaluators record a 운영성 결함
("운영 상태 정보를 사용자에게 제공하지 않음").

**Current AOLT state (from CONCERNS.md):**
- Undo/redo stacks exist but no visual feedback (buttons not greyed out, no status bar count).
- `MAX_UNDO_STACK = 100` constant exists but is never enforced, so stack depth is infinite.

**Prevention:**
- Add Undo/Redo toolbar buttons; disable (grey out) when respective stack is empty.
- Show "실행 취소 가능: N개 작업" in the status bar, or at minimum reflect availability in button state.

**Phase to address:** UI polish / usability phase.

---

## Minor Pitfalls

---

### Pitfall L-1: Submitting a Debug Build

**What goes wrong:**
Debug builds include extra assertions, slower execution, and non-production behaviour. Performance
측정 (응답 시간, CPU 사용) during evaluation may fail against benchmarks that a Release build
would pass. `Debug.WriteLine` messages appear in the output, which can leak internal state.

**Prevention:**
Submit only `Release` configuration builds. Verify with `#if DEBUG` that no debug-only code paths
affect observable behaviour.

**Phase to address:** Final submission packaging.

---

### Pitfall L-2: Nullable Reference Types Disabled

**What goes wrong:**
`<Nullable>disable</Nullable>` means the compiler does not warn about null dereferences. The
evaluator does not inspect source code directly, but the runtime consequences — NullReferenceExceptions
during testing — are caught as 신뢰성 결함.

**Prevention:**
Enable nullable reference types (`<Nullable>enable</Nullable>`) and resolve all warnings. This
is the fastest way to discover latent NullReferenceException paths before the evaluator does.

**Phase to address:** Code quality / reliability phase.

---

### Pitfall L-3: Hardcoded Developer Paths / Credentials in Binary

**What goes wrong:**
Developer machine paths (e.g., `C:\Users\developer\ffmpeg\ffmpeg.exe`) or test credentials
compiled into the binary are visible with a simple string search, which evaluators sometimes
perform for 보안성 screening.

**Prevention:**
- All file paths resolved relative to `AppContext.BaseDirectory` or user-selectable via dialog.
- No credentials, API keys, or MAC addresses in compiled source.

**Phase to address:** Code review before submission packaging.

---

### Pitfall L-4: Backup Files Left in Export Directory

**What goes wrong:**
`Services/JsonService.cs` creates `.json.backup` files as plaintext alongside the main annotation
JSON. Evaluators reviewing the output directory see unencrypted copies of annotation data. Under
GS 보안성 → 기밀성 evaluation, this can be noted as a minor finding.

**Prevention:**
Either delete backup files after a successful main-file write, or encrypt backup content using
AES-256 (symmetric encryption, key derived from application-level secret). Deleting is simpler
and sufficient for the annotation data context.

**Phase to address:** Bug-fix / data safety phase.

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|----------------|------------|
| Security hardening | No KISA-compliant hashing (C-2) | Implement PBKDF2-SHA256 + Salt before feature freeze |
| Bug fixing | Unhandled NullReference crashes (C-3) | Enable Nullable, wire global exception handler early |
| JSON / data | Coordinates out of bounds, wrong timestamp (M-2) | Add export validator that asserts COCO schema constraints |
| Logging | No audit trail (M-1) | Build file-based log at project start; other phases depend on it |
| Installer / packaging | Missing VC++ / .NET / FFmpeg prerequisites (M-3) | Test on clean VM before every candidate submission |
| Manual writing | Feature–manual mismatch (C-1, C-4) | Write manual last, against locked binary; do full walkthrough |
| UI polish | Invisible undo/redo state (M-5), raw error messages (M-4) | Fix before manual walkthrough; evaluator tests these explicitly |
| Final build | Debug build submitted (L-1) | CI or manual checklist: confirm Release configuration |

---

## KISA Security Requirements Quick Reference

| Requirement | Specification | Confidence | Source |
|-------------|---------------|------------|--------|
| Hash algorithm for stored secrets | SHA-256 minimum (SHA-2 family); SHA-1 prohibited | HIGH | KISA 암호이용활성화 |
| Salt | Random, per-value; minimum 16 bytes, recommended 32 bytes | MEDIUM | KISA / OWASP |
| Key derivation function | PBKDF2-HMAC-SHA256 (KISA referenced KDF) | MEDIUM | KISA |
| PBKDF2 iteration count | KISA legacy: 1,000 minimum; OWASP 2023: 600,000 for SHA-256; validate with certifier | LOW | KISA / OWASP |
| .NET 8 implementation class | `System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2()` | HIGH | Microsoft Learn |
| Comparison timing | Use `CryptographicOperations.FixedTimeEquals()` to prevent timing attacks | HIGH | .NET 8 docs |
| Symmetric encryption (if needed) | AES-256 (AES/CBC or AES-GCM) | HIGH | KISA |
| Audit log | File-based, records user actions; no credential values in log | MEDIUM | ISO/IEC 25023 추적성 |

---

## Sources

- ISO/IEC 25023 평가 기준: [CSLEE Tech Blog — ISO/IEC 25023 사례](https://blog.cslee.co.kr/understanding-the-international-standard-iso-iec-25023-with-case-bigzami/)
- GS인증 평가 항목 (TTA): [TTA 고객서비스포털 GS시험인증 1등급](https://cs.tta.or.kr/tta/introduce/introCont.do?menuId=700&tnc_lab=T000003&up_tnc_cls_no=T000020&tnc_cls_no=T000127&tabMode=cont)
- GS인증 사용성 평가 기준 상세: [GS인증 사용성 평가 블로그](https://quality.sansamlife.com/entry/%EC%9D%B8%EC%A6%9D-GS%EC%9D%B8%EC%A6%9D%EC%9D%98-%EB%AA%A8%EB%93%A0-%EA%B2%83-9-GS%EC%9D%B8%EC%A6%9D-%EC%B7%A8%EB%93%9D%EC%9D%84-%EC%9C%84%ED%95%B4-%EC%95%8C%EC%95%84%EC%95%BC-%ED%95%98%EB%8A%94-%EA%B2%83-GS%EC%9D%B8%EC%A6%9D-%EA%B8%B0%EC%A4%80-%EC%95%8C%EC%95%84%EB%B3%B4%EA%B8%B0-%EC%82%AC%EC%9A%A9%EC%84%B1)
- GS인증 획득률 하락 및 실패 원인: [전자신문 — GS인증 준비 소홀](https://www.etnews.com/200801160194)
- GS인증 보안 SW 명암: [보안뉴스 — GS인증 보안 SW](https://m.boannews.com/html/detail.html?idx=88589)
- KISA 암호이용활성화 안내서 목록: [KISA seed.kisa.or.kr](https://seed.kisa.or.kr/kisa/reference/EgovGuide.do)
- KISA 암호 알고리즘 및 키 길이 안내서: [KISA kisa.or.kr](https://www.kisa.or.kr/2060305)
- .NET 8 Rfc2898DeriveBytes.Pbkdf2: [Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes.pbkdf2?view=net-8.0)
- PBKDF2 비밀번호 단방향 암호화 실무: [Medium — 박상수](https://pakss328.medium.com/%EB%B9%84%EB%B0%80%EB%B2%88%ED%98%B8-%EB%8B%A8%EB%B0%A9%ED%96%A5-%EC%95%94%ED%98%B8%ED%99%94%EC%97%90-%EB%8C%80%ED%95%98%EC%97%AC-f2739a1485e)
- GS인증 제도 소개 (공개 PDF): [pstatic GS인증제도소개](https://files-scs.pstatic.net/2024/11/27/zg7XZrz692/GS%EC%9D%B8%EC%A6%9D%EC%A0%9C%EB%8F%84%EC%86%8C%EA%B0%9C.pdf)
