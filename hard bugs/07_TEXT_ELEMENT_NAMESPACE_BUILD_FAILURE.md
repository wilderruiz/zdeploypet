# Hard bug: `TextElement` not found during popup styling

## Initial symptom

A build failed in `MainWindow.ProfileGitSafety.cs` with:

```text
error CS0103: The name 'TextElement' does not exist in the current context
```

The change was part of a dark-menu styling pass for the Git-safety popup.

## Root cause

`TextElement` is a WPF type in the `System.Windows.Documents` namespace. The file referenced it without importing or qualifying that namespace.

This is particularly easy to hit when dynamically styling popup descendants, because text inside controls may be represented by `TextBlock`, `Run`, `Span`, or other document/text elements depending on how the content was constructed.

## Fragile pattern

```csharp
if (child is TextElement textElement)
{
    textElement.Foreground = AppText;
}
```

If the namespace is not in scope, compilation fails.

## Correct options

### Option A — import the namespace

```csharp
using System.Windows.Documents;
```

then:

```csharp
if (child is TextElement textElement)
    textElement.Foreground = AppText;
```

### Option B — fully qualify it

```csharp
if (child is System.Windows.Documents.TextElement textElement)
    textElement.Foreground = AppText;
```

For one-off references in partial classes, explicit qualification is often safer.

## Better long-term design

The underlying need to recursively recolor arbitrary descendants was itself a sign that styling ownership was too late and too global. PDA-8 moved toward named WPF styles/resource dictionaries so controls inherit the correct foreground/background at creation time.

In other words, the immediate compiler fix is namespace qualification; the architectural fix is to reduce descendant-search styling.

## Verification

```powershell
cd C:\Dev\ZDeployPet
dotnet test ZDeployPet.sln -c Release
powershell -ExecutionPolicy Bypass -File scripts\publish-local.ps1
```

Then open the Git-safety menu and verify both enabled and disabled text remain readable on the dark popup surface.

## Prevention rule

When dynamic styling reaches into WPF document/text primitives, qualify types from `System.Windows.Documents` explicitly unless the file already has a clear reason to import that namespace.
