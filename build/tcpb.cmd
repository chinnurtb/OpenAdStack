@echo off

::::::::::::::::::::::::::::::
:: Set local environment
::::::::::::::::::::::::::::::
setlocal enabledelayedexpansion

set TCCONFIG=%~dp0..\..\..\LucyConfig\TeamCity
for /f "tokens=1 delims=" %%i in (%TCCONFIG%\settings.txt) do set %%i

if '%TCCJAR%'=='' set TCCJAR=%~dp0TeamCity\tcc.jar
if '%MAPPING%'=='' set MAPPING=%~dp0..\.teamcity-mappings.properties
if '%CONFIGLIST%'=='' set CONFIGLIST=%TCCONFIG%\configurations.txt
if '%FILELIST%'=='' set FILELIST="%TEMP%\.%RANDOM%.tcdiff"

::::::::::::::::::::::::::::::
:: Parse arguments
::::::::::::::::::::::::::::::
if '%1'=='' goto Help

:ParseArgs
if not '%1'=='' (
  if /i '%1'=='Info' set INFO=True
  if /i '%1'=='Login' set LOGIN=True
  if /i '%1'=='Logout' set LOGOUT=True
  if /i '%1'=='Build' set BUILD=True
  if /i '%1'=='-c' (
    set CONFIGNAME=%2
    shift /1
  )
  if /i '%1'=='-m' (
    set MESSAGE=%2
    shift /1
  )
  if '%1'=='/?' goto Help 
  if '%1'=='-?' goto Help
  if /i '%1'=='/help' goto Help
  if /i '%1'=='--help' goto Help
  shift /1
  goto ParseArgs
)

:: Check that at least one valid function was specified
if '%INFO%%LOGIN%%LOGOUT%%BUILD%'=='' goto Help

::::::::::::::::::::::::::::::
:: Find Java
::::::::::::::::::::::::::::::
if '%JAVA%'=='' for %%i in (java.exe) do if exist "%%~$PATH:i" set JAVA=%%~$PATH:i
:: If JAVA not defined and java.exe not found in path, try JAVA_HOME
if not exist "%JAVA%" if not '%JAVA_HOME%'=='' if exist "%JAVA_HOME%" (
  if exist "%JAVA_HOME%\bin\java.exe" (
    set JAVA="%JAVA_HOME%\bin\java.exe"
  ) else (
    if exist "%JAVA_HOME%\java.exe" set JAVA="%JAVA_HOME%\java.exe"
  )
)
:: Check if java was found
if not exist "%JAVA%" (
  echo Error: Unable to locate java. Please add it to your path or set JAVA_HOME.
  goto End
)

::::::::::::::::::::::::::::::
:: Login and logouts
::::::::::::::::::::::::::::::
if /i '%INFO%'=='True' (
	echo   Server: %TEAMCITYURL%
	echo java.exe: %JAVA%
	echo  tcc.jar: %TCCJAR%
	echo.
)

::::::::::::::::::::::::::::::
:: Login and logouts
::::::::::::::::::::::::::::::
if /i '%LOGOUT%'=='True' (
  %JAVA% -jar %TCCJAR% logout --host %TEAMCITYURL%
  echo.
)
if /i '%LOGIN%'=='True' (
  %JAVA% -jar %TCCJAR% login --host %TEAMCITYURL% --username %USERNAME%
  echo.
)

::::::::::::::::::::::::::::::
:: Submitting private builds
::::::::::::::::::::::::::::::
if /i '%BUILD%'=='True' (
  :: Check that a message was provided
  if '%MESSAGE%'=='' goto Help

  :: Get configuration
  if '%CONFIG%'=='' (
    if not '%CONFIGNAME%'=='' (
      :: Find a matching config
      for /f "tokens=1,2 delims==" %%i in (%CONFIGLIST%) do (
        if /i '%CONFIGNAME%'=='%%i' (
          set CONFIG=%%j
        )
      )
    )

    :: Check that a config was found
    if '!CONFIG!'=='' (
      if '%CONFIGNAME%'=='' (
        echo Error: Missing configuration
      ) else (
        echo Error: Invalid configuration "%CONFIGNAME%"
      )
      echo.
      goto Help
    )
  )

  :: Create a list of changed files (excluding unversioned and ignored)
  if exist %FILELIST% del /q /f %FILELIST%
  for /f "tokens=1,2" %%i in ('svn st') do if not "%%i"=="?" if not "%%i"=="I" echo %%j >> %FILELIST%


  :: Submit the build
  echo Submitting private build to %TEAMCITYURL% for configuration "%CONFIGNAME%" ^(!CONFIG!^)
  echo.
  %JAVA% -jar %TCCJAR% run -c !CONFIG!  -m %MESSAGE% -n %MAPPING% @%FILELIST%

  :: Cleanup the file list
  del /q /f %FILELIST%
)

goto End

::::::::::::::::::::::::::::::
:: Help Message
::::::::::::::::::::::::::::::
:Help
echo Submits private builds to TeamCity using the TeamCity java client.
echo.
echo Usage: %~n0 [login ^| logout ^| build -c config -m "message"]
echo   info          Displays TeamCity server and client information.
echo   login         Log into TeamCity. Credentials are cached until logout.
echo   logout        Clear cached credentials. You will need to login again before building.
echo   build         Submit a private build. Requires -c and -m.
echo    -m "message" Specifies a message for the private build.
echo    -c config    Specifies a configuration to build. Must be one of the following:
for /f "tokens=1 delims==" %%i in (%CONFIGLIST%) do echo                    %%i

:End
endlocal
