# Codebase Structure

**Analysis Date:** 2026-04-14

## Directory Layout

```
AOLTv1.0/
├── Forms/                # UI presentation layer (Windows Forms)
│   ├── MainForm.cs       # Primary annotation interface
│   ├── MainForm.Designer.cs
│   └── AboutForm.cs      # About/Info dialog
├── Services/             # Business logic services
│   ├── VideoService.cs   # Video I/O, frame loading, playback
│   └── JsonService.cs    # COCO JSON serialization/deserialization
├── Models/               # Data models and contracts
│   ├── BoundingBox.cs    # Annotation rectangle model
│   ├── LabelingData.cs   # COCO JSON model classes + tracking extensions
│   └── WaypointMarker.cs # Track entry/exit markers
├── Helpers/              # Utility functions
│   └── CoordinateHelper.cs # View↔Image coordinate transforms
├── Theme/                # UI theming
│   └── DarkTheme.cs      # Dark theme color definitions and application
├── Program.cs            # Application entry point
├── ASLTv1.0.csproj       # Project file (net8.0-windows, C# 12)
├── ASLTv1.0.sln          # Solution file
├── .planning/            # GSD planning documents
├── bin/                  # Compiled outputs (build artifacts, git-ignored)
├── obj/                  # Intermediate build files (git-ignored)
└── .vscode/              # IDE configuration (launch.json, settings.json)
```

## Directory Purposes

**Forms/:**
- Purpose: Contains all Windows Forms UI code
- Contains: Form classes (.cs), designer files (.Designer.cs), event handlers
- Key files: `MainForm.cs` (primary UI with annotation logic), `AboutForm.cs` (modal dialog)
- Notes: MainForm is massive (~2000+ lines); coordinate transformation and annotation rendering embedded

**Services/:**
- Purpose: Business logic encapsulation separate from UI
- Contains: `VideoService` (video I/O, playback, frame loading), `JsonService` (data persistence, COCO format)
- Key files: `VideoService.cs` (OpenCvSharp integration, FFMpeg), `JsonService.cs` (JSON export/import with category mapping)
- Pattern: Both are instantiated in MainForm constructor; VideoService is stateful (holds current video); JsonService is stateless

**Models/:**
- Purpose: Data contracts and serialization models
- Contains: `BoundingBox` (core annotation model), COCO JSON classes, `WaypointMarker` (tracking metadata)
- Key files: `LabelingData.cs` (both extended COCO format and legacy format support), `BoundingBox.cs` (annotation rectangle)
- Serialization: Newtonsoft.Json attributes control JSON property names (snake_case in JSON, PascalCase in C#)

**Helpers/:**
- Purpose: Pure utility functions
- Contains: `CoordinateHelper` (static methods for coordinate system transformations)
- Key files: `CoordinateHelper.cs` (ImageToView, ViewToImage for all coordinate translation)
- Notes: Handles PictureBox aspect-ratio scaling (Zoom mode with letterboxing)

**Theme/:**
- Purpose: Consistent dark theming across application
- Contains: `DarkTheme` class (static theme colors and Apply method)
- Key files: `DarkTheme.cs` (color constants, recursive control theming)
- Colors: PersonColor (red), VehicleColor (blue), EventColor (green) for bounding box rendering

## Key File Locations

**Entry Points:**
- `Program.cs`: Single-threaded apartment (STA) entry point; creates and runs MainForm

**Configuration:**
- `ASLTv1.0.csproj`: Project manifest (net8.0-windows, x64 platform, WindowsForms, implicit usings)
- `.vscode/`: IDE settings (launch.json for F5 debugging)

**Core Logic:**
- `Forms/MainForm.cs`: Handles video rendering, annotation drawing/editing, playback, UI state management
- `Services/VideoService.cs`: Video capture lifecycle, frame buffering, subtitle loading, FFmpeg integration
- `Services/JsonService.cs`: COCO JSON export/import, category ID mapping, backward compatibility with legacy format

**Data Models:**
- `Models/BoundingBox.cs`: Annotation rectangle with label (person/vehicle/event), object IDs, frame index, deletion flag
- `Models/LabelingData.cs`: COCO-format classes (ImageInfo, AnnotationData, CategoryData, etc.) + legacy support

**Coordinate System:**
- `Helpers/CoordinateHelper.cs`: All coordinate transformations (image↔view space, handles PictureBox aspect-ratio scaling)

## Naming Conventions

**Files:**
- Form files: `{FormName}Form.cs`, `{FormName}Form.Designer.cs` (e.g., `MainForm.cs`, `AboutForm.cs`)
- Service files: `{ServiceName}Service.cs` (e.g., `VideoService.cs`, `JsonService.cs`)
- Model files: `{ModelName}.cs` (e.g., `BoundingBox.cs`, `LabelingData.cs`)
- Helper files: `{HelperName}Helper.cs` (e.g., `CoordinateHelper.cs`)
- Theme files: `{ThemeName}.cs` (e.g., `DarkTheme.cs`)

**Directories:**
- Pascal case with no underscores: `Forms/`, `Services/`, `Models/`, `Helpers/`, `Theme/`

**Classes:**
- Pascal case: `MainForm`, `VideoService`, `JsonService`, `BoundingBox`, `CoordinateHelper`, `DarkTheme`

**Methods:**
- Pascal case: `LoadVideoAsync()`, `ExportToJsonExtended()`, `GetImageDisplayRectangle()`, `ViewToImage()`

**Fields/Properties:**
- Private fields: camelCase with underscore prefix: `_videoService`, `_jsonService`
- Local variables: camelCase: `filePath`, `boundingBoxes`, `currentMode`
- Properties: Pascal case: `IsVideoLoaded`, `CurrentFrame`, `CurrentFrameIndex`
- Constants: UPPER_SNAKE_CASE: `HANDLE_SIZE`, `MIN_BBOX_SIZE`, `MAX_UNDO_STACK`

**Enums:**
- Type: Pascal case: `DrawMode`, `ResizeHandle`, `UndoActionType`
- Values: Pascal case: `DrawMode.Select`, `ResizeHandle.TopLeft`

## Where to Add New Code

**New Feature - Annotation Type:**
- Primary code: `Models/BoundingBox.cs` (add new property for type), `Forms/MainForm.cs` (add draw/edit logic)
- UI Binding: `Forms/MainForm.cs` method handlers
- Export: `Services/JsonService.cs` (update category mapping and export logic)
- Tests: No unit test framework detected; manual testing via UI

**New Video Processing Service:**
- Implementation: `Services/{NewService}Service.cs`
- Instantiation: Constructor in `MainForm.cs` (add private field, initialize in ctor)
- Integration: Call methods from MainForm event handlers

**New Helper Function:**
- Shared helpers: `Helpers/CoordinateHelper.cs` (add static method)
- Specific to domain: Create new file `Helpers/{Domain}Helper.cs` if complex

**UI Control or Dialog:**
- Implementation: `Forms/{ControlName}Form.cs` (and `.Designer.cs` if using designer)
- Instantiation: Create and show from MainForm or other forms

**Theme Customization:**
- Colors: Add to `DarkTheme.cs` static fields (Color constants)
- Application: Modify `DarkTheme.Apply()` or `ApplyToControls()` methods

## Special Directories

**bin/:**
- Purpose: Compiled executable and dependencies output folder
- Generated: Yes (build output)
- Committed: No (git-ignored)

**obj/:**
- Purpose: Intermediate build artifacts (temporary compilation files)
- Generated: Yes (MSBuild output)
- Committed: No (git-ignored)

**.planning/codebase/:**
- Purpose: GSD codebase analysis documents
- Generated: Yes (by GSD tools)
- Committed: Yes (markdown documentation)

**.claude/**
- Purpose: Claude IDE session metadata and context
- Generated: Yes (IDE internal)
- Committed: No (git-ignored)

**.vscode/**
- Purpose: IDE configuration (launch configs, settings, extensions)
- Generated: Manually, partly auto-generated by IDE
- Committed: Yes (shared team configuration)

**Labels Directory (runtime-created):**
- Purpose: `./labels/` subdirectory created at runtime inside video directory; stores `{video_name}_labels.json`
- Generated: Yes (by application on save)
- Location: Sibling to video file on disk
- Resolution: `JsonService.ResolveJsonPath()` looks for `{video_dir}/labels/{video_name}_labels.json`

## Key File Locations and Their Roles

| File | Role | Entry/Config/Logic |
|------|------|---|
| `Program.cs` | Entry point | Entry |
| `ASLTv1.0.csproj` | Project manifest | Config |
| `Forms/MainForm.cs` | Main UI and orchestration | Logic |
| `Services/VideoService.cs` | Video I/O abstraction | Logic |
| `Services/JsonService.cs` | Data persistence | Logic |
| `Models/BoundingBox.cs` | Core data model | Logic |
| `Models/LabelingData.cs` | JSON contracts | Logic |
| `Helpers/CoordinateHelper.cs` | Coordinate math | Logic |
| `Theme/DarkTheme.cs` | UI theming | Logic |

---

*Structure analysis: 2026-04-14*
