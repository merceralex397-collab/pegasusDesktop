---
id: PLAT-029
type: ticket
title: >-
  upstream:PLAT-038 · Serve intake-retained document content in the local
  profile
status: implementing
area: platform-operations
order: 90
assignee: codex-mcp-client
profile: fix
stageEntered:
  preparing: '2026-08-24T21:21:17.158Z'
taken_at: '2026-08-25T04:04:18.843Z'
branch: plat-029-local-document-content
worktree: .worktrees/plat-029
labels:
  - found-during-qa
  - developer-experience
  - upstream-carryover
  - upstream-PLAT-038
  - gateway-worker-ticket
groups:
  - EPIC-014
links: []
blocks:
  - TEST-017
  - FEAT-014
  - FEAT-016
  - FEAT-032
  - GWY-011
refs:
  - docs/frd/frd-05-documents-extraction-and-custody.md
docs_todo: true
commits:
  - a505175c94fe56eef65e5336e6290bffd3888f45
prs:
  - '25'
archived: false
created: '2026-08-24T11:49:22.980Z'
updated: '2026-08-26T20:30:38.669Z'
---

## What

Give `src/Pegasus.Infrastructure/Custody/LocalDocumentContentStore.cs` an `OpenReadVersionAsync` override that resolves the same occurrence address the local custody adapter actually writes, so a case accepted through intake under the `DevelopmentOffline` profile can serve and export its retained document content instead of throwing `FileNotFoundException("The document content is unavailable.")`. Nothing about the Box path, the interface default, or the existing `versionId` layout changes.

## Why

Locked decision **L-02** deletes the Azure dev/test/staging environment and makes the local `DevelopmentOffline` stack the **only** verification environment the conversion has. `docs/desktop/08-testing/test-uat-stack.md:27` names `LocalCaseCustody` and `LocalDocumentContentStore` as that stack's Box-custody component. This defect means that environment cannot serve the bytes of any intake-retained document — so the desktop surfaces that exist to show those bytes cannot be evidenced at all before the pilot ring.

The asymmetry is concrete. `IDocumentContentStore.OpenReadVersionAsync` is a default interface method (`src/Pegasus.Core/Documents/DocumentContracts.cs:267-281`) that discards the occurrence half of the address and delegates to the `versionId` overload. `BoxDocumentContentStore` **overrides** it (`src/Pegasus.Infrastructure/Custody/BoxDocumentContentStore.cs:75`) and resolves the flat ordinal name custody wrote into the case folder (`FlatFileName`, `:26-36`), which is why production works; its `versionId` overload deliberately throws (`:107-115`). `LocalDocumentContentStore` does not override it, and resolves `cases/<safe case reference>/managed/<versionId:N>/content` (`:136-153`) — a layout the local custody adapter never writes. `LocalCaseCustody` writes `cases/<caseId:N>/documents/<receiptId:N>/<hash>/content` for the retained source (`:78-82`), `cases/<caseId:N>/documents/<receiptId:N>/attachments/<ordinal:D3>-<hash>/content` for each attachment and promoted photograph (`:112-117`), and `cases/<caseId:N>/images/<ordinal:000>-<receiptId:N>/content` for image-case assets (`:148-152`), each beside a `metadata.json`. Both stores are composed over the same root, `Path.Combine(<local artifact root>, "custody")` (`src/Pegasus.Infrastructure/DependencyInjection.cs:367-375`) — they simply disagree about the key.

Meanwhile `EfQueuedCustodyProcessor.RecordRetainedCaseFilesAsync` (`src/Pegasus.Infrastructure/Persistence/EfQueuedCustodyProcessor.cs:362`) registers `DocumentOccurrence` and `DocumentVersion` rows for every retained file, from `RetainedCaseFile(Ordinal, FileName, MediaType, ContentLength, ContentHash, SemanticRole, OperationKey)` (`:433-440`), without ever writing content through `IDocumentContentStore`. So locally the rows exist and the content is unreachable, and every reader breaks: the case-document download (`EfDocumentCustodyStore.cs:252`), the case document export archive (`:589`), the assessment report projection source (`EfAssessmentReportProjectionSource.cs:89`) and the EVA handoff bundle (`EvaHandoffStore.cs:526`) — all of which call `OpenReadVersionAsync` with the address built at `EfDocumentCustodyStore.cs:740-754`.

No seeded conversion ticket is permitted to fix it:

- [[DSK-08-17]] builds the stack and would be the obvious owner, but its scope boundary allows only `scripts/Invoke-LocalDevelopment.ps1`, `scripts/Invoke-Doctor.ps1`, the seed fixtures and named documentation — `src/Pegasus.Infrastructure` is out of bounds, and its § Known gaps list does not mention this at all.
- [[DSK-05-14]] and [[DSK-07-06]] both forbid any desktop project from referencing `src/Pegasus.Infrastructure/Custody/`, enforced by [[DSK-02-12]]'s architecture test.
- [[DSK-03-11]] is confined to `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Uploads/**`, `openapi/`, the generated client and the test projects.

Production is unaffected, which is why this has survived — but under L-02 "dev-only" and "the only place we can prove anything" are now the same environment. Under **D-001** the fork becomes the single release source and upstream is frozen, so this fix will not arrive by sync either.

## Source of truth

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
## The gap

`IDocumentContentStore.OpenReadVersionAsync` is a **default interface method** that delegates to the `versionId` overload:

```csharp
Task<Stream> OpenReadVersionAsync(ManagedDocumentContentAddress address, …) =>
    OpenReadAsync(address.CaseId, address.CaseReference, address.VersionId, …);
```

`BoxDocumentContentStore` **overrides** it and resolves the full occurrence address — the flat ordinal name custody wrote into the case folder. Its `versionId` overload deliberately throws:

> "Managed Box reads require the persisted business occurrence and revision address."

`LocalDocumentContentStore` does **not** override it. It resolves `<caseReference>/<versionId>` on disk, and intake's local custody adapter never writes that layout — it writes its own attachments tree. So a locally accepted case has `DocumentVersions` rows whose content the store cannot find, and any read throws:

```
System.IO.FileNotFoundException : The document content is unavailable.
```

## Why this is dev-only

Production is unaffected: the Box override resolves the address, and Box already holds the file custody uploaded (DOCS-007 registers records without re-sending content). The asymmetry is that one implementation overrides the default and the other relies on it while storing under a different key.

## What it costs

- The case Evidence gallery cannot serve an intake-retained image under `DevelopmentOffline`, so local visual QA of that surface is impossible.
- The same is true of the case-document download route and of [[CASE-019]]'s export.
- Found while writing `ExportingACaseProducesTheEvaFormatArchive`, which had to seed the content into the local store to run at all. That seeding is a stand-in for what Box already holds and is commented as such — it should be deleted when this is fixed.

## Scope

Give `LocalDocumentContentStore` an `OpenReadVersionAsync` that resolves the same occurrence address the local custody adapter writes, so the two agree. Nothing about the Box path or the interface default changes.

## How to verify

A case accepted through intake under `DevelopmentOffline` serves its retained photographs on the Evidence tab and exports them, with no test-only content seeding.
````

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (this is a `Pegasus.Infrastructure` adapter fix, not desktop work); tests by `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `run-tests` (`dotnet/skills` `98f84851`, `plugins/dotnet-test/skills/run-tests/SKILL.md`) → `code-testing-agent` (`dotnet/skills` `98f84851`, `plugins/dotnet-test/skills/code-testing-agent/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for `FileStream` / `RandomAccess` async and share-mode semantics, if a read path needs confirming). No Azure MCP tool.
- **Kanmer pipeline** for profile `fix`: `kanmer-research` (optional) → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (`.grok/skills/<name>/SKILL.md`). Profile `fix` needs `files` + `plan` + questions-resolved to leave Preparing, `post-implementation-report` + questions-resolved to enter Review, and `proof` + questions-resolved to enter Done; call `get_doc_gates <this ticket id>` before every move and cross at most one gated boundary per move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient. Read this body in full, including the verbatim upstream block above, then `src/Pegasus.Core/Documents/DocumentContracts.cs:226-295`, `src/Pegasus.Infrastructure/Custody/BoxDocumentContentStore.cs:26-115`, `src/Pegasus.Infrastructure/Custody/LocalDocumentContentStore.cs` in full (183 lines) and `src/Pegasus.Infrastructure/Custody/LocalCaseCustody.cs:67-166` and `:305-318`. Call `get_doc_gates <this ticket id>`, then `take_ticket`, and work in this ticket's own worktree and branch cut from `origin/dev`.
2. Reproduce before fixing. Write a failing integration fact in `tests/Pegasus.IntegrationTests` beside `DocumentCustodyDurabilityTests.cs`: retain an intake source and one attachment through `LocalCaseCustody`, register the matching `DocumentOccurrence`/`DocumentVersion` rows the way `EfQueuedCustodyProcessor.RecordRetainedCaseFilesAsync` does (`EfQueuedCustodyProcessor.cs:362`, `:433-440`), then call `OpenReadVersionAsync` through `IDocumentContentStore`. Done looks like: the test fails with `System.IO.FileNotFoundException : The document content is unavailable.` — the exact message at `LocalDocumentContentStore.cs:97`. Record the failing output in `files`.
3. In `files`, write down both layouts side by side, from the code rather than from this body: what `LocalCaseCustody` writes (`cases/{caseId:N}/documents/{receiptId:N}/{hash}/content`; `.../attachments/{ordinal:D3}-{hash}/content`; `cases/{caseId:N}/images/{ordinal:000}-{receiptId:N}/content`, each beside a `metadata.json`) against what `LocalDocumentContentStore.Resolve` looks for (`cases/{safe case reference}/managed/{versionId:N}/content`), and the facts an `OpenReadVersionAsync` call actually has to work with: the `ManagedDocumentContentAddress` fields, plus `expectedSha256` and `expectedLength`. Note explicitly that the receipt id is **not** in the address — that is the crux, and the resolution strategy must account for it.
4. Decide the resolution strategy and record the decision and its reason in `plan` before writing it. It must be derivable from what a read is given, and must agree with what the adapter writes — the two candidate strategies are (a) resolving under `cases/{caseId:N}/` by the verified content hash and the occurrence ordinal, and (b) reading the sibling `metadata.json` the adapter writes to bind the file to its identity. Whichever is chosen, the Box store's `FlatFileName` (`BoxDocumentContentStore.cs:26-36`) is the reference for *how* an address is turned into a name — do not invent a third naming convention, and do not change the Box path or the interface default.
5. Implement the override on `LocalDocumentContentStore`, keeping every existing behaviour of the class: the same `ValidateIdentifiers` guard, the same `NormalizeSha256`, the same SHA-256 **and** length verification through `VerifyAsync` before the stream is returned, the same `FileStream` options (`FileOptions.Asynchronous | FileOptions.SequentialScan`, `FileShare.Read`), and the same outside-the-custody-root `UnauthorizedAccessException` guard that `Resolve` applies at `:145-150`.
6. **Keep the existing `managed/<versionId:N>` layout working.** Unlike Box — whose `OpenReadAsync` deliberately throws — the local store really does serve content written through `StoreAsync`: `EfDocumentCustodyStore.cs:117` (`StoreVersionAsync` → the interface default → `StoreAsync`) and `EfDocumentRequestStore.cs:281` both land there. The new override must serve the occurrence-addressed intake-retained file **and** fall back to the existing `versionId` path when that is where the content is, so a document uploaded through the gateway is not made unreadable by this fix. Done looks like: `DocumentCustodyDurabilityTests` and `EvaHandoffPersistenceTests` pass unchanged.
7. Preserve the failure contract. A genuinely absent file still throws `FileNotFoundException("The document content is unavailable.")` — the same type and the same message — and a present file whose hash or length disagrees still throws `InvalidDataException` from `VerifyAsync`. Add a fact for each so the fix cannot silently turn a missing file into an empty stream.
8. Turn the step 2 reproduction green, and add facts for the three shapes the adapter writes: the retained source (ordinal 1), an attachment (ordinal ≥ 2, which is also how promoted embedded photographs land — `EfQueuedCustodyProcessor.cs:286` and `:321`), and an image-case asset (`LocalCaseCustody.cs:148-152`), including one that has been folded into a case by `MergeImageCaseContentsAsync` (`:168-208`).
9. Cover the readers that were broken, at the level each is reachable locally: the case-document download and the export archive (`EfDocumentCustodyStore.cs:252`, `:589`), the assessment report projection source (`EfAssessmentReportProjectionSource.cs:89`) and the EVA handoff bundle (`EvaHandoffStore.cs:526`). Where a reader already has a local test, extend it with an intake-retained document rather than adding a parallel one.
10. **Re-expressed for the desktop world.** The upstream ticket's own verification is written against the web app's case Evidence tab, which is being retired. Verify instead through the layer that survives: the store and the reader facts above, plus the local stack. Under `DevelopmentOffline` with `Features:LocalIntake` and `Features:LocalDocumentCustody` (`src/Pegasus.Web/Program.cs:202`, `:660`), accept a case through intake, then read its retained document content back through the same use case the `/api/v1` case-document route will call — do not add an `/api/v1` route here, that is [[DSK-03-11]]'s.
11. Delete the test-only content seeding the upstream ticket names as a stand-in **only if it is present in the fork**. It was written for upstream CASE-019's `ExportingACaseProducesTheEvaFormatArchive`, and a grep of `tests/` on 2026-08-24 finds no such test on the fork — CASE-019 is `done` upstream and arrives with the sync [[DSK-01-10]] owns. Record which case applies; if the test is not here yet, note in `plan` that the seeding removal transfers to whoever lands that sync, and do not fabricate the test to have something to delete.
12. Update the documentation named under **Documentation changes**, run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in this ticket's `plan` document, then open the PR into `dev`.

## Acceptance criteria

- [ ] A case accepted through intake under `DevelopmentOffline` serves the content of its retained source, its attachments and its promoted photographs through `IDocumentContentStore.OpenReadVersionAsync`, with no test-only content seeding.
- [ ] The same case's document export archive and its EVA handoff bundle both build locally from that content.
- [ ] Content written through `StoreAsync` into the existing `cases/<safe case reference>/managed/<versionId:N>/content` layout is still served — no gateway-uploaded document is made unreadable by this change.
- [ ] SHA-256 and length are still verified on every read, the outside-the-custody-root guard still holds, an absent file still throws `FileNotFoundException("The document content is unavailable.")`, and a corrupted one still throws `InvalidDataException`.
- [ ] `BoxDocumentContentStore`, `IDocumentContentStore`'s default `OpenReadVersionAsync`, and `LocalCaseCustody`'s write layout are all unchanged — the local **read** is what moves.
- [ ] `docs/desktop/08-testing/test-uat-stack.md` no longer implies the local stack can serve intake-retained document content when it cannot.

## Verification

- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release` — expected: exit 0; the new local occurrence-read facts pass and `DocumentCustodyDurabilityTests`, `EvaHandoffPersistenceTests`, `CaseCustodyWebTests` and `BoxDocumentContentStoreTests` pass with no assertion changed.
- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release` — expected: exit 0.
- [ ] `dotnet build ./Pegasus.slnx --configuration Release` — expected: exit 0, no new warning.
- [ ] `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start` then `-Action Smoke` — expected: exit 0; then a case accepted through local intake returns its retained document content on the case-document read path instead of `The document content is unavailable.`
- [ ] `git diff --stat origin/dev...HEAD` — expected: `src/Pegasus.Infrastructure/Custody/LocalDocumentContentStore.cs` and the test projects only; `BoxDocumentContentStore.cs`, `LocalCaseCustody.cs` and `src/Pegasus.Core/Documents/DocumentContracts.cs` unmodified.

## Evidence tier

Tier 3 — Parser/adapter contracts, with Tier 12 — Integrated workflow.
Tier 3 obliges the adapter's contract to hold exactly as before around the new resolution: path/integrity safety (the custody-root guard), hash and length verification, deterministic failure with a stable message, and cancellation honoured. Tier 12 obliges the whole path — a source receipt accepted through Core, the Worker's custody retention, the persisted document records, and an operator-visible read of the same bytes back — rather than a store-level test alone; registration-only or seeded-content paths do not satisfy it, which is the entire point of this ticket.

## Documentation changes

- `docs/desktop/08-testing/test-uat-stack.md` — § Components (line 27) and § "Known gaps (record, do not hide)" (line 173): record that intake-retained document content is served locally once this lands, and until it does, record the gap there rather than leaving it unstated. [[DSK-08-17]] owns that file's lifecycle sections; keep this edit to the two places named.
- `docs/current-architecture.md` — only if the stated local-adapter behaviour changes; line 528 describes the flat case-folder custody and the Evidence gallery reading document records.
- `docs/frd/frd-05-documents-extraction-and-custody.md` — no change. This fix brings the local adapter into line with rules the FRD already states; if implementing it appears to need an FRD change, stop and raise it rather than editing the FRD.
- This ticket's `files`, `plan` and `proof` documents — the two layouts, the chosen resolution strategy and its reason, and the local run that proves it.

## Guardrails

- **Azure**: no write, and no Azure MCP call. Asking for an Azure test resource to work around this is out of bounds under **L-02** and ADR-0014 — that is the constraint that makes this ticket necessary in the first place.
- **Scope boundary**: may touch `src/Pegasus.Infrastructure/Custody/LocalDocumentContentStore.cs` and the test projects (`tests/Pegasus.IntegrationTests`, `tests/Pegasus.Core.Tests`). **Must not** change `src/Pegasus.Core/Documents/DocumentContracts.cs` (the interface default), `src/Pegasus.Infrastructure/Custody/BoxDocumentContentStore.cs` (the production path) or `src/Pegasus.Infrastructure/Custody/LocalCaseCustody.cs` (the write layout) — the upstream scope is explicit that "Nothing about the Box path or the interface default changes", and changing the write layout instead of the read would invalidate every artifact root already on disk. **Must not** add an `/api/v1` route or contract ([[DSK-03-11]] owns those). **Must not** touch `src/Pegasus.Worker` ([[DSK-07-01]] states "No Worker code is written or changed").
- **Blocks these seeded board tickets** — none of them can be evidenced, and therefore cannot correctly ship, while the only permitted verification environment cannot serve intake-retained bytes: [[DSK-08-17]] (its `Smoke` verb and the whole Test/UAT stack are relied on as the evidence environment — sequence this ticket **before** `Smoke` is relied on), [[DSK-05-14]] (documents, custody and the transfer queue), [[DSK-05-16]] (the image gallery and its viewer, whose acceptance is measured on the local workstation), [[DSK-07-06]] (the desktop document browser, transfer queue, preview pane and bounded working cache), and [[DSK-03-11]] (the case document, custody-retry, export and EVA-handoff endpoints, whose local integration evidence reads the same content). A later pass wires these as `blocks` links.
- **Blocked by**: nothing. This ticket has no dependency and can start immediately — it is the smallest change in the carry-over set with the widest evidence unblock.
- **Neighbouring import — do not collide**: the imported upstream PLAT-032 sweeps the same `Custody/` folder for duplicate routes. If both are in flight, land this one first: it is one file and one method, and PLAT-032's sweep should read the settled shape.
- **Traps**: production is unaffected and must stay that way — a change that alters what Box resolves is a stop condition. The local store serves **both** layouts after this change; dropping the `managed/<versionId:N>` fallback would break gateway-uploaded documents, which is a worse defect than the one being fixed. Do not weaken the hash or length verification to make a path resolve. Do not add a marker or binding sidecar file to make resolution easier — `docs/current-architecture.md:528` records that the marker-file approach was deliberately removed by an operator decision. Never seed content into the local store to make a test pass; that stand-in is the thing this ticket exists to delete.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in this ticket's `plan` document.

## Outcome

_Filled at closeout._
