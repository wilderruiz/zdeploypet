namespace ZDeployPet.Core;

public enum DeploymentAccessSessionState
{
    Locked,
    Ready,
    Expiring,
    Expired,
    Invalid
}

public sealed record DeploymentAccessSessionLease(
    string ProfileId,
    string WslDistribution,
    int AgentPid,
    string AgentSocketPath,
    string ApprovedKeyFingerprint,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset ExpiresAtUtc)
{
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(8);
    public static readonly TimeSpan DefaultExpiringWindow = TimeSpan.FromMinutes(30);

    public DeploymentAccessSessionState Evaluate(
        DateTimeOffset nowUtc,
        bool agentReachable,
        bool approvedKeyLoaded,
        TimeSpan? expiringWindow = null)
    {
        if (AgentPid <= 0 || string.IsNullOrWhiteSpace(AgentSocketPath) ||
            string.IsNullOrWhiteSpace(ApprovedKeyFingerprint))
            return DeploymentAccessSessionState.Invalid;

        if (!agentReachable || !approvedKeyLoaded)
            return DeploymentAccessSessionState.Locked;

        if (nowUtc >= ExpiresAtUtc)
            return DeploymentAccessSessionState.Expired;

        TimeSpan window = expiringWindow ?? DefaultExpiringWindow;
        if (window < TimeSpan.Zero) window = TimeSpan.Zero;

        return ExpiresAtUtc - nowUtc <= window
            ? DeploymentAccessSessionState.Expiring
            : DeploymentAccessSessionState.Ready;
    }
}
