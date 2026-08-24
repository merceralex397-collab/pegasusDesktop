# Phase 4 — case editing and concurrency

## What this phase delivers

The primary write workflow (proposal §24 Phase 4): case create and edit,
validation, assignment and status, workflow/closure/tasks commands, parties and
reference data, optimistic concurrency with leases, audit, the conflict and
lease-lost UX, and a local draft only where it is justified.

## Plan folders and ticket-handle ranges

| Plan folder | Handles | Board area |
| --- | --- | --- |
| `docs/desktop/05-implementation-and-migration/` slices S4–S8 | DSK-05-04 … DSK-05-08 | `desktop-features` (FEAT) |
| `docs/desktop/03-gateway-api-and-data/` (write endpoints) | DSK-03-01 … DSK-03-18 | `gateway-api` (GWY) |

Two cross-cutting area-05 rows land here: `DSK-05-24` (retire the
`CaseMutationPageModel` state machine for desktop paths, depends on
`DSK-05-05` and `DSK-05-08`) and the continuing `DSK-05-25` parity evidence.

## Entry condition and exit gate

Entry: the Phase 3 exit gate is met — the read-only slice runs on real test
data through the gateway with matching parallel comparison, and the reviewer
has signed off the foundation spike.

Exit gate (proposal §24 Phase 4; **owner: plan 05**):

- The two-user conflict test passes.
- All critical case rules are unit tested.
- No silent overwrite occurs.
- UAT approves the primary case workflow.

## Decisions and constraints that bind this phase

- **L-01** — authoritative writes, audit and concurrency stay behind the
  gateway in the evolved `Pegasus.Web`; the desktop owns interaction and
  validation only.
- **L-02** — the two-user conflict test and UAT run on the local Test/UAT
  stack; there is no Azure test environment.
- **Characterization before moving a rule** — where a rule lives in a page
  model rather than Core, the slice first moves it into Core with a test. The
  gaps this phase must close first are the create-screen draft-to-case mapping
  (S4) and the case completeness confirmation rules (S5).
- **Expand/contract** — new endpoints and desktop features ship behind the
  existing `Features:*` composition gate so `main` stays releasable for the
  live web app throughout.

## Azure rule

Reads are free; every write is ⚠, exact-target approved
(`docs/runbook.md` § Live operation approval matrix) and mirrored in
`docs/desktop/11-azure-disposition/README.md`; nothing is deprovisioned before
cutover, observed use and rollback approval. **This phase performs no Azure
write** — area 05 consumes the gateway and area 03 ships code, not
infrastructure.

## Read before starting

- `docs/desktop/README.md`
- `docs/desktop/00-governance-and-workflow/README.md` § Phase map
- `docs/desktop/05-implementation-and-migration/README.md`
- `docs/desktop/05-implementation-and-migration/vertical-slices.md`
- `docs/desktop/05-implementation-and-migration/reuse-map.md`
- `docs/desktop/03-gateway-api-and-data/README.md`
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md`
- `docs/desktop/06-ui-design/screen-specs.md`
- `docs/desktop/08-testing/test-uat-stack.md`
- `docs/desktop/01-inventory-and-parity/parity-matrix.md`
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 10.4, § 10.5, § 24 Phase 4
- `.kanmer/groups/HZN-001/board-conventions.md`
