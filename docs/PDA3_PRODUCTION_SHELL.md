# PDA-3 — Production shell, companion and operator visibility

**Status:** 🟡 ACTIVE

PDA-3 turns the onboarding-oriented application into the durable ZDeployPet desktop shell without moving deployment authority into UI code.

## Implemented so far

The PDA-3 shell foundation now includes:

- `ShellStateStore` as the shared app-level presentation state source for profile/session status;
- `ShellActivityLog` as a bounded in-memory activity stream;
- redaction for private-key markers and common `password=`, `passphrase=`, `token=` and `secret=` assignments before activity entries reach the UI;
- `ShellRuntime` as the shared process-level state/log owner;
- application, profile, deployment-access, session, probe and Git-safety events entering the sanitized activity stream;
- a live **Console / Activity** window with current profile/session state plus Copy selected / Copy all support;
- a top **Project / Profile**, **View**, **Help** application menu that routes to the existing approved actions rather than duplicating deployment authority;
- an **About ZDeployPet** surface with runtime version/build/platform/.NET/architecture/repository information and an explicit note that public release licensing remains PDA-8 work.

The shell/menu/activity surfaces are presentation and operator-visibility layers only. They do not bypass deployment-access checks, create new arbitrary execution paths, or execute a deployment script.

## Planned sequence

1. ✅ wire profile/session state transitions into `ShellStateStore` and lifecycle events into `ShellActivityLog`;
2. 🟡 continue extracting durable shell/view-model boundaries so later surfaces do not accumulate inside `MainWindow`;
3. ✅ add the sanitized Console / Activity surface with copy support;
4. ✅ add the top Project/Profile, View, Help and About menu;
5. next: move shared theme resources into an application-level dark/red theme and apply them consistently to existing windows;
6. ✅ seed About with real runtime/build/platform/repository information; refine packaged build metadata later in PDA-8;
7. add ZDeployPet icon assets and window/taskbar integration;
8. add the companion/pet as a non-authoritative reflection of shell state;
9. complete keyboard/focus/accessibility smoke and ensure the pet cannot trigger privileged actions.

## Non-goals for PDA-3

PDA-3 does not execute `deploy_millenova.sh` or another project runner. Dry-run execution remains PDA-5 and live deployment remains PDA-6. The future normal operator experience will launch the approved project script from ZDeployPet, but the project-owned script remains the deployment engine.
