@echo off
setlocal enabledelayedexpansion

REM ============================
REM CONFIG
REM ============================
set CONFIG=Release
set ROOT=%~dp0
set BUILDS=%ROOT%Builds

REM Usage:
REM   build.bat

REM ============================
REM WINDOWS BUILD
REM ============================
echo.
echo ============================
echo Publishing Everything Process Finder (Windows)
echo ============================

dotnet publish "%ROOT%Everything Process Finder\Everything Process Finder.csproj" ^
 -c %CONFIG% ^
 -r win-x64 ^
 --self-contained true ^
 -o "%BUILDS%\Windows"

if errorlevel 1 goto :error

goto :done

REM ============================
REM END
REM ============================
:done
echo.
echo Build complete
echo Output directory:
echo   %BUILDS%
exit /b 0

:error
echo.
echo Build failed
exit /b 1