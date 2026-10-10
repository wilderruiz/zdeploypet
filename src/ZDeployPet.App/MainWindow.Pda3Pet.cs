using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ZDeployPet.Core;

namespace ZDeployPet.App;

public partial class MainWindow
{
    private Border? _petCompanion;
    private TextBlock? _petStateText;
    private TextBlock? _petMessageText;
    private Ellipse? _petStatusDot;
    private Border? _petFace;

    private void InitializePetCompanion()
    {
        if (_petCompanion is not null) return;
        if (DiscoveryPanel.Parent is not Grid shellGrid) return;

        Grid petGrid = new();
        petGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        petGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid faceHost = new()
        {
            Width = 42,
            Height = 42,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        Polygon leftEar = new()
        {
            Points = new PointCollection { new(5, 13), new(11, 2), new(17, 14) },
            Fill = FindBrush("AppSurfaceRaisedBrush", Brushes.DimGray),
            Stroke = FindBrush("AppBorderBrush", Brushes.Gray),
            StrokeThickness = 1
        };
        Polygon rightEar = new()
        {
            Points = new PointCollection { new(25, 14), new(31, 2), new(37, 13) },
            Fill = FindBrush("AppSurfaceRaisedBrush", Brushes.DimGray),
            Stroke = FindBrush("AppBorderBrush", Brushes.Gray),
            StrokeThickness = 1
        };
        faceHost.Children.Add(leftEar);
        faceHost.Children.Add(rightEar);

        _petFace = new Border
        {
            Width = 34,
            Height = 30,
            Margin = new Thickness(4, 10, 4, 2),
            CornerRadius = new CornerRadius(12),
            Background = FindBrush("AppSurfaceRaisedBrush", Brushes.DimGray),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.Gray),
            BorderThickness = new Thickness(1)
        };

        Grid face = new();
        face.Children.Add(new TextBlock
        {
            Text = "•   •",
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 4, 0, 0),
            Foreground = FindBrush("AppTextBrush", Brushes.White)
        });
        face.Children.Add(new TextBlock
        {
            Text = "ᴥ",
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 3),
            Foreground = FindBrush("AppTextBrush", Brushes.White)
        });
        _petFace.Child = face;
        faceHost.Children.Add(_petFace);

        _petStatusDot = new Ellipse
        {
            Width = 9,
            Height = 9,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 1, 1),
            Stroke = FindBrush("AppBackgroundBrush", Brushes.Black),
            StrokeThickness = 1.5
        };
        faceHost.Children.Add(_petStatusDot);
        petGrid.Children.Add(faceHost);

        StackPanel copy = new()
        {
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 125
        };
        copy.Children.Add(new TextBlock
        {
            Text = "ZPet",
            FontWeight = FontWeights.SemiBold,
            Foreground = FindBrush("AppTextBrush", Brushes.White)
        });
        _petStateText = new TextBlock
        {
            Text = "LOCKED",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 1, 0, 0),
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        };
        _petMessageText = new TextBlock
        {
            Text = "Deployment access is off.",
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 1, 0, 0),
            Foreground = FindBrush("AppMutedTextBrush", Brushes.LightGray)
        };
        copy.Children.Add(_petStateText);
        copy.Children.Add(_petMessageText);
        Grid.SetColumn(copy, 1);
        petGrid.Children.Add(copy);

        _petCompanion = new Border
        {
            Child = petGrid,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(9, 6, 10, 6),
            Margin = new Thickness(0, 0, 0, 0),
            Background = FindBrush("AppSurfaceBrush", Brushes.Black),
            BorderBrush = FindBrush("AppBorderBrush", Brushes.DimGray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
            ToolTip = "ZPet mirrors ZDeployPet status only. It cannot unlock access, run a deployment, or bypass a confirmation."
        };

        Grid.SetRow(_petCompanion, 0);
        shellGrid.Children.Add(_petCompanion);

        DiscoveryPanel.IsVisibleChanged += (_, _) => RefreshPetCompanion();
        RefreshPetCompanion();
    }

    private void RefreshPetCompanion()
    {
        if (_petCompanion is null || _petStateText is null || _petMessageText is null ||
            _petStatusDot is null || _petFace is null)
            return;

        _petCompanion.Visibility = DiscoveryPanel.Visibility == Visibility.Visible && _activeProfile is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
        if (_petCompanion.Visibility != Visibility.Visible) return;

        string state;
        string message;
        Brush stateBrush;

        if (_operatorAccessBusy)
        {
            state = "RUNNING";
            message = "Checking deployment access.";
            stateBrush = Brushes.Goldenrod;
        }
        else if (_deploymentSession.State is DeploymentAccessSessionState.Invalid or DeploymentAccessSessionState.Expired)
        {
            state = "ERROR";
            message = "Deployment access needs attention.";
            stateBrush = Brushes.IndianRed;
        }
        else if (_deploymentSession.State is DeploymentAccessSessionState.Ready or DeploymentAccessSessionState.Expiring &&
                 _operatorAccessTargetsPassed)
        {
            state = "READY";
            message = "Approved targets are ready.";
            stateBrush = Brushes.MediumSeaGreen;
        }
        else
        {
            state = "LOCKED";
            message = "Deployment access is off.";
            stateBrush = FindBrush("AppMutedTextBrush", Brushes.LightGray);
        }

        _petStateText.Text = state;
        _petStateText.Foreground = stateBrush;
        _petMessageText.Text = message;
        _petStatusDot.Fill = stateBrush;
        _petFace.BorderBrush = stateBrush;
    }
}
