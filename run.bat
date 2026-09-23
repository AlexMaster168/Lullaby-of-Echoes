@echo off
chcp 65001 >nul
cd /d "%~dp0"
if not exist "Build\LullabyOfEchoes.exe" (
    echo Игра ещё не собрана — запускаю build_and_run.bat
    call build_and_run.bat
    exit /b
)
start "" "Build\LullabyOfEchoes.exe"
