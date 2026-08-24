# Plan — TOOL-004 (plan handle `DSK-12-04`): Remove the duplicate skill copies under `.codex/skills/` so there is one list

**Diff estimate: ~10 files, ~300 lines.** One tracked file deleted
(`.codex/skills/pegasus-release/SKILL.md`, 275 lines, 13,299 bytes) plus **nine** documents
edited across **23** stale references (measured, see step 8). Eight untracked `winui-*`
folders (18 files, 7.9 MiB) are removed from disk and produce **no git diff at all**, which
is why the proof for that half has to be `git status --porcelain` before and after rather
than a diff.

## Approach

Delete only after the replacement is proven, and treat the proof as an interlock rather
than paperwork. The eight `winui-*` folders are **untracked**, so `git checkout` cannot
bring them back — once they are gone from disk the only recovery is a re-run of
[[TOOL-002]]'s (`DSK-12-02`) sync against the pinned commits, which needs network access to
three upstream repositories. So the order is: verify the vendored tree hashes green, verify
each of the eight destinations exists, verify the binary payload survived, verify the
`pegasus-release` duplicate really is byte-identical, and only then remove anything. The
alternative considered and rejected was **leaving both trees in place** and relying on the
lockfile to say which is canonical: `docs/desktop/12-agent-tooling/README.md` § 7 and
`EPIC-013/context.md` § Traps both make a third copy of a skill a stop condition, and the
failure mode is silent — an agent reads a stale `.codex/skills/winui-design/SKILL.md`,
follows guidance that no longer matches the pinned revision, and CI stays green because
[[TOOL-003]]'s verifier only hashes `.agents/skills/vendor/**`.

## Governing docs

The ticket carries `refs: []` and **`docs_todo: true`**.

> **New ADR** — ADR-0110 (agent-skill pinning and the invocation protocol), authored by
> [[TOOL-008]] (plan handle `DSK-12-08`), filename
> `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md`. This plan is written to the
> decision as recorded in `docs/desktop/12-agent-tooling/README.md` § 3 ("after the move,
> `.codex/skills/winui-*` and the duplicate `.codex/skills/pegasus-release` are removed so
> there is one list") and § 7, and in the reserved ADR block at
> `docs/desktop/00-governance-and-workflow/README.md` § 3. If the ADR lands differently
> this plan is revised before implementation.

Programme-level authorities this plan meets:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `docs/desktop/12-agent-tooling/README.md` § 7 | One list per concept; a third copy is a stop condition | Steps 5–7 |
| Proposal §20.2 / §20.3 | Skills load from one pinned, vendored location | Steps 7–9 |
| L-04 (locked) | Every ticket names its skills; a name must resolve to **one** file | Step 7's `find` assertion |
| L-05 (locked) | Board seeded from these plans; the plan requires one list | Whole ticket |
| `AGENTS.md` § New Markdown placement | Documentation edits stay inside allowed roots | Step 11; only existing `docs/desktop/**` files are edited, no new `.md` |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (`sandbox_mode = "workspace-write"`).
- **Skills**, in load order:
  1. `pegasus-desktop` — `.agents/skills/project/pegasus-desktop/SKILL.md`
  2. `kanmer-plan`, `kanmer-execute` — `.grok/skills/<name>/SKILL.md` (Kanmer 0.1.0)

  **No upstream skill is needed to delete files; do not load one for form's sake.** In
  particular do not load `winui-session-report` "to check discovery" — it reads session
  transcripts and carries a privacy warning.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates confirmed by
  `get_doc_gates TOOL-004`: `leave-preparing` needs `plan` + `questions-resolved`;
  `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates TOOL-004` before
  every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

Refines the body's 12 steps in the same order.

1. **Orientation.** Read `EPIC-013/context.md` (`get_group_doc EPIC-013 context.md`), then
   the plan sections in the body's **Source of truth**. `get_doc_gates TOOL-004`, then
   `take_ticket`. Read [[TOOL-001]]'s (`DSK-12-01`) research verdict — it is the safety
   interlock. If its verdict says Codex reads `.codex/skills` and **not**
   `.agents/skills`, stop: deleting is then the wrong move and [[TOOL-002]] must be
   re-planned first.
2. **Prove the replacement exists.** `pwsh ./eng/skills/verify-skills.ps1` must exit 0.
   Then `ls .agents/skills/vendor/windows/` must show all eight: `winui-setup`,
   `winui-dev-workflow`, `winui-design`, `winui-code-review`, `winui-ui-testing`,
   `winui-packaging`, `winui-wpf-migration`, `winui-session-report`.
3. **Prove the payload survived, not just the Markdown.** The local tree measured
   2026-08-24 is 19 files / 7.9 MiB, and 98% of that weight is one file. Check:
   `ls -l .agents/skills/vendor/windows/winui-design/winui-search.exe` (expect
   **7,911,936 bytes**), `ls .agents/skills/vendor/windows/winui-dev-workflow/analyzer/`
   (expect `Microsoft.WindowsAppSDK.Analyzers.dll`, 49,664 bytes), and
   `ls -l .agents/skills/vendor/windows/winui-session-report/Analyze-Session.ps1` (expect
   45,966 bytes). If [[TOOL-002]] step 5 decided the binaries are **fetched on demand**
   rather than committed, that recorded decision replaces this check — read it, quote it,
   and confirm the fetch path works before deleting the only local copies.
   For reference the per-skill file counts in the tree being deleted are:
   `winui-design` 5, `winui-dev-workflow` 4, `winui-code-review` 2, `winui-packaging` 2,
   `winui-session-report` 2, `winui-setup` 1, `winui-ui-testing` 1, `winui-wpf-migration` 1
   — 18 files, plus the tracked `pegasus-release/SKILL.md` for 19 in the tree. The vendored
   tree should match, skill for skill.
4. **Prove the `pegasus-release` duplicate is genuinely a duplicate.**
   `Get-FileHash .agents/skills/pegasus-release/SKILL.md` and
   `Get-FileHash .codex/skills/pegasus-release/SKILL.md` must match. Both are 13,299 bytes
   / 275 lines as of 2026-08-24, so a mismatch would mean a hand edit landed in one copy
   only — if they differ, **stop**, reconcile the difference into
   `.agents/skills/pegasus-release/SKILL.md` first, and record what changed.
5. **Remove the tracked duplicate**: `git rm .codex/skills/pegasus-release/SKILL.md`.
   `.agents/skills/pegasus-release/SKILL.md` is the surviving copy — do not delete it.
   Note that `pegasus-release` is **not** in `eng/skills/skills.lock.json` (it is a
   repository-owned skill, not an upstream vendored one), so the verifier will not notice
   if the wrong copy goes.
6. **Remove the eight untracked WinUI folders from disk**: `rm -r .codex/skills/winui-*`.
   They are working-tree only, so this produces no git diff. Capture
   `git status --porcelain | grep skills` **before and after** so the proof shows the eight
   `??` entries disappearing — that output is the only evidence this half happened.
7. **Assert one list.** `find .codex/skills -name 'SKILL.md'` must return nothing, and
   `.codex/skills/` should be empty or gone. Then
   `find .agents/skills -name 'SKILL.md' | sort` — no skill name may appear twice. Expect
   37 results: 35 vendored + `pegasus-release` + `project/pegasus-desktop`.
8. **Fix every stale pointer. There are 23 of them across nine files, and the body's
   Guardrails now scope the ticket to exactly that set.** Measured 2026-08-24 with

   ```bash
   grep -rIn '\.codex/skills' --include='*.md' --include='*.toml' . \
     | grep -v '^\./\.codex/skills/'
   ```

   (`-n`, not `-l` — the count per file is the number that has to reconcile, and a
   file-name-only listing hides the two hits that share a file):

   | File | Hits | What it says today |
   | --- | --- | --- |
   | `docs/desktop/12-agent-tooling/README.md` | **10** (`:43`, `:46`, `:47`, `:53`, `:104`, `:115`, `:117`, `:118`, `:170`, `:228`) | § 2 evidence base, § 3 deviation, § 5 the DSK-12-04 row itself, § 7 "Discovery mismatch" trap |
   | `docs/desktop/02-architecture-and-foundation/README.md` | 3 (`:87`, `:88`, `:274`) | `winui-dev-workflow` / `winui-setup` / `BuildAndRun.ps1` paths |
   | `docs/desktop/09-release-update-and-distribution/README.md` | 3 (`:46`, `:81`, `:309`) | the `pegasus-release` byte-identical-copy note and two `winui-packaging` paths |
   | `docs/desktop/08-testing/README.md` | 2 (`:125`, `:277`) | vendored `winui-ui-testing` path |
   | `docs/desktop/README.md` | 1 (`:101`) | Routing legend: WinUI skills "vendored under `.codex/skills/` today" |
   | `docs/desktop/06-ui-design/README.md` | 1 (`:87`) | vendored `winui-design` path |
   | `docs/desktop/06-ui-design/keyboard-and-accessibility.md` | 1 (`:103`) | vendored `winui-ui-testing` path |
   | `docs/desktop/10-security-observability-performance/README.md` | 1 (`:88`) | vendored `winui-ui-testing` path |
   | `docs/desktop/12-agent-tooling/skill-routing.md` | 1 (`:14`) | § Pinned sources parenthetical |
   | **Total** | **23** | across **9** files |

   The per-file hits sum to 23 — check that they do before starting, because a table whose
   rows do not add up to its own total is how a sweep leaves references behind. Every hit is
   Markdown under `docs/desktop/`; **no `.codex/agents/*.toml` contains the string** (0 hits,
   step 9).

   Re-measure before editing: [[TOOL-002]] lands ahead of this ticket and its own
   **Documentation changes** already rewrite `12-agent-tooling/README.md` § 3 and
   `skill-routing.md` § Pinned sources, so the live number may be lower than 23 by the time
   this runs. Reconcile the difference explicitly rather than trusting either number.

   Then classify each hit and record the disposition:
   - **Live routing instruction** → rewrite to `.agents/skills/vendor/windows/<name>/`.
   - **Historical sentence** (§ 2's evidence base, § 3's deviation, § 7's "Discovery
     mismatch" trap at `:228`, the `09` note about two copies) → keep the fact as a **dated
     historical note** rather than erasing it.
   - **The § 5 plan row at `:170`** — this ticket's own row, "Remove duplicate skill copies
     under `.codex/skills/`" → **leave exactly as it stands.** Renaming a plan row to hide
     the path it names would make the plan set stop describing the work that was done.

   Re-run the grep afterwards and expect zero live routing instructions, with only dated
   history and `:170` remaining.
9. **Check the agent TOMLs.** Verified 2026-08-24: **no `.codex/agents/*.toml` contains the
   string `.codex/skills`** — `grep -rIn '\.codex/skills' --include='*.toml' .codex/agents/`
   returns 0 hits, and the eight TOMLs name skills by bare name and the project skill by its
   `.agents/skills/project/pegasus-desktop/SKILL.md` path only. So the acceptance criterion
   "no agent TOML still points at `.codex/skills`" is met by evidence, not by an edit.
   Re-run the grep to confirm nothing changed, and make no edit if it has not.
10. **Operator step** — restart Codex and run `/skills`; hand back the listing. Expected:
    every skill in `eng/skills/skills.lock.json` appears **once**, and no `winui-*` entry
    resolves from `.codex/skills`. If a skill vanished entirely, revert the deletion (the
    tracked one with `git`, the untracked eight by re-running
    `pwsh ./eng/skills/sync-skills.ps1`) and reopen [[TOOL-001]]'s verdict.
11. **Run the documentation gates.** `pwsh ./scripts/Test-DocumentationLinks.ps1` → expect
    `All relative Markdown links resolve (<n> files checked).`
    `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base with dev> -Head HEAD` →
    expect `Markdown placement passed for <base>..<head>.` (deletions and in-place edits
    are not checked by the placement gate, only additions and renames — so this gate is a
    formality here and the link checker is the one that can actually bite, since the nine
    edited files are all under `docs/`).
12. **Record the Appendix C evidence**: the before/after `find` and
    `git status --porcelain` output, the step 4 hash comparison, the `/skills` listing, the
    before/after grep counts, and the list of nine documents edited with the disposition of
    each of the 23 references.

## Verification

Evidence tier **1 — Static/build/architecture**, as the body states. Filesystem and
tool-listing evidence that exactly one copy of each skill remains and every name still
resolves. `proof` is a `command-log` plus the operator's `/skills` capture.

1. `find .codex/skills -name 'SKILL.md' | wc -l` → `0`.
2. `git ls-files .codex` → the eight `.codex/agents/*.toml` files and `.codex/config.toml`,
   and nothing under `.codex/skills`.
3. `grep -rIn '\.codex/skills' --include='*.md' docs/` → only dated historical sentences and
   the § 5 plan row at `docs/desktop/12-agent-tooling/README.md:170`; no live routing
   instruction. Report the count against the 23-hit / 9-file baseline.
4. `grep -rIn '\.codex/skills' --include='*.toml' .codex/agents/ | wc -l` → `0`, as it
   already is today.
5. `pwsh ./eng/skills/verify-skills.ps1` → exit 0 **after** the deletions.
6. `pwsh ./scripts/Test-DocumentationLinks.ps1` →
   `All relative Markdown links resolve (<n> files checked).`
7. The operator's `/skills` listing → each skill exactly once.

## Risks / open questions

| Risk | Mitigation |
| --- | --- |
| **Irreversible deletion of untracked content.** The eight `winui-*` folders are untracked; `git` cannot restore them, and the 7.9 MiB `winui-search.exe` is the expensive part. | Steps 2 and 3 are the interlock and must be run in that order. Recovery path if it goes wrong: re-run `pwsh ./eng/skills/sync-skills.ps1`, which needs network access to `microsoft/win-dev-skills` at `f1028dd5`. Say so in the report. |
| **CI will not catch a stale pointer inside `.codex/` or `.agents/`.** `scripts/Test-DocumentationLinks.ps1:14` excludes `^(node_modules\|corpus\|artifacts\|\.git\|\.claude\|\.agents\|\.codex\|\.kanmer)/`. | Step 8's grep is done by hand and its result recorded; do not rely on the `documentation` job here. |
| A sweep undercounts and leaves references pointing at a deleted directory. | The step 8 table gives per-file counts that sum to its own total (23), and step 12 records the before/after grep. This is the defect the earlier draft of this plan carried: it recorded 8 hits for `12-agent-tooling/README.md` when there are 10, so its rows summed to 21 against a stated 23. |
| [[TOOL-002]] moves two of the nine files first, so the live count differs from the baseline. | Step 8 re-measures before editing and reconciles the difference explicitly. |
| `pegasus-release` is not covered by the lockfile, so deleting the wrong copy is invisible to the verifier. | Step 4's hash comparison and step 5's explicit "the surviving copy is `.agents/skills/pegasus-release/SKILL.md`". |
| A skill disappears from `/skills` entirely after the deletion. | Step 10 is the detector and names the revert path; do not "fix" it by re-creating a `.codex/skills` copy — that reintroduces the duplicate. |

Open questions: **none opened, and not for want of a cheap price** — an unticked
`open-questions` box blocks `leave-preparing`, `enter-review` and `enter-done` (never
`leave-backlog`), so opening one would be affordable if there were a question. There is
not. Every decision in this ticket is settled by `docs/desktop/12-agent-tooling/README.md`
§ 3 and § 7 and by [[TOOL-001]]'s verdict, the scope boundary that used to disagree with
step 8 has been corrected in the body, and nothing in the body instructs that a question be
recorded in `open-questions/`.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. The branch deletes one
tracked file and edits nine documents; it is effectively docs-and-deletions, but it is not
purely documentation, so run the four lenses and record the dispositions rather than writing
`n/a — docs-only`._
