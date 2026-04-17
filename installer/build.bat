@echo off
REM ASLT Installer Build Script
REM Runs: dotnet publish (self-contained x64) -> Inno Setup ISCC compile
REM Output: installer\Output\ASLT-Setup-v1.0.0.exe
REM Prereqs: .NET 8 SDK, Inno Setup 6.x (ISCC.exe), installer\ffmpeg\ffmpeg.exe placed manually

setlocal
set REPO_ROOT=%~dp0..
set ISCC="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

echo [1/3] Verifying prerequisites...
if not exist "%~dp0ffmpeg\ffmpeg.exe" (
  echo ERROR: installer\ffmpeg\ffmpeg.exe not found. Download from https://www.gyan.dev/ffmpeg/builds/ ^(essentials, win-x64^) and place ffmpeg.exe in installer\ffmpeg\.
  exit /b 1
)
if not exist %ISCC% (
  echo ERROR: Inno Setup 6 not found at %ISCC%. Install from https://jrsoftware.org/isinfo.php
  exit /b 1
)

echo [2/3] Running dotnet publish ^(self-contained x64^)...
pushd "%REPO_ROOT%"
dotnet publish ASLTv1.0.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
if errorlevel 1 (
  echo ERROR: dotnet publish failed.
  popd
  exit /b 1
)
popd

echo [3/3] Compiling Inno Setup script...
%ISCC% "%~dp0ASLT-Setup.iss"
if errorlevel 1 (
  echo ERROR: ISCC compile failed.
  exit /b 1
)

echo.
echo BUILD SUCCESS.
echo Installer: %~dp0Output\ASLT-Setup-v1.0.0.exe
endlocal
