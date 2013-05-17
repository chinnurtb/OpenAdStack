@echo off
setlocal

call %~dp0build\buildall.cmd Debug Integration Package NoFxCop NoXmlDoc CleanSql DeploySql CreateAdmin CreateCompany SqlServer .\SQLExpress

endlocal
