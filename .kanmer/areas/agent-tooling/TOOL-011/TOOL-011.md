---
id: TOOL-011
type: ticket
title: >-
  DSK-12-11 · Decide and record Claude Code parity for the agent roster
  (`.claude/agents` mirror or not needed)
status: preparing
area: agent-tooling
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:21:30.533Z'
labels:
  - desktop-conversion
  - plan-12
  - phase-0
  - tier-1
  - needs-operator
groups:
  - EPIC-013
  - HZN-001
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:12:34.603Z'
updated: '2026-08-24T21:21:30.533Z'
---

## What

Decide whether the eight Codex subagents need an equivalent roster for Claude Code, and record the answer with its date: either mirror them under `.claude/agents/` with equivalent tool restrictions and a stated single source of truth, or record that it is not needed.

## Why

This repository is driven by both tools — `.codex/config.toml` wires the Codex MCP servers and `.mcp.json` wires the same Kanmer server for Claude Code, and the Kanmer skills under `.grok/skills/` are exposed to Claude Code as the `kanmer:` plugin. If a session in the second tool has no roster, its work silently bypasses L-04: no named subagent, no sandbox boundary, and in particular no read-only guarantee for the reviewer and the Azure auditor. The plan marks the mirror **optional**, so the deliverable is a recorded decision rather than an assumed one — an undecided item here is exactly the sort of thing that gets re-litigated in every later ticket.

## Source of truth

- Plan row: `docs/desktop/12-agent-tooling/README.md` § 5 — `DSK-12-11` ("If the team also runs Claude Code, the same roster exists with equivalent tool restrictions; otherwise recorded as not needed")
- Plan detail: `docs/desktop/12-agent-tooling/subagents.md` — § Roster (the eight agents with sandbox and load-first skills) and the opening sentence that `.codex/agents/` is the source of truth if the document and the files ever differ
- Plan detail: `docs/desktop/12-agent-tooling/README.md` § 2 (Codex platform facts) and § 7 ("Read-only agents cannot write; callers must capture their output into the ticket or the plan")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 20.3 Project-local Pegasus skill, § 20.6 Review protocol
- Repository evidence:
  - `.gitignore:23` — `/.claude/` is ignored, so anything written under `.claude/agents/` is **untracked** and would not survive a fresh clone. This is the blocking fact for any mirror.
  - `.claude/` — contains only `settings.local.json` today
  - `.mcp.json` — the Claude Code MCP configuration; carries the Kanmer stdio server and nothing else
  - `.grok/skills/` — twelve Kanmer skills, tracked, also surfaced to Claude Code as the `kanmer:` plugin
  - `.codex/agents/*.toml` — the eight authoritative agent definitions with `sandbox_mode` and `model_reasoning_effort`
  - `.agents/skills/project/pegasus-desktop/SKILL.md` — the project skill both tools can read; `.agents/skills` is a documented Codex scan root
- Binding decisions:
  - **L-04** (locked) — specialist subagents exist as `.codex/agents/*.toml` and every ticket names its subagent, skills and MCP tools. A second tool without a roster is a gap in that lock, which is what this ticket closes or explicitly accepts.
  - **L-05** — the board is seeded from these plans.
- Depends on: `DSK-12-05` — the Codex roster must be reconciled and enabled first; mirroring a roster that is itself unverified copies the drift. [[DSK-12-01]] step 9 already records which trees each tool discovers, and is the input to step 3 here.

## Routing

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml` (read-only; it audits any mirrored roster against the Codex originals)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-plan`, `kanmer-execute` (`.grok/skills/<name>/SKILL.md`). Do **not** load `create-custom-agent`: it is on the do-not-load table in `docs/desktop/12-agent-tooling/skill-routing.md` because it targets the VS Code `.agent.md` format, which is neither Codex TOML nor the Claude Code format.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates: `leave-preparing` needs `plan` + `questions-resolved`; `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates <this ticket's board id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row and the plan sections named under **Source of truth**, then `get_doc_gates <this ticket's board id>` and `take_ticket`.
2. **Operator step** — ask the operator directly: is Claude Code used on this repository for conversion work alongside Codex, and is it expected to be after cutover? Record the answer verbatim with its date. If the answer is no, jump to step 9 and record "not needed" — that is a complete and acceptable outcome for this row.
3. Read [[DSK-12-01]]'s research verdict for which skill trees each tool discovers (`.agents/skills`, `.codex/skills`, `.grok/skills`). A mirror is only worth building if Claude Code can reach the project skill and the vendored skills the roster names.
4. Establish the target format before writing a single file. Codex agents are TOML under `.codex/agents/`; Claude Code agents are a different format under `.claude/agents/`. Check the installed Claude Code's own documentation or its agent listing for the current frontmatter keys and the tool-restriction field — **do not guess key names**; an agent file with invented keys either fails to load or loads without its restrictions, which is worse.
5. Record the blocking fact and **decide and record** what to do about it: `.gitignore:23` ignores `/.claude/`, so a mirrored roster is untracked, invisible in review and absent from a fresh clone. The options are (a) add a narrow negation such as `!/.claude/agents/` so the roster is tracked while local settings stay ignored, (b) keep the mirror untracked and generated on demand from the TOMLs, or (c) do not mirror. Write the choice and its reason in the plan document.
6. If mirroring: create one file per agent carrying the same `description` and the same developer instructions, and map the sandbox intent to the target tool's mechanism — Codex expresses it as `sandbox_mode`, so `pegasus-parity-researcher`, `pegasus-desktop-reviewer` and `pegasus-azure-auditor` must end up with a read-only tool set, and the other five with write access scoped as `subagents.md` § Roster describes.
7. If mirroring: every mirrored agent keeps step `0.` — load `.agents/skills/project/pegasus-desktop/SKILL.md` first — and keeps its "never delegate to an agent of your own kind" sentence. Both rules are what make the roster safe, not decoration.
8. Keep one source of truth. `docs/desktop/12-agent-tooling/subagents.md` says `.codex/agents/` is authoritative if the files and the document differ; extend that sentence to say whether the Claude Code roster is generated from those TOMLs or maintained in parallel, and if in parallel, that a change must be applied to both in the same PR.
9. Record the decision, either way, in `docs/desktop/12-agent-tooling/subagents.md` with a date — "mirrored on <date>, source of truth `.codex/agents/`, tracked via `<gitignore rule>`" or "not needed on <date>: Claude Code is not used for conversion work". An undated "optional" line is not a decision.
10. If a mirror was built, have `pegasus-desktop-reviewer` compare each mirrored file against its TOML original and report any divergence in description, instructions or restriction; transcribe the findings — it cannot write.
11. Verify: either both tools list the roster (attach both listings), or the dated "not needed" line exists. Then run `pwsh ./scripts/Test-DocumentationLinks.ps1` and, if a `.md` was added anywhere, `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base with dev> -Head HEAD`.
12. Record the Appendix C evidence: the operator answer, the format check from step 4, the gitignore decision, and the roster listings or the not-needed line.

## Acceptance criteria

- [ ] The operator's answer about Claude Code usage is recorded verbatim with its date.
- [ ] The `.gitignore` consequence for `/.claude/` is recorded, with the chosen option and its reason.
- [ ] Either: eight mirrored agent files exist in the correct installed format, with read-only restrictions on `pegasus-parity-researcher`, `pegasus-desktop-reviewer` and `pegasus-azure-auditor`, each loading the project skill first — or: a dated "not needed" line exists in `docs/desktop/12-agent-tooling/subagents.md`.
- [ ] The single source of truth for the roster is stated, including how a change reaches both tools if two copies exist.
- [ ] No agent file was written with guessed frontmatter keys.

## Verification

- [ ] `grep -n 'Claude Code' docs/desktop/12-agent-tooling/subagents.md` — expected: a dated decision line, whichever way it went.
- [ ] If mirrored: `ls .claude/agents` — expected: eight files whose names match `ls .codex/agents` one for one.
- [ ] If mirrored: `git check-ignore -v .claude/agents/<one file>` — expected: no output if option (a) was chosen and the negation works; a `.gitignore:23` match if the roster is deliberately untracked.
- [ ] If mirrored: the recorded roster listing from each tool — expected: the same eight names in both.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: `All relative Markdown links resolve (<n> files checked).`

## Evidence tier

Tier 1 — Static/build/architecture. It obliges a recorded decision plus, if a mirror was built, both roster listings and the tracked/untracked status; it makes no claim that either roster behaves correctly, which is [[DSK-12-09]]'s territory.

## Documentation changes

- `docs/desktop/12-agent-tooling/subagents.md` — the dated decision and, if mirrored, the source-of-truth sentence covering both tools.
- `.gitignore` — only if step 5 chose the narrow negation; keep the change to a single line so local Claude settings stay ignored.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may create `.claude/agents/**` (if mirroring), edit `.gitignore` by one line, and edit `docs/desktop/12-agent-tooling/subagents.md`. Must not change any `.codex/agents/*.toml`, `.codex/config.toml`, `.mcp.json`, `eng/skills/**`, `src/` or `tests/`.
- **Traps**: `/.claude/` is gitignored, so an unreviewed mirror is invisible to review and absent from a clone — step 5 exists so that is a decision, not an accident. Guessing the target tool's agent-file schema produces a file that loads without its restrictions; check the installed documentation instead. Two rosters is two lists for one concept — the source-of-truth sentence in step 8 is what keeps that from becoming a stop condition. `create-custom-agent` is on the do-not-load table and does not describe either format.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, or `n/a — docs-only` if the outcome is the dated "not needed" line; recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
