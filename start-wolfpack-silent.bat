@echo off
REM Silent auto-start script for Windows Startup folder
REM This runs in the background without showing windows

REM Check if Docker Desktop is running
docker info >nul 2>&1
if %errorlevel% neq 0 (
    start "" "C:\Program Files\Docker\Docker\Docker Desktop.exe"
    timeout /t 45 /nobreak >nul
)

REM Start WolfPackAI Services (minimized)
start /min "WolfPackAI Services" cmd /c "cd /d %~dp0 && dotnet run --project WolfPackAI.AppHost"
timeout /t 20 /nobreak >nul

REM Start Gatekeeper (minimized)
start /min "WolfPackAI Gatekeeper" cmd /c "cd /d %~dp0 && dotnet run --project WolfPackAI.Gatekeeper"

exit
