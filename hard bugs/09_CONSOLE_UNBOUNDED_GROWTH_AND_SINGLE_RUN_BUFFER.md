# Hard bug: console accumulation caused slowdown risk

## Initial symptom

The Dry Run and Live deploy consoles accumulated more and more historical output in the right pane. After enough runs, the console became a candidate for UI sluggishness or apparent hanging.

This was unnecessary because deployment history and report artifacts were already persisted elsewhere.

## Product insight

The live console has a different job from deployment history.

The console should answer:

```text
What is happening in the current/latest run?
```

History should answer:

```text
What happened in previous runs?
```

Trying to make the console do both created duplication and unbounded text growth.

## Correct behavior

Each execution channel keeps only one current/latest run:

```text
Dry Run tab    → one dry run
Live deploy tab → one live deployment
ZDeployPet tab → bounded application/session activity
Reports tab    → one selected summary/full report at a time
```

Historical runs stay in trusted report history.

## Engineering pattern

Before starting a new Dry run:

```csharp
_dryRunLines.Clear();
RefreshDryRunConsole();
```

Then append only lines belonging to the new run:

```csharp
void AppendDryRunLine(string line)
{
    _dryRunLines.Add(line);
    DryRunConsoleTextBox.Text = string.Join(Environment.NewLine, _dryRunLines);
    DryRunConsoleTextBox.ScrollToEnd();
}
```

Same principle for Live:

```csharp
_liveDeployLines.Clear();
```

For the ZDeployPet application log, use a bounded collection rather than an infinite one:

```csharp
const int MaxActivityLines = 500;

_activityLines.Add(line);
while (_activityLines.Count > MaxActivityLines)
    _activityLines.RemoveAt(0);
```

The exact bound can be tuned, but the important property is finite memory/rendering cost.

## Why TextBox/RichTextBox growth matters

Large WPF text surfaces can become expensive because every update may involve:

- string allocation;
- layout;
- selection bookkeeping;
- scroll extent recalculation;
- visual invalidation.

Appending thousands of historical lines to a control the user only needs for the latest run wastes UI resources.

## Verification

Run this stress smoke:

```text
DRY × 10
LIVE × 5
switch tabs repeatedly
open/copy console text
open report history
```

Expected result:

- Dry Run shows only latest Dry run;
- Live deploy shows only latest Live run;
- historical entries remain available through report history;
- UI remains responsive;
- Copy all operates on the visible/current channel only.

## Prevention rule

Never use a live WPF console as durable history. Keep live channels bounded and persist durable evidence in report artifacts/history instead.
