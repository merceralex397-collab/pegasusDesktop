# Post-implementation report — GWY-001

## Scope delivered

- Added `src/Pegasus.Contracts/ContractConventions.cs` as the documented marker for DTO suffixes, Core-record exclusion, string enum serialization, UTC `DateTimeOffset`, and the stable assembly anchor.
- Extended the existing `ContractsProjectHasNoDependencies` fact in `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` with exact-or-dot-qualified assembly-reference checks for the eight forbidden dependency families.
- Verified all FND-029-owned contract files in place and unchanged. No duplicate envelope type, host change, or new project was introduced.

## Validation

- `dotnet build Pegasus.slnx -c Release` — succeeded, 0 warnings, 0 errors.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj -c Release` — 110 passed, 0 failed, 0 skipped.
- Static checks — no dependency XML references in `Pegasus.Contracts.csproj`; one `PagedResult` declaration; no `Total` or `ActionActor`; `Problems` and `Commands` directories absent; boundary-corrected forbidden-name check returned 0.
- The prescribed unanchored `ProblemTypes\\b` grep has legitimate false positives from the required `PegasusProblemTypes` symbol; this is recorded in the plan with the corrected boundary check.

## Delivery state

- Branch: `task/gwy-001-contract-conventions`
- Commit: `b1fe439b`
- PR: #27, base `dev`, head `b1fe439b`
- Independent review and exact-head CI: pending.
- No endpoint behavior is claimed; this is tier-1 static/build/architecture evidence only.

## Review remediation — 2026-08-26

- Addressed the independent review finding by adding the requested XML documentation to the existing `PagingLimits`, `PegasusProblem`, and `MutationEnvelope` contract files. No code or wire-shape behavior changed.
- Commit: `ed4e2776`; PR #27 remains open against `dev`, now at head `ed4e2776`.
- Fresh local validation: `dotnet build Pegasus.slnx -c Release -nr:false` succeeded with 0 warnings/errors; architecture tests passed 110/110 with 0 failed/skipped; static contract checks and `git diff --check` passed as recorded in the plan.
- Fresh independent review and exact-head CI are pending; no endpoint behavior is claimed.

## Independent review — 2026-08-26

- Hilbert completed a fresh independent review of PR #27 at exact head `ed4e2776c8529d2d4d170b6fab52fd20d39594b4`.
- Verdict: implementation PASS; the prior XML-documentation omission is fully remediated. The review confirms `PagingLimits.cs` documents lower endpoint caps and ListIntake’s 100 limit, `PegasusProblem.cs` documents the safe RFC 9457 boundary and typed-version rationale, and `MutationEnvelope.cs` documents body placement, `desk:<guid>`, validation ownership, and the 200-character exception.
- Review found no remaining plan, scope, implementation, or simplification gap. The review is not merge approval yet only because exact-head CI run `33021359764` remains in progress for SQL shards 1 and 3; all other jobs are green.
