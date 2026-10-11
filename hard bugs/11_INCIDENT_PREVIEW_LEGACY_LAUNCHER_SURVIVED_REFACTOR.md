# Hard bug: Incident Preview moved to Help, but the old footer button survived

## Initial symptom

The Incident Bundle Preview feature was intentionally moved from Home into:

```text
Help → Incident bundle preview…
```

However, after a successful build and publish, the old Home footer still showed:

```text
Incident preview…   i
```

The Help menu initially showed only `About ZDeployPet…`, so the refactor had not actually produced the intended single entry point.

## Why this was difficult

ZDeployPet had accumulated shell initialization in multiple partial classes and runtime setup paths. Removing one creation site did not guarantee that another legacy path was not still creating or retaining the old controls.

The visual symptom looked like a failed commit, but the deeper problem was lifecycle duplication:

```text
new Help menu path exists
+
legacy footer path still initializes
=
feature has two launchers
```

## Engineering requirement

The feature needed one authoritative launcher:

```text
Help → Incident bundle preview…
```

and the popup itself needed its own contextual `i` help in the top-right area.

## Final fix strategy

### 1. Add the new authoritative Help action

```csharp
panel.Children.Add(CreatePopupAction(
    "Incident bundle preview…",
    popup,
    () => IncidentPreview_Click(this, new RoutedEventArgs())));
```

### 2. Remove the footer creation path

Do not merely hide the old control in one visual state. Remove or collapse its initialization path.

### 3. Add defensive cleanup

Because older shell paths could still have created the button earlier in startup, initialization also explicitly removed/hid any surviving legacy Incident Preview control and its adjacent info button.

The key lesson was that a refactor from one navigation location to another may require both:

```text
stop future creation
+
clean already-created legacy controls
```

### 4. Move contextual help into the popup

The old footer `i` no longer had a reason to exist. Its content moved into the Incident Bundle Preview window itself, beside the surface it explains.

## Verification

After publish:

1. Home has no `Incident preview…` footer button;
2. Home has no orphaned adjacent `i`;
3. Help menu contains `Incident bundle preview…`;
4. clicking it opens the existing sanitized bundle window;
5. popup has its own `i` help;
6. closing and reopening does not recreate the old footer control.

## Prevention rule

When relocating a feature in a dynamically-built WPF shell, search for both the event handler **and every control-construction path**. A feature can have only one intended navigation owner.
