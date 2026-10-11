# Hard bug: Incident Bundle export failed to compile because `Path`, `Directory`, and `File` were unresolved

## Initial symptom

After adding local export support to the Incident Bundle Preview window, Release build failed with eight errors such as:

```text
error CS0103: The name 'Path' does not exist in the current context
error CS0103: The name 'Directory' does not exist in the current context
error CS0103: The name 'File' does not exist in the current context
```

All failures were in `IncidentBundlePreviewWindow.cs` around the new export code.

## Root cause

The feature used filesystem APIs but the file did not have `System.IO` in scope.

The new code needed operations such as:

```csharp
Path.Combine(...)
Directory.CreateDirectory(...)
File.WriteAllText(...)
File.Exists(...)
```

Without:

```csharp
using System.IO;
```

those type names were unknown.

## Why this later became related to another hard bug

Once `System.IO` was imported, files that also used WPF vector `Path` could become ambiguous:

```text
System.IO.Path
vs
System.Windows.Shapes.Path
```

So there are two separate lessons:

1. filesystem code needs `System.IO`;
2. mixed UI/filesystem files should qualify WPF `Path` explicitly.

## Correct export pattern

```csharp
using System.IO;

string downloads = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    "Downloads");

Directory.CreateDirectory(downloads);

string filename = $"zdeploypet-incident-{safeDeploymentId}.txt";
string outputPath = Path.Combine(downloads, filename);

File.WriteAllText(outputPath, sanitizedBundle, Encoding.UTF8);
```

If the same source file also draws vector paths:

```csharp
System.Windows.Shapes.Path refreshGlyph = new();
```

## Security requirement

The filesystem fix did not change the Incident Bundle safety model. Export still had to occur only after generating the sanitized preview and had to preserve:

```text
DIAGNOSTIC EVIDENCE ONLY
no credentials
no SSH-agent capability
no deployment authorization
redaction/truncation metadata
operator omissions
```

## Verification

```powershell
cd C:\Dev\ZDeployPet
dotnet test ZDeployPet.sln -c Release
powershell -ExecutionPolicy Bypass -File scripts\publish-local.ps1
```

Runtime:

1. Help → Incident bundle preview;
2. build sanitized preview;
3. export;
4. verify file exists at the displayed path;
5. open file and compare it to preview;
6. verify no raw private-key/passphrase material exists.

## Prevention rule

When adding filesystem work to a WPF-heavy partial class, audit namespace collisions immediately after importing `System.IO`.
