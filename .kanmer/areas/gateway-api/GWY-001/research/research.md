# Research — GWY-001: the shared contract vocabulary in `src/Pegasus.Contracts`

## Question

What must this ticket actually author, given that [[FND-029]] (plan handle `DSK-02-04`) owns every
envelope type in `src/Pegasus.Contracts`, and what does the repository already fix about the DTO
conventions, the problem-type catalogue, the paging shape and the operation-key rule so that this
ticket documents and enforces them rather than reinventing them?

## Current behaviour

**No parity-matrix row covers this ticket, and none should.**
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted, not
copied: `grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` returns **46** —
and every row is "keyed by the Razor page model and handler group that implements it today"
(`parity-matrix.md:3-5`). A dependency-free DTO assembly plus one architecture fact has no page
model and no operator-observable capability, so it falls outside the matrix by construction.

The closest existing repository mechanisms — what does this job today:

- **There is no DTO layer, and no wire contract at all.** Razor page models bind Core records
  straight into markup: `src/Pegasus.Web/Pages/Cases/Index.cshtml.cs:65` exposes
  `public SearchCasesResult? Results { get; private set; }`. That is safe only because the Core
  record never leaves the process — `CaseMutationRequest`
  (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188`) carries `ActionActor`, a
  server-only identity type that must never appear on the wire.
- **There is no problem-details machinery in the web host.**
  `grep -n 'AddProblemDetails\|ProblemDetails' src/Pegasus.Web/Program.cs` returns **no matches**:
  the composition root (1,216 lines) registers no `IProblemDetailsService`. The nearest thing to an
  error vocabulary is `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs`, whose `ExecuteAsync`
  (`:19-67`) translates `StaffAuthorizationException`, `CaseEditLeaseExpiredException`,
  `CaseEditLeaseConflictException` and `CaseVersionConflictException` into caller-safe prose and
  collapses everything unexpected into a generic failure, because "no infrastructure detail crosses
  the boundary" (`:7-15`). It produces **messages**, never a stable machine-readable `type` URI —
  which is precisely the gap the thirteen-slug catalogue closes.
- **The nearest thing to an operation-key convention** is
  `AutomationMcpErrors.RequireOperationKey` (`:76-89`): trim, `mcp:` prefix,
  `Length is <= 4 or > 100`, and rejection of any whitespace or control character. The desktop's
  `desk:` prefix is that same rule with a different prefix, not a new invention.
- **The nearest thing to an assembly-boundary guarantee** is
  `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:42-48`
  (`CoreHasNoInfrastructureOrHostDependencies`), which reads
  `typeof(CoreAssembly).Assembly.GetReferencedAssemblies()` and asserts none matches
  `ForbiddenCoreDependencyPrefixes` (`:23-39`, sixteen prefixes) through the
  `IsForbiddenCoreDependency` helper at `:475-478`. That is the exact shape step 8 must copy.
- **The single shared vocabulary list today** is `src/Pegasus.Web/Presentation/OperatorLabels.cs`
  (685 lines). It stays where it is for this ticket: [[GWY-016]] (plan handle `DSK-03-16`) and
  [[FEAT-023]] (plan handle `DSK-05-23`) own its relocation.

## Findings

### Facts

Verified by reading the repository at fork `main`, 2026-08-24; each carries the command or path that
produced it.

- **`src/Pegasus.Contracts` does not exist yet.** `ls src/Pegasus.Contracts` → *No such file or
  directory*; `ls src` returns exactly `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`,
  `Pegasus.Worker`. `Pegasus.slnx` (14 lines) lists four `/src/` and three `/tests/` projects. The
  project is created by [[FND-029]], and step 2 of this ticket's body is therefore a real stop
  condition, not a formality.
- **[[FND-029]] owns all four envelope types and states the same five-member `PagedResult<T>`.**
  Its plan document (read 2026-08-24) pins in step 3
  `public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, bool HasPreviousPage, bool HasNextPage)`
  plus `Paging/PagingLimits.cs` with `MaxPageSize = 200`; step 4 the thirteen problem slugs; step 5
  `ProblemDetails/PegasusProblem.cs`; step 6 `Requests/MutationEnvelope.cs` with
  `OperationKeys.MaxLength = 100` and the `desk:` prefix; step 11 the
  `ContractsProjectHasNoDependencies` fact. Its own Risks section names this ticket as "the
  consumer: it extends those types in place, changes no existing member". The two statements agree
  word for word today, and keeping them so is a stop condition on both sides.
- **No Core paging port produces a total count.** Four paged read ports exist and all four are the
  fetch-one-extra cursor shape:
  - `SearchCasesResult` — `src/Pegasus.Core/Cases/CaseQueries.cs:69-74` —
    `(IReadOnlyList<CaseSearchItem> Items, int Page, int PageSize, bool HasPreviousPage, bool HasNextPage)`.
  - `OrganizationListPage` — `src/Pegasus.Core/Cases/OrganizationAdministration.cs:47-52` —
    `(… Organizations, int PageNumber, int PageSize, bool HasPreviousPage, bool HasMoreOrganizations)`.
  - `ListAutomationActivityResult` — `src/Pegasus.Core/Identity/AutomationActivity.cs:44-50` —
    `(… Records, string? CorrelationId, int Page, int PageSize, bool HasPreviousPage, bool HasMoreRecords)`.
  - `ListStaffAccountsResult` — `src/Pegasus.Core/Identity/StaffAccountAdministration.cs:16-21` —
    `(… Accounts, int PageNumber, int PageSize, bool HasPreviousPage, bool HasMoreAccounts)`.
  - **Refinement of the ticket body, not a contradiction.** The body calls these "the other three
    paged ports, all the same five-member cursor shape". Measured: the *pattern* is identical in all
    four, but the member **names** differ per port (`Items`/`Organizations`/`Records`/`Accounts`;
    `Page`/`PageNumber`; `HasNextPage`/`HasMoreOrganizations`/`HasMoreRecords`/`HasMoreAccounts`)
    and `ListAutomationActivityResult` carries a **sixth** member, `string? CorrelationId`. The
    body's load-bearing claim — that none of them counts — is confirmed exactly.
- **The implementation behind those flags issues no `COUNT`.**
  `src/Pegasus.Infrastructure/Persistence/EfCaseQueryStore.cs:115-133`: `.Take(query.PageSize + 1)`,
  then `var hasNextPage = page.Length > query.PageSize;` and a return of
  `(items, query.Page, query.PageSize, query.Page > 1, hasNextPage)`. No `CountAsync` appears in the
  method. This is the evidence to quote if anyone proposes an `int? Total`.
- **A per-endpoint cap already exists that is lower than 200.** `ListIntake`
  (`src/Pegasus.Core/Intake/IntakeQueryUseCases.cs:22-27`) throws
  `ArgumentOutOfRangeException` when `query.PageSize is < 1 or > 100`, and at `:16-21` when
  `query.Page is < 1 or > 10_000`. `PagingLimits.MaxPageSize = 200` is therefore a ceiling, not a
  promise — exactly the XML-doc note step 5 requires.
- **`CaseMutationRequest` is the concurrency shape to mirror.**
  `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188`:
  `public abstract record CaseMutationRequest(Guid CaseId, long ExpectedVersion, ActionActor Actor, string OperationKey, string Reason, string EditLeaseToken)`.
  It is `abstract`; `CaseId` is a `Guid` (the route carries it on the wire); `ActionActor` is the
  member the wire envelope drops. The four conflict exceptions the problem catalogue maps are in the
  same file — `CaseVersionConflictException` at `:125` (carrying `ExpectedVersion` and
  `ActualVersion`), `CaseEditLeaseConflictException` at `:135`, `CaseEditLeaseExpiredException` at
  `:143`, `CaseOperationConflictException` at `:151`. Two of them expose a **case version**, which is
  why `PegasusProblem` needs a typed `CurrentVersion` accessor.
- **The operation-key rule already exists in code.** `AutomationMcpErrors.cs:76-89`: trimmed,
  prefixed `mcp:`, `Length is <= 4 or > 100`, no whitespace or control characters. Its doc comment
  (`:70-75`) says it mirrors "the existing command contracts (100-character maximum, no whitespace or
  control characters)". `OperationKeys.MaxLength = 100` must equal that number.
- **200 characters is allowed in exactly one Core area.**
  `src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs:398` —
  `public const int MaximumOperationKeyLength = 200;` inside `UnidentifiedValidation` (`:394`),
  beside `MaximumDetailLength = 1000` and `MaximumReasonLength = 500`. The ticket body's "200 only
  where Core allows" is confirmed and this is the one place.
- **The problem catalogue is fixed at thirteen slugs.**
  `docs/desktop/03-gateway-api-and-data/README.md:167` (§ 3 row *Problem details*): `validation`,
  `not-authorized`, `version-conflict`, `lease-conflict`, `lease-expired`, `operation-conflict`,
  `client-unsupported`, `password-change-required`, `account-disabled`, `provider-unavailable`,
  `not-found`, `rate-limited`, `maintenance` — all under `urn:pegasus:problem:`, with
  `correlationId` always present and no payload dumps.
- **Page-size cap and the totals rule.** `docs/desktop/03-gateway-api-and-data/README.md:169`:
  offset paging with `pageSize ≤ 200`, newest-first default, and "totals returned only where the
  existing query port already counts" — which, per the four ports above, is nowhere.
- **`CoreAssembly` is the marker-type precedent.** `src/Pegasus.Core/CoreAssembly.cs` is six lines:
  a namespace, a three-line XML summary ("Stable marker for the Core assembly.") and
  `public static class CoreAssembly;`. It exists solely so `DependencyDirectionTests.cs:45` can write
  `typeof(CoreAssembly).Assembly.GetReferencedAssemblies()`. `ContractConventions` (step 7) is the
  same device with a documentation payload attached — and it is what step 8's assembly-reference
  assertion will anchor `typeof(…)` on.
- **The forbidden-prefix list does not yet cover WinUI or `Pegasus.Core`.**
  `ForbiddenCoreDependencyPrefixes` (`DependencyDirectionTests.cs:23-39`) holds sixteen prefixes:
  `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`, `Azure`, `Microsoft.Graph`, `Box`,
  `MimeKit`, `DocumentFormat.OpenXml`, `UglyToad.PdfPig`, `Microsoft.Data.SqlClient`,
  `System.Net.Http`, `OpenIddict`, `ModelContextProtocol`, `Pegasus.Infrastructure`, `Pegasus.Web`,
  `Pegasus.Worker`. It does **not** contain `Microsoft.WindowsAppSDK`, `Microsoft.UI.Xaml` or
  `Pegasus.Core`, all three of which step 8 must forbid for Contracts. The list is a private
  `static readonly string[]` used by `IsForbiddenCoreDependency` (`:475-478`), which matches an exact
  name or a `"{prefix}."` start, ordinal.
- **The test project must reference Contracts for an assembly-level assertion to be possible.**
  `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj` (30 lines) project-references
  exactly `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker` today.
  [[FND-029]] step 12 adds the fifth reference to `Pegasus.Contracts`; without it,
  `typeof(ContractConventions)` does not compile in the test project.
- **`Directory.Build.props` (19 lines) applies to the new project from its first build**:
  `TreatWarningsAsErrors=true` (`:8`), `AnalysisLevel=latest-recommended` (`:7`), `Nullable`,
  `ImplicitUsings`, `LangVersion=latest`, `Deterministic=true`, `Version=0.1.0-alpha.1`. "Zero
  warnings" in the acceptance criteria is enforced by the compiler, not by review.
- **A stray Markdown file really would fail CI.** `.github/workflows/ci.yml` § `documentation`
  (`:70-87`) runs `./scripts/Test-TestMarkdownPlacement.ps1` and
  `./scripts/Test-DocumentationLinks.ps1` on `windows-latest`, and its own comment (`:71-74`) says it
  is "the one lane every change set runs". This is why step 7 puts the conventions in XML
  documentation rather than a `.md` file.
- **There is no `openapi/` directory.** `ls openapi` → *No such file or directory*. The
  `openapi/pegasus-v1.json` snapshot that contract changes normally ripple into does not exist yet;
  [[GWY-004]] (plan handle `DSK-03-04`) creates it and [[GWY-005]] (plan handle `DSK-03-05`)
  generates the Kiota client from it. Claiming a snapshot ripple today would be false.
- **No `Directory.Packages.props` and no `Pegasus.Server.slnf` exist yet.** Both `ls` calls return
  *No such file or directory*; [[FND-027]] (plan handle `DSK-02-02`) and [[FND-028]] (plan handle
  `DSK-02-03`) create them. Neither is edited by this ticket — Contracts takes no packages, and the
  server entry-point registration is [[FND-029]] step 10's.

### Assumptions

- **A-GWY001-1 — [[FND-029]] has landed before this ticket starts, so all four envelope files exist
  and steps 3–6 are the "extend in place" branch.** *Confirms it*:
  `ls src/Pegasus.Contracts/Paging/PagedResult.cs src/Pegasus.Contracts/ProblemDetails/PegasusProblemTypes.cs src/Pegasus.Contracts/ProblemDetails/PegasusProblem.cs src/Pegasus.Contracts/Requests/MutationEnvelope.cs`
  at step 2. *If wrong in part*: this ticket creates only the missing files, to the exact shapes
  [[FND-029]] steps 3–6 pin, and records which case applied in the plan — the body requires that
  record. *If wrong entirely* (no csproj at all): step 2 is a hard stop and the blocker is recorded;
  a second Contracts project must never be created. The diff estimate below is stated for the
  expected branch and the fallback is priced separately.
- **A-GWY001-2 — the `ContractsProjectHasNoDependencies` fact [[FND-029]] step 11 writes reads the
  csproj XML, so an assembly-reference assertion is genuinely additive rather than duplicative.**
  [[FND-029]]'s own step 11 describes loading the csproj with `XDocument` and asserting no
  `PackageReference`/`ProjectReference`/`FrameworkReference` element. *Confirms it*: read the fact
  before editing. *If wrong* (it already asserts on the loaded assembly), step 8 becomes a
  no-op check and must be recorded as such rather than adding a parallel assertion — the body's
  "never add a second matcher" applies to both halves.
- **A-GWY001-3 — `GetReferencedAssemblies()` on a Contracts assembly that references nothing returns
  only the framework assemblies the compiler actually kept.** The C# compiler prunes unused
  references, so the assertion is `DoesNotContain(forbidden)` — never `Assert.Empty`. *Confirms it*:
  the fact passing at step 10. *If wrong*: an `Assert.Empty` formulation fails on
  `System.Runtime`/`System.Text.Json` and would be "fixed" by weakening the test — the failure mode
  this assumption exists to name.
- **A-GWY001-4 — [[FND-037]] (plan handle `DSK-02-12`) does not also add WinUI prefixes to
  `ForbiddenCoreDependencyPrefixes`, or if it does, the two edits merge cleanly.** [[FND-037]]
  extends the same file for "the desktop boundaries and the no-WebView rule". *Confirms it*: read
  `DependencyDirectionTests.cs` at step 1 and check for `Microsoft.UI.Xaml` before adding it. *If
  wrong*: a merge conflict in one array, resolved by keeping one entry per prefix — never by
  declaring a second array with overlapping contents.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | The ticket adds a documentation-carrying marker type and one architecture assertion. `src/Pegasus.Contracts` holds records and `const` strings and has no state; `src/Pegasus.Core/CoreAssembly.cs` is the same device and likewise holds none. |
| Unattended execution — must it run with every desktop closed? | **No** | A type library and an xunit fact execute only inside a host or a test run. No process is introduced. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No** | The project carries no secret by construction: the acceptance criteria forbid a `PackageReference` of any kind, and the architecture fact this ticket extends fails if one appears. Nothing secret is modelled here. |
| Public callback — must an external service call a stable public endpoint? | **No** | No endpoint is added. The first `/api/v1` route group is [[GWY-002]] (plan handle `DSK-03-02`); the only anonymous endpoint in the map, `GET /api/v1/client-compatibility`, is [[GWY-023]] (plan handle `DSK-04-06`). |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **Yes — and it lands on the already-existing evolved `Pegasus.Web` gateway, not on any new Azure resource.** | The concurrency and idempotency envelope exists *because* `expectedVersion`, `editLeaseToken` and `operationKey` must be validated where the client cannot reach: `src/Pegasus.Core/Workflow/CaseEditAuthority.cs` owns lease validation, `CaseWorkflowContracts.cs:182-188` is the request Core checks, and `AutomationMcpErrors.cs:76-89` is where the key rule is enforced today. Locked decision L-01 fixes that host as `Pegasus.Web` evolved in place (`docs/desktop/README.md` § Locked decisions); ADR-0103 records "gateway, never direct database access". The Container App already exists, so no Azure write arises. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | No measurement exists or is claimed. The placement follows from the row above and from L-01, not from a benchmark; area 03 § 2 assumption A-1 explicitly defers the resource question to the area-10 performance baseline. Asserting a measured advantage would be the dishonest answer this test exists to catch. |

**Conclusion.** Five "no" and one "yes"; the "yes" names *where* the enforcement lives — the existing
gateway process — and places nothing new anywhere. The assembly is compiled into both the gateway and
the desktop and carries no responsibility of its own.

## Implications

1. **This ticket authors far less than its title suggests, and that is correct.** [[FND-029]] owns
   `PagedResult<T>`/`PagingLimits`, `PegasusProblemTypes`, `PegasusProblem` and `MutationEnvelope`.
   What GWY-001 owns outright is the `ContractConventions` marker type (step 7) and the extension of
   the `ContractsProjectHasNoDependencies` fact with an assembly-reference assertion (step 8).
   Steps 3–6 are *check-then-extend*, and the body requires recording which branch applied. A plan
   that treats this as a greenfield authoring ticket has misread the ownership split.
2. **The marker type is not decoration — it is load-bearing for step 8.**
   `CoreHasNoInfrastructureOrHostDependencies` works only because `CoreAssembly` gives it a
   `typeof(…)` anchor. `ContractConventions` plays the same role for Contracts, so step 7 must land
   before step 8 can compile, and the checklist order must reflect that.
3. **The forbidden-prefix list must grow by three, and only three.** `Microsoft.WindowsAppSDK`,
   `Microsoft.UI.Xaml` and `Pegasus.Core` are absent from `ForbiddenCoreDependencyPrefixes` today and
   are all required by step 8; `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`,
   `Pegasus.Infrastructure`, `Pegasus.Web` and `Pegasus.Worker` are already there. Because that array
   is shared with the Core fact, adding `Pegasus.Core` to it would break
   `CoreHasNoInfrastructureOrHostDependencies` (Core references itself trivially — the prefix would
   match its own assembly name). The Contracts assertion therefore needs its **own** prefix array
   used through the same `IsForbiddenCoreDependency` *shape*, inside the one existing fact — which is
   what "reusing the existing helper shape" and "never a second matcher" together mean.
4. **The paging XML-doc note has a measured source.** `ListIntake` refuses a page size above 100
   (`IntakeQueryUseCases.cs:22-27`), so the note is a statement of fact about the codebase, not a
   hedge.
5. **The problem-type catalogue closes a real gap, not a stylistic one.** There is no
   `AddProblemDetails` call in `Program.cs` today, and `AutomationMcpErrors` emits prose. The
   thirteen `urn:pegasus:problem:` slugs are what lets the desktop tell reload-and-reacquire
   (`version-conflict`) from reacquire-the-lease (`lease-expired`) from correct-the-input
   (`validation`) — the operator-visible consequence the ticket body claims.
6. **No OpenAPI or generated-client ripple is claimable today.** With no `openapi/` directory, the
   honest statement is "future": every member added to these types after [[GWY-004]] lands changes
   `openapi/pegasus-v1.json` and the Kiota output from [[GWY-005]].
7. **`TreatWarningsAsErrors=true` bites on documentation too.** XML documentation comments on public
   members are analyzed at `latest-recommended`; a malformed `<see cref="…"/>` in the conventions
   comment is a build break, not a warning.

## Open questions

- None that must be answered before implementation. Every shape this ticket touches is pinned either
  by a named plan section (`docs/desktop/03-gateway-api-and-data/README.md:163-169`) or by
  [[FND-029]]'s already-written plan, and the four assumptions above each name the command inside
  this ticket's own steps that settles them. The one genuine branch — whether [[FND-029]] has landed
  — is resolved by step 2 and recorded in the plan, which is what the body instructs; it is a
  sequencing dependency, not an unanswered question, so no `open-questions` document is created.
