# ZDeployPet hard bugs

This folder records the difficult engineering failures encountered while turning ZDeployPet from a shell-script companion into a stable Windows/WPF deployment front end.

These are not generic troubleshooting notes. Each document captures the original symptom, the engineering path that led to the real cause, the fix pattern that survived smoke testing, and the verification commands used on the real application.

## Why this exists

Several bugs were deceptively small on screen but involved WPF ownership, event timing, native Windows chrome, VisualTree/LogicalTree behavior, popup HWND behavior, partial-class namespace collisions, resource loading, or hidden coupling between legacy UI and deployment state. A green build was sometimes not enough: some failures compiled perfectly and only appeared when the UI was constructed at runtime.

The goal of this folder is to prevent those lessons from being lost and to give future coding agents a concrete history of what not to repeat.

## Bug dossiers

1. `01_PATH_AMBIGUITY_SYSTEM_IO_VS_WPF_SHAPES.md` — `Path` collisions between `System.IO.Path` and `System.Windows.Shapes.Path`.
2. `02_WPF_UIELEMENT_PARENT_REPARENTING.md` — re-parenting existing WPF controls without relying on `UIElement.Parent`.
3. `03_GIT_SAFETY_POPUP_CLICK_COLLAPSE_AND_HANG.md` — popup that only stayed open while the mouse button was held, then later hung the app.
4. `04_DISABLED_MENU_ITEM_GRAY_SQUARE_ARTIFACT.md` — the strange gray square beside a disabled Git-safety menu item.
5. `05_UI_POLISH_RAN_BEFORE_CONTROLS_EXISTED.md` — a build that succeeded but visually changed nothing because runtime polish ran too early.
6. `06_ISVISIBLECHANGED_EVENT_HANDLER_TYPE_MISMATCH.md` — `RoutedEventHandler` vs `DependencyPropertyChangedEventHandler`.
7. `07_TEXT_ELEMENT_NAMESPACE_BUILD_FAILURE.md` — WPF `TextElement` compile failure caused by missing namespace context.
8. `08_BOUNDED_SSH_TERMINAL_POPUP_TO_SHARED_ACTIVITY.md` — removing an unnecessary terminal/popup while preserving the OpenSSH passphrase boundary.
9. `09_CONSOLE_UNBOUNDED_GROWTH_AND_SINGLE_RUN_BUFFER.md` — keeping Dry/Live consoles bounded to one run to prevent UI slowdown/hanging.
10. `10_REPORT_POPUPS_TO_SHARED_REPORTS_TAB.md` — replacing report windows with one reusable Reports surface.
11. `11_INCIDENT_PREVIEW_LEGACY_LAUNCHER_SURVIVED_REFACTOR.md` — why an old footer launcher remained visible after moving the feature to Help.
12. `12_EMBEDDED_DRY_LIVE_WORKSPACE_WITHOUT_DUPLICATING_AUTHORITY.md` — embedding PDA-5/PDA-6 workflows while preserving one safety/authority implementation.
13. `13_THEME_PACK_URI_AND_WPF_RESOURCE_LOADING.md` — native WPF theme loading and the assembly-name pack URI trap.
14. `14_NATIVE_TITLE_BAR_VS_WPF_SHELL_CHROME.md` — separating Windows caption styling from WPF menu/shell styling.
15. `15_BUILD_AND_PUBLISH_VERIFICATION_LOOP.md` — the release loop used to catch compile, publish, stale-binary, and runtime regressions.
16. `16_INCIDENT_EXPORT_MISSING_SYSTEM_IO_SYMBOLS.md` — Incident Bundle export build failure from unresolved `Path`, `Directory`, and `File`.
17. `17_REFRESH_GLYPH_AND_ACTIVE_NAVIGATION_STATE.md` — refresh-glyph conversion plus Home/Profile active-state correctness.
18. `18_REMOVING_LEGACY_ON_OFF_BROKE_DRY_LIVE_VISIBILITY.md` — hidden coupling between the removed ON/OFF UI and DRY/LIVE availability.
19. `19_DUPLICATED_GIT_SAFETY_HELP_AND_TOOLTIP_MERGE.md` — consolidating competing Git-safety help surfaces into one authoritative tooltip.
20. `20_RUNTIME_COSMETIC_PATCHES_VS_CENTRAL_WPF_THEME.md` — why repeated code-behind visual patches were replaced by a staged resource/style architecture.

## Standard verification loop

From the repository root:

```powershell
cd C:\Dev\ZDeployPet
dotnet test ZDeployPet.sln -c Release
powershell -ExecutionPolicy Bypass -File scripts\publish-local.ps1
```

The publish script produces the development executable at:

```text
C:\Dev\ZDeployPet_Releases\current\ZDeployPet.exe
```

A bug is not considered resolved merely because the compiler is green. WPF changes must also be smoke-tested in the published executable, because many ownership, popup, lifecycle, template, resource, and VisualTree problems only appear after controls are created and displayed.

## Recommended use by future agents

Before changing one of these areas, read the matching dossier first. In particular:

- popup/menu changes → read 03 and 04;
- moving controls between surfaces → read 02 and 12;
- runtime styling → read 05, 17, and 20;
- shell/theme work → read 13 and 14;
- deployment-access UI changes → read 08 and 18;
- reports/console changes → read 09 and 10.

The documents intentionally include commands and code patterns because the failures were not purely visual; several were architecture or lifecycle problems disguised as cosmetic bugs.
