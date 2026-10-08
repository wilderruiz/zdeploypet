# ZDeployPet

A friendly Windows + WSL deployment companion. ZDeployPet is designed to unlock approved SSH deployment keys once per bounded session, run allowlisted deployment profiles, monitor structured results, and keep production credentials local.

The current PDA-0A build is intentionally read-only. First run creates a validated local deployment profile, then discovery verifies Windows, .NET, WSL, OpenSSH, the selected project, approved script fingerprint, and optional deployment-report storage. It does not create keys, start an SSH agent, change SSH configuration, contact a server, or run a deployment.

The existing `deploy_millenova.sh` remains owned by the Millenova repository and is not copied or modified here.

ZDeployPet is a sibling in the Zomniverse application family, not a component embedded inside ZomniverseGitPet. A future optional integration may exchange repository/profile metadata, but never credentials, SSH-agent access, or deployment authority.

## Development

Open `ZDeployPet.code-workspace` in VS Code. For the normal edit/test loop, fully exit ZDeployPet and run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\publish-local.ps1
```

The script builds the solution, runs both regression suites, and publishes a self-contained single EXE to:

```text
C:\Dev\ZDeployPet_Releases\current\ZDeployPet.exe
```

Launch that EXE for visual smoke tests. If a previous build is stuck, inspect and stop it before publishing:

```powershell
Get-Process -Name ZDeployPet -ErrorAction SilentlyContinue | Select-Object Id,ProcessName,Path
Stop-Process -Name ZDeployPet -Force -ErrorAction SilentlyContinue
```

To run the checks manually:

```powershell
dotnet build ZDeployPet.sln -c Release
dotnet run --project tests\ZDeployPet.Core.Tests\ZDeployPet.Core.Tests.csproj -c Release
dotnet run --project tests\ZDeployPet.Infrastructure.Tests\ZDeployPet.Infrastructure.Tests.csproj -c Release
```

Discovery capabilities are documented in `docs/PDA0_DISCOVERY.md`; community profile storage and validation are documented in `docs/PROFILES.md`.

Release artifacts are generated outside the source repository; runtime binaries and private profiles live under `%LOCALAPPDATA%\Zomniverse\ZDeployPet`.
