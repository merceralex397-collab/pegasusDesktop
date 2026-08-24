# Plan — FND-032: Generic Host, DI, options, channel-selected configuration and bounded redacted logging in `App.xaml.cs`

**Diff estimate: ~11 files, ~330 lines** (excluding the regenerated `packages.lock.json`).

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. Derived from the files
document: `Hosting/PegasusHost.cs` ~110; `Logging/DiagnosticsLoggerProvider.cs` ~70;
`Logging/RedactionMessageProcessor.cs` ~55; four `Configuration/appsettings*.json` files at ~7 lines
each (~28); `App.xaml.cs` edits ~30; `Pegasus.Desktop.csproj` edits ~14 (the `PegasusChannel`
property, two `EmbeddedResource` items, three `PackageReference` lines);
`Directory.Packages.props` +3; `docs/current-architecture.md` ~+3. The four tests land in
`tests/Pegasus.Desktop.ViewModelTests` and are counted against that project.

## Approach

Build **one** host, inside `App.xaml.cs`, before the first window, and make the channel a **build-time**
property rather than a runtime setting. The channel decision is the load-bearing one: an on-disk
`appsettings.<channel>.json` beside the executable would be simpler to author and to change, and that
is exactly why it is rejected — an operator or a support script could re-point a pilot package at
production by editing a file, and a signed MSIX gives no other protection against it. Embedding the
base and the `$(PegasusChannel)`-selected file as resources with fixed logical names means the channel
is fixed at the moment the package is built and signed, which under D-002 is the in-house signing host.
Plan 02 § 3 decision 7 specifies exactly this; this plan records *why* so a later simplification pass
does not "helpfully" move the files onto disk.

Two measured facts shape the rest:

- **There is no options-validation idiom in this repository.**
  `grep -rn "ValidateOnStart\|ValidateDataAnnotations\|AddOptions<" src/` returns nothing. The
  repository's fail-closed precedent is an explicit throw at
  `src/Pegasus.Web/Program.cs:101-110`. So the *behaviour* is what must match — refuse to start,
  naming the setting — and step 9's test asserts the behaviour rather than the mechanism.
- **A `BuildAndRun.ps1` build is not the build CI runs.**
  `.codex/skills/winui-dev-workflow/BuildAndRun.ps1:146-157` writes a `Directory.Build.props` into the
  project directory when that exact file is absent, and MSBuild stops at the first one walking up, so
  the root props is shadowed. Step 11's channel proof therefore uses a plain `dotnet build`.

## Governing docs

The ticket's `refs` array is empty and `get_doc_gates FND-032` reports `docs_todo: true`, so there is
no linked PRD/FRD/ADR to meet today.

> **New ADR** — ADR-0109 (desktop diagnostics bundle plus the existing Application Insights; no new
> telemetry fleet), authored by [[FND-006]] (plan handle `DSK-00-06`). It bounds the log design: a
> local, bounded, redacted rolling sink, not a telemetry client. ADR-0104 (online-required, bounded
> local cache) bounds what the cache registered here may hold; it is authored by [[FND-005]] (plan
> handle `DSK-00-05`) and also claimed by [[FND-026]] (plan handle `DSK-02-01`) — see [[FND-026]]'s
> plan for the ownership reconciliation.
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table) and
> `docs/desktop/02-architecture-and-foundation/README.md` § 3 decisions 7 and 9; if either ADR lands
> differently this plan is revised before implementation.

Because `refs` is empty, these are the authorities that actually bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 7.2 Application composition | Hosting for DI, configuration, logging and lifetime; `IHttpClientFactory`; structured logging | Steps 5, 6, 8 |
| Proposal § 18.1 Desktop diagnostics | Structured rolling local logs, a per-launch session identifier, API correlation identifiers, redaction by default, bounded size and retention | Steps 6, 7 |
| Proposal § 21.1 Build properties | Channel selection is a build property | Step 4 |
| Plan 02 § 3 decision 7 | Generic host in `App.xaml.cs`; one HTTP pipeline; bounded redacting rolling sink; embedded base + `appsettings.<channel>.json` selected by an MSBuild property at package time; channels `pilot` / `production` / `local` | Steps 3–8 |
| Plan 02 § 3 decision 9 | No desktop framework on top of WinUI | Step 5 registers only services with a real caller |
| Plan 04 § 3 item 8 | The package carries only the gateway base URL, feed URL and channel name — no secrets | Step 3 |
| Plan 04 § 3 item 6 | The compatibility response is cached 24 hours and the app then fails closed with no bypass — so the embedded base address is the only reachable gateway | Steps 3, 4 |
| **L-02** (locked) | Test/UAT is a local production-mimicking stack; ADR-0014 stands | Step 3 — `appsettings.local.json` points at the local stack, never an Azure test resource |
| **D-003** (locked) | The feed is a UNC share over SMB, `\\<host>\<share>\<channel>\Pegasus.appinstaller` | Step 3 — `Update:FeedUri` must accept a UNC path, not only a URL |
| `docs/engineering.md` § Abstractions (`:113`) | No dormant registration; anything unwired for two weeks gains a caller or is deleted | Step 5 — navigation/dialog services are registered when [[FND-033]] defines them, not before |
| `AGENTS.md` § Simplicity rails | One list per concept | Step 7 — one redaction rule, in the sink's processor, reused by [[FND-036]] |
| `docs/engineering.md` § Required evidence tiers (`:72`), tier 2 | Positive **and** failure cases: resolution, missing-setting failure, redaction of a planted token, rotation past the cap | § Verification |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml` (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`, win-dev-skills v0.5.0 `f1028dd5`).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search` for `Host.CreateApplicationBuilder`,
  `IOptions` validation, `ILoggerProvider` custom provider, `AddJsonStream` embedded configuration).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move;
  a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's twelve steps: same order, same ownership, same paths.

1. **Orient.** Read the current `src/Pegasus.Desktop/App.xaml.cs` (created by [[FND-030]], plan handle
   `DSK-02-05`) and `src/Pegasus.Web/Program.cs:100-116` — the latter is the repository's fail-closed
   configuration precedent and the behaviour step 5's validation must match. Then
   `get_doc_gates FND-032` and `take_ticket` on branch `task/desktop-host` from `origin/dev`.
2. **Add the three packages.** `Microsoft.Extensions.Hosting`,
   `Microsoft.Extensions.Configuration.Binder` and `Microsoft.Extensions.Options.DataAnnotations` to
   `Directory.Packages.props` (created by [[FND-027]], plan handle `DSK-02-02`), referenced from
   `src/Pegasus.Desktop/Pegasus.Desktop.csproj` without version literals. None of the three is
   referenced anywhere in the repository today — the server gets Hosting from `Microsoft.NET.Sdk.Web`
   and the Worker from the Functions SDK, neither of which a `Microsoft.NET.Sdk` desktop project has.
   Use `microsoft_docs_search` for `Host.CreateApplicationBuilder` to confirm the current builder API
   before writing code.
3. **Write the four configuration files** under `src/Pegasus.Desktop/Configuration/`:
   `appsettings.json`, `appsettings.local.json`, `appsettings.pilot.json`,
   `appsettings.production.json` — each holding exactly `Gateway:BaseAddress`, `Update:FeedUri` and
   `Channel`, and nothing else (plan 04 § 3 item 8). `Update:FeedUri` must accept the D-003 UNC form
   `\\<host>\<share>\<channel>\Pegasus.appinstaller`, so do not type it as a `Uri` requiring a scheme
   without checking. `appsettings.local.json` points at the **local** Test/UAT stack (L-02), never an
   Azure test resource. **No secret, token, connection string or Azure identifier** may appear in any
   of them — `src/Pegasus.Web/appsettings.json` carries a plaintext `Bootstrap:VerificationAccount`
   marked TEMPORARY in its own comment, and a shipped desktop package cannot be fixed by a redeploy
   the way a server can.
4. **Make the channel a build property.** In `src/Pegasus.Desktop/Pegasus.Desktop.csproj` add
   `<PegasusChannel Condition="'$(PegasusChannel)'==''">local</PegasusChannel>` and embed
   `Configuration/appsettings.json` plus `Configuration/appsettings.$(PegasusChannel).json` as
   `EmbeddedResource` with **fixed logical names**, so the reader does not need to know the channel.
   The default of `local` means an unspecified build is harmless rather than accidentally production —
   [[FND-040]] (plan handle `DSK-02-15`) must pass the property explicitly.
5. **`src/Pegasus.Desktop/Hosting/PegasusHost.cs`** — build the host: read the two embedded streams
   with `AddJsonStream` (base first, channel second, so the channel wins); bind `GatewayOptions`,
   `UpdateOptions` and `ChannelOptions` with data-annotation validation and `ValidateOnStart`;
   register `AddPegasusApiClient(…)` from [[FND-031]] (plan handle `DSK-02-06`); register the
   credential store with `ApplicationData.Current.LocalFolder.Path` as its `storeRoot`; register the
   bounded cache. Register navigation and dialog services **only once [[FND-033]] (plan handle
   `DSK-02-08`) defines them** — creating empty interfaces now is the dormant registration
   `docs/engineering.md` § Abstractions (`:113`) forbids.
6. **Wire logging.** `builder.Logging.ClearProviders()`, then add the `IDiagnosticsWriter`-backed
   `ILoggerProvider` over [[FND-031]]'s `Diagnostics/` writer. Configure the sink with an explicit
   total-size cap and file-retention count. Generate a per-launch session identifier **once at host
   build** and attach it to every log scope alongside the request correlation id that
   [[FND-031]]'s `PegasusRequestHandler` supplies — [[FND-036]] (plan handle `DSK-02-11`) correlates
   a crash bundle by that identifier, so it must be on the first line and every line after.
7. **Implement redaction in one place** — the sink's message processor removes bearer tokens, refresh
   tokens, `Authorization` header values, password fields and any value keyed `token` / `secret` /
   `password` before a line is written. It must redact the value while **preserving the surrounding
   message**; suppression is not redaction, and with ADR-0109 making the bundle the only support
   channel a suppressed message costs real diagnosis. The repository's only existing redaction is
   type-level (`src/Pegasus.Core/Documents/RequestUploadPolicy.cs:110`), so this is the first sink
   rule and must remain the only one — [[FND-036]] re-applies **this** processor at bundle collection
   rather than writing a second rule set. Assert with a fixture test, never by inspection.
8. **Change `App.xaml.cs`** to build the host in `OnLaunched` **before** creating the window, hold it
   in a single accessor (`public static IHost Services` or equivalent), and dispose it on
   `Application.Current.Exit`. After this, nothing else constructs services; a view model that news up
   a client is a defect the review must catch.
9. **Tests in `tests/Pegasus.Desktop.ViewModelTests`** ([[FND-038]], plan handle `DSK-02-13`): a fake
   host fixture resolves `GatewayOptions`, the API client and the credential store; an
   options-validation test proves a missing `Gateway:BaseAddress` fails at start (matching the
   observable behaviour of `src/Pegasus.Web/Program.cs:101-110`); a log fixture writes a line
   containing a fake bearer token and asserts the token is **absent** while the surrounding message
   **survives**; a rotation test writes past the size cap and asserts the retention count is honoured.
   Add one the body does not name but A-FND032-2 requires: an **override** test setting
   `Gateway:BaseAddress` in both embedded files and asserting the channel file wins — without it, a
   silently non-layering configuration would ship every package pointing at the same gateway and no
   smoke test would notice.
10. **Build and launch.** `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun`,
    then the same command asynchronously; confirm `✅ <pkg> launched (PID: …)` and that a log file
    appears under the packaged app's local folder with the session identifier in its first line.
11. **Prove the channel selection with a plain build**, not the script:
    `dotnet build ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -c Release -p:PegasusChannel=pilot`,
    then inspect the produced assembly's manifest resource names — expected: `appsettings.pilot.json`
    present, `appsettings.production.json` and `appsettings.local.json` absent. Use the plain command
    because `BuildAndRun.ps1:146-157` injects a project-level `Directory.Build.props` that shadows the
    repository root one, so a script build is not the build CI performs.
12. **Test and close.** `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`,
    then add the composition entry to `docs/current-architecture.md` § Components and dependency
    direction (`:55`), run the simplification pass, record it under a dated heading below, and open the
    PR into `dev`.

## Verification

Evidence tier **2 — Core/domain** (`docs/engineering.md` § Required evidence tiers, `:72`), as the
ticket body states: positive **and** failure cases for composition and logging — successful
resolution, missing-setting failure, redaction of a planted token, rotation past the cap.

The `proof` document is produced from these:

1. `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`
   — expected: host-resolution, options-validation, redaction, rotation **and** the configuration
   override test all pass. Paste the test names, not just the count.
2. `dotnet build ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -c Release -p:PegasusChannel=pilot`
   — expected exit 0, and the manifest resource listing showing only the pilot channel file embedded.
   Include the listing.
3. `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj`
   (async) — expected: the app launches and writes a log file whose first line carries the session
   identifier. Attach the first lines of that file, with any token already redacted (it should be).
4. Additionally, and not in the body — a **grep gate** on the shipped configuration:
   `grep -rniE 'password|secret|token|connectionstring|AccountKey|SharedAccessSignature' src/Pegasus.Desktop/Configuration/`
   — expected: no matches. The acceptance criterion "no secret … in any `appsettings*.json` shipped in
   the package" deserves an executable check, given that the server-side counter-example
   (`src/Pegasus.Web/appsettings.json`) exists in this very repository.
5. Additionally: confirm each of the four configuration files contains exactly three settings —
   `Gateway:BaseAddress`, `Update:FeedUri`, `Channel` — and no fourth.

## Risks / open questions

- **Risk — the embedded layering does not actually override.** Assumption A-FND032-2. If `AddJsonStream`
  layering silently fails, every channel package points at the base gateway and no smoke test notices.
  *Mitigation*: the override test added at step 9, which is the single most valuable test in this
  ticket.
- **Risk — the channel files move onto disk in a later "simplification".** That would remove the only
  protection against re-pointing a pilot package at production. *Mitigation*: recorded in § Approach
  as a rejected alternative with its reason, so a future pass sees the decision rather than an
  accident.
- **Risk — redaction becomes suppression.** *Mitigation*: the step 9 fixture asserts both halves —
  token absent **and** surrounding message present.
- **Risk — a `BuildAndRun.ps1` result is trusted for the channel check.** The script shadows the root
  `Directory.Build.props` (`BuildAndRun.ps1:146-157`). *Mitigation*: step 11 uses a plain
  `dotnet build`.
- **Risk — CI builds an unintended channel.** *Mitigation*: the `PegasusChannel` default is `local`,
  so an unspecified build is harmless; [[FND-040]] must pass the property explicitly and this is
  recorded for it.
- **Sequencing, not an open question — [[FND-038]] and [[FND-031]].**
  `tests/Pegasus.Desktop.ViewModelTests` does not exist and neither do `IDiagnosticsWriter`,
  `AddPegasusApiClient` or `IDesktopCredentialStore`; both are named owners with their own tickets. Do
  not stub any of them here.
- **Scope boundary, not an open question — navigation and dialog services.** [[FND-033]] defines them;
  this host registers them when they exist. Creating empty interfaces now is forbidden by
  `docs/engineering.md` § Abstractions (`:113`).
- **Scope boundary, not an open question — the diagnostics bundle, single-instance redirection and
  every authentication flow.** [[FND-036]], [[FND-035]] (plan handle `DSK-02-10`) and area 04
  respectively.
- **No `open-questions` document is opened.** Nothing here needs an answer from outside the ticket
  before implementation begins; every assumption in the research names the command that settles it,
  and the `local` channel's target is settled by L-02, not open.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
