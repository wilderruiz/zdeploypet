# PDA-4 — Structured deployment-report monitor

**Status:** 🟡 ACTIVE

PDA-4 adds a read-only monitor for project-owned deployment reporter output. The reporter JSON is authoritative deployment truth; ZDeployPet must never override it with process exit status or UI assumptions.

## Contract

For the Millenova adapter, schema v1 contains the stable top-level fields:

- `schema_version`
- `deployment_id`
- `release`
- `previous_release`
- `mode`
- `target`
- `result`
- `exit_status`
- `started_at`
- `finished_at`
- `duration_seconds`
- `checks`
- `tests`
- `hostinger`
- `vps`
- `failure`
- `artifacts`

The generic ZDeployPet core consumes the stable run/result/artifact envelope and preserves unknown project-specific fields for forward compatibility. Project-specific detail remains reporter-owned.

## Security / integrity rules

- report monitoring is strictly read-only;
- the configured report root is the only trust boundary for report/artifact paths;
- every report/artifact path is canonicalized before use;
- paths escaping the configured report root fail closed;
- malformed JSON, missing required identity/result fields, unsupported schema versions and partially-written JSON fail closed;
- `latest.json` may be consumed only after a complete parse and validation pass;
- reporter `result` is authoritative; process exit status never upgrades a failed/cancelled report to success;
- no report path, content or artifact may grant deployment authority;
- the WSL reader resolves the configured root and report/artifact paths with `realpath`, rejects symlink escapes, accepts only regular files, and enforces bounded payload sizes before reading;
- the shell report surfaces validate all declared JSON/summary/full-log artifact paths against the canonical report root before rendering the report as trusted;
- historical discovery may enumerate only bounded report files below the configured canonical report root and must never execute, source or interpret shell content from the report tree;
- `latest.json` is a convenience pointer/current snapshot and must not appear as a duplicate historical deployment row when the same `deployment_id` is already represented by its dated report JSON;
- summary/full-log text is decoded as UTF-8 so reporter labels such as `STUDIO · TRACK DNA` render without mojibake.

## Latest report surface

Keep the existing **Latest deployment report** card as the fast current-state surface. It shows:

- authoritative PASS / FAILED / CANCELLED result;
- release and previous release;
- mode and target;
- start/finish timestamps and duration;
- deployment id;
- read-only **View summary** and **View full log** actions after artifact-path validation.

The latest card remains independent from deployment-access state. A LOCKED ZDeployPet session may still read deployment reports because monitoring is read-only.

## Deployment history table

Add a **Deployment history** table directly below the latest-report card rather than a single historical dropdown. The table is the primary historical browsing surface because it allows runs to be scanned and compared without repeatedly opening a selector.

Initial row model, newest first:

`Started | Release | Mode | Target | Result | Duration | Summary | Full log`

Requirements:

- default to the newest 30 trusted report JSON files; do not recursively load an unbounded history into the UI;
- derive each row from the same schema-v1 parser used by the latest card, not from filenames alone;
- enumerate dated report JSON files below the canonical report root and ignore `latest.json` as a duplicate history source;
- deduplicate by `deployment_id` if the same authoritative report is encountered through more than one safe path;
- preserve reporter result as the row result; do not infer success from exit code, filename, colour, or current UI state;
- keep PASS / FAILED / CANCELLED visually distinct while still exposing the literal reporter result text to accessibility tooling;
- malformed, unsupported, partially written, escaped or otherwise untrusted report files must not become normal history rows; surface a bounded warning/count instead of crashing the table;
- **Summary** and **Full log** actions on each row use the report's validated artifact paths and the same bounded read-only viewer as the latest card;
- selecting a row may later open a richer report-details surface, but row selection itself grants no deployment authority and performs no write;
- keep table refresh explicit initially; bounded polling/watch behavior can be layered later without deployment-side writes;
- future filters may include Result and Target, but they are not required for the first history-table acceptance slice.

## Current implementation slice

Implemented on `main`, awaiting published-development smoke:

- WSL history discovery scans dated `*.json` report files below the canonical report root and excludes root `latest.json`;
- candidate enumeration is capped at 90 paths before report parsing, while the UI renders at most the newest 30 trusted deployment ids;
- every historical row is parsed through the same schema-v1 Core parser used by the latest card;
- artifact paths are revalidated against the canonical report root before a row is trusted;
- duplicate deployment ids are suppressed;
- malformed/untrusted candidates are skipped and counted in the history status line;
- the history table shows Started, Release, Mode, Target, Result and Duration plus row-specific Summary / Full log actions;
- row actions use the selected report's own artifact paths and the existing bounded read-only artifact viewer;
- WSL stdout/stderr decoding is explicitly UTF-8 to correct middle-dot reporter labels.

## Initial implementation sequence

1. ✅ add immutable Core report contracts and a schema-v1 parser/validator;
2. ✅ add Core path-boundary validation for report/artifact paths;
3. ✅ add automated tests for valid pass/fail/cancelled reports, malformed/partial JSON, unsupported schema and path escapes;
4. ✅ add a narrow WSL read-only report-root reader for `latest.json` with canonical root/file containment and bounded payload size;
5. ✅ add a shell **Latest deployment report** card showing deployment id, release, mode, target, timestamps, duration and authoritative PASS/FAILED/CANCELLED result;
6. ✅ add read-only **View summary** / **View full log** actions for validated bounded artifacts;
7. 🟡 explicit UTF-8 artifact decoding implemented; published-development smoke for `·` labels pending;
8. 🟡 bounded WSL history enumerator implemented; published-development smoke pending;
9. 🟡 **Deployment history** table implemented with newest 30 trusted runs and per-row Summary / Full log actions; published-development smoke pending;
10. add automated tests for history enumeration bounds, deduplication, ordering, path escape and partial JSON;
11. 🟡 manual Refresh refreshes both latest and history; bounded watch/poll behavior remains later work without deployment-side writes;
12. ✅ smoke the latest-report card and summary/full-log viewers against real Millenova reporter output; historical-table smoke remains pending.

## Acceptance gate

PDA-4 is accepted when:

- valid real `latest.json` renders correctly;
- real historical Millenova runs render newest-first in the bounded Deployment history table;
- the table shows Started, Release, Mode, Target, Result and Duration for each trusted run;
- historical Summary / Full log actions open the artifact belonging to the selected deployment, not the current latest deployment;
- `latest.json` does not create a duplicate historical row;
- malformed/partial latest or historical reports fail closed without crashing the shell;
- report/artifact path escapes are rejected;
- PASS/FAILED/CANCELLED remain reporter-authoritative regardless of process/UI state;
- validated summary/full-log artifacts stay bounded under the configured report root;
- UTF-8 reporter text, including middle-dot labels, renders correctly;
- monitoring performs no deployment-side write;
- automated parser/path-boundary/history-enumeration tests pass;
- published-development smoke passes against the Millenova report root for both latest and historical reports.
