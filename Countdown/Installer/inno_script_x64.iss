; This script assumes that the release configuration has been published
; and that the publishing profile defines a self contained app.
; Admin privileges are required to create the program files entry.
; Inno 6.2.2

#define appName "Countdown"
#define appVer "3.5"
#define appExeName "countdown.exe"

[Setup]
AppName={#appName}
AppVersion={#appVer}
AppVerName={cm:NameAndVersion,{#appName},{#appVer}}
DefaultDirName={autopf}\{#appName}
DefaultGroupName={#appName}
SourceDir=..\bin\x64\Release\publish
OutputDir={#SourcePath}\bin
UninstallDisplayIcon={app}\{#appExeName}
Compression=lzma2
SolidCompression=yes
OutputBaseFilename=countdown_v{#appVer}_x64
PrivilegesRequired=admin
WizardStyle=classic
DisableWelcomePage=no
DirExistsWarning=yes
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0.17763
AppPublisher=David
AppUpdatesURL=https://github.com/DHancock/Countdown/releases

[Files]
Source: "*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\{#appName}"; Filename: "{app}\{#appExeName}"
Name: "{autodesktop}\{#appName}"; Filename: "{app}\{#appExeName}"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"

[Run]
Filename: "{app}\{#appExeName}"; Description: "{cm:LaunchProgram,{#appName}}"; Flags: nowait postinstall skipifsilent
