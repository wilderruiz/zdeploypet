using ZDeployPet.Core;
using ZDeployPet.Infrastructure;
using ZDeployPet.WslBridge;

namespace ZDeployPet.App;

public sealed record DeploymentSessionProbeResult(
    DeploymentTarget Target,
    SshProbeResult Result);

public sealed class DeploymentSessionController
{
    private readonly DeploymentAccessIdentityStore _identityStore = new();
    private readonly WslSshAgentSession _agent = new();
    private readonly WslSshAgentProbe _agentProbe = new();
    private WslSshAgentRuntime? _runtime;
    private DeploymentAccessSessionLease? _lease;

    public DeploymentAccessSessionState State { get; private set; } = DeploymentAccessSessionState.Locked;
    public string Message { get; private set; } = "Deployment access is locked.";
    public DateTimeOffset? ExpiresAtUtc => _lease?.ExpiresAtUtc;
    public bool HasOwnedAgent => _runtime is not null;
    public bool TargetsReady { get; private set; }

    public async Task BeginUnlockAsync(DeploymentProfile profile, CancellationToken cancellationToken = default)
    {
        TargetsReady = false;
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
            TargetsReady = false;
            State = DeploymentAccessSessionState.Locked;
            Message = "No ZDeployPet deployment session is running. Click Unlock first.";
            return;
        }

        if (!string.Equals(_lease.ProfileId, profile.Id, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(_runtime.Distribution, profile.WslDistribution, StringComparison.Ordinal))
        {
            TargetsReady = false;
            State = DeploymentAccessSessionState.Invalid;
            Message = "The running deployment session belongs to a different profile or WSL distribution. Lock it before continuing.";
            return;
        }

        WslSshAgentInspection inspection = await _agent.InspectAsync(_runtime, cancellationToken);
        if (!inspection.AgentReachable)
        {
            TargetsReady = false;
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
            TargetsReady = false;
            State = DeploymentAccessSessionState.Invalid;
            Message = "The dedicated ZDeployPet ssh-agent contains an unexpected key. Access is not READY; lock the session and start again.";
            return;
        }

        State = _lease.Evaluate(DateTimeOffset.UtcNow, true, approvedLoaded);
        if (State is not DeploymentAccessSessionState.Ready and not DeploymentAccessSessionState.Expiring)
            TargetsReady = false;

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

    public async Task<WslSshAgentRuntime?> GetReadyRuntimeAsync(
        DeploymentProfile profile,
        CancellationToken cancellationToken = default)
    {
        await CheckAsync(profile, cancellationToken);
        if (_runtime is null || _lease is null ||
            State is not DeploymentAccessSessionState.Ready and not DeploymentAccessSessionState.Expiring)
            return null;

        DeploymentAccessIdentity identity = await _identityStore.LoadAsync(profile.Id, cancellationToken);
        DeploymentKeyIdentity? key = identity.DeploymentKey;
        if (key is null)
        {
            TargetsReady = false;
            State = DeploymentAccessSessionState.Invalid;
            Message = "The profile no longer has an approved deployment key. Deployment execution is blocked.";
            return null;
        }

        if (!string.Equals(
                NormalizeFingerprint(key.Fingerprint),
                NormalizeFingerprint(_lease.ApprovedKeyFingerprint),
                StringComparison.Ordinal))
        {
            TargetsReady = false;
            State = DeploymentAccessSessionState.Invalid;
            Message = "The approved deployment key changed after this session was unlocked. Lock and unlock again before deployment.";
            return null;
        }

        return _runtime;
    }

    public async Task<IReadOnlyList<DeploymentSessionProbeResult>> ProbeReadyTargetsAsync(
        DeploymentProfile profile,
        CancellationToken cancellationToken = default)
    {
        TargetsReady = false;
        await CheckAsync(profile, cancellationToken);
        if (_runtime is null || State is not DeploymentAccessSessionState.Ready and not DeploymentAccessSessionState.Expiring)
        {
            Message = "The bounded deployment session is not READY. Unlock and verify the session before probing targets.";
            return [];
        }

        DeploymentAccessIdentity identity = await _identityStore.LoadAsync(profile.Id, cancellationToken);
        DeploymentKeyIdentity? key = identity.DeploymentKey;
        if (key is null)
        {
            State = DeploymentAccessSessionState.Invalid;
            Message = "The profile no longer has an approved deployment key. The bounded session cannot be used.";
            return [];
        }

        List<DeploymentSessionProbeResult> results = [];
        foreach (DeploymentTarget target in profile.Targets)
        {
            TargetHostIdentity? hostKey = identity.FindHostKey(target.Id);
            if (hostKey is null)
            {
                TargetHostIdentity? recovered = await TryRecoverHostTrustAfterTargetIdChangeAsync(
                    profile,
                    target,
                    identity,
                    key,
                    cancellationToken);
                if (recovered is not null)
                {
                    hostKey = recovered;
                    identity = await _identityStore.LoadAsync(profile.Id, cancellationToken);
                    ShellRuntime.Activity.Add(
                        ShellActivityLevel.Success,
                        "Access",
                        $"Recovered previously enrolled SSH host trust for '{target.Label}' after a profile target-id change. The exact trusted host fingerprint and bounded-agent authentication both matched.");
                }
            }

            SshProbeResult probe = hostKey is null
                ? new SshProbeResult(
                    SshProbeStatus.HostKeyNotEnrolled,
                    "No enrolled SSH host fingerprint exists for this target.")
                : await _agentProbe.ProbeAsync(
                    profile.WslDistribution,
                    target,
                    key,
                    hostKey,
                    _runtime,
                    cancellationToken);
            results.Add(new(target, probe));
        }

        int passed = results.Count(item => item.Result.Success);
        TargetsReady = passed == results.Count && results.Count > 0;
        Message = TargetsReady
            ? $"READY — bounded-agent probes passed for all {passed} configured target{(passed == 1 ? string.Empty : "s")}."
            : $"READY — bounded session remains valid; {passed} of {results.Count} target probes passed. Review the failed target before deployment.";
        return results;
    }

    private async Task<TargetHostIdentity?> TryRecoverHostTrustAfterTargetIdChangeAsync(
        DeploymentProfile profile,
        DeploymentTarget target,
        DeploymentAccessIdentity identity,
        DeploymentKeyIdentity deploymentKey,
        CancellationToken cancellationToken)
    {
        if (_runtime is null)
            return null;

        HashSet<string> currentTargetIds = profile.Targets
            .Select(item => item.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        TargetHostIdentity[] legacyCandidates = identity.HostKeys
            .Where(item => !currentTargetIds.Contains(item.TargetId))
            .GroupBy(
                item => $"{NormalizeFingerprint(item.Fingerprint)}|{item.KeyType.Trim().ToUpperInvariant()}",
                StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
        if (legacyCandidates.Length == 0)
            return null;

        List<TargetHostIdentity> verifiedMatches = [];
        foreach (TargetHostIdentity candidate in legacyCandidates)
        {
            SshProbeResult verification = await _agentProbe.ProbeAsync(
                profile.WslDistribution,
                target,
                deploymentKey,
                candidate,
                _runtime,
                cancellationToken);
            if (verification.Success)
                verifiedMatches.Add(candidate);
        }

        if (verifiedMatches.Count != 1)
            return null;

        TargetHostIdentity matched = verifiedMatches[0];
        TargetHostIdentity rebound = new(target.Id, matched.Fingerprint, matched.KeyType);
        await _identityStore.EnrollHostKeyAsync(profile.Id, rebound, cancellationToken);
        return rebound;
    }

    public async Task LockAsync(CancellationToken cancellationToken = default)
    {
        TargetsReady = false;
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

    public void LockSynchronouslyBestEffort(TimeSpan? timeout = null)
    {
        if (_runtime is null) return;

        using CancellationTokenSource cancellation = new(timeout ?? TimeSpan.FromSeconds(2));
        try
        {
            LockAsync(cancellation.Token).GetAwaiter().GetResult();
        }
        catch
        {
            _runtime = null;
            _lease = null;
            TargetsReady = false;
            State = DeploymentAccessSessionState.Locked;
            Message = "LOCKED locally — shutdown cleanup timed out or failed.";
        }
    }

    private static string NormalizeFingerprint(string value) =>
        (value ?? string.Empty).Trim().TrimEnd('=');
}
