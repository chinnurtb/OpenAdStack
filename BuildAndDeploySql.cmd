@echo off
setlocal

set CONFIGURATION=Debug
set RUN_UNIT_TESTS=True
set CONTINUE_ON_TEST_FAILURE=False
set TARGETPROFILE=Local
call %~dp0build\buildall.cmd DeploySql CleanSql CreateCompany CreateAdmin %1 %2 %3 %4 %5 %6 %7 %8 %9

endlocal
