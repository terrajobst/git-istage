@ECHO OFF

if not exist "%~dp0bin" mkdir "%~dp0bin"

"%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe" "%~dp0build.proj" /nologo /m /v:m /nr:false /flp:verbosity=normal;LogFile="%~dp0bin\msbuild.log" %*