# PDA-1 — Deployment key onboarding and SSH diagnostics

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

The operator explicitly approves one public key. ZDeployPet records its path and fingerprint only. Before every authenticated probe, the public key is fingerprinted again. A changed or missing key fails closed and requires explicit re-approval.

PDA-1 does not ask for a key passphrase. The authenticated probe uses SSH batch mode, so an encrypted key that is not already available to SSH will be rejected without an app-owned passphrase prompt. PDA-2 will introduce the bounded unlock/session workflow.

## Host-key enrollment

For a configured target, ZDeployPet performs a read-only `ssh-keyscan` and displays the observed SHA-256 host fingerprint. `ssh-keyscan` is discovery, not trust: the UI tells the operator to compare the fingerprint with the hosting provider/server console or another trusted channel before enrollment.

Enrollment requires an explicit confirmation. Only the expected fingerprint and key type are persisted. The raw scanned host-key line remains transient.

Before a probe, ZDeployPet scans again and compares the observed fingerprint with the enrolled value. Any change hard-blocks the probe until the operator deliberately verifies and re-enrolls the new fingerprint.

## Non-writing authenticated probe

The target probe is deliberately narrow:

- `BatchMode=yes`;
- password and keyboard-interactive authentication disabled;
- `StrictHostKeyChecking=yes`;
- a temporary local WSL `known_hosts` file built only from the verified scan result;
- the selected matching key identity only;
- a fixed remote command: `printf ZDEPLOYPET_PROBE_OK`.

The temporary known-hosts file is deleted on exit. The remote command does not modify files, destinations, services or application state.

Probe diagnostics distinguish:

- missing deployment key;
- changed deployment public-key fingerprint;
- unreachable host / incorrect endpoint;
- changed host-key fingerprint;
- authentication/key/username rejection;
- unclassified SSH failure.

## PDA-1 smoke

Before marking PDA-1 accepted, verify from the published development executable that:

1. public keys are listed from the selected WSL distribution and a selected fingerprint remains stable after restart;
2. no app field or saved JSON contains a passphrase or private-key content;
3. a target host fingerprint can be scanned, independently verified and explicitly enrolled;
4. an enrolled target passes only the fixed authenticated non-writing probe;
5. a deliberately incorrect enrolled host fingerprint blocks before SSH authentication;
6. replacing/reselecting a public key with a different fingerprint blocks until explicitly approved;
7. unreachable host, incorrect username/key and missing key produce actionable diagnostics;
8. no deployment script is started and no target destination is modified during these checks.
