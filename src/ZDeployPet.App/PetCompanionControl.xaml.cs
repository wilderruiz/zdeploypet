using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

public enum PetCompanionState
{
    Locked,
    Running,
    Ready,
    Error
}

public partial class PetCompanionControl : UserControl
{
    public PetCompanionControl()
    {
        InitializeComponent();
        ActionButton.Click += (_, e) => Click?.Invoke(this, e);
        ToolTipService.SetInitialShowDelay(ActionButton, 250);
        ToolTipService.SetShowDuration(ActionButton, 20000);
        SetState(PetCompanionState.Locked, "Deployment access is off.");
    }

    public event RoutedEventHandler? Click;

    public PetCompanionState State { get; private set; } = PetCompanionState.Locked;

    public void SetActionEnabled(bool enabled) => ActionButton.IsEnabled = enabled;

    public void SetState(PetCompanionState state, string message)
    {
        State = state;

        string stateText;
        string eyes;
        string mouth;
        Brush accent;
        Brush shellBackground;

        switch (state)
        {
            case PetCompanionState.Running:
                stateText = "RUNNING";
                eyes = "◦   ◦";
                mouth = "ᴥ";
                accent = FindBrush("AppWarningBrush", Brushes.Goldenrod);
                shellBackground = FindBrush("AppSurfaceRaisedBrush", Brushes.DarkSlateGray);
                break;
            case PetCompanionState.Ready:
                stateText = "READY";
                eyes = "•   •";
                mouth = "⌣";
                accent = FindBrush("AppSuccessBrush", Brushes.MediumSeaGreen);
                shellBackground = FindBrush("AppLiveBrush", Brushes.DarkGreen);
                break;
            case PetCompanionState.Error:
                stateText = "ERROR";
                eyes = "×   ×";
                mouth = "⌢";
                accent = FindBrush("AppDangerBrush", Brushes.IndianRed);
                shellBackground = FindBrush("AppSurfaceRaisedBrush", Brushes.DarkSlateGray);
                break;
            default:
                stateText = "LOCKED";
                eyes = "•   •";
                mouth = "ᴥ";
                accent = FindBrush("AppMutedTextBrush", Brushes.LightGray);
                shellBackground = FindBrush("AppSurfaceRaisedBrush", Brushes.DarkSlateGray);
                break;
        }

        StateText.Text = stateText;
        StateText.Foreground = accent;
        MessageText.Text = string.IsNullOrWhiteSpace(message) ? stateText : message;
        EyesText.Text = eyes;
        MouthText.Text = mouth;
        StatusDot.Fill = accent;
        FaceBorder.BorderBrush = accent;
        LeftEar.Stroke = accent;
        RightEar.Stroke = accent;
        ShellBorder.BorderBrush = accent;
        ShellBorder.Background = shellBackground;

        string action = state switch
        {
            PetCompanionState.Ready => "Click ZDeploy Pet to turn deployment access off.",
            PetCompanionState.Running => "Deployment/session work is in progress; the access action is temporarily unavailable.",
            PetCompanionState.Error => "Click ZDeploy Pet to retry or continue deployment-access setup.",
            _ => "Click ZDeploy Pet to turn deployment access on or continue setup."
        };

        string detail = state switch
        {
            PetCompanionState.Ready => "READY means the approved key is loaded in the bounded app-owned ssh-agent and the configured target probes passed.",
            PetCompanionState.Running => "ZDeployPet may open a trusted terminal only when OpenSSH needs the private-key passphrase; the passphrase is never received or stored by ZDeployPet.",
            PetCompanionState.Error => "Open the ZDeployPet output tab for the detailed session/probe reason.",
            _ => "Detailed session and probe messages appear in the ZDeployPet output tab."
        };

        ActionButton.ToolTip = $"ZDeploy Pet — {stateText}\n{MessageText.Text}\n\n{action}\n{detail}";
    }

    private static Brush FindBrush(string key, Brush fallback)
        => Application.Current.TryFindResource(key) as Brush ?? fallback;
}
