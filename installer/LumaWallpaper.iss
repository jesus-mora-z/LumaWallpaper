#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#define AppName "Luma Wallpaper"
#define AppId "{{B6C7D949-0D31-47C3-A2E4-0FC04F88DB28}"
#define PublishDirectory "..\artifacts\publish"
#define OutputDirectory "..\dist"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Luma Wallpaper
DefaultDirName={localappdata}\Programs\Luma Wallpaper
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
OutputDir={#OutputDirectory}
OutputBaseFilename=LumaWallpaper-Setup-{#AppVersion}-x64
UninstallDisplayName={#AppName}
CloseApplications=yes
RestartApplications=no
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupLogging=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: checkedonce

[Files]
Source: "{#PublishDirectory}\LumaWallpaper.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDirectory}\ffmpeg\win-x64\*"; DestDir: "{app}\ffmpeg\win-x64"; Flags: ignoreversion
Source: "{#PublishDirectory}\ffmpeg\licenses\*"; DestDir: "{app}\ffmpeg\licenses"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\LumaWallpaper.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\LumaWallpaper.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\LumaWallpaper.exe"; Description: "Iniciar {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeUninstall(): Boolean;
begin
  Result := True;
  RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'LumaWallpaper');
end;
