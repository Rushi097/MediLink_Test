@echo off
rem MediLink local launcher for Command Prompt and PowerShell.
rem Usage: run.cmd start ^| stop ^| status

set MEDILINK_DB_USERNAME=root
set MEDILINK_DB_PASSWORD=root
set MEDILINK_JWT_SECRET=medilink-development-secret-must-be-32-characters

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0run.ps1" %*
