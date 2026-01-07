# Steam Mobile Authenticator (SMA)

A cross‑platform .NET MAUI application for managing Steam Guard, trade confirmations, and maFile operations.  
Supports **Windows** and **Android**, works fully offline, and does not require a browser or any third‑party services.

# SDA

This repository is a modern MAUI port of  
https://github.com/Jessecar96/SteamDesktopAuthenticator  
Some parts of the codebase are reused, while the rest is written manually from scratch.

---

## 🚀 Features

### 🔐 Steam Guard
- One‑time code generation (TOTP)
- Automatic time synchronization with Steam servers
- Multiple account support
- Quick code copying

### 📦 maFile Management
- Import maFile via FilePicker (Android/Windows)
- Encrypt/decrypt `manifest.json`
- Manage accounts stored inside the manifest
- Transfer maFiles between devices

### 🔔 Trade Confirmations
- Manual confirmation checking
- Automatic periodic checking
- Popup notifications for new confirmations
- Optional auto‑confirmation

### 🖥 Cross‑Platform UI
- Windows: full desktop experience
- Android: adaptive mobile‑friendly interface

---

## 📥 Installation

### 🟦 Windows
Download `com.f1enddd.sma-Signed.apk` from the Releases section and install the application.

### 🟩 Android
Download the **APK**:
- `com.f1enddd.sma-Signed.apk`

Enable installation from unknown sources if required.

---

## 📸 Screenshots

*(Add your screenshots here once they are ready)*

---

## 🛠 Build Instructions

### Windows
```bash
dotnet publish -f net8.0-windows10.0.19041.0 -c Release -p:WindowsPackageType=None
