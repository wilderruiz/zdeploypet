# PDA-8 — Pet-first UI simplification and embedded report viewer

**Status:** 🟡 ACTIVE — four-tab output/report refactor + ZDeploy Pet access toggle implemented; Release build + operator smoke pending

This plan defines the operator-UX simplification work that must happen before ZDeployPet is treated as public-release ready.

ZDeployPet is already functionally capable of operating the approved project deployment script without requiring the operator to manually drive the normal deployment workflow in a terminal. The project-owned deployment script remains authoritative and is intentionally allowed to evolve independently. ZDeployPet must therefore stay a small deployment companion around that script rather than becoming a visual replacement for the script's internal deployment logic.

The UI goal is to make ZDeployPet feel like a **pet/companion**, not a command centre.

## Product principle

The normal operator workflow should answer three questions quickly:

1. **Am I ready?** — deployment access state.
2. **What can I do now?** — Dry Run or Live deploy.
3. **What happened?** — latest result and, on demand, report/history/detail.

Advanced diagnostics, history, configuration and incident tooling remain available but must not dominate the normal home experience.

The shell should use progressive disclosure: primary actions are visible; secondary/diagnostic information appears only when requested or when an execution requires attention.

## Product boundary

UI simplification must not weaken any accepted PDA-0 through PDA-7 security or execution invariant.

In particular:

- the project-owned deploy script remains authoritative;
- script fingerprint approval remains mandatory;
- deployment access still requires the bounded PDA-2 session;
- dry execution stays structurally dry;
- live execution still requires immutable review and exact manually typed confirmation;
- reporter truth remains authoritative over UI/process state;
- no auto-retry is introduced;
- report viewers remain read-only and canonically bounded;
- incident bundles remain sanitized/no-authority exports.

This phase changes **presentation and navigation**, not deployment authority.

---

# 1. Immediate pre-polish functional cleanup

## 1.1 Deployment consoles retain only one run

The embedded deployment consoles are transient execution surfaces, not history stores.

Required behavior:

- **Dry Run** shows only the latest/current dry execution;
- starting a new dry run removes the previous dry-run console rows;
- **Live deploy** shows only the latest/current live execution;
- starting a new live deployment removes the previous live-deploy console rows;
- Dry Run and Live deploy do not clear one another;
- **ZDeployPet** application/session/probe activity is independent;
- historical deployment output remains available through PDA-4 Summary / Full log artifacts and Deployment history;
- the embedded UI buffer itself remains hard bounded so a long application session cannot create an unbounded WPF text/list workload.

Implemented:

- `bae8c50b785da1e5b296c4a5e805ce28eee53d36` — latest-run-only embedded deployment consoles plus a bounded embedded activity-row buffer.

## 1.2 Deployment history timestamp precision

Deployment history displays the start time with seconds.

Target format:

```text
2026-10-11 06:45:56
```

Latest-report start/finish timestamps already use second-level precision.

Implemented in the current four-tab refactor; Release/operator verification remains pending.

---

# 2. Right pane: exactly four permanent tabs

The current Console / Activity pane becomes a reusable **Output / Reports** surface with **exactly four permanent tabs**.

No per-run or per-report tabs may be created.

## 2.1 Dry Run

Purpose: current/latest single dry-run execution.

- cleared when a new dry run starts;
- receives only Dry run lifecycle + approved script output for that run;
- automatically receives focus when a dry run begins;
- keeps Copy all and Copy selection;
- observational/read-only.

## 2.2 Live deploy

Purpose: current/latest single live deployment.

- cleared when a new live deployment starts;
- receives only Live deploy lifecycle + approved live script output for that run;
- automatically receives focus when a live run begins;
- keeps Copy all and Copy selection;
- observational/read-only.

## 2.3 ZDeployPet

Purpose: bounded/sanitized application diagnostics such as profile/session state, probes, report refreshes, application lifecycle and non-deployment shell events.

It is not cleared when a new dry/live execution starts.

## 2.4 Reports

Purpose: one shared read-only report viewer for **all deployment Summary and Full log artifacts**.

Required behavior:

- clicking Summary anywhere in Latest deployment report or Deployment history loads that Summary into Reports;
- clicking Full log anywhere loads that Full log into Reports;
- Reports replaces whatever report artifact was previously displayed;
- selecting a report automatically exposes/focuses the right pane and selects Reports;
- no normal Summary / Full log popup window is opened;
- no per-report tab is created;
- Copy all and Copy selection are reused;
- canonical-path, bounded-read and UTF-8 behavior from PDA-4 remains authoritative;
- Reports is always read-only.

Implemented in:

- `1fda174b02dd7b604be236f3df7a543e4811beb0` — permanent Reports channel, shared viewer state, four-tab pane, Copy all reuse and restrained tab accents;
- `d923864587dcce6f13eb25362b6d493d63a5f598` — Summary / Full log routing into Reports plus history second precision.

### Reports context header

The pane identifies the currently loaded artifact, for example:

```text
Summary · v4.4.27 · DRY · hostinger · PASS
```

The same status line also includes deployment id, duration and read-only context.

---

# 3. Normal report popup workflow retired

For normal operation:

- Latest report **View summary** routes to Reports;
- Latest report **View full log** routes to Reports;
- Deployment history **Summary** routes to Reports;
- Deployment history **Full log** routes to Reports.

The older `ReportArtifactWindow` implementation may remain in the codebase temporarily, but these normal report actions no longer instantiate it.

Safety/error dialogs may still appear when an artifact cannot be read safely; report **content** itself stays embedded.

---

# 4. Tab visual identity

The four permanent tabs use restrained semantic text accents:

- **Dry Run** — amber/gold;
- **Live deploy** — coral/red;
- **ZDeployPet** — cool blue-gray;
- **Reports** — green/teal.

Labels remain explicit; color is orientation assistance rather than the only state indicator.

This first accent pass is implemented. It can be refined during the larger visual-polish slice after functional smoke.

---

# 5. Larger pet-first shell simplification

After the four-tab/report-viewer slice is verified, simplify the main shell itself.

## 5.1 ZDeploy Pet is the deployment-access action

The companion is no longer a read-only decorative badge. **ZDeploy Pet** is the single normal access launcher/toggle.

Required behavior:

- visible label is **ZDeploy Pet** rather than `ZPet`;
- the companion surface is a real keyboard-focusable button;
- when access is locked/configured, clicking it follows the existing bounded **Turn on** flow;
- if onboarding is incomplete, clicking it opens the existing deployment-access setup flow;
- when access is **ON / READY**, clicking the same companion invokes the existing bounded-session lock path and turns access off;
- no second permanent Turn on / Turn off button is shown in the Deployment access card;
- the Deployment access card remains status/instructional text only;
- the companion is disabled while access verification is busy;
- the companion is also disabled during an active Dry Run or Live deployment so the bounded agent cannot be intentionally locked out from under an active execution;
- no session, fingerprint, target-probe or live-confirmation safety invariant changes.

Implemented in:

- `35c1b903120bdfe969fbec589122a493aaa804b3` — converts the companion visual into an accessible button and renames it **ZDeploy Pet**;
- `8387e3a6e34c3a34e6c72679662ffbcf7fcc54d4` — exposes the companion action and state-aware action guidance;
- `c2d8eef0eca402ba79587aaa615261cfcb391868` — removes the old Deployment access Turn on/Turn off button and keeps the same bounded access logic behind a shared toggle method;
- `7aece50645530fd6a10c916c5f4eb0d8e33ff41f` — wires ZDeploy Pet to the access toggle and disables it during active deployment execution.

Release/operator verification remains pending.

## 5.2 Home becomes task-focused

The normal Home surface should emphasize only:

- ZDeploy Pet / deployment-access state;
- Dry Run;
- Live deploy review;
- compact latest-result summary.

A separate permanent Turn on / Turn off button is intentionally no longer part of the target Home design because the companion itself owns that action.

A user should be able to understand the home screen in roughly five seconds.

## 5.3 Deployment history becomes secondary

The full history table should not permanently dominate Home.

Target direction:

- Home shows a compact latest-result card;
- full Deployment history is opened on demand or in a secondary section/page;
- history remains reporter-backed and authoritative.

## 5.4 Output pane is contextual

The large right output pane should not dominate an idle app.

Target direction:

- collapse/hide when idle by default or make it easy to collapse;
- automatically surface/focus when an execution starts or a report is explicitly opened;
- remember sensible user preference without obscuring primary controls.

## 5.5 Utility controls move out of the permanent bottom shelf

Current secondary actions such as Git safety, Edit profile, Run discovery, Console/viewer controls and Incident preview should move toward a compact **Tools / More** affordance rather than remain permanently exposed after setup/stabilization.

## 5.6 Reduce popup/window count

Audit every secondary window. Keep separate windows only when the task genuinely needs an independent lifetime or the trusted terminal boundary requires it.

Candidate flows to integrate into the main shell or focused in-app surfaces:

- Dry run request;
- Live deployment review;
- routine diagnostics.

The trusted OpenSSH/passphrase terminal boundary remains separate because ZDeployPet must not capture the private-key passphrase.

---

# 6. Pet-first visual hierarchy

ZDeploy Pet becomes the emotional/status and primary access anchor rather than a decorative badge.

Possible presentation states map onto existing authoritative state without replacing it:

- **LOCKED** — resting/locked; click to turn access on/continue setup;
- **READY** — awake/ready; click to turn access off;
- **RUNNING DRY** — busy safe-preview state; access action disabled;
- **RUNNING LIVE** — serious active state; access action disabled;
- **PASS** — brief success reaction;
- **ATTENTION / FAILED** — clear attention state.

ZDeploy Pet remains non-authoritative. Text status and actual session/execution/reporter state remain the source of truth.

---

# 7. Implementation order

1. ✅ latest-run-only Dry Run / Live deploy consoles and embedded hard bound;
2. ✅ display second precision in Deployment history timestamps — implementation complete, smoke pending;
3. ✅ add permanent **Reports** tab to the right pane — implementation complete, smoke pending;
4. ✅ refactor Summary / Full log loading into reusable embedded report-viewer state;
5. ✅ route Latest report Summary / Full log actions to Reports;
6. ✅ route history Summary / Full log actions to Reports;
7. ✅ retire popup viewers from the normal report workflow;
8. ✅ preserve/reuse Copy all + Copy selection for all four tabs;
9. ✅ add first restrained semantic tab accents;
10. ✅ make **ZDeploy Pet** the single normal deployment-access toggle and remove the separate Turn on / Turn off button — implementation complete, smoke pending;
11. 🟡 run Release build + report-viewer/console/access-toggle operator smoke;
12. ⬜ perform broader Home/history/output/tools/popup simplification;
13. ⬜ complete pet-first operator UX smoke before release packaging is considered final.

---

# 8. Acceptance gate for the current functional UI slice

The current refactor is accepted only when:

- exactly four permanent output tabs exist: Dry Run, Live deploy, ZDeployPet, Reports;
- Dry Run shows one latest/current dry run only;
- Live deploy shows one latest/current live run only;
- ZDeployPet retains bounded app activity independently;
- Reports holds one selected Summary or Full log at a time;
- no per-run or per-report tabs accumulate;
- all Latest report and history Summary / Full log actions reuse Reports;
- selecting an artifact automatically focuses Reports;
- Reports clearly identifies selected artifact/run context;
- Copy all / Copy selection work for Reports as well as console tabs;
- history shows seconds;
- normal Summary / Full log clicks no longer create popup viewer windows;
- **ZDeploy Pet** is the only normal Turn on / Turn off launcher;
- clicking ZDeploy Pet when locked follows the existing bounded unlock/setup path;
- clicking ZDeploy Pet when READY locks access through the existing bounded-session path;
- the pet is keyboard accessible and cannot be used to lock access during an active deployment execution;
- PDA-4 canonical containment/bounds/read-only guarantees are unchanged;
- PDA-5/PDA-6 execution and authorization behavior is unchanged;
- Release build/tests pass;
- operator smoke confirms repeated runs/report selections do not grow an unbounded UI console or create window/tab clutter.

---

# 9. Public-release sequencing

This UX work is part of PDA-8 and must happen **before final installer/public-launch acceptance**.

Recommended PDA-8 order from this checkpoint:

1. pet-first functional/UI simplification;
2. clean-clone build/test/publish CI;
3. release secret/artifact audit;
4. deterministic portable package;
5. clean-state portable smoke;
6. installer/upgrade/uninstall lifecycle;
7. packaged icon/shortcut parity;
8. final repository/release-note/public-policy work;
9. final installed + portable security/UX parity smoke.

The goal is not merely to package the current developer-oriented shell. The release should package the simplified, operator-ready ZDeployPet experience.
