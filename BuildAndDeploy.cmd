@echo off
setlocal

call %~dp0build\buildall.cmd Cloud Release Deploy Production RunPostDeploymentTests %1 %2 %3 %4 %5 %6 %7 %8 %9

endlocal