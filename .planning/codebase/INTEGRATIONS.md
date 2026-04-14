# External Integrations

**Analysis Date:** 2026-04-14

## APIs & External Services

**Video Processing APIs:**
- FFmpeg (via FFMpegCore) - Subtitle extraction, video codec handling
  - SDK/Client: FFMpegCore v5.1.0
  - Discovery: System PATH or local `/ffmpeg/ffmpeg.exe`
  - Type: Command-line utility wrapper

## Data Storage

**Databases:**
- Not used - Application is file-based only

**File Storage:**
- Local filesystem only
  - Video files: User-selected directories
  - JSON annotation files: User-selected export paths (COCO format)
  - SRT subtitle files: Extracted to same directory as video file
  - Auto-generated paths: `[video_name].srt` for subtitle extracts

**Caching:**
- None - All data loaded on demand

## File I/O Operations

**Video Files:**
- Input: OpenCvSharp VideoCapture from user-selected paths
- Supported: Any format FFmpeg can decode
- Read-only access

**Annotation Data:**
- Format: COCO-like JSON with extended tracking information
- Export: `JsonService.ExportToJsonExtended()` → `LabelingDataExtended` model
- Import: `JsonService` loads legacy and extended formats
- Location: User-selected via file dialogs

**Subtitle Files:**
- Format: SRT (SubRip)
- Source: Embedded in video via FFmpeg extraction
- Destination: `[video_dir]/[video_name].srt`
- Read: `VideoService.LoadSrtFileAsync()` after extraction

## Configuration & Initialization

**FFmpeg Setup:**
- Method: `VideoService.SetupFFmpegPath()`
- Called at application startup
- Detection order:
  1. System PATH (checks `ffmpeg -version`)
  2. Local folder: `[AppStartupPath]/ffmpeg/ffmpeg.exe`
- Property: `isFFmpegAvailable` indicates success

**OpenCV Native Libraries:**
- Required: OpenCvSharpExtern.dll, opencv_videoio_ffmpeg4110_64.dll
- Location: Auto-loaded by NuGet runtime package
- Error handling: Exception handling in `VideoService` constructor indicates missing DLLs

## Data Models & Serialization

**COCO Format Export (Extended with Tracking):**
- Classes: `LabelingDataExtended`, `ImageInfo`, `AnnotationData`, `CategoryData`, `TrackInfo`
- Location: `Models/LabelingData.cs`
- JSON properties: Images (frames), Annotations (bounding boxes), Categories (person/vehicle/event types)
- Extension: TrackInfo includes entry/exit frames and timestamps per annotation

**Legacy Format Support:**
- Classes: `LegacyLabelingData`, `LegacyItem`, `LegacyAnnotation`
- Location: `Models/LabelingData.cs`
- Purpose: Backward compatibility for older project files

**Category Mapping:**
- Person categories: person_01 to person_20 (IDs 1-20)
- Vehicle categories: car, motorcycle, e_scooter, bicycle (IDs 21-24)
- Event categories: hazard, accident, damage, fire, intrusion, leak, failure, lost_object, fall, abnormal_behavior (IDs 25-34)
- Mapping: `JsonService.CategoryNameToIdMap` dictionary

## Webhook & Callback Integration

**None detected** - Application is standalone desktop tool with no remote callbacks or webhooks.

## External Dependencies Summary

**Runtime Dependencies:**
- FFmpeg binary (external executable) - Required for subtitle extraction
- OpenCV native libraries - Required for video processing

**No External APIs:**
- No cloud services
- No authentication services
- No remote data synchronization
- No API keys or tokens required
- No network requests detected

**Data Flow:**
```
[Video File] → OpenCvSharp VideoCapture
    ↓
[Frame Processing] → OpenCV image operations
    ↓
[User Annotation] → In-memory BoundingBox collection
    ↓
[Export] → Newtonsoft.Json → JSON file
    ↓
[Subtitle Extraction] → FFMpegCore → SRT file
```

---

*Integration audit: 2026-04-14*
