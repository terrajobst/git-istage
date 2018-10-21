@echo off

dotnet build %~dp0src\git-istage.sln /p:OutDir=%~dp0bin /nologo