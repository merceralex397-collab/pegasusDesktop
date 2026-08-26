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
