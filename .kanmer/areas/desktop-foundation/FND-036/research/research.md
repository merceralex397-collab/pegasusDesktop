# Research — FND-036: the unhandled-exception path and the exportable diagnostics bundle

## Question

What must the crash path do (and never do), what exactly may go into the bundle, and why is a local
zip the answer rather than telemetry — with evidence rather than assertion?

## Current behaviour

**No parity-matrix row covers this ticket, and none should.** The matrix at
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds `PAR-01`…`PAR-46` — counted with
`grep -c '^| PAR-'`, which returns **46** — and every row is "keyed by the Razor page model and
handler group that implements it today" (`parity-matrix.md:3-5`). A crash handler and an export
command have no page model.

The closest existing repository mechanisms:

- **Server-side observability is Application Insights.**
  `src/Pegasus.Web/Pegasus.Web.csproj:38` (`Microsoft.ApplicationInsights.AspNetCore`) and
  `src/Pegasus.Worker/Pegasus.Worker.csproj:15` (`Microsoft.ApplicationInsights.WorkerService`).
- **The nearest thing to a version manifest already exists on the server**:
  `src/Pegasus.Web/Program.cs:953-958` — `app.MapGet("/diagnostics/version", () => Results.Ok(new { version = productVersion, sourceSha })).AllowAnonymous()`.
  It is a two-field JSON identity document, and it is the shape the bundle manifest's server-identity
  section mirrors ([[PLAT-009]]'s § Source of truth names it for exactly that reason).
- **The only redaction in the repository is type-level**:
  `src/Pegasus.Core/Documents/RequestUploadPolicy.cs:110`,
  `public override string ToString() => "[REDACTED]";`. There is no log-sink or archive redaction
  anywhere today.
- **There is no crash-handling code on the desktop side at all**, because there is no desktop side yet:
  `ls src` returns exactly `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`.

## Findings

### Facts

Verified by reading the repository at fork `main`, 2026-08-24. Each carries its source.

- **The central signal is measurably blind for most of each working day — and this is recorded as
  fact, not risk.**
  `docs/operations.md:362-369`: "**the Log Analytics workspace runs a 0.1 GB daily quota resetting at
  03:00Z**, and the estate exhausts it within hours. Ingestion stops for the rest of the day, so every
  check run in a UK working hour comes back empty. Both custody failures fell inside a capped window
  and left no trace, which is why release 20's cause had to be found by reading the permission tables
  instead of a stack trace. The two alert rules are blind for the same window. Raising the quota is a
  billing decision and is left with the operator."
  `docs/current-architecture.md:160-177` states the same with the configuration key
  (`workspaceCapping.dataIngestionStatus: RespectQuota`), adds that sampling is on
  (`APPLICATIONINSIGHTS_ENABLEADAPTIVESAMPLING`) and that "the Worker's own polling produces most of
  the volume, so measuring before buying quota is the cheaper order", and records PLAT-034 as open.
  This is the evidence ADR-0109 rests on and the reason the bundle is the support channel rather than
  a convenience.
- **Everything this ticket builds on is created by named earlier tickets.**
  `src/Pegasus.Desktop.Infrastructure/Diagnostics/IDiagnosticsWriter.cs` and its redaction hook —
  [[FND-031]] (plan handle `DSK-02-06`); `src/Pegasus.Desktop/Hosting/PegasusHost.cs`, the logging
  pipeline and the per-launch session identifier — [[FND-032]] (plan handle `DSK-02-07`);
  `src/Pegasus.Desktop/App.xaml.cs` and `Package.appxmanifest` — [[FND-030]] (plan handle
  `DSK-02-05`); the single-instance/activation log — [[FND-035]] (plan handle `DSK-02-10`), whose
  step 8 fixes its line format precisely because this ticket consumes it.
- **The overlap with [[PLAT-009]] is settled by [[PLAT-009]]'s own body, and it is a completion
  relationship, not a duplicate.** Its § Source of truth reads: "New: the host logging configuration
  from `DSK-02-07`, the diagnostics writer from `DSK-02-06`, and **the first bundle export and
  unhandled-exception handler from `DSK-02-11` — this ticket completes them rather than starting
  again**", and its § Implementation steps 8 and 10 say "extend the export from `DSK-02-11`" and add
  the bundle schema test. So this ticket owns the **first** bundle, its manifest schema and the crash
  path; [[PLAT-009]] later adds the shared secret/PII pattern list ([[PLAT-001]], plan handle
  `DSK-10-01`), the fuller version block, the last-N API failures section, the runbook procedure and
  the injected-failure sufficiency walkthrough.
- **The settings route and its AutomationIds are specified.**
  `docs/desktop/06-ui-design/screen-specs.md` § Diagnostics and settings: "Route: user menu →
  Diagnostics. Sections render only when populated: About (version, channel, package identity,
  Windows version, gateway address); Preferences…; Diagnostics (**Export diagnostics bundle —
  primary**; Open logs folder); Developer (gallery page; non-production only)", with AutomationIds
  `Settings.Theme`, `Settings.ExportDiagnostics`, `Settings.OpenLogs`, `Settings.Gallery`.
  The AutomationId convention (`screen-specs.md:31-40`) requires 100 % coverage of interactive
  controls.
- **"Sections render only when populated" is a repository rule, not a page preference.**
  `docs/design/README.md:172`: "A capability that is not composed in a deployment is absent from the
  interface — never a disabled item, inert card, or 'Unavailable' placeholder." The Developer section
  being non-production only is that rule applied.
- **Operator copy is bounded.** `docs/design/README.md:169`: "Screens carry no lede or subtitle: one
  H1 and the content. Guidance appears only beside a control whose action has a consequence the
  operator must understand, and is one sentence." So "shows the operator the produced path" is one
  sentence, not an explanation of what a bundle is.
- **ADR-0109's scope is explicit and narrow.**
  `docs/desktop/00-governance-and-workflow/README.md` § 3 ADR set table: "ADR-0109 | Desktop
  diagnostics bundle + existing App Insights; no new telemetry fleet | Proposal §18 | Relates
  PLAT-034". Authored by [[FND-006]] (plan handle `DSK-00-06`). ADR-0104 (online-required, bounded
  local cache) bounds what may be held locally; authored by [[FND-005]] (plan handle `DSK-00-05`) and
  also claimed by [[FND-026]] (plan handle `DSK-02-01`).
- **[[PLAT-009]] records an explicit do-not-load instruction** that binds this area too: the
  `configuring-opentelemetry-dotnet` skill "is explicitly not loaded (plan § 3 deviation, and
  `docs/desktop/12-agent-tooling/skill-routing.md` § 'Not applicable — do not load')", and adding a
  collector fleet contradicts ADR-0109.
- **`tests/Pegasus.Desktop.ViewModelTests` does not exist** (`ls tests` → `Pegasus.ArchitectureTests`,
  `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`); [[FND-038]] (plan handle `DSK-02-13`) creates it,
  and it is the only project with the Windows target framework `Package.Current.Id` needs.
- **`Directory.Build.props` (19 lines) applies**: `TreatWarningsAsErrors=true`,
  `AnalysisLevel=latest-recommended`.
- **`docs/runbook.md` is 1254 lines** and already carries the support-procedure shape this ticket adds
  to; § Supported platform is `:19-40` and § Locked restore, build, and test is `:298-305`.
  [[PLAT-009]] step 12 also writes a desktop-diagnostics procedure there — "coordinate with area 09
  runbooks so the instruction lives once" is this ticket's own § Documentation changes, and
  [[PLAT-009]]'s completion relationship is what makes that possible without two procedures.

### Assumptions

- **A-FND036-1 — a WinUI process can complete a bundle write inside an unhandled-exception handler
  before the runtime tears it down.** The three handlers
  (`Application.Current.UnhandledException`, `AppDomain.CurrentDomain.UnhandledException`,
  `TaskScheduler.UnobservedTaskException`) do not all give the same guarantees, and
  `AppDomain.CurrentDomain.UnhandledException` in particular runs during teardown. *Confirms it*: the
  fault-injection test at step 10 plus the real deliberate crash at step 11. *If wrong*: the fallback
  at step 6 (a single plain-text line to the log directory) is the whole evidence for that path, and
  that must be stated rather than implied.
- **A-FND036-2 — `TaskScheduler.UnobservedTaskException` fires deterministically enough to test.** It
  is raised on finalisation, so it depends on a garbage collection. *Confirms it*: a test that forces
  collection and waits for pending finalizers. *If wrong*, that handler is registered but unproven,
  and the proof must say so rather than claiming all three are demonstrated.
- **A-FND036-3 — re-running redaction over already-written files at collection time catches what a
  pre-fix log missed.** This is the point of step 4. *Confirms it*: the planted-token test that writes
  a fixture log **without** redaction and then asserts the token is absent from the bundle. *If
  wrong*, the bundle inherits every historical leak and the `Select-String -Pattern 'Bearer '` check
  is the only thing standing between a leak and a support e-mail.
- **A-FND036-4 — `Package.Current.Id` is readable from the process at crash time.** It is used for the
  manifest's package identity block. *Confirms it*: the schema test and the real export.
  *If wrong*: the manifest carries a null identity exactly when it matters most, so the builder must
  tolerate it and record the absence rather than throwing — a crash-path handler that throws is
  forbidden by step 6.
- **A-FND036-5 — the bundle stays small enough that retention by count and total size is meaningful.**
  With bounded rolling logs from [[FND-031]] as the only bulk content, it should. *Confirms it*: the
  bounded-size assertion in the schema test and the measured size in the proof. *If wrong*, the
  retention policy is doing nothing and a workstation disk is at risk — which step 8 exists to
  prevent.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered. This is the rare case where
question 6 has **real measurement** and it points away from central placement.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | A bundle is a per-workstation artefact describing one launch of one process. Nothing about it is shared, and nothing about it is authoritative for anyone else. |
| Unattended execution — must it run with every desktop closed? | **No** | The crash handler runs inside the failing process; the export is an operator-initiated command on the Diagnostics settings route. |
| Protected credentials — a long-lived secret that must not sit on workstations? | **No — and this ticket's job is to keep it that way.** | The bundle's allowed contents are fixed: manifest, redacted rolling logs, the last compatibility response, the activation log — "and nothing else" (proposal § 18.1). Redaction is re-applied at collection (step 4) and the `Select-String -Pattern 'Bearer '` check in the verification is the executable form of the rule. Nothing secret is *placed* here; something secret is actively *excluded*. |
| Public callback — must an external service call a stable public endpoint? | **No** | "No bundle is uploaded anywhere" (this ticket's Guardrails). It is written to the packaged app's local folder or a folder the operator chooses. |
| Central enforcement — revocation, permissions, audit or an invariant independent of the client? | **No** | There is nothing to revoke or authorise. The one invariant this ticket enforces — never continue in a corrupted state (proposal § 16.3) — is enforced **in the failing process itself**, because by definition no other party can stop a process that has already decided to keep running. |
| Measured operational advantage — measured evidence that central is materially better? | **No — and there is measurement, pointing the other way.** | `docs/operations.md:362-369` and `docs/current-architecture.md:160-177`: the Log Analytics workspace runs a **0.1 GB daily quota resetting at 03:00Z**, the estate exhausts it within hours, "every check run in a UK working hour comes back empty", both production custody failures "fell inside a capped window and left no trace", release 20's cause had to be found by reading permission tables rather than a stack trace, and the two alert rules are blind for the same window. Sampling is already on and the Worker produces most of the volume. Raising the quota is an unmade billing decision left with the operator (PLAT-034, open). A local bundle is available during exactly the hours the central signal is not. |

**Conclusion.** All six "no" — the responsibility belongs in the desktop, and unusually the sixth
answer is backed by recorded production evidence rather than by the absence of evidence. No Azure
write arises, no telemetry fleet is added, and the `configuring-opentelemetry-dotnet` skill stays on
the do-not-load list.

## Implications

1. **The bundle is the support channel, not a nice-to-have.** With the central signal blind for most
   of each working day and ten users, an incomplete bundle has no fallback. That justifies the
   schema test, the retention bound and the redaction re-run being *tests* rather than intentions.
2. **The manifest is a contract with three consumers.** The schema test, the support runbook and
   [[PLAT-009]]'s extension all read it. Step 2's instruction to write the schema into the plan as
   well as the code is what keeps them in step; `schemaVersion` is what lets [[PLAT-009]] add fields
   without breaking a bundle already sent to support.
3. **Redaction must run twice and must be one rule.** [[FND-031]] defines the hook, [[FND-032]] wires
   it into the sink, and this ticket re-applies **that same** processor at collection so a log written
   before a rule fix cannot leak. A second regex set here would be the third-copy failure applied to
   security code, where drift is silent.
4. **The crash path has to be more defensive than ordinary code.** It must complete within a short
   explicit timeout, must never itself throw, and must fall back to a single plain-text line. A
   handler that hangs is indistinguishable from a hung application — which is exactly the failure the
   operator is already trying to report.
5. **`e.Handled = true` is the one line that must never appear.** Proposal § 16.3 forbids continuing
   in a corrupted state, and this ticket's acceptance criteria restate it. It is a single-token defect
   with no compiler signal, so it belongs in the verification as a grep, not only in a review.
6. **The temporary fault-injection command is a real hazard.** Step 11 requires deliberately crashing
   the app from a debug-only command and then removing it. A shipped "crash now" command is a defect,
   and "must not survive into the PR" is in the ticket's own Traps.
7. **The two runbook procedures must be one.** This ticket's § Documentation changes says "coordinate
   with area 09 runbooks so the instruction lives once", and [[PLAT-009]] step 12 writes the fuller
   procedure. The completion relationship means this ticket writes the minimum that is true today and
   [[PLAT-009]] extends it in place — not a second heading.

## Open questions

- **None.** Every field of the manifest is named by proposal § 18.1 and this ticket's step 2; the
  bundle's allowed contents are an explicit closed list; the redaction rule is [[FND-031]]'s and is
  reused rather than redefined; and the overlap with [[PLAT-009]] is settled **by [[PLAT-009]]'s own
  body**, which states it "completes them rather than starting again". The five assumptions above each
  name a test inside this ticket, or a sibling ticket, that settles them. The one thing that could
  have become a question — who writes the `docs/runbook.md` support procedure — is a scope boundary
  with a named owner and is recorded in the plan's Risks section.
