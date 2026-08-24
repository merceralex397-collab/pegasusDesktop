# Checklist — FND-036

One box per plan step, in plan order. Each names the file, field or command whose completion makes it
true, so it can be ticked independently and honestly.

- [ ] Read plan 02 § 3 decision 10 and § 4's exit-gate table, `docs/desktop/06-ui-design/screen-specs.md:116-125` § Diagnostics and settings, and `src/Pegasus.Core/Eva/EvaBundleSchema.cs` (the versioned-zip precedent at `:523-525`, `:737`, `:809`, `:823-825`).
- [ ] Confirm [[PLAT-009]] (plan handle `DSK-10-09`) has not been taken; apply the ownership split recorded in the plan's Approach; record the same split in [[PLAT-009]]'s plan. If it is already taken and started, stop and reconcile with its holder rather than building the bundle twice.
- [ ] Confirm [[FND-031]] (plan handle `DSK-02-06`), [[FND-032]] (plan handle `DSK-02-07`), [[FND-033]] (plan handle `DSK-02-08`) and [[FND-035]] (plan handle `DSK-02-10`) have landed. Then `get_doc_gates FND-036` and `take_ticket` on branch `task/desktop-diagnostics-bundle` from `origin/dev`.
- [ ] Create `src/Pegasus.Desktop.Infrastructure/Diagnostics/DiagnosticsBundleManifest.cs` with a named `SchemaVersion` constant and fixed entry-name constants, following `EvaBundleSchema.cs:523-525`.
- [ ] Give the manifest exactly these fields: app version; package identity `FamilyName`/`Name`/`Publisher`/`Version`; Windows version; Windows App SDK and dependency versions; channel; per-launch session identifier; bundle creation timestamp; reason (`crash` | `user-export`); and a `truncated` flag with its reason.
- [ ] Write the manifest schema into the plan document as well as into code, so the schema test and the `docs/runbook.md` support entry can be checked against each other.
- [ ] Implement `src/Pegasus.Desktop.Infrastructure/Diagnostics/DiagnosticsBundleBuilder.cs` collecting the manifest, the redacted rolling logs, the last compatibility response from the bounded cache, and [[FND-035]]'s activation log — and **nothing else**.
- [ ] Choose a bundle file name carrying no case reference, operator name or VRM ([[PLAT-007]], plan handle `DSK-10-07`, inherits it under "no PII in file names").
- [ ] Re-apply [[FND-031]]'s redaction hook over every file copied into the bundle at collection time, calling the hook rather than re-implementing the regex set.
- [ ] Register `Application.Current.UnhandledException`, `AppDomain.CurrentDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException` in `src/Pegasus.Desktop/App.xaml.cs`; each writes a `crash` bundle, flushes the sink, and exits. Confirm no handler sets `e.Handled = true` and continues.
- [ ] Bound the crash path with a short explicit timeout, wrap the bundle write in a try/catch whose fallback is a single plain-text line, and read package identity from the value cached at host build rather than calling into WinRT while unwinding.
- [ ] Set the manifest's `truncated` flag where the timeout cuts collection short, rather than producing a silently short bundle.
- [ ] Add "Export diagnostics" (`AutomationProperties.AutomationId="Settings.ExportDiagnostics"`, primary in the section) and "Open logs folder" (`Settings.OpenLogs`) to the Diagnostics section of the settings route; export writes the same bundle with reason `user-export` and reports the path in **one sentence**.
- [ ] Confirm no empty Preferences, About or Developer section was added — `screen-specs.md:118` requires sections to render only when populated, and those belong to other tickets.
- [ ] Bound retention at **5 bundles or 50 MB, whichever is reached first**, oldest deleted first, pruning on **every** write including the crash path.
- [ ] Write the schema test in `tests/Pegasus.Desktop.ViewModelTests` ([[FND-038]], plan handle `DSK-02-13`): the manifest parses, every required field is present, and the archive contains the expected entries **and nothing else**.
- [ ] Write the fault-injection test: a raised unhandled exception writes a `crash` bundle and invokes the process-exit action **exactly once**.
- [ ] Write the redaction test: a planted fake bearer token is absent from the built bundle **while the surrounding message survives**.
- [ ] Write the retention test: writing past 5 bundles / 50 MB leaves exactly the bound, oldest deleted first.
- [ ] Run the packaged app via `winapp run`, trigger the export command, and check the zip's entry listing against the allowed-contents list line by line.
- [ ] Add a temporary debug-only deliberate-crash command, confirm a crash bundle appears and the app exits, measure the crash-bundle write duration against the step-6 timeout, then **remove the temporary command** before the PR.
- [ ] Add the support entry to `docs/runbook.md` near § Monitoring and diagnosis (`:881`) — how an operator exports a bundle and what it contains — coordinating with area 09's runbooks and [[FND-049]] (plan handle `DSK-04-13`) so the instruction lives once, and record in the plan which document it landed in.
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] Verification run (this box produces `proof`, evidence tier 9): `dotnet build ./Pegasus.slnx --configuration Release` (exit 0, `0 Warning(s)` — the authoritative gate); `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --filter "FullyQualifiedName~Diagnostics"`; the manual export with its reported path, zip entry listing and full manifest JSON; `Select-String -Path <extracted bundle>\* -Pattern 'Bearer |refresh_token|password'` returning **no matches**; and the crash demonstration with its manifest showing `reason: crash`, the measured write duration, and `git diff` evidence that the temporary command was removed. Write the honesty clauses into the proof: which [[PLAT-009]] case applied; whether any exception source was **registered but never observed firing**; whether the `truncated` flag was ever set; whether the export came from a real packaged launch or a fixture; and that `BuildAndRun.ps1` green ≠ `dotnet build` green.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
