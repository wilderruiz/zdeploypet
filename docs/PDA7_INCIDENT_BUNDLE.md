# PDA-7 — Sanitized incident bundle and agent handoff

**Status:** ✅ ACCEPTED 2026-10-11

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

No arbitrary recursive collection is permitted. The Core builder rejects undefined/out-of-range `IncidentEvidenceKind` values instead of treating an enum cast as trusted.

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

The Core sanitizer redacts:

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

The operator can explicitly select deployment JSON, deployment summary, deployment full-log excerpt, Dry run console, Live deploy console and ZDeployPet console.

The preview surface:

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

The deterministic manifest contains only bounded non-secret context: repository label, profile display name, deployment id, reporter outcome, release/mode/target, and included/omitted evidence labels.

It explicitly states:

- `Authority: NONE`;
- `Credentials included: NO`;
- `SSH-agent/session capability included: NO`;
- `Remote-write/deployment authorization included: NO`.

Agent rules require treating the bundle as read-only diagnostic evidence and any returned patch as untrusted until human diff review and tests. Deployment-script changes require fresh script approval/fingerprint validation, and every deployment still requires a fresh ZDeployPet authorization flow.

Manifest scalar fields and evidence labels are themselves validated through the sanitizer and fail closed on secret-like/path-like unsafe material or control characters.

## Safe export destination policy

The Core export policy requires a fully qualified local Windows path, `.txt` extension, no UNC/network path, no alternate-data-stream syntax, and a destination outside the active Windows project repository.

The UI uses an explicit operator `SaveFileDialog`; no automatic/background export exists. The deterministic suggested filename is `zdeploypet-incident-<deployment-id>.txt`. The destination directory must already exist. The exported content starts with an explicit diagnostic-only/no-deployment-authority notice.

## Fail-closed rules

The Core builder rejects null/empty evidence sets, too many items, unsupported/out-of-range evidence kinds, missing/unsafe labels, null content, oversized raw items, and over-limit bundles.

The collector rejects an invalid canonical report root, any report whose declared artifacts escape that root, and any artifact whose resolved canonical path escapes the root. No collector API accepts a directory, glob, shell command, raw environment block, private key, SSH-agent runtime or deployment-session capability.

The export policy rejects relative destinations, network/UNC destinations, non-`.txt` extensions, alternate-data-stream syntax and destinations inside the active project repository.

## Acceptance evidence

Accepted implementation on `main` includes the explicit evidence allowlist, bounds, deterministic sanitizer, Unix/Windows private-key-path redaction, undefined-kind rejection, canonical-root collector, bounded console/report collection, exact preview, `Copy all`, guarded local export, deterministic coding-agent manifest and export/security regression coverage.

Operator evidence:

- published-development preview smoke against real Millenova deployment `20261011-051521_v4.4.25_live_vps` succeeded;
- all six evidence selections were requested; four evidence items were available/included for that session;
- reporter context was PASS / release `4.4.25` / live / vps;
- initial preview reported 46,373 sanitized characters, zero Core redactions and zero Core truncations;
- `Copy all` was verified in the published UI;
- real local `.txt` export succeeded to an operator-selected Downloads path;
- published agent-manifest smoke visibly showed `Authority: NONE`, no credentials, no SSH-agent/session capability and no remote-write/deployment authorization;
- final post-hardening Release build/publish passed with **0 warnings / 0 errors**;
- final published executable SHA-256: `5B989530A81DEB80FCFD378E4B80E2DB4532D570AEB1029F8D95F06A9ED01C83`.

## Final authority review

The accepted bundle contains diagnostic text only. It carries no private key bytes, credential values, SSH-agent socket/runtime, deployment session, confirmation phrase, remote-write primitive or execution token. The manifest explicitly declares no authority, while PDA-2/PDA-5/PDA-6 continue to require their own live runtime/fingerprint/session/human-confirmation gates. Therefore possession of an exported PDA-7 bundle cannot satisfy or bypass deployment authorization.

## Implementation sequence

1. ✅ Core evidence kinds, bounds and deterministic redaction;
2. ✅ Core regression tests for secret/bounds behavior;
3. ✅ narrow collector for trusted PDA-4 artifacts + the three existing console channels with canonical path containment;
4. ✅ operator preview showing included evidence, redactions, truncation and omissions before export;
5. ✅ deterministic local bundle format and safe destination policy;
6. ✅ agent-handoff manifest containing repository/profile/report context without secrets or deployment capability;
7. ✅ negative/security hardening for unsupported evidence, Windows/Unix key paths and export rules;
8. ✅ published-development preview + real local file export + manifest UI smokes;
9. ✅ final no-deployment-authority review and final green Release regression/publish.

## Acceptance gate

✅ **PASSED 2026-10-11.** Evidence sources are explicit/allowlisted and bounded; file-backed evidence is canonically contained; secrets/private-key material/paths/tokens/agent sockets are denied or deterministically redacted; operator preview matches export; export is explicit; manifest grants no deployment authority; returned patches remain untrusted; automated negative tests/build are green; and real sanitized reporter/console preview/export smokes passed.
