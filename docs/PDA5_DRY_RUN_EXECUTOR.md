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

### Interactive-script safety correction

The first PDA-5 implementation pre-fed the numeric target/release/mode answers to the configured script's stdin. Published-development smoke on 2026-10-10 proved that this is unsafe for an interactive deployment script: commands executed during preflight can share/consume stdin, so later synthetic answers can be shifted away from the prompt they were intended for. A run requested as dry unexpectedly entered the script's live branch and completed a real 4.4.24 deployment. The reporter correctly recorded that run as `mode=live`, so PDA-4 truth exposed the safety failure immediately.

That stdin-prefeed design is rejected and must never be restored.

The replacement contract is prompt-driven and fail-closed:

- no target/release/mode answer is written before its exact expected prompt is observed;
- target choice is emitted only after `Choose deployment destination [1/2/3]:`;
- release choice is emitted only after `Choose release type [1/2/3]:` when the selected target requires it;
- dry mode `1` is emitted only after `Choose [1/2]:`;
- ZDeployPet never emits the live authorization phrase `DEPLOY MILLENOVA`;
- observing `LIVE MILLENOVA DEPLOYMENT` or the live confirmation prompt is an immediate safety violation and the process tree is killed;
- a successful process exit is not accepted unless the script first proves `DRY RUN — NOTHING WILL BE MODIFIED` and later reaches `DRY RUN COMPLETE`;
- any missing/shifted/unexpected prompt sequence fails closed with a dedicated ZDeployPet safety failure rather than silently trusting exit code.

This preserves the project-owned interactive deployment script while preventing queued stdin from crossing command/prompt boundaries.

## Current implementation slice

Implemented on `main`:

- Core dry-run guard rejects changed/missing script fingerprints, non-READY access, unsafe release tokens, non-allowlisted targets and concurrent execution;
- per-user script approval records exact profile/script/SHA-256/timestamp and must match the current file immediately before launch;
- direct WSL executor uses `ProcessStartInfo.ArgumentList`, verified WSL project directory, fixed app-owned agent environment and `/usr/bin/bash` on the configured relative script;
- controller exposes a revalidated execution runtime only after live agent inspection plus a fresh comparison between the currently approved deployment key and the bounded lease fingerprint;
- a Core single-execution gate prevents a second deployment execution from acquiring authority concurrently;
- the shell exposes **Dry run…** and a dedicated operator window with exact script fingerprint approval, allowlisted target selection and PATCH/MINOR/MAJOR release choices;
- the current Millenova adapter exposes Hostinger, VPS and Both targets; VPS-only omits the release-choice prompt;
- target identity persistence and legacy host-trust recovery were corrected after smoke exposed target GUID churn;
- the rejected stdin-prefeed executor has been replaced with a prompt-driven protocol that sends one bounded answer only after the exact corresponding script prompt appears;
- the prompt protocol kills the process on any live-mode marker and requires both the dry-run banner and `DRY RUN COMPLETE` before it can report protocol success;
- ZPet switches to RUNNING while a dry-run process is executing;
- after process completion ZDeployPet records bounded PASS/FAIL/completion highlights in the sanitized Console / Activity stream and explicitly refreshes PDA-4 latest/history reporter truth;
- user-reported Release build/tests on 2026-10-10 passed after the prompt-driven safety replacement; published-development runtime smoke is now the remaining gate.

## Initial implementation sequence

1. ✅ define immutable Core dry-run request, guard context, state and validation contracts;
2. ✅ add Core tests for changed script fingerprint, non-READY access, invalid target/release, concurrent execution and shell-like input rejection;
3. ✅ add separate per-user deployment-script approval contract/store plus matching validation and persistence tests;
4. ✅ add a narrow WSL executor using direct argument vectors, verified WSL working directory and fixed agent environment; no operator input is interpolated into `sh -c`;
5. ✅ revalidate the PDA-2 session immediately before launch, including current approved-key fingerprint versus the live bounded lease;
6. ✅ add a single in-process execution lease/lock with Core coverage;
7. ✅ add a shell dry-run surface with allowlisted target and bounded release controls; no arbitrary command field;
8. 🟡 sanitized start/exit/highlight lifecycle is wired to Console / Activity; true line-by-line display remains optional follow-up;
9. ✅ refresh PDA-4 latest/history after process completion and direct the operator to reporter truth as the authoritative outcome;
10. ❌ first published-development Millenova dry-run smoke failed safety: stdin-prefeed shifted answers and the script entered live mode; this design is retired;
11. 🟡 replacement prompt-driven protocol implemented and build/tests passed; published-development runtime smoke remains pending.

## Acceptance gate

PDA-5 is accepted only when:

- only the approved configured script can run;
- the script fingerprint is rechecked immediately before execution and a mismatch blocks launch;
- LOCKED/expired/wrong-key deployment access blocks launch;
- only allowlisted targets can be selected/executed;
- unsafe release input cannot reach the process layer;
- a second concurrent execution is blocked;
- no generic command textbox or `sh -c` path exists for operator input;
- no prompt answer is pre-buffered before its exact expected script prompt;
- PDA-5 never emits the live authorization phrase;
- a live-mode banner/prompt kills the run before authorization;
- successful execution requires observed dry-mode banner plus `DRY RUN COMPLETE`;
- dry-run lifecycle is visible in sanitized Console / Activity;
- PDA-4 structured reporter evidence is refreshed after completion and remains authoritative over process exit status;
- automated execution-guardrail and prompt-protocol tests pass;
- a real Millenova dry run starts from the ZDeployPet UI, remains reporter `mode=dry`, completes successfully, and does not require manually invoking the script in a terminal.
