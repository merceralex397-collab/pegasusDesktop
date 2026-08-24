# Files — FND-036

Surveyed 2026-08-24 against fork `main`. Existing paths were confirmed with `ls`/`sed`; new files are
marked; files created by a named earlier ticket say so.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Desktop.Infrastructure/Diagnostics/DiagnosticsBundleManifest.cs` | **New.** A versioned JSON schema with `schemaVersion` plus exactly the fields proposal § 18.1 and this ticket's step 2 name: app version; package identity (`Package.Current.Id.FamilyName`, `Name`, `Publisher`, `Version`); Windows version; Windows App SDK and dependency versions; channel; per-launch session identifier; bundle creation timestamp; and reason (`crash` \| `user-export`). `schemaVersion` is what lets [[PLAT-009]] (plan handle `DSK-10-09`) add fields later without breaking a bundle already sent to support. |
| `src/Pegasus.Desktop.Infrastructure/Diagnostics/DiagnosticsBundleBuilder.cs` | **New.** Collects the manifest, the redacted rolling logs from [[FND-031]]'s (plan handle `DSK-02-06`) writer, the last compatibility response from the bounded cache, and the single-instance/activation log from [[FND-035]] (plan handle `DSK-02-10`) — **and nothing else** — then writes a zip. Also applies retention: a maximum bundle count and total size, oldest deleted first. |
| `src/Pegasus.Desktop/App.xaml.cs` (created by [[FND-030]], plan handle `DSK-02-05`; edited by [[FND-032]] and [[FND-035]]) | Register the three unhandled-exception handlers: `Application.Current.UnhandledException`, `AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`. Each writes a `crash` bundle, flushes the sink, and exits. **Never** `e.Handled = true`. |
| The Diagnostics section of the settings route | **New.** "Export diagnostics" with `AutomationProperties.AutomationId="Settings.ExportDiagnostics"` and "Open logs folder" with `Settings.OpenLogs`, per `docs/desktop/06-ui-design/screen-specs.md` § Diagnostics and settings. The export writes the same bundle with reason `user-export` and reports the produced path in **one sentence**. |
| `tests/Pegasus.Desktop.ViewModelTests/…` (created by [[FND-038]], plan handle `DSK-02-13`) | The bundle schema test (manifest parses, every required field present, archive contains the log and activation-log entries **and nothing else**, total size bounded) and the fault-injection test (handlers raise → a `crash` bundle written, process-exit action invoked exactly once, planted bearer token absent). |
| `docs/runbook.md` | 1254 lines. A support entry: how an operator exports a bundle and what it contains. Written to be **extended in place** by [[PLAT-009]] step 12, not duplicated — this ticket's § Documentation changes says "coordinate with area 09 runbooks so the instruction lives once". |

## Context files

What the implementer must **read** and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `docs/operations.md:362-369` | **Why this ticket exists, in production evidence.** "the Log Analytics workspace runs a 0.1 GB daily quota resetting at 03:00Z, and the estate exhausts it within hours. Ingestion stops for the rest of the day, so every check run in a UK working hour comes back empty. Both custody failures fell inside a capped window and left no trace… The two alert rules are blind for the same window. Raising the quota is a billing decision and is left with the operator." An incomplete bundle has no fallback during those hours. |
| `docs/current-architecture.md:160-177` | The same fact with its configuration key (`workspaceCapping.dataIngestionStatus: RespectQuota`), that sampling is on (`APPLICATIONINSIGHTS_ENABLEADAPTIVESAMPLING`), that the Worker produces most of the volume, and that PLAT-034 is open. Read it before anyone proposes "just send it to App Insights". |
| `src/Pegasus.Web/Program.cs:953-958` | `app.MapGet("/diagnostics/version", () => Results.Ok(new { version = productVersion, sourceSha }))` — the repository's existing identity-document shape, which the manifest's server-identity section mirrors. It is two fields, `AllowAnonymous`, and carries no secret; the manifest should read the same way. |
| `src/Pegasus.Desktop.Infrastructure/Diagnostics/IDiagnosticsWriter.cs` (created by [[FND-031]]) | The writer whose files the bundle collects, its size and retention bounds, and — critically — **its redaction hook**. Step 4 re-runs *that* hook over every file copied into the bundle. Writing a second regex set here would be the third-copy failure applied to security code, where drift is silent. |
| `src/Pegasus.Desktop/Hosting/PegasusHost.cs` (created by [[FND-032]], plan handle `DSK-02-07`) | Where the per-launch session identifier is generated and how the sink is configured — both go into the manifest, and the session identifier is what correlates a crash bundle with the launch that produced it. |
| [[FND-035]]'s activation log (plan step 8) | The single-instance/activation log the bundle collects. [[FND-035]] fixes its line format and redaction **because** this ticket consumes it; a later format change there breaks a consumer that has already shipped. |
| `docs/desktop/06-ui-design/screen-specs.md` § Diagnostics and settings | The route (user menu → Diagnostics), the four sections (About, Preferences, Diagnostics, Developer — the last non-production only), and the four AutomationIds `Settings.Theme`, `Settings.ExportDiagnostics`, `Settings.OpenLogs`, `Settings.Gallery`. Note "Sections render only when populated". |
| `docs/desktop/06-ui-design/screen-specs.md:31-40` § AutomationId convention | The naming grammar and that coverage "must report 100%" — so `Settings.ExportDiagnostics` is a harness contract, not a label. |
| `docs/design/README.md:169` | "Screens carry no lede or subtitle: one H1 and the content. Guidance appears only beside a control whose action has a consequence the operator must understand, and is one sentence." The export command reports the produced path; it does not explain what a bundle is. |
| `docs/design/README.md:172` | "A capability that is not composed in a deployment is absent from the interface — never a disabled item, inert card, or 'Unavailable' placeholder." Why the Developer section is absent in production rather than greyed out. |
| `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table) | ADR-0109's exact scope: "Desktop diagnostics bundle + existing App Insights; **no new telemetry fleet**", relating PLAT-034. Authored by [[FND-006]] (plan handle `DSK-00-06`). |
| [[PLAT-009]]'s ticket body, § Source of truth and steps 8/10 | **The overlap reconciliation, already written by the sibling**: "the first bundle export and unhandled-exception handler from `DSK-02-11` — **this ticket completes them rather than starting again**", and its own steps say "extend the export from `DSK-02-11`". It also records that `configuring-opentelemetry-dotnet` is on the do-not-load list (`docs/desktop/12-agent-tooling/skill-routing.md` § "Not applicable — do not load"). |
| `src/Pegasus.Core/Documents/RequestUploadPolicy.cs:110` | `public override string ToString() => "[REDACTED]";` — the repository's only existing redaction, and it is type-level. There is no archive-scanning precedent to reuse; this is the first. |
| `docs/runbook.md` (1254 lines) | The file the support entry joins, and the shape its existing procedures take. Write the minimum that is true today so [[PLAT-009]] step 12 can extend it in place rather than adding a second heading. |
| `Directory.Build.props` (19 lines) | `TreatWarningsAsErrors=true` and `AnalysisLevel=latest-recommended` apply — including to a crash handler, where a suppressed warning is more tempting than usual. |

## Ripple effects

- **Tests.** `tests/Pegasus.Desktop.ViewModelTests` gains the schema test and the fault-injection
  test. That project does not exist yet ([[FND-038]]) and is the only one with the Windows target
  framework `Package.Current.Id` needs.
- **[[PLAT-009]] extends everything here in place.** Its steps 6, 8, 10 and 12 add the shared
  secret/PII pattern list ([[PLAT-001]], plan handle `DSK-10-01`), the fuller version block including
  WebView2 runtime version, the last-N API failures section, the bundle secret scan and the runbook
  procedure. `schemaVersion` in step 2 is what makes those additions non-breaking.
- **[[FND-035]]'s activation-log format becomes a shipped contract** the moment this bundle collects
  it.
- **The settings route gains its first real content.** [[FND-033]] (plan handle `DSK-02-08`) put
  Diagnostics in the user menu; this ticket is what the menu item reaches.
- **Downstream tickets.** [[FND-041]] (plan handle `DSK-02-16`) has a Phase 1 exit-gate row
  ("Diagnostics bundle exports — bundle zip contains the documented manifest, tier 9");
  [[PLAT-007]] and [[PLAT-009]] are blocked by this ticket.
- **Documentation.** `docs/runbook.md` gains a support entry;
  `scripts/Test-DocumentationLinks.ps1` runs in the CI `documentation` lane
  (`.github/workflows/ci.yml:71-87`). The `DSK-02` capability row is [[FND-008]]'s (plan handle
  `DSK-00-08`), not this ticket's.
- **No solution, package, restore or architecture-test change.** No project and no package is added;
  `Pegasus.slnx`, `DependencyDirectionTests.cs` and every `packages.lock.json` are untouched.
  `System.IO.Compression` (`ZipFile`) is in the shared framework.

## Out of scope

Recorded so the reviewer sees each was a decision, matching the ticket's Guardrails.

- **Uploading a bundle anywhere** — refused. Central telemetry stays the existing Application Insights
  on the gateway side (proposal § 18.2, ADR-0109).
- **An OpenTelemetry collector, or loading `configuring-opentelemetry-dotnet`** — explicitly out of
  scope; the skill is on the do-not-load table
  (`docs/desktop/12-agent-tooling/skill-routing.md` § "Not applicable — do not load") and a collector
  fleet contradicts ADR-0109.
- **Adding an Application Insights SDK to the desktop** — refused (the same Guardrail in
  [[PLAT-009]]).
- **Draft checkpointing and draft recovery** — proposal § 16.3's draft clauses are area 05's and
  [[PLAT-017]]'s (plan handle `DSK-10-17`).
- **Building the bundle twice** — refused. [[PLAT-009]]'s own body settles the ownership: this ticket
  builds the first bundle and the crash path; [[PLAT-009]] completes them.
- **A second redaction rule set** — refused. [[FND-031]]'s hook is re-applied, not re-implemented.
- **Setting `e.Handled = true` and continuing** — refused by proposal § 16.3 and by the acceptance
  criteria.
- **A "crash now" command surviving into the PR** — refused. The temporary fault-injection command
  used at step 11 is removed before the PR.
- **Anything in the bundle beyond the four allowed items** — no attachment content, no case data, no
  credentials, no machine name without consent.
- **`src/Pegasus.Web` telemetry** — [[PLAT-014]]'s, not this ticket's.
