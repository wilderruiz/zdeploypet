# ZDeployPet — Master Implementation Plan

> [!IMPORTANT]
> This file is the authoritative programme index and handoff record for ZDeployPet. Update the compact overview, current handoff, affected phase, validation matrix, and next action together whenever the programme changes. Do not reconstruct status from memory when this file and repository evidence are available.

**Status:** 🟡 **PDA-1 active — key onboarding and diagnostics next**  
**Reviewed repository baseline:** `main@21084ea` (`checkpoint: 2026-10-08 23:33`)  
**Completed phase:** `PDA-0A — Community profile onboarding`  
**Active phase:** `PDA-1 — Key onboarding and diagnostics`  
**Next phase after acceptance:** `PDA-2 — Bounded SSH-agent session`  
**Remaining phases:** 8 including active PDA-1; 7 after PDA-1 acceptance  
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
| **PDA-1** | 🟡 ACTIVE | Dedicated key onboarding, public-key/host-key fingerprints, non-writing target probes | This plan §7 |
| **PDA-2** | ⬜ QUEUED | App-owned bounded SSH-agent session: unlock once, status, expiry and lock | This plan §8 |
| **PDA-3** | ⬜ QUEUED | Single-instance production shell, accessible state model, companion pet, dark/red visual identity, menu/about and console/log UI | [`docs/PRODUCT_IDENTITY.md`](docs/PRODUCT_IDENTITY.md), this plan §9 |
| **PDA-4** | ⬜ QUEUED | Read-only structured deployment-report monitor | Millenova reporter plan, this plan §10 |
| **PDA-5** | ⬜ QUEUED | Allowlisted dry-run executor for the unchanged project script | This plan §11 |
| **PDA-6** | ⬜ QUEUED | Live executor with immutable review and exact human confirmation | This plan §12 |
| **PDA-7** | ⬜ QUEUED | Sanitized incident bundle and local/remote-agent handoff | This plan §13 |
| **PDA-8** | ⬜ QUEUED | Security hardening, clean-clone CI, installer/portable release, desktop shortcut verification and public launch | This plan §14 |

### Immediate next action

Begin PDA-1 implementation and validation:

- add dedicated deployment-key onboarding without collecting private-key passphrases in app-owned fields;
- display and persist only approved public-key fingerprints and expected host-key fingerprints;
- add non-writing SSH target probes that prove identity/connectivity without deployment side effects;
- preserve the existing profile boundary, keeping real endpoints and machine-specific bindings local;
- maintain the no-password-storage, no-generic-shell and no-live-deploy guarantees.

### PDA-0A acceptance evidence

Corrected operator smoke passed from:

```text
C:\Dev\ZDeployPet_Releases\current\ZDeployPet.exe
```

Verified on 2026-10-09:

1. incomplete draft saves without contacting WSL or a server when the project folder is missing;
2. UI reports `Draft saved — project folder required`;
3. app closes without UI-thread deadlock;
4. entered profile name restores after restart;
5. tooltip activation is limited to each card's `i` button;
6. `Forget suggestions` clears remembered suggestions without deleting the current draft or saved profile.

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

**Status:** ✅ ACCEPTED 2026-10-09

| Subphase | Status | Implemented result |
| --- | --- | --- |
| **0A.1 Profile contract/storage** | ✅ COMPLETE | Schema v1, atomic local profile save/load, GUID identity, Git-ignored per-user storage |
| **0A.2 Project/WSL/script validation** | ✅ COMPLETE | Folder picker, installed WSL selection, derived/verified WSL path, contained script validation |
| **0A.3 Targets and destinations** | ✅ COMPLETE | Multiple SSH targets and multiple labelled destinations per target; safe `/path` and scoped `~/path` rules |
| **0A.4 First-run usability** | ✅ ACCEPTED | Plain-language five-section UI, `i`-only tooltips, dropdown suggestions, forget-history action, incomplete draft recovery |
| **0A.5 Local publish loop** | ✅ COMPLETE | VS Code workspace and build/test/self-contained `publish-local.ps1` workflow |
| **0A.6 Public repository readiness** | ✅ COMPLETE FOR CURRENT SCOPE | GitHub repository linked and healthy; source/public scope established; broader launch policy/license/release work remains PDA-8 |

PDA-0A acceptance is based on the corrected operator smoke listed near the top of this plan.

---

# 7. PDA-1 — Key onboarding and diagnostics

**Status:** 🟡 ACTIVE

PDA-1 establishes explicit key identity and non-writing target verification before any bounded session is introduced.

Required outcomes:

- enumerate/select a dedicated deployment public key without reading or storing its passphrase;
- compute and display the selected public-key fingerprint;
- allow explicit expected host-key enrollment per target and display the enrolled fingerprint;
- hard-block host-key changes until the operator explicitly re-enrolls;
- add non-writing SSH probes for configured targets using allowlisted command forms only;
- surface actionable diagnostics for missing key, unreachable host, bad username/port, key rejection and host-key mismatch;
- never modify target files, deployment destinations or application state during probe operations;
- keep all real hosts, usernames, ports and fingerprints in the private local profile boundary.

Acceptance gate:

- key and host fingerprints are visible and stable;
- probe success proves only authenticated non-writing reachability;
- key mismatch and host-key mismatch fail closed;
- no password/passphrase is stored or echoed by ZDeployPet;
- tests cover command construction and diagnostic parsing;
- owning operator smoke is completed against the intended private profile.

---

# 8. PDA-2 — Bounded SSH-agent session

**Status:** ⬜ QUEUED

PDA-2 introduces app-owned bounded access state without making the app a credential collector.

Required outcomes:

- create/own a dedicated SSH-agent process/session;
- unlock the approved key through trusted terminal/`ssh-add`, not an app password field;
- support bounded lifetime, initially eight hours;
- show LOCKED / READY / EXPIRING state;
- support explicit Lock and lock-on-close;
- verify loaded key fingerprint(s) against the profile before declaring READY;
- reject stale, foreign or unverifiable agent state;
- persist only non-secret session metadata required for safe recovery/expiry decisions.

Acceptance gate:

- one unlock supports repeated authenticated probes/deployments within the valid lease;
- closing ZDeployPet locks access;
- expired or foreign state fails closed;
- no agent socket or private material is exported.

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
- view-model/service separation so session, execution, reporting and pet/UI behavior do not accumulate in `MainWindow`;
- preserve responsive async behavior and avoid dispatcher deadlocks.

Acceptance gate:

- shell remains single-instance and responsive across setup/session/deployment states;
- dark/red visual identity is consistent and accessible;
- icons appear correctly in window chrome, executable, taskbar and packaged shortcut contexts;
- About contents report the actual build metadata and repository accurately;
- pet state mirrors application state but cannot trigger privileged actions;
- Console / Activity view shows useful sanitized diagnostics with no credential leakage;
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
- release secret scan and artifact audit;
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
- release package contains no secrets/private infrastructure;
- public documentation matches shipped behavior.

---

# 15. Validation matrix

| Area | Automated | Operator smoke | Production/private target smoke |
| --- | --- | --- | --- |
| PDA-0 discovery | ✅ | ✅ | N/A |
| PDA-0A profile onboarding | ✅ | ✅ 2026-10-09 | N/A |
| PDA-1 key/host onboarding | Required | Required | Required non-writing probe |
| PDA-2 bounded agent | Required | Required | Required |
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
**Baseline:** `21084eaa8829521ff456dd2ddbb8aeb18b83520c`  
**Repository state at handoff:** local and GitHub `main` aligned; previous standalone-publishing history preserved on `pre-migration-standalone`  
**Completed:** PDA-0 and PDA-0A  
**Active:** PDA-1

Recent acceptance evidence:

- corrected PDA-0A smoke passed from `C:\Dev\ZDeployPet_Releases\current\ZDeployPet.exe`;
- incomplete draft behavior, close/restart recovery, `i`-only help and suggestion-history clearing verified;
- GitHub repository migration completed with old standalone history preserved on `pre-migration-standalone`;
- ZomniverseGitPet now guards against unrelated-history reconciliation before attempting a merge.

Next concrete implementation target:

1. design the PDA-1 local key/host fingerprint records;
2. implement public-key fingerprint extraction without passphrase capture;
3. implement explicit host-key enrollment and mismatch hard-block;
4. add allowlisted non-writing target probes;
5. add tests and complete the owning private-profile smoke.

Future product-shell requirements already committed to the roadmap:

- ZDeployPet icon set and packaged shortcut integration;
- actual companion/pet UI;
- top menu and About dialog with version/build/platform/license/repository information;
- primary dark theme with red accent;
- sanitized Console / Activity log view;
- installer/portable parity verification against the validated local build.

---

# 17. Restart instructions

When resuming work:

1. read this file first;
2. confirm repository `main` and current checkpoint;
3. inspect the active phase only after reading its acceptance gate;
4. preserve all security invariants before expanding UI behavior;
5. keep real infrastructure data outside the public repository;
6. update this plan in the same change that materially changes phase status or acceptance evidence.
