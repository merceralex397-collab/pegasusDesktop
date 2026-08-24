# Plan — PLAT-002

## Objective

Replace the committed `Bootstrap:VerificationAccount` block in `src/Pegasus.Web/appsettings.json` with `{ "Removed": "claudeuiverification" }`, deploy it, confirm the account is deleted on the production estate, and remove the verification-account clause from `docs/operations.md`.

## Chosen approach

`src/Pegasus.Web/appsettings.json:8-14` currently commits an enabled production Administrator (`claudeuiverification`) **with its password in source control**. `docs/operations.md:768-775` records that it exists at the operator's request and "must be removed before go-live". The desktop conversion's go-live is that moment: proposal §17.1 requires that no production credential ships in the client and that server-side permission checks and account revocation are trustworthy, and the plan's risk table lists "plaintext verification account shipped to go-live" as a Phase 8 exit-gate item. Operator-visible consequence: anyone who can read the repository can sign in to production as an Administrator. Related: [[DSK-10-01]] carries this as a live register row until it ships.

## Governing docs

- No canonical PRD/FRD/ADR is linked yet. `docs_todo: true` is intentionally retained: several desktop conversion decisions named by the ticket are planned canonical documents and must not be linked until they exist on `origin/dev`.
- Use the ticket's Source of truth and the owning desktop-area plan as the current planning authority; add a real governing-doc ref only through `link_doc` after the file exists.

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `pegasus-release` (`.agents/skills/pegasus-release/SKILL.md`) for the gateway release route → `run-tests` (dotnet/skills `98f84851`, plugin `dotnet-test`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`). Azure MCP **read-only** `containerapps` may be used to read the deployed revision; no Azure write tool.
- **Kanmer pipeline** for profile `fix`: `kanmer-research` (optional) → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (leave-preparing needs `files`, `plan`, `questions-resolved`; enter-review needs `post-implementation-report`; enter-done needs `proof`)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Ordered implementation

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
7. Run `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition` (the repository's shard-assignment guard) so the new tests land in exactly one shard, then `pwsh ./scripts/Test-TestShard.ps1`. Both must exit 0.
8. Prove the secret is gone from the working tree: `git grep -n "Pegasus-UI-Verify"` and `git grep -n "claudeuiverification"` — expected: the only remaining hits are the `Removed` value, the ticket documents and the change log. Record in the ticket that the password remains in git **history** and is therefore treated as permanently disclosed; rotation is deletion, not redaction.
9. Edit `docs/operations.md` to delete the "Temporary verification account" clause at `:768-775` and add a dated line in its place recording that the account was removed by this ticket, the release it shipped in, and the confirmation evidence. Run `pwsh ./scripts/Test-DocumentationLinks.ps1`.
10. **Operator step** — release `Pegasus.Web` to production by the existing route (load `pegasus-release` and follow it; the desktop route in `docs/desktop/09-release-update-and-distribution/README.md` does not apply to the gateway). Hand back: the release number, the image digest, the revision suffix.
11. Confirm deletion on the estate: run `pwsh ./scripts/Invoke-ProductionSmoke.ps1 -BaseUri <production base uri> -ExpectedSourceRevision <40-hex sha> -ExpectedVersion <version> -ResourceGroupName rg-pegasus-prod -SubscriptionId e6076573-23a5-46a8-acef-7e22d264e5db -ExpectedWorkerActivation approved-live-worker` and expect it to pass. Then, with the operator, attempt a sign-in as `claudeuiverification` and expect it to be refused as an unknown account; capture the result as the proof (no password may be written into the proof document).
12. If the smoke script has no assertion for the account's absence, extend it with one narrow check that the account cannot sign in — the plan row names "`Invoke-ProductionSmoke.ps1` extension" as the verification — and keep the check credential-free (assert the negative outcome, never store the password).
13. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, then open the PR into `dev` and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~VerificationAccount"` — expected: all tests pass, including the new deletion test.
- [ ] `git grep -n "Pegasus-UI-Verify"` — expected: no match under `src/`.
- [ ] `pwsh ./scripts/Invoke-ProductionSmoke.ps1 …` (parameters as step 11) — expected: exits 0 after the release.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exits 0.

## Risks and constraints

- **Azure**: no write from this ticket's tooling. The production **deployment** is an operator-run release under `docs/runbook.md` § Live-operation approval matrix ("Deploy, restore, fail over, or retire": exact environment, explicit approval, rollback path) and is mirrored in `docs/desktop/11-azure-disposition/README.md`. Azure MCP use is read-only.
- **Scope boundary**: may touch `src/Pegasus.Web/appsettings.json`, `tests/Pegasus.IntegrationTests`, `scripts/Invoke-ProductionSmoke.ps1`, `docs/operations.md`. Must not touch `src/Pegasus.Core`, the desktop projects, `infra/`, or any other credential. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: the password is in git history and cannot be un-published — treat the credential as permanently disclosed and say so in the proof; deleting the block **without** the `Removed` key leaves the account alive on the estate because reconciliation then does nothing (`Program.cs:686-687`, `:1016-1019`); the two-environments rule (ADR-0014) means there is no staging rehearsal — the local stack is the rehearsal.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Simplification pass

Before the PR, independently review the branch diff for reuse, unnecessary abstraction, duplicated policy, and scope expansion; record findings and dispositions here.
