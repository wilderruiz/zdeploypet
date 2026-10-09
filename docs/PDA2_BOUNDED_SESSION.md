# PDA-2 — Bounded SSH-agent session

**Status:** ✅ ACCEPTED 2026-10-09

PDA-2 established one app-owned, time-bounded deployment-access session that reuses the already-approved PDA-1 deployment key without collecting or storing passwords or passphrases in ZDeployPet.

## Accepted behavior

- ZDeployPet starts a dedicated `ssh-agent` inside the selected WSL distribution instead of trusting an arbitrary ambient agent.
- The owned agent is addressed through a unique local socket and recorded PID and is live-validated before trust decisions.
- Unlock opens a trusted OpenSSH terminal and runs `ssh-add -t 28800` for only the approved profile-scoped deployment key.
- A private-key passphrase, if one exists, is entered only into OpenSSH. ZDeployPet never receives or stores it.
- READY is granted only when the exact approved SHA-256 key fingerprint is loaded and no foreign key is present.
- The initial lease is eight hours and exposes explicit LOCKED / READY / EXPIRING / EXPIRED / INVALID state.
- Explicit Lock stops the app-owned agent without deleting the persistent deployment key or altering remote `authorized_keys`.
- Closing the main ZDeployPet application performs bounded best-effort cleanup and removes deployment authorization.
- Session-backed Hostinger and VPS probes re-check enrolled host fingerprints and authenticate through the agent socket without passing a private-key `-i` path to `ssh`.
- Hostinger and VPS both passed within one READY session without another remote account-password prompt.
- Inline WSL shell probes normalize CRLF to LF before execution so `/bin/sh` does not misread `set -eu`.

## Operator smoke evidence

Published-development executable smoke completed on 2026-10-09:

1. initial state opened as LOCKED;
2. Unlock created the dedicated agent and OpenSSH loaded `zdeploypet_millenova_ed25519` with a 28,800-second lifetime;
3. Check session returned READY and displayed lease expiry;
4. explicit Lock returned the session to LOCKED;
5. lock-on-main-window-close was verified by reopening the app and observing LOCKED / no active lease;
6. bounded-agent target probes returned PASS for both configured Millenova targets: Hostinger and VPS;
7. no additional Hostinger/VPS account-password prompt occurred during bounded-agent probes.

## Automated evidence

`dotnet test ZDeployPet.sln -c Release` passed after the final persistence-race fix:

- `ZDeployPet.Core.Tests`: 20 passed, 0 failed;
- `ZDeployPet.Infrastructure.Tests`: 18 passed, 0 failed;
- total: 38 passed, 0 failed.

The concurrent draft-write regression discovered during the acceptance run was fixed by serializing atomic writes per destination path while retaining unique temporary files.

## Security boundary retained

PDA-2 does not execute a project deployment script. It establishes bounded deployment authorization only. Live and dry-run execution remain later phases and must continue to use the approved project-owned deployment script rather than replacing project-specific deployment logic inside ZDeployPet.
