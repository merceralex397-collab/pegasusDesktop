# Plan — FND-029: Create `src/Pegasus.Contracts` with the paging, problem-details, concurrency and operation-key envelopes

**Diff estimate: ~16 files, ~330 lines.**

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. Derived from the files
document, file by file: ten new files under `src/Pegasus.Contracts/` (csproj ~14, `PagedResult.cs`
~12, `PagingLimits.cs` ~10, `PegasusProblemTypes.cs` ~28, `PegasusProblem.cs` ~45,
`MutationEnvelope.cs` ~24, `PegasusHeaders.cs` ~12, `ClientCompatibilityResponse.cs` ~14,
`PegasusJson.cs` ~18, generated `packages.lock.json` ~8 — measured against
`src/Pegasus.Core/packages.lock.json`, 124 bytes); one new test file
(`ContractSerializationTests.cs` ~95); and five small edits — `Pegasus.slnx` +1,
`Pegasus.Server.slnf` +1, `DependencyDirectionTests.cs` +1 array entry and ~26 lines of new fact,
`Pegasus.ArchitectureTests.csproj` +1, `docs/current-architecture.md` ~+4.

## Approach

Declare each envelope **once**, in a project with literally no dependency edge, and prove both
properties mechanically rather than by review. The alternative rejected is putting these types in
`Pegasus.Core`: it would avoid a new top-level project (and so avoid the `AGENTS.md` § Product
invariants ADR obligation entirely), but Core is the *domain* owner and its records carry
server-only members — `CaseMutationRequest` (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188`)
carries `ActionActor` — so putting wire types there either exposes those members or forces Core to
hold two parallel shapes of every request. Area 03 § 3 (Contracts row, `docs/desktop/03-gateway-api-and-data/README.md:163`)
settles it: "Core records are **not** exposed directly (they carry `ActionActor` and server-only
members)". A separate dependency-free assembly is the only shape where the desktop can reference the
wire contract without dragging domain policy across the boundary.

Two design choices inside that shape are load-bearing and are argued from measurement, not taste:

- **`PagedResult<T>` has five members and no total.** All four Core paging ports are the
  fetch-one-extra cursor shape and none counts; `EfCaseQueryStore.cs:115-133` reaches its flags with
  `.Take(query.PageSize + 1)` and issues no `CountAsync`. A total member would be `null` on every
  endpoint or would oblige a second query no port performs.
- **The validator stays in the gateway; Contracts declares the constant.** The live rule is
  `AutomationMcpErrors.RequireOperationKey` (`src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:76-89`):
  trim, `mcp:` prefix, `Length is <= 4 or > 100`, no whitespace or control characters. Contracts
  exports `OperationKeys.MaxLength = 100` and the `desk:` prefix so both sides share one number;
  re-implementing the check here would create a second enforcement point.

## Governing docs

The ticket's `refs` array is empty and `get_doc_gates FND-029` reports `docs_todo: true`, so there is
no linked PRD/FRD/ADR to meet today.

> **New ADR** — ADR-0103 (gateway, never direct database access from workstations), authored by
> [[FND-005]] (plan handle `DSK-00-05`); ADR-0100 (native WinUI 3 client in the fork, which
> authorises the new top-level projects) is claimed by both [[FND-005]] and [[FND-026]] (plan handle
> `DSK-02-01`) — see [[FND-026]]'s plan for the ownership reconciliation.
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table, ADR-0100 and ADR-0103
> rows); if either ADR lands differently this plan is revised before implementation. This ticket
> **cites** ADR-0103 and never edits it — [[GWY-001]] (plan handle `DSK-03-01`) states the same.

Because `refs` is empty, these are the authorities that actually bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 10.3 | A generated client, and no hand-written duplicate DTOs | Steps 2–9 declare each envelope once; step 11's fact keeps the project dependency-free so the generated client can consume it |
| Plan 03 § 3 *Contracts* (`:163`) | `src/Pegasus.Contracts` holds request/response/problem DTOs with no EF, ASP.NET or WinUI references; Core records are not exposed directly | Steps 2, 6, 11 |
| Plan 03 § 3 *Problem details* (`:167`) | Stable `urn:pegasus:problem:<slug>` URIs, exactly thirteen slugs, `correlationId` always present, no payload dumps | Steps 4, 5 |
| Plan 03 § 3 *Paging/filter/sort* (`:169`) | `pageSize ≤ 200`; "totals returned only where the existing query port already counts" | Step 3, and the `grep -rn 'Total'` gate in § Verification |
| Plan 03 § 3 *Idempotency* (`:165`) | Caller-supplied `OperationKey` as a body field; desktop keys `desk:<guid>`, ≤ 100 characters | Step 6 |
| Plan 03 § 3 *Concurrency* (`:166`) | Explicit body fields `expectedVersion` and `editLeaseToken` mirroring `CaseMutationRequest` | Step 6 |
| Plan 03 § 3 *Correlation & client version* (`:168`) | `X-Correlation-Id` and `X-Pegasus-Client-Version` as one shared list | Step 7 |
| Plan 04 § 3 item 5 | The compatibility payload is exactly `minimumVersion`, `currentVersion`, `channel`, `maintenanceMessage`, `validForSeconds` | Step 8 |
| Plan 02 § 4 target-state table | `src/Pegasus.Contracts` — `net10.0`, System.Text.Json only, no EF/ASP.NET/WinUI types | Steps 2, 11 |
| L-01 (locked) | The gateway is `Pegasus.Web` evolved in place, so Contracts is referenced by `Pegasus.Web`, never by a new host | Step 10 registers the project only; no host is added |
| `AGENTS.md` § Simplicity rails | One list per concept | Steps 4, 6, 7 and the sole-ownership statement in § Risks |
| `AGENTS.md` § Product invariants (`:235`) | A new top-level project requires an accepted ADR | The New-ADR paragraph above; the dependency on [[FND-026]] |
| `docs/engineering.md` § Required evidence tiers (`:72`), tier 2 | Positive, contradictory, ambiguous and failure cases — not a compiling project | Step 12 and § Verification |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`,
  `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) → `microsoft-code-reference` (Microsoft
  Learn plugin).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_code_sample_search` for RFC 9457
  `ProblemDetails` members and `JsonStringEnumConverter`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move;
  a move crosses at most one gated boundary. `get_doc_gates FND-029` owes `research`, `files`, `plan`,
  `checklist` and `questions-resolved` at `leave-preparing`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's thirteen steps: same order, same ownership, same paths.

1. **Orient.** Read `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:120-200` and
   `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:76-89`, then `get_doc_gates FND-029` and
   `take_ticket` on branch `task/pegasus-contracts` from `origin/dev`.
2. **Create the csproj.** `src/Pegasus.Contracts/Pegasus.Contracts.csproj`, copying the 14-line shape
   of `src/Pegasus.Core/Pegasus.Core.csproj`: `Microsoft.NET.Sdk`, `<TargetFramework>net10.0</TargetFramework>`,
   `<RuntimeIdentifiers>linux-x64;win-x64</RuntimeIdentifiers>`, `ImplicitUsings` and `Nullable`
   enabled. No `PackageReference`, no `ProjectReference`, no `FrameworkReference`. `System.Text.Json`
   needs none — Core proves it: zero packages and six files importing it
   (`src/Pegasus.Core/Custody/CustodyContracts.cs:3` and five others).
3. **`Paging/PagedResult.cs`** — `public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, bool HasPreviousPage, bool HasNextPage)`,
   those five members in that order and no other. Add a code comment above the declaration citing
   `src/Pegasus.Core/Cases/CaseQueries.cs:69-74` and
   `src/Pegasus.Infrastructure/Persistence/EfCaseQueryStore.cs:115-133` so the next reader sees *why*
   there is no total without re-deriving it. Then `Paging/PagingLimits.cs` with
   `public const int MaxPageSize = 200;` (area 03 § 3 `:169`).
4. **`ProblemDetails/PegasusProblemTypes.cs`** — `public const string Prefix = "urn:pegasus:problem:";`
   plus one `const string` per slug, exactly these thirteen and no other, transcribed from
   `docs/desktop/03-gateway-api-and-data/README.md:167`: `validation`, `not-authorized`,
   `version-conflict`, `lease-conflict`, `lease-expired`, `operation-conflict`, `client-unsupported`,
   `password-change-required`, `account-disabled`, `provider-unavailable`, `not-found`,
   `rate-limited`, `maintenance`.
5. **`ProblemDetails/PegasusProblem.cs`** — RFC 9457 members `Type`, `Title`, `Status`, `Detail`,
   `Instance`, plus `CorrelationId` (always present) and an optional `Extensions` dictionary, with
   typed accessors for `CurrentVersion` and `MinimumVersion` because `version-conflict` and
   `client-unsupported` carry them. Confirm the RFC 9457 member names with
   `microsoft_code_sample_search` before writing. It carries no payload dump and no exception text —
   `AutomationMcpErrors.cs:9-15` states the boundary rule this type inherits.
6. **`Requests/MutationEnvelope.cs`** — mirror `CaseMutationRequest`
   (`CaseWorkflowContracts.cs:182-188`) minus `ActionActor` and minus `CaseId` (the id is in the
   route): `long ExpectedVersion`, `string OperationKey`, `string Reason`, `string EditLeaseToken`.
   Add `OperationKeys.MaxLength = 100` and the `desk:` prefix constant, with a comment citing
   `AutomationMcpErrors.cs:76-89` as the existing `mcp:` precedent. **Do not** re-implement the
   validation; the gateway owns it.
7. **`PegasusHeaders.cs`** — `ClientVersion = "X-Pegasus-Client-Version"`,
   `CorrelationId = "X-Correlation-Id"`, so [[FND-031]] (plan handle `DSK-02-06`) step 4 and area
   03's middleware read one list.
8. **`Responses/ClientCompatibilityResponse.cs`** — exactly the five fields of plan 04 § 3 item 5:
   `string MinimumVersion`, `string CurrentVersion`, `string Channel`, `string? MaintenanceMessage`,
   `int ValidForSeconds`. `Channel` is a `string`, not an enum — the plan specifies it that way.
9. **`PegasusJson.cs`** — one `public static JsonSerializerOptions Options` with
   `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` and
   `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`, exposed as the single shared
   options instance. Apply `[JsonConverter(typeof(JsonStringEnumConverter))]` to every enum this
   project declares — measured today that is **none** (see research A-FND029-2), so the step is a
   standing rule for whichever enum arrives, not a no-op to be dropped from the code review.
10. **Register the project.** Add `<Project Path="src/Pegasus.Contracts/Pegasus.Contracts.csproj" />`
    to the `/src/` folder in `Pegasus.slnx`; add the same path to the server entry point created by
    [[FND-028]] (plan handle `DSK-02-03`) — Contracts is `net10.0` and must build on Linux; and insert
    it into the ordinal expected array in
    `DependencyDirectionTests.ApplicationSolutionExcludesSourceWorkspaces` at `:137-149`, where it
    sorts between `src/Pegasus.Core/…` and `src/Pegasus.Infrastructure/…`. If [[FND-028]] has not
    landed, record that in this document rather than skipping the entry silently.
11. **`ContractsProjectHasNoDependencies`** — a new fact in `DependencyDirectionTests.cs` that loads
    `src/Pegasus.Contracts/Pegasus.Contracts.csproj` with `XDocument` through `FindRepositoryRoot()`
    (`:509`) and asserts no `PackageReference`, no `ProjectReference` and no `FrameworkReference`
    element exists. Reuse `ProjectReferences(root, path)` — **measured at `:493`, not the `:497` the
    ticket body cites** — for the project-reference half, and follow the element-walking shape of
    `ForbiddenDirectDependencies` (`:480-491`) for the other two.
12. **`tests/Pegasus.ArchitectureTests/ContractSerializationTests.cs`** — add the fifth
    `ProjectReference` to `Pegasus.ArchitectureTests.csproj` first, then write the tier-2 case set,
    all through `PegasusJson.Options`: `PagedResult<string>`, `PegasusProblem` and
    `ClientCompatibilityResponse` round-trip with camelCase names; `PagedResult<string>` serialises
    **exactly** the five members of step 3 with no total-count property of any name; a null
    `MaintenanceMessage` is omitted on write and tolerated as absent on read; an unknown enum value
    behaves as A-FND029-1 requires (a placeholder assertion today, since no enum exists — write it
    against a test-local enum so the rule is proven rather than assumed); and `PegasusProblem` with
    no `Extensions` round-trips without emitting an empty object. Record in this document that these
    facts move to `tests/Pegasus.Api.ContractTests` when [[TEST-001]] (plan handle `DSK-08-01`) lands.
13. **Build, restore, test, close.** `dotnet restore ./src/Pegasus.Contracts/Pegasus.Contracts.csproj --force-evaluate`
    and commit the generated `packages.lock.json` (expect the 8-line, three-empty-entry shape of
    `src/Pegasus.Core/packages.lock.json`); then `dotnet restore ./Pegasus.slnx --locked-mode`,
    `dotnet build ./Pegasus.slnx --configuration Release --no-restore` and
    `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build`.
    Add the `src/Pegasus.Contracts` row to `docs/current-architecture.md` § Components and dependency
    direction (`:55`). Run the simplification pass, record it under a dated heading below, and open
    the PR into `dev`.

## Verification

Evidence tier **2 — Core/domain** (`docs/engineering.md` § Required evidence tiers, `:72`), as the
ticket body states: positive **and** failure serialization cases for the envelope types — null
handling, unknown enum value, absent optional fields — not merely a compiling project.

The `proof` document is produced from these command logs:

1. `dotnet build ./Pegasus.slnx --configuration Release` — expected exit 0, **zero warnings**
   (`Directory.Build.props` sets `TreatWarningsAsErrors=true`, so a warning is already a failure;
   paste the summary line showing `0 Warning(s)`).
2. `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --filter "FullyQualifiedName~Contract"`
   — expected: the round-trip and `ContractsProjectHasNoDependencies` facts pass. Then re-run
   unfiltered so the extended `ApplicationSolutionExcludesSourceWorkspaces` is visibly green too.
3. `grep -rn 'PackageReference\|ProjectReference' src/Pegasus.Contracts/Pegasus.Contracts.csproj` —
   expected: no matches.
4. `grep -n 'record PagedResult' src/Pegasus.Contracts/Paging/PagedResult.cs` — expected: one line,
   `public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, bool HasPreviousPage, bool HasNextPage)`;
   and `grep -rn 'Total' src/Pegasus.Contracts/Paging/` — expected: no matches.
5. Additionally, and not in the body: `grep -rn 'ActionActor' src/Pegasus.Contracts/` — expected: no
   matches. The acceptance criterion "`ActionActor` does not appear there" deserves an executable
   check, not an eyeball.
6. `cat src/Pegasus.Contracts/packages.lock.json` — expected: the same three empty TFM/RID entries as
   `src/Pegasus.Core/packages.lock.json`. Any dependency listed means a package slipped in.

## Risks / open questions

- **Risk — the two `PagedResult<T>` statements drift.** Step 3 here and [[GWY-001]] step 5 state the
  same five members. *Mitigation*: step 12's serialised-property-set assertion fails mechanically if
  either side adds, renames or removes a member, so the guard is a test rather than a convention.
  Reintroducing a total-count member no Core port can populate is a stop condition on both tickets.
- **Risk — a second copy of an envelope type appears under an alternative name or folder.** This
  ticket is the **single owner** of `Paging/PagedResult.cs`, `ProblemDetails/PegasusProblemTypes.cs`,
  `ProblemDetails/PegasusProblem.cs`, `Requests/MutationEnvelope.cs` and the
  `ContractsProjectHasNoDependencies` fact. [[GWY-001]] is the consumer: it extends those types in
  place, changes no existing member, and never redefines them under `Problems/`, `Commands/`,
  `ProblemTypes` or `CommandEnvelope`, and adds no second no-dependency matcher.
- **Risk — the lock file is forgotten.** `dotnet restore ./Pegasus.slnx --locked-mode` runs in the CI
  composite action on every lane (`.github/actions/dotnet-build/action.yml:22`), whose cache key
  already globs `src/**/packages.lock.json`, so an uncommitted Contracts lock file breaks the whole
  workflow rather than one job. *Mitigation*: step 13 generates and commits it before the solution
  restore.
- **Risk — a path assertion that passes on Windows and fails on Linux.** `ProjectReferences` at
  `:502` normalises `\` to `/` with the comment that "MSBuild Include paths use backslashes, which
  are not path separators on Linux". Any new path handling in step 11 must do the same.
- **Scope boundary, not an open question — where the serialization facts live.**
  `tests/Pegasus.Api.ContractTests` does not exist; [[TEST-001]] creates it and the facts move then.
  Recorded here per the ticket body's step 12.
- **Scope boundary, not an open question — the OpenAPI snapshot.** There is no `openapi/` directory
  today, so no snapshot ripple is claimable. [[GWY-004]] (plan handle `DSK-03-04`) creates
  `openapi/pegasus-v1.json` and [[GWY-005]] (plan handle `DSK-03-05`) generates the client; from then
  on every member change here ripples into both.
- **Scope boundary, not an open question — `OperatorLabels`.** [[GWY-016]] (plan handle `DSK-03-16`)
  and [[FEAT-023]] (plan handle `DSK-05-23`) own its relocation to this project. Do not pre-empt it.
- **Body imprecision worth recording, not a contradiction.** The body describes "the same five-member
  cursor shape in the other three paged ports"; measured, the *pattern* is identical in all four but
  the member names differ per port and `ListAutomationActivityResult`
  (`src/Pegasus.Core/Identity/AutomationActivity.cs:44-50`) has a sixth member, `string? CorrelationId`.
  The body's load-bearing claim — that none of them counts — is confirmed. The body's `:497` citation
  for `ProjectReferences` is `:493` as measured. Neither changes any instruction.
- **No open question is opened on this ticket.** Everything unknown is settled by a command inside
  the ticket's own steps; nothing requires an answer from outside before implementation may begin.



## Execution result — 2026-08-25

Implementation is complete within the planned scope. The solution entry point Pegasus.Server.slnf does not exist on the live origin/dev head; FND-028 has not landed, so its planned Contracts registration is explicitly deferred to that ticket. No duplicate server filter was created.

Evidence and validation:

- dotnet restore ./src/Pegasus.Contracts/Pegasus.Contracts.csproj --force-evaluate -p:RestorePackagesWithLockFile=true — exit 0; generated src/Pegasus.Contracts/packages.lock.json is 124 bytes with the same three empty TFM/RID entries as Pegasus.Core.
- dotnet restore ./Pegasus.slnx --locked-mode — exit 0.
- dotnet build ./Pegasus.slnx --configuration Release --no-restore — exit 0, 0 warnings, 0 errors.
- dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build --filter FullyQualifiedName~Contract — 19 passed, 0 failed.
- dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build — 104 passed, 0 failed.
- pwsh ./scripts/Test-DocumentationLinks.ps1 — exit 0; 232 files checked.
- pwsh ./scripts/Test-TestMarkdownPlacement.ps1 — exit 0.
- Static checks: the Contracts project has no PackageReference, ProjectReference or FrameworkReference; ActionActor and paging Total are absent; Pegasus.slnx includes Contracts; no server filter exists to update.

The first full-solution build attempt overlapped another build invocation and failed on an own WorkerExtensions.csproj file lock. Only the identified MSBuild nodes from those overlapping attempts were stopped; the serial canonical build above passed. This is an execution-environment event, not a source failure.

## Simplification pass

### 2026-08-25

- Reuse: copied the existing dependency-free Pegasus.Core project shape and reused the repository's XDocument solution/dependency test helpers; no new framework or validation abstraction was introduced.
- Simplification: each envelope, problem slug list, paging cap, header list and JSON-options owner has one canonical location. The gateway validator remains outside Contracts.
- Efficiency: the five-member paging envelope preserves the existing fetch-one-extra query shape and avoids introducing an unpopulated COUNT contract; the Contracts project adds no package restore cost.
- Altitude: the documentation change is one current-architecture row, and the temporary serialization facts stay in the existing architecture test project until TEST-001 creates the dedicated contract-test project.
- Disposition: no behaviour-preserving simplification finding remained unapplied. The missing Pegasus.Server.slnf integration is an FND-028 dependency, not an omission to compensate for here.

## Review correction — 2026-08-25

The independent reviewer identified that the first `PegasusProblem` shape nested RFC 9457 extension members under `extensions`; because the typed accessors were ignored properties, top-level `currentVersion` and `minimumVersion` could not round-trip. The implementation now uses `PegasusProblemJsonConverter` to read and write arbitrary extension members at the problem document's top level, while retaining typed accessors and omitting null standard members under the shared JSON options. Added focused serialization tests for top-level write and read. The reviewer also found the branch had no commit; commit is required before the fresh review.

Corrective validation:

- `dotnet build ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore` — exit 0, 0 warnings, 0 errors.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~ContractSerialization"` — 6 passed, 0 failed.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — 106 passed, 0 failed.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — exit 0, 0 warnings, 0 errors.
- Documentation links and Markdown placement scripts — both passed (232 links checked).

The reviewer’s read-only full rerun was constrained by unrelated temporary-directory ACL failures; the serial local full test and canonical build are green.

## FND-028 synchronization and server-filter completion — 2026-08-26

The previously deferred server-filter registration is now executable because current \`origin/dev\` contains \`Pegasus.Server.slnf\` from FND-028. The task branch merged \`origin/dev\` as \`17d49224\`. Inspection showed that the filter and its architecture expectation both omitted \`src/Pegasus.Contracts/Pegasus.Contracts.csproj\`; no duplicate filter was created. Added that single project entry to \`Pegasus.Server.slnf\` and to \`ServerSolutionFilterContainsExactlyTheServerProjects\`, then committed/pushed \`0a3d23be\`.

Validation on exact head \`0a3d23becc5a1038ab166effafd5203847bc3b5c5\`:

- \`dotnet restore ./Pegasus.Server.slnf --locked-mode\` — passed.
- \`dotnet build ./Pegasus.Server.slnf --configuration Release --no-restore --nologo\` — passed, 0 warnings, 0 errors.
- \`dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build --logger 'console;verbosity=minimal'\` — 110 passed, 0 failed, 0 skipped.
- \`dotnet build ./Pegasus.slnx --configuration Release --no-restore -p:UseSharedCompilation=false --nologo\` — passed, 0 warnings, 0 errors.
- \`git diff --check\` — passed; the FND-029 worktree is clean.
- PR #26 is open against \`dev\`; exact-head CI and fresh independent review are pending.

The change remains limited to the FND-029 Contracts project integration and its architecture expectation. No cloud, upstream, credential, Worker, API, or desktop implementation change was made.

## Current-head simplification pass — 2026-08-26

The synchronized branch diff includes the required `Pegasus.Server.slnf` registration and the matching `DependencyDirectionTests` expectation added in commit `0a3d23becc5a1038ab166effafd5203847bc3b5c`. Independent review rechecked the current head. **Reuse:** the server filter remains the existing FND-028 entry point and the architecture test remains the existing exact-list assertion; no new abstraction or duplicate project-registration mechanism was introduced. **Simplification:** the fix is one filter entry plus one expected-list entry; no broader solution or project-file redesign was made. **Efficiency:** the existing filter keeps the Linux server build focused; the Contracts project remains dependency-free and uses no new package. **Altitude:** no unrelated source, endpoint, or host changes were added. **Disposition:** no behavior-preserving simplification change is warranted; the evidence is now current for the complete branch diff.

## Final independent review and CI — 2026-08-26

The independent reviewer re-reviewed the corrected evidence and returned **PASS — merge to `dev` is allowed**. The current-head simplification pass explicitly covers both synchronized additions (`Pegasus.Server.slnf` and the matching architecture expectation). The recorded commit `0a3d23becc5a1038ab166effafd5203847bc3b5c` is valid and matches `git rev-parse`. PR #26 is exact-head, `MERGEABLE`, `CLEAN`; CI run `33014659206` completed successfully for changes, documentation, local-development-scripts, reference-data, unit, browser, SQL shards 1–3, and coverage (infrastructure correctly skipped). Local server-filter restore, Release build (0 warnings/0 errors), architecture tests (110/110), and full solution build passed. Historical pending wording earlier in this document is superseded by this final result. Merge to `dev` is authorized by the repository workflow; proof remains deferred until the merged commit reaches `main`.

## Merge-to-dev boundary — 2026-08-26

PR #26 merged to remote `dev` at `b5a3a6e87388db20d4c38226b4a5297e8f400145`; `git ls-remote origin` confirms `dev` at that SHA. Remote `main` remains `3b1737de2a27f84aa1bea03bf2c34d41d5a8006a`, so this ticket is not yet verifiable on `main`. Kanmer is therefore honestly in `verifying`, not `done`; no `proof.md` has been written. Next action is an exact-SHA `dev`→`main` promotion under the repository’s required fresh literal authorization, followed by verification on `main` and proof/closeout.
