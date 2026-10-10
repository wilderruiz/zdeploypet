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
        SetState(PetCompanionState.Locked, "Deployment access is off.");
    }

    public PetCompanionState State { get; private set; } = PetCompanionState.Locked;

    public void SetState(PetCompanionState state, string message)
    {
        State = state;

        string stateText;
        string eyes;
        string mouth;
        Brush accent;

        switch (state)
        {
            case PetCompanionState.Running:
                stateText = "RUNNING";
                eyes = "◦   ◦";
                mouth = "ᴥ";
                accent = Brushes.Goldenrod;
                break;
            case PetCompanionState.Ready:
                stateText = "READY";
                eyes = "•   •";
                mouth = "⌣";
                accent = Brushes.MediumSeaGreen;
                break;
            case PetCompanionState.Error:
                stateText = "ERROR";
                eyes = "×   ×";
                mouth = "⌢";
                accent = Brushes.IndianRed;
                break;
            default:
                stateText = "LOCKED";
                eyes = "•   •";
                mouth = "ᴥ";
                accent = FindBrush("AppMutedTextBrush", Brushes.LightGray);
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
        ToolTip = $"ZPet status: {stateText}. {MessageText.Text} ZPet mirrors status only and cannot unlock access, deploy, or bypass confirmation.";
    }

    private static Brush FindBrush(string key, Brush fallback)
        => Application.Current.TryFindResource(key) as Brush ?? fallback;
}
