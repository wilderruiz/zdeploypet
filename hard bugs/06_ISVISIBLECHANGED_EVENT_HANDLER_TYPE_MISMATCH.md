# Hard bug: `IsVisibleChanged` used the wrong event-handler type

## Initial symptom

A UI polish build failed with:

```text
error CS0029: Cannot implicitly convert type
'System.Windows.RoutedEventHandler'
to
'System.Windows.DependencyPropertyChangedEventHandler'
```

Two lines failed because handlers intended for `IsVisibleChanged` were declared as ordinary routed-event handlers.

## Root cause

`FrameworkElement.IsVisibleChanged` is not a routed event. It is a dependency-property change notification and therefore uses:

```csharp
DependencyPropertyChangedEventHandler
```

not:

```csharp
RoutedEventHandler
```

This distinction is easy to miss because many WPF events such as `Loaded`, `Click`, and other UI interactions do use routed event types.

## Broken pattern

```csharp
RoutedEventHandler handler = (_, _) => RefreshSomething();
element.IsVisibleChanged += handler;
```

## Correct pattern

```csharp
DependencyPropertyChangedEventHandler handler = (_, _) => RefreshSomething();
element.IsVisibleChanged += handler;
```

or directly:

```csharp
element.IsVisibleChanged += (_, _) => RefreshSomething();
```

when no later unsubscription is required.

## Engineering lesson

WPF has several event families:

```text
RoutedEventHandler
DependencyPropertyChangedEventHandler
PropertyChangedEventHandler
MouseButtonEventHandler
KeyEventHandler
SelectionChangedEventHandler
```

Do not generalize from `Loaded`/`Click` to property-change events.

## When named handler variables matter

If the handler must later be removed, declare it with the exact event delegate type:

```csharp
private readonly DependencyPropertyChangedEventHandler _visibilityHandler;
```

This prevents accidental implicit conversions and makes lifecycle cleanup explicit.

## Verification

```powershell
cd C:\Dev\ZDeployPet
dotnet test ZDeployPet.sln -c Release
powershell -ExecutionPolicy Bypass -File scripts\publish-local.ps1
```

Then switch surfaces repeatedly so `IsVisibleChanged` actually fires and verify the callback does not recurse or run against disposed/re-parented controls.

## Prevention rule

Before manually typing an event delegate type in WPF, inspect the event declaration or let the compiler infer the lambda. Property-change events are frequently not routed events.
