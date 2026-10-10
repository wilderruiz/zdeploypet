# PDA-6 — Live executor and exact human confirmation

**Status:** 🟡 ACTIVE — guarded live executor wired to reviewed UI; published build/runtime smoke still required

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

## Review and exact confirmation

The immutable review freezes profile identity, configured script path, current approved SHA-256, target, release, configured confirmation phrase and review id/timestamp. Review creation fails closed unless the current script still matches approval, deployment access is READY, no execution is running, target/release are bounded, and the confirmation phrase is safe.

The operator-entered confirmation is accepted only as an ordinal exact match. Case changes, leading/trailing whitespace, script changes, profile/target/release/phrase changes, lost READY state or a concurrent execution invalidate authorization.

The confirmation input always starts blank. Editing it after validation disarms live launch immediately.

## Live executor

`LiveDeploymentPromptProtocol` observes the approved project script and fails closed unless the run proves the live protocol. Any dry-run marker is a safety violation. The protocol requires the live banner, exact configured confirmation prompt and `MILLENOVA DEPLOYMENT COMPLETE` before a successful process exit can be protocol-complete.

`WslLiveDeploymentExecutor`:

- uses the existing app-owned bounded SSH-agent runtime;
- invokes only the configured project-relative deployment script;
- supplies only bounded numeric target/release choices;
- structurally selects mode `2` for live deployment;
- accepts only the already human-entered phrase after Core validation;
- forwards that phrase exactly once and only at the exact live-confirmation prompt;
- never derives, generates or substitutes confirmation text;
- rejects repeated confirmation prompts;
- streams bounded stdout/stderr observationally;
- has a 30-minute hard ceiling;
- never auto-retries after cancellation, timeout, failure or partial deployment.

## Current implementation slice

Implemented on `main`:

- Core immutable review/confirmation contracts and negative tests;
- immutable WPF review surface with blank confirmation input;
- `Live deploy review…` available only while deployment access is ON / READY;
- target/release changes invalidate the review;
- published-development review-only UI smoke passed on 2026-10-10;
- live prompt protocol and staged WSL executor with safety tests;
- exact confirmation validation now arms a separate **Start LIVE deployment** button;
- any text edit after validation immediately disarms that button;
- immediately before launch, ZDeployPet rechecks script SHA/approval, READY bounded-agent runtime, frozen target/release/phrase and the global execution gate;
- one global execution lease spans the full live run;
- natural script output is emitted only to the `deploy_millenova.live.sh` / **Live deploy** console channel;
- completion refreshes PDA-4 latest/history evidence and treats reporter truth as authoritative;
- cancellation/failure messaging explicitly warns that a partial deployment may exist and no retry is attempted;
- confirmation text is never written to activity logs.

Still required before acceptance:

- published Release build/test of this wired slice;
- operator UI smoke proving validate/disarm/revalidate behavior;
- one explicit operator-controlled real Millenova live deployment from ZDeployPet;
- final reporter confirmation of expected `live` target/result;
- any regression fixes exposed by that smoke.

## Implementation sequence

1. ✅ Core immutable live request/review/confirmation contracts;
2. ✅ Core tests for exact confirmation and invalidation rules;
3. ✅ immutable WPF review surface with confirmation input blank by default;
4. ✅ launch-time revalidation wired for script approval/fingerprint, session, target, release and phrase;
5. ✅ narrow live WSL executor with bounded target/release/mode inputs;
6. ✅ exact human-entered phrase forwarded only at the exact live prompt;
7. ✅ natural output routed to the Live deploy console channel;
8. ✅ one execution lease for the full run; no auto-retry;
9. ✅ PDA-4 report refresh wired after completion;
10. ✅ Core live-protocol negative tests in place;
11. 🟡 published-development build + UI smoke of the wired live path;
12. ⬜ explicit operator-controlled real live smoke and reporter acceptance.

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
