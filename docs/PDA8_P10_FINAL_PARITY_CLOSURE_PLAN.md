# PDA-8 P10 — Final parity / DPI closure plan

**Status:** IN PROGRESS — lifecycle prerequisite smoke passed; P10.1 implemented and awaiting operator smoke.

**Source of truth:** `mocks up/zdeploypet-ui-proposal.html` plus the accepted current WPF screenshots.

P10 is the final visual closure slice. It must not change deployment authority, DRY/LIVE/REPORTS behavior, bounded access/session rules, report routing, or execution safety.

## Lifecycle prerequisite — COMPLETE

The critical lifecycle defect was fixed before cosmetic P10 work.

Acceptance confirmed by operator smoke:

- launch while LOCKED, close with `X`, process exits;
- turn ZDeploy Pet ON/READY, close with `X`, the UI closes and the process terminates;
- immediate relaunch works without manual `Get-Process` / `Stop-Process` recovery;
- main-window close remains authoritative even if bounded WSL/OpenSSH cleanup stalls, because cleanup is hard-bounded and cannot strand the single-instance mutex indefinitely.

This satisfies the planned invariant: **closed UI means OFF/process exit; minimized UI may remain alive.**

## Ordered implementation

1. **Home context strip wrapping — IMPLEMENTED, SMOKE NEXT**
   - replaced the previous single-line `NoWrap + CharacterEllipsis` presentation with compact wrapping;
   - metadata order remains `Profile • Project • WSL • Runner • Targets • Destinations`;
   - project/runner values remain inside the Home column rather than projecting across Activity;
   - removed the oversized full-value hover tooltip that previously stretched across the workspace;
   - constrained the strip to a compact two-line visual height;
   - Home remains read-only.

2. **Home hero/workspace breathing room — NEXT AFTER P10.1 SMOKE**
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
