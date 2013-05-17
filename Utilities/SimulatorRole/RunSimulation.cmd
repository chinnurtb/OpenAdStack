@echo off
setlocal
set AppRoot=E:\approot
set SimExe=%AppRoot%\AllocationSimulator.exe
set Output=%USERPROFILE%\Desktop\Results

pushd %AppRoot%
echo Running: %SimExe% -i %1 -o %Output%
echo.
%SimExe% -i %1 -o %Output%
popd

pause
endlocal