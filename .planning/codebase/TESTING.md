# Testing Patterns

**Analysis Date:** 2026-04-14

## Test Framework

**Status:** No automated testing framework detected

**Runner:**
- None configured
- No test projects found in solution
- No xUnit, NUnit, MSTest, or other test framework packages in csproj

**Assertion Library:**
- Not applicable - no testing framework present

**Run Commands:**
```bash
# No test commands available
# Application is Windows Forms desktop app (tested via UI)
```

## Test File Organization

**Location:**
- No test files present in codebase
- No `Tests/`, `UnitTests/`, or `.Tests` directories found

**Naming:**
- Not applicable (no test files)

**Structure:**
- Not applicable (no test files)

## Test Coverage

**Status:** No unit tests present

**Requirements:** None enforced

**Approach:** Manual testing via Windows Forms UI
- Application is a desktop tool for video annotation
- Testing is performed through interactive form controls
- No automated test coverage

## Testing Gaps

**Critical untested areas:**

1. **JSON Serialization (`JsonService.cs`)**
   - Loading/parsing COCO format JSON
   - Category ID mapping logic
   - Bbox conversion between coordinate systems
   - Files affected: `JsonService.cs`

2. **Video Frame Processing (`VideoService.cs`)**
   - Frame seeking and loading via OpenCV
   - Playback timing calculations
   - SRT subtitle parsing and time mapping
   - Files affected: `VideoService.cs`

3. **Coordinate Transformations (`CoordinateHelper.cs`)**
   - Image-to-view coordinate conversion with aspect ratio scaling
   - View-to-image conversion
   - Zoom and letterboxing calculations
   - Files affected: `CoordinateHelper.cs`

4. **Bounding Box Operations (`MainForm.cs`)**
   - Box drawing, resizing, moving
   - Hit testing and selection
   - Undo/redo stack management
   - Files affected: `MainForm.cs` (~2000+ lines)

5. **Model Classes (`Models/`)**
   - BoundingBox, WaypointMarker, LabelingData classes
   - JSON deserialization with Newtonsoft.Json
   - Files affected: `Models/LabelingData.cs`, `Models/BoundingBox.cs`, `Models/WaypointMarker.cs`

## Manual Testing Observations

**How Testing Currently Works:**
- Developers use Windows Forms UI directly
- Manual verification of video playback, annotation, JSON export
- No regression testing framework

**Areas Needing Test Coverage:**
- File I/O operations with error handling
- Large file memory management (OutOfMemoryException handling)
- FFmpeg integration for subtitle extraction
- Backup file creation during JSON load

## Potential Testing Strategy

**What Should Be Tested (Recommendations):**

1. **Unit Tests for Services:**
   - `JsonService`: Test JSON parsing, category mapping, bbox calculations
   - `VideoService`: Test frame operations, SRT parsing, timestamp extraction
   - `CoordinateHelper`: Test all transformation methods with edge cases

2. **Integration Tests:**
   - Load video + JSON annotation file
   - Export annotations and validate JSON structure
   - SRT subtitle extraction and mapping

3. **Manual/UI Tests:**
   - Box drawing and manipulation
   - Playback controls
   - Keyboard shortcuts
   - Theme application

## Known Testing Challenges

**Difficulty in Testing:**

1. **Windows Forms Dependency**
   - `PictureBox`, `Form`, `Control` classes tight to UI
   - Makes unit testing service methods difficult
   - `CoordinateHelper` methods require `PictureBox` instance

2. **External Dependencies**
   - OpenCV (OpenCvSharp4) - complex native interop
   - FFmpeg (FFMpegCore) - external process
   - File I/O operations
   - System DLL loading

3. **Stateful Services**
   - `VideoService` maintains internal state (currentFrame, isPlaying)
   - `MainForm` has extensive state management (100+ fields)
   - Difficult to test without initialization of full application

## Recommended Testing Improvements

**Short-term (if testing to be added):**

1. **Create unit test project:**
   ```
   ASLTv1.Tests/
   ```

2. **Test critical business logic first:**
   - `JsonService.GetCategoryId()` and `GetCategoryName()` - pure functions
   - `VideoService.FormatFrameTime()` and `ParseSrtTime()` - utility methods
   - `CoordinateHelper` transformation methods - math-heavy

3. **Use xUnit or NUnit:**
   ```csharp
   [Fact]
   public void GetCategoryId_PersonLabel_ReturnsCategoryIdRange()
   {
       // Arrange
       var box = new BoundingBox { Label = "person", PersonId = 5 };
       
       // Act
       int categoryId = JsonService.GetCategoryId(box);
       
       // Assert
       Assert.InRange(categoryId, 1, 20);
   }
   ```

4. **Mock external dependencies:**
   - Mock `VideoCapture` for frame operations
   - Mock file I/O for JSON operations
   - Use Moq or NSubstitute for Windows Forms controls

5. **Integration test example:**
   ```csharp
   [Fact]
   public async Task LoadLabelingDataAsync_ValidJsonFile_ReturnsSuccessWithData()
   {
       // Arrange
       var jsonService = new JsonService();
       string testFilePath = "test_video.mp4";
       
       // Act
       var result = await jsonService.LoadLabelingDataAsync(testFilePath, 30.0);
       
       // Assert
       Assert.True(result.Success);
       Assert.NotNull(result.BoundingBoxes);
   }
   ```

---

*Testing analysis: 2026-04-14*
