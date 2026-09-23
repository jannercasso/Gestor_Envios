; ============================================
; CONFIGURACIÓN GENERAL
; ============================================
[Setup]
AppId={{A1B2C3D4-1234-4E5F-9A8B-000000000001}}
AppName=Gestor de Envíos Procter
AppVersion=1.0
AppPublisher=ICOLTRANS LTDA
AppPublisherURL=https://www.icoltrans.com
AppSupportURL=https://www.icoltrans.com/soporte
AppUpdatesURL=https://www.icoltrans.com/descargas

DefaultDirName={autopf}\GestorEnvios
DefaultGroupName=Gestor de Envíos Procter

Compression=lzma2
SolidCompression=yes

OutputDir=C:\GestorEnvios_Instalador
OutputBaseFilename=Setup_GestorEnvios_v1.1

Uninstallable=yes
CreateUninstallRegKey=yes
PrivilegesRequired=admin

VersionInfoVersion=1.0.0.0
VersionInfoCompany=ICOLTRANS LTDA
VersionInfoDescription=Gestor de Envíos Procter
VersionInfoCopyright=ICOLTRANS LTDA
VersionInfoProductName=Gestor de Envíos Procter
VersionInfoProductVersion=1.0

SetupIconFile=C:\Users\jcasso\OneDrive - Industria Colombiana de Logistica y Transporte\Escritorio\Proyectos_C#\GestorEnvios\Procter.ico

UninstallFilesDir={app}\Uninstall
UsePreviousAppDir=yes

ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

WizardStyle=modern
DisableWelcomePage=no

AppMutex=GestorEnviosProcterMutex


; ============================================
; IDIOMAS
; ============================================
[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"


; ============================================
; TAREAS OPCIONALES
; ============================================
[Tasks]
Name: "desktopicon"; \
    Description: "Crear icono en el escritorio"; \
    GroupDescription: "Iconos adicionales:"


; ============================================
; ARCHIVOS A INSTALAR
; ============================================
[Files]
Source: "C:\publish\*"; \
    DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs


; ============================================
; ICONOS / ACCESOS DIRECTOS
; ============================================
[Icons]

; Menú Inicio
Name: "{group}\Gestor de Envíos Procter"; \
    Filename: "{app}\GestorEnvios.exe"; \
    WorkingDir: "{app}"

; Desinstalador
Name: "{group}\Desinstalar Gestor de Envíos"; \
    Filename: "{uninstallexe}"

; Escritorio - opcional
Name: "{commondesktop}\Gestor de Envíos Procter"; \
    Filename: "{app}\GestorEnvios.exe"; \
    WorkingDir: "{app}"; \
    Tasks: desktopicon


; ============================================
; EJECUTAR DESPUÉS DE INSTALAR
; ============================================
[Run]
Filename: "{app}\GestorEnvios.exe"; \
    Description: "Ejecutar Gestor de Envíos Procter"; \
    Flags: postinstall nowait skipifsilent


; ============================================
; DESINSTALACIÓN
; ============================================
[UninstallDelete]
Type: filesandordirs; \
    Name: "{app}"


; ============================================
; CÓDIGO PERSONALIZADO
; ============================================
[Code]

procedure InitializeWizard;
begin
  WizardForm.Caption := 'Instalador de Gestor de Envíos Procter';
end;
