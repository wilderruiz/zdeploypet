# Hard engineering lesson: green compile is not enough for WPF

## Why this deserves its own dossier

Many ZDeployPet bugs fell into one of three categories:

```text
compile-time failure
publish-time failure
runtime/UI lifecycle failure
```

Treating `dotnet build` as the whole verification story repeatedly missed the third category.

## Canonical verification loop

From the repository root:

```powershell
cd C:\Dev\ZDeployPet
dotnet test ZDeployPet.sln -c Release
powershell -ExecutionPolicy Bypass -File scripts\publish-local.ps1
```

The local published executable is:

```text
C:\Dev\ZDeployPet_Releases\current\ZDeployPet.exe
```

The publish script also emits the SHA-256 of the executable. That makes it possible to confirm that the binary being launched really corresponds to the latest local publish.

## Why Release configuration matters

The actual user path is the published Release app. Differences can appear in:

- trimming/publish layout;
- resource resolution;
- startup timing;
- file locations;
- runtime initialization;
- stale executable confusion.

Therefore the accepted loop is Release-first.

## Minimum smoke after UI work

```text
launch
Home visible
Profile visible
Home ↔ Profile switch
pet LOCKED → READY → LOCKED
DRY tab
LIVE tab
REPORTS tab
right pane tabs
Help menu
close/relaunch
```

If the change touched popup behavior:

```text
open/close popup repeatedly
click outside
select every action
verify keyboard/focus remains responsive
```

If it touched reports/history:

```text
load latest report
open historical summary
open historical full log
copy all
copy selection
```

If it touched deployment workflows:

```text
real DRY smoke first
then LIVE review/execution smoke only after DRY passes
```

## Detecting stale executable mistakes

When the UI appears unchanged after a known source change, verify:

1. Get/pull actually fetched the commit;
2. publish completed after the pull;
3. launch path is exactly `ZDeployPet_Releases\current\ZDeployPet.exe`;
4. compare the printed SHA-256 with the latest publish output;
5. ensure an old single-instance process is not being reactivated instead of the new binary.

This last item matters because ZDeployPet intentionally has a single-instance activation mechanism.

## Useful diagnostic commands

Build only:

```powershell
dotnet build ZDeployPet.sln -c Release
```

Tests:

```powershell
dotnet test ZDeployPet.sln -c Release
```

Publish:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\publish-local.ps1
```

Check process if the wrong UI seems to reopen:

```powershell
Get-Process ZDeployPet -ErrorAction SilentlyContinue
```

## Acceptance rule

A fix is accepted only when:

```text
Release tests pass
+
publish succeeds
+
published executable launches
+
the exact original symptom cannot be reproduced
```

## Prevention rule

For WPF, never report a visual/lifecycle bug as resolved from compilation alone. Require a published-runtime smoke against the original reproduction steps.
