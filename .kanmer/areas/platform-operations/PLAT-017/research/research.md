# Research — PLAT-017

## Question

Implement proposal §16.1's operation model in the desktop — a correlation identifier, an explicit state (not started, running, succeeded, failed, cancelled, uncertain), cancellation where safe, an idempotency key where repetition could duplicate effects, and user-readable recovery advice — and §16.3's crash recovery: encrypted local drafts for approved long forms, a recovery offer after an abnormal exit, drafts cleared on a successful save, and an unhandled-exception path that writes a diagnostics bundle and exits rather than continuing corrupted.

## Findings

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

## Implications

Proposal §16.1 `:1117-1126` and §16.3 `:1138-1146` set the reliability contract for every non-trivial operation. Without an explicit `uncertain` state a timed-out save is presented as either success or failure, and one of those is a lie — the operator then retries and duplicates the effect. The plan's risk table names "crash handling that swallows exceptions and continues" as a specific failure to prevent. Operator-visible consequence: silent duplicate writes, or an hour of assessment notes lost to a crash. Siblings: [[DSK-10-09]] (the bundle the crash path writes), [[DSK-10-07]] (where drafts are stored and how they are cleared), [[DSK-10-15]] (provider state), `DSK-05-08` (the concurrency UX that consumes these states).

## Constraints

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts` (only to place a shared operation type), and the desktop test projects. Must not touch `src/Pegasus.Core` policy or `src/Pegasus.Web`; the gateway idempotency behaviour belongs to `DSK-03-08`. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: crash handling that swallows exceptions and continues in a corrupted state is the specific failure this ticket exists to prevent — the exit is not optional; a draft store that grows into an offline queue contradicts ADR-0104 and proposal §11.3, which forbids queueing a save silently as a server command; collapsing `Uncertain` into `Failed` produces duplicate writes on retry; a second encryption scheme beside the DPAPI credential store splits the security review; drafts are approved per form, never global.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Conclusion

The ticket's cited evidence is sufficient to plan the bounded change. No planned canonical document is linked or claimed to exist.
