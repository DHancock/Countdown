; This script assumes that all release configurations have been published
; and is framework dependent targeting a minimum WinAppSdk version of 1.3
; but will roll forward to any later 1.n version.
; Inno 6.2.2

#define appName "Countdown"
#define appVer "3.6.0"
#define appExeName appName + ".exe"

[Setup]
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
WizardStyle=modern
DisableWelcomePage=yes
DirExistsWarning=yes
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
function IsWinAppSdkInstalled() : Boolean; forward;
function IsNetDesktopInstalled() : Boolean; forward;
function GetPlatformStr() : String; forward;
function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64) : Boolean; forward;

var
  DownloadsList : TStringList;

function InitializeSetup(): Boolean;
var 
  UpdateNet, UpdateWinAppSdk : Boolean;
  IniFile, DownloadUrl, Message : String;
begin
  Result := true;
  DownloadsList := TStringList.Create;

  try
    UpdateNet := not IsNetDesktopInstalled;
    UpdateWinAppSdk := not IsWinAppSdkInstalled;

    if UpdateNet or UpdateWinAppSdk then  
    begin
      // This also checks for a valid network connection. MS use a similar redirrection scheme.
      if DownloadTemporaryFile('https://raw.githubusercontent.com/DHancock/Common/main/versions.ini', 'versions.ini', '', @OnDownloadProgress) > 0 then
      begin
        
        IniFile := ExpandConstant('{tmp}\versions.ini');

        if UpdateNet then
        begin
          DownloadUrl := GetIniString('NetDesktopRuntime', GetPlatformStr, '', IniFile);
          Result := Length(DownloadUrl) > 0;
          DownloadsList.Add(DownloadUrl)
        end;

        if UpdateWinAppSdk and Result then
        begin
          DownloadUrl := GetIniString('WinAppSdk', GetPlatformStr, '', IniFile);
          Result := Length(DownloadUrl) > 0;
          DownloadsList.Add(DownloadUrl)
        end;
      end;
  
      if not Result then
      begin
        Message := 'Setup has detected that a prerequisite sdk needs to be installed but cannot determine the download Url.';
        SuppressibleMsgBox(Message, mbCriticalError, MB_OK, IDOK);
      end;
    end;

  except
    Message := 'An fatal error occured when checking instal prerequesites: '#13#10 + GetExceptionMessage;
    SuppressibleMsgBox(Message, mbCriticalError, MB_OK, IDOK);
    Result := false;
  end;
end;


function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Retry: Boolean;
  DownloadUrl, ExeFilePath : String;
  ResultCode, Index: Integer;
  DownloadPage: TDownloadWizardPage;
begin
  NeedsRestart := false;
  Result := ''; 

  if DownloadsList.Count > 0 then
  begin
    DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), @OnDownloadProgress);
    DownloadPage.Show;
    
    Index := 0;

    repeat
      DownloadUrl := DownloadsList[Index];

      DownloadPage.Clear;
      DownloadPage.Add(DownloadUrl, ExtractFileName(DownloadUrl), '');
      
      repeat 
        Retry := false;
        try
          DownloadPage.Download;
        except
        
          if DownloadPage.AbortedByUser then
          begin
            Result := DownloadUrl;
            Index := DownloadsList.Count;
            break;
          end
          else
          begin
            case SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbError, MB_ABORTRETRYIGNORE, IDIGNORE) of
              IDABORT: begin
                Result := DownloadUrl;
                Index := DownloadsList.Count;
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
        DownloadPage.SetText('Installing ' + ExtractFileName(DownloadUrl), '');
        DownloadPage.ProgressBar.Style := npbstMarquee;
        
        ExeFilePath := ExpandConstant('{tmp}\') + ExtractFileName(DownloadUrl);

        if not Exec(ExeFilePath, '', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
        begin
          Result := 'An error occured installing ' + ExeFilePath + '.'#13#10 + SysErrorMessage(ResultCode);
          break;
        end;

        DeleteFile(ExeFilePath);
        DownloadPage.ProgressBar.Style := npbstNormal;
        DownloadPage.AbortButton.Show;
      end;

      Index := Index + 1;

    until Index >= DownloadsList.Count;

    DownloadPage.Hide;
    DownloadPage.Free;
  end;
end;                                                            


function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log('Successfully downloaded file: ' + ExpandConstant('{tmp}') + '\' + FileName);

  Result := True;
end;
 

function GetPlatformStr() : String;
begin
  case ProcessorArchitecture of
    paX86: Result := 'x86';
    paX64: Result := 'x64';
    paARM64: Result := 'arm64';
  end;
end;

// returns a Windows.System.ProcessorArchitecture enum value
function GetPlatformParam() : String;
begin
  case ProcessorArchitecture of
    paX86: Result := '0';
    paX64: Result := '9';
    paARM64: Result := '12';
  end;
end;


function IsWinAppSdkInstalled() : Boolean;
var
  ResultCode : Integer;
begin
  ExtractTemporaryFile('CheckWinAppSdk.exe');

  if not Exec(ExpandConstant('{tmp}\CheckWinAppSdk.exe'), '3000 ' + GetPlatformParam(), '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Log('Exec CheckWinAppSdk.exe failed: ' + SysErrorMessage(ResultCode));    
  end;

  Result := ResultCode = 0 ;
end;


function IsNetDesktopInstalled() : Boolean;
var
  ResultCode : Integer;
begin
  ExtractTemporaryFile('NetCoreCheck.exe');

  if not Exec(ExpandConstant('{tmp}\NetCoreCheck.exe'), '-n Microsoft.WindowsDesktop.App -v 6.0.16 -r LatestMajor', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Log('Exec NetCoreCheck.exe failed: ' + SysErrorMessage(ResultCode));    
  end;

  Result := ResultCode = 0 ;
end;
