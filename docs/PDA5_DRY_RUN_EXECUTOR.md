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

## Input safety

A release token is intentionally narrow. Initial accepted characters are ASCII letters, digits, `.`, `_`, and `-`, with a maximum length of 64 characters and an alphanumeric first character. Whitespace, path separators, quotes, command separators, substitutions, environment syntax and control characters are rejected.

Target ids are never interpreted as shell fragments. A target is valid only when it exactly matches one item from the trusted allowlist.

The execution service must use `ProcessStartInfo.ArgumentList` / direct argument vectors. It must not build `sh -c` command strings from operator input.

## Initial implementation sequence

1. ✅ define immutable Core dry-run request, guard context, state and validation contracts;
2. ✅ add Core tests for changed script fingerprint, non-READY access, invalid target/release, concurrent execution and shell-like input rejection;
3. add approved script-fingerprint persistence/approval semantics without weakening the existing profile boundary;
4. add a narrow WSL execution service that runs only the configured profile script through an argument vector;
5. revalidate the PDA-2 session immediately before launch and inject only the owned agent environment required by the approved script;
6. add a single in-process execution lease/lock;
7. add a shell dry-run surface with allowlisted target and bounded release controls; no arbitrary command field;
8. stream sanitized lifecycle/status output to Console / Activity;
9. refresh PDA-4 latest/history after completion and display reporter truth as the authoritative outcome;
10. run the published-development Millenova dry-run smoke from ZDeployPet with no manual terminal script invocation.

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
