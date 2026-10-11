using System.Text;
using System.Text.RegularExpressions;

namespace ZDeployPet.Core;

public enum IncidentEvidenceKind
{
    ApplicationActivity,
    DryRunConsole,
    LiveDeployConsole,
    DeploymentSummary,
    DeploymentFullLog,
    DeploymentJson,
    EnvironmentSummary
}

public sealed record IncidentEvidenceInput(
    IncidentEvidenceKind Kind,
    string Label,
    string Content);

public sealed record SanitizedIncidentEvidence(
    IncidentEvidenceKind Kind,
    string Label,
    string Content,
    bool RedactionApplied,
    bool Truncated);

public sealed record IncidentBundleBuildResult(
    bool Success,
    IReadOnlyList<SanitizedIncidentEvidence> Evidence,
    IReadOnlyList<string> Errors,
    int TotalCharacters);

public static class IncidentBundleLimits
{
    public const int MaximumItems = 20;
    public const int MaximumLabelCharacters = 120;
    public const int MaximumInputCharactersPerItem = 512 * 1024;
    public const int MaximumSanitizedCharactersPerItem = 256 * 1024;
    public const int MaximumBundleCharacters = 2 * 1024 * 1024;
}

public static partial class IncidentEvidenceSanitizer
{
    private const string Redacted = "[REDACTED]";
    private const string PrivateKeyRedacted = "[PRIVATE KEY REDACTED]";
    private const string AgentSocketRedacted = "[SSH AGENT SOCKET REDACTED]";
    private const string PrivateKeyPathRedacted = "[SSH PRIVATE KEY PATH REDACTED]";

    public static IncidentBundleBuildResult Build(IEnumerable<IncidentEvidenceInput>? inputs)
    {
        if (inputs is null)
            return Failed("Incident evidence is required.");

        IncidentEvidenceInput[] items = inputs.ToArray();
        List<string> errors = [];
        if (items.Length == 0)
            errors.Add("At least one incident-evidence item is required.");
        if (items.Length > IncidentBundleLimits.MaximumItems)
            errors.Add($"Incident bundles are limited to {IncidentBundleLimits.MaximumItems} evidence items.");

        List<SanitizedIncidentEvidence> sanitized = [];
        int totalCharacters = 0;

        foreach (IncidentEvidenceInput item in items.Take(IncidentBundleLimits.MaximumItems))
        {
            if (!Enum.IsDefined(item.Kind))
            {
                errors.Add($"Unsupported incident-evidence kind: {(int)item.Kind}.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.Label))
            {
                errors.Add("Every incident-evidence item requires a non-empty label.");
                continue;
            }

            string label = item.Label.Trim();
            if (label.Length > IncidentBundleLimits.MaximumLabelCharacters || ContainsControlCharacters(label))
            {
                errors.Add($"Incident-evidence label '{SafeLabelPreview(label)}' is invalid or too long.");
                continue;
            }

            if (item.Content is null)
            {
                errors.Add($"Incident-evidence item '{label}' has no content.");
                continue;
            }

            if (item.Content.Length > IncidentBundleLimits.MaximumInputCharactersPerItem)
            {
                errors.Add($"Incident-evidence item '{label}' exceeds the {IncidentBundleLimits.MaximumInputCharactersPerItem}-character input limit.");
                continue;
            }

            SanitizationResult clean = Sanitize(item.Content);
            bool truncated = false;
            string bounded = clean.Content;
            if (bounded.Length > IncidentBundleLimits.MaximumSanitizedCharactersPerItem)
            {
                bounded = bounded[..IncidentBundleLimits.MaximumSanitizedCharactersPerItem] + "\n[ZDeployPet evidence truncated]\n";
                truncated = true;
            }

            if (totalCharacters + bounded.Length > IncidentBundleLimits.MaximumBundleCharacters)
            {
                errors.Add($"Incident bundle exceeds the {IncidentBundleLimits.MaximumBundleCharacters}-character total limit.");
                break;
            }

            totalCharacters += bounded.Length;
            sanitized.Add(new SanitizedIncidentEvidence(item.Kind, label, bounded, clean.RedactionApplied, truncated));
        }

        return new IncidentBundleBuildResult(errors.Count == 0, sanitized, errors, totalCharacters);
    }

    public static SanitizationResult Sanitize(string value)
    {
        if (value is null)
            return new SanitizationResult(string.Empty, false);

        string sanitized = value.Replace("\0", string.Empty, StringComparison.Ordinal);
        bool changed = !string.Equals(sanitized, value, StringComparison.Ordinal);

        sanitized = ReplaceAndTrack(PrivateKeyBlockRegex(), sanitized, PrivateKeyRedacted, ref changed);
        sanitized = ReplaceAndTrack(BearerRegex(), sanitized, "$1" + Redacted, ref changed);
        sanitized = ReplaceAndTrack(SecretAssignmentRegex(), sanitized, "$1$2" + Redacted, ref changed);
        sanitized = ReplaceAndTrack(SshAgentAssignmentRegex(), sanitized, "$1" + AgentSocketRedacted, ref changed);
        sanitized = ReplaceAndTrack(SshAgentSocketPathRegex(), sanitized, AgentSocketRedacted, ref changed);
        sanitized = ReplaceAndTrack(PrivateKeyPathRegex(), sanitized, PrivateKeyPathRedacted, ref changed);
        sanitized = ReplaceAndTrack(WindowsPrivateKeyPathRegex(), sanitized, PrivateKeyPathRedacted, ref changed);

        return new SanitizationResult(sanitized, changed);
    }

    private static string ReplaceAndTrack(Regex regex, string input, string replacement, ref bool changed)
    {
        string output = regex.Replace(input, replacement);
        if (!string.Equals(output, input, StringComparison.Ordinal))
            changed = true;
        return output;
    }

    private static bool ContainsControlCharacters(string value)
        => value.Any(character => char.IsControl(character));

    private static string SafeLabelPreview(string value)
    {
        StringBuilder preview = new();
        foreach (char character in value.Take(40))
            preview.Append(char.IsControl(character) ? '?' : character);
        return preview.ToString();
    }

    private static IncidentBundleBuildResult Failed(string error)
        => new(false, Array.Empty<SanitizedIncidentEvidence>(), [error], 0);

    public sealed record SanitizationResult(string Content, bool RedactionApplied);

    [GeneratedRegex("-----BEGIN (?:OPENSSH |RSA |EC |DSA )?PRIVATE KEY-----[\\s\\S]*?-----END (?:OPENSSH |RSA |EC |DSA )?PRIVATE KEY-----", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PrivateKeyBlockRegex();

    [GeneratedRegex("(?i)(\\bAuthorization\\s*:\\s*Bearer\\s+)[^\\s,;]+", RegexOptions.CultureInvariant)]
    private static partial Regex BearerRegex();

    [GeneratedRegex("(?i)(\\b(?:password|passphrase|token|secret|api[_-]?key|access[_-]?key)\\b\\s*[:=]\\s*)([\\\"']?)[^\\s,;\\\"']+", RegexOptions.CultureInvariant)]
    private static partial Regex SecretAssignmentRegex();

    [GeneratedRegex("(?i)(\\bSSH_AUTH_SOCK\\s*=\\s*)[^\\s,;]+", RegexOptions.CultureInvariant)]
    private static partial Regex SshAgentAssignmentRegex();

    [GeneratedRegex("/(?:tmp|run/user/\\d+)/ssh-[^/\\s]+/agent\\.\\d+", RegexOptions.CultureInvariant)]
    private static partial Regex SshAgentSocketPathRegex();

    [GeneratedRegex("(?:~|/home/[^/\\s]+|/root)/\\.ssh/(?:id_[A-Za-z0-9_.-]+|[^/\\s]+\\.(?:pem|key))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PrivateKeyPathRegex();

    [GeneratedRegex(@"(?:[A-Za-z]:\\Users\\[^\\\s]+|%USERPROFILE%)\\\.ssh\\(?:id_[A-Za-z0-9_.-]+|[^\\\s]+\.(?:pem|key))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WindowsPrivateKeyPathRegex();
}
