; This script assumes that the release configuration has been published
; and that the publishing profile defines a self contained app.
; Inno 6.2.2

#ifndef platform
  #error platform is not defined
#endif
  
#if !((platform == "x64") || (platform == "x86") || (platform == "arm64"))
  #error invalid platform definition
#endif

#define appName "Countdown"
#define appVer "3.5"
#define appExeName "countdown.exe"

[Setup]
AppName={#appName}
AppVersion={#appVer}
AppVerName={cm:NameAndVersion,{#appName},{#appVer}}
DefaultDirName={autopf}\{#appName}
DefaultGroupName={#appName}
SourceDir=..\bin\{#platform}\Release\publish
OutputDir={#SourcePath}\bin
UninstallDisplayIcon={app}\{#appExeName}
Compression=lzma2
SolidCompression=yes
OutputBaseFilename={#appName}_v{#appVer}_{#platform}
InfoBeforeFile="{#SourcePath}\unlicense.txt"
PrivilegesRequired=lowest
WizardStyle=classic
DisableWelcomePage=no
DirExistsWarning=yes
DisableProgramGroupPage=yes
MinVersion=10.0.17763
AppPublisher=David
AppUpdatesURL=https://github.com/DHancock/Countdown/releases

#if ((platform == "x64") || (platform == "arm64"))
ArchitecturesAllowed={#platform}
ArchitecturesInstallIn64BitMode={#platform}
#else
ArchitecturesAllowed=x86 x64
#endif

[Files]
Source: "*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\{#appName}"; Filename: "{app}\{#appExeName}"
Name: "{autodesktop}\{#appName}"; Filename: "{app}\{#appExeName}"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"

[Run]
Filename: "{app}\{#appExeName}"; Description: "{cm:LaunchProgram,{#appName}}"; Flags: nowait postinstall skipifsilent
