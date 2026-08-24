---
id: PLAT-022
type: ticket
title: DSK-11-04 · Cost baseline and forecast for the desktop-era estate
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
created: '2026-08-24T08:30:10.916Z'
updated: '2026-08-24T08:30:10.916Z'
---

## What

Produce a read-only monthly cost baseline for `rg-pegasus-prod` broken down by resource, and a forecast of the conversion's cost delta — which is **nil for distribution** because D-002 chose a self-managed certificate and D-003 an in-house UNC share, leaving only gateway compute and any telemetry decision as movement — then record the result as a short note in `docs/operations.md`.

## Why

The estate runs under one consumption budget of 75 per month with alerts at 50/80/100% and a forecast alert (`infra/main.bicep:114`). Proposal §28 argues the reduced-cloud target on cost as well as design, and §19 requires that any new resource is priced before approval. The plan's assumption "costs stay within the existing budget; any new resource is priced before approval (Azure MCP `pricing`)" is currently unevidenced. Operator-visible consequence: without a baseline, the first budget alert during the pilot cannot be attributed, and the telemetry-quota decision in [[DSK-11-09]] has no cost side.

Siblings: [[DSK-11-01]] supplies the resource list this prices; [[DSK-11-09]] needs the price of raising the Log Analytics daily cap; [[DSK-11-08]] uses cost as one of the three deprovision-candidate signals (traffic, dependencies, cost).

## Source of truth

- Plan row: `docs/desktop/11-azure-disposition/README.md` § 5 — `DSK-11-04`
- Plan detail: `docs/desktop/11-azure-disposition/README.md` § 2 "Assumptions" ("Costs stay within the existing 75/month budget; any new resource is priced before approval") and § 5 "Resource disposition register" (the budget row)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 19 Azure service disposition; § 28 "Where it costs more"
- Repository evidence:
  - `infra/main.bicep:114` budget `pegasus-prod-monthly`, amount 75, monthly, notifications at 50/80/100% plus forecast
  - `infra/modules/platform.bicep:354-478` the Web container app — single revision, min 1 / max 1 replica (no scale-to-zero), cpu 1.0 / 2 GiB raised for in-process Chromium per ADR-0028 (`:436-445`); `:480` the FC1 FlexConsumption Worker plan; `:214` the S0 SQL database; `:229` Basic ACR; `:46` Log Analytics PerGB2018 with 31-day retention
  - `docs/operations.md:280` § Production environment — the compute/data shape the note attaches to
  - `docs/desktop/01-inventory-and-parity/azure-resource-register.md` — the row-by-row list to price
- Binding decisions:
  - **D-002** — self-managed certificate: no signing-service cost line.
  - **D-003** — in-house UNC share: no feed-hosting cost line; the feed host is an existing in-house Windows machine, not an Azure resource.
  - **C-01** — once the repositories are private, GitHub Actions minutes stop being free and Windows runners bill at a 2× multiplier; that is a CI cost, not an Azure cost — name it in the note and point at `docs/desktop/08-testing/README.md` § 7 and ticket DSK-08-19 rather than pricing it here.
  - **L-02** — there is no Azure test environment to price (ADR-0014).
- Depends on: `DSK-11-01` — the verified register is the list of things to price.

## Routing

- **Subagent**: `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (read-only)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `azure-cost` (`microsoft/azure-skills` `1a03acfb`) → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** (`pricing`, `group_resource_list`, `containerapps`, `functionapp`, `sql`, `monitor`, `advisor`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`; gates are `plan` + `questions-resolved` to leave `preparing`, `proof` + `questions-resolved` to enter `done`. Call `get_doc_gates <this ticket id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read `docs/desktop/11-azure-disposition/README.md` § 2 and § 5, `docs/desktop/01-inventory-and-parity/azure-resource-register.md`, and `docs/runbook.md` § Live-operation approval matrix (cost queries are reads and need no approval). Then `get_doc_gates <this ticket id>` and `take_ticket <this ticket id>`.
2. Load `pegasus-desktop` then `azure-cost`. Follow the skill's cost-query section; **do not** follow any remediation step it offers that would resize, delete or reconfigure a resource — this ticket only measures.
3. Pull actual spend: use `azure-cost` to query the last three complete months for subscription `e6076573-23a5-46a8-acef-7e22d264e5db`, grouped by resource, scoped to `rg-pegasus-prod`. Attach the raw output with `append_scratch`. Done when every register row has a spend figure or an explicit "no metered cost".
4. Pull list prices for the fixed-shape items with Azure MCP `pricing` for region `uksouth`: Container Apps Consumption at 1.0 vCPU / 2 GiB always-warm single replica, Azure SQL S0, Basic ACR, Log Analytics PerGB2018 ingestion and 31-day retention, two Standard_LRS storage accounts, FC1 FlexConsumption. Record price, unit and the date of the quote — list prices move.
5. Reconcile: actual spend versus list price per resource. Where they differ materially, say why (free grants, always-warm replica hours, ingestion suppressed by the 0.1 GB daily cap). The capped workspace makes telemetry look cheap; state that explicitly so nobody reads the baseline as "telemetry is nearly free".
6. Build the forecast table with three columns — today, desktop-era, delta — and exactly these movements:
   - **Distribution: nil.** D-002 (self-managed certificate) and D-003 (in-house UNC share) mean the whole sign-and-publish path touches no Azure resource.
   - **Gateway compute**: the `/api/v1` route group runs in the same Container App (L-01), so the delta is request volume on an already always-warm replica; after cutover the Razor Pages and Playwright base image can leave the image, which is a possible *reduction* — record it as "candidate reduction, not before cutover".
   - **Telemetry**: the only plausible increase, and only if [[DSK-11-09]] recommends raising the Log Analytics daily cap. Price the raise per GB/day so that ticket has a number.
7. Compare the total against the 75 monthly budget at `infra/main.bicep:114` and state the headroom. If the forecast exceeds the budget, do **not** change the budget — record it as a decision needed and raise it in the ticket's open questions (a budget change is an Azure write and belongs in [[DSK-11-03]]'s catalogue).
8. Run Azure MCP `advisor` for cost recommendations and record them read-only. Do not act on any of them; each one that looks worthwhile becomes its own ticket (same rule as [[DSK-11-05]]).
9. Write the note into `docs/operations.md` § Production environment (starting `docs/operations.md:280`) — a short dated paragraph with the baseline total, the three-line forecast and a link to this ticket. Keep it to current state; the working out lives in the ticket, not the tree.
10. Add the CI-cost sentence required by C-01: private-repository Windows runners bill at 2× and the desktop packaging and UI lanes are still to be added — pointing to `docs/desktop/08-testing/README.md` § 7 and DSK-08-19, without pricing it here.
11. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`; both must exit 0.
12. Simplification pass (`AGENTS.md` step 4), then `proof` as a `command-log` with the cost and pricing outputs, and review by `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] Every register row has a current monthly cost figure or an explicit "no metered cost", each traceable to an attached read-only query.
- [ ] A dated list-price quote exists for Container Apps, SQL S0, ACR Basic, Log Analytics, both storage accounts and the FC1 plan in `uksouth`.
- [ ] The forecast records distribution as nil (D-002, D-003), gateway compute as request-volume-only with a candidate post-cutover reduction, and telemetry as the only plausible increase with a per-GB/day price.
- [ ] Headroom against the 75 monthly budget is stated; no budget change is made.
- [ ] Advisor cost recommendations are recorded and none is acted on.
- [ ] `docs/operations.md` § Production environment carries the dated baseline note including the C-01 CI-cost sentence.

## Verification

- [ ] Azure MCP `pricing` for `uksouth` re-run by the reviewer on one line item — expected: the same unit price as the recorded quote, or a dated explanation of the drift.
- [ ] `grep -n "cost baseline" docs/operations.md` — expected: the dated note is present in § Production environment.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exits 0.
- [ ] `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected: exits 0.
- [ ] `git diff --stat` — expected: `docs/operations.md` and, if the assumption needed correcting, `docs/desktop/11-azure-disposition/README.md`; nothing under `infra/` or `src/`.

## Evidence tier

Tier 9 — Security/observability. Every figure must be produced by a named read-only query whose output is attached; nothing is estimated from memory, and the effect of the telemetry cap on the apparent cost is stated rather than left to be misread.

## Documentation changes

- `docs/operations.md` § Production environment — dated cost baseline and forecast note, plus the C-01 CI-cost sentence.
- `docs/desktop/11-azure-disposition/README.md` § 2 Assumptions — replace the unevidenced cost assumption with the measured figure and a link to this ticket.

## Guardrails

- **Azure**: no write. Cost and pricing queries are reads and are permitted with no per-target approval (`docs/runbook.md` § Live-operation approval matrix). Changing the budget, resizing a resource or acting on an advisor recommendation is a marked ⚠ Azure write needing exact-target approval and a row in `docs/desktop/11-azure-disposition/README.md` § Conditional Azure writes. **Nothing is deprovisioned before cutover, observed non-use and rollback approval** — a resource being expensive is not a reason to remove it.
- **Scope boundary**: `docs/operations.md` and `docs/desktop/11-azure-disposition/README.md` only. Do not touch `infra/main.bicep` (the budget), `src/`, or the CI workflows.
- **Traps** (plan § 7): the capped workspace (PLAT-034) makes telemetry look cheaper than it will be once the cap is lifted — say so; stale current-state docs must be refreshed in the same task, and `docs/operations.md:295` still says "release 14" while its own release table is current, so do not copy that number into the note.
- **Board placement**: this plan area seeds into `platform-operations` because the board-shape table in `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape assigns no area to plan 11.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document (`n/a — docs-only` for documentation-only tickets).

## Outcome

_Filled at closeout._
