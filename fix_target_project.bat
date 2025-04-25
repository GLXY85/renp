@echo off
echo Создание проектного файла в целевой директории...

if not exist "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\" (
    mkdir "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\"
    echo Создана директория renp
)

echo Копирование библиотек...
call copy_libs.bat

echo Создание проектного файла...
echo ^<Project Sdk="Microsoft.NET.Sdk"^> > "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo   ^<PropertyGroup^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<TargetFramework^>net8.0-windows^</TargetFramework^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<OutputType^>Library^</OutputType^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<AssemblyName^>RENP^</AssemblyName^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<UseWindowsForms^>true^</UseWindowsForms^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<UseWPF^>true^</UseWPF^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<PlatformTarget^>x64^</PlatformTarget^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<DebugType^>embedded^</DebugType^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<GenerateAssemblyInfo^>false^</GenerateAssemblyInfo^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo   ^</PropertyGroup^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo   ^<ItemGroup^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<Reference Include="ExileCore2"^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo       ^<HintPath^>ExileCore2.dll^</HintPath^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^</Reference^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<Reference Include="ImGui.NET"^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo       ^<HintPath^>ImGui.NET.dll^</HintPath^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^</Reference^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<Reference Include="SharpDX"^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo       ^<HintPath^>SharpDX.dll^</HintPath^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^</Reference^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<Reference Include="SharpDX.Mathematics"^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo       ^<HintPath^>SharpDX.Mathematics.dll^</HintPath^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^</Reference^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<Reference Include="System.Windows.Forms" /^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<Reference Include="PresentationCore" /^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<Reference Include="PresentationFramework" /^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<Reference Include="System.Xaml" /^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<Reference Include="WindowsBase" /^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo   ^</ItemGroup^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo   ^<ItemGroup^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<PackageReference Include="morelinq" Version="4.0.0" /^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<PackageReference Include="Newtonsoft.Json" Version="13.0.3" /^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<PackageReference Include="System.Text.Json" Version="8.0.0" /^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo   ^</ItemGroup^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo   ^<ItemGroup^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<None Update="cimgui.dll"^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo       ^<CopyToOutputDirectory^>PreserveNewest^</CopyToOutputDirectory^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^</None^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<None Update="ExileCore2.dll"^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo       ^<CopyToOutputDirectory^>PreserveNewest^</CopyToOutputDirectory^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^</None^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<None Update="ImGui.NET.dll"^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo       ^<CopyToOutputDirectory^>PreserveNewest^</CopyToOutputDirectory^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^</None^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<None Update="SharpDX.dll"^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo       ^<CopyToOutputDirectory^>PreserveNewest^</CopyToOutputDirectory^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^</None^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^<None Update="SharpDX.Mathematics.dll"^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo       ^<CopyToOutputDirectory^>PreserveNewest^</CopyToOutputDirectory^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo     ^</None^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo   ^</ItemGroup^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"
echo ^</Project^> >> "C:\Users\GLXY\Desktop\ExileCore2-15\Plugins\Source\renp\renp.csproj"

echo Проектный файл успешно создан!
echo Пожалуйста, откройте Visual Studio и используйте этот новый проектный файл. 