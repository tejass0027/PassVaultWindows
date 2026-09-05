# PassVault for Windows

A local, offline Windows password manager - the desktop counterpart to the PassVault Android
app. Everything is stored only on this PC, encrypted, protected by a pattern lock (plus
optional Windows Hello), with security questions as a recovery option if you ever forget your
pattern or move to a new PC.

## How to open and run it

This project was written without access to Visual Studio/the .NET SDK on the machine that
generated it, so it hasn't been compiled yet. To build and run it:

1. Install **Visual Studio 2022 Community** (free) with the **".NET desktop development"**
   workload checked during setup - this gets you the .NET 8 SDK, WPF tooling, and a debugger
   in one go. (Alternative: just the [.NET 8 SDK](https://dotnet.microsoft.com/download) plus
   VS Code, then `dotnet build` / `dotnet run` from a terminal in the `PassVaultWindows` folder.)
2. Open `PassVaultWindows.sln` in Visual Studio.
3. Let it restore NuGet packages (there's only the .NET SDK's own Windows projections - no
   third-party packages - so this should be quick and needs internet only for that first step).
4. Press **F5** (or the green Run arrow) to build and launch it.

If the first build fails with a compile error, that's expected risk of generating a project
without a compiler in the loop - copy the exact error text back and it can be fixed directly.

## Manual test checklist

1. First launch → Welcome screen → draw a pattern (4+ dots) → confirm it → set 3 security
   questions and answers → optionally enable Windows Hello (only offered if this PC has it set up).
2. App lands on the vault list, unlocked.
3. Click the lock icon (top-left) → app returns to the login screen.
4. Log back in: Windows Hello prompt (if enabled) → draw your pattern → back in the vault.
5. Add a password entry (title/password required); try "Generate" for a random password.
6. Open the entry → reveal/hide the password → copy it → confirm it clears from the clipboard
   after 30 seconds.
7. Switch to another window and back after your auto-lock timeout → should be locked again.
8. Settings → Export encrypted backup → set a backup password → save the `.pvbk` file.
9. Settings → Erase all data → confirm → back at the Welcome screen with nothing left.
10. Onboard again, then Settings → Import backup → enter the backup password → pick the file
    → confirm replace → your entries are back.
11. **Cross-platform check**: export a backup from the Android app, then import that exact
    `.pvbk` file here (or vice versa) → entries should appear correctly on both sides.
12. From the login screen, click "Forgot pattern?" → answer your 3 security questions → draw
    and confirm a new pattern → back in the vault with the same data.
13. Photo vault (camera icon on the vault list) → add a photo from a file → it appears in the
    grid → click it to view full-size → delete it.

## Security model

Mirrors the Android app's design exactly, so a backup file interchanges cleanly between them:

- **Data Encryption Key (DEK)**: one random 256-bit key generated once, used to encrypt the
  vault (AES-256-GCM via .NET's built-in `AesGcm`). Never derived from your pattern or
  Windows Hello directly.
- **Two wrapped copies of the DEK**: one wrapped under a key derived from your pattern
  (PBKDF2-HMAC-SHA256, salted, 210,000 iterations via `Rfc2898DeriveBytes`), one wrapped under
  a key derived the same way from your security question answers. Either path unlocks the
  same vault.
- **Windows Hello is a device-level gate**, not a key source - like on Android, it's used to
  gate the pattern step rather than to derive the encryption key. If Windows Hello isn't set
  up on this PC (or the WinRT API can't fully activate without package identity on some
  configurations), the app simply skips straight to the pattern step.
- **At rest**: a single `vault.dat` file (and a `photos_index.dat` + one file per photo) under
  `%LOCALAPPDATA%\PassVault\`, AES-GCM encrypted. Metadata (salts, wrapped DEK copies, security
  question text, settings, login activity) lives in a file protected by the **Windows Data
  Protection API** (tied to your Windows user account) - the desktop analogue of the Android
  app's Keystore-backed encrypted storage.
- **No network access at all** - the app makes no HTTP calls anywhere in the codebase, so
  nothing ever leaves this PC except via the manual, password-protected `.pvbk` export you
  trigger yourself.
- **Cross-platform backups**: the `.pvbk` file format and key-derivation parameters are
  byte-identical to the Android app's, so a backup exported from one imports cleanly on the
  other. For best compatibility, stick to plain ASCII characters (letters, numbers, common
  symbols) in your backup password and security answers.
- Copied passwords auto-clear from the clipboard after 30 seconds.

## Project layout

- `Crypto/` - AES-GCM helpers and PBKDF2 key derivation (byte-compatible with the Android app).
- `Auth/` - pattern, security-question, and Windows Hello managers; DPAPI-protected settings storage.
- `Data/` - the `Credential`/`VaultPhoto` models, the encrypted vault and photo stores, and `.pvbk` backup export/import.
- `Controls/` - the custom pattern-lock control and a reusable masked-password field with a show/hide toggle.
- `Views/` - one WPF UserControl per screen (onboarding, login, vault, settings, etc.).
- `AppState.cs` - single shared session/state holder (mirrors the Android app's `VaultViewModel`).
- `MainWindow.xaml.cs` - screen navigation and the auto-lock logic (mirrors the Android app's nav graph).
