# PDA-8 — Visual parity checkpoint — 2026-10-12

**Source of truth:** `mocks up/zdeploypet-ui-proposal.html` (v2) plus the accepted operator screenshots.

**Purpose:** freeze the remaining visual delta between the current native WPF application and the accepted mockup before final V12 cleanup continues. This checkpoint is visual only and must not alter deployment authority, DRY/LIVE execution semantics, reporter truth, SSH boundaries, or the existing close-means-OFF lifecycle plan.

## Current assessment

The current WPF UI is structurally close to the mockup and the major architecture is already in place: dark shell, left navigation rail, Home hero, ZDeploy Pet card, embedded DRY/LIVE/REPORTS workspace, Activity & reports pane, shared scrolling, dark tooltips, compact `i` affordances, and Profile parity work.

The remaining gap is no longer architectural. It is primarily **geometry, density, control chrome and surface hierarchy**.

## Intentional differences that are already accepted

Do **not** spend a future parity slice rebuilding these merely to imitate the HTML literally:

- keep the native Windows title bar and standard minimize/maximize/close behavior; the HTML mockup title bar is a visual cue, not a requirement to custom-draw window chrome;
- no WebView/browser dependency;
- the native ZDeploy Pet face/glyph may remain native; parity is about the friendly card hierarchy/state treatment, not reproducing the mockup emoji exactly;
- functional text, security confirmations and operator evidence remain authoritative even if the HTML mockup abbreviates them.

## Remaining parity gaps — execute in this order

### P1 — restore the mockup's left/right workspace proportion

The current application can read visually close to a 50/50 split between the main workspace and `Activity & reports`. The v2 mockup is intentionally main-workspace dominant (`~1.55fr : 0.9fr`, with the right side behaving more like a compact inspector/console).

Required:

- make the main left content clearly dominant at normal desktop widths;
- target roughly 60–65% left / 35–40% right where width permits;
- preserve a useful minimum right-console width rather than shrinking it until logs are unreadable;
- preserve current splitter/resizing behavior if operator-controlled resizing already exists;
- at narrow widths, follow the existing responsive rules rather than forcing the ratio.

Acceptance: at a typical maximized desktop width the eye lands on Deployment workspace first; Activity & reports reads as a secondary inspector, as in the mockup.

### P2 — remove remaining legacy bright Profile `GroupBox`/fieldset chrome

The largest visual mismatch still visible in Profile is the strong pale/white rectangular section outline around:

- `1 — Choose the project you want to deploy`;
- `2 — Choose the deployment script and safety settings`;
- `3 — Add the servers that receive your files`;
- remaining lower Profile sections.

The mockup language calls for soft dark surfaces and restrained separators, not bright legacy fieldset frames.

Required:

- replace the bright GroupBox border with the shared dark subtle card/border grammar;
- retain the section title/legend and section grouping;
- use a low-contrast dark border or raised surface, rounded consistently with PDA-8 cards;
- section `i` remains borderless and sits naturally in the top-right;
- no visual-tree runtime repaint to achieve this: style/template or owning Profile layout only.

Acceptance: Profile no longer looks like legacy Windows forms embedded inside the new shell.

### P3 — finish shared control radius and density parity on Profile

Current Profile buttons/inputs are dark and readable but remain more rectangular/heavy than the v2 mockup.

Required:

- normalize `Load profile`, `Browse…`, `Refresh`, `Forget suggestions`, `Check settings and save` and similar controls to the shared compact button grammar;
- apply the accepted subtle radius, padding and border weights consistently;
- normalize ComboBox/TextBox/DataGrid editing cells to the same input family where technically safe;
- primary/success action remains semantically green only where appropriate;
- secondary actions stay neutral and visually quieter.

Acceptance: Profile controls look like one system rather than a mixture of new shell styling and old WPF control chrome.

### P4 — tighten the Home segmented workspace to the mockup capsule treatment

The functional `DRY | LIVE | REPORTS` implementation is accepted. Final parity should make the three tabs read as one compact segmented control.

Required:

- shared dark capsule/background around the tab group;
- smaller inter-tab gap;
- selected DRY uses muted blue, LIVE muted green, REPORTS restrained purple/warm tone;
- inactive tabs are flatter/quieter;
- no enlargement back into three independent cards.

Acceptance: the tab strip looks visually like the v2 mockup while preserving all current embedded workflow behavior.

### P5 — reduce remaining heavy card borders and flatten hierarchy where the mockup is softer

Some current Home/Profile panels still use medium-contrast rectangular borders that are stronger than the v2 design.

Required:

- audit outer workspace, report cards, profile intro card and metadata strips;
- prefer background/surface contrast and spacing before adding a visible border;
- keep stronger borders only for selected/armed/security-significant states;
- preserve clear boundaries around terminal/report content where they materially aid scanning.

Acceptance: surfaces feel GitHub/VS Code-like and layered rather than boxed-in.

### P6 — final toolbar/icon-button parity

The borderless `i` treatment is accepted. Remaining contextual icon buttons (Git safety, discovery/search and similar toolbar actions) should use the same restrained shell grammar.

Required:

- default state visually quiet on the dark toolbar;
- subtle rounded hover surface;
- active/selected state uses warm accent only when meaningful;
- avoid permanent heavy square borders around every icon;
- retain accessible hit targets and tooltips.

Acceptance: toolbar reads as a compact developer-tool command strip, not a row of legacy buttons.

#### P6.1 — toolbar info glyph placement

The Profile command-strip `i` helpers should read as annotations to Git safety / discovery rather than equal-weight toolbar commands.

Required:

- keep the shared chrome-free `i` behavior and existing tooltip payloads;
- use the shared muted resting foreground and brighten only on hover/focus/press;
- present each toolbar `i` slightly raised beside its command, visually approaching a superscript helper cue;
- retain a practical click/focus target even though the visible glyph is smaller;
- do not change Profile section-card `i` placement or global help-button behavior.

Acceptance: each toolbar action reads as one primary command plus a quiet raised help glyph, matching the accepted visual cue without adding another visible box.

#### P6.2 — nested contextual help flyout

P6.1 proved the muted helper-glyph language, but the operator prefers progressive disclosure so the toolbar can remain even cleaner and scale to many contextual help points.

Required:

- remove the permanently visible toolbar `i` glyphs for Git safety and discovery;
- hovering/focusing a toolbar command opens a compact contextual flyout with the short label (`Git safety` / `Run discovery`) plus one muted nested `i`;
- the nested `i` reuses the existing `HelpTipFactory` detailed payload, so help content keeps one owner;
- the contextual flyout remains open while the pointer moves from the command into the flyout and closes shortly after both lose hover/focus;
- retain the command's existing click action, automation name/help text and accessible hit target;
- do not change section-card `i` controls elsewhere yet; this slice establishes a reusable pattern first.

Acceptance: at rest the Profile command strip shows only the Git/discovery icons. On hover/focus, the short contextual label appears with a quiet nested `i`; moving to that `i` reveals the existing detailed help without cluttering the resting toolbar.

Future use: once operator-smoked, this contextual-help host may be reused for other dense command surfaces where a permanent `i` would add visual noise.

### P7 — shell/menu warm-accent parity

The v2 mockup uses a restrained gold/warm cue for active navigation/menu state rather than broad red highlighting.

Required:

- verify active Home/Profile rail state matches the warm accent hierarchy;
- verify top menu/current context does not appear permanently highlighted when inactive;
- if a current-page/menu underline is retained, use the same restrained warm token consistently;
- do not replace the native Windows title bar.

Acceptance: the warm brand accent is visible but sparse.

### P8 — Activity & reports spacing/density final pass

The right pane is structurally close to the target already.

Required:

- keep heading, subtitle and session strip compact;
- keep console tabs text-forward with a restrained selected underline/accent;
- avoid excessive vertical chrome above the console;
- preserve near-black console surface and monospace content;
- Copy all / Copy selection remain small secondary actions.

Acceptance: the pane reads like a VS Code-style inspector/terminal companion rather than a second equal dashboard.

### P9 — final pet-card visual parity pass

Function/state behavior is accepted. Final polish only:

- keep READY green and LOCKED neutral;
- ensure card border, radius and subtle elevation match the v2 premium-card treatment;
- make pet face/status the friendly visual anchor without enlarging the card;
- preserve text state so color is never the only indicator.

### P10 — final side-by-side parity and DPI checkpoint

After P1–P9:

1. compare real Home against `mocks up/zdeploypet-ui-proposal.html` at the same approximate aspect ratio;
2. compare Profile against the same design language even though the HTML mockup is Home-first;
3. smoke at 100%, 125%, 150% DPI;
4. smoke short, normal and maximized window heights;
5. smoke `Activity & reports` visible and hidden;
6. verify no bright OS fallback controls, white tooltips, default selection blue, default scrollbar chrome or legacy GroupBox borders remain;
7. run Release tests/build/publish and one real Millenova DRY + live-review smoke.

## Execution rule relative to V12 cleanup

Do not blindly continue deleting compatibility mappings while a visible surface still depends on one. Continue V12 in small proven groups, but treat P1–P10 as the **visual closure queue** that must be completed before PDA-8 can be declared visually finished.

If a cleanup step reveals a visible regression, restore only the narrow compatibility bridge that is still demonstrably required and record the owner before continuing.

## Final parity invariant

> The native WPF application does not need to pixel-copy the HTML, but at normal desktop size it should be immediately recognizable as the same design: main workspace dominant, compact right inspector, dark navy/charcoal layered surfaces, soft card chrome, warm sparse accent, compact segmented DRY/LIVE/REPORTS control, friendly ZDeploy Pet status anchor, and no visible legacy Windows-form styling.
