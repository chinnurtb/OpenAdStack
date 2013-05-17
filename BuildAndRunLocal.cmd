@echo off
setlocal
pushd %~dp0

:ParseArgs
if /i not '%1'=='' (
  if /i '%1'=='InitStorage' set InitStorage=true
  if /i '%1'=='UserId' (
    set DEFAULT_USER_ID=%2
    shift /1
  )

  shift /1
  goto ParseArgs
)

:: Default to the same default user id as the Local target profile
if '%DEFAULT_USER_ID%'=='' (
  set DEFAULT_USER_ID="juwUea/zycXC0/QK7QF/oHkWkk6QTLrfzL3VPyepyjc="
)

:: Setup environment
call build\env.cmd
set CSRun="%ProgramFiles%\Microsoft SDKs\Windows Azure\Emulator\csrun.exe"

:: Shutdown emulator and clean Azure solution
echo Shutting down devstore...
%CSRun% /devstore:shutdown
echo Shutting down devfabric...
%CSRun% /devfabric:shutdown
echo Cleaning devfabric...
%CSRun% /devfabric:clean
echo Purging Azure solution...
pushd Azure
call svnpurge -q
popd
echo.

:: Build
if '%Configuration%'=='' set Configuration=Release
set BuildParams=%Configuration% Integration Package NoFxCop NoXmlDoc
if /i '%InitStorage%'=='true' (
  set BuildParams=%BuildParams% CleanSql DeploySql CreateAdmin CreateCompany SqlServer .\SQLExpress
)
echo Building with parameters: %BuildParams%
call buildall  %BuildParams%
echo.

:: Launch the emulator and then the browser
set AzurePath=%CD%\Azure\Azure
set CsxPath="%AzurePath%\csx\%Configuration%"
set CscfgPath="%AzurePath%\ServiceConfiguration.Integration.cscfg"
echo Starting devstore...
%CSRun% /devstore:start
echo Deploying to devfabric...
%CSRun% /run:%CsxPath%;%CscfgPath%
echo.

echo Launching browser...
start https://localhost/

echo Done.
popd
endlocal
