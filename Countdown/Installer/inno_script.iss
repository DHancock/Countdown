; This script assumes that all release configurations have been published
; and is framework dependent targeting a minimum WinAppSdk version of 1.3
; but will roll forward to any later 1.n version.
; Inno 6.2.2

#define appName "Countdown"
#define appVer "3.6.0"
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
    CheckFunction: TCheckFunc;
  end;
  
var
  DownloadsList: array of TDependencyItem;

   
function IsWinAppSdkInstalled(): Boolean; forward;
function IsNetDesktopInstalled(): Boolean; forward;
function GetPlatformStr(): String; forward;
function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean; forward;
procedure AddDownload(const Url, Title: String; const CheckFunction: TCheckFunc); forward;
function VersionComparer(const A, B: String): Integer; forward;
function IsSelfcontained(const Version: String): Boolean; forward;
function UninstallSelfContainedVersion(): String; forward;
function DownloadAndInstallPrerequesites(): String; forward;
function NewLine(): String; forward;
  
  
function InitializeSetup(): Boolean;
var 
  UpdateNet, UpdateWinAppSdk: Boolean;
  IniFile, DownloadUrl, Message: String;
begin
  Result := true;
  
  try
    UpdateNet := not IsNetDesktopInstalled;
    UpdateWinAppSdk := not IsWinAppSdkInstalled;

    if UpdateNet or UpdateWinAppSdk then  
    begin
      Result := false;
     
      if DownloadTemporaryFile('https://raw.githubusercontent.com/DHancock/Common/main/versions.ini', 'versions.ini', '', @OnDownloadProgress) > 0 then
      begin
        IniFile := ExpandConstant('{tmp}\versions.ini');

        if UpdateNet then
        begin
          DownloadUrl := GetIniString('NetDesktopRuntime', GetPlatformStr, '', IniFile);
          Result := Length(DownloadUrl) > 0;
          AddDownload(DownloadUrl, 'Net Desktop Runtime', @IsNetDesktopInstalled);
        end;

        if UpdateWinAppSdk then
        begin
          DownloadUrl := GetIniString('WinAppSdk', GetPlatformStr, '', IniFile);
          Result := Length(DownloadUrl) > 0;
          AddDownload(DownloadUrl, 'Windows App SDK', @IsWinAppSdkInstalled);
        end;
      end;
  
      if not Result then
        RaiseException('Unable to determine the download Url.');
    end;
    
  except
    Message := 'An fatal error occured when checking install prerequesites: ' + NewLine + GetExceptionMessage;
    SuppressibleMsgBox(Message, mbCriticalError, MB_OK, IDOK);
    Result := false;
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
    Result := UninstallSelfContainedVersion();
  
  if (Result = '') and IsDownloadRequired then
    Result := DownloadAndInstallPrerequesites();
end;


function DownloadAndInstallPrerequesites(): String;
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

          DownloadPage.Clear;
          DownloadPage.Add(Dependency.Url, ExtractFileName(Dependency.Url), '');
          
          repeat 
            Retry := false;
            try
              DownloadPage.Download;
            except
            
              if DownloadPage.AbortedByUser then
              begin
                RaiseException('Download of ' + Dependency.Title + ' was cancelled.');
                break;
              end
              else
              begin
                case SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbError, MB_ABORTRETRYIGNORE, IDIGNORE) of
                  IDABORT: begin
                    RaiseException('Download of ' + Dependency.Title + ' was aborted.');
                    break;
                  end;
                  IDRETRY: begin
                    Retry := True;
                  end;
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
            begin
              RaiseException('An error occured installing ' + Dependency.Title + '.' + NewLine + SysErrorMessage(ResultCode));
              break;
            end;

            DeleteFile(ExeFilePath);
            
            if not Dependency.CheckFunction() then
            begin
              RaiseException('Installation of ' + Dependency.Title + ' failed.');
              break;
            end;
            
            DownloadPage.ProgressBar.Style := npbstNormal;
            DownloadPage.AbortButton.Show;
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
 

function GetPlatformStr(): String;
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
function GetPlatformParamStr(): String;
begin
  case ProcessorArchitecture of
    paX86: Result := '0';
    paX64: Result := '9';
    paARM64: Result := '12';
  else
    RaiseException('unknown ProcessorArchitecture'); 
  end;
end;


function IsWinAppSdkInstalled(): Boolean;
var
  ExeFilePath: String;
  ResultCode: Integer;
begin
  ExeFilePath := ExpandConstant('{tmp}\CheckWinAppSdk.exe');

  if not FileExists(ExeFilePath) then
    ExtractTemporaryFile('CheckWinAppSdk.exe');

  if not Exec(ExeFilePath, '3000 ' + GetPlatformParamStr, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Log('Exec CheckWinAppSdk.exe failed: ' + SysErrorMessage(ResultCode));    

  Result := ResultCode = 0;
end;


function IsNetDesktopInstalled(): Boolean;
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
function UninstallSelfContainedVersion(): String;
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
        Log('Uninstall exception: ' + GetExceptionMessage);
      end;
    finally
      ProgressPage.Hide;
    end;
  end;
  
  if (ResultCode <> 0) or FileExists(UninstallerPath) then
    Result := 'Failed to uninstall version ' + InstalledVersion;
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
  if not StrToVersion(A, X) then
    Log('StrToVersion() failed for A: ' + A);
    
  if not StrToVersion(B, Y) then
    Log('StrToVersion() failed for B: ' + B);
  
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


function NewLine(): String;
begin
  Result := #13#10;
end;