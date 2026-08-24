# Plan — TOOL-010 (plan handle `DSK-12-10`): Write the skill-update procedure and the §20.6 review checklist, and rehearse one bump

**Diff estimate: ~3 files, ~65 lines** — plus the rehearsal bump's own diff, which is
**0 lines if the rehearsal is a no-op** (no newer upstream commit) and otherwise the
vendored files plus `eng/skills/skills.lock.json`. `docs/runbook.md` +~45 (the
`### Agent skill updates` subsection: nine numbered steps plus the rename rule),
the chosen checklist home +~15 (the §20.6 block), and
`docs/desktop/12-agent-tooling/README.md` § 8 +1 line pointing at the runbook subsection.

## Approach

**Write the recipe where the operational procedures already live, and rehearse it before
claiming it works.** `docs/runbook.md` is the repository's operational home and
`docs/desktop/12-agent-tooling/README.md` § 8 already names it as this area's documentation
change. The alternative — an `eng/skills/README.md` beside the scripts, which is where a
reader would look first — is **impossible**, not merely undesirable:
`scripts/Test-MarkdownPlacement.ps1:31` allows Markdown only under
`docs/(prd|frd|adr|design|desktop)`, `workspaces/document-extraction`, `.agents/skills`,
`.design-sync`, `.grok`, `.stitch` and `design/planning-and-old-designs`, and `eng/` is not
among them, so the CI `documentation` job (`.github/workflows/ci.yml:71-87`) would fail the
PR. Say that in the plan so nobody tries it again.

The second choice is to make the **rename rule** as prominent as the bump steps. The hash
verifier from [[TOOL-002]] (`DSK-12-02`) proves the vendored bytes match the lockfile; it
cannot know that a skill was renamed, split or removed upstream, and
`microsoft/win-dev-skills` is a 0.x preview whose own README warns of breaking changes. A
rename therefore produces a **green CI and broken routing** — the exact failure this ticket
is here to prevent. So the rename rule names all five places by path rather than saying
"update the routing tables".

The third choice is that the **checklist has exactly one home, coordinated with
[[TOOL-007]]** (`DSK-12-07`). That ticket's step 4 chooses the durable home for the
`## Routing` block; this ticket's checklist follows the same choice so the two do not create
two conventions. If [[TOOL-007]] has not been worked yet, this ticket adopts its
recommendation — the project skill as the definition, a pointer elsewhere — and records
that it did.

## Governing docs

The ticket carries `refs: []` and **`docs_todo: true`**.

> **New ADR** — ADR-0110 (agent-skill pinning and the invocation/review protocol), authored
> by [[TOOL-008]] (plan handle `DSK-12-08`), filename
> `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md`. This plan is written to the
> decision as recorded in `docs/desktop/12-agent-tooling/README.md` § 3 ("Agents never fetch
> a moving `main`; a skill update is a reviewed PR that bumps the commit in the lockfile and
> re-runs the sync script") and § 8. ADR-0110's Decision section will point at this
> runbook subsection for the *how*; if the ADR lands differently this plan is revised before
> implementation.

Programme-level authorities this plan meets:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal §20.2 | Pin by commit, never by branch; a bump is a reviewed change | Steps 4, 8 |
| Proposal §20.6 | The reviewer loads the skills independently and verifies eight named points | Step 6 |
| Proposal §26 § Agent development | The documentation set includes the review checklist and the pinned-skills lockfile | Steps 3–7 |
| `docs/engineering.md:10-52` § Branches and delivery | Task branch from `dev`; PR into `dev`; exact-SHA promotion to `main` with the literal `MERGE AUTH GRANTED` | Step 4 items 1 and 9 |
| L-04 (locked) | Every ticket names its subagent, skills and MCP tools — so a rename must be chased everywhere those names appear | Step 5 |
| C-01 (2026-08-23) | CI minutes are a live cost | Step 9 |
| `AGENTS.md` § New Markdown placement | `eng/` is not an allowed Markdown root | Step 3 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (`sandbox_mode = "workspace-write"`, `model_reasoning_effort = "high"`).
- **Skills**, in load order:
  1. `pegasus-desktop` — `.agents/skills/project/pegasus-desktop/SKILL.md`
  2. `authoring-github-workflows` — `.agents/skills/vendor/dotnet/authoring-github-workflows/`
     (from `dotnet/skills` `98f848512e9ee4877e399a0ae367bb5e4a193144`)
  3. `kanmer-docs` — `.grok/skills/kanmer-docs/SKILL.md`
  4. `kanmer-review` — `.grok/skills/kanmer-review/SKILL.md`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates confirmed by
  `get_doc_gates TOOL-010`: `leave-preparing` needs `plan` + `questions-resolved`;
  `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates TOOL-010` before
  every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

Refines the body's 12 steps in the same order.

1. **Orientation.** Read `EPIC-013/context.md`, then the plan sections in the body's
   **Source of truth**. `get_doc_gates TOOL-010`, then `take_ticket`. Read
   [[TOOL-007]]'s plan step 4 decision if it exists — the checklist home in step 7 follows it.
2. **Confirm the tooling is green before documenting it.**
   `pwsh ./eng/skills/verify-skills.ps1` must exit 0. A runbook entry for a script that does
   not run is worse than none.
3. **Add `### Agent skill updates` to `docs/runbook.md` under an existing `##` heading.**
   **Recommended: `## Maintenance`** (`docs/runbook.md:1250`) — its own text is "Reconcile
   this procedure whenever requirements, accepted decisions, production callers, external
   contracts, supported platforms, evidence boundaries, or deployment architecture change",
   which is exactly what a pinned-skill bump is. Runner-up is
   `## Repository and delivery operations` (`docs/runbook.md:1242`), which is about the work
   queue and the task workflow rather than recurring maintenance. Pick one and **say why** in
   this plan under a dated heading.
   **Do not create `eng/skills/README.md`** — see **Approach**; the placement gate forbids
   it and the CI `documentation` job would fail.
4. **Write the recipe as nine numbered steps** a weaker agent can follow without inference:
   1. branch from `dev` (`task/<slug>`), never from `main`;
   2. for each skill whose source is being bumped, read the upstream diff between the pinned
      commit and the candidate commit **for that `skillPath`** and summarise what changed;
   3. edit only the `commit` (and `commitDate`) fields of the affected `sources` entry in
      `eng/skills/skills.lock.json` — not the per-skill `computedHash` values, which the
      sync script writes;
   4. run `pwsh ./eng/skills/sync-skills.ps1`;
   5. run `pwsh ./eng/skills/verify-skills.ps1` and require exit 0;
   6. commit the vendored diff and the lockfile **together in one commit**, so a bisect
      never lands on a tree whose hashes do not match;
   7. open a PR into `dev` whose description carries the step 2 diff summary;
   8. independent review by `pegasus-desktop-reviewer`, which loads the changed skills
      itself;
   9. exact-SHA promotion to `main` with the literal `MERGE AUTH GRANTED`
      (`docs/engineering.md:10-52`; `scripts/Test-MainBranchHistory.ps1` guards `main` on
      push).
5. **Write the rename rule explicitly, naming all five paths**, because the hash verifier
   cannot see a rename. If a bump renames, splits or removes a skill, the same PR must
   update:
   1. `docs/desktop/12-agent-tooling/skill-routing.md` (work-type routing table, per-area
      index, and the "Not applicable — do not load" table);
   2. `docs/desktop/12-agent-tooling/subagents.md` (the eight agent bodies name skills in
      prose);
   3. every affected `.codex/agents/*.toml`;
   4. `.agents/skills/project/pegasus-desktop/SKILL.md` § Next skill to load;
   5. `docs/desktop/README.md` § Routing legend.
   Add a sixth, worth naming even though the body lists five: each `EPIC-*/context.md`
   § Routing table on the Kanmer board carries the same names — say that a rename is chased
   there too, or say explicitly that board context is out of scope for a repository PR and
   name the ticket that reconciles it.
6. **Write the §20.6 review checklist as a copyable block**, prefixed by the rule that
   **the reviewer loads the relevant skills itself rather than trusting the implementer's
   summary**, then its eight points: dependency boundaries; XAML/native implementation;
   async and UI-thread safety; accessibility; package and update implications; API and data
   compatibility; test evidence; cloud-placement justification.
7. **Put it where `kanmer-review` will reach it — one copy only.** Coordinate with
   [[TOOL-007]] step 4. If that ticket chose the project skill as the definition home, the
   checklist goes there and `docs/runbook.md` **links** to it; if it chose
   `.grok/skills/kanmer-review/assets/pr-review.md`, the checklist goes there with the same
   `kanmer-setup` reconcile note (`.grok/skills/` is machine-managed and `get_status`
   reports drift by content hash). **A second copy is a stop condition.**
8. **Rehearse one bump end to end** following step 4. If a newer upstream commit exists for
   any of the three sources, use it. If none exists, rehearse against the **same** commit and
   prove the run is a genuine no-op (`git status --porcelain` clean,
   `verify-skills.ps1` exit 0) — then **record plainly that the rehearsal did not exercise a
   real diff, so the recipe's diff-reading step (item 2) is untested.** That honesty is part
   of the acceptance, not a caveat to bury.
9. **Record what the rehearsal cost**: which CI lanes the PR triggered and roughly how long
   each took. For a lockfile-and-vendored-files-only PR, expect `changes` (ubuntu),
   `documentation` (windows) and `local-development-scripts` (windows) to run, and the
   application build lanes **not** to — `scripts/Get-CiChangeFlags.ps1:11-12` has neither
   `.agents/` nor `eng/` in `$buildPattern` or `$infrastructurePattern`. Confirm that
   against the actual run rather than assuming it; under C-01 the Windows lanes bill at 2×.
10. **Have `pegasus-desktop-reviewer` review the rehearsal PR using the new checklist** and
    report whether the checklist was sufficient. A checklist that cannot be applied to its
    own first PR needs fixing now, not after twenty-two slices. Transcribe its verdict — it
    is read-only and cannot write.
11. **Run the documentation gates.** `pwsh ./scripts/Test-DocumentationLinks.ps1` → expect
    `All relative Markdown links resolve (<n> files checked).`
    `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base with dev> -Head HEAD` →
    expect `Markdown placement passed for <base>..<head>.`
12. **Record the Appendix C evidence**: the runbook section added and the heading chosen with
    its reason, the checklist location, the rehearsal PR number, the verifier output, the CI
    lane costs, and the step 8 statement about whether a real diff was exercised.

## Verification

Evidence tier **1 — Static/build/architecture**, as the body states. A documented procedure
that has never been run is not evidence, which is why the rehearsal is part of the
acceptance. `proof` is a `command-log` plus the rehearsal PR link.

1. `grep -n 'Agent skill updates' -A 20 docs/runbook.md` → the numbered recipe naming both
   `eng/skills/sync-skills.ps1` and `eng/skills/verify-skills.ps1` by path.
2. **One live copy of the checklist.** The body suggests
   `grep -rn 'dependency boundaries' --include='*.md' . | wc -l` → `1`; **that check does not
   work as written.** Measured 2026-08-24, before this ticket adds anything, the phrase
   "dependency boundaries" already appears **7 times across 6 files**:
   `docs/adr/0002-dotnet-modular-monolith-on-azure.md`,
   `docs/desktop/12-agent-tooling/README.md`,
   `docs/desktop/12-agent-tooling/subagents.md`,
   `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md`,
   `.agents/skills/project/pegasus-desktop/SKILL.md` and
   `.codex/agents/pegasus-desktop-reviewer.toml`. Use the body's own escape hatch — "the
   equivalent grep for the checklist's first line" — and grep for the distinctive prefix
   sentence instead, for example
   `grep -rc 'loads the relevant skills itself' --include='*.md' . ` → exactly one file with
   a non-zero count. Record which string was used as the canary.
3. `pwsh ./eng/skills/verify-skills.ps1` after the rehearsal → exit 0.
4. `git status --porcelain` after the rehearsal sync → empty for a no-op rehearsal, or only
   vendored files plus the lockfile for a real bump.
5. `pwsh ./scripts/Test-DocumentationLinks.ps1` →
   `All relative Markdown links resolve (<n> files checked).`
6. The rehearsal PR link plus `pegasus-desktop-reviewer`'s recorded verdict.

## Risks / open questions

| Risk | Mitigation |
| --- | --- |
| **A rename passes CI green and breaks routing silently.** The hash verifier sees bytes, not names, and `win-dev-skills` is a 0.x preview warning of breaking changes. | Step 5's rename rule names all five (six) paths; step 10's reviewer applies the checklist to the rehearsal PR. |
| The rehearsal is a no-op and the diff-reading step is never exercised, but the ticket reads as "procedure proven". | Step 8 requires the honest statement; the acceptance criterion repeats it. |
| A second copy of the checklist (one list per concept). | Step 7 coordinates with [[TOOL-007]] step 4 and verification item 2 is the canary grep. |
| An `.md` under `eng/` fails the CI `documentation` job. | Step 3 states the constraint with the regex and the line reference; `docs/runbook.md` is the home. |
| Pushing the rehearsal branch to `main` instead of PRing into `dev`. | Step 4 items 1 and 9; `scripts/Test-MainBranchHistory.ps1` fails a push whose history is not contained in `dev`. |
| `.grok/skills/` is machine-managed, so a checklist placed there is reported stale and can be overwritten. | Step 7's reconcile note; and the recommended home is the project skill, which no upgrade touches. |
| The rehearsal bump triggers more CI lanes than expected and costs 2× Windows minutes (C-01). | Step 9 measures it against the actual run rather than assuming. |

Open questions: **none opened as a blocking document.** The one decision left to the
implementer (which `##` heading in `docs/runbook.md`) has a recommended default with its
reason in step 3, and the checklist home is settled by following [[TOOL-007]] step 4 rather
than by asking.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. If the rehearsal is a
no-op the branch is documentation-only and the honest record is `n/a — docs-only` with the
date; if the rehearsal bumps a real commit the branch also carries vendored files and the
lockfile, so run the four lenses and record the dispositions instead._
