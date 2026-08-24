---
id: TOOL-008
type: ticket
title: DSK-12-08 · Author ADR-0110 — agent-skill pinning and the invocation protocol
status: preparing
area: agent-tooling
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:21:30.169Z'
labels:
  - desktop-conversion
  - plan-12
  - phase-0
  - tier-1
groups:
  - EPIC-013
  - HZN-001
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:10:38.229Z'
updated: '2026-08-24T21:21:30.169Z'
---

## What

Write `docs/adr/0110-*.md` recording the agent-skill pinning decision — the lockfile, the vendored destinations, the three pinned commits, the update-by-PR rule, the invocation protocol and the review protocol — with the six-question cloud-justification table answered, and add its row to `docs/adr/README.md`.

## Why

Every routing decision in this plan set rests on "skills are pinned and vendored, agents never fetch a moving branch". That is a durable technical decision and `AGENTS.md` § Documentation model puts durable technical decisions in an ADR, not in a plan: `docs/desktop/` is programme planning only. Without ADR-0110 the lockfile has no authority, and a future agent bumping a pinned commit without review has nothing to be in breach of. `docs/desktop/12-agent-tooling/README.md` § 8 lists ADR-0110 as this area's documentation deliverable, and `docs/desktop/00-governance-and-workflow/README.md` § 3 reserves it in the ADR-0100…ADR-0110 block.

## Source of truth

- Plan row: `docs/desktop/12-agent-tooling/README.md` § 5 — `DSK-12-08`
- Plan detail: `docs/desktop/12-agent-tooling/README.md` § 3 Decisions and assumptions, § 6 Invocation protocol and Review protocol, § 8 Documentation changes
- Plan detail: `docs/desktop/00-governance-and-workflow/README.md` § 3 — the ADR set table ("ADR-0110 | Agent-skill pinning (lockfile, vendored revisions) and invocation/review protocol | Proposal §20 | Relates `skills-lock.json`") and the cloud-justification test table to copy verbatim
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 20 in full (§20.1 purpose, §20.2 pinning and vendoring, §20.3 project skill, §20.4 routing, §20.5 invocation, §20.6 review) and Appendix A Architecture decision template (Status / Context / Current evidence / Options / Cloud-justification test / Decision / Consequences / Verification / Reversal-deprovision condition)
- Repository evidence:
  - `AGENTS.md:77` — § ADR conventions: stable IDs, YAML frontmatter, one decision per ADR, supersede by new ADR, never renumber; the reserved ADR-0100…ADR-0110 block is recorded there
  - `docs/adr/README.md:1-20` — the index preamble and the required frontmatter fields (`id`, `status`, `date`, `supersedes`, `superseded_by`, `related_capabilities`, `related_frd`, `tags`); the accepted table runs ADR-0001…ADR-0029 with 0017 never issued
  - `docs/adr/0025-integrate-renderer-and-extractor-into-the-application.md` — a recent accepted ADR to copy the shape from
  - `eng/skills/skills.lock.json` — the artefact this ADR governs (created by [[DSK-12-02]])
  - `scripts/Test-DocumentationLinks.ps1` — the CI `documentation` job link checker that a new ADR and index row must survive
  - `docs/agent/` does **not** exist — proposal §20.2 asks for `docs/agent/skill-routing.md`, but `scripts/Test-MarkdownPlacement.ps1:31` allows only `docs/(prd|frd|adr|design|desktop)`, so the routing document lives at `docs/desktop/12-agent-tooling/skill-routing.md`. That is a deviation the ADR must record.
- Binding decisions:
  - **ADR block** — the conversion uses the reserved ADR-0100…ADR-0110, never "next free number"; upstream keeps issuing ADRs and would collide.
  - **L-04** — subagents exist as `.codex/agents/*.toml` and every ticket names its subagent, skills and MCP tools; the ADR is where that becomes binding rather than planned.
  - **L-05** — the board is seeded from these plans.
- Depends on:
  - `DSK-12-02` — the ADR records the lockfile that must already exist with real hashes; an ADR describing a file that does not exist is a wish.
  - `DSK-00-05` — that ticket's title also claims ADR-0110. **Check first**: if `docs/adr/0110-*.md` already exists, this ticket verifies and completes it against the shipped lockfile instead of writing a second ADR.

## Routing

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml` (read-only; it checks the ADR against the shipped lockfile and the conventions)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-docs` (`.grok/skills/kanmer-docs/SKILL.md`) → `kanmer-plan`, `kanmer-execute` (`.grok/skills/<name>/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `link_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates: `leave-preparing` needs `plan` + `questions-resolved`; `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates <this ticket's board id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row and the plan sections named under **Source of truth**, then `get_doc_gates <this ticket's board id>` and `take_ticket`.
2. **Check before writing**: `ls docs/adr/0110-*.md`. If a file exists, [[DSK-00-05]] has already authored it — this ticket then verifies and completes that file (steps 5–10) and creates nothing new. ADR-0110 is also claimed by [[DSK-00-05]]; one filename, `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md`, and one rule — whichever of the two is worked first authors the file, the other verifies that it covers its content and extends it in place, never a second file for the same number. Record which path was taken in the first line of the plan document.
3. Read `AGENTS.md` § ADR conventions (`AGENTS.md:77`) and `docs/adr/README.md:1-20` for the frontmatter contract, then read `docs/adr/0025-integrate-renderer-and-extractor-into-the-application.md` as the shape reference.
4. Create `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md` with frontmatter: `id: ADR-0110`, `status: accepted`, `date: <today>`, `supersedes: []`, `superseded_by: []`, `related_capabilities: []`, `related_frd: []`, `tags: [agent-tooling, desktop-conversion]`. The id carries the `ADR-` prefix and `superseded_by` is an empty list, never `null`: the precedent is `docs/adr/0026-enable-automation-mcp-by-explicit-deployment-configuration.md:1-9`, whose frontmatter reads `id: ADR-0026` at line 2 and `superseded_by: []` at line 6, and every other ADR in `docs/adr/` matches it. Follow the exact key names used by the existing ADRs; do not invent a field.
5. Body sections, following proposal Appendix A: **Context** (proposal §20.1–20.2: skills are playbooks, mutable instructions make review and reproduction unreliable); **Current evidence** (the lockfile at `eng/skills/skills.lock.json`, the vendored destinations `.agents/skills/vendor/{dotnet,windows,azure}/`, the CI verifier step in `.github/workflows/ci.yml`); **Options** (fetch at execution time / vendor unpinned / vendor pinned by commit).
6. **Decision**: agents load skills only from the vendored destinations at the pinned commits, never from a moving branch. Record the three pins verbatim: `dotnet/skills` `98f848512e9ee4877e399a0ae367bb5e4a193144`, `microsoft/win-dev-skills` `f1028dd5bb19af59df400cb4a2ab867e40a40a4a` (v0.5.0), `microsoft/azure-skills` `1a03acfb9ac1a1a05518bf7420d4618cc41847be`. Record that a skill update is a reviewed PR that bumps the commit and re-runs the sync ([[DSK-12-10]]).
7. Include the invocation protocol as the seven numbered steps from `docs/desktop/12-agent-tooling/README.md` § 6, and the review protocol from proposal §20.6 (the reviewer loads the skills independently and verifies dependency boundaries, XAML/native implementation, async and UI-thread safety, accessibility, package and update implications, API and data compatibility, test evidence, cloud placement). Do not rewrite them into new words.
8. Answer the six-question cloud-justification table — copy the table from `docs/desktop/00-governance-and-workflow/README.md` § 3 and fill every row with yes/no plus evidence. All six answers are **no** for agent tooling (playbooks are local files read by a local toolchain), so the responsibility sits on the workstation and no Azure resource is involved. Six blank rows is not an answer.
9. **Consequences** must record two deviations honestly: (a) the routing document lives at `docs/desktop/12-agent-tooling/skill-routing.md`, not the proposal's `docs/agent/skill-routing.md`, because `scripts/Test-MarkdownPlacement.ps1:31` allows only `docs/(prd|frd|adr|design|desktop)` and `docs/agent/` would fail the CI `documentation` job; (b) the ADR uses the reserved ADR-0100…ADR-0110 block rather than the next free number, to avoid collision with upstream's still-active ADR series.
10. **Verification** and **Reversal/deprovision condition** sections: verification is `pwsh ./eng/skills/verify-skills.ps1` green locally and in the CI `changes` job; reversal is a superseding ADR, never an edit — published ADR bodies are immutable.
11. Add the index row to `docs/adr/README.md` under "Current architecture decisions (`status: accepted`)", in ADR-number order, matching the existing row format `| [0110](0110-….md) | Title | — |`.
12. Link the ADR to this ticket with `link_doc` so the governing-doc reference is real, and clear `docs_todo` once it is.
13. Run the documentation gates and record their output: `pwsh ./scripts/Test-DocumentationLinks.ps1` (expected `All relative Markdown links resolve`) and `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base with dev> -Head HEAD` (expected `Markdown placement passed` — `docs/adr/` is an allowed root).

## Acceptance criteria

- [ ] `docs/adr/0110-*.md` exists exactly once, with valid frontmatter matching the field set used by the existing ADRs and `status: accepted`.
- [ ] The three pinned commit SHAs appear verbatim in the decision.
- [ ] The six-question cloud-justification table is present with all six rows answered and evidence given.
- [ ] Both deviations (routing-document path, reserved ADR block) are recorded under Consequences.
- [ ] The invocation protocol (seven steps) and the review protocol are included, not paraphrased into something new.
- [ ] `docs/adr/README.md` carries the ADR-0110 row in number order.
- [ ] The ticket links the ADR with `link_doc` and no longer needs `docs_todo`.

## Verification

- [ ] `ls docs/adr/0110-*.md` — expected: exactly one file.
- [ ] `head -12 docs/adr/0110-*.md` — expected: YAML frontmatter with `id`, `status: accepted`, `date`, `supersedes`, `superseded_by`, `related_capabilities`, `related_frd`, `tags`.
- [ ] `grep -c '98f848512e9ee4877e399a0ae367bb5e4a193144\|f1028dd5bb19af59df400cb4a2ab867e40a40a4a\|1a03acfb9ac1a1a05518bf7420d4618cc41847be' docs/adr/0110-*.md` — expected: `3`.
- [ ] `grep -n '0110' docs/adr/README.md` — expected: one index row in the accepted table.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: `All relative Markdown links resolve (<n> files checked).`
- [ ] `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base> -Head HEAD` — expected: `Markdown placement passed for <base>..<head>.`

## Evidence tier

Tier 1 — Static/build/architecture. It obliges the link checker and placement gate passing and the frontmatter being valid; an ADR proves a decision was recorded, never that it is enforced — the enforcement evidence is [[DSK-12-03]]'s CI run.

## Documentation changes

- `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md` — new ADR.
- `docs/adr/README.md` — index row in the accepted table.
- `docs/index.md` — check whether a link is owed; the desktop plan-set row already exists, so probably `None.` Verify rather than assume.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may create `docs/adr/0110-*.md` and edit `docs/adr/README.md` (and `docs/index.md` if a link is genuinely owed). Must not edit any other ADR body — published bodies are immutable and a changed decision is a new superseding ADR. Must not touch `eng/skills/**`, `.codex/`, `src/` or `tests/`.
- **Traps**: never take "the next free number" — upstream keeps issuing ADRs and the one-way sync would collide; the reserved block is ADR-0100…ADR-0110. Check `docs/adr/README.md` after every upstream sync. [[DSK-00-05]] also claims ADR-0110, so step 2 is a real interlock against two agents writing two versions of the same ADR. Any new `.md` outside `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — docs-only`, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
