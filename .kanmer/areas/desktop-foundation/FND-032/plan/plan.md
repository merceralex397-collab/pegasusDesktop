# Plan — FND-032: Generic Host, DI, options, channel-selected configuration and bounded redacted logging in `App.xaml.cs`

**Diff estimate: ~13 files, ~430 lines** (excluding the regenerated `packages.lock.json`).

`docs/engineering.md` § Plan sizing (`:201`) requires the estimate first. Derived from the `files`
document, file by file, measured 2026-08-24:
`Hosting/PegasusHost.cs` ~110; `Logging/DiagnosticsLoggerProvider.cs` ~90;
`Options/GatewayOptions.cs` / `UpdateOptions.cs` / `ChannelOptions.cs` ~15 each (~45);
`Configuration/appsettings.json` + three channel files ~8 each (~32);
`App.xaml.cs` ~+35 / ~-5 against the template's version;
`src/Pegasus.Desktop/Pegasus.Desktop.csproj` ~+14 (the `PegasusChannel` property, two
`EmbeddedResource` items, three `PackageReference` entries);
`Directory.Packages.props` +3; `docs/current-architecture.md` ~+4.
The four tests land in `tests/Pegasus.Desktop.ViewModelTests` (~100 lines) and are counted against
that project, not this estimate — see the sequencing risk below.

## Approach

Build the host in a **separate `Hosting/PegasusHost.cs`** and have `App.xaml.cs` merely call it,
rather than composing inline in `OnLaunched` as the ticket's title suggests. The title names the
outcome ("in `App.xaml.cs`") and the body's step 5 already names the separate file; this plan makes
the reason explicit. A host built inline can only be exercised by launching the application, and
[[FND-038]] (plan handle `DSK-02-13`) must resolve `GatewayOptions`, the API client and the
credential store **without a dispatcher**. Extracting the builder is what makes the acceptance
criterion "every service a view model needs resolves from the container in a test without a
dispatcher" achievable at all. `App.xaml.cs` keeps ownership of lifetime — build before the window,
dispose on exit — and owns nothing else.

The rejected alternative is a service-locator static (`App.GetService<T>()` called from view model
constructors). It is the shape the `winui-mvvm` template nudges toward and it would work, but it
makes every view model's real dependencies invisible to the compiler and untestable without the
static being initialised. Constructor injection through the container, with one static accessor used
only by the composition root and the frame's page activator, is the version review can check.

Two properties of this repository shape the plan beyond the body's instructions:

1. **Neither pattern in this ticket has prior art here.**
   `grep -rn "ValidateOnStart\|ValidateDataAnnotations\|AddOptions<" src/ --include=*.cs` returns
   **zero matches**, and `grep -rln "Redact\|redact" src/ --include=*.cs` returns nothing. There is
   no convention to extend; this ticket sets one. The nearest precedent is the *fail-at-start*
   property of `src/Pegasus.Web/Program.cs:101-103`
   (`?? throw new InvalidOperationException("Runtime:Profile is required.")`), which is preserved
   even though the mechanism differs.
2. **The desktop's `appsettings.json` is shipped to workstations, and this repository already has a
   plaintext password in a `appsettings.json`.** `src/Pegasus.Web/appsettings.json` carries
   `Bootstrap.VerificationAccount` with `"Password": "Pegasus-UI-Verify-2026!"` above a comment
   admitting it is temporary. A web server's file sits on infrastructure the operator controls; an
   MSIX-embedded file is copied to every operator's disk. That asymmetry is why step 3 permits three
   keys and no fourth, and why the check is a grep in § Verification rather than a review opinion.

## Governing docs

The ticket's `refs` array is empty and `get_doc_gates FND-032` reports `docs_todo: true`, so there
is no linked PRD/FRD/ADR to meet today.

> **New ADR** — ADR-0109 (desktop diagnostics bundle plus the existing Application Insights; no new
> telemetry fleet — the ADR that bounds this ticket's log design, retention and the decision not to
> ship desktop logs anywhere), authored by [[FND-006]] (plan handle `DSK-00-06`). ADR-0104
> (online-required, bounded local cache) bounds the cache this host registers and has two claimants —
> [[FND-005]] (plan handle `DSK-00-05`) and [[FND-026]] (plan handle `DSK-02-01`); see [[FND-026]]'s
> plan for the ownership reconciliation.
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table) and
> `docs/desktop/02-architecture-and-foundation/README.md` § 3 decisions 7 and 9; if either ADR lands
> differently this plan is revised before implementation.

Because `refs` is empty, the authorities that actually bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 7.2 Application composition | Hosting for DI, configuration, logging and lifetime; `IHttpClientFactory`; structured logging | Steps 5, 6, 8 |
| Proposal § 18.1 Desktop diagnostics | Structured rolling local logs with a per-launch session identifier, API correlation ids, redaction by default, bounded size and retention | Steps 6, 7 |
| Proposal § 21.1 Build properties | Build-time properties select what ships | Step 4 (`PegasusChannel`) |
| Plan 02 § 3 decision 7 | Generic host in `App.xaml.cs`; one `IHttpClientFactory` pipeline; bounded redacting rolling file sink; configuration layered as embedded base + `appsettings.<channel>.json` selected by an MSBuild property at package time; channel = `pilot` \| `production` \| `local` | Steps 3–8 |
| Plan 02 § 3 decision 9 | No desktop framework on top of WinUI — a shell service, a navigation service, a dialog service and a handful of controls are the whole permitted surface | Step 5's refusal to register placeholders |
| Plan 02 § 4 target-state table (`src/Pegasus.Desktop` row) | References Core, Contracts and Desktop.Infrastructure only | Step 2 adds three `Microsoft.Extensions.*` packages and no server or ASP.NET reference |
| **Plan 04 § 3 item 8** (`docs/desktop/04-auth-session-update-and-startup/README.md:198-199`) | "Secrets in the package: none. The package carries only the gateway base URL, feed URL, and channel name per channel" | Step 3 — exactly three keys; § Verification greps for a fourth |
| Plan 04 § 3 (`:222`) | "no secrets in MSIX (package content scan) (tier 9)" | § Verification V5 |
| **L-02** (locked, `docs/desktop/README.md`) | Test/UAT is a local production-mimicking stack; no Azure test environment; ADR-0014 stands | Step 3 — the `local` channel points at the local stack, never at an Azure resource |
| **L-01** (locked) | The gateway is `Pegasus.Web` evolved in place; the desktop talks only to it | Step 5 — `BaseAddress` comes from `GatewayOptions`; no data-access type is registered |
| `AGENTS.md` § Simplicity rails — one list per concept | An exception taxonomy, a key list, a redaction rule lives in exactly one place | Step 7 (redaction defined once, on [[FND-031]]'s `IDiagnosticsWriter`); step 5 (check for an existing `GatewayOptions` before declaring a second) |
| `docs/engineering.md` § Abstractions (`:113`) | No dormant scaffolding; nothing built but unwired survives | Step 5 — no navigation/dialog placeholder interfaces until [[FND-033]] has a real caller |
| `docs/engineering.md` § Plan sizing (`:201`) | A plan states its diff estimate first, from a measured inventory | The estimate above |
| `docs/engineering.md` § Required evidence tiers (`:72`), tier 2 | Positive **and** failure cases — not a compiling project | § Verification V2 |
| **C-01** (constraint) | The repositories become private; Actions minutes stop being free | This ticket adds no CI job — [[FND-040]] (plan handle `DSK-02-15`) owns the lane |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires of the plan document
specifically.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`, win-dev-skills v0.5.0 `f1028dd5`).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`); Microsoft Learn (`microsoft_docs_search` for `Host.CreateApplicationBuilder`,
  `IOptions` validation, `ILoggerProvider` custom provider, `AddJsonStream` embedded configuration).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates <id>` before every move;
  a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5).

## Steps

These refine the ticket body's twelve implementation steps: same order, same ownership, same file
paths, adding the *how* the body leaves out.

1. **Orient.** Read plan 02 § 3 decisions 7 and 9 and plan 04 § 3 item 8 (`:198-199`); read the
   current `src/Pegasus.Desktop/App.xaml.cs` as [[FND-030]] left it; read
   `src/Pegasus.Desktop.Infrastructure/Diagnostics/IDiagnosticsWriter.cs` and
   `Api/PegasusHttpClientRegistration.cs` as [[FND-031]] left them — in particular whether
   `GatewayOptions` already exists there. Confirm both prerequisite tickets have landed before
   starting; the plan's arrow names only [[FND-030]], but [[FND-031]] supplies two types step 5 and
   step 6 call. Then `get_doc_gates FND-032` and `take_ticket` on branch `task/desktop-host` from
   `origin/dev`.
2. **Add the three packages.** `Microsoft.Extensions.Hosting`,
   `Microsoft.Extensions.Configuration.Binder` and `Microsoft.Extensions.Options.DataAnnotations` to
   `Directory.Packages.props`, referenced from `src/Pegasus.Desktop/Pegasus.Desktop.csproj` **without
   version literals**. Before writing code, run `microsoft_docs_search` for
   `Host.CreateApplicationBuilder` and confirm the current builder API — the body requires this and
   it is not ceremony: the builder shape changed between generic-host generations, and the vendored
   skills do not cover it. After adding them, check
   `git diff --stat src/*/packages.lock.json` once the solution restores: if a central pin is lower
   than a version a server project already resolves transitively, this desktop ticket silently moves
   the server graph — the same trap [[FND-031]] recorded for
   `System.Security.Cryptography.ProtectedData`.
3. **Write the four configuration files** under `src/Pegasus.Desktop/Configuration/`:
   `appsettings.json` (base) plus `appsettings.local.json`, `appsettings.pilot.json` and
   `appsettings.production.json`. Each holds **exactly three settings and nothing else**:
   `Gateway:BaseAddress`, `Update:FeedUri`, `Channel`. No secret, token, connection string, account
   name or Azure identifier may appear in any of them — plan 04 § 3 item 8 says the package carries
   "none". `local` points at the local Test/UAT stack (L-02); pointing it at an Azure endpoint needs
   a new accepted decision, not a configuration edit (ADR-0014 stands).
4. **Make the channel a build-time property.** In `src/Pegasus.Desktop/Pegasus.Desktop.csproj` add
   `<PegasusChannel Condition="'$(PegasusChannel)'==''">local</PegasusChannel>`, then embed
   `Configuration/appsettings.json` and `Configuration/appsettings.$(PegasusChannel).json` as
   `EmbeddedResource` with **fixed** `LogicalName` values — follow the repository idiom at
   `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:31-53`. Fixed logical names are the
   point: the file name on disk varies by channel, the name the code reads does not, so
   `PegasusHost` calls `AddJsonStream` with two constants and never learns which channel it is. The
   channel is chosen at package time so a pilot package cannot be repointed at production by editing
   a file on disk — which is only true if the unselected files are genuinely absent from the
   assembly, and step 11 is what proves it.
5. **Write `src/Pegasus.Desktop/Hosting/PegasusHost.cs`.** Read the two embedded streams with
   `AddJsonStream`; bind `GatewayOptions`, `UpdateOptions` and `ChannelOptions` with data-annotation
   validation and `ValidateOnStart`; call `AddPegasusApiClient(…)` from [[FND-031]]; register the
   credential store with `ApplicationData.Current.LocalFolder.Path` as its store root; register the
   bounded cache. **Reuse [[FND-031]]'s `GatewayOptions` if it exists** rather than declaring a
   second — a duplicated options class is the "one list per concept" failure. **Register no
   navigation or dialog service and create no empty interface for one**: [[FND-033]] (plan handle
   `DSK-02-08`) defines them when it has a real caller, and `docs/engineering.md` § Abstractions
   (`:113`) forbids dormant scaffolding. Build the host — do **not** call a blocking `Run`; WinUI
   owns the UI thread and the dispatcher (see § Risks, A-FND032-1).
6. **Wire logging.** `builder.Logging.ClearProviders()`, then add
   `src/Pegasus.Desktop/Logging/DiagnosticsLoggerProvider.cs` — an `ILoggerProvider` over
   `Microsoft.Extensions.Logging` writing through [[FND-031]]'s `IDiagnosticsWriter`. Configure the
   sink with an explicit total-size cap and file-retention count. Generate a per-launch session
   identifier **once at host build** and attach it to every log scope alongside the request
   correlation id that [[FND-031]]'s `PegasusRequestHandler` exposes — one identifier per launch, one
   per request, both on every line. No third-party logging framework: the Guardrails forbid it and
   this adapter is the whole sink.
7. **Implement redaction in exactly one place.** The rule lives on [[FND-031]]'s `IDiagnosticsWriter`
   message processor, not in this provider and not again in [[FND-036]] (plan handle `DSK-02-11`)'s
   bundle collector: remove bearer tokens, refresh tokens, `Authorization` header values, password
   fields, and any value keyed `token` / `secret` / `password`, **before** the line is written.
   `grep -rln "Redact\|redact" src/ --include=*.cs` returns nothing today, so there is no existing
   rule to align with and none to copy — which is why it must be proven by the fixture test in step 9
   rather than by inspection.
8. **Change `App.xaml.cs`.** Build the host in `OnLaunched` **before** creating the window; hold it
   behind one `public static IHost` (or an equivalent single accessor); dispose it on
   `Application.Current.Exit`. Nothing else constructs services: a view model that news up a client
   is a defect review must catch. Leave the pre-window region tidy — [[FND-035]] (plan handle
   `DSK-02-10`) inserts activation redirection into exactly this region and it must run before any
   window exists.
9. **Write the four tests** in `tests/Pegasus.Desktop.ViewModelTests` ([[FND-038]]): a fake host
   fixture resolves `GatewayOptions`, the API client and the credential store; an options-validation
   test proves a missing `Gateway:BaseAddress` fails **at start**, not on first use; a log fixture
   writes a line containing a planted fake bearer token and asserts the token is absent from the file
   **while the surrounding message survives** (redaction that eats the whole line is not redaction);
   a rotation test writes past the size cap and asserts the retention count is honoured. If
   [[FND-038]] has not landed, sequence it first and record the sequencing — do not duplicate the
   test scaffold here.
10. **Build and launch.**
    `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj -SkipRun`,
    then the same command asynchronously. Confirm `✅ <pkg> launched (PID: …)`, a visible window,
    **and** a non-empty log file under the packaged app's local folder whose first line carries the
    session identifier. A window with no log file, or a log file with no window, both mean the host
    took the UI thread — see § Risks.
11. **Prove the channel selection.**
    `dotnet build ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -c Release -p:PegasusChannel=pilot`,
    then inspect the produced assembly's embedded-resource list and confirm it contains
    `appsettings.pilot.json` and **not** `appsettings.production.json`. If the unselected files are
    still embedded, the security property in step 4 is false and must be reported, not glossed.
12. **Verify, simplify, open the PR.**
    `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`,
    then `dotnet build ./Pegasus.slnx --configuration Release` for the authoritative zero-warning
    gate. Add the composition note to `docs/current-architecture.md` § Components and dependency
    direction (`:55`). Run the simplification pass over this branch's own diff, record it under a
    dated `## Simplification pass` heading in this document, and open the PR into `dev`.

## Verification

Evidence tier **2 — Core/domain** (`docs/engineering.md` § Required evidence tiers, `:72`), as the
ticket body states: positive **and** failure cases for composition and logging — successful
resolution, missing-setting failure at start, redaction of a planted token, rotation past the cap.
A compiling project is not this tier.

The `proof` document is produced from these five outputs.

- **V1.** `dotnet build ./Pegasus.slnx --configuration Release` — expected exit 0 and
  `0 Warning(s)`. This is the authoritative gate: it is what
  `.github/actions/dotnet-build/action.yml:22-27` runs, and unlike `BuildAndRun.ps1` it sees the
  repository-root `Directory.Build.props` (see § Risks).
- **V2.** `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`
  — expected to cover, at minimum:
  - **Resolution, positive**: a fake host resolves `GatewayOptions`, the API client and the
    credential store, with no dispatcher present.
  - **Options, failure at start**: a configuration missing `Gateway:BaseAddress` fails when the host
    starts, not when the client is first used. Assert the *timing*, not just that it throws.
  - **Redaction, positive and negative in one test**: a planted fake bearer token is absent from the
    written file **and** the surrounding message text is still present. A test asserting only
    absence passes trivially if the sink writes nothing.
  - **Rotation**: writing past the size cap leaves exactly the configured retention count of files.
- **V3.** `dotnet build ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -c Release -p:PegasusChannel=pilot`
  — expected exit 0, and the embedded-resource list contains `appsettings.pilot.json` and **not**
  `appsettings.production.json`. Paste the resource list, not just the exit code.
- **V4.** `pwsh .codex/skills/winui-dev-workflow/BuildAndRun.ps1 src/Pegasus.Desktop/Pegasus.Desktop.csproj`
  (async) — expected `✅ <pkg> launched (PID: …)`, a visible window, and a log file whose first line
  carries the session identifier. Attach the first three lines of the log.
- **V5.** `grep -rniE '(password|secret|token|connectionstring|accountkey)' src/Pegasus.Desktop/Configuration/`
  — expected **no matches**. This is the executable form of plan 04 § 3 item 8 ("Secrets in the
  package: none") and of `:222` ("no secrets in MSIX (package content scan)"). Cite
  `src/Pegasus.Web/appsettings.json`'s `Bootstrap.VerificationAccount` plaintext password in the
  proof as the mistake this check exists to prevent.

**Honesty clauses for the proof.**

- A green `BuildAndRun.ps1` is **not** the same claim as a green `dotnet build` — the script injects
  a project-level `Directory.Build.props` that shadows the root one, dropping
  `TreatWarningsAsErrors`. Record both; V1 is authoritative.
- No CI job builds a desktop project until [[FND-040]] lands, so a green `repository-check` run says
  nothing about this ticket.
- If step 11 shows the unselected channel files still embedded, say so plainly. The claim "a pilot
  package cannot be pointed at production" is then unproven and must not appear in the proof.

## Risks / open questions

- **Risk — A-FND032-1: the generic host may seize the UI thread.** WinUI owns the dispatcher; the
  host owns a lifetime. A blocking `Run`/`RunAsync` in `OnLaunched` would deadlock or prevent the
  window appearing. *Mitigation*: step 5 builds the host and starts services explicitly rather than
  blocking, and step 10's dual requirement (a visible window **and** a written log file) is the
  detector — either symptom alone means the composition is wrong.
- **Risk — A-FND032-3: the unselected channel files may remain embedded.** If
  `Configuration/appsettings.$(PegasusChannel).json` does not evaluate per build as expected, all
  four files ship and the build-time channel gives no security benefit over a runtime switch.
  *Mitigation*: step 11 and V3 check the assembly's resource list directly. *If wrong*: report it;
  do not quietly redefine the property as a convenience.
- **Risk — the three new central pins move the server restore.** A `PackageVersion` lower than a
  version `src/Pegasus.Infrastructure` or `src/Pegasus.Web` already resolves transitively would change
  their lock files as a side effect of a desktop ticket. *Mitigation*: step 2's
  `git diff --stat src/*/packages.lock.json` check after the solution restore — expected no change to
  the server lock files; if either moved, raise the pin rather than accept it.
- **Risk — a second `GatewayOptions`.** [[FND-031]] step 5 creates `Api/GatewayOptions.cs` in
  `Pegasus.Desktop.Infrastructure` and this ticket also needs a bound gateway options class.
  *Mitigation*: step 1 reads that project first and step 5 reuses the type. Two classes with the same
  name and different validation is the "one list per concept" failure, and it will not fail the build
  — only a reader will catch it.
- **Risk — redaction implemented twice.** [[FND-036]] re-collects these logs into the bundle and may
  re-apply a regex set. *Mitigation*: the hook is defined once on [[FND-031]]'s `IDiagnosticsWriter`;
  both this ticket and [[FND-036]] call it. Recorded here because the duplication is invisible until
  the two sets drift.
- **Risk — redaction that eats the message.** A rule aggressive enough to remove every token can
  also remove the surrounding text, producing logs that are safe and useless. *Mitigation*: V2's
  redaction test asserts the surrounding message **survives**, not merely that the token is absent.
- **Risk — A-FND032-4: `ApplicationData.Current.LocalFolder` requires package identity.** An
  unpackaged launch, or a launch before identity is available, would throw at host build.
  *Mitigation*: the sink degrades to no-op rather than crashing the launch, and that degradation is
  itself a tested case. Never run the packaged `.exe` directly — "App silently exits → use
  `winapp run`" (`.codex/skills/winui-dev-workflow/SKILL.md:76`).
- **Risk — `BuildAndRun.ps1` shadows the root `Directory.Build.props`.** Measured at
  `.codex/skills/winui-dev-workflow/BuildAndRun.ps1:142-172`: the existence test at `:152` is against
  the **project directory only**, so the script injects and MSBuild stops at that file, dropping
  `TreatWarningsAsErrors`, `Nullable`, `ImplicitUsings`, `LangVersion` and `Version` for that build.
  *Mitigation*: V1, not V4, is the gate.
- **Sequencing, recorded not resolved — [[FND-038]] must land before step 9.**
  `tests/Pegasus.Desktop.ViewModelTests` does not exist yet (`ls tests` returns only the three
  existing projects) and `tests/Pegasus.ArchitectureTests` targets `net10.0`, so it cannot host these
  tests. *Mitigation*: sequence [[FND-038]] first and record it; do not duplicate the scaffold. This
  is a scope boundary with a named owner, not an open question.
- **Sequencing, recorded not resolved — [[FND-031]] must land before steps 5–7.** It supplies
  `AddPegasusApiClient`, `IDiagnosticsWriter` and the credential store. The plan's dependency arrow
  names only [[FND-030]]; this plan records the second dependency rather than discovering it at
  implementation time.
- **Scope boundary, not an open question — the shell, single instance, the bundle and
  authentication.** [[FND-033]], [[FND-035]], [[FND-036]] and area 04 ([[FND-043]], plan handle
  `DSK-04-07`) respectively. This ticket registers the credential store; it never calls it.
- **No open question is opened on this ticket.** Nothing here is unsettled in a way that must be
  answered before implementation begins. The three configuration keys are fixed by plan 04 § 3
  item 8, the channel names by plan 02 § 3 decision 7, and every assumption above names the command
  inside the ticket that settles it. No operator decision is required, and none of the settled
  operator decisions (D-002, D-003, D-004, the Send-to-AI exclusion) is reopened.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._

## Lifecycle disposition — 2026-08-29

The WinUI 3 API was verified against the Microsoft Learn Windows App SDK lifecycle documentation and the compiled Microsoft.UI.Xaml.Application reference: Application.Exit() is an imperative method, not an exit event, so Application.Current.Exit += ... is not a supported subscription shape. FND-032's documented boundary is implemented in App.ExitApplication(), which calls DisposeServices() before Application.Current.Exit(). The host is also disposed through AppDomain.CurrentDomain.ProcessExit as the process-level safety boundary, and the main Window.Closed handler remains necessary because WinUI 3 terminates when the last window closes and Microsoft documents that event as the managed-resource cleanup hook. Disposal is idempotent across those paths. No Application.Exit event exists to wire directly.

The FND-038-owned test classes remain deferred: this ticket adds no files under tests/Pegasus.Desktop.ViewModelTests/** and does not duplicate its scaffold. Composition, validation, logging, redaction, and rotation test evidence belongs to FND-038.

## Simplification pass — 2026-08-29

- Reuse: retained FND-031's GatewayOptions, AddPegasusApiClient, IDiagnosticsWriter/RollingFileDiagnosticsWriter redaction and rolling sink, DpapiCredentialStore, BoundedSnapshotCache, and request correlation scope. No duplicate options, redaction, credential store, cache, or test scaffold was added.
- Composition: kept one PegasusHost builder and one App composition root; no service-locator helpers, navigation/dialog placeholders, extra logging dependency, or new abstraction was introduced.
- Efficiency: used the generated LoggerMessage method and an IsEnabled guard for the launch record; the existing writer remains the single bounded/redaction owner. Fixed resource logical names keep channel selection at build time.
- Lifecycle review: the initially attempted Application.Exit event subscription was invalid for WinUI 3 because Exit is a method. The final App-owned ExitApplication path disposes before Application.Current.Exit(), while idempotent ProcessExit and necessary Window.Closed paths cover process and last-window termination.
- Scope correction: removed the two FND-038-owned test files and left test evidence to FND-038; no test scaffold or test source was changed. The ViewModelTests lock graph remains only because it is a required transitive project lock update from the Desktop package references.
- No unapplied behavior-preserving simplification finding remains. Repository documentation was not edited because the implementation scope supplied for this run is limited to the production desktop source, package props, and required lock/project files.

## Independent review — 2026-08-29

Zeno (pegasus-desktop-reviewer) reviewed exact head 704996c7d41c9c59de8a75ef7f2b5a84a9ccff9c. Host composition, lifecycle disposal, dependency direction, package lock isolation, and the no-test-source scope are acceptable. Full acceptance is blocked by three findings: (1) appsettings.pilot.json and appsettings.production.json use local gateway/feed placeholders rather than authoritative pilot/production endpoints; no exact endpoint or share is established in the repository and no cloud write is permitted, so none may be invented; (2) the shared FND-031 RollingFileDiagnosticsWriter redaction owner does not yet cover the ticket's generic token/Authorization/password requirements, which must be fixed in FND-031 rather than duplicated here; and (3) the required post-implementation report was absent. The docs/current-architecture omission is recorded as a warning and must be resolved or explicitly dispositioned before closure. No merge or Done claim is made.

## Configuration correction — 2026-08-29

A read-only Azure resource lookup of Container App pegasus-prod-web-252ow37gij in resource group rg-pegasus-prod returned the current ingress hostname https://pegasus-prod-web-252ow37gij.ashymushroom-676209e5.uksouth.azurecontainerapps.io/. The pilot and production Gateway:BaseAddress values were updated to that exact observed hostname in the ticket branch. No Azure resource was changed. The D-003 pilot and production feed host/share remains unresolved: repository authorities specify only the UNC form host/share/channel/Pegasus.appinstaller and contain no actual host/share. The local file URIs therefore remain a known non-release placeholder; they are not claimed as pilot or production acceptance.

## Dependency revalidation — 2026-08-29

Merged `origin/dev` into the owned `task/desktop-host` branch after PR #43 landed. The resulting branch head is `925e98724554c1ba7528492e6a3136f44c8b0416`, containing merge commit `52a1741cfa6544dfdad2632b5192a162c2430a2f` and the shared redaction correction. `dotnet restore ./Pegasus.slnx --locked-mode` passed. Targeted Release builds passed with zero warnings and zero errors for both `Pegasus.Desktop.Infrastructure` and `Pegasus.Desktop`. The branch was pushed to `origin/task/desktop-host`.

The ticket remains implementing: exact feed host/share values are still not established by repository authority, and FND-038's required host/log/validation tests and later independent review remain outstanding.

## Independent review — 2026-08-29

Reviewer Boole inspected exact head `925e98724554c1ba7528492e6a3136f44c8b0416` and returned BLOCKED. The required missing-`Gateway:BaseAddress` failure was thrown while registering the client, before host start; the narrow correction is being made in FND-031's owned infrastructure file at follow-up commit `bec8d1bc` (pending independent review and merge). The report wording was also stale and must be corrected.

FND-032 remains incomplete and must not be merged as delivered or marked Done until the corrected registration is present, FND-038's extension tests cover host resolution/start validation/redaction/rotation, the fallback store-root behavior is reconciled with its plan, and exact release feed host/share authority is supplied.
