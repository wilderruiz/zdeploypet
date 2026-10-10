using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZDeployPet.App;

public sealed class ReportArtifactWindow : Window
{
    public ReportArtifactWindow(string title, string subtitle, string content)
    {
        Title = title;
        Width = 980;
        Height = 720;
        MinWidth = 680;
        MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        Grid root = new()
        {
            Margin = new Thickness(18)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        StackPanel heading = new();
        heading.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = FindBrush("AppTextBrush", Brushes.White)
        });
        heading.Children.Add(new TextBlock
        {
            Text = subtitle,
            Margin = new Thickness(0, 4, 0, 12),
            TextWrapping = TextWrapping.Wrap,
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        });
        root.Children.Add(heading);

        TextBox viewer = new()
        {
            Text = content,
            IsReadOnly = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            Padding = new Thickness(12),
            Background = FindBrush("AppBackgroundBrush", Brushes.Black),
            Foreground = FindBrush("AppTextBrush", Brushes.White),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.Gray),
            BorderThickness = new Thickness(1)
        };
        Grid.SetRow(viewer, 1);
        root.Children.Add(viewer);

        StackPanel actions = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };

        Button copy = new()
        {
            Content = "Copy all",
            Padding = new Thickness(14, 7, 14, 7),
            Margin = new Thickness(0, 0, 10, 0)
        };
        copy.Click += (_, _) => Clipboard.SetText(content ?? string.Empty);
        actions.Children.Add(copy);

        Button close = new()
        {
            Content = "Close",
            Padding = new Thickness(14, 7, 14, 7)
        };
        close.Click += (_, _) => Close();
        actions.Children.Add(close);

        Grid.SetRow(actions, 2);
        root.Children.Add(actions);

        Content = root;
    }

    private static Brush FindBrush(string key, Brush fallback)
        => Application.Current.TryFindResource(key) as Brush ?? fallback;
}
