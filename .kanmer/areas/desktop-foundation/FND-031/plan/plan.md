# Plan — FND-031: Create `src/Pegasus.Desktop.Infrastructure` (HTTP pipeline, DPAPI credential store, bounded cache, diagnostics writer)

**Diff estimate: ~14 files, ~460 lines** (excluding the generated `packages.lock.json`).

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. Derived from the files
document, file by file: csproj ~18; `Api/PegasusRequestHandler.cs` ~55; `Api/PegasusHttpClientRegistration.cs` ~60;
`Api/GatewayOptions.cs` ~15; `Authentication/IDesktopCredentialStore.cs` ~15;
`Authentication/DpapiCredentialStore.cs` ~85; `Caching/BoundedSnapshotCache.cs` ~70;
`Diagnostics/IDiagnosticsWriter.cs` ~15; `Diagnostics/RollingFileDiagnosticsWriter.cs` ~95;
`Windows/PackageClientVersionProvider.cs` ~20; plus `Directory.Packages.props` +2,
`Pegasus.slnx` +1, `DependencyDirectionTests.cs` +1, `src/Pegasus.Desktop/Pegasus.Desktop.csproj` +1
and `docs/current-architecture.md` ~+4. The two unit tests land in
`tests/Pegasus.Desktop.ViewModelTests` and are counted against that project, not this estimate — see
the sequencing risk below.

## Approach

Put the four adapters behind interfaces in one boundary project that references **only** `Pegasus.Core`
and `Pegasus.Contracts`, and prove the boundary by the reference set rather than by discipline. The
rejected alternative is folding these types into `src/Pegasus.Desktop` itself: fewer projects, but the
desktop application assembly would then be the thing asserted to have no EF/Azure/ASP.NET reference,
and every view model would sit in the same assembly as the credential store — so a single careless
`using` becomes invisible to any project-level test. Proposal § 5.3 fixes the direction inward and
plan 02 § 4 names this project explicitly with its two permitted references; a separate assembly is
what makes `ProjectReferencesFollowTheModularMonolithDirection` able to say anything at all about the
desktop.

Two measured facts shape the plan beyond the body's instructions:

- **The desktop's forbidden-prefix list cannot be Core's.**
  `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:23-39` forbids `System.Net.Http` at
  `:33` — the one thing this project exists to do. The *shape* (`IsForbiddenCoreDependency` `:475`,
  `ForbiddenDirectDependencies` `:480`) is reusable; the list is not. [[FND-037]] (plan handle
  `DSK-02-12`) authors the desktop list; this plan records why so it is not "reused" by mistake.
- **A `PackageVersion` here can move the server restore.**
  `System.Security.Cryptography.ProtectedData` already resolves transitively at **9.0.4** in
  `src/Pegasus.Infrastructure/packages.lock.json:887-891`. Under central package management a lower
  pin would change `src/Pegasus.Infrastructure` and `src/Pegasus.Web` as a side effect of a desktop
  ticket, so the plan pins ≥ 9.0.4 and verifies the server lock files are byte-unchanged.

## Governing docs

The ticket's `refs` array is empty and `get_doc_gates FND-031` reports `docs_todo: true`, so there is
no linked PRD/FRD/ADR to meet today.

> **New ADR** — ADR-0104 (online-required; no offline replication; bounded local cache only) bounds
> what `Caching/BoundedSnapshotCache.cs` may hold. It is authored by [[FND-005]] (plan handle
> `DSK-00-05`) and also claimed by [[FND-026]] (plan handle `DSK-02-01`) — see [[FND-026]]'s plan for
> the ownership reconciliation. ADR-0107 (Box and DVLA/DVSA credentials stay behind the gateway; no
> long-lived provider secret in the package) is authored by [[FND-006]] (plan handle `DSK-00-06`), as
> is ADR-0109 (desktop diagnostics bundle plus the existing Application Insights, no new telemetry
> fleet), which bounds the diagnostics writer.
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table) and
> `docs/desktop/02-architecture-and-foundation/README.md` § 3 decisions 6 and 7; if any of them lands
> differently this plan is revised before implementation.

Because `refs` is empty, these are the authorities that actually bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 5.3 Native desktop layers | Dependency direction inward; the desktop may not reference EF contexts, Azure SDK credentials or server integration implementations | Step 2 (two references only), step 10 (inspection) |
| Proposal § 7.2 Application composition | `IHttpClientFactory` plus a generated typed client, and a DPAPI-backed store for refresh tokens | Steps 4–6 |
| Proposal § 8.2, § 11.1 | The access token stays in memory and is never persisted | Step 6 |
| Proposal § 16.2 External provider resilience | Bounded retry behaviour at the client boundary | Step 5 |
| Plan 02 § 3 decision 6 | DPAPI `CurrentUser`, file-backed under the packaged app's local folder, **not** `PasswordVault`, with the reasoning from the Credential Locker guidance | Step 6 |
| Plan 02 § 3 decision 7 | One HTTP pipeline; structured logging to a bounded, redacting rolling sink | Steps 5, 8 |
| Plan 02 § 4 target-state table | `src/Pegasus.Desktop.Infrastructure` — `net10.0-windows10.0.26100.0`, referencing Core and Contracts only | Step 2 |
| Plan 03 § 3 *Correlation & client version* (`:168`) | `X-Correlation-Id` accepted or generated; `X-Pegasus-Client-Version` on every `/api/v1` request | Step 4 |
| Plan 03 § 3 *Retry* (`:173`) | Idempotent `GET`s only, bounded and jittered; commands never retried automatically | Step 5 |
| Plan 04 § 3 item 8 | The package carries no secret | Steps 2–8; nothing secret is modelled or embedded |
| L-01 (locked) | The desktop talks only to the evolved `Pegasus.Web` gateway, never to the database | Step 5 (`BaseAddress` from options; no data-access type anywhere) |
| **D-002 / D-003** (locked) | The whole distribution path is in-house and touches no Azure resource | No Azure SDK, credential or connection string enters this project |
| `AGENTS.md` § Simplicity rails | One list per concept | Step 4 uses `PegasusHeaders` constants, never literals |
| `docs/engineering.md` § Abstractions (`:113`) | No dormant scaffolding | Step 3 leaves `Documents/` out |
| `docs/engineering.md` § Required evidence tiers (`:72`), tier 2 | Positive, failure and boundary cases — not a compiling project | § Verification |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml` (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`, win-dev-skills v0.5.0 `f1028dd5`) →
  `microsoft-code-reference` (Microsoft Learn plugin).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search` for
  `ProtectedData.Protect DataProtectionScope.CurrentUser`,
  `IHttpClientFactory AddHttpClient DelegatingHandler`, `ApplicationData.Current.LocalFolder`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move;
  a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's twelve steps: same order, same ownership, same paths.

1. **Orient.** Read `src/Pegasus.Contracts/PegasusHeaders.cs` (created by [[FND-029]], plan handle
   `DSK-02-04`) and `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:23-39`. Note while
   reading that `:33` forbids `System.Net.Http` for **Core** — that list is not reusable for the
   desktop. Then `get_doc_gates FND-031` and `take_ticket` on branch `task/desktop-infrastructure`
   from `origin/dev`.
2. **Create the csproj.** `Microsoft.NET.Sdk`, `<TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>`,
   `<TargetPlatformMinVersion>10.0.22000.0</TargetPlatformMinVersion>`, `<Platforms>x64</Platforms>`,
   `ImplicitUsings` and `Nullable` enabled, and exactly two `ProjectReference` entries —
   `..\Pegasus.Core\Pegasus.Core.csproj` and `..\Pegasus.Contracts\Pegasus.Contracts.csproj`. Add
   `System.Security.Cryptography.ProtectedData` and `Microsoft.Extensions.Http` to
   `Directory.Packages.props` and reference them here **without** version literals.
   **Pin `ProtectedData` at ≥ 9.0.4** — it already resolves transitively at that version in
   `src/Pegasus.Infrastructure/packages.lock.json:887-891`, and a lower central pin would change the
   server projects' restore. `Microsoft.Extensions.Http` is genuinely required: the repository's one
   `AddHttpClient` call (`src/Pegasus.Web/AiWork/ChannelAiHandOffTransport.cs:193`) gets it from
   `Microsoft.NET.Sdk.Web`, which this project does not use.
3. **Create only the folders with content.** From proposal § 5.4: `Api/`, `Authentication/`,
   `Caching/`, `Diagnostics/`, `Windows/`. **Do not create `Documents/`** — nothing fills it until
   area 05 and `docs/engineering.md` § Abstractions (`:113`) forbids dormant scaffolding.
4. **`Api/PegasusRequestHandler.cs`** — a `DelegatingHandler` that sets `PegasusHeaders.ClientVersion`
   from the package version and `PegasusHeaders.CorrelationId` to a fresh `Guid` **only when the
   caller has not supplied one** (a caller-supplied id must survive — area 03 `:168` says the gateway
   accepts or generates and echoes), and exposes the correlation id to the logger scope. Read the
   version through an injected `IClientVersionProvider` with a `Package.Current.Id.Version`
   implementation in `Windows/`, so the handler is testable without package identity. Use the
   Contracts constants; a string literal here is the "one list per concept" defect and the compiler
   cannot see it.
5. **`Api/PegasusHttpClientRegistration.cs`** — `AddPegasusApiClient(this IServiceCollection, Action<GatewayOptions>)`
   calling `AddHttpClient("pegasus")`, setting `BaseAddress` from options, adding the handler from
   step 4, and configuring a bounded jittered retry **scoped to idempotent `GET` requests only**.
   Attaching a policy to the named client as a whole would silently retry commands — area 03 `:173`
   forbids that ("commands are never retried automatically"). Assert the rule in a code comment and in
   the test [[FND-038]] (plan handle `DSK-02-13`) adds. Mirror the registration shape at
   `ChannelAiHandOffTransport.cs:193`, but reference nothing in `AiWork/` — it is the gated
   `Features:SendToAi` surface, out of parity scope.
6. **`Authentication/IDesktopCredentialStore.cs` and `DpapiCredentialStore.cs`** — `Save(string key, string value)`,
   `TryRead(string key, out string? value)`, `Clear(string key)`; the implementation calls
   `ProtectedData.Protect`/`Unprotect` at `DataProtectionScope.CurrentUser` and writes one file per key
   under an injected `storeRoot`. The constructor takes `string storeRoot`: the app passes
   `ApplicationData.Current.LocalFolder.Path`, tests pass a temporary directory. **Never** store the
   access token — it stays in memory (proposal § 8.2, § 11.1). A corrupted blob must surface as a
   store failure, not as a garbage value (see § Verification).
7. **`Caching/BoundedSnapshotCache.cs`** — in-memory, explicit entry-count cap and per-entry expiry,
   holding only what ADR-0104 permits: small reference-data snapshots, thumbnails, the last
   compatibility response. No file-backed store and no SQLite; a durable cache needs the profiling
   evidence proposal § 11.2 demands.
8. **`Diagnostics/IDiagnosticsWriter.cs` plus a rolling-file implementation** — total-size cap,
   retention count, and a redaction hook that removes bearer tokens, refresh tokens and password
   fields **before** a line is written. The interface has two real callers already named — [[FND-032]]
   (plan handle `DSK-02-07`) wires it into the logging pipeline and [[FND-036]] (plan handle
   `DSK-02-11`) packages its output — which is what makes it a permitted abstraction under
   `docs/engineering.md` § Abstractions rather than dormant scaffolding. Do **not** build the bundle
   here.
9. **Register the project.** Add it to `Pegasus.slnx`; keep it **out** of the server entry point from
   [[FND-028]] (plan handle `DSK-02-03`); extend the ordinal expected array at
   `DependencyDirectionTests.cs:137-149` (it sorts immediately after
   `src/Pegasus.Desktop/Pegasus.Desktop.csproj`); and add a `ProjectReference` from
   `src/Pegasus.Desktop/Pegasus.Desktop.csproj` to this project.
10. **Verify the boundary by inspection now.**
    `grep -rn 'EntityFrameworkCore\|Azure\.\|Box\.\|Microsoft.Graph\|Microsoft.AspNetCore\|Pegasus.Infrastructure' src/Pegasus.Desktop.Infrastructure`
    must return nothing. State plainly in the proof that this is an **inspection**, not enforcement —
    [[FND-037]] (plan handle `DSK-02-12`) turns it into a test, and it will need a **new** desktop
    prefix list because Core's forbids `System.Net.Http`.
11. **Land the unit tests in `tests/Pegasus.Desktop.ViewModelTests`.** That project has the Windows
    target framework `ProtectedData` and `ApplicationData` need; `tests/Pegasus.ArchitectureTests`
    targets `net10.0` and cannot host them. The project does not exist yet — `ls tests` returns only
    the three existing ones — and [[FND-038]]'s dependency arrow points **at** this ticket. Sequence
    [[FND-038]] first and **record the sequencing in this document**; do not duplicate the test
    scaffold here (that is the third-copy failure `docs/engineering.md` § One Core owner names).
12. **Restore, build and close.**
    `dotnet restore ./src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj -r win-x64 --force-evaluate`,
    commit the lock file, then `dotnet restore ./Pegasus.slnx --locked-mode` and
    `dotnet build ./Pegasus.slnx --configuration Release`. Expected: exit 0, zero warnings. Add the
    project and its two permitted references to `docs/current-architecture.md` § Components and
    dependency direction (`:55`). Run the simplification pass, record it under a dated heading below,
    and open the PR into `dev`.

## Verification

Evidence tier **2 — Core/domain** (`docs/engineering.md` § Required evidence tiers, `:72`), as the
ticket body states: positive, failure **and** boundary cases for the credential store and the header
handler — not merely a compiling project.

The `proof` document is produced from these:

1. `dotnet build ./Pegasus.slnx --configuration Release` — expected exit 0 with `0 Warning(s)` (the
   command CI runs, `.github/actions/dotnet-build/action.yml:22-27`).
2. `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`
   — expected to cover, at minimum:
   - **Credential store, positive**: save → read returns the value → clear → read returns `false`.
   - **Credential store, missing key**: `TryRead` on a never-written key returns `false` and does not
     throw.
   - **Credential store, corrupted blob**: write a file of arbitrary bytes under `storeRoot`, then
     `TryRead` — expected a store-level failure surfaced as such, never a garbage value.
   - **Credential store, isolation**: two different `storeRoot` directories do not see each other's
     keys.
   - **Handler, headers present**: a request through the pipeline carries both
     `X-Pegasus-Client-Version` and `X-Correlation-Id`.
   - **Handler, caller-supplied correlation id preserved**: a request that already carries
     `X-Correlation-Id` keeps its value.
   - **Retry, asymmetry**: a failing `GET` is retried within the bound; a failing `POST` is **not**
     retried.
3. `grep -rn 'EntityFrameworkCore\|Azure\.\|Box\.\|Microsoft.Graph\|Microsoft.AspNetCore\|Pegasus.Infrastructure' src/Pegasus.Desktop.Infrastructure`
   — expected: no matches.
4. Additionally, and not in the body:
   `grep -c 'ProjectReference' src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj`
   — expected exactly `2`. The reference **count** is the acceptance criterion, and a count is
   cheaper to check than a prose review.
5. Additionally: `git diff --stat src/Pegasus.Infrastructure/packages.lock.json src/Pegasus.Web/packages.lock.json`
   after the solution restore — expected: **no change**. If either moved, the `ProtectedData`
   `PackageVersion` has altered the server graph and must be raised, not accepted.

## Risks / open questions

- **Risk — the desktop boundary test reuses Core's forbidden list.**
  `ForbiddenCoreDependencyPrefixes` (`DependencyDirectionTests.cs:23-39`) contains `System.Net.Http`
  at `:33`. Reusing it unchanged would fail the desktop projects for using HTTP.
  *Mitigation*: recorded here and in this ticket's research so [[FND-037]] authors a separate list;
  the reusable part is the shape, not the array.
- **Risk — central package management moves the server restore.** `ProtectedData` resolves
  transitively at 9.0.4 in `src/Pegasus.Infrastructure/packages.lock.json:887-891`.
  *Mitigation*: pin ≥ 9.0.4 at step 2 and check the server lock files with § Verification item 5.
- **Risk — a redaction rule implemented twice.** [[FND-032]] wires the writer into logging and
  [[FND-036]] re-applies redaction at bundle collection. *Mitigation*: the hook is defined once, here,
  on `IDiagnosticsWriter`; both later tickets call it rather than re-implementing the regex set.
- **Sequencing, recorded not resolved — [[FND-038]] must land first.** The plan's dependency arrow
  points from [[FND-038]] to this ticket, but step 11's tests can only live in that project.
  *Mitigation*: the body requires the sequencing note; take it, and do not duplicate the scaffold.
  This is a scope boundary with a named owner, not an open question.
- **Risk — an MSIX upgrade invalidates the DPAPI store.** Assumption A-FND031-4. *Mitigation*: it is
  proven by the install/upgrade scenarios owned by [[FND-039]] (plan handle `DSK-02-14`) and area 08,
  not argued here; the symptom would be a silent sign-out on every upgrade.
- **Scope boundary, not an open question — the generated client.** [[GWY-005]] (plan handle
  `DSK-03-05`) writes into `Api/Generated/`; nothing generated is added here.
- **Scope boundary, not an open question — token acquisition and refresh.** Area 04, [[FND-043]]
  (plan handle `DSK-04-07`). This ticket supplies the store, not the flow.
- **Scope boundary, not an open question — the diagnostics bundle.** [[FND-036]].
- **No `open-questions` document is opened.** Nothing here needs an answer from outside the ticket
  before implementation begins; every assumption in the research names the command that settles it.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._

## Implementation checkpoint — 2026-08-28

Implemented the desktop infrastructure boundary on `task/desktop-infrastructure` from `origin/dev` `28ba13a4`: the Windows-targeted project and lock file, HTTP header/correlation handler with GET-only bounded jittered retry, DPAPI `CurrentUser` credential store, bounded in-memory snapshot cache, rolling redacted diagnostics writer, project/solution registrations, architecture solution-list fact, and current-architecture snapshot. The new project references only `Pegasus.Core` and `Pegasus.Contracts`; `Documents/` was not created. The two central package versions are conditioned to this desktop-infrastructure project so the existing server lock files remain byte-unchanged; the desktop project lock file records the new transitive graph.

The repository's `tests/Pegasus.Desktop.ViewModelTests` project does not yet exist. FND-038 owns that scaffold and is currently dependency-blocked by FND-031, while this ticket's acceptance requires its Windows behavior tests. No duplicate test project was created. The ticket cannot truthfully claim those repository test cases until that ownership/dependency contradiction is resolved; a temporary out-of-repository probe did pass DPAPI round-trip/clear/corruption handling, header injection with caller correlation preservation, cache expiry, and diagnostics redaction, but it is not repository proof.

## Simplification pass — 2026-08-28

- Reuse: used the existing `Pegasus.Contracts.PegasusHeaders` constants and the repository's `IHttpClientFactory` registration shape; no duplicate header vocabulary or Core policy was added.
- Scope: kept the implementation inside the ticket's named infrastructure boundary; omitted the empty `Documents/` folder, generated client, token flow, diagnostics bundle, Azure SDKs, durable cache, and test scaffold owned by other tickets.
- Simplicity: one small GET-only retry handler, one injected DPAPI store root, one bounded in-memory cache, and one rolling writer with a single redaction implementation. No new runtime, table, service, or compatibility path was introduced.
- Efficiency: cache operations are lock-protected and return defensive byte copies; diagnostics trims by retention count and total bytes before appending; no background worker or unbounded queue was introduced.
- Findings: the first build exposed an analyzer-enforced argument-guard form and a missing `System.Net` import; both were corrected. The architecture test exposed the actual ordinal sort (`Pegasus.Desktop.Infrastructure` sorts before `Pegasus.Desktop`), and the expected list was corrected. No unapplied behavior-preserving simplification finding remains.

## 2026-08-29 validation and sequencing checkpoint

- dotnet restore .\src\Pegasus.Desktop.Infrastructure\Pegasus.Desktop.Infrastructure.csproj -r win-x64 --force-evaluate passed.
- dotnet build .\Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal passed with 0 warnings and 0 errors.
- dotnet test .\tests\Pegasus.ArchitectureTests\Pegasus.ArchitectureTests.csproj --configuration Release --no-build --no-restore --verbosity minimal passed 121/121.
- The forbidden-reference scan over src/Pegasus.Desktop.Infrastructure returned no matches; git diff --check passed.
- The required tests/Pegasus.Desktop.ViewModelTests project is absent on this branch and origin/dev; FND-038 owns creating that Windows test scaffold. Creating a second scaffold here would violate the ticket scope and EPIC-003 context. The credential-store and handler tests therefore remain open FND-031 acceptance items and this ticket must not be marked done until those tests and merged-main proof exist. An independent reviewer is deciding whether this implementation may merge as the prerequisite that unblocks FND-038 while FND-031 remains incomplete.

\n\n## 2026-08-29 independent review remediation\n\n- Erdos independently reviewed c39ea6f and found the GET retry handler reused the same HttpRequestMessage across attempts. This was a correctness blocker because .NET requests are not guaranteed reusable after send.\n- Fixed in 879055551f30c23da6a69e7fda2f1078ae19990f by creating a fresh GET request for each retry, copying method-independent request metadata and headers, while leaving non-GET requests on the single-attempt path. The fix is committed and pushed to task/desktop-infrastructure.\n- The reviewer accepted the FND-031/FND-038 sequencing in principle: FND-038 owns the one shared Windows ViewModelTests scaffold; FND-031 remains partial/not Done until its credential, header, correlation, retry, and boundary tests run and merged-main proof is written.\n- Non-blocking review warnings were recorded for the generic credential-store API, cache byte-size limit, and redactor-hook contract; none is a current acceptance blocker, and no speculative expansion was added.

\n\n## 2026-08-29 exact-head CI remediation\n\n- PR #42 run 33261304295 failed before tests because locked restore reported NU1004: tests/Pegasus.Desktop.ViewModelTests referenced the newly wired Pegasus.Desktop.Infrastructure but its packages.lock.json was stale. The SQL coverage failure was a downstream missing-shard artifact caused by that restore failure.\n- Merged current origin/dev into task/desktop-infrastructure as adc7b9d2e2c0adfcb6b07a56ccad41f779e25f35, regenerated only tests/Pegasus.Desktop.ViewModelTests/packages.lock.json for the new project dependency, and committed it as 26aae2fae0c69e99d6dc4bf4bf6fcebfe2748055.\n- The branch is pushed and PR #42 now points to 26aae2fae0c69e99d6dc4bf4bf6fcebfe2748055. The fresh exact-head run is pending; no merge or Done claim is made.
