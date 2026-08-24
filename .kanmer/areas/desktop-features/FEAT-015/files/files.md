# Files — FEAT-015

Surveyed 2026-08-24 against fork `main` `191ddf33`. Paths marked *(created by …)*
do not exist today — `ls src` returns only `Pegasus.Core`,
`Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`; `ls tests` only
`Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`.

**This slice extends a view model it does not own.** `CaseVehicleViewModel` and
`CaseVehicleView.xaml` belong to [[FEAT-036]] (plan handle `DSK-07-10`); a second
view model for the Vehicle tab is a stop condition.

## Where the change lands

| Path | Why |
|---|---|
| `src/Pegasus.Contracts/` *(created by [[FND-029]], plan handle `DSK-02-04`)* | Vehicle and EVA DTOs: the lookup request and its status, the suggestion **with its source and obtained-at**, the mileage observations with their supplied/external/estimated classification, the provider error class from [[FEAT-045]] (plan handle `DSK-07-19`), and the handoff revision identifier. |
| `src/Pegasus.Desktop/` — `CaseVehicleViewModel`, `CaseVehicleView.xaml` *(owned by [[FEAT-036]]; created by [[FND-030]], plan handle `DSK-02-05`)* | **Add the lookup-status refresh and the EVA handoff generate and download commands in place**, changing no existing member; or create with exactly the members [[FEAT-036]] step 5 pins (`ObservableObject`, `[ObservableProperty]` partial properties, `[RelayCommand]`, and the shared Core normalisation rule reused rather than a second copy) and record which case applied. AutomationIds are fixed by `docs/desktop/06-ui-design/screen-specs.md:328-330`. |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]], plan handle `DSK-02-06`)* | Only the wiring for the streamed bundle download, which **reuses [[FEAT-014]] (plan handle `DSK-05-14`)'s transfer service** — never a second byte path. |
| `src/Pegasus.Web/` — the `/api/v1` vehicle and EVA groups only | Only where [[FEAT-035]] (plan handle `DSK-07-09`) left a gap this slice must close to consume its own contract. Behind `Features:DesktopGateway` ([[GWY-002]], plan handle `DSK-03-02`). |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]], plan handle `DSK-08-01`)* | Two suites. (1) Lookup, accept and EVA facts across the **full** provider error taxonomy using the replay adapter: success, not-found, each provider failure class, rate-limited, 401, 403, 409 stale version, replay of the same `operationKey`, and an assertion that **no provider key appears in any response**. (2) `EvaBundleContent` — the archive's entry list and JSON layout diffed against `reference/eva_information/AX_SP58WVO.json`, and the thirteen field values against both known-good samples. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[TEST-004]], plan handle `DSK-08-04`)* | Normalisation delegating to Core, each provider state rendering **distinctly**, freshness display, accept updating the case version, and EVA generate-then-download. |
| `tests/Pegasus.ArchitectureTests/` | The desktop references no provider adapter and **no second normalizer exists**. [[FND-037]] (plan handle `DSK-02-12`) owns the dependency-direction rules these sit beside. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Row `PAR-14` only. **Row `PAR-18` is written by [[FND-018]] (plan handle `DSK-01-05`)** — this ticket supplies the evidence that EVA parity covers the bundle's CONTENT and does not edit that row itself. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(created by [[DUI-013]], plan handle `DSK-06-13`)* | The EVA handoff behaviour **inside the Vehicle tab section [[FEAT-036]] creates** — a sub-heading under that section, **not a second vehicle section** — citing FRD-06 and FRD-07. |
| `docs/capabilities.md` | `DSK` rows for vehicle lookup and EVA handoff. |

## Context files

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Web/Pages/Cases/Eva/Download.cshtml.cs:16,21-28,44-50` | The download is **not a plain read**: it is `[ResponseCache(NoStore = true)]` and takes `revision`, `expectedVersion`, `operationKey`, `reason` **and** `editLeaseToken`, with `NotFound`, `Conflict` and `Refused` outcomes and a `SafeEvaFileName` guard. Note `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` shows this as a `GET`, which cannot carry a reason and a lease token — settle the shape with [[FEAT-035]] before binding. |
| `src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs:24,46,87` | The three handlers, in a 149-line page model. `:24` creates a **durable** lookup request the Worker executes — the desktop triggers and then follows status; it does not call a provider. |
| `reference/eva_information/AX_SP58WVO.json` (696 bytes) | The known-good bundle JSON: thirteen keys in a fixed order, **two-space indentation**, no trailing newline, and **no companion files** in the folder — no `manifest.sha256`, no `provenance.json`. This file is the diff target for entry list and layout. |
| `reference/eva_information/Final Format Example 02.json` (656 bytes) | The second sample, and the one that shows `Reference` carrying a work-provider claim number in a different format (`"SBL-B0492438"` versus `"1070277"`), `Inspection Address` as a real six-line postal block, and `Mileage Unit` as `"Km"` rather than `"Miles"`. Both casings are capitalised-first-letter. |
| `reference/eva_information/eva_information.md:31-45` | Why `Reference` is the work provider's number and not ours, in the operator's own words: "Case/Po - Our reference…"; "Claim no - 'Their' ref - ie the work providers reference". This is the source for upstream ENG-015 (board [[ENG-002]])'s first field fix. |
| `src/Pegasus.Core/Eva/EvaBundleSchema.cs` (916 lines) | The frozen-revision rules and the bundle shape. **Not modified by this ticket** — the content assertion detects a defect here; [[ENG-001]] and [[ENG-002]] fix it. |
| `src/Pegasus.Core/Eva/CaseEvaMapping.cs` | The case-to-EVA field mapping — the other half of what upstream ENG-015 (board [[ENG-002]]) corrects. Also out of bounds here. |
| `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs` | Where the archive is assembled and persisted, alongside `EvaHandoffEntities.cs` and `EvaHandoffModelConfiguration.cs`. Out of bounds; named so a failing assertion is routed correctly. |
| `src/Pegasus.Infrastructure/Vehicle/DvlaDvsaAdapters.cs:7,17,205-218` | `DvlaDvsaReplayAdapter`, built from a `fixtureRoot`, deserialising a `ReplayFixture` that carries a `ReplayFailure`. This is what makes each provider failure class reproducible deterministically — and it is why the Test/UAT run never needs a live provider (L-02, ADR-0014). |
| `src/Pegasus.Infrastructure/Vehicle/DvlaDvsaProductionAdapter.cs` (412 lines) | The production adapter and the shape of the provider keys. Read to understand, never to reference: `docs/desktop/05-implementation-and-migration/reuse-map.md:42-48` forbids the desktop referencing `Pegasus.Infrastructure`, and [[FND-037]] enforces it as a test. |
| `src/Pegasus.Core/Vehicle/` (`LookupContracts.cs`, `LookupWorkItem.cs`, `VehicleMileagePolicy.cs`, `VehicleWorkflow.cs`) | The registration normalisation rule the desktop **calls** rather than copies, the durable work item's shape, and the mileage policy inputs the screen must classify as supplied/external/estimated. |
| `docs/desktop/05-implementation-and-migration/reuse-map.md:36,42-48` | `Eva/` is marked **REUSE**, so the desktop ships byte-identical output; and the boundary note that permits `Pegasus.Core` for deterministic validation while forbidding `Pegasus.Infrastructure`, EF Core, Azure SDKs, Box and Graph SDKs. |
| `docs/desktop/06-ui-design/screen-specs.md:319-330` | The Vehicle tab spec, including the five provider states that must be **distinct from "not found"** — `unknown`, `stale`, `partial`, `unavailable`, `failed` — the "staff confirmation never overwritten by refresh" rule, and the four AutomationIds. |
| `docs/desktop/08-testing/test-uat-stack.md` | The local stack the replay-adapter integration check runs on, and the record that proves no live provider call was made. |
| Group document `HZN-001` / `board-conventions.md` | The join table. Board `ENG-001` is upstream ENG-014; board `ENG-002` is upstream ENG-015; upstream `ENG-001` is an unrelated post-alpha capability that was dropped and never imported. Always `upstream <ID> (board [[<board-id>]])`. |

## Ripple effects

- **OpenAPI and the generated client.** The vehicle and EVA DTOs change
  `openapi/pegasus-v1.json` and the generated client that [[FEAT-035]] and the
  contract tests bind to.
- **[[FEAT-036]] owns `CaseVehicleViewModel` and `CaseVehicleView.xaml`.** If it
  lands during this slice, the created type must be reconciled with its pinned
  shape before either merges.
- **[[FEAT-014]] (plan handle `DSK-05-14`)** owns the transfer service the bundle
  download reuses.
- **[[FEAT-045]]** owns the provider error taxonomy in `src/Pegasus.Contracts`;
  this slice consumes it and does not extend it.
- **A failing content assertion routes to two existing board tickets**, not to a
  new one: **upstream ENG-014 (board [[ENG-001]])** for packaging and indentation,
  **upstream ENG-015 (board [[ENG-002]])** for the field values, sequenced ENG-014
  then ENG-015 so the archive bytes change once.
- **`PAR-18` is [[FND-018]]'s row.** This ticket supplies the evidence that EVA
  parity covers entry list, JSON layout and the thirteen field values — not only
  the download command and frozen revisions — and does not edit the row.
- **`tests/Pegasus.IntegrationTests`** — `CaseVehicleWebTests.cs`,
  `AutomaticVehicleLookupTests.cs`, `ProductionVehicleLookupTests.cs`,
  `VehicleWorkflowTerminalTests.cs` and `EvaHandoffPersistenceTests.cs` must stay
  green; this slice changes no Razor page and no Core file.
- **`docs/frd/frd-13-desktop-operator-experience.md` does not exist yet** — it is
  authored by [[DUI-013]]; this slice contributes a **sub-heading** under
  [[FEAT-036]]'s Vehicle tab section, never a second vehicle section.

## Out of scope

- **`src/Pegasus.Core/Eva/EvaBundleSchema.cs`, `src/Pegasus.Core/Eva/CaseEvaMapping.cs`
  and `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs`.** This ticket
  **asserts** the bundle's content; upstream ENG-014 (board [[ENG-001]]) and
  upstream ENG-015 (board [[ENG-002]]) fix it.
- **`src/Pegasus.Infrastructure/Vehicle/` from the desktop.** The desktop never
  calls a provider directly.
- **A second registration normalizer.** One rule, in `src/Pegasus.Core/Vehicle/`,
  called by the desktop and re-checked by the gateway on write.
- **A second EVA mapping**, in the desktop or the gateway.
- **A second view model for the Vehicle tab, or a second byte path** for the
  bundle download.
- **A live DVLA or DVSA call in Test/UAT.** The replay adapter
  (`src/Pegasus.Infrastructure/Vehicle/DvlaDvsaAdapters.cs:7`) is the only
  provider in that stack (L-02, ADR-0014), and asking for an Azure test resource is
  out of bounds.
- **upstream ENG-013** — it arrives via the upstream sync. **upstream ENG-009**
  (Cazana valuation) — it stays backlog and must not be pulled in. **Neither has a
  fork ticket**, so neither may be written as a board wiki-link.
- **Row `PAR-18`.** [[FND-018]]'s to write.
- **Any Azure write.**
