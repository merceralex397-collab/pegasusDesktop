# Phase 9 — pilot and parallel operation

## What this phase delivers

The desktop and the web application running side by side on real work
(proposal §24 Phase 9): deploy the backward-compatible gateway, release to the
pilot ring, run both in parallel, compare records and reports, collect
diagnostics, fix parity defects, and train users with concise workflow
guidance.

## Plan folders and ticket-handle ranges

| Plan folder | Handles | Board area |
| --- | --- | --- |
| `docs/desktop/09-release-update-and-distribution/` | DSK-09-01 … DSK-09-18 (pilot-ring and runbook subset) | `release-desktop` (REL) |
| `docs/desktop/08-testing/` | DSK-08-01 … DSK-08-19 | `testing` (TEST) |
| `docs/desktop/01-inventory-and-parity/` (parity evidence) | DSK-01-01 … DSK-01-12 | `desktop-foundation` (FND) |

`DSK-09-07` and `DSK-09-09` are **withdrawn** — do not depend on them.
`DSK-05-25` completes each slice's parity row here.

## Entry condition and exit gate

Entry: the Phase 8 exit gate is met — full automated suite green, accessibility
critical issues resolved, no unresolved high-risk security item, and a
production-like package tested.

Exit gate (proposal §24 Phase 9; **owner: plan 09**):

- Pilot users complete all normal workflows.
- No unexplained data divergence.
- Update and rollback have been exercised.
- The support runbook is proven.
- Explicit cutover approval is given.

## Decisions and constraints that bind this phase

- **L-02** — this is the *production* pilot ring, and it is the only place
  real-Azure validation happens; there is still no Azure dev/test/staging
  environment (ADR-0014 stands).
- **D-001** (2026-08-23) — the fork is the single release source; upstream is
  merged one final time and then frozen. Agreeing the freeze with the upstream
  repository's owners is an **operator step** (DSK-00-10).
- **D-002** — packages are signed with the self-managed certificate; each pilot
  workstation must trust it in `LocalMachine\TrustedPeople` (an operator step).
- **D-003** — pilot packages are published to the UNC share and served to App
  Installer over SMB; provisioning and access on that share are operator steps.
- **C-01** — no anonymous HTTPS feed; GitHub Releases and Pages are ruled out.

## Azure rule

Reads are free. Every write is ⚠, needs exact-target approval
(`docs/runbook.md` § Live operation approval matrix) and is mirrored in
`docs/desktop/11-azure-disposition/README.md`. The backward-compatible gateway
deployment is the real write in this phase and goes through the `pegasus-release`
route, never ad hoc. **Nothing is deprovisioned** — parallel operation is what
proves what is still in use.

## Read before starting

- `docs/desktop/README.md`
- `docs/desktop/00-governance-and-workflow/README.md` § Phase map
- `docs/desktop/09-release-update-and-distribution/README.md`
- `docs/desktop/09-release-update-and-distribution/runbooks.md`
- `docs/desktop/09-release-update-and-distribution/appinstaller-template.md`
- `docs/desktop/08-testing/README.md`
- `docs/desktop/01-inventory-and-parity/parity-matrix.md`
- `docs/desktop/11-azure-disposition/README.md`
- `docs/operations.md`
- `docs/runbook.md` § Live operation approval matrix
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 23, § 24 Phase 9
- `.kanmer/groups/HZN-001/board-conventions.md`
