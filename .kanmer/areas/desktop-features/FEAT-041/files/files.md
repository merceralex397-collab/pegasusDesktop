# Files — FEAT-041

Surveyed on 2026-08-24 at fork `main`. Paths that do not exist today are marked with the named
ticket that creates them; every other path was confirmed with `ls` or `wc -l`.

## Where the change lands

| Path | Why |
| --- | --- |
| `tests/Pegasus.IntegrationTests/Reports/ReportParityAssertions.cs` | **New.** The single shared assertion set — the four tolerance families (text, values, page count, named anchor positions) expressed once, taking a `RenderedReportArtifact` and a fixture record. Both renderers are asserted through it. Breaks if the two sides ever grow separate assertion code, which is the failure body step 5 names. |
| `tests/Pegasus.IntegrationTests/Reports/ReportFixtureCatalogue.cs` | **New.** The fixture definitions in code: the five cases (four `AssessmentReportOutcome` values plus the density case), each with its recorded purpose, its snapshot builder and its expected token/anchor set. Kept beside the assertions so a fixture and its meaning cannot drift apart. |
| `tests/Pegasus.IntegrationTests/Reports/ReportFixtureCaptureTests.cs` | **New.** The gateway-side capture and comparison: renders each catalogue case through `GenerateAssessmentReportDraft`, asserts it against the committed fixture through the shared helper. Carries `[Trait("Category", "Browser")]` so it lands in the existing lane filter and nowhere else. |
| `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` | **Edit, additive only** (158 lines today). Add the six `PEGASUS_RENDER_EVIDENCE` lines to `NormalDensityFlowsLongListsAndMultiplePhotosAcrossPagesWithoutClipping` (`:62-98`), mirroring `:53-59`, so the density fixture can be captured the same way as the four outcomes. Do not weaken or relocate any existing assertion — this file is the reviewed definition of a correct report. |
| `tests/Pegasus.IntegrationTests/Reports/fixtures/*.pdf` | **New, binary.** Ten baseline artifacts: assessment + fee note for each of the five cases. `.gitattributes:18` already declares `*.pdf binary`, so no attribute change is needed. Synthetic data only (`Snapshot(...)`, `AssessmentReportRendererTests.cs:137-150`) — not corpus. |
| `tests/Pegasus.IntegrationTests/Reports/fixtures/manifest.md` | **New.** The human-reviewable manifest: one entry per fixture with its purpose, the Playwright version, `AssessmentReportContract.TemplateVersion`, the capture date, the tolerance table, the sentence stating pixel equality is not the target, and the re-baseline procedure with its named approver. This file is what a reviewer reads when a fixture fails. |
| `tests/Pegasus.Desktop.ViewModelTests/Reports/ReportParityTests.cs` | **New.** The desktop half: renders the same catalogue cases through [[FEAT-040]] (plan handle `DSK-07-14`)'s `WebView2AssessmentReportRenderer` and asserts against the same fixtures through the **same** helper. Test names contain `ReportParity` so the body's verification filter `FullyQualifiedName~ReportParity` selects them. Project created by [[TEST-004]] (plan handle `DSK-08-04`) and [[FND-038]] (plan handle `DSK-02-13`). |
| `tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj` | **Edit** (created by [[TEST-004]] / [[FND-038]]). Add a `PdfPig` `PackageReference` and the linked `<Compile Include="..\Pegasus.IntegrationTests\Reports\ReportParityAssertions.cs" Link="Reports\ReportParityAssertions.cs" />` pair that shares the helper without copying it. Assumption `A-07-15-2` is settled here. |
| `docs/adr/0108-desktop-webview2-report-rendering.md` | **Not edited by this ticket.** Its results table is handed to [[FEAT-038]] as acceptance evidence. [[FND-007]] authored the proposed ADR; [[FEAT-038]] later makes only the `proposed` → `accepted` frontmatter change and index row. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | **Edit.** Move the report rows — `PAR-15` at `:60` is the one this suite proves — from `inventoried` to `automated verification passed`, and name this ticket's results table as the evidence. |

## Context files

What the implementer must **read** before writing a line, and the specific trap or precedent each
one holds.

| Path | What it tells the implementer |
| --- | --- |
| `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` | The whole design brief, in 158 lines. `:14-20` gives the four outcomes and the `Browser` trait; `:30-41` gives the exact per-outcome title strings and the shared text anchors; `:43-51` gives the eight fee-note tokens including the sort code `30-12-80` and account `50858868`; `:53-59` is the capture mechanism to reuse; `:88-97` shows page count, per-page reference and embedded-image count already being asserted on the density case. **The trap is `AssertArtifact` at `:112-119`: it asserts `EngineVersion` contains `"Playwright"`.** Copy that into a shared helper and every desktop fixture fails for the one property that is meant to differ. |
| `src/Pegasus.Core/Reports/AssessmentReportRendering.cs` | `:272-278` is the artifact shape the helper consumes; `:8` is `TemplateVersion = "rendererref1-v1"` and `:9` is `VatNumber = "262 0937 10"` — assert the constants, never the literals. `:291-307` shows Core **already** re-hashes and rejects mismatched provenance, throwing `ReportRenderRejectedException` (`:312`): this suite must not duplicate that check, and a SHA-256 comparison **between** the two renderers is meaningless because the bytes legitimately differ. |
| `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` | The baseline being captured. `:120-127` is the exact print setup the desktop renderer must match (A4, `PrintBackground`, `DisplayHeaderFooter`, empty header `"<span></span>"`, margins `8mm/12mm/22mm/12mm`); a fixture failure at a page boundary is usually a margin or scale mismatch here, not a template problem. `:19` is the `SemaphoreSlim(1, 1)` that makes capture runs serial and therefore slow — budget for it. `:105-114` shows unresolved-placeholder rejection happens *before* any PDF exists, so a `{{` leak is never a fixture failure. |
| `.github/workflows/ci.yml:207-234` | The lane the gateway captures must fit into. The filter is exactly `Category=Browser&Category!=Corpus` (`:232`) — a capture test without the `Browser` trait silently never runs in CI. `timeout-minutes: 25` (`:211`) is the budget the five capture cases share with the existing browser tests. The `xUnit.MaxParallelThreads=2` cap (`:234`) and its comment explain why: each browser test starts a Chromium, a Kestrel host and its own restored database. |
| `Directory.Build.props:10-17` | `PlaywrightVersion` is `1.61.0` and is single-sourced on purpose; `:10-16`'s comment says the package and the container base image "cannot silently desynchronise". **This is the asymmetry the whole tolerance design exists for**: this number is pinned and the WebView2 Evergreen runtime is not. Read the number from here into the manifest rather than typing it. |
| `docs/desktop/07-integrations/README.md` § 7 | The trap rows in force here: "Golden-file drift between Chromium builds (WebView2 runtime updates itself; Playwright is pinned to 1.61.0)" with its recorded mitigation — "Tolerant comparisons (text, values, page count, positions within tolerance), fixture review on failure, **not pixel equality**". The design is prescribed, not chosen. § 4's Phase 7 gate row is the acceptance sentence. |
| `docs/desktop/README.md` § Locked decisions | L-03 in the operator's own words: the gateway renderer "is retained only until golden-file parity passes". This ticket is that condition. C-01 in the Constraints table explains why a third CI lane is not free. |
| `.gitignore:1-2`, `:20-21` | `/corpus/` is ignored with the comment "Never commit operational emails or case files"; `**/artifacts/` and `/artifacts/` are ignored. Tells the implementer that committed fixtures must come from the synthetic `Snapshot(...)` builder and that a fixtures directory named `artifacts/` would be silently untracked. |
| `.gitattributes:4-5`, `:18` | `*.pdf binary` (`:18`) means fixture PDFs need no new attribute. `:4-5` pin the `.scriban` and `.css` sources to LF — a CRLF checkout changes the rendered HTML and therefore every fixture, so a whole-catalogue failure with no code change points here first. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | The legend block (`:12-24`) defines the exact status string to write: `automated verification passed`, "Unit/contract/UI automation evidence recorded". `PAR-15` at `:60` already names both existing report test files in its evidence column — extend that column, do not replace it. The file holds 46 `PAR-` rows. |
| `docs/engineering.md:72-88` | Tier 3 is "Parser/adapter contracts — … corruption, encryption, expansion/resource limits, cancellation, path/integrity safety, stable contract codes, and **deterministic external failures**". The word that matters is *deterministic*: a fixture that passes intermittently fails this tier. `:201-207` § Plan sizing requires the diff estimate first and a facts-versus-assumptions split. |
| `AGENTS.md` § Repository task workflow | Step 4 requires the simplification pass over this branch's own diff before the PR; step 5 requires review by an agent that did not implement — which is also the default this ticket adopts as the re-baseline approver. |

## Ripple effects

- **Tests.** `AssessmentReportRendererTests.cs` is edited additively; its existing facts must stay
  green and are named in the plan's verification for exactly that reason. `AssessmentReportDraftWebTests.cs`
  (259 lines) drives the same renderer through the web caller and is **not** edited, but it shares
  the renderer singleton and must be re-run to prove nothing regressed.
- **CI lanes.** No new lane. The gateway capture tests join the existing `browser` filter
  (`.github/workflows/ci.yml:232`) by carrying the `Browser` trait; the desktop comparison joins the
  desktop lane [[TEST-013]] (plan handle `DSK-08-13`) creates. [[TEST-018]] (plan handle
  `DSK-08-18`) — which this ticket **blocks** — runs both together on the Test/UAT stack and
  consumes the catalogue and manifest as-is.
- **Lane duration.** Five capture cases through a serialised Chromium land inside a lane already
  capped at `timeout-minutes: 25` with `MaxParallelThreads=2`. If the capture is run in the same
  lane as the comparison the budget must be re-measured, and C-01 makes overflowing into a new lane
  a real recurring cost.
- **Documentation.** ADR-0108's Verification section (edited through [[FEAT-038]]'s file) and the
  parity-matrix report rows. The **results table** produced by this ticket is consumed by
  [[FEAT-038]] as the acceptance evidence and by [[FEAT-042]] (plan handle `DSK-07-16`) as the
  condition for switching the gateway renderer off behind its flag — so it is a deliverable with
  two named readers, not an internal artefact.
- **No contract ripple.** This ticket adds no route, no DTO and no persisted field, so
  `openapi/pegasus-v1.json` and the generated Kiota client are **not** regenerated and must show no
  diff. That is asserted in the plan's verification rather than assumed.
- **No migration.** No new table, so no runtime-role `Grant*` migration and no
  `scripts/Test-MigrationGrants.ps1` involvement.
- **Repository size.** Ten committed PDFs. Reviewable, but the catalogue is deliberately capped at
  five cases; adding a sixth is a scope decision recorded in the manifest, not a convenience.

## Out of scope

Recorded here because the ticket's Guardrails already forbid each one, so the reviewer sees it was
a decision.

- **Changing either renderer.** Guardrails: "Must not change either renderer to make a fixture
  pass — a failing fixture is either a renderer defect ([[FEAT-040]]) or a reviewed tolerance
  change, never a quiet edit." `src/Pegasus.Infrastructure/Reports/` and
  `src/Pegasus.Desktop.Infrastructure/` are read-only to this ticket.
- **Pixel or byte comparison.** Explicitly excluded by the area plan's § 7 mitigation and by
  assumption `A-07-15-3`. A SHA-256 comparison between the two renderers' outputs is not a weaker
  version of this suite; it is a wrong one.
- **Flipping ADR-0108 to `accepted` or adding its index row.** Both belong to [[FEAT-038]] (plan
  handle `DSK-07-12`); ADR bodies are immutable once accepted and the flip is frontmatter-only.
  This ticket produces the evidence and edits only the Verification section.
- **Switching the gateway renderer off.** L-03 keeps it until parity passes and [[FEAT-042]] owns
  the flag.
- **Adding a third CI lane.** C-01 (index § Constraints): private-repository Windows runner minutes
  bill at 2×. Reuse the existing lanes; the cost decision itself is [[TEST-019]] (plan handle
  `DSK-08-19`)'s.
- **The four unembedded templates.** `expert_report`, `fee_note`, `market_valuation_evidence` and
  `advert_evidence_pack` are governed but embedded by neither renderer today; whether they ever
  ship is [[FEAT-043]] (plan handle `DSK-07-17`)'s recorded disposition of the upstream
  report-decision tickets. No fixture is captured for them.
- **Corpus evidence.** `docs/engineering.md` tier 8 keeps detailed corpus evidence local and
  ignored (`.gitignore:1`); every fixture here is synthetic.
- **Creating the desktop test project or `src/Pegasus.Desktop.Infrastructure`.** Created by
  [[TEST-004]] / [[FND-038]] and [[FND-031]] (plan handle `DSK-02-06`) respectively.
- **Any Azure write.** Guardrails: "Azure: no write." Nothing here reads or writes an Azure
  resource.
