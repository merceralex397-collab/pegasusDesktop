# Research — FEAT-039: sharing one governed report-asset set between two renderer assemblies

## Question

Can `Pegasus.Infrastructure` and the new `Pegasus.Desktop.Infrastructure` embed **byte-identical**
report assets from the single governed source under `docs/design/`, without copying a file into
`src/`, without changing any existing logical resource name, and with drift caught by a test rather
than by a new CI job?

## Current behaviour

Report assets are embedded today by exactly one project, by relative path out of `docs/`:

| Fact | `path:line` |
| --- | --- |
| Five `EmbeddedResource` items for report assets | `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:42-53` |
| The renderer resolves them by suffix under one prefix | `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:309-314` (`ResourceStream` builds `Pegasus.Infrastructure.Reports.Assets.{suffix}` and throws `"Required report resource '{name}' is missing."` when absent) |
| Template lookup by name | `PlaywrightAssessmentReportRenderer.cs:106` — `Template.Parse(ResourceText($"templates.{name}"))` |
| CSS lookup | `PlaywrightAssessmentReportRenderer.cs:146` — `ResourceText("templates.report.css")` |
| Logo lookup as a data URI | `PlaywrightAssessmentReportRenderer.cs:147` area — `ResourceDataUri("brand.logo.png", "image/png")`, backed by `ReadResourceDataUri` at `:292-299` |

Parity row: **`PAR-15`** (§13.9 Assessment and reporting, FRD-11/FRD-06,
`docs/desktop/01-inventory-and-parity/parity-matrix.md:60`) is the row that covers the page model
whose report draft these assets render. **No parity row covers the build/packaging surface itself,
and none should** — the parity matrix holds 46 rows (`grep -c '^| PAR-'` → `46`), all keyed to page
models under `src/Pegasus.Web/Pages/**`, and an MSBuild item group is not an operator-visible
behaviour. The closest existing repository mechanism is therefore the build itself: the
`Directory.Build.props` single-source property pattern (see Facts) and the CI `browser` lane at
`.github/workflows/ci.yml:207-234` that proves the renderer still resolves its resources.

## Findings

### Facts

Measured on 2026-08-24 at fork `main`.

- **The governed template source holds six `.scriban` files plus `report.css` — seven files, not
  seven `.scriban` files.** `ls -la docs/design/assets/report-renderer/templates/` gives
  `advert_evidence_pack.scriban` (1,858 bytes), `assessment_fee_note.scriban` (2,088),
  `assessment_report.scriban` (2,561), `expert_report.scriban` (3,164), `fee_note.scriban` (2,003),
  `market_valuation_evidence.scriban` (2,795) and `report.css` (12,529). The plan set and this
  ticket's body both say "seven `.scriban` files" while naming six — the measured count is six
  `.scriban` plus `report.css`. Use the measured count; the six names the body lists are correct.
- **Three of those seven are embedded today**, plus two brand assets. The five items at
  `Pegasus.Infrastructure.csproj:42-53` are, with their logical names:
  | Source | LogicalName |
  | --- | --- |
  | `..\..\docs\design\assets\report-renderer\templates\assessment_report.scriban` | `Pegasus.Infrastructure.Reports.Assets.templates.assessment_report.scriban` |
  | `…\templates\assessment_fee_note.scriban` | `…Reports.Assets.templates.assessment_fee_note.scriban` |
  | `…\templates\report.css` | `…Reports.Assets.templates.report.css` |
  | `..\..\docs\design\brand\logos\logo_no_margin.png` (with `<Link>Reports\Assets\brand\logo.png</Link>`) | `…Reports.Assets.brand.logo.png` |
  | `..\..\docs\design\brand\signatures\andy_patterson.png` | `…Reports.Assets.brand.signatures.andy_patterson.png` |
  The four unembedded templates are `advert_evidence_pack`, `expert_report`, `fee_note` and
  `market_valuation_evidence`.
- **All three governed signatures exist on disk; only one is embedded.**
  `ls -la docs/design/brand/signatures/` → `andy_patterson.png` (3,972 bytes),
  `ed_mawdsley.png` (80,989), `neil_oreilly.png` (30,418).
- **`.gitattributes` pins only the report-renderer text assets to LF.** `.gitattributes:4-5`:
  `docs/design/assets/report-renderer/**/*.css text eol=lf` and
  `…/**/*.scriban text eol=lf`. The PNGs are covered by `*.png binary` at `:20`, so their bytes are
  already preserved and cannot drift by line-ending normalisation — the CRLF assertion the body's
  step 10 requires applies to the `.scriban` and `.css` assets only, which is exactly what it says.
- **The repository already has the single-source build-property pattern this ticket generalises.**
  `Directory.Build.props` (19 lines) defines `PlaywrightVersion` at `:18` with a comment at
  `:9-17` explaining that `Pegasus.Infrastructure`'s `PackageReference` and `Pegasus.Web`'s
  `ContainerBaseImage` both derive from it "so a future Playwright bump cannot silently
  desynchronise the two". That is the same argument this ticket makes about the report assets, and
  it is the precedent the props file should cite.
- **`eng/` does not exist yet.** `ls -d eng` → absent. The repository's only script directory is
  `scripts/` (22 `.ps1` files plus `email-eval-desktop/` and `reference_data/`). Creating
  `eng/build/ReportAssets.props` is therefore a new directory, and the `directory-build-organization`
  skill's recommendation must be recorded rather than assumed.
- **Central package management does not exist yet either.** There is no `Directory.Packages.props`;
  every version is inline in each `.csproj` (for example `Pegasus.Infrastructure.csproj:23-28`
  pins `PdfPig` 0.1.15, `PDFsharp` 6.2.4, `Scriban` 7.2.6, `Microsoft.Playwright`
  `$(PlaywrightVersion)`, `SkiaSharp` 3.116.1). [[FND-027]] (plan handle `DSK-02-02`) introduces it.
- **The solution is `Pegasus.slnx`, listing four `src/` projects and three `tests/` projects.**
  `src/Pegasus.Desktop.Infrastructure` ([[FND-031]], plan handle `DSK-02-06`) and
  `tests/Pegasus.Desktop.ViewModelTests` ([[FND-038]] / [[TEST-004]], plan handles `DSK-02-13` and
  `DSK-08-04`) do not exist yet.
- **The renderer test that proves logical names survive a refactor already exists.**
  `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` is 158 lines; its
  `[Theory]` over the four `AssessmentReportOutcome` values carries `[Trait("Category","Browser")]`
  and asserts real rendered text via `UglyToad.PdfPig`. If a `LogicalName` changes, `ResourceStream`
  throws at runtime and this suite is what catches it.
- **The browser lane is an existing CI lane, not a new one.**
  `.github/workflows/ci.yml:207-234` runs `windows-latest`, caches the pinned Playwright browsers
  keyed on `tests/Pegasus.IntegrationTests/packages.lock.json`, installs Chromium, and runs
  `--filter "Category=Browser&Category!=Corpus" -- xUnit.MaxParallelThreads=2`. C-01 makes adding a
  third lane a real recurring cost, so the drift check belongs inside these lanes.

### Assumptions

- **`A-07-13-1` — an `EmbeddedResource` `LogicalName` can be composed from an MSBuild property so
  one item group serves two assemblies with different prefixes.** Standard MSBuild property
  expansion inside item metadata; confirmed during step 4 by building both projects and listing
  manifest resource names. If wrong, the fallback is two item groups in one props file guarded by a
  condition — same single source, slightly more lines, no change to the acceptance property.
- **`A-07-13-2` — `docs/design/brand/signatures/*.png` are the exact byte sources the accepted
  TICK-216 contract authorises**, rather than a re-export of some other original. Confirmed by the
  [[FEAT-043]] record this ticket waits on. If wrong, the hash test would pin the wrong bytes; that
  is why the ticket does not land before that record exists.
- **`A-07-13-3` — `tests/Pegasus.Core.Tests` can read an embedded resource from
  `Pegasus.Infrastructure` without a database.** The body prefers the cheaper project. Confirmed by
  checking that project's references during step 8; if it does not already reference
  `Pegasus.Infrastructure`, adding that reference for a hash test is a larger change than putting
  the test in `Pegasus.IntegrationTests`, and the integration project is then the correct home.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md:166-178`, answered for **the embedding of the
governed report assets**:

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **no** | An embedded resource is immutable build output; it is not state anyone updates at run time. The *source* is governed in the repository under `docs/design/`, which is version control, not a runtime authority. |
| Unattended execution — must it run with every desktop closed? | **no** | Embedding happens at build time. The gateway renderer that consumes the same assets continues to run centrally, but that placement is ADR-0028's and is unchanged by this ticket. |
| Protected credentials — long-lived secret that must not sit on workstations? | **no** | The assets are templates, a brand logo and engineer signature images. They are not secrets. **They are, however, personal professional attributions**, and the desktop-exposure judgement that follows from putting them in every MSIX is recorded by [[FEAT-043]] (plan handle `DSK-07-17`) under the accepted upstream TICK-216 contract — recorded, not re-decided, and not a credential question. |
| Public callback — must an external service call a stable public endpoint? | **no** | Nothing external is involved. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **no** | The invariant this ticket enforces — byte-identical assets in both assemblies — is enforced by a **test in CI**, which is exactly the "independent of the client" mechanism the question asks about, without a running service. |
| Measured operational advantage — measured evidence central is materially better? | **no** | None claimed. The `Directory.Build.props` `PlaywrightVersion` precedent shows the repository already prefers a build-time single source over a runtime one. |

All six **no** → the responsibility belongs in the desktop's build, which is where this ticket puts
it. Nothing is placed in Azure and no Azure write is required.

## Implications

1. **The props file is the whole design; the tests are the whole proof.** Everything else is
   deletion — one item group removed from `Pegasus.Infrastructure.csproj` and re-imported.
2. **Logical-name preservation is the sharp edge.** `ResourceStream`
   (`PlaywrightAssessmentReportRenderer.cs:309-314`) fails at **runtime**, not at build time, so a
   renamed resource compiles cleanly and breaks a report. The browser suite is the only guard, and
   the verification therefore has to run it.
3. **Byte-identity, not co-presence, is the acceptance property.** Two assemblies both having "a
   template called `assessment_report`" proves nothing; the cross-assembly equal-hash assertion in
   the desktop test project is the fact that matters, and it is what makes a [[FEAT-041]] (plan
   handle `DSK-07-15`) golden-file failure attributable to the renderer rather than to the inputs.
4. **The negative assertion is asymmetric and easy to get wrong.** An identifier being unavailable
   in the retained gateway renderer proves nothing about the client assembly, so the twelve legacy
   identifiers from upstream TICK-206 plus one unknown must be asserted **against
   `Pegasus.Desktop.Infrastructure`**, not against `Pegasus.Infrastructure`.
5. **Scope is bounded by a record this ticket does not own.** [[FEAT-043]] owns the template scope
   table, the upstream TICK-216 signature record and the twelve-entry legacy negative list. This
   ticket embeds *to* that record. Adding the four unembedded templates here would be a scope
   change.
6. **The LF pinning is load-bearing for the `.scriban` and `.css` hashes only** (`.gitattributes:4-5`);
   the PNGs are `binary` (`:20`) and cannot be perturbed that way. Write the CRLF assertion for the
   text assets and do not claim it covers the images.

## Open questions

- None for this ticket. The one decision that could have been an open question — which signature
  assets may be embedded, given that a desktop assembly ships to every workstation — is **owned by
  [[FEAT-043]]** as a recorded disposition of the already-accepted upstream TICK-216 contract, and
  the body makes not landing before that record a precondition rather than a question. That is a
  scope boundary, recorded in the plan's Risks section, not an unresolved question here.
