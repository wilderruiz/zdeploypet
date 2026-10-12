# PDA-8 — Visual redesign implementation progress

**Source plan:** `docs/PDA8_VISUAL_REDESIGN_IMPLEMENTATION_PLAN.md`

**Last updated:** 2026-10-12

This file is the live implementation checkpoint for the PDA-8 native WPF redesign. The detailed design and invariants remain owned by `PDA8_VISUAL_REDESIGN_IMPLEMENTATION_PLAN.md`; this companion exists so implementation progress can be updated in small commits without rewriting the full specification.

## Current status

**Overall:** 🟡 V11 polish in progress. V1–V10 visual implementation is operator-smoked. V11.1 workspace containment and the shared information-button normalization are also operator-smoked.

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

### Confirmed implementation details

- `Pda8InteractionTheme.xaml` owns shared ScrollBar/ScrollViewer interaction chrome.
- `Pda8InfoButtonTheme.xaml` owns the shared lightweight `i` affordance.
- Profile V10 layout preserves its local positioning while normalizing the `i` controls to the shared style.
- The right `Activity & reports` pane remains independently scrollable/usable and is not owned by the left workflow scrollbar.

## Current next step — V11.2 shared focus and disabled-state polish

Before V12 cleanup, finish the remaining interaction/accessibility layer centrally rather than adding more one-off runtime patches.

Scope:

1. audit the central Button / input / segmented-tab templates for keyboard focus visibility;
2. ensure keyboard focus is visible without reintroducing bright rectangular chrome on lightweight icon/info actions;
3. normalize disabled foreground/background/border behavior so disabled controls remain readable and clearly unavailable;
4. keep semantic DRY/LIVE/REPORTS selected-state identity intact when controls receive keyboard focus;
5. preserve native Tab / Shift+Tab navigation and existing automation/accessibility names;
6. do not alter deployment authority, session state, execution gates or command behavior;
7. build and publish, then smoke Home + Profile with mouse and keyboard.

### V11.2 smoke gate

- Tab through top menus, Home/Profile rail, DRY/LIVE/REPORTS, primary workflow actions and Profile controls;
- focus is always visible on an actionable control;
- borderless `i` controls remain borderless at rest and on hover, while keyboard focus remains discernible through the glyph treatment;
- disabled buttons/inputs are readable but unmistakably disabled;
- no light OS fallback focus rectangle appears unexpectedly;
- selected DRY/LIVE/REPORTS state remains clear while focused;
- mouse styling is unchanged from the accepted PDA-8 screenshots;
- Release test/build/publish succeeds.

## Remaining after V11.2

- V11 DPI/contrast verification at 100%, 125%, 150% and deliberately narrow/short window sizes.
- V12 — remove only visual/runtime shims proven superseded by the central resources/templates; rebuild and smoke after each cleanup group.
- Separate functional lifecycle slice remains outstanding: **closing the main UI means OFF and process exit; minimize may preserve READY**. This is not cosmetic V11/V12 work and must remain separately guarded.
