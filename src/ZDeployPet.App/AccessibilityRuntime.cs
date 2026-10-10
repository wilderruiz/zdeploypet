using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ZDeployPet.App;

/// <summary>
/// Applies shared keyboard/focus and automation metadata without moving any
/// deployment authority into the presentation layer.
/// </summary>
internal static class AccessibilityRuntime
{
    private static readonly Brush FocusBrush = new SolidColorBrush(Color.FromRgb(0xE9, 0x48, 0x58));

    public static void Apply(Window window)
    {
        ApplyRecursive(window);
    }

    private static void ApplyRecursive(DependencyObject node)
    {
        if (node is FrameworkElement element)
        {
            EnsureAutomationName(element);
            EnsureKeyboardFocusCue(element);
        }

        int childCount = VisualTreeHelper.GetChildrenCount(node);
        for (int i = 0; i < childCount; i++)
            ApplyRecursive(VisualTreeHelper.GetChild(node, i));
    }

    private static void EnsureAutomationName(FrameworkElement element)
    {
        if (!string.IsNullOrWhiteSpace(AutomationProperties.GetName(element)))
            return;

        string? name = element switch
        {
            ButtonBase button => GetButtonName(button),
            ComboBox comboBox => HumanizeName(comboBox.Name),
            TextBox textBox => HumanizeName(textBox.Name),
            DataGrid dataGrid => HumanizeName(dataGrid.Name),
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(name))
            AutomationProperties.SetName(element, name);
    }

    private static string? GetButtonName(ButtonBase button)
    {
        if (button.Content is string content && !string.IsNullOrWhiteSpace(content))
        {
            if (content.Trim().Equals("i", StringComparison.OrdinalIgnoreCase))
                return "Help information";

            return content.Replace("_", string.Empty).Trim();
        }

        if (button.ToolTip is string toolTip && !string.IsNullOrWhiteSpace(toolTip))
            return toolTip.Trim();

        return HumanizeName(button.Name);
    }

    private static string? HumanizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        string value = name;
        foreach (string suffix in new[] { "Button", "TextBox", "ComboBox", "DataGrid", "Grid", "Control" })
        {
            if (value.EndsWith(suffix, StringComparison.Ordinal))
            {
                value = value[..^suffix.Length];
                break;
            }
        }

        if (value.Length == 0)
            return null;

        StringBuilder result = new();
        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (i > 0 && char.IsUpper(current) && !char.IsUpper(value[i - 1]))
                result.Append(' ');
            result.Append(current);
        }

        return result.ToString().Trim();
    }

    private static void EnsureKeyboardFocusCue(FrameworkElement element)
    {
        if (element is not Control control || !control.Focusable)
            return;

        if (control is ButtonBase)
            return; // Button chrome already owns a strong accent focus cue.

        if (control is TextBox or ComboBox)
        {
            control.GotKeyboardFocus -= Control_GotKeyboardFocus;
            control.LostKeyboardFocus -= Control_LostKeyboardFocus;
            control.GotKeyboardFocus += Control_GotKeyboardFocus;
            control.LostKeyboardFocus += Control_LostKeyboardFocus;
        }
    }

    private static void Control_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is Control control)
        {
            control.SetCurrentValue(Control.BorderBrushProperty, FocusBrush);
            control.SetCurrentValue(Control.BorderThicknessProperty, new Thickness(1));
        }
    }

    private static void Control_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is Control control && Application.Current.Resources["AppInputBackgroundBrush"] is Brush background)
            control.SetCurrentValue(Control.BorderBrushProperty, background);
    }
}
