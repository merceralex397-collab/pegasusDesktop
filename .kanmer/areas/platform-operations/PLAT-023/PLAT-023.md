---
id: PLAT-023
type: ticket
title: 'DSK-11-05 · Resource-health, advisor and compliance read of the estate'
status: backlog
area: platform-operations
assignee: ''
profile: chore
labels:
  - desktop-conversion
  - plan-11
  - phase-0
  - tier-9
  - needs-operator
groups:
  - EPIC-012
  - HZN-001
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:30:10.934Z'
updated: '2026-08-24T08:30:10.934Z'
---

## What

Run a read-only health, advisor and compliance pass over `rg-pegasus-prod` — Azure MCP `resourcehealth` and `advisor` plus an `azure-compliance` (azqr) review — and record every finding against its register row, with the explicit rule that **no finding is acted on in this ticket**: each one either becomes its own Kanmer ticket or is recorded as accepted with a reason.

## Why

Proposal §19 requires the conversion to know what exists and what state it is in before anything moves, and §27 item 16 forbids removing a resource before dependency, backup and rollback verification. The estate has known posture facts the conversion must not rediscover by accident: Entra-only SQL auth with a public endpoint and an `AllowAzureServices` firewall rule (`infra/modules/platform.bicep:195`, `:223`), shared-key access disabled on the transport account (`:100`), a Key Vault with RBAC, 90-day soft delete and purge protection (`:85`), and an App Insights instance with local auth disabled (`:56`). Operator-visible consequence: an unrecorded health or compliance finding surfaces during the pilot as an unexplained failure, at exactly the moment the App Insights daily cap (PLAT-034) hides the trace.

Siblings: [[DSK-11-01]] supplies the register rows findings attach to; [[DSK-10-01]] owns the threat → control → test register that a security finding should join; [[DSK-11-04]] runs the cost half of the advisor output.

## Source of truth

- Plan row: `docs/desktop/11-azure-disposition/README.md` § 5 — `DSK-11-05`
- Plan detail: `docs/desktop/11-azure-disposition/README.md` § 6 Routing table (`azure-compliance`, `azure-diagnostics` are read-only here; `azure-validate` only when a write is approved) and § 7 Risks and traps
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 19 Azure service disposition; § 18.3 Health
- Repository evidence:
  - `infra/modules/platform.bicep:46` Log Analytics, `:56` App Insights (`DisableLocalAuth: true`), `:85` Key Vault (RBAC, soft delete, purge protection), `:100` transport storage (shared key disabled), `:154` custody storage, `:195`/`:223` SQL server and the `AllowAzureServices` firewall rule, `:354` Web container app with `/health/*` probes, `:489` Worker Function App, `:576`/`:617` the two Sev1 alert rules
  - `infra/modules/platform.bicep:35-36` the fail-closed activation gates — a resource absent because its gate is closed is not a health finding
  - `docs/current-architecture.md:160-175` the capped telemetry window (PLAT-034): alerts cannot fire on the capped part of the day
  - `docs/operations.md:829` § Azure activation remains fail-closed
  - `docs/desktop/01-inventory-and-parity/azure-resource-register.md` — the rows findings attach to
- Binding decisions:
  - **L-02** — no Azure dev/test/staging exists (ADR-0014), so a finding recommending a staging environment is out of bounds; `docs/operations.md:910-918` lists separate staging/QA/UAT/demo as a permanent "Not planned" boundary.
  - **C-01** — private repositories: a finding recommending public exposure of anything is rejected on the constraint, not debated.
  - Proposal § 19.1 — a finding that recommends adding API Management, SignalR, Service Bus, Event Grid, Redis, AKS, a new identity tenant, a new database or a new document store fails the cloud-justification test by default and is recorded as rejected with that reason.
- Depends on: `DSK-11-01` — findings are recorded per verified register row.

## Routing

- **Subagent**: `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (read-only sandbox)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `azure-compliance` (`microsoft/azure-skills` `1a03acfb`, **read-only azqr review only**) → `azure-diagnostics` (same pin, for resource-health and AppLens reads) → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** (`resourcehealth`, `advisor`, `group_resource_list`, `storage`, `keyvault`, `sql`, `containerapps`, `functionapp`, `monitor`, `applicationinsights`, `role`); Microsoft Learn (`microsoft_docs_search`) to confirm what a finding actually means before recording it
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`; gates are `plan` + `questions-resolved` to leave `preparing`, `proof` + `questions-resolved` to enter `done`. Call `get_doc_gates <this ticket id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read `docs/desktop/11-azure-disposition/README.md` § 6 and § 7, `docs/operations.md` § Azure activation remains fail-closed (`:829`), and `docs/runbook.md` § Live-operation approval matrix. Then `get_doc_gates <this ticket id>` and `take_ticket <this ticket id>`.
2. Load `pegasus-desktop`, then `azure-compliance` and `azure-diagnostics`. Both have remediation paths — **use the assessment half only**. Never load `azure-deploy`, `azure-prepare`, `azure-app-onboard` or `azure-enterprise-infra-planner` (do-not-load table, `docs/desktop/12-agent-tooling/skill-routing.md`), and do not run `azure-validate` at all here: it belongs to an approved write.
3. Resource health: Azure MCP `resourcehealth` for every resource id returned by `group_resource_list` on `rg-pegasus-prod`. Record availability state and any recent health event per resource. Attach the raw output with `append_scratch`.
4. Advisor: Azure MCP `advisor` for the subscription, filtered to `rg-pegasus-prod`. Split the recommendations into the four Advisor categories and hand the cost ones to [[DSK-11-04]] rather than duplicating them here.
5. Compliance: run the `azure-compliance` skill's read-only azqr review over `rg-pegasus-prod`. Follow the skill for how to invoke the CLI; if it must be installed, install it in the agent's own environment only — it inspects Azure and writes nothing.
6. Expiry checks that azqr does not cover: list Key Vault **secret names and expiry dates only** (`keyvault`, never a secret value) and record anything expiring within 90 days as a finding, since the Box, DVLA, DVSA and Automation MCP secrets at `platform.bicep:382-398` and `:555-563` are what the gateway needs during the conversion.
7. Classify every finding into exactly one of three dispositions and write the disposition next to it: **ticket** (a new Kanmer ticket with the area it belongs to), **accepted** (with the reason and the decision or boundary that accepts it), or **out of bounds** (contradicts L-02, C-01 or proposal §19.1 — name which). Nothing is left unclassified.
8. Suppress the false positives the estate's design creates, with the reason recorded: resources absent because `webActivationApproved` or `workerActivation` gates are closed (`platform.bicep:35-36`) are not health findings; "no staging environment" and "no zone redundancy / multi-region" are permanent `Not planned` boundaries (`docs/operations.md:910-918`); "no private networking" likewise.
9. Record the one finding that is already known and must not be re-litigated as new: the Log Analytics daily cap means the two Sev1 alert rules cannot fire for most of each working day (PLAT-034). Cross-reference [[DSK-11-09]] and [[DSK-10-16]] rather than proposing a cap change here.
10. Write the findings table into `docs/desktop/11-azure-disposition/README.md` (or, if it is long, a new `docs/desktop/11-azure-disposition/estate-health-findings.md` linked from § 4 — decide and record which, as the plan does not say) with columns: Finding · Source (resourcehealth / advisor / azqr / keyvault expiry) · Register row · Severity · Disposition · Ticket id or reason · Date read.
11. Create the follow-up tickets with Kanmer for every "ticket" disposition, in the area that owns the resource, and record their ids in the table. Do not fix anything in this ticket.
12. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`, do the simplification pass (`AGENTS.md` step 4), write `proof` as a `command-log`, and hand to `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] Every resource in `rg-pegasus-prod` has a recorded resource-health state with the date it was read.
- [ ] Advisor recommendations are recorded, with the cost ones referred to [[DSK-11-04]] rather than duplicated.
- [ ] An azqr read-only review has been run and its findings recorded.
- [ ] Key Vault secret **names and expiry dates** are recorded; no secret value appears anywhere in the ticket or the tree.
- [ ] Every finding carries one of three dispositions — ticket / accepted / out of bounds — with a ticket id or a reason; none is acted on in this ticket.
- [ ] Findings that are artefacts of the fail-closed activation gates or of the permanent `Not planned` boundaries are marked as such and not raised as defects.

## Verification

- [ ] Azure MCP `resourcehealth` re-run by the reviewer on three register rows — expected: the same availability states as recorded, or a dated change.
- [ ] `grep -rn "Disposition" docs/desktop/11-azure-disposition/` — expected: a findings table where every row has one of `ticket` / `accepted` / `out of bounds`.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exits 0.
- [ ] `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected: exits 0.
- [ ] `git diff --stat` — expected: only files under `docs/desktop/11-azure-disposition/`; nothing under `infra/`, `src/` or `scripts/`.

## Evidence tier

Tier 9 — Security/observability. Every finding must name the read-only command that produced it, no secret value may be captured, and the known observability gap (PLAT-034) must be stated as the reason some questions cannot be answered from telemetry at all.

## Documentation changes

- `docs/desktop/11-azure-disposition/README.md` § 4 or a new `docs/desktop/11-azure-disposition/estate-health-findings.md` — the findings table with dispositions and read dates.
- New Kanmer tickets for every "ticket" disposition, in the area that owns the resource — not repository documents.

## Guardrails

- **Azure**: no write. Health, advisor, azqr and Key Vault name/expiry reads are permitted with no per-target approval (`docs/runbook.md` § Live-operation approval matrix). Acting on any finding — a firewall rule, a role assignment, a TLS or retention setting — is a marked ⚠ Azure write needing exact-target approval and a row in `docs/desktop/11-azure-disposition/README.md` § Conditional Azure writes. **Nothing is deprovisioned before cutover, observed non-use and rollback approval**; "advisor says it is idle" is not evidence of non-use.
- **Scope boundary**: documentation under `docs/desktop/11-azure-disposition/` plus new Kanmer tickets. Do not edit `infra/`, `src/`, `scripts/` or `.github/workflows/`.
- **Traps** (plan § 7): a write without approval is the disqualifying failure of this area; telemetry blind spots (PLAT-034) mean the alert rules cannot fire for most of the day, so absence of an alert proves nothing; out-of-band configuration is invisible to `azd provision` — the Log Analytics daily cap is exactly such a setting.
- **Board placement**: this plan area seeds into `platform-operations` because the board-shape table in `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape assigns no area to plan 11.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document (`n/a — docs-only` for documentation-only tickets).

## Outcome

_Filled at closeout._
