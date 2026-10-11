# Hard bug: `Path` ambiguity between `System.IO` and WPF Shapes

## Initial symptom

A Release build failed after UI-polish work introduced vector icons:

```text
error CS0104: 'Path' is an ambiguous reference between
'System.Windows.Shapes.Path' and 'System.IO.Path'
```

This appeared in files such as:

```text
MainWindow.Pda3Menu.cs
MainWindow.UiPolishRuntime.cs
```

The confusing part was that both meanings of `Path` were legitimate in the same partial `MainWindow` type. Deployment code needed file-system paths while the UI code needed WPF vector paths.

## Why it happened

ZDeployPet uses a partial `MainWindow` split across multiple `.cs` files. Namespaces imported in one file can create collisions in another when common type names are used.

Two classes named `Path` were in scope:

```csharp
System.IO.Path
System.Windows.Shapes.Path
```

WPF icon code such as this became ambiguous:

```csharp
Path icon = new()
{
    Data = Geometry.Parse("M 2,8 L 9,2 L 16,8")
};
```

The compiler could not infer whether `Path` meant a file-system helper or a vector shape.

## Failed/fragile approach

Adding more `using` statements does not solve this reliably. An alias can work, but it is easy for later partial-class files to recreate the collision.

## Final solution

Use the fully-qualified WPF type at the construction site:

```csharp
System.Windows.Shapes.Path icon = new()
{
    Data = Geometry.Parse("M 2,8 L 9,2 L 16,8"),
    Stroke = AppText,
    StrokeThickness = 1.7,
    Fill = Brushes.Transparent
};
```

When file-system work is performed in the same file, keep `System.IO.Path` explicit as well when clarity matters:

```csharp
string filename = System.IO.Path.GetFileName(sourcePath);
```

## Why this is the preferred fix

It is local, deterministic, and survives future `using` changes. UI icon code is also easier to read because the type makes its purpose obvious.

## Verification

```powershell
cd C:\Dev\ZDeployPet
dotnet test ZDeployPet.sln -c Release
powershell -ExecutionPolicy Bypass -File scripts\publish-local.ps1
```

Then launch the published executable and verify that the vector icon actually renders. Compilation alone only proves that the ambiguity is gone.

## Prevention rule

In WPF files that also touch paths on disk, do not use bare `Path` for shapes. Prefer:

```csharp
System.Windows.Shapes.Path
```

for vector graphics and explicit `System.IO.Path` where needed for filesystem operations.
