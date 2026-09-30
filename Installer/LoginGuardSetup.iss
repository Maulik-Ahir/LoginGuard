; =====================================================================
; LoginGuard Inno Setup Installer Script
; Windows Laptop Security Tool
; Target: 64-bit Windows (.NET 10)
; =====================================================================

#define MyAppName "LoginGuard"
#define MyAppVersion "0.7.0"
#define MyAppPublisher "Maulik"
#define MyAppExeName "LoginGuardUI.exe"

[Setup]
AppId={{A142D054-0C9F-4D28-895E-4B07DE254F2E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; 64-bit mode directive is required to avoid 32-bit Program Files redirection
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
PrivilegesRequired=admin
OutputDir=..\installer_output
OutputBaseFilename=LoginGuardSetup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\UI\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Service published binaries
Source: "..\publish\Service\*"; DestDir: "{app}\Service"; Flags: ignoreversion recursesubdirs createallsubdirs
; UI published binaries
Source: "..\publish\UI\*"; DestDir: "{app}\UI"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\UI\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\UI\{#MyAppExeName}"; Tasks: desktopicon

[Dirs]
Name: "{sd}\CameraSpikeLog\Captures"; Permissions: system-full admins-full
Name: "{sd}\CameraSpikeLog\PendingNotifications"; Permissions: system-full admins-full

[Run]
; Register and configure Windows Service
Filename: "{sys}\sc.exe"; Parameters: "create LoginGuardService binPath= ""{app}\Service\LoginGuardService.exe"" start= auto"; Flags: runhidden; StatusMsg: "Registering LoginGuard Windows Service..."
Filename: "{sys}\sc.exe"; Parameters: "description LoginGuardService ""LoginGuard Windows Laptop Security & Alerting Service"""; Flags: runhidden
; Start the Windows Service
Filename: "{sys}\net.exe"; Parameters: "start LoginGuardService"; Flags: runhidden; StatusMsg: "Starting LoginGuard Service..."
; Launch Monitoring UI option
Filename: "{app}\UI\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop and remove Windows Service cleanly before uninstalling files
Filename: "{sys}\sc.exe"; Parameters: "stop LoginGuardService"; Flags: runhidden; RunOnceId: "StopLoginGuardService"
Filename: "{sys}\sc.exe"; Parameters: "delete LoginGuardService"; Flags: runhidden; RunOnceId: "DeleteLoginGuardService"

[UninstallDelete]
Type: files; Name: "{app}\Service\appsettings.Secrets.json"
Type: files; Name: "{app}\Service\*.tmp*"

[Code]
var
  TelegramPage: TInputQueryWizardPage;

procedure InitializeWizard;
begin
  // Custom Wizard Page to securely collect Telegram credentials during install
  TelegramPage := CreateInputQueryPage(
    wpSelectDir,
    'Telegram Alert Configuration',
    'Enter your Telegram Bot credentials',
    'LoginGuard uses a Telegram bot to send instant evidence photos and receive remote /lock commands.' + #13#10 +
    'These credentials are saved locally to appsettings.Secrets.json and are never shared or sent to external servers.'
  );
  TelegramPage.Add('Telegram Bot Token:', True);
  TelegramPage.Add('Authorized Chat ID:', False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  SecretsFile: string;
  TokenVal: string;
  ChatIdVal: string;
  JsonContent: string;
begin
  if CurStep = ssPostInstall then
  begin
    TokenVal := Trim(TelegramPage.Values[0]);
    ChatIdVal := Trim(TelegramPage.Values[1]);
    SecretsFile := ExpandConstant('{app}\Service\appsettings.Secrets.json');

    // Sanitize string escaping for JSON
    StringChange(TokenVal, '\', '\\');
    StringChange(TokenVal, '"', '\"');
    StringChange(ChatIdVal, '\', '\\');
    StringChange(ChatIdVal, '"', '\"');

    // Generate appsettings.Secrets.json with provided credentials or defaults
    JsonContent := 
      '{' + #13#10 +
      '  "Telegram": {' + #13#10 +
      '    "BotToken": "' + TokenVal + '",' + #13#10 +
      '    "ChatId": "' + ChatIdVal + '"' + #13#10 +
      '  },' + #13#10 +
      '  "Camera": {' + #13#10 +
      '    "DeviceIndex": 0' + #13#10 +
      '  },' + #13#10 +
      '  "Storage": {' + #13#10 +
      '    "CaptureRetentionDays": 30,' + #13#10 +
      '    "LogRetentionDays": 15' + #13#10 +
      '  }' + #13#10 +
      '}';

    SaveStringToFile(SecretsFile, JsonContent, False);
  end;
end;
