# Research — FND-036: the crash path and the exportable diagnostics bundle

## Question

What must the bundle contain and refuse to contain, what does this repository already know about
building a versioned zip artefact, and who owns the bundle given that a second ticket in area 10
names the same deliverable?

## Current behaviour

**No parity-matrix row covers this ticket, and none should.** The matrix at
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted with
`grep -c '^| PAR-'`, which returns **46** — and every row is keyed to a page model under
`src/Pegasus.Web/Pages/**`. A crash handler and a support artefact have no page model, and the web
application has no equivalent: when a browser session misbehaves the evidence is server-side
telemetry, which is exactly the thing the desktop does **not** get.

The closest existing repository mechanisms — what does this job today:

- **Server-side telemetry is the current diagnosis channel, and it does not extend to the desktop.**
  `src/Pegasus.Web/Program.cs:194-197` and `src/Pegasus.Worker/Program.cs:14-15` register Application
  Insights. `docs/runbook.md` § Monitoring and diagnosis (`:881-897`) describes the whole model in
  those terms: "correlated Web/Worker telemetry and alerts…", "Local telemetry must be content-safe
  and prove correlation, attributes, health, and **redaction**." ADR-0109 keeps that arrangement and
  adds no desktop collector, so on a workstation the bundle **is** the channel.
- **This repository has already built a versioned, hashed zip artefact once**, and it is the shape to
  learn from: `src/Pegasus.Core/Eva/EvaBundleSchema.cs` (916 lines). It declares
  `public const string SchemaVersion = "eva-handoff-v2"` (`:523`), fixed entry names
  (`ProvenanceFileName` `:524`, `ManifestFileName = "manifest.sha256"` `:525`), writes
  `writer.WriteString("schemaVersion", SchemaVersion)` into the JSON (`:737`), and assembles the
  archive with `new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true, Encoding.UTF8)`
  (`:809`) through a single `WriteEntry` helper (`:823-825`). `:246` shows a second, numeric
  `schemaVersion = 1` on a different payload.
- **There is no log-redaction code anywhere in `src/` today.**
  `grep -rln "Redact\|redact" src/ --include=*.cs` returns nothing. The redaction hook this ticket
  re-applies is created by [[FND-031]] (plan handle `DSK-02-06`) and has no prior art here.

## Findings

### Facts

Verified by reading the repository at fork `main`, 2026-08-24. Each carries its source.

- **`src/Pegasus.Desktop` and `src/Pegasus.Desktop.Infrastructure` do not exist yet.** `ls src`
  returns exactly `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`.
  `IDiagnosticsWriter` and the bounded cache come from [[FND-031]]; `App.xaml.cs` from [[FND-030]]
  (plan handle `DSK-02-05`); `Hosting/PegasusHost.cs` and the per-launch session identifier from
  [[FND-032]] (plan handle `DSK-02-07`); the activation log from [[FND-035]] (plan handle
  `DSK-02-10`).
- **`System.IO.Compression` is already in use in three production files** —
  `src/Pegasus.Core/Eva/EvaBundleSchema.cs:1`,
  `src/Pegasus.Infrastructure/Intake/MimeKitPdfPigOpenXmlIntakeSourceReader.cs:2`,
  `src/Pegasus.Infrastructure/Persistence/EfDocumentCustodyStore.cs:1` — so zip handling needs no new
  dependency and has an established idiom.
- **The settings route and its four AutomationIds are specified.**
  `docs/desktop/06-ui-design/screen-specs.md:116-125` § Diagnostics and settings: "Route: user menu →
  Diagnostics. **Sections render only when populated**: About (version, channel, package identity,
  Windows version, gateway address); Preferences (theme follows system / light / dark, grid column
  layouts per table, window position restore — **local only**); Diagnostics (**Export diagnostics
  bundle — primary**; Open logs folder); Developer (gallery page; **non-production only**)." The ids
  are `Settings.Theme`, `Settings.ExportDiagnostics`, `Settings.OpenLogs`, `Settings.Gallery`.
- **"Sections render only when populated" is a real constraint on this ticket**, not decoration: the
  Diagnostics section must not appear as an empty shell before the bundle builder exists, and the
  Developer section is non-production only.
- **The AutomationId convention is repository-wide.** `screen-specs.md:31-39`:
  `<Screen>.<Region>.<Element>[.<Key>]`, PascalCase, "stable across releases, unique per window",
  with "Every interactive control has one; `pegasus-ui-verifier`'s coverage audit must report 100%".
  The four settings ids are instances of it, read by [[TEST-006]] (plan handle `DSK-08-06`) and
  [[DUI-015]] (plan handle `DSK-06-15`).
- **A second board ticket names the same deliverable, and the board itself answers the ownership
  question.** [[PLAT-009]] (plan handle `DSK-10-09`) is titled "Desktop diagnostics: bounded redacted
  rolling logs, session and correlation ids, exportable bundle", sits in area `platform-operations`,
  group `EPIC-011` / `HZN-002`, phase-1, tier-9, and has no documents. **This ticket's `blocks` array
  lists `PLAT-009`** — the board records FND-036 as the prerequisite, not the duplicate. It also
  blocks [[PLAT-007]] (plan handle `DSK-10-07`, desktop temp files and cache: per-user ACLs, bounded
  retention, secure delete).
- **`docs/runbook.md` is 1254 lines with § Monitoring and diagnosis at `:881`** and § Recovery at
  `:1101`. The support entry this ticket owes has an existing home and existing neighbours whose tone
  it must match; § Release dependency order (`:828`) and area 09's runbooks are the coordination point
  the body names so the instruction lives once.
- **`Directory.Build.props` (19 lines) applies**: `TreatWarningsAsErrors=true`,
  `AnalysisLevel=latest-recommended`, `Nullable`, `ImplicitUsings`.
- **`tests/Pegasus.Desktop.ViewModelTests` does not exist** (`ls tests` returns
  `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`); [[FND-038]] (plan
  handle `DSK-02-13`) creates it. `tests/Pegasus.ArchitectureTests` targets `net10.0` and cannot host
  tests needing `Package.Current` or the Windows TFM.

### Assumptions

- **A-FND036-1 — `Application.Current.UnhandledException` in WinUI 3 fires for exceptions on the UI
  thread only**, so all three sources are genuinely needed rather than redundant.
  `AppDomain.CurrentDomain.UnhandledException` covers non-UI threads and
  `TaskScheduler.UnobservedTaskException` covers faulted tasks nobody awaited. *Confirms it*:
  `microsoft_docs_search` for `Application.UnhandledException` WinUI semantics at implementation
  time, then the fault-injection test raising from each source. *If wrong* — if one source is
  unreachable in a packaged WinUI app — register it anyway and record that it was never observed
  firing, rather than removing a handler on an assumption.
- **A-FND036-2 — a bundle can be written from inside a crash handler before the process dies.** This
  is the assumption the whole ticket rests on and it is not obviously true: the process may be in a
  state where file I/O, allocation or the logging sink is already broken. *Confirms it*: step 11's
  deliberate unhandled exception on a real launch, producing a real crash bundle. *If wrong*: the
  fallback is the single plain-text line the body's step 6 requires, and the honest outcome is a
  crash path that records less than the export path — recorded, not hidden.
- **A-FND036-3 — `Package.Current.Id` is readable during a crash.** The manifest wants
  `FamilyName`, `Name`, `Publisher` and `Version`. *Confirms it*: the crash bundle's manifest at step
  11. *Mitigation if wrong*: capture package identity **once at host build** ([[FND-032]]) and hold
  it, so the crash path reads a cached value rather than calling into WinRT while unwinding. This is
  the safer design regardless and the plan takes it.
- **A-FND036-4 — re-running redaction at collection is cheap enough for the crash path's timeout.**
  Step 4 requires re-redacting every file copied into the bundle, and step 6 requires the handler to
  complete within a short explicit timeout. Large rolling logs could make those conflict. *Confirms
  it*: measure the crash-bundle write at step 11 against the chosen timeout. *If wrong*: the bound
  wins — a truncated bundle beats a hung crash handler — and the truncation is recorded in the
  manifest rather than left silent.
- **A-FND036-5 — the retention bound cannot be defeated by the crash path itself.** A crash loop
  writes a bundle per crash; if pruning runs only on export, bundles accumulate. *Confirms it*: a
  test that writes past the count and size caps. *If wrong*: the bundle mechanism fills the
  workstation disk, which is the failure ADR-0104 and proposal § 18.1's "bounded size and retention"
  exist to prevent.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered. This ticket **produces an artefact
containing operational evidence**, so the section is answered fully rather than waved through.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | A bundle is a per-workstation snapshot written to the packaged app's local folder. It is carried to support by the operator; nothing reads or updates it concurrently. |
| Unattended execution — must it run with every desktop closed? | **No** | Both paths are triggered by the running application: a crash, or an operator pressing Export diagnostics. Nothing collects on a schedule. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No — and this ticket's main job is keeping it that way.** | The bundle carries logs, so the *risk* of credential material is real even though the answer is no. Redaction is applied twice: at write by [[FND-031]]'s `IDiagnosticsWriter` hook, and again **at collection** (step 4) so a log written before a redaction-rule fix cannot leak. The allowed-contents list is closed: manifest, redacted rolling logs, last compatibility response, activation log — no attachment content, no case data, no credentials (proposal § 18.1). Tier 9 obliges demonstrating the absence, which is why the planted-token check is a verification step and not a review opinion. |
| Public callback — must an external service call a stable public endpoint? | **No** | No bundle is uploaded anywhere. The Guardrails are explicit: central telemetry stays the existing Application Insights on the gateway side (proposal § 18.2, ADR-0109), and adding an OpenTelemetry collector is out of scope. |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **Yes for the surrounding telemetry, and it is already placed on the existing gateway — this ticket adds nothing central.** | ADR-0109 is "desktop diagnostics bundle **plus the existing** Application Insights; no new telemetry fleet". Gateway-side correlation and audit stay where they are (`src/Pegasus.Web/Program.cs:194-197`); the correlation id the bundle carries is the one [[FND-031]]'s request handler already sends on every `/api/v1` call, which is what lets a workstation bundle be joined to a server-side trace **without** a desktop collector. The join is the design; a collector fleet is what it avoids. |
| Measured operational advantage — measured evidence that central is materially better? | **No** | None claimed, and the plan set argues the opposite: with ten users, a file the operator can attach to a message beats a telemetry pipeline nobody has built. Plan 02 § 3 decision 10 makes the bundle a foundation feature for exactly that reason. |

**Conclusion.** Four "no" and two "yes"; both "yes" answers name responsibilities that **already sit**
on the evolved `Pegasus.Web` gateway and are not moved by this ticket. Everything this ticket places —
the crash handler, the builder, the bundle files, the retention bound — is local to the workstation.
No Azure write arises, and no bundle leaves the machine except by the operator carrying it.

## Implications

1. **The ownership overlap with [[PLAT-009]] is settled by the board itself, not by argument.** This
   ticket's `blocks` array lists `PLAT-009`, so FND-036 is the prerequisite. The plan records the
   split explicitly, as the Guardrails require, before any code is written.
2. **`EvaBundleSchema.cs` is the shape to copy, and it answers three design questions at once.** A
   named `SchemaVersion` constant (`:523`), fixed entry-name constants (`:524-525`), and a single
   `WriteEntry` helper (`:823`) mean the manifest, the schema test and the support runbook can all
   reference one set of literals. It is also the repository's evidence that a versioned zip artefact
   is an accepted pattern here, not an invention.
3. **The allowed-contents list must be enforced as a closed set, not an open one.** "Contains the
   logs and the manifest" is satisfiable by a bundle that also contains a case attachment. The schema
   test must assert the archive contains the expected entries **and nothing else** — the body's step 9
   says exactly that, and it is the difference between a tier-9 claim and a tier-1 one.
4. **Package identity should be captured at host build, not read during a crash.** A crash handler
   calling into WinRT while the process unwinds is the kind of thing that turns a crash into a hang.
   Caching it at startup (A-FND036-3) costs nothing and removes a failure mode.
5. **The crash path's bound beats its completeness.** Step 6's timeout and step 4's re-redaction can
   conflict on a large log. When they do, truncate and say so in the manifest: "a crash handler that
   hangs looks identical to a hung application", and a hung application is a worse outcome than a
   shorter bundle.
6. **The temporary fault-injection command is a real risk to the diff.** Step 11 requires adding a
   deliberate-crash command and removing it before the PR. A shipped debug crash command is a defect,
   and the Guardrails name it.
7. **The bundle's line format is a contract with two other tickets.** [[FND-035]]'s activation log and
   [[FND-032]]'s session identifier both appear in it, and [[FND-049]] (plan handle `DSK-04-13`) tells
   an operator where to find the result. Fix the manifest schema once and write it into the plan, as
   the body's step 2 requires, so the schema test and the support runbook agree.

## Open questions

- **None that must be answered before implementation.** The ownership overlap with [[PLAT-009]] is a
  scope boundary with a named sibling ticket which the ticket body directs to be settled **in the
  plan**, and the board's own `blocks` edge settles it; it is recorded there rather than opened as a
  blocking question. Every assumption above names the command inside the ticket that settles it.
- One value is deliberately left to the plan rather than to a question: the exact retention count and
  total-size cap for bundles. The body requires them to be explicit and bounded but names no number,
  and `docs/engineering.md` § Plan sizing prefers a stated default to a deferred decision — so the
  plan takes a default and says it took one.
