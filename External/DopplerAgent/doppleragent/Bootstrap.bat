@echo off 
echo "Starting the utility to configure Doppler Agent for this Service" >> "%~dp0DopplerAgent_install_details.log"

Powershell.exe -Command "Set-ExecutionPolicy RemoteSigned" 2>> "%~dp0DopplerAgent_install_err.log"
Powershell.exe -Command "& '%~dp0SetupDopplerAgent_Updater.ps1'" >> "%~dp0DopplerAgent_install_details.log" 2>> "%~dp0DopplerAgent_install_err.log"

echo "done" >"%ROLEROOT%\startup.task.done.sem"
