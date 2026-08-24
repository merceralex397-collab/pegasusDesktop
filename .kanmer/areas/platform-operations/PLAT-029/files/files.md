# File map — PLAT-029

## Direct change surface

- `docs/desktop/08-testing/test-uat-stack.md` — § Components (line 27) and § "Known gaps (record, do not hide)" (line 173): record that intake-retained document content is served locally once this lands, and until it does, record the gap there rather than leaving it unstated. [[DSK-08-17]] owns that file's lifecycle sections; keep this edit to the two places named.
- `docs/current-architecture.md` — only if the stated local-adapter behaviour changes; line 528 describes the flat case-folder custody and the Evidence gallery reading document records.
- `docs/frd/frd-05-documents-extraction-and-custody.md` — no change. This fix brings the local adapter into line with rules the FRD already states; if implementing it appears to need an FRD change, stop and raise it rather than editing the FRD.
- This ticket's `files`, `plan` and `proof` documents — the two layouts, the chosen resolution strategy and its reason, and the local run that proves it.

## Context files

- **Upstream provenance** — upstream area `platform-operations`; upstream status `backlog`; upstream profile `fix`; upstream labels `found-during-qa`, `developer-experience`; upstream links `CASE-019`, `DOCS-009` (both `done` upstream). Read in full from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at clone commit `a5b28111`, read date **2026-08-24**. The upstream ticket carries no `research`, `files`, `plan`, `checklist` or `open-questions` document — nothing was omitted in the copy.
- **Carry-over register row**: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:181` — disposition `gateway-worker-ticket (needed by the Test/UAT stack)`, target area plan 08, fork area `platform-operations`.
- **Governing document**: `docs/frd/frd-05-documents-extraction-and-custody.md` § Staging and custody — receipt/staging and accepted case custody are different states, and a custody transition records source identity, content hash, target identity/version, actor/caller, time, and failure/retry state. This fix changes no rule in that FRD; it brings the local adapter into line with rules the FRD already states.
- **Plan detail**: `docs/desktop/08-testing/test-uat-stack.md:27` (§ Components — the Box-custody row naming `LocalCaseCustody` and `LocalDocumentContentStore` under the ignored artifact root) and `:173` (§ "Known gaps (record, do not hide)" — which does not currently record this).
- **Reuse classification**: `docs/desktop/05-implementation-and-migration/reuse-map.md:60` — `Custody/` (`BoxCaseCustody.cs` 1,016; `BoxDocumentContentStore` 240; `LocalCaseCustody` 549; `LocalDocumentContentStore` 183) is **REUSE server-side**, so the desktop inherits this defect unchanged.
- **Repository evidence** (every path and line confirmed in the fork tree on 2026-08-24):
  - `src/Pegasus.Core/Documents/DocumentContracts.cs:226-282` — `IDocumentContentStore`, the `OpenReadVersionAsync` default at `:267`, and `ManagedDocumentContentAddress` at `:284` (`CaseId`, `CaseReference`, `OccurrenceId`, `OccurrenceOrdinal`, `DocumentId`, `VersionId`, `Version`, `SemanticRole`, `FileName`, `MediaType`).
  - `src/Pegasus.Infrastructure/Custody/BoxDocumentContentStore.cs:26-36` (`FlatFileName`), `:75` (the override), `:107-115` (the deliberate throw).
  - `src/Pegasus.Infrastructure/Custody/LocalDocumentContentStore.cs:85-108` (`OpenReadAsync`, and the `FileNotFoundException` at `:97`), `:136-153` (`Resolve`), `:166-171` (`SafeCaseFolderName` via `CustodyNames.SafeName`).
  - `src/Pegasus.Infrastructure/Custody/LocalCaseCustody.cs:78-82`, `:112-117`, `:148-152`, `:449` (`GetCaseRelativeId` → `cases/{caseId:N}`), `:305-318` (`Resolve` and its outside-the-root guard).
  - `src/Pegasus.Infrastructure/DependencyInjection.cs:367-375` — both stores composed over `Path.Combine(<local artifact root>, "custody")`.
  - `src/Pegasus.Infrastructure/Persistence/EfQueuedCustodyProcessor.cs:186`, `:286`, `:321`, `:362`, `:433-440` — the retention calls and the document-record registration.
  - Readers that break: `src/Pegasus.Infrastructure/Persistence/EfDocumentCustodyStore.cs:252` (download), `:589` (export archive), `:740-754` (`Address`); `src/Pegasus.Infrastructure/Persistence/EfAssessmentReportProjectionSource.cs:89`; `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs:526`.
  - Writers that must keep working after the change: `src/Pegasus.Infrastructure/Persistence/EfDocumentCustodyStore.cs:117` (`StoreVersionAsync` → the interface default → `StoreAsync`) and `src/Pegasus.Infrastructure/Persistence/EfDocumentRequestStore.cs:281` (`StoreAsync`), both of which land in the existing `managed/<versionId:N>` layout.
  - Existing tests over this store: `tests/Pegasus.IntegrationTests/DocumentCustodyDurabilityTests.cs:74`, `:273`; `tests/Pegasus.IntegrationTests/EvaHandoffPersistenceTests.cs:199`, `:285`, `:318`, `:372`, `:478`.
  - Profile gate: `src/Pegasus.Web/Program.cs:202` and `:660` — `Features:LocalDocumentCustody` requires the `DevelopmentOffline` runtime profile.
- **Binding decisions**: **L-02** — Test/UAT is the local production-mimicking stack and there is no Azure dev/test/staging; ADR-0014 stands, and this defect therefore blocks evidence rather than merely inconveniencing a developer. **L-01** — the gateway is `Pegasus.Web` evolved in place, so this Infrastructure adapter is production code the desktop era keeps. **D-001** — the fork is the single release source and upstream freezes, so this fix arrives only if the fork board holds it.
- **Depends on**: `None.`

### Upstream ticket PLAT-038 (verbatim)

````

## Ripple effects

- [ ] A case accepted through intake under `DevelopmentOffline` serves the content of its retained source, its attachments and its promoted photographs through `IDocumentContentStore.OpenReadVersionAsync`, with no test-only content seeding.
- [ ] The same case's document export archive and its EVA handoff bundle both build locally from that content.
- [ ] Content written through `StoreAsync` into the existing `cases/<safe case reference>/managed/<versionId:N>/content` layout is still served — no gateway-uploaded document is made unreadable by this change.
- [ ] SHA-256 and length are still verified on every read, the outside-the-custody-root guard still holds, an absent file still throws `FileNotFoundException("The document content is unavailable.")`, and a corrupted one still throws `InvalidDataException`.
- [ ] `BoxDocumentContentStore`, `IDocumentContentStore`'s default `OpenReadVersionAsync`, and `LocalCaseCustody`'s write layout are all unchanged — the local **read** is what moves.
- [ ] `docs/desktop/08-testing/test-uat-stack.md` no longer implies the local stack can serve intake-retained document content when it cannot.

## Out of scope

- **Azure**: no write, and no Azure MCP call. Asking for an Azure test resource to work around this is out of bounds under **L-02** and ADR-0014 — that is the constraint that makes this ticket necessary in the first place.
- **Scope boundary**: may touch `src/Pegasus.Infrastructure/Custody/LocalDocumentContentStore.cs` and the test projects (`tests/Pegasus.IntegrationTests`, `tests/Pegasus.Core.Tests`). **Must not** change `src/Pegasus.Core/Documents/DocumentContracts.cs` (the interface default), `src/Pegasus.Infrastructure/Custody/BoxDocumentContentStore.cs` (the production path) or `src/Pegasus.Infrastructure/Custody/LocalCaseCustody.cs` (the write layout) — the upstream scope is explicit that "Nothing about the Box path or the interface default changes", and changing the write layout instead of the read would invalidate every artifact root already on disk. **Must not** add an `/api/v1` route or contract ([[DSK-03-11]] owns those). **Must not** touch `src/Pegasus.Worker` ([[DSK-07-01]] states "No Worker code is written or changed").
- **Blocks these seeded board tickets** — none of them can be evidenced, and therefore cannot correctly ship, while the only permitted verification environment cannot serve intake-retained bytes: [[DSK-08-17]] (its `Smoke` verb and the whole Test/UAT stack are relied on as the evidence environment — sequence this ticket **before** `Smoke` is relied on), [[DSK-05-14]] (documents, custody and the transfer queue), [[DSK-05-16]] (the image gallery and its viewer, whose acceptance is measured on the local workstation), [[DSK-07-06]] (the desktop document browser, transfer queue, preview pane and bounded working cache), and [[DSK-03-11]] (the case document, custody-retry, export and EVA-handoff endpoints, whose local integration evidence reads the same content). A later pass wires these as `blocks` links.
- **Blocked by**: nothing. This ticket has no dependency and can start immediately — it is the smallest change in the carry-over set with the widest evidence unblock.
- **Neighbouring import — do not collide**: the imported upstream PLAT-032 sweeps the same `Custody/` folder for duplicate routes. If both are in flight, land this one first: it is one file and one method, and PLAT-032's sweep should read the settled shape.
- **Traps**: production is unaffected and must stay that way — a change that alters what Box resolves is a stop condition. The local store serves **both** layouts after this change; dropping the `managed/<versionId:N>` fallback would break gateway-uploaded documents, which is a worse defect than the one being fixed. Do not weaken the hash or length verification to make a path resolve. Do not add a marker or binding sidecar file to make resolution easier — `docs/current-architecture.md:528` records that the marker-file approach was deliberately removed by an operator decision. Never seed content into the local store to make a test pass; that stand-in is the thing this ticket exists to delete.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in this ticket's `plan` document.
