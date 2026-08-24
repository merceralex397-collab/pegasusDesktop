# Checklist — FND-032

One box per plan step, in plan order. Each names the file or command whose completion makes it true,
so it can be ticked independently and honestly.

- [ ] Read plan 02 § 3 decisions 7 and 9 and plan 04 § 3 item 8 (`docs/desktop/04-auth-session-update-and-startup/README.md:198-199`); read `src/Pegasus.Desktop/App.xaml.cs` as [[FND-030]] (plan handle `DSK-02-05`) left it; read `src/Pegasus.Desktop.Infrastructure/Diagnostics/IDiagnosticsWriter.cs` and `Api/PegasusHttpClientRegistration.cs` as [[FND-031]] (plan handle `DSK-02-06`) left them, noting whether `GatewayOptions` already exists there.
- [ ] Confirm both [[FND-030]] and [[FND-031]] have landed, then `get_doc_gates FND-032` and `take_ticket` on branch `task/desktop-host` from `origin/dev`.
- [ ] Run `microsoft_docs_search` for `Host.CreateApplicationBuilder` and confirm the current generic-host builder API before writing any host code.
- [ ] Add `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Configuration.Binder` and `Microsoft.Extensions.Options.DataAnnotations` to `Directory.Packages.props` and reference them from `src/Pegasus.Desktop/Pegasus.Desktop.csproj` with no version literals.
- [ ] After the solution restore, run `git diff --stat src/*/packages.lock.json` and confirm the server projects' lock files are unchanged; if either moved, raise the central pin rather than accept it.
- [ ] Create `src/Pegasus.Desktop/Configuration/appsettings.json` plus `appsettings.local.json`, `appsettings.pilot.json` and `appsettings.production.json`, each holding exactly `Gateway:BaseAddress`, `Update:FeedUri` and `Channel` and nothing else. Point `local` at the local Test/UAT stack (L-02), never at an Azure resource.
- [ ] Add `<PegasusChannel Condition="'$(PegasusChannel)'==''">local</PegasusChannel>` to `src/Pegasus.Desktop/Pegasus.Desktop.csproj`.
- [ ] Embed `Configuration/appsettings.json` and `Configuration/appsettings.$(PegasusChannel).json` as `EmbeddedResource` with **fixed** `LogicalName` values, following the idiom at `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:31-53`.
- [ ] Write `src/Pegasus.Desktop/Hosting/PegasusHost.cs`: two `AddJsonStream` calls over the fixed logical names, and `GatewayOptions` / `UpdateOptions` / `ChannelOptions` bound with data-annotation validation and `ValidateOnStart`.
- [ ] In `PegasusHost.cs`, register `AddPegasusApiClient(…)` from [[FND-031]], the credential store rooted at `ApplicationData.Current.LocalFolder.Path`, and the bounded cache — reusing [[FND-031]]'s `GatewayOptions` rather than declaring a second one.
- [ ] Confirm `PegasusHost.cs` registers **no** navigation or dialog service and creates no empty interface for one ([[FND-033]], plan handle `DSK-02-08`, owns them; `docs/engineering.md` § Abstractions `:113` forbids dormant scaffolding).
- [ ] Confirm the host is **built**, not `Run`-blocked — WinUI keeps the UI thread and the dispatcher.
- [ ] Write `src/Pegasus.Desktop/Logging/DiagnosticsLoggerProvider.cs` as an `ILoggerProvider` over `Microsoft.Extensions.Logging` writing through [[FND-031]]'s `IDiagnosticsWriter`, after `builder.Logging.ClearProviders()`. No third-party logging framework.
- [ ] Configure the sink with an explicit total-size cap and file-retention count, and generate a per-launch session identifier once at host build, attached to every log scope alongside the request correlation id.
- [ ] Confirm the redaction rule (bearer tokens, refresh tokens, `Authorization` values, password fields, values keyed `token`/`secret`/`password`) lives in exactly one place — [[FND-031]]'s `IDiagnosticsWriter` message processor — and is not re-implemented here or in [[FND-036]] (plan handle `DSK-02-11`).
- [ ] Change `src/Pegasus.Desktop/App.xaml.cs` to build the host in `OnLaunched` before creating the window, hold it behind one static accessor, and dispose it on `Application.Current.Exit`. Leave the pre-window region clean for [[FND-035]] (plan handle `DSK-02-10`).
- [ ] Write the four tests in `tests/Pegasus.Desktop.ViewModelTests` ([[FND-038]], plan handle `DSK-02-13`): host resolution without a dispatcher; missing `Gateway:BaseAddress` failing **at start**; a planted bearer token absent from the log file **while the surrounding message survives**; rotation past the size cap leaving exactly the retention count. If [[FND-038]] has not landed, sequence it first and record the sequencing in the plan.
- [ ] Run `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun`, then the same command async; confirm `✅ <pkg> launched (PID: …)`, a visible window **and** a non-empty log file whose first line carries the session identifier.
- [ ] Run `dotnet build ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -c Release -p:PegasusChannel=pilot` and confirm the embedded-resource list contains `appsettings.pilot.json` and **not** `appsettings.production.json`. If the unselected files are still embedded, record that the build-time channel gives no security benefit.
- [ ] Add the composition note (generic host, channel-selected embedded configuration) to `docs/current-architecture.md` § Components and dependency direction (`:55`).
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] Verification run (this box produces `proof`, evidence tier 2): `dotnet build ./Pegasus.slnx --configuration Release` (exit 0, `0 Warning(s)` — the authoritative gate); `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` (the four cases above); the pilot-channel build with its resource list pasted; the async `BuildAndRun.ps1` launch with the log's first three lines; and `grep -rniE '(password|secret|token|connectionstring|accountkey)' src/Pegasus.Desktop/Configuration/` returning **no matches**. Write the honesty clauses into the proof: `BuildAndRun.ps1` green ≠ `dotnet build` green, and no CI job builds the desktop until [[FND-040]] (plan handle `DSK-02-15`) lands.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
