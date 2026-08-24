# Checklist — GWY-005: DSK-03-05 · Kiota client generation script, tool pin, committed output and CI no-op check

- [ ] Orient. Read `docs/desktop/03-gateway-api-and-data/README.md` § 3 row *OpenAPI & client*, § 2 assumption A-3 and § 7, then `get_doc_gates <this ticket id>` and `take_ticket`.
- [ ] Use `microsoft_docs_fetch` on <https://learn.microsoft.com/openapi/kiota/quickstarts/dotnet> and confirm the current tool package id, the `kiota generate` argument names (`--openapi`, `--language`, `--class-name`, `--namespace-name`, `--output`, `--clean-output`) and the runtime packages the generated code needs. Record the fetch date in the ticket research document.
- [ ] Pin the tool in `.config/dotnet-tools.json` with an exact version and `"rollForward": false`, matching the `dotnet-ef` entry already there. Run `dotnet tool restore` and confirm it resolves offline-repeatably.
- [ ] Add `eng/api/Generate-ApiClient.ps1` that runs `dotnet tool run kiota generate --openapi ./openapi/pegasus-v1.json --language CSharp --class-name PegasusApiClient --namespace-name Pegasus.Desktop.Infrastructure.Api.Generated --output ./src/Pegasus.Desktop.Infrastructure/Api/Generated --clean-output`, then normalises line endings, and exits non-zero on any failure. Keep it beside `eng/api/Export-OpenApiDocument.ps1` from [[DSK-03-04]].
- [ ] Run the script and commit the generated tree. Add the Kiota runtime package references (`Microsoft.Kiota.Abstractions` and the serialization packages the quickstart lists) to `src/Pegasus.Desktop.Infrastructure` through central package management.
- [ ] Build the desktop infrastructure project. If analyzer warnings from generated code break `TreatWarningsAsErrors`, add `src/Pegasus.Desktop.Infrastructure/Api/Generated/Directory.Build.props` that disables analysis for that folder only (`<EnableNETAnalyzers>false</EnableNETAnalyzers>`, `<NoWarn>` for the specific ids). Never lower the repository-wide policy in the root `Directory.Build.props` — this is the § 7 trap, and A-3 exists precisely to be resolved here. Record which option was taken and why in the ticket plan.
- [ ] Assert the boundary: extend `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` with a fact that the desktop UI project references the generated client only through `Pegasus.Desktop.Infrastructure`, and that no hand-written type in the desktop duplicates a generated DTO name (proposal § 10.3 "prevent handwritten duplicate DTOs").
- [ ] Add the no-op check to the existing `unit` job in `.github/workflows/ci.yml`: a step that runs `dotnet tool restore`, `pwsh ./eng/api/Generate-ApiClient.ps1`, then `git diff --exit-code src/Pegasus.Desktop.Infrastructure/Api/Generated`. Done means the job fails with a readable message naming the regeneration command when the tree differs.
- [ ] Verify determinism: run the script twice in a clean worktree and confirm the second run produces no diff. If Kiota emits a timestamp or machine-specific comment, strip it in the script's normalisation step rather than accepting a dirty tree.
- [ ] Document the regeneration contract inside `eng/api/Generate-ApiClient.ps1` as a comment header (not a new `.md` — the CI `documentation` job rejects Markdown outside the allowed roots).
- [ ] Run `dotnet build Pegasus.slnx -c Release` and the architecture tests. Done means green with zero warnings.
- [ ] Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the ticket plan.
- [ ] Kiota is pinned in `.config/dotnet-tools.json` with `rollForward: false`.
- [ ] `eng/api/Generate-ApiClient.ps1` regenerates the client deterministically; a second run leaves no diff.
- [ ] The generated client is committed and compiles under `TreatWarningsAsErrors` without changing the repository-wide analysis policy.
- [ ] CI fails when the committed generated tree does not match the current OpenAPI document.
- [ ] The desktop references only the generated client; no hand-written duplicate DTO exists.
- [ ] `dotnet tool restore && pwsh ./eng/api/Generate-ApiClient.ps1 && git diff --exit-code src/Pegasus.Desktop.Infrastructure/Api/Generated` — expected: exit code 0, no output.
- [ ] `dotnet build Pegasus.slnx -c Release` — expected: `Build succeeded`, `0 Warning(s)`.

## Progress notes

No implementation has started. This checklist is derived from the ticket’s accepted scope and is maintained by the ticket implementer.
