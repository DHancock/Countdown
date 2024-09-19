; This script assumes that all release configurations have been published
; and that the WinAppSdk and .Net framework are self contained.
; Inno 6.2.2

#define appName "Countdown"
#define appExeName appName + ".exe"
#define appVer RemoveFileExt(GetVersionNumbersString("..\bin\Release\win-x64\publish\" + appExeName));
#define appId appName

[Setup]
AppId={#appId}
AppName={#appName}
AppVersion={#appVer}
AppVerName={cm:NameAndVersion,{#appName},{#appVer}}
DefaultDirName={autopf}\{#appName}
DefaultGroupName={#appName}
OutputDir={#SourcePath}\bin
UninstallDisplayIcon={app}\{#appExeName}
Compression=lzma2/ultra64 
SolidCompression=yes
OutputBaseFilename={#appName}_v{#appVer}
InfoBeforeFile="{#SourcePath}\unlicense.txt"
PrivilegesRequired=lowest
WizardStyle=classic
WizardSizePercent=110,110
DirExistsWarning=yes
DisableWelcomePage=yes
DisableProgramGroupPage=yes
DisableReadyPage=yes
MinVersion=10.0.17763
AppPublisher=David
AppUpdatesURL=https://github.com/DHancock/Countdown/releases
ArchitecturesInstallIn64BitMode=x64 arm64
ArchitecturesAllowed=x86 x64 arm64

[Files]
Source: "..\bin\Release\win-x64\publish\*"; DestDir: "{app}"; Check: IsX64; Flags: recursesubdirs;
Source: "..\bin\Release\win-arm64\publish\*"; DestDir: "{app}"; Check: IsARM64; Flags: recursesubdirs solidbreak;
Source: "..\bin\Release\win-x86\publish\*"; DestDir: "{app}"; Check: IsX86; Flags: recursesubdirs solidbreak;

[Icons]
Name: "{group}\{#appName}"; Filename: "{app}\{#appExeName}"
Name: "{autodesktop}\{#appName}"; Filename: "{app}\{#appExeName}"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"

[Run]
Filename: "{app}\{#appExeName}"; Description: "{cm:LaunchProgram,{#appName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\{#appExeName}"; Parameters: "/uninstall";

[Code]
function IsInstalledAppUntrimmed(const InstalledVersion: String): Boolean; forward;
procedure BackupAppData; forward;
procedure RestoreAppData; forward;

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
  InstalledVersion: String;
begin
  Result := false;
  
  if RegQueryStringValue(HKCU, GetUninstallRegKey, 'DisplayVersion', InstalledVersion) then
    Result := VersionComparer(InstalledVersion, '{#appVer}') > 0;
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


function IsInstalledAppUntrimmed(const InstalledVersion: String): Boolean;
begin
  Result := VersionComparer(InstalledVersion, '3.11.0') < 0 ;
end;


// The remnants of an untrimmed install will cause a trimmed version
// to fail to start. Have to uninstall the untrimmed version first.
// This also means the benfits of trimming will now be in effect.
// The old installer releases will be removed from GitHub.
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode, Attempts: Integer;
  InstalledVersion, UninstallerPath: String; 
begin
  if (CurStep = ssInstall) then
  begin
    if RegQueryStringValue(HKCU, GetUninstallRegKey, 'DisplayVersion', InstalledVersion) and IsInstalledAppUntrimmed(InstalledVersion) then
    begin
      if RegQueryStringValue(HKCU, GetUninstallRegKey, 'UninstallString', UninstallerPath) then
      begin
        BackupAppData;

        Exec(RemoveQuotes(UninstallerPath), '/VERYSILENT', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
        
        if ResultCode = 0 then // wait until the uninstall has completed
        begin
          Attempts := 2 * 30 ; // timeout after approximately 30 seconds
           
          while FileExists(UninstallerPath) and (Attempts > 0) do
          Begin
            Sleep(500);
            Attempts := Attempts - 1;
          end;
        end;
      
        if (ResultCode <> 0) or FileExists(UninstallerPath) then
        begin
          SuppressibleMsgBox('Setup failed to uninstall a previous version.', mbCriticalError, MB_OK, IDOK) ;
          Abort;
        end;
        
        RestoreAppData;
      end;
    end;
  end;
end;


procedure TransferFiles(const BackUp: Boolean);
var
  SourceDir, DestDir, DirPart, FilePart, TempA, TempB: string;
begin
  try
    DirPart := '\countdown.davidhancock.net';
    TempA := ExpandConstant('{localappdata}') + DirPart;
    TempB := ExpandConstant('{%temp}') + DirPart; 
    
    if BackUp then
    begin
      SourceDir := TempA;
      DestDir := TempB;
    end
    else
    begin
      SourceDir := TempB
      DestDir := TempA;
    end;
      
    if ForceDirectories(DestDir) then
    begin
      FilePart := '\settings.json';
      
      if FileExists(SourceDir + FilePart) then
        FileCopy(SourceDir + FilePart, DestDir + FilePart, false);        
    end;
  except
  end;
end;   


procedure BackupAppData();
begin
  TransferFiles(true);
end;  


procedure RestoreAppData();
begin
  TransferFiles(false);
end;  
