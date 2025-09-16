@echo off
setlocal

set SLN=%~dp0src\git-istage.sln
set CONFIG=release

dotnet build %SLN% -c %CONFIG% /nologo
