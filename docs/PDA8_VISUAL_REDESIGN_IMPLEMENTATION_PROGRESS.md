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
5. Narrowed `ApplyMainWindowThemeFixups()` by removing duplicate Profile DataGrid header/row-header normalization. The attempted removal of the MainWindow-local ToolTip bridge exposed a Profile-only white-tooltip regression, proving that bridge is still required by legacy XAML. The bridge was restored and operator smoke passed 2026-10-12. MainWindow SystemColors mappings and the local `InfoButtonStyle` compatibility bridge remain intentionally retained.
6. Removed duplicate standalone-window theme application. `DryRunWindow`, `LiveDeploymentReviewWindow`, and `IncidentBundlePreviewWindow` keep their existing single local `ThemeRuntime.Apply(...)` pass, while the global Window Loaded hook skips those three compatibility windows. Operator smoke passed 2026-10-12.
7. Audited and removed the remaining recursive `ThemeRuntime` legacy color-conversion layer. No legacy literal warning/info/muted colors remained in the repository, so the descendant scan and delayed recoloring passes were deleted. Operator smoke passed 2026-10-12.
8. Removed the obsolete `SystemColors.ScrollBarBrushKey` MainWindow override after confirming V11's explicit shared scrollbar template is the sole scrollbar visual owner. Operator smoke passed 2026-10-12.

### Current `ThemeRuntime` boundary after cleanup group 6

`ThemeRuntime` now deliberately owns only:

- base Window background/foreground compatibility for older standalone windows;
- shared tooltip normalization/readability via `ApplyToolTip(...)`.

The Window background/foreground assignment is **not yet removed**. The repository still contains several standalone/fallback windows beyond DRY/LIVE/Incident (for example About, Activity, Git Safety, Access Onboarding, Deployment Session and report artifact surfaces), so a wholesale removal would be broader than the evidence currently supports.

### V12 cleanup group 8 — implemented, awaiting operator smoke

The remaining MainWindow `SystemColors` compatibility bridge is being retired one family at a time.

This slice removes only:

- `SystemColors.HighlightBrushKey`
- `SystemColors.HighlightTextBrushKey`

Evidence for removal:

- current ComboBox item selection is explicitly styled with `AppSelectionBrush` and `AppTextBrush`;
- current Profile/DataGrid selection and PDA-8 interaction states have explicit resource/template owners;
- no code outside `ApplyMainWindowThemeFixups()` references these two SystemColors keys.

The local `selection` brush variable became dead after this removal and was deleted as part of the same slice.

No Window, Control or Info SystemColors mappings were changed in this group.

### Next V12 cleanup group after group 8 smoke

Audit the `SystemColors.InfoBrushKey` / `InfoTextBrushKey` pair next. Remove them only if Profile help/tooltips are fully protected by the restored MainWindow `SharedToolTipStyle` bridge and no remaining legacy control reads the Info SystemColors family.

After that, audit Control and Window mappings separately; do not collapse those families into one removal.

Do **not** remove `ThemeRuntime.Apply(window)` wholesale yet; older standalone windows still need a separate ownership audit.

Smoke requirements after each V12 cleanup group:

- Home and Profile retain the accepted PDA-8 appearance;
- DRY/LIVE/REPORTS embedded surfaces are unchanged;
- standalone/fallback DRY, LIVE review and Incident preview windows remain dark and readable;
- shared `i` controls remain chrome-free;
- Profile and Home tooltips remain dark and readable;
- ComboBox/DataGrid selection remains dark and legible;
- shared scrollbar styling remains intact;
- refresh glyph behavior remains intact;
- no delayed restyling/flicker appears;
- Release test/build/publish succeeds.

## Remaining outside PDA-8 visual cleanup

A separate functional lifecycle slice remains outstanding: **closing the main UI means OFF and process exit; minimize may preserve READY**. This is not cosmetic V11/V12 work and must remain separately guarded.
