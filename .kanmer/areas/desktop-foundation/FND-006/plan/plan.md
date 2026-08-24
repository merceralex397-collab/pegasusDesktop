# Plan — FND-006: Author ADR-0102, ADR-0106, ADR-0107 and ADR-0109 from the area 01 flow records

**Diff estimate: ~5 files, ~410 lines.**

`docs/engineering.md:201-207` § Plan sizing requires the estimate first, derived
from the `files` document rather than shrugged. Four new ADRs at the measured
house length (ADR-0015 is 66 lines, ADR-0028 84, ADR-0025 114; each of these
carries the eight-row cloud-justification table plus an evidence-dense Context,
so ~100 each → ~400), plus four index rows in `docs/adr/README.md` (~4 lines).
Five files, ~410 lines. No `AGENTS.md` line here — that correction is
[[FND-005]]'s.

## Approach

Gate on evidence first, then write all four in one PR. These four decisions
share one property that the other conversion ADRs do not: each can only be
written honestly *after* the current flow is recorded, because proposal § 4
demands evidence per cloud-justification answer rather than prose. So step 2 is
a real gate — [[FND-019]] (plan handle `DSK-01-06`) and [[FND-020]] (plan handle
`DSK-01-07`) must be `done` with every relevant record question carrying a code
citation or a line in `docs/open-decisions.md` — and the ticket stops rather
than guessing.

Writing the four together, rather than one per PR, is deliberate: ADR-0106 and
ADR-0107 answer the same "unattended execution / protected credentials" pair
from the same Worker evidence, and ADR-0102's revocation story is what makes
ADR-0107's broker model coherent. The rejected alternative was pairing each ADR
with its consuming area ticket (0102 with area 04, 0107 with area 07): that
would let a slice ship against an unwritten decision, which is the situation
this ticket exists to end.

**ADR-0109 is sourced differently and that must not be forgotten.** There is no
flow record for telemetry — records 1–6 are authentication, database/migrations,
Graph intake, Box custody, DVLA/DVSA and report rendering. Its evidence is
`src/Pegasus.Web/Program.cs:196`, `src/Pegasus.Worker/Program.cs:14-15`, the
capped Log Analytics workspace at
`docs/desktop/01-inventory-and-parity/azure-resource-register.md:36`, and
**upstream PLAT-034**. Do not wait for a record that does not exist.

## Governing docs

`refs` is empty and `docs_todo: true` — confirm with `get_doc_gates FND-006`,
which for profile `feature` shows `leave-backlog: [governing-doc]` satisfied by
`docs_todo`, and `leave-preparing: [research, files, plan, checklist,
questions-resolved]`.

> **New ADRs — this ticket authors them.** ADR-0102 (existing Pegasus
> credentials; short-lived access token plus rotated refresh token), ADR-0106
> (Graph intake worker stays central), ADR-0107 (Box and DVLA/DVSA credentials
> stay behind the gateway) and ADR-0109 (desktop diagnostics bundle beside the
> existing Application Insights).
> ADR-0102 is co-claimed by [[FND-042]] (plan handle `DSK-04-01`), so it is
> `authored by whichever of the two is worked first; the other verifies and
> extends it in place` — see step 4 for the reconciliation. There is **no**
> operator ownership question outstanding on ADR-0102 (unlike ADR-0105, which is
> [[FND-005]]'s and [[REL-001]]'s to reconcile).
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (the ADR set table)
> and to the flow records; if a completed record contradicts a provisional
> answer, this plan is revised before implementation.

Because `refs` is empty, the authorities that bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `AGENTS.md:78-83` | Stable IDs; supersede by a new ADR | Steps 4, 7 (these four supersede nothing) |
| `AGENTS.md:84-90` | The operator-confirmed reserved block ADR-0100–ADR-0110 | Step 4 |
| `AGENTS.md:94-108` | The YAML frontmatter block, verbatim in shape | Step 5 |
| `AGENTS.md:109-110` | Heading set `Status · Context · Decision · Consequences · Options considered · Links` | Step 5 |
| `AGENTS.md:111-113` | No dated cost tables or runbooks in an ADR; feature behaviour belongs in an FRD | Steps 6–8 |
| `docs/adr/README.md:12-14` | Published bodies are immutable — an unanswered question cannot be patched later | Step 2 (the evidence gate) |
| `docs/adr/README.md:18-19` | Index columns `ADR \| Title \| Related FRD` | Step 9 |
| Proposal § 4 | The six-question test answered with evidence, never prose | Step 6 |
| Proposal § 8, § 12.1, § 12.2–12.3, § 18 | The four decisions' subject matter | Step 8 |
| `flow-records.md:7-8` | A record is closed when every open question has a code citation or an `docs/open-decisions.md` line | Step 2 |
| L-01 | The gateway is `Pegasus.Web` evolved in place — ADR-0107's credential boundary is a gateway responsibility, not a new service | Step 8 |
| L-02 / ADR-0014 | Test/UAT is local; ADR-0109 adds no Azure telemetry resource | Step 8 |
| C-01 | Repositories become private; nothing here may depend on an anonymous public endpoint | Step 8 |
| `HZN-001/board-conventions.md` § *Upstream ids versus board ids* | A bare `<PREFIX>-<nnn>` is a fork board id; upstream ids are written `upstream <ID>` | Step 7 (`upstream PLAT-034`) |
| `scripts/Test-MarkdownPlacement.ps1:31` + `.github/workflows/ci.yml:71-87` | Placement and link gates on every change set | Step 10 |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § "Ticket template" requires
of the plan document specifically.

- **Subagent**: `pegasus-parity-researcher` —
  `.codex/agents/pegasus-parity-researcher.toml` (verified present). Read-only;
  it returns the `file:line` evidence each cloud-test answer needs and cannot
  write files.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-docs`
  (`.grok/skills/kanmer-docs/SKILL.md`) → `microsoft-docs` (Microsoft Learn
  plugin) for Graph and token-storage API claims.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `link_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`, `microsoft_docs_fetch`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-006` before every move; a move crosses at most one gated
  boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's twelve implementation steps; order, ownership and file
paths are the body's.

1. **Orient, then run the collision check.** Read the plan row, § 3's ADR table
   rows for 0102/0106/0107/0109, and `flow-records.md` in full (433 lines). Call
   `get_doc_gates FND-006`, then `take_ticket`. Then:
   ```
   ls docs/adr/010*
   ```
   **Measured 2026-08-24: no such file** — the highest ADR in the tree is 0029.
   If an ADR-0102 file is already there, [[FND-042]] authored it: verify it
   covers the material below and extend it in place; create no second ADR-0102
   file.
2. **Gate on evidence.** Confirm [[FND-019]] and [[FND-020]] are `done` and that
   their records carry no unanswered open question. Thirteen questions bear on
   these four ADRs, counted with
   `grep -n '^- Q[0-9]' docs/desktop/01-inventory-and-parity/flow-records.md`:
   **Q1.1–Q1.4** (`:90-99`, record 1 → ADR-0102), **Q3.1–Q3.3** (`:227-234`,
   record 3 → ADR-0106), **Q4.1–Q4.3** (`:296-303`, record 4) and **Q5.1–Q5.3**
   (`:350-354`, record 5) → ADR-0107. Each must carry a code citation or a named
   line in `docs/open-decisions.md` (`flow-records.md:7-8`). Two of them can
   change an ADR's *content* rather than merely confirm it — **Q4.1** (can the
   Box SDK issue short-lived, constrained upload/download tokens) and **Q5.1**
   (does the provider contract allow a direct public/native client call). If
   either resolves against the assumed boundary, stop and revise this plan before
   writing ADR-0107.
3. **Ask `pegasus-parity-researcher` for the per-ADR evidence set** as `file:line`
   rows: the OpenIddict/Identity registration and staff sign-in path in
   `src/Pegasus.Web` for ADR-0102 (start from `Program.cs:262-274`, `:353`,
   `:368-457`, `Mcp/AutomationMcpExtensions.cs:134`); the Graph polling trigger
   and its schedule setting in `src/Pegasus.Worker` for ADR-0106
   (`MailboxFunctions.cs:15`, `EmailEvidenceFunctions.cs:16`,
   `IntakeFunctions.cs:13,33,50,75`); the Box and DVLA/DVSA client and credential
   resolution in `src/Pegasus.Infrastructure` for ADR-0107
   (`Custody/BoxCaseCustody.cs:82-84`, `Vehicle/DvlaDvsaProductionAdapter.cs`,
   with the secret bindings at `infra/modules/platform.bicep:382-398`,
   `:555-563`); and the Application Insights registration and health surface for
   ADR-0109 (`src/Pegasus.Web/Program.cs:196`,
   `src/Pegasus.Worker/Program.cs:14-15`). It cannot write files — paste its
   answer into the ADR yourself.
4. **Create four files** under `docs/adr/`, matching the existing
   `NNNN-kebab-title.md` pattern:
   `0102-existing-pegasus-credentials-token-session.md`,
   `0106-graph-intake-worker-stays-central.md`,
   `0107-provider-credentials-behind-the-gateway.md`,
   `0109-desktop-diagnostics-bundle-and-existing-app-insights.md`.
   The ADR-0102 filename is **not** a free choice: it is the only ADR-0102 path
   the plan set itself names
   (`docs/desktop/04-auth-session-update-and-startup/README.md:296`) and the path
   [[FND-042]], [[GWY-019]], [[GWY-020]], [[GWY-021]] and [[GWY-022]] already
   name as the file they author or extend. One filename, one rule: whichever of
   the two authoring tickets is worked first authors it; the other verifies and
   extends in place, never a second file for the same number.
5. **Frontmatter and headings.** Add the `AGENTS.md:94-108` block to each
   (`id`, `status: accepted`, `date`, `supersedes: []`, `superseded_by: []`,
   `related_capabilities`, `related_frd`, `tags`) and use the heading set
   `## Status · ## Context · ## Decision · ## Consequences · ## Options
   considered · ## Links`, following `docs/adr/0029-*.md:11-20` — the newest
   house form, which opens at `## Status`. **House-style trap:** `related_frd`
   values in this repository are lowercase file stems (`[frd-08]`,
   `[frd-10, frd-11]`); the display form `[FRD-08]` appears nowhere in
   `docs/adr/*.md`.
6. **Put the six-question cloud-justification table in each `## Context`**, with
   a real answer and a real citation per row — no blank cells. Provisional
   answers with their evidence are in this ticket's `research` document under
   *Execution placement*; re-confirm each against the completed record before
   writing it. Specifically: for ADR-0106 "unattended execution" is **yes** with
   the Worker timer cited; for ADR-0102 "central enforcement" is **yes** with the
   per-request `IsEnabled` re-check and the revocation path cited; for ADR-0109
   the answers must justify why **no new telemetry service is added** — and the
   honest "measured operational advantage" answer is **no**, because the Log
   Analytics workspace is capped at 0.1 GB/day
   (`azure-resource-register.md:36`), which is evidence *against* centralising
   more. A "yes" names *where* the responsibility lands; it never means "in
   Azure" on its own.
7. **Record relations in frontmatter and in `## Links`.** ADR-0102 relates
   ADR-0004, ADR-0011 and ADR-0027; ADR-0106 relates ADR-0024; ADR-0109 relates
   the existing platform telemetry work, written **`upstream PLAT-034`** — never
   bare. The board's `platform-operations` area tops out at `PLAT-029`, so a bare
   `PLAT-034` is a fork board id pointing at nothing on the board, and the rule
   in the Kanmer group document `HZN-001/board-conventions.md` § *Upstream ids
   versus board ids* is absolute. `docs/current-architecture.md:175` is where the
   upstream item is recorded as open. **Supersede nothing** — none of these four
   replaces an accepted decision.
8. **State the negative decisions explicitly**, because later tickets rely on
   them: ADR-0107 — no long-lived provider secret is ever placed in the MSIX
   package or on a workstation; ADR-0106 — intake must continue with every
   desktop closed; ADR-0102 — no Microsoft-account or Entra login for staff;
   ADR-0109 — no OpenTelemetry collector fleet, the App Insights SDK stays the
   telemetry path. Put each in `## Decision` or `## Consequences`, not in
   passing prose.
9. **Index rows.** One row per ADR in `docs/adr/README.md`'s accepted table
   (heading `:16`, header `:18-19`), in ID order, **three cells**. Ignore the
   `AGENTS.md:114-117` sentence describing a five-column index — the real file
   contradicts it and **the file wins**; correcting that sentence is
   [[FND-005]]'s (plan handle `DSK-00-05`), not this ticket's.
10. **Run the gates**, the same two the CI `documentation` job runs at
    `.github/workflows/ci.yml:84,87`:
    ```
    pwsh ./scripts/Test-DocumentationLinks.ps1
    pwsh ./scripts/Test-TestMarkdownPlacement.ps1
    ```
    Both exit 0. Confirm every new frontmatter block parses — no tabs, no smart
    quotes.
11. **Link, then clear.** `link_doc` the four paths to this ticket, then clear
    `docs_todo` on the conversion tickets whose governing ADR now exists — area
    04 auth tickets for ADR-0102, area 07 integration tickets for ADR-0106 and
    ADR-0107, area 10 observability tickets for ADR-0109. `docs_todo: true` is
    what currently satisfies `leave-backlog` for every `feature` ticket, so link
    first and clear second; clearing without a link removes a satisfied gate.
12. **Open the PR against `dev`**, take the independent review from
    `pegasus-desktop-reviewer`, and record `n/a — docs-only` under a dated
    `## Simplification pass` heading below.

## Verification

Evidence tier 1 — Static/build/architecture (`docs/engineering.md:72-90`), as
the body states: documentation and citation integrity. The underlying flow
evidence is owned by the area 01 tickets and is **cited, not re-derived here**.

`proof` is produced from these commands, run on merged `main` after the merge:

| Command | Expected |
| --- | --- |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0 |
| `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` | exits 0 |
| `ls docs/adr/010*` | exactly one file per ADR number — no duplicate 0102 |
| `grep -c '^| ' docs/adr/0102-*.md` (and the other three) on the cloud-test block | six question rows present in each of the four files, none with an empty answer cell |
| `grep -n 'PLAT-034' docs/adr/0109-*.md` | every occurrence reads `upstream PLAT-034` |
| `grep -n 'related_frd' docs/adr/010[2679]-*.md` | lowercase stems only |
| `grep -n '0102\|0106\|0107\|0109' docs/adr/README.md` | exactly one row per ADR ID, three cells each |
| `git diff --stat -- AGENTS.md` | empty — this ticket does not edit it |

Plus, recorded in the proof: the step 1 `ls docs/adr/010*` result *from before*
the write, and the step 2 gate evidence — each of the thirteen questions with the
citation or `open-decisions.md` line that closed it.

## Risks / open questions

- **Writing an ADR ahead of its evidence.** The failure this ticket exists to
  prevent, and the one that cannot be undone: bodies are immutable
  (`docs/adr/README.md:12-14`), so a question left open becomes a superseding
  ADR later. Mitigation: step 2 is a hard gate with a counted list.
- **Q4.1 and Q5.1 can change content, not just confirm it.** If the Box SDK
  cannot issue short-lived constrained tokens, or the vehicle provider contract
  does allow a direct native call, ADR-0107's decision text changes. Mitigation:
  step 2 names them and requires a stop-and-revise rather than an accommodation.
- **A bare `PLAT-034`.** Points at a fork board id that does not exist and reads
  as conversion work. Mitigation: step 7 and a proof grep.
- **Waiting for a telemetry flow record that does not exist.** There is none;
  ADR-0109's evidence is named in step 3 and in the `files` document.
- **Copying the wrong index shape** from `AGENTS.md:115`. Mitigation: step 9
  says follow the file, and points at the ticket that owns the fix.
- **Copying ADR-0015's heading set** (no `## Status`). Mitigation: step 5 names
  ADR-0029 as the model.
- **Scope boundaries owned by named tickets, not questions:** the flow records
  themselves ([[FND-019]], [[FND-020]]); the `AGENTS.md` index sentence
  ([[FND-005]]); ADR-0108 ([[FND-007]] and [[FEAT-038]]); FRD-13 and the `DSK`
  capability family ([[FND-008]]); which of this ticket and [[FND-042]] authors
  ADR-0102 (settled by the one-file rule, no operator question outstanding).
- **Not open, and not to be reopened:** the reserved block (operator,
  2026-08-23); L-01, L-02, C-01; ADR-0014 is not superseded; and the recorded
  Send-to-AI exclusion, which has a reactivation condition and is not a conflict.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading.
Expected outcome: `n/a — docs-only`._
