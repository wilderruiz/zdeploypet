using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ZDeployPet.App;

internal sealed record HelpTipSpec(
    string Title,
    string Summary,
    string? WhenToUse = null,
    string? WhatItDoes = null,
    string? Example = null,
    string? Safety = null,
    string? Tip = null);

internal static class HelpTipFactory
{
    public static Button AttachToButton(Button target, HelpTipSpec spec)
    {
        if (target.Parent is not Panel panel)
            throw new InvalidOperationException("Help buttons can only be attached to controls hosted by a Panel.");

        Button helpButton = Create(spec);
        helpButton.SetBinding(UIElement.VisibilityProperty, new Binding(nameof(UIElement.Visibility))
        {
            Source = target,
            Mode = BindingMode.OneWay
        });

        int targetIndex = panel.Children.IndexOf(target);
        panel.Children.Insert(targetIndex + 1, helpButton);
        return helpButton;
    }

    public static Button Create(HelpTipSpec spec)
    {
        Button button = new();
        if (Application.Current.TryFindResource("SharedInfoButtonStyle") is Style style)
            button.Style = style;

        button.ToolTip = BuildToolTip(spec);
        return button;
    }

    private static ToolTip BuildToolTip(HelpTipSpec spec)
    {
        StackPanel content = new() { MaxWidth = 520 };
        AddText(content, spec.Title, bold: true);
        AddText(content, spec.Summary, marginTop: 7);
        AddSection(content, "When to use this", spec.WhenToUse);
        AddSection(content, "What it does", spec.WhatItDoes);
        AddSection(content, "Example", spec.Example);
        AddSection(content, "Safety", spec.Safety);

        if (!string.IsNullOrWhiteSpace(spec.Tip))
            AddText(content, spec.Tip!, marginTop: 9);

        ToolTip toolTip = new() { Content = content };
        if (Application.Current.TryFindResource("SharedToolTipStyle") is Style style)
            toolTip.Style = style;

        return toolTip;
    }

    private static void AddSection(StackPanel content, string heading, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        AddText(content, heading, bold: true, marginTop: 9);
        AddText(content, text, marginTop: 3);
    }

    private static TextBlock AddText(StackPanel content, string text, bool bold = false, double marginTop = 0)
    {
        TextBlock block = new()
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, marginTop, 0, 0),
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal
        };
        content.Children.Add(block);
        return block;
    }
}
