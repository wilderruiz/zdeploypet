# PDA-6 — Live executor and exact human confirmation

**Status:** 🟡 ACTIVE — immutable review smoke passed; live executor staged but not wired to UI

PDA-6 extends the accepted PDA-5 execution foundation to live deployment. The safety boundary is stricter than dry run: ZDeployPet may prepare and display an immutable review, but it must never synthesize, prefill, auto-type, paste, or otherwise supply the operator's live confirmation phrase.

## Authority boundary

The project-owned deployment script remains the deployment engine and the structured reporter remains authoritative after execution.

PDA-6 must preserve all PDA-5 execution controls and additionally require:

- an immutable pre-deployment review snapshot;
- exact human confirmation typed by the operator;
- revalidation immediately before execution;
- confirmation invalidation if profile/script/target/release/confirmation phrase/session state changes;
- no automatic live confirmation phrase generation or insertion by ZDeployPet;
- no automatic retry of a partial live deployment;
- one execution lock spanning the full live run;
- natural live script output isolated in the **Live deploy** console tab;
- reporter truth, not process exit code, closes the run.

## Core review snapshot

A review snapshot freezes:

- review id;
- profile id + display name;
- configured project-relative script path;
- current approved script SHA-256;
- target id + display label;
- bounded release token;
- configured live confirmation phrase;
- creation timestamp.

Review creation fails closed unless:

- the current script SHA-256 still equals the approved fingerprint;
- deployment access is READY;
- no execution is already running;
- target is allowlisted;
- release token passes the accepted bounded-token grammar;
- live confirmation phrase is non-empty, bounded, and contains no control characters.

## Exact confirmation and invalidation

The operator-entered confirmation is accepted only when it is an **ordinal exact match** to the phrase frozen in the review snapshot. Case changes, leading/trailing whitespace, or any other mutation fail.

Immediately before a future live execution begins, the current state must still equal the frozen review for:

- profile id;
- configured script path;
- current script SHA-256;
- target id;
- release token;
- configured live confirmation phrase;
- deployment-access READY state;
- no concurrent execution.

Any mismatch invalidates the confirmation and requires a brand-new review + brand-new human confirmation.

## Live prompt/executor staging

The next internal slice is now implemented but intentionally **not connected to any UI execution button yet**.

`LiveDeploymentPromptProtocol` observes the approved project script and fails closed unless the run proves the live protocol. It treats any dry-run marker as a safety violation, requires the live banner, requires the exact configured confirmation prompt, and requires `MILLENOVA DEPLOYMENT COMPLETE` before a successful process exit can be considered protocol-complete.

`WslLiveDeploymentExecutor` is a narrow WSL adapter that:

- uses the existing app-owned bounded SSH-agent runtime;
- invokes only the configured project-relative deployment script;
- supplies only allowlisted numeric target/release choices;
- structurally selects mode `2` for live deployment;
- accepts an already human-entered confirmation phrase only after exact Core validation;
- forwards that phrase exactly once and only when the script asks the exact configured live-confirmation prompt;
- never derives, generates or substitutes confirmation text;
- rejects a repeated confirmation prompt;
- streams bounded stdout/stderr observationally;
- has a 30-minute hard ceiling;
- kills the process on cancellation/timeout and never auto-retries.

This executor exists for compilation/test staging only. The Live deployment review window still cannot start a live process.

## Current implementation slice

Implemented on `main`:

- `LiveDeploymentRequest`;
- `LiveDeploymentReviewContext`;
- immutable `LiveDeploymentReviewSnapshot`;
- `LiveDeploymentConfirmationContext`;
- `LiveDeploymentReviewGuard.CreateReview(...)`;
- `LiveDeploymentReviewGuard.ValidateConfirmation(...)`;
- exact ordinal confirmation matching;
- confirmation invalidation on session lock, concurrent execution, profile/script/fingerprint/target/release/phrase changes;
- bounded confirmation-phrase validation;
- automated Core regression coverage for the above;
- `Live deploy review…` control directly under Deployment access, enabled only while access is ON / READY;
- modeless `LiveDeploymentReviewWindow` with allowlisted target and bounded release choices;
- review-time revalidation of script approval/fingerprint, READY runtime and global execution gate;
- immutable review display of profile/project, target, mapped destination summary, release transition preview, script path, SHA-256 and review id;
- exact configured live confirmation phrase displayed separately while the operator input field always starts blank;
- a **Validate typed confirmation** action that performs confirmation validation only and cannot launch live deployment;
- target/release changes invalidate the existing review and clear the typed phrase;
- published-development UI smoke passed on 2026-10-10: build succeeded, Live deploy review surfaced correctly under ON / READY, and review-only flow behaved as expected;
- Core `LiveDeploymentPromptProtocol` plus regression tests for complete live flow, dry-mode rejection, missing live proof, missing confirmation prompt and successful-exit-without-completion rejection;
- staged `WslLiveDeploymentExecutor` with exact-prompt single-use operator confirmation forwarding and no UI wiring.

Not implemented yet:

- launch-time wiring from the reviewed/confirmed UI into the live executor;
- natural live script output routing to the Live deploy console during an actual run;
- reporter refresh/closeout for a real live run;
- production live smoke.

## Implementation sequence

1. ✅ Core immutable live request/review/confirmation contracts;
2. ✅ Core tests for exact confirmation and invalidation rules;
3. ✅ immutable WPF review surface with confirmation input blank by default and no auto-fill helper;
4. 🟡 revalidate approved script/session/target/release immediately before launch — review-time validation is implemented; launch-time wiring remains required;
5. ✅ stage narrow live WSL executor that supplies bounded target/release/mode choices and can forward only the already human-entered exact phrase at the exact confirmation prompt;
6. ✅ add observational live prompt protocol and negative regression tests; no UI execution path yet;
7. ⬜ wire confirmed immutable review to the executor only after a fresh launch-time revalidation;
8. ⬜ isolate natural output in the Live deploy console tab;
9. ⬜ keep one execution lease for the full live run and never auto-retry;
10. ⬜ refresh PDA-4 report truth after completion and use reporter result as authoritative;
11. ⬜ complete final automated negative tests around executor invocation and stale review handling;
12. ⬜ explicit operator-controlled published-development live smoke.

## Acceptance gate

PDA-6 is accepted only when:

- live execution cannot begin without an immutable review;
- exact operator-entered confirmation is required;
- ZDeployPet never auto-types, auto-fills, pastes, derives or synthesizes the confirmation phrase;
- any material change after review invalidates confirmation;
- the current script fingerprint is rechecked immediately before launch;
- deployment access is revalidated immediately before launch;
- target/release remain allowlisted and bounded;
- concurrent execution is blocked;
- the exact confirmation reaches only the approved script's exact live-confirmation prompt;
- the app never auto-retries a partial live deployment;
- natural live output is isolated in the Live deploy console tab;
- PDA-4 reporter evidence is refreshed and authoritative after completion;
- automated Core/executor safety tests pass;
- an explicit operator-controlled real Millenova live deployment completes from ZDeployPet and the reporter records the expected live result.
