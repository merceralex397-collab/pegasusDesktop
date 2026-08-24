# Files — FEAT-007

Surface area of `DSK-05-07 · S7 Parties and reference data (organizations,
principals)`. Paths that do not exist at `HEAD` `bbd1c549` are marked with the
ticket that creates them; every other path was confirmed with `ls`, `wc -l` or
`grep`.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Contracts/` *(created by [[FND-029]] (plan handle `DSK-02-04`); conventions by [[GWY-001]] (plan handle `DSK-03-01`))* | Five request DTOs and four response DTOs. `CreateOrganization` (name ≤ 300, roles), `UpdateOrganizationRoles` (**`expectedVersion` + `reason` ≤ 500**), `CreatePrincipal` (organization id, code ≤ 20, inspection mode), `ReplacePrincipal` (**`expectedVersion` + successor organization + successor code + `reason`**), plus the paged organization list and the organization detail carrying its principal rows and `hasMorePrincipals`. Every mutation carries `operationKey` **≤ 100**. The organization and principal `version` values must both stay on the wire — they are different versions and a stale write of either must return 409 rather than overwrite. |
| `src/Pegasus.Web/` — the `/api/v1` `admin/organizations` and `admin/principals` routes only *(group by [[GWY-002]] (plan handle `DSK-03-02`); routes by [[GWY-015]] (plan handle `DSK-03-15`))* | Six routes calling the same four Core use cases the Razor handlers call, plus the `GET /api/v1/reference/providers` read. `StaleVersion` and `OperationConflict` translate to **409**; `StaffAuthorizationException` to **403 `not-authorized`**. Consuming, not authoring — see *Ripple effects* if a shape is missing. |
| `src/Pegasus.Desktop/` *(created by [[FND-030]] (plan handle `DSK-02-05`))* | `OrganizationsViewModel` — the **single** Administration entry point for parties, on the [[DUI-007]] (plan handle `DSK-06-07`) data-table pattern — and `OrganizationDetailViewModel`, the consolidated owner of the organization form, that organization's principal rows, and the principal create and replace commands, on the [[DUI-008]] (plan handle `DSK-06-08`) form pattern. Plus the read-only reference-data view. **No Principals view model, no Principals rail or card entry, no second destination.** |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]] (plan handle `DSK-02-06`))* | The typed client calls for the six routes and the reference read. The place the two page sizes live as named constants (25 for the list, 100 for the organization picker) so neither is a literal in XAML. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]] (plan handle `DSK-08-01`))* | Per endpoint: 200 administrator, **403 `not-authorized` for a `PerformCasework`-only staff session and for the Automation Actor**, 401 without a token, 409 stale version (Edit and Replace only — the creates have no version), replay of the same `operationKey` returning the same result, and the reference read succeeding at `PerformCasework`. Plus the two negative facts: **no `PUT /api/v1/admin/principals/{id}` route exists**, and a replace cannot reuse the predecessor's code. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[FND-038]] (plan handle `DSK-02-13`))* | List paging, create validation against the four `OrganizationAdministrationPolicy` bounds, edit dirty state, replace refusing an empty reason, the successor organization defaulting to the predecessor's, the operation-key regeneration rule, and the structural fact that **the principal row exposes no edit command** and **no navigation target named Principals exists**. |
| `tests/Pegasus.Desktop.UITests/` *(created by [[TEST-006]] (plan handle `DSK-08-06`))* | `ui-tests.ps1 -Script parties`: create-organization and replace-principal by keyboard, **both reached from Organization detail**, plus the `axe-windows` scan over the list and the detail. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | `PAR-40` (`:85`, organizations) and `PAR-41` (`:86`, principals). PAR-41's native-screen cell must name **Organization detail**, not a Principals screen. |
| `docs/desktop/06-ui-design/screen-specs.md:332-341` | Record the consolidation, replacing the "Organizations list/edit … and Principals create/replace" pairing at `:333-335`; reconcile the `Admin.Principals.Create` / `Admin.Principals.Replace` AutomationIds at `:338-339` with their new host; and correct "addresses, contacts" (see the Out-of-scope note). The carry-over line at `:340-341` names upstream PLAT-028 but not the decision it carries. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(skeleton by [[FND-008]] (plan handle `DSK-00-08`))* | New parties and reference-data section. |
| `docs/capabilities.md` | `DSK` rows for organizations and principals. |

## Context files

Read these before writing code. Each is here for one specific trap.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Web/Pages/Administration/Principals/Index.cshtml.cs` (31 lines) | **The single most important file for this ticket.** All 31 lines call `IListOrganizations` and nothing else — it injects no principal service. The "Principals destination" the consolidation removes is already just the organization list on a second route. Read this before deciding the consolidation is a big change; it is a deletion. |
| `src/Pegasus.Core/Cases/OrganizationAdministration.cs:270-275` | The four bounds as constants: organization name ≤ 300, principal code ≤ 20, **operation key ≤ 100**, reason ≤ 500. The 100 is the trap — intake commands use 200, and a shared key generator sized for intake passes locally and is refused by the gateway. |
| `src/Pegasus.Core/Cases/OrganizationAdministration.cs:341-388` (`PlanPrincipalReplacement`) | The whole replacement rule in one method: predecessor gets `SuccessorId`, `IsActive = false`, version+1; successor is a **new** id with the **same** `SequenceLineageId`, `PredecessorId` set, `AllocatedCaseCount = 0`, inheriting `InspectionMode`. Three guards fire before it — `StaleVersion` (`:354`), `PrincipalAlreadyReplaced` (`:359`), `PrincipalInactive` (`:364`) — and `RequireUniquePrincipalCode` (`:400-408`) is the "never reuses a reference" rule. None of this is expressible client-side; the desktop collects fields and confirms. |
| `src/Pegasus.Core/Cases/CaseContracts.cs:345-366` | The four use-case interfaces — `ICreateOrganization`, `IUpdateOrganizationRoles`, `ICreatePrincipal`, `IReplacePrincipal`. **There is no `IUpdatePrincipal` and no delete.** A principal's immutability is structural, not guarded: there is nothing to call. Prove it with a missing route and a missing command, never with a client-side check. |
| `src/Pegasus.Core/Cases/CaseContracts.cs:7-28` | `OrganizationRole` has exactly two values (`WorkProvider`, `InstructionIntermediary`); `Organization` is `Id, Name, Roles, Version` — **no addresses, no contacts**; `Principal` carries the lineage triple (`SequenceLineageId`, `PredecessorId`, `SuccessorId`) plus `IsActive`, `Version`, `AllocatedCaseCount` and `InspectionMode`. |
| `src/Pegasus.Core/Cases/OrganizationAdministration.cs:126`, `:174` | The two paging bounds that are the same number for different reasons: `ListOrganizations.MaximumPageSize = 100` bounds an organization page, `GetOrganization.MaximumPrincipalCount = 100` bounds the principals returned inside one organization, with `HasMorePrincipals` reporting truncation. The list screens use **25**; the pickers use **100**. |
| `src/Pegasus.Core/Cases/OrganizationAdministration.cs:183-185` | `GetOrganization` calls `StaffAuthorization.Require(actor, ManageOrganizationsAndPrincipals)` before touching the queries — **the read is administrator-only too**, not just the writes. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs:45-53`, `:39-41`, `:57` | `ManageOrganizationsAndPrincipals` needs `ActorKind.Staff` **and** `IsInRole(StaffRole.Administrator)`; the Automation Actor holds `PerformCasework` only; the matrix falls closed on anything unknown. This is why the 403 contract fact must cover the Automation Actor as well as a casework-only staff session. |
| `src/Pegasus.Web/Pages/Administration/Principals/Create.cshtml.cs:90-119` and `Replace.cshtml.cs:122-171` | The dropdown top-up: if the selected organization is not in the returned page, fetch it with `IGetOrganization` and prepend it. Without it a selection made from a deep page silently vanishes on a validation round trip — a correctness bug, not a cosmetic one. `Replace` additionally pre-fills `SuccessorOrganizationId = Organization.Id` on GET (`:163`), so the default is "replace within the same organization". |
| `src/Pegasus.Web/Pages/Administration/Principals/Replace.cshtml.cs:176-198` | The eight-branch error map — the richest of the four. Its sentences are the settled operator wording for each refusal, including "The predecessor is already disabled and cannot be replaced again." and "That normalized successor code already exists." |
| `src/Pegasus.Web/Pages/Administration/Organizations/Edit.cshtml.cs:130-145` | The five-branch map, including `ActivePrincipalsRequireWorkProvider` — "Work Provider cannot be removed while the organization has an active principal." A role checkbox pair that does not surface this refusal looks broken. |
| `src/Pegasus.Web/Pages/Administration/AdministrationPageModel.cs` (7 lines) | Contributes exactly one member, `IsOperationKeyValid` — the operation key must be a non-empty `Guid` in `"N"` format. The ticket forbids modifying this file; read it only to learn the key shape. |
| `src/Pegasus.Web/Pages/Administration/Organizations/Index.cshtml.cs:87`, `Edit.cshtml.cs:88`, `Principals/Create.cshtml.cs:87`, `Replace.cshtml.cs:114` | All four regenerate the operation key after a **validation** failure, so an edited-and-resubmitted form is a new operation while an unchanged resend replays. Reproduce that distinction deliberately in the view model. |
| `src/Pegasus.Core/ReferenceData/ReferenceDataContracts.cs:37-42` | `IProviderReferenceCatalog` has **one** method, `FindCandidatesByDomainSuffixAsync`. It cannot list. A browsable read is new work — see the plan's conditional step, and do not assume a Core list query exists. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:129-130`, `:132` | The two administration rows at `ManageOrganizationsAndPrincipals` with "yes (key)" idempotency, and — separately — `GET /reference/providers` at **`PerformCasework`**, described as a dropdown source with a short ETag cache. Gating the reference read on the administrator right would break the Create/Details/Triage callers it exists for. |
| `docs/desktop/06-ui-design/screen-specs.md:332-341` | The current § 13.6 block: the destination pairing this ticket replaces, the four AutomationIds, and the carry-over line. Also the source of the "addresses, contacts" phrase that no Core field supports. |
| `docs/design/README.md:412-420` | The banned operator words. `lease` and `bounded` are not at issue here, but `artifact` and `aggregate` are easy to reach for in an administration screen; and the ban is a **review rule, not a CI check** — nothing catches it automatically. |
| `docs/design/README.md:424-440` | "A field is a label and a control, nothing more" and "No how-it-works copy." An administration form is where hint text creeps in; the required marker is visual, never prose. The consequence sentence for principal replacement must come from the closed necessary-copy list, not be written fresh. |
| `docs/design/README.md:459` | "Administration has no generic rules editor, credential/cloud/release operation, bulk predecessor import, or bulk Case-edit tool. No surface permits permanent deletion." A bulk principal import is explicitly forbidden. |
| `AGENTS.md` § Product invariants | "Principal and reference immutable after allocation… neither reference reused." The invariant this whole ticket exists to preserve, and the reason the replace test is a *negative* one. |
| `docs/desktop/05-implementation-and-migration/reuse-map.md:14-17`, `:44-50` | Core is reused as-is; the boundary note permits `Pegasus.Desktop` to reference `Pegasus.Core` for deterministic local validation — which is how the four bounds run client-side — but never `Pegasus.Infrastructure`, EF Core or any provider SDK. `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` enforces it. |
| `tests/Pegasus.IntegrationTests/OrganizationAdministrationWebTests.cs` (190 lines) | The two route-level oracles: `AdministratorRoutesAreDiscoverableAndPostThroughCoreEfCallers` (`:15`) and `DirectOrganizationAndPrincipalRoutesDenyNonAdministratorSession` (`:141`). The second is the shape the `/api/v1` 403 facts should mirror. Must stay green; not modified. |
| `tests/Pegasus.IntegrationTests/OrganizationAdministrationPersistenceTests.cs` (424 lines) | The persistence oracle for lineage, uniqueness and version behaviour. Must stay green; not modified. |
| `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26` | The shared `WebApplicationFactory<Program>`. `Features:DesktopGateway` must be enabled explicitly or every `/api/v1` route returns 404 (plan 05 § 7). |
| `docs/desktop/08-testing/test-uat-stack.md:22` | The Test/UAT gateway configuration, including `Features:DesktopGateway=true`. |

## Ripple effects

- **Generated client and OpenAPI snapshot.** Six new administration shapes plus
  the reference read. [[GWY-005]] (plan handle `DSK-03-05`) commits the Kiota
  output with a CI no-op check; [[TEST-001]] (plan handle `DSK-08-01`) fails the
  snapshot test on an undeclared change to `openapi/pegasus-v1.json`.
- **[[GWY-015]] (plan handle `DSK-03-15`) is the upstream dependency and sits a
  phase later.** It is `HZN-009` / `phase-8`; this ticket is `HZN-005` /
  `phase-4`. If a route shape is missing or differs, raise it there — do not add
  an endpoint from this ticket, whose scope boundary permits only the
  organizations and principals routes it consumes.
- **[[TEST-002]] (plan handle `DSK-08-02`) seven-case matrix needs two recorded
  exemptions.** The two *create* commands have no version, so the stale-version
  case is inapplicable to them; and the reference read is a `PerformCasework`
  endpoint, so its 403 case is a different actor than the administration rows'.
  Coordinate rather than silently skipping coverage — that template "fails when a
  command lacks coverage".
- **[[FND-046]] (plan handle `DSK-04-10`) supplies the role-aware shell.** The
  Administration entry must be absent for a non-administrator; this ticket
  consumes that mechanism and adds no second visibility rule.
- **[[FEAT-019]] (plan handle `DSK-05-19`) adds the rest of Administration onto
  the same shell.** The card/entry conventions this ticket establishes for
  Organizations are the ones FEAT-019 extends; a divergent pattern here costs
  twice. `FEAT-007` blocks `FEAT-019`, `FEAT-022` and `FEAT-025`.
- **[[FEAT-004]] (plan handle `DSK-05-04`) consumes the principal picker.** Case
  create allocates against a principal, so the same organization/principal read
  shapes surface there; keep the DTOs shared rather than forking a second
  projection.
- **Existing web tests must stay green.** Nothing here touches the five Razor page
  models or `AdministrationPageModel.cs`, so `OrganizationAdministrationWebTests`
  and `OrganizationAdministrationPersistenceTests` must pass unchanged.
- **Parity matrix rows change meaning, not just status.** `PAR-41`'s native-screen
  cell moves from "Principals admin" to Organization detail. That is a
  consolidation record, and a reviewer reading only the status column would miss
  it.
- **Documentation link check.** `scripts/Test-DocumentationLinks.ps1` runs over
  repository documentation, so a broken relative link in the new FRD-13 section or
  the edited screen-spec block fails CI.

## Out of scope

Recorded so the reviewer sees each was a decision, not an oversight.

- **The five Razor page models and `AdministrationPageModel.cs` are not
  modified.** The ticket's scope boundary says so explicitly. They stay live until
  `PAR-40` and `PAR-41` reach cut-over; the cut is [[FEAT-026]] (plan handle
  `DSK-05-26`).
- **No separate Principals destination of any kind** — no view model, no rail
  entry, no Administration card, no route. Building one is a **stop condition**: it
  is the fragmentation upstream PLAT-028's 2026-08-21 operator decision removed.
- **Principal-scoped provider credential controls** (generate, reset, revoke,
  pause, resume; secret shown once, hash retained). Deferred by the ticket's
  Guardrails, blocked on upstream `TICK-058` (the API contract) and upstream
  `TICK-061` (the credential lifecycle) under `HZN-002`. **Do not build a partial
  credential surface.** When they activate, upstream PLAT-028's second recorded
  decision places them on the Principal *inside* the consolidated Organization
  detail.
- **Organization addresses and contact records.** The screen spec says "name,
  addresses, contacts" (`screen-specs.md:333`) but `Organization`
  (`src/Pegasus.Core/Cases/CaseContracts.cs:13-17`) carries none, and no request
  or projection does either. Adding them is new capability under proposal §13.11 —
  a separate ticket. This ticket corrects the spec phrase instead.
- **Any principal edit or delete affordance.** Core exposes neither
  (`CaseContracts.cs:345-366`), `AGENTS.md` § Product invariants forbids reference
  reuse, and `docs/design/README.md:459` forbids any permanently deleting surface.
- **Bulk predecessor import.** Explicitly forbidden by
  `docs/design/README.md:459`.
- **Writing the administration endpoints.** They are [[GWY-015]]'s (plan handle
  `DSK-03-15`). This ticket consumes them and raises gaps there.
- **upstream `TICK-034` (DATA-02)** stays backlog and must not be pulled in — the
  ticket's Guardrails and plan 05 § 7's §13.11 scope-creep rule.
- **Upstream PLAT-028 has no fork ticket.** It is absorbed here. Note the two
  collisions the ticket body records: the board's `PLAT-028` is upstream PLAT-032
  (the duplicate-route sweep), and the board's `PLAT-024` is `DSK-11-06`, not the
  upstream PLAT-024 the first operator decision archives. `HZN-001` group document
  `board-conventions.md` § "Upstream ids versus board ids" holds the join table.
- **No Azure write.** Enabling `Features:DesktopGateway` in production is
  [[PLAT-024]] (plan handle `DSK-11-06`).
