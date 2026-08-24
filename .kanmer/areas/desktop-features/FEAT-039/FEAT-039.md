---
id: FEAT-039
type: ticket
title: >-
  DSK-07-13 · Share the report templates once: embed the governed source into
  both renderer assemblies, hash-checked
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-07
  - phase-7
  - tier-1
groups:
  - EPIC-008
  - HZN-008
links: []
refs:
  - docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md
docs_todo: true
archived: false
created: '2026-08-24T08:30:09.616Z'
updated: '2026-08-24T08:30:09.616Z'
---

## What

Make one governed template set feed two renderers. Move the report-asset `EmbeddedResource` block into a shared MSBuild props file so `Pegasus.Infrastructure` and the new `Pegasus.Desktop.Infrastructure` embed **byte-identical** resources from `docs/design/assets/report-renderer/`, and add a resource-hash test in both test projects that fails on drift.

## Why

This area's § 7 names template duplication as a trap: a second copy of the Scriban and CSS set in the desktop would break the one-list rule (`AGENTS.md` § Simplicity rails — "a label table, a precedence order lives in exactly one place"). Proposal § 12.5 requires deterministic tests comparing key text, values and layout against approved fixtures — which is only meaningful if both renderers consume the same bytes. `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj` already embeds them by relative path from `docs/design/assets/report-renderer/templates/` with explicit `LogicalName`s; the desktop must reuse that mechanism rather than copy files. Sibling: [[DSK-07-14]] cannot render faithfully without this, and [[DSK-07-15]] cannot attribute a golden-file failure to the renderer if the inputs might differ.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-13`
- Plan context: `docs/desktop/07-integrations/README.md` § 7 Risks and traps ("Template duplication")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.5 Documents, PDFs and reports, § 21.1 Build properties
- Repository evidence: `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj` — the `EmbeddedResource` items for `..\..\docs\design\assets\report-renderer\templates\assessment_report.scriban`, `assessment_fee_note.scriban`, `report.css`, plus `..\..\docs\design\brand\logos\logo_no_margin.png` (linked to `Reports\Assets\brand\logo.png`) and `..\..\docs\design\brand\signatures\andy_patterson.png`, each with an explicit `LogicalName` under `Pegasus.Infrastructure.Reports.Assets.*`; `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:305-315` (`ResourceStream` composes `Pegasus.Infrastructure.Reports.Assets.{suffix}`), `:104-106` (`templates.{name}` lookup), `:146` (`templates.report.css`); `docs/design/assets/report-renderer/templates/` — seven `.scriban` files plus `report.css`; `.gitattributes` (LF pinning for those assets)
- Binding decisions: L-03 — both renderers exist at once until parity passes, which is precisely why the assets must be shared rather than duplicated. C-01 — private-repository Windows runner minutes bill at 2×, so the drift check must be a cheap test, not a new CI job.
- Depends on: `DSK-02-06` the `src/Pegasus.Desktop.Infrastructure` project; `DSK-02-02` central package management and the `Directory.Build.props` conventions

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `directory-build-organization` (dotnet/skills `98f84851`, plugin `dotnet-msbuild`) → `run-tests` (dotnet/skills `98f84851`, plugin `dotnet-test`) → `binlog-failure-analysis` (dotnet/skills `98f84851`) if the build misbehaves
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for MSBuild `EmbeddedResource` `LogicalName` and shared `.props` import semantics)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, this area's § 7 duplication trap, and the `EmbeddedResource` block in `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj`. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-13-shared-report-templates`.
2. Inventory the governed source exactly: `ls docs/design/assets/report-renderer/templates/` returns seven `.scriban` files (`advert_evidence_pack`, `assessment_fee_note`, `assessment_report`, `expert_report`, `fee_note`, `market_valuation_evidence`) plus `report.css`. Record in `research` which of them `Pegasus.Infrastructure` embeds **today** — only `assessment_report.scriban`, `assessment_fee_note.scriban` and `report.css` — and treat that as the set the desktop must match. Adding the other four is a scope change and belongs to [[DSK-07-17]]'s disposition work, not here.
3. Create `eng/build/ReportAssets.props` (or the equivalent location the `directory-build-organization` skill recommends for this repository's layout — record the choice) containing exactly the five `EmbeddedResource` items currently in `Pegasus.Infrastructure.csproj`, with their paths made relative to `$(MSBuildThisFileDirectory)` and their `LogicalName`s **parameterised by an assembly-specific prefix property**, defaulting to `Pegasus.Infrastructure.Reports.Assets`.
4. Import the props file from `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj`, delete the now-duplicated item group, and confirm the logical names are byte-for-byte the same as before: `Pegasus.Infrastructure.Reports.Assets.templates.assessment_report.scriban`, `...templates.assessment_fee_note.scriban`, `...templates.report.css`, `...brand.logo.png`, `...brand.signatures.andy_patterson.png`. If any name changes, `PlaywrightAssessmentReportRenderer.ResourceStream` breaks at runtime.
5. Import the same props file from `src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj` with the prefix property set to that assembly's own namespace, so the desktop renderer in [[DSK-07-14]] resolves the same suffixes (`templates.assessment_report.scriban` and so on) through its own `ResourceStream` equivalent.
6. Do not copy any asset file into `src/`. The single source stays `docs/design/assets/report-renderer/` and `docs/design/brand/`; the props file references them by relative path exactly as the existing csproj does.
7. Add a resource-hash test to `tests/Pegasus.IntegrationTests` (or `tests/Pegasus.Core.Tests` if it can run without a database — prefer the cheaper project) that reads every embedded report asset from `Pegasus.Infrastructure` and asserts its SHA-256 against the hash of the on-disk governed file. A changed template without a reviewed hash update fails the build.
8. Add the mirror test to `tests/Pegasus.Desktop.ViewModelTests` (or the desktop test project [[DSK-08-04]] establishes): read the same five suffixes from `Pegasus.Desktop.Infrastructure` and assert each hash **equals the hash of the same resource in `Pegasus.Infrastructure`**. Byte-identical is the acceptance property, not "both present".
9. Guard the LF pinning: the `.gitattributes` entries keep these assets LF, and a CRLF checkout would change every hash. Assert in the test that the embedded `.scriban` and `.css` bytes contain no `\r\n` sequence, so a mis-normalised checkout fails loudly rather than producing a mysterious render diff.
10. Build both assemblies and run the two tests. Expected: `dotnet build ./src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj -c Release` succeeds and the existing `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` browser-category tests still render — proving the logical names survived the refactor.
11. Confirm no new CI job is added: the drift check runs inside existing test lanes (`sql-integration` and the desktop lane), because C-01 makes private-repository Windows minutes a live cost.
12. Run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the plan document, then open the PR into `dev`.

## Acceptance criteria

- [ ] One MSBuild props file holds the report-asset item group; both assemblies import it and no asset is copied into `src/`.
- [ ] `Pegasus.Infrastructure`'s existing logical resource names are unchanged, proven by the existing renderer tests still passing.
- [ ] Both assemblies embed byte-identical assets for the same five suffixes, proven by an equal-hash test.
- [ ] A change to a governed template without a reviewed hash update fails a test.
- [ ] The embedded text assets contain no CRLF, so a mis-normalised checkout fails loudly.
- [ ] No new CI job is introduced.

## Verification

- [ ] `dotnet build ./src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj -c Release` and `dotnet build ./src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj -c Release` — expected: both succeed.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: the resource-hash and no-CRLF facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category=Browser&Category!=Corpus" -- xUnit.MaxParallelThreads=2` — expected: `AssessmentReportRendererTests` still renders, proving the logical names are intact.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` — expected: the cross-assembly equal-hash fact passes.

## Evidence tier

Tier 1 — Static/build/architecture.
Tier 1 obliges build-level consistency evidence: both projects compile, the dependency direction is unchanged, and no second copy of a governed asset exists in the tree.

## Documentation changes

- `docs/design/assets/report-renderer/README.md` (or the governing note in `docs/design/README.md`) — record that two assemblies now embed the same source and that a hash test enforces it
- `docs/current-architecture.md` — the embedded-template sharing step, once the desktop renderer ships

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `eng/build/`, `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj`, `src/Pegasus.Desktop.Infrastructure/*.csproj` and the two test projects. Must not edit any `.scriban` or `.css` asset, must not change renderer code, must not add the four unembedded templates.
- **Traps**: a second copy of the template set in the desktop breaks the one-list rule; changing a `LogicalName` silently breaks `ResourceStream` at runtime rather than at build time — the browser test is the guard; the `.gitattributes` LF pinning is load-bearing for hash equality; C-01 means no new CI job.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
