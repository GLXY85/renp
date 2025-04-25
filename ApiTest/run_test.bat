@echo off
chcp 65001 > nul
echo Compiling and running poe2scout.com API tests

REM Compile the project
echo Compiling project...
dotnet build ApiTest.csproj -c Release
if %ERRORLEVEL% NEQ 0 (
    echo Error during compilation.
    pause
    exit /b %ERRORLEVEL%
)

REM Run the test
echo.
echo Running API test...
dotnet run --project ApiTest.csproj -c Release

pause 