# Files — FND-031

Surveyed 2026-08-24 against fork `main`. Existing paths were confirmed with `ls`/`grep`; new files are
marked; files created by a named earlier ticket say so.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj` | **New.** `Microsoft.NET.Sdk`, `net10.0-windows10.0.26100.0`, `TargetPlatformMinVersion 10.0.22000.0`, `<Platforms>x64</Platforms>`, `ImplicitUsings` and `Nullable` enabled, and **exactly two** `ProjectReference` entries: `..\Pegasus.Core\Pegasus.Core.csproj` and `..\Pegasus.Contracts\Pegasus.Contracts.csproj`. A third reference is the defect [[FND-037]] (plan handle `DSK-02-12`) will later fail on. |
| `src/Pegasus.Desktop.Infrastructure/Api/PegasusRequestHandler.cs` | **New.** `DelegatingHandler` setting `PegasusHeaders.ClientVersion` from the package version and `PegasusHeaders.CorrelationId` to a fresh `Guid` when the caller supplied none, and exposing the correlation id to the logger scope. Uses the constants from Contracts, never string literals. |
| `src/Pegasus.Desktop.Infrastructure/Api/PegasusHttpClientRegistration.cs` | **New.** `AddPegasusApiClient(this IServiceCollection, Action<GatewayOptions>)` calling `AddHttpClient("pegasus")`, setting `BaseAddress` from options, adding the handler, and configuring a bounded jittered retry **for idempotent `GET` only**. Commands are never retried automatically (area 03 `:173`). |
| `src/Pegasus.Desktop.Infrastructure/Api/GatewayOptions.cs` | **New** (implied by the registration signature). Holds the gateway base address; [[FND-032]] (plan handle `DSK-02-07`) binds and validates it from the embedded channel configuration. |
| `src/Pegasus.Desktop.Infrastructure/Authentication/IDesktopCredentialStore.cs` | **New.** `Save(string key, string value)`, `TryRead(string key, out string? value)`, `Clear(string key)`. |
| `src/Pegasus.Desktop.Infrastructure/Authentication/DpapiCredentialStore.cs` | **New.** `ProtectedData.Protect`/`Unprotect` at `DataProtectionScope.CurrentUser`, one file per key under an injected `storeRoot`. Constructor takes `string storeRoot` so the app can pass `ApplicationData.Current.LocalFolder.Path` and tests a temporary directory. **Never** stores the access token — that stays in memory (proposal § 8.2, § 11.1). |
| `src/Pegasus.Desktop.Infrastructure/Caching/BoundedSnapshotCache.cs` | **New.** In-memory only, explicit entry-count cap and per-entry expiry, holding only what ADR-0104 permits — small reference-data snapshots, thumbnails, the last compatibility response. No file-backed store, no SQLite. |
| `src/Pegasus.Desktop.Infrastructure/Diagnostics/IDiagnosticsWriter.cs` | **New.** The interface [[FND-032]] wires into the logging pipeline and [[FND-036]] (plan handle `DSK-02-11`) packages into the bundle. Defining it here is what stops two implementations existing. |
| `src/Pegasus.Desktop.Infrastructure/Diagnostics/RollingFileDiagnosticsWriter.cs` | **New.** Total-size cap, retention count, and a redaction hook that strips bearer tokens, refresh tokens and password fields **before** a line is written. |
| `src/Pegasus.Desktop.Infrastructure/Windows/…` | **New.** The `IClientVersionProvider` implementation over `Package.Current.Id.Version`. Kept here so `PegasusRequestHandler` is testable without package identity. |
| `Directory.Packages.props` (created by [[FND-027]], plan handle `DSK-02-02`) | Add `PackageVersion` entries for `System.Security.Cryptography.ProtectedData` — **at ≥ 9.0.4**, the version already resolved transitively in `src/Pegasus.Infrastructure/packages.lock.json:887-891` — and `Microsoft.Extensions.Http`. Reference both here without a version literal. |
| `Pegasus.slnx` | Add `<Project Path="src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj" />` under `/src/`. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` | Extend the ordinal expected array at `:137-149`; the new path sorts immediately after `src/Pegasus.Desktop/Pegasus.Desktop.csproj`. No rule change — that is [[FND-037]]. |
| `src/Pegasus.Desktop/Pegasus.Desktop.csproj` (created by [[FND-030]], plan handle `DSK-02-05`) | Add one `ProjectReference` to this project. This single line is what `ProjectReferencesFollowTheModularMonolithDirection` (`DependencyDirectionTests.cs:111-125`) will later assert for the desktop. |
| `src/Pegasus.Desktop.Infrastructure/packages.lock.json` | **New, generated** with `-r win-x64 --force-evaluate` and committed, or the CI locked restore fails on every lane. |
| `docs/current-architecture.md` | 682 lines; § Components and dependency direction at `:55`. Add the project and its **two** permitted references. |

**Deliberately not created:** `Documents/`. Proposal § 5.4 names it, but nothing in this ticket fills
it and `docs/engineering.md` § Abstractions and deferred capabilities (`:113`) forbids dormant
scaffolding — "Anything built but unwired for two weeks gains a real caller or is deleted". The body's
step 3 says the same: leave a folder out rather than creating an empty placeholder.

## Context files

What the implementer must **read** and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Web/AiWork/ChannelAiHandOffTransport.cs:19`, `:193` | The repository's **only** `AddHttpClient` registration and `IHttpClientFactory` injection — the shape to mirror for `AddPegasusApiClient`. Also the code to keep away from: `AiWork/` is the gated `Features:SendToAi` surface that composes only under the `DevelopmentOffline` runtime profile, marked "gated, out of parity scope" in `docs/desktop/05-implementation-and-migration/reuse-map.md:38`. Read the pattern; reference nothing in it. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:23-39` | The forbidden-prefix list this project's boundary check mirrors — **and the trap**. It is *Core's* list and it contains `System.Net.Http` at `:33`, which the desktop must be allowed to use. The reusable part is the shape (`IsForbiddenCoreDependency` `:475`, `ForbiddenDirectDependencies` `:480`); the list itself must be authored fresh for the desktop by [[FND-037]]. |
| `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:111-125` | `ProjectReferencesFollowTheModularMonolithDirection` asserts an exact reference set per project. It does not cover the desktop yet, but it is the fact that will later pin "exactly Core and Contracts" — so getting the two references right now is what makes that later fact trivially green. |
| `src/Pegasus.Core/Pegasus.Core.csproj` (14 lines) | Why referencing Core is safe: zero package references and zero project references, so nothing Azure, EF or ASP.NET arrives transitively. The whole "only Core and Contracts" rule depends on this staying true. |
| `src/Pegasus.Infrastructure/packages.lock.json:887-891` | `System.Security.Cryptography.ProtectedData` already resolves **transitively at 9.0.4** in the server graph. Pinning a lower `PackageVersion` under central package management would silently change the *server* restore from a desktop ticket — check the server lock files are unchanged after the solution restore. |
| `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:8-28` | The 21 packages this project must never acquire — `Azure.Identity`, `Azure.Storage.Blobs`, `Box.Sdk.Gen`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.Graph`, `OpenIddict.EntityFrameworkCore`, `MimeKit` and the rest. It is the concrete answer to "what does referencing `Pegasus.Infrastructure` actually drag in". |
| `docs/desktop/03-gateway-api-and-data/README.md:168` | `X-Correlation-Id` accepted or generated, echoed and logged; `X-Pegasus-Client-Version` **required on every** `/api/v1` request, absence → `client-unsupported`. This is why a broken header pipeline does not degrade gracefully — the workstation simply cannot work. |
| `docs/desktop/03-gateway-api-and-data/README.md:173` | The retry rule, verbatim: idempotent `GET`s only, bounded and jittered; commands never retried automatically; provider-backed endpoints carry provider-specific timeouts. A policy attached to the whole named client would silently violate this. |
| `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 6 | Why DPAPI and not `PasswordVault`: the Credential Locker's 20-credential AppContainer limit and its guidance to use it "for passwords and not for larger data blobs", against a session handle that may exceed that. Also: the access token stays in memory; the abstraction lives here and area 04 implements the flow. |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 items 2 and 3 | What is actually stored and for how long — rolling refresh token, 2-hour idle, absolute 8-hour cap — and that revocation is central: disable, password change and logout revoke in the OpenIddict token store, with `IsEnabled` and the security stamp re-checked on **every** `/api/v1` request. This is the justification for a client-side store existing at all. |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 item 8 | "Secrets in the package: none." The package carries only the gateway base URL, feed URL and channel name. Nothing in this project may widen that. |
| `src/Pegasus.Contracts/PegasusHeaders.cs` (created by [[FND-029]], plan handle `DSK-02-04`) | The two header-name constants. Using a literal `"X-Correlation-Id"` here instead is the "one list per concept" defect, and it is invisible to the compiler. |
| `src/Pegasus.Web/Pegasus.Web.csproj:38`, `src/Pegasus.Worker/Pegasus.Worker.csproj:15` | Where server observability lives today (`Microsoft.ApplicationInsights.*`). ADR-0109 keeps it and adds a desktop bundle instead of a second telemetry fleet — so the writer here is a local file sink, not a telemetry client. |
| `docs/engineering.md` § Abstractions and deferred capabilities (`:113`) | "Add an interface only for a real external boundary, a second concrete caller, or an accepted ADR… Anything built but unwired for two weeks gains a real caller or is deleted." The reason `Documents/` is not created and the reason `IDiagnosticsWriter` *is* (two named callers: [[FND-032]] and [[FND-036]]). |
| `Directory.Build.props` (19 lines) | `TreatWarningsAsErrors=true` and `AnalysisLevel=latest-recommended` apply from the first build; "zero warnings" is compiler-enforced. |
| `.github/actions/dotnet-build/action.yml:14-23` | The locked restore is over `./Pegasus.slnx` and the cache key globs `src/**/packages.lock.json` — a missing desktop-infrastructure lock file breaks every lane, not one. |
| `docs/adr/0016-standalone-desktop-email-evaluator.md` (present in `docs/adr/`) | The precedent that a Windows-target project in this repository is a recorded decision, and that `scripts/email-eval-desktop/` — which *does* reference `Pegasus.Infrastructure` — is deliberately outside the solution. It is the counter-example to this project's reference rule. |

## Ripple effects

- **Tests.** `tests/Pegasus.Desktop.ViewModelTests` gains the credential-store round-trip and
  header-injection tests. That project **does not exist yet** ([[FND-038]], plan handle `DSK-02-13`),
  and `tests/Pegasus.ArchitectureTests` cannot host them: it targets `net10.0`, while `ProtectedData`
  and `ApplicationData.Current` need the Windows target framework. The body's step 11 sequencing note
  is therefore mandatory.
- **Architecture test.** The ordinal expected array at `DependencyDirectionTests.cs:137-149` fails
  until it is extended — the intended coupling. It runs unfiltered in the CI `unit` lane
  (`.github/workflows/ci.yml:136-148`).
- **Restore graph, in two directions.** The new project needs its own committed lock file; and the new
  `PackageVersion` for `ProtectedData` can move the **server** projects' resolved graph if pinned
  below 9.0.4. Both `src/Pegasus.Infrastructure/packages.lock.json` and
  `src/Pegasus.Web/packages.lock.json` must be checked for an unintended diff.
- **`src/Pegasus.Desktop`.** Gains one `ProjectReference` line — the only edit this ticket makes to
  [[FND-030]]'s project.
- **Downstream tickets.** [[FND-032]] registers `AddPegasusApiClient` and the credential store in the
  host and wires `IDiagnosticsWriter` into logging; [[FND-036]] collects the writer's output into the
  bundle; [[FND-043]] (plan handle `DSK-04-07`) implements the session flow over this store;
  [[GWY-005]] (plan handle `DSK-03-05`) writes the generated client into `Api/Generated/`;
  [[FND-037]] turns step 10's grep into tests; [[FND-038]], [[FND-041]] (plan handle `DSK-02-16`),
  [[FEAT-039]], [[PLAT-007]] and [[PLAT-012]] all depend on this project existing.
- **No OpenAPI or generated-client ripple yet.** There is no `openapi/` directory in the repository
  (`ls openapi` → *No such file or directory*); [[GWY-004]] (plan handle `DSK-03-04`) creates the
  snapshot and [[GWY-005]] the client. From that point a change to `Api/` ripples into both.
- **Documentation.** `docs/current-architecture.md` § Components and dependency direction gains a row;
  `scripts/Test-DocumentationLinks.ps1` runs in the CI `documentation` lane.

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **Any Azure SDK, credential or connection string** — refused. The desktop never holds a database,
  Graph, Box or DVLA/DVSA secret; those stay behind the gateway under ADR-0107.
- **The generated API client** — [[GWY-005]] writes it into `Api/Generated/`.
- **Token acquisition and refresh** — area 04 ([[FND-043]]). This ticket provides the store, not the
  flow.
- **The diagnostics bundle** — [[FND-036]]. This ticket provides the writer, not the zip.
- **A durable/file-backed cache or SQLite** — refused. Adding one requires the profiling evidence
  proposal § 11.2 demands and an ADR-0104 change.
- **`Documents/`** — not created; no caller until area 05.
- **Copying Core policy types or `OperatorLabels` into the desktop** — refused; [[FEAT-023]] (plan
  handle `DSK-05-23`) and [[GWY-016]] (plan handle `DSK-03-16`) decide where shared code moves, once.
- **Desktop dependency-direction *rules*** — [[FND-037]]; this ticket only extends the solution
  contents array and verifies its own boundary by inspection.
- **Relaxing `TreatWarningsAsErrors`** — refused; generated code gets `GeneratedCodeAttribute` or a
  narrow, commented `NoWarn`.
