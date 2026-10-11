# Hard bug: re-parenting WPF controls without `UIElement.Parent`

## Initial symptom

The first Home workspace implementation attempted to move existing report/action controls into a new DRY | LIVE | REPORTS host. Release compilation failed with:

```text
error CS1061: 'UIElement' does not contain a definition for 'Parent'
```

The failure was reported from `MainWindow.Pda8HomeWorkspace.cs`.

## Engineering goal

The redesign intentionally reused existing controls instead of recreating them. That required detaching a control from its current parent before adding it to a new `Panel`, `Border`, or `ContentControl`.

The tempting code was:

```csharp
UIElement element = ...;
var parent = element.Parent;
```

That does not compile because `Parent` is not declared by `UIElement`.

## First attempted correction

Casting through `FrameworkElement` can work in many cases:

```csharp
if (element is FrameworkElement frameworkElement)
{
    DependencyObject? parent = frameworkElement.Parent;
}
```

But the Home workspace needed a helper that was robust across WPF logical and visual ownership, not only the common `FrameworkElement.Parent` path.

## Final solution

The final helper stopped using `.Parent` directly and resolved ownership through WPF tree APIs:

```csharp
DependencyObject? parent = LogicalTreeHelper.GetParent(element);

if (parent is null)
{
    try
    {
        parent = VisualTreeHelper.GetParent(element);
    }
    catch (InvalidOperationException)
    {
        parent = null;
    }
}
```

Then detach according to the real container type:

```csharp
switch (parent)
{
    case Panel panel:
        panel.Children.Remove(element);
        break;

    case Border border when ReferenceEquals(border.Child, element):
        border.Child = null;
        break;

    case ContentControl contentControl when ReferenceEquals(contentControl.Content, element):
        contentControl.Content = null;
        break;

    case Decorator decorator when ReferenceEquals(decorator.Child, element):
        decorator.Child = null;
        break;
}
```

## Why both logical and visual trees matter

WPF has two overlapping ownership models:

- the logical tree expresses content relationships;
- the visual tree expresses rendered ownership.

A control may be discoverable through one but not the other, especially around templates, decorators and content hosts.

## Important WPF rule

A WPF visual can have only one visual parent. Adding an already-parented control to another container throws at runtime even if compilation succeeds.

Therefore the safe sequence is always:

```text
resolve current parent
→ detach
→ attach to new host
```

## Verification

```powershell
cd C:\Dev\ZDeployPet
dotnet test ZDeployPet.sln -c Release
powershell -ExecutionPolicy Bypass -File scripts\publish-local.ps1
```

Runtime smoke:

1. open Home;
2. switch DRY → LIVE → REPORTS repeatedly;
3. switch Home → Profile → Home;
4. verify no `Specified element is already the logical child of another element` exception;
5. verify the controls keep their event handlers and state.

## Prevention rule

When moving existing WPF elements between containers, never assume `.Parent` exists on `UIElement`, and never attach before explicitly detaching from the logical/visual owner.
