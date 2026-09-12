#define MyAppName "EasyCoop Mod Manager"
#define MyAppVersion "1.0.0.0"
#define MyAppPublisher "EasyCoopClanTV"
#define MyAppExeName "EasyCoop Mod Manager.exe"

[Setup]
AppId={{FACEBB56-0525-4212-B292-A27A95ECEF33}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\EasyCoop Mod Manager
DefaultGroupName=EasyCoop Mod Manager
OutputDir=..\installer-output
OutputBaseFilename=EasyCoop-Mod-Manager-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=..\Assets\EasyCoop.ico
WizardImageFile=..\Assets\EasyCoop-Installer-Background.png
WizardImageBackColor=$111815
InfoBeforeFile=Vorinstallation.txt
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Desktop-Verknüpfung erstellen"; GroupDescription: "Zusätzliche Verknüpfungen:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} starten"; Flags: nowait postinstall skipifsilent
