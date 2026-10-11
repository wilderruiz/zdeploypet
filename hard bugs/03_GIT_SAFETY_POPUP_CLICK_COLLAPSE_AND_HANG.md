# Hard bug: Git-safety popup collapsed on mouse release and later hung the app

## Initial symptom

The Profile toolbar gained a small Git-safety icon that opened a menu containing actions such as:

```text
Scan repository
Add/update .gitignore safety block
```

The first implementation behaved incorrectly:

- mouse down opened the popup;
- holding the mouse button kept it visible;
- releasing the button immediately closed it;
- a later attempted fix made the entire app appear hung after opening the menu.

This was a classic WPF popup/input-routing problem rather than a deployment problem.

## Why it was difficult

`Popup` is rendered in its own native window. It does not behave exactly like ordinary children in the main WPF visual tree. Click handling, capture, `StaysOpen`, placement target behavior and mouse-up routing can interact in surprising ways.

A naïve toggle looked harmless:

```csharp
anchor.Click += (_, _) => popup.IsOpen = !popup.IsOpen;
```

But if another handler, focus transition, preview event or close-on-outside-click path executes in the same input cycle, the result can be:

```text
open → immediately close
```

or repeated re-entry that makes the UI feel frozen.

## Engineering path

### Phase 1 — prove the issue was popup lifecycle, not the menu command

The actions themselves were temporarily ignored. The important test was whether an empty popup could remain open after a normal click/release.

### Phase 2 — remove overlapping toggle paths

The popup must have one owner for state changes. Avoid simultaneously toggling from:

- `PreviewMouseLeftButtonDown`;
- `MouseLeftButtonUp`;
- `Click`;
- popup close callbacks.

Use one deliberate path.

### Phase 3 — use normal click-to-open semantics

A stable pattern is:

```csharp
Popup popup = new()
{
    Placement = PlacementMode.Bottom,
    PlacementTarget = anchor,
    StaysOpen = false,
    AllowsTransparency = true
};

anchor.Click += (_, _) =>
{
    if (!popup.IsOpen)
        popup.IsOpen = true;
};
```

For a true toggle, guard against close events occurring in the same routed event cycle rather than mixing preview and bubbling handlers.

### Phase 4 — keep menu actions responsible for closing

```csharp
item.Click += (_, _) =>
{
    popup.IsOpen = false;
    action();
};
```

This makes the lifecycle explicit:

```text
anchor opens
→ user chooses action
→ action closes
```

Outside clicks are handled by `StaysOpen = false`.

## Why the app appeared hung

Popup bugs can look like a dead application because a transparent/native popup window may still own mouse capture or focus while the main window stops receiving expected input. The process is alive, but interaction is trapped.

When debugging a suspected popup hang, check whether:

```text
CPU is idle
window still repaints
keyboard focus is trapped
an invisible popup HWND remains active
```

before treating it as a thread deadlock.

## Verification checklist

1. single click opens the menu;
2. mouse release does not close it;
3. moving the pointer into the popup does not close it;
4. clicking outside closes it;
5. selecting `Scan repository` closes it exactly once;
6. opening/closing 20 times does not degrade responsiveness;
7. switching Home/Profile after using the popup still works.

## Prevention rule

For custom WPF popup menus, use one state owner and one input path. Do not stack mouse-down, mouse-up and click toggles for the same popup.
