# Research — FND-029: the shared `Pegasus.Contracts` envelope project

## Question

What shape must the shared gateway/desktop DTO project take so that the paging, problem-details,
concurrency and operation-key envelopes are each declared exactly once, and what does the existing
repository already fix about those shapes that this project must mirror rather than invent?

## Current behaviour

**No parity-matrix row covers this ticket, and none should.** The matrix at
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted, not
copied: `grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` returns **46** —
and every row is "keyed by the Razor page model and handler group that implements it today"
(`parity-matrix.md:3-5`). A dependency-free DTO assembly has no page model and no observable
operator capability, so it is out of the matrix's scope by construction.

The closest existing repository mechanisms — what does this job today:

- **There is no DTO layer at all.** Razor page models bind Core records straight into markup:
  `src/Pegasus.Web/Pages/Cases/Index.cshtml.cs:65` exposes `public SearchCasesResult? Results`,
  `src/Pegasus.Web/Pages/Triage/Index.cshtml.cs:84` constructs one inline
  (`new([], 1, PageSize, false, false)`), and four Administration page models
  (`src/Pegasus.Web/Pages/Administration/Organizations/Index.cshtml.cs:15`,
  `.../Principals/Index.cshtml.cs:12`, `.../Principals/Create.cshtml.cs:16`,
  `.../Principals/Replace.cshtml.cs:18`) expose `OrganizationListPage` directly. Server-rendered
  Razor can do this because the Core record never leaves the process; a wire contract cannot,
  because `CaseMutationRequest` carries `ActionActor` (a server-only identity type).
- **The nearest thing to a problem catalogue** is `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs`
  (154 lines), whose `ExecuteAsync` at `:19-67` translates Core refusals
  (`StaffAuthorizationException`, `CaseEditLeaseExpiredException`, `CaseEditLeaseConflictException`,
  `CaseVersionConflictException`) into caller-safe messages and collapses everything unexpected to
  a generic failure so "no infrastructure detail crosses the boundary" (`:9-15`). It produces
  prose, not a stable machine-readable `type` URI.
- **The nearest thing to an operation-key rule** is `AutomationMcpErrors.RequireOperationKey`
  (`:76-89`): prefix `mcp:`, `Length is <= 4 or > 100`, no whitespace or control characters. The
  desktop's `desk:` prefix and 100-character cap are that same rule with a different prefix, not a
  new invention.
- **The single shared presentation vocabulary today** is
  `src/Pegasus.Web/Presentation/OperatorLabels.cs` (685 lines, 32,550 bytes). It stays where it is:
  [[FEAT-023]] (plan handle `DSK-05-23`) and [[GWY-016]] (plan handle `DSK-03-16`) own its
  relocation.

## Findings

### Facts

Verified by reading the repository at fork `main`, 2026-08-24. Each carries its source.

- **`src/Pegasus.Contracts` does not exist.** `ls src` returns exactly `Pegasus.Core`,
  `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`. `Pegasus.slnx` (14 lines) lists four
  `/src/` and three `/tests/` projects and nothing else.
- **`src/Pegasus.Core/Pegasus.Core.csproj` is the exact shape to copy** — 14 lines:
  `Microsoft.NET.Sdk`, `<TargetFramework>net10.0</TargetFramework>`,
  `<RuntimeIdentifiers>linux-x64;win-x64</RuntimeIdentifiers>`, `ImplicitUsings` and `Nullable`
  enabled, one `InternalsVisibleTo`, **zero `PackageReference` and zero `ProjectReference`**.
- **`System.Text.Json` needs no package reference at `net10.0`.** Proof from this repository, not
  from documentation: `Pegasus.Core` has zero package references yet six of its files import
  `System.Text.Json` — `src/Pegasus.Core/Custody/CustodyContracts.cs:3`,
  `src/Pegasus.Core/Eva/EvaBundleSchema.cs:5`,
  `src/Pegasus.Core/Intake/CaseMatching/AutomaticMailCaseAssociation.cs:3`,
  `src/Pegasus.Core/Intake/IntakeAllocation.cs:3`,
  `src/Pegasus.Core/ReferenceData/ReferenceDataModels.cs:2` (`System.Text.Json.Serialization`) and
  `src/Pegasus.Core/ReferenceData/ReferenceDataPolicy.cs:12`.
- **A dependency-free project's lock file is eight lines.** `src/Pegasus.Core/packages.lock.json`
  is 124 bytes: `{"version":1,"dependencies":{"net10.0":{},"net10.0/linux-x64":{},"net10.0/win-x64":{}}}`.
  `src/Pegasus.Contracts/packages.lock.json` will be byte-identical.
- **No Core paging port produces a total count.** Four paged read ports exist and all four are the
  fetch-one-extra cursor shape:
  - `SearchCasesResult` — `src/Pegasus.Core/Cases/CaseQueries.cs:69-74` —
    `(IReadOnlyList<CaseSearchItem> Items, int Page, int PageSize, bool HasPreviousPage, bool HasNextPage)`.
  - `OrganizationListPage` — `src/Pegasus.Core/Cases/OrganizationAdministration.cs:47-52` —
    `(IReadOnlyList<OrganizationListItem> Organizations, int PageNumber, int PageSize, bool HasPreviousPage, bool HasMoreOrganizations)`.
  - `ListAutomationActivityResult` — `src/Pegasus.Core/Identity/AutomationActivity.cs:44-50` —
    `(IReadOnlyList<AutomationActivityRecord> Records, string? CorrelationId, int Page, int PageSize, bool HasPreviousPage, bool HasMoreRecords)`.
  - `ListStaffAccountsResult` — `src/Pegasus.Core/Identity/StaffAccountAdministration.cs:16-21` —
    `(IReadOnlyList<StaffAccountSummary> Accounts, int PageNumber, int PageSize, bool HasPreviousPage, bool HasMoreAccounts)`.
  - **Refinement of the ticket body, not a contradiction.** The body describes "the same five-member
    cursor shape in the other three paged ports". Measured: the *pattern* is identical in all four —
    items, page, page size, has-previous, has-more — but the member **names** differ per port
    (`Items`/`Organizations`/`Records`/`Accounts`; `Page`/`PageNumber`;
    `HasNextPage`/`HasMoreOrganizations`/`HasMoreRecords`/`HasMoreAccounts`), and
    `ListAutomationActivityResult` carries a **sixth** member, `string? CorrelationId`. None of the
    four carries a count of any name, which is the load-bearing point the body makes and which this
    research confirms.
- **The implementation behind those flags issues no `COUNT`.**
  `src/Pegasus.Infrastructure/Persistence/EfCaseQueryStore.cs:115-133`:
  `.Take(query.PageSize + 1)`, then `var hasNextPage = page.Length > query.PageSize;` and a return of
  `(items, query.Page, query.PageSize, query.Page > 1, hasNextPage)`. No `CountAsync` appears in the
  method.
- **`CaseMutationRequest` is the concurrency shape to mirror** —
  `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188`:
  `public abstract record CaseMutationRequest(Guid CaseId, long ExpectedVersion, ActionActor Actor, string OperationKey, string Reason, string EditLeaseToken)`.
  It is `abstract` and `CaseId` is a `Guid`. `ActionActor` is the member the wire envelope must drop.
- **The operation-key rule already exists** — `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:76-89`:
  trimmed, prefixed `mcp:`, `Length is <= 4 or > 100`, no whitespace or control characters, and
  `src/Pegasus.Web/Mcp/AssessmentMcpTools.cs:219`, `:296`, `:376` and
  `src/Pegasus.Web/Mcp/CaseMcpTools.cs:265` are its four callers.
- **The problem-type catalogue is fixed at thirteen slugs** —
  `docs/desktop/03-gateway-api-and-data/README.md:167`: `validation`, `not-authorized`,
  `version-conflict`, `lease-conflict`, `lease-expired`, `operation-conflict`, `client-unsupported`,
  `password-change-required`, `account-disabled`, `provider-unavailable`, `not-found`,
  `rate-limited`, `maintenance`, all under `urn:pegasus:problem:`, with `correlationId` always
  present and no payload dumps.
- **Page size cap and the totals rule** — `docs/desktop/03-gateway-api-and-data/README.md:169`:
  offset paging with `pageSize ≤ 200`, newest-first default, and "totals returned only where the
  existing query port already counts" — which, per the four ports above, is nowhere.
- **The compatibility payload is fixed at five fields** —
  `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 item 5:
  `minimumVersion`, `currentVersion`, `channel`, `maintenanceMessage`, `validForSeconds`, returned by
  an anonymous `GET /api/v1/client-compatibility`.
- **Header names** — `docs/desktop/03-gateway-api-and-data/README.md:168`: `X-Correlation-Id`
  accepted or generated and echoed; `X-Pegasus-Client-Version` required on every `/api/v1` request,
  absence yielding `client-unsupported`.
- **The solution-contents architecture fact** —
  `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:128` declares
  `ApplicationSolutionExcludesSourceWorkspaces`; its expected seven-path array is `:137-149`; it uses
  `XDocument.Load`, `.Order(StringComparer.Ordinal)` and `FindRepositoryRoot()`.
- **Reusable helpers, with corrected line numbers.** `ProjectReferences(string root, string relativeProjectPath)`
  begins at `:493` (the ticket body cites `:497` — measured `:493`; the body's `:137-149` and
  `:128` citations are exact). `ForbiddenDirectDependencies(XDocument)` at `:480-491` already walks
  `PackageReference`, `FrameworkReference` **and** `Reference` elements and is the closest existing
  shape for the new no-dependency fact. `FindRepositoryRoot()` is at `:509`.
- **The architecture test project can host the serialization facts.**
  `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj` targets `net10.0`, sets
  `RestorePackagesWithLockFile=true`, references xunit 2.9.3 and `Microsoft.NET.Test.Sdk` 17.14.1, and
  already project-references Core, Infrastructure, Web and Worker. It runs unfiltered in the CI
  `unit` lane (`.github/workflows/ci.yml:131-148`, a chained `dotnet test` over `Pegasus.Core.Tests`
  then `Pegasus.ArchitectureTests`, with the comment at `:132-133` recording that both run whole
  because neither declares a test trait).
- **`tests/Pegasus.Api.ContractTests` does not exist.** `ls tests` returns exactly
  `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`. [[TEST-001]] (plan
  handle `DSK-08-01`) creates it.
- **There is no `openapi/` directory.** `ls openapi` → *No such file or directory*. The
  `openapi/pegasus-v1.json` snapshot this board's contract changes normally ripple into does not
  exist yet; [[GWY-004]] (plan handle `DSK-03-04`) creates it. Saying a change ripples into it today
  would be false.
- **No `Directory.Packages.props` exists** (`ls Directory.Packages.props` → *No such file*).
  [[FND-027]] (plan handle `DSK-02-02`) introduces central package management. Because this project
  takes zero packages, it needs no `PackageVersion` entry either way.
- **`Directory.Build.props` (19 lines) applies to the new project**: `TreatWarningsAsErrors=true`,
  `AnalysisLevel=latest-recommended`, `Nullable`, `ImplicitUsings`, `LangVersion=latest`,
  `Deterministic=true`, `Version=0.1.0-alpha.1`.

### Assumptions

- **A-FND029-1 — `JsonStringEnumConverter` applied by attribute serialises and *deserialises*
  correctly through the shared `PegasusJson.Options` in both directions for every enum this project
  declares.** *Confirms it*: the step-12 round-trip facts, which must include an unknown-enum-value
  case. *If wrong*: an unknown value from a newer gateway throws on an older desktop instead of
  surfacing as a problem, so the desktop breaks on a compatible-looking response. The failure mode
  is exactly what the "unknown enum value" case in the tier-2 evidence obligation exists to catch.
- **A-FND029-2 — this project declares no enum on day one.** The five types named by the ticket body
  (`PagedResult<T>`, `PegasusProblem`, `MutationEnvelope`, `PegasusHeaders`,
  `ClientCompatibilityResponse`) are records and constant classes; `Channel` on
  `ClientCompatibilityResponse` is specified as a `string` by area 04 § 3 item 5, not an enum.
  *Confirms it*: `grep -rn 'enum ' src/Pegasus.Contracts/` after step 9 returns nothing. *If wrong*:
  step 9's blanket "add the attribute on every enum" applies to whichever enum arrives, and the
  round-trip fact must cover it.
- **A-FND029-3 — `dotnet restore ./Pegasus.slnx --locked-mode` accepts the new project once its lock
  file is generated with `--force-evaluate`.** *Confirms it*: step 13's restore. *If wrong*: the CI
  composite action `.github/actions/dotnet-build/action.yml:22-23` fails on every lane, because its
  cache key already globs `src/**/packages.lock.json`.
- **A-FND029-4 — the server entry point from [[FND-028]] (plan handle `DSK-02-03`) exists and accepts
  a new project path when this ticket runs.** *Confirms it*: `ls Pegasus.Server.slnf`. *If wrong*:
  step 10's second edit is a no-op and must be recorded as deferred in the plan, not silently
  dropped — Contracts is `net10.0` and genuinely must build on Linux.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | The assembly holds only records and `const` strings; it has no state. `src/Pegasus.Core/Pegasus.Core.csproj` is the shape being copied and is likewise stateless. |
| Unattended execution — must it run with every desktop closed? | **No** | A type library executes only inside its host. The library is compiled into both hosts; it introduces no process. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No** | The project carries no secret by construction; the acceptance criteria forbid a `PackageReference` of any kind, and area 04 § 3 item 8 states the desktop package carries "none". |
| Public callback — must an external service call a stable public endpoint? | **No** | No endpoint is added here; endpoints are [[GWY-001]] (plan handle `DSK-03-01`) onward. |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **Yes — and it lands on the already-existing evolved `Pegasus.Web` gateway, not on any new Azure resource.** | The concurrency envelope exists *because* `expectedVersion` and `editLeaseToken` must be validated where the client cannot reach: `src/Pegasus.Core/Workflow/CaseEditAuthority.cs` owns lease validation and `CaseWorkflowContracts.cs:182-188` is the request Core checks. L-01 fixes that host as `Pegasus.Web` evolved in place; ADR-0103 (authored by [[FND-005]], plan handle `DSK-00-05`) records "gateway, never direct database access". No Azure write arises: the Container App already exists. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | No measurement exists or is claimed. The placement follows from the previous row and from L-01, not from a benchmark; asserting a measured advantage would be the dishonest answer this test exists to catch. |

**Conclusion.** Five "no" and one "yes"; the "yes" names *where* — the existing gateway process — and
places nothing new anywhere. The assembly itself is compiled into both the gateway and the desktop
and carries no responsibility of its own.

## Implications

1. **The paging envelope must not carry a total.** Not as a style preference: no Core port produces
   one, and `EfCaseQueryStore.cs:115-133` shows the implementation deliberately avoids a second
   query. An `int? Total` member would be `null` on every endpoint or would oblige a `COUNT` no port
   performs. This constrains step 3 to exactly five members and makes the
   `grep -rn 'Total' src/Pegasus.Contracts/Paging/` check in § Verification a real gate.
2. **Two tickets must state the same five members word for word.** This ticket's step 3 and
   [[GWY-001]] step 5 are the two statements. The mechanical guard is the serialization fact of
   step 12 asserting the serialised property set, not a review convention.
3. **`ActionActor` must not cross.** `CaseMutationRequest` carries it; the wire envelope drops it and
   the gateway reconstructs the actor from the authenticated principal. The
   `ContractsProjectHasNoDependencies` fact enforces the *project* boundary; a `grep` for
   `ActionActor` under `src/Pegasus.Contracts/` enforces the *type* boundary, and both belong in the
   verification.
4. **The serialization facts have a temporary home.** `tests/Pegasus.Api.ContractTests` does not
   exist, so `tests/Pegasus.ArchitectureTests/ContractSerializationTests.cs` is the landing place —
   and that project must gain a `ProjectReference` to `Pegasus.Contracts`, which is itself a change
   to a file the solution-contents fact reads. The relocation to
   `tests/Pegasus.Api.ContractTests` when [[TEST-001]] lands is a recorded follow-up, not a
   forgotten one.
5. **No OpenAPI ripple is claimable today.** With no `openapi/` directory, the honest ripple is
   "future": [[GWY-004]] snapshots these types once endpoints expose them, and [[GWY-005]] (plan
   handle `DSK-03-05`) generates the client from that snapshot.
6. **The `desk:` prefix is a precedent, not a novelty.** `RequireOperationKey`'s `mcp:` rule
   (`AutomationMcpErrors.cs:76-89`) already fixes the trim, the prefix test, the 100-character cap
   and the whitespace/control rejection. `OperationKeys.MaxLength = 100` in Contracts must equal that
   number, and the plan should say so rather than restating the validation logic here — the
   validator stays in the gateway.
7. **`TreatWarningsAsErrors=true` bites immediately.** Attribute-heavy record declarations and
   `[JsonConverter]` usage must compile warning-free at `AnalysisLevel=latest-recommended`;
   the acceptance bar is "zero warnings", and relaxing `Directory.Build.props` is out of scope.

## Open questions

- None that must be answered before implementation. Every shape this project declares is fixed by a
  named plan section (area 03 § 3 rows `:165-169`, area 04 § 3 item 5) or by a measured repository
  fact recorded above, and the four assumptions each name the command inside this ticket that settles
  them. The one boundary that is a *scope* question rather than an open one — whether these
  serialization facts stay in `Pegasus.ArchitectureTests` — is owned by [[TEST-001]] and is recorded
  in the plan's Risks section, not here.
