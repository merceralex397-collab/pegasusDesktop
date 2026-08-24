# Plan — FEAT-039: Share the report templates once — embed the governed source into both renderer assemblies, hash-checked

**Diff estimate: ~7 files, ~280 lines** (~30 added, ~12 removed in the two `.csproj` files; ~30 in
the new props file; ~90 and ~110 in the two test files; ~25 in the two documentation files).

Derived from the files document: 1 new props file, 2 `.csproj` edits (`Pegasus.Infrastructure.csproj`
loses the 12-line item group at `:42-53` and gains one `<Import>`; the desktop infrastructure csproj
gains an `<Import>` plus one property), 2 new test files, 2 documentation edits. No asset file is
copied, no renderer source changes, no package is added.

## Approach

Move the existing five-item `EmbeddedResource` group into one shared MSBuild props file that both
assemblies import, with the logical-name prefix parameterised, and prove equality with hashes. The
rejected alternative was a build step that **copies** the governed assets into each project before
compiling — rejected because it puts a second copy of a governed asset in the tree (the exact trap
`docs/desktop/07-integrations/README.md` § 7 names and the "one list per concept" rule at
`AGENTS.md:164-165` forbids), and because a copy step has to be kept in sync with `.gitattributes`'
LF pinning while an `Include=` by relative path does not. The repository has already made this
choice once for the same reason: `Directory.Build.props:9-18` single-sources `PlaywrightVersion` so
the package reference and the container base image "cannot silently desynchronise". This ticket
generalises that precedent from one property to one item group.

## Governing docs

`refs`: `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md`.

| Ref | Requirement | Meets |
| --- | --- | --- |
| FRD-11 (reports, correspondence and reviewed proposals) | The governed report assets are the ones a report is rendered from; nothing else may be substituted | Steps 4–6 keep one source; step 8/9's hash tests make substitution a build failure |

The ticket carries **`docs_todo: true`**:

> **New ADR** — ADR-0108 (isolated, non-UI WebView2 HTML→PDF rendering; gateway renderer retained
> until golden-file parity), authored by [[FND-007]] (plan handle `DSK-00-07`); ADR-0108 has two
> claimants, so see [[FND-007]]'s plan for the ownership reconciliation — [[FEAT-038]] (plan handle
> `DSK-07-12`) owns the Phase 7 content and the acceptance flip.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (the ADR-0108 row) and in
> `docs/desktop/README.md` § Locked decisions (L-03); if the ADR lands differently this plan is
> revised before implementation.

`refs` carries no ADR, so the programme-level authorities that bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-03 (index § Locked decisions) | Both renderers exist at once until parity passes — which is precisely why the assets are shared rather than duplicated | Steps 4–6 |
| Proposal § 12.5 | Deterministic tests comparing key text, values and layout against approved fixtures — meaningful only if both renderers consume the same bytes | Step 9's equal-hash fact, consumed by [[FEAT-041]] |
| Proposal § 21.1 (build properties) | Build configuration is centralised, not repeated per project | Step 4 |
| C-01 (index § Constraints) | Private-repository Windows runner minutes bill at 2×; the drift check must be a cheap test, not a new CI job | Step 12 |
| `AGENTS.md` § Simplicity rails, "One list per concept" (`:164-165`) | A table, vocabulary or governed set lives in exactly one place | Steps 4 and 7 |
| Upstream `TICK-216`, accepted by the Collision Engineers operator on 2026-08-19 | The exact `reference/rendererref1/` wording, its named qualifications and all three bundled engineer signatures are authorised for active draft generation, provided name, qualification and signature match as one tuple and any missing, unknown, mismatched or substituted value fails closed | Step 3 records it via [[FEAT-043]]; this ticket embeds to that record and decides nothing |
| Upstream `TICK-206`, recorded upstream | Only the `rendererref1` assessment-report family plus its fee note is activated; twelve legacy identifiers stay inactive and non-discoverable | Step 9's negative assertion, extended to the client assembly |
| `HZN-001` / `board-conventions.md` § Upstream ids versus board ids | Upstream ids are never written bare | Every citation of upstream `TICK-206` / `TICK-216` in this plan |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `directory-build-organization`
  (dotnet/skills `98f84851`, plugin `dotnet-msbuild`) → `run-tests` (dotnet/skills `98f84851`,
  plugin `dotnet-test`) → `binlog-failure-analysis` (dotnet/skills `98f84851`) if the build
  misbehaves
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for MSBuild
  `EmbeddedResource` `LogicalName` and shared `.props` import semantics)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute`
  → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every
  move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refines the body's thirteen steps in the same order.

1. **Orient and take.** Read the plan row (`docs/desktop/07-integrations/README.md` § 5,
   `DSK-07-13`), that area's § 7 duplication trap, and the item group at
   `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:42-53`. Call `get_doc_gates FEAT-039`,
   then `take_ticket` on branch `task/dsk-07-13-shared-report-templates`.
2. **Record the measured template inventory in `research` (append).**
   `ls docs/design/assets/report-renderer/templates/` returns **six** `.scriban` files
   (`advert_evidence_pack`, `assessment_fee_note`, `assessment_report`, `expert_report`,
   `fee_note`, `market_valuation_evidence`) **plus `report.css`** — the plan set's "seven
   `.scriban` files" names six; write the measured count. Record that `Pegasus.Infrastructure`
   embeds only `assessment_report.scriban`, `assessment_fee_note.scriban` and `report.css` today,
   and treat that as the template set the desktop must match. Adding the other four is a scope
   change belonging to [[FEAT-043]] (plan handle `DSK-07-17`), and so is making any of upstream
   TICK-206's twelve legacy identifiers dispatchable — which step 9's test asserts against.
3. **Record the authorised signature set before touching the props file — and do not land without
   it.** Upstream `TICK-216`'s 2026-08-19 operator decision authorises three engineer signatures
   (`andy_patterson.png`, `ed_mawdsley.png`, `neil_oreilly.png`); `docs/design/brand/signatures/`
   holds all three (3,972 / 80,989 / 30,418 bytes) while `Pegasus.Infrastructure.csproj:52-53`
   embeds only the first. Record which assets the accepted contract authorises and embed the same
   set in both assemblies **or state why not** — an asset embedded in a desktop assembly ships to
   every workstation inside the MSIX (~115 KB for all three), a different exposure from one inside
   a server container. **If [[FEAT-043]] has not recorded the upstream TICK-216 desktop-exposure
   decision, stay in Preparing** rather than guessing.
4. **Create the shared props file.** `eng/build/ReportAssets.props` — or the location
   `directory-build-organization` recommends for this layout; `eng/` does not exist today, so
   record the choice and its reason. It contains exactly the five items now at
   `Pegasus.Infrastructure.csproj:42-53` plus any signature step 3 adds, with:
   - paths rewritten relative to `$(MSBuildThisFileDirectory)` rather than `..\..\docs\…`;
   - the `logo_no_margin.png` item keeping its element form with the
     `<Link>Reports\Assets\brand\logo.png</Link>` child (`:48-51`) — it is the only item that is
     not a plain attribute form;
   - `LogicalName`s parameterised by an assembly-specific prefix property defaulting to
     `Pegasus.Infrastructure.Reports.Assets` (assumption `A-07-13-1`);
   - a comment citing `Directory.Build.props:9-18` as the precedent and reason.
5. **Import from `Pegasus.Infrastructure` and prove the names are unchanged.** Add the `<Import>`,
   delete the duplicated `ItemGroup`, then list the assembly's manifest resource names and confirm,
   character for character: `Pegasus.Infrastructure.Reports.Assets.templates.assessment_report.scriban`,
   `…templates.assessment_fee_note.scriban`, `…templates.report.css`, `…brand.logo.png`,
   `…brand.signatures.andy_patterson.png`, plus any suffix step 3 adds. A changed name breaks
   `ResourceStream` (`PlaywrightAssessmentReportRenderer.cs:309-314`) at run time, not build time.
6. **Import from `Pegasus.Desktop.Infrastructure`** ([[FND-031]], plan handle `DSK-02-06`) with the
   prefix property set to that assembly's own namespace, so [[FEAT-040]] (plan handle `DSK-07-14`)
   resolves the same suffixes — `templates.assessment_report.scriban` and so on — through its own
   `ResourceStream` equivalent.
7. **Copy no asset into `src/`.** The single source stays `docs/design/assets/report-renderer/` and
   `docs/design/brand/`; the props file references them by relative path exactly as the existing
   csproj does.
8. **Hash test in the cheaper project.** Prefer `tests/Pegasus.Core.Tests` if it can read an
   embedded resource from `Pegasus.Infrastructure` without a database (assumption `A-07-13-3`);
   otherwise `tests/Pegasus.IntegrationTests`. For every embedded report asset, assert SHA-256
   against the hash of the on-disk governed file. A changed template or signature without a
   reviewed hash update fails the build.
9. **Mirror test in the desktop test project** ([[TEST-004]], plan handle `DSK-08-04`; project
   shape from [[FND-038]], plan handle `DSK-02-13`): read the same suffixes from
   `Pegasus.Desktop.Infrastructure` and assert each hash **equals the hash of the same resource in
   `Pegasus.Infrastructure`**. Byte-identical is the acceptance property, not "both present". Then
   the negative fact [[FEAT-043]] step 5 records as this ticket's obligation: **no template
   identifier outside the embedded set resolves from `Pegasus.Desktop.Infrastructure`** —
   enumerate upstream TICK-206's twelve legacy identifiers (`market-valuation-evidence`,
   `advert-evidence-pack`, `fee-note` as a raw selector, `expert-report`, `blank-letterhead`,
   `repairable-contract-repair-report`, `total-loss-report`, `addendum-report`,
   `diminution-rebuttal`, `roadworthy-criminal-report`, `part-35-response`, `response-letter`) plus
   one unknown identifier, and assert each resolves to nothing. Assert against the **client**
   assembly: an identifier being unavailable in the retained gateway renderer proves nothing about
   this one.
10. **Guard the LF pinning.** `.gitattributes:4-5` keeps
    `docs/design/assets/report-renderer/**/*.css` and `**/*.scriban` at LF, and a CRLF checkout
    would change every text-asset hash. Assert that the embedded `.scriban` and `.css` bytes
    contain no `\r\n` sequence. The PNGs are `*.png binary` (`.gitattributes:20`) and are not
    covered by this assertion — do not claim they are.
11. **Build both assemblies and run both tests**, then run the browser suite: the existing
    `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` must still render,
    which is the proof the logical names survived.
12. **Confirm no new CI job.** The drift check runs inside the existing lanes — the default
    integration filter and the `browser` lane at `.github/workflows/ci.yml:207-234`, plus the
    desktop lane [[TEST-013]] (plan handle `DSK-08-13`) establishes. C-01 makes a third lane a real
    recurring cost.
13. **Simplification pass and PR.** Run the pass over this branch's diff, record it under a dated
    `## Simplification pass` heading below, and open the PR into `dev`.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture**
(`docs/engineering.md` § Required evidence tiers item 1 — build-level consistency: the projects
compile, dependency direction is unchanged, and no second copy of a governed asset exists).
`proof` is the captured output of:

- `dotnet build ./src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj -c Release` and
  `dotnet build ./src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj -c Release`
  — expected: both succeed under `TreatWarningsAsErrors` (`Directory.Build.props:8`).
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"`
  — expected: the resource-hash and no-CRLF facts pass.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category=Browser&Category!=Corpus" -- xUnit.MaxParallelThreads=2`
  — expected: `AssessmentReportRendererTests` still renders all four outcomes, proving the logical
  names are intact.
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`
  — expected: the cross-assembly equal-hash fact and the twelve-plus-one legacy-identifier
  non-resolution fact pass.
- `git status --porcelain -- docs/design` — expected: **empty**; no governed asset was edited.
- The [[FEAT-043]] upstream TICK-216 record, cited by ticket id and date in the proof — expected:
  present before this ticket leaves Preparing, naming which signature assets may be embedded.

## Risks / open questions

- **Risk — a `LogicalName` changes silently.** It compiles and breaks a report at run time.
  Mitigation: step 5 lists the manifest resource names explicitly, and the browser suite is run as
  named verification rather than left to CI.
- **Risk — the props file's relative paths resolve differently from `..\..\docs\…`.** Mitigation:
  `$(MSBuildThisFileDirectory)` anchoring plus the manifest-name check in step 5.
- **Risk — a mis-normalised checkout changes every text hash.** Mitigation: step 10's explicit
  no-CRLF assertion, so it fails loudly rather than as a mysterious render diff.
- **Risk — the equal-hash test is written as "both present".** Mitigation: step 9 states
  byte-identity as the acceptance property in as many words, and [[FEAT-041]] depends on it.
- **Risk — scope creep into the four unembedded templates.** Mitigation: step 2 records the
  embedded set as the target and names [[FEAT-043]] as the owner of any change to it.
- **Scope boundary, not an open question** — which signature assets may ship is
  [[FEAT-043]]'s recorded disposition of the already-accepted upstream TICK-216 contract. This
  ticket does not leave Preparing until that record exists; that is a sequencing precondition, not
  a question for anyone.
- **Scope boundary, not an open question** — the desktop test project and
  `src/Pegasus.Desktop.Infrastructure` are created by [[TEST-004]] / [[FND-038]] and [[FND-031]].
- **No open question is opened.** The body instructs none, and the one operator judgement in this
  area was taken upstream on 2026-08-19.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
