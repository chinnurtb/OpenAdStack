@echo off
setlocal

call %~dp0build\buildall.cmd Debug Integration Package NoFxCop NoXmlDoc CreateUser

endlocal
