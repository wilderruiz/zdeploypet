using ZDeployPet.Core;
using ZDeployPet.Infrastructure;
using ZDeployPet.WslBridge;

namespace ZDeployPet.App;

public sealed class DeploymentSessionController
{
    private readonly DeploymentAccessIdentityStore _identityStore = new();
    private readonly WslSshAgentSession _agent = new();
    private WslSshAgentRuntime? _runtime;
    private DeploymentAccessSessionLease? _lease;

    public DeploymentAccessSessionState State { get; private set; } = DeploymentAccessSessionState.Locked;
    public string Message { get; private set; } = "Deployment access is locked.";
    public DateTimeOffset? ExpiresAtUtc => _lease?.ExpiresAtUtc;
    public bool HasOwnedAgent => _runtime is not null;

    public async Task BeginUnlockAsync(DeploymentProfile profile, CancellationToken cancellationToken = default)
    {
        if (_runtime is not null)
            await LockAsync(cancellationToken);

        DeploymentAccessIdentity identity = await _identityStore.LoadAsync(profile.Id, cancellationToken);
        DeploymentKeyIdentity? key = identity.DeploymentKey;
        if (key is null)
        {
            State = DeploymentAccessSessionState.Invalid;
            Message = "No approved deployment key is enrolled. Complete Deployment access first.";
            return;
        }

        WslSshAgentStartResult started = await _agent.StartAsync(
            profile.WslDistribution,
            profile.Id,
            cancellationToken);
        if (!started.Success || started.Runtime is null)
        {
            State = DeploymentAccessSessionState.Invalid;
            Message = started.Error ?? "ZDeployPet could not start its dedicated ssh-agent.";
            return;
        }

        _runtime = started.Runtime;
        _lease = WslSshAgentSession.CreateLease(profile.Id, _runtime, key.Fingerprint);
        State = DeploymentAccessSessionState.Locked;

        WslSshAgentUnlockLaunchResult launched = await _agent.LaunchUnlockAsync(
            _runtime,
            key,
            profile.Name,
            DeploymentAccessSessionLease.DefaultLifetime,
            cancellationToken);
        Message = launched.Message;

        if (!launched.Success)
        {
            await LockAsync(cancellationToken);
            State = DeploymentAccessSessionState.Invalid;
            Message = launched.Message;
        }
    }

    public async Task CheckAsync(DeploymentProfile profile, CancellationToken cancellationToken = default)
    {
        if (_runtime is null || _lease is null)
        {
            State = DeploymentAccessSessionState.Locked;
            Message = "No ZDeployPet deployment session is running. Click Unlock first.";
            return;
        }

        if (!string.Equals(_lease.ProfileId, profile.Id, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(_runtime.Distribution, profile.WslDistribution, StringComparison.Ordinal))
        {
            State = DeploymentAccessSessionState.Invalid;
            Message = "The running deployment session belongs to a different profile or WSL distribution. Lock it before continuing.";
            return;
        }

        WslSshAgentInspection inspection = await _agent.InspectAsync(_runtime, cancellationToken);
        if (!inspection.AgentReachable)
        {
            State = DeploymentAccessSessionState.Locked;
            Message = inspection.Error ?? "The ZDeployPet ssh-agent is no longer reachable.";
            return;
        }

        string approved = NormalizeFingerprint(_lease.ApprovedKeyFingerprint);
        IReadOnlyList<string> loaded = inspection.LoadedFingerprints
            .Select(NormalizeFingerprint)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        bool approvedLoaded = loaded.Contains(approved, StringComparer.Ordinal);
        bool foreignLoaded = loaded.Any(value => !string.Equals(value, approved, StringComparison.Ordinal));
        if (foreignLoaded)
        {
            State = DeploymentAccessSessionState.Invalid;
            Message = "The dedicated ZDeployPet ssh-agent contains an unexpected key. Access is not READY; lock the session and start again.";
            return;
        }

        State = _lease.Evaluate(DateTimeOffset.UtcNow, true, approvedLoaded);
        Message = State switch
        {
            DeploymentAccessSessionState.Ready =>
                $"READY — the exact approved deployment key is loaded. Session expires at {_lease.ExpiresAtUtc.ToLocalTime():g}.",
            DeploymentAccessSessionState.Expiring =>
                $"EXPIRING — deployment access remains valid until {_lease.ExpiresAtUtc.ToLocalTime():g}.",
            DeploymentAccessSessionState.Expired =>
                "EXPIRED — the bounded deployment-access lease has ended. Lock and unlock again before deployment.",
            DeploymentAccessSessionState.Locked when loaded.Count == 0 =>
                "LOCKED — the app-owned ssh-agent is running, but the approved key is not loaded. Complete ssh-add in the trusted terminal, then check again.",
            DeploymentAccessSessionState.Invalid =>
                "INVALID — the running session metadata could not be trusted.",
            _ => "Deployment access is locked."
        };
    }

    public async Task LockAsync(CancellationToken cancellationToken = default)
    {
        if (_runtime is not null)
        {
            WslSshAgentLockResult result = await _agent.LockAsync(_runtime, cancellationToken);
            Message = result.Success
                ? "LOCKED — the ZDeployPet ssh-agent was stopped and deployment access was removed."
                : "LOCKED locally — ZDeployPet could not verify/stop the previous agent: " + (result.Error ?? "unknown error");
        }
        else
        {
            Message = "LOCKED — no ZDeployPet deployment session is running.";
        }

        _runtime = null;
        _lease = null;
        State = DeploymentAccessSessionState.Locked;
    }

    public void LockSynchronouslyBestEffort()
    {
        if (_runtime is null) return;
        try
        {
            LockAsync().GetAwaiter().GetResult();
        }
        catch
        {
            _runtime = null;
            _lease = null;
            State = DeploymentAccessSessionState.Locked;
        }
    }

    private static string NormalizeFingerprint(string value) =>
        (value ?? string.Empty).Trim().TrimEnd('=');
}
