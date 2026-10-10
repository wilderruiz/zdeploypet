# PDA-5 — Allowlisted dry-run executor

**Status:** ✅ ACCEPTED 2026-10-10

PDA-5 adds the first execution capability to ZDeployPet while deliberately remaining narrower than a terminal. ZDeployPet may invoke only the configured/approved project deployment script, with bounded target/release choices and a structurally forced dry-run mode.

## Authority boundary

The project-owned deployment script remains the deployment engine. ZDeployPet does not rewrite project deployment logic, infer arbitrary commands, or treat process exit status as stronger than the project reporter.

Accepted invariants:

- execution mode is fixed to **dry run**;
- only the configured profile script may run;
- the current script SHA-256 must exactly match the separately approved fingerprint immediately before launch;
- target values come only from the trusted allowlist;
- release selection is bounded and never interpreted as shell text;
- PDA-2 deployment access must be live and READY after ownership/liveness/fingerprint revalidation;
- only one deployment execution may run at a time;
- ZDeployPet never emits the live authorization phrase `DEPLOY MILLENOVA`;
- observing a live-mode banner or live confirmation request during PDA-5 kills the process fail-closed;
- after completion PDA-4 reporter truth is refreshed and remains authoritative over process exit status.

## Interactive-script safety correction

The first PDA-5 prototype pre-fed target/release/mode answers to stdin. Published-development smoke on 2026-10-10 proved that unsafe for an interactive deployment script because commands run during preflight can share/consume stdin. A run requested as dry unexpectedly entered the script's live branch. PDA-4 correctly exposed the event as reporter `mode=live`.

That stdin-prefeed design is permanently rejected.

The accepted implementation uses a fixed internal Bash read adapter that handles only the three approved interactive choices. It structurally forces deployment mode `1` (dry), cannot emit the live confirmation phrase, normalizes Windows CRLF before execution, and treats any live marker as a safety violation. The protocol requires both `DRY RUN — NOTHING WILL BE MODIFIED` and `DRY RUN COMPLETE` before protocol success. Stdout/stderr arrival ordering is explicitly non-authoritative so harmless stream races cannot trigger false safety failures.

## Accepted implementation

- Core dry-run guard rejects changed/missing script fingerprints, non-READY access, unsafe release values, non-allowlisted targets and concurrent execution.
- Per-user script approval stores only profile id, project-relative script path, approved SHA-256 and approval timestamp.
- WSL execution uses direct argument vectors, verified WSL project directory, fixed app-owned agent environment and the configured project script.
- PDA-2 execution runtime is revalidated immediately before launch.
- A Core single-execution gate prevents overlapping deployment runs.
- **Dry run…** is placed under Deployment access, disabled while access is OFF and enabled only at ON / READY with approved target probes.
- The operator window is modeless so the main shell and console remain usable during execution.
- Millenova target choices are Hostinger, VPS and Both targets; PATCH/MINOR/MAJOR are bounded release choices where applicable.
- A 20-minute execution ceiling prevents indefinite RUNNING state.
- Console / Activity is split into **Dry run / Live deploy / ZDeployPet** channels. Dry-run script output is shown naturally without ZDeployPet metadata wrapping each script line.
- ZPet reflects RUNNING while the process executes.
- PDA-4 latest/history truth is refreshed after completion.

## Safety incident and resolution evidence

The initial unsafe stdin-prefeed smoke is retained as negative evidence and must not be repeated. Subsequent corrections addressed:

- prompt/input shifting across preflight commands;
- target GUID/trust identity churn;
- deployment-access state synchronization;
- CRLF leaking into the internal Bash adapter;
- invisible `read -p` prompts with redirected stdin;
- stdout/stderr ordering races in dry-marker observation;
- console separation and natural script-stream rendering.

## Acceptance evidence

Published-development Millenova smoke on 2026-10-10 passed from the ZDeployPet UI using **Both targets / PATCH**:

- approved script fingerprint matched immediately before launch;
- bounded deployment access was ON / READY with both target probes passed;
- script selected Both targets and PATCH `4.4.25`;
- `DRY RUN — NOTHING WILL BE MODIFIED` was observed;
- Hostinger frontend delta preview completed;
- Hostinger config preview completed with zero changes;
- VPS preview completed with zero changes;
- `DRY RUN COMPLETE` was observed;
- local `version.txt` was restored to `v4.4.24`;
- reporter artifacts were generated for `v4.4.25_dry_both`;
- process exit code was `0`;
- no live authorization phrase or live-mode marker appeared.

The user then reported the complete `dotnet test ZDeployPet.sln -c Release` regression run green after the final prompt-ordering regression test.

## Acceptance gate — satisfied

- ✅ only the approved configured script can run;
- ✅ changed script fingerprint blocks launch;
- ✅ LOCKED/expired/wrong-key deployment access blocks launch;
- ✅ only allowlisted targets and bounded release selections reach execution;
- ✅ concurrent execution is blocked;
- ✅ no generic command textbox/operator-controlled shell-command path exists;
- ✅ PDA-5 never emits the live authorization phrase;
- ✅ live-mode banner/prompt fails closed;
- ✅ successful execution requires verified dry marker plus `DRY RUN COMPLETE`;
- ✅ dry-run shell output is isolated in its own sanitized console channel;
- ✅ PDA-4 structured reporter truth is refreshed after completion and remains authoritative;
- ✅ automated execution-guardrail and prompt-protocol regression tests pass;
- ✅ real Millenova dry run completed successfully from ZDeployPet with reporter `mode=dry` and no manual script invocation.

**Next phase:** PDA-6 — Live executor and exact human confirmation.
