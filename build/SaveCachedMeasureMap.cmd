@echo off
setlocal

:: Parse command-line parameters
:ParseArgs
if /i not '%1'=='' (
  if /i '%1'=='-q' set TRACE=rem
  if /i '%1'=='-cs' (
    set CS=%2
    shift /1
  )
  if /i '%1'=='-o' (
    set OUTPUT=%2
    shift /1
  )
  if /i '%1'=='-account' (
    set ACCOUNT=%2
    shift /1
  )
  if /i '%1'=='-network' (
    set NETWORK=%2
    shift /1
  )
  shift /1
  goto ParseArgs
)

:: Private Configuration
:: Default private config located in separate SVN alongside Lucy
if '%PRIVATECONFIG%'=='' set PRIVATECONFIG=%~dp0..\..\..\LucyConfig\
:: Load settings from private config
for /f "tokens=1 delims=" %%i in (%PRIVATECONFIG%\settings.txt) do set settings.%%i
:: Specific purpose config paths
if '%SQLUSERPWD%'=='' set SQLUSERPWD=%settings.SQLUSERPWD%

:: Default configuration
if '%TRACE%'=='' set TRACE=echo
if '%OUTPUT%'=='' set OUTPUT="%CD%\MeasureMap.js"
if '%NETWORK%'=='' set NETWORK=apnx
if '%ACCOUNT%'=='' set ACCOUNT=2131
if '%CS%'=='' set CS="Data Source=.\SQLEXPRESS;Initial Catalog=DictionaryStore;Integrated Security=False;User ID=lucyAppUser;Password=%SQLUSERPWD%"

:: Display config
%TRACE% Saving cached measures as: %OUTPUT%
%TRACE% Network: %NETWORK%
%TRACE% Account: %ACCOUNT%
%TRACE% SQL: %CS%
%TRACE%.

:: exe macros
set DLIST=dview -q -cs %CS% -c list -s measurecache
set DGET=dview -q -cs %CS% -c view -s measurecache -k
set REGEX=regex /in %OUTPUT% /out %OUTPUT%

:: Start the output file
echo {>%OUTPUT%

:: Get the "offline" measures (not specific to any account)
%TRACE% Downloading "offline" measures for network %NETWORK%:
for /f "delims=" %%i in ('%DLIST% ^| find "-offline"') do (
  %TRACE%    %%i...
  %DGET% "%%i">>%OUTPUT%
)
%TRACE%.

:: Get the account measures
%TRACE% Downloading account "%ACCOUNT%" measures for network %NETWORK%:
for /f "delims=" %%i in ('%DLIST% ^| find "-%ACCOUNT%["') do (
  %TRACE%    %%i...
  %DGET% "%%i">>%OUTPUT%
)
%TRACE%.

:: Clean up the formatting (remove enclosing XML, add closing }, etc)
%TRACE% Formatting measure map:
%TRACE%     Remove opening XML tags...
%REGEX% /pattern "<MeasureMapCacheEntry.*MeasureMapJson>{" /replace ""
%TRACE%     Remove closing XML tags...
%REGEX% /pattern "}</MeasureMapJson></MeasureMapCacheEntry>" /replace ","
%TRACE%     Add closing brace...
echo }>>%OUTPUT%
%REGEX% /pattern "},\r\n}" /replace "}}" /singleline
%TRACE%     Adding line breaks...
set REPLACE=%TEMP%\replace.%RANDOM%
echo },>%REPLACE%
%REGEX% /pattern "}," /replacefile %REPLACE% /notrim
echo.>%REPLACE%
%REGEX% /pattern "\r\n\r\n" /replacefile %REPLACE% /notrim
del %REPLACE%
%REGEX% /pattern "{\r\n" /replace "{" /singleline
%TRACE% Done.
%TRACE%.

:: Summary message
for /f "tokens=2,3 delims=:" %%a in ('find /c """:{" %OUTPUT%') do if '%%b'=='' (set /a MEASURES=%%a) else (set /a MEASURES=%%b)
%TRACE% Created measure map %OUTPUT% containing %MEASURES% measures.
%TRACE%.

endlocal
