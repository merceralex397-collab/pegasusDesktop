# Proof

## Result

The fork's intake duplication work is complete. The retry taxonomy uses named intake faults, `IntakeDecisionCodes` is the single persisted decision-code vocabulary, Web composition is guarded against Worker intake ownership, and `IIntakeSubmission` remains only because its two concrete callers and the existing grouped-intake fake are documented in the plan.

## Acceptance and local validation

The final implementation record and validation on the task branch report:

- `IntakeExceptionPolicy.IsTransientFailure` contains no raw `IOException`, `TimeoutException`, or `DbException`; named dependency and version faults are used.
- Operations, persistence, and MCP use `IntakeDecisionCodes). Unknown persisted values fail closed: domain/persistence parsing throws `InvalidDataException`, the Operations projection maps unknown/blocked values to `Unknown`, and MCP rejects an unknown filter.
- `DependencyDirectionTests` guards the Web assembly against Worker and Azure Queue ownership; the live Web-composition test confirms neither `ProcessQueuedIntake` nor `IProcessQueuedIntake` is resolvable.
- `IIntakeSubmission` is retained with its two real callers and focused fake; no unsupported port refactor was introduced.
- No operator-facing vocabulary, desktop UI, cloud, deployment, migration, or upstream change was added.

Recorded validation:

- Release build of `Pegasus.slnx` — passed with 0 warnings and 0 errors.
- Focused Core intake tests — 46 passed.
- Architecture tests — 101 passed.
- `RecoveryTests` — 27 passed.
- Live Web composition test — 1 passed; Worker processor services were absent.
- Grouped-intake deadlock retry test after adapter translation — 1 passed; the non-transient SQL error 334 regression — 1 passed and remained non-translated.
- `git diff --check` — passed.

## Review, CI, and merged-main evidence

- PR #7 initial implementation head `e430e9b801687f486094b4b3e08eb627df4f42f1` merged to `dev` as `8cb3c0ffa486be724598a65391805f126a80a7f9`; exact-head repository-check run `32857303628` passed all applicable lanes.
- Independent review passed the final PR #7 scope; the review record is retained in the ticket scratch notes.
- PR #16 supplied the required EF-wrapped deadlock correction at head `5373d9c1dba15c7a27baa037669697f906f82b89`; it merged to `dev` as `ec49e40989e8865f7127ab362bce6b30a7a0c9b0`.
- Exact-head repository-check run `32971272123` for PR #16 passed changes, documentation, local-development-scripts, reference-data, unit, browser, SQL-integration shards 1/2/3, and SQL-integration-coverage; infrastructure was skipped by path selection.
- The independent `pegasus-desktop-reviewer` final verdict for PR #16 was PASS; the review record confirms the full transaction catch, direct-`SqlException`-only test helper, SQL 334 regression, simplification pass, exact two-file scope, and clean worktree.
- After the authorized non-force exact-SHA promotion, read-back was `origin/main=origin/dev=3b1737de2a27f84aa1bea03bf2c34d41d5a8006a`.
- Read-only merged-main checks show PR #16's head is an ancestor of `origin/main`, named fault translation and retry handling are present, `IntakeDecisionCodes` is used by MCP, and the Web composition architecture fact is present.

No upstream operation, cloud write, deployment, credential change, mailbox/Box mutation, or direct `.kanmer` edit was performed.
