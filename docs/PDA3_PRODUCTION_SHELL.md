# PDA-3 — Production shell, companion and operator visibility

**Status:** 🟡 ACTIVE

PDA-3 turns the onboarding-oriented application into the durable ZDeployPet desktop shell without moving deployment authority into UI code.

## First architectural slice

The first slice establishes shared shell state and activity infrastructure before cosmetic work:

- `ShellStateStore` is the single app-level presentation state source for profile/session status that future shell surfaces can bind to without reaching into `MainWindow` internals;
- `ShellActivityLog` is a bounded in-memory activity stream for later Console / Activity UI;
- activity messages pass through an initial redaction layer for private-key markers and common `password=`, `passphrase=`, `token=` and `secret=` assignments;
- `ShellRuntime` owns the shared state/log services for the desktop process;
- app startup/exit now enter sanitized lifecycle events into the activity stream.

This is intentionally not the final logger, view model layer, persistence policy or console UI. It is the seam that later PDA-3 work will use so session/execution/reporting code does not continue accumulating directly inside `MainWindow`.

## Planned sequence

1. wire profile/session state transitions into `ShellStateStore` and lifecycle events into `ShellActivityLog`;
2. add a dedicated shell view-model/service boundary around the main production window;
3. add the sanitized Console / Activity surface with copy support;
4. add the top Project/Profile, View, Help and About menu;
5. move shared theme resources into an application-level dark/red theme and apply them consistently to existing windows;
6. add real build/version/platform/repository information to About;
7. add ZDeployPet icon assets and window/taskbar integration;
8. add the companion/pet as a non-authoritative reflection of shell state;
9. complete keyboard/focus/accessibility smoke and ensure the pet cannot trigger privileged actions.

## Non-goals for PDA-3

PDA-3 does not execute `deploy_millenova.sh` or another project runner. Dry-run execution remains PDA-5 and live deployment remains PDA-6. The future normal operator experience will launch the approved project script from ZDeployPet, but the project-owned script remains the deployment engine.
