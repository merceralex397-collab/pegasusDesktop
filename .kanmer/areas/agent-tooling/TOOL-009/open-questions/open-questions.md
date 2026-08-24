# Open questions — TOOL-009 (plan handle `DSK-12-09`): subagent-protocol dry run

**Why this document exists.** `TOOL-009` is a `spike`, and for a spike the board owes
`research` at `enter-done` — the research document *is* the deliverable, so writing it
satisfies the gate by itself. The `research` document on this ticket is an honest scaffold:
its **Facts** section is captured, and the whole `## Dry-run record` is `NOT YET CAPTURED`
because the dry run has not happened. Without this document `get_doc_gates TOOL-009`
reported `enter-done` **passable** — one `move_item` from Done with no dry run ever run.
The banner at the top of `research` is prose; the gate reads document existence and unticked
boxes. This document is the gate.

**What it blocks.** Per the corrected authoring contract § 7, an unticked `- [ ]` line above
the `## Parked` heading blocks exactly three boundaries — `leave-preparing`, `enter-review`
and `enter-done` — and never `leave-backlog`. For profile `spike` the board declares only
one of those three (`get_doc_gates` with no id: `spike` → `enter-done: [research,
questions-resolved]`), so these boxes block **Done and nothing else**. The spike can be
taken and worked freely; it cannot be closed until the run has produced its output.

**How to tick a box.** Tick it only when the named evidence is pasted into the matching
section of the `research` document. Do **not** move any of these below
`## Parked (explicitly deferred)` — each one is the dry run's own output, and parking it
would close the ticket without its deliverable. Body step 13 says the same thing in the
other direction: tick them, then move.

This spike writes Kanmer documents and files follow-up tickets. Its Guardrails forbid
editing `.codex/agents/*.toml`, `.agents/skills/**` and `eng/skills/**`, and forbid any
Azure write; an agent asking for an Azure test resource during the run is a finding to
record, not a request to fulfil (L-02, ADR-0014).

## Uncaptured items

- [ ] **Preconditions checked, with their real state — body step 1.** Run
      `grep -n '^\[agents\]' .codex/config.toml` and `get_item FND-030`. Their output must
      answer: *is there an `[agents]` table yet (assumption **A-12-7**,
      [[TOOL-005]] (plan handle `DSK-12-05`)), and is the default target still `blocked`
      (assumption **A-12-6**)?* Record both answers before the run starts. If the target is
      still blocked, the ticket body is explicit: **record that as the finding rather than
      switching targets mid-run**, and the fallback the body permits — the earliest
      unstarted row that still routes to `winui-dev` — is chosen *before* the run, with the
      reason written down.

- [ ] **Target ticket and why — body step 2.** Name the target and write one sentence
      saying why it was picked. Its output must answer: *which row does the rehearsal ride
      on, and why that one over [[FND-026]] (plan handle `DSK-02-01`)?* Default is
      [[FND-030]] (plan handle `DSK-02-05`), scaffold `src/Pegasus.Desktop`, because
      `DSK-02-01` is ADR authoring routed to `kanmer-docs` and exercises neither the
      implementer nor the test engineer. Resolve the handle with
      `search_items DSK-02-05`, never by computing it.

- [ ] **Protocol walk with timings — body step 3.** Walk the seven steps of
      `docs/desktop/12-agent-tooling/README.md` § 6 literally and fill every cell of the
      seven-row table in `research` § "Protocol walk with timings". Its output must answer:
      *what did each hand-off cost in wall-clock, and where did it stall?* The body's Sizing
      concern makes the cost a number rather than an impression; three agents plus a
      `feature`-profile document pipeline can absorb unlimited time.

- [ ] **`winui-dev` Appendix C report — body step 4.** Delegate the implementation to
      `winui-dev`, which must load `pegasus-desktop` first, then `winui-dev-workflow` and
      `winui-design`, from the vendored paths. Its evidence must include the
      `BuildAndRun.ps1` output **and the launched process id**. The output must answer:
      *did the app actually run?* — a build log alone does not prove it (assumption
      **A-12-9**).

- [ ] **`pegasus-test-engineer` Appendix C report — body step 5.** Delegate the test
      scaffold in parallel, per `docs/desktop/12-agent-tooling/subagents.md` § Usage
      examples. Its evidence must include the verbatim canonical command from
      `docs/runbook.md:298-306` —
      `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category!=Corpus"`,
      preceded by `dotnet restore ./Pegasus.slnx --locked-mode` and
      `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — plus the
      pass/fail counts. Its output must answer: *were the repository's own locked profiles
      used, and what did they return?* A summary sentence does not answer it.

- [ ] **`pegasus-desktop-reviewer` Appendix C report — body step 6.** Delegate the review of
      the combined diff. Its output must answer: *did the reviewer load `winui-code-review`
      and `winui-design` **itself** rather than trusting the implementer's summary, and did
      it produce the findings table (severity, `file:line`, finding, cost, alternative,
      blocks merge yes/no) plus a one-line verdict?*

- [ ] **Appendix C grading table — body step 7.** Fill every cell of the seven-heading ×
      three-agent table in `research` § "Appendix C grading", marking each
      complete / vague / empty / invented. Its output must answer: *which of the seven
      headings (Skills consulted / Applicable guidance / Project decisions taking precedence
      / Repository evidence / Implementation / Verification / Deviations) came back unusable,
      and from which agent?* A prose paragraph saying "it went well" is the failure mode this
      ticket exists to prevent for the twenty-two vertical slices of area 05.

- [ ] **Instruction gaps as proposed edits — body step 8.** Write a numbered list in which
      every entry names the exact file and line it would touch — a `.codex/agents/*.toml`
      line, or a section of `.agents/skills/project/pegasus-desktop/SKILL.md`. Its output
      must answer: *which question did an agent have to ask that its own instructions should
      have answered?* "The reviewer could not find the endpoint map" is not an entry; "add
      to `.codex/agents/pegasus-desktop-reviewer.toml` after the step 1 line: read
      `docs/desktop/03-gateway-api-and-data/endpoint-map.md`" is.

- [ ] **Read-only transcription answered with evidence — body step 9.**
      `pegasus-desktop-reviewer` has `sandbox_mode = "read-only"`
      (`.codex/agents/pegasus-desktop-reviewer.toml:3-4`) and cannot write, so its final
      message is the deliverable and the caller must write it into the ticket. Note, **at
      the moment the reviewer returns**, whether the findings were written in with
      `set_ticket_doc` / `append_scratch` or left in the transcript. Its output must answer:
      *did the review evidence reach the ticket, or would it have been lost at session end?*
      Reconstructing this afterwards is guesswork, and `winui-session-report` is not the way
      to reconstruct it — it is user-invoked only and carries a privacy warning.

- [ ] **Self-hop check, observed not assumed — body step 10.** Record the delegation calls
      actually made. Its output must answer: *how was it observed that no agent delegated to
      an agent of its own kind?* All eight TOMLs carry a never-self-delegate sentence
      (`grep -ci` returns ≥1 for each, verified 2026-08-24); the box asks for the observation,
      not the restatement.

- [ ] **Follow-up ticket ids — body step 12.** File the proposed edits from step 8 as
      tickets in the `agent-tooling` area (prefix `TOOL`) and name their board ids in
      `research`. Its output must answer: *where does each proposed edit now live?* Verify
      with `search_items` that the ids exist. **No agent TOML or project skill is edited by
      this ticket** — a spike that quietly rewrites eight agent definitions has stopped being
      a spike, and the edits would land unreviewed.

## Parked (explicitly deferred)

- **Whether the Appendix C shape is the right shape at all.** Parked, not open: the shape is
  fixed by proposal Appendix C and installing it is
  [[TOOL-007]] (plan handle `DSK-12-07`)'s work. If the dry run finds it wanting, that is a
  follow-up ticket under step 12, not a decision taken here — a decision a named sibling
  ticket owns is a scope boundary, not an open question.

- **Whether the skills named in the routing tables resolve from the vendored paths
  (A-12-8).** Parked as a question, because it is answered mechanically by each agent's
  "Skills consulted" heading during the run: a path that does not exist on disk with the
  pinned SHA is an instruction gap for step 8, not a new question. It depends on
  [[TOOL-002]] (plan handle `DSK-12-02`) and [[TOOL-004]] (plan handle `DSK-12-04`) landing
  first, and both are named tickets.
