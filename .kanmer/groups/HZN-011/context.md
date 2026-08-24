# Phase 10 — cutover and cloud rationalization

## What this phase delivers

The end of the conversion (proposal §24 Phase 10): the mandatory production
desktop release, the web application set read-only or access-restricted,
monitoring across at least one complete business cycle, disabling web-only
resources in test, removing code and infrastructure dependencies, and
deprovisioning **only** through the approved process.

## Plan folders and ticket-handle ranges

| Plan folder | Handles | Board area |
| --- | --- | --- |
| `docs/desktop/09-release-update-and-distribution/` | DSK-09-01 … DSK-09-18 (production release subset) | `release-desktop` (REL) |
| `docs/desktop/11-azure-disposition/` | DSK-11-01 … DSK-11-09 | `platform-operations` (PLAT) |

`DSK-05-26` executes the reuse-map cut list (Razor pages, partials,
`site.css`, `site.js`, the browser lane) after cutover approval. `DSK-11-08` is
the deprovision checklist — prepared in Phase 0, executed only here.
`DSK-09-07` and `DSK-09-09` are **withdrawn**.

## Entry condition and exit gate

Entry: the Phase 9 exit gate is met — pilot users complete all normal
workflows, no unexplained data divergence, update and rollback exercised,
support runbook proven, and **explicit cutover approval** given.

Exit gate (proposal §24 Phase 10; **owner: plan 11**):

- No user requires the legacy web UI.
- The cloud dependency map matches the target.
- The rollback window has expired with approval.
- Candidate resources are backed up and safely removed.

## Decisions and constraints that bind this phase

- **D-001** (2026-08-23) — the fork is the single release source; upstream is
  frozen. The freeze itself is an **operator step** in that repository.
- **D-002 / D-003** — the mandatory production release is signed with the
  self-managed certificate and published to the UNC share; the whole
  distribution path touches no Azure resource.
- **L-03** — the gateway renderer may only be retired once golden-file parity
  has passed (ADR-0108); check before it enters any cut list.
- **C-01** — the repositories become private once the conversion completes.
- **Cut-list discipline** — web-only routes are kept; only what the reuse-map
  cut list names is removed, and the build, architecture tests and release
  notes must stay green.

## Azure rule

Reads are free. Every write is ⚠, needs exact-target approval
(`docs/runbook.md` § Live operation approval matrix) and is mirrored in
`docs/desktop/11-azure-disposition/README.md`. **Deprovisioning happens only
after cutover, one full business cycle of observed use, and explicit rollback
approval** — with a dependency check, a backup and a recorded rollback path per
resource (proposal §19.2 steps 1–9). "It looks unused" is not an approval.

## Read before starting

- `docs/desktop/README.md`
- `docs/desktop/00-governance-and-workflow/README.md` § Phase map and
  § Programme exit checklist
- `docs/desktop/11-azure-disposition/README.md`
- `docs/desktop/09-release-update-and-distribution/README.md`
- `docs/desktop/09-release-update-and-distribution/runbooks.md`
- `docs/desktop/05-implementation-and-migration/reuse-map.md`
- `docs/desktop/01-inventory-and-parity/azure-resource-register.md`
- `docs/desktop/01-inventory-and-parity/parity-matrix.md`
- `docs/boundaries.md`
- `docs/operations.md`
- `docs/runbook.md` § Live operation approval matrix
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 19, § 24 Phase 10, § 27
- `.kanmer/groups/HZN-001/board-conventions.md`
