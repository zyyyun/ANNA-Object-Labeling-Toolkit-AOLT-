# Coding Conventions

**Analysis Date:** 2026-04-14

## Naming Patterns

**Files:**
- PascalCase for classes: `MainForm.cs`, `VideoService.cs`, `BoundingBox.cs`
- Designer files: `MainForm.Designer.cs`
- Plural for folders: `Forms/`, `Models/`, `Services/`, `Helpers/`, `Theme/`

**Functions/Methods:**
- PascalCase for public methods: `LoadVideoAsync()`, `LoadFrame()`, `GetBoxId()`
- PascalCase for private methods: `EnableDoubleBuffering()`, `ApplyToControls()`
- Async methods suffixed with `Async`: `LoadVideoAsync()`, `LoadSrtFileAsync()`, `ExtractSrtFromVideoAsync()`

**Variables:**
- camelCase for local variables and parameters: `videoCapture`, `currentFrame`, `frameIndex`, `isPlaying`
- camelCase for private fields: `_videoService`, `_jsonService`, `videoCapture`, `currentFrame`
- Prefix underscore for private fields (inconsistently used): Some fields use underscore (`_videoService`), others don't (`videoCapture`)
- ALL_CAPS for constants: `HANDLE_SIZE`, `MIN_BBOX_SIZE`, `MAX_UNDO_STACK`, `RESIZE_BORDER_WIDTH`

**Types/Classes:**
- PascalCase for class names: `VideoService`, `JsonService`, `BoundingBox`, `WaypointMarker`
- PascalCase for enum names: `DrawMode`, `UndoActionType`, `ResizeHandle`

## Code Style

**Formatting:**
- No enforced automatic formatter (no .editorconfig, prettier, or similar found)
- 4-space indentation observed
- Braces follow C# convention (opening brace on same line)
- No enforced line length limit observed

**Linting:**
- No linting configuration found (no .editorconfig, .ruleset files)
- StyleCop or Roslyn analyzers not configured
- Nullable types disabled in csproj: `<Nullable>disable</Nullable>`

## Import Organization

**Order:**
1. System namespaces: `using System;`, `using System.Collections.Generic;`
2. Third-party libraries: `using OpenCvSharp;`, `using FFMpegCore;`, `using Newtonsoft.Json;`
3. Project namespaces: `using ASLTv1.Models;`, `using ASLTv1.Services;`

**Path Aliases:**
- No path aliases configured
- Fully qualified namespace imports used throughout

## Error Handling

**Patterns:**
- Try-catch blocks with specific exception types:
  - `TypeInitializationException` for OpenCV DLL initialization
  - `DllNotFoundException` for missing native dependencies
  - `OutOfMemoryException` for large file loads
  - `Exception` as fallback catch-all
- Custom error messages with context: `"OpenCvSharp 네이티브 DLL 초기화 실패:\n\n"`
- Result objects for async operations: `LoadResult` class with `Success`, `ErrorMessage`, and data fields
- Debug output via `System.Diagnostics.Debug.WriteLine()` for logging errors

**Examples from `JsonService.cs`:**
```csharp
catch (OutOfMemoryException oomEx)
{
    result.Success = false;
    result.ErrorMessage = $"메모리 부족으로 파일을 로드할 수 없습니다.\n{oomEx.Message}";
}
catch (Exception ex)
{
    result.Success = false;
    result.ErrorMessage = $"라벨링 데이터 로드 오류: {ex.Message}";
    if (ex.InnerException != null)
        result.ErrorMessage += $"\n\n상세 정보: {ex.InnerException.Message}";
}
```

**In VideoService.cs:**
```csharp
catch (TypeInitializationException tiex)
{
    string errorMsg = "OpenCvSharp 네이티브 DLL 초기화 실패:\n\n" +
                    $"{tiex.Message}\n\n" +
                    "가능한 원인:\n" +
                    "1. Visual C++ 재배포 가능 패키지가 설치되지 않았습니다.\n" +
                    "(Microsoft Visual C++ 2015-2022 Redistributable 설치 필요)\n" +
                    "2. OpenCvSharpExtern.dll 또는 관련 DLL이 누락되었습니다.\n" +
                    "3. 플랫폼 아키텍처 불일치 (x64 필요)";
    throw new InvalidOperationException(errorMsg, tiex);
}
```

## Logging

**Framework:** No dedicated logging framework - uses `System.Diagnostics.Debug.WriteLine()`

**Patterns:**
- Debug output for errors: `System.Diagnostics.Debug.WriteLine($"[프레임 로드 오류] {ex.Message}\n{ex.StackTrace}")`
- Prefix format: `[Operation Name]` for categorization
- Korean language comments and messages throughout codebase
- Error tracking in try-catch blocks only

**Examples:**
```csharp
System.Diagnostics.Debug.WriteLine($"[JSON 로드] 백업 파일 생성 실패: {backupEx.Message}");
System.Diagnostics.Debug.WriteLine($"[자막 로드 오류] {ex.Message}");
System.Diagnostics.Debug.WriteLine($"[프레임 로드 오류] {ex.Message}\n{ex.StackTrace}");
```

## Comments

**When to Comment:**
- XML documentation comments (/// style) for public classes and public methods
- Inline comments for complex logic or non-obvious behavior
- Korean language comments predominate throughout

**JSDoc/TSDoc:**
```csharp
/// <summary>
/// Handles loading and exporting labeling data in COCO-like JSON format.
/// JSON 라벨링 데이터 로드/내보내기 서비스.
/// </summary>
public class JsonService

/// <summary>
/// Resolves the _labels.json file path for a given video file.
/// Returns null if no JSON file exists.
/// </summary>
public string? ResolveJsonPath(string videoFilePath)

/// <summary>Raised when a new frame has been loaded.</summary>
public event EventHandler<int>? FrameChanged;
```

## Function Design

**Size:**
- Methods range from 20-600+ lines
- Service methods tend to be longer (180-500 lines) due to complex data processing
- Helper methods are typically shorter (5-30 lines)

**Parameters:**
- Minimal parameter lists observed
- Action/callback parameters used for progress updates: `Action<string>? progressCallback = null`
- Optional parameters with null defaults: `VideoService? videoService = null`

**Return Values:**
- Nullable return types: `string?`, `Bitmap?`, `LoadResult`
- Result objects returned instead of throwing exceptions
- Properties exposed via public Properties blocks

## Module Design

**Exports:**
- Public classes define API surface
- Services use dependency injection through constructor
- Events for notifications: `FrameChanged`, `PlayStateChanged`, `VideoLoaded`

**Barrel Files:**
- No barrel files or index.ts equivalents
- Direct imports from specific files: `using ASLTv1.Services;`

## Region Organization

**Pattern:**
- Code organized into logical sections using `#region`/`#endregion` blocks
- Common region names:
  - `#region Constants`
  - `#region Fields`
  - `#region Properties`
  - `#region Events`
  - `#region [Feature Name]`
  - `#region IDisposable`
  - `#region Helper Methods`
  - `#region Category ID Mapping`

**Examples from `VideoService.cs`:**
```csharp
#region Fields
#region Properties
#region Events
#region Video Loading
#region Frame Loading
#region Video Playback
#region Time Formatting
#region SRT Subtitle Extraction
#region IDisposable
```

## Static Methods vs Instance Methods

**Pattern:**
- Helper classes use static methods: `CoordinateHelper` with all static methods
- Service classes use instance methods with stateful fields
- Factory methods: `DarkTheme.Apply()`, `DarkTheme.ApplyButton()`

## Nullability Handling

- Csproj sets `<Nullable>disable</Nullable>` - nullable reference types NOT enforced
- Null-coalescing operator used: `?? ""`, `?? null`
- Null-conditional operator used: `?.Invoke()`, `?.Dispose()`
- Explicit null checks: `if (string.IsNullOrEmpty(...))`, `if (videoCapture != null)`

---

*Convention analysis: 2026-04-14*
