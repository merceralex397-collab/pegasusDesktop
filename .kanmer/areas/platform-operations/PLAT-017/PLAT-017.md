---
id: PLAT-017
type: ticket
title: >-
  DSK-10-17 · Reliability: the desktop operation model and crash recovery for
  approved long forms
status: backlog
area: platform-operations
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-10
  - phase-8
  - tier-7
groups:
  - EPIC-011
  - HZN-009
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:16:25.627Z'
updated: '2026-08-24T08:16:25.627Z'
---

## What

Implement proposal §16.1's operation model in the desktop — a correlation identifier, an explicit state (not started, running, succeeded, failed, cancelled, uncertain), cancellation where safe, an idempotency key where repetition could duplicate effects, and user-readable recovery advice — and §16.3's crash recovery: encrypted local drafts for approved long forms, a recovery offer after an abnormal exit, drafts cleared on a successful save, and an unhandled-exception path that writes a diagnostics bundle and exits rather than continuing corrupted.

## Why

Proposal §16.1 `:1117-1126` and §16.3 `:1138-1146` set the reliability contract for every non-trivial operation. Without an explicit `uncertain` state a timed-out save is presented as either success or failure, and one of those is a lie — the operator then retries and duplicates the effect. The plan's risk table names "crash handling that swallows exceptions and continues" as a specific failure to prevent. Operator-visible consequence: silent duplicate writes, or an hour of assessment notes lost to a crash. Siblings: [[DSK-10-09]] (the bundle the crash path writes), [[DSK-10-07]] (where drafts are stored and how they are cleared), [[DSK-10-15]] (provider state), `DSK-05-08` (the concurrency UX that consumes these states).

## Source of truth

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-17`
- Plan detail: same file § 1 (§16 coverage — "operation model and provider taxonomy carried into contracts (with 03/07); crash recovery and drafts scoped"), § 7
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 16.1 Operation model `:1117-1126`; § 16.2 External provider resilience `:1128-1136`; § 16.3 Crash recovery `:1138-1146`; § 11.1 `:641-651` ("optionally, encrypted drafts for selected long forms"); § 11.3 Connectivity handling `:667-677`
- Repository evidence:
  - New: the diagnostics writer and bounded cache from `DSK-02-06`; the unhandled-exception handler and bundle export from `DSK-02-11`; the storage locations, ACLs and retention from [[DSK-10-07]]; the redacted logging from [[DSK-10-09]]
  - New: the `OperationKey` / concurrency-token envelope types in `src/Pegasus.Contracts` (`DSK-02-04`, `DSK-03-01`) — the idempotency key is that type, not a new one
  - New: the provider error taxonomy from `DSK-07-19` (`terminal` / `transient` / `unknown` plus `not-found`, `invalid-request`, `not-authorized`, `rate-limited`, `provider-unavailable`)
  - New: the case edit slice `DSK-05-05` and the concurrency UX `DSK-05-08`, which are the first consumers
  - `src/Pegasus.Core/Identity/IdentityContracts.cs:98-137` — `CorrelationId` is already a first-class field on `SecurityEvent` and `ActionHistoryEntry`; the desktop correlation id must be the same value end to end
- Binding decisions:
  - **ADR-0104** (to be authored) — online-required; drafts are a crash-recovery convenience, never an offline queue. Proposal §11.3 `:672` forbids queueing a save silently as a server command.
  - **ADR-0102** — the DPAPI mechanism already chosen for the refresh token is the encryption mechanism for drafts; do not introduce a second scheme.
  - **ADR-0109** — the crash path writes a diagnostics bundle; that bundle is the support artefact.
- Depends on: `DSK-02-07` (Generic Host, DI, logging), `DSK-05-05` (S5 case edit with lease, version and completeness — the first long form and the first consumer of the model).

## Routing

- **Subagents**: `winui-dev` — `.codex/agents/winui-dev.toml` (implementation); `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml` (the state tests)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`; win-dev-skills v0.5.0 `f1028dd5`) → `code-testing-agent` (dotnet/skills `98f84851`, plugin `dotnet-test`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) for the recovery dialog and status copy
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for `AppDomain.UnhandledException` / `Application.UnhandledException` / `TaskScheduler.UnobservedTaskException` coverage in a WinUI 3 packaged app, and for `ProtectedData` usage for the draft blob
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

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

## Acceptance criteria

- [ ] One `OperationState` enum with exactly the six §16.1 values and one execution helper every non-trivial command uses.
- [ ] `Uncertain` is produced for a post-send timeout and carries recovery advice that does not offer an unguarded retry.
- [ ] Every operation carries a correlation id and, where repetition could duplicate effects, the existing `OperationKey`; a replayed key returns the same outcome.
- [ ] Cancellation gives immediate feedback and resolves to `Cancelled` only when the server had not committed.
- [ ] Drafts exist only for approved long forms, are DPAPI-encrypted, hold no plaintext, and are cleared by a confirmed save.
- [ ] Recovery is offered after an abnormal exit, routes through compare-and-reapply when the server has moved on, and never partially applies a corrupted draft.
- [ ] The unhandled-exception path writes a bundle and exits; a test proves the process does not continue.

## Verification

- [ ] `dotnet test` on the desktop view-model test project filtered to the operation-state tests — expected: one passing fact per state including `Uncertain`, plus the replayed-key fact.
- [ ] `dotnet test` filtered to the draft tests — expected: encryption, clearing, corruption and single-offer facts pass.
- [ ] Manual crash injection on the Test/UAT stack — expected: bundle written at the reported path, process exits, relaunch offers recovery; sequence captured in the proof.

## Evidence tier

Tier 7 — Browser/accessibility. Here that obliges the operator-facing behaviour to be exercised as a real user would: keyboard-reachable recovery dialog, focus and error behaviour, text-plus-colour operation states, and a recorded manual walkthrough — automated results do not replace the manual keyboard and assistive-technology review.

## Documentation changes

- `docs/desktop/10-security-observability-performance/threat-register.md` — update the "duplicate or conflicting data writes" and "third-party provider outage" rows with these tests.
- `docs/frd/frd-13-desktop-operator-experience.md` — the operation-state vocabulary and the recovery copy, once FRD-13 exists (`DSK-00-08`).
- `docs/runbook.md` — what support should ask for after a crash (the bundle path shown to the operator).

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts` (only to place a shared operation type), and the desktop test projects. Must not touch `src/Pegasus.Core` policy or `src/Pegasus.Web`; the gateway idempotency behaviour belongs to `DSK-03-08`. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: crash handling that swallows exceptions and continues in a corrupted state is the specific failure this ticket exists to prevent — the exit is not optional; a draft store that grows into an offline queue contradicts ADR-0104 and proposal §11.3, which forbids queueing a save silently as a server command; collapsing `Uncertain` into `Failed` produces duplicate writes on retry; a second encryption scheme beside the DPAPI credential store splits the security review; drafts are approved per form, never global.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Outcome

_Filled at closeout._
