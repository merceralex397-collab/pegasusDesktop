# Plan — PLAT-017

## Objective

Implement proposal §16.1's operation model in the desktop — a correlation identifier, an explicit state (not started, running, succeeded, failed, cancelled, uncertain), cancellation where safe, an idempotency key where repetition could duplicate effects, and user-readable recovery advice — and §16.3's crash recovery: encrypted local drafts for approved long forms, a recovery offer after an abnormal exit, drafts cleared on a successful save, and an unhandled-exception path that writes a diagnostics bundle and exits rather than continuing corrupted.

## Chosen approach

Proposal §16.1 `:1117-1126` and §16.3 `:1138-1146` set the reliability contract for every non-trivial operation. Without an explicit `uncertain` state a timed-out save is presented as either success or failure, and one of those is a lie — the operator then retries and duplicates the effect. The plan's risk table names "crash handling that swallows exceptions and continues" as a specific failure to prevent. Operator-visible consequence: silent duplicate writes, or an hour of assessment notes lost to a crash. Siblings: [[DSK-10-09]] (the bundle the crash path writes), [[DSK-10-07]] (where drafts are stored and how they are cleared), [[DSK-10-15]] (provider state), `DSK-05-08` (the concurrency UX that consumes these states).

## Governing docs

- No canonical PRD/FRD/ADR is linked yet. Keep `docs_todo: true`; planned desktop governing documents must not be linked until they exist on `origin/dev`.
- Use the ticket's Source of truth and its area plan until a real governing doc can be linked.

## Routing

- **Subagents**: `winui-dev` — `.codex/agents/winui-dev.toml` (implementation); `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml` (the state tests)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`; win-dev-skills v0.5.0 `f1028dd5`) → `code-testing-agent` (dotnet/skills `98f84851`, plugin `dotnet-test`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) for the recovery dialog and status copy
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for `AppDomain.UnhandledException` / `Application.UnhandledException` / `TaskScheduler.UnobservedTaskException` coverage in a WinUI 3 packaged app, and for `ProtectedData` usage for the draft blob
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Ordered implementation

1. Orientation. Read the plan row, proposal `:1117-1146` and `:667-677`, and the contracts from `DSK-02-04`/`DSK-03-01` so the idempotency key is the existing `OperationKey` type. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-17-reliability-operation-model` from `dev`.
3. Define one `OperationState` enum with exactly the §16.1 values — `NotStarted`, `Running`, `Succeeded`, `Failed`, `Cancelled`, `Uncertain` — and one `Operation<T>` (or equivalent) record carrying the state, the correlation id, the operation key, the started/completed timestamps, the failure classification from `DSK-07-19`, and operator-readable recovery advice. Put it in `src/Pegasus.Desktop.Infrastructure` (or `src/Pegasus.Contracts` if the gateway shares it) — one type, not one per feature.
4. Define `Uncertain` precisely and write the definition into the code comment and the plan document: the request was sent, no response classified it, and repetition without the operation key could duplicate the effect. A timeout after the request left the client is `Uncertain`, not `Failed`. This distinction is the point of the enum.
5. Route every non-trivial command through one execution helper that: generates or reuses the correlation id and the operation key; sets `Running`; passes the `CancellationToken`; maps the response to a terminal state; and logs each transition through the redacted logger from [[DSK-10-09]] with the state name. No view model may hand-roll this.
6. Make cancellation real where it is safe: propagate the token to the HTTP call, surface immediate visual feedback (the §15.1 "User cancellation feedback — immediate" budget, measured as ≤ 200 ms in [[DSK-10-10]]), and mark the operation `Cancelled` only when the client abandoned it before the server confirmed. Where cancellation is not safe (the server may already have committed), resolve to `Uncertain` and say so.
7. Write recovery advice per terminal state as operator copy, following `docs/design/README.md`: what happened, what to do next, and — for `Uncertain` — "reload before retrying" rather than an unguarded retry button. No stack traces, no protocol jargon, no explanation of internals.
8. Implement drafts for **approved long forms only** — start with the case edit form from `DSK-05-05` and any assessment form the plan for `DSK-05-17` marks as long. The draft is DPAPI-protected, written to the storage location and retention policy from [[DSK-10-07]], checkpointed periodically and on window deactivation, and cleared when the server confirms a save. Never write a draft for a form the plan has not approved.
9. Implement the recovery offer: on startup after an abnormal exit, if a draft exists for a case the user can still edit, offer to restore it, showing the draft's age and the case it belongs to. If the server version has moved on, offer compare-and-reapply through the concurrency path from `DSK-05-08` — never silently overwrite. A draft that fails to decrypt or deserialize is discarded with a named message; a corrupted draft is never partially applied.
10. Complete the unhandled-exception path from `DSK-02-11`: catch at every entry point (`Application.UnhandledException`, `AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException` — confirm the WinUI 3 set with `microsoft_docs_search`), write the diagnostics bundle, show a short operator message with the bundle path, and **exit**. Never mark handled and continue. Add a test that proves the process exits.
11. Add view-model tests for every state: each command reaches `Succeeded`, `Failed`, `Cancelled` and `Uncertain` under the corresponding fake gateway behaviour; the operation key is stable across a retry of the same logical operation and different for a new one; a replayed operation key returns the same outcome rather than duplicating.
12. Add the draft tests: checkpoint writes an encrypted blob; the blob contains no plaintext form data; a successful save clears it; a corrupted blob is discarded not applied; recovery is offered exactly once.
13. Inject a crash manually (a test-only command that throws on a background thread) on the Test/UAT stack, confirm a bundle is written, the message shows its path, the process exits, and on relaunch the draft recovery is offered. Capture the sequence as the proof.
14. Run `dotnet test` on the desktop view-model and infrastructure test projects, then the `winapp ui` recovery script from `DSK-08-07`. All green.
15. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `dotnet test` on the desktop view-model test project filtered to the operation-state tests — expected: one passing fact per state including `Uncertain`, plus the replayed-key fact.
- [ ] `dotnet test` filtered to the draft tests — expected: encryption, clearing, corruption and single-offer facts pass.
- [ ] Manual crash injection on the Test/UAT stack — expected: bundle written at the reported path, process exits, relaunch offers recovery; sequence captured in the proof.

## Risks and constraints

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts` (only to place a shared operation type), and the desktop test projects. Must not touch `src/Pegasus.Core` policy or `src/Pegasus.Web`; the gateway idempotency behaviour belongs to `DSK-03-08`. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: crash handling that swallows exceptions and continues in a corrupted state is the specific failure this ticket exists to prevent — the exit is not optional; a draft store that grows into an offline queue contradicts ADR-0104 and proposal §11.3, which forbids queueing a save silently as a server command; collapsing `Uncertain` into `Failed` produces duplicate writes on retry; a second encryption scheme beside the DPAPI credential store splits the security review; drafts are approved per form, never global.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Simplification pass

Before the PR, independently assess the branch diff for unnecessary abstractions, duplicated policy, or scope expansion and record the disposition here.
