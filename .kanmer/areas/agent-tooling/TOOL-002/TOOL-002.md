---
id: TOOL-002
type: ticket
title: >-
  DSK-12-02 · Vendor the pinned skills and promote `skills.lock.json` with real
  hashes
status: backlog
area: agent-tooling
assignee: ''
profile: chore
labels:
  - desktop-conversion
  - plan-12
  - phase-0
  - tier-1
groups:
  - EPIC-013
  - HZN-001
links: []
blocks:
  - TOOL-003
  - TOOL-004
  - TOOL-008
  - TOOL-010
docs_todo: true
archived: false
created: '2026-08-24T08:04:54.702Z'
updated: '2026-08-24T08:51:46.986Z'
---

## What

Create `.agents/skills/vendor/{dotnet,windows,azure}/`, populate it from the three pinned upstream commits, and promote `docs/desktop/12-agent-tooling/skills.lock.draft.json` to `eng/skills/skills.lock.json` with real SHA-256 hashes — written by a new idempotent `eng/skills/sync-skills.ps1` and checked by a new `eng/skills/verify-skills.ps1`.

## Why

Proposal §20.2: "Do not let every agent clone the latest upstream skill at execution time. Mutable instructions make code review and reproduction unreliable." Today the eight WinUI skills exist only as **untracked** working-tree folders under `.codex/skills/` (`git status --porcelain` lists them `??`), so a fresh clone of this fork has no WinUI guidance at all and no record of which revision anyone was reading. Every ticket in this plan set names a skill; without this ticket those names resolve to nothing reproducible. [[DSK-12-03]] adds the CI check, [[DSK-12-04]] removes the duplicate tree, [[DSK-12-08]] records the decision as ADR-0110, and [[DSK-12-10]] writes the bump procedure — all four depend on the lockfile existing.

## Source of truth

- Plan row: `docs/desktop/12-agent-tooling/README.md` § 5 — `DSK-12-02`
- Plan detail: `docs/desktop/12-agent-tooling/skills.lock.draft.json` — the complete draft: schema `version: 2`, a `policy` block, three `sources` with commits, and **35** skill entries each carrying `name`, `source`, `skillPath`, `destination`, `computedHash` (`"TBD - computed by eng/skills/sync-skills.ps1 (SHA-256 of SKILL.md)"`), `reviewedOn`, `owner`, `reason`. Copy it; do not retype it.
- Plan detail: `docs/desktop/12-agent-tooling/README.md` § 3 Decisions and assumptions (canonical vendored tree, lockfile fields, "agents never fetch a moving `main`")
- Plan detail: `docs/desktop/12-agent-tooling/skill-routing.md` § Pinned sources and § Not applicable — do not load
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 20.2 Pinning and vendoring, § 21.2 CI stages
- Repository evidence:
  - `.codex/skills/` — `winui-code-review`, `winui-design`, `winui-dev-workflow`, `winui-packaging`, `winui-session-report`, `winui-setup`, `winui-ui-testing`, `winui-wpf-migration` (all untracked) plus the tracked `pegasus-release/SKILL.md`
  - `.codex/skills/winui-design/winui-search.exe` — **7,911,936 bytes**; `.codex/skills/winui-dev-workflow/analyzer/Microsoft.WindowsAppSDK.Analyzers.dll` — 49,664 bytes; `.codex/skills/winui-session-report/Analyze-Session.ps1` — 45,966 bytes. These are the non-Markdown payloads the lockfile does not currently describe.
  - `scripts/Test-MarkdownPlacement.ps1:31` — `Test-AllowedMarkdownPath` allows `^((docs/(prd|frd|adr|design|desktop))|workspaces/document-extraction|\.agents/skills|\.design-sync|\.grok|\.stitch|design/planning-and-old-designs)/.+\.md$`. `.agents/skills` is allowed; **`eng/` is not**.
  - `scripts/Test-DocumentationLinks.ps1:14` — excludes `^(node_modules|corpus|artifacts|\.git|\.claude|\.agents|\.codex|\.kanmer)/`, so vendored Markdown is not link-checked.
  - `skills-lock.json` (repository root) — the unrelated `version: 1` mattpocock file with four entries and real `computedHash` values.
  - `eng/` does not exist yet; this ticket creates it (area 09 later adds `eng/packaging/`).
- Binding decisions:
  - **L-04** — every ticket names its subagent, skills and MCP tools; the lockfile is what makes those names verifiable.
  - **C-01** (2026-08-23) — the repositories become private on completion and private Windows runner minutes bill at 2×; repository weight and CI time are live costs, which is why the binary payload question in step 5 has to be answered rather than assumed.
- Depends on: `DSK-12-01` — its verdict decides whether the vendored tree is discoverable at all, and therefore whether `.agents/skills/vendor/` is the right destination.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `directory-build-organization` (`dotnet/skills` `98f84851`, `plugins/dotnet-msbuild/skills/directory-build-organization/SKILL.md`) → `authoring-github-workflows` (`dotnet/skills` `98f84851`, `.agents/skills/authoring-github-workflows/SKILL.md`) → `kanmer-plan`, `kanmer-execute` (`.grok/skills/<name>/SKILL.md`). Note the bootstrap order: until this ticket lands, the two `dotnet/skills` files are read from the upstream repository at the pinned commit, not from a vendored path.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) only for PowerShell API facts such as `Get-FileHash` semantics.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates: `leave-preparing` needs `plan` + `questions-resolved`; `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates <this ticket's board id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row and the plan sections named under **Source of truth**, then `get_doc_gates <this ticket's board id>` and `take_ticket`. Read [[DSK-12-01]]'s `research/` verdict before writing any path.
2. Read `docs/desktop/12-agent-tooling/skills.lock.draft.json` in full and confirm its shape against the plan: 35 entries and three sources — `dotnet/skills` `98f848512e9ee4877e399a0ae367bb5e4a193144` (2026-08-21), `microsoft/win-dev-skills` `f1028dd5bb19af59df400cb4a2ab867e40a40a4a` (v0.5.0, 2026-07-22), `microsoft/azure-skills` `1a03acfb9ac1a1a05518bf7420d4618cc41847be` (2026-08-21). These commit SHAs are the contract; never substitute a branch name.
3. Create `eng/skills/` and write `eng/skills/sync-skills.ps1`. It must: read the lockfile; for each `source`, fetch that repository **at the pinned commit** into a temporary directory (`git clone --filter=blob:none --no-checkout <repo> <tmp>` then `git -C <tmp> checkout <commit> -- <skillPath directory>`); copy the whole skill folder — not only `SKILL.md` — to the entry's `destination`; compute SHA-256 for every copied file; write the per-skill `computedHash` back into the lockfile. Give it a `-Verify` switch that computes and compares without copying.
4. Write `eng/skills/verify-skills.ps1`: recompute the hashes of every destination named in `eng/skills/skills.lock.json` and exit non-zero naming each drifted or missing path. It must run under `pwsh` on `ubuntu-latest`, because [[DSK-12-03]] puts it in the `changes` job (`.github/workflows/ci.yml:12-15`) — use forward slashes, `Join-Path`, and no Windows-only cmdlet; assume the filesystem is case-sensitive.
5. **Decide and record** how the non-Markdown payload is vendored, because the draft lockfile hashes `SKILL.md` only while the skills ship binaries: `winui-design/winui-search.exe` (7,911,936 bytes), `winui-dev-workflow/analyzer/Microsoft.WindowsAppSDK.Analyzers.dll` (49,664 bytes), `winui-session-report/Analyze-Session.ps1` (45,966 bytes). Either commit them and hash them, or have `sync-skills.ps1` fetch them on demand and hash only the text. Record the choice and its reason in the lockfile `policy` block and in the ticket plan; a 7.5 MiB binary in a repository that becomes private (C-01) is a decision, not a default.
6. Run `pwsh ./eng/skills/sync-skills.ps1`. Confirm the working tree changed **only** under `.agents/skills/vendor/**` and `eng/skills/**`: `git status --porcelain` must show nothing under `src/`, `tests/`, `scripts/`, `.codex/` or `docs/`.
7. Prove idempotency: run `pwsh ./eng/skills/sync-skills.ps1` a second time and confirm `git status --porcelain` is clean afterwards. A sync that rewrites hashes or line endings on every run cannot be a CI gate.
8. Prove the verifier bites: append one character to a vendored `SKILL.md`, run `pwsh ./eng/skills/verify-skills.ps1` and confirm it exits non-zero and names that exact path; then `git checkout --` the file and confirm it exits 0.
9. Confirm every entry in the lockfile has a real hash — no `computedHash` may still read `TBD`. Check with `grep -c '"computedHash"' eng/skills/skills.lock.json` (expect 35) and `grep -c 'TBD' eng/skills/skills.lock.json` (expect 0).
10. Do not vendor a skill on the do-not-load table. `docs/desktop/12-agent-tooling/skill-routing.md` § Not applicable names `azure-deploy`, `azure-prepare`, `azure-app-onboard`, `azure-app-onboard-prereq`, `azure-cloud-migrate`, `azure-enterprise-infra-planner`, `python-appservice-deploy`, `entra-app-registration`, `entra-agent-id` and others; none of them appears in the draft lockfile — verify that is still true after the sync and record the count.
11. **Decide and record** what happens to the root `skills-lock.json` (mattpocock, `version: 1`, four entries whose skill bodies are not in the tree). Plan § 3 permits leaving it as is or folding it into the new file. State the decision in the plan document; do not silently delete it.
12. Run the two documentation gates that a new tree can break and record their output: `pwsh ./scripts/Test-DocumentationLinks.ps1` (expected: "All relative Markdown links resolve") and `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base with dev> -Head HEAD` (expected: "Markdown placement passed"). Note that `.agents/skills` is an allowed root but `eng/` is not — **no `.md` file may be added under `eng/skills/`**; the procedure documentation belongs in `docs/runbook.md` and is [[DSK-12-10]]'s work.
13. Record the Appendix C evidence in the post-implementation report: skills consulted with their pinned SHAs, the commands run verbatim, the two sync runs, the drift test, and the binary-payload decision from step 5.

## Acceptance criteria

- [ ] `.agents/skills/vendor/dotnet/`, `.agents/skills/vendor/windows/` and `.agents/skills/vendor/azure/` exist and hold every skill named in the lockfile at its `destination`.
- [ ] `eng/skills/skills.lock.json` exists with schema `version: 2`, the three pinned commits, and 35 entries with real SHA-256 hashes and no `TBD`.
- [ ] `eng/skills/sync-skills.ps1` is idempotent: a second consecutive run leaves `git status --porcelain` clean.
- [ ] `eng/skills/verify-skills.ps1` exits 0 on a clean tree and non-zero naming the file on a one-byte mutation.
- [ ] The binary-payload decision and the root `skills-lock.json` decision are recorded in the plan document with their reasons.
- [ ] No `.md` file was added under `eng/`; the placement gate passes.

## Verification

- [ ] `pwsh ./eng/skills/verify-skills.ps1` — expected: exit code 0 and a line naming the number of skills verified.
- [ ] `pwsh ./eng/skills/sync-skills.ps1; git status --porcelain` — expected: empty output.
- [ ] `grep -c 'TBD' eng/skills/skills.lock.json` — expected: `0`.
- [ ] `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base> -Head HEAD` — expected: `Markdown placement passed for <base>..<head>.`
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: `All relative Markdown links resolve (<n> files checked).`

## Evidence tier

Tier 1 — Static/build/architecture. It obliges recorded command output showing the vendored tree matches the lockfile hashes and that the verifier fails on drift; it proves consistency only, never that any agent read a skill.

## Documentation changes

- `docs/desktop/12-agent-tooling/README.md` § 3 — the sentence "today the WinUI skills live under `.codex/skills/`" becomes historical once the move lands; update it to name the vendored destination and keep the date.
- `docs/desktop/12-agent-tooling/skill-routing.md` § Pinned sources — the parenthetical "today the WinUI skills sit under `.codex/skills/`" is updated to the vendored path.
- `docs/runbook.md` — **not** in this ticket; the sync/verify procedure is [[DSK-12-10]].

## Guardrails

- **Azure**: no write. `azure-*` skills are vendored as text only; nothing in this ticket calls an Azure tool.
- **Scope boundary**: may create `eng/skills/` and `.agents/skills/vendor/**`, and edit the two `docs/desktop/12-agent-tooling/` files named above. Must not touch `src/`, `tests/`, `infra/`, `.github/workflows/ci.yml` (that is [[DSK-12-03]]) or delete anything under `.codex/skills/` (that is [[DSK-12-04]]).
- **Traps**: `microsoft/win-dev-skills` is a 0.x preview whose README warns of breaking changes — pin by commit, never by branch, and never re-fetch at execution time. Skills are playbooks, not dependencies: never add a vendored folder to a `.csproj`, a project reference or a deployment. The CI `documentation` job fails on any new `.md` outside the allowed roots, and `eng/` is not one of them.
- **Sizing concern**: this row carries a scripted sync, a verifier, a lockfile promotion and two recorded decisions. It is deliberately not split (seed rule: write the steps and note the concern) — if the binary-payload decision turns into design work, file it as a follow-up rather than widening this ticket.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
