# Architecture

**Analysis Date:** 2026-04-14

## Pattern Overview

**Overall:** Three-Tier Layered Architecture with Service-Oriented Pattern (Windows Forms MVC-inspired)

**Key Characteristics:**
- Presentation Layer (Windows Forms UI) decoupled from business logic via Services
- Service Layer provides video and JSON data operations
- Model Layer defines data contracts with COCO-format JSON serialization
- Single entry point through `MainForm`, driven by user interaction events
- Unidirectional data flow: UI → Services → Models → UI

## Layers

**Presentation Layer (Forms):**
- Purpose: Render video frames, handle user annotations, display playback controls
- Location: `Forms/`
- Contains: UI forms (`MainForm.cs`, `AboutForm.cs`), designer files, event handlers
- Depends on: `Services/`, `Models/`, `Helpers/`, `Theme/`
- Used by: Entry point (`Program.cs`), user interaction loop

**Service Layer:**
- Purpose: Encapsulate business logic for video management and data persistence
- Location: `Services/`
- Contains: `VideoService.cs`, `JsonService.cs`
- Depends on: `Models/`, external libraries (OpenCvSharp4, FFMpegCore, Newtonsoft.Json)
- Used by: MainForm for async operations, data loading/saving

**Model Layer:**
- Purpose: Define data contracts and structures for annotations, COCO JSON format, and tracking
- Location: `Models/`
- Contains: `BoundingBox.cs`, `LabelingData.cs`, `WaypointMarker.cs` and supporting classes
- Depends on: Newtonsoft.Json for serialization
- Used by: Services and Presentation for data representation

**Helper Layer:**
- Purpose: Provide utility functions for coordinate transformations between image and view space
- Location: `Helpers/`
- Contains: `CoordinateHelper.cs` (static helper methods)
- Depends on: System.Drawing
- Used by: MainForm for bounding box positioning with aspect-ratio scaling

**Theme/Styling:**
- Purpose: Apply consistent dark theme across all UI controls
- Location: `Theme/`
- Contains: `DarkTheme.cs` (static theme definitions and application logic)
- Depends on: System.Windows.Forms
- Used by: MainForm during initialization

## Data Flow

**Video Playback Pipeline:**

1. User selects video file via file dialog
2. `MainForm.btnSelectFolder_Click()` invokes `LoadVideoWithSubtitle()`
3. `VideoService.LoadVideoAsync()` opens video using OpenCvSharp VideoCapture
4. Frame is loaded via `VideoService.LoadFrame()` and rendered to `pictureBoxVideo`
5. User interaction triggers frame navigation or playback
6. Timeline update occurs via `panelTimeline` paint event

**Annotation Load/Save Pipeline:**

1. Video loads successfully → `LoadLabelingData()` invoked
2. `JsonService.LoadLabelingDataAsync()` reads `video_labels.json` from `./labels/` directory
3. JSON deserialized into `LabelingDataExtended` objects
4. Bounding boxes and waypoint markers populated into UI list views
5. User edits annotations → changes stored in `boundingBoxes` and `waypointMarkers` lists
6. On save → `JsonService.ExportToJsonExtended()` serializes to COCO-format JSON

**User Annotation Input:**

1. User selects Draw/Select mode via toggle
2. Mouse down + drag creates or modifies `BoundingBox`
3. Label selection dropdown updates `currentSelectedLabel`
4. Category ID mapping applied via `JsonService.GetCategoryId()`
5. Undo/Redo stacks track changes via `UndoAction` objects
6. PictureBox paint event renders all annotations with coordinate transformation

**State Management:**

- **Annotation State:** `boundingBoxes` (List<BoundingBox>), `selectedBox`, `waypointMarkers` (List<WaypointMarker>)
- **UI State:** `currentMode` (DrawMode), `isDrawing`, `isDragging`, `isResizing`, draw points, drag offsets
- **Video State:** Maintained in `VideoService` (frame index, FPS, playback speed, is playing)
- **Undo/Redo:** `undoStack` and `redoStack` (Stack<UndoAction>) track annotation changes

## Key Abstractions

**VideoService:**
- Purpose: Encapsulates video capture lifecycle, frame loading, SRT subtitle extraction, FFmpeg integration
- Examples: `LoadVideoAsync()`, `LoadFrame()`, `GetSubtitleTimestampForFrame()`, playback control methods
- Pattern: Stateful service managing single video at a time; events raised on frame change and playback state change

**JsonService:**
- Purpose: Handles COCO-format JSON serialization/deserialization and category mapping
- Examples: `LoadLabelingDataAsync()`, `ExportToJsonExtended()`, category name/ID mapping via static dictionaries
- Pattern: Stateless service; static helper methods for category management; returns structured results (LoadResult)

**CoordinateHelper:**
- Purpose: Transforms between image space coordinates and PictureBox view space (handles aspect ratio scaling)
- Examples: `ImageToView()`, `ViewToImage()`, `GetImageDisplayRectangle()`
- Pattern: Pure static utility class with no state

**BoundingBox Model:**
- Purpose: Represents a single annotation rectangle with label, object IDs (person/vehicle/event), and frame association
- Fields: `FrameIndex`, `Rectangle`, `Label`, `PersonId`, `VehicleId`, `EventId`, `Action`, `IsDeleted`
- Usage: Core data structure passed between layers

**WaypointMarker Model:**
- Purpose: Represents temporal range (entry/exit frames) for tracking objects across multiple frames
- Fields: `EntryFrame`, `ExitFrame`, `ObjectId`, `Label`, `MarkerColor`, timestamps, `InteractingObject`
- Usage: Defines track lifecycle; linked to bounding boxes during export

## Entry Points

**Program.cs:**
- Location: `Program.cs`
- Triggers: Application startup (STAThread entry point)
- Responsibilities: Initialize Windows Forms application, instantiate and run MainForm

**MainForm:**
- Location: `Forms/MainForm.cs`
- Triggers: Form load event, user clicks on buttons/controls, keyboard input, paint events
- Responsibilities: Render video frames, handle user annotation input, manage playback, coordinate services

## Error Handling

**Strategy:** Try-catch blocks with user-facing MessageBox dialogs for critical failures; graceful degradation for partial failures

**Patterns:**
- `LoadVideoWithSubtitle()`: Catches and displays video load failures
- `JsonService.LoadLabelingDataAsync()`: Handles OutOfMemoryException separately; returns success flag in LoadResult
- `ExportToJsonExtended()`: Wraps in try-catch, throws InvalidOperationException with context
- Backup creation: JSON files backed up before load; exceptions logged to Debug output, not thrown
- Missing resources: ResolveJsonPath() returns null if no JSON exists (no exception)

## Cross-Cutting Concerns

**Logging:** Debug.WriteLine() used for diagnostic output (e.g., backup failures); no centralized logging framework

**Validation:**
- Bounding box size validation: `MIN_BBOX_SIZE` constant enforced
- Frame index bounds checking before LoadFrame()
- JSON file existence checks via Directory/File APIs
- Video capture readiness verified via `IsVideoLoaded` property

**Authentication:** Not applicable (local desktop application)

**Coordinate System Handling:**
- Image space: Raw frame pixel coordinates (e.g., 1920x1080 actual frame size)
- View space: PictureBox display coordinates with aspect-ratio scaling applied
- Automatic transformation via `CoordinateHelper` for all annotations
- Zoom mode in PictureBox applies letterboxing; scale factors computed per render

---

*Architecture analysis: 2026-04-14*
