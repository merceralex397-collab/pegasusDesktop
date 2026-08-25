# Plan — FND-026: author ADR-0100 (native WinUI 3 client) and ADR-0104 (online-required)

**Diff estimate: ~4 files, ~240 lines** — 2 new ADR files (~235 lines) plus ~5 modified
lines across two existing files.

`docs/engineering.md` § Plan sizing requires the estimate first. This is a `chore`, so
it owes no `research` and no `files` document and this plan carries the surface area
alone. The estimate is derived from the measured inventory below, not asserted.

### Measured surface-area inventory

Measured in `C:\Users\PC\Documents\GitHub\pegasusDesktop` at `bbd1c549`, 2026-08-24.

| Path | Measured current value | Change |
| --- | --- | --- |
| `docs/adr/0100-native-winui-3-client-in-the-fork.md` | **does not exist** — `ls docs/adr/` returns `0001`…`0029` (0017 never issued) plus `README.md`, 28 ADR files in all; the reserved block ADR-0100…ADR-0110 is entirely empty | new, ~140 lines |
| `docs/adr/0104-online-required-bounded-local-cache.md` | **does not exist** (same command) | new, ~95 lines |
| `docs/adr/README.md` | 46 lines; `grep -c '^\| '` → **32**; accepted table header at `:18` (`\| ADR \| Title \| Related FRD \|`), separator `:19`, 22 data rows `:20-41` ending with the ADR-0029 row at **`:41`**; the "Superseded and relocated" table header at `:45` | +2 rows after `:41`; count becomes 34 |
| `docs/desktop/README.md` | § Status table holds **one** row — `\| 00–12 \| Drafted 2026-08-23 \| Awaiting first ticket creation on the fork's Kanmer board (see 00) \|` — not one row per area | +1 row or an annotation; see Risks |

The ~235-line figure for the two new files is grounded in the repository's own recent
ADRs: `wc -l docs/adr/0014-*.md` → 28, `0015-*.md` → 66, `0026-*.md` → 70,
`0028-*.md` → 84. ADR-0100 lands above that band because it must carry the six-row
cloud-justification table, the seven authorised project names, two recorded deviations,
the D-001 consequence line and the "prior documents" sentence; ADR-0104 lands near the
top of it because of proposal §11.1's permitted-local-state list.

Nothing under `src/`, `tests/`, `scripts/` or `.github/` is created or edited.

### Measured: the frontmatter shape to copy

`sed -n '1,10p' docs/adr/0026-enable-automation-mcp-by-explicit-deployment-configuration.md`:

```yaml
---
id: ADR-0026
status: accepted
date: 2026-08-18
supersedes: []
superseded_by: []
related_capabilities: [MCP-01, MCP-02, MCP-03, MCP-04, MCP-06]
related_frd: [frd-10, frd-11]
tags: [mcp, automation, deployment]
---
```

Eight keys, in that order. The body's step 2 writes "`supersedes_by`→`superseded_by`";
the real key is **`superseded_by`**, as shown. Body headings follow with
`Status · Context · Decision · Consequences · Options considered (optional) · Links`,
Status first (`AGENTS.md:107-109`).

### Measured: the ADR-0009 supersession precedent

`sed -n '70,80p' docs/adr/0009-adopt-pegasus-monorepo-workspaces.md`:
`:74` is the deferral clause — "The future desktop workbench remains deferred until
the Web capability is complete." `:77-80` is the repository's own partial-supersession
pattern — "This decision supersedes ADR-0002 **only where** ADR-0002 implies … ADR-0002's
runtime, dependency direction, one Core, one database, one migration stream and
four-project production boundary remain accepted." ADR-0009 itself carries
`supersedes: []` and ADR-0002 keeps `status: accepted` with an empty `superseded_by`.
ADR-0100 copies that shape exactly.

## Approach

Write both records as **new files in the reserved block**, with `supersedes: []` on
ADR-0100 and the ADR-0009 supersession stated in prose in `## Context`, then add two
rows to the accepted index table. The alternative — writing
`supersedes: [ADR-0009]` in frontmatter because ADR-0100 does supersede something in
ADR-0009 — is rejected because in this repository frontmatter `supersedes` is the
**full**-supersession relation (ADR-0029 / ADR-0013), and using it would take ADR-0009
out of the accepted table and silently retire the whole workspaces decision. The
repository has already solved this exact problem once, at
`docs/adr/0009-...:77-80`, and copying its solution is cheaper and safer than
inventing a partial-supersession convention.

The second decision is sequencing: because a published ADR body is **immutable**
(`AGENTS.md` § ADR conventions), every text other tickets owe ADR-0100 must be in the
body **before** the acceptance flip, never patched in afterwards. That makes this a
coordination job as much as an authoring one — see step 4 and Risks.

## Governing docs

The ticket's `refs` is **empty** and `docs_todo: true` — confirmed by
`get_doc_gates FND-026`. Profile `chore` has no `leave-backlog` boundary on this board,
so `docs_todo` satisfies no gate here; it states honestly that no *existing*
`docs/(prd|frd|adr)` document is implemented by this work, because the documents this
ticket meets are the two it creates.

> **New ADR — ADR-0100** (native WinUI 3 desktop client in the fork; authorises the new
> top-level projects). **Co-claimed.** [[FND-005]] (plan handle `DSK-00-05`) also
> claims ADR-0100 and ADR-0104. The reconciliation, stated identically in both tickets'
> bodies: **whichever ticket runs first writes the file; the other verifies and creates
> no second file for either number.** Step 1's `ls docs/adr/0100-*.md docs/adr/0104-*.md`
> is how that is detected — not prose. Two further points are already reconciled and
> [[FND-005]] steps 2, 3 and 6 state them identically: **one filename**,
> `docs/adr/0100-native-winui-3-client-in-the-fork.md` (one ADR ID, one file); and **one
> ADR-0009 rule**, `supersedes: []` with the deferral-clause supersession stated in
> `## Context`, leaving ADR-0009's body *and* frontmatter untouched and its
> `status: accepted` intact.
>
> **New ADR — ADR-0104** (online-required; no offline replication; bounded local cache
> only). Same co-claim and the same first-writer-wins rule.
>
> This plan is written to the decisions as recorded in
> `docs/desktop/02-architecture-and-foundation/README.md` § 3 (decisions 1–10) and
> `docs/desktop/00-governance-and-workflow/README.md` § 3. If either ADR lands
> differently, this plan is revised before implementation.

Because `refs` is empty, the programme-level authorities that bind today are listed
with the step that satisfies each. `kanmer-review` checks this table against the diff.

| Authority | Requirement | Met by |
| --- | --- | --- |
| `AGENTS.md:235` § Product invariants | No new top-level project, runtime, deployment unit or migration stream without an accepted ADR proving the existing boundary cannot carry it | ADR-0100's Decision (step 3) — the seven authorised project names |
| `AGENTS.md:77-89` § ADR conventions (reserved block) | Stable IDs, never renumber or reuse; the conversion uses **ADR-0100…ADR-0110** rather than the next free number, operator-confirmed 2026-08-23 | Steps 1, 3, 6; the collision check |
| `AGENTS.md:92-109` § ADR conventions (frontmatter and template) | Eight frontmatter keys; body headings `Status · Context · Decision · Consequences · Options considered · Links`, Status first | Step 2 |
| `AGENTS.md` § ADR conventions ("Keep ADRs durable") | No dated cost tables, retail prices or historical runbooks in an ADR | Step 7 |
| `docs/desktop/00-governance-and-workflow/README.md` § 3 | The six-question cloud-justification table, copied **verbatim** and **answered** — prose instead of six answers is a defect | Step 5 |
| Proposal § 5.4 | Recommended solution structure | ADR-0100's Decision and the two recorded deviations (step 4) |
| Proposal § 7.1 | Runtime baseline — WinUI 3 on Windows App SDK 2.x stable, Windows 11 x64 | Step 3 |
| Proposal § 11.1 / § 11.2 | The permitted local-state list; no replicated case database and no synchronisation engine | Step 6 |
| L-01 | The gateway is `Pegasus.Web` evolved in place, so ADR-0100 must **not** describe a new deployment unit | Step 3 |
| L-02 / ADR-0014 | Test/UAT is a local stack; ADR-0104 must not imply an Azure test environment | Step 6 |
| D-001 (decided 2026-08-23) | The fork becomes the single release source at the first production gateway change | Step 4's Consequences line; wording owned by [[FND-010]] (plan handle `DSK-00-10`) |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff, recorded under a dated heading in this plan | § Simplification pass below |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing → reviewer |
| `AGENTS.md:119` § New Markdown placement | No new `.md` outside `docs/(prd\|frd\|adr\|design\|desktop)` | Both new files are under `docs/adr/` |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory
in the plan document.

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
  (read-only; **it reviews, an implementing agent writes the files**).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-docs`
  (`.grok/skills/kanmer-docs/SKILL.md`) → `microsoft-docs` (Microsoft Learn plugin,
  **only** for the Windows App SDK support-lifecycle claims in ADR-0100).
- **MCP**: Kanmer — `get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`, `link_doc`; Microsoft Learn — `microsoft_docs_search`,
  `microsoft_docs_fetch`.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gated boundaries confirmed by
  `get_doc_gates FND-026`: `leave-preparing` (`plan`, `questions-resolved`) and
  `enter-done` (`proof`, `questions-resolved`). Call `get_doc_gates FND-026` before
  every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's ten implementation steps: same order, same ownership,
same file paths, with the measured current values a step must be checked against.

1. **Orient, take, then check for a collision before writing anything.** Read the plan
   row and `docs/desktop/02-architecture-and-foundation/README.md` §§ 3–4 and § 8,
   `docs/desktop/00-governance-and-workflow/README.md` § 3, and `AGENTS.md`
   § ADR conventions (`:77`) and § Product invariants (`:235`) in full. Call
   `get_doc_gates FND-026` and `take_ticket` with the real branch and worktree —
   `task/adr-0100-0104`, `../pegasus-worktrees/adr-0100-0104`, branched from
   `origin/dev`. Then run `ls docs/adr/0100-*.md docs/adr/0104-*.md`. **Measured today:
   neither exists** (`ls docs/adr/` returns `0001`…`0029` plus `README.md`). If either
   does exist when this runs, it was authored by [[FND-005]] (plan handle `DSK-00-05`)
   — this ticket then **verifies** that it covers the material below and extends it in
   place, and creates no second file for either number. Record the result of the check
   either way; it is the detection mechanism, not the prose above it.
2. **Copy the frontmatter shape exactly.** From
   `docs/adr/0026-enable-automation-mcp-by-explicit-deployment-configuration.md:1-10`:
   `id`, `status`, `date`, `supersedes`, `superseded_by`, `related_capabilities`,
   `related_frd`, `tags` — eight keys, that order. The key is **`superseded_by`**.
   Body headings: `Status · Context · Decision · Consequences · Options considered
   (optional) · Links`, Status first.
3. **Create `docs/adr/0100-native-winui-3-client-in-the-fork.md`** with `id: ADR-0100`,
   `status: accepted`, today's date, and **`supersedes: []`**. Do **not** list ADR-0009
   in frontmatter: in this repository that key is the full-supersession relation
   (ADR-0029 / ADR-0013) and would take ADR-0009 out of the accepted table. Instead say
   in `## Context` that ADR-0100 supersedes **only** the deferral clause at
   `docs/adr/0009-adopt-pegasus-monorepo-workspaces.md:74` ("The future desktop
   workbench remains deferred until the Web capability is complete") and leaves the
   workspaces decision intact — the same pattern ADR-0009 itself uses at `:77-80`
   against ADR-0002. State also that **ADR-0014 and ADR-0016 are unchanged**. The
   Decision text must state: native WinUI 3 on Windows App SDK **2.x stable**,
   Windows 11 **x64**, `net10.0-windows10.0.26100.0`, packaged **single-project MSIX**,
   **self-contained**, **no WebView shell**, and the seven authorised new top-level
   projects — `src/Pegasus.Contracts`, `src/Pegasus.Desktop`,
   `src/Pegasus.Desktop.Infrastructure`, `tests/Pegasus.Desktop.ViewModelTests`,
   `tests/Pegasus.Api.ContractTests`, `tests/Pegasus.Desktop.UITests`,
   `tests/Pegasus.Packaging.Tests`. Use `microsoft_docs_search` /
   `microsoft_docs_fetch` for any Windows App SDK support-lifecycle claim and record the
   URL with its fetch date; do not answer it from memory.
4. **Record in ADR-0100's Consequences the four texts it owes before acceptance.**
   (a) *Deviation:* `Pegasus.Core` stays one project holding Domain + Application,
   because `AGENTS.md:235` § Product invariants names one Core owner of business policy
   and `docs/engineering.md` § Abstractions forbids a split without a second concrete
   need — measured corroboration: `src/Pegasus.Core/Pegasus.Core.csproj` is 14 lines
   with **zero** `PackageReference` items and one
   `InternalsVisibleTo Include="Pegasus.Core.Tests"`. (b) *Deviation (additive):* the
   server projects stay Linux-publishable through a solution filter — [[FND-028]] (plan
   handle `DSK-02-03`). (c) The **D-001** consequence line: the fork becomes the single
   release source at the first production gateway change; [[FND-010]] (plan handle
   `DSK-00-10`) owns the wording in `docs/operations.md`. (d) The sentence that the
   proposal's three "prior documents" are **not** in this repository and are not an
   input — [[FND-013]] (plan handle `DSK-00-13`). **A published body is immutable**, so
   (c) and (d) must be coordinated into this PR; if they cannot be, record the cost of a
   superseding ADR rather than patching after acceptance.
5. **Answer the cloud-justification test inside ADR-0100.** Copy the six-row table
   **verbatim** from `docs/desktop/00-governance-and-workflow/README.md` § 3 — the rows
   are `Shared authority`, `Unattended execution`, `Protected credentials`,
   `Public callback`, `Central enforcement`, `Measured operational advantage` — and
   fill a yes/no **and** an evidence cell for each. Prose instead of six answers is a
   defect. Remember what a "yes" means on this programme: it names *where* the
   responsibility lands, not "in Azure".
6. **Create `docs/adr/0104-online-required-bounded-local-cache.md`** with
   `id: ADR-0104`, `status: accepted`. Decision: the desktop is online-required, and
   the only permitted local state is proposal §11.1's list — access token in memory;
   refresh/session token in the DPAPI store; window position, theme, grid columns and
   preferences; small reference-data snapshots; thumbnails; temporary document working
   copies; optionally encrypted drafts for approved long forms; the last signed
   compatibility response for a bounded period; and rolling redacted diagnostic logs.
   Consequences must name proposal §11.2 explicitly: **no replicated case database and
   no synchronisation engine**; SQLite or a comparable durable cache is added only after
   profiling proves server queries plus memory caching cannot meet the target. L-02 and
   ADR-0014 stand, so nothing here may imply an Azure test environment.
7. **Add a Links section to each ADR** pointing at
   `docs/desktop/02-architecture-and-foundation/README.md` and the proposal sections
   used. Put **no** dated cost tables, prices or runbook steps in either ADR
   (`AGENTS.md` § ADR conventions, "Keep ADRs durable").
8. **Add exactly one index row per ADR to `docs/adr/README.md`.** The accepted table's
   real columns are `ADR | Title | Related FRD` — **three cells, no status column** —
   header at `:18`, separator at `:19`. Ignore the `AGENTS.md:115` sentence describing a
   five-column index (`ID | Title | Status | Superseded-by | Owner capability`): the
   real file contradicts it and **the file wins**, or this step writes five-cell rows
   into a three-column table. Correcting that `AGENTS.md` sentence is owned by
   [[FND-005]] (plan handle `DSK-00-05`), not by this ticket. Follow the existing rows
   exactly:
   `| [0100](0100-native-winui-3-client-in-the-fork.md) | Native WinUI 3 desktop client | FRD-13 |`
   and the same shape for `0104`, whose third cell is its `related_frd` as plain text or
   `—` where it has none, as the ADR-0002 row does. **Place both immediately after the
   ADR-0029 row, which is `docs/adr/README.md:41`.** Do not renumber anything and do not
   touch the "Superseded and relocated" table, whose header is at `:45`.
9. **Run both documentation gates from the repository root.**
   `pwsh ./scripts/Test-DocumentationLinks.ps1` and
   `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — both files exist
   (`ls scripts/Test-*.ps1`). Note for the implementer: the script the CI `documentation`
   job runs as the **placement gate** is `scripts/Test-MarkdownPlacement.ps1`;
   `Test-TestMarkdownPlacement.ps1` is its self-test. Run both, and treat
   `Test-MarkdownPlacement.ps1` exiting 0 as the evidence for the body's parenthetical
   "both new files are under `docs/adr/`, an allowed root". A broken relative link
   inside either ADR fails the CI `documentation` job.
10. **Record the simplification pass, open the PR, hand over the review.** Write
    `n/a — docs-only` under a dated `## Simplification pass` heading in this plan
    (`AGENTS.md` § Repository task workflow step 4), open the PR into `dev`, and hand the
    review to `pegasus-desktop-reviewer`.

## Verification

`proof` is produced from the outputs below. Evidence tier: **Tier 1 —
Static/build/architecture** (`docs/engineering.md` § Required evidence tiers). This
proves consistency only — the documents exist, their frontmatter parses and the link
checker passes. It proves nothing about implementation.

- `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exit code 0, no broken link
  reported.
- `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected: exit code 0. Run
  `pwsh ./scripts/Test-MarkdownPlacement.ps1` alongside it — expected: exit code 0,
  which is the actual evidence that both new files sit under an allowed root.
- `grep -c '^| ' docs/adr/README.md` before and after — **measured baseline: 32**;
  expected after: **34**, i.e. the table grows by exactly 2 rows and nothing else
  changes.
- `ls docs/adr/0100-*.md docs/adr/0104-*.md` — expected: exactly one file per number.
- `sed -n '1,10p'` over each new ADR — expected: the eight frontmatter keys in the
  ADR-0026 order, `status: accepted`, and `supersedes: []` on ADR-0100.
- `grep -n "status: accepted" docs/adr/0009-adopt-pegasus-monorepo-workspaces.md` and
  `grep -n "superseded_by" docs/adr/0009-*.md` — expected: unchanged; ADR-0009 keeps
  `status: accepted` and an empty `superseded_by`, and its body is untouched.

## Risks / open questions

- **Risk — the co-claim produces two files for one ADR id.** [[FND-005]] (plan handle
  `DSK-00-05`) claims ADR-0100 and ADR-0104 as well. *Mitigation:* step 1's
  `ls docs/adr/0100-*.md docs/adr/0104-*.md` runs **before** anything is written, and
  the reconciliation is first-writer-wins with one filename per ADR id. Measured today:
  neither file exists, so this ticket would be the first writer.
- **Risk — `supersedes: [ADR-0009]` in frontmatter.** That is the full-supersession
  relation in this repository and would drop ADR-0009 out of the accepted table.
  *Mitigation:* step 3's explicit rule, with `docs/adr/0009-*.md:77-80` as the
  repository's own precedent for a prose partial supersession.
- **Risk — five-cell rows in a three-column table.** `AGENTS.md:115` describes an index
  shape the real file does not have. *Mitigation:* step 8 follows the file
  (`docs/adr/README.md:18`), and correcting `AGENTS.md` is [[FND-005]]'s scope.
- **Risk — a text owed by another ticket arrives after acceptance.** ADR bodies are
  immutable once accepted, so the D-001 line ([[FND-010]], plan handle `DSK-00-10`) and
  the "prior documents" sentence ([[FND-013]], plan handle `DSK-00-13`) must be in the
  body **before** the acceptance flip. *Mitigation:* step 4 coordinates both into this
  PR; if that fails, record the cost of a superseding ADR rather than patching.
- **Risk — an upstream ADR-number collision.** Upstream keeps issuing ADRs below 0100
  and every sync can bring one. *Mitigation:* the reserved block ADR-0100…ADR-0110 is
  currently empty on the fork, and `docs/adr/README.md` is re-checked after every
  upstream sync — [[FND-023]]'s (plan handle `DSK-01-10`) step 8.
- **Finding — `docs/desktop/README.md` § Status has no per-area row.** The ticket's
  Documentation changes says "area 02 status row changes to 'in progress'", but the
  table holds a **single** row covering `00–12`. *Recommended default:* split out one
  `02` row rather than rewriting the collective row, so the other twelve areas keep
  their recorded state; record the choice in the PR. This is one line either way and is
  inside the ticket's own documentation-changes scope.
- **Finding to hand on — the Verification block names the placement script's
  self-test.** `scripts/Test-TestMarkdownPlacement.ps1` exists and exits 0 without
  proving anything about the two new files' placement; the gate CI runs is
  `scripts/Test-MarkdownPlacement.ps1`. Step 9 runs both. Correcting the ticket text is
  owned by [[FND-052]] (board grooming — unrunnable verification commands).
- **Open question — is FRD-13 the right `related_frd` for ADR-0100?** The index row in
  step 8 writes `FRD-13`, and FRD-13 ("Desktop operator experience") is authored by
  [[FND-008]] (plan handle `DSK-00-08`), which has not run. **Answered by:** the
  implementer at step 8 — if FRD-13 does not exist yet, write `—` in the third cell as
  the ADR-0002 row does, and let [[FND-008]] add the link when it lands. Recorded here
  rather than as an unticked `open-questions` box because the default is trivial and is
  taken, not asked.
- **Not an open question — the reserved ADR block.** ADR-0100…ADR-0110 was
  operator-confirmed on 2026-08-23 and `AGENTS.md:84-89` records it. Do not re-derive
  it and do not take "next free number".
- **Not an open question — which ADRs this ticket authors.** ADR-0101, ADR-0102,
  ADR-0103, ADR-0105, ADR-0106, ADR-0107, ADR-0109 and ADR-0110 belong to [[FND-005]]
  (plan handle `DSK-00-05`) and [[FND-006]] (plan handle `DSK-00-06`); ADR-0108 belongs
  to [[FND-007]] (plan handle `DSK-00-07`). This ticket writes two files and no more.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. Expected result:
`n/a — docs-only`, since the branch adds two Markdown files and edits two more._

## Execution result — 2026-08-25

This ticket's co-claimed authoring work was already completed by FND-005 before FND-026 was taken. Live origin/dev verification found the exact canonical files and index rows:

- docs/adr/0100-native-winui-3-client-in-the-fork.md and docs/adr/0104-online-required-bounded-local-cache.md exist at accepted status on merged dev commit 5770eb21c0d03620a6a6d99e0431bde91ec2ad6a.
- Both ADRs contain the six required cloud-justification rows; ADR-0100 records the reserved native-client decision and leaves ADR-0014 and ADR-0016 unchanged; ADR-0104 records online-required bounded local state with no replication.
- docs/adr/README.md contains exactly one row for ADR-0100 and one for ADR-0104.
- pwsh ./scripts/Test-DocumentationLinks.ps1 passed: 232 files checked.
- pwsh ./scripts/Test-TestMarkdownPlacement.ps1 passed.
- gh pr view 1 identifies the authoring PR as merged into dev with merge commit 5770eb21c0d03620a6a6d99e0431bde91ec2ad6a.

No distinct FND-026 authoring change remains. This ticket is archived as a duplicate/non-actionable authoring item, with the merged FND-005 commit and the live validation above as evidence. No dependency links were changed.
