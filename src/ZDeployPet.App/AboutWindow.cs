using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace ZDeployPet.App;

public sealed class AboutWindow : Window
{
    private const string RepositoryUrl = "https://github.com/wilderruiz/zdeploypet";

    public AboutWindow()
    {
        Title = "About ZDeployPet";
        Width = 620;
        Height = 520;
        MinWidth = 520;
        MinHeight = 440;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResize;

        Version version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0, 0);
        string executablePath = Environment.ProcessPath ?? string.Empty;
        string buildDate = !string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath)
            ? File.GetLastWriteTime(executablePath).ToString("yyyy-MM-dd HH:mm")
            : "Unavailable";

        StackPanel content = new() { Margin = new Thickness(28) };
        content.Children.Add(new TextBlock
        {
            Text = "ZDeployPet",
            FontSize = 30,
            FontWeight = FontWeights.SemiBold
        });
        content.Children.Add(new TextBlock
        {
            Text = "A friendly Windows + WSL deployment companion in the Zomniverse family.",
            Margin = new Thickness(0, 8, 0, 22),
            TextWrapping = TextWrapping.Wrap
        });

        Grid facts = new();
        facts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        facts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        AddFact(facts, 0, "Version", version.ToString());
        AddFact(facts, 1, "Build date", buildDate);
        AddFact(facts, 2, "Platform", RuntimeInformation.OSDescription);
        AddFact(facts, 3, ".NET", RuntimeInformation.FrameworkDescription);
        AddFact(facts, 4, "Architecture", RuntimeInformation.ProcessArchitecture.ToString());
        AddFact(facts, 5, "Product boundary", "Profiles, bounded SSH sessions, safe deployment execution, report monitoring and sanitized diagnostics.");
        AddFact(facts, 6, "License", "Not finalized yet — public release licensing is tracked in PDA-8.");
        content.Children.Add(facts);

        TextBlock repository = new() { Margin = new Thickness(0, 22, 0, 0) };
        repository.Inlines.Add(new Run("Repository: "));
        Hyperlink link = new(new Run(RepositoryUrl)) { NavigateUri = new Uri(RepositoryUrl) };
        link.RequestNavigate += RepositoryLink_RequestNavigate;
        repository.Inlines.Add(link);
        content.Children.Add(repository);

        Border boundary = new()
        {
            Margin = new Thickness(0, 22, 0, 0),
            Padding = new Thickness(14),
            BorderThickness = new Thickness(1),
            BorderBrush = SystemColors.ControlDarkBrush,
            Child = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Text = "Security note: ZDeployPet does not store SSH account passwords or private-key passphrases, and its bounded deployment session is locked when the application closes."
            }
        };
        content.Children.Add(boundary);

        Button close = new()
        {
            Content = "Close",
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 22, 0, 0),
            Padding = new Thickness(18, 8, 18, 8),
            MinWidth = 90
        };
        close.Click += (_, _) => Close();
        content.Children.Add(close);

        ScrollViewer scroll = new()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content
        };
        Content = scroll;
    }

    private static void AddFact(Grid grid, int row, string label, string value)
    {
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        TextBlock labelText = new()
        {
            Text = label,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 4, 12, 4)
        };
        TextBlock valueText = new()
        {
            Text = value,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 4)
        };
        Grid.SetRow(labelText, row);
        Grid.SetColumn(labelText, 0);
        Grid.SetRow(valueText, row);
        Grid.SetColumn(valueText, 1);
        grid.Children.Add(labelText);
        grid.Children.Add(valueText);
    }

    private static void RepositoryLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        catch (Exception exception)
        {
            ShellRuntime.Activity.Add(ShellActivityLevel.Warning, "Shell", "Repository link could not be opened: " + exception.Message);
        }
    }
}
