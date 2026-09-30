@echo off
rem MediLink local launcher for Command Prompt and PowerShell.
rem Usage: run.cmd start ^| stop ^| status

if not defined MEDILINK_DB_USERNAME set "MEDILINK_DB_USERNAME=root"
if not defined MEDILINK_DB_PASSWORD set "MEDILINK_DB_PASSWORD=root"
if not defined MEDILINK_JWT_SECRET set "MEDILINK_JWT_SECRET=medilink-development-secret-must-be-32-characters"
if not defined MEDILINK_INTERNAL_KEY set "MEDILINK_INTERNAL_KEY=medilink-internal-development-key"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0run.ps1" %*
