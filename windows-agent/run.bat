@echo off
REM JARVIS Windows companion agent - start and pair
setlocal

if "%JARVIS_BACKEND_URL%"=="" (
  set /p JARVIS_BACKEND_URL=JARVIS backend URL (https://...):
)

if "%JARVIS_PAIR_CODE%"=="" (
  set /p JARVIS_PAIR_CODE=Pairing code from JARVIS - Computer:
)

"%~dp0.venv\Scripts\python.exe" "%~dp0jarvis_agent.py" --backend "%JARVIS_BACKEND_URL%" --code "%JARVIS_PAIR_CODE%"
pause
