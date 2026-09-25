@echo off
REM JARVIS Windows companion agent - one-time install
setlocal

where python >nul 2>nul
if errorlevel 1 (
  echo Python 3.11+ is required. Install it from https://www.python.org/downloads/windows/ and tick "Add python.exe to PATH".
  pause
  exit /b 1
)

echo Creating virtual environment...
python -m venv "%~dp0.venv"
if errorlevel 1 (
  echo Failed to create the virtual environment.
  pause
  exit /b 1
)

echo Installing dependencies...
"%~dp0.venv\Scripts\python.exe" -m pip install --upgrade pip
"%~dp0.venv\Scripts\python.exe" -m pip install -r "%~dp0requirements.txt"
if errorlevel 1 (
  echo Dependency install failed.
  pause
  exit /b 1
)

echo.
echo Install complete. Next: open JARVIS - MENU - Computer, press "Pair agent",
echo then run run.bat and paste the 6-digit pairing code.
pause
