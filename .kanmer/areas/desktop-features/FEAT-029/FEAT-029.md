---
id: FEAT-029
type: ticket
title: >-
  DSK-07-03 · Mail endpoints reuse: list, preview, detail, link/unlink,
  classify, move-to-recommended
status: review
area: desktop-features
assignee: codex-mcp-client
profile: feature
stageEntered:
  preparing: '2026-08-24T21:31:40.214Z'
  review: '2026-08-30T06:22:22.899Z'
taken_at: '2026-08-30T04:05:52.485Z'
branch: task/dsk-07-03-mail-endpoints
worktree: 'C:\Users\PC\Documents\GitHub\pegasus-worktrees\dsk-07-03-mail-endpoints'
labels:
  - desktop-conversion
  - plan-07
  - phase-5
  - tier-5
groups:
  - EPIC-008
  - HZN-006
links: []
refs:
  - docs/frd/frd-08-email-mailbox-and-background-processing.md
docs_todo: true
commits:
  - e12ad914
prs:
  - '55'
archived: false
created: '2026-08-24T08:18:48.639Z'
updated: '2026-08-30T06:22:58.733Z'
---

## What

Project the mail workspace onto `/api/v1` with the **same Core owners** the Razor pages call today: list with freshness, inert body preview, message detail, prepare/confirm link and unlink to a case, classification correction, and move-to-recommended-folder — with the move control absent whenever the folder-move provider port is not composed.

## Why

Proposal § 13.8 Communications requires source e-mails, attachments, history and an explicit draft/queued/sent/failed distinction on the native client; § 12.1 keeps the Graph side central. The behaviour lives today in `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs` (list + `OnGetPreviewAsync`) and `Pages/Mail/Message.cshtml.cs` (detail plus six `OnPost*` handlers). `docs/current-architecture.md:104` records two constraints the desktop must not break: Deleted-Items search is a **GET-only** Graph read composed in the Web host, and the folder mover is **unavailable by default with no production writer**. Siblings: [[DSK-05-10]] is the desktop screen; [[DSK-03-12]] owns the same route group from the gateway plan — this ticket is the integration half and the two must land as one contract, not two.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-03`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Mail workspace` (all eight rows, with their Core ports and concurrency tokens)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.4 Intake` → `Inbox — replaces Pages/Mail/Index.cshtml.cs (list, preview) and Pages/Mail/Message.cshtml.cs (detail)`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.1 Microsoft Graph intake, § 13.8 Communications, § 16.2 External provider resilience
- Repository evidence: `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs:69` (`OnGetAsync`), `:158` (`OnGetPreviewAsync`); `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:157` (`OnGetAsync`), `:199`, `:260`, `:318`, `:383` (prepare/confirm link and unlink), `:448` (`OnPostCorrectClassificationAsync`), `:511` (`OnPostMoveToRecommendedFolderAsync`); `src/Pegasus.Core/Intake/RetainedMail.cs:386` (`ListRetainedMail`), `:480` (`GetRetainedMail`), `:641` (`GetRetainedMailFreshness`); `src/Pegasus.Core/Intake/DeletedMailSearch.cs:54` (`SearchDeletedMail`, 100-message cap); `src/Pegasus.Core/Intake/RetainedMailFolderMove.cs:41-76` (`IRetainedMailFolderMover`, `EmptyRetainedMailFolderMoveStore`); `docs/current-architecture.md:104`; `tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs`
- Binding decisions: L-01 — the endpoints live in `Pegasus.Web`. ADR-0106 — no desktop Graph credential and no desktop poller; the only Graph traffic stays the Web host's GET-only Deleted-Items read. L-02 — parity is proven on the local stack with the existing fakes.
- Depends on: `DSK-03-02` route-group skeleton; `DSK-03-03` right filter; `DSK-03-12` is the same route group in the gateway plan — coordinate, do not duplicate

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `run-tests` → `test-gap-analysis` (dotnet/skills `98f84851`, plugin `dotnet-test`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for Microsoft Graph mail read semantics)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, the endpoint map § `Mail workspace`, the Inbox screen spec, and `docs/frd/frd-08-email-mailbox-and-background-processing.md`. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-03-mail-endpoints`.
2. Check whether [[DSK-03-12]] has already landed the mail route group. If it has, this ticket **extends** it with the provider-availability and Deleted-Items rules below and adds no second group; if it has not, this ticket creates the group and [[DSK-03-12]] reviews it. Record the choice in `plan` before writing code.
3. Read `Pages/Mail/Index.cshtml.cs` and `Pages/Mail/Message.cshtml.cs` in full. Tabulate in `research`, per handler: the Core call, the version fields it verifies, the reason it requires, and the exact operator sentence it produces (for example the unlink warning "Unlinking this email cancels case &lt;ref&gt;" recorded in the endpoint map). Record the commit SHA read — upstream MAIL-011/012 fixes arrive with the first sync.
4. Implement the read endpoints: `GET /api/v1/mail?mailbox&folder&page&pageSize&q&deleted` over `ListRetainedMail` + `GetRetainedMailFreshness`, `GET /api/v1/mail/{id}/preview` over the same inert body presentation the page uses, and `GET /api/v1/mail/{id}` over `GetRetainedMail`. Lists default newest first; each read returns `version` and a weak `ETag`.
5. Keep the Deleted-Items path exactly as it is: `deleted=true` routes to `SearchDeletedMail`, capped at the 100 newest messages, GET-only against the resolved `deleteditems` folder, nothing retained, nothing backfilled. Do not add a write path, a subscription or a change-notification callback — that would need a new accepted decision under proposal § 4.
6. Implement the mutations as explicit verbs with `operationKey` and the versions the handler verifies today: `POST /api/v1/mail/{id}/link-case/prepare`, `.../link-case`, `.../unlink-case/prepare`, `.../unlink-case`, `POST /api/v1/mail/{id}/classification`, `POST /api/v1/mail/{id}/move-to-recommended-folder` (which also requires `reason`). Reuse `IAcquireCaseEditLease`, `ILinkIntake` and `IReverseIntakeLink` — no new Core code.
7. Represent provider availability honestly: when the folder mover resolves to `EmptyRetainedMailFolderMoveStore` (`src/Pegasus.Core/Intake/RetainedMailFolderMove.cs:72`), the message detail response must omit the move affordance entirely rather than returning a control that fails on use. Add a boolean capability field to the detail DTO in `src/Pegasus.Contracts` and assert both compositions.
8. Add contract tests in `tests/Pegasus.Api.ContractTests` mirroring the scenarios already proven in `tests/Pegasus.IntegrationTests/MailWorkspaceWebTests.cs`: paging and freshness, preview inertness, link/unlink prepare-then-confirm with version verification, classification correction, move with reason, and the move-absent composition.
9. Add a parity test that the endpoint and the Razor handler produce the same Core effect for the same input — same versions consumed, same association written — so "same Core owner" is proven rather than asserted.
10. Assert no credential leakage: no response carries a Graph token, mailbox secret or raw provider JSON; the client depends only on Pegasus-owned contracts (proposal § 12.3's rule applied to mail).
11. Update `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Mail workspace` if any row's shape changed, and add the capability field to the detail row's `Returns` column.
12. Run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the plan document, then open the PR into `dev`.

## Acceptance criteria

- [ ] Every mail endpoint calls the same Core use case as the Razor handler it replaces; no second business implementation exists.
- [ ] Deleted-Items search stays a capped, GET-only Graph read composed in the Web host; nothing is retained or backfilled.
- [ ] The move-to-recommended-folder affordance is absent from the contract when the provider port is not composed.
- [ ] Link and unlink verify the same versions and lease the page verifies, and the unlink consequence sentence is carried in the contract.
- [ ] No Graph token, mailbox secret or raw provider payload appears in any response.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` — expected: all mail endpoint facts pass, including both provider compositions.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~MailWorkspaceWebTests"` — expected: every existing fact stays green.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: the new parity facts pass.

## Evidence tier

Tier 5 — Web/API/MCP caller.
Tier 5 obliges evidence that the real routes reach Core with authentication, validation, versions and exception translation observable, for every mail endpoint added.

## Documentation changes

- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — § `Mail workspace` row updates
- `docs/frd/frd-08-email-mailbox-and-background-processing.md` — desktop behaviour clause (behaviour, not mechanism)

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web` (`/api/v1` mail group), `src/Pegasus.Contracts`, `tests/Pegasus.Api.ContractTests`, `tests/Pegasus.IntegrationTests`. Must not touch `src/Pegasus.Infrastructure/Email/GraphApprovedSources.cs`, `src/Pegasus.Worker`, or the Razor mail pages.
- **Traps**: Graph intake stays central (ADR-0106) — no desktop poller, no change-notification callback, no Graph credential in the package; the folder mover has no production writer, so a control that assumes it exists is a defect; scope creep into MAIL-12/13/17/19 (compose, mailbox mutation, chasers) is out of conversion scope (proposal § 13.11) — only the seam is built; run the upstream sync before characterising the pages, then record the SHA read.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
