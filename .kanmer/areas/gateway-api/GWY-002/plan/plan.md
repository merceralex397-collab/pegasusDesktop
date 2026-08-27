# Plan — GWY-002: `/api/v1` route-group skeleton behind `Features:DesktopGateway`

**Diff estimate: ~9 files, ~535 lines.**

`docs/engineering.md` § Plan sizing requires the estimate first. Derived from the files document,
file by file:

| File | Change | Lines |
| --- | --- | --- |
| `src/Pegasus.Web/Api/DesktopGateway.cs` | new — fixed-names class plus `DesktopGatewayOptions.TryCreate`; far smaller than `AutomationMcp.cs` because there is no secret, origin or redirect list to validate | ~45 |
| `src/Pegasus.Web/Api/DesktopGatewayExtensions.cs` | new — `AddPegasusDesktopGateway` and `MapPegasusDesktopGateway`, modelled on `AutomationMcpExtensions.cs:131-140` | ~35 |
| `src/Pegasus.Web/Api/DesktopGatewayProblems.cs` | new — the `IExceptionHandler`, seven branches at ~10 lines each plus problem construction and correlation lookup | ~110 |
| `src/Pegasus.Web/Api/CorrelationIdEndpointFilter.cs` | new — accept/validate/generate/echo plus the named client-version extension point | ~55 |
| `src/Pegasus.Web/Program.cs` | 1,216 lines; five composition edits — options resolution at `:246`, services beside `:625-628`, the scoped `UseExceptionHandler` branch, `MapPegasusDesktopGateway()` after `:959-960`, and `DesktopGateway.BasePath` added to `IsMachineSurface` at `:973-977` | ~+14 |
| `src/Pegasus.Web/Pegasus.Web.csproj` | one `ProjectReference` to `src/Pegasus.Contracts`, **only if** [[GWY-001]] (plan handle `DSK-03-01`) has not already added it | ~+1 |
| `tests/Pegasus.IntegrationTests/DesktopGatewayCompositionTests.cs` | new — gate-off theory (absent and `false`), gate-on `EndpointDataSource` fact | ~120 |
| `tests/Pegasus.IntegrationTests/DesktopGatewayProblemTests.cs` | new — the throwing-endpoint harness plus seven facts | ~150 |
| `docs/current-architecture.md` | 682 lines; § Current callers and entry points (`:92`) gains the `/api/v1` surface and the `Features:DesktopGateway` gate, in the shape of the `Features:AutomationMcp` prose at `:520` | ~+6 |

If `ProjectReferencesFollowTheModularMonolithDirection` in
`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` must also be updated because this ticket
adds the Web → Contracts reference, add one file and ~2 lines.

## Approach

Copy the Automation MCP composition idiom exactly — it already solved "a whole surface that is absent
unless a flag is on" in this codebase, and re-solving it differently would leave two gate shapes for a
reviewer to hold in mind. The rejected alternative was a middleware-based gate (map the group
unconditionally and 404 it in middleware when the flag is off): it is fewer lines, but it leaves
`/api/v1` routes present in `EndpointDataSource`, which makes the gate-off assertion untestable in the
way `LocalIntakeAccessTests.cs:56` tests it, and it contradicts area 03 § 3's *Composition gate* rule
that "when false nothing under `/api/v1` is mapped".

One choice inside that shape is load-bearing and is argued from measurement rather than taste. The
body's step 6 requires the problem mapping to be an `IExceptionHandler` "scoped to the group".
`AddExceptionHandler` only registers the handler in DI; the middleware that invokes it,
`UseExceptionHandler`, exists in this application **only outside Development**
(`src/Pegasus.Web/Program.cs:754-756`) — and every integration test runs Development
(`tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26`, `:42-62`, `:97-99`). An
`AddExceptionHandler`-only implementation would therefore compile, ship, and be silently dead under
every test that claims to prove it. "Scoped to the group" is implemented literally, as a path branch:

```
app.UseWhen(
    context => context.Request.Path.StartsWithSegments(DesktopGateway.BasePath),
    branch => branch.UseExceptionHandler(new ExceptionHandlerOptions()));
```

registered only when the gate is open, in every environment, and never by relocating or unguarding
`UseExceptionHandler("/Error")` — which would change Razor error behaviour and is outside the
Guardrails.

## Governing docs

The ticket's `refs` array is empty and `get_doc_gates GWY-002` reports `docs_todo: true`, so there is
no linked PRD/FRD/ADR to meet today.

> **New ADR** — ADR-0103 (gateway, never direct database access from workstations) and ADR-0101
> (local-execution / cloud-authority split and the six-question cloud-justification test), both
> authored by [[FND-005]] (plan handle `DSK-00-05`); ADR-0100 (native WinUI 3 client in the fork) is
> claimed by both [[FND-005]] and [[FND-026]] (plan handle `DSK-02-01`) — see [[FND-026]]'s plan for
> the ownership reconciliation. ADR-0102 (desktop session tokens) is owned by area 04 and is cited,
> not authored, here.
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table); if any of them lands
> differently this plan is revised before implementation.

Because `refs` is empty, these are the authorities that actually bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 10.2 | One versioned REST API, feature-based route groups, standard problem responses, correlation identifiers | Steps 3–7 |
| Proposal § 16.1 | Operation model: correlation ids, problem details, cancellation, retry eligibility | Steps 6, 7 (cancellation rethrow; retry eligibility is [[GWY-017]]'s) |
| Plan 03 § 3 *Hosting* (`README.md:160`) | Versioned route groups registered in `Program.cs` beside Razor Pages; same Container App; no new deployment unit | Steps 4, 5 |
| Plan 03 § 3 *Composition gate* (`:161`) | Gate false → nothing under `/api/v1` mapped; same shape as `Features:AutomationMcp`; production enablement is an app-setting change | Steps 3, 5, 9 |
| Plan 03 § 3 *Projection style* (`:162`) | Thin argument-mappers over Core; no business rule in Web; MCP and API stay two ingresses over one Core | Step 4 maps a group and nothing else; § Risks records the "two policy engines" trap |
| Plan 03 § 3 *Problem details* (`:167`) | RFC 9457 via `AddProblemDetails`; the thirteen `urn:pegasus:problem:` slugs; no payload dumps; `correlationId` always present | Steps 6, 8 |
| Plan 03 § 3 *Correlation & client version* (`:168`) | `X-Correlation-Id` accepted or generated, echoed, logged; the client-version check hooks in | Step 7 |
| Plan 03 § 4 exit gate | "`Features:DesktopGateway=false` leaves no `/api/v1` route (404 test)"; "Razor page tests stay green" | Steps 9, 11 |
| Plan 03 § 7 traps (`:249-286`) | Assert the gate both ways; no second policy engine; do not change the cookie scheme defaults; no Windows-only package in `Pegasus.Web` | Steps 5, 9; § Risks |
| L-01 (locked, `docs/desktop/README.md`) | The gateway is `Pegasus.Web` evolved in place | Step 5 maps into the existing host; no new project or deployment unit |
| L-02 (locked) | The gate is exercised in the local production-mimicking stack; there is no Azure test environment | Steps 9–11 run against `WebApplicationFactory` and LocalDB only |
| D-001 (decided 2026-08-23) | The fork becomes the single release source at the first production gateway change | Recorded; no action in this ticket — the release itself is area 09's |
| `AGENTS.md` safety rails | A change that leaves the current-state documents stale is unfinished | Step 11's `docs/current-architecture.md` edit, in this same task |
| `AGENTS.md` § Repository task workflow step 4 | A simplification pass over the branch diff before the PR | Step 12 |
| `docs/engineering.md` § Required evidence tiers, tier 5 | Web/API/MCP caller: prove the route reaches (or does not reach) Core through a real host | § Verification |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`,
  plugin `dotnet-aspnetcore`) → `microsoft-code-reference` (Microsoft Learn plugin).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move;
  a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's twelve steps: same order, same ownership, same file paths.

1. **Orient.** Read `docs/desktop/03-gateway-api-and-data/README.md` § 3 (`:160-168`) and § 7
   (`:249-286`), then `endpoint-map.md` § Conventions. Read `src/Pegasus.Web/Mcp/AutomationMcp.cs`
   whole and `src/Pegasus.Web/Program.cs:246`, `:517-520`, `:625-628`, `:750-756`, `:820`, `:874-900`,
   `:939-966`, `:969-977`. Call `get_doc_gates GWY-002`, then `take_ticket` with branch
   `task/desktop-gateway-group` and worktree `../pegasus-worktrees/desktop-gateway-group` from
   `origin/dev`.
2. **Confirm the .NET 10 shapes before writing code.** `microsoft_docs_search` for
   `ASP.NET Core minimal API route groups MapGroup endpoint filters` and for
   `AddProblemDetails IProblemDetailsService IExceptionHandler RFC 9457`; `global.json` pins SDK
   `10.0.302` (`rollForward: latestFeature`, `allowPrerelease: false`), so verify signatures rather
   than recalling them. Also `ls src/Pegasus.Contracts/ProblemDetails/PegasusProblemTypes.cs` and
   `grep -n Pegasus.Contracts src/Pegasus.Web/Pegasus.Web.csproj`: if the project reference is absent,
   add it in step 3 **and** update the expected array in
   `ProjectReferencesFollowTheModularMonolithDirection`
   (`tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`) in the same change, or the `unit`
   lane fails. Note the naming: the body says "the `ProblemTypes` constants"; the type is
   `PegasusProblemTypes` and [[GWY-001]] forbids a type called `ProblemTypes` — use the file that
   exists.
3. **`src/Pegasus.Web/Api/DesktopGateway.cs`.** `public static class DesktopGateway` with
   `public const string FeatureFlag = "Features:DesktopGateway";` and
   `public const string BasePath = "/api/v1";`, then
   `public sealed record DesktopGatewayOptions` with
   `static DesktopGatewayOptions? TryCreate(IConfiguration configuration)` returning `null` when
   `configuration.GetValue<bool>(DesktopGateway.FeatureFlag)` is false, exactly as
   `AutomationMcpOptions.TryCreate` does. Do **not** add a runtime-profile check: unlike
   `Features:LocalIntake` (`Program.cs:112-116`, `:655`) and `Features:SendToAi`, this gate is meant
   for production. Copy the two-outcome discipline — `null` for a closed gate, `InvalidOperationException`
   for a configured-but-invalid one — even though there is nothing to invalidate yet, so a later
   setting has an obvious home.
4. **`src/Pegasus.Web/Api/DesktopGatewayExtensions.cs`.**
   `AddPegasusDesktopGateway(this IServiceCollection services, DesktopGatewayOptions options)`
   registers `AddProblemDetails()` and `AddExceptionHandler<DesktopGatewayExceptionHandler>()`.
   `MapPegasusDesktopGateway(this WebApplication app)` calls `app.MapGroup(DesktopGateway.BasePath)`,
   attaches `CorrelationIdEndpointFilter` and the named client-version extension point, and
   **returns the `RouteGroupBuilder`** — thirteen downstream tickets attach to it. It registers no
   endpoint of its own. Document on the method, as `AutomationMcpExtensions.cs:122-130` does, exactly
   what authentication does and does not apply at this stage.
5. **`src/Pegasus.Web/Program.cs` — five composition edits and no logic.**
   (a) beside `:246`, `var desktopGatewayOptions = DesktopGatewayOptions.TryCreate(builder.Configuration);`
   (b) beside `:625-628`, `if (desktopGatewayOptions is not null) { builder.Services.AddPegasusDesktopGateway(desktopGatewayOptions); }`
   (c) before routing, and only when the options are non-null, the branch-scoped
   `app.UseWhen(context => context.Request.Path.StartsWithSegments(DesktopGateway.BasePath), branch => branch.UseExceptionHandler(new ExceptionHandlerOptions()));`
   — this is the edit the ticket turns on; see § Approach for why proximity to the group is not
   enough. Place it beside the existing `UseWhen` at `:750-752` so both path branches read together.
   (d) immediately after `app.MapRazorPages()` at `:959-960`,
   `if (desktopGatewayOptions is not null) { app.MapPegasusDesktopGateway(); }`
   (e) add `|| path.StartsWithSegments(DesktopGateway.BasePath)` to `IsMachineSurface` at `:973-977`,
   so `UseStatusCodePagesWithReExecute("/status/{0}")` (`:750-752`) does not turn an API 4xx into an
   HTML card. Nothing under `/api/v1` may be reachable when the flag is off.
6. **`src/Pegasus.Web/Api/DesktopGatewayProblems.cs`.** The `IExceptionHandler`, using
   `PegasusProblemTypes` constants from `src/Pegasus.Contracts`. Branch order is a **correctness
   requirement**, because `CaseVersionConflictException` (`CaseWorkflowContracts.cs:125`),
   `CaseEditLeaseConflictException` (`:135`), `CaseEditLeaseExpiredException` (`:143`) and
   `CaseOperationConflictException` (`:151`) all derive from `InvalidOperationException`:
   1. `StaffAuthorizationException` → 403 `not-authorized`
   2. `CaseVersionConflictException` → 409 `version-conflict`, `currentVersion = ActualVersion`
   3. `CaseEditLeaseConflictException` → 409 `lease-conflict`, `currentVersion = CaseVersion`
   4. `CaseEditLeaseExpiredException` → 409 `lease-expired`, `currentVersion = CaseVersion`
   5. `CaseOperationConflictException` → 409 `operation-conflict` (the one
      `AutomationMcpErrors.cs:19-67` does **not** name today, so it currently falls into the
      validation bucket — putting it after the combined branch makes it unreachable)
   6. `ArgumentException` / `InvalidOperationException` / `InvalidDataException` → 400 `validation`
   7. `OperationCanceledException` → rethrow (client abort), then anything else → 500 generic problem
      carrying **no exception text**.
   Copy the message discipline of `AutomationMcpErrors.cs:7-15`: a domain refusal names the guard and
   the current version so the client can reload and reacquire; an unexpected failure collapses; no
   token or holder material crosses the boundary. Every body carries `correlationId`.
7. **`src/Pegasus.Web/Api/CorrelationIdEndpointFilter.cs`.** Read `PegasusHeaders.CorrelationId` (never
   a string literal) from the request; accept it when it is ≤ 200 characters with no control
   characters, otherwise generate from `HttpContext.TraceIdentifier`; stash it where step 6's handler
   can read it; echo it on the response header; log it. Add the named no-op filter registration where
   [[GWY-023]] (plan handle `DSK-04-06`) inserts the `X-Pegasus-Client-Version` check — a named type,
   not a `TODO` comment. Do **not** implement the version check here.
8. **`AddProblemDetails()` and the Razor guard.** It is added in step 4(a)'s service extension;
   confirm by `grep` that no second registration exists (`grep -rn 'AddProblemDetails' src/` returned
   nothing before this ticket). Then confirm the addition changes no Razor behaviour: `Pages/Error.cshtml.cs`
   and `Pages/StatusCode.cshtml.cs` must still render, and the `IsMachineSurface` branch at `:750-752`
   is what keeps the two worlds apart now that `/api/v1` is on the machine side.
9. **`tests/Pegasus.IntegrationTests/DesktopGatewayCompositionTests.cs`.** Model it on
   `LocalIntakeAccessTests.cs:10-56`. Class-level `[Trait("Category", "SqlServer")]`. A
   `TheoryData<bool?>` of `null` (absent) and `false`, mirroring `DeniedConfigurations` at `:13-17`.
   For each: `GET /api/v1/anything` is 404, **and** `factory.Services.GetRequiredService<EndpointDataSource>().Endpoints`
   contains no endpoint whose `RoutePattern.RawText` starts `api/v1`. Then a gate-on fact supplying the
   flag with `WithWebHostBuilder(builder => builder.UseSetting("Features:DesktopGateway", "true"))` —
   the shared factory sets only `Features:LocalIntake` and `Features:LocalDocumentCustody`
   (`IntakeWebTestSupport.cs:100-141`) and therefore overrides nothing. Assert the gate-on case on
   `EndpointDataSource`, **not** on a status code: `SetFallbackPolicy(RequireAuthenticatedUser())`
   (`Program.cs:517-520`) makes an unauthenticated `/api/v1` call 302 to `/Account/SignIn`, which would
   make a status-code assertion prove the wrong thing.
10. **`tests/Pegasus.IntegrationTests/DesktopGatewayProblemTests.cs`.** Seven facts, one per branch of
    step 6, asserting status code, `type` URI and the presence of `correlationId`. Map the throwing
    endpoint from the test only — `WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<IStartupFilter, …>))`
    mapping `/api/v1/__throw/{kind}` with `.AllowAnonymous()` — so no test hook enters production code.
    If that proves unworkable on minimal hosting, fall back to unit-testing `DesktopGatewayProblems`
    directly and keep only the composition facts as integration tests, and **record the fallback in
    this document** rather than dropping coverage silently. Add one further fact not in the body: with
    the gate on, an API 404 returns a problem body and not `/status/404` HTML — that is the
    `IsMachineSurface` edit's only executable proof.
11. **Build, test, document.** `dotnet build Pegasus.slnx -c Release`; then
    `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGateway"`;
    then the whole integration project so the existing Razor page tests are visibly green. Update
    `docs/current-architecture.md` § Current callers and entry points (`:92`, `### Technical entry
    points` at `:116`) with the `/api/v1` surface and the `Features:DesktopGateway` gate, in the shape
    of the `Features:AutomationMcp` prose at `:520` — including its closing discipline that source
    inventory is not deployed inventory.
12. **Simplify and close.** Run the simplification pass over the branch diff (`AGENTS.md` step 4) and
    record findings and dispositions under the dated `## Simplification pass` heading below. Open the
    PR into `dev`.

## Verification

Evidence tier **5 — Web/API/MCP caller** (`docs/engineering.md` § Required evidence tiers), as the
ticket body states: this obliges proving the actual route reaches (or does not reach) Core through a
real `WebApplicationFactory` host, with exception translation and the composition gate **observable**,
not merely registered.

The `proof` document is produced from these command logs:

1. `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayCompositionTests"`
   — expected: all facts pass, including both gate-off cases (flag absent and flag `false`) asserting
   404 **and** an empty `api/v1` slice of `EndpointDataSource`.
2. `dotnet test … --filter "FullyQualifiedName~DesktopGatewayProblemTests"` — expected: seven branch
   facts plus the "404 is a problem body, not `/status/404` HTML" fact.
3. `dotnet build Pegasus.slnx -c Release` — expected: `Build succeeded`, `0 Warning(s)`,
   `0 Error(s)`.
4. `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release` (whole
   project) — expected: green, proving the acceptance criterion that Razor Pages, OpenIddict and MCP
   behave exactly as before.
5. `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj -c Release` —
   expected: green, in particular `ProjectReferencesFollowTheModularMonolithDirection` if the
   Web → Contracts reference was added here.
6. Additionally, and not in the body:
   `grep -rn 'Features:DesktopGateway\|"/api/v1"' src/Pegasus.Web/ --include=*.cs` — expected: the flag
   string appears **once**, in `DesktopGateway.cs`, and the base path appears **once**, in the same
   file. Every other use goes through the constants. A second literal is the duplication the fixed-names
   class exists to prevent.

## Risks / open questions

- **Risk — the exception handler is registered but never invoked.** `UseExceptionHandler("/Error")` is
  inside `if (!app.Environment.IsDevelopment())` (`Program.cs:754-756`) and the integration tests run
  Development, so an `AddExceptionHandler`-only implementation ships dead. *Mitigation*: step 5(c)'s
  branch-scoped `UseExceptionHandler`, and step 10's seven facts running under the Development-hosted
  factory, which fail loudly if it is missing. **Never** unguard or relocate the Razor handler.
- **Risk — API 4xx arrives as an HTML card.** `UseStatusCodePagesWithReExecute("/status/{0}")`
  (`:750-752`) applies to everything not on `IsMachineSurface` (`:973-977`), and `/api/v1` is not there
  today. *Mitigation*: step 5(e), with step 10's extra fact as its executable proof.
- **Risk — the gate-on test proves the wrong thing.** With
  `SetFallbackPolicy(RequireAuthenticatedUser())` (`:517-520`), an unauthenticated `/api/v1` call 302s
  to `/Account/SignIn`. *Mitigation*: step 9 asserts on `EndpointDataSource`, copying
  `LocalIntakeAccessTests.cs:56`; only the gate-**off** case asserts a status code, where a missing
  route genuinely 404s before authorization runs.
- **Risk — `CaseOperationConflictException` silently becomes a 400.** It derives from
  `InvalidOperationException` and `AutomationMcpErrors` does not name it. *Mitigation*: step 6 fixes
  the branch order explicitly and step 10 gives it its own fact.
- **Risk — a second policy engine.** Any authorization or business rule that appears in the group or
  its filters is a defect (area 03 § 7). *Mitigation*: step 4 maps a group and nothing else; the
  correlation filter touches headers only; authorization is [[GWY-003]]'s and [[GWY-021]]'s.
- **Risk — the Web → Contracts project reference breaks an architecture fact.**
  `ProjectReferencesFollowTheModularMonolithDirection` asserts `Pegasus.Web`'s exact reference list.
  *Mitigation*: step 2 checks and step 2 updates the expected array in the same change.
- **Risk — coexistence regressions.** Adding `AddProblemDetails()` changes default error responses for
  endpoints that do not opt out; the cookie scheme defaults (`__Host-Pegasus`, `SameSite=Strict`) must
  not move. *Mitigation*: step 11 runs the whole integration project, which the body makes an
  acceptance criterion.
- **Scope boundary, not an open question — the production app setting.**
  `Features__DesktopGateway=true` on the production Web Container App is owned by [[PLAT-024]] (plan
  handle `DSK-11-06`) and by no other ticket, performed once at the Phase 2 release under exact-target
  approval and mirrored in `docs/desktop/11-azure-disposition/README.md`. **No Azure write from this
  ticket.**
- **Scope boundary, not an open question — bearer authentication and the client-version check.**
  [[GWY-021]] (plan handle `DSK-04-04`) owns claims-to-actor bearer authentication, [[GWY-003]] (plan
  handle `DSK-03-03`) the per-right endpoint filter, and [[GWY-023]] (plan handle `DSK-04-06`) the
  `X-Pegasus-Client-Version` check. Step 7 leaves a named extension point and no implementation.
- **Scope boundary, not an open question — the `MustChangePassword` redirect.** The middleware at
  `:875-899` will 302 an `/api/v1` call for such a user; the `password-change-required` slug exists for
  the translation, which belongs to [[GWY-021]] / [[GWY-022]] (plan handle `DSK-04-05`). This ticket
  records it and forecloses nothing.
- **Body imprecision worth recording, not a contradiction.** The body's step 8 cites "the
  `IsMachineSurface` re-execute guard at `Program.cs:968-975`"; measured, `:969-977` is the helper's
  doc comment and body, and the guard that *uses* it is at `:750-752`. Both edits are needed and both
  are in step 5. The body's step 6 says "the `ProblemTypes` constants"; the type is
  `PegasusProblemTypes` ([[GWY-001]] forbids a type named `ProblemTypes`). The body's
  `AutomationMcpErrors.cs:19-70` is `:19-67` as measured. None of these changes an instruction.
- **No open question is opened on this ticket.** Everything unknown is settled by a command inside the
  ticket's own steps, and the one genuine design fork — how the throwing endpoint is mapped for the
  seven problem facts — has a stated primary and a stated fallback in step 10, both inside this
  ticket's mandate.

## Simplification pass

_Completed on 2026-08-27; see the dated `## Simplification pass` heading below._

## Implementation checkpoint — 2026-08-27

Implemented the planned gateway composition, path-scoped exception handler, correlation/client-version filter registration, safe problem mapping, Web → Contracts reference, architecture expectation, and current-state documentation. Microsoft Learn verification confirmed the .NET 10 `MapGroup`, endpoint-filter, `AddProblemDetails`, `IExceptionHandler`, and `UseExceptionHandler` shapes.

The planned `IStartupFilter` throwing-endpoint harness was not used. The production group intentionally contains no endpoint, and adding a test-only route through minimal-host startup would require a production-facing test hook or alter route composition. The fallback named in step 10 is therefore used: `DesktopGatewayProblemTests` directly invoke the internal handler for every mapping branch, while `DesktopGatewayCompositionTests` exercise the real `WebApplicationFactory` composition and machine-surface behavior. This keeps production scope unchanged and preserves branch-complete coverage.

Focused validation completed: `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGateway" -nr:false` — 19 passed, 0 failed, 0 skipped.

## Simplification pass — 2026-08-27

- **Reuse:** the existing Automation MCP option/gate composition idiom is reused; shared contracts use [[GWY-001]]'s `PegasusHeaders`, `PegasusJson`, and `PegasusProblemTypes` rather than duplicating names or serialization rules.
- **Simplification:** the gateway adds no endpoint, controller, policy engine, compatibility path, deployment unit, or new dependency. The named client-version filter is the smallest required extension point and remains a no-op until [[GWY-023]].
- **Efficiency:** the initial logging call triggered repository analyzers `CA1848` and `CA1873`; it was replaced with a source-generated `LoggerMessage` delegate. No unnecessary per-request allocation or logging-template parsing remains.
- **Clarity/altitude:** exception branches are ordered before their `InvalidOperationException` base type, and the path-scoped handler is composed only when the feature gate is open. No business rules, authentication, authorization, or client-version policy were added outside their owning tickets.
- **Disposition:** no behavior-preserving simplification findings remain unapplied. The documented direct-handler test fallback is retained because the production group intentionally has no endpoint and adding a test-only production hook would expand scope.


## Review remediation — 2026-08-27

Hilbert's independent review identified evidence and boundary gaps. Remediated them before merge:

- The closed-gate composition theory now issues the required `GET /api/v1/anything` and asserts 404 for both absent and explicit-false configurations.
- The enabled composition fact now proves an unmatched API request returns `application/problem+json`, the `not-found` type, and the supplied correlation ID rather than the Razor `/status/404` page.
- The enabled path branch now applies correlation middleware before the path-scoped exception handler and a path-scoped status-code writer, so unmatched API 404s and pre-endpoint failures receive the same correlation/problem boundary as matched endpoints. The endpoint filter remains attached for later routes and reuses the middleware's correlation value.
- A test-only `IStartupFilter` appends a throwing middleware after the application pipeline, allowing the real host's path-scoped exception handler to be exercised without a production route or hook. The direct handler facts remain as branch-level coverage.
- Focused validation after remediation: 19 passed, 0 failed, 0 skipped.

## Final validation checkpoint — 2026-08-27

After the review remediation, the full repository checks completed:

- `dotnet build Pegasus.slnx -c Release -nr:false` — 0 warnings, 0 errors.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGateway" -nr:false` — 19 passed, 0 failed, 0 skipped.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release -nr:false` — 958 passed, 16 skipped, 0 failed (974 total); skips are expected absent-local-corpus cases.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj -c Release -nr:false` — 110 passed, 0 failed, 0 skipped.
- Static checks — exactly one `AddProblemDetails` registration and exactly one literal for each gateway constant; `git diff --check` clean.
