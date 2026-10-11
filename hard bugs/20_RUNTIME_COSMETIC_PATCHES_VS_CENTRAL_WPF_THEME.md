# Hard engineering lesson: repeated runtime cosmetic patches became fragile

## Initial problem

As ZDeployPet matured, many visual requests were implemented directly in code-behind:

```text
change a button background
move an i button
recolor a popup
replace Refresh with a glyph
change Home/Profile active border
change DRY/LIVE enabled colors
```

Each individual change was small, but together they created a fragile runtime-polish layer.

## Symptoms of the fragility

The project hit several failures associated with this pattern:

- styling code ran before controls existed;
- old visual state survived after navigation;
- popup descendants needed recursive recoloring;
- disabled default templates leaked gray/light artifacts;
- active navigation accents became static instead of state-driven;
- one-off event handlers accumulated;
- cosmetic code introduced compile errors such as `Path` ambiguity and wrong event delegate types.

## Why code-behind styling becomes expensive

A pattern like this looks harmless:

```csharp
button.Background = new SolidColorBrush(...);
button.BorderBrush = new SolidColorBrush(...);
button.MouseEnter += ...;
button.MouseLeave += ...;
```

But at scale it creates:

```text
visual policy distributed across partial classes
+
lifecycle dependency
+
manual hover/disabled state handling
+
harder consistency checks
```

The redesign therefore introduced a dedicated theme foundation.

## Architectural solution

Create a native WPF resource dictionary:

```text
src/ZDeployPet.App/Themes/Pda8VisualTheme.xaml
```

Centralize:

```text
palette tokens
semantic colors
radii
padding
button templates
icon-button templates
info-button template
DRY/LIVE/REPORTS styles
input styles
console styles
tooltip styles
```

Example token:

```xml
<SolidColorBrush x:Key="AppBackgroundBrush" Color="#080A0D" />
<SolidColorBrush x:Key="AppSurfaceBrush" Color="#11151B" />
<SolidColorBrush x:Key="AppSurfaceRaisedBrush" Color="#161B22" />
<SolidColorBrush x:Key="AppTextBrush" Color="#F4F1EA" />
<SolidColorBrush x:Key="AppMutedTextBrush" Color="#8E97A5" />
```

Reusable button style:

```xml
<Style x:Key="Pda8ButtonStyle" TargetType="Button">
    <Setter Property="Foreground" Value="{StaticResource AppTextBrush}" />
    <Setter Property="Background" Value="{StaticResource AppSurfaceRaisedBrush}" />
    <Setter Property="BorderBrush" Value="{StaticResource AppBorderStrongBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="Padding" Value="12,7" />
    <!-- custom template + hover/focus/disabled triggers -->
</Style>
```

## Why V1 intentionally did not repaint everything immediately

A full implicit-theme replacement in one commit would have had a large regression surface. The safer migration was:

```text
V1 define named tokens/styles
V2 shell opts in
V3 navigation opts in
V4 home/pet opts in
...
remove legacy runtime shims last
```

This is slower in number of phases but safer in actual engineering risk.

## Runtime styling still has legitimate uses

Not all code-behind styling is wrong. Dynamic state that genuinely depends on runtime data can still set:

```text
READY/LOCKED state
selected navigation item
current tab
PASS/FAIL status
```

But runtime code should select a semantic style/state, not manually reinvent the entire visual template.

## Verification strategy

For every visual migration phase:

1. build/test Release;
2. publish;
3. smoke only the owned surface;
4. compare with the HTML visual source of truth;
5. verify unaffected deployment logic;
6. delete old runtime shim only after the replacement is proven.

## Prevention rule

When more than one surface needs the same color, spacing, hover or disabled behavior, it belongs in a WPF resource/style—not another runtime patch.
