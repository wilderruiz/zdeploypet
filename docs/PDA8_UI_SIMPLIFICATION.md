# PDA-8 — Pet-first UI simplification and embedded report viewer

**Status:** 🟡 ACTIVE — plan approved before implementation

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

Before restructuring the overall shell, stabilize the long-running console/viewer behavior.

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

Implementation checkpoint already completed before this document:

- `bae8c50b785da1e5b296c4a5e805ce28eee53d36` — latest-run-only embedded deployment consoles plus a bounded embedded activity-row buffer.

## 1.2 Deployment history timestamp precision

Deployment history must display the start time with seconds.

Target format:

```text
2026-10-11 06:45:56
```

not minute-only:

```text
2026-10-11 06:45
```

Reason: deployments can occur within the same minute, so second precision materially improves run identification.

Latest-report start/finish timestamps should remain internally consistent with the same second-level precision wherever a concise local timestamp is shown.

---

# 2. Right pane: exactly four permanent tabs

The current Console / Activity pane becomes a reusable output/report surface with **exactly four permanent tabs**.

No per-run tabs may be created.

## 2.1 Dry Run

Purpose: current/latest single dry-run execution.

Behavior:

- cleared when a new dry run starts;
- receives only Dry run lifecycle + approved script output for that run;
- automatically receives focus when a dry run begins;
- keeps existing Copy all and Copy selection behavior;
- remains observational/read-only.

## 2.2 Live deploy

Purpose: current/latest single live deployment.

Behavior:

- cleared when a new live deployment starts;
- receives only Live deploy lifecycle + approved live script output for that run;
- automatically receives focus when a live run begins;
- keeps existing Copy all and Copy selection behavior;
- remains observational/read-only.

## 2.3 ZDeployPet

Purpose: ZDeployPet application diagnostics.

Contains bounded/sanitized app-level activity such as:

- profile/session state;
- target probes;
- report refresh/activity;
- application lifecycle and diagnostics;
- non-deployment shell events.

It is not cleared when a new dry/live execution starts.

## 2.4 Reports

Purpose: one shared read-only report viewer for **all deployment Summary and Full log artifacts**.

This tab is reused one artifact at a time.

Required behavior:

- clicking **Summary** anywhere in Latest deployment report or Deployment history loads that Summary into Reports;
- clicking **Full log** anywhere loads that Full log into Reports;
- the Reports tab replaces whatever report artifact was previously displayed;
- clicking a report action automatically switches/focuses the right pane to Reports;
- no new popup/window is opened for normal Summary or Full log viewing;
- no new per-report tab is created;
- existing Copy all and Copy selection controls are reused;
- canonical-path, bounded-read and UTF-8 behavior from PDA-4 remains authoritative;
- Reports is always read-only.

### Reports context header

The pane must clearly identify what is currently open, for example:

```text
Summary · v4.4.27 · DRY · hostinger · PASS
```

or:

```text
Full log · v4.4.26 · LIVE · hostinger · PASS
```

A second compact line may include deployment id / duration where useful.

The operator must never have to guess which historical run is currently displayed.

---

# 3. Retire normal Summary / Full log popup workflow

The current deployment artifact popup windows were useful during PDA-4 development but add unnecessary window fragmentation in the mature app.

For normal operation:

- Latest report **View summary** routes to Reports;
- Latest report **View full log** routes to Reports;
- Deployment history **Summary** routes to Reports;
- Deployment history **Full log** routes to Reports.

The old viewer implementation may be retained temporarily as internal/fallback code during migration, but the normal operator workflow must not launch those separate windows once this slice is accepted.

This is the first deliberate popup reduction before the larger UI-polish work.

---

# 4. Tab visual identity

The four permanent tabs should be distinguishable with restrained accents rather than large blocks of color.

Suggested semantic accents:

- **Dry Run** — muted amber/gold;
- **Live deploy** — red/coral;
- **ZDeployPet** — neutral/cool blue-gray;
- **Reports** — green/teal or another clearly distinct read-only accent.

Use accent through one or more of:

- selected-tab underline/border;
- small status dot/icon;
- subtle selected-background tint;
- text accent.

Do **not** turn the pane into four bright saturated blocks. ZDeployPet should remain calm and readable.

Color is orientation assistance, not the only indication of state; labels remain explicit for accessibility.

---

# 5. Larger pet-first shell simplification

After the four-tab/report-viewer slice is stable, simplify the main shell itself.

## 5.1 Home becomes task-focused

The normal Home surface should emphasize only:

- ZPet state / deployment-access state;
- primary access action: Turn on / Turn off;
- Dry Run action;
- Live deploy review action;
- compact latest-result summary.

A user should be able to understand the home screen in roughly five seconds.

## 5.2 Deployment history becomes secondary

The full history table should not permanently dominate Home.

Target direction:

- Home shows a compact latest-result card;
- full Deployment history is opened on demand or in a secondary section/page;
- history remains reporter-backed and authoritative.

## 5.3 Output pane is contextual

The large right output pane should not dominate an idle app.

Target direction:

- collapse/hide when idle by default or make it easy to collapse;
- automatically surface/focus when an execution starts or a report is explicitly opened;
- remember sensible user preference without obscuring primary controls.

## 5.4 Utility controls move out of the permanent bottom shelf

Current secondary actions such as:

- Git safety;
- Edit profile;
- Run discovery;
- Console/viewer controls;
- Incident preview;

should move toward a compact **Tools / More** affordance or other secondary navigation rather than remain permanently exposed after setup/stabilization.

## 5.5 Reduce popup/window count

Audit every secondary window.

Keep separate windows only when the task genuinely needs an independent lifetime or the trusted terminal boundary requires it.

Candidate flows to integrate into the main shell or focused in-app surfaces:

- Dry run request;
- Live deployment review;
- report viewing;
- routine diagnostics.

The trusted OpenSSH/passphrase terminal boundary is different: ZDeployPet must continue to avoid capturing the private-key passphrase itself.

---

# 6. Pet-first visual hierarchy

ZPet becomes the emotional/status anchor rather than a decorative badge.

The pet can communicate the existing state machine without inventing new authority:

- **LOCKED** — resting/locked;
- **READY** — awake/green/ready;
- **RUNNING DRY** — busy but safe-preview state;
- **RUNNING LIVE** — serious active state;
- **PASS** — brief success reaction;
- **ATTENTION / FAILED** — clear attention state.

ZPet remains non-authoritative. Text status and actual session/execution/reporter state remain the source of truth.

---

# 7. Implementation order

Implement in this order so functional cleanup precedes visual redesign:

1. ✅ latest-run-only Dry Run / Live deploy consoles and embedded hard bound;
2. ⬜ display second precision in Deployment history timestamps;
3. ⬜ add the permanent **Reports** tab to the right pane;
4. ⬜ refactor Summary / Full log artifact loading into a reusable embedded report-viewer state;
5. ⬜ route Latest report Summary / Full log actions to Reports;
6. ⬜ route history Summary / Full log actions to Reports;
7. ⬜ retire popup viewers from the normal report workflow;
8. ⬜ preserve/reuse Copy all + Copy selection for all four tabs;
9. ⬜ add restrained semantic tab accents and accessibility-safe selected states;
10. ⬜ run report-viewer/console regression smoke;
11. ⬜ then perform broader Home/history/output/tools/popup simplification;
12. ⬜ complete pet-first operator UX smoke before release packaging is considered final.

---

# 8. Acceptance gate for the four-tab slice

The embedded output/report refactor is accepted only when:

- exactly four permanent tabs exist: Dry Run, Live deploy, ZDeployPet, Reports;
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
