@echo off

set PROJECT=src/git-istage.sln

:: Create tag
dotnet nbgv tag -p %PROJECT% >nul 2>&1

:: Get the tag we just created
for /f "tokens=* USEBACKQ" %%f in (`dotnet nbgv get-version -p %PROJECT% -v NuGetPackageVersion`) do (
	set TAG_NAME=v%%f
)

:: Push tag
git push origin "%TAG_NAME%"
