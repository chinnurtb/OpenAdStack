@echo off
setlocal

set CONFIGURATION=Release
set TARGETPROFILE=Local
call %~dp0build\buildall.cmd %1 %2 %3 %4 %5 %6 %7 %8 %9

endlocal
