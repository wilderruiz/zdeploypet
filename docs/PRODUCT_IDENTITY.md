# Product identity

## Name

**ZDeployPet** — a friendly Windows + WSL deployment companion in the Zomniverse family.

## Product boundary

ZDeployPet owns deployment profiles, bounded SSH-agent sessions, deployment execution, report monitoring and sanitized incident handoff. ZomniverseGitPet owns Git workflows. They remain separate applications and security boundaries.

A future integration may let GitPet open ZDeployPet with a repository path, profile identifier, source commit or incident-bundle path. It must never transfer passphrases, private keys, agent sockets, target credentials or deployment authorization.

## Companion direction

The proposed companion is an original robotic space-otter courier. Its accessible states are:

- sleeping — access locked;
- alert — authentication required;
- ready — required targets authenticated;
- carrying a package — deployment active;
- celebrating — deployment passed;
- worried — warning or partial result;
- protective — unsafe action blocked;
- holding an incident package — diagnostic bundle ready.

Text, color-independent symbols and a static fallback must communicate every state. Rendering failure must never affect authentication, deployment execution or reporting.

## Distribution identities

- `DEV-ZDeployPet` — local development build under `%LOCALAPPDATA%\Zomniverse\ZDeployPet\DEV`;
- `ZDeployPet` portable — self-contained executable with manual updates;
- `ZDeployPet` installed — installer-managed release eligible for hash-verified updates.

Source, release artifacts, runtime files and per-user configuration remain separate.
