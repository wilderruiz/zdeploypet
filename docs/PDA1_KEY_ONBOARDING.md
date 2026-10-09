# PDA-1 — Deployment key onboarding and SSH diagnostics

**Status:** ✅ ACCEPTED 2026-10-09

PDA-1 establishes explicit SSH identity before ZDeployPet introduces its own bounded SSH-agent session in PDA-2.

## Local-only identity state

ZDeployPet stores non-secret deployment identity metadata below the existing per-user application root:

```text
%LOCALAPPDATA%\Zomniverse\ZDeployPet\identity\<profile-id>.json
```

The identity record may contain:

- the selected WSL public-key path;
- the selected public-key SHA-256 fingerprint and algorithm;
- the expected SSH host-key fingerprint and key type for each configured target.

It never contains a password, private-key contents, private-key passphrase, SSH-agent socket or deployment authorization.

## Deployment key onboarding

The **Deployment access…** window enumerates `~/.ssh/*.pub` inside the profile's selected WSL distribution and fingerprints candidates with `ssh-keygen -lf ... -E sha256`.

The operator can explicitly approve an existing public key or create a dedicated profile-scoped ZDeployPet Ed25519 key under the selected WSL user's `~/.ssh`. ZDeployPet records only path/fingerprint metadata. The private key is never copied into the project repository or to a remote server.

The approved public-key fingerprint is rechecked before installation/probe use and fingerprint changes fail closed until explicitly approved again.

## Host-key enrollment

For a configured target, ZDeployPet performs a read-only host-key scan and displays the observed SHA-256 fingerprint. Scanning is discovery, not trust: the operator must independently verify the fingerprint before enrollment.

Enrollment is explicit. ZDeployPet persists only the expected fingerprint and key type. Installer and probe rechecks request the exact enrolled host-key type, so servers exposing several key types do not produce false mismatches. Any real mismatch blocks before authentication.

## Public-key installation

ZDeployPet installs only the approved `.pub` half through the trusted WSL/SSH process. Before the one-time account-password prompt, the terminal clearly identifies the friendly server name, username, host, port and account so the operator does not confuse credentials between targets.

The remote account password is entered only into the SSH terminal. ZDeployPet does not receive, echo or persist it.

## Non-writing authenticated probe

The target probe is deliberately narrow:

- password and keyboard-interactive authentication are disabled for the probe;
- strict host-key checking is required;
- a temporary WSL `known_hosts` file is built only from the already verified host-key line;
- the selected approved deployment identity is used;
- the fixed remote command is non-writing and does not run the deployment script.

The temporary known-hosts file is deleted on exit. Probe diagnostics distinguish missing/changed keys, unreachable endpoints, host-key mismatch, authentication rejection and other SSH failures.

## Git/private-key safety

The **Git safety…** window scans tracked and relevant unignored files for private-key material, including known private-key headers. Tracked findings are reported separately because `.gitignore` cannot remediate an already tracked secret.

ZDeployPet can add an idempotent, narrowly scoped managed `.gitignore` safety block without rewriting unrelated project rules. It does not blanket-ignore `*.pub`.

## Accepted operator smoke

Accepted from the published development executable on 2026-10-09:

1. dedicated `zdeploypet:Millenova` key creation and persistence passed;
2. Hostinger host fingerprint was independently verified, explicitly enrolled and rechecked;
3. Hostinger public-key installation completed through `ssh-copy-id` using the trusted one-time password prompt;
4. Hostinger returned `Status: Success` on the fixed non-writing authenticated probe without another account-password prompt;
5. VPS also returned `Status: Success` using the approved deployment key;
6. guided action state enabled only the next valid onboarding action;
7. repository secret scan found no private-key material;
8. the managed `.gitignore` block was added and the follow-up scan returned `PASS` with the block present;
9. no deployment script was started and no deployment destination/application file was modified by these checks.

PDA-1 is closed. The active phase is **PDA-2 — bounded SSH-agent session**.