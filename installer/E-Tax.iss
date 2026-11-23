; Inno Setup script cho E-Tax. Biên dịch bằng installer\publish-v1.ps1 (truyền /DAppVersion, /DSourceDir, /DOutputDir).
; Cài theo từng người dùng, không cần quyền admin. Gỡ cài đặt KHÔNG xóa dữ liệu trong %AppData%\E-Tax và %LocalAppData%\E-Tax.
#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\publish\E-Tax-win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts"
#endif

[Setup]
AppId={{6F1B7C52-3A94-4E0D-9B8A-5D2C71E4A3F9}
AppName=E-Tax
AppVersion={#AppVersion}
AppPublisher=E-Tax
DefaultDirName={localappdata}\Programs\E-Tax
DefaultGroupName=E-Tax
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=E-Tax-{#AppVersion}-win-x64-setup
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\E-Tax.exe
DisableProgramGroupPage=yes

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\E-Tax"; Filename: "{app}\E-Tax.exe"

[Run]
Filename: "{app}\E-Tax.exe"; Description: "Mở E-Tax"; Flags: nowait postinstall skipifsilent

; Không có mục [UninstallDelete] cho %AppData%\E-Tax hay %LocalAppData%\E-Tax: dữ liệu người dùng được giữ lại.
