# Checklist — FEAT-027

One box per plan step, in plan order, ending with the verification box that
produces `proof`. Tick with `set_ticket_doc(doc: "checklist")`; append progress
notes below rather than rewriting.

- [ ] Read plan row `DSK-07-01` in `docs/desktop/07-integrations/README.md` § 5, that plan's § 2 and § 4, `endpoint-map.md` § `Triage, Unidentified, Operations` (`:108-116`) with the Conventions header (`:11-27`), and FRD-08 § Inbound mailbox identity; call `get_doc_gates FEAT-027`; `take_ticket` on branch `task/dsk-07-01-intake-status-endpoints` from `origin/dev`
- [ ] Append to `research` the Operations page handler table (Core call per handler, the `LoadedAtUtc` rule at `:41-45`/`:67`, the rendered `RequestOperationProjection` fields) and the `GetRetainedMailFreshness.Evaluate` walk-through; record the SHA read after the latest [[FND-023]] sync
- [ ] Confirm the four read ports by reading them: `GetRequestOperations` (`RequestOperations.cs:72`), `GetEmailOperations` (`EmailOperations.cs:62`), `ListPollHealthAsync` (`RetainedMail.cs:382`) and `ListMailboxesAsync` (`:379`); record the `MailboxId` join in the plan
- [ ] Add `IntakeStatusResponse` + `MailboxIntakeStatus` (with `isPolled`) to `src/Pegasus.Contracts` as plain records with no EF, ASP.NET or Core types
- [ ] Add `ExternalWorkResponse` + its row record (`kind`, `caseReference`, `attemptCount`, `failureCode`, `failureReason`, `canRetry`, `lastActivityAtUtc`, `limitReached`) to `src/Pegasus.Contracts`
- [ ] Add the `queue_poisoned` constant to `src/Pegasus.Contracts` so the literal is spelled once
- [ ] Register `GET /api/v1/operations/intake-status` in [[GWY-002]]'s `/api/v1` operations group behind `Features:DesktopGateway` and the `PerformCasework` filter from [[GWY-003]], with a weak `ETag`
- [ ] Register `GET /api/v1/operations/external-work` in the same group with the same gate, filter and `ETag`
- [ ] Record in the plan whether [[GWY-013]] had already registered the operations group, and extend it rather than creating a second group
- [ ] Set `asOfUtc` from `TimeProvider` **after** the last await, reproducing `Operations/Index.cshtml.cs:67`; make a failed query return a problem, never a body with a fresh timestamp
- [ ] Compute per-mailbox freshness by calling `GetRetainedMailFreshness.Evaluate(new[] { health }, nowUtc)` and render it with the exact strings `current` / `stale` / `unavailable` from `Mail/Index.cshtml.cs:253-258` — no second freshness rule
- [ ] Carry `lastFailureCode` verbatim from Core and keep `RequestOperationState.UnknownExternal` and `EmailOperationState.Unknown` as their own wire values, never folded into success
- [ ] Report the poison count as its own field, derived from the `queue_poisoned` constant, excluding rows completed by `CompletePoisonReplay` (`EfExternalWorkStore.cs:435`, `:468`, `:499`); add no column and no table
- [ ] Add the test asserting the `queue_poisoned` constant equals the store literal at `EfIntakeWorkStore.cs:410`
- [ ] Add contract tests: gate off → 404; unauthenticated → 401; wrong right → 403 `urn:pegasus:problem:not-authorized`
- [ ] Add contract tests for the three freshness cases: healthy → `current`; failure code with future `dueAtUtc` → `unavailable`; never polled → `unavailable` with `isPolled` false
- [ ] Add the contract test asserting no response field carries a mailbox credential, Graph token, connection string or storage key
- [ ] Add the LocalDB integration test seeded with a failed external work item and a failed mailbox poll, following `OperationsWebTests.cs` (`:345`) and `OperationsPersistenceTests.cs`; assert `canRetry` matches the Core projections and `limitReached` surfaces
- [ ] Run `dotnet build ./src/Pegasus.Web/Pegasus.Web.csproj -c Release` and confirm the existing `OperationsWebTests` and `OperationsPersistenceTests` stay green
- [ ] Add the two rows to `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Triage, Unidentified, Operations`
- [ ] Confirm the `DSK` capability family exists (created by [[FND-011]]) and add the intake-status row to `docs/capabilities.md`, naming its canonical owner
- [ ] Regenerate `openapi/pegasus-v1.json` and the Kiota client, and commit the output in this PR so CI's no-op check passes
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document
- [ ] Verification run — `dotnet test` for `Pegasus.Api.ContractTests` and `Pegasus.IntegrationTests` (`Category!=Corpus&Category!=Browser`), plus `git diff --stat origin/dev -- src/Pegasus.Worker` expecting empty output; this box produces `proof`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
