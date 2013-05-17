@echo off

if '%1'=='' goto Usage
if '%2'=='' goto Usage

set CS=%1
set StoreName=%2
set BlobList=%TEMP%\%RANDOM%.txt
set BlobDir=%~dp0\blobs
set ErrorLog=%~dp0\errors.log
if exist %BlobDir% rmdir /q /s %BlobDir%
mkdir %BlobDir%

dview -cs "%CS%" -t cloud -s "%StoreName%" -c list -q >%BlobList% 2>>%ErrorLog%
if errorlevel 1 (
    echo ERROR ^(%errorlevel%^): Unable to get blob list.
    echo.
    type %BlobList%
    goto End
)

for /f "delims=" %%i in (%BlobList%) do (
  echo [%DATE% %TIME%] Converting "%%i"
  dview -cs "%CS%" -t cloud -s "%StoreName%" -c get -k "%%i" -o "%BlobDir%\%%i.xml" 2>>%ErrorLog%
  base64 -dec -xmlser -i "%BlobDir%\%%i.xml" -o "%BlobDir%\%%i.bin" 2>>%ErrorLog%
  if errorlevel 1 (
    echo ERROR: Unable to deserialize "%%i" from base64
  ) else (
    dview -cs "%CS%" -t cloud -s "%StoreName%" -c set -k "%%i" -i "%BlobDir%\%%i.bin" 2>>%ErrorLog%
    echo Converted "%%i"
    del /f /q "%BlobDir%\%%i.*" 2>>%ErrorLog%
  )
  echo.
)
goto End

:Usage
echo Usage: %~n0 "ConnectionString" "StoreName"
echo Example: %~n0 "UseDevelopmentStorage=true" "entityblobassociations"
echo.

:End
del /q /f %BlobList% 2>NUL
echo.>>%ErrorLog%
echo.>>%ErrorLog%
