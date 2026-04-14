# Technology Stack — GS Certification Quality Additions

**Project:** AOLT (ANNA Object Labeling Tool)
**Researched:** 2026-04-14
**Scope:** Libraries to add to existing .NET 8.0 WinForms app for GS 1등급 certification

---

## Existing Stack (Do Not Replace)

| Package | Version | Role |
|---------|---------|------|
| OpenCvSharp4 | 4.11.0.20250507 | Video frame capture |
| FFMpegCore | 5.1.0 | SRT subtitle extraction |
| Newtonsoft.Json | 13.0.3 | COCO format serialization |
| System.Drawing.Common | 8.0.11 | Rendering / bounding boxes |

These packages are not touched by the GS quality improvement milestone. All additions below are additive.

---

## Additions Required for GS Certification

### 1. Structured Logging (유지보수성 + 보안성 감사추적)

**Use: Serilog**

| Package | Version | Purpose |
|---------|---------|---------|
| Serilog | 4.3.1 | Core logging engine |
| Serilog.Sinks.File | 7.0.0 | Rolling file output (daily rotation) |
| Serilog.Enrichers.Thread | 4.0.0 | ThreadId per log entry for audit trails |
| Serilog.Enrichers.Process | 3.0.0 | ProcessId — required for traceability |

**Why Serilog over NLog:**
NLog is faster in high-throughput throttling scenarios, but this is a desktop labeling tool that generates low log volume. Serilog's structured event model (key-value properties, not formatted strings) is the correct choice because GS 보안성 traceability requires queryable, machine-readable log entries — not plain text lines. Serilog's enricher model makes adding `UserId`, `SessionId`, `MacAddress` (used in the existing license system) trivially composable without touching log call sites. NLog can do this but requires more XML/JSON configuration ceremony.

**Do NOT use:** `Debug.WriteLine` (current state — loses all output in Release builds, zero persistence), `Console.WriteLine` (no-op in WinExe), `Trace.WriteLine` (not structured).

**Configuration pattern for WinForms (no Host/ASP.NET needed):**
```csharp
// Program.cs — before Application.Run()
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.WithThreadId()
    .Enrich.WithProcessId()
    .WriteTo.File(
        path: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "aolt-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [T{ThreadId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
```

**Confidence:** HIGH — Serilog 4.3.1 and Sinks.File 7.0.0 confirmed on NuGet (April 2025 release). ILogger integration confirmed in official Microsoft .NET 8 docs.

---

### 2. Security — SHA-256 + Salt Encryption (보안성 / KISA 준수)

**Use: System.Security.Cryptography (BCL, no NuGet package required)**

| API | Source | Purpose |
|-----|--------|---------|
| `SHA256.HashData()` | .NET 8 BCL | One-way hash (KISA SHA-256 requirement) |
| `RandomNumberGenerator.GetBytes()` | .NET 8 BCL | Cryptographically secure salt generation |
| `Rfc2898DeriveBytes.Pbkdf2()` | .NET 8 BCL | PBKDF2-HMACSHA256 with iteration count |

**Why no external library:**
`System.Security.Cryptography` in .NET 8 ships FIPS-compliant implementations of SHA-256, PBKDF2, and HMACSHA256 as part of the runtime. KISA's 암호기술 구현 안내서 requires SHA-256 or stronger for 단방향 암호화 with salting — this is exactly what `Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, outputLength)` delivers. Adding a third-party crypto library (BCrypt.Net, libsodium) would introduce an unnecessary dependency, potential version drift, and supply-chain risk with no functional gain for this use case.

**Do NOT use:** SHA-1 (KISA explicitly deprecated — 취약점 발견), MD5 (same), `SHA256CryptoServiceProvider` directly without salt (rainbow table vulnerable), plain `SHA256.HashData()` without salt (KISA non-compliant for password/license key hashing).

**Recommended implementation for MAC-based license key hashing:**
```csharp
// SecurityHelper.cs
public static string HashWithSalt(string input, out string saltBase64)
{
    byte[] salt = RandomNumberGenerator.GetBytes(16); // 128-bit salt
    saltBase64 = Convert.ToBase64String(salt);
    byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
        input,
        salt,
        iterations: 310_000,          // OWASP 2023 recommendation for PBKDF2-SHA256
        hashAlgorithm: HashAlgorithmName.SHA256,
        outputLength: 32);
    return Convert.ToBase64String(hash);
}
```

**Confidence:** HIGH — `Rfc2898DeriveBytes.Pbkdf2` static method confirmed in official .NET 8 Microsoft docs. KISA SHA-256 + Salt requirement confirmed via KISA seed.kisa.or.kr and Korean cryptographic standards documentation.

---

### 3. Performance Monitoring / Profiling (성능효율성)

**Two tools, different purposes:**

#### 3a. BenchmarkDotNet — Development-time benchmarking

| Package | Version | Purpose |
|---------|---------|---------|
| BenchmarkDotNet | 0.15.8 | Micro-benchmark frame load, bounding box lookup O(n) vs indexed |

**Why:** The PROJECT.md explicitly requires optimizing bounding box lookup from O(n) to indexed structure and improving frame caching. BenchmarkDotNet provides statistically reliable measurements with memory allocation tracking (`[MemoryDiagnoser]`) that will generate concrete evidence for GS 성능효율성 평가. This is a **dev-time tool** — not shipped in the final release binary.

**Usage scope:** Added only to a separate benchmark console project or conditional `#if DEBUG` compile block referencing the main project. Never deployed to end users.

**Confidence:** HIGH — BenchmarkDotNet 0.15.8 confirmed on NuGet (November 2025). Official Microsoft Visual Studio profiling integration documented.

#### 3b. System.Diagnostics.PerformanceCounter — Runtime CPU/Memory sampling

| Package | Version | Purpose |
|---------|---------|---------|
| System.Diagnostics.PerformanceCounter | 8.0.0 | Read Windows perf counters (CPU %, working set) |

**Why:** GS 성능효율성 평가 requires demonstrating CPU and memory usage within acceptable bounds during labeling operations. `PerformanceCounter` is the standard Windows API for reading per-process CPU% and private bytes. It is already included in the `net8.0-windows` target — no NuGet install needed unless explicit package pinning is desired. Log sampled values via Serilog at INFO level every 60 seconds to produce a performance audit trail.

**Do NOT use:** External APM agents (Datadog, New Relic) — they are server-side SaaS tools with no fit for an offline Windows desktop certification context.

**Confidence:** HIGH — `System.Diagnostics.PerformanceCounter` is part of the .NET 8 Windows SDK. Confirmed available at NuGet v8.0.0 for explicit referencing.

---

### 4. Error Handling Infrastructure (신뢰성 + 유지보수성)

**Use: No additional NuGet package — refactor pattern only**

The PROJECT.md problem list shows "Generic exception catch 다수." The correct fix is structured exception handling using existing .NET 8 BCL capabilities, not a third-party library.

**Pattern to enforce:**

```csharp
// Replace: catch (Exception ex) { /* swallow */ }

// With:
catch (InvalidOperationException ex)
{
    Log.Error(ex, "Bounding box state invalid during {Operation}", operationName);
    ShowUserError("작업을 수행할 수 없습니다. 이전 상태로 되돌립니다.");
    _undoStack.Undo();
}
catch (IOException ex)
{
    Log.Error(ex, "File I/O failure reading {FilePath}", filePath);
    ShowUserError($"파일을 읽을 수 없습니다: {Path.GetFileName(filePath)}");
}
catch (Exception ex) when (Log.Error(ex, "Unhandled exception in {Operation}", operationName) == false)
{
    // Log.Error always returns false — this when() clause logs and re-throws
    throw;
}
```

**Why no Polly:** Polly is a retry/circuit-breaker library designed for transient network faults (HTTP calls, database retries). AOLT is an offline WinForms app — its failures are file I/O errors, null references, and state corruption, none of which benefit from retry logic. Adding Polly would be cargo-culting.

**Application-level unhandled exception handler (Program.cs):**
```csharp
Application.ThreadException += (s, e) =>
{
    Log.Fatal(e.Exception, "Unhandled UI thread exception");
    MessageBox.Show("예기치 못한 오류가 발생했습니다. 로그를 확인하세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
};
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    Log.Fatal(e.ExceptionObject as Exception, "Unhandled AppDomain exception, terminating={IsTerminating}", e.IsTerminating);
    Log.CloseAndFlush();
};
```

**Confidence:** HIGH — these are .NET 8 BCL event handlers, documented in official Microsoft docs.

---

## Installation

Add to `ASLTv1.0.csproj`:

```xml
<!-- Logging -->
<PackageReference Include="Serilog" Version="4.3.1" />
<PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
<PackageReference Include="Serilog.Enrichers.Thread" Version="4.0.0" />
<PackageReference Include="Serilog.Enrichers.Process" Version="3.0.0" />

<!-- Performance (dev only — exclude from Release publish if desired) -->
<!-- BenchmarkDotNet goes in a SEPARATE benchmark project, not this csproj -->

<!-- System.Diagnostics.PerformanceCounter is included in net8.0-windows target -->
<!-- System.Security.Cryptography is part of BCL — no reference needed -->
```

CLI install:
```bash
dotnet add package Serilog --version 4.3.1
dotnet add package Serilog.Sinks.File --version 7.0.0
dotnet add package Serilog.Enrichers.Thread --version 4.0.0
dotnet add package Serilog.Enrichers.Process --version 3.0.0
```

Total new runtime dependencies added to the shipped binary: **4 packages** (all Serilog ecosystem).
Security and performance monitoring use BCL — zero new runtime dependencies.

---

## Alternatives Considered and Rejected

| Category | Recommended | Alternative | Why Rejected |
|----------|-------------|-------------|--------------|
| Logging | Serilog 4.3.1 | NLog 5.x | NLog's advantage (throttle performance) irrelevant for low-volume desktop app. Serilog enricher model better for audit trails. |
| Logging | Serilog 4.3.1 | Microsoft.Extensions.Logging + provider | MEL is an abstraction layer requiring a DI host to be useful. Serilog is self-contained and easier to wire in WinForms Program.cs without a HostBuilder. |
| Security | BCL SHA256/PBKDF2 | BCrypt.Net-Next | BCrypt is excellent but adds a NuGet dependency for functionality the .NET 8 BCL already provides. KISA explicitly recommends SHA-256; BCrypt is not listed in KISA's approved algorithm set for Korean public-sector software. |
| Security | BCL SHA256/PBKDF2 | Bouncy Castle | Significant dependency weight (~5 MB), FIPS mode complexity, overkill for single hash+salt operation. |
| Error handling | BCL patterns | Polly | Polly is a network resilience library; AOLT has no network calls. Wrong tool for the problem. |
| Performance | BenchmarkDotNet | dotTrace / dotMemory | JetBrains profilers are commercial IDE plugins, not scriptable CLI tools. BenchmarkDotNet produces exportable data suitable for GS documentation evidence. |

---

## GS Certification Mapping

| GS Quality Characteristic | Tool/Approach | Evidence Produced |
|---------------------------|---------------|-------------------|
| 보안성 — 암호화 (KISA SHA-256 + Salt) | BCL `Rfc2898DeriveBytes.Pbkdf2` | Code review: algorithm name + iteration count visible in source |
| 보안성 — 감사추적 | Serilog file sink + Thread/Process enrichers | Log files with timestamps, thread IDs, operation context |
| 보안성 — 접근통제 | Existing MAC lock + improved hash (above) | License validation log entries |
| 유지보수성 — 구조화 로그 | Serilog rolling file | `logs/aolt-YYYYMMDD.log` — reviewable by GS evaluators |
| 신뢰성 — 고장 회피 | `Application.ThreadException` + typed catch blocks | No silent exception swallowing; all exceptions logged before surface to user |
| 성능효율성 — 응답 시간 | BenchmarkDotNet (dev) + PerformanceCounter (runtime log) | Benchmark reports + performance log entries as measurement evidence |

---

## Confidence Assessment

| Area | Confidence | Basis |
|------|------------|-------|
| Serilog versions | HIGH | Confirmed on NuGet.org (Serilog 4.3.1, Sinks.File 7.0.0, Apr/Nov 2025) |
| SHA-256/PBKDF2 BCL | HIGH | Official Microsoft .NET 8 docs (`Rfc2898DeriveBytes.Pbkdf2` static method) |
| KISA SHA-256 requirement | HIGH | Confirmed via KISA seed.kisa.or.kr — SHA-1 deprecated, SHA-224~512 recommended |
| BenchmarkDotNet version | HIGH | Confirmed on NuGet.org (0.15.8, November 2025) |
| GS security traceability items | MEDIUM | TTA GS certification overview confirms traceability/access control as evaluation items; exact scoring rubric not publicly available |
| Polly exclusion rationale | HIGH | Polly GitHub confirms it targets transient network faults — no network I/O in AOLT |

---

## Sources

- [Serilog 4.3.1 on NuGet](https://www.nuget.org/packages/serilog/)
- [Serilog.Sinks.File 7.0.0 on NuGet](https://www.nuget.org/packages/serilog.sinks.file/)
- [Serilog.Enrichers.Thread 4.0.0 on NuGet](https://www.nuget.org/packages/serilog.enrichers.thread)
- [BenchmarkDotNet 0.15.8 on NuGet](https://www.nuget.org/packages/benchmarkdotnet/)
- [Rfc2898DeriveBytes.Pbkdf2 — Microsoft .NET 8 docs](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes.pbkdf2?view=net-8.0)
- [HMACSHA256 Class — Microsoft .NET 8 docs](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.hmacsha256?view=net-8.0)
- [KISA 암호이용활성화 — 검증대상 암호알고리즘](https://seed.kisa.or.kr/kisa/kcmvp/EgovVerification.do)
- [GS Testing and Certification — TTA](https://sw.tta.or.kr/eng/service/gsce_it.jsp)
- [Polly GitHub — App-vNext/Polly](https://github.com/App-vNext/Polly)
- [System.Diagnostics.PerformanceCounter 8.0.0 on NuGet](https://www.nuget.org/packages/System.Diagnostics.PerformanceCounter/8.0.0)

---

*Research date: 2026-04-14*
