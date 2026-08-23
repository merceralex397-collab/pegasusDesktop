# 12 · Agent tooling — skills, lockfile, subagents, MCP, invocation protocol

## 1. Purpose and proposal coverage

This area makes the implementing agents reproducible: which upstream skills
are used, at which pinned revision, from which directory; which specialist
subagents exist and what each may do; which MCP servers are wired; and the
protocol every ticket follows before, during, and after implementation.

Proposal coverage: §20 (Integrating `dotnet/skills` and
`microsoft/win-dev-skills`) in full — §20.1 purpose, §20.2 pinning and
vendoring, §20.3 project-local skill, §20.4 routing by work type, §20.5
invocation protocol, §20.6 review protocol; §25 ticket metadata
(`agent_skills:` block); §26 agent-development documentation set;
Appendix C agent implementation evidence; Appendix D research basis (pins);
ADR-0110 agent-skill pinning and invocation.

Companion files in this folder:

- [`subagents.md`](subagents.md): the eight Codex subagents, one section each,
  with the TOML as written under `.codex/agents/`.
- [`skill-routing.md`](skill-routing.md): §20.4 resolved to exact skill names
  and paths, a per-area routing index, and the not-applicable list.
- [`skills.lock.draft.json`](skills.lock.draft.json): the draft lockfile
  (sources, commits, 35 skills, destinations) to be promoted to
  `eng/skills/skills.lock.json` by the sync script.
- Project skill: `.agents/skills/project/pegasus-desktop/SKILL.md` (the
  routing entry point every agent loads first).

## 2. Evidence base

### Facts

Repository (verified 2026-08-23):

- `.codex/config.toml` declares two MCP servers only: `mcp_microsoftdocs`
  (`https://learn.microsoft.com/api/mcp`) and `kanmer` (stdio, Electron as
  node, rooted at `.worktrees/kanmer`); `[features] apps = false`,
  `remote_plugin = false`; no `[agents]` table.
- `.codex/agents/winui-dev.toml` was the only custom agent (fields `name`,
  `description`, `developer_instructions`). This task added seven siblings and
  one line to `winui-dev.toml` (load the project skill first).
- Eight `winui-*` skills sit under `.codex/skills/` (untracked), vendored from
  `microsoft/win-dev-skills` v0.5.0 at commit
  `f1028dd5bb19af59df400cb4a2ab867e40a40a4a` (2026-07-22); attribution in
  `.codex/skills/winui-setup/SKILL.md:3` and
  `.codex/skills/winui-session-report/Analyze-Session.ps1:190`. Bundled
  tools: `winui-design/winui-search.exe`, `winui-dev-workflow/BuildAndRun.ps1`
  plus `analyzer/Microsoft.WindowsAppSDK.Analyzers.dll`,
  `winui-session-report/Analyze-Session.ps1`; `winui-packaging` references
  `references/sourcegen-patterns.md`, which is present in the vendored copy.
- `.agents/skills/pegasus-release/SKILL.md` is byte-identical to
  `.codex/skills/pegasus-release/SKILL.md` (13,299 bytes).
- Root `skills-lock.json` (`version: 1`) pins four `mattpocock/skills` entries
  with `source`, `sourceType`, `skillPath`, `computedHash`; the skill bodies
  are not in the tree.
- Kanmer skills (12) live under `.grok/skills/` (`.kanmer-skills-version`
  0.1.0) and are also exposed to Claude Code as the `kanmer:` plugin; the
  Claude Code `.mcp.json` carries the same Kanmer server entry.
- `docs/index.md` now links this plan set; AGENTS.md § New Markdown placement
  names `docs/desktop/` as the planning exception.

Codex platform (fetched 2026-08-23):

- Custom agents: TOML files in `.codex/agents/` (project) or
  `~/.codex/agents/` (personal); required `name`, `description`,
  `developer_instructions`; optional `model`, `model_reasoning_effort`
  (`ultra`/`max`/`xhigh`/`high`/`medium`/`low`), `sandbox_mode`
  (`read-only`/`workspace-write`), `mcp_servers`, `skills.config`; global
  `[agents]` table (`enabled`, `max_concurrent_threads_per_session`,
  `default_subagent_model`, `default_subagent_reasoning_effort`,
  `interrupt_message`); built-ins `default`, `worker`, `explorer`; invocation
  by direct request, instructions, or AGENTS.md; `/agent` switches threads.
  Source: https://learn.chatgpt.com/docs/agent-configuration/subagents.
- Skills discovery: `$CWD/.agents/skills` and parents up to the repository
  root, `$HOME/.agents/skills`, `/etc/codex/skills`, built-ins; `SKILL.md`
  frontmatter `name` + `description`; disable with `[[skills.config]]`
  (`path`, `enabled = false`) in `~/.codex/config.toml`; invoke with a
  `$skill` mention, implicitly, or via `/skills`.
  Source: https://learn.chatgpt.com/docs/build-skills.

Upstream skill repositories (trees listed 2026-08-23):

- `dotnet/skills` at `98f848512e9ee4877e399a0ae367bb5e4a193144` (2026-08-21):
  106 `SKILL.md` across 16 plugins (`dotnet`, `dotnet-advanced`, `dotnet-ai`,
  `dotnet-aspnetcore`, `dotnet-blazor`, `dotnet-data`, `dotnet-diag`,
  `dotnet-experimental`, `dotnet-maui`, `dotnet-msbuild`, `dotnet-nuget`,
  `dotnet-template-engine`, `dotnet-test`, `dotnet-test-migration`,
  `dotnet-upgrade`, `dotnet11`) plus `.agents/skills/` (for example
  `authoring-github-workflows`, `create-custom-agent`); its agents are VS Code
  `.agent.md` files, not Codex TOML.
- `microsoft/win-dev-skills` at `f1028dd5` (v0.5.0): `plugins/winui/skills/
  <name>/SKILL.md` (eight skills) and `plugins/winui/agents/winui-dev.agent.md`;
  README marks the project 0.x preview with breaking changes possible.
- `microsoft/azure-skills` at `1a03acfb9ac1a1a05518bf7420d4618cc41847be`
  (2026-08-21): `skills/<name>/SKILL.md` (for example `azure-resource-lookup`,
  `azure-resource-visualizer`, `azure-cost`, `azure-diagnostics`,
  `azure-compliance`, `azure-validate`, `azure-storage`,
  `appinsights-instrumentation`) and an `.mcp.json` for the Azure MCP server.

### Assumptions

- The installed Codex build scans `.agents/skills` as documented; whether it
  also scans `.codex/skills` is unverified (ticket DSK-12-01 checks with
  `codex --version` and `/skills`).
- The Azure MCP server command in azure-skills `.mcp.json` works unchanged on
  the Windows workstations (verified in DSK-12-06 before enabling).
- The weaker AI can install the Kanmer MCP and Microsoft Learn MCP as today
  (`.codex/config.toml` is already wired for both).

## 3. Decisions and assumptions

- Canonical vendored tree: `.agents/skills/vendor/{dotnet,windows,azure}/
  <skill>/` plus `.agents/skills/project/pegasus-desktop/` (proposal §20.2).
  Deviation: today the WinUI skills live under `.codex/skills/`; the move is
  conditional on DSK-12-01 confirming Codex discovery. Until then both trees
  may coexist; after the move, `.codex/skills/winui-*` and the duplicate
  `.codex/skills/pegasus-release` are removed so there is one list.
- Lockfile `eng/skills/skills.lock.json` (schema `version: 2`, drafted here as
  [`skills.lock.draft.json`](skills.lock.draft.json)) records source
  repository, commit SHA, skill path, local destination, content hash, date
  reviewed, owner, and reason; `eng/skills/sync-skills.ps1` copies from the
  pinned commit and writes hashes; `eng/skills/verify-skills.ps1` recomputes
  hashes and fails on drift; CI runs the verifier in the `changes` job of
  `.github/workflows/ci.yml` (proposal §21.2 step 2). The root
  `skills-lock.json` (mattpocock) is left as is or merged into the new file in
  DSK-12-02.
- Azure skills are vendored too, for read-only usage only; `azure-deploy`,
  `azure-prepare`, and `azure-app-onboard` are not vendored (see the
  not-applicable list in `skill-routing.md`).
- Agents never fetch a moving `main`; a skill update is a reviewed PR that
  bumps the commit in the lockfile and re-runs the sync script.
- Subagents: eight Codex agents (`subagents.md`), models not hardcoded,
  `sandbox_mode = "read-only"` for researcher, reviewer, and auditor; the
  reviewer must not be the implementer (AGENTS.md task workflow step 5).
- `.codex/config.toml` gains an `[agents]` table and, once verified, a
  disabled-by-default Azure MCP server entry (DSK-12-06); no Azure writes are
  enabled by any agent.
- Kanmer: every ticket plan carries a `## Routing` block; the
  post-implementation report uses the Appendix C shape; `get_doc_gates` is
  authoritative over `board.yml`.
- No Azure writes in this area.

## 4. Target state and exit gate

Target: an agent opening any desktop-conversion ticket loads the project
skill, finds the exact pinned upstream skills by name, delegates to the right
subagent with the right sandbox, and leaves Appendix C evidence; CI proves
the vendored skills match the lockfile.

Exit gate:

- `codex --version` and `/skills` listing recorded; vendored tree discovered
  by Codex (DSK-12-01).
- `eng/skills/skills.lock.json` committed with real hashes; `verify-skills.ps1`
  green locally and in CI (DSK-12-02, DSK-12-03).
- All eight agent TOMLs parse (`python -c "import tomllib, sys;
  tomllib.load(open(sys.argv[1], 'rb'))" <file>`) and appear in Codex.
- Project skill present and referenced by every agent TOML.
- First ticket executed end to end with the protocol and reviewed by
  `pegasus-desktop-reviewer` (DSK-12-09).

## 5. Work breakdown

| ID | Title | Profile | Depends on | Acceptance | Verification | Tier | Routing |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DSK-12-01 | Verify Codex skill and agent discovery on the workstation | spike | none | Recorded `codex --version`, `/skills` output, `/agent` roster; statement of which directories are scanned | Command output attached as research doc | 1 | `pegasus-desktop-reviewer` · `pegasus-desktop` · Kanmer |
| DSK-12-02 | Vendor pinned skills and write the lockfile | chore | DSK-12-01 | `.agents/skills/vendor/{dotnet,windows,azure}` populated from the pinned commits; `eng/skills/skills.lock.json` with real hashes; `sync-skills.ps1` idempotent | `pwsh eng/skills/sync-skills.ps1 -Verify`; diff shows only vendored files | 1 | `pegasus-release-packager` · `directory-build-organization`, `authoring-github-workflows` · Microsoft Learn |
| DSK-12-03 | Add the lockfile hash check to CI | chore | DSK-12-02 | `verify-skills.ps1` runs in the `changes` job; a mutated vendored file fails the job | PR with a deliberate drift shows a red check, then reverted | 1 | `pegasus-release-packager` · `authoring-github-workflows` · none |
| DSK-12-04 | Remove duplicate skill copies under `.codex/skills/` | chore | DSK-12-01, DSK-12-02 | One tree only; `pegasus-release` kept once under `.agents/skills/`; agents still load every skill by name | `/skills` shows each skill once | 1 | `pegasus-release-packager` · none · Kanmer |
| DSK-12-05 | Adopt the project skill `pegasus-desktop` and the `[agents]` table | chore | none | Project skill committed; `.codex/config.toml` `[agents]` table present; every TOML loads the skill first | Codex session shows the skill; TOML parse check | 1 | `pegasus-desktop-reviewer` · `pegasus-desktop` · Kanmer |
| DSK-12-06 | Wire the Azure MCP server for read-only agents | chore | DSK-12-01 | `.codex/config.toml` carries the azure-skills MCP entry (disabled until verified); `pegasus-azure-auditor` can list `rg-pegasus-prod` read-only | `group_resource_list` output recorded; no write tool used | 1 | `pegasus-azure-auditor` · `azure-resource-lookup` · Azure MCP (read) |
| DSK-12-07 | Ticket template: `## Routing` block and Appendix C report shape in Kanmer docs | chore | area 00 board setup | `kanmer-tickets` template carries Routing; post-implementation-report doc follows Appendix C | Two tickets created with the template | 1 | `pegasus-desktop-reviewer` · `kanmer-tickets`, `kanmer-docs` · Kanmer |
| DSK-12-08 | ADR-0110 agent-skill pinning and invocation | chore | DSK-12-02 | ADR accepted; links lockfile, routing, protocol | `docs/adr/README.md` row; link checker green | 1 | `pegasus-desktop-reviewer` · `kanmer-docs` · Kanmer |
| DSK-12-09 | Dry run: one foundation ticket end to end with subagents | spike | DSK-12-05, area 02 first ticket | Implementer agent, test agent, and reviewer each leave Appendix C evidence; gaps in instructions fixed | Ticket proof + reviewer verdict recorded | 1 | `winui-dev`, `pegasus-test-engineer`, `pegasus-desktop-reviewer` · `pegasus-desktop` · Kanmer |
| DSK-12-10 | Skill update procedure and review checklist | chore | DSK-12-02 | A documented PR recipe to bump a pinned commit (diff review, re-sync, re-verify) and the §20.6 review checklist in the Kanmer review template | One rehearsed bump PR | 1 | `pegasus-release-packager` · `authoring-github-workflows` · Kanmer |
| DSK-12-11 | Claude Code parity: mirror the agents as `.claude/agents` (optional) | chore | DSK-12-05 | If the team also runs Claude Code, the same roster exists with equivalent tool restrictions; otherwise recorded as not needed | Roster listed in both tools or a recorded decision | 1 | `pegasus-desktop-reviewer` · none · none |

## 6. Routing table

| Purpose | Skills (pinned source) | MCP tools | Subagent |
| --- | --- | --- | --- |
| Discovery and configuration checks | `pegasus-desktop` (project) | Kanmer (`get_status`) | `pegasus-desktop-reviewer` |
| Vendoring, lockfile, CI hash check | `directory-build-organization`, `authoring-github-workflows` (`dotnet/skills` `98f84851`), `winui-packaging` (`win-dev-skills` `f1028dd5`) for the CI sample | Microsoft Learn | `pegasus-release-packager` |
| Azure MCP wiring (read-only) | `azure-resource-lookup` (`azure-skills` `1a03acfb`) | Azure MCP `group_resource_list`, `subscription_list` | `pegasus-azure-auditor` |
| Ticket templates and ADR-0110 | `kanmer-tickets`, `kanmer-docs`, `kanmer-setup` (`.grok/skills/`) | Kanmer (`create_item`, `set_ticket_doc`, `link_doc`) | `pegasus-desktop-reviewer` |
| Dry run | whatever the chosen foundation ticket routes to | Kanmer, Microsoft Learn | `winui-dev`, `pegasus-test-engineer`, `pegasus-desktop-reviewer` |

Invocation protocol (§20.5) every ticket follows:

1. Read the project skill, the area plan, and the ticket folder
   (`get_doc_gates` before every move).
2. Read the exact relevant upstream `SKILL.md` files from the lockfile.
3. Summarise only the applicable guidance in the plan; name any upstream
   guidance a Pegasus decision overrides.
4. Implement the smallest vertical slice.
5. Run the skill-prescribed verification plus the repository profiles.
6. Record skills and SHAs, commands, results, screenshots or traces, and any
   deviation with its reason (Appendix C).
7. Hand to an independent reviewer (`pegasus-desktop-reviewer`).

Review protocol (§20.6): the reviewer loads the skills independently and
verifies dependency boundaries, XAML/native implementation, async and
UI-thread safety, accessibility, package and update implications, API and
data compatibility, test evidence, and cloud-placement justification.

Ticket metadata (proposal §25), recorded in the ticket body:

```yaml
agent_skills:
  project:
    - .agents/skills/project/pegasus-desktop/SKILL.md
  required_capabilities:
    - dotnet-testing        # resolves to run-tests, code-testing-agent
    - winui3-xaml           # resolves to winui-dev-workflow, winui-design
    - windows-accessibility # resolves to winui-ui-testing, winui-code-review
  lockfile: eng/skills/skills.lock.json
routing:
  subagent: winui-dev
  mcp: [microsoft-learn, kanmer]
```

## 7. Risks and traps

- Upstream skill names and layouts may change: `win-dev-skills` is 0.x
  preview and its README warns of breaking changes; pin by commit, never by
  branch; review each bump.
- Discovery mismatch: skills under `.codex/skills/` may not be found by Codex
  (documented scan root is `.agents/skills`); verify before assuming a skill
  loaded.
- Two copies of `pegasus-release` today; a third anywhere is a stop condition
  (one list per concept).
- `winui-session-report` reads session transcripts and must surface its
  privacy warning before anything is shared.
- Skills are playbooks, not dependencies: never add a skill folder to a
  project reference or deployment.
- Codex self-hop: an agent must never delegate to an agent of its own kind;
  every TOML says so.
- Read-only agents cannot write; callers must capture their output into the
  ticket or the plan.
- The Azure MCP entry, if enabled, must stay limited to read tools by
  instruction; there is no per-tool permission in the TOML, so the guardrail
  is the agent text plus the repository approval matrix.
- Kanmer doc gates are resolved at runtime (`get_doc_gates`), not from
  `board.yml`.

## 8. Documentation changes

- ADR-0110 (agent-skill pinning and invocation) under `docs/adr/`; index row.
- `AGENTS.md`: a sentence that agents load the project skill first and that
  the lockfile governs skill revisions (routing rule, not an ADR).
- `docs/runbook.md`: how to run `sync-skills.ps1` / `verify-skills.ps1`.
- `docs/desktop/README.md` routing legend stays the canonical name list;
  `skill-routing.md` resolves it.
- `.codex/config.toml`: `[agents]` table and the Azure MCP entry (disabled
  until DSK-12-06).
