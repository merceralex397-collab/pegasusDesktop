# EPIC-001 — Area 00 · Governance and workflow

Read this once before working any `DSK-00-*` ticket (board ids `FND-001`…`FND-013`).
It carries what binds the whole batch; the per-ticket detail is in the ticket body.

## What this epic delivers

The rules every other conversion area assumes: the fork's branch topology (`dev`, the
read-only one-way `upstream` sync) and release tags; this Kanmer board's shape and the
208-ticket seed plus the upstream carry-over; the reserved ADR-0100…ADR-0110 block and
the ADRs themselves; FRD-13, the PRD scope change and the `DSK` capability family; the
ticket template and its enforcement; the canonical parity-matrix path. Every row is
documentation, git topology or board mechanics — **no ticket in this epic changes
`src/`, `tests/` or `.github/workflows/`**.

## Proposal coverage

§1 executive decision, §2 authority/scope/non-goals, §3 reconciliation, §6 repository
strategy (fork, not greenfield), §24 phase map, §25 ticket structure, §26 documentation
set, §27 acceptance criteria, §28 optimality (recorded, not re-argued), §29 immediate
next actions, Appendix A ADR template. The §4 cloud-justification *test* is defined
here; its per-capability answers belong to areas 03, 07 and 11.

## Decisions, assumptions and deviations binding every ticket

- **L-01** gateway is `Pegasus.Web` evolved in place — no new deployment unit.
- **L-02** Test/UAT is a local production-mimicking stack; **ADR-0014 is not superseded**;
  a ticket that asks for an Azure test resource is out of bounds.
- **L-03** report rendering moves to an isolated non-UI WebView2 path (ADR-0108).
- **L-04** every ticket names its subagent, skills and MCP tools.
- **L-05** the board is seeded from these plans — the board is the plan's executable form.
- **D-001** (2026-08-23) the fork becomes the single release source at the first
  production gateway change; upstream is merged once more, then frozen.
- **D-002 / D-003** (2026-08-23) self-managed signing certificate; UNC update feed.
  **C-01** the repositories become private on completion (GitHub Releases/Pages ruled
  out permanently; private Windows runner minutes bill at 2×).
- **Deviation — reserved ADR block.** `AGENTS.md` normally says "next free number"; the
  conversion uses **ADR-0100…ADR-0110** because upstream keeps issuing numbers below
  0100 and a sync would collide. Operator-confirmed 2026-08-23 and recorded in
  `AGENTS.md` § ADR conventions.
- **Deviation — parity-matrix path.** Proposal §23 names `docs/features/…`; the plan set
  uses `docs/desktop/01-inventory-and-parity/parity-matrix.md`.
- **A-00-3** if a board area can only be created in the Kanmer GUI, that step is
  operator-performed and recorded as such.
- Authority order for any conflict: operator notes > PRD > FRD > capabilities > ADRs >
  current-state docs > working rules > **these plans** > skill guidance.

## Exit gate and what proves it

`pwsh ./scripts/Test-DocumentationLinks.ps1` and the Markdown placement gate pass on
`dev`; `get_status` shows the 16 areas and 24 groups; `list_items` returns every DSK
ticket with its labels and `docs_todo`/`refs`; `get_doc_gates` on a `feature` ticket
shows the `governing-doc` requirement is satisfiable; ADR-0100…ADR-0110 exist
(ADR-0108 may stay `proposed` until the Phase 7 spike); FRD-13 and the PRD/capabilities
updates are merged; `dev` exists, `upstream` is fetch-only and its first sync has landed.

## Routing for this epic

| Need | Subagent | Skills (name · pinned source) | MCP |
| --- | --- | --- | --- |
| Board and ticket mechanics | — (parent session) | `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-setup`, `kanmer-tickets`, `kanmer-groom` (`.grok/skills/<name>/SKILL.md`, Kanmer 0.1.0) | Kanmer `get_status`, `list_board`, `list_groups`, `list_items`, `search_items`, `create_items`, `update_item`, `set_group_doc`, `link_doc`, `get_doc_gates` |
| ADR/FRD/PRD authoring | `pegasus-parity-researcher` (read-only evidence; it cannot write files) | `pegasus-desktop` → `kanmer-docs`; `microsoft-docs` for any API claim | Kanmer `link_doc`, `set_ticket_doc`; Microsoft Learn `microsoft_docs_search`, `microsoft_docs_fetch` |
| Release-tag convention | `pegasus-release-packager` | `pegasus-desktop` → `pegasus-release` (`.agents/skills/pegasus-release/SKILL.md`) | — |
| Upstream-sync and governance review | `pegasus-desktop-reviewer` | `pegasus-desktop` → `kanmer-review` | Kanmer `get_ticket_doc` |

Subagents are `.codex/agents/<name>.toml`. Never load a skill from the
"Not applicable — do not load" table in `docs/desktop/12-agent-tooling/skill-routing.md`
(no `azure-deploy`/`azure-prepare` family, no `entra-*`, no `winui-wpf-migration`).

## Traps (area plan § 7)

- Any `.md` outside `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job
  (`scripts/Test-MarkdownPlacement.ps1:31`); ticket-transient documents live in Kanmer.
- ADR collision with upstream: never take the next free number; re-check
  `docs/adr/README.md` after every sync. **ADR bodies are immutable once published.**
- `scripts/Test-MainBranchHistory.ps1` fails a push to `main` whose history is not
  contained in `dev`; never merge `upstream` straight into `main`; a GitHub squash or
  rebase merge is not an exact-SHA promotion.
- Upstream syncs bring Razor/web changes the conversion will retire — merge them anyway
  until cutover and raise upstream-owned defects upstream.
- Kanmer: creation is ungated (a re-seed duplicates silently); a move crosses one gated
  boundary; an unticked `open-questions/` item blocks; `board.yml` is not the effective
  gate set — `get_doc_gates` is.
- Capability IDs (`CASE-17`, `DSK-01`) are not ticket IDs (`CASE-017`, `DSK-00-01`).
- Operator copy rules bind every UI ticket; explanation in the UI is a defect.
- Two environments only (ADR-0014).

## Read before starting any ticket in this epic

1. `.agents/skills/project/pegasus-desktop/SKILL.md`
2. `docs/desktop/00-governance-and-workflow/README.md` (the whole file)
3. `docs/desktop/README.md` — decisions, routing legend, area index
4. `AGENTS.md` — Kanmer block (lines 1–22), § ADR conventions, § New Markdown placement,
   § Repository task workflow
5. `docs/engineering.md` lines 1–52 (branches and delivery) and § Required evidence tiers
6. `docs/index.md` § Authority
7. `docs/desktop/12-agent-tooling/skill-routing.md` — exact skill, subagent and MCP names
8. `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` §§ 1–3, 6, 24–29, Appendix A
