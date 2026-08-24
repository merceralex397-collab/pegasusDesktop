---
id: FEAT-022
type: ticket
title: DSK-05-22 · S22 Hardening sweep
status: preparing
area: desktop-features
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:31:38.416Z'
labels:
  - desktop-conversion
  - plan-05
  - phase-8
  - tier-7
  - tier-9
  - tier-10
  - needs-operator
groups:
  - EPIC-006
  - HZN-009
links: []
refs:
  - docs/frd/frd-12-operator-experience.md
docs_todo: true
archived: false
created: '2026-08-24T08:02:19.010Z'
updated: '2026-08-24T21:31:38.416Z'
---

## What

Sweep every shipped desktop screen against the accessibility, performance and security baselines on the baseline workstation — `axe-windows` scan, the full `winapp ui` suite, keyboard-only walkthrough, Narrator smoke, 200 % scale, forced colours, a performance regression report and the security checklist — and raise or fix the findings in their owning slices.

## Why

Proposal §14.9, §15 and §17 and the Phase 8 exit gate require the full automated suite to pass, every critical accessibility issue resolved, no unresolved high-risk security item and a production-like package tested. Today only the web is covered: `tests/Pegasus.IntegrationTests/Browser/AccessibilityTests.cs` (Deque.AxeCore.Playwright) is part of a 20-fact browser lane that says nothing about the desktop. Without one sweep across all twenty-one slices, each slice's local checks would leave cross-screen regressions invisible. Siblings: every slice [[DSK-05-01]] to [[DSK-05-21]] is in scope; [[DSK-06-15]] and [[DSK-06-16]] own the accessibility automation and the ten recorded reviews; [[DSK-08-09]], [[DSK-08-11]] and [[DSK-08-15]] own the lanes this sweep executes.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-22`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S22 · Hardening sweep (DSK-05-22)`
- Screen spec: `docs/desktop/06-ui-design/keyboard-and-accessibility.md` and `docs/desktop/06-ui-design/screen-specs.md` § `AutomationId convention`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 14.9 Keyboard and accessibility, § 15 Performance design, § 17 Security and privacy, § 24 Phase 8
- Repository evidence: `tests/Pegasus.IntegrationTests/Browser/AccessibilityTests.cs` (the web-only precedent), `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` (520 lines), `docs/design/README.md` § `No explanatory copy and page economy` and the banned-words list; `src/Pegasus.Core/Actors/ActorDisplayNames.cs:12` and `src/Pegasus.Core/Identity/StaffAccountAdministration.cs:110` (`IStaffAccountQueries`) — the named sources a picker draws from
- Upstream evidence: upstream `PLAT-015` names the entry-side breaches by file — the `Engineer ID` text input at `src/Pegasus.Web/Pages/Cases/Shared/_CaseWorkflow.cshtml:268`, the typed `Report SHA-256` input at `:296`, the `Assignee ID` text inputs at `:352` and `:371`, the reply picker showing `InternetMessageIdentity`, and the raw `AggregateId` in the Automation Activity Target column at `src/Pegasus.Web/Pages/Administration/Automation/Activity.cshtml:67`
- Binding decisions: L-02 every measurement runs on the local Test/UAT workstation, never an Azure environment; L-04 routing named on the ticket; C-01 private-repository Windows runner minutes bill at 2×, so the CI cost of any lane this sweep adds is a live constraint (see [[DSK-08-19]])
- Depends on: `DSK-05-01`, `DSK-05-02`, `DSK-05-03`, `DSK-05-04`, `DSK-05-05`, `DSK-05-06`, `DSK-05-07`, `DSK-05-08`, `DSK-05-09`, `DSK-05-10`, `DSK-05-11`, `DSK-05-12`, `DSK-05-13`, `DSK-05-14`, `DSK-05-15`, `DSK-05-16`, `DSK-05-17`, `DSK-05-18`, `DSK-05-19`, `DSK-05-20`, `DSK-05-21` — every slice must be merged before the sweep is meaningful

## Routing

- **Subagent**: `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml` (scans, UI suite, performance); `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml` (independent review of findings); `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml` (suite health and gap analysis)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`) → `winui-code-review` (`.codex/skills/winui-code-review/SKILL.md`) → `analyzing-dotnet-performance` (dotnet/skills `98f84851`, `plugins/dotnet-diag/skills/analyzing-dotnet-performance/SKILL.md`) → `test-gap-analysis` (dotnet/skills `98f84851`) → `run-tests`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`, `create_item` for findings); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary; `chore` needs `plan` and `questions-resolved` to leave Preparing and `proof` to enter Done)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S22, `docs/desktop/06-ui-design/keyboard-and-accessibility.md` and `docs/desktop/10-security-observability-performance/README.md` for the budgets and the security checklist. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-22-hardening-sweep` and worktree `../pegasus-worktrees/dsk-05-22-hardening-sweep` from `origin/dev`.
2. Confirm the sweep's preconditions: every slice `DSK-05-01`…`DSK-05-21` is merged on `dev`, and the lanes from [[DSK-08-09]], [[DSK-08-11]] and [[DSK-08-15]] exist. Record the exact `dev` SHA the sweep runs against — every finding is anchored to it.
3. Build and install the production-like package on the baseline Test/UAT workstation using `eng/packaging/Test-Package.ps1` from [[DSK-08-10]]; the sweep never runs against a developer `dotnet run` build.
4. Run the full `winapp ui` suite (`pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -All`) and record pass/fail per script. A flake is a finding, not a rerun — record it.
5. Run the `axe-windows` scan per screen through the lane from [[DSK-06-15]] and collect the artefacts. Every critical finding must be resolved before the gate; every non-critical finding is recorded with a disposition.
6. **Operator step** — perform the manual reviews the automated scan cannot replace, from the checklist in [[DSK-06-16]]: keyboard-only completion of every critical workflow, Narrator smoke, 200 % scale, Windows forced-colours mode, reduced motion, focus visibility and logical focus order, and contrast. Automated axe results do not substitute for these (`docs/engineering.md` § Required evidence tiers, tier 7). Record who performed each review and when.
7. Run the performance scripts from [[DSK-08-15]] on the baseline workstation — cold and warm startup, repeated navigation, large list, document- and image-heavy case, memory after prolonged use, slow network, provider timeout, ten concurrent users with the worker, report generation — and produce a regression report against the baseline recorded by [[DSK-01-11]].
8. Run the security checklist from [[DSK-08-11]]: token lifecycle, disabled account, role bypass, direct-object access, malformed uploads, unsafe paths, manifest tampering, version spoofing, temporary-file ACLs, and a secret and log scan over the package and the diagnostics bundle.
9. Run the operator-copy review across every shipped screen against `docs/design/README.md`, covering both what the screen **shows** and what it **asks for**. Display side: no banned word (`intake`, `bounded`, `projection`, `lease`, `opaque`, `ingress`, `composed`, `artifact`, `durable`, `aggregate`, `caller`, `correlation identifier`, `bytes`), no field hints, no how-it-works copy, only populated sections, filters as dropdowns and newest-first tables. **Entry side (upstream PLAT-015, the half this list omitted)**: no identifier entry anywhere — a staff, case or evidence identifier is chosen from a named picker sourced from `ActorDisplayNames`/`IStaffAccountQueries`, never typed as a key or hash — and no raw aggregate identifier appears in a Target or reference column; it resolves to the Case/PO reference or is omitted. Upstream PLAT-015 names the Razor originals this conversion must not reproduce: the `Engineer ID` and `Assignee ID` text inputs (`_CaseWorkflow.cshtml:268`, `:352`, `:371`), the typed `Report SHA-256` input (`:296`), the reply picker showing `InternetMessageIdentity`, and the raw `AggregateId` Target column (`Automation/Activity.cshtml:67`). [[DSK-06-05]]'s `NoRawCodeReachesTheView` reflection test inspects view-model **output** properties only and so cannot see a typed identifier input; the companion test over bound **input** properties is [[DSK-06-05]]'s to add, and this review is the backstop for it, not a substitute. This is a review rule with merge force, not an automated check.
10. For each finding, create a Kanmer ticket in the **owning slice's** area and epic rather than fixing it here, and link it to this ticket. Only cross-screen fixes with no single owner are made on this branch. Record the finding, its severity, its owner and its ticket id in the proof.
11. Capture desktop screenshots for the documentation set via `winapp ui screenshot` (upstream PLAT-005 is absorbed here — screenshots come from a real local run, never a mock-up).
12. Assemble the proof: scan reports, UI suite output, performance regression report, security checklist, manual review records with names and dates, the screenshot set, and the findings table with dispositions. Then run the simplification pass over any code changed on this branch (`n/a — no code change` if the sweep only raised tickets), record it under a dated `## Simplification pass` heading, and open the PR into `dev`.

## Acceptance criteria

- [ ] The sweep ran against a production-like installed package on the baseline workstation, at a recorded `dev` SHA.
- [ ] Zero critical accessibility findings remain; every non-critical finding has a recorded disposition.
- [ ] The manual reviews (keyboard-only, Narrator, 200 %, forced colours, reduced motion, focus, contrast) were performed by a named person on a named date.
- [ ] The performance regression report shows every budget met, or a recorded exception with an owning ticket.
- [ ] The security checklist has no unresolved high-risk item.
- [ ] No banned operator word and no explanatory copy survives on any shipped screen.
- [ ] No identifier entry anywhere — a staff, case or evidence identifier is chosen from a named picker sourced from `ActorDisplayNames`/`IStaffAccountQueries`, never typed as a key or hash — and no raw aggregate identifier appears in a Target or reference column; it resolves to the Case/PO reference or is omitted (upstream PLAT-015).
- [ ] Every finding has an owning ticket in the slice that owns the screen.

## Verification

- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -All` — expected: every script passes on the installed package; any flake is recorded as a finding.
- [ ] `pwsh ./eng/packaging/Test-Package.ps1` — expected: clean install of the production-like package on the baseline workstation.
- [ ] `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category!=Corpus"` — expected: the full automated suite passes at the recorded SHA.
- [ ] `axe-windows` scan artefacts — expected: zero critical findings; artefacts attached to the ticket proof.
- [ ] Operator-copy review record in the ticket proof — expected: a per-screen pass covering the display side and the entry side, naming the reviewer and the date, with every identifier-entry and Target-column exception listed and owned.
- [ ] Manual review, performance and security records in the ticket proof — expected: named reviewers with dates, budgets met, no unresolved high-risk item.

## Evidence tier

Tier 7 — Browser/accessibility. Tier 9 — Security/observability. Tier 10 — Performance/concurrency.
Tier 7 obliges keyboard, focus, semantic-label, text-plus-colour and 200 %-scale evidence, and explicitly states that automated axe results do not replace manual keyboard or assistive-technology review; tier 9 obliges the role matrix, throttling, request-forgery, dependency scanning, redaction and bounded-failure evidence; tier 10 obliges measured behaviour at eight concurrent operators and the stated case and file volumes.

## Documentation changes

- `docs/desktop/10-security-observability-performance/README.md` — the recorded performance baseline for this release candidate
- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — confirm every row carries its verification evidence
- `docs/frd/frd-13-desktop-operator-experience.md` — only where a finding changes stated behaviour

## Guardrails

- **Azure**: no write.
- **Scope boundary**: this ticket produces evidence and raises tickets. Fixes land in the owning slice's projects; only a genuinely cross-screen fix with no single owner is made on this branch.
- **Traps**: automated scans do not replace manual keyboard and assistive-technology review; performance figures must come from the baseline workstation, not a developer machine; the operator-copy rules are merge rules with no CI enforcement, so the review is manual and must be recorded honestly; a reflection test over view-model output properties cannot see an identifier that is *typed in*, so the entry-side rule is reviewed here and tested in [[DSK-06-05]] — treating the output-only test as coverage is how upstream PLAT-015's GUID inputs would survive the conversion; C-01 makes added CI lanes cost real money on private-repository Windows runners — coordinate any new lane with [[DSK-08-19]]; upstream PLAT-005 and upstream PLAT-015 are absorbed here, and absorbing upstream PLAT-015 means both its display and its entry halves — **note the collisions: neither has a fork ticket, and the board's `PLAT-005` and `PLAT-015` are `DSK-10-05` and `DSK-10-15`, different tickets entirely** (`HZN-001` group document `board-conventions.md` § Upstream ids versus board ids holds the join table).
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document (`n/a — no code change` when the sweep only raised tickets).

## Outcome

_Filled at closeout._
