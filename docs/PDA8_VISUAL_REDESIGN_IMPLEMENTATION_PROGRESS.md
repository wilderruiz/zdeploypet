# PDA-8 — Visual redesign implementation progress

**Source plan:** `docs/PDA8_VISUAL_REDESIGN_IMPLEMENTATION_PLAN.md`

**Last updated:** 2026-10-12

This file is the live implementation checkpoint for the PDA-8 native WPF redesign. The detailed design and invariants remain owned by `PDA8_VISUAL_REDESIGN_IMPLEMENTATION_PLAN.md`; this companion exists so implementation progress can be updated in small commits without rewriting the full specification.

## Current status

**Overall:** 🟡 V12 cleanup in progress. V1–V11 visual implementation and resilience work are operator-smoked.

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
- Shared information affordance — all application `i` actions use the borderless/backgroundless shared treatment; the glyph alone is visible on dark surfaces and highlights on hover/focus.
- Profile regression fix — legacy Profile-local `InfoButtonStyle` no longer visually overrides the shared information affordance. Operator smoke passed 2026-10-12.
- V11.2 — keyboard focus and disabled-state polish for shared information actions and shell/menu actions. Operator smoke passed 2026-10-12; no light OS focus chrome regression observed.
- V11.3 — DPI/resize resilience audit. The stale `MainWindow.ActualHeight - 360` embedded-workflow height calculation was removed so V11.1 remains the sole Home vertical viewport owner. Operator smoke passed 2026-10-12.

### Confirmed implementation details

- `Pda8InteractionTheme.xaml` owns shared ScrollBar/ScrollViewer interaction chrome.
- `Pda8InfoButtonTheme.xaml` owns the shared lightweight `i` affordance.
- Profile V10 layout preserves its local positioning while normalizing the `i` controls to the shared style.
- The right `Activity & reports` pane remains independently scrollable/usable and is not owned by the left workflow scrollbar.
- V11.1 owns Home workflow vertical containment through the star-sized workspace/content rows. Embedded DRY/LIVE scroll viewers do not derive their maximum height from `MainWindow.ActualHeight`.

## Current next step — V12 cleanup of superseded visual/runtime shims

V12 removes only visual/runtime compatibility layers that are proven redundant after V1–V11. Each cleanup group must build and be operator-smoked before the next one.

### V12 cleanup already completed

1. Removed the old `MainWindow.UiPolishRuntime.cs` retry/timer layer after moving the accepted refresh-glyph behavior into the V8 report owner.
2. Removed the old recursive Profile visual-tree fixup after V10 became the explicit Profile visual owner.
3. Removed runtime ScrollBar scanning/fade timers from `ThemeRuntime`; shared scrollbar templates are now owned by `Pda8InteractionTheme.xaml`.
4. Removed runtime `button.Content == "i"` repainting from `ThemeRuntime`; shared information controls are now owned by `Pda8InfoButtonTheme.xaml` and no longer risk being re-boxed by legacy runtime code.

### V12 cleanup group 4 — implemented, awaiting operator smoke

`ApplyMainWindowThemeFixups()` has been narrowed further:

- removed the MainWindow-local ToolTip resource override because V11 now supplies the application-wide implicit ToolTip style;
- removed duplicate `TargetsGrid` / `DestinationsGrid` header and row-header normalization because V10 is the explicit Profile DataGrid owner;
- retained the MainWindow SystemColors dark mappings for legacy XAML controls;
- retained the local `InfoButtonStyle` compatibility bridge because `MainWindow.xaml` still declares/uses that legacy key and it must continue deriving from the shared chrome-free info style.

This cleanup changes no deployment/profile behavior and should be visually neutral.

### `ThemeRuntime` compatibility boundary after cleanup group 3

`ThemeRuntime` is intentionally **not** removed wholesale yet. It still performs compatibility work for older standalone windows that retain legacy literal colors:

- converts old muted TextBlock foreground literals to the current muted-text token;
- converts old warning/info Border backgrounds to current dark surfaces;
- converts old warning/neutral Border colors to the current border token;
- applies the shared tooltip style and normalizes old tooltip text;
- is still invoked by standalone compatibility windows such as DRY, LIVE review and Incident bundle preview.

These remaining responsibilities must be audited before any further deletion.

### Next V12 cleanup group after group 4 smoke

Audit the duplicate per-window `ThemeRuntime.Apply(...)` calls. `App.xaml.cs` already invokes `ThemeRuntime.Apply(window)` for every loaded Window, so standalone DRY/LIVE/Incident windows may be doing a second redundant recursive pass. Remove those local calls only after confirming the global class-handler timing fully covers their legacy surfaces.

Smoke requirements after each V12 cleanup group:

- Home and Profile retain the accepted PDA-8 appearance;
- DRY/LIVE/REPORTS embedded surfaces are unchanged;
- standalone/fallback DRY, LIVE review and Incident preview windows remain dark and readable;
- shared `i` controls remain chrome-free;
- shared scrollbar styling remains intact;
- refresh glyph behavior remains intact;
- no delayed restyling/flicker appears;
- Release test/build/publish succeeds.

## Remaining outside PDA-8 visual cleanup

A separate functional lifecycle slice remains outstanding: **closing the main UI means OFF and process exit; minimize may preserve READY**. This is not cosmetic V11/V12 work and must remain separately guarded.
