# Research — FEAT-015: the Vehicle tab's three handlers, and why the EVA bundle's *content* is a gate

## Question

What do `Cases/Vehicle.cshtml.cs` and `Cases/Eva/Download.cshtml.cs` actually
require, and what exactly must a generated EVA bundle look like — down to key
order and indentation — for the slice to be able to say the desktop produces
byte-identical output rather than a package EVA refuses?

## Current behaviour

Read at fork `main` `191ddf33`. The implementer re-reads and records the SHA
(ticket step 2).

| Surface | `path:line` | What it does |
| --- | --- | --- |
| Request lookup | `src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs:24` `OnPostRequestVehicleLookupAsync` | Creates a durable lookup request row the Worker executes |
| Accept suggestion | `…/Vehicle.cshtml.cs:46` `OnPostAcceptVehicleSuggestionAsync` | Writes the accepted value against the case |
| Generate EVA handoff | `…:87` `OnPostGenerateEvaHandoffAsync` | `src/Pegasus.Core/Eva/` handoff generation |
| Download EVA bundle | `src/Pegasus.Web/Pages/Cases/Eva/Download.cshtml.cs:21` `OnPostAsync` | `IDownloadEvaHandoff` |

Parity-matrix rows: **`PAR-14`** (the three Vehicle handlers) and **`PAR-18`**
(the EVA download), `docs/desktop/01-inventory-and-parity/parity-matrix.md`, both
`not inventoried` with test evidence "to locate". The matrix holds
`PAR-01`…`PAR-46` (`grep -c '^| PAR-' …/parity-matrix.md` → 46).

## Findings

### Facts

- **All three Vehicle handlers are at the lines the ticket gives** —
  `grep -n "    public .*On[A-Z]" src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs`
  returns `:24`, `:46`, `:87` exactly, in a 149-line page model.
- **The EVA download is a reasoned, lease-bearing POST, not a plain read.**
  `src/Pegasus.Web/Pages/Cases/Eva/Download.cshtml.cs:21-28` takes
  `Guid id, int revision, long expectedVersion, string operationKey, string
  reason, string editLeaseToken`; `:42-43` calls
  `downloadEvaHandoff.ExecuteAsync(new(id, revision, expectedVersion, actor,
  operationKey, reason, editLeaseToken), …)`. Its outcomes include `NotFound`,
  `Conflict` and `Refused` (`:44-50`), and the file name passes through
  `SafeEvaFileName`. The page is `[ResponseCache(NoStore = true)]` (`:16`).
  **Note:** `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` EVA
  row shows the download as `GET /cases/{id}/eva-handoff/{revision}/bundle`.
  A `GET` cannot carry a required reason and an edit-lease token, so the endpoint's
  shape must accommodate them — that is [[FEAT-035]] (plan handle `DSK-07-09`)'s
  contract to settle, recorded in Risks.
- **`EvaBundleSchema.cs` is 916 lines** and `CaseEvaMapping.cs` sits beside it in
  `src/Pegasus.Core/Eva/`. The persistence side is
  `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs`, with
  `EvaHandoffEntities.cs` and `EvaHandoffModelConfiguration.cs`.
  `docs/desktop/05-implementation-and-migration/reuse-map.md:36` marks `Eva/`
  **REUSE** — so the desktop must ship byte-identical output.
- **The known-good corpus is in the repository and is small enough to diff.**
  `ls reference/eva_information/` returns `AX_SP58WVO.json` (696 bytes),
  `Final Format Example 02.json` (656 bytes), `eva_information.md` (34,482 bytes)
  and `screenshots/`. **No companion files** — no `manifest.sha256`, no
  `provenance.json`.
- **The thirteen keys, in the order both samples use**, read from
  `reference/eva_information/AX_SP58WVO.json`: `Work Provider`, `VRM`,
  `Vehicle Model`, `Claimant Name`, `Reference`, `Incident Date`,
  `Instruction Date`, `Inspection Date`, `Inspection Address`,
  `Accident Circumstances`, `VAT Status`, `Mileage`, `Mileage Unit`. Both files are
  **two-space indented** and neither ends with a trailing newline.
- **The four values upstream ENG-015 (board [[ENG-002]]) exists to fix are visible
  in the samples.** `Reference` carries the **work provider's** claim number —
  `"1070277"` in `AX_SP58WVO.json`, `"SBL-B0492438"` in `Final Format Example
  02.json` — not our case reference; `eva_information.md:31-45` states the
  distinction in the operator's own words ("Case/Po - Our reference…", "Claim no -
  'Their' ref - ie the work providers reference"). `Inspection Address` is a
  **six-line block**: `"Image-based Assessment\n\n\n\n\n"` in one sample and
  `"109 Valley View\nHoole\n\n\n\nCH490DJ"` in the other — five `\n` separators,
  six lines, in both. `Vehicle Model` carries **make and model**:
  `"HONDA CIVIC TYPE-R GT I-VTEC"`, `"Skoda Superb"`. `Mileage Unit` casing is
  `"Miles"` and `"Km"` — capitalised first letter, not upper-case, not lower-case.
- **The provider adapters and their replay variant are real.**
  `src/Pegasus.Infrastructure/Vehicle/DvlaDvsaProductionAdapter.cs` is 412 lines;
  `DvlaDvsaAdapters.cs` is 222 lines and declares `DvlaDvsaReplayAdapter` at `:7`,
  constructed from a `fixtureRoot` (`:17`) and deserialising a `ReplayFixture`
  (`:205-216`) that carries a `ReplayFailure` (`:218`). The replay adapter is
  therefore able to reproduce each provider failure class deterministically —
  which is what the tier-5 matrix needs and what keeps L-02 satisfiable.
- **The Core vehicle folder is four files** —
  `src/Pegasus.Core/Vehicle/LookupContracts.cs`, `LookupWorkItem.cs`,
  `VehicleMileagePolicy.cs`, `VehicleWorkflow.cs`. The registration normalisation
  the desktop reuses lives in Core, and
  `docs/desktop/05-implementation-and-migration/reuse-map.md:42-48` explicitly
  permits `Pegasus.Desktop` to reference `Pegasus.Core` "for deterministic local
  validation and calculations", while forbidding `Pegasus.Infrastructure`, EF
  Core, Azure SDKs, Box and Graph SDKs.
- **The screen spec is detailed and already names the provider states.**
  `docs/desktop/06-ui-design/screen-specs.md:319-330`: VRM plate;
  make/model/colour/year from lookup with source/version/age chips; MOT/mileage
  observations classified supplied/external/estimated; suggestion rows with Accept
  where "staff confirmation [is] never overwritten by refresh"; a Request lookup
  command with provider state **distinct from "not found"** — the named states are
  `unknown`, `stale`, `partial`, `unavailable`, `failed`; inspection address with
  provider-determined mode and reasoned per-case override; engineer allocation from
  the EVA proxy with its limitation; and Generate EVA handoff (once-per-case
  proxy) plus Download. AutomationIds: `Case.Vehicle.Lookup`,
  `Case.Vehicle.Suggestion.Accept.<Key>`, `Case.Vehicle.Address.Mode`,
  `Case.Vehicle.Eva.Generate`.
- **Existing test evidence exists but is not the bundle's content.**
  `tests/Pegasus.IntegrationTests/` holds `CaseVehicleWebTests.cs`,
  `AutomaticVehicleLookupTests.cs`, `ProductionVehicleLookupTests.cs`,
  `VehicleWorkflowTerminalTests.cs` and `EvaHandoffPersistenceTests.cs`;
  `tests/Pegasus.Core.Tests/Vehicle/` exists. **None of them diffs a generated
  archive against the operator corpus** — which is why the content assertion in
  step 10 is a gate rather than a re-run.
- **Both refs on this ticket are real files.**
  `docs/frd/frd-06-vehicle-and-engineering-evidence.md` and
  `docs/frd/frd-07-eva-and-external-engineering-handoff.md` both exist
  (`ls docs/frd`).
- **The two fixing tickets are already on the board and their ids do not match
  upstream.** Board `ENG-001` is **upstream ENG-014** (drop the invented
  `manifest.sha256` and `provenance.json`; indent the JSON), on the upstream branch
  `task/eng-014-drop-manifest-indent-json` against `dev` — **not** in [[FND-023]]
  (plan handle `DSK-01-10`)'s 32-commit `main` range. Board `ENG-002` is **upstream
  ENG-015** (the four wrong field values), with **no upstream branch at all**.
  Neither arrives by sync, so under D-001 both exist only because the fork board
  holds them.
- **The projects this slice writes into do not exist yet.** `ls src` returns only
  `Pegasus.Core Pegasus.Infrastructure Pegasus.Web Pegasus.Worker`; `ls tests`
  only `Pegasus.ArchitectureTests Pegasus.Core.Tests Pegasus.IntegrationTests`.
  `CaseVehicleViewModel` and `CaseVehicleView.xaml` are [[FEAT-036]] (plan handle
  `DSK-07-10`)'s to create.

### Assumptions

- **A-05-15-1 — [[FEAT-035]] carries the cache lifetime and the provenance
  fields (source and obtained-at) on the lookup responses.** The screen spec needs
  them for the source/version/age chips and for freshness without hover. Confirmed
  by: reading the generated client at step 3. Breaks if: they are absent — the
  freshness display would then be a client-side guess, which the design authority
  forbids; stop and raise on [[FEAT-035]].
- **A-05-15-2 — [[FEAT-045]] (plan handle `DSK-07-19`)'s provider error taxonomy
  is applied to these endpoints**, so `terminal` / `transient` / `unknown` arrive
  alongside `not-found`, `invalid-request`, `not-authorized`, `rate-limited` and
  `unavailable`. Confirmed by: step 4. Breaks if: a provider failure is
  indistinguishable from a genuine not-found in the contract — the desktop would
  have to infer it, which the acceptance forbids.
- **A-05-15-3 — the seeded Test/UAT case can produce a bundle comparable with the
  operator corpus.** Confirmed by: generating one at step 10 and diffing. Breaks
  if: no seeded case maps onto the sample shapes — then the content assertion
  cannot run and the ticket stops rather than signing off on an unverified bundle.
- **A-05-15-4 — [[FEAT-036]] may or may not have landed `CaseVehicleViewModel`.**
  Both cases are legitimate and handled at step 7: extend in place, or create with
  exactly the members that ticket's step 5 pins, and record which applied. Breaks
  if: it lands mid-slice — then the created type is reconciled with its pinned
  shape before either merges.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | Accepting a suggestion writes against the case with an `expectedVersion`, and the EVA download itself carries `expectedVersion` **and** an `editLeaseToken` (`src/Pegasus.Web/Pages/Cases/Eva/Download.cshtml.cs:21-28`), with a `Conflict` outcome (`:47`). Lands in the gateway (L-01, ADR-0103). |
| Unattended execution — must it run with every desktop closed? | **yes** | A lookup request becomes a durable work item the **Worker** executes, and `docs/desktop/05-implementation-and-migration/vertical-slices.md:528-531` records that the Worker reconciliation sweep enqueues lookups. Lands in the existing `src/Pegasus.Worker` (ADR-0106) — untouched by this slice. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | The DVLA and DVSA API keys behind `src/Pegasus.Infrastructure/Vehicle/DvlaDvsaProductionAdapter.cs` (412 lines). Lands behind the gateway (ADR-0107); `reuse-map.md:42-48` forbids the desktop referencing `Pegasus.Infrastructure` at all, and [[FND-037]] (plan handle `DSK-02-12`) enforces it. |
| Public callback — must an external service call a stable public endpoint? | **no** | DVLA and DVSA are called outbound and answer inline or through the durable request row; neither calls back. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | `StaffAccessRight.PerformCasework` (`src/Pegasus.Core/Identity/StaffAuthorization.cs:10`), the edit lease and version on the EVA download, the reason it requires, and the frozen-revision rule in `src/Pegasus.Core/Eva/EvaBundleSchema.cs` (916 lines) must hold whatever the client is. Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **no** | There is no measurement in this repository showing a central lookup cache is materially better. The shared cache is justified by the two "yes" answers above — protected credentials and a rate-limited provider — not by a measured advantage, and saying otherwise would be an unevidenced claim. |

Conclusion: four "yes" answers place the provider keys, the shared lookup cache,
the EVA generation and the audit in the gateway (L-01, ADR-0107), and the durable
lookup execution in the existing Worker (ADR-0106). Registration entry, provider-
state rendering, freshness display and the streamed bundle download belong in the
desktop. No new Azure resource; no Azure write; and no live provider call in
Test/UAT (L-02, ADR-0014).

## Implications

- **The content assertion is the point of the slice, not a nicety.** Without it
  this ticket can pass every other gate and still sign off on a package EVA
  rejects — which is precisely what happened upstream on 2026-08-24 exporting
  `ap.QDOS26015`. Two things are pinned: (a) the archive's entry list — the
  thirteen-key JSON plus `Images/` and nothing else — and the JSON's layout,
  two-space indentation with the same key set and key order, diffed against
  `reference/eva_information/AX_SP58WVO.json`; and (b) the thirteen field values
  against both known-good samples.
- **A failure of that assertion is a finding, not a fix here.** Packaging and
  indentation belong to **upstream ENG-014 (board [[ENG-001]])**, field values to
  **upstream ENG-015 (board [[ENG-002]])**, both in `src/Pegasus.Core/Eva/` and
  `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs`, sequenced ENG-014
  then ENG-015 so the archive bytes change once. Writing a second EVA mapping in
  the desktop or the gateway is a stop condition.
- **One normalisation rule, and it is Core's.** `reuse-map.md:42-48` permits a
  direct `Pegasus.Core` reference for deterministic validation, so the desktop
  calls the existing rule from `src/Pegasus.Core/Vehicle/`; the gateway re-checks
  on write. A second normaliser is a stop condition and
  `tests/Pegasus.ArchitectureTests` asserts its absence.
- **Provider state is contract data, not a client inference.** The screen spec
  names five states plus not-found; [[FEAT-045]]'s taxonomy supplies them; the
  desktop renders each distinctly and never one generic "failed".
- **Freshness must be readable without hovering.** The header control from
  [[DUI-012]] (plan handle `DSK-06-12`) carries it, and
  `docs/design/README.md`'s rule that permanent consequences must be visible
  without hover or colour alone applies to the cached-versus-fresh distinction too.
- **The download is reasoned and lease-bearing today.** Whatever verb the
  endpoint ends up using, it must carry `revision`, `expectedVersion`,
  `operationKey`, `reason` and `editLeaseToken`, and must surface `NotFound`,
  `Conflict` and `Refused` distinctly.
- **This slice extends a view model it does not own.** `CaseVehicleViewModel` and
  `CaseVehicleView.xaml` are [[FEAT-036]]'s; a second view model for the Vehicle
  tab is a stop condition.

## Open questions

None that block. The three points that could look like questions each have a
named owner or a measured answer:

- The endpoint map shows the EVA download as a `GET`, while the current page is a
  reasoned, lease-bearing `POST`. The **contract** is [[FEAT-035]]'s to settle;
  this slice records the requirement and consumes whatever shape carries all six
  parameters. Recorded in the plan's Risks.
- A failing content assertion is routed to [[ENG-001]] and [[ENG-002]], which are
  already on the board and need finding by those board ids, not creating.
- Whether `CaseVehicleViewModel` already exists is answered by looking, and both
  answers have a defined action (step 7).
