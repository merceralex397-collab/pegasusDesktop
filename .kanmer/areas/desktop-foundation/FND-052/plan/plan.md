# Plan — FND-052: groom the seeded board

**Diff estimate: 22 Kanmer ticket bodies, ~49 edited lines. No repository file
changes at all.**

Derived from the measured inventory below, not asserted. The unit here is a
ticket body under `.worktrees/kanmer/.kanmer/areas/<area>/<ID>/<ID>.md`, edited
through `update_item`; `docs/engineering.md:201-203` § Plan sizing still applies
and the number is real.

## Measured file-and-line inventory

Profile `chore` owes no `research` and no `files` document, so this plan
carries the surface-area burden alone —
`.grok/skills/kanmer-plan/assets/plan-template.md`'s "written FROM the ticket's
`research` and `files` documents" precondition does not apply to `chore`. Every
row below was measured against the board store on 2026-08-24 with `grep -n`,
`grep -c` and `sed -n`, reading **ticket bodies only** (`<ID>.md`), never the
`plan/` or `files/` documents beside them.

### A · The Markdown placement gate — 16 actual command call sites, 11 bodies

| Body | Call sites (bare) | Line numbers |
| --- | --- | --- |
| `DUI-013` | 2 | `:73` (step), `:89` (verification) |
| `DUI-017` | 2 | `:129`, `:145` |
| `FEAT-025` | 1 | `:81` |
| `FEAT-038` | 0 | Its `Test-TestMarkdownPlacement.ps1` regression self-test is not a gate invocation; leave it unchanged. |
| `FEAT-043` | 2 | `:72`, `:91` |
| `FND-014` | 1 | `:102` |
| `FND-015` | 1 | `:88` |
| `FND-019` | 1 | `:98` |
| `FND-020` | 1 | `:96` |
| `FND-023` | 2 | `:76`, and `:154` inside the embedded `DSK-01-13` specification |
| `FND-042` | 2 | `:80`, `:97` |

**16 invocations.** Ten of them are `## Verification` lines, exactly as the body
says. The four known path references — `DUI-013:47`, `DUI-017:95`,
`DUI-017:171`, and `FEAT-025:41` — are *path references* in Source-of-truth
and evidence prose, not commands. **Leave those four alone**; adding arguments
to a path reference is noise. The separate `FEAT-038` self-test is also
intentionally unchanged.

The gate itself: `scripts/Test-MarkdownPlacement.ps1:2-6` declares
`[Parameter(Mandatory)][string] $Base` and `[Parameter(Mandatory)][string]
$Head`, and `:81` prints `Markdown placement passed for $Base..$Head.` on
success. A bare invocation prompts or fails and checks nothing.

### B · `-VerifyPartition` — 4 call sites, 3 bodies

| Body | Line | Current text |
| --- | --- | --- |
| `FND-046` | `:91` | `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition` |
| `PLAT-002` | `:75` | `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition` (step) |
| `PLAT-006` | `:71` | same (step) |
| `PLAT-006` | `:87` | same (verification) |

The working form to copy is `TEST-003:68` and `TEST-003:84`:
`pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition -ArtifactRoot ./artifacts/test-shards -ShardCount 3`.
Why it is needed: `scripts/Invoke-TestShard.ps1:35-36` declares
`[Parameter(Mandatory)] [int] $ShardCount` **with no `ParameterSetName`**, so
it is mandatory in the `Verify` set too — and the script's own worked example
at `:20` is the form above. `[CmdletBinding(DefaultParameterSetName = 'Run')]`
at `:23` means a bare call does not even land in the `Verify` set cleanly.

### C · The placeholder — 1 site, 1 body

`PLAT-002:95` reads `pwsh ./scripts/Invoke-ProductionSmoke.ps1 …` with a
literal ellipsis. The real invocation is in the same body at `:79`:
`-BaseUri <production base uri> -ExpectedSourceRevision <40-hex sha>
-ExpectedVersion <version> -ResourceGroupName …`. Copy from `:79` into `:95`;
do not re-derive it from the script.

### D · Ambiguous ids — 19 occurrences, 7 bodies

| Body | Count | What it means |
| --- | --- | --- |
| `FEAT-013` | 2 | Both mean **upstream `INTK-001`**, which is absorbed and has no fork ticket. Board `INTK-001` is upstream `INTK-002`, a live import (`HZN-001` / `board-conventions.md` join table). |
| `FEAT-003` | 1 | `(upstream CASE-009 / upstream CASE-002)`. Board `CASE-002` is upstream `CASE-022`, a live import — this is the one that can point at the wrong live ticket. |
| `FEAT-043` | 2 | — |
| `FEAT-019` | 2 | Repeat the word "upstream" **per id** in the list; one leading "upstream" does not carry across a comma-separated run. |
| `DOCS-001` | — | 9 occurrences of the string in the body, some inside its `### Upstream ticket <ID> (verbatim)` block (1 such block present) |
| `DOCS-003` | — | 13 occurrences, likewise with 1 verbatim block |
| `FND-004` | — | 1 occurrence, **no** verbatim block |

The first four rows are 7 occurrences and are the whole of the operational
risk. The `DOCS-001` group is the remaining 12 **outside** the verbatim blocks
— and the raw string counts above (9 + 13 + 1 = 23) are **not** the edit count,
because the guardrail forbids touching ids inside a
`### Upstream ticket <ID> (verbatim)` block. Count the outside-the-block
occurrences at execution; the body's figure of 12 is the sweep's number and is
the target.

### E · Dangling/namespace wiki-links — 6 sites, 1 body

`REL-007:61` — step 1's closing clause reads "…do not re-open Artifact Signing
or an OV certificate, whose spikes `[[DSK-09-07]]` and `[[DSK-09-09]]` were
withdrawn." Both handles are withdrawn and no ticket carries them. Confirmed by
`search_items`: neither resolves.

### F · `REL-013`'s missing real validator — 2 sites, 1 body

`REL-013:67` (step 13) and `REL-013:84` (verification) both call
`pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — the **regression test of**
the placement script, which is what `.github/workflows/ci.yml:82-84` runs. It
proves the validator works; it never inspects `REL-013`'s own new file
`docs/desktop/09-release-update-and-distribution/first-install.md`. Body step 4
adds the real validator **beside** it, not instead of it.

### Summary

| Group | Bodies | Edits |
| --- | --- | --- |
| A placement gate | 11 | 16 |
| B `-VerifyPartition` | 3 (`FND-046`, `PLAT-002`, `PLAT-006`) | 4 |
| C placeholder | 1 (`PLAT-002`, already counted) | 1 |
| D ambiguous ids | 7 (`FEAT-043` already counted) | 19 |
| E dangling/namespace links | 1 | 6 |
| F `REL-013` validator | 1 | 2 |
| **Distinct** | **22** | **49** |

### Measured and deliberately not touched

| Target | Measured now | Why not |
| --- | --- | --- |
| The **77 correct bare occurrences** | Includes `FND-022`'s 28, of which 23 sit inside its collision Traps paragraph — the paragraph that exists to *state* the rule | Guardrail (a). Qualifying an id inside a sentence teaching the collision destroys the lesson. |
| The **~515 bare upstream-only ids** across ~83 bodies | No board ticket holds those numbers | Guardrail (b). None can mis-route a reader; the cost is 83 bodies against 6. |
| The **108 tickets** whose verification names a file that does not exist yet | Every one of those paths is created by a **named** ticket on this board | Guardrail (c), restated in the acceptance criteria: "A line naming a file a named earlier ticket creates is not a failure — that is the conversion working as designed." |
| Anything inside a `### Upstream ticket <ID> (verbatim)` block | `DOCS-001` and `DOCS-003` each carry one | `board-conventions.md` § "Where a bare upstream id is still correct": that text is a quotation and its ids are upstream ids by definition. **Never "fix" ids inside a verbatim block.** |
| Any repository file | — | Body § Documentation changes: "None. This ticket changes Kanmer ticket bodies only." |
| `blocks`, `labels`, `groups`, `area`, `profile`, `refs`, stage | — | Scope boundary. `update_item` on the body text only; no `move_item` on another ticket. |

## Approach

**One spelling per gate, applied everywhere, decided by reading the script's
own parameter block first.** Every item in this ticket is the same shape: a
command that cannot run, or an id whose namespace is not stated. Both are fixed
by making the text match a fact that already exists — the script's `param()`
block, or the join table in `HZN-001` / `board-conventions.md`. Nothing here is
a judgement call, which is why it is a `chore` and why the acceptance criteria
are greppable.

The alternative rejected is **an eighth repair round over the whole board**.
The seeding audits already closed every defect that could make an agent act on
the wrong ticket — `FND-022` step 7's "Drop `CASE-001`" being the one that
mattered, where board `CASE-001` is a live imported production defect blocking
four tickets. What is left cannot misdirect anyone: an agent reaching a broken
verification command gets a parameter prompt or an error, notices, and stops.
That is a **broken gate rather than a wrong action**, so it is recorded and
owned in one ticket instead of triggering another sweep of 208.

The second alternative rejected is **qualifying every bare id on the board**.
Guardrail (b) measures the trade directly: ~515 upstream-only ids across ~83
bodies, none of which collides with a board id, against the 19 that do. Doing
all of them would be 83 bodies of churn to fix nothing, and would bury the 19
that matter in the diff.

## Execution placement

**Omitted — this ticket places no responsibility anywhere.** It is pure board
work: it edits Kanmer ticket bodies and changes no code, no configuration, no
credential and no artefact, so the six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md:166-178` has nothing to
answer about. The one placement it **assumes** is the board itself: the Kanmer
store lives in this repository at `.worktrees/kanmer/.kanmer` on the
`kanmer-board` branch (`00/README.md` § Kanmer (fork)) and is reached through
the Kanmer MCP from the workstation — desktop-side, with no server component
and no Azure resource.

## Governing docs

The ticket's `refs` list is **empty** and `get_doc_gates FND-052` reports
`docs_todo: true`. No PRD, FRD or ADR is claimed to be met, and **none should
be**: this ticket produces no durable technical decision, so under
`AGENTS.md` § New Markdown placement it is a commit message's worth of change,
not an ADR's.

**This ticket has no plan row and no `DSK-` handle.** It originates from the
board-seeding audit, not from `docs/desktop/`, and its title carries the
`BOARD ·` prefix for that reason. It does **not** add a plan row — unlike
[[FND-051]] (plan handle `DSK-01-13`), which does, because that one is
conversion work that belongs in a plan area. Board hygiene does not.

### Programme-level authorities that bind today

| Authority | Requirement | Met by |
| --- | --- | --- |
| `HZN-001` / `board-conventions.md` § "Upstream ids versus board ids" | "A bare `<PREFIX>-<nnn>` anywhere in a ticket body, document or checklist is a **fork board id**. An upstream id is **never** written bare — it is always `upstream <ID>`, and where both are meant, `upstream <ID> (board [[<board-id>]])`." | Steps 7–8 |
| `HZN-001` / `board-conventions.md` § the join table | The 19 imports, with `DOCS-001` singled out as "the trap in this table" because board and upstream numbers coincide there by coincidence | Step 8 |
| `HZN-001` / `board-conventions.md` § "Where a bare upstream id is still correct" | Inside a `### Upstream ticket <ID> (verbatim)` block; "Never 'fix' ids inside a verbatim block" | The not-touched table; step 10's sweep excludes them |
| `scripts/Test-MarkdownPlacement.ps1:2-6` | `-Base` and `-Head` are both `[Parameter(Mandatory)]` | Steps 2–3 |
| `scripts/Invoke-TestShard.ps1:23`, `:35-36`, `:20` | `DefaultParameterSetName = 'Run'`; `-ShardCount` mandatory with **no** `ParameterSetName`; the worked example is the form to copy | Step 5 |
| `scripts/Invoke-ProductionSmoke.ps1` and `PLAT-002:79` | The real parameter list already exists in the same body | Step 6 |
| `AGENTS.md` § Simplicity rails (one list per concept) | Two spellings of one gate is the duplication these rounds kept removing | Step 2's single board-wide spelling |
| `docs/engineering.md:72-74` tier 1 | Static/build/architecture — "This proves consistency only" | Verification: greps over the board store plus each command running |
| `docs/engineering.md:201-203` § Plan sizing | A plan states its diff estimate first, from a real number | The first line and the inventory above |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass under a dated heading | Recorded as **`n/a — board-only`** (body Guardrails) |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing → Reviewer |

## Routing

Copied from the ticket body's `## Routing` block; required in the plan document
by `docs/desktop/00-governance-and-workflow/README.md` § Ticket template.

- **Subagent**: `pegasus-desktop-reviewer` —
  `.codex/agents/pegasus-desktop-reviewer.toml` (confirmed present 2026-08-24).
  Note it is declared `sandbox_mode = "read-only"`: it cannot write repository
  files, which suits a ticket that writes none, but the `update_item` calls
  themselves go through the Kanmer MCP.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-groom`
  (`.grok/skills/kanmer-groom/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `list_items`, `get_item`, `update_item`,
  `search_items`, `get_group_doc`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `move_item`). **No Azure MCP, no Microsoft Learn.**
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates FND-052` before every move; a move crosses at most one gated
  boundary). `chore` owes `plan` at `leave-preparing` and `proof` at
  `enter-done`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's ten implementation steps in the same order, with
the same ownership and the same targets.

1. **Orient and take.** `get_status`, then read `HZN-001` /
   `board-conventions.md` **in full** with
   `get_group_doc HZN-001 board-conventions.md` — the join table and the
   absolute bare-id rule are the authority for steps 7–8, and reading half of
   it is how the `FND-022` "Drop `CASE-001`" defect happened. Then
   `get_doc_gates FND-052` and `take_ticket`. **This ticket edits ticket bodies
   only; it changes no repository file.**
2. **Fix one spelling, after reading the parameter block.** Confirm
   `scripts/Test-MarkdownPlacement.ps1:2-6` still declares both `-Base` and
   `-Head` `[Parameter(Mandatory)]`, then adopt
   `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD` as
   the single board-wide spelling. Do not vary it per ticket — two spellings of
   one gate is the duplication these rounds kept removing.
3. **Apply the 16 actual command call sites** listed in inventory § A, across
   `DUI-013`, `DUI-017`, `FEAT-025`, `FEAT-043`, `FND-014`, `FND-015`,
   `FND-019`, `FND-020`, `FND-023`, and `FND-042`. **Leave the four path
   references alone** — `DUI-013:47`, `DUI-017:95`, `DUI-017:171`, and
   `FEAT-025:41` name the script as evidence, not as a command. The separate
   `FEAT-038` `Test-TestMarkdownPlacement.ps1` self-test is not a gate call and
   remains unchanged. Note `FND-023:154` is
   inside that body's embedded `DSK-01-13` specification; it is a call site and
   is in scope.
4. **While in `REL-013`, add the real validator beside the self-test.**
   `REL-013:67` and `:84` call `Test-TestMarkdownPlacement.ps1` — the
   regression test of the placement script, and the one
   `.github/workflows/ci.yml:82-84` runs. Keep it, and **add**
   `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD`
   beside it, so `REL-013`'s own new file
   `docs/desktop/09-release-update-and-distribution/first-install.md` is
   actually inspected. Adding, not replacing: the self-test guards the
   validator and the validator guards the file, and neither substitutes for
   the other.
5. **Add the shard arguments to the four `-VerifyPartition` call sites** —
   `FND-046:91`, `PLAT-002:75`, `PLAT-006:71`, `PLAT-006:87` — copying
   `TEST-003:68`'s working form
   `-VerifyPartition -ArtifactRoot ./artifacts/test-shards -ShardCount 3`.
   **Verify first** that `scripts/Invoke-TestShard.ps1:35-36` still declares
   `-ShardCount` `[Parameter(Mandatory)]` with no `ParameterSetName`, which is
   what makes it mandatory in the `Verify` set as well as `Run`.
6. **Replace `PLAT-002:95`'s ellipsis** with the actual invocation from that
   same body's step 11 at `:79` — `-BaseUri`, `-ExpectedSourceRevision`,
   `-ExpectedVersion`, `-ResourceGroupName` and the rest, with the same
   placeholders step 11 uses. Copy from `:79`; do not re-derive it from the
   script, or the two lines drift apart in the same body.
7. **Qualify the five high-value id sites first**, in this order of value:
   `FEAT-013` ×2 (both mean **upstream `INTK-001`** — absorbed, no fork ticket;
   board `INTK-001` is upstream `INTK-002`, a live import); `FEAT-003` ×1
   (`(upstream CASE-009 / upstream CASE-002)` — board `CASE-002` is upstream
   `CASE-022`, a live import, so this is the one that can point at a live
   ticket that is not the one meant); `FEAT-043` ×2; `FEAT-019` ×2, **repeating
   the word "upstream" per id in the list** — a single leading "upstream" does
   not carry across a comma. These seven are the only occurrences that can
   point at a live import or a seeded ticket that is not the one meant.
   *Measured caveat:* a spot check on 2026-08-24 found some occurrences in
   `FEAT-013` already reading `upstream INTK-001`. Re-derive each site at
   execution; an occurrence already correctly qualified is **ticked as done,
   not re-edited**, and the count in the proof is what was found, not what was
   forecast.
8. **Then the twelve `DOCS-001` occurrences** in `DOCS-001`, `DOCS-003` and
   `FND-004`, written `upstream DOCS-001 (board [[DOCS-001]])` on **first use
   in each body**. Board and upstream numbers coincide on that row alone, so
   both readings resolve to the same ticket and the operational risk is nil —
   but `board-conventions.md` singles it out as the trap of the table, and
   these three bodies are the ones most likely to be copied. **Measured:** the
   raw string appears 9 times in `DOCS-001`, 13 in `DOCS-003` and once in
   `FND-004`; `DOCS-001` and `DOCS-003` each carry one
   `### Upstream ticket <ID> (verbatim)` block, and **every occurrence inside a
   verbatim block is out of scope**. Count the outside-the-block occurrences
   before editing.
9. **Turn `REL-007:61`'s two dangling wiki-links into plain code spans** —
   `[[DSK-09-07]]` → `` `DSK-09-07` `` and `[[DSK-09-09]]` → `` `DSK-09-09` ``.
   Both handles are withdrawn, no ticket carries them, and they are the only
   two dangling links out of 2,620 on the board. Keep the surrounding sentence
   intact: it is what records *why* those spikes were withdrawn.
10. **Re-run the sweep and record the result.** Confirm: no bare id under the
    join table outside a `### Upstream ticket <ID> (verbatim)` block; no
    `## Verification` command that fails on invocation for wrong or missing
    arguments; no ellipsis standing in for an argument list; every `[[…]]`
    target resolves. Record the simplification pass as **`n/a — board-only`**
    with the date.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture**
(`docs/engineering.md:72-74`). The result is proved by grepping the board store
and by each command running, not by application behaviour — "This proves
consistency only".

| Command / observation | Expected | Becomes evidence as |
| --- | --- | --- |
| `grep -rn 'Test-MarkdownPlacement.ps1' .worktrees/kanmer/.kanmer/areas/` | every hit that is a **command** carries `-Base` and `-Head`; the four path references (`DUI-013:47`, `DUI-017:95`, `DUI-017:171`, `FEAT-025:41`) are unchanged and expected in the output | `proof` (command-log), with the four exceptions named so the reviewer does not read them as misses |
| `grep -rn 'VerifyPartition' .worktrees/kanmer/.kanmer/areas/` | every hit carries `-ShardCount` | `proof` (command-log) |
| `grep -rn '…' .worktrees/kanmer/.kanmer/areas/*/*/[A-Z]*.md` restricted to `## Verification` blocks | no ellipsis standing in for an argument list | `proof` (command-log) |
| Every `[[…]]` target resolved via `get_item` | no dangling target | `proof` (command-log) |
| `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD` run once, to prove the adopted spelling works | exit `0` and `Markdown placement passed for <base>..<head>.` (`:81`) | `proof` (command-log) — the spelling is only correct if it runs |
| `pwsh ./scripts/Invoke-TestShard.ps1 -VerifyPartition -ArtifactRoot ./artifacts/test-shards -ShardCount 3` run once, likewise | exit `0` | `proof` (command-log) |
| `git status --porcelain` in the main worktree at close | **empty** — this ticket changes no repository file | `proof` (command-log) |

## Risks / open questions

- **Risk — the sweep widens.** Guardrail (b) and (c) exist because the obvious
  next move is "while we're here". Mitigation: the not-touched table names each
  excluded set with its measured size and the reason, so widening the sweep is
  visible in the diff as a deviation from this plan rather than as diligence.
- **Risk — an id is "fixed" inside a verbatim block.** `DOCS-001` and
  `DOCS-003` each carry one, and step 8 is the step most likely to walk into
  it. Mitigation: step 8 requires counting outside-the-block occurrences first;
  `board-conventions.md` § "Where a bare upstream id is still correct" is the
  cited rule.
- **Risk — the counts have moved since the sweep.** They may already have: a
  spot check on 2026-08-24 found `FEAT-013`'s `INTK-001` occurrences already
  reading `upstream INTK-001`. Mitigation: step 7 requires re-derivation at
  execution and records what was **found**, not what was forecast. The body's
  numbers are the sweep's, and a site already correct is a tick, not an edit.
- **Risk — a second spelling of the placement gate is introduced.** Mitigation:
  step 2 fixes one, step 3 applies only that one, and step 4 is explicit that
  `REL-013` gets the validator **added beside** the self-test rather than
  swapped for it — the one place where two commands are correct, and the reason
  is written down.
- **Risk — the 108 "missing file" verification lines get re-flagged as
  defects.** They are not defects: every path is created by a named ticket on
  this board, and the acceptance criteria say so. Mitigation: guardrail (c) is
  restated in the not-touched table, and step 10's sweep is scoped to commands
  that **fail on invocation**, not to commands whose target does not exist yet.
- **Risk — a `blocks`, `labels` or stage field is changed in passing.**
  `update_item` can carry more than body text. Mitigation: the scope boundary,
  and the final `git status --porcelain` plus a `list_items` comparison before
  and after if the reviewer wants it.
- **Scope boundary, not an open question — `FND-022` and the collision
  paragraph.** [[FND-022]] (plan handle `DSK-01-09`) owns the carry-over batch,
  and 23 of its 28 bare ids sit inside the Traps paragraph that teaches the
  collision. Guardrail (a) protects them; this ticket does not edit that
  paragraph.
- **Scope boundary, not an open question — the withdrawn `DSK-09-07` /
  `DSK-09-09` spikes.** They were withdrawn when D-002 was decided
  (2026-08-23, self-managed certificate). This ticket demotes the links; it
  does not re-open the decision, and `operator-decisions.md` plus
  `docs/desktop/README.md` both record that **no open decisions remain**.
- **Open questions**: none. No `open-questions` document is created — the
  ticket body does not instruct one, every item is independently applicable
  (the body's § Source of truth says "Depends on: none"), and the two remaining
  unknowns are scope boundaries owned by named tickets.

## Simplification pass

2026-08-25 — **n/a — board-only.** The ticket changes only Kanmer ticket bodies
through MCP; there is no repository diff to simplify. The live inventory
corrections were scope-preserving namespace and measurement fixes, not
behavioural changes.


## Live inventory correction — 2026-08-25

The original 2026-08-24 inventory stated that REL-007 had only the two withdrawn wiki-links. A live `get_links REL-007` read immediately before implementation found six unresolved parser targets: the two withdrawn handles plus four existing plan handles (`DSK-09-11`, `DSK-09-14`, `DSK-09-15`, `DSK-09-18`). The four map unambiguously to `REL-009`, `REL-012`, `REL-013`, and `REL-016` by the live board titles. The body-only correction remains within the plan's namespace-normalization purpose; no ticket fields, dependencies, repository files, or product decisions change.
