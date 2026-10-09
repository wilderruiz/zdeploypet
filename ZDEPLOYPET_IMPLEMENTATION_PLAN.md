# ZDeployPet — Master Implementation Plan

> [!IMPORTANT]
> This file is the authoritative programme index and handoff record for ZDeployPet. Update the compact overview, current handoff, affected phase, validation matrix, and next action together whenever the programme changes. Do not reconstruct status from memory when this file and repository evidence are available.

**Status:** 🟡 **PDA-2 active — bounded SSH-agent session implementation next**  
**Reviewed repository baseline:** `main@f3bef91` (`2026-10-09`)  
**Completed phase:** `PDA-1 — Key onboarding and diagnostics`  
**Active phase:** `PDA-2 — Bounded SSH-agent session`  
**Next phase after acceptance:** `PDA-3 — Production shell, companion and operator visibility`  
**Remaining phases:** 7 including active PDA-2; 6 after PDA-2 acceptance  
**Primary outcome:** one bounded deployment-access login session, no repeated Hostinger/VPS account-password prompts, and one explicit authorization for every live deployment  
**Product boundary:** standalone Windows/.NET 8 WPF application using WSL through a narrow bridge  
**Deployment boundary:** project-owned deployment scripts remain authoritative and are not rewritten by ZDeployPet  
**Decision owner:** Wilder Ruiz

## Compact phase overview

**Legend:** ✅ complete · 🟡 active/acceptance pending · ⬜ queued · ⏸ gated · ❌ rejected

| Phase | Status | Outcome | Primary documentation |
| --- | --- | --- | --- |
| **PDA-0** | ✅ ACCEPTED 2026-10-08 | Standalone repository, read-only Windows/WSL/OpenSSH/project/script/report discovery | [`docs/PDA0_DISCOVERY.md`](docs/PDA0_DISCOVERY.md) |
| **PDA-0A** | ✅ ACCEPTED 2026-10-09 | Community-safe local profiles, first-run setup, multiple targets/destinations, input guidance/history/draft recovery | [`docs/PROFILES.md`](docs/PROFILES.md), [`docs/PRODUCT_IDENTITY.md`](docs/PRODUCT_IDENTITY.md) |
| **PDA-1** | ✅ ACCEPTED 2026-10-09 | Dedicated deployment key, public-key-only installation, explicit host trust, non-writing authenticated probes, Git/private-key safety | [`docs/PDA1_KEY_ONBOARDING.md`](docs/PDA1_KEY_ONBOARDING.md), this plan §7 |
| **PDA-2** | 🟡 ACTIVE | App-owned bounded SSH-agent session: unlock once, status, expiry and lock | This plan §8 |
| **PDA-3** | ⬜ QUEUED | Single-instance production shell, accessible state model, companion pet, dark/red visual identity, menu/about and console/log UI | [`docs/PRODUCT_IDENTITY.md`](docs/PRODUCT_IDENTITY.md), this plan §9 |
| **PDA-4** | ⬜ QUEUED | Read-only structured deployment-report monitor | Millenova reporter plan, this plan §10 |
| **PDA-5** | ⬜ QUEUED | Allowlisted dry-run executor for the unchanged project script | This plan §11 |
| **PDA-6** | ⬜ QUEUED | Live executor with immutable review and exact human confirmation | This plan §12 |
| **PDA-7** | ⬜ QUEUED | Sanitized incident bundle and local/remote-agent handoff | This plan §13 |
| **PDA-8** | ⬜ QUEUED | Security hardening, clean-clone CI, installer/portable release, desktop shortcut verification and public launch | This plan §14 |

### Immediate next action

Begin PDA-2 in this order:

1. introduce a dedicated ZDeployPet SSH-agent session boundary rather than reusing an arbitrary ambient agent;
2. start the agent inside the selected WSL environment and capture only the non-secret session metadata ZDeployPet needs to address it locally;
3. verify agent ownership/liveness before every state transition and reject stale, foreign or unverifiable state;
4. add an explicit **Unlock deployment access** action that invokes `ssh-add` through a trusted terminal/process rather than collecting a passphrase in a ZDeployPet field;
5. load only the already-approved PDA-1 deployment key and verify the loaded fingerprint exactly matches the approved profile fingerprint before declaring READY;
6. apply an initial bounded key lifetime of eight hours and expose **LOCKED / READY / EXPIRING** state plus remaining lease time;
7. add explicit **Lock** and lock-on-close behavior, clearing the app-owned agent/key session without deleting the persistent key from WSL `~/.ssh`;
8. prove repeated Hostinger and VPS non-writing probes work during one valid session without account-password prompts or repeated key onboarding;
9. add automated tests for agent-output parsing, ownership/state validation, fingerprint matching, expiry and lock decisions before operator smoke.

### PDA-1 acceptance evidence

Accepted from the published development executable on 2026-10-09. Verified end-to-end:

1. existing WSL public keys are discovered and fingerprinted;
2. a dedicated `zdeploypet:Millenova` Ed25519 key can be created under the selected WSL user's `~/.ssh` and survives app restart;
3. only the public half is installed remotely; the private key remains local;
4. Hostinger host identity was independently verified, explicitly enrolled and rechecked by exact host-key type;
5. the installer clearly identifies the friendly target (`Hostinger`), SSH user, host and port before the one-time account-password prompt;
6. the Hostinger public-key installation completed through `ssh-copy-id`, with the password typed only into the trusted terminal and not retained by ZDeployPet;
7. Hostinger non-writing authenticated probe returned `Status: Success` without another account-password prompt;
8. VPS enrollment/public-key authentication also returned `Status: Success` for the configured VPS target;
9. target-changing and host-key-type ordering defects were fixed so enrolled fingerprints are compared against the exact enrolled key type and mismatches fail closed;
10. guided button state enables only the next valid onboarding action instead of exposing every trust/install/probe action simultaneously;
11. Repository Git safety scan found no private-key material in tracked or relevant untracked files;
12. the ZDeployPet-managed `.gitignore` safety block was added idempotently and a rescan returned `PASS` with the block present;
13. no deployment script was run and no deployment destination/application file was modified by PDA-1 probe operations.

### Maintenance rule

When a phase changes:

- update the compact table and status header;
- update the phase's completed/remaining/acceptance lists;
- update the handoff snapshot and next concrete action;
- add or revise the linked focused document when detail no longer belongs here;
- record tests and operator smoke separately—automated tests do not substitute for the owning UI/remote smoke;
- never mark a production/authentication behavior complete from code inspection alone.

---

# 1. Documentation map and precedence

## 1.1 Repository documents

| Document | Role | Authority |
| --- | --- | --- |
| [`ZDEPLOYPET_IMPLEMENTATION_PLAN.md`](ZDEPLOYPET_IMPLEMENTATION_PLAN.md) | Master roadmap, compact index, phase contracts, handoff and restart record | Programme status and sequencing |
| [`README.md`](README.md) | Public product introduction and daily build/test/publish commands | User/developer entry point |
| [`docs/PDA0_DISCOVERY.md`](docs/PDA0_DISCOVERY.md) | Accepted machine-capability baseline | PDA-0 evidence |
| [`docs/PDA1_KEY_ONBOARDING.md`](docs/PDA1_KEY_ONBOARDING.md) | Accepted deployment-key/host-trust/probe workflow | PDA-1 evidence |
| [`docs/PRODUCT_IDENTITY.md`](docs/PRODUCT_IDENTITY.md) | Product boundary, companion direction and build identities | Naming/identity boundary |
| [`docs/PROFILES.md`](docs/PROFILES.md) | Shareable-contract/private-binding split and profile validation | Profile contract summary |
| [`examples/generic-wsl-script/deployment.json`](examples/generic-wsl-script/deployment.json) | Sanitized shareable project-contract example | Public example only |

`docs/Wilder_Notes.md` is a local operator convenience note, currently untracked and deliberately excluded from the public project scope. It is not normative and must not be required by a clean clone.

## 1.2 External Millenova documents

ZDeployPet is generic; Millenova is its first private local profile. The following Millenova documents define adapter-side behavior and must not be copied into public defaults:

- [`MILLENOVA_PARALLEL_DEPLOY_APP_IMPLEMENTATION_PLAN.md`](https://github.com/wilderruiz/millenova/blob/main/docs/MILLENOVA_PARALLEL_DEPLOY_APP_IMPLEMENTATION_PLAN.md) — original ZDeployPet decision record and detailed PDA-0 through PDA-8 scope; this master plan incorporates and supersedes its ZDeployPet status tracking.
- [`MILLENOVA_DEPLOYMENT_REPORTER_IMPLEMENTATION_PLAN.md`](https://github.com/wilderruiz/millenova/blob/main/docs/MILLENOVA_DEPLOYMENT_REPORTER_IMPLEMENTATION_PLAN.md) — authoritative Millenova reporter schema/semantics and the completed DR programme; ZDeployPet consumes reporter output but does not redefine it.
- [`MILLENOVA_FIRST_PARTY_MEDIA_SIGNALS_IMPLEMENTATION_PLAN.md`](https://github.com/wilderruiz/millenova/blob/main/docs/MILLENOVA_FIRST_PARTY_MEDIA_SIGNALS_IMPLEMENTATION_PLAN.md) — structural model used for this plan's compact overview, phase map, handoff, validation matrix, non-goals and restart instructions; it is not a ZDeployPet runtime dependency.

## 1.3 Source-of-truth order

When documents disagree:

1. runtime security invariants and validated code behavior win over descriptive prose;
2. this master plan owns ZDeployPet sequencing and current status;
3. focused repository documents own their narrow contracts;
4. Millenova's reporter plan owns Millenova report semantics;
5. the private local profile owns real endpoints and paths;
6. examples and screenshots are illustrative and never authority for credentials or production state.

---

# 2. Product boundary

ZDeployPet is a friendly local Windows + WSL deployment companion. It is not a website admin page, generic remote shell, credential vault, replacement deployment engine or Git client.

```text
PROJECT REPOSITORY
shareable .zdeploypet contract + project-owned deploy script
        ↓
PRIVATE LOCAL PROFILE
machine path + WSL + targets + destinations + report root
        ↓
LOCAL DEPLOYMENT IDENTITY
private key stays in WSL ~/.ssh; only public key is installed remotely
        ↓
BOUNDED ACCESS SESSION
dedicated SSH agent + approved key fingerprints + expiry
        ↓
SAFE EXECUTION
known target/mode/release prompts + explicit live confirmation
        ↓
AUTHORITATIVE REPORT
structured JSON + summary + full log
        ↓
SANITIZED HANDOFF
local Codex or exportable incident bundle without credentials
```

ZomniverseGitPet and ZDeployPet remain sibling applications:

- GitPet owns repository selection, checkpoints, commits, remotes and GitHub synchronization.
- ZDeployPet owns deployment profiles, authentication identities/sessions, safe deployment execution, report monitoring and incident handoff.
- A future integration may pass a repository path, profile ID, source commit or sanitized incident-bundle path.
- It must never pass private keys, passphrases, agent sockets, host credentials or deployment authorization.

## 2.1 Script-driven v1

ZDeployPet v1 does not infer how arbitrary projects deploy. A project-owned allowlisted script continues to own:

- build and test operations;
- source selection and exclusion;
- transfer behavior;
- remote activation/restart;
- application-specific health checks;
- report generation.

The script remains the deployment engine but becomes implementation plumbing for the desktop experience. The normal operator flow must not require opening a terminal and manually invoking `deploy_millenova.sh` or an equivalent project script. Later PDA-5/PDA-6 UI actions call the already-approved script with bounded known inputs.

ZDeployPet owns validation, deployment-key onboarding, session access, bounded prompt driving, human authorization, monitoring and safe evidence export.

## 2.2 Community configuration boundary

```text
SAFE TO COMMIT
<project>/.zdeploypet/deployment.json
  schema, runner kind/path, logical target IDs,
  reporter kind/schema, required capabilities

PRIVATE TO THIS COMPUTER
%LOCALAPPDATA%\Zomniverse\ZDeployPet\
  profiles, drafts, suggestions, UI settings,
  real hosts/users/ports/destinations/fingerprints

PRIVATE SSH MATERIAL
<selected WSL user's HOME>/.ssh/
  zdeploypet_<profile>_ed25519          private key
  zdeploypet_<profile>_ed25519.pub      public half

REMOTE TARGET
~/.ssh/authorized_keys
  public key only; never the private key
```

An SSH key is not a stored/translated password. SSH public-key authentication is a separate authentication mechanism: the desktop proves possession of the private key and the server verifies that proof against the enrolled public key. Existing website, database, application, API or admin passwords remain independent and are not replaced by the deployment key.

Passwords, passphrases, tokens and private-key contents are never profile, draft, suggestion, report or incident fields.

## 2.3 Git and secret boundary

Primary protection is physical/logical separation: ZDeployPet-generated private keys live outside the repository under WSL `~/.ssh`. `.gitignore` is defense in depth, not a vault.

ZDeployPet provides an idempotent Git-hygiene helper for projects it manages. The managed block is narrowly scoped to ZDeployPet local/private artifacts; it does not blanket-ignore `*.pub`.

Rules:

- do not blanket-ignore public keys (`*.pub`); public keys are not secrets and projects may legitimately version public material;
- do not assume ignore rules make a repository safe if a secret was already tracked;
- scan bounded project files for private-key markers such as `-----BEGIN OPENSSH PRIVATE KEY-----` and `-----BEGIN PRIVATE KEY-----` and hard-block/report suspicious matches in push/release-oriented workflows;
- never automatically move, copy or stage a private key into the repository;
- any remediation that changes tracked files remains explicit to the operator.

---

# 3. Operator experience

```text
Open ZDeployPet
        ↓
No validated profile?
        ↓
Choose project + WSL + deployment script
        ↓
Describe servers and one-or-more destinations per server
        ↓
Check settings and save locally
        ↓
Read-only discovery passes
        ↓
Deployment access setup
        ↓
Use existing SSH key OR create dedicated ZDeployPet key
        ↓
Approve public-key fingerprint
        ↓
Verify/enroll server host fingerprint
        ↓
Install public key on each approved server
(first-time SSH/OS prompt may request existing account password; ZDeployPet does not save it)
        ↓
Non-writing authenticated probe passes
        ↓
Git safety passes
        ↓
Deployment access LOCKED
        ↓
Unlock approved key once per bounded session
        ↓
Approved targets READY
        ↓
Choose target + release + dry/live mode in ZDeployPet
        ↓
Dry run starts after review
Live run requires exact project confirmation phrase
        ↓
ZDeployPet invokes the approved project script internally
        ↓
Read authoritative structured report
        ↓
Lock manually or automatically on app close/expiry
```

Default access policy:

- closing the app locks deployment access;
- reboot/logoff requires a new unlock;
- abandoned/crashed access expires after a bounded lease, initially eight hours;
- repeat deployments in the same valid session do not request remote account passwords or repeated key passphrases;
- every live deployment still requires its exact human authorization phrase;
- no normal deployment flow requires users to manually execute the project deployment script in a terminal.

---

# 4. Repository and architecture

```text
ZDeployPet/
├── ZDeployPet.sln
├── ZDeployPet.code-workspace
├── ZDEPLOYPET_IMPLEMENTATION_PLAN.md
├── src/
│   ├── ZDeployPet.App/             WPF presentation and interaction
│   ├── ZDeployPet.Core/            immutable contracts and validation
│   ├── ZDeployPet.Infrastructure/  local persistence and Windows discovery
│   └── ZDeployPet.WslBridge/       narrow WSL command/discovery boundary
├── tests/
│   ├── ZDeployPet.Core.Tests/
│   └── ZDeployPet.Infrastructure.Tests/
├── examples/
│   └── generic-wsl-script/deployment.json
├── scripts/
│   └── publish-local.ps1
└── docs/
    ├── PDA0_DISCOVERY.md
    ├── PDA1_KEY_ONBOARDING.md
    ├── PRODUCT_IDENTITY.md
    └── PROFILES.md
```

Architectural rules:

- keep Core independent from WPF, filesystem process launching and WSL;
- keep WSL command construction and parsing testable outside the UI;
- keep persistence atomic and per-user;
- never block the WPF dispatcher on an async continuation;
- use unique temporary files for potentially overlapping writes;
- keep UI copy separate from security/execution state;
- use the shared structured `i` help system for new operator-facing actions/screens rather than duplicating ad-hoc tooltip implementations;
- do not concentrate session, execution, reporting and presentation in `MainWindow` as later phases grow;
- add view models/services before PDA-3 expands the shell.

## 4.1 Build identities and output

| Identity | Purpose | Expected location |
| --- | --- | --- |
| `DEV-ZDeployPet` | Later distinct developer identity/state | `%LOCALAPPDATA%\Zomniverse\ZDeployPet\DEV` |
| `ZDeployPet` local current | Current visual smoke executable | `C:\Dev\ZDeployPet_Releases\current\ZDeployPet.exe` |
| `ZDeployPet` portable | Versioned public portable release | External versioned release folder/GitHub Release |
| `ZDeployPet` installed | Installer-managed public release | Installer-selected Program Files location |

The current `scripts/publish-local.ps1` build/test/publish loop is complete. A full versioned release builder and installer remain PDA-8 work.

---

# 5. Security invariants

The implementation must not:

- store Hostinger/VPS/other remote account passwords as application/profile plaintext or normal local JSON;
- display or collect a private-key passphrase in an app-owned field;
- copy private keys into either source repository or release package;
- copy private keys to remote targets; remote SSH onboarding receives only the public key;
- export or remotely expose an SSH-agent socket;
- use `StrictHostKeyChecking=no`;
- accept a changed host key silently;
- expose a production HTTP deployment endpoint;
- provide a generic arbitrary-command textbox;
- silently choose or broaden a remote destination;
- rewrite a project deployment script to fit the app;
- auto-type a live confirmation phrase;
- auto-retry a partial live deployment;
- treat process exit code or UI state as stronger than structured reporter truth;
- make GitHub the only incident-recovery route;
- auto-apply or auto-deploy an agent-generated patch;
- commit real hosts, usernames, personal paths, reports, incident bundles or credentials to the public repository.

If a future target cannot support public-key authentication and password retention becomes unavoidable, it requires a separately reviewed design using an OS-protected secret store such as Windows Credential Manager/DPAPI and a non-secret profile reference. It is not part of the current architecture and must never regress to plaintext profile storage.

| Threat | Required control |
| --- | --- |
| Passphrase captured by app | Input belongs to `ssh-add` or an approved OS credential provider |
| First-time server password captured/persisted | Use trusted SSH/OS prompt; ZDeployPet does not retain the password |
| Private key committed | Keep key under WSL `~/.ssh` outside repo; add scoped ignore defense, private-key marker scan and release audit |
| Private key copied to server | Public-key installer accepts/transmits only `.pub` material |
| Stale or foreign agent | Validate socket/PID ownership, permissions, lease and key fingerprints |
| Wrong server | Explicit host-key enrollment and mismatch hard block |
| Access remains unlocked | Lock-on-close plus bounded `ssh-add -t` lifetime |
| Arbitrary execution | Fixed executable/script and allowlisted inputs/actions only |
| Accidental live deploy | Immutable review plus exact UI and script confirmation gates |
| Concurrent deployment | Single-instance execution lock |
| Report path escape | Canonical artifact paths must remain below configured report root |
| Secret in evidence | Denylist, redaction, size bounds and preview before export |
| Remote agent gains authority | Export evidence only; never keys, sockets or deployment capability |
| Malicious returned patch | Check, human diff review, tests and a new deployment authorization |

---

# 6. PDA-0 / PDA-0A — Foundation and profile onboarding

## 6.1 PDA-0 — Standalone boundary and discovery

**Status:** ✅ ACCEPTED 2026-10-08

Completed:

- created the standalone .NET 8/WPF repository outside Millenova;
- separated App, Core, Infrastructure and WslBridge projects;
- discovered Windows/.NET, WSL, OpenSSH, ssh-agent and ssh-add capabilities read-only;
- checked the selected Windows and WSL repository paths, deployment script and report root;
- recorded the script SHA-256 without changing the script;
- fixed and regression-tested WSL `--exec` argument forwarding;
- completed the owning smoke with `Discovery passed`;
- rebranded the initial prototype as ZDeployPet.

Evidence:

- [`docs/PDA0_DISCOVERY.md`](docs/PDA0_DISCOVERY.md)
- commits `6a229a6`, `68b8bfc`, `04fd534`, `caff377`

PDA-0 explicitly did not create keys, start a persistent agent, enroll host keys, contact production targets or run a deployment.

## 6.2 PDA-0A phase map

**Status:** ✅ ACCEPTED 2026-10-09

| Subphase | Status | Implemented result |
| --- | --- | --- |
| **0A.1 Profile contract/storage** | ✅ COMPLETE | Schema v1, atomic local profile save/load, GUID identity, Git-ignored per-user storage |
| **0A.2 Project/WSL/script validation** | ✅ COMPLETE | Folder picker, installed WSL selection, derived/verified WSL path, contained script validation |
| **0A.3 Targets and destinations** | ✅ COMPLETE | Multiple SSH targets and multiple labelled destinations per target; safe `/path` and scoped `~/path` rules |
| **0A.4 First-run usability** | ✅ ACCEPTED | Plain-language five-section UI, shared structured `i` tooltips, dropdown suggestions, forget-history action, incomplete draft recovery |
| **0A.5 Local publish loop** | ✅ COMPLETE | VS Code workspace and build/test/self-contained `publish-local.ps1` workflow |
| **0A.6 Public repository readiness** | ✅ COMPLETE FOR CURRENT SCOPE | GitHub repository linked and healthy; source/public scope established; broader launch policy/license/release work remains PDA-8 |

PDA-0A acceptance is based on the corrected operator smoke listed near the top of this plan.

---

# 7. PDA-1 — Key onboarding and diagnostics

**Status:** ✅ ACCEPTED 2026-10-09

PDA-1 established the normal-user deployment identity workflow and non-writing target verification required before bounded sessions.

## 7.1 Accepted implementation/evidence

Completed and smoke-verified:

- Deployment access window opens from the validated profile;
- existing WSL public-key discovery and SHA-256 fingerprinting work;
- dedicated profile-scoped ZDeployPet Ed25519 key creation works under WSL `~/.ssh` without repository private-key placement;
- key selection/fingerprint metadata survives application restart;
- guided onboarding enables only the next valid trust/install/probe action;
- host-key scan requires explicit independent verification and enrollment;
- host-key rechecks compare the exact enrolled host-key type and fail closed on mismatch;
- public-key installer rechecks both the local approved key and enrolled server host identity before opening `ssh-copy-id`;
- terminal guidance displays friendly server name, username, host, port and account before asking for an account password;
- first-time password entry occurs only in the trusted SSH terminal and is not received or stored by ZDeployPet;
- Hostinger public-key installation succeeded and its fixed non-writing authenticated probe returned `Status: Success`;
- VPS public-key onboarding/probe also returned `Status: Success`;
- probes do not run the deployment script and do not modify deployment destinations;
- Git-safety UI scans tracked and relevant untracked repository files for private-key material;
- the managed `.gitignore` defense block is idempotent and preserves existing repository rules;
- the Millenova Git-safety operator smoke returned `PASS`, with no private-key material detected and the managed block present.

## 7.2 Persistence boundary retained

ZDeployPet may persist selected public-key path/fingerprint/algorithm/comment and expected target host-key fingerprint/type plus other non-secret onboarding metadata.

ZDeployPet does not persist private-key bytes, private-key passphrases, remote SSH account passwords, agent sockets, or website/database/API/admin credentials.

## 7.3 Acceptance gate result

PDA-1 acceptance requirements are satisfied by the automated/regression coverage implemented during the phase plus the owning operator smoke against the Millenova private profile. Hostinger and VPS both passed authenticated non-writing probes using the dedicated ZDeployPet key, and Git safety passed after the managed defense block was installed.

---

# 8. PDA-2 — Bounded SSH-agent session

**Status:** 🟡 ACTIVE

PDA-2 introduces app-owned bounded access state without making the app a credential collector. PDA-1 already proves the approved deployment key can authenticate to both intended private targets; PDA-2 now turns that persistent key into a temporary, explicitly controlled session.

## 8.1 Required outcomes

- create/own a dedicated SSH-agent process/session in the selected WSL environment rather than trusting an arbitrary ambient agent;
- record only the minimum non-secret local session metadata required to address and validate that agent;
- verify agent ownership/liveness and reject stale, foreign or unverifiable state;
- unlock the approved key through trusted terminal/`ssh-add`, not an app password/passphrase field;
- load only the PDA-1-approved deployment key and verify the actual loaded fingerprint against the approved profile fingerprint;
- support bounded lifetime, initially eight hours, using agent/key lifetime controls rather than an unbounded remembered unlock;
- show **LOCKED / READY / EXPIRING** state and remaining lease time;
- support explicit **Lock** and lock-on-close;
- locking removes the key/session authorization but does not delete the persistent deployment key from WSL `~/.ssh` or alter remote `authorized_keys`;
- closing/restarting the app must never silently restore READY from stale metadata alone;
- repeat authenticated probes during one valid lease must not request Hostinger/VPS account passwords or repeated key onboarding;
- preserve PDA-1 host-key verification and fail-closed fingerprint checks for all session-backed probes;
- persist only non-secret session metadata required for safe recovery/expiry decisions;
- never expose/export the agent socket to remote targets, Git, incident bundles or external agents.

## 8.2 Initial implementation sequence

1. add a WSL agent-session service with testable parsing of `ssh-agent -s` output;
2. define session metadata/state contracts separately from WPF UI;
3. start a dedicated agent and validate PID/socket/liveness before accepting it;
4. add trusted-terminal `ssh-add -t 8h <approved-private-key>` onboarding without any app-owned passphrase field;
5. enumerate loaded agent fingerprints and require the approved fingerprint before READY;
6. add LOCKED/READY/EXPIRING state computation and countdown refresh;
7. add explicit Lock that removes loaded identities and terminates the owned agent where appropriate;
8. wire lock-on-close and stale-session cleanup;
9. route non-writing target probes through the bounded agent session and smoke repeated Hostinger/VPS access;
10. add negative tests for foreign agent, wrong fingerprint, expired lease, dead PID/socket and stale metadata.

## 8.3 Acceptance gate

PDA-2 is accepted only when:

- one trusted unlock establishes READY with the expected approved key fingerprint;
- one unlock supports repeated authenticated Hostinger/VPS probes within the valid lease without remote account-password prompts;
- the UI exposes clear LOCKED / READY / EXPIRING state and expiry information;
- explicit Lock immediately removes deployment authorization;
- closing ZDeployPet locks access;
- expiry returns the session to a non-authorized state without operator ambiguity;
- app restart does not trust stale/foreign agent metadata without live validation;
- wrong/foreign key fingerprint fails closed;
- no passphrase is collected/stored by a ZDeployPet field;
- no agent socket or private material is exported or written into the project repository;
- automated tests cover parsing, validation, expiry, fingerprint matching and lock decisions;
- the owning published-executable operator smoke passes against the Millenova profile.

---

# 9. PDA-3 — Production shell, companion and operator visibility

**Status:** ⬜ QUEUED

PDA-3 turns the onboarding-oriented window into the durable production shell while keeping deployment authority explicit and bounded.

Required outcomes:

- single-instance desktop shell with accessible keyboard/focus behavior;
- durable state presentation for profile, session, target and deployment readiness;
- top application menu with at minimum Project/Profile, View, Help and About surfaces;
- About dialog modeled on the useful transparency of ZomniverseGitPet: product name, purpose, builder, version, build date, platform, license and repository, plus relevant sibling-project references without coupling runtimes;
- dark theme as the primary application theme, using a restrained red accent for ZDeployPet identity while preserving readable warning/error semantics;
- application icon set for executable/window/taskbar/installer/desktop shortcut with consistent ZDeployPet branding;
- an actual ZDeployPet companion/pet presence, implemented as a non-authoritative status/feedback layer that can reflect READY/LOCKED/RUNNING/ERROR states but can never bypass confirmations or initiate deployments;
- a built-in Console / Activity view that exposes sanitized application logs, probe/session/deployment lifecycle messages and diagnostics, with copy support and clear separation from secrets;
- log view must redact private-key material, passphrases, tokens and any configured secret fields before rendering or export;
- retain and expand the shared structured `i` guidance system across all new screens/actions so the desktop app remains understandable without external instructions;
- view-model/service separation so session, execution, reporting and pet/UI behavior do not accumulate in `MainWindow`;
- preserve responsive async behavior and avoid dispatcher deadlocks.

Acceptance gate:

- shell remains single-instance and responsive across setup/session/deployment states;
- dark/red visual identity is consistent and accessible;
- icons appear correctly in window chrome, executable, taskbar and packaged shortcut contexts;
- About contents report the actual build metadata and repository accurately;
- pet state mirrors application state but cannot trigger privileged actions;
- Console / Activity view shows useful sanitized diagnostics with no credential leakage;
- shared `i` guidance is present for new primary actions;
- keyboard navigation and screen-reader labels cover all primary controls.

---

# 10. PDA-4 — Structured deployment-report monitor

**Status:** ⬜ QUEUED

Required outcomes:

- consume the project/reporter structured JSON as authoritative deployment truth;
- watch configured report root read-only;
- display summary, timestamps, target/mode/release, outcome and links/paths to bounded artifacts;
- canonicalize all report/artifact paths and reject escapes outside configured report root;
- handle partially-written reports safely;
- never infer success from process exit code when reporter truth disagrees.

Acceptance gate:

- valid reports render correctly;
- path escapes and malformed reports fail closed;
- monitor performs no deployment-side write.

---

# 11. PDA-5 — Allowlisted dry-run executor

**Status:** ⬜ QUEUED

Required outcomes:

- execute only the configured/approved deployment script;
- normal operators start the dry run from ZDeployPet; manual terminal invocation of the project script is not part of the normal product workflow;
- expose only known target/mode/release inputs and required fixed actions;
- validate script fingerprint before execution;
- use the bounded session from PDA-2;
- prevent concurrent deployment execution;
- capture sanitized lifecycle diagnostics for PDA-3 Console / Activity and PDA-4 reports;
- dry run performs no live activation beyond what the authoritative project script defines as dry-run behavior.

Acceptance gate:

- only allowlisted script/action/input combinations can run;
- changed script fingerprint blocks execution;
- dry-run evidence is visible and attributable.

---

# 12. PDA-6 — Live executor and exact confirmation

**Status:** ⬜ QUEUED

Required outcomes:

- immutable pre-deployment review of project/profile/target/destination/release/script fingerprint;
- exact human confirmation phrase displayed and entered by operator;
- app never auto-types or supplies the live confirmation phrase;
- pass confirmation only to the fixed project script in its expected bounded form;
- the normal live deployment starts from ZDeployPet rather than requiring manual execution of the project script;
- do not auto-retry partial live deployments;
- single-instance execution lock spans the full live run;
- authoritative structured reporter result closes the run.

Acceptance gate:

- wrong or incomplete confirmation cannot start live deployment;
- review data cannot silently change between approval and execution;
- cancelled/failed/partial results remain explicit.

---

# 13. PDA-7 — Sanitized incident bundle and agent handoff

**Status:** ⬜ QUEUED

Required outcomes:

- create bounded incident bundles from selected logs/reports/state;
- redact/deny credentials, secrets, keys, agent sockets and unsafe paths;
- preview bundle before export;
- support local coding-agent handoff or explicit export without granting deployment authority;
- any returned patch remains untrusted until human diff review, tests and a fresh deployment authorization.

Acceptance gate:

- exported bundle passes redaction/secret checks;
- no remote handoff receives deployment credentials or capability;
- evidence is sufficient for debugging common deployment failures.

---

# 14. PDA-8 — Hardening, packaging and public launch

**Status:** ⬜ QUEUED

Required outcomes:

- clean-clone build/test CI;
- release secret scan and artifact audit, including private-key marker detection;
- versioned portable package;
- Windows installer with normal install/uninstall lifecycle;
- installer-created Start Menu entry and optional desktop shortcut;
- verify packaged/installed app behavior against the known-good local publish, including profile storage location, WSL discovery, window/taskbar icons, About/build metadata, dark/red theme, Console / Activity logging and all security boundaries;
- verify shortcut launch uses the intended installed binary and working-directory assumptions;
- validate upgrade/reinstall behavior without losing local profiles/drafts/suggestions unless explicitly requested;
- license/public policy/repository hygiene finalized;
- release notes and public launch documentation.

Packaging parity requirement:

The installed and portable builds must behave the same as the validated development/local-current build for all security and core workflow behavior. Packaging may change installation paths and update mechanics only; it must not alter profile semantics, WSL/SSH behavior, authorization gates, logging/redaction, session lifetime or deployment execution rules.

ZomniverseGitPet may be used as a UX/release-quality reference for installer behavior, desktop/start-menu integration, About transparency and packaged-app polish, but ZDeployPet remains an independent product with its own red-accent identity and deployment-specific security contract.

Acceptance gate:

- clean-clone CI passes;
- installer and portable package pass the same core smoke matrix as local current;
- desktop shortcut and Start Menu launch correctly;
- icons/theme/About/log console render correctly in packaged builds;
- upgrade preserves intended local user state;
- release package contains no secrets/private infrastructure/private keys;
- public documentation matches shipped behavior.

---

# 15. Validation matrix

| Area | Automated | Operator smoke | Production/private target smoke |
| --- | --- | --- | --- |
| PDA-0 discovery | ✅ | ✅ | N/A |
| PDA-0A profile onboarding | ✅ | ✅ 2026-10-09 | N/A |
| PDA-1 key/host/Git safety | ✅ phase tests/regressions | ✅ 2026-10-09 | ✅ Hostinger + VPS non-writing authenticated probes |
| PDA-2 bounded agent | Required: parsing, ownership/liveness, fingerprint, expiry, lock | Required | Required repeated Hostinger + VPS probes during one lease |
| PDA-3 shell/pet/theme/menu/log UI | Required where practical | Required | N/A |
| PDA-4 report monitor | Required | Required | Required against real reporter output |
| PDA-5 dry-run | Required | Required | Required |
| PDA-6 live execution | Required | Required | Required explicit live smoke |
| PDA-7 incident bundle | Required | Required | Required sanitized handoff smoke |
| PDA-8 packaging | CI/release automation | Required installed + portable smoke | Security parity with validated workflows |

---

# 16. Current handoff snapshot

**Repository:** `wilderruiz/zdeploypet`  
**Branch:** `main`  
**Reviewed baseline before this plan update:** `f3bef910d58a4d7c149b1a707135c061862a061c`  
**Repository state at handoff:** local/GitHub synchronization is managed through ZomniverseGitPet; the known-good local publish workflow remains `scripts/publish-local.ps1`  
**Completed:** PDA-0, PDA-0A and PDA-1  
**Active:** PDA-2

Recent acceptance/evidence:

- PDA-1 dedicated ZDeployPet key creation/persistence smoke passed;
- Hostinger host fingerprint was independently verified and explicitly enrolled;
- Hostinger public-key-only installation succeeded through the trusted SSH terminal with clear friendly-target/password guidance;
- Hostinger non-writing authenticated probe returned `Status: Success` without another account-password prompt;
- VPS target also returned `Status: Success` using the approved deployment key;
- exact enrolled host-key type is now honored during installer/probe rechecks, avoiding false mismatches on multi-key-type servers;
- guided action state enables only the next procedural trust/onboarding step;
- Repository Git safety scan passed with no private-key material detected;
- managed `.gitignore` defense block is present and its update action becomes inactive once already correct;
- PDA-1 caused no deployment-script execution and no application/deployment destination modification.

Next concrete implementation target:

1. create the PDA-2 session-state/core contracts and WSL agent-session service;
2. parse/start/validate a dedicated app-owned `ssh-agent` inside the selected WSL distribution;
3. add trusted-terminal `ssh-add -t 8h` unlock for only the PDA-1-approved key;
4. verify the loaded fingerprint before showing READY;
5. implement LOCKED / READY / EXPIRING plus remaining lease display;
6. implement explicit Lock, lock-on-close and stale/foreign agent rejection;
7. route repeated Hostinger/VPS probes through the bounded session and complete the owning smoke.

Future product-shell requirements already committed to the roadmap:

- ZDeployPet icon set and packaged shortcut integration;
- actual companion/pet UI;
- top menu and About dialog with version/build/platform/license/repository information;
- primary dark theme with red accent;
- shared structured `i` help across operator screens;
- sanitized Console / Activity log view;
- installer/portable parity verification against the validated local build.

---

# 17. Restart instructions

When resuming work:

1. read this file first;
2. confirm repository `main` and current checkpoint;
3. inspect the active phase only after reading its acceptance gate;
4. preserve all security invariants before expanding UI behavior;
5. keep real infrastructure data and all private key material outside the public repository;
6. treat `.gitignore` as defense in depth, not secret storage;
7. for PDA-2, never treat persisted agent metadata alone as authorization—live ownership/liveness/fingerprint validation is mandatory;
8. update this plan in the same change that materially changes phase status or acceptance evidence.