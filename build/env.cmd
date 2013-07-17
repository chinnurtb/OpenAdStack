@echo off

::
:: Setup VS Environment
::
call "%VS100COMNTOOLS%\..\..\VC\vcvarsall.bat" x86

::
:: Add build directory to path
::
set PATH=%PATH%;%~dp0;%~dp0..\Public\bin\Release;%~dp0..\Public\bin\Debug;

::
:: Chdir to the root of the branch
::
cd %~dp0\..

::
:: Set branch environment variable
::
for %%a in (%CD%) do set BRANCH=%%~nxa
echo Current branch is "%BRANCH%"

:: Check for svn.exe in the path and if not found try adding TortoiseSVN's bin folder (if it exists)
:: This has to be done in one line because the parentheses in the path will break blocks
for %%i in (svn.exe) do if not exist "%%~$PATH:i" if exist "%ProgramFiles%\TortoiseSVN\bin" set PATH=%PATH%;%ProgramFiles%\TortoiseSVN\bin

:: Echo an error if svn.exe still cannot be found,
:: otherwise display SVN information
for %%i in (svn.exe) do if not exist "%%~$PATH:i" (
  echo ERROR: svn.exe not found in the path!
  echo Install a subversion client and/or add its bin directory to the path.
) else (
  echo Subversion client found at "%%~$PATH:i"
  svn info | find "Working Copy"
  svn info | find "URL: "
)

echo.
