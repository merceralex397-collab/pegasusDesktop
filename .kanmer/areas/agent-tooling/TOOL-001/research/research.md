# Research — TOOL-001 (plan handle `DSK-12-01`): Codex skill and agent discovery on the conversion workstation

> **STATUS — INCOMPLETE. Do not move this ticket to Done yet.**
> This ticket is a `spike`; `get_doc_gates TOOL-001` owes `research` at `enter-done`, so
> this document *is* the deliverable, not an input to it. Everything under
> **Facts (repository, verified 2026-08-24)** is captured and needs no rework. Everything
> marked `NOT YET CAPTURED` needs the operator to run a command in a real Codex (and Claude
> Code) session and paste the verbatim output here. The verdict paragraph at the end is a
> fill-in template with blanks, and the blanks are the point of the ticket.

## Question

Does the Codex build actually installed on the conversion workstation discover skills from
`.codex/skills/`, from `.agents/skills/`, or from both — and does it load the eight custom
agents in `.codex/agents/` without an `[agents]` table in `.codex/config.toml`? Everything
that [[TOOL-002]] (`DSK-12-02`) vendors and everything [[TOOL-004]] (`DSK-12-04`) deletes
depends on the answer.

## Current behaviour

This ticket touches no web-application behaviour: there is no route, no Razor page model,
no `Pegasus.Core` use case and no Worker path involved. It is developer-toolchain
discovery.

**No `PAR-nn` row in `docs/desktop/01-inventory-and-parity/parity-matrix.md` covers it, and
none should.** That matrix has 47 rows keyed to `src/Pegasus.Web/Pages/**` page models and
their operator-visible capabilities (see the column notes at
`docs/desktop/01-inventory-and-parity/parity-matrix.md:36-44`); agent tooling is not an
operator capability and has no legacy web surface to reach parity with.

What "today" looks like on disk instead, verified 2026-08-24 at the repository root
`C:\Users\PC\Documents\GitHub\pegasusDesktop`:

| Tree | State | Tracked? |
| --- | --- | --- |
| `.codex/agents/` | 8 TOML files | all 8 tracked (`git ls-files .codex`) |
| `.codex/skills/` | 9 folders, 19 files, 7.9 MiB | only `.codex/skills/pegasus-release/SKILL.md` is tracked; the 8 `winui-*` folders are working-tree only |
| `.agents/skills/` | `pegasus-release/SKILL.md`, `project/pegasus-desktop/SKILL.md` | both tracked |
| `.grok/skills/` | 12 Kanmer skills, `.kanmer-skills-version` = `0.1.0` | tracked, but installed/reconciled by `kanmer-setup` |

## Findings

- The documented Codex scan root and the directory the WinUI skills actually live in are
  different directories, and nothing in the repository proves the installed build bridges
  the gap.
  - Documented root: `.agents/skills` (current directory up to the repository root),
    `~/.agents/skills`, `/etc/codex/skills` — recorded in
    `docs/desktop/00-governance-and-workflow/README.md` § 2 from
    <https://developers.openai.com/codex/skills> (fetched 2026-08-23) and in
    `docs/desktop/12-agent-tooling/README.md` § 2.
  - Actual location: `.codex/skills/winui-*` (8 folders).
- The eight `winui-*` folders are **untracked**, so the risk is larger than a discovery
  question: a fresh clone of this fork has no WinUI guidance at all.
- `.codex/config.toml` has no `[agents]` table today, so whether the roster loads at all is
  an observation, not a deduction.
- Two byte-identical copies of `pegasus-release` exist, which is why step 6(b) of the ticket
  asks whether `/skills` lists it once or twice — the answer tells you whether Codex
  de-duplicates by skill `name` or by path.
- Claude Code is a first-class tool on this repository, not a hypothetical: `CLAUDE.md`
  (tracked, one line, `AGENTS.md`) and `.mcp.json` (tracked, carries the same Kanmer stdio
  server as `.codex/config.toml`) are both committed. This is why step 9 costs one command
  and is the input [[TOOL-011]] (`DSK-12-11`) needs.

### Facts

Verified by reading the repository on 2026-08-24 (commands shown; all read-only):

| Fact | Evidence |
| --- | --- |
| `.codex/config.toml` declares `[features] apps = false`, `remote_plugin = false` and exactly two MCP servers, `mcp_microsoftdocs` (line 5) and `kanmer` (line 9, with `[mcp_servers.kanmer.env]` at line 13). **There is no `[agents]` table.** | `cat -n .codex/config.toml` — 15 lines total |
| `.codex/config.toml` also carries an uncommitted machine-local edit (absolute `C:\Users\PC\...` paths in `[mcp_servers.kanmer]`); `git status --porcelain` shows it as ` M`. | `git status --porcelain` |
| Eight agent TOMLs exist and are tracked: `pegasus-azure-auditor`, `pegasus-desktop-reviewer`, `pegasus-gateway-dev`, `pegasus-parity-researcher`, `pegasus-release-packager`, `pegasus-test-engineer`, `pegasus-ui-verifier`, `winui-dev`. | `git ls-files .codex` |
| `.codex/skills/` holds nine folders. Only `pegasus-release/SKILL.md` is tracked; the eight `winui-*` folders are untracked. | `git ls-files .codex`; `git status --porcelain` |
| The `.codex/skills` tree is 19 files / 7.9 MiB, of which 14 are Markdown. The weight is three binaries: `winui-design/winui-search.exe` 7,911,936 B; `winui-dev-workflow/analyzer/Microsoft.WindowsAppSDK.Analyzers.dll` 49,664 B; `winui-session-report/Analyze-Session.ps1` 45,966 B. | `find .codex/skills -type f \| wc -l`; `du -sh .codex/skills`; `ls -l` on each |
| `.agents/skills/pegasus-release/SKILL.md` and `.codex/skills/pegasus-release/SKILL.md` are both 13,299 bytes — the duplicate the one-list rule targets. | `ls -l` on both |
| `.agents/skills/project/pegasus-desktop/SKILL.md` exists, is tracked, is 110 lines, and its frontmatter `name` is `pegasus-desktop`. | `git ls-files .agents`; `head` |
| `.codex/skills/winui-setup/SKILL.md:1-5` carries frontmatter `name: winui-setup` and `disable-model-invocation: true`. A skill with that flag will not be auto-invoked even where it is discovered — so "did not fire" is not proof of "was not found". | `sed -n '1,8p'` |
| `.grok/skills/.kanmer-skills-version` is `0.1.0`; twelve Kanmer skills are present. | `cat`; `ls .grok/skills` |
| Claude Code is configured for this repository: `CLAUDE.md` and `.mcp.json` are both tracked. | `git ls-files` |

Official documentation to re-fetch (step 8 of the body; both are `learn.chatgpt.com`, which
`microsoft_docs_search` / `microsoft_docs_fetch` do **not** index — use a direct web fetch):

- <https://learn.chatgpt.com/docs/build-skills> — skill discovery roots, `SKILL.md`
  frontmatter, `[[skills.config]]` disabling, `$skill` mention. Last recorded fetch
  2026-08-23; re-fetch and record the new date.
- <https://learn.chatgpt.com/docs/agent-configuration/subagents> — custom-agent TOML fields
  (`name`, `description`, `developer_instructions`; optional `model`,
  `model_reasoning_effort`, `sandbox_mode`, `mcp_servers`, `skills.config`) and the global
  `[agents]` table. Last recorded fetch 2026-08-23; re-fetch and record the new date.

### Assumptions

- **A-12-1 — the installed Codex build scans `.agents/skills` as documented.**
  Confirmed by: the `/skills` listing showing `pegasus-desktop` and `pegasus-release` with
  `.agents/skills/...` as the discovered path.
  Breaks if wrong: the whole vendoring destination in
  `docs/desktop/12-agent-tooling/skills.lock.draft.json` (`vendorRoot: .agents/skills/vendor/`)
  is the wrong target and [[TOOL-002]] must be re-planned before it starts.
- **A-12-2 — the installed build also scans `.codex/skills`.** Unverified; this is the
  ticket's headline question.
  Confirmed by: `winui-*` names appearing in `/skills` with a `.codex/skills/...` path.
  Breaks if wrong: every plan-set claim that an agent "loaded `winui-design`" to date is
  unsupported, and the eight untracked folders have never done anything.
- **A-12-3 — the agent roster loads without an `[agents]` table.** Unverified.
  Confirmed by: `/agent` listing the eight names on today's config.
  Breaks if wrong: [[TOOL-005]] (`DSK-12-05`) is a hard prerequisite for
  [[TOOL-009]] (`DSK-12-09`) rather than a tidy-up, and no delegation has ever worked.
- **A-12-4 — the build honours `sandbox_mode` and `model_reasoning_effort`.** Unverified,
  and it is the one that matters for safety: the read-only guarantee for
  `pegasus-parity-researcher`, `pegasus-desktop-reviewer` and `pegasus-azure-auditor` is
  either enforced by the runtime or is only the prose in `developer_instructions`.
  Confirmed by: the build reporting the field, or a read-only agent visibly refusing a write.
  Breaks if wrong: record it for [[TOOL-005]] step 11 — the guardrail for
  [[TOOL-006]] (`DSK-12-06`)'s Azure MCP wiring becomes text plus discipline only.
- **A-12-5 — a skill that *lists* also *resolves*.** Unverified, which is exactly why body
  step 7 probes `$winui-design` explicitly. Note that `winui-setup` sets
  `disable-model-invocation: true`, so it may list and deliberately not auto-fire; that is
  not a discovery failure.

## Execution placement

Not applicable, and the heading is kept rather than dropped so the omission is visible: this
ticket places no responsibility in either the desktop or the cloud. It records what a local
developer toolchain does on one workstation, produces no runtime code path, and calls no
Azure tool. The six-question test in
`docs/desktop/00-governance-and-workflow/README.md` § 3 has nothing to be asked about here.

## Implications

1. **[[TOOL-002]] cannot pick a vendor destination until A-12-1 is confirmed.** The draft
   lockfile's `policy.vendorRoot` is `.agents/skills/vendor/`; if `/skills` shows nothing is
   read from `.agents/skills`, that destination is wrong and the lockfile changes before any
   sync script is written.
2. **[[TOOL-004]] is interlocked on this document, not merely sequenced after it.** Deleting
   `.codex/skills/winui-*` is irreversible in effect if the vendored copy is incomplete —
   the eight folders are untracked, so `git checkout` will not bring them back. The verdict
   paragraph is what unblocks that deletion.
3. **The one-list rule survives either answer.** If the build *does* scan `.codex/skills`,
   the move is still required — `docs/desktop/12-agent-tooling/README.md` § 7 makes a third
   copy of a skill a stop condition, and `EPIC-013/context.md` § Traps repeats it. Record
   that as a consequence, do not reopen it as a decision (ticket body step 11).
4. **Two copies today means the `/skills` count for `pegasus-release` is diagnostic.** One
   entry means Codex de-duplicates by skill `name`; two means by path, and every duplicate
   anywhere is a live ambiguity about which revision an agent read.
5. **The Claude Code answer is nearly free here and expensive later.** Step 9 costs one
   listing per tool and is the whole input to [[TOOL-011]]'s step 3.
6. **`disable-model-invocation: true` on `winui-setup` means "listed but not auto-invoked"
   is a valid, non-failing state.** Grade discovery by the listing and the explicit
   `$skill` probe, never by whether a skill fired on its own.

## Verdict paragraph — fill this in, do not paraphrase it

The ticket's acceptance criteria require this exact shape (body step 10):

> Codex build `<version>` discovers skills from `<directories>` and does not discover
> `<directories>`; the agent roster is `<loaded | not loaded>` without an `[agents]` table.

Follow it with one sentence for [[TOOL-002]] (which vendor destination is correct) and one
for [[TOOL-004]] (whether the `.codex/skills` copies can be deleted safely).

## Captured output

### `codex --version`

`NOT YET CAPTURED` — operator step (body step 3). Paste the exact output string and the
date captured.

### `/skills`

`NOT YET CAPTURED` — operator step (body step 4). Paste the **complete** listing including
the discovered path for each entry, and the date. The three questions it must answer:

- [ ] Do `winui-setup`, `winui-dev-workflow`, `winui-design`, `winui-code-review`,
      `winui-ui-testing`, `winui-packaging`, `winui-wpf-migration`, `winui-session-report`
      appear, and from which directory?
- [ ] Does `pegasus-release` appear once or twice?
- [ ] Does `pegasus-desktop` appear, from `.agents/skills/project/pegasus-desktop/`?

### `/agent`

`NOT YET CAPTURED` — operator step (body step 5). Paste the roster and the date. Expected:
the eight names from `.codex/agents/`. If it is empty or partial, *that is the finding* —
today there is no `[agents]` table.

### `$winui-design` explicit probe

`NOT YET CAPTURED` — operator step (body step 7). Record whether Codex resolves the mention
and from which file. A name that lists but does not resolve is a different failure from one
that never lists.

### Claude Code discovery (body step 9)

`NOT YET CAPTURED`. Record, for Claude Code running against this same repository, which of
`.agents/skills`, `.codex/skills` and `.grok/skills` it discovers.

### Documentation re-fetch (body step 8)

`NOT YET CAPTURED`. Record the fetch date beside each observed behaviour and note any TOML
field the installed build does not honour.

## Open questions

None are opened as a blocking `open-questions` document, deliberately: an unticked
`- [ ]` line there blocks *every* stage move, and the questions below are the spike's own
subject matter — blocking this ticket from starting in order to ask it to start would be
circular. They are carried here instead, and each has a named destination:

- Whether the installed build honours `sandbox_mode` / `model_reasoning_effort` (A-12-4).
  If it honours neither, record it for [[TOOL-005]] step 11 as an open question **there**,
  where it is actionable, per the ticket body's "Open question to carry" guardrail.
- Whether `/skills` de-duplicates by name or by path. Answered by the `pegasus-release`
  count; no separate work if it de-duplicates by name.
- Whether Claude Code needs a roster at all. Not decided here — it is [[TOOL-011]]'s
  decision, and step 9 only supplies the discovery input.
