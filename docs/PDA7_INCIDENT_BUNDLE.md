# PDA-7 — Sanitized incident bundle and agent handoff

**Status:** 🟡 ACTIVE — Core redaction + bounded collector + preview-only UI implemented; export/handoff still pending

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

No arbitrary recursive collection is permitted.

## Narrow evidence collector

The collector consumes only already trusted ZDeployPet surfaces:

- one already parsed/trusted `DeploymentReport` from PDA-4;
- that report's declared JSON, Summary and Full log paths only;
- the canonical report root already resolved by the PDA-4 reader;
- a bounded snapshot of the existing in-memory Console / Activity stream.

The collector does **not** accept an arbitrary artifact path from UI callers. Before every file-backed read it revalidates that all report-declared artifacts remain inside the canonical report root. The existing `WslDeploymentReportReader` then resolves the actual artifact path with `realpath`, requires a regular file, enforces the reader byte limit and rejects canonical escapes/symlink escapes.

Console evidence is reconstructed only from the three existing channels:

- Dry run lifecycle + raw `deploy_millenova.sh` lines;
- Live deploy lifecycle + raw `deploy_millenova.live.sh` lines;
- ZDeployPet application/session/report/probe activity excluding the two deployment channels.

Raw script rows remain natural shell text; app lifecycle rows retain timestamp / level / category formatting. Collection is read-only and carries no deployment-session object, SSH-agent runtime or execution authority.

## Hard bounds

Current Core limits:

- maximum 20 evidence items per bundle;
- maximum label length 120 characters;
- maximum raw input per item 512 KiB characters;
- maximum sanitized preview/export content per item 256 KiB characters;
- maximum total sanitized content 2 MiB characters.

The collector also bounds text before it enters Core. Summary/JSON evidence keeps the beginning of oversized content; full logs and console streams keep bounded head + tail context with an explicit omission marker. The WSL reader remains bounded independently at its report/summary/full-log byte ceilings.

Oversized raw input presented directly to the Core builder fails closed. Sanitized item content may be truncated only at the explicit bounded preview/export limit and carries a visible truncation marker.

## Deterministic redaction

The Core sanitizer currently redacts:

- PEM/OpenSSH/RSA/EC/DSA private-key blocks;
- bearer authorization values;
- common password/passphrase/token/secret/API-key/access-key assignments;
- `SSH_AUTH_SOCK` values;
- common Unix SSH-agent socket paths;
- common private-key paths under `~/.ssh`, `/home/<user>/.ssh`, and `/root/.ssh`.

Redaction is deterministic and happens before preview/export. Evidence records retain whether redaction and truncation occurred so the UI can tell the operator exactly what was altered.

## Preview-only operator UI

`Incident preview…` is now available from the main discovery surface independently of deployment-access ON/OFF state. It requires an already trusted latest PDA-4 report and opens a modeless preview window.

The operator can explicitly select:

- deployment JSON;
- deployment summary;
- deployment full-log excerpt;
- Dry run console;
- Live deploy console;
- ZDeployPet console.

The preview surface then:

- collects only the selected trusted sources;
- runs the Core sanitizer and bundle bounds;
- shows the exact sanitized content that would later be exported;
- labels every included item with evidence kind plus `REDACTION APPLIED: YES/NO` and `TRUNCATED: YES/NO`;
- shows an explicit `OMITTED BY OPERATOR` section for unchecked evidence;
- shows reporter id/outcome/release/mode/target context;
- fails closed if any selected trusted artifact cannot be safely read;
- has **no export action** in this slice.

## Fail-closed rules

The Core builder rejects:

- null/empty evidence sets;
- more than the maximum number of items;
- missing labels;
- labels with control characters or excessive length;
- null content;
- raw item content beyond the maximum input bound;
- bundles that exceed the total sanitized-content bound.

The collector rejects an invalid canonical report root, any report whose declared artifacts escape that root, and any artifact whose resolved canonical path escapes the root. No collector API accepts a directory, glob, shell command, raw environment block, private key, SSH agent runtime, or deployment-session capability.

## Current implementation slice

Implemented on `main`:

- `IncidentEvidenceKind` explicit evidence allowlist;
- `IncidentEvidenceInput` and `SanitizedIncidentEvidence` contracts;
- `IncidentBundleLimits` hard bounds;
- deterministic `IncidentEvidenceSanitizer`;
- `IncidentBundleBuildResult` carrying errors, sanitized evidence and total size;
- redaction/truncation metadata per item;
- Core tests covering private-key blocks, secret assignments, bearer tokens, SSH-agent sockets, private-key paths, normal output preservation, item count limits, label controls, raw input limits and bounded truncation;
- App-layer `IncidentEvidenceCollector` with explicit selection flags only;
- PDA-4 canonical-root revalidation for JSON/Summary/Full-log sources;
- artifact reads delegated to the existing realpath/type/size-bounded WSL report reader;
- bounded head/head+tail excerpts before Core sanitization;
- read-only collection from Dry run / Live deploy / ZDeployPet console channels;
- preview-only WPF surface with explicit selection checkboxes;
- exact included-content preview with redaction/truncation indicators and explicit omission list;
- preview entry point remains read-only and does not require deployment access to be ON.

Not implemented yet:

- local export format/location;
- handoff manifest for coding agents;
- selection of a historical report row from a future preview UI (collector already accepts any trusted `DeploymentReport` supplied by the report surface);
- export-destination/path negative tests;
- real sanitized bundle preview/export smoke.

## Implementation sequence

1. ✅ Core evidence kinds, bounds and deterministic redaction;
2. ✅ Core regression tests for first-slice secret/bounds behavior;
3. ✅ narrow collector for trusted PDA-4 artifacts + the three existing console channels with canonical path containment;
4. ✅ operator preview showing included evidence, redactions, truncation and omissions before export;
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
