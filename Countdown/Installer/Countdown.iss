; This script assumes that all release configurations have been published
; and that the WinAppSdk and .Net framework are self contained.
; Inno 6.5.4

; Caution: There be dragons here. The only way I could get upgrades to work reliably with trimming
; which rewrites dll's is to delete the install dir contents before copying the new stuff in.
; To that end I specify a compulsory unique dir for the install in the users hidden AppData dir.
; I wouldn't recomend it. This makes the install experience similar to installing a store app. 

#define appDisplayName "Countdown" 
#define appName appDisplayName
#define appExeName appName + ".exe"
#define appVer RemoveFileExt(GetVersionNumbersString("..\bin\Release\win-x64\publish\" + appExeName));
#define appId "605EF4DB-EFDD-42E1-BD83-4BE052A17DA7"
#define appMutexName "06482883-F905-4F5C-88E1-3B6B328144DD"
#define setupMutexName "3283C559-580A-47CA-82EA-B7AA35912ECD"

[Setup]
AppId={#appId}
AppName={#appDisplayName}
AppVersion={#appVer}
AppVerName={cm:NameAndVersion,{#appDisplayName},{#appVer}}
DefaultDirName={autopf}\{#appId}
OutputDir={#SourcePath}\bin
UninstallDisplayIcon={app}\{#appExeName}
AppMutex={#appMutexName},Global\{#appMutexName}
SetupMutex={#setupMutexName},Global\{#setupMutexName}
Compression=lzma2/ultra64 
SolidCompression=yes
OutputBaseFilename={#appName}_v{#appVer}
PrivilegesRequired=lowest
WizardStyle=modern
WizardSizePercent=100,100
DisableProgramGroupPage=yes
DisableDirPage=yes
MinVersion=10.0.17763
AppPublisher=David
ArchitecturesAllowed=x64compatible or arm64
ArchitecturesInstallIn64BitMode=x64compatible or arm64

[Files]
Source: "..\bin\Release\win-arm64\publish\*"; DestDir: "{app}"; Check: PreferArm64Files; Flags: ignoreversion recursesubdirs;
Source: "..\bin\Release\win-x64\publish\*";   DestDir: "{app}"; Check: PreferX64Files;   Flags: ignoreversion recursesubdirs solidbreak;

[Icons]
Name: "{autoprograms}\{#appDisplayName}"; Filename: "{app}\{#appExeName}"

[Run]
Filename: "{app}\{#appExeName}"; Description: "{cm:LaunchProgram,{#appDisplayName}}"; Flags: nowait postinstall skipifsilent

[InstallDelete]
Type: filesandordirs; Name: "{app}\*"

[Code]
function PreferArm64Files: Boolean;
begin
  Result := IsArm64;
end;

function PreferX64Files: Boolean;
begin
  Result := not PreferArm64Files and IsX64Compatible;
end;


procedure CurPageChanged(CurPageID: Integer);
begin  
  if CurPageID = wpInstalling then 
  begin   
    // hide the extracted file name etc.          
    WizardForm.FilenameLabel.Visible := false;
    WizardForm.StatusLabel.Visible := false;
  end;
end;


procedure UninstallOnUpgrade;
var
  Key, UninstallerPath, AppPath: String;
  ResultCode: Integer;
begin
  // Uninstalling an old version shouldn't have any side effects as it's now a different app
  Key := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\Countdown_is1';
  
  if RegQueryStringValue(HKCU, Key, 'UninstallString', UninstallerPath) and
     RegQueryStringValue(HKCU, Key, 'InstallLocation', AppPath) then
  begin  
    // the new app has mutex guards
    AppPath := RemoveBackSlash(RemoveQuotes(AppPath)) + '\{#AppExeName}' ;
    
    if not Exec('powershell.exe', 'gps | where path -eq ''' + AppPath + ''' | kill -force', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Begin
      Exec('taskkill.exe', '/t /f /im {#appExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    end;
    
    Exec(RemoveQuotes(UninstallerPath), '/VERYSILENT', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;


procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    UninstallOnUpgrade;
  end;
end;

