; Soundboard Inno Setup Script
; Compile with: ISCC.exe soundboard.iss
; NOTE: Keep MyAppVersion in sync with <Version> in SoundboardApp.csproj

#define MyAppName "Soundboard"
#define MyAppVersion "1.0.3"
#define MyAppPublisher "Michael Cassidy"
#define MyAppExeName "Soundboard.exe"
#define MyAppURL "https://github.com/cassidymichael/SoundboardApp"

[Setup]
AppId={{b0fda393-8b65-432f-9a57-6c41282d0562}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Output settings
OutputDir=..\dist
OutputBaseFilename=SoundboardSetup-{#MyAppVersion}
; Compression
Compression=lzma2/ultra64
SolidCompression=yes
; Modern installer look
WizardStyle=modern
; Require admin for Program Files install, but allow per-user
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
; Uninstaller
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start with Windows"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Include all files from the publish output
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent



