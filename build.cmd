@echo off
setlocal

set SLN=%~dp0src\git-istage.slnx
set CONFIG=release

dotnet build %SLN% -c %CONFIG% /nologo
