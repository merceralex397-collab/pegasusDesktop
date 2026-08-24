# Files — FND-032

Surveyed 2026-08-24 against fork `main`. Existing paths were confirmed with `ls`/`grep`; new files are
marked; files created by a named earlier ticket say so.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop/App.xaml.cs` (created by [[FND-030]], plan handle `DSK-02-05`) | Build the host in `OnLaunched` **before** the window is created, hold it in a single static accessor, and dispose it on `Application.Current.Exit`. This is the file that decides whether any view model can ever `new` up a client — after this change, nothing else constructs services. |
| `src/Pegasus.Desktop/Hosting/PegasusHost.cs` | **New.** Builds the host: two `AddJsonStream` calls over the embedded configuration, `GatewayOptions` / `UpdateOptions` / `ChannelOptions` bound with validation that fails at start, `AddPegasusApiClient(…)` from [[FND-031]] (plan handle `DSK-02-06`), the credential store rooted at `ApplicationData.Current.LocalFolder.Path`, and the bounded cache. Navigation and dialog services are registered **only once [[FND-033]] (plan handle `DSK-02-08`) defines them** — no empty interfaces now. |
| `src/Pegasus.Desktop/Configuration/appsettings.json` | **New.** The base layer. |
| `src/Pegasus.Desktop/Configuration/appsettings.local.json` | **New.** Points at the local Test/UAT stack (L-02), never an Azure test resource. |
| `src/Pegasus.Desktop/Configuration/appsettings.pilot.json` | **New.** |
| `src/Pegasus.Desktop/Configuration/appsettings.production.json` | **New.** |
| — each of the four | Exactly three settings and nothing else: `Gateway:BaseAddress`, `Update:FeedUri`, `Channel` (plan 04 § 3 item 8). No secret, token, connection string or Azure identifier in any of them. |
| `src/Pegasus.Desktop/Pegasus.Desktop.csproj` | Add `<PegasusChannel Condition="'$(PegasusChannel)'==''">local</PegasusChannel>` and embed `Configuration/appsettings.json` plus `Configuration/appsettings.$(PegasusChannel).json` as `EmbeddedResource` with **fixed logical names** (so the reader does not have to know the channel). Also add the three `Microsoft.Extensions.*` package references without version literals. |
| `src/Pegasus.Desktop/Logging/…` | **New.** The `ILoggerProvider` that adapts `IDiagnosticsWriter` from [[FND-031]] into `Microsoft.Extensions.Logging`, plus the redaction message processor. One provider, one processor — [[FND-036]] (plan handle `DSK-02-11`) re-applies the same processor at bundle collection rather than writing a second rule set. |
| `Directory.Packages.props` (created by [[FND-027]], plan handle `DSK-02-02`) | Add `PackageVersion` entries for `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Configuration.Binder` and `Microsoft.Extensions.Options.DataAnnotations`. None of the three is referenced anywhere in the repository today. |
| `tests/Pegasus.Desktop.ViewModelTests/…` (created by [[FND-038]], plan handle `DSK-02-13`) | The fake-host fixture, the options-validation test, the redaction fixture and the rotation test. |
| `docs/current-architecture.md` | 682 lines; § Components and dependency direction at `:55`. One entry: the desktop client composes on the generic host with channel-selected embedded configuration. |

## Context files

What the implementer must **read** and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Web/Program.cs:100-116` | The repository's fail-closed configuration precedent, and its exact shape: `builder.Configuration["Runtime:Profile"] ?? throw new InvalidOperationException("Runtime:Profile is required.")` at `:101-102`, then two guards at `:104-110` and `:112-116` that refuse to start when a profile/feature combination is wrong. Whatever validation mechanism the desktop uses, this is the **behaviour** to match: refuse to start, name the setting. |
| `src/Pegasus.Web/appsettings.json` | Two things. First, the layering and `Logging:LogLevel` shape the desktop mirrors. Second, and more important: it carries a plaintext `Bootstrap:VerificationAccount` user name and password, with a comment in the file calling it TEMPORARY and saying "Never leave it configured in a real production deployment." That is the exact failure the desktop's "no secret in any shipped `appsettings*.json`" criterion prevents — and a desktop package on ten workstations cannot be fixed by a redeploy. Read it as the counter-example, and never copy its shape. |
| `src/Pegasus.Core/Documents/RequestUploadPolicy.cs:110` | `public override string ToString() => "[REDACTED]";` — the repository's **only** redaction, and it is type-level, not sink-level. It tells the implementer there is no existing log-redaction rule to reuse, so the processor built here is the first and must be the only one. |
| `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 7 | The composition contract in full: generic host inside `App.xaml.cs`, one `IHttpClientFactory` pipeline, structured logging to a bounded rolling file sink with redaction, and configuration layered as embedded base + channel **selected by an MSBuild property at package time** — the last clause being the security control, not a build convenience. |
| `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 9 | "No desktop framework on top of WinUI": a shell service, a navigation service, a dialog service and a handful of controls are the whole permitted surface. It bounds what may be registered in the container. |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 item 8 | The exact payload of the configuration files — gateway base URL, feed URL, channel name — and that there are no secrets. Three settings, no fourth. |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 item 6 | Why the base address matters: the compatibility response is cached for 24 hours and the app then fails closed with no bypass. The gateway a package carries is the only one it can reach. |
| `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 item 5 | That the feed path baked per channel is `\\<host>\<share>\<channel>\Pegasus.appinstaller` under D-003 — a UNC path, not a URL, so `Update:FeedUri` must tolerate that form. |
| `.codex/skills/winui-dev-workflow/BuildAndRun.ps1:146-157` | That the script writes a `Directory.Build.props` into the **project directory** whenever that exact file is absent, and MSBuild stops at the first one found walking up — so a script build silently loses `TreatWarningsAsErrors`, `Nullable` and the rest. Consequence for step 11: prove the channel selection with a plain `dotnet build`, not with the script. |
| `Directory.Build.props` (19 lines) | What the desktop project inherits and what a script build would shadow: `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, `LangVersion=latest`, `Version=0.1.0-alpha.1`. |
| `src/Pegasus.Desktop.Infrastructure/Diagnostics/IDiagnosticsWriter.cs`, `Api/PegasusHttpClientRegistration.cs`, `Authentication/IDesktopCredentialStore.cs` (all created by [[FND-031]]) | The three things this host registers. Their signatures decide `PegasusHost.cs`'s shape, and the credential store's constructor taking `string storeRoot` is why the host is the place that supplies `ApplicationData.Current.LocalFolder.Path`. |
| `docs/engineering.md` § Abstractions and deferred capabilities (`:113`) | "Add an interface only for a real external boundary, a second concrete caller, or an accepted ADR… Anything built but unwired for two weeks gains a real caller or is deleted." The rule behind "do not create empty navigation/dialog interfaces now" — [[FND-033]] defines them. |
| `docs/desktop/README.md` § Locked decisions (L-02) | Test/UAT is a local production-mimicking stack; ADR-0014 stands. The `local` channel points there. Writing an Azure endpoint into `appsettings.local.json` needs a new accepted decision. |
| `src/Pegasus.Web/Pegasus.Web.csproj:38`, `src/Pegasus.Worker/Pegasus.Worker.csproj:15` | Where telemetry lives today (Application Insights). ADR-0109 keeps it and adds a local bundle instead of a second fleet, which is why the desktop sink is a file, not a telemetry client, and why no third-party logging framework is added. |
| `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:8-28`, `src/Pegasus.Web/Pegasus.Web.csproj:36-42`, `src/Pegasus.Worker/Pegasus.Worker.csproj:15-23` | All 33 package references in the repository — and none is an `Microsoft.Extensions.*` package. Confirms the three additions in step 2 are genuinely new rather than duplicating an existing pin. |

## Ripple effects

- **Every later desktop ticket resolves through this container.** [[FND-033]] registers the navigation
  and dialog services here; [[FND-035]] (plan handle `DSK-02-10`) registers `IActivationRouter` here;
  [[FND-036]] reads the sink's output; [[FND-043]] (plan handle `DSK-04-07`), [[FND-045]] (plan handle
  `DSK-04-09`), [[TEST-017]], [[PLAT-009]] and [[PLAT-017]] all depend on it.
- **Tests.** Four new tests land in `tests/Pegasus.Desktop.ViewModelTests` — host resolution, options
  validation, redaction, rotation. That project does not exist yet ([[FND-038]]).
- **Package contents.** The embedded resources become part of the shipped MSIX, so the "no secrets"
  criterion is a *distribution* property, not a source-tree one. [[FND-039]] (plan handle `DSK-02-14`)
  and [[REL-002]] (plan handle `DSK-09-02`) inherit it.
- **CI.** [[FND-040]] (plan handle `DSK-02-15`) must build with an explicit `-p:PegasusChannel=` or it
  will silently produce `local` packages; the default set in step 4 is `local` precisely so an
  unspecified build is harmless rather than accidentally production.
- **Restore graph.** Three new `PackageVersion` entries in `Directory.Packages.props` and a regenerated
  `src/Pegasus.Desktop/packages.lock.json`; `dotnet restore ./Pegasus.slnx --locked-mode` runs on every
  CI lane (`.github/actions/dotnet-build/action.yml:22`).
- **Documentation.** `docs/current-architecture.md` § Components and dependency direction gains one
  entry; `scripts/Test-DocumentationLinks.ps1` runs in the CI `documentation` lane.
- **No solution or architecture-test change.** This ticket adds no project, so
  `DependencyDirectionTests.ApplicationSolutionExcludesSourceWorkspaces` (`:128-154`) is untouched.

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **The shell** — [[FND-033]]. No `NavigationView`, no rail, no status bar here.
- **Single-instance redirection** — [[FND-035]].
- **The diagnostics bundle** — [[FND-036]]. This ticket wires the writer into logging; it does not zip
  anything.
- **Any authentication flow** — area 04. The host registers the credential store; it acquires no token.
- **Empty `INavigationService` / `IDialogService` interfaces** — not created. [[FND-033]] defines them
  and this host registers them then.
- **A third-party logging framework** — refused. The sink is the project's own `ILoggerProvider` over
  `Microsoft.Extensions.Logging`.
- **A large desktop framework on top of WinUI** — refused (plan 02 § 3 decision 9).
- **An Azure test endpoint in `appsettings.local.json`** — refused; L-02 and ADR-0014 stand.
- **Relaxing `TreatWarningsAsErrors` in `Directory.Build.props`** — refused.
- **`src/Pegasus.Core`, `src/Pegasus.Infrastructure`, `src/Pegasus.Web`, `src/Pegasus.Worker`** — not
  touched; the Guardrails limit this ticket to `src/Pegasus.Desktop/**` and `Directory.Packages.props`.
