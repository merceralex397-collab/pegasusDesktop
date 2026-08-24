# Checklist — FEAT-037

One box per plan step, in plan order. Tick with `set_ticket_doc`; append progress notes below
rather than rewriting.

- [ ] Read the plan row `DSK-07-11` in `docs/desktop/07-integrations/README.md` § 5, that area's outbound-mail evidence paragraph and its scope-creep trap row, FRD-11 in full, and FRD-08 `:120-135`
- [ ] Call `get_doc_gates FEAT-037` and `take_ticket` on branch `task/dsk-07-11-outbound-command-seam` with a worktree at `../pegasus-worktrees/dsk-07-11-outbound-command-seam` from `origin/dev`
- [ ] Append a dated "Boundary" heading to the plan document recording that this ticket builds the seam, the vocabulary and the classification field only — naming upstream MAIL-12/13/17/19 and upstream CASE-002 (not imported) as the excluded work
- [ ] Read `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:583-660` and the Core send and reconcile use cases, and append the required field list, the `IsOperationKeyValid` contract (`:738`) and reconcile's unknown-result behaviour to the `files` document
- [ ] Record in `research` whether assumptions `A-07-11-1` (no new Core overload needed) and `A-07-11-3` (`Pending` = queued-but-unconfirmed) held
- [ ] Add `OutboundOperationState` to `src/Pegasus.Contracts` with the five wire values and an XML-documented map from `EmailOperationState`, stating that `draft` is client-only and is never returned by the gateway
- [ ] Implement `POST /api/v1/cases/{caseId}/assessment/send` over the existing send use case, validating `operationKey` first, taking `expectedVersion` and `editLeaseToken`, and returning the state plus the provider message identifier where known
- [ ] Apply the `PerformCasework` per-group `StaffAccessRight` filter from [[GWY-003]] to the send endpoint, with no client-supplied authorisation input
- [ ] Make replay of the same `operationKey` return the original result with no second provider effect, reusing the existing operation-key mechanics and adding no idempotency table
- [ ] Return `unknown` with the reconcile path named for a command whose provider outcome is not yet known, never an optimistic `sent`
- [ ] Confirm the request contract has no field by which a client can assert a send, and add the test that a client-supplied "sent" claim is refused
- [ ] Widen the `IRetainedMailQueries` case-scoped projection to join the existing classification row (no new table); stop and raise if a new table appears necessary
- [ ] Implement `GET /api/v1/cases/{caseId}/communications` returning direction, the five states, discovery/link/sent times, the correlating actor and each linked e-mail's `MailOperationalDestination` and `MailCategory?`
- [ ] Confirm no communications response carries `PolicyKey` or `PolicyVersion`
- [ ] Write the nine contract facts in `tests/Pegasus.Api.ContractTests`: success; replay; missing/malformed key → `validation`; unauthorised → `not-authorized`; stale `expectedVersion` → `version-conflict`; client-asserted send refused; `unknown` distinct from `sent`; a `Queries`-classified e-mail distinguishable; no policy key or version on any response
- [ ] Write the integration test following `tests/Pegasus.IntegrationTests/SentEvidencePollPersistenceTests.cs`: `unknown` before the evidence poll, `sent` with the provider identifier audited after it
- [ ] Update `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — the communications row (`:52`) with the classification field and the send row (`:79`) with the returned state
- [ ] Add the outbound command seam clause to `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md` as behaviour, not mechanism
- [ ] Add the classification sentence to `docs/desktop/06-ui-design/screen-specs.md` § `§13.8 Communications` (`:362-369`) for [[DUI-013]]
- [ ] Regenerate `openapi/pegasus-v1.json` and the Kiota client per [[GWY-004]] and [[GWY-005]], and confirm `git diff --exit-code openapi/pegasus-v1.json` is clean afterwards
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document
- [ ] Verification run — `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release`, `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"`, and `git diff --stat origin/dev -- src/Pegasus.Worker` (expected empty) — captured as `proof` at tier 5
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
