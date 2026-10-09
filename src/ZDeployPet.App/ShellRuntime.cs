using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public enum ShellActivityLevel
{
    Info,
    Success,
    Warning,
    Error
}

public sealed record ShellActivityEntry(
    DateTimeOffset TimestampUtc,
    ShellActivityLevel Level,
    string Category,
    string Message);

public sealed class ShellActivityLog
{
    private const int MaximumEntries = 500;
    private readonly object _sync = new();
    private readonly List<ShellActivityEntry> _entries = [];

    public event EventHandler<ShellActivityEntry>? EntryAdded;

    public IReadOnlyList<ShellActivityEntry> Snapshot()
    {
        lock (_sync) return _entries.ToArray();
    }

    public void Add(ShellActivityLevel level, string category, string message)
    {
        string sanitized = Sanitize(message);
        ShellActivityEntry entry = new(
            DateTimeOffset.UtcNow,
            level,
            string.IsNullOrWhiteSpace(category) ? "General" : category.Trim(),
            sanitized);

        lock (_sync)
        {
            _entries.Add(entry);
            if (_entries.Count > MaximumEntries)
                _entries.RemoveRange(0, _entries.Count - MaximumEntries);
        }

        EntryAdded?.Invoke(this, entry);
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        string sanitized = value
            .Replace("-----BEGIN OPENSSH PRIVATE KEY-----", "[PRIVATE KEY REDACTED]", StringComparison.Ordinal)
            .Replace("-----BEGIN PRIVATE KEY-----", "[PRIVATE KEY REDACTED]", StringComparison.Ordinal)
            .Replace("-----BEGIN RSA PRIVATE KEY-----", "[PRIVATE KEY REDACTED]", StringComparison.Ordinal);

        string[] secretLabels = ["password=", "passphrase=", "token=", "secret="];
        foreach (string label in secretLabels)
        {
            int start = 0;
            while (true)
            {
                int index = sanitized.IndexOf(label, start, StringComparison.OrdinalIgnoreCase);
                if (index < 0) break;

                int valueStart = index + label.Length;
                int valueEnd = sanitized.IndexOfAny([' ', '\t', '\r', '\n', ';', ','], valueStart);
                if (valueEnd < 0) valueEnd = sanitized.Length;
                sanitized = sanitized[..valueStart] + "[REDACTED]" + sanitized[valueEnd..];
                start = valueStart + "[REDACTED]".Length;
            }
        }

        return sanitized;
    }
}

public sealed class ShellStateStore : INotifyPropertyChanged
{
    private string _profileName = string.Empty;
    private DeploymentAccessSessionState _sessionState = DeploymentAccessSessionState.Locked;
    private string _sessionMessage = "Deployment access is locked.";
    private DateTimeOffset? _sessionExpiresAtUtc;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ProfileName
    {
        get => _profileName;
        private set => SetField(ref _profileName, value);
    }

    public DeploymentAccessSessionState SessionState
    {
        get => _sessionState;
        private set => SetField(ref _sessionState, value);
    }

    public string SessionMessage
    {
        get => _sessionMessage;
        private set => SetField(ref _sessionMessage, value);
    }

    public DateTimeOffset? SessionExpiresAtUtc
    {
        get => _sessionExpiresAtUtc;
        private set => SetField(ref _sessionExpiresAtUtc, value);
    }

    public void UpdateProfile(string? profileName)
    {
        ProfileName = profileName?.Trim() ?? string.Empty;
    }

    public void UpdateSession(
        DeploymentAccessSessionState state,
        string message,
        DateTimeOffset? expiresAtUtc)
    {
        SessionState = state;
        SessionMessage = message;
        SessionExpiresAtUtc = expiresAtUtc;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public static class ShellRuntime
{
    public static ShellActivityLog Activity { get; } = new();
    public static ShellStateStore State { get; } = new();
}
