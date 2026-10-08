# ZDeployPet — Master Implementation Plan

> [!IMPORTANT]
> This file is the authoritative programme index and handoff record for ZDeployPet. Update the compact overview, current handoff, affected phase, validation matrix, and next action together whenever the programme changes. Do not reconstruct status from memory when this file and repository evidence are available.

**Status:** 🟡 **PDA-0A active — implementation complete; corrected operator smoke pending**  
**Reviewed repository baseline:** `main@219336a` (`checkpoint: 2026-10-08 11:57`)  
**Completed phase:** `PDA-0 — Standalone boundary and read-only discovery`  
**Active phase:** `PDA-0A — Community profile onboarding`  
**Next phase after acceptance:** `PDA-1 — Key onboarding and diagnostics`  
**Remaining phases:** 9 including active PDA-0A; 8 after PDA-0A acceptance  
**Primary outcome:** one bounded deployment-access login session, no repeated Hostinger/VPS account-password prompts, and one explicit authorization for every live deployment  
**Product boundary:** standalone Windows/.NET 8 WPF application using WSL through a narrow bridge  
**Deployment boundary:** project-owned deployment scripts remain authoritative and are not rewritten by ZDeployPet  
**Decision owner:** Wilder Ruiz

## Compact phase overview

**Legend:** ✅ complete · 🟡 active/acceptance pending · ⬜ queued · ⏸ gated · ❌ rejected

| Phase | Status | Outcome | Primary documentation |
| --- | --- | --- | --- |
| **PDA-0** | ✅ ACCEPTED 2026-10-08 | Standalone repository, read-only Windows/WSL/OpenSSH/project/script/report discovery | [`docs/PDA0_DISCOVERY.md`](docs/PDA0_DISCOVERY.md) |
| **PDA-0A** | 🟡 ACTIVE | Community-safe local profiles, first-run setup, multiple targets/destinations, input guidance/history/draft recovery | [`docs/PROFILES.md`](docs/PROFILES.md), [`docs/PRODUCT_IDENTITY.md`](docs/PRODUCT_IDENTITY.md) |
| **PDA-1** | ⬜ QUEUED | Dedicated key onboarding, public-key/host-key fingerprints, non-writing target probes | This plan §7 |
| **PDA-2** | ⬜ QUEUED | App-owned bounded SSH-agent session: unlock once, status, expiry and lock | This plan §8 |
| **PDA-3** | ⬜ QUEUED | Single-instance production shell, accessible state model and companion UI | [`docs/PRODUCT_IDENTITY.md`](docs/PRODUCT_IDENTITY.md), this plan §9 |
| **PDA-4** | ⬜ QUEUED | Read-only structured deployment-report monitor | Millenova reporter plan, this plan §10 |
| **PDA-5** | ⬜ QUEUED | Allowlisted dry-run executor for the unchanged project script | This plan §11 |
| **PDA-6** | ⬜ QUEUED | Live executor with immutable review and exact human confirmation | This plan §12 |
| **PDA-7** | ⬜ QUEUED | Sanitized incident bundle and local/remote-agent handoff | This plan §13 |
| **PDA-8** | ⬜ QUEUED | Security hardening, clean-clone CI, installer/portable release and public launch | This plan §14 |

### Immediate next action

Finish the corrected PDA-0A operator smoke from the published development executable:

```text
C:\Dev\ZDeployPet_Releases\current\ZDeployPet.exe
```

The acceptance smoke must prove that an incomplete draft:

1. saves without contacting WSL or a server when the project folder is missing;
2. reports `Draft saved — project folder required`;
3. closes immediately without a UI-thread deadlock;
4. restores the entered profile name after restart;
5. keeps tooltip activation limited to each card's `i` button;
6. clears suggestion history without deleting the current draft or validated profile.

Do not begin PDA-1 until this smoke passes and PDA-0A is marked accepted.

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
- ZDeployPet owns deployment profiles, authentication sessions, safe deployment execution, report monitoring and incident handoff.
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

ZDeployPet owns validation, session access, bounded prompt driving, human authorization, monitoring and safe evidence export.

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
```

Passwords, passphrases, tokens and private-key contents are never profile, draft, suggestion, report or incident fields.

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
Deployment access LOCKED
        ↓
Unlock in trusted terminal once per bounded session
        ↓
Approved targets READY
        ↓
Choose target + release + dry/live mode
        ↓
Dry run starts after review
Live run requires exact project confirmation phrase
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
- every live deployment still requires its exact human authorization phrase.

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

- store Hostinger/VPS/other remote account passwords;
- display or collect a private-key passphrase in an app-owned field;
- copy private keys into either source repository or release package;
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

| Threat | Required control |
| --- | --- |
| Passphrase captured by app | Input belongs to `ssh-add` or an approved OS credential provider |
| Private key committed | Key material outside repositories; ignore/secret scan/release audit |
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

| Subphase | Status | Implemented result |
| --- | --- | --- |
| **0A.1 Profile contract/storage** | ✅ CODE COMPLETE | Schema v1, atomic local profile save/load, GUID identity, Git-ignored per-user storage |
| **0A.2 Project/WSL/script validation** | ✅ CODE COMPLETE | Folder picker, installed WSL selection, derived/verified WSL path, contained script validation |
| **0A.3 Targets and destinations** | ✅ CODE COMPLETE | Multiple SSH targets and multiple labelled destinations per target; safe `/path` and scoped `~/path` rules |
| **0A.4 First-run usability** | 🟡 SMOKE PENDING | Plain-language five-section UI, `i`-only tooltips, dropdown suggestions, forget-history action, incomplete draft recovery |
| **0A.5 Local publish loop** | ✅ COMPLETE | VS Code workspace and build/test/self-contained `publish-local.ps1` workflow |
| **0A.6 Public repository readiness** | 🟡 ACTIVE | GitPet scope/hygiene configured; public GitHub origin still unverified/pending; public policy/license docs remain PDA-8 |

### Implemented profile behavior

- no real infrastructure is compiled into public source;
- local bindings live below `%LOCALAPPDATA%\Zomniverse\ZDeployPet`;
- shareable project contracts contain logical capabilities, not real hosts or destinations;
- a target may own several destinations, allowing a site folder and separate configuration file without collapsing them into one root;
- profile validation blocks missing folders/scripts, script escape, invalid ports, unknown destination targets and broad/unsafe destinations;
- `~/child` represents a scoped path below the authenticated remote user's home;
- incomplete non-secret setup values are saved as a draft before full validation;
- remembered suggestions are capped and independently forgettable;
- forgetting suggestions does not delete the current draft or validated profile;
- profile/draft/history writes use unique temporary files to tolerate overlap;
- the WPF close path uses synchronous local persistence and must never synchronously wait on an async UI continuation.

### PDA-0A defects already corrected

- editable deployment rows not committing correctly — fixed in `534f510`;
- one destination per server could not represent split site/config deployment — fixed in `06cd9bd`;
- technical setup labels and missing guidance — fixed in `521cfa9` and `9d3384d`;
- no recent-value suggestions — implemented in `1a8f6ce`;
- invalid/incomplete profiles lost entered values — fixed in `f42c5ad`;
- overlapping draft writes reused one `.tmp` file — fixed in `7d6c803`;
- close handler deadlocked the WPF UI by synchronously waiting on async persistence — fixed in `5de7f5f`;
- generated local publish artifacts entering project status — excluded by `.gitignore` and GitPet hygiene.

### PDA-0A remaining work

- run and accept the corrected draft-save/close/reopen smoke from the published EXE;
- complete a valid Millenova private profile and restart discovery smoke;
- prove all target/destination rows survive restart exactly;
- prove the public Git repository has its own correct GitHub origin and contains no local binding;
- decide whether draft/history storage needs a visible path/open-folder affordance;
- update [`docs/PROFILES.md`](docs/PROFILES.md) if the final accepted UI changes schema or storage behavior.

### PDA-0A acceptance

- [ ] fresh install starts without a private profile;
- [ ] incomplete draft saves, closes and restores without hanging;
- [ ] `i` tooltip works only from the icon and is keyboard accessible;
- [ ] saved non-secret values appear in dropdown suggestions;
- [ ] Forget suggestions removes history but not draft/profile;
- [ ] valid project and derived WSL paths identify the same repository;
- [ ] selected deployment script is a regular contained project file;
- [ ] unsafe destination fixtures are rejected;
- [ ] multiple destinations on one target survive save/reload;
- [ ] full private Millenova profile saves and reopens;
- [ ] read-only discovery reports `Discovery passed` after restart;
- [ ] no server was contacted, key created, agent started or deployment run;
- [ ] public repository contains no real local profile or personal infrastructure values.

### PDA-0A operator smoke

```powershell
cd C:\Dev\ZDeployPet
powershell -ExecutionPolicy Bypass -File scripts\publish-local.ps1
C:\Dev\ZDeployPet_Releases\current\ZDeployPet.exe
```

First run the incomplete-draft close/reopen test described in the compact overview. Then complete the valid private profile, save it, restart the EXE and run discovery. Record the result here before moving to PDA-1.

---

# 7. PDA-1 — Key onboarding and remote diagnostics

**Status:** ⬜ QUEUED / GATED BY PDA-0A ACCEPTANCE

Purpose: replace repeated remote account-password entry with dedicated, revocable, passphrase-protected deployment keys while keeping all secret input outside the app.

## 7.1 Required implementation

- guide generation or selection of one Ed25519 deployment key per target;
- store only key paths and public-key fingerprints in the private binding;
- show/copy public keys for registration;
- guide Hostinger control-panel/public-key registration when required;
- perform explicit host-key enrollment with expected/observed fingerprints;
- implement `BatchMode=yes` non-writing SSH readiness probes;
- distinguish missing key, passphrase-locked key, network failure, host-key mismatch and remote rejection;
- evaluate targets independently;
- never offer a password/passphrase textbox.

Suggested private key shape for the first Millenova binding:

```text
~/.ssh/millenova_hostinger
~/.ssh/millenova_vps
```

These names are local profile guidance, not public hard-coded defaults.

## 7.2 Acceptance

- [ ] two independently revocable keys exist outside all repositories;
- [ ] public fingerprints match the private profile;
- [ ] explicit host-key fingerprints are enrolled;
- [ ] non-writing key-auth probe passes for each intended target;
- [ ] wrong/missing key and changed-host-key fixtures fail closed;
- [ ] no account password, key passphrase or private-key content is stored/logged.

**Operator smoke:** register each public key and prove each target reports `KEY AUTH PASS` without executing a deployment command.

---

# 8. PDA-2 — One-session access broker

**Status:** ⬜ QUEUED

Use an app-specific SSH agent in WSL:

```text
${XDG_RUNTIME_DIR:-/tmp}/zdeploypet/ssh-agent.sock
```

## 8.1 Required implementation

- start or safely reuse only the app-owned agent;
- validate state-directory/socket/PID ownership and permissions;
- load only approved fingerprints with a bounded `ssh-add -t` lifetime;
- open a visible trusted terminal for passphrase entry;
- return structured `LOCKED`, `PARTIAL`, `READY`, `EXPIRED` or `UNAVAILABLE` state;
- expose target-specific key readiness;
- unload keys and terminate the app-owned agent on Lock;
- lock on normal app close by default;
- recover safely from stale sockets, WSL restart and abandoned/crashed sessions;
- pass `SSH_AUTH_SOCK` only to approved probes/executions.

## 8.2 Acceptance

- [ ] one Unlock action makes all required targets READY;
- [ ] repeat probes/deployments within the lease cause no repeat passphrase/account-password prompts;
- [ ] Lock removes keys and causes probes to fail safely;
- [ ] close, expiry, logoff/reboot and WSL restart produce deterministic locked/recovery state;
- [ ] foreign/stale agent state cannot be adopted silently.

**Operator smoke:** unlock once, repeat both target probes without prompts, lock, and prove both probes become unavailable.

---

# 9. PDA-3 — Production application shell and companion

**Status:** ⬜ QUEUED

The current setup/discovery window is a functional prototype. PDA-3 turns it into the stable application shell.

## 9.1 Required implementation

- enforce one application instance and activate the existing window;
- split orchestration/state from `MainWindow` into testable services/view models;
- centralize theme, spacing, controls and status semantics;
- support DPI scaling and clamped multi-monitor window placement;
- show profile, repository, report and access state without exposing secrets;
- add Unlock/Lock controls and target readiness;
- distinguish quiet monitoring from foreground work;
- give DEV, portable and installed builds distinct identity/state roots;
- implement the original robotic space-otter courier companion states from [`docs/PRODUCT_IDENTITY.md`](docs/PRODUCT_IDENTITY.md);
- provide text, symbol, reduced-motion and static fallbacks;
- ensure rendering failure cannot affect authentication or execution.

## 9.2 Acceptance

- [ ] only one application instance owns a profile/session;
- [ ] no app-owned secret input field exists;
- [ ] status text remains complete without color or animation;
- [ ] window placement survives normal display changes safely;
- [ ] companion state exactly reflects the underlying state machine;
- [ ] UI/rendering failure cannot unlock, execute or alter report truth.

---

# 10. PDA-4 — Read-only report monitor

**Status:** ⬜ QUEUED

The Millenova deployment reporter remains authoritative for Millenova. Generic profiles may declare another supported reporter adapter or no structured reporter.

## 10.1 Required implementation

- parse supported schema versions explicitly;
- display latest run and bounded history;
- show deployment ID, source/release transition, target, mode, result, exit status and duration;
- show checks, test totals, per-target transfer/activation/health and bounded failure detail;
- open/copy known-safe summary and full-log artifacts;
- canonicalize every artifact beneath the configured report root;
- fail closed on malformed/unsupported JSON;
- never infer success from process exit or UI completion when reporter truth disagrees.

## 10.2 Acceptance

- [ ] valid success/failure fixtures render correctly;
- [ ] malformed/unknown schema fails closed;
- [ ] path traversal/symlink escape fixtures cannot open arbitrary files;
- [ ] missing reporter is shown distinctly from deployment failure;
- [ ] monitoring does not mutate reports or target state.

---

# 11. PDA-5 — Dry-run executor

**Status:** ⬜ QUEUED

## 11.1 Required implementation

- choose only profile-declared targets, release inputs and dry/live modes;
- display immutable pre-run review;
- launch the fixed, contained script from the approved WSL project directory;
- inject only the dedicated `SSH_AUTH_SOCK` and declared reporter environment;
- allocate a pseudo-terminal only when required;
- drive only an explicit prompt contract;
- fail closed on unknown prompts and all password prompts;
- prevent concurrent deployment processes;
- support cancellation without bypassing reporter cleanup;
- stream bounded output while treating final reporter JSON as truth;
- never synthesize file-transfer or destination behavior independently of the script.

## 11.2 Acceptance

- [ ] Hostinger-only, VPS-only and combined dry runs select the intended script paths;
- [ ] no account-password prompt appears after one valid unlock;
- [ ] unknown/password prompt stops safely;
- [ ] second concurrent run is rejected;
- [ ] cancellation is explicit and report cleanup completes;
- [ ] dry run cannot cross into live confirmation/execution.

---

# 12. PDA-6 — Live executor

**Status:** ⬜ QUEUED / GATED BY PDA-5 DRY-RUN ACCEPTANCE

## 12.1 Required implementation

- require an immutable review of profile, commit, target, release and live mode;
- require the exact profile-declared live confirmation phrase in the UI;
- preserve the deployment script's own confirmation gate;
- never auto-fill or bypass either authorization boundary;
- prevent stale review state from authorizing a changed repository/profile;
- never automatically retry a partial live deployment;
- surface reporter failure and next safe action without guessing recovery.

## 12.2 Acceptance

- [ ] incorrect/empty phrase blocks execution;
- [ ] changed repository/profile invalidates prior review;
- [ ] combined live deployment completes after one unlock with no account-password prompts;
- [ ] only the explicit live authorization stops the expected flow;
- [ ] partial failure is reported and never auto-retried;
- [ ] final UI state agrees with reporter JSON.

---

# 13. PDA-7 — Incident bundle and agent handoff

**Status:** ⬜ QUEUED

GitHub carries source. A sanitized bundle carries failure evidence. Credentials and deployment authority remain local.

```text
ZDeployPetIncidents/<deployment-id>/
├── incident.json
├── report.json
├── report.summary.txt
├── failure-excerpt.txt
├── repository-state.txt
├── git-status.txt
├── committed-changes.patch
├── staged-changes.patch
├── working-tree.patch
├── untracked-files-manifest.txt
└── README_FOR_AGENT.md
```

## 13.1 Required implementation

- create bundles outside source repositories and private by default;
- include bounded repository/branch/HEAD/upstream/ahead-behind and deployment context;
- include unpushed commit patches when remote state is known;
- include staged/working-tree diffs separately and binary-capably where safe;
- include untracked files only by explicit allowlist plus secret scan;
- make redacted full-log inclusion explicit and previewable;
- offer `Open in local Codex`, `Create ChatGPT upload ZIP`, `Copy failure summary`, and optional private diagnostic branch;
- preserve a ZIP path when GitHub push is unavailable;
- import returned patches only through check → review → explicit apply → tests → new deployment authorization.

## 13.2 Acceptance

- [ ] simulated deployment failure produces a useful sanitized bundle;
- [ ] simulated push failure remains diagnosable without GitHub;
- [ ] secret fixtures are redacted or block export;
- [ ] invalid/path-escaping returned patches are rejected;
- [ ] valid returned patches still require human review;
- [ ] no bundle contains keys, agent state, tokens or production configuration.

---

# 14. PDA-8 — Hardening, public repository and release

**Status:** ⬜ QUEUED; some groundwork complete

## 14.1 Groundwork already complete

- source and local release output are separated;
- `scripts/publish-local.ps1` builds, runs both test projects and publishes one self-contained win-x64 EXE;
- generated artifacts are ignored;
- VS Code workspace exists;
- profile example is sanitized;
- GitPet project scope and hygiene were configured;
- repository has a clean `main` history except the deliberately untracked local operator note.

## 14.2 Remaining implementation

- create/verify the repository's own public GitHub origin;
- audit complete history and release tree for secrets/personal infrastructure;
- add `LICENSE`, `SECURITY.md`, `CONTRIBUTING.md`, threat model and incident-handoff documentation;
- add clean-clone CI for build/tests and secret/path audits;
- isolate all build/test/publish artifact roots;
- implement distinct DEV/portable/installed identities;
- implement a versioned release builder separate from `publish-local.ps1`;
- produce portable and installer artifacts;
- generate checksums and a machine-readable manifest tied to an exact clean commit;
- restrict self-update to installed builds and verify installer hashes;
- test clean install, upgrade, portable use and uninstall without deleting user profiles unexpectedly;
- ensure a clean clone builds/tests without Millenova or production access.

## 14.3 Acceptance

- [ ] public GitHub repository is connected and correctly described;
- [ ] clean clone builds/tests on a supported Windows host;
- [ ] no private binding, key, host, account, report or incident evidence exists in source/history/artifacts;
- [ ] self-contained portable EXE works without the .NET SDK;
- [ ] installer and portable artifacts match published SHA-256/manifest;
- [ ] packages come from an exact clean named-branch commit;
- [ ] auth/session failure matrix passes;
- [ ] Hostinger-only, VPS-only and combined dry/live matrix passes for the private Millenova profile;
- [ ] project deployment script fingerprint and reporter DR-0 through DR-6 remain unchanged/green;
- [ ] public docs explain safe onboarding without exposing private infrastructure.

---

# 15. Validation matrix

| Area | Case | Expected status |
| --- | --- | --- |
| Discovery | Windows/.NET/WSL/OpenSSH available | ✅ PDA-0 accepted |
| Discovery | WSL argument forwarding | ✅ automated regression covered |
| Profile | First run without validated profile | 🟡 implemented; final smoke pending |
| Profile | Incomplete draft save/close/reopen | 🟡 deadlock fix implemented; operator smoke pending |
| Profile | Script outside approved root | ✅ automated rejection |
| Profile | Unsafe `/`, home root or traversal destination | ✅ automated rejection |
| Profile | Multiple destinations for one target | ✅ model/tests implemented; restart smoke pending |
| Profile | Suggestions/forget behavior | 🟡 implemented; operator smoke pending |
| Public source | Sanitized example contract | ✅ present |
| Public source | Own GitHub origin | ⬜ not verified/pending |
| Authentication | Dedicated keys/fingerprints | ⬜ PDA-1 |
| Authentication | Host-key enrollment/mismatch | ⬜ PDA-1 |
| Session | Unlock once/repeat probes/lock | ⬜ PDA-2 |
| Session | Close/expiry/reboot/WSL restart | ⬜ PDA-2/PDA-8 |
| Shell | Single instance/accessibility/companion | ⬜ PDA-3 |
| Reporter | Valid/malformed/path escape | ⬜ PDA-4 |
| Dry execution | Target combinations/no password prompts | ⬜ PDA-5 |
| Dry execution | Unknown prompt/concurrency/cancel | ⬜ PDA-5 |
| Live execution | Exact confirmation/stale review | ⬜ PDA-6 |
| Live execution | Partial failure/no auto-retry | ⬜ PDA-6 |
| Incident | Deploy failure/push failure/secret fixture | ⬜ PDA-7 |
| Release | Clean clone/portable/installer/manifest/hash | ⬜ PDA-8 |

Current automated suites:

```powershell
dotnet build ZDeployPet.sln -c Release
dotnet run --project tests\ZDeployPet.Core.Tests\ZDeployPet.Core.Tests.csproj -c Release
dotnet run --project tests\ZDeployPet.Infrastructure.Tests\ZDeployPet.Infrastructure.Tests.csproj -c Release
```

Normal visual-smoke build:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\publish-local.ps1
```

---

# 16. Explicit non-goals

Do not implement unless the product direction changes explicitly:

- ❌ a browser OAuth imitation for SSH authentication;
- ❌ storage of remote account passwords or private-key passphrases;
- ❌ a built-in private-key vault in v1;
- ❌ a public web deployment endpoint;
- ❌ a generic terminal or arbitrary-command runner;
- ❌ automatic invention of rsync/source/destination rules;
- ❌ modification of a project's deployment script merely to fit ZDeployPet;
- ❌ implicit `StrictHostKeyChecking=no` or automatic host-key replacement;
- ❌ automatic live confirmation, retry, rollback or deployment;
- ❌ granting ChatGPT/Codex/another remote agent production credentials or an agent socket;
- ❌ requiring a successful Git push before a deployment failure can be diagnosed;
- ❌ committing private profiles, reports or incident bundles to a public repository;
- ❌ making mascot animation or visual styling a dependency of security/execution state;
- ❌ embedding ZDeployPet into ZomniverseGitPet or Millenova as a tightly coupled feature;
- ❌ claiming universal deployment-engine support before a declared adapter/contract exists;
- ❌ publishing an installer or release artifact from a dirty/unidentified source state.

---

# 17. Agent implementation order

```text
PDA-0 standalone discovery ✅
  ↓
PDA-0A community profile onboarding 🟡 acceptance smoke
  ├─ schema/store ✅
  ├─ project/WSL/script validation ✅
  ├─ multiple targets/destinations ✅
  ├─ friendly help/suggestions/draft recovery ✅ code
  ├─ local publish workflow ✅
  └─ corrected close/restart + full-profile smoke ⬜
  ↓
PDA-1 key onboarding + host identity
  ↓
PDA-2 bounded one-session SSH-agent broker
  ↓
PDA-3 stable single-instance shell + accessible companion
  ↓
PDA-4 read-only structured report monitor
  ↓
PDA-5 allowlisted dry-run executor
  ↓
PDA-6 live executor + exact human authorization
  ↓
PDA-7 sanitized incident/agent handoff
  ↓
PDA-8 hardening + clean-clone CI + portable/installer public release
```

PDA-1 may design key onboarding, but it must not be accepted or used against production before PDA-0A's local profile lifecycle is proven. PDA-6 cannot begin before PDA-5 dry-run behavior is accepted. PDA-8 release packaging cannot claim readiness while any security/production smoke remains open.

---

# 18. Current handoff snapshot

```text
CURRENT PHASE:                 PDA-0A — Community profile onboarding
REVIEWED BASE:                 main@219336a
LAST CHECKPOINT:               checkpoint: 2026-10-08 11:57
GITHUB ORIGIN:                 not present at review time; verify after GitPet creation
LOCAL-ONLY UNTRACKED FILE:     docs/Wilder_Notes.md (not normative; do not publish by default)

PDA-0 RESULT:                 ACCEPTED — Discovery passed
PDA-0A CODE STATE:            implemented through profile/destination UX,
                               suggestions, draft recovery and local publish loop
PDA-0A ACCEPTANCE STATE:      pending corrected operator smoke

LATEST FIX:                   close-path async deadlock removed
                               close now uses synchronous atomic local draft persistence
                               unique temp files prevent overlapping-save collisions

DEVELOPMENT COMMAND:          powershell -ExecutionPolicy Bypass -File scripts\publish-local.ps1
SMOKE EXECUTABLE:             C:\Dev\ZDeployPet_Releases\current\ZDeployPet.exe

NEXT CONCRETE ACTION:         1. publish latest local build
                               2. enter profile name only
                               3. Check settings and save
                               4. confirm draft-required status
                               5. close immediately
                               6. reopen and confirm restoration
                               7. complete valid private profile
                               8. restart and confirm Discovery passed
                               9. verify GitHub origin/public contents
                               10. mark PDA-0A accepted; begin PDA-1

DO NOT SKIP:                  no server contact during PDA-0A,
                               no credentials in profile/history/source,
                               no WPF sync-over-async,
                               no script mutation,
                               no public real infrastructure defaults
```

---

# 19. Crash/restart instructions

If the app, terminal or chat stops, read this file first.

```powershell
cd C:\Dev\ZDeployPet
git status --short
git log -5 --oneline
git remote -v
git show --stat --oneline HEAD

dotnet build ZDeployPet.sln -c Release
dotnet run --project tests\ZDeployPet.Core.Tests\ZDeployPet.Core.Tests.csproj -c Release
dotnet run --project tests\ZDeployPet.Infrastructure.Tests\ZDeployPet.Infrastructure.Tests.csproj -c Release
```

For normal visual testing:

```powershell
Get-Process -Name ZDeployPet -ErrorAction SilentlyContinue |
    Select-Object Id,ProcessName,Path

Stop-Process -Name ZDeployPet -Force -ErrorAction SilentlyContinue
powershell -ExecutionPolicy Bypass -File scripts\publish-local.ps1
C:\Dev\ZDeployPet_Releases\current\ZDeployPet.exe
```

Before changing anything after a restart:

1. confirm the current phase and next action in §§17–18;
2. inspect the working tree and preserve unrelated/user-owned files;
3. do not add `docs/Wilder_Notes.md` to the public project without explicit intent;
4. verify the GitHub origin rather than assuming GitPet completed creation;
5. do not contact a remote target while PDA-0A is active;
6. do not create keys or an agent until PDA-1/PDA-2 begins;
7. never modify the Millenova deployment script as part of ZDeployPet work;
8. use the published development EXE for operator smoke, not a stale `dotnet run` process;
9. update this plan before ending any phase transition.

This file is the durable ZDeployPet handoff record. A phase is unfinished until its code, automated validation, operator smoke, documentation and compact status agree.
