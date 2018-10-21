@echo off
setlocal

set SLN=%~dp0src\git-istage.sln
set BIN=%~dp0bin\

dotnet build %SLN% -o=%BIN% /nologo
