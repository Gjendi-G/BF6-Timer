@echo off
setlocal
cd /d "%~dp0"

REM Einmalig eine normale Windows-Desktop-App aus dem sichtbaren C#-Quellcode bauen.
REM Der Windows-.NET-Framework-Compiler ist auf vielen Windows-11-Systemen vorhanden.
if exist "BF6_Timer.exe" goto RUN
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo Fehler: Windows-.NET-Framework-C#-Compiler nicht gefunden.
  echo Bitte .NET Framework 4.x aktivieren oder installieren.
  pause
  exit /b 1
)

echo Erstelle den kleinen Timer (einmalig) ...
"%CSC%" /nologo /target:winexe /optimize+ /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:"%~dp0BF6_Timer.exe" "%~dp0BF6_Timer.cs"
if errorlevel 1 (
  echo Der Bau ist fehlgeschlagen. Die Fehlermeldung steht oben.
  pause
  exit /b 1
)

:RUN
start "" "%~dp0BF6_Timer.exe"
endlocal
