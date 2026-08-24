# Files — FEAT-014

Surveyed 2026-08-24 against fork `main` `191ddf33`. Paths marked *(created by …)*
do not exist today — `ls src` returns only `Pegasus.Core`,
`Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`; `ls tests` only
`Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`.

**This slice extends three types it does not own.** `CaseDocumentsViewModel`,
`CaseDocumentsView.xaml` and `TransferQueueService` belong to [[FEAT-032]] (plan
handle `DSK-07-06`); a second view model for the same screen, or a second
transfer service, is a stop condition.

## Where the change lands

| Path | Why |
|---|---|
| `src/Pegasus.Contracts/` *(created by [[FND-029]], plan handle `DSK-02-04`)* | Document DTOs carrying file type, size, source, uploader, timestamp, custody state and a **canonical-copy indicator**, so the UI distinguishes a local temporary copy from the canonical Box copy without inference (proposal §14.6). |
| `src/Pegasus.Desktop.Infrastructure/` — `TransferQueueService` *(owned by [[FEAT-032]]; created by [[FND-031]], plan handle `DSK-02-06`)* | **Extend in place if it exists**, changing no existing member. If it has not landed, create it with exactly the shape [[FEAT-032]] step 3 pins and record which case applied: a bounded queue of upload and download items, each with `notStarted`/`running`/`succeeded`/`failed`/`cancelled` state, a correlation id, progress in bytes, cancellation via `CancellationTokenSource`, and explicit retry of a failed item (proposal §16.1); uploads use the three-step session from [[FEAT-031]] (plan handle `DSK-07-05`) and a cancelled or failed upload **never** calls `complete`. This slice's own requirement either way: temporary files on a per-user path with restrictive ACLs and bounded retention (area 10), deleted when the transfer completes or is abandoned. |
| `src/Pegasus.Desktop/` — `CaseDocumentsViewModel`, `CaseDocumentsView.xaml` *(owned by [[FEAT-032]]; created by [[FND-030]], plan handle `DSK-02-05`)* | **Add the export, custody-retry and permission-checked removal commands in place**, changing no existing member; or create with exactly the members [[FEAT-032]] step 5 pins (`[ObservableProperty]` partial properties, `[RelayCommand]`, no UI types in the view model) and record which case applied. AutomationIds are fixed by `docs/desktop/06-ui-design/screen-specs.md:359-361`. |
| `src/Pegasus.Web/` — the `/api/v1` documents and custody groups only | Only where [[FEAT-031]] or [[GWY-011]] (plan handle `DSK-03-11`) left a gap this slice must close to consume its own contract. Behind `Features:DesktopGateway` ([[GWY-002]], plan handle `DSK-03-02`). |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]], plan handle `DSK-08-01`)* | Per endpoint: success, 401, 403, 409 stale version, replay of the same `operationKey`, reason required on removal, range download; an assertion that **no Box credential or token appears in any response**; and one fact that `POST /api/v1/cases/{id}/request-upload-links` under the production composition returns the named `provider-unavailable` problem rather than a 500 or a fabricated link. |
| `tests/Pegasus.IntegrationTests/` | Transfer-failure facts extending the `CustodyOutboxIntegrationTests.cs` (1,796 lines) patterns — a large transfer interrupted mid-stream leaves no partial canonical document and is retryable; a cancelled upload leaves no orphan; a failed custody item can be retried through the human-only command. **Do not invent a parallel harness.** |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[TEST-004]], plan handle `DSK-08-04`)* | Queue state transitions, cancel, retry, permission-gated removal, preview-type gating, the canonical indicator, and the request-link commands surfacing the named unavailable state **with no fabricated link value**. |
| `tests/Pegasus.Desktop.UITests/` *(created by [[TEST-006]], plan handle `DSK-08-06`)* | The `documents` script — upload, preview and export by keyboard — plus the `axe-windows` scan from [[TEST-009]] (plan handle `DSK-08-09`). |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Row `PAR-13` and the document download/export rows `PAR-16`/`PAR-17`. The request-upload-link entry records that the capability is **inert** until upstream CASE-022 (board [[CASE-002]]) activates it. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(created by [[DUI-013]], plan handle `DSK-06-13`)* | Export, custody-retry and permission-checked removal behaviour **inside the documents and transfer-queue section [[FEAT-032]] creates** — a sub-heading under that section, **not a second documents section**. |
| `docs/capabilities.md` | `DSK` rows for the document browser and transfer queue. |

## Context files

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:28,74,138,162,186,237` | The six handlers. `:138` removal is **soft**: its own note at `:160` reads "The document occurrence was logically removed; custody content and history were retained." Removal is a logical operation with a reason, never a delete. |
| `src/Pegasus.Infrastructure/DependencyInjection.cs:431-441` | The `else` branch that composes `UnavailableDocumentRequestStore` as `ICreateRequestUploadLink`, `IRevokeRequestUploadLink`, `IUploadToRequest` and `IGetRequestUpload`. This is *the* fact that makes the two request-link commands plumbing over a closed capability. **Owned by upstream CASE-022 (board [[CASE-002]]); not touched here.** |
| `src/Pegasus.Infrastructure/Persistence/UnavailableDocumentRequestStore.cs` (44 lines) | What "closed" means concretely: create (`:13-20`) and revoke (`:22-29`) each authorise the actor and then `throw new DocumentRequestUnavailableException()`; `IUploadToRequest` (`:31-38`) returns `RequestUploadDecision.Unavailable`; `IGetRequestUpload` (`:40-43`) returns `null`, which is what makes the anonymous `/Uploads/{token}` page 404 rather than reveal anything. Note the authorisation check still runs **before** the throw — the closed path is not a shortcut around permissions. |
| `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs:110-136` | The composition tests that pin it closed, with the reason in the comment at `:111-112`: "INT-31 is not on the alpha path and its limits are an open decision, so composing document custody must not activate anonymous upload links." Assertions at `:116` and `:130`. **Editing this file is a stop condition.** |
| `src/Pegasus.Core/Documents/RequestUploadPolicy.cs` (469 lines) | The link policy bounds. Two of the operator's accepted answers of 2026-08-24 — a per-link expiry and no rate limiting — are **inexpressible** in the built `RequestUploadPolicy`/`RequestUploadLimits` contract, which is why the endpoint map's "link id + expiry" promise cannot be met yet. **Owned by [[CASE-002]].** |
| `src/Pegasus.Core/Custody/CustodyContracts.cs` (622 lines) | `ICaseCustody` and the custody states the DTO's `custodyState` must render, including the failed state the human-only retry acts on. |
| `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs` (1,016 lines) | The Box adapter — server-side only. Read it to understand what the gateway does, never to call it: `docs/desktop/05-implementation-and-migration/reuse-map.md:42-48` forbids the desktop referencing `Pegasus.Infrastructure`, EF Core, Azure SDKs, Box or Graph SDKs, and [[FND-037]] (plan handle `DSK-02-12`) enforces it as a test. |
| `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs` (1,796 lines) | The existing transfer-failure harness. Extend these patterns; a parallel harness is duplicated effort and duplicated maintenance. |
| `docs/desktop/06-ui-design/screen-specs.md:343-361` | The Documents-tab spec: folder/file list with size in MB to one decimal; drag-and-drop and picker into a transfer queue pane with progress, cancel, retry and **failed rows kept**; preview for supported images and PDF, with PDF "via the isolated report/preview path — never a WebView hosting app UI"; Open externally as an explicit command; reasoned logical Remove; Confirm third-party vehicle evidence; **"Create / Revoke public upload link (findable — CASE-022)"**; the clear local-versus-canonical distinction; **no hidden overwrite**; conflicting versions as rows with the newer one named. Also the evidence-gallery line at `:357-359`, which is [[FEAT-016]] (plan handle `DSK-05-16`)'s control, not this ticket's. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (Custody and Documents rows) | The six custody routes plus content download with `ETag` and range, and export. The Custody upload-session row states the limits come from `IntakeEnvelopeLimits` — the same constants at `src/Pegasus.Core/Intake/IntakeContracts.cs:7-56` that [[FEAT-013]] (plan handle `DSK-05-13`) reads. The request-link row's "link id, expiry" is the promise that cannot be met yet. |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | The upstream register. Check it before fixing anything forward: upstream PLAT-039 (Box token refresh) and PLAT-041 (folder resolve once per export) arrive via the one-way sync ([[FND-023]], plan handle `DSK-01-10`). |
| `docs/desktop/05-implementation-and-migration/README.md` § 7 | The recorded repository traps, including that a new table needs runtime role GRANT migrations (upstream PLAT-035, `scripts/Test-MigrationGrants.ps1`) — so this slice adds no table. |
| Group document `HZN-001` / `board-conventions.md` | The join table. Board `CASE-002` is upstream CASE-022; board `CASE-001` is upstream CASE-021 and is a different, live production defect. Always `upstream CASE-022 (board [[CASE-002]])`. |

## Ripple effects

- **OpenAPI and the generated client.** The document DTOs change
  `openapi/pegasus-v1.json` and the generated client that [[FEAT-031]],
  [[GWY-011]] and the contract tests bind to.
- **[[FEAT-032]] owns three of the types this slice writes into.** If it lands
  during this slice, the created types must be reconciled with its pinned shape
  before either merges.
- **[[FEAT-033]] (plan handle `DSK-07-07`) gates the transfer mode.** Its outcome
  decides gateway streaming versus direct downscoped-token transfer; without it
  the ticket stays in Preparing.
- **[[FEAT-034]] (plan handle `DSK-07-08`)** owns version-conflict handling; this
  slice surfaces a collision as a decision and stops there.
- **[[FEAT-016]] (plan handle `DSK-05-16`)** owns the evidence gallery and its
  viewer; [[FEAT-032]] owns the document-preview pane and the safe-type list.
- **[[CASE-002]] flips the request-link commands live.** Nothing in this slice
  changes when it does, other than the state the same commands render — which is
  the point of building them honestly now.
- **`tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs` must stay green
  and unchanged**; it is one of this ticket's verification commands.
- **`docs/frd/frd-13-desktop-operator-experience.md` does not exist yet** — it is
  authored by [[DUI-013]]; this slice contributes a **sub-heading** under
  [[FEAT-032]]'s section, never a second documents section.

## Out of scope

- **`src/Pegasus.Core/Documents/RequestUploadPolicy.cs`,
  `src/Pegasus.Infrastructure/Persistence/UnavailableDocumentRequestStore.cs`, the
  composition at `src/Pegasus.Infrastructure/DependencyInjection.cs:431-441`, and
  `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs`** — all four are
  owned by upstream CASE-022 (board [[CASE-002]]) and are not touched here.
- **Activating INT-31, or any substitute for it**: no second issuer in
  `src/Pegasus.Desktop.Infrastructure`, no locally generated token, no stubbed
  expiry, no offline stub that behaves like a link, no fabricated link, QR code or
  copyable URL — and no command that reads to an operator as though it worked.
- **`src/Pegasus.Infrastructure/Custody/` and any Box SDK from the desktop.**
  Forbidden by `reuse-map.md:42-48` and enforced by [[FND-037]].
- **Version-conflict handling.** [[FEAT-034]]'s.
- **The document-preview pane, its safe-type list and the [[FEAT-040]] (plan
  handle `DSK-07-14`) binding.** [[FEAT-032]]'s.
- **The evidence gallery and its viewer.** [[FEAT-016]]'s.
- **A second view model for the Documents tab, or a second transfer service.**
- **A new database table.** It would need a runtime role GRANT migration
  (upstream PLAT-035).
- **Fixing upstream PLAT-039 or PLAT-041 forward.** Both arrive by the one-way
  sync; the export and evidence-gallery paths are not exposed until PLAT-041 has
  landed, and the O(1) + N Box-call budget is [[FEAT-031]]'s to own and measure.
- **Any Azure write.**
