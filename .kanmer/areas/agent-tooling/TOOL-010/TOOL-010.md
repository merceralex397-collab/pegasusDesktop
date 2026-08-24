---
id: TOOL-010
type: ticket
title: >-
  DSK-12-10 · Write the skill-update procedure and the §20.6 review checklist,
  and rehearse one bump
status: preparing
area: agent-tooling
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:21:30.511Z'
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
created: '2026-08-24T08:12:34.585Z'
updated: '2026-08-24T21:21:30.511Z'
---

## What

Document in `docs/runbook.md` the exact PR recipe for bumping a pinned skill revision (read the upstream diff → bump the commit → re-sync → re-verify → reviewed PR into `dev`), put the proposal §20.6 review checklist where the reviewer will find it, and rehearse one bump end to end.

## Why

`docs/desktop/12-agent-tooling/README.md` § 3 states the rule — "Agents never fetch a moving `main`; a skill update is a reviewed PR that bumps the commit in the lockfile and re-runs the sync script" — but nowhere says how. `microsoft/win-dev-skills` is a 0.x preview whose own README warns of breaking changes (plan § 7), so a bump can rename or remove a skill that `skill-routing.md`, `subagents.md` and eight agent TOMLs name by hand. Without a written recipe the first bump either does not happen (the pins rot) or happens carelessly (routing names break with a green CI, because the verifier only checks hashes, not names). Plan § 8 lists the runbook entry as this area's documentation change.

## Source of truth

- Plan row: `docs/desktop/12-agent-tooling/README.md` § 5 — `DSK-12-10`
- Plan detail: `docs/desktop/12-agent-tooling/README.md` § 3 (update-by-reviewed-PR rule), § 6 Review protocol, § 7 Risks and traps (0.x preview, one list per concept), § 8 Documentation changes ("`docs/runbook.md`: how to run `sync-skills.ps1` / `verify-skills.ps1`")
- Plan detail: `docs/desktop/12-agent-tooling/skill-routing.md` — every table a rename would invalidate
- Plan detail: `docs/desktop/12-agent-tooling/subagents.md` — the eight agent bodies that name skills in prose
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 20.2 Pinning and vendoring, § 20.6 Review protocol (the eight verification points), § 26 Documentation set § Agent development ("pinned skills lockfile; skill routing; project-local skill; agent planning template; implementation evidence template; review checklist; architectural boundary rules")
- Repository evidence:
  - `eng/skills/sync-skills.ps1` and `eng/skills/verify-skills.ps1` — created by [[DSK-12-02]]; the two commands the recipe drives
  - `eng/skills/skills.lock.json` — the `sources` block whose `commit` field is what a bump edits
  - `docs/runbook.md` — the operational home; existing `##` sections include `## Locked restore, build, and test`, `## Testing model`, `## Repository and delivery operations`, `## Maintenance`
  - `docs/engineering.md:10-52` — the branching rule: task branch from `dev`, PR into `dev`, exact-SHA atomic promotion to `main` with the literal `MERGE AUTH GRANTED`; `scripts/Test-MainBranchHistory.ps1` guards `main` on push
  - `scripts/Test-MarkdownPlacement.ps1:31` — `eng/` is **not** an allowed Markdown root, so the procedure cannot live in an `eng/skills/README.md`
  - `.grok/skills/kanmer-review/assets/pr-review.md` — the review template; `.grok` is an allowed Markdown root but the tree is machine-managed by `kanmer-setup`
- Binding decisions:
  - **L-04** — every ticket names its subagent, skills and MCP tools; a bump that renames a skill breaks that naming across the plan set, so the recipe must require the rename to be chased in the same PR.
  - **C-01** — the repositories become private; CI minutes are a live cost, so a bump PR should not trigger more lanes than the change needs.
- Depends on: `DSK-12-02` — the sync script, the verifier and the lockfile must exist before a procedure can drive them or a bump can be rehearsed.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `authoring-github-workflows` (`.agents/skills/vendor/dotnet/authoring-github-workflows/`, from `dotnet/skills` `98f84851`) → `kanmer-docs` (`.grok/skills/kanmer-docs/SKILL.md`) → `kanmer-review` (`.grok/skills/kanmer-review/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates: `leave-preparing` needs `plan` + `questions-resolved`; `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates <this ticket's board id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row and the plan sections named under **Source of truth**, then `get_doc_gates <this ticket's board id>` and `take_ticket`.
2. Confirm the tooling exists and is green before documenting it: `pwsh ./eng/skills/verify-skills.ps1` must exit 0. A runbook entry for a script that does not run is worse than none.
3. Add a `### Agent skill updates` subsection to `docs/runbook.md`, under an existing `##` heading (`## Maintenance` or `## Repository and delivery operations` — pick one and say why). Do **not** create `eng/skills/README.md`: `eng/` is not an allowed Markdown root in `scripts/Test-MarkdownPlacement.ps1:31` and the CI `documentation` job would fail.
4. Write the recipe as numbered steps a weaker agent can follow:
   1. branch from `dev` (`task/<slug>`), never from `main`;
   2. for each skill whose source is being bumped, read the upstream diff between the pinned commit and the candidate commit for that `skillPath` and summarise what changed;
   3. edit only the `commit` (and `commitDate`) fields of the affected `sources` entry in `eng/skills/skills.lock.json`;
   4. run `pwsh ./eng/skills/sync-skills.ps1`;
   5. run `pwsh ./eng/skills/verify-skills.ps1` and require exit 0;
   6. commit the vendored diff and the lockfile **together** in one commit;
   7. open a PR into `dev` whose description carries the upstream diff summary from step 2;
   8. independent review by `pegasus-desktop-reviewer`, which loads the changed skills itself;
   9. exact-SHA promotion to `main` with the literal `MERGE AUTH GRANTED` (`docs/engineering.md:10-52`).
5. Add the rename rule explicitly, because the hash verifier cannot catch it: if a bump renames, splits or removes a skill, the same PR must update `docs/desktop/12-agent-tooling/skill-routing.md`, `docs/desktop/12-agent-tooling/subagents.md`, every affected `.codex/agents/*.toml`, `.agents/skills/project/pegasus-desktop/SKILL.md` § Next skill to load, and `docs/desktop/README.md` § Routing legend. Name all five paths in the runbook so nobody has to guess.
6. Write the §20.6 review checklist as a copyable block with its eight points — dependency boundaries; XAML/native implementation; async and UI-thread safety; accessibility; package and update implications; API and data compatibility; test evidence; cloud-placement justification — prefixed by the rule that the reviewer loads the relevant skills itself rather than trusting the implementer's summary.
7. Put that checklist where `kanmer-review` will actually reach it, and **coordinate with [[DSK-12-07]]'s step-4 decision on the durable home**. One copy only: if [[DSK-12-07]] chose the project skill, the checklist goes there and the runbook links to it; if it chose `.grok/skills/kanmer-review/assets/pr-review.md`, the checklist goes there with the same reconcile note. A second copy is a stop condition.
8. Rehearse one bump end to end following step 4. If a newer upstream commit exists for any of the three sources, use it. If none exists, rehearse against the **same** commit and prove the run is a genuine no-op (`git status --porcelain` clean, `verify-skills.ps1` exit 0) — then record plainly that the rehearsal did not exercise a real diff, so the recipe's diff-reading step is untested.
9. Record what the rehearsal cost: which CI lanes the PR triggered and roughly how long they took. C-01 makes that a real number, not trivia.
10. Have `pegasus-desktop-reviewer` review the rehearsal PR using the new checklist and report whether the checklist was sufficient — a checklist that cannot be applied to its own first PR needs fixing now.
11. Run the documentation gates and record their output: `pwsh ./scripts/Test-DocumentationLinks.ps1` (expected `All relative Markdown links resolve`) and `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base with dev> -Head HEAD` (expected `Markdown placement passed`).
12. Record the Appendix C evidence: the runbook section added, the checklist location, the rehearsal PR number, the verifier output, and the honest statement from step 8 about whether a real diff was exercised.

## Acceptance criteria

- [ ] `docs/runbook.md` carries an `### Agent skill updates` subsection with the nine numbered steps, naming both scripts by path.
- [ ] The rename rule names all five documents and file sets a rename must chase in the same PR.
- [ ] The §20.6 review checklist exists in exactly one place, with the "the reviewer loads the skills itself" rule at the top.
- [ ] One bump was rehearsed end to end, with the PR number recorded; if it was a no-op rehearsal, that is stated plainly.
- [ ] `pegasus-desktop-reviewer` reviewed the rehearsal PR with the new checklist and its verdict is recorded.
- [ ] No `.md` was added under `eng/`; the placement gate passes.

## Verification

- [ ] `grep -n 'Agent skill updates' -A 20 docs/runbook.md` — expected: the numbered recipe naming `eng/skills/sync-skills.ps1` and `eng/skills/verify-skills.ps1`.
- [ ] `grep -rn 'dependency boundaries' --include='*.md' . | wc -l` (or the equivalent grep for the checklist's first line) — expected: `1` live copy of the checklist.
- [ ] `pwsh ./eng/skills/verify-skills.ps1` after the rehearsal — expected: exit 0.
- [ ] `git status --porcelain` after the rehearsal sync — expected: empty for a no-op rehearsal, or only vendored files plus the lockfile for a real bump.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: `All relative Markdown links resolve (<n> files checked).`

## Evidence tier

Tier 1 — Static/build/architecture. It obliges the rehearsal PR, the verifier output and the link/placement gates; a documented procedure that has never been run is not evidence, which is why step 8 is part of the acceptance.

## Documentation changes

- `docs/runbook.md` — new `### Agent skill updates` subsection (procedure + rename rule).
- The chosen durable home from [[DSK-12-07]] step 4 — the §20.6 review checklist, one copy.
- `docs/desktop/12-agent-tooling/README.md` § 8 — a line pointing at the runbook subsection.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may edit `docs/runbook.md`, `docs/desktop/12-agent-tooling/README.md`, the single checklist home, and `eng/skills/skills.lock.json` plus `.agents/skills/vendor/**` **only** as part of the rehearsal bump. Must not edit `eng/skills/*.ps1` (that is [[DSK-12-02]]), `.github/workflows/ci.yml` (that is [[DSK-12-03]]), `src/` or `tests/`.
- **Traps**: `microsoft/win-dev-skills` is 0.x preview — a bump can rename or remove a skill and the hash verifier will not notice, only the routing tables will; never bump by branch name. `.grok/skills/` is machine-managed by `kanmer-setup`, so a checklist placed there needs the reconcile note. Never merge to `main` other than by exact-SHA promotion with `MERGE AUTH GRANTED`; `scripts/Test-MainBranchHistory.ps1` fails a push whose history is not contained in `dev`. Any new `.md` outside the allowed roots fails the CI `documentation` job, and `eng/` is not one of them.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
