# Plan — FEAT-022: S22 Hardening sweep

**Diff estimate: ~6 files, ~250 lines of repository change, plus 10–25 new Kanmer tickets.**
This is a `chore` that produces evidence and raises tickets; only a genuinely cross-screen fix with
no single owner is made on this branch. The repository change is: `docs/desktop/10-security-observability-performance/README.md`
(the recorded performance baseline for this release candidate, ~40 lines),
`docs/desktop/01-inventory-and-parity/parity-matrix.md` (evidence confirmation across the 46 `PAR-`
rows, ~60 lines), `docs/frd/frd-13-desktop-operator-experience.md` (only where a finding changes
stated behaviour, ~30 lines), and up to three cross-screen fix files (~120 lines) if any survive
triage. Every other fix lands in the owning slice's projects.

**Chore inventory** (this profile owes no `research` or `files` document, so the measured surface
area is stated here; `docs/engineering.md` § plan sizing requires the estimate to come from a real
inventory):

| Path | Measured today | What the sweep does with it |
| --- | --- | --- |
| `tests/Pegasus.IntegrationTests/Browser/` | 9 files, 2,463 lines total across the lane; `AccessibilityTests.cs` **156 lines**, `OperatorJourneyTests.cs` 612, `BrowserTestSupport.cs` 209 | The **web-only** precedent. It says nothing about the desktop and is not extended by this ticket. |
| `tests/Pegasus.Desktop.UITests/ui-tests.ps1` *(created by [[TEST-006]] (plan handle `DSK-08-06`))* | does not exist yet | Run with `-All`; pass/fail recorded per script. |
| `eng/packaging/Test-Package.ps1` *(created by [[TEST-010]] (plan handle `DSK-08-10`))* | does not exist yet | Installs the production-like package on the baseline workstation. |
| `tests/Pegasus.ArchitectureTests/` | 11 `.cs` files; `DependencyDirectionTests.cs` **520 lines** | Run as part of the full suite; not modified here. |
| `docs/design/README.md` | banned-words paragraph at **`:412-421`**; approved copy list at **`:400-410`**; the four hard rules under `## No explanatory copy and page economy` at **`:422-445`** | The display-side review rules. `:417-421` states plainly that this "is a review rule, not an automated check — nothing in CI enforces it today". |
| `docs/desktop/06-ui-design/keyboard-and-accessibility.md` | `## Keyboard map` `:9`, `## Focus order and visible focus` `:40`, `## Accessibility checklist (per screen, enforced in review)` `:56`, `## Automated checks` `:99`, `## The ten recorded reviews …` `:115`, `## Acceptance` `:148` | The checklist the operator step works through. |
| `docs/desktop/10-security-observability-performance/README.md` | `## 4. Target state and exit gate` at **`:131`**; the Phase 8 exit gate restated at `:148-152` | The budgets and the security checklist; the recorded baseline is written back here. |
| `src/Pegasus.Core/Actors/ActorDisplayNames.cs` | **`:12`** `public static class ActorDisplayNames`; the comment at `:8-11` states that "every read model that shows 'who did this' resolves through here rather than printing the subject id (a raw GUID for a staff actor) directly" | The named source a staff picker draws from. |
| `src/Pegasus.Core/Identity/StaffAccountAdministration.cs` | **`:110`** `public interface IStaffAccountQueries` (`ListAsync(offset, limit, …)`, returning `StaffAccountQuerySlice`) | The other named source. |
| `src/Pegasus.Web/Pages/Cases/Shared/_CaseWorkflow.cshtml` | `:268` `<label>Engineer ID<input type="text" name="engineerId" required /></label>`; `:296` `<label>Report SHA-256<input name="artifactSha256" required minlength="64" maxlength="64" pattern="[0-9A-Fa-f]{64}" /></label>`; `:352` and `:371` `<label>Assignee ID<input type="text" name="assigneeId" /></label>` | The four Razor originals upstream PLAT-015 names. **Not edited** — they are the pattern the conversion must not reproduce. |
| `src/Pegasus.Web/Pages/Administration/Automation/Activity.cshtml` | `:67` `<td>@(record.AggregateId ?? "—")</td>` — the raw aggregate identifier in the Target column, beside `OperatorLabels.Humanise(...)` at `:64` and `:66` | The fifth PLAT-015 original. Also not edited. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | **46** `PAR-` rows (`grep -c '^| PAR-'`) | Confirm every row carries its verification evidence. |

## Approach

Run the sweep **once, against one recorded `dev` SHA, on a production-like installed package**, and
anchor every finding to that SHA. Automated evidence (`winapp ui -All`, `axe-windows`, the
performance scripts, the security checklist) is gathered first; the manual reviews that automation
cannot replace are performed second by a named person on a named date; the operator-copy review —
**both its display half and its entry half** — is performed third as a merge-force review rule.
Each finding becomes a Kanmer ticket in the **owning slice's** area and epic, linked to this one.

Rejected: **fixing findings on this branch**. It would concentrate twenty-one slices' worth of
change in one PR reviewed by an agent with no context on any of them, and it would hide which slice
regressed. Also rejected: **treating the `axe-windows` scan as the accessibility evidence**;
`docs/engineering.md` § Required evidence tiers, tier 7, states explicitly that "Automated axe
results do not replace manual keyboard or assistive-technology review", and
`docs/design/README.md:417-421` says the copy rules have no CI enforcement at all.

## Governing docs

The ticket's `refs` is `docs/frd/frd-12-operator-experience.md`, which exists.

| Ref | Requirement | Meets |
| --- | --- | --- |
| FRD-12 § `Operator experience` (`:4`ff) | The operator sees state without inferring it; a surface does not assert more than it knows | Step 9 (display-side review: no banned word, no how-it-works copy, only populated sections) |
| FRD-12 § `Upload` (`:29`ff), § `Queues: tabs and filters` (`:58`ff) | Settled operator-facing behaviour for the surfaces the sweep walks | Steps 4–6 (every critical workflow completed keyboard-only) |
| FRD-12 § `Dashboard freshness and reconciliation` (`:93`ff) | Freshness is stated, not implied | Step 6 (manual review includes freshness on every screen that claims it) |

`docs_todo: true`, confirmed in `get_doc_gates FEAT-022` — this is a `chore`, so `leave-preparing`
requires `plan` and `questions-resolved` only, and `enter-done` requires `proof`.

> **New ADR** — ADR-0109 (desktop diagnostics bundle plus the existing App Insights; no new
> telemetry fleet), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table, `:164`); if the ADR lands
> differently this plan is revised before implementation. ADR-0105 (signed MSIX/App Installer
> distribution with a gateway minimum-version gate) also governs the packaged artefact the sweep
> installs; it has **more than one claimant** — `REL-001`, `FND-005` and `FND-042` — so it is
> written *authored by [[REL-001]]; see [[REL-001]]'s plan for the ownership reconciliation* rather
> than asserting a single author.

Programme-level authorities that bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 14.9 Keyboard and accessibility | Keyboard, focus, assistive-technology, scale and forced-colours behaviour on every screen | Steps 5–6 |
| Proposal § 15 Performance design | The §15.1 budgets are measured on the recorded baseline workstation for every release candidate | Step 7 |
| Proposal § 17 Security and privacy | Every §17.3 threat has a named control and a test that exercises it; the package carries no secret | Step 8 |
| Proposal § 24, Phase 8 exit gate (restated `docs/desktop/10-…/README.md:148-152`) | Full automated suite passes; accessibility critical issues resolved; no unresolved high-risk security item; production-like package tested | Steps 3–8, § Verification |
| `docs/engineering.md` § Required evidence tiers, tier 7 | "Automated axe results do not replace manual keyboard or assistive-technology review" | Step 6, and § Approach's rejected alternative |
| `docs/engineering.md` § Required evidence tiers, tier 10 | Measured behaviour at eight concurrent operators and the stated case and file volumes | Step 7 |
| `docs/design/README.md:412-421` | The banned-words list, and the statement that it is a review rule with no CI enforcement | Step 9 |
| `docs/design/README.md:422-445` | The four hard rules — a field is a label and a control; no how-it-works copy; only populated sections render; filters are dropdowns and tables sort newest first | Step 9 |
| Upstream PLAT-015 (absorbed here) | No identifier entry; no raw aggregate identifier in a Target or reference column | Step 9's **entry-side** half |
| Upstream PLAT-005 (absorbed here) | Screenshots come from a real local run | Step 11 |
| L-02 | Every measurement runs on the local Test/UAT workstation, never an Azure environment | Steps 3, 7 |
| L-04 | Routing named on the ticket | § Routing |
| C-01 | Private-repository Windows runner minutes bill at 2×; any lane this sweep adds costs real money | § Risks; coordinate with [[TEST-019]] (plan handle `DSK-08-19`) |
| `HZN-001` / `board-conventions.md` § Upstream ids versus board ids | A bare `<PREFIX>-<nnn>` is a fork board id | Step 2 |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires in the plan.

- **Subagent**: `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml` (scans, UI suite,
  performance); `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
  (independent review of findings); `pegasus-test-engineer` —
  `.codex/agents/pegasus-test-engineer.toml` (suite health and gap analysis)
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-ui-testing`
  (`.codex/skills/winui-ui-testing/SKILL.md`) → `winui-code-review`
  (`.codex/skills/winui-code-review/SKILL.md`) → `analyzing-dotnet-performance` (dotnet/skills
  `98f84851`, `plugins/dotnet-diag/skills/analyzing-dotnet-performance/SKILL.md`) →
  `test-gap-analysis` (dotnet/skills `98f84851`) → `run-tests`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`, `create_item` for findings); Microsoft Learn
  (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` →
  `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated
  boundary; `chore` needs `plan` and `questions-resolved` to leave Preparing and `proof` to enter
  Done)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refining the ticket body's twelve steps — same order, same ownership. Body step numbers in
brackets.

1. **[body 1] Orient and take.** Read the plan row, `vertical-slices.md` § S22,
   `docs/desktop/06-ui-design/keyboard-and-accessibility.md` (the checklist at `:56`, the automated
   checks at `:99`, the ten recorded reviews at `:115`, acceptance at `:148`) and
   `docs/desktop/10-security-observability-performance/README.md` § 4 (`:131-152`) for the budgets
   and the security checklist. Call `get_doc_gates FEAT-022`, then `take_ticket` with branch
   `task/dsk-05-22-hardening-sweep` and worktree `../pegasus-worktrees/dsk-05-22-hardening-sweep`
   from `origin/dev`.
2. **[body 2] Confirm preconditions and pin the SHA.** Every slice [[FEAT-001]] … [[FEAT-021]]
   (plan handles `DSK-05-01` … `DSK-05-21`) is merged on `dev`, and the lanes from [[TEST-009]],
   [[TEST-011]] and [[TEST-015]] (plan handles `DSK-08-09`, `DSK-08-11`, `DSK-08-15`) exist.
   Record the exact `dev` SHA the sweep runs against — every finding is anchored to it. Record also
   the namespace facts: neither upstream `PLAT-005` nor upstream `PLAT-015` has a fork ticket, and
   the board's own `PLAT-005` and `PLAT-015` are `DSK-10-05` and `DSK-10-15`, different tickets
   entirely (`HZN-001` / `board-conventions.md`).
3. **[body 3] Install the production-like package.** `pwsh ./eng/packaging/Test-Package.ps1` from
   [[TEST-010]] (plan handle `DSK-08-10`), on the baseline Test/UAT workstation. The sweep never
   runs against a developer `dotnet run` build. Record the workstation specification here — the
   performance figures are meaningless without it.
4. **[body 4] Full UI suite.** `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -All`; record
   pass/fail per script. **A flake is a finding, not a rerun** — record it with its script name and
   the run in which it flaked.
5. **[body 5] `axe-windows` scan per screen.** Through the lane from [[DUI-015]] (plan handle
   `DSK-06-15`); collect the artefacts. Every **critical** finding must be resolved before the
   gate; every non-critical finding is recorded with a disposition.
6. **[body 6] Operator step — the manual reviews.** From [[DUI-016]]'s checklist (plan handle
   `DSK-06-16`), and from `keyboard-and-accessibility.md:115-147`: keyboard-only completion of every
   critical workflow, Narrator smoke, 200 % scale, Windows forced-colours mode, reduced motion,
   focus visibility and logical focus order, and contrast. Automated axe results do **not**
   substitute (`docs/engineering.md` § Required evidence tiers, tier 7). Record **who** performed
   each review and **when**.
7. **[body 7] Performance.** Run the scripts from [[TEST-015]] (plan handle `DSK-08-15`) on the
   baseline workstation — cold and warm startup, repeated navigation, large list, document- and
   image-heavy case, memory after prolonged use, slow network, provider timeout, ten concurrent
   users with the Worker, report generation — and produce a regression report against the baseline
   recorded by [[FND-024]] (plan handle `DSK-01-11`). Tier 10 obliges measured behaviour at eight
   concurrent operators and the stated case and file volumes.
8. **[body 8] Security checklist.** From [[TEST-011]] (plan handle `DSK-08-11`): token lifecycle,
   disabled account, role bypass, direct-object access, malformed uploads, unsafe paths, manifest
   tampering, version spoofing, temporary-file ACLs, and a secret and log scan over the package and
   the diagnostics bundle.
9. **[body 9] The operator-copy review, both halves.** Across every shipped screen, against
   `docs/design/README.md`:
   - **Display side** (`:412-421`, `:422-445`): no banned word — `intake`, `bounded`, `projection`,
     `lease`, `opaque`, `ingress`, `composed`, `artifact`, `durable`, `aggregate`, `caller`,
     `correlation identifier`, `bytes`; no field hints; no how-it-works copy; only populated
     sections render; filters as dropdowns and newest-first tables.
   - **Entry side (upstream PLAT-015 — the half the display list omits)**: **no identifier entry
     anywhere.** A staff, case or evidence identifier is chosen from a named picker sourced from
     `ActorDisplayNames` (`src/Pegasus.Core/Actors/ActorDisplayNames.cs:12`, whose own comment at
     `:8-11` says every "who did this" read model "resolves through here rather than printing the
     subject id (a raw GUID for a staff actor) directly") or `IStaffAccountQueries`
     (`src/Pegasus.Core/Identity/StaffAccountAdministration.cs:110`) — never typed as a key or a
     hash. And **no raw aggregate identifier** appears in a Target or reference column; it resolves
     to the Case/PO reference or is omitted.
   - The Razor originals this conversion must not reproduce, for reference only:
     `_CaseWorkflow.cshtml:268` (`Engineer ID` text input), `:296` (typed `Report SHA-256`),
     `:352` and `:371` (`Assignee ID` text inputs), the reply picker showing
     `InternetMessageIdentity`, and `Administration/Automation/Activity.cshtml:67`
     (`@(record.AggregateId ?? "—")`).
   - **Why the review, not a test:** [[DUI-005]]'s (plan handle `DSK-06-05`)
     `NoRawCodeReachesTheView` reflection test inspects view-model **output** properties only and so
     cannot see a typed identifier *input*. The companion test over bound **input** properties is
     [[DUI-005]]'s to add; this review is the **backstop for it, not a substitute**. Treating the
     output-only test as coverage is how upstream PLAT-015's GUID inputs would survive the
     conversion. This is a review rule with merge force and no CI enforcement.
10. **[body 10] Raise, do not fix.** For each finding, create a Kanmer ticket in the **owning
    slice's** area and epic and link it to this ticket. Only cross-screen fixes with no single
    owner are made on this branch. Record the finding, its severity, its owner and its ticket id in
    the proof.
11. **[body 11] Screenshots from a real run.** `winapp ui screenshot` for the documentation set —
    upstream PLAT-005 is absorbed here, and its point is that screenshots come from a real local
    run, never a mock-up.
12. **[body 12] Assemble the proof and close.** Scan reports, UI suite output, the performance
    regression report, the security checklist, the manual review records with names and dates, the
    screenshot set, and the findings table with dispositions. Then run the simplification pass over
    any code changed on this branch (`n/a — no code change` if the sweep only raised tickets),
    record it under a dated `## Simplification pass` heading, and open the PR into `dev`.

## Verification

Evidence tiers from the body: **7** (Browser/accessibility), **9** (Security/observability),
**10** (Performance/concurrency).

- `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -All` — every script passes on the installed
  package; any flake is recorded as a finding, not rerun away.
- `pwsh ./eng/packaging/Test-Package.ps1` — clean install of the production-like package on the
  baseline workstation.
- `dotnet test ./Pegasus.slnx --configuration Release --no-build --filter "Category!=Corpus"` —
  the full automated suite passes at the recorded SHA.
- `axe-windows` scan artefacts — zero critical findings; artefacts attached to the proof.
- **Operator-copy review record** — a per-screen pass covering the **display** side and the
  **entry** side, naming the reviewer and the date, with every identifier-entry and Target-column
  exception listed and owned.
- **Manual review, performance and security records** — named reviewers with dates, budgets met,
  no unresolved high-risk item.

Evidence that becomes `proof`: all of the above, plus the findings table (finding, severity, owner,
ticket id) and the screenshot set.

## Risks / open questions

- **The sweep is only meaningful once every slice has merged.** Mitigation: step 2 is a hard
  precondition; a sweep run early produces findings against code that will change, which is worse
  than no sweep.
- **Automated scans are not the accessibility evidence.** `docs/engineering.md` § Required evidence
  tiers, tier 7, says so explicitly. Mitigation: step 6 is an operator step with named reviewers and
  dates; the proof is rejected without them.
- **The operator-copy rules have no CI enforcement**, stated at `docs/design/README.md:417-421`.
  Mitigation: step 9 is a recorded manual review with merge force. Recording it honestly — including
  the exceptions — is the whole control.
- **The entry-side rule cannot be tested by the existing reflection test.**
  [[DUI-005]]'s `NoRawCodeReachesTheView` sees view-model *output* properties only. The companion
  input-property test is **[[DUI-005]]'s** to add — that is the owner of the gap. Mitigation:
  step 9 is the backstop and says so; a finding here raises a ticket against [[DUI-005]], not
  against the sweep.
- **Performance figures from the wrong machine are worthless.** Mitigation: step 3 records the
  workstation specification and step 7 measures on it; L-02 forbids an Azure environment.
- **Namespace collisions.** Neither upstream `PLAT-005` nor upstream `PLAT-015` has a fork ticket;
  the board's `PLAT-005` and `PLAT-015` are `DSK-10-05` and `DSK-10-15`. Mitigation: step 2 records
  it; the join table is in `HZN-001`'s `board-conventions.md`. A finding filed against the wrong id
  is how the board deletes real work.
- **C-01: added CI lanes cost real money.** Private-repository Windows runners bill at a 2×
  multiplier. Mitigation: coordinate any new lane with [[TEST-019]] (plan handle `DSK-08-19`), who
  owns the CI cost and runner plan. Owner of that decision: [[TEST-019]].
- **A flake will be tempting to rerun.** Mitigation: step 4 states that a flake is a finding, and
  the proof records the script name and the failing run.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading — `n/a — no code change` where the sweep
only raised tickets._
