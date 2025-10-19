; This script assumes that all release configurations have been published
; and that the WinAppSdk and .Net framework are self contained.
; Inno 6.5.4

#define appName "Countdown"
#define appExeName appName + ".exe"
#define appVer RemoveFileExt(GetVersionNumbersString("..\bin\Release\win-x64\publish\" + appExeName));
#define appId appName
#define appMutexName "06482883-F905-4F5C-88E1-3B6B328144DD"
#define setupMutexName "3283C559-580A-47CA-82EA-B7AA35912ECD"

[Setup]
AppId={#appId}
AppName={#appName}
AppVersion={#appVer}
AppVerName={cm:NameAndVersion,{#appName},{#appVer}}
DefaultDirName={autopf}\{#appName}
DefaultGroupName={#appName}
OutputDir={#SourcePath}\bin
UninstallDisplayIcon={app}\{#appExeName}
AppMutex={#appMutexName},Global\{#appMutexName}
SetupMutex={#setupMutexName},Global\{#setupMutexName}
Compression=lzma2/ultra64 
SolidCompression=yes
OutputBaseFilename={#appName}_v{#appVer}
PrivilegesRequired=lowest
WizardStyle=modern
DisableProgramGroupPage=yes
DisableDirPage=yes
MinVersion=10.0.17763
AppPublisher=David
ArchitecturesInstallIn64BitMode=x64compatible or arm64

[Files]
Source: "..\bin\Release\win-arm64\publish\*"; DestDir: "{app}"; Check: PreferArm64Files; Flags: ignoreversion recursesubdirs;
Source: "..\bin\Release\win-x64\publish\*";   DestDir: "{app}"; Check: PreferX64Files;   Flags: ignoreversion recursesubdirs solidbreak;
Source: "..\bin\Release\win-x86\publish\*";   DestDir: "{app}"; Check: PreferX86Files;   Flags: ignoreversion recursesubdirs solidbreak;

[Icons]
Name: "{autodesktop}\{#appName}"; Filename: "{app}\{#appExeName}"

[Run]
Filename: "{app}\{#appExeName}"; Description: "{cm:LaunchProgram,{#appName}}"; Flags: nowait postinstall skipifsilent

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

function PreferX86Files: Boolean;
begin
  Result := not PreferArm64Files and not PreferX64Files;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  // if an old version of the app is running ensure inno setup shuts it down
  if CurPageID = wpPreparing then
  begin
    WizardForm.PreparingNoRadio.Enabled := false;
  end;
end;

// A < B returns -ve
// A = B returns 0
// A > B returns +ve
function VersionComparer(const A, B: String): Integer;
var
  X, Y: Int64;
begin
  if not (StrToVersion(A, X) and StrToVersion(B, Y)) then
    RaiseException('StrToVersion(''' + A + ''', ''' + B + ''')');
  
  Result := ComparePackedVersion(X, Y);
end;


function GetUninstallRegKey: String;
begin
  Result := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#appId}_is1';
end;


function NewLine: String;
begin
  Result := #13#10;
end;


function IsDowngradeInstall: Boolean;
var
  InstalledVersion, UninstallerPath: String;
begin
  Result := false;
  
  if RegQueryStringValue(HKCU, GetUninstallRegKey, 'DisplayVersion', InstalledVersion) and 
     RegQueryStringValue(HKCU, GetUninstallRegKey, 'UninstallString', UninstallerPath) then
  begin   
    // check both the app version and that it (may be) possible to uninstall it 
    Result := (VersionComparer(InstalledVersion, '{#appVer}') > 0) and FileExists(RemoveQuotes(UninstallerPath));
  end;
end;


function InitializeSetup: Boolean;
var 
  Message: String;
begin
  Result := true;
  
  try
    if IsDowngradeInstall then
      RaiseException('Downgrading isn''t supported.' + NewLine + 'Please uninstall the current version first.');

  except
    Message := 'An error occured when checking install prerequesites:' + NewLine + GetExceptionMessage;
    SuppressibleMsgBox(Message, mbCriticalError, MB_OK, IDOK);
    Result := false;
  end;
end;


procedure TransferSettingsIfRequired;
var
  ResultCode, Attempts: Integer;
  old, new, root, file : String; 
begin    
  old := '\countdown.davidhancock.net';
  new := '\countdown.davidhancock.net.v2';
  root := ExpandConstant('{localappdata}')
  file := '\settings.json';
  
  if (not FileExists(root + new + file)) and FileExists(root + old + file) then
  begin 
    CreateDir(root + new);
    CopyFile(root + old + file,  root + new + file, false);
  end;
end;


procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssInstall) then
  begin
    // unfortunately I once thought that deleting the settings on uninstall was a good idea
    TransferSettingsIfRequired;
  end;
end;

