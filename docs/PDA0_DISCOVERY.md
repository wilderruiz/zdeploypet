# PDA-0 discovery baseline

Recorded: 2026-10-07 (Europe/Kiev)

- Windows host with .NET SDK `8.0.424` and Windows Desktop runtime `8.0.30`
- WSL distribution: `Ubuntu-24.04` on WSL 2
- WSL OpenSSH: `/usr/bin/ssh`, OpenSSH 9.6p1
- WSL agent tools: `/usr/bin/ssh-agent` and `/usr/bin/ssh-add`
- Development-profile repository, WSL path, runner and report root were verified locally and are intentionally omitted from public source.
- Initial runner fingerprint was recorded locally during acceptance and remained unchanged.
- Windows Terminal launcher: `%LOCALAPPDATA%\Microsoft\WindowsApps\wt.exe`

The application repeats these checks without mutation. A missing or changed capability is shown as a discovery result; it is not repaired automatically during PDA-0.
