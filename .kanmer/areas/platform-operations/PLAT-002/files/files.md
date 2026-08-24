# File map — PLAT-002

## Change surface

- `docs/operations.md` — delete the "Temporary verification account" clause (`:768-775`); record the removal, its date and its release number.
- `docs/desktop/10-security-observability-performance/threat-register.md` — mark the corresponding row closed once the production confirmation exists ([[DSK-10-01]]).

## Context files and evidence

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-02`
- Plan detail: same file § 2 (Facts — Secrets), § 4 (target state), § 7 (risks and traps — "Plaintext verification account shipped to go-live")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 17.1 Required controls `:1153-1172`; § 24 Phase 8 exit gate `:1885-1890`
- Repository evidence:
  - `src/Pegasus.Web/appsettings.json:8-14` — the committed block, including the `//` comment that documents the retirement mechanism
  - `src/Pegasus.Web/Program.cs:678-702` — the bootstrap gate that decides whether reconciliation runs
  - `src/Pegasus.Web/Program.cs:986-1040` — `ReconcileVerificationAccountAsync`: when `Bootstrap:VerificationAccount:Removed` is set (and no username), it calls `userManager.FindByNameAsync(removed.Trim())` then `DeleteAsync` and returns
  - `docs/operations.md:768-775` — the operations clause to be removed
  - `scripts/Invoke-ProductionSmoke.ps1:1-45` — the production smoke entry point and its mandatory parameters
- Binding decisions:
  - **D-001** (2026-08-23) — the fork becomes the single release source at the first production gateway change; this deployment goes through the fork's release route, not upstream.
  - **L-02** — there is no Azure test environment (ADR-0014); the change is proved on the local stack first, then in production.
- Depends on: an operator decision and an operator-run production release. The plan row records the dependency as "operator decision"; it resolves to no plan handle, so it is not listed in `dependsOn`.

## Ripple effects and acceptance

- [ ] `src/Pegasus.Web/appsettings.json` contains no username and no password; the `Bootstrap:VerificationAccount` object holds only `Removed`.
- [ ] A test asserts that the `Removed` form deletes the named account on start, and that an absent block deletes nothing.
- [ ] `git grep "Pegasus-UI-Verify"` returns no configuration hit in the working tree.
- [ ] The account is confirmed absent on the production estate after the deployment, with dated evidence in the ticket proof.
- [ ] `docs/operations.md` no longer instructs that the account exists; it records the removal, its date and its release.

## Deliberately out of scope

- **Azure**: no write from this ticket's tooling. The production **deployment** is an operator-run release under `docs/runbook.md` § Live-operation approval matrix ("Deploy, restore, fail over, or retire": exact environment, explicit approval, rollback path) and is mirrored in `docs/desktop/11-azure-disposition/README.md`. Azure MCP use is read-only.
- **Scope boundary**: may touch `src/Pegasus.Web/appsettings.json`, `tests/Pegasus.IntegrationTests`, `scripts/Invoke-ProductionSmoke.ps1`, `docs/operations.md`. Must not touch `src/Pegasus.Core`, the desktop projects, `infra/`, or any other credential. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: the password is in git history and cannot be un-published — treat the credential as permanently disclosed and say so in the proof; deleting the block **without** the `Removed` key leaves the account alive on the estate because reconciliation then does nothing (`Program.cs:686-687`, `:1016-1019`); the two-environments rule (ADR-0014) means there is no staging rehearsal — the local stack is the rehearsal.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.
