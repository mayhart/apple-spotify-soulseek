; ============================================================================
; installer.nsi — NSIS installer script for Spotseek
; ============================================================================
; Called by build-win.ps1. Defines:
;   /DVERSION        — e.g. 1.0.0
;   /DPUBLISH_DIR    — path to dotnet publish output
;   /DOUTPUT_DIR     — where the final .exe will be placed

!define APP_NAME     "Spotseek"
!define PUBLISHER    "Spotseek"
!define REG_KEY      "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"

Name "${APP_NAME}"
OutFile "${OUTPUT_DIR}\${APP_NAME}-${VERSION}-Setup.exe"
InstallDir "$PROGRAMFILES64\${APP_NAME}"
InstallDirRegKey HKLM "${REG_KEY}" "InstallLocation"
RequestExecutionLevel admin
SetCompressor /SOLID lzma

; ── Pages ─────────────────────────────────────────────────────────────────
Page directory
Page instfiles
UninstPage uninstConfirm
UninstPage instfiles

; ── Install ────────────────────────────────────────────────────────────────
Section "Install"
    SetOutPath "$INSTDIR"
    File /r "${PUBLISH_DIR}\*.*"

    ; Start Menu shortcut
    CreateDirectory "$SMPROGRAMS\${APP_NAME}"
    CreateShortcut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\Spotseek.exe"
    CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\Spotseek.exe"

    ; Write uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"

    ; Registry entries for Add/Remove Programs
    WriteRegStr HKLM "${REG_KEY}" "DisplayName"      "${APP_NAME}"
    WriteRegStr HKLM "${REG_KEY}" "DisplayVersion"   "${VERSION}"
    WriteRegStr HKLM "${REG_KEY}" "Publisher"        "${PUBLISHER}"
    WriteRegStr HKLM "${REG_KEY}" "InstallLocation"  "$INSTDIR"
    WriteRegStr HKLM "${REG_KEY}" "UninstallString"  '"$INSTDIR\Uninstall.exe"'
    WriteRegDWORD HKLM "${REG_KEY}" "NoModify" 1
    WriteRegDWORD HKLM "${REG_KEY}" "NoRepair"  1
SectionEnd

; ── Uninstall ──────────────────────────────────────────────────────────────
Section "Uninstall"
    Delete "$INSTDIR\Spotseek.exe"
    Delete "$INSTDIR\Uninstall.exe"
    RMDir /r "$INSTDIR"

    Delete "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk"
    RMDir  "$SMPROGRAMS\${APP_NAME}"
    Delete "$DESKTOP\${APP_NAME}.lnk"

    DeleteRegKey HKLM "${REG_KEY}"
SectionEnd
