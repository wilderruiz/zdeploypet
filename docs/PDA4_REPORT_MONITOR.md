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
- no report path, content or artifact may grant deployment authority.

## Initial implementation sequence

1. add immutable Core report contracts and a schema-v1 parser/validator;
2. add Core path-boundary validation for report/artifact paths;
3. add automated tests for valid pass/fail/cancelled reports, malformed/partial JSON, unsupported schema and path escapes;
4. add a narrow WSL read-only report-root reader for `latest.json` and bounded artifacts;
5. add a shell report-status surface showing deployment id, release, mode, target, timestamps and authoritative result;
6. add read-only links/actions for validated summary/full-log artifacts;
7. add safe refresh/watch behavior without deployment-side writes;
8. smoke against real Millenova reporter output.

## Acceptance gate

PDA-4 is accepted when:

- valid real reporter JSON renders correctly;
- malformed/partial reports fail closed without crashing the shell;
- report/artifact path escapes are rejected;
- PASS/FAILED/CANCELLED remain reporter-authoritative regardless of process/UI state;
- validated summary/full-log artifacts stay bounded under the configured report root;
- monitoring performs no deployment-side write;
- automated parser/path-boundary tests pass;
- published-development smoke passes against the Millenova report root.
