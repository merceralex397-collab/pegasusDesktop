---
id: PLAT-027
type: ticket
title: DSK-11-09 · Telemetry cap decision input (PLAT-036) after the desktop pilot
status: backlog
area: platform-operations
assignee: ''
profile: spike
labels:
  - desktop-conversion
  - plan-11
  - phase-10
  - tier-9
  - azure-write
  - needs-operator
groups:
  - EPIC-012
  - HZN-011
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:33:11.212Z'
updated: '2026-08-24T08:33:11.212Z'
---

## What

Measure the desktop-era Log Analytics ingestion volume after the pilot ring has run, price the options, and produce a written recommendation on whether to raise the 0.1 GB/day cap on `pegasus-prod-logs-<suffix>` — including the exact-target approval text and rollback if the answer is yes. The spike itself performs **no** Azure write.

## Why

The workspace runs a 0.1 GB daily quota that resets at 03:00Z and the estate exhausts it within hours, so every check run in a UK working hour returns empty, both production custody failures left no trace, and the two Sev1 alert rules cannot fire on the capped window (`docs/current-architecture.md:160-175`, PLAT-034). PLAT-036 carries the quota question. The desktop conversion adds client-version, blocked-client and update-required dimensions to that same workspace ([[DSK-10-14]]), so the volume changes and the decision cannot be made on pre-desktop numbers. Operator-visible consequence: if the cap stays where it is, the pilot's support evidence comes only from desktop diagnostics bundles; if it is raised without measurement, the 75 monthly budget absorbs an unpriced increase.

Siblings: [[DSK-10-14]] adds the dimensions and writes the volume report this consumes; [[DSK-10-16]] owns the alert-rule and quota follow-up on the area-10 side; [[DSK-11-04]] supplies the per-GB price; [[DSK-11-03]] holds the catalogue row for the cap change.

## Source of truth

- Plan row: `docs/desktop/11-azure-disposition/README.md` § 5 — `DSK-11-09`
- Plan detail: `docs/desktop/11-azure-disposition/README.md` § 3 ("Telemetry: the Log Analytics daily cap (PLAT-036) is only raised if measurements after the desktop pilot show it is needed ⚠") and § 5 "Conditional Azure writes" row "Log Analytics daily cap change"
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 18.2 Central telemetry; § 19 Azure service disposition; § 24 Phase 10
- Repository evidence:
  - `infra/modules/platform.bicep:46` the Log Analytics workspace — PerGB2018, `retentionInDays: 31`, and **no `workspaceCapping` property**: the 0.1 GB/day cap is set out of band, so a Bicep change would have to introduce the property
  - `infra/modules/platform.bicep:56` App Insights (workspace-based, `DisableLocalAuth: true`); `:576`/`:617` the two Sev1 alert rules that cannot fire on the capped window
  - `infra/main.bicep:114` budget `pegasus-prod-monthly` amount 75 — the cost envelope any raise sits inside
  - `docs/current-architecture.md:160-175` — the measured description of the blind window, sampling (`APPLICATIONINSIGHTS_ENABLEADAPTIVESAMPLING`) and the fact that the Worker's own polling produces most of the volume
  - `docs/desktop/01-inventory-and-parity/azure-resource-register.md:90` — the register's PLAT-036 note
  - `docs/desktop/10-security-observability-performance/README.md:123`, `:174` — area 10's position: measure desktop-era volume first
- Binding decisions:
  - **L-02** — there is no Azure test environment; the measurement is taken from production during the pilot, read-only.
  - **ADR-0109** (to be authored) — desktop diagnostics bundle plus the existing App Insights; no new telemetry fleet, so "send desktop telemetry somewhere else" is not an option this spike may recommend.
  - Proposal § 19.1 — the recommendation may not introduce a new service (a second workspace, Event Hubs, a collector fleet); the only levers are the cap, retention, sampling and what is emitted.
- Depends on: `DSK-10-14` — the gateway telemetry dimensions and the desktop-era volume report (the plan row cites `DSK-10-07`, the desktop temp-files and cache ticket, which produces no telemetry volume; treat that as an off-by-one). `DSK-11-04` supplies the per-GB/day price.

## Routing

- **Subagent**: `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (read-only; it produces the approval text and stops there)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `azure-diagnostics` (`microsoft/azure-skills` `1a03acfb`, monitor and AppLens reads) → `appinsights-instrumentation` (same pin, for what the sampling and ingestion levers actually do) → `kanmer-research` (`.grok/skills/kanmer-research/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** (`monitor`, `applicationinsights`, `pricing`, `group_resource_list`); Microsoft Learn (`microsoft_docs_search` for `workspaceCapping` / daily cap semantics and the `Usage` table schema)
- **Kanmer pipeline** for profile `spike`: `kanmer-research` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. The `spike` profile has one gate only — `research` + `questions-resolved` to enter `done` — so the finished `research` document *is* the deliverable. Call `get_doc_gates <this ticket id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read `docs/desktop/11-azure-disposition/README.md` § 3 and § 5, `docs/current-architecture.md:160-175`, and `docs/desktop/10-security-observability-performance/README.md` § 5 rows DSK-10-14 and DSK-10-16. Then `get_doc_gates <this ticket id>` and `take_ticket <this ticket id>`.
2. Confirm the pilot has actually run: this spike is only meaningful after the pilot ring has been in use for a representative period (proposal § 24 Phase 9). If it has not, stop and record that the timebox has not opened yet rather than measuring pre-desktop volume.
3. Read the current cap state: Azure MCP `monitor` workspace show for `pegasus-prod-logs-<suffix>` — record `workspaceCapping.dailyQuotaGb`, `workspaceCapping.dataIngestionStatus` and `workspaceCapping.quotaNextResetTime`. Note in the research document that no `workspaceCapping` property exists in `infra/modules/platform.bicep:46`, so the current cap is out-of-band configuration.
4. Measure the volume with a read-only KQL query over the `Usage` table for the pilot period, for example `Usage | where TimeGenerated > ago(30d) | where IsBillable == true | summarize BillableGb = sum(Quantity) / 1000 by bin(TimeGenerated, 1d), DataType | order by TimeGenerated asc`. Record the daily total, the split by `DataType`, and how early in the day the cap is reached. Attach the raw result with `append_scratch`.
5. Separate the sources so the recommendation is actionable: Worker polling versus Web/gateway requests versus the new desktop-era dimensions (client version, channel, blocked-client count, update-required responses) that [[DSK-10-14]] added. `docs/current-architecture.md:160-175` records that Worker polling produced most of the volume before the desktop existed — confirm or refute that with the measurement.
6. Establish what is lost at the current cap, in operational terms rather than bytes: how many UK working hours are blind per day, whether either Sev1 alert rule could have fired in that window, and which of the pilot's own incidents left no trace. This is the evidence that decides the question.
7. Price each option with Azure MCP `pricing` for `uksouth`, reusing [[DSK-11-04]]'s baseline: keep the cap; raise it to a named GB/day figure; reduce ingestion instead (sampling, dropping a noisy `DataType`, shortening the 31-day retention). Give each option a monthly cost and the headroom left against the 75 budget.
8. Write the recommendation as one of exactly three outcomes with its reason: **keep the cap** (desktop diagnostics bundles carry the support load, ADR-0109); **reduce ingestion first** (named `DataType` or sampling change, no Azure write to the cap); or **raise the cap to N GB/day** (a marked ⚠ Azure write). Do not recommend a new service — proposal § 19.1 rules that out by default.
9. If, and only if, the recommendation is to raise the cap, draft the approval text from the plan's template and stop there: *Request `change` of `workspaceCapping.dailyQuotaGb` on Log Analytics workspace `pegasus-prod-logs-<suffix>` in `rg-pegasus-prod` (subscription `e6076573-23a5-46a8-acef-7e22d264e5db`, tenant `858cf5b3-aa0a-47a6-9b40-4851fd0afa94`) because `<measured evidence>`; Bicep change at `infra/modules/platform.bicep:46`; applied through `azd provision` by the `pegasus-release` route; rollback: restore the previous `dailyQuotaGb` and re-provision; nothing else changes.* Note that the property must be **added** to Bicep, bringing an out-of-band setting under IaC in the same change.
10. **Operator step — the decision, not the measurement.** Hand the recommendation, the cost table and the approval text to the operator. Evidence handed back: the decision with its date. If it is "raise", the execution is a separate ticket owned by `pegasus-release-packager` and mirrored in [[DSK-11-03]]'s catalogue and [[DSK-10-16]]; this spike closes either way.
11. Record the outcome in `docs/desktop/11-azure-disposition/README.md` § 3 (replacing the conditional sentence with the dated decision) and cross-reference PLAT-036 so the upstream carry-over ticket can be resolved.
12. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`, complete the `research` document (the spike's only gate), and hand to `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] Measured desktop-era daily ingestion volume is recorded with its query, its period and its split by `DataType`.
- [ ] The volume is attributed across Worker polling, gateway requests and the desktop-era dimensions.
- [ ] The operational loss at the current cap is stated in working hours blind per day and in alert-rule terms, not only in gigabytes.
- [ ] Each option — keep, reduce ingestion, raise to N GB/day — has a monthly cost and the headroom left against the 75 budget.
- [ ] A single recommendation is written with its reason; no new Azure service is proposed.
- [ ] If the recommendation is to raise, the exact-target approval text and rollback are drafted and the execution is left to a separate approved ticket.
- [ ] Zero Azure writes were performed by this spike.

## Verification

- [ ] Azure MCP `monitor` workspace show for `pegasus-prod-logs-<suffix>` — expected: `workspaceCapping.dailyQuotaGb` unchanged at the end of the spike, proving no write occurred.
- [ ] The `Usage` KQL query re-run by the reviewer over the same window — expected: the same billable total within rounding.
- [ ] `grep -n "PLAT-036" docs/desktop/11-azure-disposition/README.md` — expected: the dated decision replaces the conditional sentence.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exits 0.
- [ ] `git diff --stat` — expected: only `docs/desktop/11-azure-disposition/README.md`; nothing under `infra/`, `src/` or `scripts/`.

## Evidence tier

Tier 9 — Security/observability. The obligation is that the recommendation rests on measured, attached, read-only telemetry evidence and on priced options — not on the intuition that more logging is better — and that the blind-window loss is quantified rather than asserted.

## Documentation changes

- `docs/desktop/11-azure-disposition/README.md` § 3 — the conditional telemetry sentence replaced by the dated decision; § 5 Conditional Azure writes — the cap-change row updated to "recommended / not recommended" with the date.
- `docs/current-architecture.md:160-175` — only if the measurement changes the recorded description of the blind window; refresh it in the same task if so.

## Guardrails

- **⚠ Azure write** (marked on the plan row, **not performed here**): a change to `workspaceCapping.dailyQuotaGb` on `pegasus-prod-logs-<suffix>` in `rg-pegasus-prod` would need explicit approval for that exact target with fresh inventory (`docs/runbook.md` § Live-operation approval matrix) and is mirrored in `docs/desktop/11-azure-disposition/README.md` § Conditional Azure writes. This spike measures, prices and recommends only; reads are free. **Nothing is deprovisioned before cutover, observed non-use and rollback approval.**
- **Scope boundary**: `docs/desktop/11-azure-disposition/README.md` and, if contradicted, `docs/current-architecture.md`. Do not edit `infra/modules/platform.bicep` — adding the `workspaceCapping` property belongs to the approved execution ticket — and do not touch `src/` or the alert rules.
- **Traps** (plan § 7): telemetry blind spots are the subject here, so the measurement itself is affected by them — state the window the data covers and where it is truncated by the cap; out-of-band configuration (this cap) is invisible to `azd provision`, which is why any raise must also bring it into `infra/`; a service is not "unused" merely because the capped window shows no traffic.
- **Board placement**: this plan area seeds into `platform-operations` because the board-shape table in `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape assigns no area to plan 11.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document (`n/a — docs-only` for documentation-only tickets).

## Outcome

_Filled at closeout._
