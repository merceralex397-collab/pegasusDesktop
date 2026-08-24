# Checklist — GWY-017: gateway performance and resilience

- [ ] Read plan 03 § 3 rows *Compression* (`README.md:184`), *Concurrency* (`:181`) and *Retry* (`:187`), § 2 assumptions A-1 and A-4 (`:150-158`), § 4, and `docs/desktop/10-security-observability-performance/README.md`; run `get_doc_gates GWY-017` and `take_ticket`.
- [ ] `microsoft_docs_fetch` <https://learn.microsoft.com/aspnet/core/performance/response-compression>; confirm the .NET 10 `AddResponseCompression` shape, the default MIME-type set and the exclusion mechanism; append the fetch date to this ticket's `research` under **Facts**.
- [ ] Register `AddResponseCompression` in `src/Pegasus.Web/Program.cs` for exactly `application/json` and `application/problem+json` (replacing the default MIME set, not appending to it).
- [ ] Add the compression middleware stage after `UseRouting` (`Program.cs:796`) and before `UseRateLimiter` (`:819`), scoped to `/api/v1`; add a fact that a Razor page and a static asset are byte-identical before and after.
- [ ] Assert every byte endpoint from [[GWY-006]]…[[GWY-015]] — the `/api/v1` projections of the seven `return File(...)` handlers — is excluded from compression, so range offsets stay valid.
- [ ] Create `src/Pegasus.Web/Api/GatewayResilience.cs` with the single weak-`ETag` helper: generate `W/"<version>"` and answer `If-None-Match` with `304` and no body.
- [ ] Audit every read endpoint under `src/Pegasus.Web/Api/`; add the missing `ETag`/`If-None-Match` pairs **through the helper** and delete any per-endpoint variant an earlier ticket invented. Do not introduce `If-Match`.
- [ ] Give versionless read responses (lists, rail counts) a weak validator derived from the page's identifiers and versions.
- [ ] Audit every handler under `src/Pegasus.Web/Api/` for `CancellationToken` propagation into every Core call and every provider call; write the list of handlers that ignored it into the post-implementation report and fix all of them.
- [ ] Confirm cancellation still surfaces as `OperationCanceledException` rethrown unchanged (precedent `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:61-64`) and is not mapped onto a problem type.
- [ ] Add the named provider-timeout constants to `src/Pegasus.Web/Api/GatewayResilience.cs` — one each for DVLA/DVSA lookup, Graph read and Box operation — each with a one-line reason, following the `Directory.Build.props:10-17` pattern.
- [ ] Replace the three first-wins `TryAddSingleton(HttpClient)` registrations in `src/Pegasus.Infrastructure/DependencyInjection.cs` (`:531-534`, `:566-569`, `:608-611`) with `IHttpClientFactory` named clients per provider, and re-point the adapter registrations at `:536`, `:570`, `:589`, `:612`, `:617`.
- [ ] Add a `tests/Pegasus.Api.ContractTests` assertion that reads the generated OpenAPI document and fails if any non-`GET` operation is marked retryable.
- [ ] Confirm whether [[GWY-004]] emits `openapi/pegasus-v1.previous.json`; if not, create it from the last released contract and record in the report that a first run against a copied snapshot proves nothing yet.
- [ ] Add `tests/Pegasus.Api.ContractTests/PreviousContractRangeTests.cs`: exercise every operation in the previous snapshot against the current server for the supported client range and assert each response still validates against the **previous** schema; confirm a removed field fails it.
- [ ] Add the three cancellation facts to `tests/Pegasus.IntegrationTests/DesktopGatewayResilienceTests.cs`: cancelled list releases its database connection (asserted on the pool or the exception path, never a sleep), cancelled command writes nothing partial, cancellation surfaces unchanged.
- [ ] Add the three compression facts to the same file: JSON compressed, problem response compressed, PDF/image byte response not compressed.
- [ ] Run the `dotnet-counters` sample on the local production-mimicking stack with ten concurrent simulated clients per the `analyzing-dotnet-performance` skill; record the numbers in the post-implementation report with an explicit statement about assumption A-4 and no invented threshold.
- [ ] Regenerate and commit `openapi/pegasus-v1.json`, regenerate the Kiota client via `eng/api/Generate-ApiClient.ps1`, and confirm `git diff --exit-code` is clean after regeneration.
- [ ] Run the simplification pass over this branch's own diff and record findings and dispositions under a dated `## Simplification pass` heading in the `plan` document.
- [ ] Produce `proof`: run `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj -c Release` and `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayResilienceTests"`, plus `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj -c Release`; attach the test output (tier 5) and the `dotnet-counters` command log (tier 10) as the ticket's evidence.

## Progress notes
