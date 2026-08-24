# Files — GWY-001

Surveyed 2026-08-24 against fork `main`. Every existing path below was confirmed with `ls`/`grep`;
paths that do not exist yet are marked with the ticket that creates them.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Contracts/ContractConventions.cs` | **New — the one file this ticket owns outright** (body step 7). A marker type carrying the DTO conventions as XML documentation: `Request`/`Response` suffixes, Core records never exposed directly (they carry `ActionActor` and server-only members), enums serialise as strings, dates are `DateTimeOffset` in UTC. Copy the six-line shape of `src/Pegasus.Core/CoreAssembly.cs`. It is **not** decoration: step 8's assembly-reference assertion needs a `typeof(…)` anchor inside the Contracts assembly, exactly as `CoreAssembly` anchors `DependencyDirectionTests.cs:45`. Must not be a `.md` file — see the CI `documentation` lane below. |
| `src/Pegasus.Contracts/Paging/PagedResult.cs` (created by [[FND-029]] (plan handle `DSK-02-04`)) | **Check-then-extend, body step 5.** If present, change no existing member — the five-member list `(Items, Page, PageSize, HasPreviousPage, HasNextPage)` must stay word for word identical to [[FND-029]] step 3. If absent, create it to exactly that shape. Adding a total-count member under any name is a stop condition: no Core paging port produces one. |
| `src/Pegasus.Contracts/Paging/PagingLimits.cs` (created by [[FND-029]]) | **Extend with XML documentation only**, body step 5's closing sentence: record that a per-endpoint cap may be lower than `MaxPageSize = 200` — `ListIntake` refuses a page size above 100 (`src/Pegasus.Core/Intake/IntakeQueryUseCases.cs:22-27`). No constant changes. |
| `src/Pegasus.Contracts/ProblemDetails/PegasusProblemTypes.cs` (created by [[FND-029]]) | **Check-then-extend, body step 3.** Exactly thirteen slugs under `Prefix = "urn:pegasus:problem:"`. A fourteenth slug requires an area-03 plan change first, not an edit here. There is one literal path and one type name for this concept — never `Problems/ProblemTypes.cs`, never a type called `ProblemTypes`. |
| `src/Pegasus.Contracts/ProblemDetails/PegasusProblem.cs` (created by [[FND-029]]) | **Check-then-extend, body step 4.** Whichever branch applies, XML-document that the body never carries payload dumps or infrastructure detail — the rule `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:7-15` already states for MCP. `CurrentVersion` and `MinimumVersion` stay typed extension accessors because `version-conflict` and `client-unsupported` carry them. Never `Problems/PegasusProblem.cs`. |
| `src/Pegasus.Contracts/Requests/MutationEnvelope.cs` (created by [[FND-029]]) | **Check-then-extend, body step 6.** Confirm `ExpectedVersion`, `OperationKey`, `EditLeaseToken` are body fields and not headers, then XML-document the desktop key format `desk:<guid>` under the same constraints `RequireOperationKey` enforces (`AutomationMcpErrors.cs:76-89` — ≤ 100 characters, no whitespace, no control characters), noting that 200 is allowed only where Core allows it (`src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs:398`). `ActionActor` is never copied in. Never `Commands/CommandEnvelope.cs`, never a type called `CommandEnvelope`. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` | 520 lines. **One fact edited, body step 8.** Extend the single `ContractsProjectHasNoDependencies` fact that [[FND-029]] step 11 adds — leave its csproj-XML assertions (no `PackageReference`, no `ProjectReference`, no `FrameworkReference`) untouched and add an assembly-reference assertion over `typeof(ContractConventions).Assembly.GetReferencedAssemblies()`. Needs its own forbidden-prefix array (see *Ripple effects*), used through the shape of `IsForbiddenCoreDependency` (`:475-478`). Never a second fact; never the name `ContractsHasNoInfrastructureOrHostDependencies`. |

## Context files

What the implementer must **read**, and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| [[FND-029]]'s `plan` and `files` documents (Kanmer `get_ticket_doc FND-029 plan`) | The authoritative pinned shape of all four envelope types this ticket extends — step 3 the five paging members, step 4 the thirteen slugs, step 5 `PegasusProblem`, step 6 `MutationEnvelope` and `OperationKeys.MaxLength = 100`, step 11 the `ContractsProjectHasNoDependencies` fact. Read it **before** editing anything under `src/Pegasus.Contracts`: this ticket may not change a member [[FND-029]] declared, and its Risks section names GWY-001 as "the consumer". |
| `src/Pegasus.Core/CoreAssembly.cs` (6 lines) | The exact precedent for `ContractConventions`: namespace, three-line XML summary, `public static class CoreAssembly;`. It exists solely so an architecture fact can write `typeof(CoreAssembly).Assembly`. Copy the device; the only difference is that this one also carries the conventions text. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:42-48` | `CoreHasNoInfrastructureOrHostDependencies` — the four-line idiom step 8 must mirror: `GetReferencedAssemblies()` then `Assert.DoesNotContain(references, reference => IsForbiddenCoreDependency(reference.Name ?? string.Empty))`. Note it is `DoesNotContain`, never `Assert.Empty`: the compiler always keeps framework references, so an emptiness assertion would fail and invite someone to weaken the test. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:23-39` | `ForbiddenCoreDependencyPrefixes`, sixteen entries. Tells the implementer two things: the format (bare assembly-name prefixes, ordinal) and, critically, that it **does not** contain `Microsoft.WindowsAppSDK`, `Microsoft.UI.Xaml` or `Pegasus.Core` — the three the Contracts assertion additionally needs. Adding `Pegasus.Core` to *this* array would break the Core fact, which is why the Contracts assertion needs its own array. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:475-478` | `IsForbiddenCoreDependency` — the matcher shape to reuse: exact-name equality or a `"{prefix}."` ordinal start. Reuse the *shape* over a Contracts-specific array; do not widen this helper's array. |
| `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj` (30 lines) | That the test project references exactly four projects today. `typeof(ContractConventions)` will not compile until [[FND-029]] step 12 adds the fifth `ProjectReference` to `Pegasus.Contracts`. If that reference is missing when this ticket runs, adding it here is in scope — but it belongs to [[FND-029]], so record it. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188` | The shape `MutationEnvelope` mirrors and the one member it drops: `ActionActor Actor`. Also that the Core record is `abstract` and carries `Guid CaseId` — the wire envelope is neither abstract nor carries the id, because the route does. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:125-157` | The four conflict exceptions the catalogue maps: `CaseVersionConflictException` (`:125`, exposing `ExpectedVersion` **and** `ActualVersion`), `CaseEditLeaseConflictException` (`:135`, exposing `CaseVersion`), `CaseEditLeaseExpiredException` (`:143`, exposing `CaseVersion`), `CaseOperationConflictException` (`:151`). Three of the four surface a case version — that is *why* `PegasusProblem` needs a typed `CurrentVersion` accessor rather than a free-form extensions bag. |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:7-15` | The boundary rule `PegasusProblem`'s XML documentation must restate: domain refusals "carry deliberately safe messages … anything unexpected collapses to a generic failure so no infrastructure detail crosses the boundary", and "no token or other holder material crosses the boundary". |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:70-89` | The live operation-key rule and its doc comment: `mcp:` prefix, `Length is <= 4 or > 100`, no whitespace or control characters, mirroring "the existing command contracts". This fixes `OperationKeys.MaxLength = 100` and tells the implementer the *validator* stays in the gateway — Contracts documents and declares, it does not re-implement. |
| `src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs:394-400` | `UnidentifiedValidation.MaximumOperationKeyLength = 200` — the single Core area where a 200-character key is legal. Quote this, not a general "200 sometimes", when documenting the cap. |
| `src/Pegasus.Core/Intake/IntakeQueryUseCases.cs:16-27` | `ListIntake` refuses `Page` outside `1..10_000` and `PageSize` outside `1..100`. The measured evidence behind step 5's XML-doc note that a per-endpoint cap can be below `PagingLimits.MaxPageSize`. |
| `src/Pegasus.Core/Cases/CaseQueries.cs:69-74` | The canonical five-member paging shape whose member names `PagedResult<T>` adopts. |
| `src/Pegasus.Infrastructure/Persistence/EfCaseQueryStore.cs:115-133` | *Why* there is no total: `.Take(query.PageSize + 1)`, `hasNextPage = page.Length > query.PageSize`, and no `CountAsync` in the method. The evidence to quote if anyone proposes an `int? Total`. |
| `src/Pegasus.Core/Cases/OrganizationAdministration.cs:47-52`, `src/Pegasus.Core/Identity/AutomationActivity.cs:44-50`, `src/Pegasus.Core/Identity/StaffAccountAdministration.cs:16-21` | The other three paged ports. They tell the implementer the *pattern* is universal but the *names* are not (`PageNumber` vs `Page`; `HasMoreOrganizations`/`HasMoreRecords`/`HasMoreAccounts` vs `HasNextPage`; a sixth `CorrelationId` on the automation one) — so gateway mapping cannot be done by naive name matching, and none of the four carries a count. |
| `docs/desktop/03-gateway-api-and-data/README.md:163-169` | The five binding § 3 rows: *Contracts* (no EF/ASP.NET/WinUI; Core records never exposed directly), *Idempotency*, *Concurrency*, *Problem details* (the thirteen slugs verbatim, at `:167`), *Paging/filter/sort* (`pageSize ≤ 200`, "totals returned only where the existing query port already counts", at `:169`). |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § Conventions | That every command body carries `operationKey`, case-scoped commands also carry `expectedVersion` and where Core requires it `editLeaseToken`, and that reads return `version` plus a weak `ETag`. Confirms these are body fields, which is what step 6 must verify. |
| `src/Pegasus.Web/Program.cs` | 1,216 lines with **no** `AddProblemDetails` call (`grep -n 'AddProblemDetails' → no matches`). Tells the implementer that nothing consumes `PegasusProblem` yet: registering problem-details middleware is [[GWY-002]] (plan handle `DSK-03-02`), not this ticket. |
| `Directory.Build.props` (19 lines) | `AnalysisLevel=latest-recommended` (`:7`) and `TreatWarningsAsErrors=true` (`:8`) apply to the new file from its first build, and XML documentation is analyzed too — a malformed `<see cref="…"/>` is a build break, not a warning. |
| `.github/workflows/ci.yml:70-87` | The `documentation` lane — "the one lane every change set runs" — invokes `scripts/Test-TestMarkdownPlacement.ps1`. This is why the conventions live as XML documentation and not as a new `.md` file. |
| `src/Pegasus.Web/Presentation/OperatorLabels.cs` (685 lines) | The file this ticket must **not** move. [[GWY-016]] (plan handle `DSK-03-16`) and [[FEAT-023]] (plan handle `DSK-05-23`) own its relocation into this project. |

## Ripple effects

- **The one architecture fact.** `ContractsProjectHasNoDependencies` gains an assembly-reference
  assertion. It needs a Contracts-specific prefix array — `Microsoft.AspNetCore`,
  `Microsoft.EntityFrameworkCore`, `Microsoft.WindowsAppSDK`, `Microsoft.UI.Xaml`, `Pegasus.Core`,
  `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker` — because adding `Pegasus.Core` to the
  shared `ForbiddenCoreDependencyPrefixes` would make `CoreHasNoInfrastructureOrHostDependencies`
  fail against Core's own assembly name. One fact, two assertions, two arrays: that is not "a second
  matcher".
- **Test-project reference.** `typeof(ContractConventions)` compiles only if
  `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj` project-references
  `Pegasus.Contracts` — [[FND-029]] step 12's edit. Record its presence at step 2; do not silently
  duplicate it.
- **Merge surface with [[FND-037]] (plan handle `DSK-02-12`).** That ticket also extends
  `DependencyDirectionTests.cs`, for "the desktop boundaries and the no-WebView rule", and may add
  WinUI prefixes. Check before adding `Microsoft.UI.Xaml`; resolve any conflict by keeping one entry
  per prefix, never by declaring an overlapping second array.
- **CI lanes touched.** `.github/workflows/ci.yml` § `unit` runs `Pegasus.ArchitectureTests` whole and
  unfiltered, so the extended fact is checked on every PR. The `documentation` lane runs regardless of
  what changed; it stays green only because no `.md` is added.
- **Callers — none.** Nothing in `src/Pegasus.Web`, `src/Pegasus.Core`, `src/Pegasus.Infrastructure`
  or any desktop project consumes `ContractConventions`; it is a documentation and assertion anchor.
  The first behavioural consumer of these types is [[GWY-002]].
- **OpenAPI and the generated client — a *future* ripple, not a present one.** There is no `openapi/`
  directory today (`ls openapi` → *No such file or directory*), so no snapshot changes here.
  [[GWY-004]] creates `openapi/pegasus-v1.json` and [[GWY-005]] generates the Kiota client from it;
  from that point on, every member added to a Contracts type changes both. Record that as the standing
  consequence.
- **Lock files and restore.** Contracts takes no packages, so `src/Pegasus.Contracts/packages.lock.json`
  ([[FND-029]]'s, byte-identical in shape to `src/Pegasus.Core/packages.lock.json`) does not change and
  `tests/Pegasus.ArchitectureTests/packages.lock.json` does not change — a project reference is not a
  package.
- **Documentation.** None. The conventions are XML documentation inside the project; the ticket body's
  *Documentation changes* section says explicitly that `docs/index.md` gains the OpenAPI location in
  [[GWY-004]], not here.

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **Declaring or redefining any envelope type.** `PagedResult<T>`/`PagingLimits`,
  `PegasusProblemTypes`, `PegasusProblem` and `MutationEnvelope` belong to [[FND-029]]. This ticket
  extends them in place and changes no existing member.
- **A total-count member on `PagedResult<T>` under any name** — explicitly refused; the evidence is in
  the research document, and reintroducing one is a stop condition on both tickets.
- **A fourteenth problem slug** — that is an area-03 plan change, not an edit here.
- **A `Problems/` or `Commands/` folder, or a `ProblemTypes` or `CommandEnvelope` type** — forbidden by
  the acceptance criteria; one literal path and one type name per concept.
- **A second architecture fact or a second no-dependency matcher**, and in particular the name
  `ContractsHasNoInfrastructureOrHostDependencies` — forbidden by body step 8.
- **`src/Pegasus.Web`, `src/Pegasus.Core`, `src/Pegasus.Infrastructure`, `src/Pegasus.Worker` and every
  desktop project** — untouched. Endpoints, problem-details registration and correlation middleware
  arrive in [[GWY-002]].
- **`src/Pegasus.Web/Presentation/OperatorLabels.cs`** — not moved; [[GWY-016]] / [[FEAT-023]] own it.
- **Re-implementing operation-key validation in Contracts** — refused; the validator stays in the
  gateway (`AutomationMcpErrors.cs:76-89`), Contracts declares the constant and documents the format.
- **Any new `.md` file** — would fail the CI `documentation` lane outside
  `docs/(prd|frd|adr|design|desktop)`; the conventions are XML documentation instead.
- **Relaxing `TreatWarningsAsErrors` or `AnalysisLevel`** — refused outright by the ticket's Traps.
- **Azure** — no write of any kind.
