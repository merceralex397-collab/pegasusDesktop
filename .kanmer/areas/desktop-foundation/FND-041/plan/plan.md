# Plan — FND-041: Phase 1 exit review on a clean Windows 11 machine

**Diff estimate: ~2 files, ~6 lines.** The repository diff is deliberately almost nothing:
this ticket changes **no source file**, and its substantive output is the ticket `proof`
document plus one follow-up Kanmer ticket per failed gate row. That is why the body records
the simplification pass as `n/a — docs-only`.

## Measured file-and-line inventory

Profile `chore` owes no `research` and no `files` document, so this plan carries the
surface-area burden alone (`.grok/skills/kanmer-plan/assets/plan-template.md`'s
"written FROM research and files" precondition does not apply). Measured against the fork
working tree on 2026-08-24 with `grep -n` and `cat -n`.

| Path | Measured now | Change | Lines |
| --- | --- | --- | --- |
| `docs/desktop/README.md` § Status | `:138` heading; the table is `:140-142` and holds exactly **one** row: `\| 00–12 \| Drafted 2026-08-23 \| Awaiting first ticket creation on the fork's Kanmer board (see 00) \|` | Split out an area-02 row reading "Phase 1 gate passed" with the date — **only** when every gate row passes | ~+2 |
| `docs/current-architecture.md` § Architecture invariants | `:69-91`. The section explicitly *reports* how the system is wired to `AGENTS.md` § Product invariants and "does not restate or compete with that owner" (`:77-79`); two engineering conventions not yet in AGENTS.md are carried at `:84-90` | **Only where provably wrong.** Confirm the desktop-client and Contracts rows the earlier tickets added match what was actually installed; if they do not, that is a finding, and the correction is at most a few lines | +0 to 4 |
| *(no repository file)* | — | The ticket `proof` document: one row per gate row — gate, evidence artefact, pass/fail, owning ticket for any failure | — |
| *(no repository file)* | — | One new Kanmer ticket in `desktop-foundation` per failure, naming the gate row it blocks | — |

**Nothing under `src/`, `tests/`, `scripts/` or `.github/` is touched.** Fixing a failure is
the owning ticket's work; this ticket records it.

## Approach

**Run the plan § 4 exit-gate table as a checklist of seven rows, each producing a named
artefact, and treat "no artefact" as a fail rather than as a judgement call.** The
alternative rejected is **a narrative review** — an agent reading the diffs of fifteen
tickets and writing an opinion. It is cheaper and it is what the gate exists to prevent:
proposal § 24 and plan 02 § 4 both name *evidence* per row, and `docs/engineering.md:76`
tiers 7 and 11 both require observation on a real machine rather than inference from source.
The second alternative rejected is **letting the implementing agent self-review**:
`docs/desktop/00-governance-and-workflow/README.md` § 3 and `AGENTS.md` § Repository task
workflow step 5 both require an agent that did not implement, and a self-review is not
evidence — step 1 makes that a stated, checkable fact rather than an assumption.

Two rows carry a trap that changes how they are run. **"Architecture boundaries enforced"**
is not satisfied by a green test suite: a fact that has never been shown red might read
nothing at all, so step 5 plants a real violation and demands the red run — the same double
proof [[FND-037]] (plan handle `DSK-02-12`) builds into its own verification.
**"No WebView/web dependency"** is absolute today: `ls docs/adr/010*` returns nothing, so
ADR-0108 does not exist and there is no exemption to grant.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(confirmed by `get_doc_gates FND-041`). No existing PRD, FRD or ADR is claimed to be met.

> **New ADR** — ADR-0100 (native WinUI 3 desktop client inside this fork, which authorises
> the projects whose Phase 1 state this review gates), authored by [[FND-026]] (plan handle
> `DSK-02-01`); [[FND-005]] (plan handle `DSK-00-05`) also claims ADR-0100 — see
> [[FND-026]]'s plan for the ownership reconciliation.
> **ADR-0108** (isolated non-UI WebView2 HTML→PDF rendering) is named here for the opposite
> reason: it is authored by [[FEAT-038]] (plan handle `DSK-07-12`), with [[FND-007]] (plan
> handle `DSK-00-07`) as the other claimant — see [[FEAT-038]]'s plan for the ownership
> reconciliation — and **it does not exist yet** (`ls docs/adr/010*` returns nothing,
> 2026-08-24), which is precisely why step 6 grants no WebView exemption.
> This plan is written to the decisions as recorded in
> `docs/desktop/02-architecture-and-foundation/README.md` § 4 and `docs/desktop/README.md`
> § Locked decisions (L-02, L-03, D-002); if any lands differently this plan is revised
> before the review is run.

Because `refs` is empty, the programme-level authorities that bind today, each with the step
that satisfies it:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 24 Phase 1 exit gate | Clean Windows 11 machine launches the shell; no WebView/web dependency; foundation tests pass; install/uninstall works | Steps 4, 6, 7, 8, 11 |
| Proposal § 27 acceptance criterion 1 | Operators use a native Windows desktop application | Step 8 |
| Proposal § 27 acceptance criterion 2 | No primary workflow embeds or depends on the web application | Step 6 |
| Proposal § 27 acceptance criterion 13 | Diagnostics are exportable | Step 10 |
| Proposal § 29 item 8 | The foundation spike is reviewed before converting further | The whole ticket; it is the gate for area 04 and the first slice in area 05 |
| Plan 02 § 4 exit-gate table (seven rows) | Each row has its **named** evidence | Steps 4–12, one row at a time |
| Plan 02 § 4 target-state project table | The solution shape after Phase 1 | Step 3 |
| Plan 00 § 3 | Review by an agent that did not implement; the conversion reviewer is `pegasus-desktop-reviewer` | Step 1, stated in the proof |
| **L-02** (`docs/desktop/README.md`) | The only stack is the local Test/UAT one; no Azure test environment | Guardrails — asking for an Azure test resource is out of bounds |
| **L-03** | WebView2 only through the isolated non-UI path ADR-0108 authorises | Step 6 — no ADR, therefore no exemption |
| **D-002** | Development trust is Trusted Root on test machines; production trust is `LocalMachine\TrustedPeople` | Step 7 records which store was used |
| `docs/engineering.md:76` § Required evidence tiers, tier 7 | Keyboard, focus, semantic labels, text-plus-colour states, high contrast — "Automated axe results do not replace manual keyboard or assistive-technology review" | Step 12 |
| `docs/engineering.md:76` § Required evidence tiers, tier 11 | Install, previous-artifact compatibility, clean removal on a real machine | Steps 7 and 11 |
| `docs/engineering.md:201` § Plan sizing | A plan states its diff estimate first, from a real number | The first line and the inventory above |
| `docs/runbook.md:19` § Supported platform | "record the platform actually exercised" | Step 7 states physical machine or VM snapshot |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing and step 1 |
| `AGENTS.md` § Safety rails | Refresh current-state documents in the same task | Step 13's `docs/desktop/README.md` § Status update |

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in the
plan document.

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
  (read-only sandbox; it must not have implemented any area 02 ticket).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-code-review`
  (`.codex/skills/winui-code-review/SKILL.md` and its `references/quality-rules.md`) →
  `winui-design` (`.codex/skills/winui-design/SKILL.md`) for the theming and layout
  checklists. All three vendored, win-dev-skills v0.5.0 `f1028dd5`, verified present
  2026-08-24.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `get_item`, `get_ticket_doc`,
  `set_ticket_doc`, `append_scratch`, `move_item`, `take_ticket`); Microsoft Learn
  (`microsoft_docs_search`) **only** to check a claimed API behaviour.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates FND-041` before
  every move; a move crosses at most one gated boundary. `chore` owes `plan` at
  `leave-preparing` and `proof` at `enter-done`, and no `research`, `files` or `checklist`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement. **If the same
  agent implemented an area 02 ticket, a different agent must run this review.**

## Steps

These refine the body's thirteen implementation steps in the same order, with the same
ownership and the same file paths.

1. **Orient, and prove the reviewer is eligible.** Read
   `docs/desktop/02-architecture-and-foundation/README.md` § 4 in full — the seven-row
   exit-gate table and the target-state project table are the whole specification for this
   ticket. Then `get_doc_gates FND-041` and `take_ticket FND-041`. **State in this plan which
   area 02 tickets the reviewing agent did not implement**, by name, not as a blanket claim;
   if it implemented any of [[FND-026]] through [[FND-040]], hand the ticket to another agent
   rather than proceeding. A self-review is not evidence.
2. **Confirm every prerequisite is Done.** `get_item` for each of [[FND-026]] (plan handle
   `DSK-02-01`), [[FND-027]] (`DSK-02-02`), [[FND-028]] (`DSK-02-03`), [[FND-029]]
   (`DSK-02-04`), [[FND-030]] (`DSK-02-05`), [[FND-031]] (`DSK-02-06`), [[FND-032]]
   (`DSK-02-07`), [[FND-033]] (`DSK-02-08`), [[FND-034]] (`DSK-02-09`), [[FND-035]]
   (`DSK-02-10`), [[FND-036]] (`DSK-02-11`), [[FND-037]] (`DSK-02-12`), [[FND-038]]
   (`DSK-02-13`), [[FND-039]] (`DSK-02-14`) and [[FND-040]] (`DSK-02-15`), and record each
   ticket's stage in a table in the proof. **A gate row whose owning ticket is unfinished is a
   fail, not a "partially met".**
3. **Verify the solution shape.** `cat Pegasus.slnx` — it holds 13 lines and seven projects
   today (four under `/src/`, three under `/tests/`), so after Phase 1 it must additionally
   list `src/Pegasus.Contracts`, `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`
   and `tests/Pegasus.Desktop.ViewModelTests`. Confirm the server entry point [[FND-028]]
   creates lists **only** the server set. Compare against the plan § 4 target-state table row
   by row and record any divergence as a finding rather than silently accepting it.
4. **Gate row "Foundation tests pass".**
   `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`
   and
   `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release`.
   Expected: both green, **zero skipped** — a skipped test is not a passing test. Capture both
   consoles as `command-log` proof.
5. **Gate row "Architecture boundaries enforced" — show it red.** In a scratch worktree (never
   the reviewed one), plant `<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />`
   in `src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj`, re-run the
   architecture tests, confirm the suite goes red **naming the package**, then revert. Capture
   both outputs. [[FND-037]] owns the facts; this step owns the proof that they fire. A fact
   that cannot be shown red is not enforcement — it may be reading a file the desktop csproj
   does not populate.
6. **Gate row "No WebView/web dependency in the package".** Run
   `grep -rn 'WebView2\|Microsoft.Web.WebView2' src/Pegasus.Desktop src/Pegasus.Desktop.Infrastructure`
   and list the assemblies inside the produced `.msix`. Expected: no WebView2 assembly and no
   `WebView2` XAML element. **Any hit is a gate failure**: `ls docs/adr/010*` returns nothing,
   so ADR-0108 does not exist and there is no exemption to grant — L-03 makes the isolated
   renderer conditional on that ADR, and it is area 07's work.
7. **Operator step — clean machine.** On a clean Windows 11 x64 machine or a fresh VM
   snapshot, with the development certificate trusted per [[FND-039]], run
   `pwsh ./tests/Pegasus.Packaging.Tests/Test-InstallUninstall.ps1 -Package <msix>`. The
   operator hands back the script log, the `Get-AppxPackage` output showing `Name`,
   `Publisher`, `PackageFamilyName` and `Version`, and confirmation that installation needed
   **no** administrator elevation. Record two things the runbook and D-002 both ask for:
   whether the machine was physical or a VM snapshot (`docs/runbook.md:19` § Supported
   platform requires the platform actually exercised), and which certificate store held the
   trust — Trusted Root for the development certificate on a test machine, which is **not**
   the production arrangement (`LocalMachine\TrustedPeople`, D-002, owned by [[REL-007]],
   plan handle `DSK-09-08`).
8. **Gate row "Clean Windows 11 machine launches the native shell".** On that machine, launch
   via `winapp run`, screenshot the shell, then navigate **every** rail route in the approved
   order — Dashboard, Inbox, Upload, Queues, Cases, Operations, Administration
   (`docs/desktop/06-ui-design/keyboard-and-accessibility.md:22`) — screenshotting each.
   Confirm the environment badge is visible on a non-production channel and the status bar
   shows connection state ([[FND-033]], plan handle `DSK-02-08`).
9. **Gate row "Single instance".** Launch the installed application a second time in the same
   user session. Expected: exactly **one** window and **one** process, with the activation
   log recording the redirect ([[FND-035]], plan handle `DSK-02-10`). Capture the screenshot
   and the log excerpt — the log line is what distinguishes a redirect from a second launch
   that merely failed.
10. **Gate row "Diagnostics bundle exports".** Use the Settings route's Export diagnostics
    command — AutomationId `Settings.ExportDiagnostics`
    (`docs/desktop/06-ui-design/screen-specs.md:124`) — extract the zip, and check its
    manifest against the schema [[FND-036]] (plan handle `DSK-02-11`) documents. Then run
    `Select-String -Path <extracted>\* -Pattern 'Bearer '` and expect **no matches**. Capture
    the manifest and the scan result. A bundle carrying a bearer or refresh token is a
    security finding, not a cosmetic one.
11. **Gate row "Install/uninstall leaves only intended user settings".** After uninstall,
    confirm `%LOCALAPPDATA%\Packages\<PackageFamilyName>` is gone and no DPAPI
    credential-store file remains. Capture the directory listing — an empty `Test-Path`
    result with no listing beside it proves less than it appears to.
12. **Manual keyboard, theme and accessibility pass — by hand.** Tab through the shell and
    confirm focus order and visible focus; exercise the rail access keys `Alt+D/I/U/Q/C/O/A`
    (`keyboard-and-accessibility.md:22`), `Ctrl+K` (which focuses the Cases search box —
    `screen-specs.md:78-79` records the deviation that there is no separate grouped search
    screen) and `F5`; and view the shell in Light, Dark and Windows high contrast. Capture the
    three theme screenshots. `docs/engineering.md:76` tier 7 is explicit that automated axe
    results do not replace this, so an automated scan is an addition here, never a substitute.
13. **Write the verdict, file the failures, do not fix anything.** Write the proof with one
    row per gate row: gate, evidence artefact, pass/fail, and the ticket that owns any
    failure. **Every failure becomes a new Kanmer ticket in `desktop-foundation`** naming the
    gate row it blocks; do not change code in this ticket, and do not mark the gate passed
    with an open finding "to be fixed later". Only when every row passes, update
    `docs/desktop/README.md` § Status (`:138-142`, currently one row covering areas 00–12) to
    carry an area-02 row reading "Phase 1 gate passed" with the date, and correct
    `docs/current-architecture.md` (`:69-91` and the implementation-map rows) only where it is
    provably wrong against what was installed. Then `set_ticket_doc` the proof and move the
    ticket, calling `get_doc_gates FND-041` first.

## Verification

Evidence tiers from the body: **Tier 7 — Browser/accessibility** and **Tier 11 —
Migration/recovery** (`docs/engineering.md:76`). Tier 7 obliges the manual keyboard, focus,
semantic-label and high-contrast review on a real session; tier 11 obliges install,
previous-artifact compatibility and clean-removal evidence on a real machine. Neither is
satisfied by automation alone, and the proof must say so. Proof types: `visual` (shell,
per-route and three theme screenshots) and `command-log` (test consoles, install script log,
scans).

| Command / observation | Expected evidence |
| --- | --- |
| `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` | green, zero skipped |
| `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` | green, zero skipped |
| The same, with a planted forbidden `PackageReference` in a scratch worktree | red, message naming `Microsoft.EntityFrameworkCore.SqlServer`; green again after revert |
| `grep -rn 'WebView2\|Microsoft.Web.WebView2' src/Pegasus.Desktop src/Pegasus.Desktop.Infrastructure` | no matches |
| `ls docs/adr/010*` | nothing — recorded, because it is why no WebView exemption is available |
| `pwsh ./tests/Pegasus.Packaging.Tests/Test-InstallUninstall.ps1 -Package <msix>` on a clean Windows 11 machine | exit `0`, log showing install → launch → uninstall → clean state |
| Second launch of the installed package | one window, one process, a redirect line in the activation log |
| `Select-String -Path <extracted bundle>\* -Pattern 'Bearer '` | no matches |
| `Test-Path "$env:LOCALAPPDATA\Packages\<PackageFamilyName>"` plus the directory listing | `False`, with the listing attached |
| Manual pass | focus order, the seven rail access keys, `Ctrl+K`, `F5`, and Light/Dark/high-contrast screenshots |
| `git diff --name-only` at PR time | at most `docs/desktop/README.md` and `docs/current-architecture.md` — **no** `src/**`, `tests/**`, `scripts/**` or `.github/**` |
| Observations stated rather than inferred | which area 02 tickets the reviewer did not implement; each prerequisite ticket's stage; physical machine or VM snapshot; which certificate store held the trust |

## Risks / open questions

- **Risk — a self-review passes the gate.** Mitigation: step 1 names the tickets the reviewer
  did not implement, in the proof, and the routing sends the ticket to another agent if the
  claim cannot be made. `AGENTS.md` step 5 and plan 00 § 3 both require it.
- **Risk — a gate row passes on a green suite that never fires.** Mitigation: step 5 plants a
  real violation and requires the red run. This is the same double proof [[FND-037]] builds
  into its own verification, and it is the reason a captured red output is listed as evidence
  rather than a passing suite.
- **Risk — a WebView2 exemption is granted on the strength of L-03.** L-03 makes the isolated
  renderer conditional on ADR-0108, which does not exist. Mitigation: step 6 records
  `ls docs/adr/010*` returning nothing as part of the evidence, so the absence is on the page
  rather than in someone's memory.
- **Risk — the gate is marked passed with an open finding.** Mitigation: step 13 files a
  ticket per failure and forbids "to be fixed later"; the `docs/desktop/README.md` § Status
  edit happens **only** when every row passes, which makes the status line a consequence of
  the evidence rather than a claim beside it.
- **Risk — evidence is captured but not attributable.** A screenshot without the route name,
  or a `Test-Path` result without its listing, proves less than it appears to. Mitigation:
  every row of the proof names its artefact explicitly, and steps 9 and 11 call out the two
  places this happens most.
- **Operator dependency, not an open question.** Steps 7–12 need a clean Windows 11 x64
  machine or a fresh VM snapshot and a human at the keyboard; the ticket carries the
  `needs-operator` label for exactly that. This is a prerequisite the operator supplies, not
  a decision anyone still has to take.
- **Scope boundary, not an open question — fixing what the review finds.** Each failure
  belongs to the area 02 ticket that owns the gate row; this ticket files it and changes no
  code. Where a failure has no obvious owner, the new ticket names the gate row and the area,
  and board grooming ([[FND-052]]) assigns it.
- **Open questions**: none. No `open-questions` document is created.

## Simplification pass

_`n/a — docs-only`. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR; this ticket's branch changes only `docs/desktop/README.md`
§ Status and, where provably wrong, `docs/current-architecture.md`. Record the dated heading
with this value rather than omitting the section._
