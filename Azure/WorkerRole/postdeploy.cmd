:: Skip if running in the emulator
if /i "%EMULATED%"=="true" goto :EOF

:: Deploy the Visual Studio 2010 CRT (required for OpenSSL)
vcredist_x64.exe /q /norestart
