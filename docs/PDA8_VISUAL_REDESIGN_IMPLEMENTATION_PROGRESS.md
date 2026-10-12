# PDA-8 — Visual redesign implementation progress

**Source plan:** `docs/PDA8_VISUAL_REDESIGN_IMPLEMENTATION_PLAN.md`

**Last updated:** 2026-10-12

This file is the live implementation checkpoint for the PDA-8 native WPF redesign. The detailed design and invariants remain owned by `PDA8_VISUAL_REDESIGN_IMPLEMENTATION_PLAN.md`; this companion exists so implementation progress can be updated in small commits without rewriting the full specification.

## Current status

**Overall:** 🟡 V11 polish in progress. V1–V10 visual implementation is operator-smoked. V11.1 workspace containment, the shared information-button normalization, and V11.2 keyboard focus / disabled-state polish are operator-smoked.

### Completed / operator-smoked

- V1 — central dark navy/charcoal tokens and reusable WPF resources.
- V2 — application shell/chrome direction.
- V3 — narrow Home/Profile navigation rail.
- V4 — Deployment workspace hero and ZDeploy Pet status card.
- V5 — compact Home context strip.
- V6 — compact `DRY | LIVE | REPORTS` segmented workspace.
- V7 — embedded DRY/LIVE workflow presentation.
- V7 scrolling — embedded DRY/LIVE use the shared global dark scrollbar treatment.
- V8 — report card/history presentation and shared Reports viewer routing.
- V9 — right-side `Activity & reports` pane.
- V10 — Profile page parity with the PDA-8 visual system.
- V11.1 — outer Deployment workspace height ownership/containment fix; inner workflow content owns overflow and the outer card remains bounded by the visible left viewport.
- Shared information affordance — all application `i` actions now use the borderless/backgroundless shared treatment; the glyph alone is visible on dark surfaces and highlights on hover/focus.
- Profile regression fix — legacy Profile-local `InfoButtonStyle` no longer visually overrides the shared information affordance. Operator smoke passed 2026-10-12.
- V11.2 — keyboard focus and disabled-state polish for shared information actions and shell/menu actions. Operator smoke passed 2026-10-12; no light OS focus chrome regression observed.

### Confirmed implementation details

- `Pda8InteractionTheme.xaml` owns shared ScrollBar/ScrollViewer interaction chrome.
- `Pda8InfoButtonTheme.xaml` owns the shared lightweight `i` affordance.
- Profile V10 layout preserves its local positioning while normalizing the `i` controls to the shared style.
- The right `Activity & reports` pane remains independently scrollable/usable and is not owned by the left workflow scrollbar.
- V11.1 owns Home workflow vertical containment through the star-sized workspace/content rows. Embedded DRY/LIVE scroll viewers must not also derive their maximum height from `MainWindow.ActualHeight`.

## Current next step — V11.3 DPI / resize / interaction resilience

Finish V11 with a targeted resilience audit before V12 cleanup.

### Code audit finding and correction

The V11.3 audit found one stale V7 height calculation:

```text
scroll.MaxHeight = Math.Max(320, ActualHeight - 360)
```

That calculation predated V11.1 and had become a second competing viewport owner. It was removed on 2026-10-12 so the WPF grid containment introduced by V11.1 remains the sole vertical owner. This avoids shell-height arithmetic that can become inaccurate at non-100% DPI or after resize.

### V11.3 smoke gate

Verify the accepted PDA-8 surfaces at Windows scaling 100%, 125% and 150% where practical:

1. Home at normal size and deliberately short height;
2. DRY embedded workflow with vertical scrolling;
3. LIVE immutable-review workflow with vertical scrolling;
4. REPORTS history scrolling;
5. Profile page, including section `i` controls, ComboBoxes and DataGrid rows;
6. Activity & reports visible and hidden;
7. maximize → restore → repeatedly resize narrower/shorter;
8. Tab / Shift+Tab remains usable after scaling changes;
9. no clipped bottom border, overlapping controls, light OS chrome, double vertical scrollbar or unreachable action appears;
10. semantic DRY/LIVE/REPORTS and READY/LOCKED states remain readable at every tested scale.

No deployment/session/security behavior may change during this verification.

## Remaining after V11.3

- V12 — remove only visual/runtime shims proven superseded by the central resources/templates; rebuild and smoke after each cleanup group.
- `ThemeRuntime` remains a major V12 audit target because it still performs recursive visual-tree recoloring and scrollbar/info-button runtime patching that central PDA-8 resources may now supersede. Do not remove it wholesale: first separate any still-required legacy compatibility behavior from purely visual ownership.
- Separate functional lifecycle slice remains outstanding: **closing the main UI means OFF and process exit; minimize may preserve READY**. This is not cosmetic V11/V12 work and must remain separately guarded.
