# Files — GWY-017: gateway performance and resilience

Paths marked *(created by …)* do not exist on the working tree today; a named earlier ticket
creates them. Everything else was confirmed with `ls`/`grep` on the fork at
`task/desktop-plan-segmentation`, 2026-08-24.

## Where the change lands

| Path | Change | Notes |
| --- | --- | --- |
| `src/Pegasus.Web/Program.cs` | Add `builder.Services.AddResponseCompression(...)` in the composition block and the matching middleware stage in the pipeline | The pipeline today is `UseHttpsRedirection` (`:794`) → `UseRouting` (`:796`) → sign-in global limiter (`:797-817`) → `UseRateLimiter` (`:819`) → automation middleware (`:821-872`) → `UseAuthentication` (`:874`) → must-change-password redirect (`:875-898`) → `UseAuthorization` (`:899`). The compression stage goes after `UseRouting` so it can see the endpoint, and must be scoped so Razor Pages (`:959`) and `MapStaticAssets()` (`:952`) are unaffected (assumption A-GWY017-2) |
| `src/Pegasus.Web/Api/GatewayResilience.cs` *(new; folder created by [[GWY-002]], plan handle `DSK-03-02`)* | The single weak-`ETag` helper (`W/"<version>"` generation plus `If-None-Match` → `304`) and the named provider-timeout constants | One helper, referenced by every read endpoint; one constants class, referenced and never re-typed. This is the file that satisfies steps 4 and 6 |
| `src/Pegasus.Web/Api/**` *(created by [[GWY-002]]; filled by [[GWY-006]] `DSK-03-06` … [[GWY-015]] `DSK-03-15`)* | Audit every read handler for the `ETag`/`If-None-Match` pair and every handler for `CancellationToken` propagation; replace any per-endpoint `ETag` variant with the shared helper | Steps 4, 5 and 7. The number of files actually edited depends on what the audit finds — the plan's inventory states both the certain and the conditional counts |
| `src/Pegasus.Infrastructure/DependencyInjection.cs` | Replace the three first-wins `TryAddSingleton(static _ => new HttpClient { Timeout = TimeSpan.FromSeconds(100) })` registrations at `:531-534`, `:566-569`, `:608-611` with named clients per provider, each reading its timeout from the constants file | Step 6. **This path is outside the Guardrails' stated scope boundary** — see *Out of scope* below; the plan records the stretch explicitly for the reviewer |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]], plan handle `DSK-08-01`; snapshot by [[GWY-004]] `DSK-03-04`)* | New `PreviousContractRangeTests.cs`: run every operation described by `openapi/pegasus-v1.previous.json` against the current server and assert the response still validates against the previous schema | Step 8. A removed field or narrowed type must fail |
| `tests/Pegasus.IntegrationTests/DesktopGatewayResilienceTests.cs` *(new)* | Cancellation facts (steps 9) and compression facts (step 10) | The ticket's own Verification block names this exact filter: `--filter "FullyQualifiedName~DesktopGatewayResilienceTests"` |
| `openapi/pegasus-v1.json` *(created by [[GWY-004]])* | Regenerated and committed at step 12 | Behaviour-preserving on the wire, so the regeneration should be a no-op except where a read endpoint newly advertises `ETag`/`304` |
| `openapi/pegasus-v1.previous.json` *(established by [[GWY-004]]; confirm — see Ripple effects)* | Read-only input to step 8, unless [[GWY-004]] does not in fact emit it, in which case this ticket creates it from the last released contract | Confirm before writing the test, not after |

## Context files

What each file **tells the implementer** — the constraint, gotcha or precedent living in it.

| Path | What it tells you |
| --- | --- |
| `docs/desktop/03-gateway-api-and-data/README.md:184` (*Compression* row) | Compression is for `application/json` and `application/problem+json` **only**; bytes are excluded by decision, not by omission. This is the sentence step 3 implements literally |
| `docs/desktop/03-gateway-api-and-data/README.md:181` (*Concurrency* row) | The `ETag` format is already fixed: weak, `W/"<version>"`, returned alongside `version` in the body. **`If-Match` is explicitly not the concurrency mechanism** — Core's semantics are per aggregate and lease-aware. Do not "improve" this into `If-Match` |
| `docs/desktop/03-gateway-api-and-data/README.md:187` (*Retry* row) | Only idempotent `GET`s are retry-eligible; commands are never retried automatically. Step 7's assertion enforces this against the OpenAPI document |
| `docs/desktop/03-gateway-api-and-data/README.md:189` (*Bytes & uploads* row) | Byte endpoints stream with `Content-Length`, **range support**, and `ETag`. Range support is why compressing them is a correctness bug and not merely wasteful: compression changes the offsets a byte range refers to |
| `docs/desktop/03-gateway-api-and-data/README.md:150-158` (assumptions A-1, A-4) | A-1 (the existing Container App absorbs the JSON surface) and A-4 (Azure SQL S0 tolerates ten desktop clients polling) are the two assumptions the step 11 measurement is required to speak to. The ticket asks for an explicit statement about A-4 by name |
| `docs/desktop/03-gateway-api-and-data/README.md:296-298` (observability blind spot) | App Insights ingestion is capped at 0.1 GB/day (PLAT-034), so an API failure in production may leave no trace. That is why the measurement evidence must be captured locally and attached, never "looked up in production later" |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:18-23` (Conventions) | The **Idempotent?** column is the machine-readable form of the retry rule (`yes (key)` for operation-key replay, `GET` for natural idempotence) and the **Concurrency token** column already says which reads carry an `ETag`. Step 7 reads a column that exists; it does not invent an annotation |
| `docs/desktop/10-security-observability-performance/README.md:100`, `:143` | The performance baseline ([[PLAT-010]], plan handle `DSK-10-10`) "records it before any budget is treated as pass/fail", and budgets are measured on the recorded baseline workstation. **Do not invent a latency threshold** — the ticket's Guardrails repeat this |
| `src/Pegasus.Infrastructure/DependencyInjection.cs:531-534`, `:566-569`, `:608-611` | The exact defect step 6 fixes: three registrations of the same `HttpClient` service type through `TryAddSingleton`, each with `Timeout = TimeSpan.FromSeconds(100)`. `TryAddSingleton` is first-wins, so Box, Graph and DVLA/DVSA share one client and one timeout in any host that composes more than one |
| `Directory.Build.props:10-17` | The `PlaywrightVersion` property and its comment are the repository's stated pattern for a cross-cutting value with one source of truth, and the comment says *why* (a desynchronised pair broke a build — ADR-0028, DELIV-012). Step 6 copies the pattern for timeout constants. The ticket body cites `:16`; the property itself is at `:17` |
| `Directory.Build.props:6-8` | `Deterministic=true`, `AnalysisLevel=latest-recommended`, `TreatWarningsAsErrors=true`. Any package a new test file pulls in must clear this policy; lowering the policy is never the answer (plan 03 § 7, *`TreatWarningsAsErrors` + generated code*) |
| `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:61-64` | The repository's existing cancellation decision: `OperationCanceledException` is rethrown unchanged rather than collapsed into a transport error. Step 5's fixes must preserve that, not map cancellation onto a problem type |
| `src/Pegasus.Web/Presentation/RailCountsPageFilter.cs:42` | The one place outside `Program.cs` that reaches for `HttpContext.RequestAborted`. It is also the filter whose output [[GWY-006]] (`DSK-03-06`) must match, so it is both a cancellation precedent and a parity reference |
| `src/Pegasus.Web/Program.cs:794-899` | The current middleware order. Compression is inserted into a pipeline that already carries two rate limiters, an automation branch and a redirect middleware; anything added must not reorder them |
| `src/Pegasus.Web/Program.cs:939-954` | `/health/live`, `/health/ready` and `GET /diagnostics/version` — the only machine-shaped HTTP responses today, all `.AllowAnonymous()` and health checks `.ShortCircuit()`. They show the house style for a non-Razor endpoint and are the surface `PAR-45` covers |
| `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:125`, `:135`, `:143`, `:151` | The four conflict exceptions (`version`, `lease conflict`, `lease expired`, `operation-key conflict`) whose problem mapping [[GWY-002]] ported. Step 5 must not change how they surface — it only adds the cancellation token that lets a request stop before reaching them |
| `src/Pegasus.Infrastructure/Persistence/EfWorkflowConfigurationStore.cs:35-37` | The canonical store shape: an explicit `Serializable` transaction opened before the replay check. It is why "a cancelled command wrote nothing partial" is assertable in the database — the transaction rolls back with the cancellation |
| `tests/Pegasus.IntegrationTests/AutomationMcpTestSupport.cs:32-42` | How this repository builds a gated in-process test host: `factory.WithWebHostBuilder(...)` plus `builder.UseSetting("Features:…", "true")`. The `Features:DesktopGateway` gate is turned on the same way, and step 10's compression facts need a client against that host |
| `tests/Pegasus.IntegrationTests/StaffSignInSecurityTests.cs:20-30` | The LocalDB + `ConfiguredWebApplicationFactory` pattern with `[Trait("Category", "SqlServer")]` at `:11` — the trait CI shards on. A cancellation fact that touches the database inherits both |
| `.github/workflows/ci.yml:58-60` | The migration runtime-grant check runs on every change set. This ticket adds no migration, so the check should stay silent — if it speaks, something in scope drifted |

## Ripple effects

- **OpenAPI snapshot.** Adding `ETag` and `304` responses to read endpoints changes the
  documented response set, so `openapi/pegasus-v1.json` is regenerated and its snapshot test
  in `tests/Pegasus.Api.ContractTests` must be updated in the same commit (step 12). The plan
  03 rule holds: contract changes must stay **additive** until the minimum client version
  advances — adding a `304` is additive, removing a field is a contract-test failure by design.
- **Generated client.** `src/Pegasus.Desktop.Infrastructure/Api/Generated/` is produced from
  the snapshot by `eng/api/Generate-ApiClient.ps1` ([[GWY-005]], plan handle `DSK-03-05`) and
  the CI no-op check fails if regeneration changes the tree. A regenerated snapshot therefore
  obliges a client regeneration in the same branch, or the CI check breaks on the next change.
- **`openapi/pegasus-v1.previous.json`.** Confirm with [[GWY-004]]'s output before writing the
  compat-range test. If [[GWY-004]] emits only the current snapshot, this ticket creates the
  `previous` file and the compat-range test's first run is trivially green — which must be
  stated in the post-implementation report rather than presented as a passing compatibility
  guarantee.
- **Desktop retry policy.** [[FND-043]] (plan handle `DSK-04-07`) owns the desktop session
  client and its retry behaviour. Step 7's assertion is the server-side half of the same rule;
  if the assertion forces a change to how an endpoint is marked in the OpenAPI document, the
  generated client changes with it.
- **Provider adapters.** Named `HttpClient` registrations change how `BoxContentClient`
  (`DependencyInjection.cs:536-539`), `GraphMailClient` (`:571-574`),
  `DvlaDvsaProductionAdapter` (`:588-591`) and `GraphApprovedMailboxResolver` (`:612-615`)
  receive their client. Existing adapter tests under `tests/Pegasus.IntegrationTests` that
  compose these must keep passing unchanged — the timeout value changes, the injection shape
  changes, the adapter behaviour must not.
- **Worker host.** `src/Pegasus.Worker` composes the same
  `AddProductionExternalAdapters`/`AddProductionBoxCustody` extensions. A change from
  `TryAddSingleton(HttpClient)` to named clients is a change to a shared composition path, so
  the Worker's composition tests
  (`tests/Pegasus.ArchitectureTests/WorkerCompositionTests.cs`,
  `WorkerAzureClientCompositionTests.cs`) are in the blast radius and must be run.
- **Documentation.** None beyond the regenerated snapshot — the ticket says so explicitly.
  Performance budgets and their reporting stay owned by
  `docs/desktop/10-security-observability-performance/README.md`.
- **No migration, no grant.** This ticket adds no table and no write path, so
  `scripts/Test-MigrationGrants.ps1`, `scripts/Invoke-AzureDatabaseBootstrap.ps1` and the
  pinned census in `tests/Pegasus.IntegrationTests/IntakePersistenceIntegrationTests.cs:30-90`
  are untouched. If any of them needs editing, the change has left scope.

## Out of scope

Recording what the ticket's Guardrails already forbid, so the reviewer sees it was a decision.

- **Any Azure write, including a load-test resource.** L-02 fixes the measurement to the local
  production-mimicking stack and ADR-0014 stands. Asking for an Azure load-test resource is
  named in the Guardrails as out of bounds.
- **Any endpoint contract change.** The ticket is behaviour-preserving on the wire except for
  compression and conditional responses. New fields, renamed fields, changed status codes for
  existing outcomes: all out.
- **Inventing a latency threshold.** Evidence tier 10 obliges a concurrency measurement at the
  documented operator scale; `docs/engineering.md` § Required evidence tiers item 10 and the
  Guardrails both forbid inventing a release threshold without an explicit decision.
  [[PLAT-010]] owns the budget table.
- **Compressing byte responses.** Explicitly excluded — wasted CPU and broken range requests
  (F3 in `research`).
- **Fixing endpoints' business behaviour.** If the audit in steps 4, 5 or 7 finds a
  *behavioural* defect rather than a missing `ETag`/token/annotation, it is a finding for
  [[GWY-018]] (plan handle `DSK-03-18`), which owns the independent contract and authorization
  gap review, not a fix in this branch.
- **The desktop half of the retry and conditional-request rules.** [[FND-043]] owns the session
  client's bounded, jittered `GET` retry and any `If-None-Match` the desktop sends.
- **Touching Razor Pages behaviour.** Compression must leave `MapRazorPages()` (`Program.cs:959`)
  and `MapStaticAssets()` (`:952`) byte-identical; a change there is a regression, not a scope
  extension.
