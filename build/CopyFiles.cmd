@echo off

setlocal enabledelayedexpansion

set SOLUTIONDIR=%1
set PROJECTDIR=%2
set OUTDIR=%3

set FILES_TO_COPY=%PROJECTDIR%\FilesToCopy.txt
set XCOPYFLAGS=/EVIFHY

pushd %SOLUTIONDIR%

if exist "%OUTDIR%" rmdir /q /s "%OUTDIR%"

for /f "eol=# tokens=1,2 delims=^|" %%i in (%FILES_TO_COPY%) do (
  set DEST=%OUTDIR%
  if not "%%j"=="" set DEST=%OUTDIR%\%%j
  if not exist "!DEST!" mkdir "!DEST!"
  xcopy %XCOPYFLAGS% "%%i" "!DEST!"
)

popd
endlocal
