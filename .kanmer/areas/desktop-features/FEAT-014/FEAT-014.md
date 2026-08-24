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
refs:
  - docs/frd/frd-05-documents-extraction-and-custody.md
docs_todo: true
archived: false
created: '2026-08-24T07:54:27.555Z'
updated: '2026-08-24T07:54:27.555Z'
---

## What

Deliver the case Documents tab over the Box-backed custody store: folder and file list, a transfer queue with progress, cancel and retry, a preview pane where safe, export, permission-checked removal, custody retry, and request-upload-link create and revoke — with the local temporary copy always distinguishable from the canonical Box copy.

## Why

Proposal §12.2, §13.7 and §14.6 require native document handling with a transfer queue, no hidden overwrite and visible evidence that the canonical copy was saved. Today it is `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs` (270 lines, six handlers at `:28`, `:74`, `:138`, `:162`, `:186`, `:237`), `Pages/Cases/Documents/Download.cshtml.cs` (112 lines) and `Documents/Export.cshtml.cs` (160 lines) over Core `src/Pegasus.Core/Custody/` and `src/Pegasus.Core/Documents/`, with the Box adapter in `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs`. Box tokens stay central under ADR-0107 — no long-lived provider secret ships in the package. The Phase 6 exit gate requires large and interrupted transfers to recover safely. Siblings: [[DSK-05-05]] supplies the case session, [[DSK-07-05]] the broker endpoints, [[DSK-07-07]] decides whether the desktop may move bytes directly.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-14`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S14 · Documents and custody (DSK-05-14)`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (Custody and Documents rows: upload session, remove, third-party evidence confirm, request-upload-links, content download, export)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.7 Documents and evidence — Case workspace › Documents tab`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.2 Box, § 13.7 Documents and evidence, § 14.6 Documents
- Repository evidence: `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs:28-260`, `src/Pegasus.Web/Pages/Cases/Documents/Download.cshtml.cs`, `Documents/Export.cshtml.cs`; `src/Pegasus.Core/Custody/CustodyContracts.cs` (622 lines), `src/Pegasus.Core/Documents/RequestUploadPolicy.cs` (469 lines), `ICaseCustody`, `IDocumentContentStore`; `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs` (1,016 lines, server-side only); `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs` (1,796 lines)
- Binding decisions: L-01 the gateway brokers Box; L-02 transfer-failure tests run on the local Test/UAT stack; L-04 routing named on the ticket; ADR-0107 consumed — Box credentials stay behind the gateway
- Depends on: `DSK-05-05` the case lease and version session; `DSK-07-05` the Box broker endpoints; `DSK-07-07` the spike deciding direct transfer versus gateway streaming

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`; `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`; `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `minimal-api-file-upload` (dotnet/skills `98f84851`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S14, the screen spec Documents section, `docs/frd/frd-05-documents-extraction-and-custody.md` and the [[DSK-07-07]] spike outcome. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-14-documents-custody` and worktree `../pegasus-worktrees/dsk-05-14-documents-custody` from `origin/dev`.
2. Read `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs` and both Documents pages in full. Tabulate in `research` the six custody handlers with their Core calls, the permission rules for removal, the reason requirements, the request-upload-link policy bounds from `src/Pegasus.Core/Documents/RequestUploadPolicy.cs`, and how the export builds its archive. Record the SHA read — upstream PLAT-039 (Box token refresh) and PLAT-041 (folder resolve once per export) arrive via the one-way sync.
3. Read the [[DSK-07-07]] spike result and record which transfer mode this slice implements: **gateway streaming** (the default) or direct transfer using a short-lived, file-scoped downscoped Box token. Do not decide it here — if the spike has not landed, the ticket stays in Preparing.
4. Confirm the endpoints from [[DSK-07-05]] and [[DSK-03-11]]: document list with metadata, `GET /api/v1/cases/{id}/documents/{docId}/content` with `ETag` and range, the upload-session triple, `DELETE /api/v1/cases/{id}/documents/{docId}` (soft and reasoned), `POST /api/v1/cases/{id}/custody/retry`, `POST/DELETE /api/v1/cases/{id}/request-upload-links`, and `POST /api/v1/cases/{id}/documents/export`.
5. Add the document DTOs to `src/Pegasus.Contracts` carrying file type, size, source, uploader, timestamp, custody state and a canonical-copy indicator, so the UI can distinguish local temporary from canonical without inference.
6. Implement `TransferQueueService` in `src/Pegasus.Desktop.Infrastructure`: chunked streaming uploads and downloads with progress, cancel, resume-or-restart on interruption, and explicit retry of a failed transfer. Temporary files are written to a per-user path with restrictive ACLs and bounded retention as area 10 specifies, and are deleted when the transfer completes or is abandoned.
7. Implement `CaseDocumentsViewModel` in `src/Pegasus.Desktop`: folder and file list, the transfer queue with per-item state, a preview pane for safe types only, an explicit "open externally" command, export, removal behind the permission check, custody retry, and request-link create and revoke as reasoned commands.
8. Make the canonical-versus-local distinction explicit in the UI per proposal §14.6 and show evidence that the canonical copy was saved. There is no hidden automatic overwrite: a name collision surfaces a decision, and the conflict handling itself is [[DSK-07-08]].
9. Add contract tests in `tests/Pegasus.Api.ContractTests` for each endpoint: success, 401, 403, 409 stale version, replay of the same `operationKey`, reason required on removal, range download, and an assertion that no Box credential or token appears in any response.
10. Add transfer-failure tests: a large transfer interrupted mid-stream leaves no partial canonical document and is retryable; a cancelled upload leaves no orphan; a failed custody item can be retried through the human-only retry command. Extend `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs` patterns rather than inventing a parallel harness.
11. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for queue state transitions, cancel, retry, permission-gated removal, preview-type gating and the canonical indicator.
12. Measure the performance property: a transfer in progress must not block navigation, and memory must stay steady across repeated large transfers. Record the method and figures in the ticket proof (tier 10).
13. Prove no provider secret ships: run the secret scan from [[DSK-08-11]] over the built package and the desktop logs, and record the clean result in the proof.
14. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-13` and the document rows, add the documents section to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] Case documents list with type, size, source, uploader and timestamp, and a visible canonical-copy indicator.
- [ ] Transfer queue shows progress, supports cancel and retry, and does not block navigation.
- [ ] Large and interrupted transfers recover safely and leave no partial canonical document or orphan.
- [ ] Removal is permission-checked and reasoned; there is no hidden automatic overwrite.
- [ ] Request-upload links can be created and revoked; custody retry is available for a failed item.
- [ ] No Box secret or token appears in the package, in a response body or in a log.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: all document and custody endpoint facts pass, including the no-credential assertion.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: custody outbox tests plus the new interruption and cancellation facts pass.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: queue, permission and preview facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script documents` — expected: upload, preview and export by keyboard pass; axe report attached.
- [ ] Performance and secret-scan records in the ticket proof — expected: navigation unblocked during transfer, steady memory, and a clean secret scan over package and logs.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility. Tier 10 — Performance/concurrency.
Tier 5 obliges route-level evidence that the document endpoints reach Core and the Box adapter with authorization, idempotency and exception translation; tier 7 obliges keyboard, focus and error-behaviour evidence from a real run; tier 10 obliges measured behaviour against the stated file-count and 10 MiB limits, including burst and interruption behaviour.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — row `PAR-13` and the document download/export rows
- `docs/frd/frd-13-desktop-operator-experience.md` — documents and custody section
- `docs/capabilities.md` — `DSK` rows for the document browser and transfer queue

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` documents and custody groups in `src/Pegasus.Web` and the test projects. Must not reference `src/Pegasus.Infrastructure/Custody/` or any Box SDK from the desktop — the architecture test from [[DSK-02-12]] enforces it.
- **Traps**: Box tokens stay central (ADR-0107); temporary working copies need per-user ACLs and bounded retention; no hidden overwrite — a collision is a decision, and version conflict handling is [[DSK-07-08]]; upstream DOCS-011 and DOCS-012 are absorbed by this screen spec while PLAT-039 and PLAT-041 arrive by upstream sync — check `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` before fixing forward; a new table would need a runtime role GRANT migration and `scripts/Test-MigrationGrants.ps1` (PLAT-035) — avoid adding one in this slice; `Features:DesktopGateway` must be enabled in tests.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
