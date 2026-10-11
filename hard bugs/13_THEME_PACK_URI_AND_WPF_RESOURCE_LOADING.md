# Hard bug: WPF theme pack URI used the wrong assembly identity

## Initial symptom

PDA-8 V1 introduced a dedicated resource dictionary:

```text
src/ZDeployPet.App/Themes/Pda8VisualTheme.xaml
```

The first startup loader referenced the project/root namespace rather than the actual assembly name. This is a dangerous class of bug because XAML resource URI mistakes can survive source review and then fail only when the application starts and tries to resolve the dictionary.

## Important distinction

These identifiers were not the same thing:

```text
Project path / namespace: ZDeployPet.App
AssemblyName:             ZDeployPet
```

The `.csproj` explicitly defines:

```xml
<AssemblyName>ZDeployPet</AssemblyName>
<RootNamespace>ZDeployPet.App</RootNamespace>
```

WPF pack URIs use the **assembly identity**, not the C# root namespace.

## Wrong form

```csharp
new Uri(
    "pack://application:,,,/ZDeployPet.App;component/Themes/Pda8VisualTheme.xaml",
    UriKind.Absolute)
```

## Correct form

```csharp
new Uri(
    "pack://application:,,,/ZDeployPet;component/Themes/Pda8VisualTheme.xaml",
    UriKind.Absolute)
```

Then:

```csharp
private void LoadPda8VisualTheme()
{
    ResourceDictionary theme = new()
    {
        Source = new Uri(
            "pack://application:,,,/ZDeployPet;component/Themes/Pda8VisualTheme.xaml",
            UriKind.Absolute)
    };

    Resources.MergedDictionaries.Add(theme);
}
```

## Why the theme is loaded before shell construction

Controls created in code-behind immediately query resources such as:

```csharp
Application.Current.Resources["AppSurfaceBrush"]
```

Therefore the redesign dictionary must be available before those controls are constructed or styled.

Startup ordering:

```text
single-instance gate
→ load PDA-8 theme dictionary
→ register handlers
→ create/show MainWindow
```

## Another trap: resource key collisions

V1 intentionally introduced named styles and accepted palette tokens incrementally rather than immediately replacing every implicit app style. This allowed later phases to opt surfaces into the redesign without destabilizing the working deployment UI.

When two merged dictionaries define the same key, merge order matters. The later resource can replace the earlier lookup result.

## Verification

### Compile

```powershell
dotnet test ZDeployPet.sln -c Release
```

### Startup

```powershell
powershell -ExecutionPolicy Bypass -File scripts\publish-local.ps1
C:\Dev\ZDeployPet_Releases\current\ZDeployPet.exe
```

Watch for startup exceptions similar to:

```text
Cannot locate resource 'themes/pda8visualtheme.xaml'
```

or XAML parse/resource-resolution failures.

## Prevention rule

For WPF pack URIs, inspect `<AssemblyName>` in the project file. Do not infer the assembly name from the root namespace or project folder name.
