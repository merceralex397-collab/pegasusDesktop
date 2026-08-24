# Files — FND-036

Surveyed 2026-08-24 against fork `main`. Every existing path was confirmed with `ls`/`sed`/`grep`;
paths created by an earlier ticket are marked with that ticket.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop.Infrastructure/Diagnostics/DiagnosticsBundleManifest.cs` | **New.** The versioned manifest record and its `SchemaVersion` constant, plus the fixed entry-name constants for the archive. Fields exactly as proposal § 18.1 and the body's step 2 name them: app version; package identity (`FamilyName`, `Name`, `Publisher`, `Version`); Windows version; Windows App SDK and dependency versions; channel; per-launch session identifier; bundle creation timestamp; reason (`crash` \| `user-export`). The same literals are referenced by the schema test and by the support runbook entry, so all three agree. |
| `src/Pegasus.Desktop.Infrastructure/Diagnostics/DiagnosticsBundleBuilder.cs` | **New.** Collects the manifest, the redacted rolling logs from [[FND-031]] (plan handle `DSK-02-06`)'s writer, the last compatibility response from the bounded cache, and the activation log from [[FND-035]] (plan handle `DSK-02-10`); re-applies redaction at collection; writes the zip; prunes to the retention bound. **The allowed-contents list is closed** — nothing outside it may enter the archive. |
| `src/Pegasus.Desktop/App.xaml.cs` (created by [[FND-030]], plan handle `DSK-02-05`; edited by [[FND-032]], plan handle `DSK-02-07`) | Register all three unhandled-exception handlers: `Application.Current.UnhandledException`, `AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`. Each writes a `crash` bundle, flushes the sink, and **exits**. Never `e.Handled = true` and continue. |
| `src/Pegasus.Desktop/Hosting/PegasusHost.cs` (created by [[FND-032]]) | Register the builder, and **capture package identity once at host build** so the crash path reads a cached value rather than calling into WinRT while the process unwinds. |
| `src/Pegasus.Desktop/Views/Settings/**` (Diagnostics section) | The "Export diagnostics" command (`AutomationProperties.AutomationId="Settings.ExportDiagnostics"`) and "Open logs folder" (`Settings.OpenLogs`), on the settings route reached from the user menu that [[FND-033]] (plan handle `DSK-02-08`) built. Export writes the same bundle with reason `user-export` and reports the produced path in **one sentence**. |
| `tests/Pegasus.Desktop.ViewModelTests/**` (created by [[FND-038]], plan handle `DSK-02-13`) | Three test groups: the bundle schema test (manifest parses, every required field present, archive contains the expected entries **and nothing else**); the fault-injection test (a raised exception writes a `crash` bundle and invokes the exit action exactly once); and the redaction test (a planted bearer token in a fixture log is absent from the bundle). |
| `docs/runbook.md` | 1254 lines. A support entry — how an operator exports a bundle and what it contains — placed near § Monitoring and diagnosis (`:881`). Coordinate with area 09's runbooks so the instruction lives **once**. |

## Context files

What the implementer must **read**, and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Core/Eva/EvaBundleSchema.cs` (916 lines) | **The precedent to copy, and it answers three design questions at once.** `:523` declares `public const string SchemaVersion = "eva-handoff-v2"` — a *named* schema version, not a bare integer; `:524-525` fix entry names as constants (`ProvenanceFileName`, `ManifestFileName = "manifest.sha256"`); `:737` writes `writer.WriteString("schemaVersion", SchemaVersion)` into the JSON so the artefact carries its own version; `:809` builds the archive with `new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true, Encoding.UTF8)` and `:823-825` funnels every entry through one `WriteEntry` helper. `:246` shows the alternative numeric `schemaVersion = 1` on a different payload. This file is also the repository's evidence that a versioned zip artefact is an accepted pattern here rather than an invention. |
| `docs/desktop/06-ui-design/screen-specs.md:116-125` § Diagnostics and settings | The route and its four AutomationIds, and one constraint that is easy to miss: "**Sections render only when populated**". The Diagnostics section must not appear as an empty shell before the builder exists, and Developer (the gallery page) is **non-production only**. About carries version, channel, package identity, Windows version and gateway address; Preferences is explicitly "local only". Ids: `Settings.Theme`, `Settings.ExportDiagnostics`, `Settings.OpenLogs`, `Settings.Gallery`. |
| `docs/desktop/06-ui-design/screen-specs.md:31-39` § AutomationId convention | That those four ids are instances of a repository-wide convention — `<Screen>.<Region>.<Element>[.<Key>]`, "stable across releases, unique per window", 100 % coverage audited. [[TEST-006]] (plan handle `DSK-08-06`)'s harness and [[DUI-015]] (plan handle `DSK-06-15`)'s audit both read them, so a renamed id is a break in another area's lane. |
| `src/Pegasus.Desktop.Infrastructure/Diagnostics/IDiagnosticsWriter.cs` (created by [[FND-031]]) | The sink's size cap, retention count and **redaction hook**. The hook is defined there, once; this ticket **calls it again at collection** (step 4) rather than re-implementing the regex set — `AGENTS.md` § Simplicity rails, one list per concept. Note also that `grep -rln "Redact\|redact" src/ --include=*.cs` returns nothing today, so there is no other redaction implementation to accidentally align with. |
| `src/Pegasus.Desktop/Hosting/PegasusHost.cs` (created by [[FND-032]]) | The **per-launch session identifier** the manifest carries, generated once at host build. Also the natural place to cache package identity so the crash path never calls into WinRT while unwinding (A-FND036-3). |
| `src/Pegasus.Desktop/Program.cs` (created by [[FND-035]]) | That startup is now an explicit entry point under `DISABLE_XAML_GENERATED_MAIN`, and that [[FND-035]] deliberately kept it to instancing only and left the seam clean. Handlers that must be registered before the first window go here; anything that can wait belongs in `App.xaml.cs`. |
| `src/Pegasus.Desktop.Infrastructure/Api/PegasusRequestHandler.cs` (created by [[FND-031]]) | Where the **API correlation id** the bundle carries comes from. This is the join that lets a workstation bundle be matched to a gateway-side Application Insights trace **without** a desktop collector — the design ADR-0109 chose instead of a telemetry fleet. |
| `docs/runbook.md` § Monitoring and diagnosis (`:881-897`) | The existing diagnosis model and the tone the new support entry must match: "correlated Web/Worker telemetry", "Local telemetry must be content-safe and prove correlation, attributes, health, and **redaction**", and "Bicep compilation proves syntax and type consistency only" — a repository that already distinguishes what a check proves from what it merely runs. § Recovery is at `:1101`; § Release dependency order at `:828` is the coordination point with area 09's runbooks. |
| `src/Pegasus.Web/Program.cs:194-197` and `src/Pegasus.Worker/Program.cs:14-15` | The existing Application Insights registrations that **stay exactly where they are**. ADR-0109 is "desktop diagnostics bundle **plus the existing** Application Insights; no new telemetry fleet". Read them to see what this ticket is deliberately not extending to the desktop. |
| `docs/design/README.md` § No explanatory copy and page economy (`:422`) | Why step 7's confirmation is **one sentence** naming the produced path. A settings screen that explains what a diagnostics bundle is would be a defect under the design authority. |
| `Directory.Build.props` (19 lines) | `TreatWarningsAsErrors=true` and `AnalysisLevel=latest-recommended` apply. Zip and file I/O code that ignores a return value or leaves a stream undisposed will fail the build, not merely be flagged. |

## Ripple effects

- **The manifest schema becomes a contract with three readers the moment it is written**: the schema
  test, the support runbook entry, and whoever reads a bundle months later. That is why the body's
  step 2 requires the schema to be written **into the plan** as well as into code — a schema that
  exists only in a `.cs` file cannot be checked against a runbook.
- **[[PLAT-009]] (plan handle `DSK-10-09`) is downstream of this ticket, and the board says so.** This
  ticket's `blocks` array lists `PLAT-009`, which names the same deliverable from the
  platform-operations side. The ownership split is recorded in the plan before code is written, as the
  Guardrails require.
- **[[PLAT-007]] (plan handle `DSK-10-07`) inherits the bundle directory.** It owns "desktop temp
  files and cache: per-user ACLs, bounded retention, secure delete, no PII in file names" — so the
  bundle location, its retention bound and its file-naming scheme chosen here are what that ticket
  hardens. Choose a file name with no case reference, no operator name and no VRM in it.
- **[[FND-035]]'s activation log and [[FND-032]]'s session identifier become bundle entries.** Their
  line formats are now load-bearing: the schema test asserts the entries exist, and a format change
  breaks it.
- **[[FND-041]] (plan handle `DSK-02-16`), the Phase 1 exit review**, has a dedicated gate row:
  "Diagnostics bundle exports — bundle zip contains the documented manifest (tier 9)". This ticket is
  what makes it answerable.
- **[[FND-049]] (plan handle `DSK-04-13`) documents the operator-facing side** — where the bundle is
  and how to send it. The runbook entry here must not be restated there; one instruction, one place.
- **The settings route gains its first real content.** [[FND-033]] built the user menu with a
  Diagnostics item; until this ticket, that route has nothing populated, and `screen-specs.md:118`
  says sections render only when populated.
- **No OpenAPI, generated-client or contract ripple.** This ticket adds no contract type and calls no
  endpoint; nothing is uploaded. Say so in the PR rather than leaving the reviewer to check
  `openapi/pegasus-v1.json`.
- **Documentation.** `docs/runbook.md` changes, and `scripts/Test-DocumentationLinks.ps1` runs in the
  CI `documentation` lane (`.github/workflows/ci.yml:76-87`).

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **Uploading a bundle anywhere** — refused. No Azure write, no endpoint, no collector. Central
  telemetry stays the existing Application Insights on the gateway side (proposal § 18.2, ADR-0109),
  and **adding an OpenTelemetry collector is explicitly out of scope**.
- **Draft checkpointing and draft recovery** — proposal § 16.3's draft clauses belong to area 05 and
  [[PLAT-017]] (plan handle `DSK-10-17`, "Reliability: the desktop operation model and crash recovery
  for approved long forms"). This ticket writes a bundle and exits; it does not try to save the
  operator's work.
- **Building the bundle twice** — [[PLAT-009]] names the same deliverable. The board's `blocks` edge
  and the plan's recorded split settle it; do not implement it there as well.
- **Swallowing an exception and continuing** — refused absolutely. Proposal § 16.3 forbids continuing
  in a corrupted state, and `e.Handled = true` followed by carrying on is that failure.
- **Attachment content, case data or credentials in the bundle** — refused. The allowed-contents list
  is closed and the schema test asserts "and nothing else", not merely "contains".
- **A temporary fault-injection command surviving into the PR** — refused. Step 11 adds one to prove
  the crash path and removes it before the PR; a shipped debug crash command is a defect.
- **The `docs/capabilities.md` `DSK-02` row** — [[FND-008]] (plan handle `DSK-00-08`) adds the `DSK`
  capability family, not this ticket.
- **The Preferences, About and Developer sections of the settings route** — this ticket adds only the
  **Diagnostics** section's two commands. Theme (`Settings.Theme`) belongs with [[FND-034]] (plan
  handle `DSK-02-09`) and the gallery (`Settings.Gallery`) with [[DUI-002]] (plan handle `DSK-06-02`),
  non-production only.
- **Relaxing `Directory.Build.props`** — never. Narrow, commented `NoWarn` entries in the desktop
  csproj only.
