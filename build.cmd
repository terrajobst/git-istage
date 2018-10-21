@echo off
setlocal

set SLN=%~dp0src\git-istage.sln
set BIN=%~dp0bin\
set CONFIG=release

dotnet build %SLN% -c %CONFIG% -o=%BIN% /nologo
