# Checklist — FND-032

One box per plan step, in plan order. Each is independently tickable: it names the file or command
whose completion makes the box true.

- [ ] Read `src/Pegasus.Desktop/App.xaml.cs` and `src/Pegasus.Web/Program.cs:100-116` (the fail-closed configuration precedent the options validation must match behaviourally); run `get_doc_gates FND-032`; `take_ticket` on branch `task/desktop-host` from `origin/dev`.
- [ ] Add `PackageVersion` entries for `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Configuration.Binder` and `Microsoft.Extensions.Options.DataAnnotations` to `Directory.Packages.props`, and reference all three from `src/Pegasus.Desktop/Pegasus.Desktop.csproj` without version literals.
- [ ] Run `microsoft_docs_search` for `Host.CreateApplicationBuilder` and confirm the current builder API before writing `PegasusHost.cs`.
- [ ] Create `src/Pegasus.Desktop/Configuration/appsettings.json`, `appsettings.local.json`, `appsettings.pilot.json` and `appsettings.production.json`, each holding exactly `Gateway:BaseAddress`, `Update:FeedUri` and `Channel` and nothing else.
- [ ] Confirm `Update:FeedUri` accepts the D-003 UNC form `\\<host>\<share>\<channel>\Pegasus.appinstaller`, and that `appsettings.local.json` points at the local Test/UAT stack (L-02), never an Azure test resource.
- [ ] Add `<PegasusChannel Condition="'$(PegasusChannel)'==''">local</PegasusChannel>` to `src/Pegasus.Desktop/Pegasus.Desktop.csproj` and embed `Configuration/appsettings.json` plus `Configuration/appsettings.$(PegasusChannel).json` as `EmbeddedResource` with fixed logical names.
- [ ] Write `src/Pegasus.Desktop/Hosting/PegasusHost.cs`: two `AddJsonStream` calls (base first, channel second); `GatewayOptions` / `UpdateOptions` / `ChannelOptions` bound with data-annotation validation and `ValidateOnStart`; `AddPegasusApiClient(…)`; the credential store rooted at `ApplicationData.Current.LocalFolder.Path`; the bounded cache.
- [ ] Confirm no empty `INavigationService` / `IDialogService` interfaces were created — they are registered only once [[FND-033]] (plan handle `DSK-02-08`) defines them.
- [ ] Wire logging: `builder.Logging.ClearProviders()` then the `IDiagnosticsWriter`-backed `ILoggerProvider`, with an explicit total-size cap and file-retention count.
- [ ] Generate a per-launch session identifier once at host build and attach it to every log scope alongside the request correlation id.
- [ ] Implement the redaction message processor in the sink — bearer tokens, refresh tokens, `Authorization` values, password fields, and any value keyed `token` / `secret` / `password` — preserving the surrounding message; confirm it is the only such rule in the repository.
- [ ] Change `App.xaml.cs` to build the host in `OnLaunched` before creating the window, hold it in one static accessor, and dispose it on `Application.Current.Exit`.
- [ ] Add the tests in `tests/Pegasus.Desktop.ViewModelTests`: fake-host resolution of `GatewayOptions` / API client / credential store; missing `Gateway:BaseAddress` fails at start; planted bearer token absent while the surrounding message survives; rotation past the size cap honours the retention count; **and** the configuration override test (base and channel both set `Gateway:BaseAddress`; the channel wins).
- [ ] Run `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun`, then the same command asynchronously; confirm `✅ <pkg> launched (PID: …)` and a log file under the packaged app's local folder whose first line carries the session identifier.
- [ ] Run the plain build `dotnet build ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -c Release -p:PegasusChannel=pilot` and inspect the manifest resource names: `appsettings.pilot.json` present, the other two channel files absent.
- [ ] Add the composition entry to `docs/current-architecture.md` § Components and dependency direction (`:55`).
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] Verification run (this box produces `proof`): `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` (all five tests named); the `-p:PegasusChannel=pilot` build plus its manifest resource listing; the async launch with the log file's first lines; `grep -rniE 'password|secret|token|connectionstring|AccountKey|SharedAccessSignature' src/Pegasus.Desktop/Configuration/` (no matches); and a confirmation that each configuration file holds exactly three settings. Capture every output as tier-2 evidence.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
