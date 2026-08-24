# Plan — PLAT-016

## Objective

Decide, with the volume evidence from [[DSK-10-14]], whether to raise the Log Analytics daily cap (PLAT-036) and whether to add a third alert rule for blocked-client spikes and compatibility-gate failures — and, only if approved, make the `infra/modules/platform.bicep` change and prove it with an `azure-validate` what-if before deployment.

## Chosen approach

The workspace runs a **0.1 GB/day cap resetting at 03:00Z** that the estate exhausts within hours, so most of each UK working day is blind and the two existing alert rules cannot fire in that window (`docs/operations.md:363-369`, `docs/current-architecture.md:160-177`, PLAT-034). `docs/operations.md:369` records that raising the quota is a billing decision left with the operator. Proposal §18.2 wants blocked-obsolete-client visibility, which is only useful if something can alert on it. The plan is explicit that both changes are **conditional**: the decision is recorded with evidence, and any Azure write needs exact-target approval. Operator-visible consequence, if skipped: a forced-update rollout blocks every client and nobody is told. Siblings: [[DSK-10-14]] (the measurement), `DSK-11-09` (the plan-11 mirror of the cap decision).

## Governing docs

- No canonical PRD/FRD/ADR is linked yet. Keep `docs_todo: true`; planned desktop governing documents must not be linked until they exist on `origin/dev`.
- Use the ticket's Source of truth and its area plan until a real governing doc can be linked.

## Routing

- **Subagent**: `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (read-only sandbox)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `azure-diagnostics` (azure-skills `1a03acfb`) → `azure-cost` (same pin) for the per-GB price → `azure-validate` (same pin) **only** when a write has been approved, and only for the what-if
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** `monitor`, `applicationinsights`, `group_resource_list`, `pricing`. Do not call any Azure write tool.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Ordered implementation

1. Orientation. Read the plan row and § 3 of the area plan, `docs/desktop/11-azure-disposition/README.md:186-192` (the conditional-write register), and `docs/runbook.md:776-790`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-16-alerting-quota-followups` from `dev`.
3. Read the current state, read-only: use Azure MCP `monitor` to read the workspace's `workspaceCapping` setting and its current `dailyQuotaGb`, and `group_resource_list` to confirm the two existing alert rules and the action group. Record the exact resource names and the reading date. Do not change anything.
4. Write `docs/desktop/10-security-observability-performance/telemetry-decision.md` with three sections: `## Evidence` (the volume numbers from [[DSK-10-14]], the current cap, the observed exhaustion time of day), `## Options` (leave the cap; raise it to a stated GB/day; reduce volume by sampling or by trimming Worker verbosity), and `## Cost` (per-GB price from `azure-cost`/`pricing` × the projected daily volume × 30, stated as a monthly figure).
5. Include the "reduce volume first" option seriously: `docs/current-architecture.md:160-177` records that the Worker produces most of the volume and adaptive sampling is already on. Quantify what trimming Worker verbosity or tightening sampling would save before proposing a bill increase (C-01).
6. Draft the third alert rule **as a proposal only**: a scheduled query rule over the `DesktopClientBlocked` custom event from [[DSK-10-14]], firing when blocked-client count exceeds a stated threshold in a stated window, targeting the existing `${prefix}-operations` action group. Copy the shape of `infra/modules/platform.bicep:617-689`; state the KQL, the threshold, the window and the severity in the decision file. Note explicitly that an alert on a query cannot fire inside the capped window — so the rule's value depends on the cap decision.
7. **Operator step** — present the decision file and obtain an explicit decision for each item: (a) cap — leave / raise to N GB per day / reduce volume instead; (b) alert rule — add / do not add. Approval must name the exact target resource and the operation (`docs/runbook.md` § Live-operation approval matrix). Record the decision text, the approver and the date in the file. **If either is not approved, stop here**: the recorded decision with its evidence is the complete deliverable for that item.
8. If — and only if — the alert rule is approved: add the resource to `infra/modules/platform.bicep` after `:689`, following the existing scheduled-query-rule shape and its `if (...)` activation guard so it cannot be created unintentionally. Keep the action group reference; do not create a second action group.
9. Validate locally first: run `pwsh ./scripts/Test-AzureDeploymentPlan.ps1 -Mode Local` (this is what CI's `infrastructure` job runs, `.github/workflows/ci.yml:126-130`) and expect it to compile the Bicep and pass the committed fail-closed invariants.
10. Load `azure-validate` and run a what-if against the exact target resource group, read-only. Attach the what-if output to the ticket. Expect exactly one resource added and nothing else changed — any other delta means the template drifted and the deployment must not proceed.
11. If — and only if — the cap change is approved: it is a workspace setting change, executed by the operator on the exact named workspace with the approved value. Record the before value, the after value, the date and the approver. Provide the rollback in the same record (restore the previous `dailyQuotaGb`), as `docs/desktop/11-azure-disposition/README.md:189` requires.
12. Mirror the outcome into `docs/desktop/11-azure-disposition/README.md` — update the conditional-write register rows `:189-190` with the decision, its date and the evidence link — and into `docs/operations.md` (the alert and quota state) and `docs/current-architecture.md` (PLAT-034/PLAT-036 status).
13. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `pwsh ./scripts/Test-AzureDeploymentPlan.ps1 -Mode Local` — expected: exit 0 (Bicep compiles, invariants hold).
- [ ] `azure-validate` what-if output attached — expected: exactly one `Microsoft.Insights/scheduledQueryRules` resource added, no other change.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exit 0.

## Risks and constraints

- ⚠ **Azure write** (conditional, two exact targets): (1) the Log Analytics workspace `pegasus-prod-logs-<suffix>` in `rg-pegasus-prod` — `workspaceCapping` daily quota change; (2) a new `Microsoft.Insights/scheduledQueryRules` resource in `rg-pegasus-prod` deployed from `infra/modules/platform.bicep`. Both need exact-target approval per `docs/runbook.md` § Live-operation approval matrix ("Change or use an Azure service (write/mutation/cost)": explicit approval for the exact target, fresh inventory, least-privilege identity) and are mirrored in `docs/desktop/11-azure-disposition/README.md` § conditional writes. Without approval this ticket ships a recorded decision and nothing else. This agent's own Azure MCP use stays read-only in every case.
- **Scope boundary**: may touch `infra/modules/platform.bicep` (only the new alert resource), and documentation under `docs/`. Must not touch application code, must not change the two existing alert rules, must not create a second action group, must not deprovision anything (nothing is removed before cutover, observed use and rollback approval). Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: "alert/quota changes made casually" is the recorded risk — approval and what-if are the controls; an alert built on a query cannot fire inside the capped window, so approving the rule without the cap decision buys little; raising the cap is a recurring bill under C-01; a what-if showing more than the intended delta means the deployed estate has drifted from the template and deployment must stop.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Simplification pass

Before the PR, independently assess the branch diff for unnecessary abstractions, duplicated policy, or scope expansion and record the disposition here.
