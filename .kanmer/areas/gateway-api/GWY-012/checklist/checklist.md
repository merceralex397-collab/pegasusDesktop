# Checklist — GWY-012: DSK-03-12 · Mail workspace endpoints: list, preview, message detail, case link/unlink, classify, folder move

- [ ] Orient. Read every row quoted above in `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Mail workspace, plus `docs/desktop/07-integrations/README.md` § 5 row `DSK-07-03`, which owns the provider-unavailable behaviour this ticket surfaces. Then `get_doc_gates <this ticket id>` and `take_ticket`.
- [ ] Read `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs` and `Mail/Message.cshtml.cs` in full. Record, per handler, the Core use case, every version it carries, and the exact operator sentences it produces — including the unlink warning "Unlinking this email cancels case &lt;ref&gt;" — so the API returns the same words, not a paraphrase.
- [ ] Add `src/Pegasus.Contracts/Mail/` DTOs for the list item, freshness, preview, message detail and each command. The message detail carries the thread, attachments, classification, queue, outcome, association, move result and suggested move named in the endpoint-map row, each with its own version where Core has one.
- [ ] Add `src/Pegasus.Web/Api/MailEndpoints.cs` mapping a `mail` sub-group with `.RequireStaffRight(StaffAccessRight.PerformCasework)`; map the list, refresh, preview, detail, four link/unlink routes, classification and move — ten named routes.
- [ ] Bind the list parameters to the Core queries exactly: `mailbox`, `folder`, `q`, `deleted` and paging. Honour the `SearchDeletedMail` cap of 100 and its GET-only Graph constraint; do not widen either.
- [ ] Keep the preview inert: return the same `MailBodyPresentation` output the Razor JSON handler returns — plain text, no HTML passthrough, no remote content. This is a security property, not a formatting choice.
- [ ] Implement `POST /mail/refresh` as a coalesced operation: concurrent refreshes for the same mailbox return the same freshness rather than issuing parallel provider calls, matching the manual-refresh behaviour of the page.
- [ ] Implement the prepare/commit pair for link and unlink as two distinct routes so the desktop can show the consequence before committing. The prepare route performs no mutation; the commit route carries the message/receipt versions plus the case `expectedVersion` and `editLeaseToken`.
- [ ] Make `POST /mail/{id}/move-to-recommended-folder` absent — not failing — when the provider port reports unavailable: return `404` from a route that is conditionally mapped, or a `provider-unavailable` problem where the endpoint must exist for discovery. Decide which, record the decision and its reason in the ticket plan, and make the desktop-visible behaviour match `Pages/Mail/Message.cshtml.cs` exactly.
- [ ] Take every operator-facing label from `src/Pegasus.Web/Presentation/OperatorLabels.cs` (or its relocated form after [[DSK-03-16]]). A second classification-label map in the API is a duplication defect under `AGENTS.md` § Simplicity rails.
- [ ] Add `tests/Pegasus.IntegrationTests/DesktopGatewayMailTests.cs` reusing the fixtures in `MailWorkspaceWebTests.cs`: list paging and newest-first, freshness equality with the page, preview inertness, detail versions, the seven-case matrix for each of the six commands, the unlink warning text, and the provider-unavailable case producing the decided behaviour.
- [ ] Regenerate and commit the OpenAPI snapshot and the generated client, run `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayMailTests"`, then run the simplification pass and record it under a dated `## Simplification pass` heading in the ticket plan.
- [ ] Every endpoint calls the same Core use case the matching `Pages/Mail/*` handler calls, with the same versions and leases.
- [ ] The list is newest first with freshness matching the web page for the same data.
- [ ] The preview is inert text with no HTML or remote content.
- [ ] Prepare routes mutate nothing; commit routes carry message/receipt and case versions plus the lease token.
- [ ] The move command is absent when the provider is unavailable, matching the page's behaviour.
- [ ] Operator sentences, including the unlink warning, are byte-identical to the web app's.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayMailTests"` — expected: all facts pass, including the provider-unavailable fact and the unlink warning text.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~MailWorkspaceWebTests"` — expected: the existing mail web tests still pass unchanged.

## Progress notes

No implementation has started. This checklist is derived from the ticket’s accepted scope and is maintained by the ticket implementer.
