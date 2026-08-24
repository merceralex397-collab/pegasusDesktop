# Research — FND-032: generic-host composition, channel configuration and bounded redacted logging

## Question

How must the desktop compose its host, configuration and logging so that every later view model
resolves its dependencies from one container, a package cannot be re-pointed at another channel after
it ships, and no token ever reaches a log file — and what does the repository already do for each of
those three that should be mirrored rather than invented?

## Current behaviour

**No parity-matrix row covers this ticket, and none should.** The matrix at
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted with
`grep -c '^| PAR-'`, which returns **46** — and every row is "keyed by the Razor page model and
handler group that implements it today" (`parity-matrix.md:3-5`). Application composition has no page
model.

The closest existing repository mechanisms — what does each of the three jobs today:

- **Composition and configuration**: `src/Pegasus.Web/Program.cs:100` builds a
  `WebApplication.CreateBuilder(applicationArgs)` and reads configuration positionally:
  `builder.Configuration["Runtime:Profile"] ?? throw new InvalidOperationException("Runtime:Profile is required.")`
  (`:101-102`). Layering is the ASP.NET default — `src/Pegasus.Web/appsettings.json` plus
  `src/Pegasus.Web/appsettings.Development.json` (the only two configuration files in `src/`).
- **Fail-at-start on bad configuration**: the same file, `:104-110`, refuses to start when the
  `DevelopmentOffline` runtime profile is configured outside the Development environment
  (`throw new InvalidOperationException("The DevelopmentOffline runtime profile is permitted only in the Development environment.")`),
  and `:112-116` refuses `Features:LocalIntake` outside that profile. This is the repository's
  fail-closed configuration precedent, and it is an **explicit throw**, not options validation.
- **Logging**: `src/Pegasus.Web/appsettings.json` carries a `Logging:LogLevel` section
  (`Default: Information`, `Microsoft.AspNetCore: Warning`) and the server ships telemetry to
  Application Insights (`src/Pegasus.Web/Pegasus.Web.csproj:38`).
- **Redaction**: the only redaction in the repository is type-level —
  `src/Pegasus.Core/Documents/RequestUploadPolicy.cs:110`,
  `public override string ToString() => "[REDACTED]";`. There is no log-sink redaction anywhere, so
  the message processor this ticket builds is genuinely new.

## Findings

### Facts

Verified by reading the repository at fork `main`, 2026-08-24. Each carries its source.

- **The options-validation idiom does not exist in this repository.**
  `grep -rn "ValidateOnStart\|ValidateDataAnnotations\|AddOptions<" src/` returns **nothing**. The
  bind-and-validate pattern step 5 asks for is new here, not a local convention to copy — which is why
  step 2's `microsoft_docs_search` for `Host.CreateApplicationBuilder` and `IOptions` validation is a
  real step and not ceremony.
- **The fail-closed configuration precedent is an explicit throw.**
  `src/Pegasus.Web/Program.cs:101-102` and `:104-110`. Whatever validation mechanism is chosen, its
  observable behaviour must match: the process refuses to start, with a message naming the setting.
- **`src/Pegasus.Web/appsettings.json` is the live counter-example for the "no secrets in shipped
  configuration" rule.** It carries a `Bootstrap:VerificationAccount` block with a plaintext user name
  and password, guarded by a long `"//"` comment that calls it "TEMPORARY", explains it was recorded
  at the operator's request, and says "Never leave it configured in a real production deployment."
  This is exactly the failure mode the desktop's acceptance criterion — "No secret, token or
  connection string appears in any `appsettings*.json` shipped in the package" — exists to prevent,
  and the desktop's files ship *inside a signed package on operator workstations*, where the
  server-side mitigation (redeploy and remove) does not exist. The rule is not a formality.
- **Only two configuration files exist under `src/` today**:
  `src/Pegasus.Web/appsettings.json` and `src/Pegasus.Web/appsettings.Development.json`. The four
  files this ticket creates under `src/Pegasus.Desktop/Configuration/` are new, and they are
  **embedded resources**, not content files — a different mechanism from the server's on-disk layering.
- **`src/Pegasus.Desktop` and `src/Pegasus.Desktop.Infrastructure` do not exist yet.** `ls src` returns
  exactly `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`. Every file this
  ticket edits is created by [[FND-030]] (plan handle `DSK-02-05`) or [[FND-031]] (plan handle
  `DSK-02-06`).
- **`Directory.Packages.props` does not exist** (`ls` → *No such file*); [[FND-027]] (plan handle
  `DSK-02-02`) creates it. The three packages step 2 adds assume it has landed.
- **None of `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Configuration.Binder` or
  `Microsoft.Extensions.Options.DataAnnotations` is referenced anywhere today.**
  `grep -rn "PackageReference Include" src/*/*.csproj` lists 33 references and none of them is an
  `Microsoft.Extensions.*` package — the server gets Hosting from `Microsoft.NET.Sdk.Web` and the
  Worker from `Microsoft.Azure.Functions.Worker`. A `Microsoft.NET.Sdk` desktop project has neither,
  so all three are genuinely required.
- **`Directory.Build.props` (19 lines) applies**: `TreatWarningsAsErrors=true`,
  `AnalysisLevel=latest-recommended`, `Nullable`, `ImplicitUsings`, `LangVersion=latest`,
  `Deterministic=true`, `Version=0.1.0-alpha.1`.
- **A `BuildAndRun.ps1` build does not carry those settings.** Measured at
  `.codex/skills/winui-dev-workflow/BuildAndRun.ps1:146-157`: the script tests for
  `Directory.Build.props` **in the project directory only** and writes one there when absent; MSBuild
  stops at the first such file walking up, so the injected file shadows the repository root props for
  that build. Consequence for step 11's channel check: use a plain `dotnet build … -p:PegasusChannel=pilot`,
  not the script, when the result must be trusted.
- **Plan 04 § 3 item 8 fixes the payload of the configuration files exactly**: "the package carries
  only the gateway base URL, feed URL, and channel name per channel (02's embedded
  `appsettings.<channel>.json`)". Three settings, no fourth.
- **Plan 04 § 3 item 6 is what the channel configuration serves**: the last successful compatibility
  response is cached locally for 24 hours and the app fails closed beyond that — so the base address
  a package carries is the only gateway it can ever reach, with no bypass (§ 9.3).
- **Plan 02 § 3 decision 7** fixes the composition: `Microsoft.Extensions.Hosting` generic host inside
  `App.xaml.cs`, `IHttpClientFactory` single pipeline, structured logging to a bounded rolling file
  sink with redaction, configuration layered as embedded `appsettings.json` +
  `appsettings.<channel>.json` selected by an **MSBuild property at package time**, channel =
  `pilot` | `production` | `local`.
- **Plan 02 § 3 decision 9** forbids a desktop framework on top of WinUI: "a shell service, a
  navigation service, a dialog service, and a handful of project controls".
- **Proposal § 18.1** fixes the log contract: structured rolling local logs, a per-launch session
  identifier, API correlation identifiers, redaction by default, bounded size and retention.
- **ADR-0109** (desktop diagnostics bundle plus the existing Application Insights, no new telemetry
  fleet) bounds the design and is authored by [[FND-006]] (plan handle `DSK-00-06`).
- **L-02 (locked)** makes the `local` channel point at the local Test/UAT stack — local gateway and
  Worker processes, Azurite, LocalDB/SQL container, replay adapters — never at an Azure test resource;
  ADR-0014 stands.
- **`tests/Pegasus.Desktop.ViewModelTests` does not exist** (`ls tests` → three projects only);
  [[FND-038]] (plan handle `DSK-02-13`) creates it, and every test in step 9 lands there.
- **The interfaces step 5 registers do not exist yet either.** `IDiagnosticsWriter`,
  `AddPegasusApiClient` and `IDesktopCredentialStore` are created by [[FND-031]];
  `INavigationService` and `IDialogService` by [[FND-033]] (plan handle `DSK-02-08`). The body's
  instruction not to create empty interfaces now is therefore load-bearing, not cautionary.

### Assumptions

- **A-FND032-1 — a `Microsoft.Extensions.Hosting` generic host can be built and disposed inside a WinUI
  `Application` lifetime without a background service loop.** The host here is a service container and
  configuration/logging root, not a running service. *Confirms it*: the fake-host fixture in step 9
  resolving `GatewayOptions`, the API client and the credential store. *If wrong*: composition falls
  back to a bare `ServiceCollection` plus manual configuration binding, which changes step 5's shape
  but none of its obligations.
- **A-FND032-2 — `AddJsonStream` over two embedded resources layers the same way as
  `AddJsonFile` does on disk** (later stream wins). *Confirms it*: an override test — set
  `Gateway:BaseAddress` in both files and assert the channel file wins. *If wrong*, a channel file
  silently fails to override the base and every package points at the same gateway, which is the worst
  possible failure of this ticket and would not show up in a smoke test.
- **A-FND032-3 — an `EmbeddedResource` with a fixed logical name whose `Include` is computed from
  `$(PegasusChannel)` embeds exactly one channel file.** *Confirms it*: step 11's inspection of the
  built assembly's manifest resource names. *If wrong*, either no channel file or all four are
  embedded; the "only the pilot channel file embedded" check catches both.
- **A-FND032-4 — the redaction processor can be applied to the formatted message and its structured
  state without losing the surrounding message.** *Confirms it*: the planted-token test in step 9,
  which asserts both that the token is absent **and** that the surrounding message survives. *If
  wrong*, redaction becomes message suppression and the log stops being usable for support — which,
  with ADR-0109 making the bundle the only support channel, is a real operational cost.
- **A-FND032-5 — a per-launch session identifier attached to the logger scope at host build appears on
  every subsequent line.** *Confirms it*: step 10's inspection of the first line of a real log file,
  plus a fixture assertion. *If wrong*, the diagnostics bundle from [[FND-036]] (plan handle
  `DSK-02-11`) cannot correlate a crash with the session that produced it.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | The host, its options and its log files are per-process and per-workstation. Nothing composed here is shared between operators. |
| Unattended execution — must it run with every desktop closed? | **No** | The host lives inside the operator's application session and is disposed on `Application.Current.Exit`. Unattended work stays in `Pegasus.Worker` under ADR-0106. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No, and the ticket's acceptance criteria enforce it.** | Plan 04 § 3 item 8: the package carries "only the gateway base URL, feed URL, and channel name per channel" — three settings, no secret. The one credential the desktop holds at all is the short-lived refresh handle in [[FND-031]]'s DPAPI store, never in configuration. The counter-example that makes this rail real is server-side: `src/Pegasus.Web/appsettings.json` carries a plaintext `Bootstrap:VerificationAccount`, marked TEMPORARY in its own comment — a shipped desktop package cannot be fixed by a redeploy the way that can. |
| Public callback — must an external service call a stable public endpoint? | **No** | The host makes outbound calls only. The `Update:FeedUri` it carries is, under **D-003**, a UNC path on an in-house Windows host served over SMB — deliberately not a public HTTPS endpoint, because **C-01** rules anonymous GitHub-hosted feeds out permanently. |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **Yes — and it lands on the in-house build/signing host and the already-existing gateway, not on any new Azure resource.** | The channel invariant is enforced at **package time**, not at runtime: the channel file is selected by the `PegasusChannel` MSBuild property and embedded, so "a pilot package cannot be pointed at production by editing a file on disk". That enforcement lives wherever the package is built and signed — the in-house signing host under **D-002**, exercised by the CI lane [[FND-040]] (plan handle `DSK-02-15`). The complementary runtime enforcement — revocation and the minimum-version gate — is already placed on the evolved `Pegasus.Web` gateway (plan 04 § 3 items 3 and 5, ADR-0105), where raising the minimum version is a database-backed administrative action and explicitly **not** an Azure write. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | None claimed or available. The placement follows from plan 02 § 3 decision 7 and D-002/D-003. |

**Conclusion.** Four "no" and one "yes"; the "yes" names the in-house build host and the existing
gateway. No responsibility is placed in Azure and no Azure write arises. The `local` channel points at
the local Test/UAT stack under L-02 — asking for an Azure test endpoint would need a new accepted
decision against ADR-0014.

## Implications

1. **Build-time channel selection is a security control, not a convenience.** It is the reason a
   `pilot` package cannot be re-pointed at production by editing a file, and it is the one thing in
   this ticket that a runtime configuration mechanism would quietly undo. Step 4's `EmbeddedResource`
   with a `$(PegasusChannel)`-computed `Include` is therefore not interchangeable with an on-disk
   `appsettings.<channel>.json`.
2. **Step 11's check needs a plain `dotnet build`.** `BuildAndRun.ps1` injects a project-level
   `Directory.Build.props` that shadows the root one (`BuildAndRun.ps1:146-157`), so a build through
   the script is not the build CI performs. Use
   `dotnet build ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -c Release -p:PegasusChannel=pilot` and
   inspect the produced assembly's manifest resource names.
3. **The options-validation idiom has no local precedent**, so the *observable* behaviour is what must
   match `src/Pegasus.Web/Program.cs:101-110`: refuse to start, naming the missing setting. Whether
   that is `ValidateDataAnnotations().ValidateOnStart()` or an explicit throw is an implementation
   choice; the test in step 9 asserts the behaviour either way.
4. **Registering nothing that has no caller is enforceable here.** `docs/engineering.md` § Abstractions
   (`:113`) — "Anything built but unwired for two weeks gains a real caller or is deleted". Since
   `INavigationService` and `IDialogService` do not exist until [[FND-033]], creating empty interfaces
   now would be exactly the dormant registration that rule forbids. The plan must sequence, not
   pre-declare.
5. **Redaction has one home.** It belongs in the sink's message processor so that [[FND-036]] can
   re-apply the *same* rule at bundle collection rather than writing a second regex set. A rule
   implemented twice is the third-copy failure applied to security code, where drift is silent.
6. **The `local` channel is a real constraint, not a placeholder.** L-02 fixes it at the local
   Test/UAT stack; writing an Azure endpoint into `appsettings.local.json` would breach ADR-0014
   without a decision.

## Open questions

- None that must be answered before implementation. Every unknown above is an assumption with a named
  command inside this ticket that settles it, and the two sequencing points — [[FND-031]]'s
  interfaces and [[FND-038]]'s test project — are scope boundaries with named owners, recorded in the
  plan's Risks section rather than opened as questions.
