# Open questions — TOOL-001 (plan handle `DSK-12-01`): Codex skill and agent discovery

**Why this document exists.** `TOOL-001` is a `spike`, and for a spike the board owes
`research` at `enter-done` — the research document *is* the deliverable, so writing it
satisfies the gate by itself. The `research` document on this ticket is an honest scaffold:
its **Facts** section is captured, and every operator-dependent section is a literal
`NOT YET CAPTURED` block. Without this document `get_doc_gates TOOL-001` reported
`enter-done` **passable**, one `move_item` from Done with no operator output captured at
all. The banner at the top of `research` is prose; the gate reads document existence and
unticked boxes. This document is the gate.

**What it blocks.** Per the corrected authoring contract § 7, an unticked `- [ ]` line above
the `## Parked` heading blocks exactly three boundaries — `leave-preparing`, `enter-review`
and `enter-done` — and never `leave-backlog`. For profile `spike` the board declares only
one of those three (`get_doc_gates` with no id: `spike` → `enter-done: [research,
questions-resolved]`), so these boxes block **Done and nothing else**. That is exactly the
behaviour wanted: the spike can be taken and worked freely, and cannot be closed until the
operator output exists.

**How to tick a box.** Tick it only when the verbatim output is pasted into the matching
section of the `research` document with the date it was captured. Do **not** move any of
these below `## Parked (explicitly deferred)` — each one *is* the ticket's subject matter,
and parking it would close the ticket without its deliverable. Body step 12 says the same
thing in the other direction: tick them, then move.

All commands are run by the operator in a real Codex (and Claude Code) session opened at the
repository root `C:\Users\PC\Documents\GitHub\pegasusDesktop`. Nothing here writes to the
repository; this spike's Guardrails forbid that.

## Uncaptured items

- [x] **Codex build version — body step 3.** Run `codex --version`. Its output must answer:
      *which exact build string is the verdict paragraph about?* Every other answer in this
      document is only true of that build. Paste the exact output string and the capture
      date into `research` § "Captured output" → "`codex --version`".

- [ ] **Skill discovery listing — body step 4.** Run `/skills` and capture the **complete**
      listing including the directory each entry is discovered from. Its output must answer
      the binary question this ticket exists for: *do `winui-setup`, `winui-dev-workflow`,
      `winui-design`, `winui-code-review`, `winui-ui-testing`, `winui-packaging`,
      `winui-wpf-migration` and `winui-session-report` appear, and from which directory?*
      Paste the listing into `research` § "Captured output" → "`/skills`". This is the
      answer to assumption **A-12-2**, and it is what decides whether
      [[TOOL-002]] (plan handle `DSK-12-02`) has the right `policy.vendorRoot`.

- [x] **`pegasus-release` — once or twice — body step 6(b).** From the same `/skills`
      listing, record whether `pegasus-release` appears once or twice. It exists at both
      `.codex/skills/pegasus-release/SKILL.md` and `.agents/skills/pegasus-release/SKILL.md`,
      both 13,299 bytes. Its output must answer: *does Codex de-duplicate by skill `name` or
      by path?* One entry means by name; two means by path, and every duplicate anywhere is
      then a live ambiguity about which revision an agent read.

- [x] **`pegasus-desktop` discovery — body step 6(c).** From the same `/skills` listing,
      record whether the project skill `pegasus-desktop` appears and whether its discovered
      path is `.agents/skills/project/pegasus-desktop/`. Its output must answer assumption
      **A-12-1** — *does the installed build scan `.agents/skills` as documented?* If it does
      not, the vendoring destination in
      `docs/desktop/12-agent-tooling/skills.lock.draft.json` is the wrong target and
      [[TOOL-002]] is re-planned before it starts.

- [x] **Agent roster — body step 5.** Run `/agent` and capture the roster. Its output must
      answer: *does the roster load without an `[agents]` table in `.codex/config.toml`?*
      There is no `[agents]` table today (`cat -n .codex/config.toml`, 15 lines:
      `[features]` `:1`, `[mcp_servers.mcp_microsoftdocs]` `:5`, `[mcp_servers.kanmer]` `:9`,
      `[mcp_servers.kanmer.env]` `:13`). An empty or partial roster *is* the finding — it
      makes [[TOOL-005]] (plan handle `DSK-12-05`) a hard prerequisite for
      [[TOOL-009]] (plan handle `DSK-12-09`) rather than a tidy-up. Paste into `research`
      § "Captured output" → "`/agent`".

- [x] **Explicit `$winui-design` probe — body step 7.** Mention `$winui-design` in the
      session. Its output must answer: *does Codex resolve the mention, and from which
      file?* A name that lists but does not resolve is a different failure from one that
      never lists (assumption **A-12-5**). Note that `winui-setup` carries
      `disable-model-invocation: true` at `.codex/skills/winui-setup/SKILL.md:1-5`, so
      "listed but not auto-invoked" is a valid non-failing state — grade by the listing and
      by this explicit probe, never by whether a skill fired on its own.

- [ ] **Optional TOML fields honoured — body Guardrails, "Open question to carry".** From
      the same session, record whether the installed build honours `sandbox_mode` and
      `model_reasoning_effort` (the build reporting the field, or a read-only agent visibly
      refusing a write). Its output must answer assumption **A-12-4** — *is the read-only
      guarantee for `pegasus-parity-researcher`, `pegasus-desktop-reviewer` and
      `pegasus-azure-auditor` enforced by the runtime, or is it only the prose in
      `developer_instructions`?* If it honours neither, the ticket body directs that it be
      recorded as an open question for [[TOOL-005]], where it is actionable, and it is
      material evidence for [[TOOL-006]] (plan handle `DSK-12-06`)'s Azure MCP guardrail.

- [ ] **Claude Code discovery — body step 9.** Repeat the `/skills`-equivalent listing in
      Claude Code running against this same repository (`CLAUDE.md` and `.mcp.json` are both
      tracked; `.mcp.json` carries the same Kanmer stdio server as `.codex/config.toml`).
      Its output must answer: *which of `.agents/skills`, `.codex/skills` and `.grok/skills`
      does Claude Code discover?* This is the whole discovery input
      [[TOOL-011]] (plan handle `DSK-12-11`) needs, and it costs one command here.

- [x] **Documentation re-fetch — body step 8.** Fetch
      <https://learn.chatgpt.com/docs/build-skills> (skill discovery roots) and
      <https://learn.chatgpt.com/docs/agent-configuration/subagents> (custom-agent TOML
      fields and the `[agents]` table) with a **direct web fetch** — both are
      `learn.chatgpt.com`, which `microsoft_docs_search` / `microsoft_docs_fetch` do not
      index. Record the fetch date beside each observed behaviour, and note any field the
      installed build does not honour. Last recorded fetch: 2026-08-23.

- [ ] **Verdict paragraph — body step 10.** Write it in the exact shape the acceptance
      criteria require, with no blanks left: *"Codex build `<version>` discovers skills from
      `<directories>` and does not discover `<directories>`; the agent roster is
      `<loaded | not loaded>` without an `[agents]` table."* Follow it with one sentence on
      the consequence for [[TOOL-002]] (which vendor destination is correct) and one for
      [[TOOL-004]] (plan handle `DSK-12-04`) (whether the `.codex/skills` copies can be
      deleted safely). This box is last because it is derived: it cannot be answered until
      every box above it is ticked.

## Parked (explicitly deferred)

- **Whether the one-list rule should be reopened if the build does scan `.codex/skills`.**
  Parked, not open: body step 11 settles it. `docs/desktop/12-agent-tooling/README.md` § 7
  makes a third copy of a skill a stop condition and `EPIC-013/context.md` § Traps repeats
  it, so the move to a single list is required under either answer. Record it as a
  consequence; do not reopen it as a decision.

- **Whether Claude Code needs an agent roster at all.** Parked, not open: it is
  [[TOOL-011]]'s decision. Body step 9 supplies only the discovery input, and a decision a
  named sibling ticket owns is a scope boundary rather than an open question.


## 2026-08-25 capture status

The Codex version, the concise `/skills` response, the one-entry `pegasus-release` observation, `pegasus-desktop` path, explicit `$winui-design` resolution, `/agent` response, and direct documentation fetches are recorded in `research`. The following remain unticked because they were not truthfully observable in this session: a complete unshortened `/skills` path listing, runtime proof of optional custom-agent field enforcement, Claude Code discovery across the three trees (Claude weekly limit), and the final no-blanks verdict. The ticket remains open and must not move to Done until those questions are answered by an authorized session.
