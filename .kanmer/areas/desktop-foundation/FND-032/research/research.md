# Research — FND-032: composing the desktop on a generic host with channel-selected configuration and a redacting log sink

## Question

What does this repository already do for host composition, configuration layering, options validation
and log redaction — and which of those patterns can the desktop reuse rather than invent?

## Current behaviour

**No parity-matrix row covers this ticket, and none should.** The matrix at
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted with
`grep -c '^| PAR-'`, which returns **46** — and every row is keyed to a page model under
`src/Pegasus.Web/Pages/**`. Application composition is infrastructure work with no
operator-observable capability of its own, so it is outside the matrix by construction.

The closest existing repository mechanisms — what does this job today:

- **`src/Pegasus.Web/Program.cs:100`** — `WebApplication.CreateBuilder(applicationArgs)` is the web
  application's composition root. It reads configuration through raw string indexers
  (`builder.Configuration["Runtime:Profile"]` at `:101`) and enforces required settings by **throwing
  manually**: `:101-103` throws `"Runtime:Profile is required."`; `:145` loops a key list and checks
  `string.IsNullOrWhiteSpace(builder.Configuration[key])`. This is the repository's existing
  "required configuration" idiom.
- **`src/Pegasus.Worker/Program.cs:9-11`** — `new HostBuilder().ConfigureFunctionsWorkerDefaults()`,
  the generic-host precedent, though wrapped in the Functions worker defaults the desktop will not
  use.
- **`src/Pegasus.Web/Program.cs:100-111`** — the `Runtime:Profile` gate is the closest analogue to
  the channel model this ticket introduces: a named profile that changes what composes, validated at
  start, and refused outright in the wrong environment
  (`"The DevelopmentOffline runtime profile is permitted only in the Development environment."`).
- **`src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:31-53`** — twelve `EmbeddedResource`
  items, each carrying an explicit `LogicalName`, with one (`:48-51`) also using `<Link>`. This is
  the repository's established embedded-resource idiom and the precedent step 4 follows.

## Findings

### Facts

Verified by reading the repository at fork `main`, 2026-08-24. Each carries its source.

- **`src/Pegasus.Desktop` does not exist yet.** `ls src` returns exactly `Pegasus.Core`,
  `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`. `App.xaml.cs` and the csproj this ticket
  edits are created by [[FND-030]] (plan handle `DSK-02-05`); `IDiagnosticsWriter` and
  `AddPegasusApiClient` are created by [[FND-031]] (plan handle `DSK-02-06`). Both are hard
  prerequisites in practice, and the plan's dependency arrow names only [[FND-030]].
- **This repository has no options-validation pattern at all.**
  `grep -rn "ValidateOnStart\|ValidateDataAnnotations\|AddOptions<" src/ --include=*.cs` returns
  **zero matches**. Strongly-typed options with data-annotation validation and `ValidateOnStart` are
  therefore genuinely *new* to this codebase, not an extension of an existing convention. The
  incumbent idiom is the manual throw at `src/Pegasus.Web/Program.cs:101-103` and `:145-151`.
- **This repository has no log-redaction mechanism.** `grep -rln "Redact\|redact" src/ --include=*.cs`
  returns nothing. The only `Scrub`/`Sanitiz*` hits are
  `src/Pegasus.Infrastructure/Email/GraphApprovedSources.cs` and
  `src/Pegasus.Infrastructure/Intake/MimeKitPdfPigOpenXmlIntakeSourceReader.DocMsg.cs`, and neither
  is about log output. The redaction rule this ticket implements has no prior art here to copy, which
  is exactly why step 7 requires a fixture test rather than inspection.
- **`src/Pegasus.Web/appsettings.json` ships a plaintext password.** The `Bootstrap.VerificationAccount`
  block carries `"UserName": "claudeuiverification"` and
  `"Password": "Pegasus-UI-Verify-2026!"`, above a `"//"` comment saying it is deliberate,
  temporary, and "Never leave it configured in a real production deployment." This is the precise
  mistake the desktop must not repeat, and it is why this ticket's acceptance criterion "no secret,
  token or connection string appears in any `appsettings*.json` shipped in the package" is checkable
  rather than rhetorical: the desktop's files are **embedded in an MSIX distributed to
  workstations**, so a secret there is shipped to every operator's disk.
- **The package is specified to carry nothing secret.**
  `docs/desktop/04-auth-session-update-and-startup/README.md:198-199` § 3 item 8: "**Secrets in the
  package**: none. The package carries only the gateway base URL, feed URL, and channel name per
  channel (02's embedded …)". That is exactly the three settings step 3 permits, and the sentence
  names area 02 — this ticket — as its implementer.
- **`EmbeddedResource` with `LogicalName` is established.**
  `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:31-53`, e.g.
  `<EmbeddedResource Include="Persistence\ReferenceData\provider-domains.v1.json" LogicalName="Pegasus.Infrastructure.Persistence.ReferenceData.provider-domains.v1.json" />`.
  A **fixed** logical name is what makes step 4's channel selection safe: the file on disk changes
  name per channel, the logical name the code reads does not, so `AddJsonStream` needs no
  channel-aware lookup.
- **`Directory.Build.props` (19 lines) applies to this project too**: `TreatWarningsAsErrors=true`,
  `AnalysisLevel=latest-recommended`, `Nullable`, `ImplicitUsings`, `LangVersion=latest`,
  `Deterministic=true`, `Version=0.1.0-alpha.1`.
- **`Directory.Packages.props` does not exist today** — `ls Directory.Packages.props` → *No such
  file or directory*. It is created by [[FND-027]] (plan handle `DSK-02-02`); step 2 writes
  `PackageVersion` entries into a file that ticket creates.
- **Configuration precedence in this repository is by literal key string, everywhere.** There is no
  central key catalogue: `Program.cs` reads `Runtime:Profile`, `Features:LocalIntake`,
  `AzureIdentity:WebClientId`, `CustodyStorage:ServiceUri`, `Graph:BaseUri`, `Box:ClientSecret` and
  a dozen more as bare strings. `AGENTS.md` § Simplicity rails ("one list per concept") is why the
  desktop's three keys should be constants bound once through options classes rather than repeated
  as literals across the code.
- **`Pegasus.Web` and `Pegasus.Worker` both talk to Application Insights**
  (`src/Pegasus.Worker/Program.cs:14-15`, `src/Pegasus.Web/Program.cs:194-197`). The desktop does
  **not**: ADR-0109 is "desktop diagnostics bundle + existing App Insights; no new telemetry fleet"
  (`docs/desktop/00-governance-and-workflow/README.md` § 3 ADR set table). Nothing in this ticket
  adds a telemetry client to the desktop.

Official documentation, to be re-confirmed at kickoff (the ticket body's step 2 requires it):

- `Host.CreateApplicationBuilder` / `HostApplicationBuilder` — the current generic-host builder API
  for non-web applications; the body instructs `microsoft_docs_search` for it before writing code.
- `AddJsonStream` — the `IConfigurationBuilder` extension that reads configuration from a `Stream`,
  which is how an embedded resource becomes configuration without a file on disk.
- `ILoggerProvider` — the extension point for a custom sink over `Microsoft.Extensions.Logging`; the
  Guardrails forbid a third-party logging framework.

### Assumptions

- **A-FND032-1 — `Microsoft.Extensions.Hosting` composes cleanly inside a WinUI 3 `App.xaml.cs`
  without a second message loop.** The generic host owns a lifetime; WinUI owns the dispatcher. The
  host must be *built* (not `RunAsync`-ed as a blocking call) so WinUI keeps the UI thread.
  *Confirms it*: step 10's launch, which must show a window **and** a written log file — a host that
  seized the thread would show neither. *If wrong*: build the host and start hosted services
  explicitly rather than calling a blocking `Run`, and record the deviation.
- **A-FND032-2 — data-annotation options validation is available without pulling in ASP.NET Core.**
  `Microsoft.Extensions.Options.DataAnnotations` is the named package in step 2. *Confirms it*: the
  build at step 10 with no `Microsoft.AspNetCore.*` reference appearing in the desktop csproj. *If
  wrong*: hand-written validation in the options classes' own validator, still failing at start —
  never a silent default.
- **A-FND032-3 — an `EmbeddedResource` whose `Include` uses `$(PegasusChannel)` evaluates per build
  and the unselected channel files are genuinely absent from the assembly.** *Confirms it*: step 11
  builds with `-p:PegasusChannel=pilot` and inspects the embedded-resource list. *If wrong*, the
  security property "a pilot package cannot be pointed at production" is lost, and that must be
  reported rather than glossed — it is the reason the channel is a build-time property at all.
- **A-FND032-4 — the packaged app's local folder is writable at the moment the host is built.**
  `ApplicationData.Current.LocalFolder.Path` requires package identity. *Confirms it*: step 10's log
  file appearing under that folder. *If wrong*: the log sink must degrade to no-op rather than crash
  the launch, and that behaviour is then itself a tested case.
- **A-FND032-5 — `TreatWarningsAsErrors=true` is survivable for the hosting and options code.**
  *Confirms it*: a plain `dotnet build ./Pegasus.slnx --configuration Release` reporting
  `0 Warning(s)`. *If wrong*: narrow, individually-commented `NoWarn` entries in the desktop csproj —
  never a relaxation of `Directory.Build.props`.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | The host, its options and its log sink are per-process and per-workstation. Nothing composed here is shared between operators; all shared state stays behind the gateway under L-01. |
| Unattended execution — must it run with every desktop closed? | **No** | The host lives exactly as long as the desktop application. Unattended work stays in `Pegasus.Worker` under ADR-0106, which this ticket does not touch. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No — and this ticket is the one that *enforces* that answer.** | `docs/desktop/04-auth-session-update-and-startup/README.md:198-199` § 3 item 8: "Secrets in the package: none. The package carries only the gateway base URL, feed URL, and channel name per channel." Step 3 permits exactly those three settings and nothing else. The refresh handle lives in the DPAPI store from [[FND-031]]; the access token stays in memory. |
| Public callback — must an external service call a stable public endpoint? | **No** | Nothing here listens. The desktop only makes outbound calls to the gateway, and under D-003 the update feed is a UNC share over SMB, deliberately not a public HTTPS endpoint (C-01 rules GitHub Releases and Pages out permanently). |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **Yes, for one setting — and it lands on the already-existing evolved `Pegasus.Web` gateway, not on any new Azure resource.** | The channel a package was built for is a *client* fact, but what that channel is allowed to do is enforced server-side: ADR-0105's minimum-version gate runs in the compatibility middleware on `/api/v1`, and the minimum version is "a database-backed Administrator setting with audit … not a Container App app setting" (plan 04 § 3 item 5), so raising it is an authenticated administrative action and **not** an Azure write. Making the channel a build-time MSBuild property (step 4) is precisely what stops the client from re-deciding it locally. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | None claimed. Logs stay local and bounded under ADR-0109 ("no new telemetry fleet"); shipping desktop logs to a central store is explicitly *not* the design, and no benchmark argues for it. |

**Conclusion.** Five "no" and one "yes"; the "yes" names the existing gateway, and the one
credential-shaped question is answered "none" by design and enforced by this ticket's step 3.
Nothing here places any responsibility in Azure, and no Azure write arises.

## Implications

1. **Two patterns in this ticket are new to the repository, not adaptations.** Options validation
   (`ValidateOnStart`, zero existing matches) and log redaction (zero existing matches) have no prior
   art in `src/`. The plan must therefore specify their behaviour concretely and test them, rather
   than pointing at an existing implementation to copy. The nearest precedent — the manual throw at
   `src/Pegasus.Web/Program.cs:101-103` — is a *fail-at-start* precedent, which is the property to
   preserve even if the mechanism differs.
2. **`src/Pegasus.Web/appsettings.json`'s plaintext password is the argument for step 3's strictness,
   and it should be cited in the review.** A web application's `appsettings.json` sits on a server the
   operator controls; a desktop `appsettings.json` is embedded in an MSIX and copied to every
   workstation. The same mistake has a materially worse blast radius here.
3. **The channel must be a build-time property, and step 11 is what proves it.** If the unselected
   channel files remain embedded, "a pilot package cannot be pointed at production by editing a file
   on disk" is false, and the plan's whole justification for an MSBuild property over a runtime
   switch collapses. That check is not optional polish.
4. **The `LogicalName` idiom is what keeps the reading code channel-agnostic.** Embedding
   `Configuration/appsettings.$(PegasusChannel).json` under a *fixed* logical name means
   `PegasusHost` calls `AddJsonStream` twice with two constant names and never learns which channel
   it is — the channel appears in exactly one place, the csproj.
5. **The host must not seize the UI thread.** WinUI owns the dispatcher; the host is built and its
   services started, not `Run`-blocked. A-FND032-1 is settled by the fact that step 10 requires both
   a visible window and a written log file.
6. **The redaction hook belongs to [[FND-031]]'s `IDiagnosticsWriter`, defined once.** [[FND-036]]
   (plan handle `DSK-02-11`) re-collects those logs into the bundle and must call the same hook
   rather than re-implementing the regex set — "one list per concept" (`AGENTS.md` § Simplicity
   rails).

## Open questions

- None that must be answered before implementation. Every assumption above names the command inside
  the ticket that settles it, and no value here needs an operator decision — the three configuration
  keys are fixed by plan 04 § 3 item 8, and the channel names (`local`, `pilot`, `production`) are
  fixed by plan 02 § 3 decision 7.
- Two sequencing facts are recorded rather than opened as questions: [[FND-031]] must have landed
  (`IDiagnosticsWriter`, `AddPegasusApiClient`) and [[FND-038]] (plan handle `DSK-02-13`) must exist
  before step 9's tests can be written. Both are scope boundaries with named owners, recorded in the
  plan's Risks section.
