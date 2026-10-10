namespace ZDeployPet.Core;

public sealed record LiveDeploymentPromptPlan(
    int TargetChoice,
    int? ReleaseChoice,
    string ConfirmationPhrase)
{
    public void Validate()
    {
        if (TargetChoice is < 1 or > 3)
            throw new InvalidDataException("Live target prompt choice must be 1, 2 or 3.");

        if (ReleaseChoice is not null && ReleaseChoice is < 1 or > 3)
            throw new InvalidDataException("Live release prompt choice must be 1, 2 or 3 when supplied.");

        if (!LiveDeploymentReviewGuard.IsSafeConfirmationPhrase(ConfirmationPhrase))
            throw new InvalidDataException("The live confirmation phrase is missing, too long, or contains control characters.");
    }
}

/// <summary>
/// Observational fail-closed protocol for an approved live deployment run.
/// The WSL adapter supplies only allowlisted target/release choices, forces mode 2,
/// and forwards the exact already-validated human-entered confirmation phrase only
/// at the exact live-confirmation prompt. This protocol never generates confirmation.
/// </summary>
public sealed class LiveDeploymentPromptProtocol
{
    private const int MaximumRollingCharacters = 2048;
    private readonly LiveDeploymentPromptPlan _plan;
    private string _rolling = string.Empty;

    public LiveDeploymentPromptProtocol(LiveDeploymentPromptPlan plan)
    {
        plan.Validate();
        _plan = plan;
    }

    public bool LiveModeObserved { get; private set; }
    public bool ConfirmationPromptObserved { get; private set; }
    public bool Completed { get; private set; }
    public bool SafetyViolation { get; private set; }
    public string? ViolationReason { get; private set; }

    public void Observe(string? text)
    {
        if (string.IsNullOrEmpty(text) || Completed || SafetyViolation)
            return;

        _rolling += text;
        if (_rolling.Length > MaximumRollingCharacters)
            _rolling = _rolling[^MaximumRollingCharacters..];

        if (Contains("DRY RUN — NOTHING WILL BE MODIFIED") || Contains("DRY RUN COMPLETE"))
        {
            Fail("The approved script entered dry-run mode while PDA-6 was executing a live deployment.");
            return;
        }

        if (Contains("LIVE MILLENOVA DEPLOYMENT"))
            LiveModeObserved = true;

        string confirmationPrompt = $"Type {_plan.ConfirmationPhrase} to continue:";
        if (Contains(confirmationPrompt))
            ConfirmationPromptObserved = true;

        if (Contains("MILLENOVA DEPLOYMENT COMPLETE"))
        {
            if (!LiveModeObserved)
            {
                Fail("The script reported deployment completion without first proving live mode.");
                return;
            }

            if (!ConfirmationPromptObserved)
            {
                Fail("The script reported deployment completion without the exact live-confirmation prompt being observed.");
                return;
            }

            Completed = true;
            _rolling = string.Empty;
        }
    }

    public void MarkProcessExited(int exitCode)
    {
        if (SafetyViolation || Completed)
            return;

        if (exitCode == 0)
            Fail("The script exited successfully without completing the verified PDA-6 live-deployment protocol.");
    }

    private bool Contains(string value)
        => _rolling.Contains(value, StringComparison.Ordinal);

    private void Fail(string reason)
    {
        ViolationReason = reason;
        SafetyViolation = true;
    }
}
