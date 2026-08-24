# Plan — TOOL-005 (plan handle `DSK-12-05`): Reconcile the `pegasus-desktop` project skill and add the `[agents]` table to `.codex/config.toml`

**Diff estimate: ~3 files, ~12 lines.** `.codex/config.toml` +5 (the four-key `[agents]`
table plus a blank line), `.agents/skills/project/pegasus-desktop/SKILL.md` +2 to +4 (the
two locked decisions found missing — see step 3), `AGENTS.md` +1 to +2 (the "agents load
the project skill first" sentence, **outside** the managed Kanmer block). A fourth file,
`docs/desktop/12-agent-tooling/subagents.md`, is edited **only** if step 6 finds drift; the
audit below says it will not.

## Approach

Treat this as **verify-and-reconcile, not create**. Verified 2026-08-24: the project skill
and all eight agent TOMLs already exist and are tracked, and every one of the eight already
mentions `pegasus-desktop`, already carries a never-self-delegate sentence, already has the
sandbox mode `subagents.md` § Roster specifies, and none hardcodes a `model` key. So the
only genuinely missing artefact is the four-line `[agents]` table in `.codex/config.toml`,
plus two locked-decision ids absent from the project skill. The alternative — writing the
project skill and the roster from `subagents.md` as if from scratch — is rejected on the
body's own terms: "a step that creates something already present is a defect", and a second
project skill or a ninth agent would break the one-list rule that
`docs/desktop/12-agent-tooling/README.md` § 7 makes a stop condition.

The second design choice is **committing the `[agents]` hunk alone**. `.codex/config.toml`
carries an uncommitted machine-local edit today (absolute `C:\Users\PC\...` paths in
`[mcp_servers.kanmer]`, shown by `git status --porcelain` as ` M`), so a whole-file `git add`
would push one workstation's paths into the repository. `git add -p` is not optional
housekeeping here; it is the guardrail.

## Governing docs

The ticket carries `refs: []` and **`docs_todo: true`**.

> **New ADR** — ADR-0110 (agent-skill pinning and the invocation protocol), authored by
> [[TOOL-008]] (plan handle `DSK-12-08`), filename
> `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md`. This plan is written to the
> decision as recorded in `docs/desktop/12-agent-tooling/README.md` § 3 (".codex/config.toml
> gains an `[agents]` table and, once verified, a disabled-by-default Azure MCP server
> entry") and to **L-04** as recorded in `docs/desktop/README.md` § Locked decisions. If
> ADR-0110 lands differently this plan is revised before implementation.

Programme-level authorities this plan meets:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal §20.3 | The project-local skill is the routing entry point, loaded first | Steps 2, 5 |
| Proposal §20.5 / §20.6 | Invocation and review protocols live in the project skill | Step 2 (read end to end; already present as § Invocation protocol and § Evidence format) |
| L-04 (locked) | Subagents exist as `.codex/agents/*.toml`; every ticket names its subagent, skills and MCP tools | Steps 4–7 |
| L-01/L-02/L-03/D-001/D-002/D-003/C-01 | Restated in the project skill so upstream skill guidance is overridden consistently | Step 3 |
| `AGENTS.md` § Repository task workflow | Reviewer is not the implementer | Routing block below |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
  (`sandbox_mode = "read-only"`, `model_reasoning_effort = "high"`). It **audits** the
  roster and the skill text; it cannot write, so the ticket owner makes the edits and
  transcribes the findings.
- **Skills**, in load order:
  1. `pegasus-desktop` — `.agents/skills/project/pegasus-desktop/SKILL.md`
  2. `kanmer-plan`, `kanmer-execute` — `.grok/skills/<name>/SKILL.md` (Kanmer 0.1.0)

  **Do not load `create-custom-agent`.** `docs/desktop/12-agent-tooling/skill-routing.md`
  § Not applicable rules it out (it targets the VS Code `.agent.md` format, not Codex TOML),
  and `EPIC-013/context.md` records that where the per-area index and the do-not-load table
  disagree, **the do-not-load table wins**.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates confirmed by
  `get_doc_gates TOOL-005`: `leave-preparing` needs `plan` + `questions-resolved`;
  `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates TOOL-005` before
  every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

Refines the body's 12 steps in the same order. Steps 2, 4, 5, 6 and 9 were pre-audited on
2026-08-24 and the measured results are given so the implementer can confirm rather than
discover.

1. **Orientation.** Read `EPIC-013/context.md` (`get_group_doc EPIC-013 context.md`), then
   the plan sections in the body's **Source of truth** — in particular
   `docs/desktop/12-agent-tooling/subagents.md` § Roster and § `.codex/config.toml`
   additions. `get_doc_gates TOOL-005`, then `take_ticket`.
2. **Verify, do not create.** `test -f .agents/skills/project/pegasus-desktop/SKILL.md` —
   confirmed present and tracked 2026-08-24, 110 lines, frontmatter `name: pegasus-desktop`,
   sections: Locked decisions, Dependency boundaries, UI and accessibility conventions,
   Invocation protocol, Evidence format (Appendix C), Next skill to load. Read it end to
   end. **Nothing in this ticket may create a second project skill.**
3. **Reconcile the Locked-decisions section — there is real drift, and here it is.**
   Measured 2026-08-24 (`grep -n 'L-0[1-5]\|D-00[1-3]\|C-01' .agents/skills/project/pegasus-desktop/SKILL.md`):
   present are **L-01** (`:23`), **L-02** (`:34`), **L-03** (`:37`), **D-001** (`:42`),
   **D-003** (`:44`), **C-01** (`:45`), **D-002** (`:48`). **Absent are L-04 and L-05.**
   Add them, worded the same way `docs/desktop/README.md` § Locked decisions words them:
   - L-04 — specialist Codex subagents exist as `.codex/agents/*.toml`; every ticket names
     its subagent, skills and MCP tools.
   - L-05 — the Kanmer board is seeded by the implementing agent from the ticket tables in
     these plans; the open upstream board is triaged in area 01.
   Fix drift **in the skill, not in the plan set**. Note the forward dependency, which is
   not this ticket's work: operator decision **D-004** (OPS-10 acceptance folds into the
   desktop pilot approval) is owned by plan 09 and is recorded in
   `docs/desktop/README.md` § Locked decisions by `DSK-09-11`; when it lands there, the
   project skill needs the same one-line reconcile. Do not add it before it is in the README.
4. **Verify the roster.** `ls .codex/agents` — confirmed 2026-08-24 to return exactly the
   eight tracked TOMLs: `pegasus-azure-auditor`, `pegasus-desktop-reviewer`,
   `pegasus-gateway-dev`, `pegasus-parity-researcher`, `pegasus-release-packager`,
   `pegasus-test-engineer`, `pegasus-ui-verifier`, `winui-dev`. If one is missing, restore
   it verbatim from its section of `docs/desktop/12-agent-tooling/subagents.md`.
5. **Verify every agent loads the project skill first.**
   `grep -c 'pegasus-desktop' .codex/agents/*.toml` — measured 2026-08-24: azure-auditor 1,
   desktop-reviewer 3, gateway-dev 2, parity-researcher 2, release-packager 1,
   test-engineer 2, ui-verifier 1, winui-dev 1. **All eight ≥ 1**, and in each the reference
   is the step `0.` line naming `.agents/skills/project/pegasus-desktop/SKILL.md`. Confirm
   the *position* (step `0.`), not just the count; add the line to any TOML that lacks it,
   copying the wording from `winui-dev.toml`.
6. **Verify sandbox and effort fields.** Measured 2026-08-24, all match
   `subagents.md` § Roster:
   `pegasus-parity-researcher` `read-only`/`medium`; `pegasus-desktop-reviewer`
   `read-only`/`high`; `pegasus-azure-auditor` `read-only`/`medium`;
   `pegasus-gateway-dev` `workspace-write`/`high`; `pegasus-test-engineer`
   `workspace-write`/`high`; `pegasus-release-packager` `workspace-write`/`high`;
   `pegasus-ui-verifier` `workspace-write`/`medium`; `winui-dev` sets **neither** field,
   inheriting the upstream default as the roster specifies. **No TOML hardcodes a `model`
   key** — keep it that way; models are deliberately not pinned.
   Also confirm each still carries its never-delegate-to-your-own-kind sentence: measured
   2026-08-24, all eight do.
7. **Add the `[agents]` table** to `.codex/config.toml`, exactly as
   `docs/desktop/12-agent-tooling/subagents.md` § `.codex/config.toml` additions gives it:

   ```toml
   [agents]
   enabled = true
   max_concurrent_threads_per_session = 4
   default_subagent_reasoning_effort = "medium"
   interrupt_message = true
   ```

   Today the file is 15 lines: `[features]` (`:1`), `[mcp_servers.mcp_microsoftdocs]`
   (`:5`), `[mcp_servers.kanmer]` (`:9`), `[mcp_servers.kanmer.env]` (`:13`). Leave the
   commented Azure MCP block from that same `subagents.md` section in place **as a
   comment**; enabling it is [[TOOL-006]]'s (`DSK-12-06`) work and must not happen here.
8. **Stage only that hunk.** `git add -p .codex/config.toml` and commit the `[agents]` hunk
   alone. The `[mcp_servers.kanmer]` lines in the working tree contain absolute
   `C:\Users\PC\...` paths and must not be pushed. Confirm with
   `git diff --cached -- .codex/config.toml` before committing.
9. **Parse-check all nine TOML files** so a syntax error is caught before the roster
   silently fails to load:

   ```
   python -c "import tomllib, sys; tomllib.load(open(sys.argv[1], 'rb'))" <file>
   ```

   Run it for `.codex/config.toml` and each of the eight `.codex/agents/*.toml`. Expected:
   no output, exit 0, nine times. (`tomllib` is standard from Python 3.11; if the
   workstation's Python is older, use `python -c "import tomli, sys; tomli.load(...)"` or
   any TOML parser and record which was used.)
10. **Operator step** — restart Codex at the repository root and run `/agent`; hand back
    the roster listing. Expected: the eight names. Record whether it differs from the
    "before" state [[TOOL-001]] (`DSK-12-01`) captured — that comparison is the only thing
    that proves the `[agents]` table changed anything.
11. **Record which optional fields the installed build honours.** If it ignores
    `model_reasoning_effort` or `sandbox_mode`, write that down plainly: the read-only
    guarantee for `pegasus-parity-researcher`, `pegasus-desktop-reviewer` and
    `pegasus-azure-auditor` then rests on the `developer_instructions` prose alone. That is
    the case [[TOOL-001]]'s "Open question to carry" guardrail routes here, and it is
    material to [[TOOL-006]], whose Azure read-only guarantee has no per-tool permission
    behind it either. If it is unenforced, open it as a **blocking** `open-questions` item
    on this ticket at that point — not now, because today it is unobserved, not unresolved.
12. **Record the Appendix C evidence**: the reconciliation diff (what drifted, what was
    fixed — expect L-04 and L-05), the nine parse checks, the `/agent` output, and the
    honoured-fields finding.

**`AGENTS.md` edit (plan § 8 documentation change).** Add the sentence that agents load the
project skill first and that the lockfile governs skill revisions, **outside** the
`<!-- kanmer:instructions:start … --> … <!-- kanmer:instructions:end -->` block, which spans
`AGENTS.md:1-22` and is overwritten by `kanmer-setup`. The repository-instructions body
starts immediately after it at `AGENTS.md:24` (`# Pegasus repository instructions`).

## Verification

Evidence tier **1 — Static/build/architecture**, as the body states. Parse-check output and
a tool listing showing the roster loads; it proves nothing about agent *behaviour*, which is
[[TOOL-009]]'s (`DSK-12-09`) job. `proof` is a `command-log` plus the operator's `/agent`
capture.

1. `python -c "import tomllib, sys; tomllib.load(open(sys.argv[1], 'rb'))" .codex/config.toml`
   → exit 0, no output. Repeat for each of the eight agent TOMLs — nine clean runs.
2. `grep -n '^\[agents\]' -A 5 .codex/config.toml` → the four keys `enabled`,
   `max_concurrent_threads_per_session`, `default_subagent_reasoning_effort`,
   `interrupt_message`.
3. `grep -L 'pegasus-desktop' .codex/agents/*.toml` → **no output** (every TOML mentions the
   project skill).
4. `git diff --stat origin/dev...HEAD -- .codex/config.toml` → only the `[agents]` addition;
   **no `mcp_servers.kanmer` lines**.
5. `grep -n 'L-04\|L-05' .agents/skills/project/pegasus-desktop/SKILL.md` → both present
   after the reconcile.
6. The recorded `/agent` output → the eight roster names.

## Risks / open questions

| Risk | Mitigation |
| --- | --- |
| A whole-file `git add` pushes one workstation's absolute paths into `.codex/config.toml`. | Step 8: `git add -p`, then `git diff --cached` before committing; verification item 4 catches it at review. |
| The installed build silently ignores `sandbox_mode`, so "read-only" agents are only read-only by convention. | Step 11 records it; if unenforced it becomes a blocking `open-questions` item on this ticket and is material evidence for [[TOOL-006]]'s guardrail sentence. |
| Editing inside the `AGENTS.md` managed Kanmer block (`:1-22`) — `kanmer-setup` overwrites it. | The `AGENTS.md` note above pins the insertion point at or after `:24`. |
| Creating what already exists (a second project skill, a ninth agent). | Steps 2 and 4 are stated as verifications with the measured "already present" result, so the defect shape is visible before it happens. |
| `docs/desktop/12-agent-tooling/subagents.md` and `.codex/agents/` diverge. | Step 6's audit found no divergence on 2026-08-24. If a future run does, **`.codex/agents/` is the source of truth** — correct the document, not the file. |
| D-004 is not yet in `docs/desktop/README.md` § Locked decisions, so it is not reconciled into the project skill here. | Recorded in step 3 as a forward dependency owned by plan 09 / `DSK-09-11`. It is a **decided** operator decision, not an open question, and must not be re-opened. |

Open questions: **none opened now.** The one candidate (does the build honour
`sandbox_mode`?) is an observation the ticket makes in step 11, not a question blocking its
start; opening it today would block every stage move on a ticket whose own step answers it.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. The diff is ~12 lines
of configuration and documentation; record the four lenses' dispositions honestly rather
than writing `n/a — docs-only`, since `.codex/config.toml` is configuration, not
documentation._
