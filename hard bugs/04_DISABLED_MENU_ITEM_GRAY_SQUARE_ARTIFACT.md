# Hard bug: gray square beside disabled Git-safety menu item

## Initial symptom

The Git-safety popup looked almost correct, but a mysterious gray square appeared at the left edge of the disabled action:

```text
Scan repository
Add/update .gitignore safety block
```

The second action was intentionally disabled, but WPF rendered a small light-gray block that did not belong to the visual design.

This survived multiple cosmetic attempts because the artifact was not coming from the popup background itself.

## Root cause

The disabled item was still using button/template chrome that rendered a disabled-state visual. In a dark custom popup, the default or inherited disabled chrome was especially obvious.

This is a useful WPF lesson: setting these properties is not always sufficient:

```csharp
Background = darkBrush;
BorderThickness = new Thickness(0);
```

because a `ControlTemplate` may contain its own named border, overlay, focus visual or disabled trigger.

## Investigation pattern

The reliable debugging sequence was:

1. remove popup outer border — artifact remained;
2. remove hover background — artifact remained;
3. change popup panel background — artifact remained;
4. inspect the disabled child control/template — artifact tracked the disabled action.

That isolated the problem to control chrome rather than popup geometry.

## Final engineering direction

For menu-like actions, use a template owned by ZDeployPet rather than inheriting generic Button disabled visuals.

Example:

```xml
<Style x:Key="PopupActionStyle" TargetType="Button">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="BorderThickness" Value="0" />
    <Setter Property="Foreground" Value="{StaticResource AppTextBrush}" />
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="Button">
                <Border x:Name="Chrome"
                        Background="{TemplateBinding Background}"
                        CornerRadius="6">
                    <ContentPresenter Margin="12,8"
                                      HorizontalAlignment="Left" />
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True">
                        <Setter TargetName="Chrome" Property="Background"
                                Value="{StaticResource AppSurfaceRaisedBrush}" />
                    </Trigger>
                    <Trigger Property="IsEnabled" Value="False">
                        <Setter TargetName="Chrome" Property="Background" Value="Transparent" />
                        <Setter Property="Foreground"
                                Value="{StaticResource AppDisabledTextBrush}" />
                        <Setter Property="Opacity" Value="1" />
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

The key is that the disabled state is explicitly designed rather than inherited.

## Why `Opacity` alone is a weak fix

Reducing opacity can hide an artifact but also dims the whole control unpredictably, including backgrounds and glyphs. A custom template gives deterministic disabled visuals.

## Verification

Smoke test all states:

```text
enabled + idle
enabled + hover
disabled + idle
disabled + pointer over same region
popup open over dark and raised surfaces
```

The disabled action should be visibly unavailable but should not produce any extra rectangle, square, focus halo or light operating-system chrome.

## Prevention rule

If a dark WPF UI uses custom popup/menu buttons, own the complete template for enabled, hover, pressed and disabled states. Do not assume the platform button template will blend into custom dark chrome.
