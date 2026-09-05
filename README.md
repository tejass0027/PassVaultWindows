# PassVault for Windows

A local, offline Windows password manager - the desktop counterpart to the PassVault Android
app. Everything is stored only on this PC, encrypted, protected by a pattern lock (plus optional
Windows Hello), with security questions as a recovery option if you ever forget your pattern or
move to a new PC.

## What it does

- Stores your passwords (title, username, password, URL, notes) in an encrypted vault, unlocked
  by drawing your pattern (or Windows Hello, if you enable it).
- Generates strong random passwords and shows a strength meter while you type your own.
- Copies a password to the clipboard and auto-clears it after 30 seconds.
- Keeps a separate encrypted photo vault for pictures you want stored the same secure way.
- Logs login activity, including failed attempts, so you can see if someone tried to get in.
- Lets you export/import an encrypted backup file (`.pvbk`) - the same format the Android app
  uses, so you can move your passwords between your phone and this PC.
- Light/Dark/System theme toggle.
- No network access at all - nothing leaves this PC except a backup file you explicitly export.
