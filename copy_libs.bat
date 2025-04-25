@echo off
echo Копирование библиотек в целевую директорию...

if not exist "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\" (
    mkdir "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\"
    echo Создана директория renp
)

if not exist "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\bin\Debug\" (
    mkdir "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\bin\Debug\"
    echo Создана директория bin\Debug
)

if not exist "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\bin\Release\" (
    mkdir "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\bin\Release\"
    echo Создана директория bin\Release
)

if not exist "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\obj\" (
    mkdir "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\obj\"
    echo Создана директория obj
)

echo Копирование в корневую директорию...
copy /Y "%~dp0libs\ExileCore2.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\ExileCore2.dll"
copy /Y "%~dp0libs\ImGui.NET.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\ImGui.NET.dll"
copy /Y "%~dp0libs\SharpDX.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\SharpDX.dll"
copy /Y "%~dp0libs\SharpDX.Mathematics.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\SharpDX.Mathematics.dll"
copy /Y "%~dp0libs\cimgui.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\cimgui.dll"

echo Копирование в Debug...
copy /Y "%~dp0libs\ExileCore2.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\bin\Debug\ExileCore2.dll"
copy /Y "%~dp0libs\ImGui.NET.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\bin\Debug\ImGui.NET.dll"
copy /Y "%~dp0libs\SharpDX.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\bin\Debug\SharpDX.dll"
copy /Y "%~dp0libs\SharpDX.Mathematics.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\bin\Debug\SharpDX.Mathematics.dll"
copy /Y "%~dp0libs\cimgui.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\bin\Debug\cimgui.dll"

echo Копирование в Release...
copy /Y "%~dp0libs\ExileCore2.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\bin\Release\ExileCore2.dll"
copy /Y "%~dp0libs\ImGui.NET.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\bin\Release\ImGui.NET.dll"
copy /Y "%~dp0libs\SharpDX.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\bin\Release\SharpDX.dll"
copy /Y "%~dp0libs\SharpDX.Mathematics.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\bin\Release\SharpDX.Mathematics.dll"
copy /Y "%~dp0libs\cimgui.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\bin\Release\cimgui.dll"

echo Копирование в obj...
copy /Y "%~dp0libs\ExileCore2.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\obj\ExileCore2.dll"
copy /Y "%~dp0libs\ImGui.NET.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\obj\ImGui.NET.dll"
copy /Y "%~dp0libs\SharpDX.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\obj\SharpDX.dll"
copy /Y "%~dp0libs\SharpDX.Mathematics.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\obj\SharpDX.Mathematics.dll"
copy /Y "%~dp0libs\cimgui.dll" "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\obj\cimgui.dll"

echo.
echo Копирование завершено. Запустите сборку заново. 