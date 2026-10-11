# Hard UI bug: refresh glyph and active navigation state looked correct in code but not in runtime

## Initial symptom

The requested polish was simple:

```text
Refresh text button → bare counterclockwise glyph
report i → top-right after refresh
Home/Profile red accent → only current page
```

An initial implementation compiled but the screenshot still showed:

- `Refresh` as a rectangular text button;
- the `i` in the old location;
- both Home and Profile icons carrying the red accent.

## Root cause

There were two interacting issues:

1. the runtime polish pass executed before the target controls existed;
2. active navigation state was being treated as static button decoration instead of navigation state.

## Correct refresh control design

Refresh is an action glyph, not a card/button. Its normal chrome should be transparent:

```csharp
Button refresh = new()
{
    Width = 28,
    Height = 28,
    Background = Brushes.Transparent,
    BorderBrush = Brushes.Transparent,
    BorderThickness = new Thickness(0),
    Padding = new Thickness(4),
    Content = BuildRefreshGlyph()
};
```

Vector glyph:

```csharp
System.Windows.Shapes.Path glyph = new()
{
    Data = Geometry.Parse("M 15,5 A 7,7 0 1 0 16,12 M 15,5 L 15,10 L 10,5"),
    Stroke = mutedBrush,
    StrokeThickness = 1.7,
    Fill = Brushes.Transparent
};
```

Hover should affect the glyph, not create a large rectangular button:

```csharp
refresh.MouseEnter += (_, _) => glyph.Stroke = dryBlueAccent;
refresh.MouseLeave += (_, _) => glyph.Stroke = mutedBrush;
```

Long term, a WPF `ControlTemplate`/trigger is preferable to event-based brush mutation.

## Correct top-right tool order

The card owns one compact tool row:

```text
↻   i
```

not independent elements positioned with ad hoc margins.

Example:

```csharp
StackPanel tools = new()
{
    Orientation = Orientation.Horizontal,
    HorizontalAlignment = HorizontalAlignment.Right
};

tools.Children.Add(refresh);
tools.Children.Add(infoButton);
```

## Navigation state should come from current surface

Wrong model:

```text
Home button has red border because it is a primary button.
Profile button has red border because it is a primary button.
```

Correct model:

```text
Home red only while Home is active.
Profile red only while Profile is active.
```

Use one refresh method:

```csharp
private void RefreshNavigationSelection(bool profileVisible)
{
    ApplyNavState(_homeToolbarButton, active: !profileVisible);
    ApplyNavState(_profileToolbarButton, active: profileVisible);
}
```

Then call it from the actual surface transitions:

```csharp
ShowHomeSurface();
ShowProfileSurface();
```

## Verification

1. start on Home — only Home accented;
2. switch to Profile — accent moves to Profile;
3. switch back — accent returns to Home;
4. refresh glyph has no normal rectangle/background;
5. pointer hover changes glyph toward DRY blue;
6. `i` remains immediately after refresh at card top-right.

## Prevention rule

Do not encode navigation selection into static button creation. Selection is state and must be refreshed from the active surface.
