namespace ZDeployPet.Core;

public sealed record DeploymentKeyIdentity(
    string PublicKeyPath,
    string Fingerprint,
    string Algorithm,
    string? Comment = null);

public sealed record TargetHostIdentity(
    string TargetId,
    string Fingerprint,
    string KeyType);

public sealed record DeploymentAccessIdentity(
    int SchemaVersion,
    string ProfileId,
    DeploymentKeyIdentity? DeploymentKey,
    IReadOnlyList<TargetHostIdentity> HostKeys)
{
    public const int CurrentSchemaVersion = 1;

    public static DeploymentAccessIdentity Empty(string profileId) =>
        new(CurrentSchemaVersion, profileId, null, []);

    public TargetHostIdentity? FindHostKey(string targetId) =>
        HostKeys.FirstOrDefault(item =>
            string.Equals(item.TargetId, targetId, StringComparison.OrdinalIgnoreCase));
}

public enum SshProbeStatus
{
    Success,
    MissingKey,
    KeyMismatch,
    HostUnreachable,
    HostKeyNotEnrolled,
    HostKeyMismatch,
    AuthenticationRejected,
    InvalidTarget,
    Failed
}

public sealed record SshPublicKeyCandidate(
    string PublicKeyPath,
    string Fingerprint,
    string Algorithm,
    string? Comment = null)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Comment)
        ? $"{Algorithm} — {Fingerprint}"
        : $"{Comment} — {Fingerprint}";
}

public sealed record HostKeyScanResult(
    bool Success,
    string? Fingerprint,
    string? KeyType,
    string? HostKeyLine,
    string? Error = null);

public sealed record SshProbeResult(
    SshProbeStatus Status,
    string Message,
    string? ObservedHostFingerprint = null)
{
    public bool Success => Status == SshProbeStatus.Success;
}
