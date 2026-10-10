using System.IO;
using System.Text.Json;

namespace ZDeployPet.App;

internal sealed record UiLayoutSnapshot(
    int SchemaVersion,
    double MainWidth,
    double MainHeight,
    double MainLeft,
    double MainTop,
    bool MainMaximized,
    double ConsolePaneWidth,
    bool ConsolePaneVisible)
{
    public const int CurrentSchemaVersion = 1;
}

internal sealed class UiLayoutStore
{
    private readonly string _path;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public UiLayoutStore(string? root = null)
    {
        string storageRoot = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Zomniverse", "ZDeployPet");
        _path = Path.Combine(storageRoot, "ui-layout.json");
    }

    public UiLayoutSnapshot? Load()
    {
        try
        {
            if (!File.Exists(_path)) return null;
            string json = File.ReadAllText(_path);
            UiLayoutSnapshot? snapshot = JsonSerializer.Deserialize<UiLayoutSnapshot>(json, JsonOptions);
            return snapshot?.SchemaVersion == UiLayoutSnapshot.CurrentSchemaVersion ? snapshot : null;
        }
        catch
        {
            // UI layout memory must never stop ZDeployPet from opening.
            return null;
        }
    }

    public void Save(UiLayoutSnapshot snapshot)
    {
        string? directory = Path.GetDirectoryName(_path);
        if (string.IsNullOrWhiteSpace(directory)) return;

        Directory.CreateDirectory(directory);
        string temporaryPath = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            string json = JsonSerializer.Serialize(snapshot, JsonOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _path, true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }
}
