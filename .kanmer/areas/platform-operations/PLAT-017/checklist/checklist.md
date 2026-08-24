# Checklist — PLAT-017

## Implementation

- [ ] 1. Orientation. Read the plan row, proposal `:1117-1146` and `:667-677`, and the contracts from `DSK-02-04`/`DSK-03-01` so the idempotency key is the existing `OperationKey` type. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.

- [ ] 2. Branch `task/dsk-10-17-reliability-operation-model` from `dev`.

- [ ] 3. Define one `OperationState` enum with exactly the §16.1 values — `NotStarted`, `Running`, `Succeeded`, `Failed`, `Cancelled`, `Uncertain` — and one `Operation<T>` (or equivalent) record carrying the state, the correlation id, the operation key, the started/completed timestamps, the failure classification from `DSK-07-19`, and operator-readable recovery advice. Put it in `src/Pegasus.Desktop.Infrastructure` (or `src/Pegasus.Contracts` if the gateway shares it) — one type, not one per feature.

- [ ] 4. Define `Uncertain` precisely and write the definition into the code comment and the plan document: the request was sent, no response classified it, and repetition without the operation key could duplicate the effect. A timeout after the request left the client is `Uncertain`, not `Failed`. This distinction is the point of the enum.

- [ ] 5. Route every non-trivial command through one execution helper that: generates or reuses the correlation id and the operation key; sets `Running`; passes the `CancellationToken`; maps the response to a terminal state; and logs each transition through the redacted logger from [[DSK-10-09]] with the state name. No view model may hand-roll this.

- [ ] 6. Make cancellation real where it is safe: propagate the token to the HTTP call, surface immediate visual feedback (the §15.1 "User cancellation feedback — immediate" budget, measured as ≤ 200 ms in [[DSK-10-10]]), and mark the operation `Cancelled` only when the client abandoned it before the server confirmed. Where cancellation is not safe (the server may already have committed), resolve to `Uncertain` and say so.

- [ ] 7. Write recovery advice per terminal state as operator copy, following `docs/design/README.md`: what happened, what to do next, and — for `Uncertain` — "reload before retrying" rather than an unguarded retry button. No stack traces, no protocol jargon, no explanation of internals.

- [ ] 8. Implement drafts for **approved long forms only** — start with the case edit form from `DSK-05-05` and any assessment form the plan for `DSK-05-17` marks as long. The draft is DPAPI-protected, written to the storage location and retention policy from [[DSK-10-07]], checkpointed periodically and on window deactivation, and cleared when the server confirms a save. Never write a draft for a form the plan has not approved.

- [ ] 9. Implement the recovery offer: on startup after an abnormal exit, if a draft exists for a case the user can still edit, offer to restore it, showing the draft's age and the case it belongs to. If the server version has moved on, offer compare-and-reapply through the concurrency path from `DSK-05-08` — never silently overwrite. A draft that fails to decrypt or deserialize is discarded with a named message; a corrupted draft is never partially applied.

- [ ] 10. Complete the unhandled-exception path from `DSK-02-11`: catch at every entry point (`Application.UnhandledException`, `AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException` — confirm the WinUI 3 set with `microsoft_docs_search`), write the diagnostics bundle, show a short operator message with the bundle path, and **exit**. Never mark handled and continue. Add a test that proves the process exits.

- [ ] 11. Add view-model tests for every state: each command reaches `Succeeded`, `Failed`, `Cancelled` and `Uncertain` under the corresponding fake gateway behaviour; the operation key is stable across a retry of the same logical operation and different for a new one; a replayed operation key returns the same outcome rather than duplicating.

- [ ] 12. Add the draft tests: checkpoint writes an encrypted blob; the blob contains no plaintext form data; a successful save clears it; a corrupted blob is discarded not applied; recovery is offered exactly once.

- [ ] 13. Inject a crash manually (a test-only command that throws on a background thread) on the Test/UAT stack, confirm a bundle is written, the message shows its path, the process exits, and on relaunch the draft recovery is offered. Capture the sequence as the proof.

- [ ] 14. Run `dotnet test` on the desktop view-model and infrastructure test projects, then the `winapp ui` recovery script from `DSK-08-07`. All green.

- [ ] 15. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `dotnet test` on the desktop view-model test project filtered to the operation-state tests — expected: one passing fact per state including `Uncertain`, plus the replayed-key fact.
- [ ] `dotnet test` filtered to the draft tests — expected: encryption, clearing, corruption and single-offer facts pass.
- [ ] Manual crash injection on the Test/UAT stack — expected: bundle written at the reported path, process exits, relaunch offers recovery; sequence captured in the proof.

## Progress notes

Record factual progress here.
