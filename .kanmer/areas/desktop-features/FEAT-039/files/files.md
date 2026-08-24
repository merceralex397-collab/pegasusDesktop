# Files — FEAT-039

Measured on 2026-08-24. Paths that do not exist yet carry the ticket that creates them.

## Where the change lands

| Path | Why |
|---|---|
| `eng/build/ReportAssets.props` *(new; `eng/` does not exist today — `ls -d eng` → absent)* | The single `EmbeddedResource` item group for the report assets: the five items currently at `Pegasus.Infrastructure.csproj:42-53`, plus any signature asset the [[FEAT-043]] record adds, with paths relative to `$(MSBuildThisFileDirectory)` and `LogicalName`s parameterised by an assembly-prefix property defaulting to `Pegasus.Infrastructure.Reports.Assets`. Record the location the `directory-build-organization` skill recommends for this layout rather than assuming `eng/build/`. |
| `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj` | Import the props file and delete the now-duplicated `ItemGroup` at `:42-53` (five items, 12 lines). Breakage risk: **the highest in this ticket** — if any logical name changes, `ResourceStream` (`PlaywrightAssessmentReportRenderer.cs:309-314`) throws at *run* time, not at build time. |
| `src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj` *(created by [[FND-031]] (plan handle `DSK-02-06`))* | Import the same props file with the prefix property set to that assembly's namespace, so [[FEAT-040]] (plan handle `DSK-07-14`) resolves the same suffixes through its own `ResourceStream` equivalent. |
| `tests/Pegasus.IntegrationTests/Reports/ReportAssetHashTests.cs` *(new; or `tests/Pegasus.Core.Tests` if it can run without a database — prefer the cheaper project, see `A-07-13-3`)* | SHA-256 of every embedded report asset in `Pegasus.Infrastructure` against the on-disk governed file, plus the no-CRLF assertion for the `.scriban` and `.css` assets. |
| `tests/Pegasus.Desktop.ViewModelTests/Reports/ReportAssetParityTests.cs` *(new; project created by [[TEST-004]] (plan handle `DSK-08-04`) / [[FND-038]] (plan handle `DSK-02-13`))* | The cross-assembly **equal-hash** fact, and the negative fact: none of upstream TICK-206's twelve legacy identifiers, nor one unknown identifier, resolves from `Pegasus.Desktop.Infrastructure`. |
| `docs/design/assets/report-renderer/README.md` *(new — that directory contains only `templates/` today)* **or** the governing note in `docs/design/README.md` *(exists)* | Record that two assemblies embed the same source, which signature assets are embedded under the upstream TICK-216 acceptance, and that a hash test enforces both. Pick one location and say which. |
| `docs/current-architecture.md` | The embedded-template sharing step, once the desktop renderer ships. |

## Context files

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:42-53` | The exact five items and their exact `LogicalName`s — including the one that is **not** a plain attribute: `logo_no_margin.png` uses the element form with a `<Link>Reports\Assets\brand\logo.png</Link>` child (`:48-51`). Any props-file rewrite must preserve that shape, and the `..\..\docs\…` relative roots must be re-expressed against `$(MSBuildThisFileDirectory)`, not copied verbatim. |
| `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:309-314` | Why a renamed resource is a runtime failure: `ResourceStream` composes `Pegasus.Infrastructure.Reports.Assets.{suffix}` and throws `"Required report resource '{name}' is missing."`. Nothing in the build will tell you. |
| `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:106,146,292-299` | The three consumption shapes the suffixes must keep serving: `templates.{name}` for a parsed template, `templates.report.css` as text, and `brand.logo.png` read as a base64 data URI. The desktop renderer will need the same three. |
| `Directory.Build.props:9-18` | The repository's own precedent and its stated reason: `PlaywrightVersion` is a single source so `Pegasus.Infrastructure`'s `PackageReference` and `Pegasus.Web`'s `ContainerBaseImage` "cannot silently desynchronise". Cite this in the props file's comment — it is the same argument, and it means the pattern needs no defending. |
| `.gitattributes:4-5` | `docs/design/assets/report-renderer/**/*.css` and `**/*.scriban` are `text eol=lf`. A CRLF checkout changes every text-asset hash. `:20` (`*.png binary`) covers the logo and signatures, so the no-CRLF assertion is for the text assets only — do not claim it protects the images. |
| `docs/design/assets/report-renderer/templates/` | The measured inventory: **six** `.scriban` files plus `report.css`, not seven `.scriban` files. Three are embedded; `advert_evidence_pack`, `expert_report`, `fee_note` and `market_valuation_evidence` are not. |
| `docs/design/brand/signatures/` | All three governed signatures are present (`andy_patterson.png` 3,972 bytes, `ed_mawdsley.png` 80,989, `neil_oreilly.png` 30,418) while only the first is embedded — the gap this ticket may close, but only to [[FEAT-043]]'s record. The size difference also matters: embedding all three adds ~115 KB to every MSIX. |
| `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` | The 158-line browser suite that is the *only* guard on logical-name preservation. Its `[Trait("Category","Browser")]` is what keeps it out of the default lane, which is why the verification runs the browser filter explicitly. |
| `.github/workflows/ci.yml:207-234` | The existing `browser` lane: `windows-latest`, Playwright browser cache keyed on `packages.lock.json`, `--filter "Category=Browser&Category!=Corpus" -- xUnit.MaxParallelThreads=2`. This is where the renderer proof already runs; C-01 means using it rather than adding a third lane. |
| `docs/desktop/07-integrations/README.md` § 7 ("Template duplication") | The named trap: a second copy of the Scriban/CSS set in the desktop breaks the one-list rule. |
| `AGENTS.md` § Simplicity rails ("One list per concept", `:164-165`) | The rule the trap comes from — a table or vocabulary lives in exactly one place, and a second copy in another layer is duplication even when it is "just files". |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:129,132` | The upstream `TICK-206` and `TICK-216` rows, both `report-decision`, both routed to area 07. Neither was imported; both are cited as `upstream <ID>`. |

## Ripple effects

- **`tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs`** — must still render.
  A pass here is the evidence that the five logical names survived the refactor; it is not optional.
- **`tests/Pegasus.IntegrationTests/packages.lock.json`** — the browser lane's cache key
  (`.github/workflows/ci.yml:216`). This ticket adds no package, so the key should not move; if it
  does, something unintended changed.
- **[[FEAT-040]] (plan handle `DSK-07-14`)** — cannot render faithfully until this lands; it
  resolves the same suffixes through its own `ResourceStream` equivalent.
- **[[FEAT-041]] (plan handle `DSK-07-15`)** — cannot attribute a golden-file failure to the
  renderer unless the inputs are proven identical; the equal-hash test is what makes that
  attribution honest.
- **[[FEAT-043]] (plan handle `DSK-07-17`)** — owns the template scope table, the upstream TICK-216
  signature record and the twelve-entry legacy negative list. This ticket carries the negative
  assertion for the client assembly as an acceptance obligation handed to it by that record.
- **`docs/current-architecture.md`** — one paragraph, after the desktop renderer ships.
- **No new CI job.** C-01 (private-repository Windows runners bill at 2×) makes that a cost
  decision, and the drift check runs inside existing lanes.
- **No OpenAPI or generated-client ripple.** This ticket touches no contract.

## Out of scope

- **Editing any `.scriban`, `report.css` or signature asset** — the governed source is read-only to
  this ticket.
- **Renderer source** — `PlaywrightAssessmentReportRenderer.cs` is not touched; [[FEAT-040]] writes
  the desktop renderer.
- **Adding the four unembedded templates** (`advert_evidence_pack`, `expert_report`, `fee_note`,
  `market_valuation_evidence`) — a scope change belonging to [[FEAT-043]]'s disposition work.
- **Deciding which signatures may ship** — [[FEAT-043]]'s record, adopted from the accepted upstream
  TICK-216 contract; this ticket embeds to it and decides nothing.
- **Making any of upstream TICK-206's twelve legacy identifiers dispatchable** — the test asserts
  the opposite.
- **A new CI job** — forbidden by C-01's cost consequence.
- **Copying any asset into `src/`** — the single source stays `docs/design/assets/report-renderer/`
  and `docs/design/brand/`.
