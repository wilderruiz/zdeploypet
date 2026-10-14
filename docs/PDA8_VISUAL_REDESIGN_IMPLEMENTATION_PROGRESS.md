# PDA-8 — Visual redesign implementation progress

**Source plan:** `docs/PDA8_VISUAL_REDESIGN_IMPLEMENTATION_PLAN.md`

**Visual parity checkpoint:** `docs/PDA8_VISUAL_PARITY_CHECKPOINT_2026-10-12.md`

**Last updated:** 2026-10-14

This file is the live implementation checkpoint for the PDA-8 native WPF redesign. The detailed design and invariants remain owned by `PDA8_VISUAL_REDESIGN_IMPLEMENTATION_PLAN.md`; this companion exists so implementation progress can be updated in small commits without rewriting the full specification.

## Current status

**Overall:** 🟡 V12 cleanup is in progress, while the frozen visual-parity closure queue is now being implemented before PDA-8 is declared visually finished. V1–V11 visual implementation and resilience work are operator-smoked.

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
- Parity P1 — main/activity split restore now keeps the workspace dominant and the Activity pane secondary. Operator smoke passed 2026-10-12.
- Parity P2 — Profile legacy GroupBox fieldsets replaced by Profile-owned dark rounded section cards. Operator smoke passed 2026-10-12.
- Parity P3 — Profile buttons/inputs/grids softened and compacted without changing behavior. Operator smoke passed 2026-10-12, including dark dropdown selection behavior.
- Parity P4 — Home `DRY | LIVE | REPORTS` now renders as one compact segmented capsule with semantic active colors. Operator smoke passed 2026-10-13 across DRY, LIVE and REPORTS.
- Parity P5 — passive context/report surfaces now rely on layered fills and spacing rather than visible outlines; semantic DRY/LIVE and configuration boundaries remain. Operator smoke passed 2026-10-14 on Home and Profile.

### Confirmed implementation details

- `Pda8InteractionTheme.xaml` owns shared ScrollBar/ScrollViewer interaction chrome.
- `Pda8InfoButtonTheme.xaml` owns the shared lightweight `i` affordance.
- Profile V10 layout preserves its local positioning while normalizing the `i` controls to the shared style.
- The right `Activity & reports` pane remains independently scrollable/usable and is not owned by the left workflow scrollbar.
- V11.1 owns Home workflow vertical containment through the star-sized workspace/content rows. Embedded DRY/LIVE scroll viewers do not derive their maximum height from `MainWindow.ActualHeight`.

## Visual parity checkpoint — 2026-10-12

A fresh side-by-side comparison of the real WPF application against `mocks up/zdeploypet-ui-proposal.html` confirms that the architecture is close but several visible mockup-parity items remain. The detailed ordered closure queue is recorded in `docs/PDA8_VISUAL_PARITY_CHECKPOINT_2026-10-12.md`.

Highest-priority remaining visual gaps:

1. **Main/Activity split proportion:** closed by parity P1 and operator-smoked.
2. **Profile legacy fieldset chrome:** closed by parity P2 and operator-smoked.
3. **Profile control chrome:** closed by parity P3 and operator-smoked.
4. **Home segmented control:** closed by parity P4 and operator-smoked across DRY/LIVE/REPORTS.
5. **Surface hierarchy:** closed by parity P5 and operator-smoked on Home/Profile.
6. **Toolbar icon chrome:** P6 implemented; awaiting operator smoke. Git safety/discovery and related shell icon actions now use quiet transparent default chrome with shared PDA-8 hover/focus treatment.
7. **Warm accent parity:** keep gold/warm accent sparse and consistent across active rail/menu/context states.
8. **Activity & reports density:** preserve the current structure but keep it visually secondary and compact.
9. **Pet-card final polish:** state behavior is accepted; only border/radius/elevation/friendly-anchor parity remains.
10. **Final side-by-side/DPI closure:** compare native WPF against the v2 mockup at matched aspect ratio and smoke 100/125/150% DPI.

Intentional accepted divergence: do **not** custom-draw the Windows title bar merely to reproduce the HTML mockup. Native window controls remain preferred. No WebView/browser dependency is allowed.

These parity items are now part of the implementation plan and must close before PDA-8 can be declared visually finished, even if V12 cleanup itself completes earlier.

### Parity P1 — main/activity proportion

**Implemented and operator-smoked 2026-10-12.**

The saved Activity & reports width is bounded during layout restore to a maximum of 40% of the remembered main-window width. This prevents older saved nearly-50/50 layouts from defeating the accepted mockup hierarchy after relaunch, while still leaving the splitter fully interactive during the current session.

Implementation owner: `MainWindow.LayoutMemory.cs`.

### Parity P2 — Profile section-card ownership

**Implemented and operator-smoked 2026-10-12.**

Profile no longer relies on the stock Windows `GroupBox` fieldset template for its numbered setup sections. `MainWindow.Pda8V10ProfileVisuals.cs` assigns a Profile-only control template that renders each section as one continuous dark rounded card with an internal header and subtle border.

Important ownership boundary:

- the global application `GroupBox` style is intentionally unchanged;
- Access Onboarding and other standalone/fallback surfaces are not affected by this cosmetic parity slice;
- Profile `i` buttons remain independently overlaid using the shared borderless information style;
- existing Profile controls, validation logic, grids and section content are unchanged.

### Parity P3 — Profile control chrome

**Implemented and operator-smoked 2026-10-12.**

Profile-only control styling now softens the remaining rectangular legacy chrome without widening the change to standalone/fallback windows:

- Profile action buttons use rounded dark chrome with restrained warm hover/focus treatment;
- TextBox/ComboBox spacing is tightened and normalized;
- Profile DataGrid presentation uses horizontal separators rather than heavy spreadsheet-like vertical lines;
- the green `Check settings and save` action remains the single primary completion action;
- dropdown and grid selection remain dark/readable.

Implementation owner: `MainWindow.Pda8V10ProfileVisuals.cs`.

### Parity P4 — Home segmented capsule

**Implemented and operator-smoked 2026-10-13.**

The Home `DRY | LIVE | REPORTS` selector presents the three existing buttons as one compact rounded capsule:

- one outer dark capsule/border owns the group silhouette;
- the three existing buttons are re-parented into a nested three-column segment host;
- internal segments have zero gap and only thin separators;
- first/last segments own only the capsule-end radii, while the middle segment remains square;
- existing click handlers and `HomeWorkspaceSection` selection authority are unchanged;
- DRY/LIVE/REPORTS semantic active colors remain authoritative;
- operator smoke confirmed correct geometry and state behavior across all three tabs.

Implementation owner: `MainWindow.Pda8V6SegmentedWorkspace.cs`.

### Parity P5 — quieter passive surface hierarchy

**Implemented and operator-smoked 2026-10-14.**

P5 deliberately removes visible outlines only from passive context/inspection surfaces while preserving borders that communicate state or interaction:

- the Home profile/project context strip uses `AppSurfaceBrush` + spacing with no visible border;
- Latest deployment report and Deployment history cards keep their layered `Pda8RaisedCardStyle` / `Pda8CardStyle` backgrounds and rounded geometry without a visible outline;
- the Profile intro/context card uses surface contrast and spacing without a border;
- Profile numbered section cards retain their subtle accepted boundary from P2;
- DRY/LIVE embedded workflow outlines remain because those borders communicate the selected semantic workspace state;
- terminal/report content boundaries remain intact where they materially aid scanning.

Implementation owners:

- `MainWindow.Pda8ContextStrip.cs`
- `MainWindow.Pda8V8ReportVisuals.cs`
- `MainWindow.Pda8V10ProfileVisuals.cs`

### Parity P6 — quiet toolbar icon chrome

**Implemented 2026-10-14, awaiting operator smoke.**

The contextual shell command icons now reuse the shared PDA-8 icon-button grammar instead of drawing permanent square boxes:

- Git safety and discovery/search actions are transparent and borderless at rest;
- hover/focus uses the existing rounded PDA-8 icon-button surface;
- hit target sizes, automation names and tooltips are unchanged;
- command behavior and Git/discovery authority are unchanged;
- the legacy horizontal Home/Profile command buttons use the same quiet chrome if that fallback toolbar is ever visible.

Implementation owner: `MainWindow.Pda3Menu.cs`.

Smoke acceptance:

- on Profile, Git safety and discovery/search icons no longer sit inside permanent outlined squares;
- hover/focus still gives visible feedback;
- clicking Git safety still opens the existing Git-safety action flow;
- clicking discovery still runs the existing read-only discovery flow;
- adjacent borderless `i` actions remain unchanged;
- no light/native button chrome appears during keyboard focus.

## Current next step — V12 cleanup plus parity closure

V12 removes only visual/runtime compatibility layers that are proven redundant after V1–V11. Each cleanup group must build and be operator-smoked before the next one. The visual parity closure queue above remains a separate acceptance gate; cleanup must not delete bridges that a still-visible legacy surface demonstrably needs.

### V12 cleanup already completed

1. Removed the old `MainWindow.UiPolishRuntime.cs` retry/timer layer after moving the accepted refresh-glyph behavior into the V8 report owner.
2. Removed the old recursive Profile visual-tree fixup after V10 became the explicit Profile visual owner.
3. Removed runtime ScrollBar scanning/fade timers from `ThemeRuntime`; shared scrollbar templates are now owned by `Pda8InteractionTheme.xaml`.
4. Removed runtime `button.Content == "i"` repainting from `ThemeRuntime`; shared information controls are now owned by `Pda8InfoButtonTheme.xaml` and no longer risk being re-boxed by legacy runtime code.
5. Narrowed `ApplyMainWindowThemeFixups()` by removing duplicate Profile DataGrid header/row-header normalization. The attempted removal of the MainWindow-local ToolTip bridge exposed a Profile-only white-tooltip regression, proving that bridge is still required by legacy XAML. The bridge was restored and operator smoke passed 2026-10-12. MainWindow SystemColors mappings and the local `InfoButtonStyle` compatibility bridge remain intentionally retained.
6. Removed duplicate standalone-window theme application. `DryRunWindow`, `LiveDeploymentReviewWindow`, and `IncidentBundlePreviewWindow` keep their existing single local `ThemeRuntime.Apply(...)` pass, while the global Window Loaded hook skips those three compatibility windows. Operator smoke passed 2026-10-12.
7. Audited and removed the remaining recursive `ThemeRuntime` legacy color-conversion layer. No legacy literal warning/info/muted colors remained in the repository, so the descendant scan and delayed recoloring passes were deleted. Operator smoke passed 2026-10-12.
8. Removed the obsolete `SystemColors.ScrollBarBrushKey` MainWindow override after confirming V11's explicit shared scrollbar template is the sole scrollbar visual owner. Operator smoke passed 2026-10-12.
9. Removed the obsolete `SystemColors.HighlightBrushKey` and `SystemColors.HighlightTextBrushKey` MainWindow mappings after verifying explicit ComboBox/DataGrid/PDA-8 selection styling. Operator smoke passed 2026-10-12.
10. Removed the obsolete `SystemColors.InfoBrushKey` and `SystemColors.InfoTextBrushKey` MainWindow mappings while retaining the explicit `SharedToolTipStyle` bridge required by Profile/MainWindow XAML. Operator smoke/build passed 2026-10-12; Profile and Home tooltips remained dark/readable.

### Control SystemColors audit — retain for now

The six remaining MainWindow Control SystemColors mappings were audited before removal:

- `SystemColors.ControlBrushKey`
- `SystemColors.ControlTextBrushKey`
- `SystemColors.ControlLightBrushKey`
- `SystemColors.ControlLightLightBrushKey`
- `SystemColors.ControlDarkBrushKey`
- `SystemColors.ControlDarkDarkBrushKey`

Result after P2/P3: **retain this family until remaining MainWindow/fallback default-template consumers are re-audited.** Profile no longer visibly depends on stock GroupBox/control chrome, but other MainWindow/fallback controls may still rely on these compatibility mappings.

Do not delete this family wholesale yet. Re-run the ownership audit before any removal.

### Current `ThemeRuntime` boundary

`ThemeRuntime` now deliberately owns only:

- base Window background/foreground compatibility for older standalone windows;
- shared tooltip normalization/readability via `ApplyToolTip(...)`.

The Window background/foreground assignment is **not yet removed**. The repository still contains several standalone/fallback windows beyond DRY/LIVE/Incident (for example About, Activity, Git Safety, Access Onboarding, Deployment Session and report artifact surfaces), so a wholesale removal would be broader than the evidence currently supports.

Do **not** remove `ThemeRuntime.Apply(window)` wholesale yet; older standalone windows still need a separate ownership audit.

Smoke requirements after each V12/parity cleanup group:

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
