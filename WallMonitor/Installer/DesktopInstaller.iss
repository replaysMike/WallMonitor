#define MyAppName "Wall Monitor Desktop"
#define MyAppVersion "0.0"
#define MyAppPublisher "Wall Monitor"
#define MyAppURL "https://github.com/replaysMike/WallMonitor/"
#define MyAppExeName "WallMonitor.Desktop.exe"

[Setup]
AppId={{290BE016-9076-47DB-B4C8-8BF32396AC2D}
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
OutputBaseFilename=WallMonitor-Desktop-win10x64-{#MyAppVersion}
SetupIconFile=.\icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}
WizardImageFile=.\WizardLarge.bmp
WizardSmallImageFile=.\WizardSmall.bmp
CloseApplications=force
UsePreviousTasks=no


[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel2=The System Monitor Desktop provides a user interface to view the status of multiple System Monitor services.%n%nIt does not perform any monitoring directly, it must be connected to System Monitor services.%n%nThis will install [name/ver] on your computer.

[CustomMessages]
UninstallingService=Uninstalling existing {#MyAppName}...
InstallingService=Installing {#MyAppName} {#MyAppVersion}...
StartingApp=Starting {#MyAppName}...

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "keepconfiguration"; Description: "Keep existing configuration"
Name: "installservice"; Description: "Install {#MyAppName}"

[Files]
Source: "..\Ui\WallMonitor.Desktop\bin\Release\net7.0\win10-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
//Filename: https://localhost:8090; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: postinstall shellexec skipifsilent runhidden

[UninstallRun]
//Filename: "{app}\{#MyAppExeName}"; Parameters: "uninstall -servicename {#MyAppName}"; RunOnceId: "{#MyAppName}"; Flags: runascurrentuser runhidden

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
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode : Integer;
  ServiceInstalled : Boolean;
  ServiceRunning : Boolean;
begin
end;