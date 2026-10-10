# PDA-4 — Structured deployment-report monitor

**Status:** ✅ ACCEPTED 2026-10-10

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
- summary/full-log text is decoded as UTF-8 so reporter labels such as `STUDIO · TRACK DNA` render without mojibake;
- automatic refresh is allowed only while the report surface is visible, remains read-only, and must stop when that surface is hidden or the app closes;
- automatic polling checks only `latest.json` on the 15-second cadence; full history enumeration is performed on initial/manual refresh or when a genuinely new deployment id is detected.

## Latest report surface

The **Latest deployment report** card is the fast current-state surface. It shows:

- authoritative PASS / FAILED / CANCELLED result;
- release and previous release;
- mode and target;
- start/finish timestamps and duration;
- deployment id;
- read-only **View summary** and **View full log** actions after artifact-path validation.

The latest card remains independent from deployment-access state. A LOCKED ZDeployPet session may still read deployment reports because monitoring is read-only.

## Deployment history table

The **Deployment history** table sits directly below the latest-report card and is the primary historical browsing surface.

Row model, newest first:

`Started | Release | Mode | Target | Result | Duration | Summary | Full log`

Accepted behavior:

- render at most the newest 30 trusted reports;
- derive each row from the same schema-v1 parser used by the latest card, not from filenames alone;
- enumerate dated report JSON below the canonical report root and ignore root `latest.json` as a duplicate history source;
- deduplicate by `deployment_id`;
- preserve reporter result as authoritative row result;
- visually distinguish PASS / FAILED / CANCELLED while retaining literal result text;
- skip malformed, unsupported, partially written, escaped or otherwise untrusted candidates and expose a bounded skip count instead of crashing;
- **Summary** and **Full log** on each row use that selected report's validated artifact paths and the same bounded read-only viewer as the latest card;
- row selection/actions grant no deployment authority and perform no write;
- bounded polling is lightweight and does not repeatedly rescan history when nothing changed.

## Accepted implementation

Completed on `main` and operator-smoked on 2026-10-10:

- WSL history discovery scans dated `*.json` report files below the canonical report root and excludes root `latest.json`;
- candidate enumeration is capped at 90 paths before report parsing, while the UI renders at most the newest 30 trusted deployment ids;
- every historical row is parsed through the same schema-v1 Core parser used by the latest card;
- artifact paths are revalidated against the canonical report root before a row is trusted;
- duplicate deployment ids are suppressed;
- malformed/untrusted candidates are skipped and counted in the history status line;
- the history table shows Started, Release, Mode, Target, Result and Duration plus row-specific Summary / Full log actions;
- row actions use the selected report's own artifact paths and the bounded read-only artifact viewer;
- WSL stdout/stderr decoding is explicit UTF-8;
- deterministic Core history selection owns bounded candidate processing, path/artifact validation, deployment-id deduplication, newest-first ordering and row limiting;
- automated Core tests cover history candidate bounds, row bounds, deduplication, ordering, malformed/partial JSON, report-path escape and artifact-path escape;
- the user reported the Release solution test/build run successful on 2026-10-10;
- the initial 15-second full refresh was corrected after operator smoke showed history churn and repeated activity-log entries;
- the accepted timer performs a lightweight `latest.json` check every 15 seconds only while the report surface is visible, suppresses unchanged-result log spam, and triggers the expensive history refresh only after a newly observed deployment id;
- operator smoke showed the latest PASS card stable, the history table populated with 30 trusted rows newest-first, and activity reduced to one latest-load plus one history-load event instead of repeated polling noise.

## Acceptance result

PDA-4 is accepted because:

- valid real `latest.json` renders correctly;
- real historical Millenova runs render newest-first in the bounded Deployment history table;
- Started, Release, Mode, Target, Result and Duration are visible per trusted row;
- historical Summary / Full log actions use the selected deployment's artifacts;
- root `latest.json` is not duplicated as a history row;
- malformed/partial/untrusted reports fail closed without crashing the shell;
- report/artifact path escapes are rejected;
- PASS/FAILED/CANCELLED remain reporter-authoritative regardless of process/UI state;
- validated summary/full-log reads stay bounded under the configured report root;
- UTF-8 artifact decoding is explicit;
- bounded automatic polling is visible-only, lightweight when unchanged, and stops with the report surface/app lifecycle;
- monitoring performs no deployment-side write;
- automated parser/path-boundary/history-selection tests pass in the user-reported Release solution run;
- published-development smoke passed against the real Millenova report root for both latest and historical reports.

**Next phase:** PDA-5 — Allowlisted dry-run executor.
