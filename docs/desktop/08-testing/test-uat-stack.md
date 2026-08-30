# Test/UAT stack — the local production-mimicking environment (L-02)

This document defines the environment in which desktop workflows are tested
and accepted before a pilot release. It is local by decision
([index L-02](../README.md#locked-decisions-and-open-decisions)): ADR-0014
("local development and production only") stands, so nothing here touches
Azure. Real-Azure behaviour is proved on the production pilot ring
([09 · release](../09-release-update-and-distribution/README.md)).

## Purpose

The stack exists so that a release candidate of the desktop client can be
installed from a feed, updated, rolled back, and driven through the end-to-end
business scenarios against a gateway and Worker that behave like production
at the code level, on a machine that contains no production credential and
no production data.

## Components

| Component | Runs as | Configuration | Source of truth |
| --- | --- | --- | --- |
| Gateway (`Pegasus.Web`, Razor Pages + `/api/v1`) | Local process (`dotnet run` on the Web project, Kestrel, HTTPS dev cert) | `Runtime:Profile=DevelopmentOffline`, `Features:LocalIntake=true`, `Features:LocalDocumentCustody=true`, `Features:DesktopGateway=true`, `Features:AutomationMcp` off unless a scenario needs it | `src/Pegasus.Web/appsettings.Development.json`; `src/Pegasus.Web/Program.cs:101-122` (profile validation) |
| Worker (`Pegasus.Worker`) | Local process under Azure Functions Core Tools (`func start`) | `local.settings.json` copied from `src/Pegasus.Worker/local.settings.example.json`: `AzureWebJobsStorage=UseDevelopmentStorage=true`, `Runtime__Profile=DevelopmentOffline`, LocalDB connection, schedules | `src/Pegasus.Worker/local.settings.example.json`, `host.json` |
| Queues and blobs | Azurite 3.36.0 (`npx azurite`) | Default development storage endpoints; queues `intake-work`, `intake-work-poison`, `external-work`, `external-work-poison` created by the Worker client factory | `package.json` devDependency; `src/Pegasus.Worker/WorkerAzureClientFactory.cs` |
| Database | SQL Server Express LocalDB `(localdb)\MSSQLLocalDB`, database `PegasusDevelopment` (or a per-run SQL Server container on Linux hosts — not applicable to the Windows-only desktop stack) | Migrated by `dotnet run --project src/Pegasus.Web -- --migrate-development` | `docs/runbook.md § Local database`; committed migration stream |
| Graph (inbox/sent) | Replay adapters | `LocalDurableApprovedInboxSource`, `LocalDurableApprovedSentSource` reading immutable local copies | `src/Pegasus.Infrastructure/Intake/LocalDurableApprovedInboxSource.cs`, `Email/LocalDurableApprovedSentSource.cs` |
| Box custody | Local adapter | `LocalCaseCustody`, `LocalDocumentContentStore` under the ignored artifact root; retained occurrence content and managed `versionId` content are both readable locally | `src/Pegasus.Infrastructure/Custody/LocalCaseCustody.cs`, `LocalDocumentContentStore.OpenReadVersionAsync` |
| DVLA/DVSA | Replay adapter | Recorded responses; staff requests recorded, replayed in `DevelopmentOffline` | `src/Pegasus.Infrastructure/Vehicle/DvlaDvsaAdapters.cs` |
| Intake artifacts | File system | `artifacts/local-development/default/intake` (ignored) | `FileSystemIntakeArtifactStore` |
| Report rendering (gateway side, retained until parity) | In-process Playwright Chromium | Pinned Chromium installed by `Initialize-LocalDevelopment.ps1` | `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` |
| Report rendering (desktop side, L-03) | WebView2 runtime on the test machine | Evergreen runtime present on Windows 11 | area 07 |
| Update feed | A **file share or local folder share** — the same SMB mechanism as production (D-003), so the stack rehearses the real path rather than an HTTP substitute | Correct MIME types (`application/appinstaller`, `application/msix`), `Content-Length`, byte ranges; `.appinstaller` `Uri` equals the served URL | area 09 appinstaller template |
| Desktop client | Installed MSIX from the local feed, dev-signed | Channel config baked for `teststack` (gateway base URL = local Kestrel, feed URL = local host) | area 02 channel configuration |
| Seed data | Script-loaded fixtures | Generic UK-shaped casework from `reference/` and deliberately generic fixtures; never `corpus/` | repository data rules |

### Why `DevelopmentOffline` and not a new profile

`Runtime:Profile` accepts exactly two values (`src/Pegasus.Web/Program.cs:101-122`):
`DevelopmentOffline` (Development environment only) and `Production`
(requires the full production configuration set, managed identity, Azure
storage, Key Vault references). A third `TestStack` profile would be a new
composition root with its own adapter wiring, review surface, and drift risk.
The stack therefore runs under `DevelopmentOffline`, which already composes
the replay adapters and the local stores, and the gaps are listed rather than
papered over. **Deviation** from proposal §21.3 recorded in
[README § 3](README.md#3-decisions-and-assumptions).

## Machine prerequisites

- Windows 11 x64 test workstation or VM, dedicated (it will install and
  uninstall the package many times); not a developer's machine that holds a
  pilot install.
- PowerShell 7, .NET SDK 10.0.302 (`global.json`), Node (for Azurite),
  Azure Functions Core Tools v4, SQL Server Express LocalDB.
- WebView2 Evergreen runtime (present on Windows 11; checked by the desktop
  at startup, area 04).
- `winapp` CLI ≥ 0.3 (`winget install Microsoft.WinAppCLI`) for `winapp ui`
  and packaging; `AxeWindowsCLI` for accessibility scans.
- Developer Mode is **not** required to install a signed MSIX; it is required
  only if the machine also builds and runs unpackaged. Sideloading is on by
  default on Windows 11.
- A development signing certificate trusted in
  `Cert:\LocalMachine\TrustedPeople` — the same store and mechanism the
  production certificate uses (D-002), so the stack exercises the real trust
  path with throwaway credentials. The packaging suite also covers the
  untrusted case (expect `0x800B0109`) and a trust-then-publish renewal.
- `scripts/Invoke-Doctor.ps1 -Profile Offline` reports the above; extend it
  with the desktop prerequisites (ticket DSK-08-17).

## Lifecycle (one script)

Extend `scripts/Invoke-LocalDevelopment.ps1` with a `TestStack` mode rather
than adding a sibling script — it already owns `Start`, `Status`, `Smoke`,
`Stop`, `Reset` and the failure-injection modes, and the runbook already
documents it. Recommended verbs:

| Verb | What it does |
| --- | --- |
| `Start -Mode TestStack` | Starts Azurite, LocalDB (migrate if needed), the gateway, the Worker under Core Tools, the local feed host; seeds data if empty; prints URLs and the feed `.appinstaller` link |
| `Status -Mode TestStack` | Health of each component: `/health/live`, `/health/ready`, `/api/v1/client-compatibility`, Azurite ports, Worker functions enabled, feed reachable with correct MIME types |
| `Smoke -Mode TestStack` | Logs in through `/connect/token` with a seeded staff account, lists cases, opens one, checks report generation dependencies |
| `Reset -Mode TestStack` | Drops and recreates the database, clears Azurite and the artifact root, reseeds; optionally uninstalls the desktop package |
| `Stop -Mode TestStack` | Stops all processes |
| `Publish-Feed` (new) | Copies a freshly packaged `.msix` and the `.appinstaller` for the `teststack` channel into the feed folder, bumping the version; used by the packaging tests to simulate mandatory updates and rollbacks |

Failure injection already in the script (gateway unavailable, slow
responses) is reused for the connectivity and provider-timeout scenarios.

## What the stack proves and what it does not

| Evidence tier (`docs/engineering.md`) | Proved on the stack? | Notes |
| --- | --- | --- |
| 1 Static/build/architecture | Yes | Same binaries as CI |
| 2 Core/domain, view-model | Yes | Unit projects run before the stack is needed |
| 3 Parser/adapter contracts | Yes (replay) | Real provider payload shapes only as far as the recorded fixtures go |
| 4 LocalDB persistence | Yes | Same migration stream; **not** Azure SQL locking, S0 throttling, or restore |
| 5 Web/API caller | Yes | Gateway over real HTTP; bearer tokens; rate limiting |
| 6 Functions/Azurite caller | Yes | Core Tools + Azurite; **not** Flex Consumption scaling or Key Vault references |
| 7 Browser/accessibility | Yes (desktop) | `winapp ui`, `AxeWindowsCLI`, manual reviews |
| 8 Genuine corpus | Only where `reference/` fixtures suffice | `corpus/` is never used |
| 9 Security/observability | Partly | Token lifecycle, ACLs, log redaction yes; App Insights, Container App probes, Key Vault no |
| 10 Performance/concurrency | Partly | Ten desktops against the local gateway is realistic; Azure SQL latency is not |
| 11 Migration/recovery | Partly | Package install/upgrade/rollback yes; `efbundle` against Azure SQL and point-in-time restore no |
| 12 Integrated workflow | Yes for scenarios whose integrations are replayable | Scenarios 2 (Graph arrives while closed), 7 (Box), 12 (real feed + blocked client) are repeated on the pilot ring |

Only the pilot ring proves: real Azure SQL, Blob/queues on the production
storage accounts, Key Vault-backed provider secrets, Graph mailbox polling,
Box custody against the tenant, DVLA/DVSA live calls, App Insights
telemetry, the production update feed and signing chain, and the Container
App release path (`pegasus-release` skill).

## UAT scripts — end-to-end scenarios 1–14

Each scenario is a script in `docs/desktop/08-testing/` (or the ticket's
`reference/`) with steps, expected results, evidence to capture, and where it
runs. Mapping:

| # | Scenario (proposal §22.2) | Runs on | Evidence |
| --- | --- | --- | --- |
| 1 | Existing user logs in | Stack, then pilot | `winapp ui` script, token issued, audit row |
| 2 | New Graph intake received while no desktop is open | Stack (replay inbox), **pilot** (real mailbox) | Worker log, receipt row, dashboard count |
| 3 | User sees and opens the new intake | Stack | Screenshot, receipt detail |
| 4 | Duplicate detection / provider matching behaves as approved | Stack | Core decision codes visible, no duplicate case |
| 5 | Case is created or resolved | Stack | Case row, reference allocated, history |
| 6 | Vehicle data is looked up | Stack (replay), pilot (live) | Lookup request row, accepted suggestion |
| 7 | Documents loaded from and uploaded to Box | Stack (local custody), **pilot** (Box) | Transfer queue states, custody rows |
| 8 | Assessment/case data is completed | Stack | Saved values, version increments |
| 9 | Report generated, previewed, finalized, stored | Stack | Local WebView2 PDF, golden-file diff, registered report |
| 10 | Assignment/status/history are correct | Stack | History entries, operator labels |
| 11 | Another user sees the update; conflicting edit handled | Stack (two desktops or one desktop + API client) | 409 problem shown, reload/compare path |
| 12 | Obsolete desktop version blocked and updates successfully | Stack (local feed), **pilot** (real feed) | Update-required screen, `Get-AppxPackage` version after update |
| 13 | Integration failure visible and recoverable | Stack (failure injection) | Operations view, retry outcome |
| 14 | Audit identifies who performed each sensitive action | Stack | Action history/security events per actor |

## Data

- Seed set: a small, deliberately generic UK-shaped casework dataset built
  from `reference/` material and fixtures already used by the integration
  tests (builders in `tests/Pegasus.IntegrationTests/DocumentExtraction/`);
  plausible VRMs and references, irregular counts, Europe/London dates.
- Never: `corpus/` contents, operational emails, real provider payloads, real
  credentials. The repository rule "never fabricate domain emails, images,
  documents, data" applies — fixtures are labelled as test material.
- Reset is destructive and scripted (`Reset -Mode TestStack`); the artifact
  root, Azurite data, and LocalDB database are disposable.

## Evidence capture

- UI: `winapp ui screenshot` per state, `winapp ui record` for the critical
  path; JSON results from `ui-tests.ps1`.
- Tests: TRX from `dotnet test`, filed under `artifacts/test-results/`
  (ignored); summary copied into the ticket proof.
- Accessibility: `AxeWindowsCLI` output per screen + the ten-review
  checklist.
- Packaging: `Get-AppxPackage` transcripts before/after each scenario.
- Performance: the §15.1 budget table with measured values and the baseline
  hardware description.
- Logs: gateway and Worker console logs, desktop diagnostics bundle.

Evidence is filed in the Kanmer ticket (`proof`, `reference/`), never in the
repository tree (`AGENTS.md § New Markdown placement`).

## Tickets to build it

| ID | Title | Depends on | Routing |
| --- | --- | --- | --- |
| DSK-08-17 | `TestStack` mode in `Invoke-LocalDevelopment.ps1`, `Invoke-Doctor.ps1` desktop prerequisites, local feed host, `Publish-Feed` | 02 channel config, 04 compatibility endpoint, 09 appinstaller template | `pegasus-test-engineer` · `run-tests`, `winui-packaging` · Microsoft Learn |
| DSK-08-16 | Scenario scripts 1–14 with evidence templates | slices | `pegasus-test-engineer` · `kanmer-verify` · Kanmer |
| DSK-08-10 | Packaging/update tests against the local feed | DSK-08-17 | `pegasus-release-packager` · `winui-packaging` · Microsoft Learn |
| DSK-08-15 | Performance scripts and baseline record | DSK-08-17 | `pegasus-ui-verifier` · `analyzing-dotnet-performance`, `dotnet-trace-collect` · Microsoft Learn |
| DSK-08-06 / 07 / 08 / 09 | UI automation harness, critical-path scripts, accessibility lane | DSK-08-17 | `pegasus-ui-verifier` · `winui-ui-testing` · — |

## Known gaps (record, do not hide)

- No Azure SQL semantics (locking, S0 limits, failover), no Blob/Key Vault
  behaviour, no Container App probes, no App Insights; these are pilot-ring
  checks.
- Replay adapters cannot surface new provider behaviours; a provider contract
  change is first seen on the pilot ring.
- The local feed proves App Installer mechanics (update prompt, blocked
  activation, rollback) but not the production host's MIME/range configuration
  or the production certificate and share themselves (the mechanism is the
  same; the credentials and host are not).
- `DevelopmentOffline` composes `Features:LocalIntake`; production composes
  Blob intake. Intake paths through `/Upload` are therefore exercised against
  the file-system store on the stack.
- The local custody adapter retains accepted source, attachment, and folded
  image content under the case-id custody layout. `LocalDocumentContentStore`
  resolves those occurrence-addressed files through their existing metadata
  and still serves the managed `versionId` layout used by local uploads.
