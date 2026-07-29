@echo off
rem MediLink local launcher for Command Prompt and PowerShell.
rem Usage: run.cmd start ^| stop ^| status

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0run.ps1" %*
