# Research — TOOL-009 (plan handle `DSK-12-09`): Dry run of the subagent invocation protocol on one foundation ticket

> **STATUS — INCOMPLETE. Do not move this ticket to Done yet.**
> This ticket is a `spike`; `get_doc_gates TOOL-009` owes `research` at `enter-done`, so
> this document *is* the deliverable. Everything below marked **Facts** is pre-captured and
> needs no rework. Everything marked `NOT YET CAPTURED` requires the dry run to have
> actually happened: three Appendix C reports, the grading table, the instruction gaps and
> the follow-up ticket ids. Filling those in is the ticket.

## Question

Does the invocation protocol (proposal §20.5, seven steps) survive contact with one real
ticket — and where does it fail a working agent? Specifically: do `winui-dev`,
`pegasus-test-engineer` and `pegasus-desktop-reviewer` each produce usable Appendix C
evidence, does the read-only reviewer's output actually reach the ticket, and does any agent
have to ask a question its own instructions should have answered?

## Current behaviour

There is no current web-application behaviour to record: this spike exercises the developer
toolchain, not a `src/Pegasus.Web/Pages/**` route, page model, `Pegasus.Core` use case or
Worker path.

**No `PAR-nn` row in `docs/desktop/01-inventory-and-parity/parity-matrix.md` covers it, and
none should.** The matrix's 47 rows are keyed to Razor page models and operator-visible
capabilities (`parity-matrix.md:36-44`); a protocol rehearsal is neither. The **target**
ticket the dry run rides on may have a parity row of its own — the default target,
`FND-030` (plan handle `DSK-02-05`, scaffold `src/Pegasus.Desktop`), does not, because it is
new-project scaffolding rather than a converted screen.

What exists today, before the dry run:

- Eight agent TOMLs under `.codex/agents/`, all tracked, none of which has ever been used
  on a ticket. They were written from documentation, not from use.
- A project skill at `.agents/skills/project/pegasus-desktop/SKILL.md` (110 lines) whose
  § Invocation protocol lists the same seven steps.
- Routing tables in `docs/desktop/12-agent-tooling/skill-routing.md` naming skills that,
  until [[TOOL-002]] (`DSK-12-02`) lands, resolve to `.codex/skills/` rather than the
  vendored destinations.

## Findings

- **The trio in the body maps exactly onto `subagents.md` § Usage examples, second
  example** — "spawn `pegasus-gateway-dev` for DSK-03-04 and, in parallel,
  `pegasus-test-engineer` …; when both finish, spawn `pegasus-desktop-reviewer` on the
  combined diff". The dry run is that shape with `winui-dev` substituted, so it tests a
  documented pattern rather than an invented one.
- **The default target is `FND-030`** (plan handle `DSK-02-05`), not `FND-026`
  (`DSK-02-01`). `DSK-02-01` is ADR authoring routed to `kanmer-docs` and exercises neither
  the implementer nor the test engineer, so it would rehearse almost nothing. `FND-030` is a
  `feature` routed to `winui-dev` with `winui-setup`, `winui-dev-workflow`, `winui-design`,
  and it is the first area-02 row that genuinely needs all three agents.
- **Two hard prerequisites, both currently unmet.** `FND-030` is `blocked` on the board and
  itself depends on `DSK-02-01` and `DSK-02-02`; and the `[agents]` table does not exist yet
  ([[TOOL-005]], `DSK-12-05`). If the target is still blocked when this spike is worked, the
  body is explicit: **record that as the finding rather than switching targets mid-run.**
- **`FND-030` is a `feature`**, so it owes `research`, `files`, `plan` and `checklist` before
  it can leave Preparing, plus `post-implementation-report` before Review and `proof` before
  Done. The dry run therefore exercises the *whole* document pipeline, not just three agent
  calls — budget for that, and do not count the target's document authoring as protocol
  failure.
- **The read-only transcription failure mode is real and specific.**
  `pegasus-desktop-reviewer` and `pegasus-parity-researcher` have
  `sandbox_mode = "read-only"` (verified 2026-08-24), and `subagents.md` states plainly that
  a read-only agent's "final message is the deliverable and the caller writes it into the
  ticket". If the caller does not, the review evidence exists only in a transcript and is
  lost at session end. Body step 9 exists to measure whether that actually happened.
- **Self-hop is checkable, not merely assertable.** All eight TOMLs carry a
  never-delegate-to-your-own-kind sentence (verified 2026-08-24, `grep -ci` returns ≥1 for
  each). Body step 10 asks *how* the absence of self-hop was observed, which means recording
  the delegation calls made, not asserting that none happened.

### Facts

Verified by reading the repository and the board on 2026-08-24:

| Fact | Evidence |
| --- | --- |
| The eight agent TOMLs exist and are tracked. | `git ls-files .codex` |
| `winui-dev.toml` sets **neither** `sandbox_mode` nor `model_reasoning_effort` — it inherits the upstream default, as `subagents.md` § Roster specifies. Its step `0.` loads the project skill; it requires a unique `AutomationProperties.AutomationId` on every interactive control; it must not recursively delegate. | `grep -n` on `.codex/agents/winui-dev.toml` |
| `pegasus-test-engineer.toml` — `sandbox_mode = "workspace-write"`, `model_reasoning_effort = "high"`; xunit 2.9.3 and hand-rolled fakes only (no Moq, no FluentAssertions); runbook profiles. | `.codex/agents/pegasus-test-engineer.toml:3-4` and its `developer_instructions` |
| `pegasus-desktop-reviewer.toml` — `sandbox_mode = "read-only"`, `model_reasoning_effort = "high"`; ten review lenses; "if you implemented the change, say so and stop". | `.codex/agents/pegasus-desktop-reviewer.toml:3-4` |
| `.codex/config.toml` has **no `[agents]` table** (15 lines: `[features]` `:1`, `[mcp_servers.mcp_microsoftdocs]` `:5`, `[mcp_servers.kanmer]` `:9`, `[mcp_servers.kanmer.env]` `:13`). | `cat -n .codex/config.toml` |
| The default target is board `FND-030` = plan handle `DSK-02-05`, "Scaffold `src/Pegasus.Desktop` (WinUI 3, x64, packaged, self-contained, pinned Windows App SDK 2.x)", profile `feature`, groups `EPIC-003`/`HZN-002`, currently `blocked`. | `search_items DSK-02-05` |
| The alternative target board `FND-026` = `DSK-02-01` is ADR authoring, profile `chore`, routed to `kanmer-docs`. | `search_items DSK-00-05` result set |
| Area 02's plan row for `DSK-02-05` gives acceptance "Builds with `BuildAndRun.ps1`; launches with package identity; no `AnyCPU`; `Package.appxmanifest` identity placeholders documented" and verification "`BuildAndRun.ps1 -SkipRun` then `winapp run` log; screenshot", evidence tier 1. | `docs/desktop/02-architecture-and-foundation/README.md:246` |
| The canonical repository verification commands the agents must actually use are `dotnet restore ./Pegasus.slnx --locked-mode`, `dotnet build ./Pegasus.slnx --configuration Release --no-restore`, `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category!=Corpus"`. | `docs/runbook.md:298-308` § Locked restore, build, and test |
| The focused forms and the integration-filter complement pair are at `docs/runbook.md:312-320`; the integration project caps concurrency at four in `tests/Pegasus.IntegrationTests/xunit.runner.json`, and the Browser selection halves it on the command line. | `docs/runbook.md:312-330` |
| The seven Appendix C headings are Skills consulted / Applicable guidance / Project decisions taking precedence / Repository evidence / Implementation / Verification / Deviations. | `.agents/skills/project/pegasus-desktop/SKILL.md` § Evidence format; proposal Appendix C |
| The seven invocation-protocol steps are listed identically in `docs/desktop/12-agent-tooling/README.md` § 6 and in the project skill § Invocation protocol. | both files |

### Assumptions

- **A-12-6 — `FND-030` will be unblocked when this spike is worked.** Unverified; it is
  `blocked` on the board today and depends on `DSK-02-01` and `DSK-02-02`.
  Confirmed by: `get_item FND-030` showing `blocked: false`.
  Breaks if wrong: the dry run cannot run on the default target. The body's Sizing concern
  is explicit — **record that as the finding, do not switch targets mid-run.** The
  fallback the body permits is "the earliest unstarted row that still routes to
  `winui-dev`", chosen *before* the run starts and with the reason written down.
- **A-12-7 — the agent roster loads and delegation works.** Depends on [[TOOL-005]] and on
  [[TOOL-001]]'s (`DSK-12-01`) verdict.
  Confirmed by: `.codex/config.toml` containing an `[agents]` table and `/agent` listing the
  eight names (body step 1).
  Breaks if wrong: there is nothing to delegate to and the spike stops at step 1.
- **A-12-8 — the skills named in the routing tables resolve from the vendored paths.**
  Depends on [[TOOL-002]] and [[TOOL-004]] (`DSK-12-04`).
  Confirmed by: each agent's Appendix C "Skills consulted" naming a path that exists on disk
  with the pinned SHA.
  Breaks if wrong: an agent silently proceeds without the guidance it claims to have loaded
  — which is the highest-value failure this dry run can detect.
- **A-12-9 — `BuildAndRun.ps1` runs on the workstation.** It ships inside
  `winui-dev-workflow` (verified present locally at
  `.codex/skills/winui-dev-workflow/BuildAndRun.ps1`, alongside
  `analyzer/Microsoft.WindowsAppSDK.Analyzers.dll`, 49,664 B).
  Confirmed by: the recorded output showing a **launched process id**, not merely a
  successful build. Body step 4 makes that distinction the acceptance.
  Breaks if wrong: the target ticket's own evidence tier cannot be met and that is the
  target ticket's problem, not this spike's — say so rather than absorbing it.

## Execution placement

Not applicable, and the heading is kept rather than dropped so the omission is visible: this
spike places no responsibility in either the desktop or the cloud. It rehearses a developer
protocol and produces documents. The six-question test in
`docs/desktop/00-governance-and-workflow/README.md` § 3 is answered by the **target** ticket,
not by this one — and if any agent asks for an Azure test resource during the run, L-02 and
ADR-0014 stand and that request is **a finding to record, not a request to fulfil**.

## Implications

1. **Timebox to one target ticket.** The body's Sizing concern says so, and the failure mode
   is obvious: three agents plus a `feature`-profile document pipeline can absorb unlimited
   time. Note the wall-clock at each hand-off (body step 3) so the cost is a number.
2. **Grade the evidence, do not summarise it.** The deliverable is a table of the seven
   Appendix C headings × three agents, with each cell marked complete / vague / empty /
   invented and the agent named. A prose paragraph saying "it went well" is the failure mode
   this ticket exists to prevent for the twenty-two slices of area 05.
3. **Every instruction gap becomes a proposed edit with a file and a line**, not an
   observation. "The reviewer could not find the endpoint map" is not actionable; "add to
   `.codex/agents/pegasus-desktop-reviewer.toml` after the step 1 line: read
   `docs/desktop/03-gateway-api-and-data/endpoint-map.md`" is.
4. **This spike must not make those edits.** Body step 12: file them as follow-up tickets in
   `agent-tooling` and name the ids here. A spike that quietly rewrites eight agent
   definitions has stopped being a spike, and the edits would land unreviewed.
5. **Keep the two branches separate.** The target ticket's code lands on the target ticket's
   branch and worktree; this spike's own branch must show `git status --porcelain` empty.
   The spike also must not claim the target's evidence tier as its own — the target carries
   its own tier (1 for `FND-030`), and this spike is tier 1 for the narrow claim "the
   protocol ran and cost this much".
6. **Watch for the transcription gap in real time.** The cheapest way to answer body step 9
   honestly is to note, at the moment the reviewer returns, whether its findings were
   written into the ticket with `set_ticket_doc`/`append_scratch` or left in the transcript.
   Reconstructing that afterwards is guesswork — and `winui-session-report` is **not** the
   way to reconstruct it: it is user-invoked only and carries a privacy warning before
   anything is shared.

## Dry-run record

### Target ticket and why (body step 2)

`NOT YET CAPTURED`. Default: board `FND-030` (plan handle `DSK-02-05`). Write one sentence
saying why the chosen row was picked over `FND-026` (`DSK-02-01`). If area 02 has progressed,
name the earliest unstarted row that still routes to `winui-dev` and say so.

### Protocol walk with timings (body step 3)

`NOT YET CAPTURED`. One row per step of the seven, with wall-clock and any stall at the
hand-off:

| # | Protocol step | Wall clock | Stall / friction |
| --- | --- | --- | --- |
| 1 | Read project skill, area plan, ticket folder (`get_doc_gates` before every move) | | |
| 2 | Read the exact upstream `SKILL.md` files from the lockfile | | |
| 3 | Summarise only applicable guidance; name overridden guidance | | |
| 4 | Implement the smallest vertical slice | | |
| 5 | Run skill-prescribed verification plus the repository profiles | | |
| 6 | Record Appendix C evidence | | |
| 7 | Hand to the independent reviewer | | |

### `winui-dev` report (body step 4)

`NOT YET CAPTURED`. Must include the `BuildAndRun.ps1` output **and the launched process
id** — a build log alone does not prove the app ran.

### `pegasus-test-engineer` report (body step 5)

`NOT YET CAPTURED`. Must include the verbatim `dotnet test` command from
`docs/runbook.md:298-320` and the pass/fail counts — not a summary sentence.

### `pegasus-desktop-reviewer` report (body step 6)

`NOT YET CAPTURED`. Must show it loaded `winui-code-review` and `winui-design` **itself**
rather than trusting the implementer's summary, and must produce the findings table
(severity, `file:line`, finding, cost, alternative, blocks merge yes/no) plus a one-line
verdict.

### Appendix C grading (body step 7)

`NOT YET CAPTURED`. Fill every cell:

| Appendix C heading | `winui-dev` | `pegasus-test-engineer` | `pegasus-desktop-reviewer` |
| --- | --- | --- | --- |
| Skills consulted (path + pinned SHA) | | | |
| Applicable guidance | | | |
| Project decisions taking precedence | | | |
| Repository evidence (`file:line`) | | | |
| Implementation | | | |
| Verification | | | |
| Deviations | | | |

### Instruction gaps → proposed edits (body step 8)

`NOT YET CAPTURED`. Numbered list; each entry names the exact file and the line it would
touch (a `.codex/agents/*.toml` line, or a section of
`.agents/skills/project/pegasus-desktop/SKILL.md`).

### Read-only transcription (body step 9)

`NOT YET CAPTURED`. Answer with evidence: were the reviewer's findings written into the
ticket by the caller, or did they exist only in a transcript?

### Self-hop check (body step 10)

`NOT YET CAPTURED`. Record **how** it was observed — the delegation calls made — not that it
was assumed.

### Follow-up ticket ids (body step 12)

`NOT YET CAPTURED`. File the proposed edits as tickets in the `agent-tooling` area (prefix
`TOOL`) and name their board ids here. **No agent TOML or project skill is edited by this
ticket.**

## Open questions

None are opened as a blocking `open-questions` document, deliberately: an unticked `- [ ]`
line there blocks *every* stage move, and this spike's questions are its own subject matter
— blocking it from starting in order to ask it to start would be circular. Carried here
instead, each with a destination:

- Whether `FND-030` is unblocked when the spike is worked (A-12-6). Answered by
  `get_item FND-030` at step 1; if it is still blocked, the body directs that this becomes
  the recorded finding rather than a target switch.
- Whether the skills named in the routing tables resolve from the vendored paths (A-12-8).
  Answered by each agent's "Skills consulted" heading; a path that does not exist is an
  instruction gap for step 8, not a new question.
- Whether the Appendix C shape is the right shape at all. Out of scope: the shape is fixed
  by proposal Appendix C and is [[TOOL-007]]'s (`DSK-12-07`) to install. If the dry run finds
  it wanting, that is a follow-up ticket, not a decision taken here.
