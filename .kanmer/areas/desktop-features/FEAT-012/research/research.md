# Research — FEAT-012: the two small queues, and the promote path that does not exist yet

## Question

What do the four Unidentified and Vehicle-images page models actually do — in
particular, where does the "counts exclude receipts that produced a case" rule
live — and exactly how much of the upstream INTK-035 promote path is already
built, so this slice renders a control over [[GWY-013]]'s contract instead of
growing a second registration validator?

## Current behaviour

Read at fork `main` `191ddf33`. The implementer re-reads and records the SHA
(ticket step 2).

| Surface | `path:line` | What it does |
| --- | --- | --- |
| Unidentified list | `src/Pegasus.Web/Pages/Unidentified/Index.cshtml.cs` (19 lines) | **A permanent redirect**, not a list: `OnGet()` returns `RedirectPermanent("/Triage?queue=unidentified")`. Its own comment says the list moved onto the Queues page as a tab (upstream INTK-009) and the route is kept because staff may have bookmarked it. |
| Unidentified queue (the real list) | `src/Pegasus.Web/Pages/Triage/Index.cshtml.cs:249` | `_unidentifiedStore.ListQueueAsync(null, cancellationToken)`; `:253` sets `UnidentifiedCount`; `:261-274` filters that same result set by media kind rather than re-querying. |
| Unidentified detail | `src/Pegasus.Web/Pages/Unidentified/Details.cshtml.cs` (180 lines) | `OnGetAsync` plus `OnPostResolveAsync` |
| Vehicle images list | `src/Pegasus.Web/Pages/ImageIntake/Index.cshtml.cs` (85 lines) | `IImageIntakeQueries.ListAsync(Associated, …)`, plus an exact-reference lookup (`GetByReferenceAsync`) and a registration search (`SearchByRegistrationAsync`) |
| Vehicle images detail | `src/Pegasus.Web/Pages/ImageIntake/Details.cshtml.cs` (89 lines) | `OnGetAsync` plus `OnPostCloseAsync` |

Parity-matrix rows: **`PAR-25`** (Unidentified index and detail) and **`PAR-26`**
(image intake), `docs/desktop/01-inventory-and-parity/parity-matrix.md`, both
`not inventoried` with test evidence "to locate". The matrix holds
`PAR-01`…`PAR-46` (`grep -c '^| PAR-' …/parity-matrix.md` → 46).

## Findings

### Facts

- **The Unidentified "list" page is a redirect.**
  `src/Pegasus.Web/Pages/Unidentified/Index.cshtml.cs` is 19 lines and its whole
  body is `RedirectPermanent("/Triage?queue=unidentified")`. The list that
  actually renders is the fifth tab of `src/Pegasus.Web/Pages/Triage/Index.cshtml.cs`
  (upstream INTK-009), whose own comment at `:24-26` says Unidentified "is
  unresolved retained material, not a case stage, but it is queue work the same
  way the other four tabs are".
- **The exclusion rule is a state filter, in the store.**
  `src/Pegasus.Infrastructure/Persistence/EfUnidentifiedStore.cs:250` sets
  `var openState = UnidentifiedState.Open.ToString();` and `:259` filters
  `where item.State == openState`. An item that resolved to a case is no longer
  `Open`, so it leaves the queue and the count. That is where "counts exclude
  receipts that produced a case" lives — **not** in the page model, and not in a
  join against `CaseIntakeLinks`.
- **The queue join is deliberately a left join.**
  `EfUnidentifiedStore.cs:252-254` comments that no foreign key is modelled
  between an Unidentified item and its origin, because the origin can be a receipt
  **or** a submission group. `MapQueueRow` (`:272`) therefore takes a nullable
  receipt and `UnidentifiedMediaKindPolicy.Classify` owns the no-receipt fallback.
  A desktop list must tolerate a row with no receipt.
- **The count and the filtered rows come from one query.**
  `Triage/Index.cshtml.cs:263-274` filters the count query's own result rather
  than re-querying. A desktop that asks for a filtered list and a separate count
  would make two queries where the web makes one, and could show a count that
  disagrees with its own rows.
- **The operation-key bound here is 200, not 100.**
  `src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs:398` holds
  `public const int MaximumOperationKeyLength = 200;` alongside
  `MaximumReasonLength = 500` (`:397`) and `MaximumTargetIdLength = 200` (`:399`).
  `RequireOperation` at `:440` enforces it. The administration bound is 100 — the
  two are different and the difference is a real trap.
- **`ResolveUnidentifiedRequest` requires `TargetKind` *and* `TargetId` today.**
  `UnidentifiedContracts.cs:245-253` declares
  `(UnidentifiedItemId, ExpectedVersion, Actor, OperationKey, Reason, TargetKind,
  TargetId, TargetReference, ResolvedAtUtc)`; `TargetId` is a non-nullable
  `string` and `ValidateResolve` at `:426` calls
  `RequireText(request.TargetId, MaximumTargetIdLength, …)`. There is no
  `registration` member. The optional `registration` and the optional `TargetId`
  are what [[GWY-013]] (plan handle `DSK-03-13`) adds.
- **`EnsureDestinationExistsAsync`'s `Triage` branch requires the Triage to
  already exist.** `UnidentifiedContracts.cs:375-377` resolves
  `UnidentifiedResolutionTargetKind.Triage` by `triageQueries.GetAsync(triageId, …)`
  and throws `UnidentifiedResolutionTargetNotFoundException` if it is absent
  (`:388-391`). So resolving to a Triage today presupposes one; nothing in the
  staff path can open it.
- **`UnidentifiedResolutionTargetKind` has five values** —
  `InstructionCase`, `ImageIntake`, `Triage`, `BlockedIntake`, `ExternalReference`
  (`UnidentifiedContracts.cs:33-40`). `ExternalReference` is the only branch that
  validates nothing (`:383`, "Free-form external reference; no Core-owned
  destination to validate").
- **`ICreateTriageFromIntake` takes a *normalized* registration.**
  `src/Pegasus.Core/Triage/TriageContracts.cs:138` is the interface; `:79-84` is
  `CreateTriageFromIntakeRequest(TriageOrigin Origin, string
  NormalizedVehicleRegistration, IntakeEvidence AcceptedMatchEvidence, string
  Actor, string OperationKey)`. `src/Pegasus.Core/Triage/TriageLifecycle.cs:5-16`
  (`CreateTriageFromIntake`) calls `TriageLifecycleRules.ValidateCreate`.
- **There is exactly one owner of staff-typed registration normalisation, and it
  says so.** `src/Pegasus.Core/ImageIntake/ImageIntakeLifecycle.cs:169-173` is a
  summary comment reading "The one owner for turning staff-typed registration
  input into the normalized form `ValidateNormalizedRegistration` accepts:
  uppercase ASCII letters and digits, separators removed."
  `NormalizeRegistrationInput` is at `:174`.
- **`ICreateTriageFromIntake` has one caller today.**
  `src/Pegasus.Core/Intake/DurableIntake.cs:423` injects it into
  `ProcessQueuedIntake` (`:418`), and `CreateTriageIfQualifyingAsync` at `:893` is
  the only call site. There is no manual creation page and no MCP tool for it —
  which is exactly upstream INTK-035's finding.
- **`ITriageQueries` cannot find a Triage by origin receipt.**
  `TriageContracts.cs:288-294` declares `ListAsync` and `GetAsync` only.
  `GetByOriginReceiptAsync` arrives with upstream INTK-033 (board [[INTK-007]]),
  and resolving it after [[FND-023]] (plan handle `DSK-01-10`)'s sync is
  [[GWY-013]] step 8's.
- **The operator rule the promote path closes is written down.**
  `docs/operator-notes.md:42` says to keep material **Unidentified** "until a
  vehicle registration is known, then open the Triage". Today nothing staff can do
  opens it.
- **The vehicle-images list has three query modes and no paging.**
  `ImageIntake/Index.cshtml.cs:44-73`: an empty query lists by the `associated`
  filter; a non-empty query first tries an exact Image Intake Reference
  (`GetByReferenceAsync`), and otherwise compacts the input to letters and digits
  and calls `SearchByRegistrationAsync`. The `associated` filter accepts only
  `null`, `""`, `"yes"` or `"no"` and returns 404 for anything else (`:36-39`).
- **The vehicle-images row label already routes through `OperatorLabels`.**
  `ImageIntake/Index.cshtml.cs:76-84` `OutcomeLabel` adds only the
  dash-continuation phrasing, with a comment saying "so a second copy of the
  state vocabulary never grows here". The desktop follows the same rule.
- **Settled vocabulary is exact and case-sensitive.**
  `docs/design/README.md:535-546` maps `IntakeDecision.NeedsSorting` → the
  operator label **`Unidentified`**. "Needs sorting" is the retired internal name.
- **The screen spec exists.**
  `docs/desktop/06-ui-design/screen-specs.md:298-307` § `Unidentified and Vehicle
  images` lists the `U<n>` reference, canonical reason, open/resolved history,
  group members with source download and a reasoned Resolve; the Vehicle images
  detail's Image reference plate, VRM suggestions
  (source-image/confirmed/no-result), preserved group evidence, merge history,
  registration-matched eligible cases while unassociated, and a reasoned Close.
  The AutomationIds are `Unidentified.Resolve`, `VehicleImages.Suggestions`,
  `VehicleImages.Close`. **There is no promote control in that list** — adding it
  is this ticket's documentation change.
- **The projects this slice writes into do not exist yet.** `ls src` returns only
  `Pegasus.Core Pegasus.Infrastructure Pegasus.Web Pegasus.Worker`; `ls tests`
  only `Pegasus.ArchitectureTests Pegasus.Core.Tests Pegasus.IntegrationTests`.
  `tests/Pegasus.IntegrationTests/UnidentifiedPersistenceTests.cs` (259 lines),
  `UnidentifiedReconciliationTests.cs`, `ImageIntakePersistenceTests.cs` and
  `ImageIntakeWebTests.cs` exist and are the persistence-side evidence.

### Assumptions

- **A-05-12-1 — [[GWY-013]] lands the widened resolve contract in the shape the
  ticket restates**: `expectedVersion`, `operationKey` (≤ 200), `reason`, a
  required `targetKind`, `targetId`, an optional `targetReference` and one new
  **optional** `registration`, with `registration` accepted only when
  `targetKind = Triage` and `targetId` absent in that case. Confirmed by: reading
  the generated client at step 6. Breaks if: the contract is not there — then this
  slice **stops and raises it on [[GWY-013]]** rather than building around it.
- **A-05-12-2 — the promote path's Triage-reuse rule is enforced server-side.**
  A receipt that already has a Triage must not gain a second. Confirmed by: the
  contract test at step 10. Breaks if: [[GWY-013]] cannot look a Triage up by
  origin receipt because `GetByOriginReceiptAsync` has not arrived — which is that
  ticket's step 8 to resolve, and the reason it is called out in this ticket's
  body.
- **A-05-12-3 — the desktop can render VRM suggestions without confidence
  numbers.** The screen spec says "source-image/confirmed/no-result distinction",
  not a score. Confirmed by: the DTO carrying presentation fields only. Breaks if:
  the underlying record has no such distinction — then the DTO shape is raised on
  [[GWY-013]] rather than invented here.
- **A-05-12-4 — the list-and-count consistency the web achieves by filtering one
  result set (`Triage/Index.cshtml.cs:263-274`) is reproducible over the paged
  `GET /api/v1/unidentified?page` contract.** Confirmed by: the count-exclusion
  assertion at step 10. Breaks if: the endpoint returns a count computed
  separately from its rows — then the two can disagree, and that is raised on
  [[GWY-013]].

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | Both queues are shared work with optimistic versions: `ResolveUnidentifiedRequest.ExpectedVersion` (`UnidentifiedContracts.cs:247`) and the image-intake close's `expectedVersion`. Lands in the gateway (L-01, ADR-0103). |
| Unattended execution — must it run with every desktop closed? | **yes** | `src/Pegasus.Core/Intake/ReconcileUnidentifiedDestinations.cs` and `src/Pegasus.Core/ImageIntake/ImageIntakeChaseSchedule.cs` are Worker-executed sweeps, and Triage creation from a qualifying receipt runs at `src/Pegasus.Core/Intake/DurableIntake.cs:893`. Lands in the existing `src/Pegasus.Worker` (ADR-0106) — untouched by this slice. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | The artifact-store credential behind member source downloads. Lands behind the gateway (ADR-0107); the desktop streams through `/api/v1`. |
| Public callback — must an external service call a stable public endpoint? | **no** | Nothing external calls into either queue. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | `StaffAccessRight.PerformCasework` (`src/Pegasus.Core/Identity/StaffAuthorization.cs:10`), the operation-key bound of 200 (`UnidentifiedContracts.cs:398`), `UnidentifiedValidation.ValidateResolve` (`:416-436`), `EnsureDestinationExistsAsync` (`:362-390`) and `TriageLifecycleRules.ValidateCreate` must all hold whatever the client is. Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement supports rendering two small queues centrally. The one thing that *is* measured-adjacent — the ONNX VRM engine — stays server-side for a different reason (`src/Pegasus.Infrastructure/Vision/`, ADR-0019), and whether it should move is the [[FEAT-044]] (plan handle `DSK-07-18`) spike, not this slice. |

Conclusion: four "yes" answers place both queues' commands, the promote
orchestration, the registration judgement and the member-source broker in the
gateway (L-01), and the reconciliation sweeps in the existing Worker (ADR-0106).
Rendering, paging, reason dialogs and the promote **control** belong in the
desktop. No new Azure resource; no Azure write.

## Implications

- **The promote control is a client of a contract, not a feature.** Everything
  behind `registration` — normalisation
  (`ImageIntakeLifecycle.NormalizeRegistrationInput`, `ImageIntakeLifecycle.cs:174`),
  validity (`TriageLifecycleRules.ValidateCreate`), Triage creation
  (`ICreateTriageFromIntake`, `TriageContracts.cs:138`), Triage reuse, and the
  origin-receipt lookup — is [[GWY-013]]'s. This slice sends the typed
  registration, renders the outcome and renders the refusal. A desktop-side format
  check would be a second validator and is a stop condition.
- **`registration` and `targetId` are mutually exclusive on the Triage branch.**
  With `registration` present the endpoint derives `targetId` from the Triage it
  opens; an ordinary resolve sends no `registration` and is unchanged. A
  `registration` with any other `targetKind` is a validation failure. All four
  cases need a contract test.
- **The exclusion rule needs no desktop code.** Because it is
  `item.State == Open` in the store, the desktop simply asks the endpoint for the
  queue; the assertion at step 10 exists to prove the endpoint did not
  re-implement it differently.
- **A queue row may have no origin receipt.** The left join at
  `EfUnidentifiedStore.cs:257-262` allows a submission-group origin, so the DTO
  and the view model must tolerate a missing file name, subject and sender.
- **One operation-key bound per area.** 200 here
  (`UnidentifiedContracts.cs:398`), 100 in administration. A shared client-side
  constant would be wrong in one of the two places.
- **The vehicle-images list is not paged today.** The endpoint map gives
  `GET /api/v1/image-intake?page`, so paging is added at the gateway; the desktop
  must not assume the whole set arrives in one response the way the Razor page
  does.
- **No second streaming implementation.** Member source access reuses
  [[FEAT-009]] (plan handle `DSK-05-09`)'s service by name.

## Open questions

None that block. Everything that could look like one has a named owner and is a
scope boundary rather than a question:

- The resolve contract, the promote orchestration and
  `ITriageQueries.GetByOriginReceiptAsync` are all [[GWY-013]]'s
  (`src/Pegasus.Core/Triage/**` and `src/Pegasus.Core/Intake/Unidentified/**` are
  out of bounds here). If the contract is not on the generated client, the ticket
  stops and raises it there — a stop condition, not an unanswered question.
- Whether ONNX VRM preprocessing should move to the desktop is the [[FEAT-044]]
  (plan handle `DSK-07-18`) spike.
- Trivial default taken rather than asked: the desktop requests the paged
  endpoint and renders whatever count it returns, rather than computing a count of
  its own — the web's own consistency comes from one query
  (`Triage/Index.cshtml.cs:263-274`) and a second client-side count could only
  disagree with it.
