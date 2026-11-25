param(
    [string]$InnoSetupCompiler
)

$ErrorActionPreference = 'Stop'

$projectRoot = $PSScriptRoot
$projectFile = Join-Path $projectRoot 'LumaWallpaper\LumaWallpaper.csproj'
$installerFile = Join-Path $projectRoot 'installer\LumaWallpaper.iss'
$publishDirectory = Join-Path $projectRoot 'artifacts\publish'
$outputDirectory = Join-Path $projectRoot 'dist'

if (-not (Test-Path $projectFile)) {
    throw "No se encontro el proyecto: $projectFile"
}

[xml]$project = Get-Content $projectFile -Raw
$version = $project.Project.PropertyGroup.Version
$ffmpegVersion = $project.Project.PropertyGroup.FfmpegPackageVersion
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'El proyecto no declara una version de publicacion.'
}
if ([string]::IsNullOrWhiteSpace($ffmpegVersion)) {
    throw 'El proyecto no declara la version del paquete FFmpeg.'
}

$dotnet = Join-Path $env:LOCALAPPDATA 'dotnet-sdk-luma\dotnet.exe'
if (-not (Test-Path $dotnet)) {
    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($null -eq $dotnetCommand) {
        throw 'No se encontro dotnet. Instala el SDK de .NET 10 para generar el instalador.'
    }

    $dotnet = $dotnetCommand.Source
}

if ([string]::IsNullOrWhiteSpace($InnoSetupCompiler)) {
    $InnoSetupCompiler = (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
}

if ([string]::IsNullOrWhiteSpace($InnoSetupCompiler)) {
    foreach ($candidate in @(
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe')
    )) {
        if (Test-Path $candidate) {
            $InnoSetupCompiler = $candidate
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($InnoSetupCompiler) -or -not (Test-Path $InnoSetupCompiler)) {
    throw 'No se encontro ISCC.exe. Instala Inno Setup 6 o indica su ruta con -InnoSetupCompiler.'
}

$null = New-Item -ItemType Directory -Force -Path $publishDirectory, $outputDirectory

$publishArguments = @(
    'publish',
    $projectFile,
    '--configuration', 'Release',
    '--runtime', 'win-x64',
    '--self-contained', 'true',
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    "-p:Version=$version",
    '-o', $publishDirectory
)
& $dotnet @publishArguments
if ($LASTEXITCODE -ne 0) {
    throw "La publicacion de .NET fallo con el codigo $LASTEXITCODE."
}

$licenseDirectory = Join-Path $publishDirectory 'ffmpeg\licenses'
$runtimeLicenseDirectory = Join-Path $projectRoot 'LumaWallpaper\ffmpeg-runtime\licenses'
$nugetPackageRoot = if ([string]::IsNullOrWhiteSpace($env:NUGET_PACKAGES)) {
    Join-Path $env:USERPROFILE '.nuget\packages'
} else {
    $env:NUGET_PACKAGES
}
$ffmpegNotice = Join-Path $nugetPackageRoot "devenvy.ffmpeg.binaries.lgpl\$ffmpegVersion\THIRD_PARTY_NOTICES.md"
if (-not (Test-Path $ffmpegNotice)) {
    throw "No se encontro el aviso de licencia FFmpeg: $ffmpegNotice"
}

$null = New-Item -ItemType Directory -Force -Path $licenseDirectory
Copy-Item -Path (Join-Path $runtimeLicenseDirectory '*') -Destination $licenseDirectory -Recurse -Force
Copy-Item -LiteralPath $ffmpegNotice -Destination (Join-Path $licenseDirectory 'FFmpeg-THIRD-PARTY-NOTICES.md') -Force

$ffmpegDirectory = Join-Path $publishDirectory 'ffmpeg'
foreach ($platform in @('linux-arm64', 'linux-musl-x64', 'linux-x64', 'osx-arm64', 'osx-x64')) {
    $unusedPlatformDirectory = Join-Path $ffmpegDirectory $platform
    if (Test-Path $unusedPlatformDirectory) {
        Remove-Item -LiteralPath $unusedPlatformDirectory -Recurse -Force
    }
}

& $InnoSetupCompiler "/DAppVersion=$version" $installerFile
if ($LASTEXITCODE -ne 0) {
    throw "La generacion del instalador fallo con el codigo $LASTEXITCODE."
}

$installerPath = Join-Path $outputDirectory "LumaWallpaper-Setup-$version-x64.exe"
if (-not (Test-Path $installerPath)) {
    throw "Inno Setup termino, pero no se encontro el instalador: $installerPath"
}

Write-Host "Instalador listo: $installerPath"
