# Hard bug: report inspection fragmented across popup windows

## Initial symptom

Every Summary or Full log action opened another window. Functionally it worked, but the UI became increasingly modal and cluttered:

```text
Home
→ click Summary
→ Deployment summary popup
→ close
→ click another Full log
→ another popup
```

The application already had a large right-side output pane with Copy all and Copy selection, so opening a separate window for every report duplicated interaction patterns.

## Product problem

Report artifacts are read-only inspection content. They do not need isolated authority or modal lifetime.

The popup model created several issues:

- excessive window count;
- focus jumping;
- duplicated copy controls;
- more code paths for sizing and dark theme;
- harder keyboard navigation;
- loss of context between selected history row and opened artifact.

## Final architecture

The right pane became four channels:

```text
Dry Run
Live deploy
ZDeployPet
Reports
```

`Reports` is a shared viewer. Selecting Summary or Full log replaces the previous report content and focuses the Reports tab.

That gives one simple invariant:

```text
one report viewer, one selected artifact at a time
```

## Routing pattern

Instead of:

```csharp
DeploymentSummaryWindow window = new(...);
window.Show();
```

use a shared report selection method:

```csharp
private void ShowReport(string text, ReportMetadata metadata)
{
    _reportsConsoleText = text;
    _reportsMetadata = metadata;
    SelectActivityTab(ActivityTab.Reports);
    RefreshEmbeddedActivityConsole();
}
```

History button:

```csharp
summaryButton.Click += (_, _) =>
{
    ShowReport(report.SummaryText, ReportMetadata.ForSummary(report));
};
```

Full log:

```csharp
fullLogButton.Click += (_, _) =>
{
    ShowReport(report.FullLogText, ReportMetadata.ForFullLog(report));
};
```

## Why this is safer too

A single report surface makes it easier to enforce read-only behavior consistently. There is one place to apply:

- copy behavior;
- sanitization labels;
- trusted artifact metadata;
- no-edit guarantee;
- text truncation rules if required later.

## History remains separate from the live viewer

The left-side REPORTS workspace remains the deliberate historical browser. The right-side Reports channel is the fast current selection.

This separation became:

```text
left REPORTS workspace = browse/history
right Reports console  = inspect selected artifact
```

## Verification

1. click latest Summary;
2. right pane switches to Reports;
3. content is visible and copyable;
4. click a historical Full log;
5. same Reports surface updates in place;
6. no new top-level WPF window appears;
7. return to Dry Run/Live/ZDeployPet tabs without losing app responsiveness.

## Prevention rule

Read-only inspection content should reuse a shared in-app surface unless there is a strong isolation/security reason for a new top-level window.
