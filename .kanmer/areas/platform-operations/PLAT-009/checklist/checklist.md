# Checklist — PLAT-009

## Implementation

- [ ] 1. Orientation. Read the plan row, proposal `:1204-1227`, `docs/current-architecture.md:160-177` (so the quota gap is understood as the reason for this ticket) and what `DSK-02-07`/`DSK-02-11` already built. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.

- [ ] 2. Branch `task/dsk-10-09-desktop-diagnostics` from `dev`.

- [ ] 3. Define the log entry shape once, as a structured record written through source-generated `LoggerMessage` methods: timestamp (UTC, round-trip format), level, category, session id, correlation id, operation state (`not-started`/`running`/`succeeded`/`failed`/`cancelled`/`uncertain` — proposal §16.1, shared with [[DSK-10-17]]), event name, and a small typed property bag. No interpolated message strings carrying data.

- [ ] 4. Generate the per-launch session id once in `App.xaml.cs` (a GUID), put it in a logging scope that covers the whole process, and write it into the first log line together with app version, Windows build and package identity.

- [ ] 5. Propagate the API correlation id: the HTTP pipeline built by `DSK-02-06` already sets `X-Correlation-Id`; add a logging scope so every entry produced during a request carries the same value, and record the value the gateway echoes back so a desktop log line can be joined to an App Insights request.

- [ ] 6. Implement redaction by default: a single `IDiagnosticRedactor` applied to every property before it is written, using the pattern list in `docs/desktop/10-security-observability-performance/threat-register.md` § Secret and PII pattern list ([[DSK-10-01]]). Redact tokens, passwords, connection strings, key-vault URI values, attachment bytes and file contents; replace personal data (names, addresses, registrations) with a stable hash so entries can still be correlated. Unit-test each pattern with a positive and a negative case.

- [ ] 7. Bound the log: rolling files with a maximum single-file size, a maximum total folder size and a maximum age, oldest-first eviction, and asynchronous writing that never blocks the UI thread (proposal §15.2 `:1095`). Reuse the storage locations and retention machinery from [[DSK-10-07]] rather than adding a second policy.

- [ ] 8. Complete the bundle: extend the export from `DSK-02-11` so the zip contains — the rolling logs; a `manifest.json` with app version, package identity and version, Windows version and build, .NET and Windows App SDK versions, WebView2 runtime version, machine name only if the operator consents, and the session ids covered; the last compatibility response from `GET /api/v1/client-compatibility` ([[DSK-10-14]], `DSK-04-06`); and the last N API failures with their correlation ids. No attachment content, no tokens, no credentials.

- [ ] 9. Surface the action: an "Export diagnostic bundle" command reachable from the app's Help/Settings surface with an `AutomationProperties.AutomationId` so `DSK-08-06`'s `winapp ui` harness can drive it; it writes to a user-chosen folder and shows the resulting path. Follow `docs/design/README.md` operator-copy rules — the button says what it does, not how it works.

- [ ] 10. Add the bundle schema test: build a bundle in a test, assert `manifest.json` has every required field, assert the zip contains no file matching the secret patterns (reuse the scanner from [[DSK-10-03]] against the extracted bundle), and assert the total size is bounded.

- [ ] 11. Prove sufficiency: reproduce one deliberately injected failure (for example a gateway 500 on a save) end to end, export the bundle, and show in the post-implementation report that the bundle alone identifies the failing operation, its correlation id and its state — without any central telemetry. This is the evidence ADR-0109 rests on.

- [ ] 12. Write the support procedure into `docs/runbook.md`: where the logs live, how an operator exports a bundle, what to check before sending it, and how to join a correlation id to an App Insights request during an uncapped window.

- [ ] 13. Update the threat register row "sensitive information in logs/temp files" with the redaction tests and the bundle scan ([[DSK-10-01]]).

- [ ] 14. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `dotnet test` on the desktop test project filtered to the redaction and bundle-schema tests — expected: all pass.
- [ ] `pwsh ./eng/packaging/Test-PackageSecrets.ps1 -PackagePath <msix> -AdditionalPath <extracted bundle folder>` — expected: exit 0.
- [ ] `winapp ui` script exercising the Export command against the Test/UAT stack — expected: a bundle file is produced at the reported path and the command completes without blocking the UI.

## Progress notes

Record factual progress only; unresolved decisions remain in `open-questions`.
