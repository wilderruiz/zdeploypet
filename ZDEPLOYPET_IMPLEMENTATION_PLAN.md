# ZDeployPet — Master Implementation Plan

> [!IMPORTANT]
> This file is the authoritative programme index and handoff record for ZDeployPet. Update the compact overview, current handoff, affected phase, validation matrix, and next action together whenever the programme changes. Do not reconstruct status from memory when this file and repository evidence are available.

**Status:** 🟡 **PDA-6 active — live executor and exact confirmation**  
**Reviewed repository baseline:** `main@2787b235` (`2026-10-10`)  
**Completed phase:** `PDA-5 — Allowlisted dry-run executor`  
**Active phase:** `PDA-6 — Live executor and exact confirmation`  
**Next phase after acceptance:** `PDA-7 — Sanitized incident bundle and agent handoff`  
**Remaining phases:** 3 including active PDA-6; 2 after PDA-6 acceptance  
**Primary outcome:** bounded deployment access, authoritative report monitoring, safe dry-run execution, then explicitly human-authorized live deployment of the unchanged project script  
**Product boundary:** standalone Windows/.NET 8 WPF application using WSL through a narrow bridge  
**Deployment boundary:** project-owned deployment scripts remain authoritative and are not rewritten by ZDeployPet  
**Decision owner:** Wilder Ruiz

## Compact phase overview

**Legend:** ✅ complete · 🟡 active/acceptance pending · ⬜ queued · ⏸ gated · ❌ rejected

| Phase | Status | Outcome | Primary documentation |
| --- | --- | --- | --- |
| **PDA-0** | ✅ ACCEPTED 2026-10-08 | Standalone repository, read-only Windows/WSL/OpenSSH/project/script/report discovery | [`docs/PDA0_DISCOVERY.md`](docs/PDA0_DISCOVERY.md) |
| **PDA-0A** | ✅ ACCEPTED 2026-10-09 | Community-safe local profiles, first-run setup, multiple targets/destinations, input guidance/history/draft recovery | [`docs/PROFILES.md`](docs/PROFILES.md), [`docs/PRODUCT_IDENTITY.md`](docs/PRODUCT_IDENTITY.md) |
| **PDA-1** | ✅ ACCEPTED 2026-10-09 | Dedicated deployment key, public-key-only installation, explicit host trust, non-writing authenticated probes, Git/private-key safety | [`docs/PDA1_KEY_ONBOARDING.md`](docs/PDA1_KEY_ONBOARDING.md) |
| **PDA-2** | ✅ ACCEPTED 2026-10-10 | App-owned bounded SSH-agent session with READY/LOCKED state, expiry and lock-on-close | This plan §8 |
| **PDA-3** | ✅ ACCEPTED 2026-10-10 | Production shell, single-instance behavior, ZPet, dark/red identity, menus/About and separated Console / Activity | [`docs/PDA3_PRODUCTION_SHELL.md`](docs/PDA3_PRODUCTION_SHELL.md) |
| **PDA-4** | ✅ ACCEPTED 2026-10-10 | Read-only authoritative latest + historical deployment-report monitor with bounded artifact viewers and lightweight polling | [`docs/PDA4_REPORT_MONITOR.md`](docs/PDA4_REPORT_MONITOR.md) |
| **PDA-5** | ✅ ACCEPTED 2026-10-10 | Allowlisted dry-run executor for the approved unchanged project script | [`docs/PDA5_DRY_RUN_EXECUTOR.md`](docs/PDA5_DRY_RUN_EXECUTOR.md) |
| **PDA-6** | 🟡 ACTIVE | Live executor with immutable review and exact human confirmation | This plan §12 |
| **PDA-7** | ⬜ QUEUED | Sanitized incident bundle and local/remote-agent handoff | This plan §13 |
| **PDA-8** | ⬜ QUEUED | Security hardening, clean-clone CI, installer/portable release, desktop shortcut verification and public launch | This plan §14 |

### Immediate next action — PDA-6

Implement PDA-6 in this order:

1. define immutable Core live-deployment request/review contracts; do not drive live execution directly from WPF;
2. reuse PDA-5 script approval, target allowlists, bounded release choices, PDA-2 runtime revalidation and single-execution lease;
3. build a pre-deployment review surface showing project/profile, exact target, destination summary, release transition, script path and current approved SHA-256;
4. derive the exact project-required live confirmation phrase from the fixed project adapter/contract and display it to the operator;
5. require the human operator to type that exact phrase in an app-owned confirmation field; mismatch/extra whitespace/case change fails closed;
6. the app must never generate, auto-fill, paste, synthesize or otherwise supply the live confirmation phrase on the operator's behalf;
7. after exact confirmation, pass only the bounded live choices and the operator-entered phrase to the approved script through a narrow adapter; never pre-buffer unrelated stdin;
8. stream natural live script output only to the **Live deploy** console tab while ZDeployPet application/session events remain isolated;
9. never auto-retry a partial/failed live deployment; completion always refreshes PDA-4 reporter truth and the structured report remains authoritative over process exit code;
10. add automated negative tests for wrong/missing confirmation, changed script fingerprint, non-READY access, target/release tampering, concurrent execution and any attempt to auto-supply confirmation;
11. complete one explicit real Millenova live-deployment smoke only after review/confirmation UI and automated safety tests are green.

### Recent acceptance evidence

PDA-2 accepted after operator smoke confirmed one unlock establishes READY with the approved key, repeated Hostinger/VPS probes work in the bounded session, explicit Lock returns to LOCKED, app close/reopen remains LOCKED, and no app-owned passphrase field is used.

PDA-3 accepted after operator smoke confirmed single-instance activation, dark/red production shell, menus/About, separated/sanitized Console / Activity, responsive layout memory, non-authoritative ZPet companion states, runtime window/taskbar identity and keyboard accessibility. Packaged executable/installer/shortcut icon parity remains intentionally deferred to PDA-8.

PDA-4 accepted after real Millenova reporter smoke confirmed the latest PASS card, Summary / Full log viewers, newest-first 30-row history, row-specific artifacts, UTF-8 decoding, fail-closed report/path validation, passing Release tests and lightweight visible-only polling.

PDA-5 accepted 2026-10-10 after the final implementation safely completed a real **Both targets / PATCH** Millenova dry run from the ZDeployPet UI: approved script fingerprint matched, access was ON / READY, the script printed `DRY RUN — NOTHING WILL BE MODIFIED`, Hostinger/VPS delta previews completed, `DRY RUN COMPLETE` appeared, local `version.txt` was restored, reporter artifacts identified `v4.4.25_dry_both`, exit code was `0`, no live authorization phrase appeared, and the final Release solution regression run was reported green. The earlier stdin-prefeed prototype that accidentally entered live mode is permanently rejected and documented as negative safety evidence.

---

# 1. Documentation map and precedence

| Document | Role | Authority |
| --- | --- | --- |
| [`ZDEPLOYPET_IMPLEMENTATION_PLAN.md`](ZDEPLOYPET_IMPLEMENTATION_PLAN.md) | Master roadmap, programme status, handoff and sequencing | Programme authority |
| [`README.md`](README.md) | Public product introduction and daily build/test/publish commands | User/developer entry point |
| [`docs/PDA0_DISCOVERY.md`](docs/PDA0_DISCOVERY.md) | Accepted machine-capability baseline | PDA-0 evidence |
| [`docs/PDA1_KEY_ONBOARDING.md`](docs/PDA1_KEY_ONBOARDING.md) | Accepted deployment-key/host-trust/probe workflow | PDA-1 evidence |
| [`docs/PDA3_PRODUCTION_SHELL.md`](docs/PDA3_PRODUCTION_SHELL.md) | Accepted production-shell scope/evidence | PDA-3 evidence |
| [`docs/PDA4_REPORT_MONITOR.md`](docs/PDA4_REPORT_MONITOR.md) | Accepted report-monitor contract/evidence | PDA-4 evidence |
| [`docs/PDA5_DRY_RUN_EXECUTOR.md`](docs/PDA5_DRY_RUN_EXECUTOR.md) | Accepted dry-run execution contract, incident evidence and smoke result | PDA-5 evidence |
| [`docs/PRODUCT_IDENTITY.md`](docs/PRODUCT_IDENTITY.md) | Product boundary and identity direction | Naming/identity boundary |
| [`docs/PROFILES.md`](docs/PROFILES.md) | Shareable-contract/private-binding split and profile validation | Profile contract summary |

ZDeployPet is generic; Millenova is its first private local profile. Project-owned deployment/reporter contracts own project-specific semantics. This master plan owns ZDeployPet sequencing/status.

Source-of-truth order when documents disagree:

1. runtime security invariants and validated code behavior;
2. this master plan for ZDeployPet sequencing/status;
3. focused ZDeployPet phase documents for narrow contracts;
4. project-owned reporter/deployment-script contracts for project-specific behavior;
5. private local profile for real endpoints/paths;
6. examples/screenshots only as illustrative evidence.

---

# 2. Product and configuration boundary

ZDeployPet is a friendly local Windows + WSL deployment companion. It is not a generic remote shell, credential vault, website admin page, Git client or replacement deployment engine.

```text
PROJECT REPOSITORY
shareable .zdeploypet contract + project-owned deploy script
        ↓
PRIVATE LOCAL PROFILE
machine path + WSL + targets + destinations + report root
        ↓
LOCAL DEPLOYMENT IDENTITY
private key stays in WSL ~/.ssh; public half only is installed remotely
        ↓
BOUNDED ACCESS SESSION
app-owned SSH agent + approved fingerprint + expiry
        ↓
SAFE EXECUTION
dry run first; live only after exact human confirmation
        ↓
AUTHORITATIVE REPORT
structured JSON + summary + full log + history
        ↓
SANITIZED HANDOFF
local/remote debugging evidence without deployment authority
```

The project-owned deployment script remains authoritative for build/test/source selection/transfer/activation/application health/report generation. ZDeployPet validates and invokes the already-approved script; it does not rewrite project deployment logic.

Private profile/session material stays under the per-user local application boundary. Private keys stay under the selected WSL user's `~/.ssh`. Real hosts/users/ports/personal paths must not become public defaults.

---

# 3. Security invariants

The implementation must not:

- store remote account passwords, private-key passphrases, tokens or private-key bytes in normal profile/application JSON;
- collect a private-key passphrase in an app-owned field;
- copy private keys into source repositories, release packages or remote targets;
- export an SSH-agent socket;
- use `StrictHostKeyChecking=no` or silently accept changed host keys;
- expose a production HTTP deployment endpoint or generic arbitrary-command textbox;
- silently broaden targets/destinations;
- rewrite the project deployment script to fit the application;
- auto-type, auto-fill or synthesize a live confirmation phrase;
- auto-retry a partial live deployment;
- treat process exit code/UI state as stronger than structured reporter truth;
- make an agent-generated patch trusted without human review/tests/fresh deployment authorization;
- commit real infrastructure data, reports, incident bundles or credentials to the public repository.

Required controls include exact host-key/fingerprint validation, bounded session lifetime, lock-on-close, fixed script/action/input allowlists, script-fingerprint verification, single execution lock, canonical report/artifact path containment, bounded evidence, redaction and explicit human authorization for live deployment.

---

# 4. Accepted foundation — PDA-0 / PDA-0A / PDA-1

## PDA-0 — Foundation/discovery

**Status:** ✅ ACCEPTED 2026-10-08

Standalone .NET 8/WPF repository, App/Core/Infrastructure/WslBridge separation, read-only Windows/WSL/OpenSSH/project/script/report discovery, script SHA-256 discovery and corrected WSL argument forwarding were completed and smoke-tested.

## PDA-0A — Profile onboarding

**Status:** ✅ ACCEPTED 2026-10-09

Completed schema-v1 local profile storage, WSL/project/script validation, multiple SSH targets/destinations, first-run UI, structured guidance, input history/draft recovery and local publish loop.

## PDA-1 — Key onboarding and diagnostics

**Status:** ✅ ACCEPTED 2026-10-09

Accepted behavior includes profile-scoped Ed25519 deployment keys, explicit public-key fingerprint approval, independent host-key verification/enrollment, public-key-only installation, non-writing authenticated probes, exact enrolled host-key-type checks, onboarding state and Git/private-key safety scanning.

---

# 8. PDA-2 — Bounded SSH-agent session

**Status:** ✅ ACCEPTED 2026-10-10

Accepted outcomes:

- app-owned dedicated SSH-agent session in selected WSL;
- trusted-terminal unlock without app-owned passphrase capture;
- loaded fingerprint must exactly match the PDA-1-approved deployment key before READY;
- LOCKED / READY / EXPIRING / expiry behavior with bounded lease;
- explicit Lock and lock-on-close;
- restart never restores authorization from stale metadata alone;
- repeated target probes work during one valid session without private-key file forwarding;
- agent socket/session authorization is never exported.

---

# 9. PDA-3 — Production shell, companion and operator visibility

**Status:** ✅ ACCEPTED 2026-10-10

Accepted outcomes include single-instance shell, dark/red identity, menus/About, separated sanitized Console / Activity, responsive layout memory, ZPet state companion, runtime vector identity, focus cues and keyboard accessibility. Packaged executable/installer/desktop-shortcut icon work remains deferred to PDA-8.

---

# 10. PDA-4 — Structured deployment-report monitor

**Status:** ✅ ACCEPTED 2026-10-10

Accepted outcomes include authoritative schema-v1 latest/history reporting, canonical path containment, bounded read-only Summary / Full log viewers, explicit UTF-8, newest-first deduplicated history, fail-closed malformed/untrusted handling and lightweight visible-only polling that refreshes history only for a genuinely new deployment id.

Acceptance evidence and security details are maintained in [`docs/PDA4_REPORT_MONITOR.md`](docs/PDA4_REPORT_MONITOR.md).

---

# 11. PDA-5 — Allowlisted dry-run executor

**Status:** ✅ ACCEPTED 2026-10-10

Accepted outcomes:

- only the configured/approved script can run;
- current script SHA-256 is rechecked immediately before execution;
- live PDA-2 READY runtime and approved target probes are required;
- target/release choices are allowlisted/bounded and never interpreted as arbitrary shell text;
- one execution lease prevents overlap;
- the rejected stdin-prefeed prototype is permanently retired;
- fixed internal read adapter structurally forces dry mode and cannot emit live authorization;
- any live marker fails closed;
- success requires both `DRY RUN — NOTHING WILL BE MODIFIED` and `DRY RUN COMPLETE`;
- Dry run / Live deploy / ZDeployPet console channels are separated;
- PDA-4 reporter truth refreshes after completion and remains authoritative;
- real Both targets / PATCH Millenova dry-run smoke passed with exit `0` and reporter `mode=dry`;
- final Release solution regression run was reported green.

Full evidence: [`docs/PDA5_DRY_RUN_EXECUTOR.md`](docs/PDA5_DRY_RUN_EXECUTOR.md).

---

# 12. PDA-6 — Live executor and exact confirmation

**Status:** 🟡 ACTIVE

Required outcomes:

- immutable pre-deployment review of project/profile/target/destination/release/script fingerprint;
- fresh PDA-2 READY revalidation immediately before launch;
- reuse the approved-script fingerprint gate and single-execution lease from PDA-5;
- exact human confirmation phrase displayed to the operator and typed manually;
- app never auto-types, auto-fills, pastes, derives into the field or supplies the live confirmation phrase itself;
- mismatched confirmation fails closed without launching live execution;
- confirmation is passed only to the fixed approved project script in its expected bounded form;
- natural project-script output streams to the **Live deploy** console channel only;
- no auto-retry of partial live deployments;
- session expiry/lock/cancellation during pre-launch review fails closed;
- structured reporter result closes the run and remains authoritative over exit code/UI state.

Acceptance gate:

- immutable review accurately reflects the exact run about to execute;
- changed script fingerprint or changed target/release invalidates prior review/confirmation;
- LOCKED/expired/wrong-key deployment access blocks launch;
- exact confirmation is human-entered and required immediately before live execution;
- no code path can auto-supply the phrase;
- only approved/allowlisted script inputs reach execution;
- concurrent dry/live execution is blocked by the same global execution lease;
- live output is isolated from dry-run and app logs;
- failures/cancellation are never automatically retried;
- PDA-4 reporter evidence refreshes and remains authoritative;
- automated negative tests pass;
- one explicit real Millenova live smoke is accepted by the operator after all prior gates are green.

---

# 13. PDA-7 — Sanitized incident bundle and agent handoff

**Status:** ⬜ QUEUED

Required outcomes:

- bounded incident bundles from selected logs/reports/state;
- redaction/denial of credentials, secrets, keys, agent sockets and unsafe paths;
- preview before export;
- local coding-agent handoff or explicit export without deployment authority;
- returned patches remain untrusted until human diff review, tests and a new deployment authorization.

---

# 14. PDA-8 — Hardening, packaging and public launch

**Status:** ⬜ QUEUED

Required outcomes:

- clean-clone build/test CI;
- release secret scan and artifact audit;
- versioned portable package;
- Windows installer with install/uninstall lifecycle;
- Start Menu entry and optional desktop shortcut;
- packaged executable/window/taskbar/installer/shortcut icon parity;
- About/build/theme/Console/security parity with the validated local build;
- upgrade/reinstall behavior preserving intended user state;
- final license/public policy/repository hygiene and release notes;
- release package contains no secrets/private infrastructure/private keys.

---

# 15. Validation matrix

| Area | Automated | Operator smoke | Production/private target smoke |
| --- | --- | --- | --- |
| PDA-0 discovery | ✅ | ✅ 2026-10-08 | N/A |
| PDA-0A profile onboarding | ✅ | ✅ 2026-10-09 | N/A |
| PDA-1 key/host/Git safety | ✅ | ✅ 2026-10-09 | ✅ non-writing authenticated target probes |
| PDA-2 bounded agent | ✅ parsing/state/fingerprint/expiry/lock coverage | ✅ 2026-10-10 | ✅ repeated target probes during one lease |
| PDA-3 shell/pet/theme/menu/log UI | ✅ where practical | ✅ 2026-10-10 | N/A |
| PDA-4 report monitor/history | ✅ parser/path/history-selection tests; Release run green | ✅ 2026-10-10 | ✅ real latest/history/artifact smoke |
| PDA-5 dry-run | ✅ guardrail + prompt-protocol tests; final Release run green | ✅ 2026-10-10 | ✅ real Both targets / PATCH dry run, reporter dry PASS |
| PDA-6 live execution | Required | Required | Required explicit live smoke |
| PDA-7 incident bundle | Required | Required | Required sanitized handoff smoke |
| PDA-8 packaging | CI/release automation | Required installed + portable smoke | Security parity with validated workflows |

---

# 16. Current handoff snapshot

**Repository:** `wilderruiz/zdeploypet`  
**Branch:** `main`  
**Reviewed baseline before this plan update:** `2787b235fda3b804a7858a26361de14d478b4f47`  
**Known-good local publish workflow:** `scripts/publish-local.ps1`  
**Completed:** PDA-0, PDA-0A, PDA-1, PDA-2, PDA-3, PDA-4, PDA-5  
**Active:** PDA-6

Recent accepted evidence:

- bounded deployment access READY/LOCK/close behavior passed;
- production shell/theme/menu/About/ZPet/accessibility behavior passed;
- authoritative latest/history report monitoring passed against real Millenova reports;
- separated Dry run / Live deploy / ZDeployPet console tabs are in place;
- the unsafe PDA-5 stdin-prefeed prototype was identified by reporter truth, rejected and replaced;
- accepted PDA-5 implementation safely forced dry mode, completed Hostinger/VPS previews, generated `v4.4.25_dry_both` reporter artifacts and exited `0`;
- final PDA-5 Release solution regression run was reported successful.

Next concrete implementation target — PDA-6:

1. Core immutable live request/review contract;
2. review invalidation when script/target/release/session evidence changes;
3. exact manually typed confirmation contract with no app auto-fill path;
4. narrow live WSL adapter reusing approved-script/runtime/lease controls;
5. Live deploy console streaming;
6. reporter-authoritative completion with no auto-retry;
7. automated negative tests before any real live smoke.

---

# 17. Restart instructions

When resuming work:

1. read this file first;
2. confirm repository `main` and current checkpoint;
3. inspect the active phase only after reading its acceptance gate;
4. preserve all security invariants before expanding UI behavior;
5. keep real infrastructure data and all private-key material outside the public repository;
6. treat `.gitignore` as defense in depth, not secret storage;
7. for execution phases, never treat persisted session metadata alone as authorization—live ownership/liveness/fingerprint validation remains mandatory;
8. reporter truth remains authoritative over process exit/UI state;
9. never auto-supply the live confirmation phrase;
10. update this plan in the same change that materially changes phase status or acceptance evidence.
