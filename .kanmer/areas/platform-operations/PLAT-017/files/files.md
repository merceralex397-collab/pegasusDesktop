# File map — PLAT-017

## Direct change surface

- `docs/desktop/10-security-observability-performance/threat-register.md` — update the "duplicate or conflicting data writes" and "third-party provider outage" rows with these tests.
- `docs/frd/frd-13-desktop-operator-experience.md` — the operation-state vocabulary and the recovery copy, once FRD-13 exists (`DSK-00-08`).
- `docs/runbook.md` — what support should ask for after a crash (the bundle path shown to the operator).

## Context files

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

## Ripple effects

- [ ] One `OperationState` enum with exactly the six §16.1 values and one execution helper every non-trivial command uses.
- [ ] `Uncertain` is produced for a post-send timeout and carries recovery advice that does not offer an unguarded retry.
- [ ] Every operation carries a correlation id and, where repetition could duplicate effects, the existing `OperationKey`; a replayed key returns the same outcome.
- [ ] Cancellation gives immediate feedback and resolves to `Cancelled` only when the server had not committed.
- [ ] Drafts exist only for approved long forms, are DPAPI-encrypted, hold no plaintext, and are cleared by a confirmed save.
- [ ] Recovery is offered after an abnormal exit, routes through compare-and-reapply when the server has moved on, and never partially applies a corrupted draft.
- [ ] The unhandled-exception path writes a bundle and exits; a test proves the process does not continue.

## Out of scope

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts` (only to place a shared operation type), and the desktop test projects. Must not touch `src/Pegasus.Core` policy or `src/Pegasus.Web`; the gateway idempotency behaviour belongs to `DSK-03-08`. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: crash handling that swallows exceptions and continues in a corrupted state is the specific failure this ticket exists to prevent — the exit is not optional; a draft store that grows into an offline queue contradicts ADR-0104 and proposal §11.3, which forbids queueing a save silently as a server command; collapsing `Uncertain` into `Failed` produces duplicate writes on retry; a second encryption scheme beside the DPAPI credential store splits the security review; drafts are approved per form, never global.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.
