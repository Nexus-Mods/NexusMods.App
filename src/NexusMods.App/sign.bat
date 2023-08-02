echo %~f0%

set "scriptPath=..\..\scripts\sign.ps1"

powershell.exe -ExecutionPolicy Bypass -File "%scriptPath%"
