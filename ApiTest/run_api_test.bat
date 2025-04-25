@echo off
echo Компиляция и запуск тестера API для RENP
echo.

echo Компиляция проекта...
dotnet build ApiTest.csproj -c Release
if %errorlevel% neq 0 (
  echo Ошибка при компиляции проекта!
  pause
  exit /b %errorlevel%
)

echo.
echo Запуск тестера API...
echo =====================
echo.
dotnet run --project ApiTest.csproj -c Release

pause 