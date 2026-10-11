# Hard UI bug: Git-safety help existed in multiple competing tooltips

## Initial symptom

The Profile toolbar ended up with separate `i` help surfaces around Git safety:

- one near the Git-safety icon;
- another associated with the old Git-safety popup/window content;
- overlapping explanations that did not fully agree.

The user expectation was simpler: one Git-safety icon, one adjacent `i`, and one merged explanation.

## Why duplicated help is a real engineering problem

Duplicated help text is not only cosmetic. It creates documentation drift inside the executable:

```text
Tooltip A says scan is read-only.
Tooltip B explains .gitignore mutation.
Popup text explains key location.
```

Future safety changes can update one text block and leave another stale.

For security-adjacent features such as private-key scanning, stale help is undesirable.

## Final information architecture

The top-level `i` beside the Git-safety tool owns the explanation for the entire feature:

```text
Purpose
When to use this
What it does
Example
Safety boundaries
```

The action dropdown contains commands only:

```text
Scan repository
Add/update .gitignore safety block
```

No second explanatory panel is necessary.

## Reusable help model

ZDeployPet introduced a structured help specification pattern:

```csharp
new HelpTipSpec(
    "Check repository Git safety.",
    "Scans the active project for private-key material that could accidentally enter Git...",
    WhenToUse: "Use after changing deployment credentials or repository files...",
    WhatItDoes: "The scan is read-only unless you explicitly choose the managed .gitignore action...",
    Safety: "It never makes an already tracked secret safe automatically and does not delete repository files.")
```

Then one factory renders it consistently:

```csharp
Button gitInfo = HelpTipFactory.Create(spec);
```

## Content that must remain explicit

Git safety must explain:

- deployment private keys belong in WSL `~/.ssh`, never inside the repository;
- public `.pub` keys are not automatically treated as secrets;
- the scanner checks tracked and untracked/unignored material;
- scan is read-only;
- managed `.gitignore` update only modifies the clearly marked ZDeployPet block;
- `.gitignore` cannot make an already tracked secret safe;
- the scanner does not delete repository files.

## Verification

1. Profile → hover/click the single Git-safety `i`;
2. confirm the complete merged explanation is visible;
3. open Git-safety dropdown;
4. confirm dropdown contains actions, not a second competing explanation;
5. verify disabled action remains visibly disabled without artifact;
6. verify tooltip remains readable at normal Windows scaling.

## Prevention rule

Every feature should have one authoritative user-facing explanation surface. Reuse structured help specs instead of duplicating prose across popup windows, toolbar hints, and menus.
