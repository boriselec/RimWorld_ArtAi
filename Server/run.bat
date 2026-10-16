@echo off
rem Starts the AI art local server. Arguments are passed to artai.py.
cd /d "%~dp0"
set "UV=%USERPROFILE%\.local\bin\uv.exe"
if not exist "%UV%" set "UV=uv"
"%UV%" run artai.py %*
if errorlevel 1 pause
