# PDA-8 P10 — Final parity / DPI closure plan

**Status:** NEXT visual step after the main-window lifecycle close bug is fixed and operator-smoked.

**Source of truth:** `mocks up/zdeploypet-ui-proposal.html` plus the accepted current WPF screenshots.

P10 is the final visual closure slice. It must not change deployment authority, DRY/LIVE/REPORTS behavior, bounded access/session rules, report routing, or execution safety.

## Ordered implementation

1. **Home context strip wrapping**
   - replace the current single-line truncation-first presentation with a compact wrapping layout;
   - keep the metadata order `Profile • Project • WSL • Runner • Targets • Destinations`;
   - allow the project/runner values to wrap naturally without pushing the Activity pane or workspace out of bounds;
   - remove/avoid the oversized overflow tooltip that currently stretches across the workspace when the truncated strip is hovered;
   - preserve Home read-only behavior.

2. **Home hero/workspace breathing room**
   - slightly increase vertical spacing between the hero title, subtitle, pet card/context strip and the Deployment workspace card;
   - keep the current accepted left/right pane proportion;
   - do not increase the overall dashboard feel or reintroduce oversized branding.

3. **Final typography/padding normalization**
   - strengthen the hero/title hierarchy slightly;
   - keep secondary copy softer and compact;
   - normalize Home card padding and Activity & reports horizontal/vertical spacing against the accepted mockup;
   - preserve the current compact inspector density and terminal behavior.

4. **DPI / resize closure**
   - smoke at 100%, 125% and 150% Windows scaling;
   - verify Home, Profile, DRY, LIVE and REPORTS;
   - verify the left rail, pet card, segmented workspace control, wrapped context strip, embedded workflow scrollbars and Activity pane do not clip;
   - verify narrow/short resize does not reintroduce the outer Deployment workspace containment bug.

5. **Final parity checkpoint**
   - compare the native WPF UI side-by-side with the accepted v2 mockup at a matched aspect ratio;
   - fix only remaining small visual regressions;
   - intentional divergence remains: native Windows title-bar/window controls are retained rather than recreating the HTML mockup title bar.

## Acceptance

PDA-8 may be declared visually finished when:

- Home/Profile navigation and toolbar help remain stable;
- context metadata wraps cleanly without a giant overflow tooltip;
- DRY/LIVE/REPORTS and Activity/report routing remain behaviorally unchanged;
- no white/light native chrome regressions appear;
- 100/125/150% DPI smoke passes;
- final WPF/mockup comparison has no material hierarchy, spacing or clipping mismatch.

## Lifecycle prerequisite now scheduled ahead of P10

The implementation plan already defines the invariant **closing the main UI means OFF and process exit**. The currently observed defect is being fixed before this P10 cosmetic work: closing after the ZDeploy Pet card reaches READY must not leave a hidden `ZDeployPet.exe` process or the single-instance mutex alive, and immediate relaunch must work without manual `Get-Process` / `Stop-Process` recovery.
