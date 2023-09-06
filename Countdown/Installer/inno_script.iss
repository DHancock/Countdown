; This script assumes that all release configurations have been published
; and is framework dependent targeting a WinAppSdk release of 1.3.n where n >= 0
; Inno 6.2.2

#define appName "Countdown"
#define appVer "3.7.1"
#define appExeName appName + ".exe"
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
Compression=lzma2
SolidCompression=yes
OutputBaseFilename={#appName}_v{#appVer}
InfoBeforeFile="{#SourcePath}\unlicense.txt"
PrivilegesRequired=lowest
AllowUNCPath=no
AllowNetworkDrive=no
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
Source: "..\installer\tools\NetCoreCheck.exe"; Flags: dontcopy;
Source: "..\installer\tools\CheckWinAppSdk.exe"; Flags: dontcopy;
Source: "..\bin\x64\Release\publish\*"; DestDir: "{app}"; Check: IsX64; Flags: recursesubdirs; 
Source: "..\bin\x86\Release\publish\*"; DestDir: "{app}"; Check: IsX86; Flags: recursesubdirs solidbreak; 
Source: "..\bin\arm64\Release\publish\*"; DestDir: "{app}"; Check: IsARM64; Flags: recursesubdirs solidbreak;

[Icons]
Name: "{group}\{#appName}"; Filename: "{app}\{#appExeName}"
Name: "{autodesktop}\{#appName}"; Filename: "{app}\{#appExeName}"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"

[Run]
Filename: "{app}\{#appExeName}"; Description: "{cm:LaunchProgram,{#appName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: powershell.exe; Parameters: "Get-Process {#appName} | where Path -eq '{app}\{#appExeName}' | kill -Force"; Flags: runhidden

[Code]
type
  TCheckFunc = function: Boolean;
  
  TDependencyItem = record
    Url: String;
    Title: String;
    Installed: Boolean;
    CheckFunction: TCheckFunc;
  end;
  
var
  DownloadsList: array of TDependencyItem;

   
function IsWinAppSdkInstalled: Boolean; forward;
function IsNetDesktopInstalled: Boolean; forward;
function GetPlatformStr: String; forward;
function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean; forward;
procedure AddDownload(const Url, Title: String; const CheckFunction: TCheckFunc); forward;
function VersionComparer(const A, B: String): Integer; forward;
function IsSelfcontained(const Version: String): Boolean; forward;
function UninstallSelfContainedVersion: String; forward;
function DownloadAndInstallPrerequesites: String; forward;
function NewLine: String; forward;
function IsDowngradeInstall: Boolean; forward;
function IsValidUrl(Url: String): Boolean; forward;
function GetNetDesktopRuntimeUrl: String; forward;
function GetWinAppSdkUrl: String; forward;

  
function InitializeSetup: Boolean;
var 
  DownloadUrl, Message: String;
begin
  Result := true;
  
  try
    if IsDowngradeInstall then
      RaiseException('Downgrading isn''t supported.' + NewLine + 'Please uninstall the current version first.');
    
    if not IsNetDesktopInstalled then
    begin
      DownloadUrl := GetNetDesktopRuntimeUrl;
      
      if IsValidUrl(DownloadUrl) then
        AddDownload(DownloadUrl, 'Net Desktop Runtime', @IsNetDesktopInstalled)
      else
        RaiseException('Invalid Net Desktop Runtime installer download url');
    end;

    if not IsWinAppSdkInstalled then
    begin
      DownloadUrl := GetWinAppSdkUrl;
      
      if IsValidUrl(DownloadUrl) then
        AddDownload(DownloadUrl, 'Windows App SDK', @IsWinAppSdkInstalled)
      else
        RaiseException('Invalid Windows App SDK installer download url');
    end;    
  except
    Message := 'An error occured when checking install prerequesites:' + NewLine + GetExceptionMessage;
    SuppressibleMsgBox(Message, mbCriticalError, MB_OK, IDOK);
    Result := false;
  end;
end;


function IsValidUrl(Url: String): Boolean;
begin
  Result := DownloadTemporaryFileSize(Url) > 0;
end;
  
  
function GetNetDesktopRuntimeUrl: String;
begin
  case ProcessorArchitecture of
    paX86: Result := 'https://download.visualstudio.microsoft.com/download/pr/78caa28b-2982-43ed-8b9c-20e3369f0795/c771e9fd12a67068436115cf295740f7/dotnet-runtime-6.0.16-win-x86.exe';
    paX64: Result := 'https://download.visualstudio.microsoft.com/download/pr/456fdf02-f100-4664-916d-fd46c192efea/619bbd8426537632b7598b4c7c467cf1/dotnet-runtime-6.0.16-win-x64.exe';
    paARM64: Result := 'https://download.visualstudio.microsoft.com/download/pr/91b97f3d-7783-4be3-ada2-6f1d4b299088/8d98117ad78ad15945f28a4e4bd0f79d/dotnet-runtime-6.0.16-win-arm64.exe';
  else
    RaiseException('unknown ProcessorArchitecture'); 
  end;
end;


function GetWinAppSdkUrl: String;
begin
  case ProcessorArchitecture of
    paX86: Result := 'https://aka.ms/windowsappsdk/1.4/latest/windowsappruntimeinstall-x86.exe';
    paX64: Result := 'https://aka.ms/windowsappsdk/1.4/latest/windowsappruntimeinstall-x64.exe';
    paARM64: Result := 'https://aka.ms/windowsappsdk/1.4/latest/windowsappruntimeinstall-arm64.exe';
  else
    RaiseException('unknown ProcessorArchitecture'); 
  end;
end;


function GetUninstallRegKey: String;
begin
  Result := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#appId}_is1';
end;


function IsUninstallRequired:Boolean;
var
  InstalledVersion: String;
begin
  Result := RegQueryStringValue(HKCU, GetUninstallRegKey, 'DisplayVersion', InstalledVersion) and 
            IsSelfcontained(InstalledVersion);
end;


function IsDownloadRequired: Boolean;
begin
  Result := GetArrayLength(DownloadsList) > 0;
end;


function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  NeedsRestart := false;
  Result := '';
  
  if IsUninstallRequired then
    Result := UninstallSelfContainedVersion;
  
  if (Result = '') and IsDownloadRequired then
    Result := DownloadAndInstallPrerequesites;
end;


function DownloadAndInstallPrerequesites: String;
var
  Retry: Boolean;
  ExeFilePath: String;
  Dependency: TDependencyItem;
  ResultCode, Count, Index: Integer;
  DownloadPage: TDownloadWizardPage;
begin
  Result := ''; 
  Count := GetArrayLength(DownloadsList);
  
  if Count > 0 then
  begin
    DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), @OnDownloadProgress);
    DownloadPage.Show;       
    Index := 0;
    
    try
      try
        repeat
          Dependency := DownloadsList[Index];

          if not Dependency.Installed then
          begin
            DownloadPage.Clear;
            DownloadPage.Add(Dependency.Url, ExtractFileName(Dependency.Url), '');
            
            repeat 
              Retry := false;
              try
                DownloadPage.Download;
              except
              
                if DownloadPage.AbortedByUser then
                  RaiseException('Download of ' + Dependency.Title + ' was cancelled.')
                else
                begin
                  case SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbError, MB_ABORTRETRYIGNORE, IDIGNORE) of
                    IDABORT: 
                      RaiseException('Download of ' + Dependency.Title + ' was aborted.');
                    IDRETRY:
                      Retry := True;
                  end;
                end; 
              end;
            until not Retry;

            if Result = '' then
            begin
              DownloadPage.AbortButton.Hide;
              DownloadPage.SetText('Installing the ' + Dependency.Title, '');
              DownloadPage.ProgressBar.Style := npbstMarquee;
              
              ExeFilePath := ExpandConstant('{tmp}\') + ExtractFileName(Dependency.Url);

              if not Exec(ExeFilePath, '', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
                RaiseException('An error occured installing ' + Dependency.Title + '.' + NewLine + SysErrorMessage(ResultCode));

              DeleteFile(ExeFilePath);
              
              if not Dependency.CheckFunction() then
                RaiseException('Installation of ' + Dependency.Title + ' failed.');

              DownloadsList[Index].Installed := true;
              
              DownloadPage.ProgressBar.Style := npbstNormal;
              DownloadPage.AbortButton.Show;
            end;
          end;
          
          Index := Index + 1;
          
        until Index >= Count;
      except
        Result := 'Installing prerequesites failed:' + NewLine + GetExceptionMessage;
      end;
    finally
      DownloadPage.Hide
    end;
  end;
end;


procedure AddDownload(const Url, Title: String; const CheckFunction: TCheckFunc); 
var
  Dependency: TDependencyItem;
  Count: Integer;
begin
  Dependency.Url := Url;
  Dependency.Title := Title;
  Dependency.Installed := false;
  Dependency.CheckFunction := CheckFunction;
  
  // a linked list isn't possible because forward type declarations arn't supported 
  Count := GetArrayLength(DownloadsList);
  SetArrayLength(DownloadsList, Count + 1);
  DownloadsList[Count] := Dependency;
end;


function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log('Successfully downloaded file: ' + FileName + ' size: ' + IntToStr(ProgressMax));

  Result := True;
end;
 

function GetPlatformStr: String;
begin
  case ProcessorArchitecture of
    paX86: Result := 'x86';
    paX64: Result := 'x64';
    paARM64: Result := 'arm64';
  else
    RaiseException('unknown ProcessorArchitecture'); 
  end;
end;


// returns a Windows.System.ProcessorArchitecture enum value
function GetPlatformParamStr: String;
begin
  case ProcessorArchitecture of
    paX86: Result := '0';
    paX64: Result := '9';
    paARM64: Result := '12';
  else
    RaiseException('unknown ProcessorArchitecture'); 
  end;
end;


function IsWinAppSdkInstalled: Boolean;
var
  ExeFilePath: String;
  ResultCode: Integer;
begin
  ExeFilePath := ExpandConstant('{tmp}\CheckWinAppSdk.exe');

  if not FileExists(ExeFilePath) then
    ExtractTemporaryFile('CheckWinAppSdk.exe');

  // WinAppSdk 1.4.0 is 4000.964.11.0
  // Check for any 1.4.n version where n >= 0
  
  if not Exec(ExeFilePath, '4000.964.11.0' + ' ' + GetPlatformParamStr, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Log('Exec CheckWinAppSdk.exe failed: ' + SysErrorMessage(ResultCode));    

  Result := ResultCode = 0;
end;


function IsNetDesktopInstalled: Boolean;
var
  ExeFilePath: String;
  ResultCode: Integer;
begin
  ExeFilePath := ExpandConstant('{tmp}\NetCoreCheck.exe');

  if not FileExists(ExeFilePath) then
    ExtractTemporaryFile('NetCoreCheck.exe');

  if not Exec(ExeFilePath, '-n Microsoft.WindowsDesktop.App -v 6.0.16 -r LatestMajor', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Log('Exec NetCoreCheck.exe failed: ' + SysErrorMessage(ResultCode));    

  Result := ResultCode = 0;
end;


// The remnants of a self contained install dlls will cause a framework dependent 
// app to trap on start. Have to uninstall first. Down grading from framework
// dependent to an old self contained version also causes the app to fail. 
// The old installer releases will be removed from GitHub.
function UninstallSelfContainedVersion: String;
var
  ResultCode, Attempts: Integer;
  RegKey, InstalledVersion, UninstallerPath: String;
  ProgressPage: TOutputMarqueeProgressWizardPage;
begin
  Result := '';
  ResultCode := 1;
  RegKey := GetUninstallRegKey;
  
  if RegQueryStringValue(HKCU, RegKey, 'DisplayVersion', InstalledVersion) and
      RegQueryStringValue(HKCU, RegKey, 'UninstallString', UninstallerPath) then
  begin
    ProgressPage := CreateOutputMarqueeProgressPage('Uninstall', 'Uninstalling version ' + InstalledVersion);
    ProgressPage.Animate;
    ProgressPage.Show;

    try
      try 
        Attempts := 4*30;
        UninstallerPath := RemoveQuotes(UninstallerPath);
        
        Exec(UninstallerPath, '/VERYSILENT /SUPPRESSMSGBOXES', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
        Log('Uninstall version: ' + InstalledVersion + ' returned: ' + IntToStr(ResultCode));
        
        if ResultCode = 0 then // wait until the uninstall has completed
        begin          
          repeat 
            Sleep(250);
            Attempts := Attempts - 1;
          until (not FileExists(UninstallerPath)) or (Attempts = 0);
            
          Log('Uninstall completed, attempts remaining: ' + IntToStr(Attempts));
        end;
      except
        ResultCode := 1;
        Log('Uninstall exception: ' + GetExceptionMessage);
      end;
    finally
      ProgressPage.Hide;
    end;
  end;
  
  if (ResultCode <> 0) or FileExists(UninstallerPath) then
    Result := 'Failed to uninstall version ' + InstalledVersion;
end;


function IsDowngradeInstall: Boolean;
var
  InstalledVersion: String;
begin
  Result := false;
  
  if RegQueryStringValue(HKCU, GetUninstallRegKey, 'DisplayVersion', InstalledVersion) then
    Result := VersionComparer(InstalledVersion, '{#appVer}') > 0;
end;


function IsSelfcontained(const Version: String): Boolean;
begin
  Result := VersionComparer(Version, '3.6') < 0;
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


procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectTasks then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonInstall)
  else if CurPageID = wpFinished then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonFinish)
  else
    WizardForm.NextButton.Caption := SetupMessage(msgButtonNext);
end;


function NewLine: String;
begin
  Result := #13#10;
end;