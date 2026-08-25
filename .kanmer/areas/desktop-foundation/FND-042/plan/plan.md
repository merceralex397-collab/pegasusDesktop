# Plan — FND-042: Author ADR-0102 (credentials and token session) and ADR-0105 (MSIX and minimum-version gate)

**Diff estimate: ~3 files, ~190 lines.**

## Measured file-and-line inventory

Profile `chore` owes no `research` and no `files` document, so this plan carries the
surface-area burden alone (`.grok/skills/kanmer-plan/assets/plan-template.md`'s
"written FROM research and files" precondition does not apply). Measured against the fork
working tree on 2026-08-24 with `ls`, `wc -l` and `cat -n`.

| Path | Measured now | Change | Lines |
| --- | --- | --- | --- |
| `docs/adr/0102-existing-pegasus-credentials-token-session.md` | **Does not exist.** `ls docs/adr/` returns 29 ADR bodies numbered 0001–0029 (0017 never issued) plus `README.md`; `ls docs/adr/010*` returns nothing | **New.** Appendix A's nine headings; sized against the two most recent ADRs — `0027-…` is 61 lines and `0026-…` is 70 — plus this programme's extra `## Current evidence`, `## Options`, `## Cloud-justification test` (6 rows) and `## Reversal/deprovision condition` sections | ~90 |
| `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md` | **Does not exist** (same check) | **New** — *or* an in-place extension of ~25 lines if another claimant authored it first (see the open question) | ~95 |
| `docs/adr/README.md` | 59 lines. The accepted table is `:18-41` with the header `\| ADR \| Title \| Related FRD \|` at `:18` and the separator at `:19` — **three cells, no status column**. Rows run in numeric order and end at `0029` on `:41`. `## Superseded and relocated` follows at `:43` | Two rows appended after `:41`, in numeric order, in the three-cell shape | +2 |
| `docs/index.md` | 59 lines. `grep -n 'adr' docs/index.md` returns three hits: `:21` links the decision **index**, `:46` links the `docs/adr/` folder, `:56` cites ADR-0029 as a boundary. **It does not enumerate ADRs individually** | **No change.** The body's step 10 says "if that file lists ADRs individually (check first)" — it does not, so adding rows would create the duplicate the step warns against | 0 |

Not touched: `src/`, `tests/`, `scripts/`, `.github/`, `AGENTS.md`, and every
`docs/desktop/` plan file. The `AGENTS.md` § ADR conventions index-shape correction is
[[FND-005]]'s (plan handle `DSK-00-05`), not this ticket's.

## Approach

**Write both ADRs to Appendix A's nine headings verbatim, fill both cloud-justification
tables from the area 00 § 3 table with six real yes/no answers, and check for an existing
file before writing a single byte.** The alternative rejected is **the shorter `AGENTS.md`
§ ADR conventions template** (`Status · Context · Decision · Consequences · Options
considered · Links`, `AGENTS.md:108-110`): it is the repository's general shape, but the
conversion programme requires the `## Current evidence`, `## Cloud-justification test` and
`## Reversal/deprovision condition` sections that only Appendix A carries, and the
cloud-justification table is the artefact
`docs/desktop/00-governance-and-workflow/README.md:169-176` makes mandatory "verbatim in each
ADR". The second alternative rejected is **taking the next free ADR number** — 0030 — which
is what `AGENTS.md:81-83` normally requires: the operator-confirmed exception at
`AGENTS.md:84-89` reserves ADR-0100–ADR-0110 for this conversion precisely so an upstream
sync cannot collide, and upstream keeps issuing below 0100.

The one real trap is an **index shape that two documents disagree about**. `AGENTS.md:114-117`
describes a five-column index (`ID | Title | Status | Superseded-by | Owner capability`);
the real `docs/adr/README.md:18-19` is three cells with no status column. **The file wins** —
the body says so, and writing five-cell rows into a three-column table is the specific
failure this ticket must avoid.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(confirmed by `get_doc_gates FND-042`). This ticket does not *meet* a governing document — it
**authors two of them**, which is why `docs_todo` is the right state and why nothing existing
is claimed.

> **New ADR** — ADR-0102 (existing Pegasus credentials with a token session), authored by
> **this ticket**; [[FND-006]] (plan handle `DSK-00-06`) also claims ADR-0102 — see
> [[FND-006]]'s plan for the ownership reconciliation. The rule both tickets state
> identically: one filename,
> `docs/adr/0102-existing-pegasus-credentials-token-session.md`; whichever ticket is worked
> first authors it, and the other verifies its coverage and extends it in place — never a
> second file for the same number.
> **New ADR** — ADR-0105 (MSIX/App Installer distribution and the minimum-version gate),
> **three claimants**: this ticket, [[FND-005]] (plan handle `DSK-00-05`) and [[REL-001]]
> (plan handle `DSK-09-01`) — see [[REL-001]]'s plan for the ownership reconciliation. Same
> two reconciled points: one filename,
> `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md` (the only ADR-0105 path the
> plan set itself names, at `docs/desktop/04-auth-session-update-and-startup/README.md:297`),
> and the first-worked-authors rule. **Which of the three authors it is an open ownership
> question for the operator**, recorded as an unticked box in this ticket's `open-questions`
> document — see *Risks / open questions* below.
> This plan is written to the decisions as recorded in
> `docs/desktop/04-auth-session-update-and-startup/README.md` § 3 (numbered decisions 1–8 and
> the session failure matrix) and `docs/desktop/README.md` § Locked decisions; if the
> decisions land differently this plan is revised before implementation.

Because `refs` is empty, the programme-level authorities that bind today, each with the step
that satisfies it:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 8 Authentication and authorization | The token flow decision the whole phase rests on | Steps 4–6 (ADR-0102's Decision) |
| Proposal § 9.1 Forced updates and compatibility | **Two-layer** enforcement: package layer plus gateway gate | Step 7 (ADR-0105's Decision) |
| Proposal Appendix A | The nine-heading architecture decision template, including `## Cloud-justification test` and `## Reversal/deprovision condition` | Steps 4 and 7 |
| Plan 00 § 3 (`README.md:169-176`) | The six-question cloud-justification table, used **verbatim** in each ADR, with a yes/no and evidence per row; "It is already in Azure" is not an answer | Step 6, and the same table in ADR-0105 |
| Plan 04 § 3 decisions 1–4 | Password + refresh grants for public client `pegasus-desktop`; access token 10 min; rolling refresh with 2 h idle and an 8 h absolute cap via `original-issued-at`; `UseDataProtection()` | Step 4 |
| Plan 04 § 3 decision 3 and the failure matrix | Revocation on disable, password change and logout; `password-change-required` problem type | Step 4 |
| Plan 04 § 3 decisions 5–6 | DB-backed Administrator minimum version with audit; configuration only for bootstrap; 24-hour fail-closed cache that must not be extended | Step 7 |
| Plan 04 § 7 traps | Ephemeral OpenIddict keys; the server-wide sliding switch must not be flipped | Step 5's two deviation notes |
| **L-01** | The gateway is `Pegasus.Web` evolved in place; no new deployment unit | ADR-0102's Context |
| **D-002** | Self-managed certificate, `LocalMachine\TrustedPeople`, subject equals the manifest `Publisher`, fixed before the first package | Step 8 |
| **D-003** | UNC feed over SMB, so `Uri` values read `\\<host>\<share>\<channel>\Pegasus.appinstaller` | Step 8 |
| **C-01** | Private repositories rule out GitHub Releases and Pages permanently | Step 8 |
| `AGENTS.md:84-89` § ADR conventions | The conversion uses the reserved block ADR-0100–ADR-0110, never the next free number | Step 3 |
| `AGENTS.md:92-106` § ADR conventions | YAML frontmatter with `id`, `status`, `date`, `supersedes`, `superseded_by`, `related_capabilities`, `related_frd`, `tags` | Step 2 |
| `AGENTS.md:90-91` § ADR conventions | One decision per ADR | Two files, not one |
| `AGENTS.md` § New Markdown placement | A new `.md` outside `docs/(prd\|frd\|adr\|design\|desktop)` fails the CI `documentation` job | Step 11 |
| `docs/engineering.md:201` § Plan sizing | A plan states its diff estimate first, from a real number | The first line and the inventory above |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing, reviewer `pegasus-desktop-reviewer` |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in the
plan document.

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-docs`
  (`.grok/skills/kanmer-docs/SKILL.md`).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `link_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
  **only** to confirm the App Installer schema claims quoted in ADR-0105.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates FND-042` before
  every move; a move crosses at most one gated boundary. `chore` owes `plan` at
  `leave-preparing` and `proof` at `enter-done`, and no `research`, `files` or `checklist` —
  **plus `questions-resolved`, which this ticket's open question currently holds shut.**
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's twelve implementation steps in the same order, with the same
ownership and the same file paths.

1. **Orient.** Read `docs/desktop/04-auth-session-update-and-startup/README.md` §§ 2–4 in
   full (308 lines; §§ 2–4 are the evidence base, the eight numbered decisions, the session
   failure matrix and the exit gate — all of which become ADR text). Then read
   `docs/desktop/00-governance-and-workflow/README.md` § 3, whose cloud-justification table is
   at `:169-176`. Call `get_doc_gates FND-042`, then `take_ticket FND-042`.
2. **Load the skills and copy the real frontmatter shape.** `pegasus-desktop` first, then
   `kanmer-docs`. Read `AGENTS.md:77-117` § ADR conventions and open
   `docs/adr/0027-authorization-code-for-external-mcp-connectors.md:1-10`, whose frontmatter
   is the eight-key block to copy verbatim (`id`, `status`, `date`, `supersedes`,
   `superseded_by`, `related_capabilities`, `related_frd`, `tags`). Note its heading order:
   `# ADR-NNNN: <title>` then `## Status` with a prose paragraph, not a bare word.
3. **Check for collisions before writing anything.**
   `grep -n '0102\|0105' docs/adr/README.md` and `ls docs/adr/010*`. Measured 2026-08-24:
   the index runs `0001`–`0029` and `ls docs/adr/010*` returns **nothing**, so as of this
   plan neither file exists. **Re-run both at implementation time** — [[FND-005]] and
   [[REL-001]] may have landed ADR-0105 first, and [[FND-006]] may have landed ADR-0102.
   If either file exists, do **not** author a second copy: extend the existing ADR's Context
   and Consequences with the area-04 material and record the resolution here under a dated
   note. **Before writing ADR-0105 at all, confirm the operator has answered the authorship
   question in this ticket's `open-questions` document** — the Guardrails forbid deciding it
   by starting first.
4. **Create `docs/adr/0102-existing-pegasus-credentials-token-session.md`** with Appendix A's
   nine headings verbatim: `## Status`, `## Context`, `## Current evidence`, `## Options`,
   `## Cloud-justification test`, `## Decision`, `## Consequences`, `## Verification`,
   `## Reversal/deprovision condition`. The Decision states, from plan 04 § 3 decisions 1–3:
   OpenIddict password + refresh-token grants for a first-party **public** client
   `pegasus-desktop` (no secret; scopes `pegasus.desktop` and `offline_access`); access token
   10 minutes; rolling refresh token with a 2-hour idle lifetime
   (`StaffSessionPolicy.IdleLifetime`) and an absolute 8-hour cap carried in an
   `original-issued-at` claim; `UseDataProtection()` for token protection; revocation on
   disable, password change and logout. `## Current evidence` cites the real seams:
   `src/Pegasus.Web/Mcp/AutomationMcpExtensions.cs:33-60` (the existing OpenIddict
   composition this must not disturb), `src/Pegasus.Core/Actors/StaffSessionPolicy.cs:9-13`
   (2 h idle, 8 h absolute, 10 attempts/client/min, 100 global),
   `src/Pegasus.Web/Program.cs:353` (`SecurityStampValidatorOptions.ValidationInterval =
   TimeSpan.Zero`) and `src/Pegasus.Web/Program.cs:954` (`/diagnostics/version`, the only
   version surface today).
5. **Record ADR-0102's two deviation notes verbatim in `## Consequences`.** (a) The
   Automation client's 14-day refresh lifetime and the server-wide
   `DisableSlidingRefreshTokenExpiration()` are **not** reused for staff — the idle/absolute
   pair is implemented in the token handler, so MCP connectors governed by ADR-0027 keep
   their fortnightly cap. (b) The current **ephemeral** OpenIddict keys are replaced by Data
   Protection, so a Container App restart does not invalidate staff sessions. Both are plan
   04 § 3 decisions 2 and 4 and plan 04 § 7's first two traps; an ADR that omits them leaves
   the next agent free to flip the global switch.
6. **Fill ADR-0102's cloud-justification table** with all six questions from
   `docs/desktop/00-governance-and-workflow/README.md:171-176`, each with a yes/no **and**
   an evidence cell — no blanks. Shared authority = **yes** (one account store, one identity
   per operator); central enforcement = **yes** (revocation, roles and audit must hold
   independently of any client). Answer the remaining four honestly from the same evidence
   rather than steering toward a tidy result: a "yes" names *where* the responsibility lands,
   and for this decision it lands on the existing `Pegasus.Web` gateway under L-01 — not on
   any new Azure resource.
7. **Create `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md`** — the single
   agreed path — with the same nine headings. The Decision states proposal § 9.1's two
   layers: **(a)** a signed MSIX delivered by an `.appinstaller` on the **2021** schema with
   `OnLaunch HoursBetweenUpdateChecks="0" ShowPrompt="true" UpdateBlocksActivation="true"`;
   **(b)** a gateway minimum-version gate whose minimum is a **database-backed Administrator
   setting with audit** (the ADR-0018/ADR-0024 settings pattern), with
   `Desktop:MinimumClientVersion` used **only** for bootstrap. Record explicitly that App
   Installer fails **open** when the feed is unreachable, that the gateway gate is the
   fail-closed layer with a 24-hour cache, and that the cache **must not be extended** "for
   convenience" (plan 04 § 3 decision 6 and § 7).
8. **Record D-002, D-003 and C-01 in ADR-0105's Context and Consequences.** D-002: a
   self-managed certificate trusted per workstation in `LocalMachine\TrustedPeople`, its
   subject equal to the manifest `Publisher` exactly and fixed before the first package.
   D-003: a UNC feed over SMB, so `Uri` values read
   `\\<host>\<share>\<channel>\Pegasus.appinstaller` and update checks need the office
   network or VPN. C-01: private repositories rule out GitHub Releases and Pages
   **permanently**. Cite `docs/desktop/09-release-update-and-distribution/appinstaller-template.md`
   for the template rather than restating it — `AGENTS.md:111-113` keeps operational detail
   out of an ADR.
9. **Confirm the two Microsoft claims before accepting ADR-0105**, with
   `microsoft_docs_search`: that `ShowPrompt` and `UpdateBlocksActivation` require the
   `http://schemas.microsoft.com/appx/appinstaller/2021` namespace, and that
   `ms-appinstaller:` has been disabled by default since December 2023. Cite the Learn URLs
   **with the fetch date**. Plan 09 § 2 records both from a 2026-08-23 fetch; an ADR that
   quotes a schema requirement without a dated source is the kind of claim that quietly goes
   stale.
10. **Add one index row per ADR to `docs/adr/README.md`.** The accepted table is `:18-41`;
    its header at `:18` is `| ADR | Title | Related FRD |` — **three cells**. Append the two
    rows after `0029` at `:41`, in numeric order, matching the existing row shape
    `| [0102](0102-….md) | <Title> | FRD-13 |` (or `—` if FRD-13 does not exist yet — check
    `ls docs/frd/` first; [[FND-008]], plan handle `DSK-00-08`, authors it). **Ignore
    `AGENTS.md:114-117`**, which describes a five-column index the real file contradicts: the
    file wins, and correcting that sentence is [[FND-005]]'s work. Then check `docs/index.md`
    — measured 2026-08-24 it links the decision **index** at `:21` and the folder at `:46`
    and does **not** enumerate ADRs individually, so **add nothing there**; record that
    finding rather than leaving it implicit.
11. **Run both documentation gates from the repository root.**
    `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-MarkdownPlacement.ps1`.
    Both must exit `0`; a broken relative link or a `.md` outside
    `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job
    (`.github/workflows/ci.yml:71`).
12. **Link, prove, close.** Link both ADRs to this ticket with Kanmer `link_doc`, write the
    proof with the two script exit codes and the `ls docs/adr/0105*` result, and record
    `## Simplification pass` below as `n/a — docs-only` with the date. Confirm the
    `open-questions` box is ticked — with the operator's answer written beside it — before
    moving the ticket.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture**
(`docs/engineering.md:76`). This ticket proves consistency only: the documents exist, their
links resolve, and the placement gate passes. **No runtime behaviour is claimed** — the token
flow ADR-0102 records is implemented and tested by `GWY` tickets in area 04, and the proof
must not read as if the flow works. Proof type: `command-log`.

| Command / observation | Expected evidence |
| --- | --- |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exit `0`, no broken-link lines printed |
| `pwsh ./scripts/Test-MarkdownPlacement.ps1` | exit `0` |
| `grep -c '^| Question' docs/adr/0102-….md docs/adr/0105-….md` | `1` for each file — the cloud-justification table header is present exactly once |
| `grep -c '^| ' docs/adr/0102-….md` restricted to the cloud table | six question rows, none with an empty answer or evidence cell |
| `ls docs/adr/0105*` | exactly one file, `0105-msix-app-installer-and-minimum-version-gate.md` |
| `head -10 docs/adr/0102-….md` | the eight frontmatter keys in the `ADR-0027` order, `status: accepted` |
| `sed -n '18,44p' docs/adr/README.md` | two new rows, three cells each, in numeric order after `0029` |
| `git diff --name-only` at PR time | exactly the two new ADRs and `docs/adr/README.md`; **no** `docs/index.md`, no `AGENTS.md`, no `src/**` |
| `get_doc_gates FND-042` after the proof is written | no unmet requirement for `enter-done` — which includes `questions-resolved` |
| Observations stated rather than inferred | whether either ADR already existed at step 3; the operator's answer to the authorship question; whether FRD-13 existed for the index row |

## Risks / open questions

- **Open question — who authors ADR-0105.** Recorded as an unticked box in this ticket's
  `open-questions` document, because the ticket body calls it "an open ownership question for
  the operator to settle before Phase 2" and its Guardrails forbid resolving it by starting
  first. Three tickets claim ADR-0105: this one, [[FND-005]] (plan handle `DSK-00-05`) and
  [[REL-001]] (plan handle `DSK-09-01`). The **filename** and the **first-worked-authors,
  others-extend-in-place** rule are already agreed and stated identically in all three; only
  the assignment is open. It correctly blocks `leave-preparing`, `enter-review` and
  `enter-done` — and nothing else — until the operator answers.
- **Scope boundary, not an open question — who authors ADR-0102.** [[FND-006]] (plan handle
  `DSK-00-06`) is the other claimant, and the same one-filename / extend-in-place rule
  applies. Step 3's `ls docs/adr/010*` settles it at implementation time; a named sibling
  ticket owning a decision belongs here, not in `open-questions`.
- **Risk — five-cell rows written into a three-column table.** `AGENTS.md:114-117` and
  `docs/adr/README.md:18-19` disagree about the index shape. Mitigation: step 10 states that
  the file wins and gives the measured header; the verification reads back `:18-44`.
  Correcting `AGENTS.md` is [[FND-005]]'s ticket and this one must not touch it.
- **Risk — an ADR number outside the reserved block.** `AGENTS.md:81-83` normally requires the
  next free number, which is 0030. Mitigation: the operator-confirmed exception at
  `AGENTS.md:84-89` is cited in both ADRs' Context, and step 3 re-checks the index after any
  upstream sync (plan 00 § 7).
- **Risk — a cloud-justification table with blanks or an evasive answer.** Plan 00 § 3 says
  "It is already in Azure", "the web app does it" and "it may scale later" are not answers.
  Mitigation: step 6 requires six filled rows, and the verification counts them.
- **Risk — a stale Microsoft claim.** The 2021-schema requirement and the `ms-appinstaller:`
  default are both time-sensitive. Mitigation: step 9 re-confirms both with
  `microsoft_docs_search` and cites the URLs with the fetch date.
- **Risk — ADR-0105 duplicated under a second filename.** Mitigation: one agreed path, named
  identically by all three claimants and verified by `ls docs/adr/0105*` returning exactly one
  file.
- **Assumption carried, not settled here** — plan 04 § 2 assumptions A1 (OpenIddict 7.6 still
  exposes `AllowPasswordFlow()` and per-principal lifetime overrides) and A2
  (`UseDataProtection()` does not disturb the Automation MCP client). ADR-0102 **records the
  decision**, not the proof; both are verified by the `GWY` implementation tickets, and
  ADR-0102's `## Verification` section says which tests will do it rather than claiming they
  have.

## Simplification pass

_`n/a — docs-only`. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR; this branch adds two Markdown files and two index rows and
touches no code. Record the dated heading with this value rather than omitting the section._

## Implementation result — 2026-08-25

The canonical ADR-0105 from [[FND-005]] already exists on `origin/dev`; this ticket extended that one file in place rather than creating a duplicate. Added ADR-0102 at the agreed path and its three-cell index row. ADR-0102 uses the nine required headings, six answered cloud-justification rows, and records the existing Identity/OpenIddict evidence, public-client/password+refresh decision, staff-versus-Automation boundary, Data Protection requirement, and reversal conditions. ADR-0105 now records the 2026-08-25 Microsoft Learn verification of the 2021 schema and the `ms-appinstaller:` default, plus the App Installer fail-open/gateway fail-closed split. The first local PowerShell count probe had a parser error; the corrected scoped probe passed.

### Simplification pass — 2026-08-25

n/a — docs-only. The diff adds one ADR, one index row, and a focused in-place clarification to ADR-0105. No code, runtime, Azure, source, or unrelated cleanup was added.

### Independent review follow-up — 2026-08-25
Aquinas returned FAIL on the first review for missing ADR-0105 Appendix A headings and settled distribution facts/citation, plus the missing `password-change-required` routing in ADR-0102. These were documentation-only corrections. Commit `7b68a637` adds the required sections, exact Publisher/UNC/VPN and database-backed minimum-setting evidence, direct Microsoft Learn citation, and the password-change problem/routing decision. Re-review is required before the ticket crosses the next boundary.
