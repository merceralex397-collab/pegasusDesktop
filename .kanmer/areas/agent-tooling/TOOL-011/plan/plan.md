# Plan — TOOL-011 (plan handle `DSK-12-11`): Decide and record Claude Code parity for the agent roster

**Diff estimate: ~1 file, ~2 lines if the decision is "not needed"; ~11 files, ~330 lines if
the roster is mirrored.** The deliverable is a *recorded decision*, so the diff is
branch-dependent and both figures are given rather than one averaged number.

- **Not needed**: `docs/desktop/12-agent-tooling/subagents.md` + one dated sentence.
- **Mirrored**: eight agent files under `.claude/agents/` (the eight `.codex/agents/*.toml`
  bodies run 20–45 lines each, so ~300 lines of instructions carried across), `.gitignore`
  +2 lines (see step 5 — it is two lines, not one), and
  `docs/desktop/12-agent-tooling/subagents.md` +3 lines for the dated decision and the
  source-of-truth sentence.

## Approach

**Ask the operator, then decide once and date it.** The plan marks the mirror *optional*, so
the failure mode is not "wrong answer" but "no answer" — an undecided row gets re-litigated
in every later ticket that touches an agent. The body's own step 2 offers a clean early exit
("If the answer is no, jump to step 9 and record 'not needed' — that is a complete and
acceptable outcome for this row"), and this plan takes that seriously: the shortest correct
path is a two-line dated sentence.

The alternative considered and rejected is **mirroring first and asking later**. Two rosters
maintained in parallel is two lists for one concept, which
`docs/desktop/12-agent-tooling/README.md` § 7 makes a stop condition; and because
`/.claude/` is gitignored, an unasked-for mirror would be invisible to review and absent
from every fresh clone — a roster nobody can see is worse than no roster, because it looks
like a control.

The repository evidence below means the operator question is **sharper than the body
assumes**: Claude Code is demonstrably configured for this repository today. That does not
pre-empt the decision (the question is also "is it expected after cutover?"), but the
implementer should put the evidence in front of the operator rather than asking an open
question.

## Governing docs

The ticket carries `refs: []` and **`docs_todo: true`**.

> **New ADR** — ADR-0110 (agent-skill pinning and the invocation/review protocol), authored
> by [[TOOL-008]] (plan handle `DSK-12-08`), filename
> `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md`. This plan is written to
> **L-04** as recorded in `docs/desktop/README.md` § Locked decisions and to
> `docs/desktop/12-agent-tooling/README.md` § 5 row `DSK-12-11` ("If the team also runs
> Claude Code, the same roster exists with equivalent tool restrictions; otherwise recorded
> as not needed"). If ADR-0110 lands differently this plan is revised before implementation.
> **This ticket does not author an ADR** — a tool-parity choice is a routing decision, not a
> durable architectural one, and its home is `subagents.md` with a date.

Programme-level authorities this plan meets:

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-04 (locked) | Subagents exist as `.codex/agents/*.toml`; every ticket names its subagent, skills and MCP tools | Steps 2, 6–9 — a second tool without a roster is a gap in the lock, and this ticket closes it or explicitly accepts it |
| `docs/desktop/12-agent-tooling/subagents.md` (opening) | `.codex/agents/` is the source of truth if the files and the document differ | Step 8 extends that sentence to cover both tools |
| `docs/desktop/12-agent-tooling/README.md` § 7 | Read-only agents cannot write; the caller captures their output | Step 10 |
| `AGENTS.md` § New Markdown placement | No `.md` outside the allowed roots | Step 11 — note `.claude/` is **not** an allowed Markdown root, which matters if the mirror format is Markdown (step 4) |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
  (`sandbox_mode = "read-only"`, `model_reasoning_effort = "high"`). It audits any mirrored
  roster against the Codex originals; it cannot write, so the owner transcribes.
- **Skills**, in load order:
  1. `pegasus-desktop` — `.agents/skills/project/pegasus-desktop/SKILL.md`
  2. `kanmer-plan`, `kanmer-execute` — `.grok/skills/<name>/SKILL.md`

  **Do not load `create-custom-agent`.** It is on the do-not-load table in
  `docs/desktop/12-agent-tooling/skill-routing.md` because it targets the VS Code
  `.agent.md` format — which is neither Codex TOML nor the Claude Code format, so it would
  actively mislead here. `EPIC-013/context.md` records that the do-not-load table wins over
  the per-area index, which lists it as "reference only" for area 12.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates confirmed by
  `get_doc_gates TOOL-011`: `leave-preparing` needs `plan` + `questions-resolved`;
  `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates TOOL-011` before
  every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

Refines the body's 12 steps in the same order.

1. **Orientation.** Read `EPIC-013/context.md`, then the plan sections in the body's
   **Source of truth**. `get_doc_gates TOOL-011`, then `take_ticket`. Confirm
   [[TOOL-005]] (`DSK-12-05`) landed — mirroring a roster that is itself unreconciled copies
   the drift.
2. **Operator step — ask, with the evidence in hand.** The question is two-part: *is Claude
   Code used on this repository for conversion work alongside Codex, and is it expected to be
   after cutover?* Put these verified facts in front of the operator rather than asking cold
   (all measured 2026-08-24):
   - `CLAUDE.md` is **tracked** and contains a single line, `AGENTS.md` — a deliberate
     pointer making the repository instructions apply to Claude Code.
   - `.mcp.json` is **tracked** and carries the Kanmer stdio server, the same one
     `.codex/config.toml:9` wires for Codex.
   - `.claude/` exists on this workstation and contains `settings.local.json`.
   - `.grok/skills/` (12 Kanmer skills, `.kanmer-skills-version` `0.1.0`) is tracked and is
     surfaced to Claude Code as the `kanmer:` plugin.

   So "is it used?" is very likely **yes**; the operator's real decision is whether the
   roster must exist there too. Record the answer **verbatim with its date**. If the answer
   is no, jump to step 9 — that is a complete outcome.
3. **Read [[TOOL-001]]'s (`DSK-12-01`) research verdict**, step 9 specifically: which of
   `.agents/skills`, `.codex/skills` and `.grok/skills` each tool discovers. A mirror is only
   worth building if Claude Code can reach the project skill
   (`.agents/skills/project/pegasus-desktop/SKILL.md`) and the vendored skills the roster
   names — a roster whose step `0.` points at a file the tool cannot load is decoration.
4. **Establish the target format before writing a single file. Do not guess key names.**
   Codex agents are TOML under `.codex/agents/`; Claude Code agents are a **different
   format** under `.claude/agents/` — Markdown with YAML frontmatter rather than TOML — and
   the frontmatter key set and the tool-restriction field differ from Codex's `sandbox_mode`.
   Confirm the current keys against the installed build's own documentation or its agent
   listing, and record what you read and when. An agent file with invented keys either fails
   to load or, far worse, **loads without its restrictions** — a read-only reviewer that is
   silently read-write is the failure this step exists to prevent.
   If the format is Markdown, note that `.claude/` is **not** an allowed Markdown root in
   `scripts/Test-MarkdownPlacement.ps1:31`; that only matters if the files are tracked
   (step 5 option (a)), so resolve step 5 before worrying about it, and if tracking wins,
   record whether the placement gate needs `.claude/agents` added — that would be a change to
   `scripts/Test-MarkdownPlacement.ps1` and `scripts/Test-TestMarkdownPlacement.ps1`, which
   is **outside this ticket's scope boundary** and would need its own ticket.
5. **Record the blocking fact, and decide.** `.gitignore:23` is `/.claude/` (verified: line
   22 is the comment `# Tool and editor working state`, line 23 is `/.claude/`), so anything
   under `.claude/agents/` is **untracked** — invisible in review and absent from a fresh
   clone. Options:
   - **(a) narrow negation so the roster is tracked while local settings stay ignored.**
     **This is two lines, not one, and the one-line form silently does not work:** git does
     not descend into an excluded directory, so `!/.claude/agents/` on its own re-includes
     nothing. The working form excludes the directory's *contents* and then re-includes the
     roster:

     ```gitignore
     /.claude/*
     !/.claude/agents/
     ```

     Verify with `git check-ignore -v .claude/agents/<one file>` → **no output** when it
     works. The body's own verification step expects exactly that.
   - **(b) keep the mirror untracked and generate it on demand** from the eight TOMLs — no
     `.gitignore` change, but no review and no fresh-clone parity either, and a generator is
     a new script this ticket is not scoped to write.
   - **(c) do not mirror** — the "not needed" outcome.

   Write the choice and its reason in this plan under a dated heading. **Recommended if
   mirroring at all: (a)**, because the whole justification for a mirror is that the second
   tool's sessions get the same bounded roster, and an untracked roster does not reach
   anyone else's machine.
6. **If mirroring**: one file per agent carrying the same `description` and the same
   developer instructions, mapping the sandbox intent to the target tool's mechanism.
   `pegasus-parity-researcher`, `pegasus-desktop-reviewer` and `pegasus-azure-auditor` must
   end up with a **read-only tool set**; the other five get write access scoped as
   `subagents.md` § Roster describes (verified 2026-08-24: `pegasus-gateway-dev`,
   `pegasus-test-engineer`, `pegasus-release-packager`, `pegasus-ui-verifier` are
   `workspace-write`; `winui-dev` sets no override and inherits).
7. **If mirroring**: every mirrored agent keeps step `0.` — load
   `.agents/skills/project/pegasus-desktop/SKILL.md` first — and keeps its "never delegate to
   an agent of your own kind" sentence. Verified 2026-08-24: all eight Codex TOMLs carry
   both. These two rules are what make the roster safe, not decoration.
8. **Keep one source of truth.** `docs/desktop/12-agent-tooling/subagents.md` already says
   `.codex/agents/` is authoritative if the files and the document differ. Extend that
   sentence to say whether the Claude Code roster is **generated** from those TOMLs or
   **maintained in parallel** — and if in parallel, that a change must be applied to both in
   the same PR.
9. **Record the decision either way, with a date**, in
   `docs/desktop/12-agent-tooling/subagents.md`: "mirrored on `<date>`, source of truth
   `.codex/agents/`, tracked via `<gitignore rule>`" or "not needed on `<date>`: Claude Code
   is not used for conversion work". **An undated "optional" line is not a decision.**
10. **If a mirror was built**: have `pegasus-desktop-reviewer` compare each mirrored file
    against its TOML original and report any divergence in description, instructions or
    restriction. Transcribe the findings — it cannot write.
11. **Verify.** Either both tools list the roster (attach both listings), or the dated
    "not needed" line exists. Then `pwsh ./scripts/Test-DocumentationLinks.ps1` and, if any
    `.md` was added anywhere tracked,
    `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base with dev> -Head HEAD`.
12. **Record the Appendix C evidence**: the operator answer verbatim with its date, the
    step 4 format check with what was read and when, the `.gitignore` decision, and the
    roster listings or the not-needed line.

## Verification

Evidence tier **1 — Static/build/architecture**, as the body states. A recorded decision,
plus — if a mirror was built — both roster listings and the tracked/untracked status. It
makes no claim that either roster *behaves* correctly; that is [[TOOL-009]]'s (`DSK-12-09`)
territory. `proof` is a `command-log`.

1. `grep -n 'Claude Code' docs/desktop/12-agent-tooling/subagents.md` → a dated decision
   line, whichever way it went.
2. If mirrored: `ls .claude/agents` → eight files whose names match `ls .codex/agents` one
   for one.
3. If mirrored: `git check-ignore -v .claude/agents/<one file>` → **no output** if option (a)
   was chosen and the two-line negation works; a `.gitignore:23` match if the roster is
   deliberately untracked (option (b)).
4. If mirrored: the recorded roster listing from each tool → the same eight names in both.
5. `pwsh ./scripts/Test-DocumentationLinks.ps1` →
   `All relative Markdown links resolve (<n> files checked).`

## Risks / open questions

| Risk | Mitigation |
| --- | --- |
| **Guessing the target tool's agent schema produces a file that loads without its restrictions** — a read-only reviewer silently able to write. | Step 4: read the installed build's documentation, record what was read and when; never write a key you have not seen documented. |
| **A one-line `!/.claude/agents/` negation silently does nothing** because git does not descend into an excluded directory. | Step 5 gives the working two-line form and the `git check-ignore -v` proof. |
| An unreviewed, gitignored mirror looks like a control but reaches nobody. | Step 5 makes tracking a recorded decision; step 9 records the tracking mechanism in the dated line. |
| **Two rosters is two lists for one concept** — a stop condition. | Step 8's source-of-truth sentence, and the preference for generation over parallel maintenance. |
| The mirror drifts after a Codex TOML changes. | Step 8 requires "same PR" if maintained in parallel; step 10's reviewer comparison is the detector. |
| Mirroring a roster that is itself unreconciled. | Step 1 requires [[TOOL-005]] to have landed. |
| `.claude/` is not an allowed Markdown root, so tracked Markdown agent files could fail the CI `documentation` job. | Step 4 flags it and routes the placement-gate change to its own ticket — editing `scripts/Test-MarkdownPlacement.ps1` is outside this ticket's scope boundary. |

Open questions: **none opened as a blocking document.** The operator answer in step 2 is an
input the ticket's own first step gathers, not an unresolved question blocking the board —
and opening it would block every stage move on a ticket whose step 2 is precisely to ask it.
The `.gitignore` and format questions both have recommended defaults with their reasons in
steps 4 and 5.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. If the outcome is the
dated "not needed" line the branch is documentation-only and the honest record is
`n/a — docs-only` with the date; if a mirror was built the branch carries eight agent files
and a `.gitignore` change, so run the four lenses and record the dispositions instead._
