# Research — PLAT-007

## Question

Give every desktop-written file — bounded cache, thumbnails, temporary document working copies, diagnostic logs — a per-user ACL, a bounded retention policy, an opaque file name that carries no case reference or personal data, and a clearing path on logout and uninstall. Verify the result on a clean Windows 11 machine.

## Findings

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-07`
- Plan detail: same file § 1 (§11.1 local cache list — "the security side of what may be cached locally"), § 4 (target state), § 7
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 11.1 What may be cached locally `:641-651`; § 16.3 Crash recovery `:1138-1146`; § 17.1 `:1153-1172`; § 22.2 Security tests `:1608-1621`
- Repository evidence:
  - New: the bounded cache and diagnostics writer in `src/Pegasus.Desktop.Infrastructure` (created by `DSK-02-06`), the host logging configuration (`DSK-02-07`) and the diagnostics bundle (`DSK-02-11`) — use the paths those tickets established
  - `tests/Pegasus.Packaging.Tests` — the packaging test project scaffolded by `DSK-02-14`/`DSK-08-10`, where the install-scope checks live
  - `docs/desktop/02-architecture-and-foundation/README.md` § 5 rows `DSK-02-06`, `DSK-02-07`, `DSK-02-11` — the abstractions this ticket hardens
- Binding decisions:
  - **ADR-0104** (to be authored) — online-required, bounded local cache only; nothing here may grow into a local replica.
  - **ADR-0109** (to be authored) — local rolling redacted logs with bounded retention; the diagnostics bundle is the support tool.
  - **ADR-0102** (to be authored) — the refresh token is DPAPI-protected; the access token stays in memory. This ticket must not introduce a second credential store.
- Depends on: `DSK-02-06` (bounded cache and diagnostics writer), `DSK-02-11` (diagnostics bundle export).

## Implications for this ticket

Proposal §17.1 `:1168` requires secure temporary-file ACLs and cleanup, §16.3 `:1143` requires per-user access controls and bounded retention for temporary document files, §11.1 `:641-651` lists exactly what may be cached locally, and §22.2 `:1621` makes temporary-file permissions a security test. A shared or roaming workstation is the first threat in §17.3. Operator-visible consequence: a case document extracted for preview stays readable by the next person to log on to that machine, or a file name in `%TEMP%` discloses a claimant's name. Siblings: [[DSK-10-09]] (log redaction and the bundle), [[DSK-10-17]] (encrypted drafts), [[DSK-10-01]] (register).

## Boundaries and assumptions

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Desktop` (only where a view model triggers clearing), the desktop test projects and `eng/packaging/`. Must not touch `src/Pegasus.Core`, `src/Pegasus.Web`, `src/Pegasus.Infrastructure`. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: a bounded cache quietly growing into a local replica is forbidden by ADR-0104 — keep the bounds enforced, not advisory; claiming "secure delete" where the platform cannot guarantee it is worse than recording the limitation; a second credential store next to the DPAPI refresh store would split the security review — there is exactly one.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Research conclusion

The ticket evidence identifies the target, routing and verification. It does not create or link a planned canonical governing document; `docs_todo` remains accurate until one exists.
