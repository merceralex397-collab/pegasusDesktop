# Research — GWY-008: DSK-03-08 · Case command endpoints: create, save, lease, completeness, workflow and closure

## Question

Add the case write surface to `/api/v1`: create, save details, the three edit-lease commands, confirm completeness, the seven workflow transitions and the four closure transitions — each an explicit named command carrying `operationKey`, `expectedVersion` and, where Core requires it, `editLeaseToken` — and prove the edit lease is mutually exclusive across staff and Automation Actors in both directions (upstream KANMER-005).

## Evidence examined

- Plan row: `docs/desktop/03-gateway-api-and-data/README.md` § 5 — `DSK-03-08`
- Plan detail: same file § 3 — rows *Idempotency*, *Concurrency*, *Problem details*, *Audit & transactions*; § 4 exit-gate bullet 3 (the seven-case matrix)
- Plan detail: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Cases
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 10.2 API style, § 10.4 Concurrency, § 10.5 Transactions and audit
- Upstream carry-over: **upstream KANMER-005** *Enforce exclusive editing leases between staff and Automation Actors* (`fix`, labels `bug`, `lease`, `concurrency`) — **absorbed here; it was not imported, so there is no fork ticket for it and it must never be written as a board wiki-link.** Its approach is four lines — enforce ownership atomically at claim and write boundaries for every actor type; reject a competing claim while an unexpired lease exists **without replacing its owner**; only the current holder may edit, renew or release; cover human-holds/AI-competes and AI-holds/human-competes. Its four verification lines are the facts step 12 adds. **This ticket is the single owner and the only place any of them is asserted.** [[DSK-05-08]] consumes the outcome and asserts nothing of its own: it restates the two step 12 facts verbatim under its own Source of truth, renders their result in the lease-lost path, and blocks on this ticket if either fails. Its trap formerly read "confirm it is implemented on the endpoint before claiming parity" — a verification obligation pointing at nothing named — and that wording has been withdrawn in favour of the two facts below.
- Endpoint contracts quoted from `endpoint-map.md` § Cases:
  - `POST /cases` — replaces `Cases/Create` `OnPostCreateAsync` (689 lines); Core create use case via the `IAllocateIntake`/acceptance path; `PerformCasework`; idempotent `yes (key)`; token `operationKey`; returns `201` + case id + version; phase 4.
  - `POST /cases/{id}/lease/claim`, `…/renew`, `…/release` — replaces `Cases/Details` `OnPostClaimLeaseAsync`, `OnPostRenewLeaseAsync`, `OnPostReleaseLeaseAsync`; Core `IAcquireCaseEditLease`, `IRenewCaseEditLease`, `IReleaseCaseEditLease`; `PerformCasework`; `yes (key; replay returns same token/expiry)`; tokens `expectedVersion`, `operationKey` (plus `editLeaseToken` for renew/release); returns lease token, expiry, holder; phase 4.
  - `PUT /cases/{id}` (save details) — replaces `Cases/Details` `OnPostSaveAsync`; `ICaseDataStore` save use case; `PerformCasework`; `yes (key)`; tokens `expectedVersion`, `editLeaseToken`, `operationKey`; returns the new version; phase 4.
  - `POST /cases/{id}/confirm-completeness` — replaces `Cases/Details` `OnPostConfirmCompletenessAsync`; completeness command; `PerformCasework`; `yes (key)`; same three tokens; returns the new version; phase 4.
  - `POST /cases/{id}/hold`, `/release-hold`, `/return-to-review`, `/assign-engineer`, `/start-work`, `/record-engineer-finding`, `/linked-replacement` — replaces the seven `Cases/Workflow` handlers (227 lines); Core `src/Pegasus.Core/Lifecycle/CaseLifecycle.cs`, `CaseCommandSeams.cs`; `PerformCasework` (engineer finding: Engineer role); `yes (key)`; `CaseMutationRequest` fields; returns the new version (+ replacement id); phase 4.
  - `POST /cases/{id}/report-approval`, `/close`, `/reopen`, `/archive` — replaces the four `Cases/Closure` handlers (121 lines); lifecycle commands; `PerformCasework`; `yes (key)`; `CaseMutationRequest` fields, reopen requires `reason`; returns the new version; phase 4.
- Repository evidence:
  - `src/Pegasus.Web/Pages/Cases/Workflow.cshtml.cs` — handlers `OnPostHoldAsync`, `OnPostReleaseHoldAsync`, `OnPostReturnToReviewAsync`, `OnPostAssignEngineerAsync`, `OnPostStartWorkAsync`, `OnPostRecordEngineerFindingAsync`, `OnPostCreateLinkedReplacementAsync`
  - `src/Pegasus.Web/Pages/Cases/Closure.cshtml.cs` — handlers `OnPostRecordReportApprovalAsync`, `OnPostCloseAsync`, `OnPostReopenAsync`, `OnPostArchiveAsync`
  - `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` — handlers `OnPostClaimLeaseAsync`, `OnPostRenewLeaseAsync`, `OnPostReleaseLeaseAsync`, `OnPostSaveAsync`, `OnPostConfirmCompletenessAsync`
  - `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182` — `CaseMutationRequest(CaseId, ExpectedVersion, Actor, OperationKey, Reason, EditLeaseToken)`
  - `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:159-179` — `ClaimCaseEditLeaseRequest`, `RenewCaseEditLeaseRequest`, `ReleaseCaseEditLeaseRequest`
  - `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:330-338` — `ILeaseCaseForEdit` replay semantics: an exact claim or renewal replay returns the same token and expiry; reusing a key with different material throws `CaseOperationConflictException`
  - `src/Pegasus.Core/Workflow/CaseCommandContracts.cs:77-95` — `IAcquireCaseEditLease`, `IRenewCaseEditLease`, `IReleaseCaseEditLease`
  - `src/Pegasus.Core/Workflow/CaseEditAuthority.cs:18` — `LeaseTokenLength = 64` hexadecimal characters, fixed-time comparison
  - `src/Pegasus.Core/Workflow/CaseEditAuthority.cs:42-66` — `RequireLease`: a mutation whose actor is not the retained holder throws `CaseEditLeaseConflictException`; there is "no takeover, force, or bypass"
  - `src/Pegasus.Core/Workflow/CaseEditAuthority.cs:75-81` — `CaseEditAuthorityHolder` and `CaseEditAuthorityHolder.Automation`: the Automation Actor is disclosed as itself and is a first-class holder, which is why the KANMER-005 facts must be written with an Automation holder and not only with two staff actors
  - `src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs:114-198` — `ClaimAsync` runs `IsolationLevel.Serializable` with `AcquireWorkflowMutationLockAsync`, and at `:165-168` throws `CaseEditLeaseConflictException` when `CaseEditAuthority.IsHeld` is true, without clearing the retained holder. This reads as though KANMER-005 is already closed by the CASE-27 edit-authority work — but nothing on the board proves it with an Automation Actor as the holder, which is precisely the case that failed, so step 12 proves it in both directions rather than assuming it.
  - `src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs:252`, `:326`, `:693`, `:797`, `:1296-1297` — the `RequireLease` write boundaries the second half of each fact exercises
  - `tests/Pegasus.IntegrationTests/CaseWorkflowPersistenceTests.cs`, `CaseWorkflowWebTests.cs`, `CaseClosureWebTests.cs`, `CaseEditModeWebTests.cs` — the scenarios the new tests mirror
- Binding decisions:
  - L-01 — commands are route handlers in the existing `Pegasus.Web`, calling the same Core commands the pages call.
  - L-02 — conflict and replay evidence comes from the local LocalDB stack.
- Depends on: `DSK-03-07` for the case group, contracts and read shapes the commands return versions into. `DSK-05-08` is the consumer of step 12's two facts and builds its conflict-recovery pattern to their outcome.

## Scope and constraints

Proposal § 10.2 forbids a generic "execute action" endpoint and requires explicit, auditable workflow commands; § 10.4 requires optimistic concurrency with the current server version returned on conflict; § 10.5 fixes the server transaction order. Operator-visible consequence: two operators editing the same case cannot silently overwrite each other — the second gets the current version back and can reload, compare and reapply. This is the ticket the Phase 4 case-editing slice depends on, and it sets the seven-case test matrix every later command ticket copies. Upstream KANMER-005 records the one hole that matrix does not close on its own: an Automation Actor held the editing lease, a staff user entered edit mode and took it, the actor's edits still succeeded, and the actor's release was then rejected because staff had become the recorded holder — concurrent writes with lease ownership inconsistent with the actor doing the editing. The desktop inherits whichever behaviour the server has, so this ticket proves it rather than assuming it.

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Cases/**`, the rate-limiter configuration in `Program.cs`, `openapi/`, the generated client and the test projects. Must not touch `src/Pegasus.Core/Lifecycle/**` or any Razor page model — the business rules already exist and stay where they are. **Named conditional exception for upstream KANMER-005**: if and only if a step 12 fact fails, this ticket may change `src/Pegasus.Core/Workflow/CaseEditAuthority.cs` and `src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs` `ClaimAsync` to enforce the exclusion — recorded in the ticket plan with the failing fact quoted, and reviewed as a Core change. It is not a licence to refactor the lease code when the facts pass. [[DSK-05-08]] holds no part of this exception and may not make the fix.
- **Traps**: two policy engines — any rule that appears in an endpoint filter and not in Core is a defect. Do not reproduce `Pages/Cases/CaseMutationPageModel.cs`'s TempData proposed-values/lease chaining; the desktop sends explicit fields. Reuse the existing rate limiter rather than adding a second mechanism. This row covers eighteen routes and is the largest in the epic — it is deliberately not split, but sequence the work by group (lease → save/completeness → workflow → closure → create) and keep the checklist per group. **Cross-actor lease exclusion is proved here, not assumed, and this ticket is its single owner** — [[DSK-05-08]] restates the two step 12 facts verbatim under its own Source of truth and renders their outcome, asserting nothing itself; the two facts in step 12 are the evidence both tickets point at, and a lease matrix written with two staff actors only does not close upstream KANMER-005 because the failure was an Automation holder. [[DSK-05-08]]'s earlier "confirm it is implemented on the endpoint" wording was withdrawn precisely because it pointed at nothing named — do not reintroduce that shape anywhere. **Upstream ids and fork board ids do not match**: upstream KANMER-005 has no fork ticket at all, so never write it as a board wiki-link.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
