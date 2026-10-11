# PDA-8 — Pet-first UI simplification and embedded report viewer

**Status:** 🟡 ACTIVE — current report/output/access-toggle smoke passed; next slice is the embedded Home **DRY | LIVE | REPORTS** workspace

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

Implemented and operator-smoked.

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

The four permanent right-pane tabs use restrained semantic accents. Labels remain explicit; color is orientation assistance rather than the only state indicator.

The Home deployment actions use stronger readiness identity:

- **DRY enabled** — faded dark blue;
- **LIVE enabled** — dark green;
- locked/unavailable actions — neutral dark surface.

This is implemented and operator-smoked.

---

# 5. Larger pet-first shell simplification

## 5.1 ZDeploy Pet is the deployment-access action

The companion is no longer a read-only decorative badge. **ZDeploy Pet** is the single normal access launcher/toggle.

Required behavior:

- visible label is **ZDeploy Pet** rather than `ZPet`;
- the companion surface is a real keyboard-focusable button;
- when access is locked/configured, clicking it follows the existing bounded **Turn on** flow;
- if onboarding is incomplete, clicking it opens the existing deployment-access setup flow;
- when access is **ON / READY**, clicking the same companion invokes the existing bounded-session lock path and turns access off;
- no second permanent Turn on / Turn off button is shown in the Deployment access card;
- the companion is disabled while access verification is busy;
- the companion is also disabled during an active Dry Run or Live deployment so the bounded agent cannot be intentionally locked out from under an active execution;
- no session, fingerprint, target-probe or live-confirmation safety invariant changes.

Implemented in:

- `35c1b903120bdfe969fbec589122a493aaa804b3` — converts the companion visual into an accessible button and renames it **ZDeploy Pet**;
- `8387e3a6e34c3a34e6c72679662ffbcf7fcc54d4` — exposes the companion action and state-aware action guidance;
- `c2d8eef0eca402ba79587aaa615261cfcb391868` — removes the old Deployment access Turn on/Turn off button and keeps the same bounded access logic behind a shared toggle method;
- `7aece50645530fd6a10c916c5f4eb0d8e33ff41f` — wires ZDeploy Pet to the access toggle and disables it during active deployment execution.

Operator smoke passed.

## 5.2 Home becomes task-focused

The normal Home surface emphasizes:

- ZDeploy Pet / deployment-access state;
- the task workspace described in §5.7;
- compact profile/project context;
- the contextual right-side Output / Reports pane.

A separate permanent Turn on / Turn off button is intentionally no longer part of Home because the companion itself owns that action.

A user should be able to understand the home screen in roughly five seconds.

## 5.3 Deployment history becomes secondary

The full history table must not permanently dominate Home.

Deployment history moves into the **REPORTS** workspace tab described in §5.7. It remains reporter-backed and authoritative.

## 5.4 Output pane is contextual

The right pane is the fast/latest truth surface rather than the place for persistent controls.

Its four permanent channels remain:

- Dry Run;
- Live deploy;
- ZDeployPet;
- Reports.

Executions and selected report artifacts continue to focus the appropriate right-side channel automatically. The user may hide/show the pane through **View**.

## 5.5 Utility controls leave Home

Secondary actions should not permanently occupy the normal Home surface.

Current direction:

- Profile/setup owns profile editing, Git safety and discovery;
- Incident bundle preview lives under **Help**;
- Output / Reports visibility lives under **View**;
- Home remains deployment-task focused.

## 5.6 Reduce popup/window count

Keep separate windows only when the task genuinely needs an independent lifetime or the trusted terminal boundary requires it.

The trusted OpenSSH/passphrase terminal remains separate because ZDeployPet must not capture the private-key passphrase.

Normal Dry Run and Live Review are explicitly scheduled for embedding in §5.7.

## 5.7 NEXT — embedded Home task workspace: DRY | LIVE | REPORTS

This is the **next implementation slice** after the current smoke checkpoint.

### 5.7.1 Target interaction model

The left side of Home becomes one tabbed operator workspace with this fixed order:

```text
DRY | LIVE | REPORTS
```

The two sides of Home then have deliberately different jobs:

**Left — deliberate task / inspection workspace**

- **DRY** — configure and launch the existing allowlisted dry-run flow;
- **LIVE** — create/review/confirm the existing immutable live-deployment flow;
- **REPORTS** — inspect the larger Latest deployment report and Deployment history surfaces.

**Right — fast/latest dynamic truth**

- **Dry Run** — current/latest dry execution output;
- **Live deploy** — current/latest live execution output;
- **ZDeployPet** — bounded app/session/probe activity;
- **Reports** — the one currently selected Summary or Full log artifact.

The right pane remains the place the operator watches during normal execution. The left REPORTS tab is deliberately secondary, for larger/history inspection.

### 5.7.2 Workspace shell first

Implementation must begin by creating the left-side three-tab workspace **before** moving Dry or Live logic.

Required behavior:

- tab order is always **DRY | LIVE | REPORTS**;
- only one left workspace tab is visible at a time;
- **REPORTS** remains available when deployment access is LOCKED;
- **DRY** and **LIVE** retain existing READY/execution-gate availability rules;
- DRY uses the existing dark-blue identity;
- LIVE uses the existing dark-green identity;
- REPORTS receives a restrained amber/gold identity;
- selected state is stronger than idle state but must not rely on color alone;
- no execution or authorization logic changes in this step.

### 5.7.3 Move existing report/history content into REPORTS

After the shell exists, move the existing Home report content as a unit into the **REPORTS** workspace:

- Latest deployment report;
- refresh symbol + report help;
- View summary / View full log actions;
- Deployment history table;
- Summary / Full log history actions.

The report actions continue to route content to the **right-side Reports channel** exactly as they do now. This move changes ownership/layout only; PDA-4 canonical-path/read-only/bounded guarantees remain unchanged.

At this checkpoint the old always-visible report/history area is gone from Home. Selecting **REPORTS** reveals it.

### 5.7.4 Extract DRY into one reusable surface

Refactor the existing `DryRunWindow` presentation into a reusable embedded Dry Run surface rather than reimplementing its logic.

The embedded DRY surface must preserve the existing PDA-5 behavior, including:

- configured/approved project deployment script only;
- current script SHA-256 fingerprint state;
- explicit script fingerprint approval when required;
- allowlisted target choices only;
- bounded release choices only;
- structurally forced dry-run mode;
- READY target/session checks;
- global execution gate;
- no arbitrary command field;
- authoritative reporter refresh after execution;
- current/latest output streaming to the right-side **Dry Run** channel.

The reusable surface becomes the single implementation of Dry Run presentation/interaction. Do **not** duplicate the guard/executor flow between popup and Home.

Normal **DRY** selection on Home must stop creating a popup window once the embedded surface is operational.

### 5.7.5 DRY acceptance smoke before touching LIVE

Before refactoring LIVE, verify DRY end-to-end:

1. LOCKED state disables DRY but REPORTS remains usable;
2. READY enables DRY and shows its blue active identity;
3. selecting DRY shows the embedded existing Dry Run controls;
4. script fingerprint approval behavior is unchanged;
5. target/release selection is unchanged;
6. Start dry run performs no live deployment;
7. right-side **Dry Run** automatically receives/focuses the current run;
8. authoritative latest report/history refresh after completion;
9. repeated DRY tab switching creates no windows and loses no critical state unexpectedly;
10. Release build/tests pass.

Only after this smoke passes should the LIVE surface be refactored.

### 5.7.6 Extract LIVE into one reusable surface

Refactor `LiveDeploymentReviewWindow` presentation into a reusable embedded Live Review surface, again preserving one implementation of the existing PDA-6 behavior.

The embedded LIVE surface must preserve:

- allowlisted target/release choices;
- approved/current script fingerprint validation;
- immutable review snapshot;
- exact configured confirmation phrase display;
- blank manual confirmation input;
- explicit typed-confirmation validation;
- invalidation when target/release/script/access/execution state changes;
- immediate pre-launch revalidation;
- READY bounded-agent runtime requirement;
- global execution gate;
- no automatic retry;
- authoritative reporter refresh after execution;
- current/latest output streaming to the right-side **Live deploy** channel.

Normal **LIVE** selection on Home must stop creating a popup window once the embedded surface is operational.

### 5.7.7 LIVE acceptance smoke

Verify LIVE independently after embedding:

1. LOCKED state disables LIVE;
2. READY enables LIVE and shows its green active identity;
3. selecting LIVE shows the embedded review controls;
4. creating a review performs no deployment;
5. changing frozen intent invalidates the review;
6. wrong confirmation cannot arm LIVE;
7. exact manually typed confirmation can arm LIVE only for unchanged state;
8. launch revalidates all existing PDA-6 safety conditions;
9. right-side **Live deploy** receives/focuses the current run;
10. no automatic retry is introduced;
11. authoritative report/history refresh after completion;
12. Release build/tests pass.

### 5.7.8 Retire normal Dry/Live popup launch paths last

Only after both embedded surfaces pass operator smoke:

- remove the normal `DryRunWindow.Show()` launch path;
- remove the normal `LiveDeploymentReviewWindow.Show()` launch path;
- keep legacy window classes temporarily only if still useful as compatibility wrappers during refactor;
- delete dead window-only presentation code only after repository search proves no remaining normal launch path depends on it.

This order intentionally prevents a UI refactor from weakening or accidentally rewriting accepted PDA-5/PDA-6 execution behavior.

### 5.7.9 Focus rules after embedding

Workspace selection and right-console focus are separate concepts:

- selecting **DRY** shows the Dry Run controls on the left;
- starting a dry execution focuses the right-side **Dry Run** channel;
- selecting **LIVE** shows the Live Review controls on the left;
- starting a live execution focuses the right-side **Live deploy** channel;
- selecting **REPORTS** shows latest/history inspection on the left;
- clicking any Summary / Full log focuses the right-side **Reports** channel;
- ZDeployPet session/probe activity continues to focus/use the right-side **ZDeployPet** channel when appropriate.

No per-run or per-report tabs/windows are introduced.

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

Completed foundation:

1. ✅ latest-run-only Dry Run / Live deploy consoles and embedded hard bound;
2. ✅ second precision in Deployment history timestamps;
3. ✅ permanent **Reports** tab in the right pane;
4. ✅ reusable embedded Summary / Full log viewer state;
5. ✅ Latest report Summary / Full log routes to right-side Reports;
6. ✅ history Summary / Full log routes to right-side Reports;
7. ✅ normal report popup viewers retired;
8. ✅ Copy all + Copy selection reused across right-side channels;
9. ✅ restrained right-tab accents and stronger DRY/LIVE readiness colors;
10. ✅ **ZDeploy Pet** is the single normal deployment-access toggle;
11. ✅ current Release/operator smoke for report viewer, latest-run consoles, pet access toggle and current Home polish passed.

**Next implementation sequence — do in this order:**

12. ⬜ create the left **DRY | LIVE | REPORTS** workspace shell only;
13. ⬜ move Latest deployment report + Deployment history into the left **REPORTS** tab;
14. ⬜ extract the current Dry Run window body/interaction into one reusable embeddable Dry surface;
15. ⬜ route Home **DRY** to that embedded surface and stop normal Dry popup creation;
16. ⬜ run Release/tests + full DRY operator smoke;
17. ⬜ extract the current Live Review window body/interaction into one reusable embeddable Live surface;
18. ⬜ route Home **LIVE** to that embedded surface and stop normal Live popup creation;
19. ⬜ run Release/tests + full LIVE operator smoke;
20. ⬜ retire/delete remaining dead normal Dry/Live window-only launch/presentation paths after dependency search;
21. ⬜ run complete pet-first Home + right-console focus smoke;
22. ⬜ continue remaining PDA-8 release/packaging work only after this UI slice is accepted.

---

# 8. Acceptance gate for the embedded Home workspace

The next slice is accepted only when:

- Home contains one left workspace with exactly **DRY | LIVE | REPORTS** in that order;
- the large Latest report/history content is no longer always visible;
- REPORTS is usable while ZDeploy Pet is LOCKED;
- DRY/LIVE availability still comes from authoritative READY/execution state;
- DRY uses the existing PDA-5 guard/executor logic, not a rewritten duplicate;
- LIVE uses the existing PDA-6 immutable-review/confirmation/executor logic, not a rewritten duplicate;
- normal DRY and LIVE use no popup windows;
- the trusted OpenSSH/passphrase terminal remains separate;
- Dry execution focuses the right-side Dry Run channel;
- Live execution focuses the right-side Live deploy channel;
- Summary/Full log inspection focuses the right-side Reports channel;
- report/history content remains reporter-backed and read-only;
- no per-run/per-report tabs or windows accumulate;
- no security/authorization invariant from PDA-0 through PDA-7 changes;
- Release build/tests pass after the DRY checkpoint, after the LIVE checkpoint and after final cleanup;
- real Millenova smoke proves dry remains structurally dry and live still requires immutable exact confirmation.

---

# 9. Public-release sequencing

This UX work is part of PDA-8 and must happen **before final installer/public-launch acceptance**.

Recommended PDA-8 order from this checkpoint:

1. complete the embedded **DRY | LIVE | REPORTS** Home workspace;
2. finish remaining pet-first functional/UI simplification;
3. clean-clone build/test/publish CI;
4. release secret/artifact audit;
5. deterministic portable package;
6. clean-state portable smoke;
7. installer/upgrade/uninstall lifecycle;
8. packaged icon/shortcut parity;
9. final repository/release-note/public-policy work;
10. final installed + portable security/UX parity smoke.

The goal is not merely to package the current developer-oriented shell. The release should package the simplified, operator-ready ZDeployPet experience.
