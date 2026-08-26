# Post-implementation report

## Delivered

INTK-007 is implemented in the PegasusDesktop repository on branch `task/upstream-intk-033-triage-from-intake`, commit `c85e1f1e33e3b7159c70ecf58c294379734300ba`, PR #21 targeting `dev`.

- The accepted QDOS `triage-request` classification is the single trigger.
- A classified request is downgraded to `NeedsSorting` before automatic case allocation, so no `case_type_unavailable` case attempt is made.
- Exactly one strong `AcceptedTriageMatch` is derived from the matched `body.triage-only-request` classification predicate.
- With a known registration, the queued worker creates an open pre-case Triage and does not register Unidentified.
- Without a registration, the worker registers an open Unidentified item and creates no Triage.
- The existing Unidentified reconciliation owner resolves an open item to a Triage created in the same processing pass.
- Both supported QDOS subject registration spacings are extracted and the vehicle-description rule no longer consumes the registration label.
- The inactive matcher port and no-op implementation were removed; production composition now pins the real classification/extraction route.
- Governing FRD, open-decision, QDOS mapping, and carryover documents were updated. `docs/operator-notes.md` and `docs/capabilities.md` were checked and not changed.

## Scope and constraints

The inherited upstream sync/re-check was amended out of scope under the current operator boundary. All work is in PegasusDesktop from `origin/dev`; no upstream repository operation, cloud write, mailbox/Box write, deployment, credential change, or release operation was performed.

The four existing downstream integration suites retain their valid accepted-match fixture because their downstream contract is unchanged; the real default route is covered by the added tests in `QdosTriageIntegrationTests`. This scope deviation is recorded in the checklist and does not weaken the acceptance path.

## Validation

- `dotnet restore` — passed.
- `dotnet build --configuration Release` — passed, 0 warnings, 0 errors.
- `dotnet test tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release` — passed, 935/935.
- Focused final Core validation — passed, 119/119.
- Targeted SQL integration (`QdosTriageIntegrationTests`, `ProductionCompositionTests`) — passed, 19/19.
- Full non-Corpus/non-Browser SQL integration — passed, 886 passed, 2 expected skips, 0 failed.
- `git diff --check` — passed before commit.
- Independent simplification pass by Ohm — completed; findings applied and recorded in the plan.

## Remaining delivery gates

PR #21 requires exact-head CI and independent review before merge into `dev`. After merge, verification must run on merged `main` and write `proof.md` before Kanmer closeout.

## Exact-head CI — 2026-08-26

Because the PR event did not register a workflow run, the existing `repository-check` workflow was given its authorized manual `workflow_dispatch` trigger in commit `c25099f92681db991a0003146991b676d1c8b82b`. Manual run [32992629383](https://github.com/merceralex397-collab/pegasusDesktop/actions/runs/32992629383) ran at that exact head and passed all jobs: local-development-scripts, changes, documentation, reference-data, browser, unit, infrastructure, sql-integration (1), sql-integration (2), sql-integration (3), and sql-integration-coverage.

This resolves the prior exact-head CI-only review finding. Independent re-review of the final PR head, merge to `dev`, merged-`main` verification, and `proof.md` remain outstanding.
