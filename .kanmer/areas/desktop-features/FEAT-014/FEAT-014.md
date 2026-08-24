---
id: FEAT-014
type: ticket
title: 'DSK-05-14 · S14 Documents and custody (Box browser, transfer queue, preview)'
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-05
  - phase-6
  - tier-5
  - tier-7
  - tier-10
groups:
  - EPIC-006
  - HZN-007
links: []
blocks:
  - FEAT-016
  - FEAT-022
  - FEAT-025
  - FEAT-044
  - TEST-008
  - TEST-016
refs:
  - docs/frd/frd-05-documents-extraction-and-custody.md
docs_todo: true
archived: false
created: '2026-08-24T07:54:27.555Z'
updated: '2026-08-24T12:32:29.392Z'
---

## What

Deliver the case Documents tab over the Box-backed custody store: folder and file list, a transfer queue with progress, cancel and retry, a preview pane where safe, export, permission-checked removal, custody retry, and request-upload-link create and revoke — with the local temporary copy always distinguishable from the canonical Box copy.

## Why

Proposal §12.2, §13.7 and §14.6 require native document handling with a transfer queue, no hidden overwrite and visible evidence that the canonical copy was saved. Today it is `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs` (270 lines, six handlers at `:28`, `:74`, `:138`, `:162`, `:186`, `:237`), `Pages/Cases/Documents/Download.cshtml.cs` (112 lines) and `Documents/Export.cshtml.cs` (160 lines) over Core `src/Pegasus.Core/Custody/` and `src/Pegasus.Core/Documents/`, with the Box adapter in `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs`. Box tokens stay central under ADR-0107 — no long-lived provider secret ships in the package. The Phase 6 exit gate requires large and interrupted transfers to recover safely. One command in this set is different in kind: **request-upload-link create and revoke are plumbing over a capability that is composed closed in production**, so the tab must present them honestly rather than offer an operator a findable link it cannot issue — the activation is upstream CASE-022 (board [[CASE-002]])'s, not this ticket's. Siblings: [[DSK-05-05]] supplies the case session, [[DSK-07-05]] the broker endpoints, [[DSK-07-07]] decides whether the desktop may move bytes directly, [[DSK-05-16]] owns the gallery and its viewer, and [[DSK-07-06]] owns the document browser itself — `CaseDocumentsViewModel`, `CaseDocumentsView.xaml` and `TransferQueueService`.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-14`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S14 · Documents and custody (DSK-05-14)`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (Custody and Documents rows: upload session, remove, third-party evidence confirm, request-upload-links, content download, export)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.7 Documents and evidence — Case workspace › Documents tab`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.2 Box, § 13.7 Documents and evidence, § 14.6 Documents
- Upstream carry-over: **upstream CASE-022 (board [[CASE-002]])** *Deliver public upload links (INT-31) to the operator's accepted limits* — imported into `case-reference-workflow` under board id `CASE-002`. **The fork board id and the upstream id differ, and the board also carries an unrelated `CASE-001` (upstream CASE-021); always cite it as `upstream CASE-022 (board [[CASE-002]])`.** It **owns** the activation of INT-31 and the accepted-limits change; this ticket and [[DSK-03-11]] are both consumers. What it establishes, verified against this repository: the capability is composed as `UnavailableDocumentRequestStore`, which throws; `src/Pegasus.Infrastructure/DependencyInjection.cs:433-441` is that composition; `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs:116`, `:130` pins it closed in the production profile and `/uploads` returns 404 there; and two of the operator's accepted answers of 2026-08-24 — a per-link expiry and no rate limiting — are inexpressible in the built `RequestUploadPolicy`/`RequestUploadLimits` contract. So the endpoint-map row's promise of "link id + expiry" cannot be met until that ticket lands.
- Repository evidence: `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:28-260`, `src/Pegasus.Web/Pages/Cases/Documents/Download.cshtml.cs`, `Documents/Export.cshtml.cs`; `src/Pegasus.Core/Custody/CustodyContracts.cs` (622 lines), `src/Pegasus.Core/Documents/RequestUploadPolicy.cs` (469 lines), `ICaseCustody`, `IDocumentContentStore`; `src/Pegasus.Infrastructure/Persistence/UnavailableDocumentRequestStore.cs` and `src/Pegasus.Infrastructure/DependencyInjection.cs:433-441` — the throwing composition behind the request-link commands; `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs:116`, `:130` — the composition test that pins it closed; `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs` (1,016 lines, server-side only); `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs` (1,796 lines)
- Binding decisions: L-01 the gateway brokers Box; L-02 transfer-failure tests run on the local Test/UAT stack; L-04 routing named on the ticket; ADR-0107 consumed — Box credentials stay behind the gateway
- Depends on: `DSK-05-05` the case lease and version session; `DSK-07-05` the Box broker endpoints; `DSK-07-07` the spike deciding direct transfer versus gateway streaming; `DSK-07-06` — owns `CaseDocumentsViewModel`, `CaseDocumentsView.xaml` and `TransferQueueService`; this slice adds the export, custody-retry and permission-checked removal commands to them; `DSK-03-11` the request-upload-link routes, which it makes return a named `provider-unavailable` problem while the capability is closed. The imported **upstream CASE-022 (board [[CASE-002]])** ticket owns activating INT-31: **until it lands, the request-upload-link commands on this tab are inert**, and this ticket never activates the capability, composes a different store or issues a link of its own.

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`; `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`; `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `minimal-api-file-upload` (dotnet/skills `98f84851`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S14, the screen spec Documents section, `docs/frd/frd-05-documents-extraction-and-custody.md`, the [[DSK-07-07]] spike outcome and the upstream CASE-022 (board [[CASE-002]]) body named under Source of truth. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-14-documents-custody` and worktree `../pegasus-worktrees/dsk-05-14-documents-custody` from `origin/dev`.
2. Read `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs` and both Documents pages in full. Tabulate in `research` the six custody handlers with their Core calls, the permission rules for removal, the reason requirements, the request-upload-link policy bounds from `src/Pegasus.Core/Documents/RequestUploadPolicy.cs`, and how the export builds its archive. Record the SHA read — upstream PLAT-039 (Box token refresh) and PLAT-041 (folder resolve once per export) arrive via the one-way sync.
3. Read the [[DSK-07-07]] spike result and record which transfer mode this slice implements: **gateway streaming** (the default) or direct transfer using a short-lived, file-scoped downscoped Box token. Do not decide it here — if the spike has not landed, the ticket stays in Preparing.
4. Confirm the endpoints from [[DSK-07-05]] and [[DSK-03-11]]: document list with metadata, `GET /api/v1/cases/{id}/documents/{docId}/content` with `ETag` and range, the upload-session triple, `DELETE /api/v1/cases/{id}/documents/{docId}` (soft and reasoned), `POST /api/v1/cases/{id}/custody/retry`, `POST/DELETE /api/v1/cases/{id}/request-upload-links`, and `POST /api/v1/cases/{id}/documents/export`.
5. Add the document DTOs to `src/Pegasus.Contracts` carrying file type, size, source, uploader, timestamp, custody state and a canonical-copy indicator, so the UI can distinguish local temporary from canonical without inference.
6. Check whether `TransferQueueService` already exists in `src/Pegasus.Desktop.Infrastructure` from [[DSK-07-06]], which owns it. If it does, extend it in place and change no existing member; if it has not landed, create it with exactly the shape [[DSK-07-06]] step 3 pins, restated here verbatim so the two cannot drift — a bounded queue of upload and download items, each with `notStarted`/`running`/`succeeded`/`failed`/`cancelled` state, a correlation id, progress in bytes, cancellation via `CancellationTokenSource`, and explicit retry of a failed item (proposal § 16.1); uploads use the three-step session from [[DSK-07-05]] and a cancelled or failed upload never calls `complete` — and record in the plan document which case applied. Either way, this slice's own requirement holds: temporary files are written to a per-user path with restrictive ACLs and bounded retention as area 10 specifies, and are deleted when the transfer completes or is abandoned. Never a second transfer service.
7. Check whether `CaseDocumentsViewModel` already exists from [[DSK-07-06]], which owns that type and its view. If it does, add the export, custody-retry and permission-checked removal commands to it in place and change no existing member; if it has not landed, create it with exactly the members [[DSK-07-06]] step 5 pins (`[ObservableProperty]` partial properties, `[RelayCommand]`, no UI types in the view model) and record in the plan document which case applied. Either way this slice's own surface is the same: folder and file list, the transfer queue with per-item state, a preview pane for safe types only, an explicit "open externally" command, export, removal behind the permission check, custody retry, and request-link create and revoke as reasoned commands. Never a second view model for the Documents tab.
8. **Make the request-upload-link commands honest about being inert (upstream CASE-022, board [[CASE-002]]) — the mirror of [[DSK-03-11]] step 8.** In production the capability is composed as `UnavailableDocumentRequestStore`, which throws; `/uploads` returns 404; and `ProductionCompositionTests` (`:116`, `:130`) pins that closed. [[DSK-03-11]] therefore makes `POST`/`DELETE /api/v1/cases/{id}/request-upload-links` return the named problem `urn:pegasus:problem:provider-unavailable` with a stable operator sentence saying the upload-link capability is not active. This tab renders that state and nothing more: the create and revoke commands are present and discoverable, their unavailability is stated in words on the surface rather than shown as a bare failure, and no link, expiry, QR code or copyable URL is ever fabricated. **Do not** work around it — no second issuer in `src/Pegasus.Desktop.Infrastructure`, no locally generated token, no change to `ProductionCompositionTests`, and no offline stub that behaves like a link. Record in the plan document that the commands become live when the imported upstream CASE-022 (board [[CASE-002]]) ticket activates INT-31 to the operator's accepted limits, and that until then the ticket's own acceptance is met by the honest inert state, not by a working link. If the screen spec or a design asset shows a live link, raise it against that ticket rather than implementing to the picture.
9. Make the canonical-versus-local distinction explicit in the UI per proposal §14.6 and show evidence that the canonical copy was saved. There is no hidden automatic overwrite: a name collision surfaces a decision, and the conflict handling itself is [[DSK-07-08]].
10. Add contract tests in `tests/Pegasus.Api.ContractTests` for each endpoint: success, 401, 403, 409 stale version, replay of the same `operationKey`, reason required on removal, range download, an assertion that no Box credential or token appears in any response, and one fact that `POST /api/v1/cases/{id}/request-upload-links` under the production composition returns the named `provider-unavailable` problem rather than a 500 or a fabricated link.
11. Add transfer-failure tests: a large transfer interrupted mid-stream leaves no partial canonical document and is retryable; a cancelled upload leaves no orphan; a failed custody item can be retried through the human-only retry command. Extend `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs` patterns rather than inventing a parallel harness.
12. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for queue state transitions, cancel, retry, permission-gated removal, preview-type gating, the canonical indicator, and the request-link commands surfacing the named unavailable state with no fabricated link value.
13. Measure the performance property: a transfer in progress must not block navigation, and memory must stay steady across repeated large transfers. Record the method and figures in the ticket proof (tier 10).
14. Prove no provider secret ships: run the secret scan from [[DSK-08-11]] over the built package and the desktop logs, and record the clean result in the proof.
15. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-13` and the document rows, add the export, custody-retry and permission-checked removal behaviour inside the documents and transfer-queue section [[DSK-07-06]] creates in `docs/frd/frd-13-desktop-operator-experience.md` (a sub-heading under that section, not a second documents section), run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] Case documents list with type, size, source, uploader and timestamp, and a visible canonical-copy indicator.
- [ ] Transfer queue shows progress, supports cancel and retry, and does not block navigation.
- [ ] Large and interrupted transfers recover safely and leave no partial canonical document or orphan.
- [ ] Removal is permission-checked and reasoned; there is no hidden automatic overwrite.
- [ ] Custody retry is available for a failed item.
- [ ] Request-upload link create and revoke are present as reasoned commands and are **inert until upstream CASE-022 (board [[CASE-002]]) activates INT-31**: while the capability is composed closed they render the named `provider-unavailable` state from [[DSK-03-11]] in words, and no link, expiry or copyable URL is fabricated anywhere in the desktop. No second issuer, no local stub, and `ProductionCompositionTests` unchanged and passing.
- [ ] No Box secret or token appears in the package, in a response body or in a log.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: all document and custody endpoint facts pass, including the no-credential assertion and the inert request-upload-link fact.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: custody outbox tests plus the new interruption and cancellation facts pass, and `ProductionCompositionTests` stays green and unchanged.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: queue, permission, preview and request-link-unavailable facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script documents` — expected: upload, preview and export by keyboard pass; axe report attached.
- [ ] Performance and secret-scan records in the ticket proof — expected: navigation unblocked during transfer, steady memory, and a clean secret scan over package and logs.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility. Tier 10 — Performance/concurrency.
Tier 5 obliges route-level evidence that the document endpoints reach Core and the Box adapter with authorization, idempotency and exception translation; tier 7 obliges keyboard, focus and error-behaviour evidence from a real run; tier 10 obliges measured behaviour against the stated file-count and 10 MiB limits, including burst and interruption behaviour.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — row `PAR-13` and the document download/export rows; the request-upload-link row records that the capability is inert until upstream CASE-022 (board [[CASE-002]]) activates it
- `docs/frd/frd-13-desktop-operator-experience.md` — the export, custody-retry and permission-checked removal behaviour inside the documents and transfer-queue section [[DSK-07-06]] creates; this ticket adds no second documents section
- `docs/capabilities.md` — `DSK` rows for the document browser and transfer queue

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may extend `CaseDocumentsViewModel` and `CaseDocumentsView.xaml` in `src/Pegasus.Desktop` and `TransferQueueService` in `src/Pegasus.Desktop.Infrastructure` — [[DSK-07-06]] owns all three and this slice adds members to them rather than creating its own — and may touch `src/Pegasus.Contracts`, the `/api/v1` documents and custody groups in `src/Pegasus.Web` and the test projects. Must not reference `src/Pegasus.Infrastructure/Custody/` or any Box SDK from the desktop — the architecture test from [[DSK-02-12]] enforces it. `src/Pegasus.Core/Documents/RequestUploadPolicy.cs`, `src/Pegasus.Infrastructure/Persistence/UnavailableDocumentRequestStore.cs`, the composition in `src/Pegasus.Infrastructure/DependencyInjection.cs:433-441` and `tests/Pegasus.IntegrationTests/ProductionCompositionTests.cs` are **owned by the imported upstream CASE-022 (board [[CASE-002]]) ticket** and are not touched here.
- **Traps**: Box tokens stay central (ADR-0107); temporary working copies need per-user ACLs and bounded retention; no hidden overwrite — a collision is a decision, and version conflict handling is [[DSK-07-08]]; upstream DOCS-012 is absorbed by this screen spec, while DOCS-011's viewer half is owned by [[DSK-05-16]] and is not rebuilt here; PLAT-039 and PLAT-041 arrive by upstream sync — check `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` before fixing forward, and note that **the export and evidence-gallery paths must resolve the case folder once per request and issue O(1) + N Box calls, not roughly nine per image; the export and gallery endpoints are not exposed until upstream PLAT-041 has landed via a sync (flow record Q4.3)** — [[DSK-07-05]] owns that budget and its measurement, this tab consumes it. **The request-upload-link commands are plumbing over an inactive capability** (upstream CASE-022, board [[CASE-002]]): that ticket is the **single owner** of activating INT-31 and of the accepted-limits change; [[DSK-03-11]] owns the route's named `provider-unavailable` problem; this tab only renders the honest state. A fabricated link, a locally issued token, a second document-request store, a stubbed expiry, or an edit to `ProductionCompositionTests` is a stop condition — and so is shipping a command that reads to an operator as though it worked. **Upstream ids and fork board ids do not match**: upstream CASE-022 is board `CASE-002`, and the board's `CASE-001` is upstream CASE-021 — cite it as `upstream CASE-022 (board [[CASE-002]])`, never as a bare id. A new table would need a runtime role GRANT migration and `scripts/Test-MigrationGrants.ps1` (PLAT-035) — avoid adding one in this slice; `Features:DesktopGateway` must be enabled in tests. One view model per screen and one transfer service: [[DSK-07-06]] owns `CaseDocumentsViewModel` and `TransferQueueService`, this ticket extends them; a second view model for the same screen, or a second transfer service, is a stop condition.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
