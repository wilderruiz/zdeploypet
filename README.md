# Millenova Deploy Console

Standalone Windows desktop companion for the existing Millenova deployment workflow.

PDA-0 is intentionally read-only. It verifies Windows, .NET, WSL, OpenSSH, the Millenova repository, the deploy script fingerprint, and deployment-report storage. It does not create keys, start an SSH agent, change SSH configuration, contact either server, or run a deployment.

The existing `deploy_millenova.sh` remains owned by the Millenova repository and is not copied or modified here.

## Development

```powershell
dotnet build MillenovaDeployConsole.sln
dotnet test MillenovaDeployConsole.sln --no-build
dotnet run --project src/MillenovaDeployConsole.App
```

The default discovery targets are documented in `docs/PDA0_DISCOVERY.md`.
