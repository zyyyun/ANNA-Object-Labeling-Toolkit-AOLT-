---
phase: 03-기능-정확성-보안
plan: "02"
subsystem: security
tags: [security, pbkdf2, path-traversal, kisa, secu-01, secu-04]
dependency_graph:
  requires: []
  provides: [SecurityHelper, PathValidator]
  affects: [Services/JsonService.cs, Forms/MainForm.cs]
tech_stack:
  added: []
  patterns: [PBKDF2-HMAC-SHA256, timing-safe-compare, path-normalization]
key_files:
  created:
    - Helpers/SecurityHelper.cs
    - Helpers/PathValidator.cs
  modified:
    - Services/JsonService.cs
    - Forms/MainForm.cs
decisions:
  - "Log via System.Diagnostics.Debug.WriteLine instead of Serilog (worktree version lacks Serilog dependency)"
  - "310,000 PBKDF2 iterations chosen per OWASP 2023 minimum recommendation for SHA256"
  - "PathValidator uses Path.GetFullPath normalization + OrdinalIgnoreCase prefix check for Windows compatibility"
metrics:
  duration: "~12 minutes"
  completed: "2026-04-16T07:24:36Z"
  tasks_completed: 2
  files_changed: 4
  files_created: 2
---

# Phase 03 Plan 02: KISA Security Modules Summary

PBKDF2-HMAC-SHA256 hashing module and path traversal prevention applied to all file I/O paths, satisfying KISA security guidelines for SECU-01 and SECU-04.

## What Was Built

### Task 1: SecurityHelper + PathValidator (new files)

**`Helpers/SecurityHelper.cs`** — KISA-compliant PBKDF2 hashing (SECU-01):
- `HashSecret(string input)` generates a 16-byte random salt via `RandomNumberGenerator.GetBytes`, then hashes using `Rfc2898DeriveBytes.Pbkdf2` with SHA256 and 310,000 iterations. Returns `(Hash, Salt)` as Base64 strings.
- `VerifySecret(string input, string storedHash, string storedSalt)` recomputes the hash and compares using `CryptographicOperations.FixedTimeEquals` to prevent timing attacks.

**`Helpers/PathValidator.cs`** — Path traversal prevention (SECU-04):
- `IsPathSafe(string filePath, string allowedBaseDir)` normalizes both paths via `Path.GetFullPath`, appends a trailing separator to the base, then checks that `filePath` starts with `allowedBaseDir` (OrdinalIgnoreCase). Returns `false` on `ArgumentException` for invalid paths.

### Task 2: PathValidator integration

**`Services/JsonService.cs`**:
- Added `using ASLTv1.Helpers;`
- `ResolveJsonPath`: validates `normalPath` against `videoDir` before returning — returns `null` if traversal detected
- `DeleteJsonFileForVideo`: validates `currentJsonFile` against `videoDir` before deletion — returns `false` if traversal detected

**`Forms/MainForm.cs`**:
- `SaveCurrentLabelingData`: validates `savePath` against `videoDir` before calling `ExportToJsonExtended` — shows `MessageBox` warning and returns early if traversal detected

## Build Verification

```
dotnet build -c Debug
경고 28개, 오류 0개
```

All 28 warnings are pre-existing (nullable reference type annotations, pre-existing async warning) — none introduced by this plan.

## Decisions Made

1. **Logging via Debug.WriteLine** — The worktree's JsonService lacked the Serilog dependency present in the main branch. Used `System.Diagnostics.Debug.WriteLine` for consistency with the worktree codebase.
2. **310,000 PBKDF2 iterations** — Matches OWASP 2023 minimum recommendation for PBKDF2-HMAC-SHA256.
3. **OrdinalIgnoreCase path comparison** — Required for Windows filesystem case-insensitivity correctness.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Logging approach adjusted for worktree**
- **Found during:** Task 2
- **Issue:** Worktree JsonService.cs did not have `using Serilog;` — plan specified `Log.Warning(...)` but Serilog was unavailable in the worktree codebase
- **Fix:** Used `System.Diagnostics.Debug.WriteLine(...)` which matches the existing logging pattern in the worktree's JsonService
- **Files modified:** Services/JsonService.cs, Forms/MainForm.cs
- **Commit:** f412967

## Known Stubs

None — all implemented methods are fully functional with no placeholder values.

## Requirements Covered

- **SECU-01**: SHA-256 이상 단방향 암호화 + Salt — satisfied by `SecurityHelper.HashSecret` with PBKDF2-HMAC-SHA256, 310,000 iterations, 16-byte salt
- **SECU-04**: 경로 트래버설 방지 — satisfied by `PathValidator.IsPathSafe` applied to all file I/O points in JsonService and MainForm

## Self-Check: PASSED

- Helpers/SecurityHelper.cs: FOUND
- Helpers/PathValidator.cs: FOUND
- Services/JsonService.cs PathValidator.IsPathSafe references: 2 occurrences
- Forms/MainForm.cs PathValidator.IsPathSafe references: 1 occurrence
- Build: 0 errors
- Commit f412967: FOUND
