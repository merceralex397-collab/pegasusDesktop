# Research — FEAT-007: S7 Parties and reference data (organizations, principals)

## Question

What do the five Razor administration page models for organizations and principals
actually enforce, what does upstream PLAT-028's 2026-08-21 consolidation decision
change about the shape of the native replacement, and is the read-only provider
reference surface a parity conversion or new work?

Read at `HEAD` `bbd1c549` (`git rev-parse --short HEAD`).

## Current behaviour

Five page models under `src/Pegasus.Web/Pages/Administration/`, every one of them
`[Authorize(Policy = StaffRoleNames.Administrator)]` and every one deriving from
`AdministrationPageModel` (`src/Pegasus.Web/Pages/Administration/AdministrationPageModel.cs`,
**7 lines** — it contributes exactly one member, `IsOperationKeyValid`, which
requires the operation key to be a non-empty `Guid` in `"N"` format).

| Page model | Lines | Handlers | Core use cases called |
| --- | --- | --- | --- |
| `Organizations/Index.cshtml.cs` | 126 | `OnGetAsync:36`, `OnPostCreateAsync:47` | `IListOrganizations`, `ICreateOrganization` |
| `Organizations/Edit.cshtml.cs` | 146 | `OnGetAsync:34`, `OnPostUpdateAsync:45` | `IGetOrganization`, `IUpdateOrganizationRoles` |
| `Principals/Index.cshtml.cs` | 31 | `OnGetAsync:19` | `IListOrganizations` — **and nothing else** |
| `Principals/Create.cshtml.cs` | 137 | `OnGetAsync:33`, `OnPostCreateAsync:44` | `IListOrganizations`, `IGetOrganization`, `ICreatePrincipal` |
| `Principals/Replace.cshtml.cs` | 199 | `OnGetAsync:38`, `OnPostReplaceAsync:57` | `IGetOrganization`, `IListOrganizations`, `IReplacePrincipal` |

Line counts confirmed with `wc -l`; they match the ticket body and the plan row
exactly.

Core owner: `src/Pegasus.Core/Cases/OrganizationAdministration.cs` (**596 lines**,
not the "operation key ≤ 100" one-liner the plan row implies) plus the four
use-case interfaces in `src/Pegasus.Core/Cases/CaseContracts.cs:345-366`.
Reference data: `src/Pegasus.Core/ReferenceData/` — three files, 522 lines.

**Parity rows.** `PAR-40` (`docs/desktop/01-inventory-and-parity/parity-matrix.md:85`)
covers the organizations pair; `PAR-41` (`:86`) covers the three principal pages.
Both are status `not inventoried` with test evidence `to locate`. The matrix holds
**46** rows (`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md`
→ `46`), all keyed to page models under `src/Pegasus.Web/Pages/**`.

**Provider reference data has no parity row and no current entry point.** PAR-40
and PAR-41 are both titled "13.6 Parties and reference data", but neither names a
reference-data page, and none exists — `ls src/Pegasus.Web/Pages/Administration/`
returns `Access`, `Accounts`, `Automation`, `Configuration.cshtml(.cs)`,
`Index.cshtml(.cs)`, `MailCategories.cshtml(.cs)`, `Mailboxes.cshtml(.cs)`,
`Organizations`, `Principals`, `Roles`, and nothing for reference data. The
read-only provider surface this ticket delivers is therefore **new**, not
converted. That is stated plainly here so no one later writes a parity claim
against a row that does not exist.

## Findings

### Facts

Each verified by reading the repository at `bbd1c549`.

- **`Principals/Index` is already an organization list.** All 31 lines of
  `src/Pegasus.Web/Pages/Administration/Principals/Index.cshtml.cs` do one thing:
  call `IListOrganizations` with page size 25 and render. It injects no principal
  service at all.
  - This is direct code evidence for upstream PLAT-028's consolidation decision.
    The "Principals destination" is not a second information space that the
    decision merges away — it is *already* the organization list wearing a
    different route. The decision removes a route, not a capability.
- **A principal has no update path and no delete path, anywhere in Core.**
  `grep -rn "interface ICreatePrincipal\|interface IReplacePrincipal\|interface IUpdatePrincipal\|interface IDeletePrincipal" src/Pegasus.Core/`
  returns exactly two hits: `ICreatePrincipal` (`src/Pegasus.Core/Cases/CaseContracts.cs:357`)
  and `IReplacePrincipal` (`:362`). `IOrganizationAdministrationStore`
  (`OrganizationAdministration.cs:104-118`) exposes four methods and no update or
  delete for a principal.
  - The acceptance criterion "a principal that has been allocated stays immutable
    through the desktop path" therefore needs **no new guard**. Immutability is
    structural: there is no use case to call. The desktop proves it by not having
    the command, and the contract test proves the route does not exist.
- **Replacement never reuses a reference, and the rule is in one place.**
  `OrganizationAdministrationPolicy.PlanPrincipalReplacement`
  (`OrganizationAdministration.cs:341-388`) returns a `PrincipalReplacementPlan`
  (`:266-268`) in which the predecessor becomes `SuccessorId = successorId`,
  `IsActive = false`, `Version = checked(Version + 1)`, and the successor is a
  **new** `Guid`, carrying the **same** `SequenceLineageId`, `PredecessorId =
  predecessor.Id`, `AllocatedCaseCount = 0` and the predecessor's `InspectionMode`.
  - `RequireUniquePrincipalCode(codeAlreadyExists)` (`:400-408`) is the "never
    reuses a reference" rule, and it throws `DuplicatePrincipalCode`.
  - Three preconditions guard it before that: `StaleVersion` when
    `predecessor.Version != expectedVersion` (`:354-358`),
    `PrincipalAlreadyReplaced` when `SuccessorId is not null` (`:359-363`), and
    `PrincipalInactive` when `!IsActive` (`:364-368`).
  - `RequireOrganizationCanOwnPrincipals` (`:390-398`) requires the successor
    organization to hold `OrganizationRole.WorkProvider`.
- **The four bounds are constants, and one differs from the intake bound.**
  `OrganizationAdministrationPolicy` (`OrganizationAdministration.cs:270-275`):
  `MaximumOrganizationNameLength = 300`, `MaximumPrincipalCodeLength = 20`,
  `MaximumOperationKeyLength = 100`, `MaximumReasonLength = 500`.
  - The ticket's trap is real and precise: 100 here, against the 200-character
    bound intake commands use. A shared client-side key generator sized for
    intake would pass validation locally and be refused by the gateway.
- **Two paging bounds, and they are not the same number.**
  `ListOrganizations.MaximumPageSize = 100` (`OrganizationAdministration.cs:126`)
  bounds an organization page; `GetOrganization.MaximumPrincipalCount = 100`
  (`:174`) bounds how many principals one organization detail returns, and
  `OrganizationDetails.HasMorePrincipals` (`:54-59`) reports the truncation.
  - The Razor pages use page size **25** for the two list screens
    (`Organizations/Index.cshtml.cs:24`, `:112`; `Principals/Index.cshtml.cs:27`)
    but `ListOrganizations.MaximumPageSize` — i.e. 100 — for the two dropdown
    sources (`Principals/Create.cshtml.cs:18`, `:92`;
    `Principals/Replace.cshtml.cs:20`, `:139`). Two different page sizes for two
    different jobs, deliberately.
- **The dropdown sources top up a selection that fell off the first page.**
  Both `Principals/Create.LoadAsync` (`:90-119`) and
  `Principals/Replace.LoadAsync` (`:122-171`) check whether the selected
  organization is present in the returned page and, if not, fetch it with
  `IGetOrganization` and prepend it. Without that, a selection made from a deep
  page silently vanishes on a validation round trip.
- **`Organization` carries no addresses and no contacts.**
  `src/Pegasus.Core/Cases/CaseContracts.cs:13-17` — `Organization(Guid Id, string
  Name, IReadOnlyList<OrganizationRole> Roles, long Version)`. `OrganizationRole`
  (`:7-11`) has exactly two values, `WorkProvider` and `InstructionIntermediary`.
  - The screen spec (`docs/desktop/06-ui-design/screen-specs.md:333-335`) says
    "Organizations list/edit (**name, addresses, contacts**)". No address or
    contact field exists on the record, on `OrganizationDetails`, or on any
    request. See `A-05-07-3`.
- **`ManageOrganizationsAndPrincipals` denies the Automation Actor by
  construction.** `src/Pegasus.Core/Identity/StaffAuthorization.cs:45-53` groups
  it with the other management rights: `actor.Kind == ActorKind.Staff &&
  actor.IsInRole(StaffRole.Administrator)`. `PerformCasework` (`:39-41`) is the
  only right the Automation Actor holds, and the matrix falls closed (`:57`).
  - `GetOrganization.ExecuteAsync` calls `StaffAuthorization.Require`
    (`OrganizationAdministration.cs:183-185`) *before* touching the queries, so
    even the read is administrator-only. The reference catalogue is not.
- **`IProviderReferenceCatalog` cannot list anything.**
  `src/Pegasus.Core/ReferenceData/ReferenceDataContracts.cs:37-42` declares exactly
  one method: `FindCandidatesByDomainSuffixAsync(packageVersion, domainSuffix,
  cancellationToken)` returning `ProviderDomainCandidates(Status, ProviderCodes)`
  (`ReferenceDataModels.cs:36-38`). `grep -rn "IProviderReferenceCatalog" src/`
  finds three hits and none of them is a caller in `Pegasus.Web`: the interface,
  the DI line (`src/Pegasus.Infrastructure/DependencyInjection.cs:151`) and the
  EF adapter (`src/Pegasus.Infrastructure/Persistence/EfProviderReferenceCatalog.cs:8`).
  - A browsable read is a **new** query, not a reuse. See `A-05-07-4`.
- **The reference read sits at a different right from the administration
  commands.** `docs/desktop/03-gateway-api-and-data/endpoint-map.md:132` puts
  `GET /reference/providers` (with `/principals`, `/engineers`, `/mailboxes`) at
  **`PerformCasework`**, described as "dropdown sources across Create/Details/Triage
  pages", ETag with a short cache. The administration rows `:129` and `:130` are
  `ManageOrganizationsAndPrincipals`.
  - So the reference endpoint is not an administration endpoint that happens to be
    read-only; it is a casework lookup that this screen also renders. Gating it on
    the administrator right would break the Create/Details/Triage callers it
    exists for.
- **Error translation is already exhaustive, and asymmetric across the two
  commands.** `OrganizationAdministrationError` has eleven values
  (`OrganizationAdministration.cs:5-18`). `Organizations/Index` maps three plus a
  default (`:113-125`); `Organizations/Edit` maps five plus a default (`:130-145`);
  `Principals/Create` maps four plus a default (`:121-136`); `Principals/Replace`
  maps eight plus a default (`:176-198`).
  - `StaleVersion` and `OperationConflict` are the two that must become **409**,
    and they appear on Edit and Replace only — Create has no version to be stale.
- **Reason is required on the two versioned commands and absent on the two
  creates.** `Organizations/Edit.Reason` and `Principals/Replace.Reason` are both
  `[Required, StringLength(MaximumReasonLength, MinimumLength = 1)]`
  (`Edit.cshtml.cs:27-29`, `Replace.cshtml.cs:31-33`). Neither create carries one.
- **Two web oracles already exist.**
  `tests/Pegasus.IntegrationTests/OrganizationAdministrationWebTests.cs` (190 lines)
  holds two facts —
  `AdministratorRoutesAreDiscoverableAndPostThroughCoreEfCallers` (`:15`) and
  `DirectOrganizationAndPrincipalRoutesDenyNonAdministratorSession` (`:141`) — and
  `OrganizationAdministrationPersistenceTests.cs` (424 lines) is the persistence
  oracle. Both must stay green; neither is modified.

### Assumptions

Labelled per `docs/engineering.md` § plan sizing. Each names what would confirm it
and what breaks if it is wrong.

- **`A-05-07-1` — [[GWY-015]] (plan handle `DSK-03-15`) lands the five routes in
  the shapes the ticket body names.** Namely `GET/POST /api/v1/admin/organizations`,
  `GET/PUT /api/v1/admin/organizations/{id}`, `GET/POST /api/v1/admin/principals`,
  `POST /api/v1/admin/principals/{id}/replace`.
  - *Confirmed by*: step 3 — reading the delivered route group and the committed
    `openapi/pegasus-v1.json` snapshot before any desktop binding is written.
  - *Breaks if wrong*: the desktop DTOs and every contract test are written against
    a shape that does not exist. Mitigation in the plan's Risks section, which also
    records the phase mismatch (`A-05-07-2`).
- **`A-05-07-2` — [[GWY-015]] is available when this ticket runs, despite sitting a
  phase later.** `FEAT-007` is `HZN-005` / `phase-4`; `GWY-015` is `HZN-009` /
  `phase-8` (both read from `list_items`). The plan-05 dependency row
  (`docs/desktop/05-implementation-and-migration/README.md:216`) says
  "DSK-05-03; DSK-03 administration group", so the dependency is recorded but the
  horizons disagree.
  - *Confirmed by*: `get_item GWY-015` showing it done, or an agreed partial
    delivery of the organizations and principals routes ahead of the rest of the
    administration group.
  - *Breaks if wrong*: this ticket blocks. It is a scheduling fact, not a design
    question, and it belongs to whoever sequences the board — recorded in the
    plan's Risks section naming [[GWY-015]], not opened as a question here.
- **`A-05-07-3` — the "addresses, contacts" phrase in screen spec § 13.6 is
  aspirational and not in scope.** No such field exists on `Organization`,
  `OrganizationDetails`, `CreateOrganizationRequest` or
  `UpdateOrganizationRolesRequest`.
  - *Confirmed by*: the documentation edit this ticket already owes to
    `screen-specs.md` § 13.6 — correcting the phrase at the same time as recording
    the consolidation.
  - *Breaks if wrong*: the desktop would need new Core fields, a migration and a
    parity story that no row covers — which is new capability under proposal
    §13.11, i.e. a separate ticket, not this one.
- **`A-05-07-4` — a browsable provider reference read is acceptable as a new,
  additive gateway query rather than a Core change.** The catalogue's only Core
  port answers one question, by domain suffix.
  - *Confirmed by*: step 3's reading of what [[GWY-015]] and the endpoint-map row
    `:132` actually deliver for `GET /reference/providers`.
  - *Breaks if wrong*: if the browsable read needs a new Core port, that port is a
    Core change and needs a characterization test first
    (`docs/desktop/05-implementation-and-migration/README.md` § 7, "Page-model
    logic that is really business logic"). Budgeted in the plan as a conditional
    step, not assumed away.
- **`A-05-07-5` — the data-table and form patterns from [[DUI-007]] (plan handle
  `DSK-06-07`) and [[DUI-008]] (plan handle `DSK-06-08`) support a master–detail
  screen with an embedded child collection.** Organization detail must host the
  organization form *and* the principal rows in one destination.
  - *Confirmed by*: reading the delivered controls in step 5.
  - *Breaks if wrong*: the consolidation still stands as a navigation decision; the
    detail screen composes two existing controls instead of one, which changes
    layout work, not scope.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md` § 3,
answered for the parties-administration responsibility.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **Yes** | Organizations and principals are shared reference state that every case allocation depends on; `UpdateOrganizationRoles` and `ReplacePrincipal` both carry `ExpectedVersion` and refuse a stale write with `OrganizationAdministrationError.StaleVersion` (`OrganizationAdministration.cs:288-292`, `:354-358`). Lands in the gateway (`Pegasus.Web`, L-01). |
| Unattended execution — must it run with every desktop closed? | **No** | All four commands are administrator actions taken at a screen. No sweep, timer or queue in `src/Pegasus.Worker` touches organization or principal administration — the reuse map's Worker section lists no such function. Nothing moves. |
| Protected credentials — long-lived secret that must not sit on workstations? | **No, for this ticket's scope.** | Nothing in the four commands or the reference read carries a secret. The *deferred* half — principal-scoped provider credential generate/reset/revoke/pause/resume with a secret shown once — genuinely is a credential responsibility, and it is out of scope here, blocked on upstream `TICK-058` and `TICK-061` under `HZN-002` (ticket Guardrails). When it activates it lands **in the gateway**, beside the other provider credentials ADR-0107 already keeps there; it does not land in the package. |
| Public callback — must an external service call a stable public endpoint? | **No** | No external party creates or replaces a principal. The only anonymous external surface in the repository is the request-link upload page (`Pages/Uploads/Request.cshtml.cs`, kept on web per `reuse-map.md:84`), which is unrelated. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes** | `StaffAuthorization.Require(actor, ManageOrganizationsAndPrincipals)` runs inside Core, on the read as well as the writes (`OrganizationAdministration.cs:183-185`); the Automation Actor is denied by the matrix (`StaffAuthorization.cs:45-53`); operation-key idempotency is bounded at 100 characters; and `AGENTS.md` § Product invariants — "principal and reference immutable after allocation… neither reference reused" — is enforced by `RequireUniquePrincipalCode`, not by a screen. None of it can be trusted to a client: "the desktop hides or disables commands for usability only" (`vertical-slices.md` § Common to every slice). Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **n/a** | No measurement exists either way and none is needed: questions 1 and 5 already place every command. The desktop renders forms and confirms; it computes nothing. |

**Placement:** the gateway executes, authorizes and audits all four commands and
serves both reads; the desktop renders, validates locally against the same four
`OrganizationAdministrationPolicy` bounds, and confirms. Two "yes" answers, both
naming the gateway. **No Azure resource is involved and no Azure write occurs** —
enabling `Features:DesktopGateway` in production is [[PLAT-024]] (plan handle
`DSK-11-06`), a different ticket.

## Implications

- **The consolidation is a deletion, not a merge, and the code says so.**
  `Principals/Index` already calls only `IListOrganizations`. Converting the five
  Razor destinations one-for-one would produce two native destinations where the
  web has one-and-a-half, which is the fragmentation upstream PLAT-028's
  2026-08-21 operator decision removed. The native shape is **one** entry point
  (Organizations) whose detail owns the organization form, the principal rows,
  and the two principal commands.
- **Three of the five page models collapse into one screen; two become one.**
  `Principals/Index` (31) disappears entirely — it has no capability of its own.
  `Principals/Create` (137) and `Principals/Replace` (199) become commands on
  Organization detail. `Organizations/Index` (126) and `Organizations/Edit` (146)
  become the list and the detail. That is the whole conversion, and the ripple into
  `PAR-40`/`PAR-41` is that PAR-41's "Principals admin" native-screen cell must
  name Organization detail, not a Principals screen.
- **Immutability needs a *negative* test, not a guard.** Because Core exposes no
  principal update, the honest evidence is (a) a view-model fact that the principal
  row's command collection contains no edit command, and (b) a contract fact that no
  `PUT /api/v1/admin/principals/{id}` route exists. Writing a client-side
  "immutable" check would invent a second implementation of a rule Core enforces by
  omission — a stop condition under `docs/engineering.md` § One Core owner.
- **"Never reuses a reference" is asserted server-side or not at all.** The ticket
  body step 6 already says this ("assert this in the contract test rather than in
  the UI"), and the code agrees: the uniqueness check needs `codeAlreadyExists`,
  which only the store can answer. The contract test must replace a principal and
  then attempt the predecessor's code again, expecting `DuplicatePrincipalCode` →
  409/400 per the delivered mapping.
- **The successor form has four fields and one of them is pre-filled from the
  predecessor.** `Replace.LoadAsync:163` sets `SuccessorOrganizationId =
  Organization.Id` on GET, so the default is "replace within the same
  organization". The desktop should keep that default; losing it makes the common
  case a search.
- **Two page sizes must survive the conversion.** 25 for the list, 100 for the
  organization picker inside the principal commands. Collapsing them to one number
  either makes the list heavy or truncates the picker — and the picker's top-up
  logic exists precisely because truncation there is a correctness bug, not a
  cosmetic one.
- **The reference-data section is the only part of this ticket with no oracle.**
  There is no web page to compare against and no PAR row to update. Its acceptance
  is therefore purely "renders read-only, no edit affordance", and the plan must
  not promise a parity comparison it cannot run.
- **`OperationKey` generation moves from the form to the view model.** The web
  regenerates the key on every failed post (`Index.cshtml.cs:87`,
  `Edit.cshtml.cs:88`, `Create.cshtml.cs:87`, `Replace.cshtml.cs:114`) so a retry
  after a *validation* failure is a new operation, while a retry of the *same*
  submission replays. The desktop must reproduce that distinction deliberately: a
  fresh key after the operator edits the form, the same key when resending an
  unchanged request.

## Open questions

None that block the plan. `A-05-07-1`, `A-05-07-4` and `A-05-07-5` are settled by
step 3's and step 5's reading of the delivered gateway and control set, and each
has a named consequence in the plan's *Risks / open questions* section.
`A-05-07-2` is a sequencing fact owned by whoever schedules [[GWY-015]] — a scope
boundary naming that ticket, not a question. `A-05-07-3` is resolved by a
documentation edit this ticket already owes.

The deferred provider-credential controls are **not** an open question either: the
ticket's Guardrails record them as deliberately deferred behind upstream `TICK-058`
and `TICK-061` under `HZN-002`, with their destination already decided by upstream
PLAT-028's second operator decision. A decision a named sibling owns is a scope
boundary, not a question.

**No `open-questions` document is created:** the ticket body does not instruct one,
and nothing here is genuinely unsettled.
