---
id: TOOL-005
type: ticket
title: >-
  DSK-12-05 · Reconcile the `pegasus-desktop` project skill and add the
  `[agents]` table to `.codex/config.toml`
status: implementing
area: agent-tooling
assignee: codex-mcp-client
profile: chore
stageEntered:
  preparing: '2026-08-24T21:21:29.823Z'
taken_at: '2026-08-25T04:25:39.628Z'
branch: tool-005-reconcile-agents
worktree: .worktrees/tool-005
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
  - TOOL-009
  - TOOL-011
docs_todo: true
archived: false
created: '2026-08-24T08:07:44.152Z'
updated: '2026-08-25T04:25:39.629Z'
---

## What

Verify and reconcile the already-present project skill and the eight already-present agent TOMLs against `docs/desktop/12-agent-tooling/subagents.md`, then add the missing `[agents]` table to `.codex/config.toml` so the roster is actually enabled and bounded.

## Why

Proposal §20.3 makes the project skill the routing entry point that states the decisions overriding upstream guidance, and L-04 requires every ticket to name its subagent. Both halves already exist on disk — `.agents/skills/project/pegasus-desktop/SKILL.md` is tracked, and all eight `.codex/agents/*.toml` files are tracked — but `.codex/config.toml` has **no `[agents]` table**, which is the switch (`enabled`, `max_concurrent_threads_per_session`, `default_subagent_reasoning_effort`, `interrupt_message`) the plan specifies. This is therefore a verify-and-reconcile ticket, not a create ticket: creating a second project skill or a ninth agent would break the one-list rule. [[DSK-12-09]] cannot run a dry run until the roster is enabled and its behaviour observed.

## Source of truth

- Plan row: `docs/desktop/12-agent-tooling/README.md` § 5 — `DSK-12-05`
- Plan detail: `docs/desktop/12-agent-tooling/subagents.md` — § Roster (the eight agents, their one job, sandbox, effort and load-first skills), § `.codex/config.toml` additions (the exact TOML to add), and one section per agent with the file as written
- Plan detail: `docs/desktop/12-agent-tooling/README.md` § 3 ("`.codex/config.toml` gains an `[agents]` table and, once verified, a disabled-by-default Azure MCP server entry (DSK-12-06)")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 20.3 Project-local Pegasus skill, § 20.5 Invocation protocol, § 20.6 Review protocol
- Repository evidence:
  - `.agents/skills/project/pegasus-desktop/SKILL.md` — **already exists** and tracked; frontmatter `name: pegasus-desktop`; sections: Locked decisions, Dependency boundaries, UI and accessibility conventions, Invocation protocol, Evidence format (Appendix C), Next skill to load
  - `.codex/agents/` — **already exists**: `winui-dev.toml`, `pegasus-gateway-dev.toml`, `pegasus-parity-researcher.toml`, `pegasus-test-engineer.toml`, `pegasus-desktop-reviewer.toml`, `pegasus-release-packager.toml`, `pegasus-azure-auditor.toml`, `pegasus-ui-verifier.toml`; each `developer_instructions` opens with step `0.` loading the project skill
  - `.codex/config.toml:1-13` — `[features]`, `[mcp_servers.mcp_microsoftdocs]`, `[mcp_servers.kanmer]`, `[mcp_servers.kanmer.env]`. **No `[agents]` table** — this is the gap.
  - `.codex/config.toml` also carries an uncommitted machine-local edit to `[mcp_servers.kanmer]` (absolute `C:\Users\PC\...` paths). It is not part of this change.
- Binding decisions:
  - **L-04** (locked) — specialist Codex subagents exist as `.codex/agents/*.toml`; every ticket names its subagent, skills and MCP tools.
  - **L-01, L-02, L-03, D-001, D-002, D-003, C-01** — the project skill restates them; step 3 checks that restatement is still true against `docs/desktop/README.md` § Locked decisions.
- Depends on: `None.` (The plan row has no dependency; [[DSK-12-01]] informs it but does not block it.)

## Routing

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml` (read-only; it audits the roster and the skill text, and the ticket owner makes the edits)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-plan`, `kanmer-execute` (`.grok/skills/<name>/SKILL.md`). Do **not** load `create-custom-agent`: `docs/desktop/12-agent-tooling/skill-routing.md` § Not applicable rules it out because it targets the VS Code `.agent.md` format, not Codex TOML.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates: `leave-preparing` needs `plan` + `questions-resolved`; `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates <this ticket's board id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row and the plan sections named under **Source of truth**, then `get_doc_gates <this ticket's board id>` and `take_ticket`.
2. **Verify and reconcile, do not create.** Confirm the project skill exists: `test -f .agents/skills/project/pegasus-desktop/SKILL.md`. Read it end to end. Nothing in this ticket may create a second project skill.
3. Reconcile its Locked-decisions section against `docs/desktop/README.md` § Locked decisions and open decisions: L-01 (gateway is `Pegasus.Web` evolved in place), L-02 (local Test/UAT; ADR-0014 stands), L-03 (WebView2 HTML→PDF; ADR-0108), L-04, L-05, D-001 (fork becomes the single release source), D-002 (self-managed certificate trusted per workstation in `LocalMachine\TrustedPeople`), D-003 (UNC file share over SMB), C-01 (repositories become private). Every one must be present and stated the same way; fix any drift in the skill, not in the plan.
4. Verify the roster: `ls .codex/agents` must return the eight TOMLs named under **Source of truth**. If one is missing, restore it from the corresponding section of `docs/desktop/12-agent-tooling/subagents.md`, which carries each file verbatim.
5. Verify every agent loads the project skill first: `grep -c 'pegasus-desktop' .codex/agents/*.toml` — each file must return at least 1, and the reference must be the step `0.` line naming `.agents/skills/project/pegasus-desktop/SKILL.md`. Add the line to any TOML that lacks it, copying the wording from `winui-dev.toml`.
6. Verify the sandbox and effort fields match `subagents.md` § Roster: `sandbox_mode = "read-only"` on `pegasus-parity-researcher`, `pegasus-desktop-reviewer` and `pegasus-azure-auditor`; `sandbox_mode = "workspace-write"` on `pegasus-gateway-dev`, `pegasus-test-engineer`, `pegasus-release-packager` and `pegasus-ui-verifier`; `winui-dev` inherits the upstream default and sets no override. Models are deliberately not hardcoded — do not add a `model` key.
7. Add the `[agents]` table to `.codex/config.toml`, exactly as `docs/desktop/12-agent-tooling/subagents.md` § `.codex/config.toml` additions gives it:

   ```toml
   [agents]
   enabled = true
   max_concurrent_threads_per_session = 4
   default_subagent_reasoning_effort = "medium"
   interrupt_message = true
   ```

   Leave the commented Azure MCP block from that same section in place as a comment; enabling it is [[DSK-12-06]]'s work and must not happen here.
8. Stage only that hunk. `.codex/config.toml` carries an uncommitted machine-local edit to `[mcp_servers.kanmer]` (absolute `C:\Users\PC\...` paths); use `git add -p .codex/config.toml` and commit the `[agents]` hunk alone so no workstation path is pushed.
9. Parse-check every file that was touched, so a syntax error is caught before the roster silently fails to load. For each of the eight TOMLs and for `.codex/config.toml`:

   ```
   python -c "import tomllib, sys; tomllib.load(open(sys.argv[1], 'rb'))" <file>
   ```

   Expected: no output and exit code 0 for all nine files.
10. **Operator step** — restart Codex at the repository root and run `/agent`; hand back the roster listing. Expected: the eight names appear. Record whether the roster differs from before the `[agents]` table was added — [[DSK-12-01]] captured the "before" state.
11. Record which optional fields the installed build actually honours. If it ignores `model_reasoning_effort` or `sandbox_mode`, write that down: the read-only guarantee for the three inspection agents then rests on the agent text alone, and that becomes an open question rather than an assumption.
12. Record the Appendix C evidence: the reconciliation diff (what drifted and what was fixed), the nine parse checks, the `/agent` output, and the honoured-fields finding.

## Acceptance criteria

- [ ] `.agents/skills/project/pegasus-desktop/SKILL.md` exists once and its Locked-decisions section matches `docs/desktop/README.md` § Locked decisions, including D-001, D-002, D-003 and C-01.
- [ ] All eight `.codex/agents/*.toml` exist and each names `.agents/skills/project/pegasus-desktop/SKILL.md` as its step `0.`.
- [ ] Sandbox modes match `subagents.md` § Roster; no `model` key was hardcoded.
- [ ] `.codex/config.toml` contains the `[agents]` table with the four keys above and no enabled Azure MCP entry.
- [ ] No machine-local `[mcp_servers.kanmer]` path change was committed.
- [ ] All nine TOML files parse.
- [ ] `/agent` in a fresh Codex session lists the eight agents (operator evidence attached).

## Verification

- [ ] `python -c "import tomllib, sys; tomllib.load(open(sys.argv[1], 'rb'))" .codex/config.toml` — expected: exit 0, no output. Repeat for each of the eight agent TOMLs.
- [ ] `grep -n '^\[agents\]' -A 5 .codex/config.toml` — expected: the four keys `enabled`, `max_concurrent_threads_per_session`, `default_subagent_reasoning_effort`, `interrupt_message`.
- [ ] `grep -L 'pegasus-desktop' .codex/agents/*.toml` — expected: no output (every TOML mentions the project skill).
- [ ] `git diff --stat origin/dev...HEAD -- .codex/config.toml` — expected: only the `[agents]` addition, no `mcp_servers.kanmer` lines.
- [ ] The recorded `/agent` output — expected: the eight roster names.

## Evidence tier

Tier 1 — Static/build/architecture. It obliges parse-check output and a tool listing showing the roster loads; it does not prove any agent behaves correctly, which is [[DSK-12-09]]'s job.

## Documentation changes

- `docs/desktop/12-agent-tooling/subagents.md` — only if step 6 finds drift between a TOML on disk and the block printed there; `.codex/agents/` is the source of truth if the two differ, so the document is corrected, not the file.
- `AGENTS.md` — the sentence that agents load the project skill first is listed in plan § 8 as an area-12 documentation change. Add it **outside** the `<!-- kanmer:instructions -->` managed block, which `kanmer-setup` overwrites.

## Guardrails

- **Azure**: no write. The commented Azure MCP block stays commented; enabling it is [[DSK-12-06]].
- **Scope boundary**: may edit `.codex/config.toml`, the eight `.codex/agents/*.toml`, `.agents/skills/project/pegasus-desktop/SKILL.md` and the two documents named above. Must not create a new agent, a new project skill, or touch `eng/skills/**`, `src/` or `tests/`.
- **Traps**: a step that creates something already present is a defect — the project skill and all eight TOMLs exist today, so this ticket reconciles them. Codex self-hop: every TOML must keep its "never delegate to another agent of your own kind" sentence. Read-only agents cannot write, so `pegasus-desktop-reviewer`'s audit must be transcribed by the ticket owner. The `AGENTS.md` Kanmer block is machine-managed — never edit inside it.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
