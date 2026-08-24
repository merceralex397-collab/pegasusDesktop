# Plan — FND-005: Author ADR-0100, ADR-0101, ADR-0103, ADR-0104, ADR-0105 and ADR-0110 in the reserved block

**Diff estimate: ~9 files, ~640 lines.**

`docs/engineering.md:201-207` § Plan sizing requires the estimate first, derived
rather than shrugged. From the `files` document: six new ADRs at the measured
house length (ADR-0015 is 66 lines, ADR-0028 84, ADR-0025 114 — six decisions
each carrying the eight-row cloud-justification table land near 100 lines, so
~600), plus six index rows in `docs/adr/README.md` (~6 lines) and two one-line
corrections in `docs/desktop/00-governance-and-workflow/README.md:422` and
`AGENTS.md:114-117` (~4 lines with their wrap). Nine files, ~640 lines.

## Approach

Write all six ADRs in one PR rather than one ADR per PR, and carry the two
governance corrections in the same change set. The six are a single coherent
decision set — ADR-0101 defines the placement test that ADR-0103, ADR-0104 and
ADR-0105 each answer, so splitting them would merge a rule and its first
applications at different times and leave the intermediate state citing a
document that does not exist. The rejected alternative was six PRs sequenced by
dependency: it triples the review load on identical material and, worse, invites
the ADR-0100 immutability trap — the last PR would want to amend the first.

The two corrections travel with the ADRs for the same reason. `AGENTS.md:115`
tells an author to write a five-cell index row; `docs/adr/README.md:18-19` has
three cells. This ticket is the first to add rows at scale, so it is the ticket
that would either propagate the defect or fix it. Likewise the plan 00 § 8 row
at `:422` still instructs an ADR-0009 `superseded_by` edit that step 6
deliberately does not make.

## Governing docs

`refs` is empty and `docs_todo: true` — confirmed by `get_doc_gates FND-005`,
which for profile `feature` shows `leave-backlog: [governing-doc]` satisfied by
`docs_todo`, and `leave-preparing: [research, files, plan, checklist,
questions-resolved]`.

> **New ADRs — this ticket authors them.** ADR-0100 (native WinUI 3 client in the
> fork), ADR-0101 (local-execution / cloud-authority split), ADR-0103 (gateway,
> not direct database access), ADR-0104 (online-required, bounded local cache),
> ADR-0105 (signed MSIX/App Installer plus the gateway minimum-version gate) and
> ADR-0110 (agent-skill pinning and the invocation protocol).
> ADR-0100 and ADR-0104 are co-claimed by [[FND-026]] (plan handle `DSK-02-01`);
> ADR-0105 by [[REL-001]] (plan handle `DSK-09-01`) **and** [[FND-042]] (plan
> handle `DSK-04-01`); ADR-0110 by [[TOOL-008]] (plan handle `DSK-12-08`). Where
> a number has more than one claimant this plan says
> `authored by whichever of the claimants is worked first; see step 2 for the
> ownership reconciliation` rather than asserting a single author.
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (the ADR set table)
> and `docs/desktop/README.md` § Locked decisions; if the operator's ADR-0105
> answer lands differently this plan is revised before implementation.

Because `refs` is empty, the authorities that bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `AGENTS.md:78-83` | Stable IDs; supersede by a new ADR, never renumber or delete | Steps 3, 6 |
| `AGENTS.md:84-90` | The operator-confirmed reserved block ADR-0100–ADR-0110 | Steps 1, 3 |
| `AGENTS.md:91-92` | One decision per ADR, not a bundle | Step 8 (six files, six decisions) |
| `AGENTS.md:94-108` | The YAML frontmatter block, verbatim in shape | Step 4 |
| `AGENTS.md:109-110` | Heading set `Status · Context · Decision · Consequences · Options considered · Links`, Status first | Step 5 |
| `AGENTS.md:111-113` | No dated cost tables or runbooks in an ADR; feature behaviour belongs in an FRD | Step 8; and the FRD boundary is [[FND-008]]'s |
| `docs/adr/README.md:12-14` | Published bodies are immutable | Step 7 (everything ADR-0100 will say is in it before merge) |
| `docs/adr/README.md:18-19` | Index columns `ADR \| Title \| Related FRD` | Step 10 |
| Proposal § 26 Documentation set; § 27 item 18 | The decision set exists, and every deviation has a recorded justification | Steps 3–8 |
| Proposal § 4 / plan 00 § 3 | The six-question test answered with evidence, never prose | Step 5 |
| L-01 | Gateway is `Pegasus.Web` evolved in place | ADR-0103's decision text (step 8) |
| L-02 | Test/UAT is local; **ADR-0014 is not superseded** | Step 9 |
| D-002 (2026-08-23) | Self-managed certificate, trusted per workstation in `LocalMachine\TrustedPeople` | ADR-0105 (step 8) |
| D-003 (2026-08-23) | In-house UNC share over SMB; no Azure resource | ADR-0105 (step 8) |
| C-01 (2026-08-23) | Repositories become private; GitHub Releases/Pages ruled out permanently | ADR-0105 (step 8) |
| D-001 (2026-08-23) | The fork becomes the single release source; upstream merged then frozen | ADR-0100 `## Consequences` (step 7), text owned by [[FND-010]] |
| `scripts/Test-MarkdownPlacement.ps1:31` + `.github/workflows/ci.yml:71-87` | Placement and link gates on every change set | Step 11 |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § "Ticket template" requires
of the plan document specifically.

- **Subagent**: `pegasus-parity-researcher` —
  `.codex/agents/pegasus-parity-researcher.toml` (verified present). Read-only
  evidence gathering for each ADR's `## Context`; it cannot write files, so its
  answer is pasted into the ADR by the ticket owner.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-docs`
  (`.grok/skills/kanmer-docs/SKILL.md`) → `microsoft-docs` (Microsoft Learn
  plugin) for any API claim.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `link_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`, `microsoft_docs_fetch`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-005` before every move; a move crosses at most one gated
  boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's thirteen implementation steps; order, ownership and file
paths are the body's.

1. **Orient, then run the collision check before writing a character.** Read
   `docs/desktop/00-governance-and-workflow/README.md` § 3, `AGENTS.md:77-118`,
   and `docs/adr/0029-*.md` plus `docs/adr/0028-*.md` for the current house form
   (both open at `## Status`; the older 0014/0015/0025 do not — copy the newer).
   Call `get_doc_gates FND-005`, then `take_ticket`. Then:
   ```
   ls docs/adr/010*
   ```
   One command covering all six numbers. **Measured 2026-08-24: no such file** —
   the highest ADR in the tree is 0029. If an ADR-0110 file is there,
   [[TOOL-008]] authored it: verify it covers step 8's skill-pinning content and
   extend it in place; create no second ADR-0110. If ADR-0100 or ADR-0104 exists,
   [[FND-026]] authored it. If ADR-0105 exists, [[FND-042]] or [[REL-001]] did.
   Same rule in every case. Record the command's result in the proof.
2. **Settle ownership before writing.** ADR-0105 has three claimants: this
   ticket, [[REL-001]] and [[FND-042]]. All three name **one filename**,
   `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md` — the only
   ADR-0105 path the plan set itself names, at
   `docs/desktop/04-auth-session-update-and-startup/README.md:297` — and **one
   rule**: whichever is worked first authors the file, the other two verify it
   covers their content and extend it in place, never a second file for the same
   number. Which of the three authors it is an **ownership question for the
   operator to settle before Phase 2**; it is tracked as an unticked blocking box
   on [[REL-001]]'s `open-questions` document (that ticket's body instructs the
   record there). **This** ticket's body instructs the answer be recorded in this
   plan, so write it here, verbatim and dated, when it arrives — and say whether
   it confirmed the tie-break or named a different author. Do not decide it
   silently. ADR-0110's co-claim with [[TOOL-008]] is reconciled the same way and
   carries no operator question.
3. **Create the six files** under `docs/adr/`, matching the existing
   `NNNN-kebab-title.md` pattern:
   `0100-native-winui3-desktop-client.md` (the single path [[FND-026]] also
   names), `0101-local-execution-cloud-authority-split.md`,
   `0103-gateway-not-direct-database-access.md`,
   `0104-online-required-no-offline-replication.md`,
   `0105-msix-app-installer-and-minimum-version-gate.md`,
   `0110-pin-agent-skills-and-invocation-protocol.md`.
4. **Frontmatter**, verbatim in shape from `AGENTS.md:94-108`:
   ```yaml
   ---
   id: ADR-0100
   status: accepted
   date: <the date it is accepted>
   supersedes: []
   superseded_by: []
   related_capabilities: []
   related_frd: []
   tags: []
   ---
   ```
   `status: accepted` for all six — ADR-0108 is the only `proposed` one and it
   belongs to [[FND-007]]. **House-style trap:** `related_frd` values in this
   repository are lowercase file stems — `[frd-08]`, `[frd-10, frd-11]`,
   `[frd-01, frd-02, frd-05, frd-06, frd-12]`. There is no `[FRD-11]` anywhere in
   `docs/adr/*.md`. Same for any `related_capabilities` entry, which uses the
   display form `[INT-17, INT-28]`.
5. **Body shape and the answered test.** Use
   `## Status · ## Context · ## Decision · ## Consequences · ## Options considered
   · ## Links`, following `docs/adr/0029-*.md:11-20` — H1 `# ADR-01NN: <title>`,
   then `## Status` opening with "Accepted." and any supersession sentence. Put
   the proposal Appendix A cloud-justification table verbatim inside `## Context`
   of each ADR:
   | Question | Answer (yes/no) | Evidence |
   | --- | --- | --- |
   | Shared authority — must several users see and update the same state? | | |
   | Unattended execution — must it run with every desktop closed? | | |
   | Protected credentials — long-lived secret that must not sit on workstations? | | |
   | Public callback — must an external service call a stable public endpoint? | | |
   | Central enforcement — revocation, permissions, audit, invariant independent of the client? | | |
   | Measured operational advantage — measured evidence central is materially better? | | |
   Every row gets a real answer and a real citation. All six "no" means the
   responsibility belongs in the desktop. **A "yes" names *where* the
   responsibility lands; it does not mean "in Azure".** For ADR-0105 "protected
   credentials" is *yes* and lands on an **in-house** signing host under D-002,
   with the feed on an in-house UNC share under D-003 — cite those decisions.
   "It is already in Azure" is not an answer.
6. **ADR-0100's two extra obligations.** Restate the reserved block
   ADR-0100…ADR-0110 and the operator confirmation of 2026-08-23
   (`AGENTS.md:84-90`), and record that it supersedes **only** the deferral
   clause of ADR-0009 at
   `docs/adr/0009-adopt-pegasus-monorepo-workspaces.md:73-74` ("The future
   desktop workbench remains deferred until the Web capability is complete"),
   leaving ADR-0009's workspaces decision and ADR-0016 unchanged. The single
   agreed rule — [[FND-026]] step 3 states it identically — is: keep
   `supersedes: []` in ADR-0100's frontmatter and write the clause-level
   supersession as a sentence in `## Context`. This is the repository's own
   pattern: ADR-0009 supersedes ADR-0002 only in part (`0009:76-77`) and still
   carries `supersedes: []` (`0009:5`), while ADR-0002 keeps `status: accepted`
   with an empty `superseded_by`. Do **not** write `supersedes: [ADR-0009]`: in
   frontmatter that is the full relation (ADR-0029/ADR-0013, `0029:5` with
   ADR-0013 in the `## Superseded and relocated` table at
   `docs/adr/README.md:50`), whose symmetric consequence is `status: superseded`
   on ADR-0009 and its removal from the accepted table — which is not the
   decision. ADR-0009 is left untouched, body **and** frontmatter.
7. **Write ADR-0100's `## Consequences` complete at first merge.** It must
   already carry the two texts other tickets owe it: the decided D-001
   ([[FND-010]], plan handle `DSK-00-10`) and the "the proposal's three prior
   documents are not in the repository and are not an input" sentence
   ([[FND-013]], plan handle `DSK-00-13`). A published ADR body is immutable
   (`docs/adr/README.md:12-14`), so a later edit needs a whole new ADR.
   **Default taken here, to be confirmed or overridden at execution:**
   coordinate both texts into this PR, with [[FND-010]] and [[FND-013]] verifying
   rather than re-authoring afterwards. Record the choice and who agreed it. If
   [[FND-010]]'s upstream-freeze agreement is not settled enough to write, say so
   here and accept the superseding-ADR cost knowingly rather than silently.
8. **Content per ADR**, from the § 3 table: ADR-0101 states the
   local-execution/cloud-authority split and adopts the six-question test as the
   repository's placement rule (relates ADR-0002); ADR-0103 states that
   workstations never connect to the database and that the gateway is
   `Pegasus.Web` evolved in place under L-01 (relates ADR-0002, ADR-0015);
   ADR-0104 states online-required with a bounded local cache and no
   replication; ADR-0105 states the two-layer enforcement (App Installer
   `UpdateBlocksActivation` plus the gateway minimum-client-version gate that
   fails closed), the D-002 self-managed certificate and the D-003 UNC feed, and
   relates ADR-0007 (gateway release route unchanged); ADR-0110 states skill
   pinning by revision, the vendored tree and the invocation/review protocol, and
   relates `skills-lock.json`.
9. **State explicitly in ADR-0101 and ADR-0103 that ADR-0014 is not superseded** —
   Test/UAT stays local under L-02 and no Azure dev/test/staging is created.
   ADR-0014 is 28 lines and remains `status: accepted`.
10. **Index rows.** One row per ADR in `docs/adr/README.md`'s accepted table
    (heading `:16`, header `:18-19`), in ID order, **three cells**:
    `[0100](0100-native-winui3-desktop-client.md) | <title> | <frd or —>`. Follow
    the file, not `AGENTS.md:115` — which this ticket is about to correct.
11. **Run the gates**, the same two the CI `documentation` job runs at
    `.github/workflows/ci.yml:84,87`:
    ```
    pwsh ./scripts/Test-DocumentationLinks.ps1
    pwsh ./scripts/Test-TestMarkdownPlacement.ps1
    ```
    Both exit 0. Confirm every frontmatter block parses — no tabs, no smart
    quotes.
12. **Link and clear, in that order.** `link_doc` from this ticket to each new
    path so the `governing-doc` gate is satisfied by a real document, then clear
    `docs_todo` **only** on conversion tickets whose governing ADR is one of
    these six, and only after the link exists. `docs_todo: true` is what
    currently satisfies `leave-backlog` for every `feature` ticket; clearing it
    without a real link removes a satisfied gate.
13. **Open the PR against `dev`**, request the independent review from
    `pegasus-desktop-reviewer`, and record `n/a — docs-only` under a dated
    `## Simplification pass` heading below.

**Two governance corrections this ticket owns** (they are steps in their own
right, not asides):

14. `docs/desktop/00-governance-and-workflow/README.md:422` — the
    `docs/adr/0100…0110-*.md` row of the § 8 table (heading at `:418`) still
    reads "ADR-0009 `superseded_by` note limited to its deferral clause (body
    immutable — record in ADR-0100)". Correct it to say the note **is** the
    `## Context` sentence in ADR-0100 and that ADR-0009 is untouched in body and
    frontmatter. One line.
15. `AGENTS.md:114-117` — the index-shape bullet naming
    `ID | Title | Status | Superseded-by | Owner capability`. Correct it to the
    real shape, `ADR | Title | Related FRD` (`docs/adr/README.md:18-19`). One
    line. Verify with `grep -n 'Owner capability' AGENTS.md` — expected: no match
    afterwards. [[FND-007]], [[FND-026]] and [[FND-042]] carry the same warning
    and cite this ticket instead of editing.

## Verification

Evidence tier 1 — Static/build/architecture (`docs/engineering.md:72-90`), as
the body states: documentation consistency and link integrity are the whole of
the claim; no runtime behaviour is proved.

`proof` is produced from these commands, run on merged `main` after the merge:

| Command | Expected |
| --- | --- |
| `ls docs/adr/010*` | exactly one file per ADR number — no duplicate for 0100, 0104, 0105 or 0110 |
| `grep -l '^id: ADR-01' docs/adr/*.md` | the six new files, each with `status:` and `date:` present |
| `grep -c '^\| \[01' docs/adr/README.md` and `grep -n '0110' docs/adr/README.md` | exactly one row for each of 0100, 0101, 0103, 0104, 0105 and 0110, and no duplicate 0110 row. **Do not read this as "six *new*" rows** — [[TOOL-008]] may already have added the 0110 row and [[FND-006]] its four; verify one row per ADR ID, never a total |
| `grep -n 'Owner capability' AGENTS.md` | no match |
| `grep -n 'supersedes' docs/adr/0100-*.md docs/adr/0009-*.md` | `supersedes: []` in both |
| `git diff --stat -- docs/adr/0009-adopt-pegasus-monorepo-workspaces.md` | empty |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0, no broken relative link |
| `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` | exits 0 |

Also record the step 1 `ls docs/adr/010*` result *from before* the write — the
body makes that an acceptance criterion, and it is the executable form of "we
checked for a collision".

## Risks / open questions

- **ADR-0105 authorship — an operator question, tracked as a blocking box on
  [[REL-001]]'s `open-questions` document.** Not opened as a blocking item on
  this ticket, because this body directs the record to the plan (here) and gives
  an executable tie-break that makes the ticket workable meanwhile. Record the
  operator's answer here, verbatim and dated, when it arrives. If it names a
  different author, this ticket verifies and extends ADR-0105 in place instead of
  writing it.
- **ADR-0100's immutability is the one irreversible risk in this ticket.** A
  sentence left out costs a whole new ADR. Mitigation: step 7's default
  (coordinate [[FND-010]] and [[FND-013]] into this PR) plus a pre-merge read of
  both tickets' bodies against ADR-0100's `## Consequences`.
- **Writing a five-cell index row.** `AGENTS.md:115` actively invites it.
  Mitigation: step 10 says follow the file; step 15 removes the invitation.
- **Writing `[FRD-11]` instead of `[frd-11]`.** Silent house-style break.
  Mitigation: named in step 4 and in the `files` document's Context table.
- **Copying ADR-0015's heading set** (no `## Status`). Mitigation: step 1 and
  step 5 both name ADR-0028/ADR-0029 as the model.
- **An upstream sync issuing a number below 0100 that collides.** Not possible
  for these six, but re-check `docs/adr/README.md` after every sync
  ([[FND-002]], plan handle `DSK-00-02`) — that is what the reserved block
  exists for.
- **Scope boundaries owned by named tickets, not questions:** ADR-0102/0106/0107/0109
  are [[FND-006]]'s; ADR-0108 is [[FND-007]]'s; FRD-13, the PRD scope and the
  `DSK` capability family are [[FND-008]]'s.
- **Not open, and not to be reopened:** the reserved block itself (operator,
  2026-08-23); D-002 and D-003; whether ADR-0014 is superseded (it is not).

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading.
Expected outcome: `n/a — docs-only`._
