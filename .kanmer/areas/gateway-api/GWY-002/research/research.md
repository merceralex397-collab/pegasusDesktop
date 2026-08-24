# Research — GWY-002: the `/api/v1` route-group skeleton, its gate, its problems and its correlation id

## Question

What must a versioned `/api/v1` route group do to coexist with the existing `Pegasus.Web` pipeline —
which already has a global fallback authorization policy, an HTML status-page re-execute branch and an
exception handler registered only outside Development — so that the gate is genuinely closed when off,
and so that a problem response actually reaches the caller as RFC 9457 JSON rather than an HTML card
or a redirect?

## Current behaviour

**No parity-matrix row covers this ticket, and none should.**
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted:
`grep -c '^| PAR-' …` returns **46** — and every row is "keyed by the Razor page model and handler
group that implements it today" (`parity-matrix.md:3-5`). A route-group skeleton replaces no handler
and delivers no operator capability; it is the substrate the later `GWY` tickets project handlers
onto. The endpoint map says the same by omission: the group itself appears nowhere in
`docs/desktop/03-gateway-api-and-data/endpoint-map.md`, only the endpoints it will carry.

The closest existing repository mechanism is **the gated Automation MCP ingress**, and it is a close
one — it is the same problem solved once already:

- `src/Pegasus.Web/Mcp/AutomationMcp.cs:12` — `public const string FeatureFlag = "Features:AutomationMcp";`
  in a `public static class AutomationMcp` of fixed names (flag, scheme, policies, paths, scopes,
  lifetimes). `AutomationMcpOptions.TryCreate(IConfiguration)` returns **`null`** when
  `configuration.GetValue<bool>(AutomationMcp.FeatureFlag)` is false and otherwise validates every
  required setting, throwing `InvalidOperationException` on a bad one. That is the exact
  "gate closed = absent, gate open = validated" shape step 3 must copy.
- `src/Pegasus.Web/Program.cs:246` resolves `var automationMcpOptions = AutomationMcpOptions.TryCreate(builder.Configuration);`
  once, at the top of the builder section; `:625-628` adds services only when it is non-null; `:820`
  adds middleware only when it is non-null; `:961-964` maps only when it is non-null.
- `src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs:131-140` — `MapPegasusAutomationMcp(this WebApplication app)`
  maps the token endpoint and `/mcp` and nothing else. The "map only when composed" precedent.
- `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:19-67` — the exception taxonomy to port, with the
  message discipline stated in its own XML summary at `:7-15`.

## Findings

### Facts

Read from the repository at fork `main`, 2026-08-24; each carries its path and line.

- **`src/Pegasus.Web/Api` does not exist.** `ls src/Pegasus.Web` returns `AiWork`, `Authentication`,
  `Data`, `Health`, `Mcp`, `Pages`, `Presentation`, `Program.cs`, `Properties`, `appsettings*.json`,
  `wwwroot`, `Pegasus.Web.csproj`, `packages.lock.json`. Every file in step 3–7 is new.
- **`Program.cs` is 1,216 lines and the mapping region is `:939-964`**, exactly as the body cites:
  `MapHealthChecks("/health/live")` `:939-944`, `MapHealthChecks("/health/ready")` `:945-950`,
  `MapStaticAssets()` `:952-953`, `MapGet("/diagnostics/version")` `:954-958`, `MapRazorPages()`
  `:959-960`, and the gated `if (automationMcpOptions is not null) { app.MapPegasusAutomationMcp(); }`
  at `:961-964`. `app.Run()` is `:966`.
- **There is no `AddProblemDetails` anywhere in the solution.**
  `grep -rn 'AddProblemDetails\|AddExceptionHandler\|IExceptionHandler' src/` returns exactly one hit,
  and it is not a registration: `src/Pegasus.Web/Pages/Error.cshtml.cs:33` reads
  `HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path`. Step 8's "only if it is not
  already registered" resolves to "add it".
- **`UseExceptionHandler` is registered only outside Development.** `src/Pegasus.Web/Program.cs:754-756`:
  `if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error"); app.UseHsts(); … }`.
  This is load-bearing: `AddExceptionHandler` only *registers* an `IExceptionHandler` in DI — the
  `UseExceptionHandler` middleware is what invokes it. Without a scoped registration of that
  middleware, the mapping written in step 6 would never execute in Development.
- **The integration tests run in Development.**
  `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs:26` declares
  `public sealed class IntakeWebApplicationFactory : WebApplicationFactory<Program>`; every
  parameterless and convenience constructor (`:42-62`) passes `"Development"`, and
  `ConfigureWebHost` (`:97-99`) calls `builder.UseEnvironment(environment)`. Combined with the fact
  above, **the seven problem-mapping facts step 10 requires would all fail on an unscoped
  `UseExceptionHandler`** — the single most likely way this ticket ships broken.
- **A global fallback authorization policy already applies to every endpoint.**
  `src/Pegasus.Web/Program.cs:517-520`:
  `AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())`.
  An `/api/v1` endpoint with no authorization metadata therefore challenges the default (cookie)
  scheme and returns a **302 to `/Account/SignIn`**, not a 401 and not a problem body. Bearer
  authentication for `/api/v1` is owned by [[GWY-021]] (plan handle `DSK-04-04`) and the per-right
  filter by [[GWY-003]] (plan handle `DSK-03-03`), so at this ticket's stage any endpoint mapped for
  testing must carry `.AllowAnonymous()`, and the composition assertion must be made on
  `EndpointDataSource`, not on a status code.
- **A password-change redirect middleware intercepts every non-`AllowAnonymous` endpoint.**
  `src/Pegasus.Web/Program.cs:875-899`, between `app.UseAuthentication()` (`:874`) and
  `app.UseAuthorization()` (`:900`): if the endpoint has no `IAllowAnonymous` metadata and the user is
  authenticated with `MustChangePassword == true`, it issues
  `context.Response.Redirect("/Account/PasswordChange")` unless the path starts with
  `/Account/PasswordChange`, `/Account/SignOut`, `/css`, `/js`, `/lib` or `/favicon.ico`. `/api/v1` is
  not on that list, so a desktop call by such a user would receive a 302 and an HTML page. The
  catalogue's `password-change-required` slug exists precisely for this case; converting the redirect
  into that problem is area 04's work ([[GWY-021]], [[GWY-022]] (plan handle `DSK-04-05`)), but this
  ticket must not design a pipeline that makes it impossible.
- **An HTML status-page re-execute branch already wraps most of the app, and `/api/v1` is not
  excluded from it.** `src/Pegasus.Web/Program.cs:750-752`:
  `app.UseWhen(context => !IsMachineSurface(context.Request.Path), branch => branch.UseStatusCodePagesWithReExecute("/status/{0}"));`
  and `IsMachineSurface` at `:973-977` covers only `/health`, `/diagnostics`,
  `AutomationMcp.McpEndpointPath` (`/mcp`) and `AutomationMcp.TokenEndpointPath` (`/connect/token`).
  Its own XML summary at `:969-972` states the rule: "Paths whose callers are programs, not people:
  they want a status code and a parsable body, and a re-executed HTML card would break them." An
  `/api/v1` 403 or 404 would today be re-executed into `/status/403` HTML. Extending `IsMachineSurface`
  with `DesktopGateway.BasePath` is therefore mandatory, and it is the change the body's step 8 points
  at when it says "see the `IsMachineSurface` re-execute guard".
- **The exception taxonomy to port, measured.** `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs`
  `ExecuteAsync` (`:19-67`) catches, in order: `McpException` (rethrow), `StaffAuthorizationException`,
  `CaseEditLeaseExpiredException` (message carries `exception.CaseVersion`),
  `CaseEditLeaseConflictException` (carries `CaseVersion`), `CaseVersionConflictException` (carries
  `ActualVersion` **and** `ExpectedVersion`), a combined
  `ArgumentException or InvalidOperationException or InvalidDataException` filter,
  `OperationCanceledException` (rethrow), and a final `Exception` collapsing to "The automation action
  failed." **`CaseOperationConflictException` is not caught by name** — it derives from
  `InvalidOperationException` (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:151`) so it lands in
  the 400-ish validation branch today. The body is right that the API must map it separately to
  `operation-conflict`, and the ordering consequence is concrete: **the `CaseOperationConflictException`
  branch must precede the `InvalidOperationException` branch**, or it is unreachable.
- **The same ordering trap applies to the three case exceptions.** All four in
  `CaseWorkflowContracts.cs` derive from `InvalidOperationException`:
  `CaseVersionConflictException` (`:125`), `CaseEditLeaseConflictException` (`:135`),
  `CaseEditLeaseExpiredException` (`:143`), `CaseOperationConflictException` (`:151`).
  `AutomationMcpErrors` already orders them before its combined filter; the port must preserve that.
- **The gate-both-ways test pattern exists and is exactly reusable.**
  `tests/Pegasus.IntegrationTests/LocalIntakeAccessTests.cs` (184 lines) is
  `[Trait("Category", "SqlServer")]` at `:10`; `DeniedConfigurations` at `:13-17` is a
  `TheoryData<string, bool?>` of `{ "Development", null }` and `{ "Development", false }` — absent
  **and** explicitly false; `:38-56` builds the factory, asserts 404 on each gated path, and at `:56`
  reads `factory.Services.GetRequiredService<EndpointDataSource>().Endpoints` to assert on the routing
  table rather than on a response. That last move is what makes a composition assertion immune to the
  authorization behaviour above.
- **The test factory has no `Features:DesktopGateway` knob.** `IntakeWebTestSupport.cs:97-141` sets
  only `Features:LocalIntake` and `Features:LocalDocumentCustody` (via both `UseSetting` at `:100-105`
  and an `AddInMemoryCollection` at `:109-141`), plus `Runtime:Profile`, the connection string and the
  `DocumentRequests:*` block. A new flag is therefore supplied per-test through
  `WithWebHostBuilder(builder => builder.UseSetting("Features:DesktopGateway", "true"))`, which needs
  no edit to the shared support file — and the factory does not set that key in its in-memory
  collection, so nothing overrides it.
- **CI lane placement.** `.github/workflows/ci.yml` § `sql-integration` (`:151-175`) runs
  `./scripts/Invoke-TestShard.ps1 … -Filter "Category!=Corpus&Category!=Browser"` across three shards.
  78 files in `tests/Pegasus.IntegrationTests` already carry `[Trait("Category", "SqlServer")]`.
  The lane selects by *exclusion*, so an untraited test would also run there — but the trait is the
  file-level convention and the body asks for it.
- **SDK and shapes.** `global.json` pins `"version": "10.0.302"` with `"rollForward": "latestFeature"`
  and `"allowPrerelease": false`. `Directory.Build.props:7-8` sets
  `AnalysisLevel=latest-recommended` and `TreatWarningsAsErrors=true`, so a nullable or analyzer
  warning in the new files is a build break.
- **`docs/current-architecture.md` § Current callers and entry points begins at `:92`**, with
  `### Staff Web callers` at `:94`, `### Technical entry points` at `:116`, `### Worker callers` at
  `:122`. The `Features:AutomationMcp` gate is described at `:520` and `Features:SendToAi` at `:522` —
  the precedent shape for the `Features:DesktopGateway` sentence the body's *Documentation changes*
  section requires **in this same task**.

Official documentation to confirm against before writing code (the body's step 2 requires the search;
the shapes below are the ones to verify for .NET 10, not to copy from memory):

- ASP.NET Core minimal-API route groups and endpoint filters —
  <https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/route-handlers>
- Error handling, `AddProblemDetails`, `IProblemDetailsService`, `IExceptionHandler` —
  <https://learn.microsoft.com/aspnet/core/fundamentals/error-handling>

### Assumptions

- **A-GWY002-1 — [[GWY-001]] (plan handle `DSK-03-01`) has landed, so `PegasusProblemTypes` and
  `PegasusProblem` exist in `src/Pegasus.Contracts` and `Pegasus.Web` may reference that project.**
  *Confirms it*: `ls src/Pegasus.Contracts/ProblemDetails/PegasusProblemTypes.cs` and a `grep` for the
  `Pegasus.Contracts` `ProjectReference` in `src/Pegasus.Web/Pegasus.Web.csproj`. *If wrong*: the
  problem mapping has no constants to use and the ticket is blocked — it must not declare a local copy
  of the slugs. Note that adding the `Pegasus.Web → Pegasus.Contracts` project reference also changes
  `ProjectReferencesFollowTheModularMonolithDirection` in
  `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs`, which asserts Web's exact reference
  list; if [[GWY-001]] did not add it, this ticket must.
- **A-GWY002-2 — the body's `ProblemTypes` is the type [[GWY-001]] calls `PegasusProblemTypes`.**
  The body's step 6 says "the `ProblemTypes` constants from `src/Pegasus.Contracts`"; [[GWY-001]]'s
  acceptance criteria forbid a type called `ProblemTypes` and pin
  `ProblemDetails/PegasusProblemTypes.cs`. *Confirms it*: read the file. *If wrong*: follow the file
  that exists — never introduce a second type to satisfy a prose reference.
- **A-GWY002-3 — a branch-scoped `UseExceptionHandler` invokes the `IExceptionHandler` registered by
  `AddExceptionHandler`, in every environment.** *Confirms it*: the seven step-10 facts passing under
  the Development-hosted factory. *If wrong*: the handler must instead be a plain middleware
  `try/catch` on the `/api/v1` branch, which is the fallback the plan records — never by moving
  `UseExceptionHandler("/Error")` out of its `!IsDevelopment()` guard, which would change Razor error
  behaviour and break the existing tests.
- **A-GWY002-4 — the `Features:DesktopGateway` flag needs no runtime-profile coupling.**
  `Features:LocalIntake` and `Features:SendToAi` both refuse outside `DevelopmentOffline`
  (`Program.cs:112-116`, `:655`, `:660`); the desktop gateway is the opposite — it is meant for
  production. *Confirms it*: area 03 § 3 *Composition gate* describes production enablement as a
  Container App app-setting change. *If wrong*: a profile check belongs in
  `DesktopGatewayOptions.TryCreate`, not scattered through `Program.cs`.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **Yes — on the existing evolved `Pegasus.Web` gateway, not on anything new.** | The route group is the ingress through which every desktop operator reaches the one authoritative case store; `src/Pegasus.Infrastructure/Persistence` and the Core use cases behind it are shared by definition. Locked decision L-01 (`docs/desktop/README.md` § Locked decisions) fixes the host as `Pegasus.Web` evolved in place — "same Container App, no new deployment unit" — so the responsibility lands on a process that already exists. |
| Unattended execution — must it run with every desktop closed? | **No** | The group serves requests; it initiates nothing. Unattended work stays in `Pegasus.Worker` (ADR-0106), which this ticket does not touch. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No, for this ticket.** | The skeleton composes no credential: `DesktopGatewayOptions` carries a boolean flag and a base path, unlike `AutomationMcpOptions`, which validates a client secret of at least 32 characters (`AutomationMcp.cs`, `TryCreate`). Provider secrets stay behind the gateway under ADR-0107, and the token path is area 04's. |
| Public callback — must an external service call a stable public endpoint? | **No** | Nothing external calls `/api/v1`; the desktop is the only client. The external-audience surfaces stay Razor (`Pages/Uploads/Request.cshtml.cs`, `endpoint-map.md` § Stays web-only). |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **Yes — again on the existing gateway process.** | The composition gate itself is central enforcement: with `Features:DesktopGateway` off, no desktop build of any version can reach the API, which is the rollback lever for the Phase 2 pilot. The problem-details mapping is the other half — refusals like `version-conflict` and `lease-conflict` originate in Core (`CaseWorkflowContracts.cs:125-157`) and must be stated by the server, never inferred by the client. ADR-0103 records "gateway, never direct database access from workstations". No Azure write arises here; the one app-setting change is owned by [[PLAT-020]]-class work in area 11, not by this ticket. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | No measurement exists or is claimed. Area 03 § 2 assumption A-1 explicitly defers the "can the Container App absorb the JSON surface" question to the area-10 performance baseline. Claiming a measured advantage here would be the dishonest answer this test exists to catch. |

**Conclusion.** Four "no" and two "yes"; both "yes" answers land on the **already-running
`Pegasus.Web` Container App** under L-01, and neither creates a new Azure resource or a new deployment
unit. The only Azure action the area implies is a one-off app-setting change at the Phase 2 release,
which this ticket's Guardrails explicitly assign elsewhere.

## Implications

1. **The composition gate has three registration points, not one.** `AutomationMcpOptions` is resolved
   once at `Program.cs:246` and consulted at `:625` (services), `:820` (middleware) and `:961`
   (mapping). The desktop gateway needs the same three: services (problem details + the exception
   handler), middleware (the scoped exception-handler branch and the correlation filter's pipeline
   dependencies), and mapping. Registering only at the mapping site leaves a half-composed feature.
2. **The scoped `UseExceptionHandler` is the crux of the whole ticket.** Because `UseExceptionHandler`
   is inside `if (!app.Environment.IsDevelopment())` (`:754-756`) and every integration test runs
   Development, an `AddExceptionHandler`-only implementation compiles, ships and is silently dead
   under test. The pipeline must add a branch — `app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments(DesktopGateway.BasePath), branch => branch.UseExceptionHandler(new ExceptionHandlerOptions()))`
   — registered when the gate is open, in every environment. That satisfies the body's "scoped to the
   group" literally rather than by proximity.
3. **`IsMachineSurface` must gain `/api/v1`.** Otherwise `UseStatusCodePagesWithReExecute("/status/{0}")`
   (`:750-752`) turns every 4xx from the API into HTML, and the desktop's problem parsing sees a page.
   The function's own summary already states the rule this change satisfies; this is a one-line edit
   with an outsized failure mode.
4. **Exception-branch order is a correctness requirement, not style.** All four case exceptions derive
   from `InvalidOperationException`, so the combined validation branch must come **last** of the
   InvalidOperation-family branches. `CaseOperationConflictException` is the one MCP does not name
   today and is therefore the one most likely to be forgotten into the 400 bucket.
5. **The composition assertion belongs on `EndpointDataSource`, not on a status code.** With
   `SetFallbackPolicy(RequireAuthenticatedUser())` at `:517-520`, "gate on" produces a 302 to
   `/Account/SignIn` for an unauthenticated caller, which is indistinguishable from many other
   failures. `LocalIntakeAccessTests.cs:56` already shows the right move. The gate-**off** assertion
   can and should stay a 404 assertion, because a missing route genuinely 404s before authorization.
6. **`password-change-required` has a live cause today.** The middleware at `:875-899` will 302 an
   `/api/v1` request for a user with `MustChangePassword`. This ticket does not fix it — [[GWY-021]]
   and [[GWY-022]] own the bearer path — but the slug exists in the catalogue, and the group's design
   must leave room for that translation rather than assuming the redirect is harmless.
7. **A `Pegasus.Web → Pegasus.Contracts` project reference changes an architecture fact.**
   `ProjectReferencesFollowTheModularMonolithDirection` in
   `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` asserts Web's exact reference list;
   whoever adds the reference updates that assertion in the same change.

## Open questions

- None that must be answered before implementation. Every design choice above is settled by a measured
  repository fact or by a named plan row (`docs/desktop/03-gateway-api-and-data/README.md:160-168`),
  and the four assumptions each name the command inside this ticket's own steps that settles them. The
  two decisions that are *scope boundaries* rather than open questions — bearer authentication for
  `/api/v1`, and the production app-setting that opens the gate — are owned by [[GWY-021]] and by
  area 11 respectively and are recorded in the plan's Risks section, as the ticket's Guardrails
  instruct.
