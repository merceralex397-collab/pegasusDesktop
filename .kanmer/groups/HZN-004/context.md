# Phase 3 — first vertical slice

## What this phase delivers

One complete, low-risk but representative workflow proven end to end through
the native stack (proposal §24 Phase 3): the dashboard and work queue, the case
list and search, read-only case detail, and read-only audit/history. It is the
first proof that shell, session, gateway, generated client and view models hold
together on real test data — and it is reviewed before any further conversion
(proposal §29 item 8).

## Plan folders and ticket-handle ranges

| Plan folder | Handles | Board area |
| --- | --- | --- |
| `docs/desktop/05-implementation-and-migration/` slices S1–S3 | DSK-05-01 … DSK-05-03 | `desktop-features` (FEAT) |
| `docs/desktop/06-ui-design/` | DSK-06-01 … DSK-06-16 | `desktop-ui` (DUI) |
| `docs/desktop/08-testing/` | DSK-08-01 … DSK-08-19 | `testing` (TEST) |

`DSK-05-25` (parity evidence per slice) runs alongside every slice from this
phase onward. Area 06 and area 08 rows are consumed across Phases 3–8; the
screen specs and the keyboard/accessibility baseline this phase needs are the
S1–S3 blocks in `screen-specs.md`.

## Entry condition and exit gate

Entry: the Phase 2 exit gate is met — existing credentials work, no Microsoft
login, obsolete packages are blocked, disabled accounts are rejected, and token
storage passed review.

Exit gate (proposal §24 Phase 3; **owner: plan 05**):

- The native workflow uses real test data through the gateway.
- Paging, filtering and the performance budgets pass.
- The accessibility and keyboard baseline passes.
- Parallel comparison with web results matches.

## Decisions and constraints that bind this phase

- **L-01** — the slice consumes `/api/v1` on the evolved `Pegasus.Web`; the
  Razor pages it mirrors stay live and are not removed. No page is retired
  before its parity row reaches `UAT passed`.
- **L-02** — verification runs on the local Test/UAT stack (local gateway and
  Worker, Azurite, LocalDB or a SQL container, replay adapters), never on an
  Azure test environment.
- **L-04** — every ticket names its subagent, skills and MCP tools.
- **Design authority** — `docs/design/README.md` binds every UI decision, and
  operator-facing explanation is a defect (`AGENTS.md` § Simplicity rails).
- Deliberately **not** preserved from the web: TempData-retained proposed
  values, PRG redirects and `TempData` status passing, antiforgery tokens, and
  the `IAsyncPageFilter` rail-count injection — these are web mechanics, not
  business behaviour.

## Azure rule

Reads are free; every write is ⚠, exact-target approved
(`docs/runbook.md` § Live operation approval matrix) and mirrored in plan 11;
nothing is deprovisioned before cutover, observed use and rollback approval.
**Area 05 performs no Azure write** — slices consume the gateway only.

## Read before starting

- `docs/desktop/README.md`
- `docs/desktop/00-governance-and-workflow/README.md` § Phase map
- `docs/desktop/05-implementation-and-migration/README.md`
- `docs/desktop/05-implementation-and-migration/vertical-slices.md`
- `docs/desktop/05-implementation-and-migration/reuse-map.md`
- `docs/desktop/06-ui-design/README.md`
- `docs/desktop/06-ui-design/screen-specs.md`
- `docs/desktop/06-ui-design/keyboard-and-accessibility.md`
- `docs/desktop/08-testing/README.md`
- `docs/desktop/08-testing/test-uat-stack.md`
- `docs/desktop/01-inventory-and-parity/parity-matrix.md`
- `docs/design/README.md`
- `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 14, § 24 Phase 3
- `.kanmer/groups/HZN-001/board-conventions.md`
