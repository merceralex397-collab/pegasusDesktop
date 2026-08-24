# Research — GWY-012: DSK-03-12 · Mail workspace endpoints: list, preview, message detail, case link/unlink, classify, folder move

## Question

Project the mail workspace onto `/api/v1`: the paged mailbox list with freshness, the inert body preview, the full message detail, the prepare/commit link and unlink case commands, classification correction, and the move-to-recommended-folder command that disappears when the provider is unavailable.

## Evidence examined

- Plan row: `docs/desktop/03-gateway-api-and-data/README.md` § 5 — `DSK-03-12`
- Plan detail: same file § 3 — rows *Paging/filter/sort*, *Idempotency*, *Concurrency*, *Problem details*
- Plan detail: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Mail workspace
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.1 Microsoft Graph intake, § 13.8 Communications, § 16.2 External provider resilience
- Endpoint contracts quoted from `endpoint-map.md` § Mail workspace:
  - `GET /mail?mailbox&folder&page&pageSize&q&deleted` — replaces `Mail/Index` `OnGetAsync` (428 lines); Core `ListRetainedMail`, `GetRetainedMailFreshness`, `SearchDeletedMail` (cap 100, GET-only Graph) (`src/Pegasus.Core/Intake/RetainedMail.cs`); `PerformCasework`; GET; `ETag`; returns paged messages newest first plus freshness; phase 5.
  - `POST /mail/refresh` — replaces the `Mail/Index` manual refresh; freshness refresh use case; `PerformCasework`; idempotent `yes (coalesced)`; no concurrency token; returns freshness; phase 5.
  - `GET /mail/{id}/preview` — replaces `Mail/Index` `OnGetPreviewAsync` (`JsonResult`, `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs:176`); retained body preview (`MailBodyPresentation`); `PerformCasework`; GET; `ETag`; returns an inert text preview; phase 5.
  - `GET /mail/{id}` — replaces `Mail/Message` `OnGetAsync` (1,025 lines); Core `GetRetainedMail` (thread, attachments, classification, queue, outcome, association, move result, suggested move); `PerformCasework`; GET; `ETag` + versions; returns message detail; phase 5.
  - `POST /mail/{id}/link-case/prepare`, `POST /mail/{id}/link-case`, `POST /mail/{id}/unlink-case/prepare`, `POST /mail/{id}/unlink-case` — replaces `Mail/Message` `OnPostPrepareLinkCaseAsync`, `OnPostLinkCaseAsync`, `OnPostPrepareUnlinkCaseAsync`, `OnPostUnlinkCaseAsync`; case search/detail queries + `IAcquireCaseEditLease` + `ILinkIntake` / `IReverseIntakeLink`; `PerformCasework`; `yes (key)`; tokens message/receipt versions, case `expectedVersion` + `editLeaseToken`; returns versions, and unlink warns "Unlinking this email cancels case &lt;ref&gt;"; phase 5.
  - `POST /mail/{id}/classification` — replaces `OnPostCorrectClassificationAsync`; classification correction command; `PerformCasework`; `yes (key)`; tokens classification version + `operationKey`; returns version; phase 5.
  - `POST /mail/{id}/move-to-recommended-folder` — replaces `OnPostMoveToRecommendedFolderAsync`; folder-move command (provider port; absent when the provider is unavailable); `PerformCasework`; `yes (key)`; tokens classification/recommendation/mailbox versions, `operationKey`, `reason`; returns the move record; phase 5.
- Repository evidence:
  - `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs` — `OnGetAsync` and `OnGetPreviewAsync`, the only JSON handler in the Razor surface today
  - `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs` — the seven handlers this ticket projects
  - `tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs`, `RetainedMailPersistenceTests.cs`, `MailClassificationLabelTests.cs`, `MailFailureSentenceTests.cs` — the scenarios and the operator sentences to preserve
  - `src/Pegasus.Web/Presentation/OperatorLabels.cs` — the single code → operator-vocabulary map; mail classification labels come from it and must not be re-derived in the API (see [[DSK-03-16]])
- Binding decisions:
  - L-01 — endpoints evolve inside `Pegasus.Web`; Graph credentials never leave it.
  - L-02 — replay adapters stand in for Graph in the local stack; there is no Azure test environment.
- Depends on: `DSK-03-03` for the right filter and actor resolution.

## Scope and constraints

Proposal § 13.8 makes communications a primary workflow and § 12.1 keeps Graph itself behind the gateway — no desktop holds Graph credentials. Operator-visible consequence: an operator works retained mail natively, sees the same freshness stamp the web shows, and when the mail provider is down the move control is simply absent rather than failing on click. `Mail/Message.cshtml.cs` is 1,025 lines and `Mail/Index.cshtml.cs` 428, so this is the largest single-page projection in the epic.

- Future owner: `pegasus-gateway-dev`; tests: `pegasus-test-engineer`; independent review: `pegasus-desktop-reviewer`.
- Use `dotnet-webapi`, `optimizing-ef-core-queries` where the ticket changes a query, and `run-tests` for the actual runner profile. The project decision overrides generic “service per endpoint” advice: route handlers translate to existing `Pegasus.Core` ports; no second policy/service layer is introduced.
- The shared EPIC context binds this to versioned `/api/v1` route groups in the existing `Pegasus.Web`, the existing rate-limiter mechanism, an OpenAPI snapshot, and no Azure write.

- Microsoft Learn (fetched 2026-08-24): [ASP.NET Core OpenAPI support](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0) confirms first-party OpenAPI generation. Use the repository’s planned committed snapshot and contract-test flow rather than adding a parallel API documentation path.

- **Azure**: no write. Graph is reached through the existing adapters; a replay adapter stands in locally (L-02).
- **Scope boundary**: may touch `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Mail/**`, `openapi/`, the generated client and the test projects. Must not touch `src/Pegasus.Core/Intake/RetainedMail.cs`, the Graph adapters in `src/Pegasus.Infrastructure`, or `src/Pegasus.Web/Pages/Mail/**`.
- **Traps**: **upstream drift** — upstream `main` is 32 commits ahead with MAIL-011/012 among them; start this ticket only after the first upstream sync (`DSK-00-02`) or you will project code that has since changed. One vocabulary list: labels come from `OperatorLabels`, never a second map. Two policy engines: the provider-availability rule stays in Core/the provider port. Operator copy is governed by `docs/design/README.md` — a sentence that explains rather than states is a defect.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
