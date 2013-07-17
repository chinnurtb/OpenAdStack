@echo off
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: Finds all source files containing alert log messages
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

setlocal
set PATTERN=%TEMP%\%RANDOM%.regex
echo LogManager.Log\([\n\r\t ]*LogLevels\.[a-zA-Z]+,[\n\r\t ]*true,>%PATTERN%
call RegExSearch /pf %PATTERN%
del /f %PATTERN%
endlocal
