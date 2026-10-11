# Hard bug: build succeeded but UI polish changed nothing

## Initial symptom

A UI-polish build compiled and published successfully, but the application looked unchanged:

- Refresh was still a text button instead of a bare counterclockwise glyph.
- the report `i` was still in the old position;
- Home and Profile navigation both still showed the red accent instead of only the active page.

The dangerous part was that there were no compiler errors and no runtime exception.

## Root cause

The polish layer executed before the controls it wanted to modify had been created.

The sequence was effectively:

```text
MainWindow construction
→ runtime polish attempts to find controls
→ controls not present yet
→ polish marks itself complete
→ shell/report/navigation controls are created later
→ no second attempt
```

The code was logically correct but ran at the wrong lifecycle point.

## Why this is a hard class of WPF bug

WPF UI can be constructed through several mechanisms:

- XAML initialization;
- code-behind creation;
- `Loaded` callbacks;
- `Dispatcher.BeginInvoke` work;
- later feature initialization methods;
- template expansion.

A control existing in source code does not mean it exists in the visual tree at the moment a runtime styling pass executes.

## Diagnostic clue

When all of these are true:

```text
build passes
app launches
no exceptions
requested cosmetic change is completely absent
```

suspect lifecycle/timing before suspecting the brush/template values themselves.

## Final solution pattern

Do not mark a polish operation complete until the owned controls actually exist.

Example:

```csharp
private bool _uiPolishApplied;

private void TryApplyUiPolish()
{
    if (_uiPolishApplied) return;

    if (_homeToolbarButton is null ||
        _profileToolbarButton is null ||
        _latestReportRefreshButton is null)
    {
        Dispatcher.BeginInvoke(new Action(TryApplyUiPolish));
        return;
    }

    ApplyNavigationStateStyling();
    ReplaceRefreshChrome();
    MoveReportInfoButton();

    _uiPolishApplied = true;
}
```

An even better long-term design is ownership-based styling: the method that creates the control should assign its style immediately, rather than requiring a global search-and-patch pass.

## Why direct ownership is better

Instead of:

```text
create controls
→ later search tree
→ identify controls by text/name
→ mutate them
```

prefer:

```text
create control
→ assign intended style/template at creation
```

The PDA-8 redesign later moved toward this resource/style architecture specifically to avoid repeated timing shims.

## Verification

A successful test must verify visible state, not only build state:

1. launch published executable;
2. inspect Home icon and Profile icon active states;
3. switch Home → Profile → Home;
4. verify only current page receives accent;
5. verify refresh is rendered as glyph, not legacy text button;
6. restart app and repeat to catch startup-order dependency.

## Prevention rule

Never set a `bool _applied = true` merely because a runtime styling method ran. Set it only after every required owned control was found and updated.
