# ZDeployPet — Master Implementation Plan

> [!IMPORTANT]
> This file is the authoritative programme index and handoff record for ZDeployPet. Update the compact overview, current handoff, affected phase, validation matrix, and next action together whenever the programme changes. Do not reconstruct status from memory when this file and repository evidence are available.

**Status:** 🟡 **PDA-5 active — allowlisted dry-run executor next**  
**Reviewed repository baseline:** `main@55614d64` (`2026-10-10`)  
**Completed phase:** `PDA-4 — Structured deployment-report monitor`  
**Active phase:** `PDA-5 — Allowlisted dry-run executor`  
**Next phase after acceptance:** `PDA-6 — Live executor and exact confirmation`  
**Remaining phases:** 4 including active PDA-5; 3 after PDA-5 acceptance  
**Primary outcome:** one bounded deployment-access session, read-only authoritative report monitoring, and safe UI-driven execution of the unchanged project deployment script  
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
| **PDA-2** | ✅ ACCEPTED 2026-10-10 | App-owned bounded SSH-agent session: unlock once, READY/LOCKED state, expiry and lock-on-close | This plan §8 |
| **PDA-3** | ✅ ACCEPTED 2026-10-10 | Single-instance production shell, accessible state model, ZPet companion, dark/red identity, menu/About and Console / Activity | [`docs/PDA3_PRODUCTION_SHELL.md`](docs/PDA3_PRODUCTION_SHELL.md) |
| **PDA-4** | ✅ ACCEPTED 2026-10-10 | Read-only authoritative latest + historical deployment-report monitor with bounded artifact viewers and lightweight polling | [`docs/PDA4_REPORT_MONITOR.md`](docs/PDA4_REPORT_MONITOR.md) |
| **PDA-5** | 🟡 ACTIVE | Allowlisted dry-run executor for the unchanged project script | This plan §11 |
| **PDA-6** | ⬜ QUEUED | Live executor with immutable review and exact human confirmation | This plan §12 |
| **PDA-7** | ⬜ QUEUED | Sanitized incident bundle and local/remote-agent handoff | This plan §13 |
| **PDA-8** | ⬜ QUEUED | Security hardening, clean-clone CI, installer/portable release, desktop shortcut verification and public launch | This plan §14 |

### Immediate next action

Begin PDA-5 in this order:

1. define immutable dry-run request/state contracts in Core rather than driving execution directly from WPF;
2. add a narrow WSL execution service that invokes only the configured profile script and cannot accept arbitrary command text;
3. require the current script fingerprint to match the approved profile/discovery fingerprint before execution;
4. expose only known dry-run inputs such as target/release/mode through typed/allowlisted values;
5. require the PDA-2 deployment-access session to be READY and revalidate session ownership/liveness/key fingerprint before starting;
6. enforce a single in-process deployment execution lock;
7. stream sanitized lifecycle output to Console / Activity without leaking credentials or shell secrets;
8. after completion, refresh PDA-4 reporter truth and treat the structured report—not process exit code—as the authoritative run result;
9. add automated negative tests for changed script fingerprint, non-READY access, invalid target/mode/release values, concurrent execution and arbitrary-command injection attempts;
10. complete a real Millenova dry-run smoke from the ZDeployPet UI with no manual terminal invocation.

### Recent acceptance evidence

PDA-2 accepted 2026-10-10 after operator smoke confirmed one unlock establishes READY with the approved key, repeated Hostinger/VPS probes work in the bounded session, explicit Lock returns to LOCKED, app close/reopen remains LOCKED, and no app-owned passphrase field is used.

PDA-3 accepted 2026-10-10 after operator smoke confirmed single-instance activation, dark/red production shell, menus/About, sanitized Console / Activity, responsive layout memory, reusable non-authoritative ZPet companion states, runtime window/taskbar identity, keyboard focus cues and accessible navigation. Packaged executable/installer/desktop-shortcut binary icon work remains intentionally deferred to PDA-8.

PDA-4 accepted 2026-10-10 after real Millenova reporter smoke confirmed the latest PASS card, validated Summary / Full log viewers, a newest-first 30-row historical table with row-specific artifact actions, explicit UTF-8 decoding, fail-closed path/report validation, passing Release solution tests, and lightweight 15-second visible-only polling that no longer repeatedly rescans history or spams unchanged activity events.

---

# 1. Documentation map and precedence

| Document | Role | Authority |
| --- | --- | --- |
| [`ZDEPLOYPET_IMPLEMENTATION_PLAN.md`](ZDEPLOYPET_IMPLEMENTATION_PLAN.md) | Master roadmap, programme status, handoff and sequencing | Programme authority |
| [`README.md`](README.md) | Public product introduction and daily build/test/publish commands | User/developer entry point |
| [`docs/PDA0_DISCOVERY.md`](docs/PDA0_DISCOVERY.md) | Accepted machine-capability baseline | PDA-0 evidence |
| [`docs/PDA1_KEY_ONBOARDING.md`](docs/PDA1_KEY_ONBOARDING.md) | Accepted deployment-key/host-trust/probe workflow | PDA-1 evidence |
| [`docs/PDA3_PRODUCTION_SHELL.md`](docs/PDA3_PRODUCTION_SHELL.md) | Accepted production shell scope/evidence | PDA-3 evidence |
| [`docs/PDA4_REPORT_MONITOR.md`](docs/PDA4_REPORT_MONITOR.md) | Accepted report-monitor contract/evidence | PDA-4 evidence |
| [`docs/PRODUCT_IDENTITY.md`](docs/PRODUCT_IDENTITY.md) | Product boundary and identity direction | Naming/identity boundary |
| [`docs/PROFILES.md`](docs/PROFILES.md) | Shareable-contract/private-binding split and profile validation | Profile contract summary |

ZDeployPet is generic; Millenova is its first private local profile. Millenova's reporter documentation owns Millenova-specific report semantics. This master plan owns ZDeployPet sequencing and status.

Source-of-truth order when documents disagree:

1. runtime security invariants and validated code behavior;
2. this master plan for ZDeployPet sequencing/status;
3. focused ZDeployPet phase documents for narrow contracts;
4. project-owned reporter/deployment-script contracts for project-specific behavior;
5. the private local profile for real endpoints/paths;
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
allowlisted script + known inputs + explicit live confirmation later
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
- auto-type a live confirmation phrase;
- auto-retry a partial live deployment;
- treat process exit code/UI state as stronger than structured reporter truth;
- make an agent-generated patch trusted without human review/tests/fresh deployment authorization;
- commit real infrastructure data, reports, incident bundles or credentials to the public repository.

Required controls include exact host-key/fingerprint validation, bounded session lifetime, lock-on-close, fixed script/action/input allowlists, script-fingerprint verification, single execution lock, canonical report/artifact path containment, bounded evidence, redaction and explicit human authorization for live deployment.

---

# 4. Accepted foundation — PDA-0 / PDA-0A / PDA-1

## PDA-0 — Foundation/discovery

**Status:** ✅ ACCEPTED 2026-10-08

Standalone .NET 8/WPF repository, App/Core/Infrastructure/WslBridge separation, read-only Windows/WSL/OpenSSH/project/script/report discovery, script SHA-256 discovery and corrected WSL `--exec` argument forwarding were completed and smoke-tested.

## PDA-0A — Profile onboarding

**Status:** ✅ ACCEPTED 2026-10-09

Completed schema-v1 local profile storage, WSL/project/script validation, multiple SSH targets/destinations, first-run five-section UI, structured `i` guidance, input history/draft recovery, local publish loop and current public-repository scope.

## PDA-1 — Key onboarding and diagnostics

**Status:** ✅ ACCEPTED 2026-10-09

Accepted behavior includes dedicated profile-scoped Ed25519 key creation under WSL `~/.ssh`, explicit public-key fingerprint approval, independent host-key verification/enrollment, public-key-only installation through trusted SSH terminal prompts, non-writing authenticated Hostinger/VPS probes, exact enrolled host-key-type rechecks, guided onboarding state and Git/private-key safety scanning.

---

# 8. PDA-2 — Bounded SSH-agent session

**Status:** ✅ ACCEPTED 2026-10-10

Accepted outcomes:

- app-owned dedicated SSH-agent session in the selected WSL environment;
- trusted-terminal `ssh-add -t 8h` style unlock without app-owned passphrase capture;
- loaded fingerprint must exactly match the PDA-1-approved deployment key before READY;
- LOCKED / READY / EXPIRING / expiry behavior with bounded lease;
- explicit Lock and lock-on-close;
- restart never restores authorization from stale metadata alone;
- repeated Hostinger/VPS probes work during one valid session without remote account-password prompts;
- private key/socket/session authorization is never exported.

Operator smoke accepted READY, repeated probes, explicit Lock and close/reopen LOCKED behavior.

---

# 9. PDA-3 — Production shell, companion and operator visibility

**Status:** ✅ ACCEPTED 2026-10-10

Accepted outcomes:

- single-instance shell and second-launch activation;
- dark theme with restrained red identity and readable status semantics;
- Project/Profile, View, Help and About surfaces;
- sanitized Console / Activity pane with copy support and remembered layout;
- responsive split-shell layout and wrapping profile/footer surfaces;
- reusable ZPet companion reflecting LOCKED/RUNNING/READY/ERROR without deployment authority;
- runtime vector window/taskbar identity;
- stable automation names, visible keyboard focus cues and keyboard navigation that skips the decorative/non-authoritative ZPet surface;
- shared structured `i` guidance across primary actions.

Deferred to PDA-8 only: packaged executable/installer/desktop-shortcut binary icon and packaging parity verification.

---

# 10. PDA-4 — Structured deployment-report monitor

**Status:** ✅ ACCEPTED 2026-10-10

Accepted outcomes:

- reporter JSON is authoritative deployment truth;
- `latest.json` is resolved/read safely below the configured report root;
- malformed/partial/unsupported reports fail closed;
- report and declared artifact paths are canonicalized/bounded below the configured root;
- latest card shows authoritative result, release, mode, target, timestamps, duration and deployment id;
- validated Summary / Full log artifacts open in bounded read-only viewers;
- WSL text decoding is explicit UTF-8;
- historical JSON is bounded, parsed through the same schema-v1 parser, deduplicated by `deployment_id`, newest-first and capped at 30 trusted rows;
- history rows expose Started / Release / Mode / Target / Result / Duration plus row-specific Summary / Full log actions;
- malformed/untrusted historical candidates are skipped/counted, not rendered as trusted rows;
- automatic polling runs every 15 seconds only while the report surface is visible, checks only `latest.json` when unchanged, suppresses repeated identical activity events, and refreshes history only when a genuinely new deployment id appears;
- monitoring performs no deployment-side write.

Acceptance evidence and security details are maintained in [`docs/PDA4_REPORT_MONITOR.md`](docs/PDA4_REPORT_MONITOR.md).

---

# 11. PDA-5 — Allowlisted dry-run executor

**Status:** 🟡 ACTIVE

Required outcomes:

- execute only the configured/approved deployment script;
- normal operators start the dry run from ZDeployPet; manual terminal invocation is not the normal product workflow;
- expose only known target/mode/release inputs and required fixed actions;
- validate the configured script fingerprint immediately before execution;
- require a live validated PDA-2 bounded deployment-access session;
- prevent concurrent deployment execution;
- never expose a generic arbitrary-command field or allow shell-command injection through target/release/mode inputs;
- surface sanitized lifecycle diagnostics in Console / Activity;
- after process completion, refresh PDA-4 reporter truth and treat the structured report as authoritative over process exit code;
- dry run must perform no live activation beyond what the authoritative project script defines as dry-run behavior.

Acceptance gate:

- only the approved script and allowlisted argument/action combinations can run;
- changed script fingerprint blocks execution before the script starts;
- LOCKED/expired/wrong-key session blocks execution;
- concurrent second execution is blocked;
- invalid/non-allowlisted inputs cannot become arbitrary shell syntax;
- dry-run lifecycle appears in sanitized Console / Activity;
- authoritative reporter evidence is displayed after completion;
- automated tests cover the execution guardrails;
- published-development Millenova dry-run smoke starts from ZDeployPet without manual terminal script invocation.

---

# 12. PDA-6 — Live executor and exact confirmation

**Status:** ⬜ QUEUED

Required outcomes:

- immutable pre-deployment review of project/profile/target/destination/release/script fingerprint;
- exact human confirmation phrase displayed and entered by operator;
- app never auto-types or supplies the live confirmation phrase;
- confirmation is passed only to the fixed project script in its expected bounded form;
- normal live deployment starts from ZDeployPet;
- no auto-retry of partial live deployments;
- single-instance execution lock spans the full live run;
- authoritative structured reporter result closes the run.

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
- upgrade/reinstall behavior that preserves intended local user state;
- final license/public policy/repository hygiene and release notes;
- release package contains no secrets/private infrastructure/private keys.

---

# 15. Validation matrix

| Area | Automated | Operator smoke | Production/private target smoke |
| --- | --- | --- | --- |
| PDA-0 discovery | ✅ | ✅ 2026-10-08 | N/A |
| PDA-0A profile onboarding | ✅ | ✅ 2026-10-09 | N/A |
| PDA-1 key/host/Git safety | ✅ | ✅ 2026-10-09 | ✅ Hostinger + VPS non-writing authenticated probes |
| PDA-2 bounded agent | ✅ parsing/state/fingerprint/expiry/lock coverage | ✅ 2026-10-10 | ✅ repeated Hostinger + VPS probes during one lease |
| PDA-3 shell/pet/theme/menu/log UI | ✅ where practical | ✅ 2026-10-10 | N/A |
| PDA-4 report monitor/history | ✅ parser/path/history-selection tests; Release solution run reported pass | ✅ 2026-10-10 | ✅ real Millenova latest/history/artifact smoke |
| PDA-5 dry-run | Required | Required | Required real dry-run |
| PDA-6 live execution | Required | Required | Required explicit live smoke |
| PDA-7 incident bundle | Required | Required | Required sanitized handoff smoke |
| PDA-8 packaging | CI/release automation | Required installed + portable smoke | Security parity with validated workflows |

---

# 16. Current handoff snapshot

**Repository:** `wilderruiz/zdeploypet`  
**Branch:** `main`  
**Reviewed baseline before this plan update:** `55614d64e8297e492e04dc6717fc3312666798c0`  
**Known-good local publish workflow:** `scripts/publish-local.ps1`  
**Completed:** PDA-0, PDA-0A, PDA-1, PDA-2, PDA-3, PDA-4  
**Active:** PDA-5

Recent accepted evidence:

- bounded deployment access READY/LOCK/close behavior passed;
- production shell single-instance/theme/menu/About/Console/ZPet/accessibility behavior passed;
- real Millenova `latest.json` renders as authoritative PASS with bounded Summary / Full log viewers;
- deployment history renders 30 trusted rows newest-first with row-specific artifact buttons;
- Release solution build/tests were reported successful after deterministic history-selection coverage was added;
- initial full 15-second report rescans were corrected after operator smoke exposed slow/restarting history behavior;
- accepted polling now leaves history stable, emits no repeated unchanged latest-report activity spam, and only performs full history refresh when a new deployment id appears.

Next concrete implementation target — PDA-5:

1. add Core dry-run request/validation/execution-state contracts;
2. add narrow allowlisted WSL script execution with no arbitrary shell field;
3. verify the current script fingerprint before launch;
4. require live PDA-2 READY validation and exact approved key/session state;
5. expose bounded target/release/dry-mode selection in the shell;
6. enforce one deployment execution at a time;
7. stream sanitized output to Console / Activity;
8. refresh/read PDA-4 authoritative report after completion;
9. add negative guardrail tests;
10. perform the first real Millenova dry run from ZDeployPet.

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
9. update this plan in the same change that materially changes phase status or acceptance evidence.
