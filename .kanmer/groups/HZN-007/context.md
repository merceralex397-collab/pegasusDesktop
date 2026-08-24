# Phase 6 — documents, Box and vehicle services

## What this phase delivers

Evidence handling end to end (proposal §24 Phase 6): the Box browser, the
transfer queue with progress, cancel and retry, preview and a temporary cache,
document metadata and custody audit, the DVLA/DVSA/MOT workflow with distinct
provider error states, and image handling with a reusable gallery.

## Plan folders and ticket-handle ranges

| Plan folder | Handles | Board area |
| --- | --- | --- |
| `docs/desktop/05-implementation-and-migration/` slices S14–S16 | DSK-05-14 … DSK-05-16 | `desktop-features` (FEAT) |
| `docs/desktop/07-integrations/` (Box, DVLA/DVSA, images) | DSK-07-01 … DSK-07-19 | `desktop-features` (FEAT) |

`DSK-07-18` is a **spike** on desktop-side ONNX VRM/image preprocessing
placement — a written recommendation only; no engine moves without an accepted
ADR. `DSK-05-25` parity evidence continues.

## Entry condition and exit gate

Entry: the Phase 5 exit gate is met — intake arrives with the desktop closed,
duplicate and failure paths pass, no desktop holds Graph credentials, and
source-to-case traceability is complete.

Exit gate (proposal §24 Phase 6; **owner: plan 05**):

- Large and failed transfers recover safely.
- Provider secrets are absent from the package.
- Provider rate and error handling passes.
- Document parity is approved.

## Decisions and constraints that bind this phase

- **ADR-0107** — Box and DVLA/DVSA credentials stay behind the gateway; no
  long-lived provider secret is ever placed in the MSIX. The package content
  scan in area 10 checks this.
- **L-01** — the gateway brokers every provider call; the desktop holds the
  transfer queue, cache and UI state only.
- **L-02** — provider behaviour is exercised through replay adapters on the
  local Test/UAT stack; large-transfer and failure recovery tests run there.
- **ADR-0104** — online-required, bounded local cache only. The document cache
  is temporary; it is not a local database.

## Azure rule

Reads are free; every write is ⚠, exact-target approved
(`docs/runbook.md` § Live operation approval matrix) and mirrored in
`docs/desktop/11-azure-disposition/README.md`; nothing is deprovisioned before
cutover, observed use and rollback approval. Storage and Container App
resources touched by document workflows are **read** here — the register in
plan 11 records their target position, and no removal happens until Phase 10.

## Read before starting

- `docs/desktop/README.md`
- `docs/desktop/00-governance-and-workflow/README.md` § Phase map
- `docs/desktop/05-implementation-and-migration/README.md`
- `docs/desktop/05-implementation-and-migration/vertical-slices.md`
- `docs/desktop/07-integrations/README.md`
- `docs/desktop/03-gateway-api-and-data/endpoint-map.md`
- `docs/desktop/06-ui-design/screen-specs.md`
- `docs/desktop/01-inventory-and-parity/flow-records.md`
- `docs/desktop/01-inventory-and-parity/parity-matrix.md`
- `docs/desktop/08-testing/test-uat-stack.md`
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.2, § 12.3, § 13.5, § 13.7, § 24 Phase 6
- `.kanmer/groups/HZN-001/board-conventions.md`
