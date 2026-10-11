# PDA-7 — Sanitized incident bundle and agent handoff

**Status:** 🟡 ACTIVE — preview/export/agent-handoff smokes passed; final security regression build pending

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

No arbitrary recursive collection is permitted. The Core builder now also rejects undefined/out-of-range `IncidentEvidenceKind` values instead of treating an enum cast as trusted.

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
- common private-key paths under `~/.ssh`, `/home/<user>/.ssh`, and `/root/.ssh`;
- common Windows private-key paths under `C:\Users\<user>\.ssh\...` or `%USERPROFILE%\.ssh\...`.

Redaction is deterministic and happens before preview/export. Evidence records retain whether redaction and truncation occurred so the UI can tell the operator exactly what was altered.

## Operator preview and local export

`Incident preview…` is available from the main discovery surface independently of deployment-access ON/OFF state. It requires an already trusted latest PDA-4 report and opens a modeless preview window.

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
- builds a deterministic no-authority coding-agent handoff manifest;
- shows the exact sanitized content that can be copied or exported;
- labels every included item with evidence kind plus `REDACTION APPLIED: YES/NO` and `TRUNCATED: YES/NO`;
- shows an explicit `OMITTED BY OPERATOR` section for unchecked evidence;
- shows reporter id/outcome/release/mode/target context;
- fails closed if any selected trusted artifact cannot be safely read;
- enables **Copy all** and **Export sanitized bundle…** only after a successful sanitized build;
- invalidates Copy/Export if the evidence selection changes until the operator explicitly rebuilds the preview.

The exported `.txt` file is the exact preview text encoded as UTF-8 without a BOM. Export is atomic through a same-directory temporary file followed by replace/move; failed writes clean up the temporary file best-effort.

## Agent-handoff manifest

The deterministic manifest contains only bounded non-secret context:

- repository label only, never a repository path;
- profile display name only;
- deployment id;
- reporter outcome;
- release / mode / target;
- included and omitted evidence labels.

It explicitly states:

- `Authority: NONE`;
- `Credentials included: NO`;
- `SSH-agent/session capability included: NO`;
- `Remote-write/deployment authorization included: NO`.

Agent rules require treating the bundle as read-only diagnostic evidence and any returned patch as untrusted until human diff review and tests. Deployment-script changes require fresh script approval/fingerprint validation, and every deployment still requires a fresh ZDeployPet authorization flow.

Manifest scalar fields and evidence labels are themselves validated through the sanitizer and fail closed on secret-like/path-like unsafe material or control characters.

## Safe export destination policy

The Core export policy requires:

- a fully qualified local Windows path;
- `.txt` extension;
- no UNC/network path;
- no alternate-data-stream path syntax;
- a destination outside the active Windows project repository.

The UI uses an explicit operator `SaveFileDialog`; no automatic/background export exists. The deterministic suggested filename is `zdeploypet-incident-<deployment-id>.txt`. The destination directory must already exist. The exported content starts with an explicit diagnostic-only/no-deployment-authority notice.

## Fail-closed rules

The Core builder rejects:

- null/empty evidence sets;
- more than the maximum number of items;
- unsupported/out-of-range evidence kinds;
- missing labels;
- labels with control characters or excessive length;
- null content;
- raw item content beyond the maximum input bound;
- bundles that exceed the total sanitized-content bound.

The collector rejects an invalid canonical report root, any report whose declared artifacts escape that root, and any artifact whose resolved canonical path escapes the root. No collector API accepts a directory, glob, shell command, raw environment block, private key, SSH agent runtime, or deployment-session capability.

The export policy rejects relative destinations, network/UNC destinations, non-`.txt` extensions, alternate-data-stream syntax and destinations inside the active project repository.

## Current implementation slice

Implemented on `main`:

- `IncidentEvidenceKind` explicit evidence allowlist plus undefined-kind rejection;
- `IncidentEvidenceInput` and `SanitizedIncidentEvidence` contracts;
- `IncidentBundleLimits` hard bounds;
- deterministic `IncidentEvidenceSanitizer` with Unix + Windows private-key-path redaction;
- `IncidentBundleBuildResult` carrying errors, sanitized evidence and total size;
- redaction/truncation metadata per item;
- Core tests covering private-key blocks, secret assignments, bearer tokens, SSH-agent sockets, Unix/Windows private-key paths, unsupported evidence kinds, normal output preservation, item count limits, label controls, raw input limits and bounded truncation;
- App-layer `IncidentEvidenceCollector` with explicit selection flags only;
- PDA-4 canonical-root revalidation for JSON/Summary/Full-log sources;
- artifact reads delegated to the existing realpath/type/size-bounded WSL report reader;
- bounded head/head+tail excerpts before Core sanitization;
- read-only collection from Dry run / Live deploy / ZDeployPet console channels;
- preview WPF surface with explicit selection checkboxes;
- exact included-content preview with redaction/truncation indicators and explicit omission list;
- preview entry point remains read-only and does not require deployment access to be ON;
- `Copy all` copies only a successfully built sanitized bundle;
- Core `IncidentBundleExportPolicy` and deterministic filename generation;
- guarded explicit local `.txt` export outside the active project repository;
- selection-change invalidation before copy/export;
- export policy regression tests for relative path, local path, UNC/network, project path, extension and alternate-data-stream cases;
- deterministic coding-agent handoff manifest with secret-like metadata rejection and explicit no-authority rules.

Operator evidence completed:

- published-development preview smoke against real Millenova deployment `20261011-051521_v4.4.25_live_vps` succeeded;
- all six evidence selections were requested; four evidence items were available/included for that session;
- reporter context was PASS / release `4.4.25` / live / vps;
- initial preview reported 46,373 sanitized characters, zero Core redactions and zero Core truncations;
- `Copy all` presence was verified in the published UI;
- real local `.txt` export succeeded to an operator-selected Downloads path;
- published agent-manifest smoke succeeded and visibly showed `Authority: NONE`, no credentials, no SSH-agent/session capability and no remote-write/deployment authorization.

Remaining before acceptance:

- run the final Release regression/build after the last unsupported-kind/Windows-path/export-negative hardening commits;
- perform one final authority-content review against the real exported bundle after that green build;
- update the master implementation plan and mark PDA-7 accepted only after those checks pass.

## Implementation sequence

1. ✅ Core evidence kinds, bounds and deterministic redaction;
2. ✅ Core regression tests for secret/bounds behavior;
3. ✅ narrow collector for trusted PDA-4 artifacts + the three existing console channels with canonical path containment;
4. ✅ operator preview showing included evidence, redactions, truncation and omissions before export;
5. ✅ deterministic local bundle format and safe destination policy;
6. ✅ agent-handoff manifest containing repository/profile/report context without secrets or deployment capability;
7. 🟡 remaining negative/security hardening implemented — final Release run pending;
8. ✅ published-development preview + real local file export + manifest UI smokes passed;
9. 🟡 final no-deployment-authority verification pending after green Release regression.

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
