@echo off
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: Finds all files containing regex matches and the location of the matches
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

if '%1'=='' goto Usage

setlocal enabledelayedexpansion
set FILES=%CD%\*.cs
set WORKING=%TEMP%\%RANDOM%
set MATCHES=%WORKING%\matches
set MATCHFILES=%WORKING%\matchfiles
set PATTERNFILE=%WORKING%\pattern
mkdir %WORKING%

:ParseArgs
if not '%1'=='' (
  if '%1'=='/p' (
    echo %2>%PATTERNFILE%
    shift /1
  ) else if '%1'=='/pf' (
    set PATTERNFILE=%2
    shift /1
  ) else if '%1'=='/f' (
    set FILES=%2
    shift /1
  )
  shift /1
  goto ParseArgs
)

:: Find files with matches
for /f %%i in ('dir/b/s %FILES% ^| find /v "NUnit" ^| find /v /i "Fixture"') do (
  regex /PatternFile %PATTERNFILE% /Singleline /In "%%i" >%MATCHES%
  set HasMatch=false
  for /f %%q in (%MATCHES%) do set HasMatch=true
  if !HasMatch!==true echo %%i>>%MATCHFILES%
)

:: Find the matches within the files
del %MATCHES%
for /f %%i in (%MATCHFILES%) do (
  echo %%i>>%MATCHES%
  regex /PatternFile pattern.txt /Singleline /Verbose /In "%%i" | find " - index: " >>%MATCHES%
  echo.>>%MATCHES%
)
type %MATCHES%

:: Cleanup
rmdir /q /s %WORKING%
endlocal
goto :eof

:: Display usage message
:Usage
echo Searches files containing regex matches and outputs their locations by file
echo.
echo %~n0 {/p "pattern"^|/pf "PatternFile.txt"} [/f]
echo     /p   The regex pattern for which to search.
echo     /pf  File containing the regex for which to search.
echo          Avoids issues with command-line escaping complex patterns.
echo     /f   Optional. Specifies what file(s) to search.
echo          Default is all *.cs files under the current directory (recursive)
echo.
