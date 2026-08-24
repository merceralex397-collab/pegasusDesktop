# Checklist — FEAT-029

One box per plan step, in plan order. The last box produces `proof`.

- [ ] Read the plan row `DSK-07-03` (`docs/desktop/07-integrations/README.md` § 5), `endpoint-map.md:11-27` and `:96-107`, the Inbox screen spec (`docs/desktop/06-ui-design/screen-specs.md:248-269`) and FRD-08; call `get_doc_gates FEAT-029`; `take_ticket` on branch `task/dsk-07-03-mail-endpoints` from a worktree cut off `origin/dev`
- [ ] Call `get_item GWY-012` and `get_doc_gates GWY-012`, decide whether this ticket extends or creates the `/api/v1` mail route group, and record the answer in the `plan` document under a dated note — before writing any code
- [ ] Re-read `Pages/Mail/Index.cshtml.cs` and `Pages/Mail/Message.cshtml.cs` after the latest upstream sync ([[FND-023]], plan handle `DSK-01-10`), append the completed handler table to `research` with the operator sentences verbatim, and record the SHA read
- [ ] Implement `GET /api/v1/mail` over `ListRetainedMail` + `ListMailboxesAsync` + `GetRetainedMailFreshness`, newest first, with `version` and a weak `ETag`, plain `page`, `pageSize` capped at **100**, and a `validation` refusal when both a destination and a detailed classification are supplied
- [ ] Implement `GET /api/v1/mail/{id}/preview` over `GetRetainedMail`, projecting the same nine inert fields as `Index.cshtml.cs:174-190`
- [ ] Implement `GET /api/v1/mail/{id}` over `GetRetainedMail.ExecuteAsync(actor, id, searchTerm, …)`, passing the search term through so match context survives
- [ ] Implement `GET /api/v1/mail/deleted` over `SearchDeletedMail` with its own mailbox list, capped at the 100 newest, GET-only, nothing retained or backfilled — carrying `state` and `isTruncated` on the response
- [ ] Implement `POST /api/v1/mail/{id}/link-case/prepare` and `.../unlink-case/prepare` over `IGetCase` + `IAcquireCaseEditLease` with the four preconditions each, returning the lease token that joins prepare to confirm
- [ ] Implement `POST /api/v1/mail/{id}/link-case` and `.../unlink-case` over `ILinkIntake` / `IReverseIntakeLink`, requiring the prepared tuple, both versions, the lease token, the operation key and a lower-case `reason` before Core is touched
- [ ] Implement `POST /api/v1/mail/{id}/classification` over `CorrectRetainedMailClassification`, parsing the option key through `MailClassificationSelection.TryParse`, carrying `operationKey` to the audit ledger rather than to Core, and mapping `MailClassificationConcurrencyException` → `version-conflict`
- [ ] Implement `POST /api/v1/mail/{id}/move-to-recommended-folder` over `MoveRetainedMailFolder` with the three `int` versions, the policy key, a 1–500 character reason and a **bare GUID** operation key, returning `Succeeded` / `Failed` / `Uncertain` distinctly with their approved sentences
- [ ] Register all nine endpoints in the single `/api/v1` mail group behind `Features:DesktopGateway` ([[GWY-002]], plan handle `DSK-03-02`) and the `PerformCasework` filter ([[GWY-003]], plan handle `DSK-03-03`) — no second group
- [ ] Project `RetainedMailFolderRecommendation` whole into the detail DTO (`folderType`, `policyKey`, `policyVersion`, `reason`, `mailboxVersion`, `canMove`) plus `suggestedMove` and `latestFolderMove` — resolving no `IRetainedMailFolderMover` in Web and collapsing none of the five unavailability reasons
- [ ] Add contract tests for paging, freshness, the `pageSize > 100` refusal, preview inertness, prepare-then-confirm for link and unlink, classification correction and its `version-conflict`, and move with reason
- [ ] Add the move operation-key facts: a `desk:`-prefixed key rejected as `validation`, a bare GUID accepted
- [ ] Assert **both** folder-mover compositions explicitly — one with a supplied mover (move affordance present), one on the `UnavailableRetainedMailFolderMover` default (affordance absent) — relying on no default
- [ ] Add gate-off 404, 401 and 403 `not-authorized` facts for every mail route, with `Features:DesktopGateway` enabled explicitly in the positive tests
- [ ] Add the parity facts in `tests/Pegasus.IntegrationTests` proving the endpoint and the Razor handler produce the same Core effect for the same input — same versions consumed, same association written, same classification dossier appended
- [ ] Review every DTO field against `RetainedMailSummary`, `RetainedMailDetail` and `DeletedMailSearchItem` and add the contract fact that no response carries a Graph token, mailbox secret, connection string or raw provider JSON
- [ ] Correct the `endpoint-map.md` § `Mail workspace` rows: `pageSize` cap 100, the classification row's version-based idempotency, the detail row's folder-recommendation returns, and the new deleted-search row
- [ ] Add the desktop behaviour clause to `docs/frd/frd-08-email-mailbox-and-background-processing.md` — behaviour, not mechanism
- [ ] Regenerate `openapi/pegasus-v1.json` and the Kiota client via `eng/api/Generate-ApiClient.ps1` and commit the result in this PR
- [ ] Run the simplification pass over this branch's own diff and record it under a dated `## Simplification pass` heading in the `plan` document
- [ ] Run the verification suite and capture its output as `proof`: `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release`, `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~MailWorkspaceWebTests"`, `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"`, and `git diff --stat origin/dev -- src/Pegasus.Worker src/Pegasus.Infrastructure/Email src/Pegasus.Web/Pages/Mail` (expected: empty)

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
