; Installer.nsi
; Simple NSIS installer for Heating Oil Tracker
; Usage locally: makensis -DVERSION=1.0.0 Installer.nsi
; In CI: makensis -DVERSION=%VERSION% Installer.nsi

!define APP_NAME "Heating Oil Tracker"
!define APP_NAME_SHORT "HeatingOilTracker"
!define EXE_NAME "HeatingOilTracker.exe"
!ifndef VERSION
  !define VERSION "1.0.0"
!endif
!define INSTALLER_EXE "${APP_NAME_SHORT} ${VERSION} Installer.exe"
!define PUBLISH_DIR "..\bin\Release\net9.0-windows" ; relative to the .nsi file
!define ICON_FILE "..\Assets\app.ico"

; Request elevation for per-machine install
RequestExecutionLevel admin

; Modern UI
!include "MUI2.nsh"

; Set installer and uninstaller icons
!define MUI_ICON "${ICON_FILE}"
!define MUI_UNICON "${ICON_FILE}"

Name "${APP_NAME} ${VERSION}"
OutFile "${INSTALLER_EXE}"
InstallDir "$PROGRAMFILES\${APP_NAME_SHORT}"
Icon "${ICON_FILE}"

Page directory        ; allow user to select install dir
Page instfiles

Section "Install"
  SetOutPath "$INSTDIR"

  ; Copy published files
  File /r "${PUBLISH_DIR}\*.*"

  ; Create Start Menu shortcut
  CreateDirectory "$SMPROGRAMS\${APP_NAME}"
  CreateShortCut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\${EXE_NAME}" "" "$INSTDIR\${EXE_NAME}" 0
  CreateShortCut "$SMPROGRAMS\${APP_NAME}\Uninstall.lnk" "$INSTDIR\Uninstall.exe"

  ; Create Desktop shortcut
  CreateShortCut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${EXE_NAME}"

  ; Write uninstall information to registry (appears in Programs and Features)
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME_SHORT}" "DisplayName" "${APP_NAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME_SHORT}" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME_SHORT}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME_SHORT}" "Publisher" "bernpuc"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME_SHORT}" "DisplayIcon" "$INSTDIR\${EXE_NAME}"

  ; Write installed path
  WriteRegStr HKLM "Software\${APP_NAME_SHORT}" "InstallPath" "$INSTDIR"

  ; Create the uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd

Section "Uninstall"
  ; Remove shortcuts
  Delete "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk"
  Delete "$SMPROGRAMS\${APP_NAME}\Uninstall.lnk"
  RMDir "$SMPROGRAMS\${APP_NAME}"

  Delete "$DESKTOP\${APP_NAME}.lnk"

  ; Remove files in install dir
  RMDir /r "$INSTDIR"

  ; Remove registry entries
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME_SHORT}"
  DeleteRegKey HKLM "Software\${APP_NAME_SHORT}"

  ; Delete the uninstaller itself
  Delete "$INSTDIR\Uninstall.exe"
SectionEnd
