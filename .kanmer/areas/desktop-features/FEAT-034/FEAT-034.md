---
id: FEAT-034
type: ticket
title: >-
  DSK-07-08 · Box conflict and version handling: detect a newer canonical
  version before overwrite and surface it
status: preparing
area: desktop-features
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:31:42.378Z'
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
created: '2026-08-24T08:24:13.897Z'
updated: '2026-08-24T21:31:42.378Z'
---

## What

Make a concurrent document change impossible to lose: the upload-completion path detects that a newer canonical version exists for the same document before it writes, refuses with a conflict problem carrying both versions' metadata, and the desktop shows the two versions side by side with the newer one named so the operator decides.

## Why

Proposal § 12.2 requires the desktop to "detect and communicate conflicting document versions"; § 13.7 lists version conflict and transfer failure handling as parity; § 27 item 10 makes "concurrent edits are detected; nothing is silently overwritten" a programme acceptance criterion. The Box adapter already guards folder creation with an ETag-guarded same-parent promotion (`docs/current-architecture.md:528`), but nothing today tells a desktop operator that the file they are about to replace changed underneath them. Siblings: [[DSK-07-05]] owns the upload session this ticket guards, [[DSK-07-06]] owns the queue and browser that must render the conflict.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-08`
- Plan context: `docs/desktop/07-integrations/README.md` § 4 Target state ("conflicts and failed transfers are visible and retryable"), § 7 Risks and traps
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.7 Documents and evidence` ("no hidden overwrite; conflicting versions shown as rows with the newer one named")
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (Custody upload-session rows; the Conventions header's `expectedVersion` / `409` rule)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.2 Box, § 13.7 Documents and evidence, § 27 acceptance item 10
- Repository evidence: `src/Pegasus.Core/Documents/DocumentContracts.cs:32-46` (`DocumentVersion` with `Version`, `Sha256`, `IsCurrent`, `CustodyStatus`), `:66-80` (`AddCaseDocumentCommand` with `ExpectedCaseVersion` and `EditLeaseToken`), `:80-84` (`AddCaseDocumentResult.IsReplay`); `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs:309` (`UploadAsync`), `:349` (`MoveFileAsync`); `docs/current-architecture.md:528`; `tests/Pegasus.IntegrationTests/DocumentCustodyDurabilityTests.cs`, `CustodyOutboxIntegrationTests.cs`
- Binding decisions: L-01 — the conflict is decided in the gateway over Core, never in the client. ADR-0107 — the conflict payload names Pegasus versions and hashes, never a Box object id or token.
- Depends on: `DSK-07-05` the broker endpoints; `DSK-07-06` the desktop browser and transfer queue that render the conflict

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `winui-dev` — `.codex/agents/winui-dev.toml` for the surface
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`) → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for RFC 9457 problem-details extension members)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, the Documents screen spec paragraph quoted above, and `docs/frd/frd-05-documents-extraction-and-custody.md`. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-08-document-conflict`.
2. Establish the current behaviour precisely: read `src/Pegasus.Core/Documents/DocumentContracts.cs` and the custody write path, and record in `research` what happens today when two `AddCaseDocumentCommand` calls target the same document — which one wins, what `IsReplay` means, and whether the case version alone is enough to detect it. Do not assume; run a failing test first if the answer is unclear.
3. Define the detection rule in one place: an upload completion carries the `versionId` (or version number) the client believed was current for that document occurrence, in addition to `expectedCaseVersion`. The server compares it with the current `DocumentVersion` and refuses when they differ.
4. Add the conflict problem to the catalogue rather than inventing a new one: reuse `urn:pegasus:problem:version-conflict` from `docs/desktop/03-gateway-api-and-data/README.md` § 3, extended with `currentVersion` members for **both** sides — the client's believed version and the server's current version, each with `versionId`, `version`, `fileName`, `contentLength`, `sha256`, `createdAtUtc` and `createdBy`.
5. Implement the check in the `POST /api/v1/upload-sessions/{sessionId}/complete` handler from [[DSK-07-05]], before the Core command runs. A refused completion must leave the staged bytes discardable and must not create a document version.
6. Keep replay working: the same `operationKey` replayed after a **successful** completion still returns the original result (`AddCaseDocumentResult.IsReplay`), and is not reported as a conflict. Add a test that distinguishes replay from conflict explicitly — conflating them is the likeliest defect here.
7. Surface it in the desktop: extend the transfer-queue item state from [[DSK-07-06]] with a `conflict` state whose row shows both versions as named rows, with the newer one identified by name, size, time and who created it, and offers exactly the operator choices the design authority permits — never an automatic overwrite.
8. Add an integration test with a genuinely concurrent upload following `tests/Pegasus.IntegrationTests/DocumentCustodyDurabilityTests.cs` patterns: two completions race on the same document; exactly one succeeds; the other returns `409` with both versions' metadata; the canonical store holds exactly one new version.
9. Add contract tests: stale `versionId` → `409` with both payloads; matching `versionId` → `200`; replayed key after success → `200` with `isReplay: true`; the conflict payload contains no Box object id, URL or token.
10. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests`: a conflict response puts the queue row in `conflict`, renders both versions, and offers no command that overwrites without an explicit operator action.
11. Add a `winapp ui` assertion to the documents script from [[DSK-07-06]] showing the conflict row and both versions, and attach the screenshot.
12. Update `docs/desktop/03-gateway-api-and-data/endpoint-map.md` with the extra concurrency token on the completion row. Then run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the plan document, and open the PR into `dev`.

## Acceptance criteria

- [ ] An upload completion whose believed document version is stale is refused with `409` before any canonical write.
- [ ] The conflict payload carries both versions' metadata; the desktop names the newer one.
- [ ] Replay of a successful completion is never reported as a conflict.
- [ ] There is no code path that overwrites a newer canonical version without an explicit operator action.
- [ ] The conflict payload contains no Box object id, URL or token.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` — expected: stale-version, matching-version, replay and no-credential facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: the concurrent-upload fact passes with exactly one new version persisted.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` — expected: the conflict-state facts pass.

## Evidence tier

Tier 5 — Web/API/MCP caller.
Tier 5 obliges route-level evidence that the real endpoint detects the conflict, translates it into the catalogued problem type and writes nothing when it refuses.

## Documentation changes

- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` — the upload-completion row's concurrency token
- `docs/frd/frd-05-documents-extraction-and-custody.md` — the version-conflict behaviour clause

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Web` (`/api/v1` custody group), `src/Pegasus.Contracts`, `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure` and the test projects. Must not change `BoxCaseCustody.cs` semantics or add a second conflict vocabulary — one problem-type list exists (`AGENTS.md` § Simplicity rails, "one list per concept").
- **Traps**: no hidden overwrite — the whole point of this ticket; do not conflate replay with conflict; the conflict must be decided server-side, because a client-side comparison races; ADR-0107 — the payload names Pegasus versions, never Box identifiers; a new table for conflict bookkeeping would need a runtime-role `Grant*` migration (`scripts/Test-MigrationGrants.ps1`) — prefer existing document version data.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
