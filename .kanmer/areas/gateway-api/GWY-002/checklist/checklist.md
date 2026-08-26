# Checklist — GWY-002

One box per plan step, in plan order. The last box produces `proof`.

- [x] Read `docs/desktop/03-gateway-api-and-data/README.md:160-168` and § 7 (`:249-286`), `endpoint-map.md` § Conventions, `src/Pegasus.Web/Mcp/AutomationMcp.cs` whole, and `src/Pegasus.Web/Program.cs` at `:246`, `:517-520`, `:625-628`, `:750-756`, `:820`, `:874-900`, `:939-966`, `:969-977`; call `get_doc_gates GWY-002`; `take_ticket` on branch `task/desktop-gateway-group`, worktree `../pegasus-worktrees/desktop-gateway-group`, from `origin/dev`.
- [x] Confirm the .NET 10 shapes with `microsoft_docs_search` (`MapGroup` + endpoint filters; `AddProblemDetails`/`IExceptionHandler` RFC 9457); confirm `src/Pegasus.Contracts/ProblemDetails/PegasusProblemTypes.cs` exists and that `src/Pegasus.Web/Pegasus.Web.csproj` references `Pegasus.Contracts` — if not, add the reference and update the expected array in `ProjectReferencesFollowTheModularMonolithDirection`.
- [x] Create `src/Pegasus.Web/Api/DesktopGateway.cs`: `FeatureFlag = "Features:DesktopGateway"`, `BasePath = "/api/v1"`, and `DesktopGatewayOptions.TryCreate` returning `null` on a closed gate (no runtime-profile check).
- [x] Create `src/Pegasus.Web/Api/DesktopGatewayExtensions.cs`: `AddPegasusDesktopGateway` (registering `AddProblemDetails()` and `AddExceptionHandler<…>()`) and `MapPegasusDesktopGateway` returning the `RouteGroupBuilder` with the correlation filter and the named client-version extension point attached, and no endpoint of its own.
- [x] Make the five `src/Pegasus.Web/Program.cs` composition edits: options resolution beside `:246`; services beside `:625-628`; the branch-scoped `app.UseWhen(path.StartsWithSegments(DesktopGateway.BasePath), branch => branch.UseExceptionHandler(new ExceptionHandlerOptions()))` beside `:750-752`; `app.MapPegasusDesktopGateway()` after `:959-960`; and `DesktopGateway.BasePath` added to `IsMachineSurface` at `:973-977`.
- [x] Create `src/Pegasus.Web/Api/DesktopGatewayProblems.cs` with the seven branches in the order fixed by inheritance — authorization 403, version-conflict 409, lease-conflict 409, lease-expired 409, operation-conflict 409, then the combined validation 400, then cancellation rethrow and the generic 500 with no exception text — every body carrying `correlationId`.
- [x] Create `src/Pegasus.Web/Api/CorrelationIdEndpointFilter.cs`: accept `PegasusHeaders.CorrelationId` when ≤ 200 characters with no control characters, otherwise generate from `HttpContext.TraceIdentifier`; echo it, log it, expose it to the problem handler; register the named client-version extension point without implementing the check.
- [x] Confirm `AddProblemDetails()` is registered exactly once and that `Pages/Error.cshtml.cs` and `Pages/StatusCode.cshtml.cs` still render — the `IsMachineSurface` branch is what keeps the HTML and JSON worlds apart.
- [x] Create `tests/Pegasus.IntegrationTests/DesktopGatewayCompositionTests.cs`: `[Trait("Category", "SqlServer")]`, a theory over flag-absent and flag-`false` asserting 404 **and** no `api/v1` entry in `EndpointDataSource`, plus a gate-on fact supplied via `WithWebHostBuilder(b => b.UseSetting("Features:DesktopGateway", "true"))` asserting on `EndpointDataSource` rather than a status code.
- [x] Create `tests/Pegasus.IntegrationTests/DesktopGatewayProblemTests.cs`: a test-only `/api/v1/__throw/{kind}` endpoint mapped through an `IStartupFilter` registered from `WithWebHostBuilder`, seven facts asserting status, `type` URI and `correlationId`, and one further fact that an API 404 returns a problem body rather than `/status/404` HTML. If the `IStartupFilter` route proves unworkable, take the recorded fallback and write it into the plan.
- [x] Run `dotnet build Pegasus.slnx -c Release`, the `FullyQualifiedName~DesktopGateway` filtered test run, and the whole `Pegasus.IntegrationTests` project; then update `docs/current-architecture.md` § Current callers and entry points (`:92`, `:116`) with the `/api/v1` surface and the `Features:DesktopGateway` gate, mirroring the `Features:AutomationMcp` prose at `:520`.
- [x] Run the simplification pass over the branch diff and record findings and dispositions under a dated `## Simplification pass` heading in the plan document.
- [x] **Verification run (this box produces `proof`)** — capture, as tier-5 Web/API caller evidence: the `DesktopGatewayCompositionTests` run (both gate-off cases plus the gate-on `EndpointDataSource` fact); the `DesktopGatewayProblemTests` run (seven branches plus the not-HTML fact); `dotnet build Pegasus.slnx -c Release` showing `0 Warning(s)`; the whole `Pegasus.IntegrationTests` run proving Razor, OpenIddict and MCP are unchanged; the `Pegasus.ArchitectureTests` run; and `grep -rn 'Features:DesktopGateway\|"/api/v1"' src/Pegasus.Web/ --include=*.cs` showing each literal exactly once, in `DesktopGateway.cs`.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)

## Progress notes

- [x] Steps 1–2: repository orientation, gate check/take, Microsoft Learn API-shape verification, Contracts reference confirmation, and architecture-test update.
- [x] Steps 3–8: gateway constants/options, composition extensions, Program wiring, problem mapping, correlation/client-version filters, and Razor machine-surface guard implemented.
- [x] Focused DesktopGateway validation: 11 passed, 0 failed, 0 skipped.
- [x] Step 10 fallback recorded in the plan: direct handler tests cover all mappings because the production route group intentionally has no endpoint.
