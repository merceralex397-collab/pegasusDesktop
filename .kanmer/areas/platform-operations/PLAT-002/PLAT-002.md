---
id: PLAT-002
type: ticket
title: >-
  DSK-10-02 · Retire the committed bootstrap verification account before desktop
  go-live
status: implementing
area: platform-operations
assignee: codex-mcp-client
profile: fix
stageEntered:
  preparing: '2026-08-24T21:21:13.849Z'
taken_at: '2026-08-26T15:17:12.298Z'
branch: task/dsk-10-02-retire-verification-account
worktree: ../pegasus-worktrees/dsk-10-02-retire-verification-account
labels:
  - desktop-conversion
  - plan-10
  - phase-8
  - tier-9
  - needs-operator
groups:
  - EPIC-011
  - HZN-009
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:05:04.687Z'
updated: '2026-08-26T15:17:12.298Z'
---

## What

Replace the committed `Bootstrap:VerificationAccount` block in `src/Pegasus.Web/appsettings.json` with `{ "Removed": "claudeuiverification" }`, deploy it, confirm the account is deleted on the production estate, and remove the verification-account clause from `docs/operations.md`.

## Why

`src/Pegasus.Web/appsettings.json:8-14` currently commits an enabled production Administrator (`claudeuiverification`) **with its password in source control**. `docs/operations.md:768-775` records that it exists at the operator's request and "must be removed before go-live". The desktop conversion's go-live is that moment: proposal §17.1 requires that no production credential ships in the client and that server-side permission checks and account revocation are trustworthy, and the plan's risk table lists "plaintext verification account shipped to go-live" as a Phase 8 exit-gate item. Operator-visible consequence: anyone who can read the repository can sign in to production as an Administrator. Related: [[DSK-10-01]] carries this as a live register row until it ships.

## Source of truth

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-02`
- Plan detail: same file § 2 (Facts — Secrets), § 4 (target state), § 7 (risks and traps — "Plaintext verification account shipped to go-live")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 17.1 Required controls `:1153-1172`; § 24 Phase 8 exit gate `:1885-1890`
- Repository evidence:
  - `src/Pegasus.Web/appsettings.json:8-14` — the committed block, including the `//` comment that documents the retirement mechanism
  - `src/Pegasus.Web/Program.cs:678-702` — the bootstrap gate that decides whether reconciliation runs
  - `src/Pegasus.Web/Program.cs:986-1040` — `ReconcileVerificationAccountAsync`: when `Bootstrap:VerificationAccount:Removed` is set (and no username), it calls `userManager.FindByNameAsync(removed.Trim())` then `DeleteAsync` and returns
  - `docs/operations.md:768-775` — the operations clause to be removed
  - `scripts/Invoke-ProductionSmoke.ps1:1-45` — the production smoke entry point and its mandatory parameters
- Binding decisions:
  - **D-001** (2026-08-23) — the fork becomes the single release source at the first production gateway change; this deployment goes through the fork's release route, not upstream.
  - **L-02** — there is no Azure test environment (ADR-0014); the change is proved on the local stack first, then in production.
- Depends on: an operator decision and an operator-run production release. The plan row records the dependency as "operator decision"; it resolves to no plan handle, so it is not listed in `dependsOn`.

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `pegasus-release` (`.agents/skills/pegasus-release/SKILL.md`) for the gateway release route → `run-tests` (dotnet/skills `98f84851`, plugin `dotnet-test`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`). Azure MCP **read-only** `containerapps` may be used to read the deployed revision; no Azure write tool.
- **Kanmer pipeline** for profile `fix`: `kanmer-research` (optional) → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (leave-preparing needs `files`, `plan`, `questions-resolved`; enter-review needs `post-implementation-report`; enter-done needs `proof`)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row, `docs/operations.md:768-775`, and `src/Pegasus.Web/Program.cs:986-1040` so the deletion mechanism is understood before editing anything. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. **Operator step** — obtain explicit confirmation that desktop go-live has reached the point where the verification account is no longer needed for interface verification, and that a production deployment of `Pegasus.Web` is approved for this exact target (`docs/runbook.md` § Live-operation approval matrix, row "Deploy, restore, fail over, or retire"). Record the approval text and date in the ticket's `plan` document. Do not proceed without it.
3. On a branch `task/dsk-10-02-retire-verification-account` cut from `dev`, edit `src/Pegasus.Web/appsettings.json` so the `Bootstrap` object reads exactly:
   ```json
   "Bootstrap": {
     "VerificationAccount": {
       "Removed": "claudeuiverification"
     }
   }
   ```
   Delete the `UserName` and `Password` properties and the `//` comment that describes the temporary account. Keep the JSON valid and the surrounding keys untouched.
4. Confirm the gate at `src/Pegasus.Web/Program.cs:686-687` still fires: it runs reconciliation when `Bootstrap:VerificationAccount:UserName` **or** `:Removed` is non-empty, so the `Removed` form alone still reaches `ReconcileVerificationAccountAsync`. Do not change `Program.cs`.
5. Add or extend a test in `tests/Pegasus.IntegrationTests` that starts the host with `Bootstrap:VerificationAccount:Removed` set, seeds a user with that name, and asserts the user is absent afterwards — and a second case asserting that a host with the block absent entirely deletes nothing. Name them so they read as the regression they are (for example `RemovedVerificationAccountIsDeletedOnStart`).
6. Run `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~VerificationAccount"` and expect the new tests green.
7. Run `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition -ArtifactRoot ./artifacts/test-shards -ShardCount 3` (the repository's shard-assignment guard) so the new tests land in exactly one shard, then `pwsh ./scripts/Test-TestShard.ps1`. Both must exit 0.
8. Prove the secret is gone from the working tree: `git grep -n "Pegasus-UI-Verify"` and `git grep -n "claudeuiverification"` — expected: the only remaining hits are the `Removed` value, the ticket documents and the change log. Record in the ticket that the password remains in git **history** and is therefore treated as permanently disclosed; rotation is deletion, not redaction.
9. Edit `docs/operations.md` to delete the "Temporary verification account" clause at `:768-775` and add a dated line in its place recording that the account was removed by this ticket, the release it shipped in, and the confirmation evidence. Run `pwsh ./scripts/Test-DocumentationLinks.ps1`.
10. **Operator step** — release `Pegasus.Web` to production by the existing route (load `pegasus-release` and follow it; the desktop route in `docs/desktop/09-release-update-and-distribution/README.md` does not apply to the gateway). Hand back: the release number, the image digest, the revision suffix.
11. Confirm deletion on the estate: run `pwsh ./scripts/Invoke-ProductionSmoke.ps1 -BaseUri <production base uri> -ExpectedSourceRevision <40-hex sha> -ExpectedVersion <version> -ResourceGroupName rg-pegasus-prod -SubscriptionId e6076573-23a5-46a8-acef-7e22d264e5db -ExpectedWorkerActivation approved-live-worker` and expect it to pass. Then, with the operator, attempt a sign-in as `claudeuiverification` and expect it to be refused as an unknown account; capture the result as the proof (no password may be written into the proof document).
12. If the smoke script has no assertion for the account's absence, extend it with one narrow check that the account cannot sign in — the plan row names "`Invoke-ProductionSmoke.ps1` extension" as the verification — and keep the check credential-free (assert the negative outcome, never store the password).
13. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, then open the PR into `dev` and hand review to `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] `src/Pegasus.Web/appsettings.json` contains no username and no password; the `Bootstrap:VerificationAccount` object holds only `Removed`.
- [ ] A test asserts that the `Removed` form deletes the named account on start, and that an absent block deletes nothing.
- [ ] `git grep "Pegasus-UI-Verify"` returns no configuration hit in the working tree.
- [ ] The account is confirmed absent on the production estate after the deployment, with dated evidence in the ticket proof.
- [ ] `docs/operations.md` no longer instructs that the account exists; it records the removal, its date and its release.

## Verification

- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~VerificationAccount"` — expected: all tests pass, including the new deletion test.
- [ ] `git grep -n "Pegasus-UI-Verify"` — expected: no match under `src/`.
- [ ] `pwsh ./scripts/Invoke-ProductionSmoke.ps1 -BaseUri <production base uri> -ExpectedSourceRevision <40-hex sha> -ExpectedVersion <version> -ResourceGroupName rg-pegasus-prod -SubscriptionId e6076573-23a5-46a8-acef-7e22d264e5db -ExpectedWorkerActivation approved-live-worker` — expected: exits 0 after the release.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exits 0.

## Evidence tier

Tier 9 — Security/observability. Here that obliges an observable production outcome, not a green suite: the proof must show the account refused on the deployed estate after the release, and the test must prove the deletion path rather than the configuration shape alone.

## Documentation changes

- `docs/operations.md` — delete the "Temporary verification account" clause (`:768-775`); record the removal, its date and its release number.
- `docs/desktop/10-security-observability-performance/threat-register.md` — mark the corresponding row closed once the production confirmation exists ([[DSK-10-01]]).

## Guardrails

- **Azure**: no write from this ticket's tooling. The production **deployment** is an operator-run release under `docs/runbook.md` § Live-operation approval matrix ("Deploy, restore, fail over, or retire": exact environment, explicit approval, rollback path) and is mirrored in `docs/desktop/11-azure-disposition/README.md`. Azure MCP use is read-only.
- **Scope boundary**: may touch `src/Pegasus.Web/appsettings.json`, `tests/Pegasus.IntegrationTests`, `scripts/Invoke-ProductionSmoke.ps1`, `docs/operations.md`. Must not touch `src/Pegasus.Core`, the desktop projects, `infra/`, or any other credential. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: the password is in git history and cannot be un-published — treat the credential as permanently disclosed and say so in the proof; deleting the block **without** the `Removed` key leaves the account alive on the estate because reconciliation then does nothing (`Program.cs:686-687`, `:1016-1019`); the two-environments rule (ADR-0014) means there is no staging rehearsal — the local stack is the rehearsal.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Outcome

_Filled at closeout._
