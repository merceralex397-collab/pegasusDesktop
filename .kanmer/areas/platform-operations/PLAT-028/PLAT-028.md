---
id: PLAT-028
type: ticket
title: >-
  upstream:PLAT-032 · Simplification and duplicate-route sweep across the
  codebase
status: review
area: platform-operations
order: 70
assignee: codex-mcp-client
profile: chore
stageEntered:
  preparing: '2026-08-24T21:21:17.135Z'
  review: '2026-08-25T07:31:52.654Z'
taken_at: '2026-08-25T07:09:58.676Z'
branch: task/plat-028-duplicate-route-sweep
worktree: ../pegasus-worktrees/plat-028-duplicate-route-sweep
labels:
  - simplification
  - upstream-carryover
  - upstream-PLAT-032
  - gateway-worker-ticket
groups:
  - EPIC-014
links: []
blocks:
  - GWY-010
  - GWY-011
  - GWY-012
docs_todo: true
archived: false
created: '2026-08-24T11:47:25.327Z'
updated: '2026-08-26T11:16:57.522Z'
---

## What

Carry out upstream PLAT-032's duplicate-route sweep on the **server side the desktop conversion reuses unchanged** — roster items 1–5, verbatim — across `src/Pegasus.Core` and `src/Pegasus.Infrastructure`, removing each duplicate route or dead definition and recording every finding in this ticket's `plan` document before the PR. Roster item 6 (the 1,025-line `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs`) is **moot on the fork and explicitly out of scope**: it is a Razor page model on the cut list, replaced by [[DSK-05-10]] / [[DSK-03-12]] and deleted by [[DSK-05-26]].

## Why

The desktop conversion inherits these duplications whole. `docs/desktop/05-implementation-and-migration/reuse-map.md:58` marks `Intake/` (`MimeKitPdfPigOpenXmlIntakeSourceReader.cs` 1,233 + `.DocMsg.cs` 289) **REUSE server-side**, and `:60` marks `Custody/` (`BoxCaseCustody.cs` 1,016; `LocalCaseCustody` 549; `BoxDocumentContentStore` 240; `LocalDocumentContentStore` 183) **REUSE server-side** — the exact files carrying roster items 2, 3 and 4. Five of the six roster items therefore survive the conversion untouched, and no seeded conversion ticket is permitted to remove them:

- [[DSK-03-10]]'s scope boundary reads "Must not touch `src/Pegasus.Core/Intake/**`, the Worker, or `src/Pegasus.Web/Pages/Intake/**`".
- [[DSK-03-11]]'s confines it to `src/Pegasus.Web/Api/**`, `src/Pegasus.Contracts/Uploads/**`, `openapi/`, the generated client and the test projects.
- [[DSK-05-14]] and [[DSK-07-06]] both forbid any desktop project from referencing `src/Pegasus.Infrastructure/Custody/` at all, and [[DSK-02-12]]'s architecture test enforces it.

It gets harder, not easier, if it waits. [[DSK-03-12]] projects the folder-move record onto `POST /api/v1/mail/{id}/move-to-recommended-folder` ("returns the move record") and into the generated client, which would freeze roster item 5's four never-read echo fields into a versioned contract. [[DSK-03-10]] and [[DSK-03-11]] add `/api/v1` callers over the same intake and custody stores. Once a generated client is built over a duplicated surface, removing the duplicate stops being hygiene and becomes a contract change.

The convention that would normally catch this cannot reach it. [[DSK-00-11]] enforces a per-branch dated `## Simplification pass`, which is scoped to the branch **diff** and by construction cannot see duplication that predates the diff. Only a deliberate sweep finds it. Roster item 4 is the case that already cost the programme once: upstream INTK-030 (`done`) was precisely the drift between the two inline-image classifications this ticket collapses.

Operator direction on the upstream ticket — "check for excessive bloat, duplicate callers and functions, and make document parsing, retrievals and similar all use the same routes wherever possible" — is preserved in full. Only the Razor half is dropped, and only because the front end that carried it is being deleted.

## Source of truth

- **Upstream provenance** — upstream area `platform-operations`; upstream status `backlog`; upstream profile `chore`; upstream labels `simplification`; upstream links `DOCS-007`, `INTK-030`, `PR-039`, `PR-043`, `PR-044`. Read in full from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at clone commit `a5b28111`, read date **2026-08-24**. The upstream ticket carries no `research`, `files`, `plan`, `checklist` or `open-questions` document — nothing was omitted in the copy.
- **Carry-over register row**: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:178` — disposition `gateway-worker-ticket (hygiene; overlaps the reuse/cut map)`, target area plan 05, fork area `platform-operations`. That "overlaps the reuse/cut map" parenthetical holds for exactly one of the six roster items; the scope note under **Guardrails** records why.
- **Reuse/cut classification**: `docs/desktop/05-implementation-and-migration/reuse-map.md:58` (`Intake/` REUSE server-side), `:60` (`Custody/` REUSE server-side), `:76` (53 Razor page models — REPLACE by desktop screens, CUT after cutover; `Pages/Mail/Message.cshtml.cs` 1,025 named as the largest).
- **Repository evidence** (every path and line confirmed in the fork tree on 2026-08-24):
  - `src/Pegasus.Infrastructure/DependencyInjection.cs:355-375` (the local composition: `FileSystemIntakeArtifactStore`, `LocalDocumentContentStore`, `LocalCaseCustody`) and `:543` (`BoxDocumentContentStore`) — roster item 1's two content-store routes.
  - `src/Pegasus.Core/Intake/InstructionEvidenceImages.cs:15` (`InstructionEvidenceImages`) and `:112` (`ICaseEvidenceImageQueries`); `src/Pegasus.Infrastructure/Persistence/EfIntakeReceiptStore.cs:13` (the implementation) and `:1423` (the selection call); `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs:81` and `:449` (the EVA store's own `DocumentOccurrenceEntity` query); `src/Pegasus.Infrastructure/DependencyInjection.cs:68` (the registration) — roster item 2's three definitions.
  - `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs:563`/`:571` → `:580`, `:601`/`:610` → `:620`, `:641`/`:650` → `:660`; the lease-guard default interface methods at `src/Pegasus.Core/Custody/CustodyContracts.cs:112`, `:138`, `:199`; `src/Pegasus.Infrastructure/Custody/LocalCaseCustody.cs:67`, `:99`, `:134` — roster item 3's overload pairs.
  - `src/Pegasus.Infrastructure/Intake/MimeKitPdfPigOpenXmlIntakeSourceReader.cs:861-865` and `src/Pegasus.Infrastructure/Intake/MimeKitPdfPigOpenXmlIntakeSourceReader.DocMsg.cs:234-235` — roster item 4's twice-written `isInlineImage` predicate, both feeding `IntakeAssetKind.InlineImage` / `IntakeAssetDisposition.Inline`.
  - `src/Pegasus.Core/Intake/RetainedMailFolderMove.cs:22-33` (`RetainedMailFolderMoveResult`), `src/Pegasus.Infrastructure/Persistence/EfRetainedMailFolderMoveStore.cs:303-306` (the four echo fields written), `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:541` (the only consumer, which reads `Outcome`) — roster item 5.
  - `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs` is 1,025 lines in the fork today — roster item 6, moot.
- **Already landed on the fork**: DOCS-007 (`done` upstream) is in the fork's history at `fef817b8` ("Put case files flat in the case folder and record them") and `f0d8b6eb` ("Serve case evidence from Box, and cover audit custody"), both verified ancestors of `main` on 2026-08-24. Roster items 1 and 2 are therefore the *confirm, then delete whatever became dead* half of DOCS-007, not a re-run of it.
- **Governing documents**: `docs/engineering.md` § One Core owner — "A business rule, classifier, allocator, parser, workflow transition, or external effect has one implementation… On encountering a third implementation, stop and consolidate; migrate or delete the replaced code, registrations, tests, and documentation in the same slice." That is the rule this ticket enforces. `docs/principal-rules-and-mappings/qdos.md:167` and `:188`, and `docs/current-architecture.md:528`, name `InstructionEvidenceImages` as the documented owner of the evidence-image selection.
- **Binding decisions**: **L-01** — the gateway is `Pegasus.Web` evolved in place, so this server-side code is production code the desktop era keeps, not throwaway. **D-001** — the fork becomes the single release source at the first production gateway change and upstream is then frozen, so this sweep will never arrive by sync. **L-05** — the fork board is the single work register.
- **Depends on**: `None.`

### Upstream ticket PLAT-032 (verbatim)

````
## Why

Operator direction: check for excessive bloat, duplicate callers and functions, and make document parsing, retrievals and similar all use the same routes wherever possible.

Operator also decided this ships **separately, after** the QDOS26008 regression fixes land, so a broad refactor cannot destabilise or delay them.

## Starting roster — already evidenced

1. **Two content-store routes for the same evidence** — the intake artifact blob store versus the Box document store. Largely closed by [[DOCS-007]]; verify nothing re-introduces a second path.
2. **Three definitions of "the case's images"** — `InstructionEvidenceImages`, `ICaseEvidenceImageQueries`, and the EVA store's own `DocumentOccurrence` query. Converged by [[DOCS-007]]; confirm and delete whichever becomes dead.
3. **`RetainAccepted*` overload pairs** duplicated across `BoxCaseCustody` and `LocalCaseCustody` — four near-identical wrappers differing only by lease guard.
4. **Inline-image classification written twice** in the MIME reader (`MimeKitPdfPigOpenXmlIntakeSourceReader.cs:862` and `.DocMsg.cs:234`) — precisely the kind of drift that produced [[INTK-030]].

## Added by the Release 17 review of the `codex-mcp-client` tickets (2026-08-21)

5. **`RetainedMailFolderMoveResult` carries four fields nothing reads.**
   `ExpectedClassificationVersion`, `ExpectedRecommendationPolicyKey`,
   `ExpectedRecommendationPolicyVersion` and `ExpectedMailboxVersion` are written at
   `EfRetainedMailFolderMoveStore.cs:303-306` from the persisted operation row and read
   by no caller: the only consumer, `Message.cshtml.cs:541`, reads `Outcome`. They echo
   the request's own expectations back out of a public Core contract. `IsReplay`,
   `OperationKey` and `FailureReason` on the same record need the same check. Shipped by
   [[PR-039]] / [[PR-043]] / [[PR-044]] (tick-049); behaviour is correct, the surface is
   larger than its callers.

6. **`Mail/Message.cshtml.cs` is 1,025 lines** — 49% larger than the next-biggest page
   model (`Cases/Create.cshtml.cs`, 689) — carrying link, unlink, folder-move,
   classification-correction and case-search handlers with their lease preparation. Not a
   defect and not a blocker; a split candidate if it grows again.

## Cleared by that review — checked, not defects

- PR #477's headline **+8,996 lines is 7,116 lines of generated EF `Designer.cs`**. Real
  new code across all four codex branches is ≈2,500 lines, which is proportionate.
- The `Succeeded` / `Failed` / **`Uncertain`** move taxonomy is required by the runbook's
  recovery rules, not gold-plating.
- The mailbox and folder `<nav>` elements on `Mail/Index.cshtml` are navigation with
  `aria-label`s, not pill rows standing in for a filter dropdown — the view filter already
  uses a labelled `select`.

## Already fixed rather than deferred

Two duplications found during Release 17 were fixed in place, because the work had to
touch every copy anyway and leaving one stale would have been a live defect:

- the **terminal-state vocabulary**, written three times (`CaseLifecycleRules.IsTerminal`,
  `EvaHandoffStore`, `EfVehicleWorkflowStore`) — [[INTK-029]];
- **stopping a case's chase schedule**, written four times across the workflow, case-data,
  replacement and intake-mutation stores — now `CaseChaseState.Stop`.

## Constraint

The `code-simplifier` subagent is unavailable under the standing no-subagents constraint, so the pass is done directly. Per repo convention the findings are recorded in this ticket's plan/research before the PR.
````

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (this is `Pegasus.Core` / `Pegasus.Infrastructure` work, not desktop work); tests by `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `run-tests` (`dotnet/skills` `98f84851`, `plugins/dotnet-test/skills/run-tests/SKILL.md`) → `test-gap-analysis` (`dotnet/skills` `98f84851`, `plugins/dotnet-test/skills/test-gap-analysis/SKILL.md`, before deleting any covered path) → `code-testing-agent` (`dotnet/skills` `98f84851`, `plugins/dotnet-test/skills/code-testing-agent/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) only if a .NET or EF Core semantic needs confirming. No Azure MCP tool.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (`.grok/skills/<name>/SKILL.md`). Profile `chore` needs `plan` + questions-resolved to leave Preparing and `proof` + questions-resolved to enter Done; call `get_doc_gates <this ticket id>` before every move and cross at most one gated boundary per move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient. Read this body in full, including the verbatim upstream block above, then `docs/desktop/05-implementation-and-migration/reuse-map.md` lines 58, 60 and 76, and `docs/engineering.md` § One Core owner. Call `get_doc_gates <this ticket id>`, then `take_ticket`, and work in this ticket's own worktree and branch cut from `origin/dev`.
2. Load `pegasus-desktop`, then `run-tests`. Open the `plan` document and record the scope up front: roster items 1–5 in scope; roster item 6 out of scope and why (`Pages/Mail/Message.cshtml.cs` is on the cut list at `reuse-map.md:76`; [[DSK-05-10]] and [[DSK-03-12]] replace it and [[DSK-05-26]] deletes it — splitting a page model that is being deleted is wasted work). Per the upstream ticket's own convention, every finding below is recorded in this document before the PR, whether it results in a change or not.
3. **Roster item 1 — one content-store route, not two.** DOCS-007 has already landed on the fork (`fef817b8`, `f0d8b6eb`). Confirm no second route has re-appeared: read `src/Pegasus.Infrastructure/DependencyInjection.cs:355-375` and `:543` and list every composition of `IIntakeArtifactStore` and `IDocumentContentStore`; then grep the solution for callers that read evidence bytes and record, per caller, which store it goes through. Done looks like: a table in `plan` naming each byte-reading caller and its single store, or a named second path with the change that removes it.
4. **Roster item 2 — three definitions of "the case's images".** `InstructionEvidenceImages` (`src/Pegasus.Core/Intake/InstructionEvidenceImages.cs:15`) is the documented Core owner — `docs/principal-rules-and-mappings/qdos.md:167` and `:188` and `docs/current-architecture.md:528` name it — so it is **not** a deletion candidate. Determine whether `ICaseEvidenceImageQueries` (`:112`, implemented by `src/Pegasus.Infrastructure/Persistence/EfIntakeReceiptStore.cs:13`, registered at `DependencyInjection.cs:68`, consumed today only by `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs:26`) or the EVA store's own occurrence query (`src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs:81`, `:449`) became dead once DOCS-007 made the case Evidence gallery read document records. **Re-expressed for the desktop world**: a port whose only consumer is a Razor page model is not automatically dead — [[DSK-03-07]] and [[DSK-03-10]] may need the same projection on `/api/v1`. Before deleting, check the seeded gateway tickets' endpoint rows and record the answer; delete only what nothing on either side needs, and delete its registration, its tests and its documentation line in the same change.
5. **Roster item 3 — the `RetainAccepted*` overload pairs.** Re-verify the shape before changing anything: today `BoxCaseCustody` carries three public pairs (`:563`/`:571`, `:601`/`:610`, `:641`/`:650`) each delegating to a private `…CoreAsync` (`:580`, `:620`, `:660`), while `LocalCaseCustody` implements only the guardless overloads (`:67`, `:99`, `:134`) and inherits the lease-guard variants from the default interface methods on `src/Pegasus.Core/Custody/CustodyContracts.cs:112`, `:138`, `:199`. Record in `plan` whether the four near-identical wrappers upstream described still exist in that form; collapse to one wrapper per operation where they do, keeping `CustodyEffectLeaseGuard.RequireCurrentAsync` invoked immediately before each remote mutation. Done looks like: `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj` green, with `CaseCustodyWebTests`, `LocalCaseCustodyAtomicWriteTests`, `ImageCaseCustodyIntegrationTests`, `CustodyOutboxIntegrationTests` and `ProductionBoxCustodyTests` unchanged in behaviour.
6. **Roster item 4 — one inline-image classification.** `src/Pegasus.Infrastructure/Intake/MimeKitPdfPigOpenXmlIntakeSourceReader.cs:861-865` computes `isInlineImage` from a MimeKit `ContentDisposition` plus `ContentId`; `MimeKitPdfPigOpenXmlIntakeSourceReader.DocMsg.cs:234-235` computes the same concept from the DOC/MSG attachment's `IsInline` plus `ContentId`. Extract one named rule that both call — same file family, one owner — taking the already-detected `SourceFormat` and the two disposition inputs, so the two call sites become one expression each and both keep producing `IntakeAssetKind.InlineImage` / `IntakeAssetDisposition.Inline` identically. This is the drift that produced upstream INTK-030; state that in the code comment. Done looks like: the EML and DOC/MSG asset-classification facts under `tests/Pegasus.IntegrationTests/DocumentExtraction/` stay green with no assertion changed.
7. **Roster item 5 — the four never-read echo fields.** `RetainedMailFolderMoveResult` (`src/Pegasus.Core/Intake/RetainedMailFolderMove.cs:22-33`) carries `ExpectedClassificationVersion`, `ExpectedRecommendationPolicyKey`, `ExpectedRecommendationPolicyVersion` and `ExpectedMailboxVersion`, written at `src/Pegasus.Infrastructure/Persistence/EfRetainedMailFolderMoveStore.cs:303-306` and read by nobody; check `IsReplay`, `OperationKey` and `FailureReason` the same way. **Re-expressed for the desktop world**: the upstream ticket reasons from the Razor consumer (`Pages/Mail/Message.cshtml.cs:541` reads only `Outcome`), and that page model is being deleted — so the decisive consumer is the future one. [[DSK-03-12]] step for `POST /mail/{id}/move-to-recommended-folder` says it "returns the move record", and [[DSK-07-03]] and [[DSK-07-11]] consume it. Decide the record's shape **now**, before that endpoint and its generated client exist, and record the decision and its reason in `plan`: keep a field only if a named desktop or gateway consumer needs it, and remove the rest from the Core contract and from the store write in the same change.
8. Re-run the sweep's own question once, cheaply, over the two REUSE folders the conversion inherits: `src/Pegasus.Core` and `src/Pegasus.Infrastructure`. Record any further duplicate route or duplicate caller found as a **finding only** — do not widen this ticket. Anything new goes to the board as its own row (`AGENTS.md`), and anything that turns out to be a Razor-only duplicate is recorded as moot with the cut-list line that supersedes it.
9. If the sweep produces a durable structural rule worth enforcing rather than a one-off deletion (for example "one inline-image classification", "one evidence-image owner"), do **not** create a rival test project: [[DSK-02-12]] owns `DependencyDirectionTests` and the desktop-boundary assertions. Raise the assertion there, naming this ticket, and record in `plan` that you did. The coverage decision that scoped this import also flags folding the upstream ticket's Web-composition assertion into [[DSK-02-12]]'s acceptance as optional — treat it as optional, and do not invent one.
10. Update the documentation named under **Documentation changes** in the same change as the code, per `docs/engineering.md` § One Core owner ("migrate or delete the replaced code, registrations, tests, and documentation in the same slice").
11. Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in this ticket's `plan` document before opening the PR into `dev` ([[DSK-00-11]] enforces the heading). Note in that section that this ticket is the sweep the per-branch convention cannot perform.

## Acceptance criteria

- [ ] Roster items 1, 2, 3, 4 and 5 each have a recorded outcome in the `plan` document — a change made, or a finding that the duplication no longer exists, with the evidence for it.
- [ ] There is exactly one inline-image classification rule, called from both `MimeKitPdfPigOpenXmlIntakeSourceReader.cs` and `MimeKitPdfPigOpenXmlIntakeSourceReader.DocMsg.cs`, and it names upstream INTK-030 as the drift it prevents.
- [ ] `RetainedMailFolderMoveResult`'s shape is decided against a named future consumer, not against the deleted Razor page model, and every field it still carries has one.
- [ ] Nothing deleted leaves a dangling registration, test or documentation line — `docs/engineering.md` § One Core owner's same-slice rule holds.
- [ ] Roster item 6 is recorded as moot with the cut-list line that supersedes it, and `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs` is not split, refactored or otherwise touched.
- [ ] No behaviour changes: every existing test passes with no assertion rewritten to accommodate a deletion.

## Verification

- [ ] `dotnet build ./Pegasus.slnx --configuration Release` — expected: exit 0, no new warning.
- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release` — expected: exit 0.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release` — expected: exit 0; the custody, EVA handoff and DOC/MSG/EML extraction suites pass with no assertion changed.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` — expected: exit 0; dependency direction and one-policy-owner rules still hold.
- [ ] `pwsh ./scripts/Test-PegasusPlatform.ps1` — expected: exit 0.
- [ ] `git diff --stat origin/dev...HEAD` — expected: no file under `src/Pegasus.Web/Pages/` is modified, and `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs` in particular is untouched.

## Evidence tier

Tier 1 — Static/build/architecture, with Tier 3 — Parser/adapter contracts for roster item 4.
Tier 1 obliges that the four approved projects compile, that dependency direction and one-policy-owner rules still hold after each deletion, and that no registration is left dangling. Tier 3 obliges that collapsing the two inline-image classifications changes no EML or DOC/MSG asset outcome: the same files are classified `InlineImage` versus `Attachment` as before, proved through the existing extraction contract tests rather than by inspection.

## Documentation changes

- `docs/desktop/05-implementation-and-migration/reuse-map.md` — update the `Intake/` (line 58) and `Custody/` (line 60) rows if a named type or file listed there is removed, so the REUSE inventory stays true.
- `docs/current-architecture.md` — only if a named Core owner or adapter route changes; line 528 describes the evidence-image selection and the flat case-folder custody this sweep touches.
- `docs/principal-rules-and-mappings/qdos.md:167`, `:188` — only if the evidence-image ownership statement changes (it should not; `InstructionEvidenceImages` stays the owner).
- This ticket's `plan` document — the sweep's findings, per the upstream ticket's own convention ("the findings are recorded in this ticket's plan/research before the PR").
- `None.` beyond the above — this ticket must not open an ADR or FRD; nothing here changes a governing decision.

## Guardrails

- **Azure**: no write, and no Azure MCP call. Nothing in this ticket touches an Azure resource.
- **Scope boundary**: may touch `src/Pegasus.Core`, `src/Pegasus.Infrastructure` and the three test projects (`tests/Pegasus.Core.Tests`, `tests/Pegasus.IntegrationTests`, `tests/Pegasus.ArchitectureTests`). **Must not** touch `src/Pegasus.Web/Pages/**` — including `Pages/Mail/Message.cshtml.cs`, roster item 6, which is moot via the cut list. **Must not** add `/api/v1` routes or contracts ([[DSK-03-10]], [[DSK-03-11]], [[DSK-03-12]] own those). **Must not** change `src/Pegasus.Worker` ([[DSK-07-01]] states "No Worker code is written or changed"). **Must not** change behaviour: this is a duplicate-route sweep, not a redesign, and a test assertion rewritten to accommodate a deletion is a stop condition.
- **Blocks these seeded board tickets** — they cannot correctly ship until this lands, because each one builds a versioned `/api/v1` contract and a generated client over the duplicated surface: [[DSK-03-10]] (intake endpoints over the same intake stores and the reader's asset classification), [[DSK-03-11]] (upload, document, custody-retry, export and EVA-handoff endpoints over the same custody stores), [[DSK-03-12]] (the mail folder-move endpoint, which "returns the move record" — roster item 5's contract). Sequence this ticket before all three.
- **Blocked by**: nothing. This ticket has no dependency and can start immediately.
- **Neighbouring imports — do not collide**: the imported upstream INTK-002 owns the intake duplication chores (adapter-wide fault naming, the one intake decision-code table, the Web-composition assertion, the leftover port). Where a finding here is an intake decision-code or fault-naming duplicate, record it and leave it to that ticket rather than fixing it twice. The imported upstream PLAT-038 owns the local-profile document-content read path in the same `Custody/` folder; if both are in flight, sequence PLAT-038 first — it is a smaller, load-bearing change to one file.
- **Traps**: `InstructionEvidenceImages` is the documented Core owner of the evidence-image selection and is not a deletion candidate. A port whose only consumer is a Razor page model is not automatically dead — the gateway tickets may need the same projection, so check before deleting. `CustodyEffectLeaseGuard.RequireCurrentAsync` must still be invoked immediately before every remote custody mutation after any wrapper is collapsed. The upstream operator decision that this work ships **separately, after** the regression fixes it was queued behind still applies in spirit: do not bundle this sweep into another ticket's branch.
- **Upstream constraint carried across**: the upstream ticket records "The `code-simplifier` subagent is unavailable under the standing no-subagents constraint, so the pass is done directly." That constraint is upstream's, not the fork's — this ticket routes to `pegasus-gateway-dev` per L-04. Record the substitution rather than silently ignoring the sentence.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in this ticket's `plan` document.

## Outcome

_Filled at closeout._
