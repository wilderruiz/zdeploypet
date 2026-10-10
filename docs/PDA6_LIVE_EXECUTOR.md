# PDA-6 — Live executor and exact human confirmation

**Status:** 🟡 ACTIVE — Core review/confirmation contract implemented; no live execution path enabled yet

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

The first PDA-6 slice adds deterministic Core contracts only. No live process can be launched by this slice.

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
- automated Core regression coverage for the above.

Not implemented yet:

- live review WPF surface;
- live executor;
- wiring to the Live deploy console tab;
- project-script live prompt adapter;
- reporter refresh/closeout for a real live run;
- production live smoke.

## Implementation sequence

1. ✅ Core immutable live request/review/confirmation contracts;
2. ✅ Core tests for exact confirmation and invalidation rules;
3. ⬜ build immutable WPF review surface with confirmation input blank by default and no auto-fill/paste helper;
4. ⬜ revalidate approved script/session/target/release immediately before launch;
5. ⬜ implement narrow live WSL executor that supplies bounded target/release/mode choices but never supplies the confirmation phrase itself;
6. ⬜ require the operator phrase to be passed exactly once at the script's exact live-confirmation prompt;
7. ⬜ isolate natural output in the Live deploy console tab;
8. ⬜ keep one execution lease for the full live run and never auto-retry;
9. ⬜ refresh PDA-4 report truth after completion and use reporter result as authoritative;
10. ⬜ automated negative tests for stale review, changed fingerprint, wrong confirmation, non-READY access, concurrent execution and live-confirmation automation attempts;
11. ⬜ explicit operator-controlled published-development live smoke.

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
