# Files — FND-032

Surveyed 2026-08-24 against fork `main`. Every existing path was confirmed with `ls`/`sed`/`grep`;
paths created by an earlier ticket are marked with that ticket.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop/App.xaml.cs` (created by [[FND-030]], plan handle `DSK-02-05`) | Build the host in `OnLaunched` **before** the window is created, hold it behind one static accessor, dispose it on `Application.Current.Exit`. This is the only composition root; a view model that constructs a client anywhere else is the defect review must catch. |
| `src/Pegasus.Desktop/Hosting/PegasusHost.cs` | **New.** The whole host build: two `AddJsonStream` calls over the embedded resources, options binding with validation and `ValidateOnStart`, `AddPegasusApiClient(…)` from [[FND-031]] (plan handle `DSK-02-06`), the credential store rooted at `ApplicationData.Current.LocalFolder.Path`, the bounded cache, and the logging pipeline. Keeping it out of `App.xaml.cs` is what lets [[FND-038]] (plan handle `DSK-02-13`) build a host in a test without a dispatcher. |
| `src/Pegasus.Desktop/Configuration/appsettings.json` | **New.** Base layer. Exactly three settings: `Gateway:BaseAddress`, `Update:FeedUri`, `Channel`. |
| `src/Pegasus.Desktop/Configuration/appsettings.local.json`, `appsettings.pilot.json`, `appsettings.production.json` | **New.** One per channel, the same three keys, nothing else. `local` points at the local Test/UAT stack (L-02), never at an Azure test resource. |
| `src/Pegasus.Desktop/Options/GatewayOptions.cs`, `UpdateOptions.cs`, `ChannelOptions.cs` | **New.** The three bound option classes with data annotations. `GatewayOptions` may already exist from [[FND-031]] step 5 (`Api/GatewayOptions.cs` in that project) — check before creating a second one; a duplicate options class is the "one list per concept" failure `AGENTS.md` § Simplicity rails names. |
| `src/Pegasus.Desktop/Logging/DiagnosticsLoggerProvider.cs` | **New.** The `ILoggerProvider` over `Microsoft.Extensions.Logging` that writes through [[FND-031]]'s `IDiagnosticsWriter`. The Guardrails forbid a third-party logging framework, so this adapter is the whole sink. Attaches the per-launch session identifier and the API correlation id to every scope. |
| `src/Pegasus.Desktop/Pegasus.Desktop.csproj` (created by [[FND-030]]) | Add `<PegasusChannel Condition="'$(PegasusChannel)'==''">local</PegasusChannel>`, the two `EmbeddedResource` items with **fixed** `LogicalName`s, and the three package references without version literals. |
| `Directory.Packages.props` (created by [[FND-027]], plan handle `DSK-02-02`) | Add `PackageVersion` entries for `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Configuration.Binder` and `Microsoft.Extensions.Options.DataAnnotations`. Confirmed absent today (`ls Directory.Packages.props` → *No such file or directory*). |
| `tests/Pegasus.Desktop.ViewModelTests/**` (created by [[FND-038]], plan handle `DSK-02-13`) | Four test classes: host resolution, options validation failure, redaction, rotation. They cannot live in `tests/Pegasus.ArchitectureTests` — that project targets `net10.0` and these types need the Windows TFM and package identity shims. |
| `docs/current-architecture.md` | 682 lines. § Components and dependency direction (`:55`) records that the desktop composes on the generic host with channel-selected embedded configuration. |

## Context files

What the implementer must **read**, and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Web/appsettings.json` | **Read this one first — it is the counter-example.** It ships `Bootstrap.VerificationAccount` with `"Password": "Pegasus-UI-Verify-2026!"` in plaintext, above a `"//"` comment admitting it is temporary and "Never leave it configured in a real production deployment." A web server's `appsettings.json` sits on infrastructure the operator controls; the desktop's is **embedded in an MSIX copied to every workstation**. This is exactly why step 3 permits three keys and no fourth. |
| `src/Pegasus.Web/Program.cs:100-111` | The repository's existing "required configuration, validated at start" idiom, and the closest analogue to the channel model: `builder.Configuration["Runtime:Profile"] ?? throw new InvalidOperationException("Runtime:Profile is required.")` at `:101-103`, then a hard refusal — `"The DevelopmentOffline runtime profile is permitted only in the Development environment."` — at `:108-111`. The *mechanism* differs from `ValidateOnStart`; the *property to preserve* (fail at start, never a silent default) is the same. `:145-151` shows the same idiom looping a required-key list. |
| `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:31-53` | The repository's `EmbeddedResource` idiom, twelve items, each with an explicit `LogicalName` and one (`:48-51`) also using `<Link>`. This is the precedent step 4 follows, and the reason a **fixed** logical name matters: the file name varies by channel, the logical name does not, so `PegasusHost` calls `AddJsonStream` with two constants and never learns which channel it is. |
| `src/Pegasus.Worker/Program.cs:9-11` | The repository's only generic-host precedent — `new HostBuilder().ConfigureFunctionsWorkerDefaults()`. Useful for shape only: the Functions worker defaults, the Application Insights registration at `:14-15` and the `DefaultAzureCredential` block at `:26-40` are all things the desktop must **not** copy. ADR-0109 is "no new telemetry fleet". |
| **The absence of any `AddOptions<>` / `ValidateOnStart` / `ValidateDataAnnotations` call in `src/`** | `grep -rn "ValidateOnStart\|ValidateDataAnnotations\|AddOptions<" src/ --include=*.cs` returns **zero matches**. There is no existing options-validation convention to follow, so this ticket sets one. Do not go looking for a precedent that is not there; specify the behaviour and test it. |
| **The absence of any log-redaction code in `src/`** | `grep -rln "Redact\|redact" src/ --include=*.cs` returns nothing. The `Scrub`/`Sanitiz*` hits in `Email/GraphApprovedSources.cs` and `Intake/MimeKitPdfPigOpenXmlIntakeSourceReader.DocMsg.cs` are unrelated to log output. Step 7's rule has no prior art here — which is why it must be proven by a fixture test that plants a bearer token, not by review. |
| `docs/desktop/04-auth-session-update-and-startup/README.md:198-199` (§ 3 item 8) | The exact scope of what may be embedded: "**Secrets in the package**: none. The package carries only the gateway base URL, feed URL, and channel name per channel (02's embedded …)". Three settings, named, with area 02 — this ticket — named as the implementer. `:222` adds the acceptance form: "no secrets in MSIX (package content scan) (tier 9)". |
| `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 7 | The channel model in the plan author's own words: generic host in `App.xaml.cs`, one `IHttpClientFactory` pipeline, "structured logging to a bounded rolling file sink with redaction, configuration layered as embedded `appsettings.json` + `appsettings.<channel>.json` selected by an MSBuild property at package time (channel = `pilot` \| `production` \| `local`)". |
| `docs/desktop/02-architecture-and-foundation/README.md` § 3 decision 9 | "No desktop framework on top of WinUI: a shell service, a navigation service, a dialog service, and a handful of project controls." The boundary on how much this host is allowed to grow. |
| `src/Pegasus.Desktop.Infrastructure/Diagnostics/IDiagnosticsWriter.cs` (created by [[FND-031]]) | The sink contract this ticket adapts to `ILoggerProvider`: a total-size cap, a retention count, and **a redaction hook applied before a line is written**. The hook is defined there, once; this ticket calls it and [[FND-036]] (plan handle `DSK-02-11`) calls it again for the bundle. Neither re-implements the regex set. |
| `src/Pegasus.Desktop.Infrastructure/Api/PegasusHttpClientRegistration.cs` (created by [[FND-031]]) | The `AddPegasusApiClient(this IServiceCollection, Action<GatewayOptions>)` signature step 5 calls, and where `GatewayOptions` already lives — check it before declaring a second one. |
| `Directory.Build.props` (19 lines) | What this project inherits and cannot escape: `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, `Nullable`, `ImplicitUsings`, `LangVersion=latest`, `Deterministic=true`, `Version=0.1.0-alpha.1`. Its `:8-19` comment also shows the repository's precedent for a single-source-of-truth MSBuild property (`PlaywrightVersion`) — the shape `PegasusChannel` follows. |
| `.codex/skills/winui-dev-workflow/BuildAndRun.ps1:142-172` | That the script **injects** a project-level `Directory.Build.props` (the existence test at `:152` is against the project directory only, not up the tree) which **shadows** the root one for that build, dropping `TreatWarningsAsErrors`. A green `BuildAndRun.ps1` is a weaker gate than a plain `dotnet build`; use the script to launch, and `dotnet build ./Pegasus.slnx --configuration Release` to gate. |
| `docs/engineering.md` § Abstractions (`:113`) | Why step 5 forbids registering navigation/dialog placeholders now: nothing built but unwired survives. Register a service only once [[FND-033]] (plan handle `DSK-02-08`) has a real caller for it. |

## Ripple effects

- **Every later desktop ticket resolves from this container.** [[FND-033]] (shell and navigation),
  [[FND-035]] (plan handle `DSK-02-10`, single instance), [[FND-036]] (diagnostics bundle),
  [[FND-041]] (plan handle `DSK-02-16`, Phase 1 exit) and every area 05 slice register or resolve
  here. A change to the accessor's shape after this ticket is a change to all of them.
- **The log file location and format become an interface.** [[FND-036]] packages these files into the
  diagnostics bundle and asserts a manifest against them; [[FND-049]] (plan handle `DSK-04-13`)
  documents where an operator finds them. Changing the path or the first-line format later breaks
  both.
- **`Directory.Packages.props` gains three entries**, so every project restores against them. If any
  of the three already resolves transitively at a higher version in a server project's
  `packages.lock.json`, a lower central pin moves the server graph — check
  `git diff --stat src/*/packages.lock.json` after the solution restore, the same trap [[FND-031]]
  recorded for `System.Security.Cryptography.ProtectedData`.
- **`src/Pegasus.Desktop/packages.lock.json` is regenerated** by the three new package references and
  must be recommitted, or `dotnet restore ./Pegasus.slnx --locked-mode` fails on **every** CI lane —
  `.github/actions/dotnet-build/action.yml:22-27` runs it universally and its cache key globs
  `src/**/packages.lock.json`.
- **A second channel build changes what ships.** Step 11's `-p:PegasusChannel=pilot` is what
  [[REL-002]] (plan handle `DSK-09-02`) and [[FND-039]] (plan handle `DSK-02-14`) invoke at package
  time; the property name and its default (`local`) become part of the release interface.
- **No OpenAPI or generated-client ripple.** This ticket introduces no contract type and no endpoint,
  so `openapi/pegasus-v1.json` and the generated client are untouched — unlike most tickets on this
  board. Say so explicitly in the PR rather than leaving the reviewer to check.
- **Documentation.** `docs/current-architecture.md` changes, and
  `scripts/Test-DocumentationLinks.ps1` runs in the CI `documentation` lane
  (`.github/workflows/ci.yml:76-87`).

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **The shell, the `NavigationView`, the title bar and the status bar** — [[FND-033]]. This ticket
  registers no navigation or dialog service, and creates no empty interface for one
  (`docs/engineering.md` § Abstractions).
- **Single-instance registration and activation redirection** — [[FND-035]]. Note the ordering
  constraint it will impose: redirection must happen *before* any window is created, which is the
  same `OnLaunched` region this ticket edits. Leave the seam clean.
- **The diagnostics bundle** — [[FND-036]]. This ticket produces the logs; that one packages them.
- **Any authentication flow, token acquisition or refresh** — area 04, [[FND-043]] (plan handle
  `DSK-04-07`). This ticket registers the credential store; it never calls it.
- **The generated API client** — [[GWY-005]] (plan handle `DSK-03-05`) writes into
  `Api/Generated/`. Nothing generated is added here.
- **Any Azure test endpoint for the `local` channel.** L-02 fixes Test/UAT as a local
  production-mimicking stack and ADR-0014 stands; pointing `local` at an Azure resource needs a new
  accepted decision, not a configuration edit.
- **Any third-party logging framework** (Serilog, NLog, log4net) — refused by the Guardrails. The
  sink is this project's own `ILoggerProvider` over `Microsoft.Extensions.Logging`.
- **Any telemetry client in the desktop.** ADR-0109 is "desktop diagnostics bundle + existing App
  Insights; no new telemetry fleet". Do not copy `src/Pegasus.Worker/Program.cs:14-15`.
- **Relaxing `Directory.Build.props`** — never. Narrow, commented `NoWarn` entries in the desktop
  csproj only.
