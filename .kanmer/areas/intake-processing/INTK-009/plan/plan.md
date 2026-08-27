# INTK-009 implementation plan

## Objective

Make concurrent durable completion of independent members of one grouped image submission reliable on SQL Server while retaining the FRD-02 guarantees: durable dispatch/evaluation, idempotent duplicate delivery, one group-level routing decision, and no split registration.

## Sequence

1. In the INTK-009 worktree, reproduce the focused grouped-image SQL test against the repository's supported LocalDB setup and capture the exception shape, transaction boundary, and persisted rows.
2. Inspect the generated SQL/schema/indexes and the existing completion/claim conventions. Identify the smallest owner-level change that removes the deadlock or correctly bounds a retry around only the completion operation. A solution must not hide a failed completion, weaken the test, or retry unrelated work.
3. Implement that focused repair in `EfIntakeWorkStore` (and only its direct focused tests if needed). Preserve the lease check, atomic evaluation plus work-item state update, unique per-receipt revision, and safe replay after a completed transaction.
4. Run the focused concurrency test repeatedly, then the affected integration persistence tests and repository-required restore/build/test commands. Record exact commands and outcomes.
5. Run the required simplification pass over the branch diff. Apply behaviour-preserving findings and record any unapplied finding with its reason.
6. Obtain independent review from an agent that did not implement INTK-009. Address actionable findings, rerun affected validation, and record the review.
7. Open a PR targeting `dev`, require the exact-head CI checks and review evidence, then merge only after those gates are green. Do not edit or merge PR #14 as part of this ticket.
8. Immediately after merge, verify the merged exact SHA, write Kanmer proof on the merged `main` per repository policy, move one Kanmer boundary at a time, release the claim, and clean only this ticket's worktree/branch.

## Acceptance and validation

- The exact grouped-member race passes repeatedly without relying on a test-only increased retry count.
- No completed work item is left without exactly one corresponding evaluation revision for the successful completion, and no duplicate evaluation or downstream group registration is introduced.
- A deadlock, if still possible at a supported external transaction boundary, is surfaced as a bounded retryable processing failure rather than translated into a misleading permanent product decision.
- Existing non-transient completion failures still pass through unchanged.
- Required commands will include the focused test, affected persistence tests, `dotnet restore`, `dotnet build --configuration Release`, and the repository's focused/full `dotnet test` profile as applicable; exact outcomes are recorded in the ticket report and proof.

## Scope boundaries

No changes to cloud/deployment state, credentials, upstream remotes, PR #14, unrelated transaction stores, or speculative schema/architecture. No blanket EF execution-strategy change and no weakening or deletion of the concurrency assertion.

## Simplification pass

Pending implementation. The dated result will be appended here before review.
