---
id: TOOL-004
type: ticket
title: >-
  DSK-12-04 · Remove the duplicate skill copies under `.codex/skills/` so there
  is one list
status: backlog
area: agent-tooling
assignee: ''
profile: chore
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
created: '2026-08-24T08:07:44.135Z'
updated: '2026-08-24T08:07:44.135Z'
---

## What

After the vendored tree exists, delete the second copies under `.codex/skills/` — the tracked `pegasus-release/SKILL.md` and the eight untracked `winui-*` folders — and update every document that still points at the old location, so each skill is discoverable from exactly one path.

## Why

`docs/desktop/12-agent-tooling/README.md` § 7 states the rule plainly: "Two copies of `pegasus-release` today; a third anywhere is a stop condition (one list per concept)." `.agents/skills/pegasus-release/SKILL.md` and `.codex/skills/pegasus-release/SKILL.md` are byte-identical duplicates (13,299 bytes each per plan § 2); once [[DSK-12-02]] vendors the eight WinUI skills under `.agents/skills/vendor/windows/`, the `.codex/skills/` copies become a third and fourth surface an agent can silently read the wrong revision from — defeating the lockfile that [[DSK-12-03]] enforces. The operator-visible consequence is an agent following stale WinUI guidance with a green CI, because the CI only hashes the vendored tree.

## Source of truth

- Plan row: `docs/desktop/12-agent-tooling/README.md` § 5 — `DSK-12-04`
- Plan detail: `docs/desktop/12-agent-tooling/README.md` § 3 ("after the move, `.codex/skills/winui-*` and the duplicate `.codex/skills/pegasus-release` are removed so there is one list") and § 7 Risks and traps
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 20.2 Pinning and vendoring, § 20.3 Project-local Pegasus skill
- Repository evidence:
  - `git ls-files .codex` — today returns the eight agent TOMLs, `.codex/config.toml` and **`.codex/skills/pegasus-release/SKILL.md`**; that last path is the only tracked skill under `.codex/`
  - `git status --porcelain` — the eight `.codex/skills/winui-*` folders are untracked (`??`), so they are deleted from disk, not from git
  - `.agents/skills/pegasus-release/SKILL.md` — the copy that survives
  - `.codex/skills/winui-design/winui-search.exe` (7,911,936 bytes) and `.codex/skills/winui-dev-workflow/analyzer/Microsoft.WindowsAppSDK.Analyzers.dll` — payload that must exist at the vendored destination before anything here is deleted
  - `docs/desktop/README.md` § Routing legend — the WinUI row still reads "vendored under `.codex/skills/` today"
  - `docs/desktop/12-agent-tooling/skill-routing.md` § Pinned sources — "today the WinUI skills sit under `.codex/skills/`"
  - `scripts/Test-DocumentationLinks.ps1:14` — `.codex` and `.agents` are excluded from link checking, so a stale sentence inside those trees is not caught by CI; the `docs/` sentences are
- Binding decisions:
  - **L-04** — every ticket names its skills; a name must resolve to one file.
  - **L-05** — the board is seeded from these plans; the plan requires one list.
- Depends on:
  - `DSK-12-01` — its verdict says whether Codex discovers `.codex/skills` at all; deleting a tree the toolchain actually uses without the replacement proven is how guidance disappears mid-conversion.
  - `DSK-12-02` — the vendored destinations and the lockfile must exist and verify green before anything is removed.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-plan`, `kanmer-execute` (`.grok/skills/<name>/SKILL.md`). No upstream skill is needed to delete files; do not load one for form's sake.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates: `leave-preparing` needs `plan` + `questions-resolved`; `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates <this ticket's board id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row and the plan sections named under **Source of truth**, then `get_doc_gates <this ticket's board id>` and `take_ticket`. Read [[DSK-12-01]]'s `research/` verdict — it is the safety interlock for this ticket.
2. Prove the replacement exists before deleting anything: run `pwsh ./eng/skills/verify-skills.ps1` and require exit 0, then confirm every one of the eight WinUI skills is present at its vendored destination: `ls .agents/skills/vendor/windows/` — expected `winui-setup`, `winui-dev-workflow`, `winui-design`, `winui-code-review`, `winui-ui-testing`, `winui-packaging`, `winui-wpf-migration`, `winui-session-report`.
3. Prove the payload survived the move, not just the Markdown: `ls -l .agents/skills/vendor/windows/winui-design/winui-search.exe` (expect 7,911,936 bytes, or the recorded [[DSK-12-02]] step-5 decision that it is fetched rather than committed) and `ls .agents/skills/vendor/windows/winui-dev-workflow/analyzer/`.
4. Confirm the `pegasus-release` duplicate is genuinely a duplicate before removing either copy: compare hashes with `Get-FileHash .agents/skills/pegasus-release/SKILL.md` and `Get-FileHash .codex/skills/pegasus-release/SKILL.md` — they must match. If they differ, stop: reconcile the difference into `.agents/skills/pegasus-release/SKILL.md` first and record what changed.
5. Remove the tracked duplicate from git: `git rm .codex/skills/pegasus-release/SKILL.md`. `.agents/skills/pegasus-release/SKILL.md` is the surviving copy — do not delete that one.
6. Remove the eight untracked WinUI folders from disk (`rm -r .codex/skills/winui-*`). They are working-tree only, so this produces no git diff; record the `git status --porcelain` before and after so the proof shows the `??` entries disappearing.
7. Assert one list: `find .codex/skills -name 'SKILL.md'` must return nothing, and `.codex/skills/` should be empty or gone. Then `find .agents/skills -name 'SKILL.md' | sort` must list each skill exactly once — no name may appear twice.
8. Find and fix every stale pointer: `grep -rn '\.codex/skills' --include='*.md' --include='*.toml' .` and update each hit. Expected hits are `docs/desktop/README.md` § Routing legend, `docs/desktop/12-agent-tooling/README.md` § 2 and § 3, `docs/desktop/12-agent-tooling/skill-routing.md` § Pinned sources, and possibly an agent TOML under `.codex/agents/`. Rewrite each to the vendored path and keep the historical note dated, rather than erasing the fact that the skills once lived there.
9. Re-read each of the eight `.codex/agents/*.toml` and confirm none of them names a `.codex/skills/...` path in its `developer_instructions`; they should name skills by name and the project skill by its `.agents/skills/project/pegasus-desktop/SKILL.md` path only.
10. **Operator step** — restart Codex and run `/skills`; hand back the listing. Expected: every skill in the lockfile appears **once**, and no `winui-*` entry resolves from `.codex/skills`. If a skill vanished entirely, revert the deletion and reopen [[DSK-12-01]]'s verdict.
11. Run the documentation gates and record their output: `pwsh ./scripts/Test-DocumentationLinks.ps1` (expected `All relative Markdown links resolve`) and `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base with dev> -Head HEAD` (expected `Markdown placement passed`; deletions are not checked by the placement gate, only additions and renames).
12. Record the Appendix C evidence: the before/after `find` output, the hash comparison from step 4, the `/skills` listing, and the list of documents edited.

## Acceptance criteria

- [ ] `find .codex/skills -name 'SKILL.md'` returns nothing.
- [ ] `.agents/skills/pegasus-release/SKILL.md` remains and is the only copy of that skill.
- [ ] Every skill named in `eng/skills/skills.lock.json` resolves from exactly one path under `.agents/skills/`.
- [ ] `/skills` in a fresh Codex session lists each skill once (operator evidence attached).
- [ ] No document or agent TOML still points at `.codex/skills`.
- [ ] `pwsh ./eng/skills/verify-skills.ps1` still exits 0 after the deletions.

## Verification

- [ ] `find .codex/skills -name 'SKILL.md' | wc -l` — expected: `0`.
- [ ] `git ls-files .codex` — expected: the eight `.codex/agents/*.toml` files and `.codex/config.toml`, and nothing under `.codex/skills`.
- [ ] `grep -rn '\.codex/skills' --include='*.md' docs/` — expected: only dated historical sentences, no live routing instruction.
- [ ] `pwsh ./eng/skills/verify-skills.ps1` — expected: exit 0.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: `All relative Markdown links resolve (<n> files checked).`

## Evidence tier

Tier 1 — Static/build/architecture. It obliges recorded filesystem and tool-listing evidence that exactly one copy of each skill remains and that the toolchain still resolves every name.

## Documentation changes

- `docs/desktop/README.md` § Routing legend — the WinUI skills row stops saying "vendored under `.codex/skills/` today" and names `.agents/skills/vendor/windows/`.
- `docs/desktop/12-agent-tooling/README.md` § 2 and § 3 — the `.codex/skills` sentences become dated historical notes.
- `docs/desktop/12-agent-tooling/skill-routing.md` § Pinned sources — the parenthetical about `.codex/skills` is replaced by the vendored path.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may delete under `.codex/skills/` and edit the three documents named above. Must not touch `.codex/agents/*.toml` beyond correcting a stale path, must not touch `eng/skills/**` (that is [[DSK-12-02]]), and must not delete anything under `.agents/skills/`.
- **Traps**: deleting a tree the installed toolchain actually reads is irreversible in effect if the vendored copy is incomplete — steps 2 and 3 are the interlock, not paperwork. `.codex` and `.agents` are excluded from `scripts/Test-DocumentationLinks.ps1`, so a stale link inside those trees will **not** be caught by CI; grep for it by hand. `winui-session-report` reads session transcripts and carries a privacy warning — do not run it while checking discovery.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
