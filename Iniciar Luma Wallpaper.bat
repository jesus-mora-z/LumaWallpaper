@echo off
setlocal
set "APP=%~dp0LumaWallpaper\bin\Release\net10.0-windows10.0.19041.0\LumaWallpaper.exe"
if exist "%APP%" (
    start "" "%APP%"
    exit /b 0
)

set "DOTNET=%LOCALAPPDATA%\dotnet-sdk-luma\dotnet.exe"
if not exist "%DOTNET%" set "DOTNET=dotnet"
"%DOTNET%" run --project "%~dp0LumaWallpaper\LumaWallpaper.csproj" --configuration Release
if errorlevel 1 (
    echo.
    echo No se pudo iniciar Luma Wallpaper. Comprueba que .NET 10 Desktop Runtime este instalado.
    pause
)
