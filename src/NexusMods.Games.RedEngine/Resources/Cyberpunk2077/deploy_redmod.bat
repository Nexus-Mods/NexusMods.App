@echo off

:: This script works with protontricks versions >= 1.8.0
:: And also handles the breaking change in 1.12.0 (unreleased).script

:: In 1.12.0, protontricks will respect the current working directory,
:: while currently it does not. This is a breaking change. In order
:: to handle both < 1.12.0 and >= 1.12.0, we instead use the STEAM_APP_PATH
:: variable. After that we set the working directory ourselves.

rem Check if STEAM_APP_PATH environment variable is set
if "%STEAM_APP_PATH%"=="" (
    echo Error: STEAM_APP_PATH environment variable is not set.
    echo Please make sure to run this script through protontricks-launch.
    pause
    exit /b 1
)

rem Navigate to the STEAM_APP_PATH directory
cd /d "%STEAM_APP_PATH%"

rem Change to the directory containing redMod.exe
cd tools\redmod\bin

rem Check if redMod.exe exists in the current directory
if not exist "redMod.exe" (
    echo Error: redMod.exe not found in the current directory.
    echo Make sure the STEAM_APP_PATH is set correctly and the redmod folder structure is intact.
    pause
    exit /b 1
)

echo Launching redMod.exe with deploy parameter...
redMod.exe %*
