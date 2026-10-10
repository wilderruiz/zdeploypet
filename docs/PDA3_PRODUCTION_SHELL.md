# PDA-3 — Production shell, companion and operator visibility

**Status:** ✅ ACCEPTED 2026-10-10

PDA-3 turns the onboarding-oriented application into the durable ZDeployPet desktop shell without moving deployment authority into UI code.

## Accepted outcome

The accepted PDA-3 shell includes:

- `ShellStateStore` as the shared app-level presentation state source for profile/session status;
- `ShellActivityLog` as a bounded in-memory activity stream with redaction for private-key markers and common `password=`, `passphrase=`, `token=` and `secret=` assignments before entries reach the UI;
- `ShellRuntime` as the shared process-level state/log owner;
- application, profile, deployment-access, session, probe and Git-safety events entering the sanitized activity stream;
- a persistent, resizable **Console / Activity** pane docked on the right side of the main shell, updating live while the operator works elsewhere;
- plain-text console output that wraps to the current pane width, plus responsive Profile/Session/header/footer layout and copy support;
- a responsive operator pane whose footer actions wrap onto additional rows when narrow;
- local UI layout memory under ZDeployPet Local Application Data so main-window geometry, maximized state, console-pane width and console visibility reopen as the operator left them;
- a top **Project / Profile**, **View**, **Help** application menu and an **About ZDeployPet** surface with runtime/build/platform/.NET/architecture/repository information;
- a consistent dark/red product theme across the main shell and supporting windows;
- the single daily deployment-access control: **OFF / Turn on / ON-READY / Turn off / ATTENTION**, backed by the actual PDA-1/PDA-2 security controller rather than a cosmetic toggle;
- detailed Deployment Access and Session windows retained as advanced/diagnostic paths rather than the normal daily workflow;
- a reusable `PetCompanionControl` with distinct **LOCKED / RUNNING / READY / ERROR** visual states, explicitly non-interactive and non-authoritative;
- a reusable vector `ProductIconFactory` applied automatically to every WPF window for consistent runtime window/taskbar identity; packaged executable/installer/shortcut binary icon work remains PDA-8;
- single-instance Windows behavior: a second launch activates the existing ZDeployPet shell rather than creating another process/window;
- a shared `AccessibilityRuntime` that supplies stable automation names and visible keyboard-focus cues while ensuring ZPet is non-focusable and not a tab stop.

The shell/menu/activity/pet surfaces remain presentation and operator-visibility layers only. They do not bypass deployment-access checks, create arbitrary execution paths, or execute a deployment script.

## Daily access control acceptance

The Millenova published-development smoke passed the complete simple-operator access path:

1. **Turn on** opened the trusted OpenSSH key prompt;
2. ZDeployPet automatically verified the approved deployment-key fingerprint;
3. ZDeployPet automatically probed Hostinger and VPS through the bounded agent;
4. both probes passed without a remote account-password prompt;
5. the shell, Console / Activity and ZPet all reached **ON / READY** consistently;
6. **Turn off** stopped the app-owned ssh-agent and returned the shell, console session indicator and ZPet to **LOCKED / OFF**.

## UI/operator acceptance evidence

Operator smoke also passed for:

- dark/red shell and supporting windows;
- responsive split-pane resizing in both directions;
- responsive/wrapping console text;
- responsive console header, state strip and footer actions;
- responsive left-pane footer actions;
- persisted window and console-pane sizing across restart;
- top menu and About surface;
- shared structured `i` help and corrected tooltip contrast;
- deployment-access selectors and profile inputs under dark theme;
- Git Safety and Console tooltip placement cleanup;
- runtime window/taskbar icon identity across main and secondary windows;
- ZPet placement and LOCKED → RUNNING → READY → LOCKED state transitions;
- second-launch activation of the existing single instance;
- Tab / Shift+Tab keyboard navigation, focus cues, accessibility labels and pet skip behavior.

## Progressive-disclosure rule retained

The normal operator shell stays intentionally small:

- project/profile context;
- deployment access **ON / OFF**;
- deployment action/status area introduced in PDA-5/PDA-6;
- docked live Console / Activity pane with hide/show support.

Security complexity stays in controller/service layers and advanced setup screens, not in the number of buttons the normal operator must understand.

## Deferred work

The following items are intentionally outside PDA-3:

- packaged executable/installer/desktop-shortcut binary icons and release metadata — PDA-8;
- read-only authoritative deployment-report monitoring — PDA-4;
- dry-run execution — PDA-5;
- live deployment authorization/execution — PDA-6;
- incident bundle/handoff — PDA-7.

## Non-goals satisfied

PDA-3 never executed `deploy_millenova.sh` or another project runner. Dry-run execution remains PDA-5 and live deployment remains PDA-6. The project-owned script remains the deployment engine.