# Research — FEAT-014: the six custody handlers, and the two commands that are plumbing over a capability composed closed

## Question

What do `Cases/Custody.cshtml.cs` and the two Documents pages actually do, and —
because one pair of commands on this tab is different in kind — exactly how is
the request-upload-link capability composed today, so the tab presents it
honestly instead of offering an operator a link it cannot issue?

## Current behaviour

Read at fork `main` `191ddf33`. The implementer re-reads and records the SHA
(ticket step 2).

| Surface | `path:line` | What it does |
| --- | --- | --- |
| Custody retry | `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:28` `OnPostRetryCustodyAsync` | human-only custody retry |
| Upload document | `…/Custody.cshtml.cs:74` `OnPostUploadDocumentAsync` | `IFormFile` under `IntakeEnvelopeLimits` |
| Remove document | `…:138` `OnPostRemoveDocumentAsync` | soft, reasoned — its own note at `:160` says "The document occurrence was logically removed; custody content and history were retained." |
| Confirm third-party vehicle evidence | `…:162` `OnPostConfirmThirdPartyVehicleEvidenceAsync` | evidence confirmation |
| Create request-upload link | `…:186` `OnPostCreateRequestUploadLinkAsync` | `ICreateRequestUploadLink` |
| Revoke request-upload link | `…:237` `OnPostRevokeRequestUploadLinkAsync` | `IRevokeRequestUploadLink` |
| Download document | `src/Pegasus.Web/Pages/Cases/Documents/Download.cshtml.cs` (112 lines) | `IDocumentContentStore`, no-sniff attachment |
| Export | `src/Pegasus.Web/Pages/Cases/Documents/Export.cshtml.cs` (160 lines) | case export archive |

Parity-matrix rows: **`PAR-13`** (the six custody handlers), **`PAR-16`**
(download) and **`PAR-17`** (export),
`docs/desktop/01-inventory-and-parity/parity-matrix.md`. `PAR-13` is
`inventoried`; `PAR-16` and `PAR-17` are `not inventoried`. The matrix holds
`PAR-01`…`PAR-46` (`grep -c '^| PAR-' …/parity-matrix.md` → 46).

## Findings

### Facts

- **All six custody handlers are at the lines the ticket gives.**
  `grep -n "    public .*On[A-Z]" src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs`
  returns `:28`, `:74`, `:138`, `:162`, `:186`, `:237` exactly. Three of the six
  return `Task<IActionResult>` without `async` (`:138`, `:162`, `:237`), which is
  a detail of how they delegate, not a behavioural difference.
- **The request-upload-link capability is composed **closed** in production.**
  `src/Pegasus.Infrastructure/DependencyInjection.cs:431-441` is the `else` branch
  that registers `UnavailableDocumentRequestStore` as `ICreateRequestUploadLink`,
  `IRevokeRequestUploadLink`, `IUploadToRequest` **and** `IGetRequestUpload`.
- **That store throws for the two staff commands and returns nothing for the
  public one.** `src/Pegasus.Infrastructure/Persistence/UnavailableDocumentRequestStore.cs`
  is 44 lines: create (`:13-20`) and revoke (`:22-29`) each call
  `StaffAuthorization.Require(command.Actor, StaffAccessRight.PerformCasework)`
  and then `throw new DocumentRequestUnavailableException()`; `IUploadToRequest`
  (`:31-38`) returns `RequestUploadDecision.Unavailable`; and
  `IGetRequestUpload` (`:40-43`) returns `null`, which is what makes the anonymous
  `/Uploads/{token}` page 404 rather than reveal anything.
- **A composition test pins it closed, and its comment says why.**
  `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs:110-118` comments
  "INT-31 is not on the alpha path and its limits are an open decision, so
  composing document custody must not activate anonymous upload links", then
  asserts `Assert.IsType<UnavailableDocumentRequestStore>(…GetRequiredService<ICreateRequestUploadLink>())`
  at `:116`. A second test, `ProfileWithoutDurableStorageStillFailsClosed`
  (`:120-136`), repeats the assertion at `:130` alongside
  `Assert.IsType<UnavailableCaseCustody>(…ICaseCustody)`. The file is 229 lines.
- **`RequestUploadPolicy.cs` is 469 lines** (`src/Pegasus.Core/Documents/`), and
  `CustodyContracts.cs` is 622 (`src/Pegasus.Core/Custody/`). The Box adapter
  `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs` is 1,016 lines and is
  server-side only. `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs`
  is 1,796 lines.
- **The endpoint map's request-link row promises "link id, expiry".**
  `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases`, Custody row for
  `POST /cases/{id}/request-upload-links`. That promise cannot be met while the
  capability is composed closed — which is the point of the honest inert state and
  of [[GWY-011]] (plan handle `DSK-03-11`)'s named `provider-unavailable` problem.
- **The screen spec already says "findable".**
  `docs/desktop/06-ui-design/screen-specs.md:343-360` § `§13.7 Documents and
  evidence` lists "Create / Revoke public upload link (findable — CASE-022)"
  alongside the folder/file list, the transfer queue with progress/cancel/retry
  and kept failed rows, the preview pane for supported images and PDF ("PDF via
  the isolated report/preview path — never a WebView hosting app UI"), Open
  externally, Download, Export, reasoned Remove, Confirm third-party vehicle
  evidence, the local-versus-canonical distinction, **no hidden overwrite**, and
  conflicting versions shown as rows with the newer one named. The AutomationIds
  are `Case.Documents.Table`, `Case.Documents.Upload`, `Case.Documents.Queue`,
  `Case.Documents.Preview`, `Case.Documents.OpenExternally`,
  `Case.Documents.UploadLink.Create`.
- **The evidence gallery is specified here too** (`screen-specs.md:357-359`):
  "Evidence gallery (instruction photographs) reads document records with paging
  and download (DOCS-011/012, CASE-011 gallery viewer reused across image-bearing
  screens)". That is [[FEAT-016]] (plan handle `DSK-05-16`)'s control, not this
  ticket's.
- **The upload limits are the same `IntakeEnvelopeLimits`.** The endpoint map's
  Custody upload-session row says "limits from `IntakeEnvelopeLimits`" — the same
  constants at `src/Pegasus.Core/Intake/IntakeContracts.cs:7-56` that
  [[FEAT-013]] (plan handle `DSK-05-13`) reads.
- **The Box adapter is out of the desktop's reach by architecture, not by
  discipline.** [[FND-037]] (plan handle `DSK-02-12`) extends
  `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` for the desktop
  boundaries, and `docs/desktop/05-implementation-and-migration/reuse-map.md:42-48`
  states the boundary: `Pegasus.Desktop` may reference `Pegasus.Core` for
  deterministic local validation but "never `Pegasus.Infrastructure`, EF Core,
  Azure SDKs, Box or Graph SDKs".
- **Two Box performance fixes arrive by sync.** upstream PLAT-039 (token refresh)
  and PLAT-041 (folder resolve once per export). `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md`
  is the register; neither has a fork ticket named in this ticket's body.
- **A new table would need a GRANT migration.** The ticket's Traps name
  `scripts/Test-MigrationGrants.ps1` (upstream PLAT-035) — so this slice adds no
  table.
- **The projects this slice writes into do not exist yet.** `ls src` returns only
  `Pegasus.Core Pegasus.Infrastructure Pegasus.Web Pegasus.Worker`; `ls tests`
  only `Pegasus.ArchitectureTests Pegasus.Core.Tests Pegasus.IntegrationTests`.
  `CaseDocumentsViewModel`, `CaseDocumentsView.xaml` and `TransferQueueService`
  are [[FEAT-032]] (plan handle `DSK-07-06`)'s to create.

### Assumptions

- **A-05-14-1 — [[FEAT-033]] (plan handle `DSK-07-07`) has decided the transfer
  mode before this slice starts.** Gateway streaming is the default; direct
  transfer needs a short-lived, file-scoped downscoped Box token. Confirmed by:
  reading that spike's `research` output. Breaks if: it has not landed — **the
  ticket stays in Preparing** (ticket step 3 says so in its own words). This is a
  sequencing precondition, not a question for this ticket.
- **A-05-14-2 — [[FEAT-032]] may or may not have landed `TransferQueueService`
  and `CaseDocumentsViewModel`.** Both cases are legitimate and both are handled:
  extend in place, or create with exactly the shape that ticket's steps 3 and 5
  pin, and record which applied. Breaks if: it lands *during* this slice — then the
  created types must be reconciled with its pinned shape before either merges.
- **A-05-14-3 — [[GWY-011]] returns
  `urn:pegasus:problem:provider-unavailable` from
  `POST`/`DELETE /api/v1/cases/{id}/request-upload-links` while the capability is
  closed.** Confirmed by: the contract test at step 10. Breaks if: the route
  returns a bare 500 from `DocumentRequestUnavailableException` — then the tab
  cannot state the unavailability in words and would show a raw failure, which the
  ticket's acceptance forbids. Stop and raise on [[GWY-011]].
- **A-05-14-4 — the export and evidence-gallery paths are not exposed until
  upstream PLAT-041 has landed via a sync.** The ticket's Traps state it, with the
  O(1) + N Box-call budget owned by [[FEAT-031]] (plan handle `DSK-07-05`) and
  flow record Q4.3 as the source. Confirmed by: checking
  `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` before
  exposing either. Breaks if: the sync has not run — then export ships behind the
  same gate rather than shipping with roughly nine Box calls per image.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | Every custody command carries a case `expectedVersion` and an `operationKey` (`endpoint-map.md` § `Cases`, Custody rows), and the canonical copy in Box is shared. Lands in the gateway (L-01, ADR-0103). |
| Unattended execution — must it run with every desktop closed? | **yes** | The custody outbox retries and completes with every desktop closed — `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs` (1,796 lines) is its evidence, and the human-only retry at `Custody.cshtml.cs:28` exists precisely because the automatic path is unattended. Lands in the existing `src/Pegasus.Worker` (ADR-0106). |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | The Box credential behind `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs` (1,016 lines). Lands behind the gateway (ADR-0107); `docs/desktop/05-implementation-and-migration/reuse-map.md:42-48` forbids the desktop referencing Box SDKs at all, and [[FND-037]] enforces it. |
| Public callback — must an external service call a stable public endpoint? | **yes** | A request-upload link is by definition a public endpoint an external party opens. It lands on the **gateway host**, which serves the anonymous `/Uploads/{token}` Razor page (`endpoint-map.md` § `Stays web-only`) — and today that capability is composed closed (`src/Pegasus.Infrastructure/DependencyInjection.cs:431-441`), so nothing is reachable. Activating it is upstream CASE-022 (board [[CASE-002]])'s, not this ticket's. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | Removal permissions, the reason requirement, the custody audit and the link policy in `src/Pegasus.Core/Documents/RequestUploadPolicy.cs` (469 lines) must hold whatever the client is; even the closed store still calls `StaffAuthorization.Require(…, PerformCasework)` before throwing (`UnavailableDocumentRequestStore.cs:18`, `:27`). Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **no** | The opposite is required and is measured in this ticket (tier 10): a transfer must not block navigation and memory must stay steady across repeated large transfers, both of which are workstation properties. |

Conclusion: five "yes" answers place the Box broker, the commands, the audit and
the public link endpoint on the gateway host (L-01, ADR-0107), and the custody
outbox in the existing Worker (ADR-0106). The transfer queue, the preview, the
canonical-versus-local distinction and the honest inert state belong in the
desktop. No new Azure resource; no Azure write.

## Implications

- **The request-link commands are honest plumbing, and that is the acceptance.**
  They are present and discoverable, their unavailability is stated **in words** on
  the surface, and no link, expiry, QR code or copyable URL is ever fabricated.
  The ticket's own acceptance says the criterion "is met by the honest inert
  state, not by a working link". Anything that reads to an operator as though it
  worked is a stop condition — including a stubbed expiry.
- **Four things must not be touched to "make it work".**
  `src/Pegasus.Core/Documents/RequestUploadPolicy.cs`,
  `src/Pegasus.Infrastructure/Persistence/UnavailableDocumentRequestStore.cs`, the
  composition at `src/Pegasus.Infrastructure/DependencyInjection.cs:431-441`, and
  `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs`. All four are
  upstream CASE-022 (board [[CASE-002]])'s.
- **Two of the operator's accepted answers are inexpressible today.** The ticket
  records that a per-link expiry and no rate limiting cannot be expressed in the
  built `RequestUploadPolicy`/`RequestUploadLimits` contract — which is why the
  endpoint map's "link id + expiry" promise cannot be met until [[CASE-002]]
  lands.
- **This slice extends types it does not own.** `CaseDocumentsViewModel`,
  `CaseDocumentsView.xaml` and `TransferQueueService` belong to [[FEAT-032]];
  this slice adds the export, custody-retry and permission-checked removal
  commands to them. Two view models for one screen, or two transfer services, is a
  stop condition — so both step 6 and step 7 have an explicit "already exists /
  has not landed" branch and record which applied.
- **No hidden overwrite, and version conflict is someone else's.** A name
  collision surfaces a decision here; the conflict and version handling itself is
  [[FEAT-034]] (plan handle `DSK-07-08`).
- **The preview's PDF path is not this ticket's binding.** The screen spec says
  PDF goes through the isolated report/preview path and never a WebView hosting app
  UI; [[FEAT-032]] owns the preview pane, its safe-type list and the single
  binding to [[FEAT-040]] (plan handle `DSK-07-14`)'s isolated render path.
- **Temporary files are a security surface.** Per-user path, restrictive ACLs,
  bounded retention as area 10 specifies, deleted when the transfer completes or is
  abandoned.

## Open questions

None that block. Everything that could look like one is a sequencing
precondition or a scope boundary with a named owner:

- **[[FEAT-033]] must have decided the transfer mode.** If it has not, the ticket
  stays in Preparing (ticket step 3). A precondition, not a question.
- **Activating INT-31** is upstream CASE-022 (board [[CASE-002]])'s single
  ownership, together with the accepted-limits change.
- **The named `provider-unavailable` problem** is [[GWY-011]]'s (its step 8); if
  the route returns a bare 500 instead, this slice stops and raises it there.
- **The O(1) + N Box-call budget for export and the gallery** is [[FEAT-031]]'s,
  and neither path is exposed until upstream PLAT-041 has landed via a sync.
- **Whether `TransferQueueService` and `CaseDocumentsViewModel` already exist**
  is answered by looking, and both answers have a defined action.
