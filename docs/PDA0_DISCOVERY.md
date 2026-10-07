# PDA-0 discovery baseline

Recorded: 2026-10-07 (Europe/Kiev)

- Windows host with .NET SDK `8.0.424` and Windows Desktop runtime `8.0.30`
- WSL distribution: `Ubuntu-24.04` on WSL 2
- WSL OpenSSH: `/usr/bin/ssh`, OpenSSH 9.6p1
- WSL agent tools: `/usr/bin/ssh-agent` and `/usr/bin/ssh-add`
- Millenova repository: `H:\PROGRAMMING\public_html\millenova`
- WSL repository path: `/mnt/h/PROGRAMMING/public_html/millenova`
- Existing deploy script: `deploy_millenova.sh`
- Initial deploy-script SHA-256: `998B218474F52D1DCEB389386503F85E2647B478857A854B9A80AAFE85B4997A`
- Deployment report root: `/home/wilder/.local/state/millenova/deployments`
- Windows Terminal launcher: `%LOCALAPPDATA%\Microsoft\WindowsApps\wt.exe`

The application repeats these checks without mutation. A missing or changed capability is shown as a discovery result; it is not repaired automatically during PDA-0.
