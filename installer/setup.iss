[Setup]
AppName=Network Usage Monitor
AppVersion=1.0.0
AppVerName=Network Usage Monitor 1.0.0
AppPublisher=Network Usage Monitor
DefaultDirName={autopf}\NetworkUsage
DefaultGroupName=Network Usage Monitor
OutputDir=..\bin\installer
OutputBaseFilename=NetworkUsageSetup-1.0.0
Compression=lzma
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
DisableReadyPage=yes
SetupIconFile=..\icon.ico
UninstallDisplayIcon={app}\NetworkUsage.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode
Name: "startup"; Description: "Start with Windows"; GroupDescription: "Startup Options:"; Flags: checked

[Files]
Source: "..\bin\Release\net8.0-windows\NetworkUsage.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\net8.0-windows\NetworkUsage.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\net8.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\net8.0-windows\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\net8.0-windows\*.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\net8.0-windows\*.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion isreadme

[Icons]
Name: "{group}\Network Usage Monitor"; Filename: "{app}\NetworkUsage.exe"
Name: "{group}\{cm:UninstallProgram,Network Usage Monitor}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Network Usage Monitor"; Filename: "{app}\NetworkUsage.exe"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\Network Usage Monitor"; Filename: "{app}\NetworkUsage.exe"; Tasks: quicklaunchicon

[Registry]
; Auto-start with Windows registry entry
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "NetworkUsage"; ValueData: """{app}\NetworkUsage.exe"" --minimized"; Tasks: startup

; Application settings
Root: HKCU; Subkey: "Software\NetworkUsage"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"
Root: HKCU; Subkey: "Software\NetworkUsage"; ValueType: string; ValueName: "Version"; ValueData: "1.0.0"
Root: HKCU; Subkey: "Software\NetworkUsage"; ValueType: dword; ValueName: "AutoStart"; ValueData: 1; Tasks: startup

[UninstallDelete]
Type: files; Name: "{app}\*.log"
Type: files; Name: "{app}\*.tmp"
Type: filesandordirs; Name: "{userappdata}\NetworkUsage"

[Run]
Filename: "{app}\NetworkUsage.exe"; Parameters: "--minimized"; Description: "{cm:LaunchProgram,Network Usage Monitor}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Configure Windows Firewall exception if needed
    // This would be implemented for network monitoring permissions
  end;
end;

function InitializeSetup(): Boolean;
begin
  // Check for .NET 8.0 runtime
  if not IsDotNetDetected('.NETCoreApp,Version=v8.0', 0) then
  begin
    if MsgBox('Microsoft .NET 8.0 is required. Do you want to download and install it now?', 
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOW, ewNoWait, ErrorCode);
    end;
    Result := False;
  end
  else
    Result := True;
end;
