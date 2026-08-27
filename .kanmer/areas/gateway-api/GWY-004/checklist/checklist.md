# Checklist — GWY-004

One box per plan step, in plan order. The last box produces `proof`.

- [x] Read `docs/desktop/03-gateway-api-and-data/README.md:172`, § 4 exit gate and § 7 *Pilot ring compatibility*, and [[TEST-001]]'s (plan handle `DSK-08-01`) ticket body via `get_item TEST-001` for the ownership split; call `get_doc_gates GWY-004`; `take_ticket` on branch `task/openapi-snapshot` from `origin/dev`.
- [x] `microsoft_docs_fetch` <https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi> to confirm the .NET 10 `AddOpenApi`/`MapOpenApi`, document-naming and document-transformer shapes, and record the fetch date in the research document.
- [x] Add `Microsoft.AspNetCore.OpenApi` (via `Directory.Packages.props` if [[FND-027]] (plan handle `DSK-02-02`) has landed, otherwise with a version in the csproj — record which) and `<InternalsVisibleTo Include="Pegasus.Api.ContractTests" />` to `src/Pegasus.Web/Pegasus.Web.csproj`; regenerate and commit `src/Pegasus.Web/packages.lock.json`.
- [x] Call `AddOpenApi("v1", …)` and `MapOpenApi("/openapi/{documentName}.json")` inside `AddPegasusDesktopGateway` / `MapPegasusDesktopGateway`, both within the `Features:DesktopGateway` gate; confirm `GET /diagnostics/version` does **not** appear in the produced document.
- [x] Create `src/Pegasus.Web/Api/OpenApiDocumentTransformer.cs` registering `PegasusProblem` and `PagedResult<T>` as components and setting title/version/description from the assembly product version — as a transformer, never as attributes on the Contracts DTOs.
- [x] `ls tests/Pegasus.Api.ContractTests`: extend the existing project if [[TEST-001]] has landed, otherwise scaffold it from `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj`'s property block plus `Microsoft.AspNetCore.Mvc.Testing` 10.0.10, register it in `Pegasus.slnx` and commit its lock file. Record which case applied. Never a second project.
- [x] Create `tests/Pegasus.Api.ContractTests/OpenApiSnapshotTests.cs` booting the factory with `Features:DesktopGateway=true`, `Runtime:Profile=DevelopmentOffline` and `UseEnvironment("Development")`, normalising under the four rules (stable property ordering, two-space indent, `\n` line endings, no server-specific host), comparing byte-for-byte with `openapi/pegasus-v1.json`, and failing with the exact regeneration command (writing `openapi/pegasus-v1.json.actual` when the snapshot is absent).
- [x] Create `eng/api/Export-OpenApiDocument.ps1` — new tree, comment-based help explaining why `eng/api/` rather than `scripts/`, regeneration instructions in that help, non-zero exit on failure — and commit the first generated `openapi/pegasus-v1.json`. No README beside it.
- [x] Commit `openapi/pegasus-v1.previous.json` and add the `PreviousSnapshotRemainsSatisfied` fact asserting every path, operation and required response property from it is still present, with a comment recording that it passes vacuously on this first commit.
- [x] Append the third `&&`-chained `dotnet test` for the contract project to the `unit` job at `.github/workflows/ci.yml:146-147` and correct the job comment at `:132-133`; add no new CI job (C-01).
- [x] Run `dotnet build Pegasus.slnx -c Release`, then `pwsh ./eng/api/Export-OpenApiDocument.ps1` and `git diff --exit-code openapi/` twice, then `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj -c Release`.
- [x] Add one row to the `docs/index.md` table (`:7-31`) naming `openapi/pegasus-v1.json`, and record in the plan document which branch steps 3 and 6 found.
- [x] Run the simplification pass over the branch diff and record findings and dispositions under a dated `## Simplification pass` heading in the plan document.
- [x] **Verification run (this box produces `proof`)** — capture, as tier-1 and tier-5 evidence: `pwsh ./eng/api/Export-OpenApiDocument.ps1 && git diff --exit-code openapi/` run twice with no output; the full contract-test run; `dotnet build Pegasus.slnx -c Release` showing `0 Warning(s)`; `dotnet restore ./Pegasus.slnx --locked-mode` clean; the **red-then-green** probe check (map `/api/v1/__probe`, watch `OpenApiSnapshotTests` fail naming the snapshot and the regeneration command, remove it, confirm green); a `GET /openapi/v1.json` returning 404 with the gate absent; and `grep -n 'diagnostics/version' openapi/pegasus-v1.json` returning no match.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)


## Progress notes

- 2026-08-27: Implemented the gated `/openapi/v1.json` document, explicit `PegasusProblem` and generated `PagedResult` components, stable snapshot/export tooling, and additive previous-snapshot checks.
- 2026-08-27: Verified red-then-green snapshot behavior with a temporary `/api/v1/__probe`; removed the probe after the expected failure named `openapi/pegasus-v1.json` and `pwsh ./eng/api/Export-OpenApiDocument.ps1`.
- 2026-08-27: Locked restore, Release build (0 warnings/0 errors), full solution contract filter (5 passing), deterministic export, current/previous snapshot byte equality, gate-off 404, `diagnostics/version` exclusion, documentation links, and Markdown-placement regression all passed.

- 2026-08-27: Exact-head CI run 33043859460 built successfully but unit failed during parallel contract-host startup (4 HTTP 500s after the runner timeout). Added a test-only non-parallel xUnit collection; repository-equivalent Core, Architecture, and Contract sequence then passed 935/935, 110/110, and 5/5.

- 2026-08-27: Replaced the insufficient class-level collection marker with assembly-level xUnit test serialization covering both host-backed test classes; exact local unit sequence passed 935/935, 110/110, and 5/5.

- 2026-08-27: Run 33045186858 still failed the unit contract host with HTTP 500 after serialization. Added test-only response-body diagnostics; local targeted build and contract suite remained green.

- 2026-08-27: CI run 33045552935 identified SQL error 4060 from DevelopmentOfflineAuthenticationHandler querying unavailable PegasusDevelopment. Added test-only no-op IAuthenticationService in the existing contract factory; targeted build and contract tests pass 5/5 locally.
