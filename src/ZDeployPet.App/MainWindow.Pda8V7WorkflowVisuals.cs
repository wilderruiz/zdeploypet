using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private bool _pda8V7WorkflowVisualsInitialized;
    private readonly HashSet<StackPanel> _pda8V7TransformedSelectorCards = new();

    private void InitializePda8V7WorkflowVisuals()
    {
        if (_pda8V7WorkflowVisualsInitialized)
            return;

        if (_homeDryBodyHost is null || _homeLiveBodyHost is null)
            return;

        _pda8V7WorkflowVisualsInitialized = true;

        // DRY/LIVE bodies are injected only after ZDeploy Pet becomes READY.
        // LayoutUpdated is used as a narrow owner-level observation point so the
        // already-authoritative PDA-5/PDA-6 controller content can be styled in
        // place without duplicating or moving any execution logic.
        _homeDryBodyHost.LayoutUpdated += (_, _) => RefreshPda8V7WorkflowVisuals(_homeDryBodyHost, isLive: false);
        _homeLiveBodyHost.LayoutUpdated += (_, _) => RefreshPda8V7WorkflowVisuals(_homeLiveBodyHost, isLive: true);
        SizeChanged += (_, _) =>
        {
            RefreshPda8V7WorkflowVisuals(_homeDryBodyHost, isLive: false);
            RefreshPda8V7WorkflowVisuals(_homeLiveBodyHost, isLive: true);
        };

        RefreshPda8V7WorkflowVisuals(_homeDryBodyHost, isLive: false);
        RefreshPda8V7WorkflowVisuals(_homeLiveBodyHost, isLive: true);
    }

    private void RefreshPda8V7WorkflowVisuals(Grid host, bool isLive)
    {
        if (host.Children.Count == 0)
            return;

        foreach (UIElement child in host.Children)
            ApplyPda8V7WorkflowVisuals(child, isLive);
    }

    private void ApplyPda8V7WorkflowVisuals(DependencyObject node, bool isLive)
    {
        switch (node)
        {
            case ScrollViewer scroll:
                StylePda8V11WorkflowScrollViewer(scroll);
                if (scroll.Content is DependencyObject scrollContent)
                    ApplyPda8V7WorkflowVisuals(scrollContent, isLive);
                return;

            case Border border:
                StylePda8V7WorkflowCard(border, isLive);
                if (border.Child is DependencyObject borderChild)
                    ApplyPda8V7WorkflowVisuals(borderChild, isLive);
                return;

            case StackPanel panel:
                if (IsPda8V7SelectorCard(panel))
                    TransformPda8V7SelectorCard(panel);

                foreach (UIElement child in panel.Children)
                    ApplyPda8V7WorkflowVisuals(child, isLive);
                return;

            case Grid grid:
                foreach (UIElement child in grid.Children)
                    ApplyPda8V7WorkflowVisuals(child, isLive);
                return;

            case WrapPanel wrap:
                foreach (UIElement child in wrap.Children)
                    ApplyPda8V7WorkflowVisuals(child, isLive);
                return;

            case TextBlock text:
                StylePda8V7Text(text);
                return;

            case ComboBox combo:
                if (Application.Current.TryFindResource("Pda8ComboBoxStyle") is Style comboStyle)
                    combo.Style = comboStyle;
                combo.MinHeight = 34;
                combo.HorizontalAlignment = HorizontalAlignment.Stretch;
                return;

            case TextBox textBox:
                if (Application.Current.TryFindResource("Pda8TextBoxStyle") is Style textBoxStyle)
                    textBox.Style = textBoxStyle;
                textBox.MinHeight = 34;
                textBox.MinWidth = 0;
                textBox.HorizontalAlignment = HorizontalAlignment.Stretch;
                return;

            case Button button:
                StylePda8V7Button(button, isLive);
                return;
        }
    }

    private void StylePda8V11WorkflowScrollViewer(ScrollViewer scroll)
    {
        if (Application.Current.TryFindResource("Pda8WorkflowScrollViewerStyle") is Style scrollStyle)
            scroll.Style = scrollStyle;

        scroll.Background = Brushes.Transparent;
        scroll.BorderThickness = new Thickness(0);
        scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        scroll.PanningMode = PanningMode.VerticalOnly;
        scroll.CanContentScroll = false;

        // Bound the embedded workflow to the currently visible Home viewport so long
        // immutable LIVE reviews scroll inside their tab rather than stretching the
        // whole Home surface below the application window. At small window heights,
        // keep enough usable space for the workflow itself.
        if (ActualHeight > 0)
            scroll.MaxHeight = Math.Max(320, ActualHeight - 360);
    }

    private void StylePda8V7WorkflowCard(Border border, bool isLive)
    {
        if (border.Child is not StackPanel body || body.Children.Count == 0 || body.Children[0] is not TextBlock heading)
            return;

        string title = heading.Text ?? string.Empty;
        if (!title.StartsWith("1. ", StringComparison.Ordinal) &&
            !title.StartsWith("2. ", StringComparison.Ordinal))
        {
            return;
        }

        border.Background = FindBrush("AppSurfaceRaisedBrush", Brushes.DarkSlateGray);
        border.BorderBrush = Brushes.Transparent;
        border.BorderThickness = new Thickness(0);
        border.CornerRadius = new CornerRadius(12);
        border.Padding = new Thickness(17);
        border.Margin = new Thickness(0, 12, 0, 0);

        heading.FontSize = 15;
        heading.FontWeight = FontWeights.SemiBold;
        heading.Foreground = FindBrush("AppTextBrush", Brushes.White);

        // LIVE's immutable review card may appear only after review creation.
        // The same grammar is applied when it becomes visible.
        if (isLive && title.Contains("Immutable review", StringComparison.OrdinalIgnoreCase))
        {
            border.Background = FindBrush("AppLiveBrush", border.Background);
            border.BorderBrush = FindBrush("AppLiveBorderBrush", Brushes.DarkGreen);
            border.BorderThickness = new Thickness(1);
        }
    }

    private bool IsPda8V7SelectorCard(StackPanel panel)
    {
        if (_pda8V7TransformedSelectorCards.Contains(panel) || panel.Children.Count == 0)
            return false;

        if (panel.Children[0] is not TextBlock heading)
            return false;

        return string.Equals(heading.Text, "2. Dry-run request", StringComparison.Ordinal) ||
               string.Equals(heading.Text, "1. Proposed live deployment", StringComparison.Ordinal);
    }

    private void TransformPda8V7SelectorCard(StackPanel panel)
    {
        TextBlock? targetLabel = panel.Children
            .OfType<TextBlock>()
            .FirstOrDefault(item => string.Equals(item.Text, "Target", StringComparison.Ordinal));
        TextBlock? releaseLabel = panel.Children
            .OfType<TextBlock>()
            .FirstOrDefault(item => string.Equals(item.Text, "Release type", StringComparison.Ordinal));
        ComboBox[] combos = panel.Children.OfType<ComboBox>().Take(2).ToArray();

        if (targetLabel is null || releaseLabel is null || combos.Length != 2)
            return;

        int insertAt = panel.Children.IndexOf(targetLabel);
        panel.Children.Remove(targetLabel);
        panel.Children.Remove(combos[0]);
        panel.Children.Remove(releaseLabel);
        panel.Children.Remove(combos[1]);

        WrapPanel fields = new()
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 14, 0, 0)
        };

        fields.Children.Add(BuildPda8V7Field(targetLabel, combos[0], width: 320, rightMargin: 12));
        fields.Children.Add(BuildPda8V7Field(releaseLabel, combos[1], width: 220, rightMargin: 0));
        panel.Children.Insert(Math.Max(1, insertAt), fields);

        _pda8V7TransformedSelectorCards.Add(panel);
    }

    private FrameworkElement BuildPda8V7Field(TextBlock label, ComboBox combo, double width, double rightMargin)
    {
        label.Margin = new Thickness(0, 0, 0, 6);
        label.FontSize = 11;
        label.FontWeight = FontWeights.SemiBold;
        label.Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray);

        combo.MinWidth = 0;
        combo.Width = double.NaN;
        combo.HorizontalAlignment = HorizontalAlignment.Stretch;
        if (Application.Current.TryFindResource("Pda8ComboBoxStyle") is Style comboStyle)
            combo.Style = comboStyle;

        StackPanel field = new()
        {
            Width = width,
            Margin = new Thickness(0, 0, rightMargin, 10)
        };
        field.Children.Add(label);
        field.Children.Add(combo);
        return field;
    }

    private void StylePda8V7Text(TextBlock text)
    {
        string value = text.Text ?? string.Empty;

        if (string.Equals(value, "Allowlisted dry run", StringComparison.Ordinal) ||
            string.Equals(value, "Live deployment review", StringComparison.Ordinal))
        {
            text.FontSize = 21;
            text.FontWeight = FontWeights.SemiBold;
            text.Foreground = FindBrush("AppTextBrush", Brushes.White);
            text.Margin = new Thickness(0, 0, 0, 2);
            return;
        }

        if (string.Equals(value, "Target", StringComparison.Ordinal) ||
            string.Equals(value, "Release type", StringComparison.Ordinal) ||
            string.Equals(value, "Exact confirmation phrase", StringComparison.Ordinal) ||
            string.Equals(value, "Type the phrase manually", StringComparison.Ordinal))
        {
            text.FontSize = 11;
            text.FontWeight = FontWeights.SemiBold;
            text.Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray);
        }

        if (value.StartsWith("Current SHA-256:", StringComparison.Ordinal))
        {
            text.FontFamily = new FontFamily("Consolas");
            text.FontSize = 11;
            text.Background = FindBrush("AppInputBackgroundBrush", Brushes.Black);
            text.Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray);
            text.Padding = new Thickness(10, 8, 10, 8);
            text.Margin = new Thickness(0, 9, 0, 0);
        }
    }

    private void StylePda8V7Button(Button button, bool isLive)
    {
        if (Application.Current.TryFindResource("Pda8ButtonStyle") is Style buttonStyle)
            button.Style = buttonStyle;

        button.MinHeight = 34;
        button.Padding = new Thickness(13, 8, 13, 8);
        button.HorizontalAlignment = HorizontalAlignment.Left;

        string label = button.Content?.ToString() ?? string.Empty;
        if (string.Equals(label, "Start dry run", StringComparison.Ordinal))
        {
            button.Background = FindBrush("AppDryBrush", Brushes.DarkSlateBlue);
            button.BorderBrush = FindBrush("AppDryBorderBrush", Brushes.SteelBlue);
            button.Foreground = FindBrush("AppDryTextBrush", Brushes.White);
            return;
        }

        if (string.Equals(label, "Create immutable review", StringComparison.Ordinal) ||
            string.Equals(label, "Start LIVE deployment", StringComparison.Ordinal))
        {
            button.Background = FindBrush("AppLiveBrush", Brushes.DarkGreen);
            button.BorderBrush = FindBrush("AppLiveBorderBrush", Brushes.SeaGreen);
            button.Foreground = FindBrush("AppLiveTextBrush", Brushes.White);
            return;
        }

        if (string.Equals(label, "Validate typed confirmation", StringComparison.Ordinal))
        {
            button.Background = FindBrush("AppSurfaceRaisedBrush", Brushes.DarkSlateGray);
            button.BorderBrush = FindBrush("AppBorderStrongBrush", Brushes.Gray);
            button.Foreground = FindBrush("AppTextBrush", Brushes.White);
            return;
        }

        if (!isLive && string.Equals(label, "Approve current script fingerprint", StringComparison.Ordinal))
        {
            button.Background = Brushes.Transparent;
            button.BorderBrush = FindBrush("AppBorderStrongBrush", Brushes.Gray);
            button.Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray);
        }
    }
}
