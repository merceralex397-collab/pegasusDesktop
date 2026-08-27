# Research — GWY-004: the OpenAPI document, the committed snapshot, and the contract test that guards it

## Question

What does it take to turn `/api/v1` into a document that is genuinely *the contract* — served only
behind the gate, exported deterministically, committed, and guarded by a test that fails on an
unreviewed change — in a repository that has no OpenAPI tooling, no `openapi/` directory and no `eng/`
directory at all?

## Current behaviour

**No parity-matrix row covers this ticket, and none should.**
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted:
`grep -c '^| PAR-' …` returns **46** — each "keyed by the Razor page model and handler group that
implements it today" (`parity-matrix.md:3-5`). A build artefact and a snapshot test replace no handler
and give an operator no capability.

The closest existing repository mechanisms — what does this job today:

- **Nothing describes the HTTP surface machine-readably except the MCP tool list.** Area 03 § 2
  records that the only non-Razor surface is `GET /diagnostics/version` (`src/Pegasus.Web/Program.cs:954`),
  `/health/live` and `/health/ready` (`:939-950`), and the OpenIddict/MCP endpoints, with "**no OpenAPI
  document, no Swashbuckle/NSwag/Kiota, no controllers**". Measured today: `ls openapi` → *No such file
  or directory*; `grep -rn 'AddOpenApi\|Swashbuckle\|NSwag\|Kiota' src/` → no matches.
- **The nearest existing "committed artefact guarded by a test"** is the migration-grant census:
  `scripts/Test-MigrationGrants.ps1` runs in the CI `application` lane (`.github/workflows/ci.yml:58-60`)
  and `tests/Pegasus.IntegrationTests/AzureSqlRuntimeRoleMigrationTests.cs` asserts the same invariant
  from the test side. That is the shape this ticket copies: a generator, a committed output, and a test
  that fails when the two disagree.
- **The nearest existing "regeneration is a no-op" gate** is
  `scripts/Build-ProviderReferenceData.ps1` with the `reference-data` CI job
  (`.github/workflows/ci.yml:100-113`), which runs the generator's own unit tests. Neither uses
  `git diff --exit-code`; this ticket introduces that idiom.
- **PowerShell lives in `scripts/`, not `eng/`.** All twenty-one `.ps1` files sit directly under
  `scripts/` (`ls scripts/*.ps1`). `eng/api/` is a **new tree** introduced by the area-03 plan
  (`README.md:172` names `eng/api/Generate-ApiClient.ps1`), not an existing convention.

## Findings

### Facts

Read from the repository at fork `main`, 2026-08-24.

- **No `openapi/` directory, no `eng/` directory, no OpenAPI package.** `ls openapi` and `ls eng` both
  return *No such file or directory*. `src/Pegasus.Web/Pegasus.Web.csproj` (58 lines) has eight
  `PackageReference` entries — `Azure.Extensions.AspNetCore.DataProtection.Blobs` 1.5.3,
  `Azure.Identity` 1.21.0, `Microsoft.ApplicationInsights.AspNetCore` 2.23.0,
  `ModelContextProtocol.AspNetCore` 2.0.0, `OpenIddict.AspNetCore` 7.6.0,
  `System.Security.Cryptography.Xml` 10.0.10, `Microsoft.EntityFrameworkCore.Design` 10.0.10 —
  and none of them is `Microsoft.AspNetCore.OpenApi`.
- **`WebApplicationFactory<Program>` needs `InternalsVisibleTo`, and the new test project is not on the
  list.** `src/Pegasus.Web/Pegasus.Web.csproj` declares exactly two:
  `<InternalsVisibleTo Include="Pegasus.IntegrationTests" />` and
  `<InternalsVisibleTo Include="Pegasus.ArchitectureTests" />`. Minimal hosting emits an `internal`
  `Program` class, so **`tests/Pegasus.Api.ContractTests` cannot write `WebApplicationFactory<Program>`
  until a third entry is added**. Neither this ticket's body nor [[TEST-001]]'s (plan handle
  `DSK-08-01`) mentions it; this ticket's scope explicitly includes
  `src/Pegasus.Web/Pegasus.Web.csproj`, and [[TEST-001]]'s Guardrails say it "must not change
  `src/Pegasus.Web` behaviour" — an `InternalsVisibleTo` entry is not behaviour, but it is cleanest
  added here, where the file is already in scope. Whoever gets there first adds it; recording it is
  what stops both from missing it.
- **`Runtime:Profile` is mandatory and only two values are legal.** `src/Pegasus.Web/Program.cs:101-102`
  throws `"Runtime:Profile is required."` when it is absent; `:106-112` refuses `DevelopmentOffline`
  outside the Development environment; `:118-122` throws
  `$"Unsupported Runtime:Profile '{…}' for environment '{…}'."` for anything that is neither
  `DevelopmentOffline`-in-Development nor `Production`. Any host the snapshot test boots must therefore
  set `Runtime:Profile=DevelopmentOffline` **and** `UseEnvironment("Development")`, or fail before a
  document exists. `productionProfile` additionally requires `builder.Environment.IsProduction()`
  (`:123-125`).
- **The pinned test packages, measured.** `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj` (26
  lines): `net10.0`, `ImplicitUsings`, `Nullable`, `IsPackable=false`,
  `RestorePackagesWithLockFile=true`; `coverlet.collector` 6.0.4, `Microsoft.NET.Test.Sdk` 17.14.1,
  `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4; `<Using Include="Xunit" />`.
  `tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj:14` pins
  `Microsoft.AspNetCore.Mvc.Testing` **10.0.10** — the version a `WebApplicationFactory`-hosted contract
  project reuses.
- **A new test project's lock file breaks every CI lane until it is committed.**
  `.github/actions/dotnet-build/action.yml` runs `dotnet restore ./Pegasus.slnx --locked-mode` (step
  "Restore") and its `cache-dependency-path` globs `global.json`, `src/**/packages.lock.json` and
  `tests/**/packages.lock.json`. A missing or stale lock file fails the composite action, so every lane
  that uses it fails — not one job.
- **The `unit` job is `.github/workflows/ci.yml:131-147`** (the body cites `:130-146`; measured
  `:131-147`). Its `run:` is a `>`-folded, `&&`-chained pair at `:146-147`, with the comment at
  `:144-145` — "Chained, not two lines: pwsh reports only the last command's exit code, so a failing
  first project would otherwise pass the step." Its header comment at `:132-133` reads "**Both
  projects** run whole and unfiltered: neither declares a single test trait, so no filter can drop one
  of their tests." Appending a third project therefore requires editing that comment too, and it stops
  being true the moment the new project carries `[Trait("Category","Contract")]` — which
  [[TEST-001]] step 8 requires. The honest edit says three projects run, and that the third declares a
  `Contract` trait but is still run unfiltered here.
- **C-01 is a live cost constraint on this decision.** `docs/desktop/README.md` § Constraints: the
  repositories become private once the conversion completes, and "private-repository Windows runners
  bill at a 2× multiplier against a monthly allowance". Every job in `ci.yml` already runs
  `windows-latest`. Appending to the existing `unit` job rather than adding a job is therefore a
  recorded cost decision, which is what the ticket body's step 10 encodes.
- **Ownership is settled between this ticket and [[TEST-001]], in both bodies, in opposite
  directions.** [[TEST-001]] owns `tests/Pegasus.Api.ContractTests` (the csproj, the `Pegasus.slnx`
  entry, `ContractTestWebApplicationFactory.cs`, the `Contract` trait); **this ticket owns
  `OpenApiSnapshotTests.cs` and its four normalisation rules** — stable property ordering, two-space
  indent, `\n` line endings, no server-specific host. [[TEST-001]] step 5 says so verbatim and restates
  the four rules "so the two cannot drift". Each body instructs `ls` first and extend-in-place second.
- **The `documentation` CI lane will object to a README beside the snapshot.**
  `.github/workflows/ci.yml:70-87` runs `scripts/Test-TestMarkdownPlacement.ps1`, which drives
  `scripts/Test-MarkdownPlacement.ps1` (`Test-TestMarkdownPlacement.ps1:7`) and whose canonical
  allowed set is `docs/prd/`, `docs/frd/`, `docs/adr/`, `docs/design/`, `docs/desktop/<area>/README.md`,
  `.design-sync/` and `design/planning-and-old-designs/` (`:75-86`). `openapi/README.md` is not
  allowed — which is exactly why the ticket body puts the regeneration instructions in the script and
  the test failure message.
- **`docs/index.md` is a one-row-per-question table** (`:7-31`) followed by an § Authority section
  (`:33-43`). The area-03 plan § 8 says it "already points at this plan set; add the OpenAPI file
  location when it exists" — so the change is one table row, not a section.
- **There is no `Directory.Packages.props`.** `ls Directory.Packages.props` → *No such file*.
  [[FND-027]] (plan handle `DSK-02-02`) introduces central package management. Until it lands, a
  version **belongs** in the csproj like the other eight; after it lands, a version in the csproj is
  the defect the body's step 3 names. Note `src/Pegasus.Web/Pegasus.Web.csproj` already sets
  `<NoWarn>$(NoWarn);NU1510</NoWarn>`, the CPM-adjacent warning, so the project is not naive about it.
- **The gate to hang the document off already exists in plan form.** [[GWY-002]]'s (plan handle
  `DSK-03-02`) `AddPegasusDesktopGateway` / `MapPegasusDesktopGateway` in
  `src/Pegasus.Web/Api/DesktopGatewayExtensions.cs` are where `AddOpenApi("v1", …)` and
  `MapOpenApi(...)` belong, so the document is absent on a deployment with the surface disabled — and
  the gate-off 404 fact [[GWY-002]] writes extends naturally to `/openapi/v1.json`.
- **At this ticket's stage the document has no paths.** [[GWY-002]] maps an empty group and endpoints
  begin at [[GWY-006]] (plan handle `DSK-03-06`). The first committed `openapi/pegasus-v1.json` is
  therefore mostly `components/schemas` — `PegasusProblem` and `PagedResult<T>` from step 5 — plus
  info and (if [[GWY-003]] (plan handle `DSK-03-03`) has landed) security schemes. That is a feature,
  not a problem: the snapshot mechanism is proven while the surface is small enough to read.

Official documentation to fetch (the body's step 2 requires the fetch and requires recording its date;
this document records the URL, and the implementer records the date of their own fetch):

- ASP.NET Core OpenAPI document generation — `AddOpenApi`, `MapOpenApi`, document names, document
  transformers: <https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi>

### Assumptions

- **A-GWY004-1 — [[TEST-001]] (plan handle `DSK-08-01`) has landed, so
  `tests/Pegasus.Api.ContractTests` exists with its csproj, `Pegasus.slnx` entry, committed
  `packages.lock.json` and `ContractTestWebApplicationFactory`.** *Confirms it*:
  `ls tests/Pegasus.Api.ContractTests`. *If wrong*: step 6 scaffolds the project from
  `tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj`'s property block plus
  `Microsoft.AspNetCore.Mvc.Testing` 10.0.10, registers it in `Pegasus.slnx`, generates and commits
  the lock file, and **records which case applied** — never a second project.
- **A-GWY004-2 — `Microsoft.AspNetCore.OpenApi` at .NET 10 produces a document deterministic enough
  that the four normalisation rules make regeneration byte-identical.** *Confirms it*: running
  `Export-OpenApiDocument.ps1` twice and `git diff --exit-code openapi/`. *If wrong* — for example if
  schema ordering follows reflection order rather than a stable sort — the normaliser must impose the
  ordering itself before writing, and that becomes part of the four rules rather than a reason to
  weaken the test to a semantic comparison.
- **A-GWY004-3 — [[FND-027]] (plan handle `DSK-02-02`) has landed, so the package version goes in
  `Directory.Packages.props` and not in the csproj.** *Confirms it*: `ls Directory.Packages.props`.
  *If wrong*: the version goes in `src/Pegasus.Web/Pegasus.Web.csproj` beside the other eight, which is
  correct **today**, and the plan records that [[FND-027]] will move it. The body's "a version in the
  csproj is a defect" is conditional on CPM existing.
- **A-GWY004-4 — a `WebApplicationFactory`-booted host can serve `/openapi/v1.json` without LocalDB.**
  The document is produced from endpoint metadata, not from data. *Confirms it*: the snapshot test
  running without a `[Trait("Category","SqlServer")]`-style database fixture. *If wrong*: the factory
  needs the LocalDB fixture and the test becomes materially slower, which matters because
  [[TEST-001]] step 4 explicitly asks for the factory to stay "free of LocalDB where a test does not
  need persistence".

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered. This ticket **does** place a
responsibility — a CI-checked build artefact — so the section is answered in full rather than omitted.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | The document is a build output of one repository, versioned in git. The committed `openapi/pegasus-v1.json` is the shared artefact, and git — not a service — is what several people read and update. |
| Unattended execution — must it run with every desktop closed? | **No** | Export runs on demand from `eng/api/Export-OpenApiDocument.ps1` and in CI on a pull request. Nothing schedules it, and the boot it performs is a test host that exits. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No** | The export boots a `Development` / `DevelopmentOffline` host with no provider credential and no Azure connection. The generated document carries schema and security-*scheme* names, never a secret. |
| Public callback — must an external service call a stable public endpoint? | **No** | `/openapi/v1.json` is served by the gateway for the desktop's own client generation and is gated by `Features:DesktopGateway`; no external service calls it. |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **Yes — and it lands on the GitHub Actions `unit` lane on the existing repository, not on any new host.** | The invariant "no contract change ships unreviewed" cannot be enforced by any single workstation: it is enforced where every change must pass, which is CI. The snapshot test and `PreviousSnapshotRemainsSatisfied` are that enforcement, and step 10 places them in the **existing** `unit` job (`.github/workflows/ci.yml:131-147`) rather than a new one, because C-01 makes a new private-repository Windows job a recurring 2×-billed cost. No Azure resource is involved. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | No measurement exists or is claimed. The lane choice is driven by C-01's cost constraint and by the fact that the `unit` lane already builds the solution, not by a benchmark. |

**Conclusion.** Five "no" and one "yes"; the "yes" names *where* the enforcement lives — the existing
GitHub Actions `unit` lane — and adds no job, no host and no Azure resource. The credential question
is genuinely "no" here: unlike a tag lane that signs with the production certificate, this lane boots
an offline development host and writes a JSON file.

## Implications

1. **The `InternalsVisibleTo` entry is a hard prerequisite that neither body names.** Minimal hosting's
   `Program` is `internal`, and `src/Pegasus.Web/Pegasus.Web.csproj` lists only
   `Pegasus.IntegrationTests` and `Pegasus.ArchitectureTests`. Without a third entry the snapshot test
   does not compile. This ticket already has that file in scope, so it is the natural place.
2. **The host the snapshot test boots must be configured or it throws before serving anything.**
   `Runtime:Profile=DevelopmentOffline` **plus** `UseEnvironment("Development")` **plus**
   `Features:DesktopGateway=true` — the first two because `Program.cs:101-122` refuses everything else,
   the third because step 4 puts the document inside the gate. All three are `ContractTestWebApplicationFactory`'s
   job ([[TEST-001]] step 4); if it is missing any, fix it there rather than in the test.
3. **"Regeneration is a no-op" is the load-bearing claim and it needs `git diff --exit-code`.** The
   repository has no existing idiom for it; the migration-grant census is the closest analogue but
   compares a census, not bytes. The four normalisation rules exist precisely so the byte comparison is
   meaningful, and this ticket owns them.
4. **The first snapshot is nearly empty, and that is the right time to build this.** With no endpoints
   yet ([[GWY-006]] onward add them), the committed document is essentially the `PegasusProblem` and
   `PagedResult<T>` schemas. A reviewer can read the whole file, and the "add a probe endpoint, watch
   the test go red" check [[TEST-001]] describes is trivially performable.
5. **`PreviousSnapshotRemainsSatisfied` needs a second committed file and a defined promotion moment.**
   `openapi/pegasus-v1.previous.json` is the *previously accepted* document; on the very first commit
   the two files are identical, and the fact passes vacuously — which is correct and should be stated,
   not hidden. When the previous file is promoted (and by whom) is a release-time decision that belongs
   with the minimum-client-version advance; area 03 § 7 *Pilot ring compatibility* is the authority and
   [[GWY-017]] (plan handle `DSK-03-17`) owns the compat-range test.
6. **The `unit` job's own comment becomes false when the third project is appended.** "Both projects
   run whole and unfiltered: neither declares a single test trait" — the contract project *will* carry
   `[Trait("Category","Contract")]`. Edit the comment in the same change; a stale comment about why the
   `&&` chain exists is exactly the kind of thing this repository writes comments to prevent.
7. **`eng/api/` is a new tree in a repository that keeps PowerShell in `scripts/`.** The area-03 plan
   names the path, so follow it — but say so in the plan, because a reviewer will reasonably ask why
   `scripts/Export-OpenApiDocument.ps1` was not used. The reason is that `eng/api/` groups the export
   and the Kiota generation ([[GWY-005]], plan handle `DSK-03-05`) as one API-tooling unit.

## Open questions

- None that must be answered before implementation. The one genuine decision — where the contract tests
  run in CI — is settled by C-01 and by the ticket body's step 10 (append to the existing `unit` job).
  Ownership of the shared project and of `OpenApiSnapshotTests.cs` is settled in both this ticket's and
  [[TEST-001]]'s Guardrails, in opposite directions, and each names the `ls` that decides which branch
  applies. The four assumptions each name the command inside this ticket's own steps that settles them.
  The promotion of `openapi/pegasus-v1.previous.json` is a scope boundary owned by [[GWY-017]] and area
  09, recorded in the plan's Risks section rather than opened as a question.

## Revalidation — 2026-08-27

The live board was refreshed before taking the ticket. GWY-004 was unclaimed,
GWY-002 was done, and the ticket was taken on
`task/openapi-snapshot` at `origin/dev` commit `c2939f7e7301b36d5c93eccff498550b76d9a87a`.
The implementation worktree is
`C:\Users\PC\Documents\GitHub\pegasus-worktrees\openapi-snapshot`.
No upstream remote or upstream synchronization was used.

The commands below rechecked the assumptions that were stale in the original
research:

- `Test-Path Directory.Packages.props` = **True**. FND-027's central package
  management has landed, so `Microsoft.AspNetCore.OpenApi` must be versioned
  there and referenced without a version in `Pegasus.Web.csproj`.
- `Test-Path tests/Pegasus.Api.ContractTests` = **True**. TEST-001 has landed
  and owns the existing project, solution entry, lock file, and
  `ContractTestWebApplicationFactory`; this ticket extends it and does not
  scaffold a second project.
- The existing factory already sets `UseEnvironment("Development")`,
  `Runtime:Profile=DevelopmentOffline`, and
  `Features:DesktopGateway=true`.
- `.github/workflows/ci.yml` already contains the TEST-001 contract test in
  the existing `unit` job and already describes three projects. GWY-004 must
  preserve that single-lane arrangement rather than append a duplicate command.

Microsoft Learn was searched first and the official pages were fetched again
on 2026-08-27:

- [Generate OpenAPI documents](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0)
  confirms .NET 10's `AddOpenApi("v1")`, the default
  `/openapi/{documentName}.json` route, runtime generation, and build-time
  generation through `Microsoft.Extensions.ApiDescription.Server`.
- [Customize OpenAPI documents](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/customize-openapi?view=aspnetcore-10.0)
  confirms document transformers and the .NET 10 schema-generation path through
  `GetOrCreateSchemaAsync`; it also confirms that document transformers can
  set `OpenApiDocument.Info`.

The live source still has an empty `DesktopGatewayExtensions` group and no
OpenAPI package or `InternalsVisibleTo` entry for the contract project. The
ticket therefore owns adding the central package version, the Web package
reference, the friend assembly, the gated OpenAPI composition, the transformer,
the snapshot/export assets, and the tests. The no-upstream and no-cloud
boundaries remain unchanged.
