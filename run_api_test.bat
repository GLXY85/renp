@echo off
echo Компиляция и запуск тестера API для RENP
echo.

echo Компиляция проекта...
dotnet build ApiTester.csproj -c Release
if %errorlevel% neq 0 (
  echo Ошибка при компиляции проекта!
  pause
  exit /b %errorlevel%
)

echo.
echo Запуск тестера API...
echo =====================
echo.
dotnet run --project ApiTester.csproj -c Release

pause 