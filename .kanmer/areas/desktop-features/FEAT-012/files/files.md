# Files — FEAT-012

Surveyed 2026-08-24 against fork `main` `191ddf33`. Paths marked *(created by …)*
do not exist today — `ls src` returns only `Pegasus.Core`,
`Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`; `ls tests` only
`Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`.

## Where the change lands

| Path | Why |
|---|---|
| `src/Pegasus.Contracts/` *(created by [[FND-029]], plan handle `DSK-02-04`)* | Unidentified and image-intake DTOs: queue row (which must tolerate a **missing** origin receipt — see Context), detail, resolve request **bound to [[GWY-013]]'s shape, not redesigned**, close request, VRM suggestion with confidence-free presentation fields, and the candidate case list with reference and status. |
| `src/Pegasus.Desktop/` *(created by [[FND-030]], plan handle `DSK-02-05`)* | `UnidentifiedListViewModel`, `UnidentifiedDetailViewModel`, `VehicleImagesListViewModel`, `VehicleImagesDetailViewModel` and their XAML, on the [[DUI-007]] (plan handle `DSK-06-07`) data-table pattern with [[DUI-009]] (plan handle `DSK-06-09`) reason dialogs. AutomationIds are fixed by `docs/desktop/06-ui-design/screen-specs.md:298-307`: `Unidentified.Resolve`, `VehicleImages.Suggestions`, `VehicleImages.Close`, plus the new promote control's id. |
| `src/Pegasus.Desktop/` — shell rail | Both queues added under Queues in the route order from `screen-specs.md` § `Shell`, with counts from the rail-counts endpoint. An absent count renders **nothing**. |
| `src/Pegasus.Web/` — the `/api/v1` unidentified and image-intake groups only | Only where [[GWY-013]] (plan handle `DSK-03-13`) left a gap this slice must close to consume its own contract. Behind `Features:DesktopGateway` ([[GWY-002]], plan handle `DSK-03-02`). |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]], plan handle `DSK-08-01`)* | Both queues' list/detail/resolve/close/source facts, the count-exclusion assertion, and the promote path's five cases — opens exactly one Triage from the originating receipt; an invalid registration opens nothing; a receipt that already has a Triage does not gain a second; `registration` with a non-`Triage` `targetKind` is a validation failure; an ordinary resolve with no `registration` is unchanged. These mirror [[GWY-013]] step 12's facts against the **generated client**: if one fails here but passes there, the client binding is wrong, not the endpoint. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[TEST-004]], plan handle `DSK-08-04`)* | List paging, reason-required resolve and close, the promote command's `CanExecute` and its refusal path, conflict handling through the shared [[FEAT-008]] (plan handle `DSK-05-08`) pattern, and correct vocabulary on every state. |
| `tests/Pegasus.ArchitectureTests/` | The no-second-implementation facts: no second streaming download, no second registration normaliser or validator. [[FND-037]] (plan handle `DSK-02-12`) owns the dependency-direction rules these sit beside. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Rows `PAR-25` and `PAR-26` — both `not inventoried` today with test evidence "to locate". |
| `docs/desktop/06-ui-design/screen-specs.md` § `Unidentified and Vehicle images` → `Unidentified detail` (`:298-307`) | Add the promote control (supply a vehicle registration, open the Triage) with its AutomationId. The section today lists the `U<n>` reference, canonical reason and open/resolved state only (upstream INTK-035). **This block is this ticket's**; the matching `endpoint-map.md` resolve row is [[GWY-013]]'s and is not written here. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(created by [[DUI-013]], plan handle `DSK-06-13`)* | Unidentified and vehicle-images section. The file does not exist today. |
| `docs/capabilities.md` | `DSK` rows for both queues. |

## Context files

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Web/Pages/Unidentified/Index.cshtml.cs` (19 lines) | **It is not a list.** Its whole body is `RedirectPermanent("/Triage?queue=unidentified")`. The list moved onto the Queues page as a tab (upstream INTK-009); read `Triage/Index.cshtml.cs` for the real behaviour before assuming this file has any. |
| `src/Pegasus.Web/Pages/Triage/Index.cshtml.cs:249-274` | The real Unidentified queue: `_unidentifiedStore.ListQueueAsync(null, ct)` at `:249`, `UnidentifiedCount` at `:253`, and — importantly — the media-kind filter at `:263-274` **filters the count query's own result rather than re-querying**, so the count and the rows can never disagree. A desktop that queries twice loses that property. |
| `src/Pegasus.Infrastructure/Persistence/EfUnidentifiedStore.cs:245-270` | Where the exclusion rule actually lives: `where item.State == openState` (`:250`, `:259`). An item resolved to a case is no longer `Open` and leaves both the queue and the count. Also `:252-254` explains why the receipt join is a **left** join — the origin can be a submission group — so `MapQueueRow` (`:272`) takes a nullable receipt and a row may have no file name, subject or sender. |
| `src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs:397-399,440` | `MaximumReasonLength = 500`, `MaximumOperationKeyLength = **200**`, `MaximumTargetIdLength = 200`, enforced by `RequireOperation` at `:440`. The administration bound is 100 — a single shared client constant would be wrong in one of the two areas. |
| `src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs:245-253` | `ResolveUnidentifiedRequest` **as it is today**: `TargetId` is a non-nullable `string` and there is no `registration` member. The optional `registration` and the conditionally-absent `targetId` are what [[GWY-013]] adds — this is the "before" you are binding away from. |
| `src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs:362-390` | `EnsureDestinationExistsAsync`. Its `Triage` branch (`:375-377`) resolves an existing Triage by id and throws `UnidentifiedResolutionTargetNotFoundException` if absent — so resolving to a Triage presupposes one exists, which is precisely why nothing staff can do opens it today (upstream INTK-035). |
| `src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs:416-436` | `UnidentifiedValidation.ValidateResolve` — the one validator. Reason, operation key, target id and target reference bounds, plus `Enum.IsDefined(request.TargetKind)`. The desktop adds none of this. |
| `src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs:33-40` | The five `UnidentifiedResolutionTargetKind` values. `ExternalReference` is the only one that validates nothing (`:383`). |
| `src/Pegasus.Core/ImageIntake/ImageIntakeLifecycle.cs:169-174` | The file states in its own summary that `NormalizeRegistrationInput` is **"the one owner"** of turning staff-typed registration input into normalized form. A desktop-side normaliser would be a second owner and is a stop condition. |
| `src/Pegasus.Core/Triage/TriageContracts.cs:79-84,138,288-294` | `CreateTriageFromIntakeRequest` takes a **normalized** registration; `ICreateTriageFromIntake` is the interface; and `ITriageQueries` has `ListAsync` and `GetAsync` **only** — `GetByOriginReceiptAsync` does not exist in the fork and arrives with upstream INTK-033 (board [[INTK-007]]). |
| `src/Pegasus.Core/Intake/DurableIntake.cs:418,423,893` | `ProcessQueuedIntake`, the `ICreateTriageFromIntake` injection, and `CreateTriageIfQualifyingAsync` — today the interface's **only** caller. That is the evidence that no staff-initiated path exists. |
| `src/Pegasus.Web/Pages/ImageIntake/Index.cshtml.cs:36-73` | Three query modes: `associated` filter (`null`/`""`/`"yes"`/`"no"`, anything else 404s), exact `GetByReferenceAsync`, and a compacted `SearchByRegistrationAsync`. **No paging** — the endpoint map adds `?page`, so the desktop must not assume the whole set arrives. |
| `src/Pegasus.Web/Pages/ImageIntake/Index.cshtml.cs:76-84` | `OutcomeLabel` adds only dash-continuation phrasing and says why: "so a second copy of the state vocabulary never grows here." The desktop follows the same rule through [[FEAT-023]] (plan handle `DSK-05-23`)'s list. |
| `docs/operator-notes.md:42` | The rule the promote path closes, in the operator's own words: keep it **Unidentified** "until a vehicle registration is known, then open the Triage". |
| `docs/design/README.md:535-546` | The settled vocabulary table. `NeedsSorting` → **`Unidentified`**; "Needs sorting" is the retired internal name and must never reach an operator. |
| `docs/desktop/06-ui-design/screen-specs.md:298-307` | Both details' section lists and the three AutomationIds — and the fact that **no promote control is listed**, which is the gap this ticket documents. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Triage, Unidentified, Operations` and § `Intake (received items), uploads, image intake` | The seven routes this slice consumes, and the note that unidentified operation keys are "≤ 200". |
| `tests/Pegasus.IntegrationTests/UnidentifiedPersistenceTests.cs` (259 lines), `UnidentifiedReconciliationTests.cs`, `ImageIntakePersistenceTests.cs`, `ImageIntakeWebTests.cs` | The existing persistence-side evidence. Read before writing the count-exclusion assertion — the `Open`-state property is already exercised there. |

## Ripple effects

- **OpenAPI and the generated client.** The DTOs change
  `openapi/pegasus-v1.json` and the generated client that [[GWY-013]] and the
  contract tests bind to. Because this slice's promote-path facts deliberately
  mirror [[GWY-013]] step 12's, a divergence between the two suites localises the
  fault to the client binding.
- **`src/Pegasus.Core/Triage/**` and `src/Pegasus.Core/Intake/Unidentified/**`
  change under [[GWY-013]], not here.** The widened resolve contract, the promote
  orchestration and the origin-receipt lookup are all that ticket's.
- **`tests/Pegasus.IntegrationTests`** — the Unidentified and image-intake
  persistence tests must stay green; this slice changes no Razor page and no Core
  file.
- **[[FEAT-009]] (plan handle `DSK-05-09`)** owns the streaming download service
  reused for member source access. A copy is a stop condition and
  `tests/Pegasus.ArchitectureTests` asserts it.
- **[[FEAT-016]] (plan handle `DSK-05-16`)** owns the one gallery and viewer; the
  Vehicle images detail's image rendering binds to it rather than growing its own.
- **The shell rail** gains two entries; the rail-counts endpoint is
  [[GWY-007]]/`GET /api/v1/dashboard/rail-counts`'s and an absent count must
  render nothing rather than a zero.
- **`docs/frd/frd-13-desktop-operator-experience.md` does not exist yet** — it is
  authored by [[DUI-013]]; contribute the section there if it has not landed.

## Out of scope

- **`src/Pegasus.Infrastructure/Vision/`** — the ONNX VRM engine stays
  server-side (ADR-0019). Whether it should move to the desktop is the
  [[FEAT-044]] (plan handle `DSK-07-18`) spike, not this slice.
- **`src/Pegasus.Core/Triage/**`** and **`src/Pegasus.Core/Intake/Unidentified/**`**
  — [[GWY-013]] owns the resolve contract, the promote orchestration and
  `ITriageQueries.GetByOriginReceiptAsync`.
- **Any registration normaliser, format check, Triage-creation call or
  origin-receipt lookup in this slice.** One normaliser and it is
  `ImageIntakeLifecycle.NormalizeRegistrationInput`
  (`src/Pegasus.Core/ImageIntake/ImageIntakeLifecycle.cs:174`); one judge of
  validity and it is `TriageLifecycleRules.ValidateCreate`; one Triage-creation
  call and it is the gateway's.
- **A second retention of the origin receipt's content.** The promote path opens
  a Triage from the originating receipt and retains nothing again.
- **The `endpoint-map.md` resolve row.** [[GWY-013]]'s to write.
- **A second streaming download implementation.**
- **Any Azure write.**
- **upstream INTK-035 as a fork ticket** — it was not imported, has **no fork
  ticket**, and is absorbed here and in [[GWY-013]]. It is not any board
  `INTK-0nn`: the board's `INTK-001`…`INTK-007` are upstream INTK-002, INTK-003,
  INTK-026, INTK-027, INTK-031, INTK-032 and INTK-033.
