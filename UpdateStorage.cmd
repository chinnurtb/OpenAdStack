@echo off
setlocal

call %~dp0build\buildall.cmd DeploySql CreateCompany CreateAdmin InitStorageOnly %1 %2 %3 %4 %5 %6 %7 %8 %9

endlocal
