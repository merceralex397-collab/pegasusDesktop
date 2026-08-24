# Research — FND-031: the desktop infrastructure boundary (HTTP pipeline, DPAPI store, bounded cache, diagnostics writer)

## Question

What must `src/Pegasus.Desktop.Infrastructure` contain, what may it *never* reference, and which of
its four responsibilities already have a working precedent in this repository that should be mirrored
rather than reinvented?

## Current behaviour

**No parity-matrix row covers this ticket, and none should.** The matrix at
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted with
`grep -c '^| PAR-'`, which returns **46** — and every row is "keyed by the Razor page model and
handler group that implements it today" (`parity-matrix.md:3-5`). An adapter assembly with no screen
has no row.

The closest existing repository mechanisms — what does each of the four jobs today:

- **Outbound HTTP with a typed pipeline**: exactly one registration exists in the whole repository —
  `src/Pegasus.Web/AiWork/ChannelAiHandOffTransport.cs:193`
  (`services.AddHttpClient(SendToAi.HttpClientName)`), with the `IHttpClientFactory` injection at
  `:19`. It is the shape to mirror and the code to stay away from: `AiWork/` is the gated
  `Features:SendToAi` surface that only composes under the `DevelopmentOffline` runtime profile, and
  it is a recorded exclusion from parity scope. Nothing in this ticket may reference it.
- **Session credential storage**: today the browser holds it. `Pegasus.Web` uses ASP.NET Core Identity
  cookies plus OpenIddict; there is no client-side credential store in the repository at all, so the
  DPAPI store is genuinely new rather than a port.
- **A bounded local cache**: nothing equivalent exists; server-side caching is EF and the database.
- **Diagnostics writing**: server-side observability is Application Insights
  (`Microsoft.ApplicationInsights.AspNetCore` in `src/Pegasus.Web/Pegasus.Web.csproj:38`,
  `Microsoft.ApplicationInsights.WorkerService` in `src/Pegasus.Worker/Pegasus.Worker.csproj:15`).
  ADR-0109 keeps that and adds a desktop bundle instead of a new telemetry fleet; the writer here is
  the first half of it and [[FND-036]] (plan handle `DSK-02-11`) is the second.

## Findings

### Facts

Verified by reading the repository at fork `main`, 2026-08-24. Each carries its source.

- **`src/Pegasus.Desktop.Infrastructure` does not exist**; `ls src` returns exactly `Pegasus.Core`,
  `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`.
- **Neither of this ticket's two dependencies exists yet.** `src/Pegasus.Contracts` (with
  `PegasusHeaders.cs`) is created by [[FND-029]] (plan handle `DSK-02-04`);
  `src/Pegasus.Desktop` and the desktop target framework and package pins are created by
  [[FND-030]] (plan handle `DSK-02-05`). Every path this ticket writes to therefore sits beside files
  that must land first.
- **`Pegasus.Core` is a safe reference target**: `src/Pegasus.Core/Pegasus.Core.csproj` (14 lines) has
  **zero** package references and zero project references, so referencing Core pulls no Azure, EF or
  ASP.NET dependency in transitively. That is what makes the "reference only Core and Contracts" rule
  achievable rather than aspirational.
- **The forbidden-prefix pattern to mirror is at
  `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:23-39`** — sixteen entries:
  `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`, `Azure`, `Microsoft.Graph`, `Box`,
  `MimeKit`, `DocumentFormat.OpenXml`, `UglyToad.PdfPig`, `Microsoft.Data.SqlClient`,
  `System.Net.Http`, `OpenIddict`, `ModelContextProtocol`, `Pegasus.Infrastructure`, `Pegasus.Web`,
  `Pegasus.Worker`. Consumed by `IsForbiddenCoreDependency` (`:475-478`) and
  `ForbiddenDirectDependencies` (`:480-491`).
  - **Load-bearing gotcha**: that list is **Core's**, and it forbids `System.Net.Http`. The desktop
    *requires* HTTP. So [[FND-037]] (plan handle `DSK-02-12`) must build a **separate** desktop
    prefix list — reusing `ForbiddenCoreDependencyPrefixes` unchanged would fail the desktop project
    for doing the one thing it exists to do. The shape is reusable; the list is not.
- **`System.Security.Cryptography.ProtectedData` is already in the restore graph, transitively, at
  9.0.4** — `src/Pegasus.Infrastructure/packages.lock.json:887-891`
  (`"type": "Transitive", "resolved": "9.0.4"`), and it appears in `src/Pegasus.Web/packages.lock.json`
  too (`:474`, `:994`, with a `4.5.0` constraint from an older transitive edge). No project declares
  it directly. Consequence for central package management: the `PackageVersion` this ticket adds must
  be **≥ 9.0.4**, or CPM will pin the whole solution below what the server graph already resolves and
  the server restore changes as a side effect of a desktop ticket.
- **`Microsoft.Extensions.Http` is not referenced anywhere today.** The only `AddHttpClient` call
  (`src/Pegasus.Web/AiWork/ChannelAiHandOffTransport.cs:193`) gets it from
  `Microsoft.NET.Sdk.Web`'s framework reference, which a `Microsoft.NET.Sdk` desktop project does not
  have — so the explicit package reference in step 2 is genuinely required, not belt-and-braces.
- **`Directory.Packages.props` does not exist** (`ls` → *No such file*); [[FND-027]] (plan handle
  `DSK-02-02`) creates it. Adding two `PackageVersion` entries assumes that has landed.
- **`Directory.Build.props` (19 lines) applies**: `TreatWarningsAsErrors=true`,
  `AnalysisLevel=latest-recommended`, `Nullable`, `ImplicitUsings`, `LangVersion=latest`.
- **The solution-contents fact** is `DependencyDirectionTests.cs:128`, expected array `:137-149`,
  ordered `StringComparer.Ordinal` — `src/Pegasus.Desktop.Infrastructure/…` sorts immediately after
  `src/Pegasus.Desktop/…`.
- **`ProjectReferencesFollowTheModularMonolithDirection`** (`:111-125`) asserts exact reference sets
  per project. It does **not** yet cover the desktop projects; extending it is [[FND-037]]'s job, not
  this ticket's, but the new `src/Pegasus.Desktop` → `src/Pegasus.Desktop.Infrastructure` reference
  added at step 9 is what that fact will later assert.
- **Area 03 fixes the pipeline's obligations**
  (`docs/desktop/03-gateway-api-and-data/README.md`):
  - `:168` — `X-Correlation-Id` accepted or generated, echoed and logged;
    `X-Pegasus-Client-Version` **required on every `/api/v1` request**, absence → `client-unsupported`.
  - `:173` — "Desktop retries only idempotent `GET`s (bounded, jittered); commands are never retried
    automatically; provider-backed endpoints carry provider-specific timeouts."
- **Plan 02 § 3 decision 6 fixes the credential store**: DPAPI
  (`System.Security.Cryptography.ProtectedData`, `DataProtectionScope.CurrentUser`) file-backed under
  the packaged app's `ApplicationData.Current.LocalFolder`, **not** `PasswordVault`, because the
  Credential Locker's 20-credential AppContainer limit and its "only … passwords and not … larger data
  blobs" guidance do not fit a session handle
  (<https://learn.microsoft.com/windows/apps/develop/security/credential-locker>, fetched 2026-08-23
  per plan 02 § 2). The access token stays in memory.
- **Plan 04 § 3 item 2 makes the stored secret short-lived by design**: access token 10 minutes;
  refresh token rolling, re-issued on every refresh, 2-hour idle lifetime
  (`StaffSessionPolicy.IdleLifetime`) with an **absolute 8-hour cap** carried as an
  `original-issued-at` claim.
- **Plan 04 § 3 item 3 makes revocation central**: account disable, password change and explicit
  logout revoke the subject's refresh tokens in the OpenIddict token store, and every `/api/v1`
  request re-checks `IsEnabled` and the security stamp — "a disabled account therefore stops within
  one request, not one access-token lifetime".
- **ADR-0109 bounds the diagnostics design**; it is authored by [[FND-006]] (plan handle `DSK-00-06`)
  and states the desktop bundle plus the existing Application Insights, with no new telemetry fleet.
  ADR-0104 (online-required, bounded local cache) is authored by [[FND-005]] (plan handle
  `DSK-00-05`) and, per this ticket's body, also claimed by [[FND-026]] (plan handle `DSK-02-01`).
- **`tests/Pegasus.Desktop.ViewModelTests` does not exist** — `ls tests` returns exactly
  `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`. [[FND-038]] (plan
  handle `DSK-02-13`) creates it, and its plan-level dependency arrow points **at** this ticket while
  this ticket's step 11 needs it — the inversion the body already flags.
- **`tests/Pegasus.ArchitectureTests` cannot host these tests**: it targets `net10.0`
  (`tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj`), and both `ProtectedData` (a
  Windows-only API) and `ApplicationData.Current` (package identity) need the Windows target
  framework. The body's step 11 is correct on this point.
- **The only `AddHttpClient` precedent lives in gated code.**
  `src/Pegasus.Web/AiWork/ChannelAiHandOffTransport.cs` — `IHttpClientFactory` at `:19`,
  `services.AddHttpClient(SendToAi.HttpClientName)` at `:193`. Per the recorded operator decision,
  `Features:SendToAi` composes only under the `DevelopmentOffline` runtime profile and is a **recorded
  exclusion with a reactivation condition**, not an open question; `AiWork/` is marked "gated, out of
  parity scope" in `docs/desktop/05-implementation-and-migration/reuse-map.md:38`. Read it for the
  registration shape; reference nothing in it.

### Assumptions

- **A-FND031-1 — `IHttpClientFactory` plus a `DelegatingHandler` composes cleanly in a packaged WinUI
  app without an ASP.NET host.** *Confirms it*: the header-injection unit test at step 11 resolving a
  named client from a bare `ServiceCollection`. *If wrong*: the pipeline must be built by hand and the
  retry/handler design changes shape, which would be a plan-level change, not a code tweak.
- **A-FND031-2 — a `PackageVersion` of `System.Security.Cryptography.ProtectedData` at ≥ 9.0.4 does
  not change the server projects' resolved graph.** *Confirms it*: `git diff` over
  `src/Pegasus.Infrastructure/packages.lock.json` and `src/Pegasus.Web/packages.lock.json` after the
  solution restore — expected: no change. *If wrong*: a desktop ticket has silently altered the
  server restore, which is exactly the cross-boundary surprise central package management makes
  possible; record it and raise it rather than accepting the diff.
- **A-FND031-3 — `Package.Current.Id.Version` is readable from a `Microsoft.NET.Sdk` (non-WinUI)
  library once the desktop TFM is set.** *Confirms it*: the `Windows/` implementation compiles and the
  fake-based test passes without it. *If wrong*: `IClientVersionProvider`'s Windows implementation
  moves into `src/Pegasus.Desktop` and only the interface stays here — which the abstraction in step 4
  is designed to allow.
- **A-FND031-4 — DPAPI `CurrentUser` blobs written under
  `ApplicationData.Current.LocalFolder.Path` survive an MSIX upgrade.** *Confirms it*: the packaging
  install/upgrade scenarios owned by [[FND-039]] (plan handle `DSK-02-14`) and area 08. *If wrong*:
  every upgrade silently signs the operator out, which is a Phase 2 acceptance failure rather than a
  crash — so it must be *tested*, not reasoned about.
- **A-FND031-5 — a corrupted DPAPI blob raises `CryptographicException` rather than returning
  garbage.** *Confirms it*: the corrupted-blob case in the tier-2 evidence set. *If wrong*, a
  corrupted store yields a nonsense token and the failure surfaces as an authentication error rather
  than a store error, which the operator cannot act on.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | The credential store is per-Windows-user by construction (`DataProtectionScope.CurrentUser`, plan 02 § 3 decision 6) and the cache is in-memory and per-process. Nothing here is shared between operators. |
| Unattended execution — must it run with every desktop closed? | **No** | Every responsibility here runs inside the operator's session. Unattended work stays in `Pegasus.Worker` under ADR-0106, untouched by this ticket. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No — and the distinction is the whole design.** | What sits on the workstation is a **short-lived** refresh handle: rolling, re-issued on every refresh, 2-hour idle and an absolute 8-hour cap (plan 04 § 3 item 2), DPAPI-protected at `CurrentUser` scope, with the access token never persisted at all (proposal § 8.2, § 11.1). The genuinely **long-lived** secrets — Box, Microsoft Graph, DVLA/DVSA — stay behind the gateway under **ADR-0107** ("no long-lived provider secret in the package"), and no Azure credential or connection string may enter this project at all. |
| Public callback — must an external service call a stable public endpoint? | **No** | This project only makes outbound calls to the gateway. It exposes no listener. |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **Yes — and it lands on the already-existing evolved `Pegasus.Web` gateway, not on any new Azure resource.** | Two enforcement points, both server-side and both the reason a client-side store is acceptable at all: (a) **revocation** — plan 04 § 3 item 3, account disable / password change / logout revoke refresh tokens in the OpenIddict token store and every `/api/v1` request re-checks `IsEnabled` and the security stamp, so a stolen or stale handle stops within one request; (b) **client-version enforcement** — area 03 `:168`, `X-Pegasus-Client-Version` is required on every request and its absence yields `client-unsupported`. The desktop *sends* the header; it does not enforce it. L-01 fixes the host as `Pegasus.Web` evolved in place on the existing Container App; no Azure write arises. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | None claimed. Plan 04 § 3 item 3 does record a cost estimate for the per-request re-check ("one indexed read per request, acceptable at ten users"), which is a cost note, not a measured advantage for central placement. |

**Conclusion.** Four "no" and one "yes"; the "yes" names the existing gateway process, and the one
credential that legitimately sits on a workstation does so because it is short-lived, per-user
encrypted and centrally revocable. No responsibility is placed in Azure and no Azure write arises.

## Implications

1. **The boundary is enforced twice, and this ticket owns only the first.** Step 10's `grep` is an
   inspection this ticket performs; [[FND-037]] (plan handle `DSK-02-12`) makes it a test. The plan
   must not claim the grep is the enforcement.
2. **The desktop forbidden-prefix list is not Core's.** `System.Net.Http` sits in
   `ForbiddenCoreDependencyPrefixes` (`DependencyDirectionTests.cs:33`) and the desktop needs HTTP.
   Whoever writes the desktop facts must author a new list; the *shape* (`IsForbiddenCoreDependency`,
   `ForbiddenDirectDependencies`) is what is reusable.
3. **Central package management can leak across the boundary.** `ProtectedData` already resolves at
   9.0.4 transitively in the server graph; a `PackageVersion` below that silently changes the server
   restore. The plan must pin ≥ 9.0.4 and prove the server lock files are unchanged.
4. **The test-project sequencing is inverted and must be recorded, not resolved by duplication.**
   `tests/Pegasus.Desktop.ViewModelTests` does not exist; it is the only project that can host
   `ProtectedData` and `ApplicationData` tests; and [[FND-038]]'s dependency arrow points at this
   ticket. The body's step 11 instruction — sequence [[FND-038]] first and record it — is followed.
   Duplicating the test scaffold here would be the third-copy failure.
5. **Empty folders are forbidden.** `docs/engineering.md` § Abstractions and deferred capabilities
   (`:113`) — "Anything built but unwired for two weeks gains a real caller or is deleted". Of the six
   folders proposal § 5.4 names (`Api/`, `Authentication/`, `Caching/`, `Documents/`, `Diagnostics/`,
   `Windows/`), this ticket has real content for five; `Documents/` has none until area 05, so it must
   simply not be created.
6. **The retry rule is asymmetric and must be visible in code.** `GET`s only, bounded and jittered;
   commands never. A comment plus the test [[FND-038]] adds is the body's instruction and is the right
   shape — a retry policy applied to the whole named client would silently break it.
7. **The redaction hook is an interface obligation here and a proven behaviour later.** [[FND-032]]
   (plan handle `DSK-02-07`) wires the writer into the logging pipeline and proves redaction with a
   planted token; [[FND-036]] re-applies it at bundle collection. This ticket must define the hook so
   both can use one implementation.

## Open questions

- None that must be answered before implementation. Every unknown above is an assumption with a named
  command inside this ticket or an adjacent one that settles it, and the one sequencing problem
  ([[FND-038]] before step 11) is a scope boundary with a named owner, recorded in the plan's Risks
  section rather than opened as a question.
