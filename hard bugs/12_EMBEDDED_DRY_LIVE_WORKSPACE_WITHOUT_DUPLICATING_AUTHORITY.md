# Hard bug: embed DRY/LIVE workflows without duplicating deployment authority

## Initial symptom

Home had DRY and LIVE buttons, but clicking either opened a separate top-level window:

```text
Allowlisted dry run
Live deployment review
```

The UI redesign wanted the tabs themselves to become the real workspace:

```text
DRY | LIVE | REPORTS
```

The hard part was not drawing the forms. The hard part was preserving the already-reviewed PDA-5/PDA-6 safety logic exactly.

## Why a naïve rewrite was dangerous

The existing windows owned real deployment invariants:

- approved script fingerprint checks;
- READY/access-state requirement;
- one execution at a time;
- dry-run mode enforcement;
- immutable live review;
- exact confirmation phrase;
- invalidation when target/release/fingerprint/session state changes;
- no arbitrary command field;
- reporter/history refresh after execution.

Copy/pasting those rules into a new Home surface would create two implementations of authority. They would inevitably drift.

## Design constraint

The refactor had to be:

```text
presentation relocation
not
execution-policy rewrite
```

## Final architecture

The existing workflow objects were reused as controllers/content owners. Their body content became embeddable into the Home workspace while the top-level Window chrome stopped being the normal presentation path.

Conceptually:

```text
DryRunWindow logic/content
        ↓ detach body from Window shell
Home → DRY tab host

LiveDeploymentReviewWindow logic/content
        ↓ detach body from Window shell
Home → LIVE tab host
```

The event handlers and safety state stayed attached to the original workflow implementation.

## Access-state behavior

### LOCKED

The tab shows only purpose/instructions:

```text
1. Click ZDeploy Pet.
2. Complete the bounded access check if prompted.
3. Wait for READY.
4. The workflow controls appear here.
```

No execution controls are shown.

### READY

The real existing workflow surface is embedded into the tab.

### Back to LOCKED

The embedded workflow is discarded/reset rather than leaving a stale live review armed.

That last step matters especially for LIVE. An immutable review created under one session must not survive a state change that invalidates its authorization assumptions.

## Safe re-parenting sequence

```csharp
UIElement body = workflow.DetachEmbeddableContent();
DetachWorkspaceElement(body);
host.Child = body;
```

The workflow object remains responsible for its own validation and commands.

## Why this pattern is preferable to inheritance gymnastics

Turning a `Window` directly into a `UserControl` late in development can cause a large surface-area refactor. Extracting/reusing the owned content first allowed ZDeployPet to preserve behavior while changing presentation incrementally.

A future cleanup may formalize the reusable surfaces into dedicated `UserControl` classes, but only after smoke-tested parity.

## Verification matrix

### Locked

- DRY tab: instructions only;
- LIVE tab: instructions only;
- no hidden execution through tab controls.

### Ready

- DRY shows approved fingerprint and Dry request controls;
- LIVE shows proposed deployment/review controls;
- no top-level Dry/Live popup appears from normal Home use.

### Invalidation

- turn pet OFF;
- DRY/LIVE return to instructions;
- old LIVE review cannot still launch;
- turn pet ON again;
- controls are reconstructed/refreshed against current state.

### Execution

- run real DRY smoke;
- verify right Dry Run console follows latest run;
- run LIVE review/execution smoke;
- verify reporter/history update;
- verify safety checks behave identically to pre-embed behavior.

## Prevention rule

When moving security-sensitive UI, reuse one authority implementation. Never duplicate authorization logic just to make a new presentation surface easier to build.
