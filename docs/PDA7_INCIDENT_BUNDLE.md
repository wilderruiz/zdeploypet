# PDA-7 — Sanitized incident bundle and agent handoff

**Status:** 🟡 ACTIVE — Core evidence/redaction contract implemented; collection/preview/export UI not yet implemented

PDA-7 turns selected ZDeployPet evidence into a bounded, operator-reviewed debugging handoff without transferring deployment authority. The bundle is diagnostic evidence only: it must never contain credentials, private-key material, an SSH-agent capability, arbitrary environment dumps, or an executable deployment authorization.

## Authority boundary

PDA-7 does not change deployment authority. PDA-2 still owns bounded SSH access, PDA-5/PDA-6 own execution authorization, and PDA-4 reporter evidence remains authoritative for deployment outcome.

An incident bundle may describe what happened. It may not make a recipient capable of deploying.

## Allowed evidence classes

The Core contract defines explicit evidence kinds only:

- ZDeployPet application activity;
- Dry run console output;
- Live deploy console output;
- deployment summary;
- deployment full-log excerpt;
- deployment JSON excerpt/metadata;
- bounded environment/build/runtime summary prepared by ZDeployPet.

The first Core slice accepts evidence as already-selected in-memory text. It intentionally performs **no filesystem traversal and no arbitrary path reads**. Path collection and containment belong to a later collector layer and must be explicit/allowlisted.

## Hard bounds

Current Core limits:

- maximum 20 evidence items per bundle;
- maximum label length 120 characters;
- maximum raw input per item 512 KiB characters;
- maximum sanitized preview/export content per item 256 KiB characters;
- maximum total sanitized content 2 MiB characters.

Oversized raw input fails closed rather than silently reading or exporting an unbounded source. Sanitized item content may be truncated only at the explicit bounded preview/export limit and carries a visible truncation marker.

## Deterministic redaction

The Core sanitizer currently redacts:

- PEM/OpenSSH/RSA/EC/DSA private-key blocks;
- bearer authorization values;
- common password/passphrase/token/secret/API-key/access-key assignments;
- `SSH_AUTH_SOCK` values;
- common Unix SSH-agent socket paths;
- common private-key paths under `~/.ssh`, `/home/<user>/.ssh`, and `/root/.ssh`.

Redaction is deterministic and happens before preview/export. Evidence records retain whether redaction and truncation occurred so the future UI can tell the operator exactly what was altered.

## Fail-closed rules

The Core builder rejects:

- null/empty evidence sets;
- more than the maximum number of items;
- missing labels;
- labels with control characters or excessive length;
- null content;
- raw item content beyond the maximum input bound;
- bundles that exceed the total sanitized-content bound.

No bundle builder API accepts a filesystem path, directory, glob, shell command, environment block, private key, SSH agent runtime, or deployment-session object.

## Current implementation slice

Implemented on `main`:

- `IncidentEvidenceKind` explicit evidence allowlist;
- `IncidentEvidenceInput` and `SanitizedIncidentEvidence` contracts;
- `IncidentBundleLimits` hard bounds;
- deterministic `IncidentEvidenceSanitizer`;
- `IncidentBundleBuildResult` carrying errors, sanitized evidence and total size;
- redaction/truncation metadata per item;
- Core tests covering private-key blocks, secret assignments, bearer tokens, SSH-agent sockets, private-key paths, normal output preservation, item count limits, label controls, raw input limits and bounded truncation.

Not implemented yet:

- evidence collector from PDA-4 report/artifact surfaces and the three console channels;
- canonical path containment for any file-backed evidence collection;
- incident-bundle preview UI;
- local export format/location;
- handoff manifest for coding agents;
- explicit omission list in the preview;
- real sanitized bundle smoke.

## Implementation sequence

1. ✅ Core evidence kinds, bounds and deterministic redaction;
2. ✅ Core regression tests for first-slice secret/bounds behavior;
3. ⬜ add a narrow collector that can consume only selected existing ZDeployPet evidence surfaces, with canonical path containment for file-backed report artifacts;
4. ⬜ build operator preview showing included evidence, redactions, truncation and omissions before export;
5. ⬜ define a deterministic local bundle format and safe destination policy;
6. ⬜ add an agent-handoff manifest containing repository/profile/report context without secrets or deployment capability;
7. ⬜ add negative tests for path escape/symlink/untrusted source/unsupported evidence and bundle-export destination rules;
8. ⬜ published-development smoke with real report + console evidence;
9. ⬜ verify exported bundle cannot be used as deployment authority and contains no secrets/private infrastructure capability.

## Acceptance gate

PDA-7 is accepted only when:

- evidence sources are explicit and allowlisted;
- no arbitrary recursive collection or raw environment dump exists;
- file-backed evidence is canonically contained within approved roots;
- credentials, private-key material/paths, tokens and SSH-agent sockets are denied/redacted deterministically;
- bundle/item sizes are hard bounded;
- the operator previews exactly what will be exported and what was redacted/truncated/omitted;
- export requires explicit operator action;
- bundle contents grant no deployment authority and include no live session capability;
- agent handoff clearly treats returned changes as untrusted until human review/tests/fresh authorization;
- automated negative tests pass;
- one real sanitized bundle smoke passes with report + console evidence.
