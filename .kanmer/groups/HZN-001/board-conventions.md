# Board conventions — Pegasus native desktop conversion

Read this before touching any `DSK` ticket on this board. It records how the
208 seeded tickets are joined to the plan set, how they are grouped and
labelled, and which rules are authoritative when two sources disagree.

Source: `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board
shape, and the seeding contract used to create the tickets.

---

## 1. The ticket handle lives in the title

Every seeded ticket's title begins with its **plan handle**:

```
DSK-<area>-<nn> · <imperative title>
```

for example `DSK-02-04 · Add the Pegasus.Contracts project`. The board issues
its own id from the area prefix (`FND-001`, `GWY-007`, …); the handle in the
title is what joins the ticket back to its row in the plan set. `<area>` is the
two-digit **plan folder** number, `<nn>` the row number within that folder's
§ 5 work breakdown.

To find a ticket's plan row: `docs/desktop/<area>-*/README.md` § 5, the row
whose ID column equals the handle. Never renumber a handle, and never create a
second ticket for one handle.

`DSK-09-07` and `DSK-09-09` are **withdrawn**. They do not exist on the board
and nothing may depend on them.

## 2. Plan area → board area

| Plan folder | Board area id | Prefix |
| --- | --- | --- |
| 00 governance and workflow | `desktop-foundation` | FND |
| 01 inventory and parity | `desktop-foundation` | FND |
| 02 architecture and foundation | `desktop-foundation` | FND |
| 03 gateway API and data | `gateway-api` | GWY |
| 04 auth, session, update, startup | `gateway-api` (gateway rows) / `desktop-foundation` (desktop rows) | GWY / FND |
| 05 implementation and migration | `desktop-features` | FEAT |
| 06 UI design | `desktop-ui` | DUI |
| 07 integrations | `desktop-features` | FEAT |
| 08 testing | `testing` | TEST |
| 09 release, update, distribution | `release-desktop` | REL |
| 10 security, observability, performance | `platform-operations` | PLAT |
| 11 Azure disposition | `platform-operations` | PLAT |
| 12 agent tooling | `agent-tooling` | TOOL |

`mail-communications` (MAIL), `automation-integrations` (AUTO),
`documents-reports` (DOCS), `engineering-assessment` (ENG),
`intake-processing` (INTK), `case-reference-workflow` (CASE),
`delivery-repository` (DELIV) and `pr-review` (PR) exist for the **upstream
carry-over triage** (plan 01) and hold no seeded conversion ticket.

### Deviation to note

The board-shape table in plan 00 assigns no area to plan folders **10** and
**11**. Both are seeded into `platform-operations` (PLAT) — the closest
existing home for cross-cutting platform, security, observability and cloud
work. This is a deliberate deviation from the table, recorded here so nobody
"fixes" it silently; if a later decision gives 10 or 11 its own area, move the
tickets and update this document and plan 00 together.

## 3. Group model — one EPIC and one HZN per ticket

Every conversion ticket belongs to **exactly two groups**:

- one **EPIC** — its plan area (`EPIC-001` … `EPIC-013` for plan folders
  00 … 12, in order);
- one **HZN** — its proposal phase (`HZN-001` … `HZN-011` for Phases 0 … 10,
  in order).

| Group | Plan area | | Group | Phase |
| --- | --- | --- | --- | --- |
| EPIC-001 | 00 governance and workflow | | HZN-001 | Phase 0 discovery |
| EPIC-002 | 01 inventory and parity | | HZN-002 | Phase 1 foundation |
| EPIC-003 | 02 architecture and foundation | | HZN-003 | Phase 2 compatibility/auth |
| EPIC-004 | 03 gateway API and data | | HZN-004 | Phase 3 first slice |
| EPIC-005 | 04 auth, session, update, startup | | HZN-005 | Phase 4 editing/concurrency |
| EPIC-006 | 05 implementation and migration | | HZN-006 | Phase 5 intake/comms |
| EPIC-007 | 06 UI design | | HZN-007 | Phase 6 documents/Box/vehicle |
| EPIC-008 | 07 integrations | | HZN-008 | Phase 7 assessment/reports |
| EPIC-009 | 08 testing | | HZN-009 | Phase 8 admin/hardening |
| EPIC-010 | 09 release, update, distribution | | HZN-010 | Phase 9 pilot/parallel |
| EPIC-011 | 10 security, observability, performance | | HZN-011 | Phase 10 cutover |
| EPIC-012 | 11 Azure disposition | | | |
| EPIC-013 | 12 agent tooling | | | |

Membership lives on the ticket (`update_item(groups: [...])`), never on the
group. Each `HZN-0nn/context.md` carries the phase's entry condition, exit
gate, binding decisions and its own read-before-starting list — read your
ticket's horizon context before you plan the ticket. The EPIC `context.md`
files are written by the plan-area agents, not by the board setup.

## 4. Label vocabulary

Use these labels and no invented siblings:

| Label | When |
| --- | --- |
| `desktop-conversion` | Every seeded conversion ticket, without exception. |
| `plan-NN` | The plan folder the ticket was cut from — `plan-00` … `plan-12`. |
| `phase-N` | The proposal phase — `phase-0` … `phase-10`. Matches the HZN group. |
| `tier-N` | Each evidence tier in the plan row's Tier column (`docs/engineering.md` § Required evidence tiers). A row marked `5/9` carries both `tier-5` and `tier-9`. |
| `azure-write` | The row carries a ⚠ Azure write. Implies an approval step. |
| `needs-operator` | A step only the human operator can perform: certificate issuance and trust rollout, UNC share provisioning, upstream-repository actions, physical workstation access. |

## 5. Profiles and the gates they oblige

| Profile | leave-backlog | leave-preparing | enter-review | enter-done |
| --- | --- | --- | --- | --- |
| `feature` | governing-doc | research, files, plan, checklist, questions-resolved | post-implementation-report, questions-resolved | proof, questions-resolved |
| `fix` | — | files, plan, questions-resolved | post-implementation-report, questions-resolved | proof, questions-resolved |
| `chore` | — | plan, questions-resolved | — | proof, questions-resolved |
| `spike` | — | — | — | research, questions-resolved |

The profile comes from the plan row. Where a row states none, `feature` is the
default; use `fix` only for a pure defect, `chore` for pure hygiene or
mechanics, and `spike` for a timeboxed investigation.

**Gates are read with `get_doc_gates <id>` before every move — never from
`board.yml`.** The table above is a summary for planning; the server is the
authority, a move may cross at most one gated boundary, and an unticked
`open-questions/` item blocks the move regardless of what the table says.

Stages: `backlog` → `preparing` → `implementing` → `review` → `verifying` →
`done`. Pipeline: `kanmer-research` → `kanmer-plan` → `kanmer-execute` →
`kanmer-review` → `kanmer-verify` → `kanmer-closeout`.

## 6. Rules that outrank convenience

- **Governing doc**: a `feature` cannot leave `backlog` without one. Most
  conversion tickets owe a document that does not exist yet
  (ADR-0100…ADR-0110, FRD-13) and carry `docs_todo: true`; `refs` names only
  documents that already exist in `docs/adr/`, `docs/frd/`, `docs/prd/`.
- **Reserved ADR block**: the conversion uses ADR-0100…ADR-0110, never "next
  free number" — upstream keeps issuing ADRs and would collide.
- **Azure rule**: reads are free; every write is ⚠, needs exact-target approval
  (`docs/runbook.md` § Live operation approval matrix) and is mirrored in
  `docs/desktop/11-azure-disposition/README.md`; **nothing is deprovisioned
  before cutover, observed use and rollback approval**.
- **Branching**: `task/<slug>` → PR into `dev` → exact-SHA promotion to `main`
  with the literal `MERGE AUTH GRANTED`. Never merge upstream straight into
  `main`.
- **Simplification pass** (`AGENTS.md` step 4) over the branch's own diff
  before every PR, recorded under a dated `## Simplification pass` heading in
  the plan document (`n/a — docs-only` for documentation-only tickets).
- **Independent review** by an agent that did not implement — for this
  conversion, `pegasus-desktop-reviewer`.
- **Markdown placement**: any new `.md` outside
  `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job.
  Ticket-transient documents live in Kanmer, not in the tree.
- **Capability ids are not ticket ids**: `CASE-17` (capability) is not
  `CASE-017` (ticket). Handles avoid the collision — use them.
