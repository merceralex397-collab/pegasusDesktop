# Plan — GWY-008: DSK-03-08 · Case command endpoints: create, save, lease, completeness, workflow and closure

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Chosen approach

Add the case write surface to `/api/v1`: create, save details, the three edit-lease commands, confirm completeness, the seven workflow transitions and the four closure transitions — each an explicit named command carrying `operationKey`, `expectedVersion` and, where Core requires it, `editLeaseToken` — and prove the edit lease is mutually exclusive across staff and Automation Actors in both directions (upstream KANMER-005).

## Routing and constraints

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.


## Ordered implementation steps

1. Orient. Read every `§ Cases` command row quoted above in `docs/desktop/03-gateway-api-and-data/endpoint-map.md`, plus `README.md` § 3 rows *Idempotency*, *Concurrency*, *Audit & transactions* and § 7, and the upstream KANMER-005 body named under Source of truth. Then `get_doc_gates <this ticket id>` and `take_ticket`.
2. Read `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs`, `Workflow.cshtml.cs`, `Closure.cshtml.cs` and `Create.cshtml.cs` in full and record, per handler, the exact Core command interface and request record it constructs. Every endpoint must construct the same record and call the same interface.
3. Add `src/Pegasus.Contracts/Cases/Commands/` request DTOs, one per command, each carrying `operationKey` and — for case-scoped commands — `expectedVersion`, and `editLeaseToken` where the endpoint-map row says so. Add `reason` where Core requires it (reopen, and every `CaseMutationRequest`-shaped command). Never place these on headers.
4. Add `src/Pegasus.Web/Api/CaseCommandEndpoints.cs` extending the `cases` sub-group from [[DSK-03-07]]. Map `POST /`, `POST /{id}/lease/claim|renew|release`, `PUT /{id}`, `POST /{id}/confirm-completeness`, the seven workflow routes and the four closure routes — each as its own named route. A single dispatcher route taking an action string is a defect (§ 10.2).
5. Build the actor from `HttpContext.Items` (the accessor from [[DSK-03-03]]) and hand it to the Core request record; never let the client supply the actor.
6. Validate `operationKey` at the boundary with the same rule the MCP surface uses (`src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:76`): non-empty, ≤ 100 characters, no whitespace or control characters. The desktop prefix is `desk:` per § 3 row *Idempotency*; reject a key that does not carry it with a `validation` problem.
7. Apply the Engineer-role requirement to `POST /{id}/record-engineer-finding` per the endpoint-map row, using the same right/role check `Cases/Workflow.cshtml.cs` performs — do not invent a new rule in the endpoint.
8. Return the new `version` (and the replacement id for `/linked-replacement`, the lease token/expiry/holder for the lease routes) in the response body, and `201 Created` with the case id and version for `POST /cases`. Let the problem middleware from [[DSK-03-02]] translate `CaseVersionConflictException`, `CaseEditLeaseConflictException`, `CaseEditLeaseExpiredException` and `CaseOperationConflictException` — do not catch them in the handler.
9. Add a per-user rate-limit policy for `/api/v1` writes by extending the existing `AddRateLimiter` configuration in `src/Pegasus.Web/Program.cs:275-327`. Reuse that mechanism; a second limiter is the § 7 trap.
10. Add `tests/Pegasus.IntegrationTests/DesktopGatewayCaseCommandTests.cs` with the seven-case matrix for **every** command in this ticket: authorized success, unauthorized (wrong role), version conflict returning `409` with `currentVersion`, lease conflict, lease expired, operation-key replay returning the same result, and a validation failure producing the `validation` problem shape. Mirror the scenarios in `tests/Pegasus.IntegrationTests/CaseWorkflowPersistenceTests.cs`.
11. Add one fact proving lease replay semantics precisely: claiming twice with the same `operationKey` returns the identical token and expiry (`ILeaseCaseForEdit` remarks at `CaseWorkflowContracts.cs:330-338`), while the same key with different material returns the `operation-conflict` problem.
12. **Add two facts proving the lease is mutually exclusive across actor kinds (upstream KANMER-005)**, in the same test file. Fact one — Automation holds, staff competes: with an Automation Actor holding an unexpired lease, a staff `POST /cases/{id}/lease/claim` returns the `lease-conflict` problem, the retained holder is unchanged (`CaseWorkflowEntity.EditLeaseHolder` still the Automation subject id and `EditLeaseExpiresAtUtc` unmoved), a staff `PUT /cases/{id}` is refused at the write boundary, and the Automation Actor can still save and then release afterwards. Fact two — the mirror: staff holds, an Automation Actor competes, with the same four assertions reversed. Assert the holder through the projection the endpoints return (`CaseEditAuthorityHolder`, `IsAutomation` true for the Automation case) as well as through the retained row, so a holder that changes cannot pass by presenting the same display name. If either fact fails, the defect is live and the fix is in `EfCaseWorkflowStore.ClaimAsync` (`:114-198`) and `src/Pegasus.Core/Workflow/CaseEditAuthority.cs` — stop, record the failing fact in the ticket plan, and obtain the explicit scope exception named in Guardrails before making it. If both pass, record in the plan document that upstream KANMER-005 is closed by the existing CASE-27 edit-authority work, with these two facts as the evidence. Either way, publish the outcome for [[DSK-05-08]], whose lease-lost path is built to it and which blocks on this ticket rather than working around a failure.
13. Regenerate and commit the OpenAPI snapshot and the client, run `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayCaseCommandTests"`, then run the simplification pass and record it under a dated `## Simplification pass` heading in the ticket plan.

## Acceptance conditions

- [ ] Every command is an explicit named route; no generic action endpoint exists.
- [ ] Every command carries `operationKey`, and case-scoped commands carry `expectedVersion` (and `editLeaseToken` where Core requires it) as body fields.
- [ ] Every command has all seven test cases: success, unauthorized, version conflict, lease conflict, lease expired, replay, validation.
- [ ] A version conflict returns `409` with the current server version in the problem body.
- [ ] An exact lease claim or renewal replay returns the same token and expiry.
- [ ] `/record-engineer-finding` requires the Engineer role; the rule matches the Razor handler.
- [ ] **A competing claim never replaces an unexpired lease holder, in either actor direction** — Automation holding against a staff claimant and staff holding against an Automation claimant — and the holder can still save and release after the rejected claim (upstream KANMER-005).

## Verification

- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayCaseCommandTests"` — expected: all facts pass; the count is at least seven per command, plus the two cross-actor lease facts.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~CaseWorkflowWebTests"` — expected: the existing Razor tests still pass unchanged.

## Risks and boundaries

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Cases/**`, the rate-limiter configuration in `Program.cs`, `openapi/`, the generated client and the test projects. Must not touch `src/Pegasus.Core/Lifecycle/**` or any Razor page model — the business rules already exist and stay where they are. **Named conditional exception for upstream KANMER-005**: if and only if a step 12 fact fails, this ticket may change `src/Pegasus.Core/Workflow/CaseEditAuthority.cs` and `src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs` `ClaimAsync` to enforce the exclusion — recorded in the ticket plan with the failing fact quoted, and reviewed as a Core change. It is not a licence to refactor the lease code when the facts pass. [[DSK-05-08]] holds no part of this exception and may not make the fix.
- **Traps**: two policy engines — any rule that appears in an endpoint filter and not in Core is a defect. Do not reproduce `Pages/Cases/CaseMutationPageModel.cs`'s TempData proposed-values/lease chaining; the desktop sends explicit fields. Reuse the existing rate limiter rather than adding a second mechanism. This row covers eighteen routes and is the largest in the epic — it is deliberately not split, but sequence the work by group (lease → save/completeness → workflow → closure → create) and keep the checklist per group. **Cross-actor lease exclusion is proved here, not assumed, and this ticket is its single owner** — [[DSK-05-08]] restates the two step 12 facts verbatim under its own Source of truth and renders their outcome, asserting nothing itself; the two facts in step 12 are the evidence both tickets point at, and a lease matrix written with two staff actors only does not close upstream KANMER-005 because the failure was an Automation holder. [[DSK-05-08]]'s earlier "confirm it is implemented on the endpoint" wording was withdrawn precisely because it pointed at nothing named — do not reintroduce that shape anywhere. **Upstream ids and fork board ids do not match**: upstream KANMER-005 has no fork ticket at all, so never write it as a board wiki-link.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.
