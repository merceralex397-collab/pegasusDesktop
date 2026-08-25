---
id: TOOL-001
type: ticket
title: DSK-12-01 · Verify Codex skill and agent discovery on the workstation
status: implementing
area: agent-tooling
assignee: codex-mcp-client
profile: spike
stageEntered:
  preparing: '2026-08-24T21:21:29.439Z'
taken_at: '2026-08-25T03:48:33.970Z'
branch: tool-001-verify-discovery
worktree: .worktrees/tool-001
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
blocks:
  - TOOL-002
  - TOOL-004
  - TOOL-006
docs_todo: true
archived: false
created: '2026-08-24T08:04:54.684Z'
updated: '2026-08-25T03:48:33.970Z'
---

## What

Record, from the Codex build actually installed on the conversion workstation, which directories it scans for skills and which agent definitions it loads, and write the verdict into the ticket's `research/` document. No repository file changes.

## Why

`docs/desktop/12-agent-tooling/README.md` § 2 records as an **assumption** that "the installed Codex build scans `.agents/skills` as documented; whether it also scans `.codex/skills` is unverified". Eight `winui-*` skills sit under `.codex/skills/` today and are **untracked**; the documented Codex scan root is `.agents/skills`. If Codex never reads `.codex/skills`, every agent that claimed to "load `winui-design`" has been running on nothing, and every ticket in this plan set that names a WinUI skill is unsupported. [[DSK-12-02]] cannot choose a vendor destination and [[DSK-12-04]] must not delete the duplicate tree until this is observed. Proposal §20.2 forbids fetching a moving upstream at execution time, which only works if discovery from the pinned local tree is proven.

## Source of truth

- Plan row: `docs/desktop/12-agent-tooling/README.md` § 5 — `DSK-12-01`
- Plan detail: `docs/desktop/12-agent-tooling/README.md` § 2 Evidence base (Codex platform facts, fetched 2026-08-23) and § 7 Risks and traps ("Discovery mismatch")
- Plan detail: `docs/desktop/12-agent-tooling/skill-routing.md` § Work type routing (the skill names whose discovery is being proven)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 20.2 Pinning and vendoring, § 20.3 Project-local Pegasus skill
- Repository evidence:
  - `.codex/config.toml:1-13` — `[features] apps = false`, `remote_plugin = false`; two MCP servers (`mcp_microsoftdocs`, `kanmer`); **no `[agents]` table**
  - `.codex/agents/` — eight TOMLs: `winui-dev.toml`, `pegasus-gateway-dev.toml`, `pegasus-parity-researcher.toml`, `pegasus-test-engineer.toml`, `pegasus-desktop-reviewer.toml`, `pegasus-release-packager.toml`, `pegasus-azure-auditor.toml`, `pegasus-ui-verifier.toml` (all tracked)
  - `.codex/skills/` — nine folders; only `.codex/skills/pegasus-release/SKILL.md` is tracked (`git ls-files .codex`), the eight `winui-*` folders are working-tree only (`git status --porcelain` shows them as `??`)
  - `.agents/skills/pegasus-release/SKILL.md` and `.agents/skills/project/pegasus-desktop/SKILL.md` — both tracked
  - `.codex/skills/winui-setup/SKILL.md:1-5` — frontmatter `name: winui-setup`, `disable-model-invocation: true` (upstream attribution)
  - `.grok/skills/.kanmer-skills-version` — `0.1.0` (the Kanmer skill install, machine-managed)
- Binding decisions:
  - **L-04** — every ticket names its subagent, skills and MCP tools; that naming is worthless if the named skill is not discoverable.
  - **L-05** — the board is seeded from these plans, so the plan's routing names must resolve on the real machine.
- Depends on: `None.`

## Routing

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml` (read-only; its final message is the deliverable and the ticket owner writes it down)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-research` (`.grok/skills/kanmer-research/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`). No Microsoft Learn call is useful here: the Codex documentation lives at `learn.chatgpt.com`, which `microsoft_docs_search` does not index — fetch those pages directly.
- **Kanmer pipeline** for profile `spike`: `kanmer-research` → `kanmer-closeout`. The only gate is `enter-done`: `research` + `questions-resolved`. Call `get_doc_gates <this ticket's board id>` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row and the plan sections named under **Source of truth**, then call `get_doc_gates <this ticket's board id>` (the `TOOL-0nn` id from `list_items`) and `take_ticket`.
2. Record the current on-disk state verbatim into `append_scratch`, so the verdict is anchored to observed facts:
   - `ls .codex/agents`, `ls .codex/skills`, `ls .agents/skills`, `ls .agents/skills/project`
   - `git ls-files .codex` and `git ls-files .agents`
   - `git status --porcelain | grep skills`
   Expected today: eight TOMLs; nine folders under `.codex/skills` of which only `pegasus-release/SKILL.md` is tracked; two tracked files under `.agents/skills`.
3. **Operator step** — in a Codex session opened at the repository root `C:\Users\PC\Documents\GitHub\pegasusDesktop`, run `codex --version` and hand back the exact output string.
4. **Operator step** — in the same session run `/skills` and hand back the **complete** listing, including the path each skill is discovered from. The question this answers is binary: do `winui-setup`, `winui-dev-workflow`, `winui-design`, `winui-code-review`, `winui-ui-testing`, `winui-packaging`, `winui-wpf-migration`, `winui-session-report` appear, and from which directory.
5. **Operator step** — run `/agent` and hand back the roster. Expected: the eight names in `.codex/agents/`. If the roster is empty or partial, that is the finding — `.codex/config.toml` has no `[agents]` table today ([[DSK-12-05]] adds it), so record whether the roster works without it.
6. Cross-check the listing three ways and write each answer as a sentence, not a shrug: (a) is each `winui-*` skill listed at all; (b) is `pegasus-release` listed **once or twice** (it exists at both `.codex/skills/pegasus-release/SKILL.md` and `.agents/skills/pegasus-release/SKILL.md`); (c) is the project skill `pegasus-desktop` listed from `.agents/skills/project/pegasus-desktop/`.
7. **Operator step** — probe one skill explicitly: mention `$winui-design` in the session and record whether Codex resolves it and from which file. A name that lists but does not resolve is a different failure from one that never lists.
8. Re-fetch the two Codex documentation pages and record the fetch date beside the observed behaviour: <https://learn.chatgpt.com/docs/build-skills> (skill discovery roots) and <https://learn.chatgpt.com/docs/agent-configuration/subagents> (custom-agent TOML fields, `[agents]` table). Use a direct web fetch; do **not** use `microsoft_docs_search`/`microsoft_docs_fetch`, which cover Microsoft Learn only. Note any field the installed build does not honour.
9. Repeat steps 4–6 for Claude Code, which also runs against this repository (`.mcp.json` carries the same Kanmer server; `.grok/skills/` is exposed as the `kanmer:` plugin): record which of the three trees (`.agents/skills`, `.codex/skills`, `.grok/skills`) each tool discovers. This is the input [[DSK-12-11]] needs and costs one command here.
10. Write the `research/` document with a verdict paragraph in this exact shape: *"Codex build `<version>` discovers skills from `<directories>` and does not discover `<directories>`; the agent roster is `<loaded | not loaded>` without an `[agents]` table."* Follow it with the consequence for [[DSK-12-02]] (vendor destination) and [[DSK-12-04]] (whether the `.codex/skills` copies can be deleted safely).
11. If Codex **does** scan `.codex/skills`, the one-list rule still stands (plan § 7: "Two copies of `pegasus-release` today; a third anywhere is a stop condition") — record that the move is still required and why, rather than reopening the decision.
12. Write the document with `set_ticket_doc`, tick every item in `open-questions/` (or park it below the literal `## Parked (explicitly deferred)` heading with a reason), then move to `done` after `get_doc_gates` confirms `research` and `questions-resolved` are satisfied.

## Acceptance criteria

- [ ] The `research/` document contains the verbatim output of `codex --version`, `/skills` and `/agent`, each labelled with the date it was captured.
- [ ] An explicit statement of which directories the installed Codex build scans for skills, and which it does not.
- [ ] An explicit statement of whether the agent roster loads without an `[agents]` table in `.codex/config.toml`.
- [ ] A recorded answer for `pegasus-release` appearing once or twice, and for `pegasus-desktop` being discovered.
- [ ] The Claude Code discovery answer recorded for the same three trees.
- [ ] The consequence for [[DSK-12-02]] and [[DSK-12-04]] stated in one sentence each.
- [ ] Fetch date recorded for both `learn.chatgpt.com` pages.

## Verification

- [ ] `get_ticket_doc <this ticket's board id>` — expected: a `research` document whose body contains the three captured outputs and the verdict paragraph.
- [ ] `git status --porcelain` — expected: no change to any tracked repository file from this ticket (a spike that edits the tree has exceeded its scope).
- [ ] `get_doc_gates <this ticket's board id>` before the move to `done` — expected: `research` and `questions-resolved` both satisfied.

## Evidence tier

Tier 1 — Static/build/architecture. It obliges recorded tool output and configuration facts only; nothing is compiled or deployed, and no claim beyond "this is what the installed toolchain does" may be made from it.

## Documentation changes

- `docs/desktop/12-agent-tooling/README.md` § 2 Assumptions — the first assumption bullet becomes a recorded fact once this spike lands. Make that edit only if the verdict contradicts the assumption; otherwise `None.`

## Guardrails

- **Azure**: no write. No Azure tool is called by this ticket at all.
- **Scope boundary**: may write Kanmer documents only. Must not create, move or delete anything under `.codex/`, `.agents/`, `.grok/`, `src/`, `tests/` or `eng/` — the moves belong to [[DSK-12-02]] and [[DSK-12-04]].
- **Traps**: skills under `.codex/skills/` may simply not be found (plan § 7 "Discovery mismatch"); `winui-session-report` reads session transcripts and carries a privacy warning — do not run it to "check discovery", listing it is enough; read-only agents cannot write, so the caller must transcribe `pegasus-desktop-reviewer`'s findings into the ticket.
- **Open question to carry**: if the installed build honours neither `model_reasoning_effort` nor `sandbox_mode`, the read-only guarantee for `pegasus-parity-researcher`, `pegasus-desktop-reviewer` and `pegasus-azure-auditor` rests on the agent text alone — record it as an open question for [[DSK-12-05]] rather than assuming enforcement.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — docs-only` (this spike produces no branch diff).

## Outcome

_Filled at closeout._
