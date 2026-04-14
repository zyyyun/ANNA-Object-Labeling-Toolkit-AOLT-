# Technology Stack

**Analysis Date:** 2026-04-14

## Languages

**Primary:**
- C# - Windows Forms desktop application, all business logic and UI

## Runtime

**Environment:**
- .NET 8.0 (net8.0-windows)
- Windows Forms (UseWindowsForms: true)

**Package Manager:**
- NuGet (implicit in .NET project)
- Project: `ASLTv1.0.csproj`
- Lockfile: `obj/project.assets.json` (auto-generated)

## Frameworks

**Core:**
- Windows Forms - Desktop UI framework
- .NET Framework Standard Library - Base runtime

**Video/Image Processing:**
- OpenCvSharp4 v4.11.0.20250507 - Computer vision operations, frame processing
- OpenCvSharp4.runtime.win v4.11.0.20250507 - Native Windows runtime for OpenCV
- OpenCvSharp4.Extensions v4.11.0.20250507 - Extension methods and utilities

**Serialization:**
- Newtonsoft.Json (JSON.NET) v13.0.3 - JSON serialization/deserialization for COCO format data

**Multimedia:**
- FFMpegCore v5.1.0 - FFmpeg wrapper for video processing, subtitle extraction, video analysis

**Graphics:**
- System.Drawing.Common v8.0.11 - Graphics, image rendering, coordinate transformations

## Key Dependencies

**Critical:**
- OpenCvSharp4 v4.11.0.20250507 - Core video frame capture, image processing (VideoCapture, Mat objects)
- FFMpegCore v5.1.0 - SRT subtitle extraction, video codec handling
- Newtonsoft.Json v13.0.3 - COCO format JSON export/import, annotation serialization

**Infrastructure:**
- System.Drawing.Common v8.0.11 - Display rendering, bounding box visualization, coordinate math

## Configuration

**Environment:**
- Solution file: `ASLTv1.0.sln`
- Project file: `ASLTv1.0.csproj`
- Target platforms: AnyCPU, x64 (x64 preferred)
- ImplicitUsings: enabled
- Nullable: disabled

**Build:**
- Platforms: Debug|Any CPU, Debug|x64, Debug|x86, Release variants (6 configurations)
- Primary target: x64 platform
- Output: WinExe (Windows Forms executable)

## Platform Requirements

**Development:**
- Windows (Windows Forms requirement)
- Visual Studio 2022+ (SDK Microsoft.NET.Sdk)
- .NET 8.0 SDK
- OpenCV native DLL dependencies (OpenCvSharpExtern.dll, opencv_videoio_ffmpeg4110_64.dll)
- FFmpeg binary (system PATH or `/ffmpeg/ffmpeg.exe` in app folder)

**Production:**
- Windows 7 or later (Windows Forms compatible)
- .NET 8.0 Runtime
- OpenCV native binaries
- FFmpeg binary (for subtitle extraction feature)
- x64 architecture recommended

## Feature Dependencies

**Video Playback & Analysis:**
- OpenCvSharp4 with FFmpeg codec support for video file formats (MP4, AVI, MOV, etc.)
- VideoCapture class for frame-by-frame processing
- Mat image objects for frame storage

**Subtitle Support:**
- FFMpegCore to extract embedded SRT subtitle streams
- File I/O for SRT parsing (SubtitleEntry model)

**Annotation Export:**
- Newtonsoft.Json for COCO format JSON output
- JSON schema includes: images, annotations, categories, track info (entry/exit frames, timestamps)

---

*Stack analysis: 2026-04-14*
