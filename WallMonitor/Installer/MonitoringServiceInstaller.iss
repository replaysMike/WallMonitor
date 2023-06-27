#define MyAppName "Wall Monitor"
#define MyAppDescription "Monitors configured services and agents"
#define MyAppVersion "0.0"
#define MyAppPublisher "Wall Monitor"
#define MyAppURL "https://github.com/replaysMike/WallMonitor/"
#define MyAppExeName "WallMonitor.MonitoringService.exe"

[Setup]
AppId={{0AAD7BDC-BBFD-47D3-99B0-61318322E43A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
VersionInfoVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DisableWelcomePage=no
DisableProgramGroupPage=yes
LicenseFile=..\..\LICENSE
OutputDir=.\
OutputBaseFilename=WallMonitor-MonitoringService-win10x64-{#MyAppVersion}
SetupIconFile=.\monitor-icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}
WizardImageFile=.\Monitor-WizardLarge.bmp
WizardSmallImageFile=.\Monitor-WizardSmall.bmp
CloseApplications=force
UsePreviousTasks=no


[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel2=The System Monitor Service will monitor all services that are configured and host a TCP or UDP server to allow user interfaces to receive messages.%n%nIt can connect to multiple System Monitor Agents, or directly to exposed services.%n%nThis will install [name/ver] on your computer.

[CustomMessages]
UninstallingService=Uninstalling existing {#MyAppName} service...
InstallingService=Installing {#MyAppName} {#MyAppVersion} service...
StartingApp=Starting {#MyAppName}...

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "keepconfiguration"; Description: "Keep existing configuration"
Name: "installservice"; Description: "Install {#MyAppName} as a Windows service"

[Files]
Source: "..\Service\WallMonitor.MonitoringService\bin\Release\net7.0\win10-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
//Filename: https://localhost; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: postinstall shellexec skipifsilent runhidden

[UninstallRun]
Filename: "{app}\{#MyAppExeName}"; Parameters: "uninstall -servicename {#MyAppName}"; RunOnceId: "{#MyAppName}"; Flags: runascurrentuser runhidden

[Code]
type
    SERVICE_STATUS = record
        dwServiceType               : cardinal;
        dwCurrentState              : cardinal;
        dwControlsAccepted          : cardinal;
        dwWin32ExitCode             : cardinal;
        dwServiceSpecificExitCode   : cardinal;
        dwCheckPoint                : cardinal;
        dwWaitHint                  : cardinal;
    end;
    HANDLE = cardinal;
const
    SERVICE_QUERY_CONFIG        = $1;
    SC_MANAGER_ALL_ACCESS       = $f003f;
    SERVICE_RUNNING             = $4;
    SERVICE_QUERY_STATUS        = $4;

function OpenSCManager(lpMachineName, lpDatabaseName: string; dwDesiredAccess :cardinal): HANDLE;
external 'OpenSCManagerW@advapi32.dll stdcall';

function OpenService(hSCManager :HANDLE; lpServiceName: string; dwDesiredAccess :cardinal): HANDLE;
external 'OpenServiceW@advapi32.dll stdcall';

function CloseServiceHandle(hSCObject :HANDLE): boolean;
external 'CloseServiceHandle@advapi32.dll stdcall';

function QueryServiceStatus(hService :HANDLE;var ServiceStatus :SERVICE_STATUS) : boolean;
external 'QueryServiceStatus@advapi32.dll stdcall';

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode : Integer;
  SourcePath: string;
  DestPath: string;
begin
  // backup the appsettings
  if CurStep = ssInstall then
  begin
    if WizardIsTaskSelected('keepconfiguration') then
     begin
      SourcePath := ExpandConstant('{app}\appsettings.json');
      DestPath := ExpandConstant('{app}\appsettings.installerbackup.json');
      if FileExists(SourcePath) then
      begin
        if not FileCopy(SourcePath, DestPath, False) then
        begin
         Log(Format('Backed up %s to %s', [SourcePath, DestPath]));
        end
          else
        begin
          Log(Format('Failed to Backup %s', [SourcePath]));
        end;
      end;
    end;
  end;
  
  // Install the service if the task was checked by the user
  if CurStep = ssPostInstall then
  begin
    Log('Post install');

    if WizardIsTaskSelected('keepconfiguration') then
     begin
      // restore the appsettings
      SourcePath := ExpandConstant('{app}\appsettings.json');
      DestPath := ExpandConstant('{app}\appsettings.installerbackup.json');
      if FileExists(DestPath) then
      begin
        if FileCopy(DestPath, SourcePath, False) then
        begin
          Log(Format('Restored %s from Backup %s', [SourcePath, DestPath]));
        end;
      end;
    end;

    // Install the certificate as trusted before launching apps
    //WizardForm.StatusLabel.Caption := CustomMessage('InstallingCertificates');
    //WizardForm.StatusLabel.Show();
    //Exec('powershell.exe', ExpandConstant('-ExecutionPolicy Bypass -Command Import-PfxCertificate -FilePath ""\""{app}\Certificates\Certificate.pfx\"" -CertStoreLocation cert:\LocalMachine\Root -Password (ConvertTo-SecureString -String password -Force -AsPlainText)'), '', SW_SHOW, ewWaitUntilTerminated, ResultCode);

    if WizardIsTaskSelected('installservice') then
    begin
      WizardForm.StatusLabel.Caption := CustomMessage('InstallingService');
      WizardForm.StatusLabel.Show();
      Log('Installing service...');
      //Exec(ExpandConstant('{app}\{#MyAppExeName}'), ExpandConstant('install --autostart'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Exec(ExpandConstant('sc'), ExpandConstant('create "{#MyAppName}" binpath="{app}\{#MyAppExeName}"'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Exec(ExpandConstant('sc'), ExpandConstant('description "{#MyAppName}" "{#MyAppDescription}"'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Log(ExpandConstant('Service installed with code ' + IntToStr(ResultCode)));
      Log('Configuring service...');
      Exec(ExpandConstant('sc'), ExpandConstant('failure "{#MyAppName}" reset=0 actions=restart/10000/restart/60000/run/1000'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Log('Starting service...');
      WizardForm.StatusLabel.Caption := CustomMessage('StartingApp');
      WizardForm.StatusLabel.Show();
      //Exec(ExpandConstant('{app}\{#MyAppExeName}'), ExpandConstant('start'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Exec(ExpandConstant('sc'), ExpandConstant('start "{#MyAppName}"'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Log(ExpandConstant('Service started with code ' + IntToStr(ResultCode)));
    end
    else
    begin
      Log('Running in console');
      WizardForm.StatusLabel.Caption := CustomMessage('StartingApp');
      WizardForm.StatusLabel.Show();
      Exec(ExpandConstant('{app}\{#MyAppExeName}'), '', '', SW_SHOW, ewNoWait, ResultCode);
      Log(ExpandConstant('Application run with code ' + IntToStr(ResultCode)));
    end;
  end;
end;

function OpenServiceManager() : HANDLE;
begin
    if UsingWinNT() = true then begin
        Result := OpenSCManager('','',SC_MANAGER_ALL_ACCESS);
        if Result = 0 then
            MsgBox('the servicemanager is not available', mbError, MB_OK)
    end
    else begin
            MsgBox('only nt based systems support services', mbError, MB_OK)
            Result := 0;
    end
end;

function IsServiceInstalled(ServiceName: string) : boolean;
var
    hSCM    : HANDLE;
    hService: HANDLE;
begin
    hSCM := OpenServiceManager();
    Result := false;
    if hSCM <> 0 then begin
        hService := OpenService(hSCM, ServiceName, SERVICE_QUERY_CONFIG);
        if hService <> 0 then begin
            Result := true;
            CloseServiceHandle(hService)
        end;
        CloseServiceHandle(hSCM)
    end
end;

function IsServiceRunning(ServiceName: string) : boolean;
var
    hSCM    : HANDLE;
    hService: HANDLE;
    Status  : SERVICE_STATUS;
begin
    hSCM := OpenServiceManager();
    Result := false;
    if hSCM <> 0 then begin
        hService := OpenService(hSCM, ServiceName, SERVICE_QUERY_STATUS);
        if hService <> 0 then begin
            if QueryServiceStatus(hService,Status) then begin
                Result :=(Status.dwCurrentState = SERVICE_RUNNING)
            end;
            CloseServiceHandle(hService)
            end;
        CloseServiceHandle(hSCM)
    end
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode : Integer;
  ServiceInstalled : Boolean;
  ServiceRunning : Boolean;
begin
  // uninstall the service if it's already installed
  ServiceInstalled := IsServiceInstalled(ExpandConstant('{#MyAppName}'));
  ServiceRunning := IsServiceRunning(ExpandConstant('{#MyAppName}'));
  if ServiceInstalled then begin
    Log('Will uninstall existing service...');
    WizardForm.PreparingLabel.Caption := CustomMessage('UninstallingService');
    WizardForm.PreparingLabel.Show();
    if ServiceRunning then begin
      Log('Stopping running service...');
      //Exec(ExpandConstant('{app}\{#MyAppExeName}'), ExpandConstant('stop'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Exec(ExpandConstant('sc'), ExpandConstant('stop "{#MyAppName}"'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Log(ExpandConstant('Service stopped with code ' + IntToStr(ResultCode)));
    end;
    Log('Uninstalling service...');
    //Exec(ExpandConstant('{app}\{#MyAppExeName}'), ExpandConstant('uninstall'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec(ExpandConstant('sc'), ExpandConstant('delete "{#MyAppName}"'), '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Log(ExpandConstant('Service uninstalled with code ' + IntToStr(ResultCode)));
  end;
end;