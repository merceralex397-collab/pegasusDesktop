# Proof — DOCS-003

## Merged-main identity

- PR #33 merged into `dev`; merge commit: `add9da25f0e3e1465bc3d821c240a9d3d398ada5`.
- Read-only remote refresh on 2026-08-28:
  - `origin/dev`: `add9da25f0e3e1465bc3d821c240a9d3d398ada5`
  - `origin/main`: `add9da25f0e3e1465bc3d821c240a9d3d398ada5`
  - `origin/main` is an ancestor of `origin/dev`: true.

## Delivery evidence

- PR #33 exact-head repository-check run `33132439185`: all applicable jobs passed, including changes, documentation, local-development-scripts, reference-data, infrastructure, unit, browser, SQL integration shards 1–3, and coverage.
- Main push repository-check run `33133354505`: `headSha=add9da25f0e3e1465bc3d821c240a9d3d398ada5`, status `completed`, conclusion `success`.
- Main CI URL: https://github.com/merceralex397-collab/pegasusDesktop/actions/runs/33133354505
- PR URL: https://github.com/merceralex397-collab/pegasusDesktop/pull/33

## Local validation

- `dotnet restore ./Pegasus.slnx --locked-mode` — passed.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore /nr:false` — passed, 0 warnings, 0 errors.
- Focused auto-link Core tests — passed.
- Full persistence class — 30 passed.
- Migration/runtime-role tests — 17 passed.
- Canonical full local test run with `Category!=Corpus` — Core 939, API 12, architecture 111, integration 1017; 2 skipped, 0 failed.
- Added-source negative scan found no external Graph, Box, Azure, or mail references.

## Scope truth

- The append-only issued-report-version ledger and legacy-unresolved migration behavior are merged.
- The ticket does not claim gateway Reports API, native Reports UI, deployment, mailbox mutation, Box mutation, or Azure activity.
