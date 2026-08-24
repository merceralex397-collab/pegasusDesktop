# Files — GWY-002

Surveyed 2026-08-24 against fork `main`. Every existing path was confirmed with `ls`/`grep`; new files
are marked, and paths owned by another ticket name it.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Web/Api/DesktopGateway.cs` | **New** (the `Api/` folder does not exist — `ls src/Pegasus.Web` shows `AiWork`, `Authentication`, `Data`, `Health`, `Mcp`, `Pages`, `Presentation`, `Properties`, `wwwroot` only). Mirrors `src/Pegasus.Web/Mcp/AutomationMcp.cs`: `public const string FeatureFlag = "Features:DesktopGateway";`, `public const string BasePath = "/api/v1";`, and `DesktopGatewayOptions.TryCreate(IConfiguration)` returning `null` when the flag is absent or false. Keep it small — unlike `AutomationMcpOptions` there is no secret, no origin and no redirect list to validate. |
| `src/Pegasus.Web/Api/DesktopGatewayExtensions.cs` | **New.** `AddPegasusDesktopGateway(this IServiceCollection, DesktopGatewayOptions)` and `MapPegasusDesktopGateway(this WebApplication)`. The map method calls `app.MapGroup(DesktopGateway.BasePath)` and **returns the `RouteGroupBuilder`** so later tickets attach sub-groups; it registers no endpoint of its own. Shape it on `AutomationMcpExtensions.cs:131-140`. |
| `src/Pegasus.Web/Api/DesktopGatewayProblems.cs` | **New.** The `IExceptionHandler` implementation. Seven branches in the order fixed by inheritance — `StaffAuthorizationException` → 403 `not-authorized`; `CaseVersionConflictException` → 409 `version-conflict` (`currentVersion = ActualVersion`); `CaseEditLeaseConflictException` → 409 `lease-conflict` (`currentVersion = CaseVersion`); `CaseEditLeaseExpiredException` → 409 `lease-expired` (`currentVersion = CaseVersion`); `CaseOperationConflictException` → 409 `operation-conflict`; then the combined `ArgumentException`/`InvalidOperationException`/`InvalidDataException` → 400 `validation`; `OperationCanceledException` → rethrow; anything else → 500 generic carrying **no exception text**. All four case exceptions derive from `InvalidOperationException`, so the combined branch must come last or four branches are unreachable. |
| `src/Pegasus.Web/Api/CorrelationIdEndpointFilter.cs` | **New.** Applied to the root group: accept `X-Correlation-Id` when present and well formed (≤ 200 characters, no control characters), otherwise generate from `HttpContext.TraceIdentifier`; echo it on the response and place it in every problem body's `correlationId`. Use `PegasusHeaders.CorrelationId` from `src/Pegasus.Contracts` rather than a string literal. Also the named no-op filter registration where [[GWY-023]] (plan handle `DSK-04-06`) inserts the `X-Pegasus-Client-Version` check — a named extension point, not a `TODO`. |
| `src/Pegasus.Web/Program.cs` | 1,216 lines. **Five small composition edits, no logic:** (a) resolve `DesktopGatewayOptions.TryCreate(builder.Configuration)` beside `automationMcpOptions` at `:246`; (b) `builder.Services.AddProblemDetails()` + `AddExceptionHandler<…>()` when the options are non-null, beside the `:625-628` block; (c) the branch-scoped `UseExceptionHandler` on `/api/v1` (see *Ripple effects* — this is the edit the whole ticket turns on); (d) `app.MapPegasusDesktopGateway()` when non-null, immediately after `app.MapRazorPages()` at `:959-960`; (e) add `DesktopGateway.BasePath` to `IsMachineSurface` at `:973-977`. |
| `tests/Pegasus.IntegrationTests/DesktopGatewayCompositionTests.cs` | **New**, modelled on `LocalIntakeAccessTests.cs`. `[Trait("Category", "SqlServer")]`; a `TheoryData` of *absent* and *explicitly `false`* like `DeniedConfigurations` at `:13-17`; assert `GET /api/v1/anything` is 404 and that `EndpointDataSource.Endpoints` contains no pattern starting `api/v1`; with the flag `true`, assert the group exists. Supply the flag with `WithWebHostBuilder(b => b.UseSetting("Features:DesktopGateway", "true"))` — the shared factory has no knob for it and needs none. |
| `tests/Pegasus.IntegrationTests/DesktopGatewayProblemTests.cs` | **New.** Seven facts, one per branch of `DesktopGatewayProblems`, each asserting status code, `type` URI and the presence of `correlationId`. Needs a throwing endpoint *inside* the `/api/v1` path (see *Ripple effects* for the two candidate mechanisms and the one this plan chooses). |
| `docs/current-architecture.md` | 682 lines. § Current callers and entry points begins at `:92` (`### Technical entry points` at `:116`); the feature-gate prose for `Features:AutomationMcp` is at `:520`. Add the `/api/v1` surface and the `Features:DesktopGateway` gate **in this same task** — `AGENTS.md` safety rails make a change that leaves the current-state docs stale unfinished. |

## Context files

What the implementer must **read**, and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Web/Mcp/AutomationMcp.cs` | The whole gate idiom in one file: a `public static class` of fixed names with the flag as `const string` at `:12`, and an options record whose `TryCreate` returns `null` on a closed gate but **throws `InvalidOperationException`** on a configured-but-invalid one. Copy the two-outcome shape; do not copy the secret/origin/redirect validation, which the desktop gateway has no analogue for. |
| `src/Pegasus.Web/Program.cs:246`, `:625-628`, `:820`, `:961-964` | That a gated surface in this codebase is resolved **once** and consulted at **three** later points — services, middleware, mapping. Registering only at the mapping site is the half-composed failure this pattern exists to prevent. |
| `src/Pegasus.Web/Program.cs:750-756` | The two facts that decide this ticket's pipeline design: `UseStatusCodePagesWithReExecute("/status/{0}")` is applied to everything *except* `IsMachineSurface` paths, and `UseExceptionHandler("/Error")` is inside `if (!app.Environment.IsDevelopment())`. The first means `/api/v1` 4xx becomes HTML unless `IsMachineSurface` is extended; the second means an `AddExceptionHandler`-only implementation never runs under the integration tests. |
| `src/Pegasus.Web/Program.cs:969-977` | `IsMachineSurface` and its summary — "Paths whose callers are programs, not people: they want a status code and a parsable body, and a re-executed HTML card would break them." The rule already written down that the one-line `/api/v1` addition satisfies. |
| `src/Pegasus.Web/Program.cs:517-520` | `SetFallbackPolicy(RequireAuthenticatedUser())`. Every endpoint without authorization metadata challenges the cookie scheme, so an unauthenticated `/api/v1` call **302s to `/Account/SignIn`** rather than returning 401. This is why the gate-on assertion reads `EndpointDataSource` and why any test-only endpoint carries `.AllowAnonymous()` at this stage. |
| `src/Pegasus.Web/Program.cs:874-900` | The password-change redirect middleware between `UseAuthentication` and `UseAuthorization`. Its allow-list is `/Account/PasswordChange`, `/Account/SignOut`, `/css`, `/js`, `/lib`, `/favicon.ico` — `/api/v1` is absent, so an authenticated `MustChangePassword` user gets a 302 and HTML. The `password-change-required` slug exists for this; the translation is [[GWY-021]] (plan handle `DSK-04-04`) and [[GWY-022]] (plan handle `DSK-04-05`), not this ticket — but do not design a pipeline that forecloses it. |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:7-15` | The message discipline to port verbatim in spirit: a domain refusal names which guard refused and the current version so the caller can reload and reacquire; anything unexpected collapses to a generic failure; "no token or other holder material crosses the boundary". |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:19-67` | The measured catch order — lease-expired, lease-conflict, version-conflict, then the combined `ArgumentException or InvalidOperationException or InvalidDataException`, then `OperationCanceledException` rethrow, then the generic collapse. Note what is **missing**: `CaseOperationConflictException` is never caught by name and falls into the combined branch today. |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:125-157` | The four exceptions and, decisively, that **all four derive from `InvalidOperationException`**. `CaseVersionConflictException` exposes `ExpectedVersion` and `ActualVersion`; both lease exceptions expose `CaseVersion`; `CaseOperationConflictException` exposes `OperationKey`. These are the members the 409 problem bodies carry. |
| `src/Pegasus.Contracts/ProblemDetails/PegasusProblemTypes.cs` (created by [[FND-029]] (plan handle `DSK-02-04`), extended by [[GWY-001]] (plan handle `DSK-03-01`)) | The thirteen slug constants and the `urn:pegasus:problem:` prefix. The body's step 6 calls this "the `ProblemTypes` constants"; the real type name is `PegasusProblemTypes` and [[GWY-001]]'s acceptance criteria forbid a type called `ProblemTypes`. Use the file that exists; never add a second type to match prose. |
| `src/Pegasus.Contracts/ProblemDetails/PegasusProblem.cs`, `src/Pegasus.Contracts/PegasusHeaders.cs` | The response shape (`Type`, `Title`, `Status`, `Detail`, `Instance`, always-present `CorrelationId`, typed `CurrentVersion`/`MinimumVersion`) and the header names `X-Correlation-Id` / `X-Pegasus-Client-Version`. A string literal for either header on this side is the duplication `PegasusHeaders` exists to prevent. |
| `src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs:122-140` | `MapPegasusAutomationMcp` — the "map only when composed" precedent, and the habit of documenting on the map method exactly which authentication does and does not apply. |
| `tests/Pegasus.IntegrationTests/LocalIntakeAccessTests.cs:10-56` | The gate-both-ways pattern in full: the class-level `[Trait("Category", "SqlServer")]`, a `TheoryData<string, bool?>` covering *absent* **and** *false*, per-path 404 assertions, and — the move worth copying most — `factory.Services.GetRequiredService<EndpointDataSource>().Endpoints` at `:56`, which asserts on the routing table rather than on a response and so is immune to the fallback authorization policy. |
| `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26-141` | That the shared factory is `WebApplicationFactory<Program>`, that every convenience constructor uses `"Development"`, and that `ConfigureWebHost` sets only `Features:LocalIntake` and `Features:LocalDocumentCustody`. Two consequences: the tests run in the environment where `UseExceptionHandler` is *not* registered, and `Features:DesktopGateway` can be supplied per test with `WithWebHostBuilder(…UseSetting…)` without touching this shared file. |
| `.github/workflows/ci.yml:151-175` | The `sql-integration` lane: `./scripts/Invoke-TestShard.ps1 … -Filter "Category!=Corpus&Category!=Browser"` across three shards. Tells the implementer the new tests will run on every PR, and that the `SqlServer` trait is a file convention rather than the selector. |
| `docs/desktop/03-gateway-api-and-data/README.md:160-168` | The five binding § 3 rows for this ticket: *Hosting* (route groups in `Program.cs` beside Razor Pages, no new deployment unit), *Composition gate* (gate off → nothing mapped, same shape as `Features:AutomationMcp`), *Projection style* (thin argument-mappers; no business rule in Web), *Problem details* (the thirteen `urn:pegasus:problem:` slugs, `correlationId` always present, no payload dumps), *Correlation & client version*. |
| `docs/desktop/03-gateway-api-and-data/README.md:249-286` (§ 7 Risks and traps) | "Composition gate off = 404. Tests must assert the gate both ways"; "Two policy engines … any rule that appears in an endpoint filter is a defect"; "adding bearer authentication must not change the cookie scheme's defaults (`__Host-Pegasus`, `SameSite=Strict`)"; "`Pegasus.Web` still publishes `linux-x64`". |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` (`ProjectReferencesFollowTheModularMonolithDirection`) | That `Pegasus.Web`'s project-reference list is asserted exactly. Adding `Pegasus.Contracts` to `src/Pegasus.Web/Pegasus.Web.csproj` breaks that fact until the expected array is updated in the same change. |
| `docs/current-architecture.md:520` | The precedent sentence for a gated surface — what `Features:AutomationMcp` prose looks like, including the closing rule that "source inventory must not be mistaken for deployed inventory". Mirror its shape for `Features:DesktopGateway`. |

## Ripple effects

- **The scoped exception-handler branch is the ripple that decides the ticket.**
  `AddExceptionHandler` puts an `IExceptionHandler` in DI; `UseExceptionHandler` is what invokes it,
  and today it exists only for non-Development (`Program.cs:754-756`). The pipeline therefore gains
  `app.UseWhen(context => context.Request.Path.StartsWithSegments(DesktopGateway.BasePath), branch => branch.UseExceptionHandler(new ExceptionHandlerOptions()))`,
  registered when the gate is open, in **every** environment. Do not relocate or unguard
  `UseExceptionHandler("/Error")`: that changes Razor error behaviour and is outside the Guardrails.
- **`IsMachineSurface` (`Program.cs:973-977`) gains `/api/v1`.** Without it,
  `UseStatusCodePagesWithReExecute` re-executes every API 4xx into `/status/{0}` HTML. One line; the
  largest single failure mode in this ticket.
- **The throwing endpoint for the seven problem facts.** Two mechanisms exist and the plan picks one:
  (a) a test-registered `IStartupFilter` added through
  `WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<IStartupFilter, …>))` that maps
  `/api/v1/__throw/{kind}` with `.AllowAnonymous()`, keeping production code free of test hooks; or
  (b) a production endpoint mapped only under the flag, which pollutes the real surface. **(a) is the
  choice**; if it proves unworkable on `WebApplication` minimal hosting, the fallback is to assert the
  mapping by unit-testing `DesktopGatewayProblems` directly and to keep only the composition facts as
  integration tests — recorded, not silently dropped.
- **`src/Pegasus.Web/Pegasus.Web.csproj`** gains a `ProjectReference` to `src/Pegasus.Contracts` if
  [[GWY-001]] did not already add it — and `ProjectReferencesFollowTheModularMonolithDirection` in
  `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` must be updated in the same change or
  the `unit` lane fails.
- **Existing Razor tests.** Adding `AddProblemDetails()` changes the default error-response shape for
  endpoints that do not opt out. `Pages/Error.cshtml.cs` and `Pages/StatusCode.cshtml.cs` must still
  render, and the whole existing `Pegasus.IntegrationTests` suite must stay green — the body makes that
  an acceptance criterion, and the `sql-integration` lane is where it is enforced.
- **Cookie scheme untouched.** `__Host-Pegasus` and `SameSite=Strict` defaults must not move; the
  route group adds no authentication scheme at this stage.
- **`openapi/pegasus-v1.json` — a *future* ripple, not a present one.** `ls openapi` → *No such file or
  directory*. [[GWY-004]] (plan handle `DSK-03-04`) creates the snapshot from this group's document and
  [[GWY-005]] (plan handle `DSK-03-05`) generates the Kiota client; from then on, every route or
  problem-shape change here changes both.
- **Downstream tickets.** `blocks` names thirteen: [[GWY-003]], [[GWY-004]], [[GWY-021]], [[FEAT-027]],
  [[FEAT-029]], [[FEAT-031]], [[FEAT-035]], [[FEAT-037]], [[FEAT-045]], [[DUI-010]], [[TEST-001]]
  (plan handle `DSK-08-01`), [[PLAT-014]], [[PLAT-018]]. The returned `RouteGroupBuilder` is the seam
  every one of them attaches to, which is why step 4 returns it rather than swallowing it.
- **Documentation.** `docs/current-architecture.md` only. `scripts/Test-DocumentationLinks.ps1` runs in
  the CI `documentation` lane and checks any link added.

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **The Azure app setting `Features__DesktopGateway=true` on the production Web Container App** —
  owned by [[PLAT-024]] (plan handle `DSK-11-06`) and by no other ticket, performed once at the
  Phase 2 release under exact-target approval. **No Azure write from this ticket.**
- **Bearer authentication, claims-to-actor resolution and the per-right endpoint filter** —
  [[GWY-021]] and [[GWY-003]]. No authentication scheme is added here.
- **The `X-Pegasus-Client-Version` check** — [[GWY-023]]. This ticket leaves a *named* extension point
  only, not an implementation and not a `TODO`.
- **Any endpoint under `/api/v1`** — the group is empty on purpose; endpoints begin at [[GWY-006]]
  (plan handle `DSK-03-06`).
- **`src/Pegasus.Core`, `src/Pegasus.Infrastructure`, `src/Pegasus.Web/Mcp/**` and every Razor page
  model** — untouched by the Guardrails. In particular `AutomationMcpErrors.cs` is *read and ported*,
  never edited or refactored into a shared helper: MCP and the API are two ingresses, and merging their
  error paths would couple them.
- **Any authorization or business rule inside the group or its filters** — "two policy engines" is a
  defect by area 03 § 7; filters map arguments and translate exceptions only.
- **Moving or unguarding `UseExceptionHandler("/Error")`** — refused; it would change Razor error
  behaviour and break existing tests.
- **Response compression, ETags and cancellation policy** — [[GWY-017]] (plan handle `DSK-03-17`).
- **Windows-only packages in `Pegasus.Web`** — refused; it still publishes `linux-x64` into the
  Playwright base image.
