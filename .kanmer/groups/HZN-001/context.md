# Phase 0 — discovery, inventory and decisions

## Delivers

Everything that must be known before a line of native code is written (proposal §24 Phase 0): the fork's branch and board setup, the repository-derived feature-parity matrix, six current-flow records, an Azure resource register built from read-only checks, the triage of the open upstream Kanmer board, pinned and vendored agent skills with their subagents, recorded baseline performance and business fixtures, and the first conversion ADRs. Nothing here changes runtime behaviour.

## Plan folders and ticket-handle ranges

- `docs/desktop/00-governance-and-workflow/` — DSK-00-01…DSK-00-13 → `desktop-foundation` (FND)
- `docs/desktop/01-inventory-and-parity/` — DSK-01-01…DSK-01-12 → `desktop-foundation` (FND)
- `docs/desktop/11-azure-disposition/` — DSK-11-01…DSK-11-09 → `platform-operations` (PLAT); the deprovision checklist (DSK-11-08) is *prepared* here and executed only after the Phase 10 exit gate
- `docs/desktop/12-agent-tooling/` — DSK-12-01…DSK-12-11 → `agent-tooling` (TOOL)

## Entry condition and exit gate

Entry: none — this is the first phase of the programme.

Exit gate (proposal §24 Phase 0; **owner: plan 01**, per `docs/desktop/00-governance-and-workflow/README.md` § Phase map):

- Every current production capability has an inventory row.
- Every Azure resource has an owner/use statement.
- No unresolved uncertainty remains around authentication, database or Graph intake.
- Target dependency rules compile as architecture tests or documented checks.

Plan 00 adds a governance part: `pwsh ./scripts/Test-DocumentationLinks.ps1` and the placement gate pass on `dev`, and no ticket can leave `backlog` without a governing doc.

## Decisions and constraints that bind this phase

- **L-04** — every ticket names its subagent, skills and MCP tools; area 12 is what makes that true.
- **L-05** — this board is seeded from these plans; the open upstream board is triaged in area 01.
- **D-001** (2026-08-23) — the fork becomes the single release source at the first production gateway change; until then the one-way `upstream` sync continues (DSK-00-10 records it).
- **C-01** (2026-08-23) — the repositories become private on completion: GitHub Releases and Pages are ruled out permanently, and private Windows runner minutes bill at 2×.
- **Reserved ADR block** — the conversion uses ADR-0100…ADR-0110, never "next free number"; upstream keeps issuing ADRs and would collide.

## Azure rule

Reads are free; every write is ⚠, needs exact-target approval (`docs/runbook.md` § Live operation approval matrix) and is mirrored in plan 11; **nothing is deprovisioned before cutover, observed use and rollback approval**. This phase inventories the estate and removes nothing.

## Read before starting

- `docs/desktop/README.md`
- `docs/desktop/00-governance-and-workflow/README.md`
- `docs/desktop/01-inventory-and-parity/README.md`, with `parity-matrix.md`, `flow-records.md`, `azure-resource-register.md` and `upstream-kanmer-carryover.md` in the same folder
- `docs/desktop/11-azure-disposition/README.md`
- `docs/desktop/12-agent-tooling/README.md`, with `skill-routing.md` and `subagents.md`
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 24 Phase 0
- `.kanmer/groups/HZN-001/board-conventions.md`


## Operator scope amendment — 2026-08-25

The operator has prohibited all upstream synchronization. This supersedes the historical D-001 sync cadence in this horizon context. Phase-0 work remains in-repository only, using the configured `pegasusDesktop` remote and current local history. No upstream remote is added, fetched, merged, compared, or pushed. Cloud writes and deployments remain deferred until the full refactor is complete.
