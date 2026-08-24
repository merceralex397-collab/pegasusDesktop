# EPIC-013 · Area 12 — agent tooling

Read this once before working any `DSK-12-*` ticket. It carries what binds the whole
batch; the per-ticket detail is in the ticket body.

## What this epic delivers

The tooling that makes every other conversion ticket reproducible: upstream agent skills
pinned by commit and vendored into the tree with a lockfile and a CI hash check; the eight
Codex subagents enabled and bounded; the `pegasus-desktop` project skill as the routing
entry point; the Azure MCP server wired read-only; the `## Routing` block and Appendix C
evidence shape in the Kanmer documents; ADR-0110; the skill-update procedure; and one dry
run that proves the protocol survives contact with a real ticket.

Proposal coverage: §20 in full (20.1 purpose, 20.2 pinning and vendoring, 20.3 project
skill, 20.4 routing by work type, 20.5 invocation protocol, 20.6 review protocol); §25
ticket metadata (`agent_skills:` block); §26 Documentation set § Agent development;
Appendix C agent implementation evidence; Appendix D research basis (the pins).

## Decisions that bind every ticket here

- **L-04** (locked): specialist Codex subagents exist as `.codex/agents/*.toml`; every
  ticket names its subagent, skills and MCP tools. This epic is what makes that naming
  resolve to real files.
- **L-05** (locked): the board is seeded from these plans.
- **C-01** (2026-08-23): the repositories become private on completion; private Windows
  runner minutes bill at 2×, so CI lanes go on `ubuntu-latest` where the work allows and
  repository weight is a real cost.
- **Azure rule**: reads are free; every write is ⚠ and needs exact-target approval
  (`docs/runbook.md` § Live-operation approval matrix). No ticket in this epic performs
  an Azure write.
- **ADR block**: the conversion uses the reserved ADR-0100…ADR-0110, never "next free
  number" — upstream keeps issuing ADRs and the one-way sync would collide.
- **Branching**: task branch → PR into `dev` → exact-SHA promotion to `main` with the
  literal `MERGE AUTH GRANTED`. Never merge upstream straight into `main`.

## Deviations recorded here, not re-argued per ticket

- Proposal §20.2 asks for `docs/agent/skill-routing.md`. `scripts/Test-MarkdownPlacement.ps1:31`
  allows only `docs/(prd|frd|adr|design|desktop)`, so routing lives at
  `docs/desktop/12-agent-tooling/skill-routing.md`. ADR-0110 records this.
- `eng/` is **not** an allowed Markdown root. No `.md` may be added under `eng/skills/`;
  operational text goes to `docs/runbook.md`.
- `skill-routing.md`'s per-area index lists `create-custom-agent` for area 12 while its own
  "Not applicable — do not load" table rules that skill out (VS Code `.agent.md` format).
  **The do-not-load table wins**: never load `create-custom-agent`.

## Exit gate and what proves it

`codex --version`, `/skills` and `/agent` output recorded with a statement of the scanned
directories; `eng/skills/skills.lock.json` committed with real hashes and
`verify-skills.ps1` green locally and in the CI `changes` job (proved by a deliberate-drift
red run, then green); all eight agent TOMLs parse and appear in Codex; the project skill
present and referenced by every TOML; one ticket executed end to end with the protocol and
reviewed by `pegasus-desktop-reviewer`.

## Routing for this area

| Purpose | Subagent | Skills (pinned source) | MCP |
| --- | --- | --- | --- |
| Discovery, config checks, template and ADR work | `pegasus-desktop-reviewer` | `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`); `kanmer-setup`, `kanmer-tickets`, `kanmer-docs`, `kanmer-research`, `kanmer-plan`, `kanmer-review` (`.grok/skills/<name>/SKILL.md`, Kanmer 0.1.0) | Kanmer |
| Vendoring, lockfile, CI hash check, update procedure | `pegasus-release-packager` | `directory-build-organization`, `authoring-github-workflows`, `binlog-failure-analysis` (`dotnet/skills` `98f848512e9ee4877e399a0ae367bb5e4a193144`) | Microsoft Learn, Kanmer |
| Azure MCP wiring (read-only) | `pegasus-azure-auditor` | `azure-resource-lookup` (`microsoft/azure-skills` `1a03acfb9ac1a1a05518bf7420d4618cc41847be`) | Azure MCP read tools only |
| Dry run | `winui-dev`, `pegasus-test-engineer`, `pegasus-desktop-reviewer` | whatever the target ticket routes to, from `microsoft/win-dev-skills` `f1028dd5bb19af59df400cb4a2ab867e40a40a4a` (v0.5.0) | Kanmer, Microsoft Learn |

Vendored destinations once DSK-12-02 lands: `.agents/skills/vendor/{dotnet,windows,azure}/<name>/`.
Until then the WinUI skills sit untracked under `.codex/skills/`.

## Traps (plan § 7)

- Upstream names and layouts move: `win-dev-skills` is a 0.x preview warning of breaking
  changes. Pin by commit, never by branch; review every bump; a rename breaks routing tables
  that the hash verifier cannot see.
- Discovery mismatch: skills under `.codex/skills/` may not be found at all — verify before
  assuming a skill loaded, and before deleting anything.
- Two copies of `pegasus-release` exist today; a third anywhere is a stop condition.
- `winui-session-report` reads session transcripts and must surface its privacy warning
  before anything is shared.
- Skills are playbooks, not dependencies: never add a skill folder to a project reference
  or a deployment.
- Codex self-hop: an agent must never delegate to an agent of its own kind.
- Read-only agents cannot write; the caller must transcribe their output into the ticket.
- The Azure MCP entry has no per-tool permission in the TOML — the guardrail is the agent
  text plus the approval matrix.
- Kanmer doc gates are resolved at runtime by `get_doc_gates`, never from `board.yml`.
- `.grok/skills/` is tracked but installed and reconciled by `kanmer-setup`; hand edits are
  reported stale by `get_status` and can be overwritten.
- `/.claude/` is gitignored (`.gitignore:23`) — anything mirrored there is untracked.

## Read before starting any ticket in this epic

1. `.agents/skills/project/pegasus-desktop/SKILL.md`
2. `docs/desktop/12-agent-tooling/README.md`
3. `docs/desktop/12-agent-tooling/skill-routing.md`
4. `docs/desktop/12-agent-tooling/subagents.md`
5. `docs/desktop/12-agent-tooling/skills.lock.draft.json`
6. `docs/desktop/README.md` (decisions, routing legend)
7. `docs/desktop/00-governance-and-workflow/README.md` (ticket template, board shape, ADR block)
8. `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 20, § 25, Appendix A, Appendix C
9. `AGENTS.md` (§ ADR conventions, § New Markdown placement, § Repository task workflow)
10. `docs/engineering.md` (§ Branches and delivery, § Required evidence tiers)
