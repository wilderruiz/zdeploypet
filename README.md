# ZDeployPet

A friendly Windows + WSL deployment companion. ZDeployPet is designed to unlock approved SSH deployment keys once per bounded session, run allowlisted deployment profiles, monitor structured results, and keep production credentials local.

The current PDA-0A build is intentionally read-only. First run creates a validated local deployment profile, then discovery verifies Windows, .NET, WSL, OpenSSH, the selected project, approved script fingerprint, and optional deployment-report storage. It does not create keys, start an SSH agent, change SSH configuration, contact a server, or run a deployment.

The existing `deploy_millenova.sh` remains owned by the Millenova repository and is not copied or modified here.

ZDeployPet is a sibling in the Zomniverse application family, not a component embedded inside ZomniverseGitPet. A future optional integration may exchange repository/profile metadata, but never credentials, SSH-agent access, or deployment authority.

## Development

```powershell
dotnet build ZDeployPet.sln
dotnet test ZDeployPet.sln --no-build
dotnet run --project src/ZDeployPet.App
```

Discovery capabilities are documented in `docs/PDA0_DISCOVERY.md`; community profile storage and validation are documented in `docs/PROFILES.md`.

Release artifacts are generated outside the source repository; runtime binaries and private profiles live under `%LOCALAPPDATA%\Zomniverse\ZDeployPet`.
