# Checklist — FND-036

One box per plan step, in plan order. Each is independently tickable: it names the file, value or
command whose completion makes the box true.

- [ ] Read `docs/desktop/06-ui-design/screen-specs.md` § Diagnostics and settings, plus `docs/operations.md:362-369` and `docs/current-architecture.md:160-177` (the 0.1 GB/day quota gap that makes the bundle the support channel); run `get_doc_gates FND-036`; `take_ticket` on branch `task/desktop-diagnostics-bundle` from `origin/dev`.
- [ ] Record the [[PLAT-009]] (plan handle `DSK-10-09`) ownership reconciliation in the plan before writing code: this ticket owns the first bundle, the manifest schema and the crash path; [[PLAT-009]]'s own body says it "completes them rather than starting again".
- [ ] Write `src/Pegasus.Desktop.Infrastructure/Diagnostics/DiagnosticsBundleManifest.cs` with `schemaVersion` and exactly the named fields (app version; package identity `FamilyName`/`Name`/`Publisher`/`Version`; Windows version; Windows App SDK and dependency versions; channel; session identifier; creation timestamp; reason `crash` \| `user-export`), tolerating absent package identity rather than throwing.
- [ ] Write the same schema into the plan document so the schema test, the runbook and [[PLAT-009]] agree.
- [ ] Write `Diagnostics/DiagnosticsBundleBuilder.cs` collecting the manifest, the redacted rolling logs, the last compatibility response and the activation log as an **explicit allow-list of sources** — not a directory sweep with exclusions — and writing a zip.
- [ ] Re-apply [[FND-031]]'s (plan handle `DSK-02-06`) **existing** redaction hook over every file copied into the bundle; confirm no second regex rule set was written.
- [ ] Register `Application.Current.UnhandledException`, `AppDomain.CurrentDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException` in `src/Pegasus.Desktop/App.xaml.cs`; each writes a `crash` bundle, flushes the sink and exits; confirm `e.Handled = true` appears nowhere.
- [ ] Bound the crash path: an explicit short timeout, a handler that cannot throw, and a try/catch whose fallback is a single plain-text line to the log directory.
- [ ] Add "Export diagnostics" (`AutomationProperties.AutomationId="Settings.ExportDiagnostics"`) and "Open logs folder" (`Settings.OpenLogs`) to the Diagnostics section of the settings route; the export writes the same bundle with reason `user-export` and reports the produced path in **one sentence**.
- [ ] Confirm the Developer section is **absent** in production channels rather than disabled (`docs/design/README.md:172`), and that sections render only when populated.
- [ ] Bound retention: a maximum bundle count **and** a maximum total size under the packaged app's local folder, oldest deleted first.
- [ ] Add the bundle schema test in `tests/Pegasus.Desktop.ViewModelTests`: manifest parses, every required field present, archive contains the log and activation-log entries **and nothing else**, total size bounded.
- [ ] Add the fault-injection test: handlers raise → a `crash` bundle is written and the process-exit action is invoked exactly once; and a planted bearer token in a fixture log written **without** redaction is absent from the bundle.
- [ ] For `TaskScheduler.UnobservedTaskException`, force a collection and wait for pending finalizers; if it still cannot be made deterministic, record that in the proof rather than claiming all three handlers were demonstrated.
- [ ] Run the packaged app, trigger the export command, and confirm the zip contents against the manifest.
- [ ] Trigger a deliberate unhandled exception from a temporary debug-only command, confirm a crash bundle appears and the app exits, record the handler's elapsed time against its timeout, then **remove the temporary command**.
- [ ] Add the support entry to `docs/runbook.md` — where the logs live, how an operator exports a bundle, what it contains — written as the minimum true today so [[PLAT-009]] step 12 extends it in place rather than adding a second heading.
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] Verification run (this box produces `proof`): `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --filter "FullyQualifiedName~Diagnostics"` (tests named; state which exception sources were demonstrated versus registered only); the manual export with the manifest and archive entry listing; `Select-String -Path <extracted bundle>\* -Pattern 'Bearer '` (no matches, run against a bundle built from a log that deliberately contained one); `grep -rn 'e.Handled' src/Pegasus.Desktop/App.xaml.cs` (absent, or provably `false`); the retention demonstration (more bundles than the cap, oldest deleted, total size under the cap); `grep -rniE 'crash-?now|force-?crash|throw new .*TestException' src/Pegasus.Desktop/` (no matches); and the measured bundle size. Capture every output as tier-9 evidence.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
