# PDA-5 — Allowlisted dry-run executor

**Status:** 🟡 ACTIVE

PDA-5 adds the first execution capability to ZDeployPet. It is deliberately narrower than a terminal: ZDeployPet may invoke only the deployment script already selected by the validated profile, with a fixed dry-run mode and bounded project-defined inputs.

## Authority boundary

The project-owned deployment script remains the deployment engine. ZDeployPet does not rewrite deployment logic, infer arbitrary commands, or treat process exit status as stronger than the project reporter.

For PDA-5:

- execution mode is fixed to **dry run**;
- the configured profile script is the only runnable script;
- target values must come from an explicit allowlist;
- release is a bounded token, never shell text;
- the current script SHA-256 must exactly match the approved fingerprint immediately before launch;
- PDA-2 deployment access must be live and READY after ownership/liveness/fingerprint revalidation;
- only one deployment execution may run at a time;
- stdout/stderr may be surfaced only through sanitized application activity/logging;
- after process completion, PDA-4 reporter truth is refreshed and remains authoritative over process exit code.

## Core guard contract

The Core layer owns deterministic pre-execution validation. WPF must not decide whether an execution is safe by itself.

The initial Core request contains only operator intent:

- profile id;
- target id;
- release token.

The guard context supplies trusted runtime evidence:

- configured script relative path;
- approved script SHA-256;
- current script SHA-256;
- allowlisted target ids;
- whether the bounded access session is READY;
- whether another deployment execution is already running.

The guard fails closed when any required field is missing, the script fingerprint changed, the access session is not READY, a target is not allowlisted, a release token is unsafe, or another execution is already active.

## Script approval semantics

Script approval is stored separately from the shareable/private deployment profile contract. A local approval records only:

- profile id;
- configured project-relative script path;
- approved SHA-256 fingerprint;
- approval timestamp.

The approval store is per-user local application state. It contains no credentials. A changed profile/script path, missing or malformed approval, unsupported approval schema, or changed script fingerprint fails closed.

The current script hash is still computed from the actual Windows project file immediately before execution; persisted approval metadata is never treated as evidence that the file is unchanged.

## Input safety

A release token is intentionally narrow. Initial accepted characters are ASCII letters, digits, `.`, `_`, and `-`, with a maximum length of 64 characters and an alphanumeric first character. Whitespace, path separators, quotes, command separators, substitutions, environment syntax and control characters are rejected.

Target ids are never interpreted as shell fragments. A target is valid only when it exactly matches one item from the trusted allowlist.

The execution service uses `ProcessStartInfo.ArgumentList` / direct argument vectors. It does not construct a `sh -c` command from operator input.

For the current interactive Millenova script, the narrow executor feeds only bounded numeric prompt choices. PDA-5 always appends deployment mode `1` (dry run); there is no code path in this executor that can submit live mode `2`. The app-owned `SSH_AUTH_SOCK` and `SSH_AGENT_PID` values are injected through `/usr/bin/env` as fixed environment assignments before `/usr/bin/bash` executes the configured relative script from the verified WSL project directory.

## Current implementation slice

Implemented on `main`:

- Core dry-run guard rejects changed/missing script fingerprints, non-READY access, unsafe release tokens, non-allowlisted targets and concurrent execution;
- per-user script approval records exact profile/script/SHA-256/timestamp and must match the current file immediately before launch;
- direct WSL executor uses `ProcessStartInfo.ArgumentList`, verified WSL project directory, fixed app-owned agent environment and `/usr/bin/bash` on the configured relative script;
- controller exposes a revalidated execution runtime only after live agent inspection plus a fresh comparison between the currently approved deployment key and the bounded lease fingerprint;
- a Core single-execution gate prevents a second deployment execution from acquiring authority concurrently;
- the shell now exposes **Dry run…** and a dedicated operator window with exact script fingerprint approval, allowlisted target selection and PATCH/MINOR/MAJOR release choices;
- the current Millenova adapter exposes `hostinger`, `vps`, and when both are configured, `all`; VPS-only omits the release-choice prompt while every PDA-5 path fixes deployment mode to dry run;
- ZPet switches to RUNNING while a dry-run process is executing;
- after process completion ZDeployPet records bounded PASS/FAIL/completion highlights in the sanitized Console / Activity stream and explicitly refreshes PDA-4 latest/history reporter truth;
- user-reported Release test run on 2026-10-10 passed 74 Core tests and 21 Infrastructure tests before the UI/controller execution slice; a fresh build/test run is required for the new shell slice.

## Initial implementation sequence

1. ✅ define immutable Core dry-run request, guard context, state and validation contracts;
2. ✅ add Core tests for changed script fingerprint, non-READY access, invalid target/release, concurrent execution and shell-like input rejection;
3. ✅ add separate per-user deployment-script approval contract/store plus matching validation and persistence tests;
4. ✅ add a narrow WSL executor using direct argument vectors, verified WSL working directory, fixed agent environment and bounded interactive prompt choices; no operator input is interpolated into `sh -c`;
5. ✅ revalidate the PDA-2 session immediately before launch, including current approved-key fingerprint versus the live bounded lease;
6. ✅ add a single in-process execution lease/lock with Core coverage;
7. ✅ add a shell dry-run surface with allowlisted target and bounded release controls; no arbitrary command field;
8. 🟡 sanitized start/exit/highlight lifecycle is wired to Console / Activity; true line-by-line streaming remains optional follow-up if operator smoke shows it is needed;
9. ✅ refresh PDA-4 latest/history after process completion and direct the operator to reporter truth as the authoritative outcome;
10. 🟡 published-development Millenova dry-run smoke from ZDeployPet remains pending.

## Acceptance gate

PDA-5 is accepted only when:

- only the approved configured script can run;
- the script fingerprint is rechecked immediately before execution and a mismatch blocks launch;
- LOCKED/expired/wrong-key deployment access blocks launch;
- only allowlisted targets can be selected/executed;
- unsafe release input cannot reach the process layer;
- a second concurrent execution is blocked;
- no generic command textbox or `sh -c` path exists for operator input;
- dry-run lifecycle is visible in sanitized Console / Activity;
- PDA-4 structured reporter evidence is refreshed after completion and remains authoritative over process exit status;
- automated execution-guardrail tests pass;
- a real Millenova dry run starts from the ZDeployPet UI and does not require manually invoking the script in a terminal.
