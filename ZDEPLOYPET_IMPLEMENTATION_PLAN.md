# ZDeployPet — Master Implementation Plan

> [!IMPORTANT]
> This file is the authoritative programme index and handoff record for ZDeployPet. Update the compact overview, current handoff, affected phase, validation matrix, and next action together whenever the programme changes. Do not reconstruct status from memory when this file and repository evidence are available.

**Status:** 🟡 **PDA-8 active — hardening, packaging and public launch**  
**Reviewed repository baseline:** `main@812ec327` (`2026-10-11`)  
**Completed phase:** `PDA-7 — Sanitized incident bundle and agent handoff`  
**Active phase:** `PDA-8 — Hardening, packaging and public launch`  
**Remaining phases:** 1 including active PDA-8  
**Primary outcome:** preserve all accepted deployment/security invariants while producing clean-clone CI, audited portable/installer releases and a public-ready repository  
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
| **PDA-7** | ✅ ACCEPTED 2026-10-11 | Sanitized incident bundle, guarded local export and no-authority coding-agent handoff | [`docs/PDA7_INCIDENT_BUNDLE.md`](docs/PDA7_INCIDENT_BUNDLE.md) |
| **PDA-8** | 🟡 ACTIVE | Security hardening, clean-clone CI, installer/portable release, icon/shortcut verification and public launch | This plan §14 |

### Immediate next action — PDA-8

Start PDA-8 with release engineering and reproducibility before installer polish:

1. define the clean-clone Release build/test/publish contract and make it run in CI without private local profiles, keys, WSL secrets or Millenova-specific state;
2. add release secret scanning and artifact auditing for repository + produced packages;
3. define version metadata and a deterministic portable-release layout;
4. verify the portable package on a clean Windows/user-state boundary before introducing an installer;
5. add the Windows installer with explicit install/uninstall/upgrade lifecycle, Start Menu entry and optional desktop shortcut;
6. verify packaged executable/window/taskbar/installer/shortcut icon parity and preserve validated About/theme/Console/security behavior;
7. verify upgrade/reinstall preserves only intended user state and never persists live authorization/session state;
8. finalize license/public policy/repository hygiene/release notes;
9. perform installed + portable security-parity smokes and audit final release artifacts for secrets/private infrastructure/private keys before acceptance.

### Recent acceptance evidence

PDA-5 accepted after a real Both targets / PATCH dry run from the ZDeployPet UI remained structurally dry, completed Hostinger/VPS previews, restored local version state, generated authoritative dry reporter artifacts and exited `0`. The earlier stdin-prefeed prototype that accidentally entered live mode remains permanently rejected negative safety evidence.

PDA-6 accepted after Release tests/build, immutable review/arming smoke and one explicit operator-controlled real Both targets / PATCH live deployment. The operator manually typed the exact confirmation phrase, the script completed VPS activation + Hostinger sync, no retry occurred, and PDA-4 reporter truth recorded live PASS.

PDA-7 accepted 2026-10-11 after deterministic redaction/bounds, canonical trusted-source collection, exact preview, `Copy all`, guarded local `.txt` export and a no-authority coding-agent manifest were implemented and smoke-tested against real Millenova reporter/console evidence. The final post-hardening Release publish passed with 0 warnings / 0 errors; published EXE SHA-256 was `5B989530A81DEB80FCFD378E4B80E2DB4532D570AEB1029F8D95F06A9ED01C83`. The final authority review confirmed bundles carry diagnostic text only and cannot satisfy PDA-2/PDA-5/PDA-6 deployment authorization gates.

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
| [`docs/PDA7_INCIDENT_BUNDLE.md`](docs/PDA7_INCIDENT_BUNDLE.md) | Accepted sanitized incident-bundle/export/agent-handoff contract and smoke evidence | PDA-7 evidence |
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
local debugging evidence without deployment authority
        ↓
RELEASE ENGINEERING
clean-clone CI + audited portable/installer packages
```

The project-owned deployment script remains authoritative for build/test/source selection/transfer/activation/application health/report generation. ZDeployPet validates and invokes the already-approved script; it does not rewrite project deployment logic.

Private profile/session material stays under the per-user local application boundary. Private keys stay under the selected WSL user's `~/.ssh`. Real hosts/users/ports/personal paths must not become public defaults or release artifacts.

---

# 3. Security invariants

The implementation must not:

- store remote account passwords, private-key passphrases, tokens or private-key bytes in normal profile/application JSON;
- collect a private-key passphrase in an app-owned field;
- copy private keys into source repositories, release packages or remote targets;
- export an SSH-agent socket or live session capability;
- use `StrictHostKeyChecking=no` or silently accept changed host keys;
- expose a production HTTP deployment endpoint or generic arbitrary-command textbox;
- silently broaden targets/destinations;
- rewrite the project deployment script to fit the application;
- auto-type, auto-fill or synthesize a live confirmation phrase;
- auto-retry a partial live deployment;
- treat process exit code/UI state as stronger than structured reporter truth;
- make an agent-generated patch trusted without human review/tests/fresh deployment authorization;
- commit real infrastructure data, reports, incident bundles or credentials to the public repository;
- package local profiles, local settings, private infrastructure identifiers, deployment reports, incident bundles, key material or transient authorization/session state.

Required controls include exact host-key/fingerprint validation, bounded session lifetime, lock-on-close, fixed script/action/input allowlists, script-fingerprint verification, single execution lock, canonical report/artifact path containment, bounded/redacted evidence, explicit human authorization for live deployment, and release-time secret/artifact auditing.

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

Accepted outcomes include an app-owned dedicated SSH-agent session in selected WSL; trusted-terminal unlock without app-owned passphrase capture; exact approved-fingerprint validation before READY; bounded lease/expiry; explicit Lock and lock-on-close; no restart authorization from stale metadata alone; repeated probes during one valid lease; and no exported agent socket/session authorization.

---

# 9. PDA-3 — Production shell, companion and operator visibility

**Status:** ✅ ACCEPTED 2026-10-10

Accepted outcomes include single-instance shell, dark/red identity, menus/About, separated sanitized Console / Activity, responsive layout memory, ZPet state companion, runtime vector identity, focus cues and keyboard accessibility. Packaged executable/installer/desktop-shortcut icon parity is owned by PDA-8.

---

# 10. PDA-4 — Structured deployment-report monitor

**Status:** ✅ ACCEPTED 2026-10-10

Accepted outcomes include authoritative schema-v1 latest/history reporting, canonical path containment, bounded read-only Summary / Full log viewers, explicit UTF-8, newest-first deduplicated history, fail-closed malformed/untrusted handling and lightweight visible-only polling.

Full evidence: [`docs/PDA4_REPORT_MONITOR.md`](docs/PDA4_REPORT_MONITOR.md).

---

# 11. PDA-5 — Allowlisted dry-run executor

**Status:** ✅ ACCEPTED 2026-10-10

Accepted outcomes include configured/approved-script-only execution, immediate SHA-256 recheck, live PDA-2 READY/target-probe prerequisite, bounded target/release inputs, one execution lease, structurally forced dry mode, live-marker fail-closed behavior, dedicated Dry run console and PDA-4 reporter-authoritative closeout. Real Both targets / PATCH dry-run smoke passed.

Full evidence: [`docs/PDA5_DRY_RUN_EXECUTOR.md`](docs/PDA5_DRY_RUN_EXECUTOR.md).

---

# 12. PDA-6 — Live executor and exact confirmation

**Status:** ✅ ACCEPTED 2026-10-11

Accepted outcomes include immutable review, fresh READY/script/session revalidation, one execution lease, exact manually typed human confirmation, disarming on mutation, no auto-fill/synthesis, prompt-scoped single confirmation delivery, no auto-retry, live-protocol fail-closed checks and PDA-4 reporter-authoritative closeout. Real Both targets / PATCH live deployment smoke passed.

Full evidence: [`docs/PDA6_LIVE_EXECUTOR.md`](docs/PDA6_LIVE_EXECUTOR.md).

---

# 13. PDA-7 — Sanitized incident bundle and agent handoff

**Status:** ✅ ACCEPTED 2026-10-11

Accepted outcomes:

- explicit allowlisted evidence kinds with undefined-kind rejection;
- hard item/label/input/sanitized/bundle bounds;
- deterministic redaction of private-key blocks, common secrets/tokens, SSH-agent sockets and Unix/Windows private-key paths;
- trusted PDA-4 artifact collection with canonical containment and realpath/type/size-bounded reads;
- bounded Dry run / Live deploy / ZDeployPet console collection;
- exact operator preview with included/redacted/truncated/omitted evidence;
- `Copy all` from successfully built sanitized content only;
- explicit guarded local `.txt` export with relative/UNC/ADS/project-path/non-txt denial;
- deterministic coding-agent manifest with `Authority: NONE` and no credential/session/remote-write capability;
- selection mutation invalidates Copy/Export until rebuild;
- returned agent patches remain untrusted until human review/tests/fresh authorization;
- real preview, local export and manifest UI smokes passed;
- final post-hardening Release publish passed with 0 warnings / 0 errors;
- final authority review confirmed an exported bundle cannot satisfy or bypass PDA-2/PDA-5/PDA-6 authorization.

Final published EXE SHA-256 at acceptance: `5B989530A81DEB80FCFD378E4B80E2DB4532D570AEB1029F8D95F06A9ED01C83`.

Full evidence: [`docs/PDA7_INCIDENT_BUNDLE.md`](docs/PDA7_INCIDENT_BUNDLE.md).

---

# 14. PDA-8 — Hardening, packaging and public launch

**Status:** 🟡 ACTIVE

Required outcomes:

- clean-clone build/test CI;
- release secret scan and artifact audit;
- versioned portable package;
- Windows installer with install/uninstall lifecycle;
- Start Menu entry and optional desktop shortcut;
- packaged executable/window/taskbar/installer/shortcut icon parity;
- About/build/theme/Console/security parity with the validated local build;
- upgrade/reinstall behavior preserving intended user state only;
- final license/public policy/repository hygiene and release notes;
- release package contains no secrets/private infrastructure/private keys/session state.

Acceptance gate:

- a clean clone builds/tests/publishes without private machine state;
- CI and release scanning fail closed on prohibited secret/private artifact classes;
- portable and installed packages launch and preserve the accepted security behavior from PDA-0 through PDA-7;
- uninstall/reinstall/upgrade behavior is explicitly tested and does not preserve live deployment authorization;
- package/installer/shortcut identity is consistent;
- final distributable contents are audited and contain no private profile/settings/report/bundle/key/session material;
- README/license/release notes/public policy accurately describe the product and security boundary;
- one portable and one installed smoke pass on the release candidate before PDA-8 acceptance.

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
| PDA-7 incident bundle | ✅ sanitizer/bounds/manifest/export negative coverage; final Release publish green | ✅ preview/Copy/export/manifest 2026-10-11 | ✅ real reporter + console sanitized handoff smoke |
| PDA-8 packaging | 🟡 clean-clone CI/release automation required | Required installed + portable smoke | Security parity with validated workflows |

---

# 16. Current handoff snapshot

**Repository:** `wilderruiz/zdeploypet`  
**Branch:** `main`  
**Reviewed baseline before this plan update:** `812ec32705d2a5a15e7c39e4ab3e0c2b578574ae`  
**Known-good local publish workflow:** `scripts/publish-local.ps1`  
**Completed:** PDA-0, PDA-0A, PDA-1, PDA-2, PDA-3, PDA-4, PDA-5, PDA-6, PDA-7  
**Active:** PDA-8

Latest acceptance evidence:

- PDA-7 real sanitized reporter/console preview succeeded;
- real operator-selected `.txt` export succeeded;
- coding-agent manifest smoke visibly confirmed no authority/credentials/session/remote-write capability;
- unsupported evidence-kind and Windows private-key-path hardening added;
- final Release build/publish after hardening: 0 warnings, 0 errors;
- accepted published EXE SHA-256: `5B989530A81DEB80FCFD378E4B80E2DB4532D570AEB1029F8D95F06A9ED01C83`;
- final authority-content review passed: exported evidence does not carry the runtime state or human authorization required to deploy.

Next concrete implementation target — PDA-8:

1. clean-clone CI contract and workflow;
2. repository/package secret scan + artifact audit;
3. deterministic versioned portable package;
4. clean-state portable smoke;
5. installer/uninstaller + Start Menu/optional desktop shortcut;
6. icon and production-shell parity in packaged surfaces;
7. upgrade/reinstall/user-state tests;
8. license/public-policy/repository hygiene/release notes;
9. final portable + installed release-candidate security smokes.

---

# 17. Restart instructions

When resuming work:

1. read this file first;
2. confirm repository `main` and current checkpoint;
3. inspect the active phase only after reading its acceptance gate;
4. preserve all security invariants before expanding packaging/release behavior;
5. keep real infrastructure data and all private-key material outside the public repository and release artifacts;
6. treat `.gitignore` as defense in depth, not secret storage;
7. never treat persisted session metadata alone as authorization—live ownership/liveness/fingerprint validation remains mandatory;
8. reporter truth remains authoritative over process exit/UI state;
9. never auto-supply the live confirmation phrase;
10. never package local profile/settings/report/bundle/key/session material;
11. update this plan in the same change that materially changes phase status or acceptance evidence.
