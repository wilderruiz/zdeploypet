# ZDeployPet

A friendly Windows + WSL deployment companion. ZDeployPet is designed to unlock approved SSH deployment keys once per bounded session, run allowlisted deployment profiles, monitor structured results, and keep production credentials local.

The current PDA-0 build is intentionally read-only and uses Millenova as its first development profile. It verifies Windows, .NET, WSL, OpenSSH, the Millenova repository, the deploy script fingerprint, and deployment-report storage. It does not create keys, start an SSH agent, change SSH configuration, contact either server, or run a deployment.

The existing `deploy_millenova.sh` remains owned by the Millenova repository and is not copied or modified here.

ZDeployPet is a sibling in the Zomniverse application family, not a component embedded inside ZomniverseGitPet. A future optional integration may exchange repository/profile metadata, but never credentials, SSH-agent access, or deployment authority.

## Development

```powershell
dotnet build ZDeployPet.sln
dotnet test ZDeployPet.sln --no-build
dotnet run --project src/ZDeployPet.App
```

The default discovery targets are documented in `docs/PDA0_DISCOVERY.md`.

Development source lives at `C:\Dev\ZDeployPet`. Release artifacts will be generated outside the repository at `C:\Dev\ZDeployPet_Releases`; runtime binaries and private profiles will live under `%LOCALAPPDATA%\Zomniverse\ZDeployPet`.
