@echo off

:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: WARNING: MAKE SURE EVERYTHING YOU CARE ABOUT IS IN YOUR PENDING CHANGES ::
:: WHEN IN DOUBT, MAKE A .ZIP BEFORE RUNNING THIS!                         ::
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

setlocal

::::::::::::::::::::::::::::::
:: Defaults
::::::::::::::::::::::::::::::
:: Do not hide ignored files in status ::
set svnargs=--no-ignore --depth=immediates
:: Default to current directory ::
set dir="%CD%"
:: Default to verbose ::
set quiet=false

::::::::::::::::::::::::::::::
:: Parse arguments
::::::::::::::::::::::::::::::
:ParseArgs
if /i not '%1'=='' (
    :: Specify directory to purge (default is current directory)
    if /i '%1'=='-d' (
        set dir=%2
        shift /1
    )
    :: Do not output trace messages
    if /i '%1'=='-q' (
        set quiet=true
    )
    :: Display usage message
    if /i '%1'=='--help' goto Usage
    if /i '%1'=='-?' goto Usage

    shift /1
    goto ParseArgs
)

:: Seek and destroy all unversioned and ignore files in the specified directory ::
pushd %dir%
for /f %%i in ('dir/s/b/a:d ^| find /v ".svn"') do (
    for /f "tokens=1,*" %%j in ('svn status %svnargs% "%%i" 2^>NUL') do (
        if "%%j"=="I" (
            call :Purge "%%k" %%j
        ) else if "%%j"=="?" (
            call :Purge "%%k" %%j
        )
    )
)
popd
endlocal
goto EOF

:Usage
echo Purge all non-versioned files
echo %~n0 [-d path] [-q]
echo    -d  Directory to purge
echo    -q  Quiet
goto EOF

:Purge
setlocal ENABLEDELAYEDEXPANSION
if /i %quiet%==false echo Purging %1 (%2)
if exist %1\ (
    :: Remove directories only if they do not contain versioned files ::
    set empty=true
    for /f %%i in ('svn ls %1 2^>NUL') do set empty=false
    if !empty!==true (
        rmdir /q /s %1
    ) else (
        if /i %quiet%==false echo    Skipping: Contains versioned files.
    )
) else if exist %1 (
    :: Force delete unversioned/ignored files ::
    del /f %1 2>NUL
)
endlocal

:EOF
