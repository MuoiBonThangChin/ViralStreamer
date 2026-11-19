@echo off
setlocal enabledelayedexpansion
rem ===========================================================
rem Android APK/AAB build (Unity batchmode). Close the Editor first.
rem
rem Usage:  build-android.bat [apk|aab] [versionName] [development]
rem   build-android.bat apk
rem   build-android.bat aab 1.0.1
rem   build-android.bat apk 1.0.1 development
rem
rem Signing (optional, else debug key). Set env vars before running:
rem   set KEYSTORE_PATH=C:\keys\my.keystore
rem   set KEYSTORE_PASS=...   set KEY_ALIAS=...   set KEY_PASS=...
rem Optional: set UNITY_EXE to override the Unity path.
rem ===========================================================

set "KIND=%~1"
if "%KIND%"=="" set "KIND=apk"
set "VERSION=%~2"
set "DEV=%~3"

set "PROJECT=%~dp0..\.."
pushd "%PROJECT%"
set "PROJECT=%CD%"
popd

if "%UNITY_EXE%"=="" set "UNITY_EXE=G:\Unity\6000.0.73f1\Editor\Unity.exe"
if not exist "%UNITY_EXE%" set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.0.73f1\Editor\Unity.exe"
if not exist "%UNITY_EXE%" (
  echo Unity 6000.0.73f1 not found. Set UNITY_EXE to Unity.exe.
  exit /b 2
)

set "EXTRA="
if not "%VERSION%"=="" set "EXTRA=!EXTRA! -versionName %VERSION%"
if /i "%DEV%"=="development" set "EXTRA=!EXTRA! -development"

if not exist "%PROJECT%\Builds" mkdir "%PROJECT%\Builds"
set "LOG=%PROJECT%\Builds\build-%KIND%.log"

echo Building %KIND% ... log: %LOG%
"%UNITY_EXE%" -batchmode -nographics -quit -projectPath "%PROJECT%" ^
  -buildTarget Android ^
  -executeMethod Game.EditorTools.AndroidBuildTool.BuildFromCommandLine ^
  -buildType %KIND% -outDir Builds%EXTRA% -logFile "%LOG%"
set "RC=%ERRORLEVEL%"

if %RC%==0 (echo Build OK. Output in %PROJECT%\Builds) else (echo Build FAILED ^(code %RC%^). See %LOG%)
exit /b %RC%
