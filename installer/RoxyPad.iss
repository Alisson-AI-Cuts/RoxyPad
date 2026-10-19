[Setup]
AppId={{5FC45958-3960-4DE0-8157-4ED2559C8AA1}
AppName=RoxyPad
AppVersion=0.6.1
AppPublisher=Alisson-AI-Cuts
AppPublisherURL=https://github.com/Alisson-AI-Cuts/RoxyPad
AppSupportURL=https://github.com/Alisson-AI-Cuts/RoxyPad/issues
DefaultDirName={localappdata}\Programs\RoxyPad
DefaultGroupName=RoxyPad
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
OutputDir=..\dist
OutputBaseFilename=RoxyPad-0.6.1-Setup-x64
SetupIconFile=..\src\Assets\RoxyPad.ico
UninstallDisplayIcon={app}\RoxyPad.exe
LicenseFile=..\LICENSE.txt
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
UninstallDisplayName=RoxyPad

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; Flags: unchecked

[Files]
Source: "..\dist\payload\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\RoxyPad"; Filename: "{app}\RoxyPad.exe"
Name: "{group}\Primeiros passos"; Filename: "{app}\Primeiros-passos.html"
Name: "{group}\Desinstalar RoxyPad"; Filename: "{uninstallexe}"
Name: "{autodesktop}\RoxyPad"; Filename: "{app}\RoxyPad.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ThirdParty\VB-CABLE\VBCABLE_Setup_x64.exe"; Description: "Abrir instalador oficial do VB-CABLE (se ainda não instalado; requer administrador e reinício)"; Flags: postinstall unchecked shellexec skipifsilent; Verb: "runas"
Filename: "{app}\Primeiros-passos.html"; Description: "Abrir guia de primeiros passos"; Flags: postinstall shellexec skipifsilent

[Code]
function InitializeSetup(): Boolean;
var Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
  if not Result then MsgBox('Instale o .NET Framework 4.8 pelo Windows Update antes de continuar.', mbError, MB_OK);
end;
