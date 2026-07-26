@echo off
setlocal
cd /d "%~dp0"
if not exist "FlightCompanion\lib\Microsoft.FlightSimulator.SimConnect.dll" (
  echo ERREUR : Microsoft.FlightSimulator.SimConnect.dll est absente.
  echo Copie-la dans FlightCompanion\lib\
  pause
  exit /b 1
)
where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERREUR : installe le SDK .NET 8 x64.
  pause
  exit /b 1
)
dotnet publish FlightCompanion\FlightCompanion.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
if errorlevel 1 (
  echo La compilation a echoue.
  pause
  exit /b 1
)
set "SOURCE=%~dp0FlightCompanion\bin\Release\net8.0-windows\win-x64\publish\FlightCompanion.exe"
set "DEST=%USERPROFILE%\Desktop\Flight Companion v0.2.exe"
copy /Y "%SOURCE%" "%DEST%" >nul
echo Termine : %DEST%
pause
