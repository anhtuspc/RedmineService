@echo off
set SERVICE_NAME=WAR_Redmine
set EXE_PATH=D:\Program\ARR\RedmineMonitorService.exe

:: Check for administrative privileges
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running with administrative privileges...
) else (
    echo Failure: Current permissions inadequate.
    echo Please run this script as Administrator.
    pause >nul
    exit /b 1
)

echo Stopping service if running...
sc stop "%SERVICE_NAME%" >nul 2>&1
timeout /t 5 /nobreak >nul

echo Deleting existing service...
sc delete "%SERVICE_NAME%" >nul 2>&1
timeout /t 2 /nobreak >nul

echo Installing service...
:: Ensure binPath is quoted correctly and includes the /log_off argument
sc create "%SERVICE_NAME%" binPath= "\"%EXE_PATH%\" /log_off" start= auto DisplayName= "WAR_Redmine"
if %errorLevel% neq 0 (
    echo Failed to create service. Please check the path and permissions.
    pause
    exit /b 1
)

echo Setting description...
sc description "%SERVICE_NAME%" "WAR_Redmine - Monitors Redmine tickets and generates HTML reports"

echo Starting service...
sc start "%SERVICE_NAME%"

echo Done.
pause
