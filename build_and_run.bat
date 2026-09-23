@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

rem Путь к Unity можно переопределить переменной окружения UNITY_PATH
if "%UNITY_PATH%"=="" set "UNITY_PATH=C:\Program Files\Unity\Hub\Editor\6000.0.77f1\Editor\Unity.exe"

if not exist "%UNITY_PATH%" (
    echo [!] Не найден Unity: "%UNITY_PATH%"
    echo     Укажи путь: set UNITY_PATH=C:\...\Unity.exe
    pause
    exit /b 1
)

echo === Колыбельная для Эха: сборка ===
echo (закрой проект в редакторе Unity, иначе сборка не стартанёт)
if not exist Build mkdir Build
"%UNITY_PATH%" -batchmode -nographics -quit -projectPath "%~dp0." -executeMethod ProjectSetup.Build -logFile "%~dp0Build\build.log"
if errorlevel 1 (
    echo.
    echo [!] Сборка упала. Ошибки:
    findstr /C:"error CS" /C:"Build result" /C:"another Unity instance" "Build\build.log"
    echo Полный лог: Build\build.log
    pause
    exit /b 1
)

echo === Сборка готова, запускаю ===
start "" "Build\LullabyOfEchoes.exe"
endlocal
