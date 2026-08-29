# Checklist — FND-009 immutable release tags

- [x] Record the `gateway/r<N>` and `desktop/v<M.m.b>` conventions in the release documentation and both synchronized release-skill copies.
- [ ] Add the exact “after promotion read-back” wording and the C-01/2x Windows-runner cost rationale to `docs/engineering.md` through a remediation PR.
- [ ] Apply the first `gateway/r<N>` tag on the next authorized promoted `main` release and record it in `docs/operations.md`.
- [x] Exact-head documentation CI run `33009752135` passed; .NET/SQL/browser lanes were correctly path-skipped for this docs-only diff.
- [ ] Independent review before merge — unavailable because PR #24 already merged; retrospective review is recorded honestly.
- [ ] Write final proof containing the applied tag output and operations record.

## Current blocker

The first gateway tag is a release-time acceptance criterion and cannot be created under the current no-release/no-cloud constraint. The canonical wording gap requires an in-repo remediation PR. No release or tag proof is claimed.
