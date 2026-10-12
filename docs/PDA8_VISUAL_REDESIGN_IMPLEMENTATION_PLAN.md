# PDA-8 — Native WPF visual redesign implementation plan

**Status:** 🟡 NEXT UI SLICE — implement only after preserving the accepted embedded DRY | LIVE | REPORTS behavior.

**Visual source of truth:** `mocks up/zdeploypet-ui-proposal.html` (current v2 mockup) plus the accepted visual cue reviewed with the operator on 2026-10-11.

This plan translates the accepted HTML mockup into **native C#/WPF**. The HTML is a design specification only; ZDeployPet must not add a browser/WebView dependency just to reproduce the appearance.

The target is a friendly, compact developer tool: dark navy/charcoal foundations, restrained warm red/gold brand accents, GitHub-like clarity, a small VS Code influence, and a visible pet companion without turning the application back into a command centre.

---

# 1. Non-negotiable boundary

This slice changes **visual presentation, spacing, navigation chrome and control templates only**.

It must not change:

- project-owned deployment-script authority;
- PDA-2 bounded deployment-access/session rules;
- script SHA-256 approval rules;
- structurally dry PDA-5 execution;
- PDA-6 immutable live review and exact manual confirmation;
- the global execution gate;
- no-auto-retry behavior;
- reporter truth or canonical/bounded report reads;
- the trusted external OpenSSH/passphrase terminal boundary;
- current DRY / LIVE / REPORTS workspace ownership;
- right-side Dry Run / Live deploy / ZDeployPet / Reports focus rules.

The redesign must be implemented through reusable WPF resources/templates wherever possible, not by adding more one-off runtime fixups.

## 1.1 Critical application lifecycle safety — closing the UI means OFF

A separate lifecycle defect has been observed and must be fixed in the correct functional/lifecycle slice; **do not implement it as part of cosmetic V1–V12 work unless explicitly scheduled**.

Observed failure:

- ZDeploy Pet is turned ON and reaches READY;
- the operator closes the main window with the Windows `X` button;
- the visible UI disappears, but the `ZDeployPet` process can remain alive in the background;
- a later attempt to launch ZDeployPet appears to fail because the previous hidden process is still running;
- recovery currently requires locating the process manually, for example:

```powershell
Get-Process ZDeployPet -ErrorAction SilentlyContinue
```

and terminating the stale process ID before relaunching.

Required invariant:

> **Deployment access may be ON / READY only while the ZDeployPet UI is open or intentionally minimized. If the application UI is closed, ZDeployPet must transition to OFF and the application process must terminate cleanly.**

Required behavior for the future lifecycle implementation:

- clicking the main-window `X` is an **application close**, not a hide-to-background action;
- any normal application command that closes the last/main UI window must also mean **OFF**;
- graceful shutdown must revoke/tear down the bounded deployment-access session before process exit;
- any app-owned SSH-agent/session resources created specifically for the bounded ZDeployPet access lease must be cleaned up according to the existing PDA-2 ownership rules;
- a normal close must not leave a hidden `ZDeployPet.exe` process, dispatcher, tray-less shell, background worker or execution host alive;
- **minimize is different from close**: ON / READY may remain valid while the main window is intentionally minimized because the application is still visibly represented by the normal Windows taskbar/window lifecycle;
- if the window is restored after minimize, the current bounded-access state may continue subject to its existing lease/probe rules;
- if the process is forcibly terminated by the OS/operator, the process itself obviously cannot run managed shutdown code; therefore startup/recovery design must ensure stale app-owned deployment-access artifacts cannot make a later launch appear ON or block normal startup indefinitely;
- startup should detect any recoverable stale app-owned session/agent state left by an abnormal termination and reconcile it safely before allowing ON / READY again;
- application shutdown during an active DRY or LIVE execution must follow an explicit safety policy rather than silently hiding the UI while work continues. The implementation must decide and document whether close is blocked until the execution finishes/cancels, or whether a guarded cancellation/shutdown path is used; no invisible deployment execution is acceptable;
- `Application.Current.Shutdown()` / main-window close ownership and WPF shutdown mode must be reviewed so closing the main window actually terminates the application when no guarded execution prevents it;
- no visual redesign phase may accidentally introduce tray/background behavior unless that becomes a separately approved feature.

Lifecycle smoke acceptance when this is eventually implemented:

1. launch ZDeployPet while LOCKED, close with `X`, verify no `ZDeployPet` process remains;
2. launch, turn ZDeploy Pet ON/READY, close with `X`, verify access becomes OFF and no `ZDeployPet` process remains;
3. immediately relaunch without manual `Get-Process` / `Stop-Process` recovery;
4. minimize while READY and verify the application remains alive and restorable;
5. restore from minimize and verify state remains valid only if the bounded lease/probes still permit it;
6. exercise close during DRY/LIVE execution and verify the documented guarded behavior, with no hidden deployment continuing after the UI is gone;
7. simulate/recover from an abnormal prior termination and verify a fresh launch does not inherit stale READY authority.

This lifecycle rule is a **security/operability invariant**, not merely a UI preference, and must be preserved by all later shell/navigation changes.

---

# 2. Visual language to preserve from the accepted v2 mockup

## 2.1 Overall character

ZDeployPet should feel like a small premium developer utility rather than a deployment dashboard.

Target character:

- dark navy / charcoal background rather than flat black;
- softly separated surfaces rather than large heavy bordered rectangles;
- GitHub-style restraint in borders, typography and information density;
- VS Code-like left navigation rail and terminal/output treatment;
- friendly pet/status card as the emotional anchor;
- warm red/gold accent used sparingly for brand/navigation emphasis;
- semantic blue for DRY;
- semantic green for LIVE and READY;
- purple/neutral warm treatment for REPORTS;
- no saturated red title bar dominating the whole application.

## 2.2 Design tokens

Create named WPF resources rather than scattering literal brushes through code.

Minimum token families:

- `AppBackgroundBrush` — deepest navy/charcoal shell;
- `AppSurfaceBrush` — primary panel surface;
- `AppSurfaceRaisedBrush` — cards / controls / elevated surfaces;
- `AppConsoleBrush` — near-black terminal surface;
- `AppBorderBrush` — subtle neutral border;
- `AppBorderStrongBrush` — selected/raised border;
- `AppTextBrush` — warm near-white primary text;
- `AppMutedTextBrush` — cool muted secondary text;
- `AppBrandAccentBrush` — restrained warm red/gold accent from the v2 mockup;
- `AppDryBrush` / `AppDryBorderBrush` — muted dark blue identity;
- `AppLiveBrush` / `AppLiveBorderBrush` — muted dark green identity;
- `AppReportsBrush` / `AppReportsBorderBrush` — restrained purple/warm report identity;
- `AppSuccessBrush` — READY/PASS green;
- `AppWarningBrush` — warning/attention amber;
- `AppDangerBrush` — destructive/failure red only where semantically justified.

Radii, padding and control heights should also become reusable constants/styles instead of ad-hoc values.

---

# 3. Implementation order — execute in this exact sequence

## 3.1 Phase V1 — theme/resource foundation first

Before restructuring individual screens:

1. inventory current `App.xaml`, `ThemeRuntime` and one-off brush assignments;
2. define the new dark navy/charcoal token palette centrally;
3. add reusable styles/templates for:
   - standard card/panel;
   - raised card;
   - icon-only toolbar button;
   - normal text button;
   - segmented workspace tab;
   - compact console tab;
   - ComboBox/TextBox/input surface;
   - shared `ScrollViewer` / vertical `ScrollBar` treatment for embedded workflow surfaces;
   - tooltip;
   - info (`i`) button;
4. preserve accessibility names/focus behavior;
5. keep existing semantic status colors readable at normal and high-DPI scaling.

**Acceptance:** the application still behaves identically, but shared controls can opt into the new design without runtime patch chains.

## 3.2 Phase V2 — application shell and chrome

Translate the v2 mockup shell into WPF:

- replace the visually dominant bright red title treatment with the dark premium shell direction;
- retain ZDeployPet identity through a small warm accent rather than a full-width saturated bar;
- keep the top `Project / Profile`, `View`, `Help` menus compact;
- active menu/navigation state gets the warm accent only when actually active;
- remove unnecessary visual chrome and excess borders;
- preserve native window behavior, keyboard access and resizing.

Do not custom-draw window controls unless required; prefer stable native window behavior over decorative imitation.

## 3.3 Phase V3 — left navigation rail

Match the mockup's narrow VS Code-like rail:

- Home and Profile remain the primary navigation icons;
- inactive icons are muted;
- active icon gets the restrained warm accent and stronger local surface;
- hover is subtle and neutral;
- the rail stays narrow and never competes with the workspace;
- Profile-only Git safety / discovery sub-toolbar remains contextual to Profile, not Home.

## 3.4 Phase V4 — Home hero and pet card

Rebuild the Home header hierarchy to match the accepted mockup:

- primary heading becomes **Deployment workspace**;
- one short sentence explains the surface;
- remove oversized/repetitive branding text from the main content area;
- ZDeploy Pet card sits at the upper-right of the Home header;
- the card remains the real bounded access toggle;
- READY uses restrained green;
- LOCKED returns to neutral charcoal;
- RUNNING/ATTENTION states remain explicit in text and cannot rely on color alone;
- the pet face/icon stays friendly and compact rather than ornamental.

The pet visual is non-authoritative; the underlying session state remains authoritative.

## 3.5 Phase V5 — compact project/profile context strip

Restyle the existing context line as one compact GitHub-like metadata strip:

`Profile • Project • WSL • Runner • Targets • Destinations`

Requirements:

- no oversized card padding;
- primary labels slightly stronger than values;
- long project paths must truncate/wrap safely without destroying layout;
- context remains read-only on Home.

## 3.6 Phase V6 — primary DRY | LIVE | REPORTS workspace styling

Keep the already accepted functional structure and restyle it to the v2 segmented-control model.

Fixed order:

`DRY | LIVE | REPORTS`

Visual rules:

- one compact segmented group, not three large cards;
- DRY selected = muted dark blue;
- LIVE selected = muted dark green;
- REPORTS selected = restrained report purple/warm identity;
- inactive tabs are quiet neutral surfaces;
- selected state uses border/background/text together, never color alone;
- LOCKED state still shows instructional content for DRY/LIVE;
- READY state still exposes the real embedded DRY/LIVE workflow content;
- no popup regression.

## 3.7 Phase V7 — embedded workflow cards, inputs and shared scrolling

Restyle the existing embedded DRY and LIVE controls without rewriting them.

### DRY

- `Allowlisted dry run` remains the heading;
- approved script state becomes a compact elevated card;
- SHA-256 uses monospace treatment on a darker inset surface;
- approval success uses restrained green text, not a large alert box;
- Target and Release type align cleanly in a two-column form when width allows;
- Start dry run uses the muted blue primary-action treatment.

### LIVE

- proposed live deployment uses the same card grammar;
- immutable-review sections remain visibly separate;
- exact manual confirmation input remains unmistakable;
- armed/validated LIVE action uses green only after the accepted PDA-6 state allows it;
- dangerous/destructive red is not used merely to make LIVE look dramatic.

### Shared embedded workflow scrolling

DRY and LIVE are embedded workflow surfaces inside the Home workspace and can grow taller than the available viewport. Their scroll behavior is therefore part of the shared UI contract.

Requirements:

- both embedded DRY and LIVE surfaces must remain independently **vertically scrollable** when their content exceeds the available Home workspace height;
- reuse the **global PDA-8 `ScrollViewer` / `ScrollBar` style** from the central theme resources rather than using default light OS chrome or separate one-off scrollbar implementations;
- the shared scrollbar should be slim, dark navy/charcoal, visually quiet when idle, and gain a restrained blue/neutral highlight on hover or thumb interaction;
- scrollbars should appear only when required (`Auto`) and must not reserve excessive width when hidden;
- mouse wheel scrolling must operate on the currently selected embedded DRY or LIVE workflow when the pointer/focus is over that workflow;
- keyboard scrolling (`PageUp`, `PageDown`, arrows where applicable, Home/End) must remain usable through normal WPF focus behavior;
- scrolling the left workflow must never move or reset the right-side Activity/Reports console;
- switching `DRY | LIVE | REPORTS` must not transfer stale scroll offset between different workspace surfaces; each selected surface should begin or restore position intentionally rather than inheriting another tab's offset;
- no nested-scroll trap: the embedded workflow owns its internal vertical overflow, while the overall application window and right console remain independently usable;
- the old standalone `DryRunWindow` / `LiveDeploymentReviewWindow` wrappers, while retained for compatibility during refactor, should use the same shared scrollbar resources whenever shown so embedded and standalone presentation do not diverge.

At narrower widths, forms stack cleanly rather than clipping, and vertical scrolling must remain available after the stack transition.

## 3.8 Phase V8 — REPORTS workspace

Keep REPORTS secondary but polished:

- latest deployment report uses the same card language as DRY/LIVE;
- refresh remains icon-only;
- order in the card corner remains `refresh symbol` then `i`;
- report metadata is compact and readable;
- Deployment history uses GitHub-like dense rows with restrained separators;
- PASS/FAILED remain semantically clear;
- Summary / Full log remain small secondary actions;
- clicking them continues to route to the right-side Reports channel.

## 3.9 Phase V9 — Activity & reports pane

Rename/present the right side as **Activity & reports** in the visual hierarchy while preserving its existing functional channels.

Keep exactly:

`Dry Run | Live deploy | ZDeployPet | Reports`

Target treatment:

- darker terminal surface similar to VS Code;
- restrained separators and tab accents;
- selected tab is clear but not boxed heavily;
- profile/session strip stays compact;
- output uses monospace typography;
- SUCCESS/PASS lines may use restrained green;
- warnings/errors use semantic colors only;
- Copy all / Copy selection remain small secondary controls at the bottom;
- the pane remains hideable through `View`.

## 3.10 Phase V10 — Profile page parity

After Home is accepted, apply the same design system to Profile/setup:

- same navy/charcoal foundation;
- same compact heading hierarchy;
- same input styles;
- same card grammar;
- same narrow contextual Git/discovery toolbar;
- no return to the older command-centre density;
- preserve all current profile validation, Git safety and discovery behavior.

## 3.11 Phase V11 — hover/focus/accessibility polish

Only after the static hierarchy is correct:

- consistent hover states;
- visible keyboard focus rings;
- tooltips use one style;
- disabled state remains readable and visually distinct;
- shared scrollbars have consistent idle / hover / drag / disabled states and do not fall back to light OS chrome;
- verify mouse-wheel and keyboard scrolling for embedded DRY and LIVE at normal and narrow widths;
- DPI scaling smoke at 100%, 125%, 150%;
- no tiny icon should be the only way to understand a destructive or authority-changing action;
- verify contrast for muted text, selected tabs and console output.

## 3.12 Phase V12 — remove superseded visual shims last

After the new native styles are operator-smoked:

- search for runtime-only visual patches that are now redundant;
- remove only those proven superseded by the central theme/templates;
- do not delete functional state-refresh hooks simply because they also set visuals;
- rebuild and smoke after each cleanup group.

---

# 4. Implementation discipline

The redesign must be delivered in small buildable commits.

For every phase:

1. change one visual ownership layer only;
2. run `dotnet test ZDeployPet.sln -c Release`;
3. run `scripts/publish-local.ps1`;
4. operator smoke the changed surface;
5. only then continue.

Avoid another late `UiPolishRuntime`-style layer for controls that can be styled at their owner/resource level. The preferred order is:

1. resource/style/template;
2. owning surface layout;
3. state-specific visual refresh;
4. runtime fallback only when WPF construction timing genuinely requires it.

---

# 5. Visual acceptance gate

The redesign is accepted only when all of the following are true:

- the real WPF Home is recognizably close to the accepted v2 HTML mockup;
- the UI is predominantly dark navy/charcoal rather than flat black or bright red;
- brand red/gold is restrained and does not flood the title bar or every button;
- Home reads as a friendly deployment companion, not a command centre;
- Home navigation and Profile navigation have one clear active state;
- ZDeploy Pet remains the obvious READY/LOCKED access anchor;
- DRY | LIVE | REPORTS looks like one compact workspace control;
- DRY/LIVE embedded content remains fully functional and popup-free;
- embedded DRY and LIVE workflows scroll vertically with the shared global dark scrollbar style whenever their content exceeds the viewport;
- embedded workflow scrolling remains independent from the right-side console and does not create nested-scroll or stale-offset regressions;
- Activity & reports remains the primary latest-output surface;
- report history is secondary and does not dominate normal operation;
- controls have consistent radius, padding, typography and border grammar;
- hover/focus/disabled states are coherent;
- no new browser/WebView dependency exists;
- no security or execution invariant changes;
- Release tests/build/publish pass;
- a real Millenova dry smoke and live-review smoke still prove PDA-5/PDA-6 behavior is unchanged.

The separate lifecycle-safety work is also not considered complete until the **close means OFF** invariant from section 1.1 is implemented and smoked: a normal `X` close must leave no hidden ZDeployPet process and no surviving READY deployment authority, while minimize remains the only approved UI-hidden state that may preserve ON/READY.

---

# 6. First code step after this plan

The first implementation commit must be **V1 only: central visual tokens + reusable native WPF styles/templates**.

Do not start by manually recoloring individual Home controls.

Once V1 builds and the application remains functionally unchanged, proceed to V2 shell/chrome, then through the phases above in order.
