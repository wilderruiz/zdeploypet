# Hard bug: removing legacy ON/OFF UI accidentally hid DRY/LIVE actions

## Initial symptom

The old Deployment access section had an explicit TURN ON / TURN OFF control. The redesign correctly moved that responsibility into the ZDeploy Pet status card itself:

```text
click pet → turn deployment access ON
click pet again → turn it OFF
```

After removing the legacy ON/OFF control, DRY and LIVE actions disappeared or became unavailable in ways that did not match the actual session state.

## Root cause

The actions were not only visually located near the old Deployment access container — some of their visibility/enabled-state refresh logic was coupled to that legacy section.

This is a common UI-refactor trap:

```text
control A looks redundant
→ remove A
→ unrelated control B disappears
```

because B's state was indirectly refreshed or hosted by A's old layout/lifecycle path.

## Engineering lesson

Before deleting a legacy control, search for all of these dependencies:

```text
Visibility
IsEnabled
parent container
event handler
refresh method
IsVisibleChanged
Loaded
state transition callback
```

The correct source of truth for DRY/LIVE was never the visual ON/OFF button. It was the deployment-access/session state.

## Correct state model

```csharp
bool deploymentReady = AccessState == DeploymentAccessState.Ready;

DryButton.IsEnabled = deploymentReady && !ExecutionGate.IsBusy;
LiveButton.IsEnabled = deploymentReady && !ExecutionGate.IsBusy;
```

Later, when embedded into workspace tabs:

```text
LOCKED → instructions only
READY  → real workflow content
```

The UI should derive from state, not from the existence of another control.

## Pet becomes the state control

The ZDeploy Pet card became both:

- status indicator;
- ON/OFF interaction point.

That allowed the redundant Deployment access block to disappear while keeping one clear source of truth.

## Verification

### Locked

```text
pet = LOCKED
DRY/LIVE visually unavailable or instructional
no deployment execution possible
```

### Ready

```text
click pet
bounded session/probes succeed
pet = READY
DRY/LIVE become available
```

### Off again

```text
click pet
session torn down/locked
DRY/LIVE immediately return to unavailable/instruction state
```

### Busy

Even while READY, execution gate can temporarily disable actions:

```csharp
IsEnabled = deploymentReady && !_executionGate.IsBusy;
```

## Prevention rule

Never let execution availability depend on the presence/visibility of a UI control. Bind all action availability to explicit domain/session state.
