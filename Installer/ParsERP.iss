; =====================================================
; Inno Setup Script for ParsERP
; نصب استاندارد ویندوز + انتخاب مسیر دیتابیس و بکاپ
; Release target: ParsERP 1.2.0
; =====================================================

#define MyAppName "پارس‌ئی‌آرپی"
#define MyAppNameEn "ParsERP"
#define MyAppVersion "1.2.0"
#define MyAppPublisher "ParsERP"
#define MyAppURL "https://github.com/pasargad1924-ui/ParsERP"
#define MyAppExeName "ParsERP.exe"

[Setup]
AppId={{A8F3C2E1-9B4D-4F7A-8E2C-1D5B9A0F3E7C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppNameEn}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=ParsERP_Setup_{#MyAppVersion}
SetupIconFile=
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
ArchitecturesInstallIn64BitMode=x64
DirExistsWarning=no
DisableProgramGroupPage=no
LicenseFile=
InfoBeforeFile=
InfoAfterFile=

; زبان فارسی
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "ایجاد میانبر روی دسکتاپ"; GroupDescription: "میانبرها:"; Flags: checkedonce
Name: "quicklaunchicon"; Description: "ایجاد میانبر در Quick Launch"; GroupDescription: "میانبرها:"; Flags: unchecked

[Files]
; فایل اجرایی و وابستگی‌ها (بعد از Publish)
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\حذف نصب {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; ثبت مسیر نصب برای پیدا کردن برنامه
Root: HKLM; Subkey: "Software\{#MyAppNameEn}"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\{#MyAppNameEn}"; ValueType: string; ValueName: "Version"; ValueData: "{#MyAppVersion}"

[Code]
var
  DataDirPage: TInputDirWizardPage;
  BackupDirPage: TInputDirWizardPage;

procedure InitializeWizard;
begin
  { صفحه انتخاب مسیر دیتابیس }
  DataDirPage := CreateInputDirPage(wpSelectDir,
    'مسیر دیتابیس',
    'محل ذخیره فایل دیتابیس برنامه را مشخص کنید.',
    'پیشنهاد می‌شود مسیری خارج از پوشه Program Files انتخاب شود (مثلاً Documents یا یک درایو دیگر).',
    False, '');
  DataDirPage.Add('مسیر پوشه دیتابیس:');
  DataDirPage.Values[0] := ExpandConstant('{userdocs}\ParsERP\Data');

  { صفحه انتخاب مسیر پشتیبان }
  BackupDirPage := CreateInputDirPage(DataDirPage.ID,
    'مسیر پشتیبان‌گیری',
    'محل ذخیره فایل‌های پشتیبان را مشخص کنید.',
    'این مسیر برای تهیه نسخه پشتیبان خودکار و دستی استفاده می‌شود.',
    False, '');
  BackupDirPage.Add('مسیر پوشه پشتیبان:');
  BackupDirPage.Values[0] := ExpandConstant('{userdocs}\ParsERP\Backup');
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = DataDirPage.ID then
  begin
    if DataDirPage.Values[0] = '' then
    begin
      MsgBox('لطفاً مسیر دیتابیس را مشخص کنید.', mbError, MB_OK);
      Result := False;
    end;
  end;
  if CurPageID = BackupDirPage.ID then
  begin
    if BackupDirPage.Values[0] = '' then
    begin
      MsgBox('لطفاً مسیر پشتیبان را مشخص کنید.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigFile: string;
  DataPath: string;
  BackupPath: string;
begin
  if CurStep = ssPostInstall then
  begin
    DataPath := DataDirPage.Values[0];
    BackupPath := BackupDirPage.Values[0];

    { ایجاد پوشه‌ها }
    ForceDirectories(DataPath);
    ForceDirectories(BackupPath);

    { نوشتن فایل تنظیمات اولیه }
    ConfigFile := ExpandConstant('{app}\config.ini');
    SaveStringToFile(ConfigFile,
      '[Paths]' + #13#10 +
      'Database=' + DataPath + '\ParsERP.db' + #13#10 +
      'Backup=' + BackupPath + #13#10 +
      #13#10 +
      '[App]' + #13#10 +
      'Version={#MyAppVersion}' + #13#10 +
      'Language=fa' + #13#10, False);
  end;
end;

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "اجرای {#MyAppName}"; Flags: nowait postinstall skipifsilent
