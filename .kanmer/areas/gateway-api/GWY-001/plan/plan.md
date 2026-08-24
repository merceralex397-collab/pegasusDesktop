# Plan — GWY-001: `Pegasus.Contracts` DTO conventions, problem types, paging envelope, version/operation key

**Diff estimate: ~5 files, ~90 lines.**

`docs/engineering.md` § Plan sizing requires the estimate first. Derived from the files document,
file by file, for the expected branch (A-GWY001-1: [[FND-029]] (plan handle `DSK-02-04`) has landed,
so steps 3–6 are *check-then-extend*):

| File | Change | Lines |
| --- | --- | --- |
| `src/Pegasus.Contracts/ContractConventions.cs` | new — the 6-line shape of `src/Pegasus.Core/CoreAssembly.cs` plus ~40 lines of XML documentation, one block per convention clause | ~46 |
| `src/Pegasus.Contracts/Requests/MutationEnvelope.cs` | XML documentation of the `desk:<guid>` format, the ≤ 100 cap, and the one 200-character exception | ~+9 |
| `src/Pegasus.Contracts/ProblemDetails/PegasusProblem.cs` | XML documentation of the no-payload-dump boundary rule | ~+6 |
| `src/Pegasus.Contracts/Paging/PagingLimits.cs` | XML documentation that a per-endpoint cap may be below `MaxPageSize` | ~+5 |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` | an 8-entry Contracts prefix array (~11), a matcher in the shape of `IsForbiddenCoreDependency` (~4), and the assembly-reference assertion added to the existing `ContractsProjectHasNoDependencies` fact (~9) | ~+24 |
| `src/Pegasus.Contracts/Paging/PagedResult.cs`, `.../ProblemDetails/PegasusProblemTypes.cs` | read and confirmed unchanged | 0 |

**Fallback branch, priced separately.** If [[FND-029]] has landed only in part, each missing envelope
file is created here to the exact shape [[FND-029]] pins — `PagedResult.cs` ~14, `PagingLimits.cs`
~12, `PegasusProblemTypes.cs` ~30, `PegasusProblem.cs` ~48, `MutationEnvelope.cs` ~28 — taking the
worst case to **~9 files, ~220 lines**. Step 2 decides which branch applies and step 11 records it. If
`src/Pegasus.Contracts/Pegasus.Contracts.csproj` is absent altogether, the ticket stops: creating a
second Contracts project is the failure both tickets' Guardrails forbid.

## Approach

Treat this as a **documentation-and-enforcement** ticket over a project someone else owns, not as an
authoring ticket. [[FND-029]] declares every envelope type; GWY-001 adds the two things that project
still lacks — a stated convention set the next author can read without re-deriving it, and a
mechanical guard that the assembly stays dependency-free at *runtime* and not merely in its csproj.
The rejected alternative was to author the envelope types here and let [[FND-029]] converge on them:
it reads more natural from this ticket's title, but the two tickets would then race to declare the
same four files, and the "one type, one owner" Guardrail plus [[FND-029]]'s own Risks section
(naming GWY-001 as "the consumer") settle it the other way. The cost of the chosen approach is that
this ticket is blocked on a foundation ticket landing first; the benefit is that no member of the
wire contract is ever declared twice.

Two choices inside that shape are load-bearing and argued from measurement:

- **`ContractConventions` is a marker type, not a comment block.** `CoreAssembly`
  (`src/Pegasus.Core/CoreAssembly.cs`, six lines) exists purely so `DependencyDirectionTests.cs:45`
  can write `typeof(CoreAssembly).Assembly.GetReferencedAssemblies()`. Step 8 needs the same anchor
  in Contracts, so the conventions ride on a type that has to exist anyway. Putting them in a `.md`
  file would additionally fail the CI `documentation` lane (`.github/workflows/ci.yml:70-87`).
- **The Contracts assertion needs its own prefix array inside the one existing fact.** Step 8 says
  "reuse the existing `IsForbiddenCoreDependency` helper shape" and "never add a second matcher".
  Measured, `ForbiddenCoreDependencyPrefixes` (`DependencyDirectionTests.cs:23-39`) lacks
  `Microsoft.WindowsAppSDK`, `Microsoft.UI.Xaml` and `Pegasus.Core`, and adding `Pegasus.Core` to
  *that* array would break `CoreHasNoInfrastructureOrHostDependencies` at `:42-48`, because the
  prefix would match Core's own assembly name. One fact, two assertions, two arrays, one matcher
  shape — that is the reading that satisfies both instructions.

## Governing docs

The ticket's `refs` array is empty and `get_doc_gates GWY-001` reports `docs_todo: true`, so there is
no linked PRD/FRD/ADR to meet today.

> **New ADR** — ADR-0101 (local-execution / cloud-authority split and the six-question
> cloud-justification test) and ADR-0103 (gateway, never direct database access from workstations),
> both authored by [[FND-005]] (plan handle `DSK-00-05`); ADR-0100 (native WinUI 3 client in the
> fork, which authorises the new top-level projects) is claimed by both [[FND-005]] and [[FND-026]]
> (plan handle `DSK-02-01`) — see [[FND-026]]'s plan for the ownership reconciliation.
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table, ADR-0100/0101/0103 rows);
> if any of them lands differently this plan is revised before implementation. This ticket **cites**
> those ADRs and never edits them — the ticket body's *Documentation changes* section says so
> explicitly.

Because `refs` is empty, these are the authorities that actually bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 10.3 | The OpenAPI document is the contract; no hand-written duplicate DTOs | Steps 3–6 extend one declaration per concept instead of adding a second; step 7 states the "Core records are never exposed directly" rule that keeps the duplication from reappearing |
| Proposal § 10.4 | An explicit concurrency token on every mutable aggregate | Step 6 |
| Plan 03 § 3 *Contracts* (`README.md:163`) | `src/Pegasus.Contracts` holds request/response/problem DTOs with no EF, ASP.NET or WinUI references; Core records are not exposed directly | Steps 7, 8 |
| Plan 03 § 3 *Problem details* (`:167`) | Stable `urn:pegasus:problem:<slug>` URIs, exactly thirteen slugs, `correlationId` always present, no payload dumps | Steps 3, 4 |
| Plan 03 § 3 *Paging/filter/sort* (`:169`) | `pageSize ≤ 200`; "totals returned only where the existing query port already counts" | Step 5, and the `grep -rn 'Total'` gate in § Verification |
| Plan 03 § 3 *Idempotency* (`:165`) | Caller-supplied `OperationKey` as a body field; desktop keys `desk:<guid>`, ≤ 100 characters | Step 6 |
| Plan 03 § 3 *Concurrency* (`:166`) | Explicit body fields `expectedVersion` and `editLeaseToken` mirroring `CaseMutationRequest`, never headers | Step 6 |
| Plan 03 § 7 *Traps* (`README.md:263-269`) | Never lower `TreatWarningsAsErrors`; `Pegasus.Contracts` stays `net10.0` and platform-neutral because `Pegasus.Web` still publishes `linux-x64` | Steps 8, 9 |
| Plan 03 § 5 row `DSK-03-01` | "Project builds; architecture test forbids EF/ASP.NET/WinUI references; problem-type catalogue documented in the project" | Steps 7–10 |
| `AGENTS.md` § Simplicity rails | One list per concept | Steps 3–6 (check-then-extend), step 8 (one fact), and the sole-ownership statement in § Risks |
| `AGENTS.md` § Repository task workflow step 4 | A simplification pass over the branch diff before the PR | Step 11 and the dated heading below |
| L-01 (locked, `docs/desktop/README.md` § Locked decisions) | The gateway is `Pegasus.Web` evolved in place; Contracts is a shared class library, not a deployment unit | No host is added; step 8 forbids a `Pegasus.Web` reference from Contracts |
| L-04 (locked) | Every ticket names its subagent, skills and MCP tools | § Routing below |
| `docs/engineering.md` § Required evidence tiers, tier 1 | Static/build/architecture: compiling the approved projects and enforcing dependency direction; no behaviour may be claimed | § Verification |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`,
  plugin `dotnet-aspnetcore`, `plugins/dotnet-aspnetcore/skills/dotnet-webapi/SKILL.md`) →
  `microsoft-code-reference` (Microsoft Learn plugin).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move;
  a move crosses at most one gated boundary. `get_doc_gates GWY-001` owes `research`, `files`, `plan`,
  `checklist` and `questions-resolved` at `leave-preparing`, and reports `governing-doc` already
  satisfied at `leave-backlog` by `docs_todo: true`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's eleven steps: same order, same ownership, same paths.

1. **Orient.** Read `docs/desktop/03-gateway-api-and-data/README.md` § 3 (`:163-169`) and § 7, then
   `endpoint-map.md` § Conventions. Read [[FND-029]]'s `plan` document with Kanmer
   `get_ticket_doc FND-029 plan` — it is the pinned shape of everything steps 3–6 touch. Load
   `.agents/skills/project/pegasus-desktop/SKILL.md`. Call `get_doc_gates GWY-001`, then
   `take_ticket` with branch `task/contracts-conventions` and worktree
   `../pegasus-worktrees/contracts-conventions` created from `origin/dev`
   (`AGENTS.md` § Repository task workflow steps 1–2).
2. **Confirm the project, and branch the plan.** `ls src/Pegasus.Contracts/Pegasus.Contracts.csproj`;
   confirm `<TargetFramework>net10.0</TargetFramework>` and that
   `<Project Path="src/Pegasus.Contracts/Pegasus.Contracts.csproj" />` is in `Pegasus.slnx`. Then
   `ls src/Pegasus.Contracts/Paging/PagedResult.cs src/Pegasus.Contracts/Paging/PagingLimits.cs src/Pegasus.Contracts/ProblemDetails/PegasusProblemTypes.cs src/Pegasus.Contracts/ProblemDetails/PegasusProblem.cs src/Pegasus.Contracts/Requests/MutationEnvelope.cs`
   and record in this document which of the two branches applies. If the csproj is absent, **stop**
   and record the blocker; do not create a second project.
3. **`ProblemDetails/PegasusProblemTypes.cs`.** If present, read it and change no existing member;
   verify `public const string Prefix = "urn:pegasus:problem:";` and exactly the thirteen slugs
   transcribed from `docs/desktop/03-gateway-api-and-data/README.md:167` — `validation`,
   `not-authorized`, `version-conflict`, `lease-conflict`, `lease-expired`, `operation-conflict`,
   `client-unsupported`, `password-change-required`, `account-disabled`, `provider-unavailable`,
   `not-found`, `rate-limited`, `maintenance`. If absent, create it to that exact shape. Add nothing
   beyond the thirteen: a fourteenth slug is an area-03 plan change first. One literal path, one type
   name — never `Problems/ProblemTypes.cs`, never `ProblemTypes`.
4. **`ProblemDetails/PegasusProblem.cs`.** If present, read it and change no existing member; if
   absent, create it with the RFC 9457 members `Type`, `Title`, `Status`, `Detail`, `Instance`, plus
   `CorrelationId` (always populated) and an optional `Extensions` dictionary, with `CurrentVersion`
   and `MinimumVersion` as typed extension accessors. Confirm the RFC 9457 member names with
   `microsoft_code_sample_search` before writing rather than from memory. Either way, add XML
   documentation stating that the body never carries payload dumps or infrastructure detail, quoting
   the boundary rule `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:7-15` already enforces for MCP, and
   naming *why* `CurrentVersion` is typed: `CaseVersionConflictException`
   (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:125-133`) exposes `ActualVersion`, and both
   lease exceptions (`:135`, `:143`) expose `CaseVersion`. Never `Problems/PegasusProblem.cs`.
5. **`Paging/PagedResult.cs` and `Paging/PagingLimits.cs`.** If present, read and change no existing
   member; verify by `grep` that the record is exactly
   `public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, bool HasPreviousPage, bool HasNextPage)`
   — five members, that order, no total-count member of any name — and that `PagingLimits` declares
   `public const int MaxPageSize = 200;`. If absent, create both to that shape. Then add the XML-doc
   note step 5 requires on `PagingLimits`: a per-endpoint cap may be **lower** than `MaxPageSize`, and
   `ListIntake` (`src/Pegasus.Core/Intake/IntakeQueryUseCases.cs:22-27`) refuses a page size above
   100. Do not restate the no-total rationale in a second place — [[FND-029]] step 3 already puts a
   comment above the declaration citing `src/Pegasus.Core/Cases/CaseQueries.cs:69-74` and
   `src/Pegasus.Infrastructure/Persistence/EfCaseQueryStore.cs:115-133`; if that comment is missing,
   add it there rather than duplicating it. Never declare a second paging record.
6. **`Requests/MutationEnvelope.cs`.** If present, read and change no existing member; if absent,
   create it mirroring `CaseMutationRequest`
   (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:182-188`) minus the server-only
   `ActionActor` and minus `CaseId` (the route carries it): `long ExpectedVersion`,
   `string OperationKey`, `string Reason`, `string EditLeaseToken`, with
   `OperationKeys.MaxLength = 100` and the `desk:` prefix as constants. Either way, confirm those
   members are **body fields, never headers**, and add XML documentation for the desktop key format
   `desk:<guid>` under the same constraints `RequireOperationKey` enforces
   (`src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:76-89`: trimmed, prefixed, `Length is <= 4 or > 100`,
   no whitespace, no control characters), noting that 200 characters are legal only where Core allows
   — `UnidentifiedValidation.MaximumOperationKeyLength` at
   `src/Pegasus.Core/Intake/Unidentified/UnidentifiedContracts.cs:398`. Do **not** re-implement the
   validation here; the gateway owns it. Never `Commands/CommandEnvelope.cs`, never `CommandEnvelope`.
7. **`ContractConventions.cs`.** Create `src/Pegasus.Contracts/ContractConventions.cs` in the shape of
   `src/Pegasus.Core/CoreAssembly.cs`: namespace, XML summary, `public static class ContractConventions;`.
   The summary carries the four conventions from the body — DTO suffixes `Request`/`Response`; no Core
   record is exposed directly, because they carry `ActionActor` and server-only members; enum values
   serialise as strings; dates are `DateTimeOffset` in UTC — plus one sentence saying the type also
   serves as the stable assembly marker step 8 asserts against, so nobody deletes it as unused. **Not**
   a new `.md` file: a Markdown file outside `docs/(prd|frd|adr|design|desktop)` fails the CI
   `documentation` job (`.github/workflows/ci.yml:70-87`).
8. **Extend `ContractsProjectHasNoDependencies`.** In
   `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`, leave the fact's existing csproj-XML
   assertions untouched (no `PackageReference`, no `ProjectReference`, no `FrameworkReference`) and add
   to that **same fact** an assembly-reference assertion:
   `typeof(ContractConventions).Assembly.GetReferencedAssemblies()` contains none of
   `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`, `Microsoft.WindowsAppSDK`,
   `Microsoft.UI.Xaml`, `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`.
   Use a Contracts-specific prefix array through a matcher in the shape of `IsForbiddenCoreDependency`
   (`:475-478`) — exact-name equality or a `"{prefix}."` ordinal start. Do **not** widen
   `ForbiddenCoreDependencyPrefixes` (`:23-39`): adding `Pegasus.Core` there breaks
   `CoreHasNoInfrastructureOrHostDependencies` at `:42-48`. Use `Assert.DoesNotContain`, never
   `Assert.Empty` — the compiler always keeps framework references (A-GWY001-3). Before adding
   `Microsoft.UI.Xaml`, check whether [[FND-037]] (plan handle `DSK-02-12`) has already added it to a
   shared list. Confirm `tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj` references
   `Pegasus.Contracts` ([[FND-029]] step 12); if it does not, add the reference and record it. Never a
   second matcher, and never the name `ContractsHasNoInfrastructureOrHostDependencies`.
9. **Build.** `dotnet build Pegasus.slnx -c Release`. Done means `0 Warning(s)`: `TreatWarningsAsErrors=true`
   with `AnalysisLevel=latest-recommended` (`Directory.Build.props:7-8`) makes any analyzer warning a
   build break, XML documentation included, and lowering the policy is not an option
   (`docs/desktop/03-gateway-api-and-data/README.md` § 7).
10. **Test.** `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj -c Release`
    and confirm the extended `ContractsProjectHasNoDependencies` passes and no existing fact regressed —
    in particular `CoreHasNoInfrastructureOrHostDependencies` and
    `ApplicationSolutionExcludesSourceWorkspaces`. Then run the three `grep`/`ls` gates in § Verification.
11. **Record, simplify, close.** Record in this document which branch step 2 found and, per steps 3–6,
    which case applied to each envelope file. Run the simplification pass over the branch diff
    (`AGENTS.md` step 4) and record findings and dispositions under the dated `## Simplification pass`
    heading below. Open the PR into `dev`.

## Verification

Evidence tier **1 — Static/build/architecture** (`docs/engineering.md` § Required evidence tiers), as
the ticket body states: this obliges compiling the approved projects and enforcing dependency
direction. It proves consistency only — **no endpoint behaviour may be claimed from this ticket**.

The `proof` document is produced from these command logs, in this order:

1. `dotnet build Pegasus.slnx -c Release` — expected: `Build succeeded`, `0 Warning(s)`,
   `0 Error(s)`. Paste the summary line; a single warning is already a failure.
2. `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj -c Release` —
   expected: every fact passes, including the extended `ContractsProjectHasNoDependencies`. Run it
   unfiltered so the unchanged Core and solution-contents facts are visibly green too.
3. `ls src/Pegasus.Contracts/Problems src/Pegasus.Contracts/Commands` — expected: **both absent**. The
   envelope types live under `ProblemDetails/`, `Paging/` and `Requests/` only.
4. `grep -n 'record PagedResult' src/Pegasus.Contracts/Paging/PagedResult.cs` — expected: exactly one
   line, `public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, bool HasPreviousPage, bool HasNextPage)`.
5. `grep -rn 'Total' src/Pegasus.Contracts/Paging/` — expected: **no matches**.
6. Additionally, and not in the body: `grep -rn 'ActionActor' src/Pegasus.Contracts/` — expected: no
   matches. The rule "Core records are not exposed directly" deserves an executable check, and
   `ActionActor` is the member that would carry the violation across.
7. Additionally: `grep -rn 'ContractsHasNoInfrastructureOrHostDependencies\|ProblemTypes\b\|CommandEnvelope' tests/ src/`
   — expected: no matches. The three forbidden names are cheaper to grep than to review.

## Risks / open questions

- **Risk — this ticket starts before [[FND-029]] lands.** Steps 3–6 then run on the create branch and
  the diff roughly doubles (priced above). *Mitigation*: step 2 is a hard branch point with an
  explicit stop condition for the missing-csproj case, and step 11 records which case applied. The
  failure to avoid is creating a second Contracts project or a second copy of an envelope type.
- **Risk — the two `PagedResult<T>` statements drift.** [[FND-029]] step 3 and this ticket's step 5
  state the same five members word for word. *Mitigation*: [[FND-029]] step 12's
  serialised-property-set assertion fails mechanically if either side adds, renames or removes a
  member. Reintroducing a total-count member no Core port can populate is a stop condition on both
  tickets, not a review comment.
- **Risk — widening the shared prefix array breaks the Core fact.** Adding `Pegasus.Core` to
  `ForbiddenCoreDependencyPrefixes` (`:23-39`) makes `IsForbiddenCoreDependency` match Core's own
  assembly name and fails `CoreHasNoInfrastructureOrHostDependencies` (`:42-48`). *Mitigation*: step 8
  uses a Contracts-specific array through the same matcher shape, inside the one existing fact.
- **Risk — `Assert.Empty` on `GetReferencedAssemblies()`.** The compiler always keeps framework
  references, so an emptiness assertion fails and invites someone to weaken the test. *Mitigation*:
  A-GWY001-3 in the research names the trap and step 8 pins `Assert.DoesNotContain`.
- **Risk — merge conflict with [[FND-037]] (plan handle `DSK-02-12`)**, which also extends
  `DependencyDirectionTests.cs` for the desktop boundaries and the no-WebView rule. *Mitigation*:
  step 8 checks for an existing `Microsoft.UI.Xaml` entry first; a conflict is resolved by keeping one
  entry per prefix, never by declaring an overlapping second array.
- **Risk — XML documentation breaks the build.** `AnalysisLevel=latest-recommended` analyzes doc
  comments; a malformed `<see cref="…"/>` in the conventions text is a build break under
  `TreatWarningsAsErrors=true`. *Mitigation*: step 9 runs the build before the test step, so the
  failure surfaces on the cheapest command.
- **Scope boundary, not an open question — the OpenAPI snapshot and the generated client.** There is
  no `openapi/` directory today (`ls openapi` → *No such file or directory*), so no snapshot ripple is
  claimable. [[GWY-004]] (plan handle `DSK-03-04`) creates `openapi/pegasus-v1.json` and [[GWY-005]]
  (plan handle `DSK-03-05`) generates the Kiota client; from then on every member change here ripples
  into both.
- **Scope boundary, not an open question — problem-details registration.** `Program.cs` has no
  `AddProblemDetails` call and nothing consumes `PegasusProblem` yet. [[GWY-002]] (plan handle
  `DSK-03-02`) registers the middleware and ports the mapping from `AutomationMcpErrors.cs`.
- **Scope boundary, not an open question — `OperatorLabels`.** [[GWY-016]] (plan handle `DSK-03-16`)
  and [[FEAT-023]] (plan handle `DSK-05-23`) own its relocation into this project. Do not pre-empt it.
- **Body imprecision worth recording, not a contradiction.** The body's *Source of truth* cites
  `AutomationMcpErrors.cs:19-76` for "the existing exception-to-transport mapping"; measured, the
  mapping is `:19-67` and `:76` is where `RequireOperationKey` begins — the body's second citation of
  `:76` is exact. The body describes the other three paged ports as "all the same five-member cursor
  shape"; measured, the pattern is identical but the member names differ per port and
  `ListAutomationActivityResult` has a sixth member. Neither changes any instruction, and the body's
  load-bearing claim — that none of the four counts — is confirmed.
- **No open question is opened on this ticket.** Everything unknown is settled by a command inside the
  ticket's own steps; nothing requires an answer from outside before implementation may begin.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
