# Hard bug: the bright red top bar was not WPF shell chrome

## Initial symptom

The original application looked like a red command center because the entire Windows caption/title bar was bright red. The PDA-8 mockup called for a dark navy GitHub/VS Code-like shell with only restrained warm/red accents.

A common mistake would have been to rebuild the entire title bar inside WPF.

## Root cause / architecture discovery

The very top strip containing:

```text
app icon
window title
minimize
maximize
close
```

is native Windows non-client chrome, not the same WPF tree as the `Project / Profile | View | Help` menu underneath it.

Therefore there were two independent styling layers:

```text
Windows DWM caption
WPF application shell/menu
```

Trying to style only WPF could never remove the bright native caption color.

## Why a custom fake title bar was rejected

Replacing native chrome would require taking ownership of:

- window dragging;
- resize hit testing;
- double-click maximize;
- system menu;
- snap layouts;
- high-DPI behavior;
- minimize/maximize/close semantics;
- accessibility/non-client behavior.

That was unnecessary risk for a visual redesign.

## Final design

Keep the native title bar and ask Windows DWM for dark caption colors. Style the WPF menu separately.

Conceptual native call pattern:

```csharp
[DllImport("dwmapi.dll")]
private static extern int DwmSetWindowAttribute(
    IntPtr hwnd,
    int attribute,
    ref int value,
    int size);
```

After the HWND exists:

```csharp
WindowInteropHelper helper = new(window);
IntPtr hwnd = helper.Handle;
```

Apply supported dark-mode/caption attributes defensively. Failures should not prevent app startup because chrome color is cosmetic.

The WPF shell underneath uses normal resource brushes/templates:

```csharp
bar.Background = FindBrush("Pda8ShellBackgroundBrush", AppSurface);
```

## Important separation

Never assume:

```text
Window.Background == native caption color
```

`Window.Background` paints the WPF client area. DWM controls the non-client caption.

## Verification

1. title bar is dark navy rather than bright red;
2. minimize/maximize/close remain native;
3. Windows snap layout still works;
4. resize/drag behavior is unchanged;
5. WPF menu under caption uses matching navy/charcoal palette;
6. app still launches on systems where a particular DWM attribute is unavailable.

## Prevention rule

Before custom-building window chrome, determine whether the requirement can be satisfied by native DWM attributes plus ordinary WPF client styling. Preserve OS behavior whenever possible.
