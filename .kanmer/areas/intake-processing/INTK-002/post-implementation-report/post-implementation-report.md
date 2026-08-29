# Post-implementation report — INTK-002

## Result

Implemented commit `65a10183` on branch `intk-002-recover-dispatched-work` and pushed it to `origin`. The existing reconciliation timer now returns an unleased `dispatched` row older than one hour since `DueAtUtc` to `pending`; dispatch and processing remain the existing paths.

## Acceptance evidence

- Stale unleased `dispatched` work is recovered, re-dispatched, processed once, and reaches `completed`.
- Fresh unleased `dispatched` work is unchanged.
- Duplicate delivery after recovery returns `NoOp`; evaluation revision remains 1 and retained receipt count remains 1.
- `AttemptCount` is preserved through recovery; the existing bounded retry/poison tests remain green.
- The existing reconciliation timer is reused; no table, migration, Web/API route, desktop project, or second timer was added.
- Threshold evidence is recorded in this ticket's plan and `docs/operations.md`: one hour versus five-minute visibility timeout and seven-day default queue message TTL.

## Validation

- `dotnet restore Pegasus.slnx --locked-mode` — passed.
- `dotnet build Pegasus.slnx --no-restore --configuration Release --nologo` — passed, 0 warnings, 0 errors.
- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --no-restore --configuration Release --no-build --filter "FullyQualifiedName~RecoveryTests" --logger "console;verbosity=minimal"` — 32 passed, 0 failed, 0 skipped.
- `git diff --check` — passed before commit.

## Evidence boundary

The repository's L-02 `Initialize-LocalDevelopment.ps1` / `Invoke-LocalDevelopment.ps1 -Action Start` caller proof remains unavailable: the existing launcher fails before Web/Functions readiness at line 1482 because `Process.Path` is empty for its PowerShell-owned launcher. The failure is separately recorded by AUTO-002. This ticket claims LocalDB-backed integration evidence only; it does not claim Azurite or Functions-host execution.

## Review and delivery

- Simplification pass is recorded in the ticket plan.
- Branch pushed: `origin/intk-002-recover-dispatched-work`.
- PR creation is the next delivery step; merge into `dev` requires independent review and green CI.

## PR creation blocker — 2026-08-25

`gh pr create --base dev --head intk-002-recover-dispatched-work` was attempted after the branch push and failed with the exact GitHub response: `GraphQL: must be a collaborator (createPullRequest)`. The branch is available at `origin/intk-002-recover-dispatched-work`; the smallest unblock is collaborator permission or an authorized operator creating the PR from that branch. No merge, CI claim, or Kanmer review-stage move is being claimed until the PR exists and the required independent review/CI path is satisfied.

## Independent review follow-up — 2026-08-25

Review result was FAIL on L-02 caller proof, concurrency coverage, the upstream-to-board annotation, and PR/CI availability. The branch now includes the concurrency and live-processing-lease duplicate tests and the `INTK-003 → [[INTK-002]]` carry-over annotation. Release validation must be rerun after these changes. L-02 proof remains unavailable because the existing local launcher fails before readiness; PR creation remains blocked by GitHub collaborator permission.

## Follow-up validation — 2026-08-25

Follow-up commit `338b8a51` is pushed to `origin/intk-002-recover-dispatched-work`.

- `dotnet build Pegasus.slnx --no-restore --configuration Release --nologo` — passed, 0 warnings, 0 errors.
- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~RecoveryTests" --logger "console;verbosity=minimal"` — 33 passed, 0 failed, 0 skipped.
- `git diff --check` — passed before follow-up commit.

The independent review findings on concurrency coverage and carry-over annotation are addressed. L-02 Azurite/Functions caller proof and PR/CI remain open blockers.

## Current-tree validation checkpoint — 2026-08-26

- Merged current configured `origin/dev` into the ticket branch without upstream synchronization; the branch remains limited to the ticket's intended recovery change relative to `origin/dev`.
- `dotnet build Pegasus.slnx --configuration Release --no-restore --nologo -nr:false -p:UseSharedCompilation=false` — passed with 0 warnings and 0 errors.
- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore --no-build --filter "FullyQualifiedName~RecoveryTests" --logger "console;verbosity=minimal"` — 35 passed, 0 failed, 0 skipped.
- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore --no-build --filter "FullyQualifiedName~GroupedImageIntakeConcurrencyTests.ConcurrentGroupMembersNeverSplitAcrossRepeatedRuns" --logger "console;verbosity=minimal"` — 1 passed, 0 failed, 0 skipped after the current `origin/dev` merge.
- The required L-02 Azurite/Functions-host caller journey is still not claimed. The repository launcher fails before readiness because `Start-OwnedLauncher` reads an empty PowerShell-owned `Process.Path`; LocalDB-backed tests do not prove the Azurite timer/queue journey.

## Current L-02 attempt — 2026-08-26

- `Invoke-LocalDevelopment.ps1 -Action Start` was retried with the exact SDK and failed before Web/Functions readiness in `Start-OwnedLauncher` at line 1482 because the PowerShell-owned process reported an empty `Process.Path`. The owned failed run was stopped.
- A direct run in the exact INTK-002 worktree started Azurite and Web. The normal Functions Core Tools build discovered the Worker functions, but the language worker failed during composition with `Unable to resolve service for type Microsoft.AspNetCore.Identity.UserManager<Pegasus.Infrastructure.Persistence.PegasusIdentityUser> while attempting to activate EfStaffAccountAdministration`.
- Therefore no claim is made for the required queue-message loss, timer redispatch, or exactly-once processing journey. This defect is outside INTK-002's explicit scope because the ticket must not touch `src/Pegasus.Worker`; the next action is to resolve it under the stack/runtime owner ticket or a narrowly scoped follow-up, then rerun L-02.
- Delivery state: exact PR #23 head `56fb9b05c9609e08bf14a2e26f71e6d9b8ed5e1f`; CI run `33006548735` completed successfully. Independent review passed static implementation/scope/simplification lenses but does not waive the missing L-02 proof. The ticket remains in `review` and is not merge-ready.

## L-02 retry — 2026-08-29

Retried `pwsh -NoProfile -File ./scripts/Invoke-LocalDevelopment.ps1 -Action Start -StartupTimeoutSeconds 90` in the recorded INTK-002 worktree `C:/Users/PC/Documents/GitHub/pegasusDesktop/.worktrees/intk-002`. It failed before Web, Functions, or Azurite readiness with:

`Invoke-LocalDevelopment.ps1:1482 — Local run '11e3ec0f27fe481d96bfd266949300c4' failed. Diagnostics remain at .../artifacts/local-development/11e3ec0f27fe481d96bfd266949300c4. Exception calling "GetFullPath" with "1" argument(s): "The path is empty. (Parameter 'path')"`

No queue-message-loss, timer-redispatch, or exactly-once caller proof is claimed. This remains the existing local-launcher/stack blocker recorded in the report; INTK-002's allowed source scope does not include the launcher.
