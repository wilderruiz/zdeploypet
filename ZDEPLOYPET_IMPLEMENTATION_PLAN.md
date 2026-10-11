# ZDeployPet — Master Implementation Plan

> [!IMPORTANT]
> This file is the authoritative programme index and handoff record for ZDeployPet. Update the compact overview, current handoff, affected phase, validation matrix, and next action together whenever the programme changes. Do not reconstruct status from memory when this file and repository evidence are available.

**Status:** 🟡 **PDA-7 active — sanitized incident bundle and agent handoff**  
**Reviewed repository baseline:** `main@41e38e85` (`2026-10-11`)  
**Completed phase:** `PDA-6 — Live executor and exact confirmation`  
**Active phase:** `PDA-7 — Sanitized incident bundle and agent handoff`  
**Next phase after acceptance:** `PDA-8 — Hardening, packaging and public launch`  
**Remaining phases:** 2 including active PDA-7; 1 after PDA-7 acceptance  
**Primary outcome:** bounded deployment access, authoritative report monitoring, safe dry/live execution, then sanitized debugging evidence and public-release hardening  
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
| **PDA-6** | ✅ ACCEPTED 2026-10-11 | Live executor with immutable review, exact human confirmation, isolated live console and reporter-authoritative closeout | [`docs/PDA6_LIVE_EXECUTOR.md`](docs/PDA6_LIVE_EXECUTOR.md) |
| **PDA-7** | 🟡 ACTIVE | Sanitized incident bundle and local/remote-agent handoff | This plan §13 |
| **PDA-8** | ⬜ QUEUED | Security hardening, clean-clone CI, installer/portable release, desktop shortcut verification and public launch | This plan §14 |

### Immediate next action — PDA-7

Implement PDA-7 in this order:

1. define a bounded incident-evidence contract in Core before adding export UI;
2. allow only explicit evidence classes: selected structured report metadata, bounded summary/full-log excerpts, selected sanitized Console / Activity rows, profile display metadata that is already non-secret, and app/build/runtime diagnostics;
3. deny private-key bytes/paths beyond safe public identity, passphrases, passwords, tokens, SSH-agent sockets, raw environment dumps, unbounded filesystem paths and arbitrary recursive file collection;
4. apply deterministic redaction before preview/export and make redaction tests authoritative;
5. add an operator preview showing exactly what will be included and exactly what was redacted/omitted;
6. export a local bounded bundle only after explicit operator action; no bundle may grant deployment authority or carry a live session capability;
7. define an agent-handoff manifest that explains repository/profile/report context without exposing secrets or requiring the agent to own deployment access;
8. any returned patch remains untrusted until human diff review, tests, fresh script approval/fingerprint validation if applicable, and a fresh deployment authorization;
9. add automated negative tests for secret labels, private-key material, token-shaped values, agent sockets, path escapes, oversized evidence and unsupported evidence kinds;
10. complete one sanitized real-report/console bundle smoke before PDA-7 acceptance.

### Recent acceptance evidence

PDA-2 accepted after operator smoke confirmed one unlock establishes READY with the approved key, repeated Hostinger/VPS probes work in the bounded session, explicit Lock returns to LOCKED, app close/reopen remains LOCKED, and no app-owned passphrase field is used.

PDA-3 accepted after operator smoke confirmed single-instance activation, dark/red production shell, menus/About, separated/sanitized Console / Activity, responsive layout memory, non-authoritative ZPet companion states, runtime window/taskbar identity and keyboard accessibility. Packaged executable/installer/shortcut icon parity remains intentionally deferred to PDA-8.

PDA-4 accepted after real Millenova reporter smoke confirmed the latest PASS card, Summary / Full log viewers, newest-first 30-row history, row-specific artifacts, UTF-8 decoding, fail-closed report/path validation, passing Release tests and lightweight visible-only polling.

PDA-5 accepted 2026-10-10 after the final implementation safely completed a real **Both targets / PATCH** Millenova dry run from the ZDeployPet UI: approved script fingerprint matched, access was ON / READY, the script printed `DRY RUN — NOTHING WILL BE MODIFIED`, Hostinger/VPS delta previews completed, `DRY RUN COMPLETE` appeared, local `version.txt` was restored, reporter artifacts identified `v4.4.25_dry_both`, exit code was `0`, no live authorization phrase appeared, and the final Release solution regression run was reported green. The earlier stdin-prefeed prototype that accidentally entered live mode is permanently rejected and documented as negative safety evidence.

PDA-6 accepted 2026-10-11 after Release tests/build, non-deploying arming smoke and one explicit operator-controlled real **Both targets / PATCH** live deployment from ZDeployPet. The immutable review froze target/release/script SHA and required the operator to type the exact confirmation phrase manually; wrong text failed, correct text armed launch, and editing after validation disarmed it. The real run entered `LIVE MILLENOVA DEPLOYMENT`, accepted the operator-entered phrase only at the exact prompt, passed VPS sync/activation/75-of-75 backend tests/health plus Hostinger site/config sync, printed `MILLENOVA DEPLOYMENT COMPLETE`, exited `0`, attempted no retry, and reporter truth recorded `v4.4.25`, `mode=live`, `result=pass`, duration 56s.

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
| [`docs/PDA6_LIVE_EXECUTOR.md`](docs/PDA6_LIVE_EXECUTOR.md) | Accepted live review/confirmation/execution contract and production smoke evidence | PDA-6 evidence |
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

**Status:** ✅ ACCEPTED 2026-10-11

Accepted outcomes:

- immutable pre-deployment review of project/profile/target/destination/release/script fingerprint;
- fresh PDA-2 READY revalidation immediately before launch;
- approved-script fingerprint gate and one global dry/live execution lease;
- exact human confirmation phrase displayed and manually typed by the operator;
- wrong text fails closed; editing after successful validation disarms live launch;
- app never auto-types, auto-fills, pastes, derives or synthesizes the live confirmation phrase;
- confirmation reaches only the approved script's exact live-confirmation prompt and at most once;
- target/release/mode inputs are bounded through the narrow live WSL adapter;
- natural script output streams only to the **Live deploy** console channel;
- no auto-retry of partial/failed/cancelled live deployments;
- session/script/target/release/phrase/concurrency state is revalidated immediately before launch;
- live protocol fails closed on dry markers, missing live proof, missing confirmation prompt or incomplete successful exit;
- PDA-4 reporter truth refreshes after completion and remains authoritative over exit code/UI state;
- Core/executor negative tests and Release build/tests passed;
- explicit operator-controlled production smoke completed successfully.

Production acceptance smoke:

- immutable review: **Both targets / PATCH**, release preview `v4.4.24 -> v4.4.25`;
- operator manually typed the exact configured phrase; wrong phrase was rejected and field mutation disarmed launch;
- live script entered `LIVE MILLENOVA DEPLOYMENT` and accepted the operator-entered phrase only at the exact prompt;
- VPS sync passed with zero changed files;
- VPS production activation passed with 0 npm vulnerabilities, 75/75 backend tests, restart on `:9400`, and healthy media-auth runtime;
- Hostinger site sync passed with the 7-byte version update; Hostinger config had zero changes;
- `MILLENOVA DEPLOYMENT COMPLETE` appeared;
- process exited `0`; no retry was attempted;
- reporter recorded deployment id `20261011-050132_v4.4.25_live_both`, `previous_release=4.4.24`, `release=4.4.25`, `mode=live`, `result=pass`, `exit_status=0`, duration 56s.

Full evidence: [`docs/PDA6_LIVE_EXECUTOR.md`](docs/PDA6_LIVE_EXECUTOR.md).

---

# 13. PDA-7 — Sanitized incident bundle and agent handoff

**Status:** 🟡 ACTIVE

Required outcomes:

- bounded incident bundles from selected logs/reports/state;
- deterministic redaction/denial of credentials, secrets, private keys, passphrases, tokens, SSH-agent sockets and unsafe paths;
- explicit allowlist of evidence kinds and byte/line/count ceilings;
- preview before export showing included, redacted and omitted evidence;
- local bundle export only after explicit operator action;
- local coding-agent handoff or explicit export without deployment authority;
- no live agent/session capability, credential material or remote-write authority in a bundle;
- returned patches remain untrusted until human diff review, tests and a new deployment authorization.

Acceptance gate:

- secret/private-key/token/agent-socket fixtures are redacted or rejected deterministically;
- unsupported evidence classes and path escapes fail closed;
- bundle size/content is bounded and deterministic;
- preview matches exported content;
- real reporter/console evidence can be exported without secret leakage;
- agent handoff contains enough sanitized context to debug while carrying no deployment authority;
- automated tests pass;
- one operator-controlled sanitized real-evidence smoke passes.

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
| PDA-6 live execution | ✅ review/confirmation/live-protocol safety tests; Release build/tests green | ✅ review/arming smoke 2026-10-11 | ✅ real Both targets / PATCH live run, reporter live PASS 2026-10-11 |
| PDA-7 incident bundle | Required | Required | Required sanitized handoff smoke |
| PDA-8 packaging | CI/release automation | Required installed + portable smoke | Security parity with validated workflows |

---

# 16. Current handoff snapshot

**Repository:** `wilderruiz/zdeploypet`  
**Branch:** `main`  
**Reviewed baseline before this plan update:** `41e38e85fa8463bd179735fca5df1030ceddd1c9`  
**Known-good local publish workflow:** `scripts/publish-local.ps1`  
**Completed:** PDA-0, PDA-0A, PDA-1, PDA-2, PDA-3, PDA-4, PDA-5, PDA-6  
**Active:** PDA-7

Recent accepted evidence:

- bounded deployment access READY/LOCK/close behavior passed;
- production shell/theme/menu/About/ZPet/accessibility behavior passed;
- authoritative latest/history report monitoring passed against real Millenova reports;
- separated Dry run / Live deploy / ZDeployPet console tabs are in place;
- rejected PDA-5 stdin-prefeed prototype remains documented negative safety evidence;
- accepted PDA-5 implementation safely forced dry mode, completed Hostinger/VPS previews, generated `v4.4.25_dry_both` reporter artifacts and exited `0`;
- PDA-6 immutable review and exact human-confirmation arming/disarming behavior passed;
- accepted PDA-6 real live run completed Both targets / PATCH, passed VPS activation + Hostinger sync, printed deployment complete, exited `0`, attempted no retry, and reporter recorded `v4.4.25_live_both` / `mode=live` / PASS.

Next concrete implementation target — PDA-7:

1. Core evidence-kind/size/redaction contract;
2. deterministic secret/key/token/socket/path redaction and denial rules;
3. selected report + console + runtime evidence collector with hard bounds;
4. operator preview before export;
5. sanitized local bundle format and manifest;
6. agent-handoff metadata with no deployment authority;
7. automated negative tests;
8. real reporter/console sanitized bundle smoke.

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