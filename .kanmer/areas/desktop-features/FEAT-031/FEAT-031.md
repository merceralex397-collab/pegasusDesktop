---
id: FEAT-031
type: ticket
title: >-
  DSK-07-05 · Box broker endpoints: list, metadata, download session, upload
  session, remove, confirm evidence
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-07
  - phase-6
  - tier-5
groups:
  - EPIC-008
  - HZN-007
links: []
refs:
  - docs/frd/frd-05-documents-extraction-and-custody.md
docs_todo: true
archived: false
created: '2026-08-24T08:18:48.673Z'
updated: '2026-08-24T08:18:48.673Z'
---

## What

Add the gateway-brokered document endpoints the desktop needs for Box-backed custody: list case documents with canonical metadata, read one document's metadata, a download session (streamed through the gateway with `ETag` and range), a three-step upload session, reasoned logical removal, and third-party vehicle evidence confirmation — every one of them authorising the Pegasus case/document right **before** any Box call.

## Why

Proposal § 12.2 splits Box: the gateway holds or brokers organisational credentials, enforces that a Pegasus user may access the requested case/document, maps Pegasus records to Box object identifiers and records canonical metadata and audit; the desktop owns browsing, transfer and cache. ADR-0107 makes the credential boundary binding — **no long-lived Box token may ship in the desktop package**, and a step that puts one there is a defect. Today the behaviour is `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs` and `Pages/Cases/Documents/{Download,Export}.cshtml.cs` over `src/Pegasus.Core/Documents/` and `src/Pegasus.Core/Custody/`, with `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs` (1,016 lines) as the only Box caller. Siblings: [[DSK-07-06]] is the desktop surface, [[DSK-07-07]] decides whether bytes may ever bypass the gateway, [[DSK-07-08]] adds conflict handling.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-05`
- Plan context: `docs/desktop/07-integrations/README.md` § 3 (Deviation: Box direct transfer — default is streaming through the gateway), § 4 Target state (second bullet)
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` rows `Custody` and `Documents` (upload-session triple, soft reasoned delete, third-party evidence confirm, content download, export)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.2 Box, § 13.7 Documents and evidence, § 4.1 placement row "Box document browsing"
- Repository evidence: `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs`; `src/Pegasus.Web/Pages/Cases/Documents/Download.cshtml.cs:16`, `Documents/Export.cshtml.cs:18`; `src/Pegasus.Core/Documents/DocumentContracts.cs:32-90` (`DocumentVersion`, `DocumentOccurrence`, `CaseDocument`, `AddCaseDocumentCommand`), `:90-200` (`DocumentDownload`, `ExportCaseDocumentsCommand`, `LogicallyRemoveDocumentCommand`, `ConfirmThirdPartyVehicleEvidenceCommand`, `IAddCaseDocument`, `IDownloadCaseDocument`); `src/Pegasus.Core/Intake/IntakeContracts.cs:7` (`IntakeEnvelopeLimits`); `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs:116-150` (`BoxJwtAuthorizationHeaderProvider`, `BoxContentClient`), `:502-800`; `infra/modules/platform.bicep:382-398,555-556` (Box credentials as Container App secrets and Key Vault references); `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs`, `BoxDocumentContentStoreTests.cs`, `ProductionBoxCustodyTests.cs`
- Binding decisions: L-01 — endpoints are `/api/v1` groups in `Pegasus.Web`. **ADR-0107** — Box credentials stay behind the gateway; no provider secret in the desktop package. L-02 — evidence uses the fake Box adapter on the local stack; no Azure test resource.
- Depends on: `DSK-03-02` route-group skeleton; `DSK-03-03` right filter; `DSK-03-11` the upload-session and case-document endpoints in the gateway plan — coordinate so one group exists, not two

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; secret checks by `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (read-only)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `minimal-api-file-upload` (dotnet/skills `98f84851`, plugin `dotnet-aspnetcore`) → `microsoft-code-reference` → `run-tests`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for ASP.NET Core streaming, range requests and `IAsyncEnumerable` responses); Azure MCP read-only `keyvault` (list names only, never values) for the credential-boundary evidence
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, this area's § 3 Box deviation paragraph, the endpoint map § `Cases` Custody and Documents rows, and `docs/frd/frd-05-documents-extraction-and-custody.md`. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-05-box-broker-endpoints`.
2. Read `src/Pegasus.Web/Pages/Cases/Custody.cshtml.cs` and both `Pages/Cases/Documents/*.cshtml.cs` in full. Tabulate in `research` each handler's Core call, its permission check, its required reason, and the version/lease fields it verifies. Record the commit SHA — upstream PLAT-039 (Box access-token refresh, commits `79db11f` and `282ba44`) arrives with the first sync and must be present before any live Box work.
3. Coordinate with [[DSK-03-11]]: if that ticket has landed the upload-session and case-document endpoints, extend them here with the Box-specific metadata and streaming rules; if not, create them here to the endpoint-map shape. Record the decision in `plan`.
4. Add the document DTOs to `src/Pegasus.Contracts`, projecting `DocumentOccurrence` and `DocumentVersion` (`src/Pegasus.Core/Documents/DocumentContracts.cs:32-60`) into: `documentId`, `occurrenceId`, `versionId`, `fileName`, `mediaType`, `contentLength`, `sha256`, `semanticRole`, `source`, `custodyStatus`, `createdAtUtc`, `createdBy`, `isCurrent`, `isLogicallyRemoved`. Never expose Core records directly — they carry `ActionActor` and server-only members.
5. Implement `GET /api/v1/cases/{caseId}/documents` and `GET /api/v1/cases/{caseId}/documents/{occurrenceId}` as authorised reads returning that metadata with a weak `ETag`. The authorisation check on the Pegasus case runs **before** any call into `ICaseCustody` or the Box adapter — assert the ordering in a test, not just in review.
6. Implement `GET /api/v1/cases/{caseId}/documents/{occurrenceId}/content` over `IDownloadCaseDocument`, streaming the `DocumentDownload.Content` stream to the response with `Content-Length`, `ETag`, range support, `X-Content-Type-Options: nosniff` and a safe filename — the same properties the existing `Documents/Download` page guarantees. Never buffer the whole file in memory.
7. Implement the upload session triple — `POST /api/v1/cases/{caseId}/documents/upload-session` → `PUT /api/v1/upload-sessions/{sessionId}` (bytes, chunked) → `POST /api/v1/upload-sessions/{sessionId}/complete` — with the completion carrying `expectedVersion`, `editLeaseToken` and `operationKey` and delegating to `IAddCaseDocument`. Enforce the limits from `IntakeEnvelopeLimits` (`src/Pegasus.Core/Intake/IntakeContracts.cs:7`) at the boundary and return `urn:pegasus:problem:validation` when exceeded. An interrupted upload must leave no receipt and no partial canonical document.
8. Implement `DELETE /api/v1/cases/{caseId}/documents/{occurrenceId}` (logical, reason required) over `LogicallyRemoveDocumentCommand` and `POST /api/v1/cases/{caseId}/third-party-vehicle-evidence/confirm` over `ConfirmThirdPartyVehicleEvidenceCommand`, both with `operationKey` replay semantics.
9. Keep bytes flowing **through** the gateway. Do not issue a Box URL, a Box token or a Box object id to the client in this ticket: whether direct transfer is ever permitted is decided by the [[DSK-07-07]] spike. Add a contract test asserting no response body or header contains `box.com`, a bearer token, a JWT or a Box file/folder id.
10. Write contract tests in `tests/Pegasus.Api.ContractTests` covering, per endpoint: success, 401, 403 on a case the actor may not access, 409 stale `expectedVersion`, replay of the same `operationKey`, oversize upload rejection, range download, reason-required on removal, and the no-credential assertion from step 9.
11. Write integration tests against the fake/local Box adapter following `tests/Pegasus.IntegrationTests/CustodyOutboxIntegrationTests.cs` and `BoxDocumentContentStoreTests.cs`: a completed upload produces exactly one canonical document version with a matching SHA-256; an abandoned session produces none.
12. Run the package/secret assertion for the boundary: confirm Box credentials appear only as Container App secrets and Key Vault references in `infra/modules/platform.bicep:382-398,555-556`, and record (read-only) evidence with the Azure MCP `keyvault` tool listing **names only**. Then run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the plan document, and open the PR into `dev`.

## Acceptance criteria

- [ ] Every endpoint checks the Pegasus case/document right before any Box call, proven by an ordering test.
- [ ] Canonical metadata and action history are written for every mutation; downloads stream rather than buffer.
- [ ] Upload sessions enforce `IntakeEnvelopeLimits`, and an interrupted session leaves no receipt and no partial canonical document.
- [ ] Removal is logical and reasoned; third-party evidence confirmation is idempotent by `operationKey`.
- [ ] No response carries a Box token, Box URL or Box object id; no provider secret exists outside Key Vault / Container App secrets.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` — expected: every endpoint's success, authorization, conflict, replay, limit and no-credential fact passes.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: custody and content-store facts pass, including the abandoned-session fact.
- [ ] `grep -rn "box.com\|BoxJwtAuth\|Box.Sdk" src/Pegasus.Contracts src/Pegasus.Desktop.Infrastructure` — expected: no matches.

## Evidence tier

Tier 5 — Web/API/MCP caller.
Tier 5 obliges observable route-level evidence: real routes reach Core and the custody port with authentication, authorization ordering, validation, idempotency and exception translation.

## Documentation changes

- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — Box broker rows confirmed or amended
- `docs/frd/frd-05-documents-extraction-and-custody.md` — desktop behaviour clause for brokered transfer
- `docs/capabilities.md` — `DSK` row for document transfer

## Guardrails

- **Azure**: no write. Key Vault reads are name-only and need no approval (`docs/runbook.md` § Live-operation approval matrix; mirrored in `docs/desktop/11-azure-disposition/README.md`). A secret rotation or role change would be an exact-target write and is **not** part of this ticket.
- **Scope boundary**: may touch `src/Pegasus.Web` (`/api/v1` documents and custody groups), `src/Pegasus.Contracts`, `tests/Pegasus.Api.ContractTests`, `tests/Pegasus.IntegrationTests`. Must not modify `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs` behaviour, must not add a Box SDK reference to any project the desktop consumes.
- **Traps**: ADR-0107 — a Box token, a long-lived URL or a Box object id in a response, a log or the package is a defect; custody retry stays human-only (`docs/current-architecture.md:571`) and is [[DSK-07-02]]'s command, not an automatic path here; a new table for session state would need a runtime-role `Grant*` migration checked by `scripts/Test-MigrationGrants.ps1` (PLAT-035) — prefer reusing the existing staging/receipt mechanics; sync upstream first so PLAT-039's token-refresh fix is present.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
