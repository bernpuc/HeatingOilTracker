# Building Installers — Heating Oil Tracker (MAUI)

## Prerequisites

| Tool | Purpose |
|---|---|
| .NET 9 SDK | Build and publish |
| MAUI workload (`dotnet workload install maui`) | MAUI targets |
| Android SDK (via Visual Studio or `dotnet workload install android`) | Android APK/AAB |
| NSIS (`makensis`) | Windows desktop installer |

All commands are run from the repo root unless otherwise noted.

> **SDK version**: A `global.json` at the repo root pins the .NET SDK to 9.x. If you have .NET 10 installed alongside .NET 9, this prevents the wrong SDK from being picked up during MAUI builds.

---

## Android (APK)

The keystore file `heatingoiltracker.keystore` is located in the repo root and is **not committed to git**. Keep it backed up securely.

### Build a signed APK

```bash
dotnet publish HeatingOilTracker.Maui -f net9.0-android -c Release \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=..\heatingoiltracker.keystore \
  -p:AndroidSigningKeyAlias=heatingoiltracker \
  -p:AndroidSigningKeyPass=<keystore-password> \
  -p:AndroidSigningStorePass=<keystore-password>
```

Output: `HeatingOilTracker.Maui\bin\Release\net9.0-android\com.bernpuc.heatingoiltracker-Signed.apk`

### Install directly to a connected device

```bash
adb install "HeatingOilTracker.Maui\bin\Release\net9.0-android\com.bernpuc.heatingoiltracker-Signed.apk"
```

### Notes

- The device must have **Install from unknown sources** enabled (Settings → Security) for sideloaded APKs.
- To target a specific ABI (smaller file): add `-p:RuntimeIdentifiers=android-arm64` for ARM64-only.
- For Google Play submission, build an AAB instead: add `-p:AndroidPackageFormat=aab`.

---

## Windows Desktop (NSIS Installer)

The NSIS installer produces a standard `.exe` that anyone can install without a certificate.

### Step 1 — Publish unpackaged

```bash
dotnet publish HeatingOilTracker.Maui -f net9.0-windows10.0.19041.0 -c Release \
  -p:WindowsPackageType=None
```

Output directory: `HeatingOilTracker.Maui\bin\Release\net9.0-windows10.0.19041.0\win10-x64\publish\`

### Step 2 — Build the installer

```bash
cd HeatingOilTracker.Maui\Package
makensis -DVERSION=2.0.3 Installer.nsi
```

Output: `HeatingOilTracker.Maui\Package\HeatingOilTrackerMaui 2.0.3 Installer.exe`

### Notes

- Bump `-DVERSION` to match `ApplicationDisplayVersion` in the `.csproj`.
- Windows SmartScreen may warn on first run since the installer is unsigned. Users click **More info → Run anyway**.
- The installer registers in **Apps & Features** and creates Start Menu + Desktop shortcuts.
- To silently install (e.g. for scripted deployment): run the installer with `/S`.

---

## Windows Desktop (MSIX — sideload, own machine only)

Use this when installing on a machine where the signing certificate is already trusted.

### Prerequisites (one-time per machine)

1. Import the signing certificate as trusted (run PowerShell as Administrator):

```powershell
$pwd = Read-Host "Enter PFX password" -AsSecureString
Import-PfxCertificate `
  -FilePath "HeatingOilTracker.Maui\HeatingOilTracker.pfx" `
  -CertStoreLocation "Cert:\LocalMachine\Root" `
  -Password $pwd
```

2. Import into your personal store so the build can sign:

```powershell
Import-PfxCertificate `
  -FilePath "HeatingOilTracker.Maui\HeatingOilTracker.pfx" `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -Password $pwd
```

### Build the MSIX

When bumping the version, always do a full rebuild first to avoid stale artifacts:

```bash
dotnet build HeatingOilTracker.Maui -f net9.0-windows10.0.19041.0 -c Release --no-incremental
dotnet publish HeatingOilTracker.Maui -f net9.0-windows10.0.19041.0 -c Release
```

Output: `HeatingOilTracker.Maui\bin\Release\net9.0-windows10.0.19041.0\win10-x64\AppPackages\HeatingOilTracker.Maui_<version>_Test\HeatingOilTracker.Maui_<version>_x64.msix`

### Install

Run the helper script from PowerShell (handles certificate trust automatically):

```powershell
powershell -ExecutionPolicy Bypass -File `
  "HeatingOilTracker.Maui\bin\Release\net9.0-windows10.0.19041.0\win10-x64\AppPackages\HeatingOilTracker.Maui_<version>_Test\Add-AppDevPackage.ps1" `
  -ForceUpdateFromAnyVersion
```

Or double-click the `.msix` to install via Windows App Installer (requires certificate already trusted).

### Certificate details

| Property | Value |
|---|---|
| Subject | `CN=bernpuc` |
| Thumbprint | `9155E78756A0A158B5A492F8A8F206E309EAD2C9` |
| Key file | `HeatingOilTracker.Maui\HeatingOilTracker.pfx` (not in git) |
| Expires | 5 years from creation (March 2031) |
