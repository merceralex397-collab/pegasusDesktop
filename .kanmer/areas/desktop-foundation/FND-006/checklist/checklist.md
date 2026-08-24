# Checklist — FND-006

One box per plan step, in plan order. Every box is independently tickable.

- [ ] Read the plan row, § 3's ADR table rows for 0102/0106/0107/0109 and `docs/desktop/01-inventory-and-parity/flow-records.md` in full; call `get_doc_gates FND-006` and `take_ticket`
- [ ] Run `ls docs/adr/010*` **before writing anything** and record its exact output (expected on 2026-08-24: no such file)
- [ ] Record in `plan` whether an ADR-0102 file already existed and, if so, that this ticket verifies and extends it in place rather than creating a second file
- [ ] Confirm [[FND-019]] is `done` and record 1's Q1.1–Q1.4 (`flow-records.md:90-99`) each carry a code citation or a named `docs/open-decisions.md` line
- [ ] Confirm record 3's Q3.1–Q3.3 (`:227-234`) are closed the same way
- [ ] Confirm [[FND-020]] is `done` and record 4's Q4.1–Q4.3 (`:296-303`) and record 5's Q5.1–Q5.3 (`:350-354`) are closed the same way
- [ ] Record explicitly whether Q4.1 (short-lived constrained Box tokens) and Q5.1 (direct native provider call) resolved **for** or **against** the assumed ADR-0107 boundary — and stop and revise this plan if either resolved against it
- [ ] Obtain the per-ADR `file:line` evidence set from `pegasus-parity-researcher` and paste it into the ticket (it cannot write files)
- [ ] Create `docs/adr/0102-existing-pegasus-credentials-token-session.md` — the single agreed path
- [ ] Create `docs/adr/0106-graph-intake-worker-stays-central.md`
- [ ] Create `docs/adr/0107-provider-credentials-behind-the-gateway.md`
- [ ] Create `docs/adr/0109-desktop-diagnostics-bundle-and-existing-app-insights.md`
- [ ] Give all four the `AGENTS.md:94-108` frontmatter block with `status: accepted`, a real `date`, `supersedes: []`, `superseded_by: []`, and `related_frd` written as lowercase stems
- [ ] Give all four the heading set `## Status · ## Context · ## Decision · ## Consequences · ## Options considered · ## Links`, following `docs/adr/0029-*.md:11-20`
- [ ] Put the six-question cloud-justification table in each of the four `## Context` sections, with a real answer and a real citation in every row — no blank cells
- [ ] Answer ADR-0106's "unattended execution" as **yes**, citing the Worker timer `src/Pegasus.Worker/MailboxFunctions.cs:15` and its `%ApprovedInboxPollSchedule%` setting
- [ ] Answer ADR-0102's "central enforcement" as **yes**, citing the per-request `IsEnabled` re-check (`src/Pegasus.Web/Program.cs:368-457`) and the revocation path
- [ ] Answer ADR-0109's six rows so they justify adding **no** new telemetry service, with "measured operational advantage" answered **no** and citing the 0.1 GB/day Log Analytics cap (`azure-resource-register.md:36`)
- [ ] Record relations in frontmatter and `## Links`: ADR-0102 → ADR-0004, ADR-0011, ADR-0027; ADR-0106 → ADR-0024; ADR-0109 → **`upstream PLAT-034`**, written that way, never bare
- [ ] Confirm all four carry `supersedes: []` and `superseded_by: []` — none of them supersedes an accepted decision
- [ ] State ADR-0107's negative decision: no long-lived provider secret is ever placed in the MSIX package or on a workstation
- [ ] State ADR-0106's negative decision: intake must continue with every desktop closed
- [ ] State ADR-0102's negative decision: no Microsoft-account or Entra login for staff
- [ ] State ADR-0109's negative decision: no OpenTelemetry collector fleet; the App Insights SDK stays the telemetry path
- [ ] Add one row per ADR to `docs/adr/README.md`'s accepted table in ID order, three cells each (`ADR | Title | Related FRD`)
- [ ] Confirm `git diff --stat -- AGENTS.md` is empty — the index-shape correction is [[FND-005]]'s, not this ticket's
- [ ] `link_doc` each of the four new ADR paths to this ticket
- [ ] Clear `docs_todo` on the area 04 auth tickets (ADR-0102), area 07 integration tickets (ADR-0106, ADR-0107) and area 10 observability tickets (ADR-0109) — only after each `link_doc` exists
- [ ] Open the PR against `dev` and take the independent review from `pegasus-desktop-reviewer`
- [ ] Record the simplification pass under a dated `## Simplification pass` heading in `plan` (`n/a — docs-only`)
- [ ] Verification run — `pwsh ./scripts/Test-DocumentationLinks.ps1`, `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`, `ls docs/adr/010*`, the cloud-test row counts, the `PLAT-034` and `related_frd` greps, the index-row grep and `git diff --stat -- AGENTS.md` — all as the plan's Verification table states; **this box produces `proof`**

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
