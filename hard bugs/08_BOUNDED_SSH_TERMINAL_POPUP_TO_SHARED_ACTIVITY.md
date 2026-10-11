# Hard bug: unnecessary SSH-agent terminal popup during access activation

## Initial symptom

Turning ZDeploy Pet ON launched a terminal window containing messages such as:

```text
ZDEPLOYPET BOUNDED DEPLOYMENT ACCESS
Profile              : Millenova
Approved fingerprint : SHA256:...
Lease                : 28800 seconds

ZDeployPet is asking OpenSSH to load ONLY the approved deployment key
into this app-owned ssh-agent for the bounded lease above.
```

The terminal ended with:

```text
OpenSSH accepted the key. Return to ZDeployPet and click Check session.
Press Enter to close.
```

From a security perspective this was bounded and correct, but from a UX perspective it became misleading clutter. Most successful activation attempts did not require user interaction after the key was accepted, yet a second window remained open.

## Why the popup existed originally

The passphrase boundary was intentional:

- ZDeployPet must never receive or store the private-key passphrase;
- OpenSSH/terminal owns any passphrase prompt;
- only the approved key is loaded into the app-owned `ssh-agent`;
- the resulting bounded agent/session is probed before deployment access becomes READY.

The mistake was conflating the **security boundary** with a requirement that a terminal window remain part of the normal UI.

## Desired behavior

Normal successful activation should be visible in the shared ZDeployPet activity pane:

```text
ZDeploy Pet is preparing the bounded deployment-access session.
Waiting for the trusted OpenSSH key prompt to complete.
Automatic bounded-agent target verification started.
Hostinger: Success — authenticated read-only SSH probe passed.
VPS: Success — authenticated read-only SSH probe passed.
Deployment access is ON / READY.
```

A trusted external terminal should appear only when OpenSSH genuinely requires private passphrase input.

## Engineering boundary

The important rule is:

```text
remove redundant terminal chrome
≠
move passphrase handling into ZDeployPet
```

ZDeployPet must continue to avoid passphrase capture.

## Safe implementation pattern

The application activity path should receive lifecycle events:

```csharp
ShellRuntime.Activity.Add(
    ShellActivityLevel.Info,
    "Session",
    "ZDeploy Pet is preparing the bounded deployment-access session.");
```

After the agent operation returns:

```csharp
ShellRuntime.Activity.Add(
    ShellActivityLevel.Success,
    "Session",
    "Deployment access is ON / READY. All configured target probes passed.");
```

The terminal process should be configured to auto-exit after a successful non-interactive completion. If OpenSSH asks for a passphrase, the terminal remains the trusted input boundary.

## Why the shared ZDeployPet console was the correct destination

The app already had one bounded activity view with:

- timestamped lines;
- Copy all;
- Copy selection;
- per-session status;
- no deployment authority of its own.

Creating another read-only status window duplicated information and made the product feel like a command center instead of a deployment pet.

## Verification

### Key already available / no prompt

1. click ZDeploy Pet;
2. no persistent terminal should remain;
3. ZDeployPet console should show the activation lifecycle;
4. pet changes to READY only after all configured probes pass.

### Key requires passphrase

1. click ZDeploy Pet;
2. trusted terminal may appear;
3. enter passphrase only in that terminal;
4. ZDeployPet must never display, log, receive or persist it;
5. after OpenSSH completes, normal status returns to the shared activity console.

## Prevention rule

Keep external windows only where the external security boundary actually requires them. Status/progress belongs in the shared application activity surface.
