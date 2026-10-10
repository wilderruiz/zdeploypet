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
- a live **Console / Activity** surface with current profile/session state plus Copy selection / Copy all support;
- the Console / Activity surface is docked as a persistent **right-side main-shell pane**, is resizable with a splitter, updates live while the operator works elsewhere in ZDeployPet, and can be shown/hidden from **View**;
- console text wraps to the current pane width instead of requiring horizontal resizing/scrolling, and console profile/session labels wrap when the pane is narrow;
- the main operator action footer uses a responsive wrap layout so actions can flow onto additional rows as the left pane becomes narrow instead of being clipped off-screen;
- the older standalone Activity window remains a diagnostics-compatible surface but is no longer the normal operator path;
- local **UI layout memory** stored under the ZDeployPet Local Application Data root so the main-window size/position, maximized state, console-pane width and console visibility reopen the way the operator last left them; invalid/off-screen geometry fails safe and does not block startup;
- a top **Project / Profile**, **View**, **Help** application menu that routes to the existing approved actions rather than duplicating deployment authority;
- an **About ZDeployPet** surface with runtime version/build/platform/.NET/architecture/repository information and an explicit note that public release licensing remains PDA-8 work;
- first-pass application-level dark/red resources applied across the main shell, Activity, Deployment Session, Deployment Access and Git Safety surfaces.

The shell/menu/activity surfaces are presentation and operator-visibility layers only. They do not bypass deployment-access checks, create new arbitrary execution paths, or execute a deployment script.

## Simple operator mode — required PDA-3 outcome

ZDeployPet must become as simple to operate as ZGitPet once a profile has completed first-time onboarding. The detailed security steps remain available, but they move behind progressive disclosure instead of being the normal daily interface.

The normal operator should not need to understand or repeatedly click **Create key**, **Use selected key**, **Enroll host key**, **Install public key**, **Check session**, **Probe targets** and **Lock** as separate concepts after onboarding has succeeded.

### Daily access control

The durable production shell exposes one primary deployment-access control with an obvious state:

- **OFF / LOCKED** — no bounded deployment access is active;
- **TURN ON** — starts the approved bounded session for the current profile; if the approved private key is passphrase-protected, the trusted OpenSSH terminal is allowed to request that passphrase;
- **ON / READY** — the exact approved key is loaded, the lease is valid and required targets have passed the non-writing bounded-agent probe;
- **TURN OFF** — immediately locks/stops the app-owned agent session;
- **ATTENTION** — onboarding is incomplete, a host/key fingerprint changed, the lease expired, or another fail-closed condition requires intervention.

`ON` is never merely a cosmetic toggle. The controller may display ON only after the existing PDA-1/PDA-2 security checks actually pass. Likewise, switching OFF must invoke the real lock path.

The first simple-operator smoke has passed for Millenova: **Turn on** opened the trusted key prompt, ZDeployPet automatically verified the approved key, automatically probed Hostinger and VPS, reached **ON / READY**, and **Turn off** returned the app-owned session to **LOCKED/OFF**.

### First-time setup bundle

When no approved deployment identity exists, the same access surface changes from an ON/OFF control into one guided **Set up deployment access** action. That wizard/bundle owns the current advanced workflow:

1. discover suitable existing public keys;
2. offer **Use existing dedicated key** or **Create ZDeployPet key**;
3. approve the resulting deployment-key fingerprint;
4. scan and explicitly enroll each configured server host fingerprint;
5. install only the public key where needed;
6. perform the non-writing authenticated probe;
7. run Git/private-key safety checks;
8. return the shell to **OFF / READY TO TURN ON** once onboarding is complete.

The advanced Deployment Access and Session windows remain available from a menu such as **Advanced / Deployment access details** for diagnostics, fingerprint inspection, re-enrollment and troubleshooting. They are not the default daily path.

### Progressive-disclosure rule

The main shell should aim for a very small set of operator actions:

- project/profile selector;
- deployment access **ON / OFF**;
- deployment action/status area (introduced in PDA-5/PDA-6);
- the docked, resizable live Activity console for immediate operator feedback, with hide/show available when more workspace is needed.

Security complexity stays in the controller/service layer and in advanced setup screens, not in the number of buttons the operator must understand.

## Planned sequence

1. ✅ wire profile/session state transitions into `ShellStateStore` and lifecycle events into `ShellActivityLog`;
2. 🟡 continue extracting durable shell/view-model boundaries so later surfaces do not accumulate inside `MainWindow`;
3. ✅ add the sanitized Console / Activity surface with copy support;
4. ✅ dock Console / Activity into the right side of the main shell with a resizable splitter and live updates;
5. ✅ remember main-window geometry and Console / Activity pane sizing/visibility across launches;
6. ✅ make the split shell responsive: wrap console text and allow the left action footer to flow onto multiple rows when narrow;
7. ✅ add the top Project/Profile, View, Help and About menu;
8. 🟡 finish the shared dark/red theme and correct remaining light host surfaces, low-contrast text and disabled-button rendering;
9. ✅ add the single deployment-access status/control; continue refining the first-time setup bundle and move detailed access/session controls fully into advanced/diagnostic use;
10. ✅ seed About with real runtime/build/platform/repository information; refine packaged build metadata later in PDA-8;
11. add ZDeployPet icon assets and window/taskbar integration;
12. add the companion/pet as a non-authoritative reflection of shell state;
13. complete keyboard/focus/accessibility smoke and ensure the pet cannot trigger privileged actions.

## Non-goals for PDA-3

PDA-3 does not execute `deploy_millenova.sh` or another project runner. Dry-run execution remains PDA-5 and live deployment remains PDA-6. The future normal operator experience will launch the approved project script from ZDeployPet, but the project-owned script remains the deployment engine.
