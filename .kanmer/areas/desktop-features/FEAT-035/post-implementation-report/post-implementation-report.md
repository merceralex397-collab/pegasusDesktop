# Post-implementation report — FEAT-035

## Result

Implemented the planned DVLA/DVSA vehicle gateway contract in the ticket worktree `C:\\Users\\PC\\Documents\\GitHub\\pegasus-worktrees\\dsk-07-09-vehicle-endpoints` on branch `task/dsk-07-09-vehicle-endpoints`.

## Scope delivered

- Pegasus-owned vehicle request and response DTOs.
- Authenticated, case-authorized lookup request, suggestion acceptance, and evidence/status routes.
- Core-backed normalization, concurrency, edit-lease, idempotency, refusal mapping, seven-outcome projection, provenance/cache-age projection, correlation, and weak ETag.
- OpenAPI snapshot/exporter alignment and endpoint/FRD documentation.
- Contract and replay-adapter integration tests.

## Validation

- Release solution build: 0 warnings, 0 errors.
- API contract tests: 27 passed.
- Focused vehicle Core tests: 36 passed.
- Replay adapter tests: 2 passed.
- Architecture tests: 121 passed.
- Required filtered integration suite: 970 passed, 2 skipped, 0 failed, 972 total.
- Diff check passed.
- Secret scan found no credential values or provider keys; only the existing bearer/token redaction regex matched.
- No live provider, cloud write, deployment, upstream sync, or corpus mutation was performed.

## Review handoff

The simplification pass is recorded in the ticket plan. The ticket is ready for an independent `pegasus-desktop-reviewer` review against the ticket plan, acceptance criteria, API contract, Core ownership, security boundary, and validation evidence. No merge or Kanmer finalization is claimed by this report.

## Review-fix amendment (2026-08-30)

The first independent review was BLOCK. The blocking findings were fixed: provider correlation now persists through the queued request and worker into provider headers/action history; a real HTTP → Core → SQL → worker → private replay integration test was added; missing observations use the typed refusal; expectedVersion is required and validated; and OpenAPI now exposes the closed enums and conditional GET/ETag contract. The prior simplification statement that no correlation migration was needed is superseded and corrected in the plan.

Final validation on the corrected compiled tree:

- Release solution build — 0 warnings, 0 errors.
- Full API contract suite — 29 passed, 0 failed.
- Focused vehicle Core suite — 36 passed, 0 failed.
- Focused vehicle/replay/production integration suite — 27 passed, 0 failed.
- Architecture suite — 121 passed, 0 failed.
- Required filtered integration suite — 972 passed, 2 skipped, 0 failed, 974 total.
- Migration schema guard — 1 passed.
- `git diff --check` — passed; only normal LF/CRLF conversion warnings.
- Secret scan — no credential values or provider keys introduced or exposed; only the existing bearer/token redaction regex matched.
- No live provider, cloud write, deployment, upstream sync, or corpus mutation was performed.

The corrected branch is ready for a fresh independent `pegasus-desktop-reviewer` review. No merge, proof, or Kanmer finalization is claimed.

## Hosted CI correction (2026-08-30)

The first exact-head hosted run `33280183638` failed only in the unit job's filtered API contract portion: Core 941/941 and architecture 121/121 passed, while 7/18 contract tests encountered SQL-dependent authentication middleware on the clean runner. This was diagnosed from the hosted job log, not waived. A scoped in-memory `IUserStore<PegasusIdentityUser>` was added to both contract web factories in commit `3663cd779194e7f24fc59a99d724e12ba54261d6`; the exact `Category=Contract` suite then passed locally 18/18. PR #51 has been updated and hosted CI rerun is pending. No merge or finalization is claimed.

## Fresh review-fix correction (2026-08-30)

The first review was against pre-CI-fix head 4f9dfc1e and returned BLOCK. Before requesting a fresh review, the final branch corrected all three findings: automatic reconciliation now defers all registration validity to Core; durable provider correlation is returned separately from the current HTTP correlation on queue/replay/evidence responses; and the correlation migration uniquely backfills legacy rows from each work-item ID before enforcing non-null storage. Regression coverage covers invalid automatic input plus idempotent replay/read correlation separation.

Corrected local evidence: Release build 0 warnings/0 errors; Core 941/941; Architecture 121/121; exact contract filter 18/18; focused vehicle/SQL filter 31/31; full Category!=Corpus&Category!=Browser integration 973 passed, 2 skipped, 0 failed, 975 total; migration guard 1/1; diff check passed. No merge, proof, hosted-green claim, or Kanmer finalization is claimed. Fresh independent review and exact-head hosted CI remain required.

## Exact-head CI failure and correction (2026-08-30)

Hosted run `33282640860` at `e2e9a2f5cfa4ba2827d73afb934d2da4bed025b9` exposed one concrete test-harness defect: `OpenApiSnapshotTests.DisabledGatewayDoesNotExposeOpenApiDocument` authenticated the request, so the 404 status-page re-execution invoked the rail count query and returned 500 when `PegasusDevelopment` was absent. This was not waived. The test now opts into the existing unauthenticated contract header for that probe, and the focused test plus full contract suite pass locally (1/1 and 18/18). The branch must be committed/pushed, independently reviewed at its new exact head, and re-run through hosted CI before merge.

## Final-head hosted CI success (2026-08-30)

Run `33283250011` passed at exact head `cc91137a4a9e95b99021fe652d367677e3f2c574`.

## Independent review PASS (2026-08-30)

Helmholtz the 2nd independently reviewed exact head `cc91137a4a9e95b99021fe652d367677e3f2c574` and returned PASS with no findings. The review confirmed the three prior corrections, the test-only CI harness fix, acceptance coverage, security boundary, simplification evidence, and exact-head hosted CI run `33283250011`. PR #51 is merge-ready for `dev`.

## Merge and promotion evidence (2026-08-30)

PR #51 merged into `dev` as `8aa8f211d34f9b476c5231eff60fce071104b4e3` after exact-head hosted CI run `33283250011` passed and Helmholtz the 2nd returned independent PASS at `cc91137a`. The documented atomic exact-SHA promotion advanced both remote `dev` and `main` to `8aa8f211`. A main-head repository-check run is in progress; proof and Kanmer closeout remain pending.
