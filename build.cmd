@echo off
setlocal

:: Find MSBuild to use

for /f "tokens=*" %%i in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -property installationPath') do set VSPATH=%%i
set MSBUILD_PATH=%VSPATH%\MSBuild\15.0\Bin\MSBuild.exe
set BIN_PATH=%~dp0bin

:: Run build

if not exist %BIN_PATH% mkdir %BIN_PATH%
"%MSBUILD_PATH%" /nologo /m /v:m /nr:false /bl:%BIN_PATH%\msbuild.binlog %*