---
id: PLAT-019
type: ticket
title: DSK-11-01 · Populate the Azure resource register by read-only inventory
status: preparing
area: platform-operations
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:21:16.134Z'
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
blocks:
  - PLAT-020
  - PLAT-022
  - PLAT-023
  - PLAT-025
docs_todo: true
archived: false
created: '2026-08-24T08:26:57.600Z'
updated: '2026-08-24T21:21:16.134Z'
---

## What

Turn `docs/desktop/01-inventory-and-parity/azure-resource-register.md` from a Bicep-derived draft into a verified register: every resource declared in `infra/main.bicep` and `infra/modules/platform.bicep`, and every resource actually present in `rg-pegasus-prod`, listed with owner/use, proposal §19 target position, deprovision-candidate answer and the exact read-only command that proved it — with every Bicep-versus-live difference recorded as drift.

## Why

Proposal §19 opens: "No Azure service is deprovisioned during conversion. The first action is to inventory and tag every current resource and identify which code path uses it." §27 item 15 makes "runtime Azure dependencies match the approved cloud-boundary register" an acceptance criterion of the whole conversion. Without a verified register the question "is this still used?" cannot be answered at cutover, and telemetry cannot answer it either — the Log Analytics workspace runs a 0.1 GB daily quota and the estate exhausts it within hours, so most of each UK working day is blind (`docs/current-architecture.md:160-175`, PLAT-034). Operator-visible consequence: an unverified register is the difference between a safe Phase 10 and deleting a resource the Worker still needs.

Siblings: [[DSK-01-08]] runs the same read-only verification from the area-01 side and owns the register file; [[DSK-11-02]] builds the capability records on top of this register; [[DSK-11-04]] prices it; [[DSK-11-05]] reads its health; [[DSK-11-08]] draws its deprovision-candidate list from it.

## Source of truth

- Plan row: `docs/desktop/11-azure-disposition/README.md` § 5 — `DSK-11-01`
- Plan detail: `docs/desktop/11-azure-disposition/README.md` § 2 Evidence base (the 16-row estate table with Bicep line numbers) and § 5 "Resource disposition register"
- Register file: `docs/desktop/01-inventory-and-parity/azure-resource-register.md` — the table this ticket fills, and its § "Read-only verification procedure (DSK-01-08)"
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 19 Azure service disposition (target-position vocabulary) and § 19.1 "Do not add by default"
- Repository evidence:
  - `infra/main.bicep:32` region `uksouth`, `:71` resource group `rg-pegasus-prod`, `:72-77` common tags, `:79` group resource, `:114` budget `pegasus-prod-monthly`
  - `infra/modules/platform.bicep:46` Log Analytics, `:56` Application Insights, `:68` action group, `:85` Key Vault, `:100` transport storage (`:123` `app-package`, `:134-149` four queues), `:154` custody storage (`:177` `transient-intake`, `:183` `authentication-ring`, `:189` `box-links`), `:195`/`:214`/`:223` SQL server, database and firewall rule, `:229` ACR, `:241`/`:252` Container Apps environment and diagnostics, `:264`/`:270` user-assigned identities, `:276-352` the ten role assignments, `:354` Web container app, `:480`/`:489` Worker plan and Function App, `:576`/`:617` the two alert rules
  - `src/Pegasus.Web/Program.cs:130-176` — required production settings, i.e. which code path needs which resource
  - `docs/operations.md:280` § Production environment; `docs/current-architecture.md:160-175` the capped telemetry window
  - `.azure/deployment-plan.md:24-27` subscription `e6076573-23a5-46a8-acef-7e22d264e5db`, tenant `858cf5b3-aa0a-47a6-9b40-4851fd0afa94`
- Binding decisions:
  - **L-02** — Test/UAT is a local production-mimicking stack; ADR-0014 stands, so there is no Azure dev/test/staging estate to inventory.
  - **C-01** — the repositories become private when the conversion completes; no register row may assume anonymous public hosting.
  - **D-002** — production signing uses a self-managed certificate held in-house, so the register gains no signing service and Key Vault keeps holding secrets only.
  - **D-003** — the update feed is an in-house UNC share, so no Azure resource hosts the feed; record it as a non-Azure dependency, not a register row.
- Depends on: `DSK-01-08` — it performs the read-only Azure verification and owns the register file; this ticket completes the register from its outputs. (The plan row cites `DSK-01-05`, which is the §13.5/§13.9/§13.10 parity-row spike and touches no Azure — treat that citation as an off-by-one and use `DSK-01-08`.)

## Routing

- **Subagent**: `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (read-only sandbox; it refuses every Azure write by design and returns approval text instead)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `azure-resource-lookup` (`microsoft/azure-skills` `1a03acfb`) → `azure-resource-visualizer` (same pin, only if a resource-group diagram is asked for) → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** (`subscription_list`, `group_resource_list`, `storage`, `keyvault`, `sql`, `containerapps`, `functionapp`, `monitor`, `applicationinsights`, `acr`, `role`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. The `chore` gates are `plan` + `questions-resolved` to leave `preparing` and `proof` + `questions-resolved` to enter `done`; call `get_doc_gates <this ticket id>` before every move and cross at most one gated boundary per move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read `docs/desktop/11-azure-disposition/README.md` § 2 and § 5, `docs/desktop/01-inventory-and-parity/azure-resource-register.md` in full, and `docs/runbook.md` § Live-operation approval matrix. Then run Kanmer `get_doc_gates <this ticket id>` and `take_ticket <this ticket id>`, and work in the ticket's own worktree and branch (`AGENTS.md` § Repository task workflow steps 1–2).
2. Load the skills in the Routing order. **Do not load** `azure-deploy`, `azure-prepare`, `azure-app-onboard` or `azure-enterprise-infra-planner` — they are on the "Not applicable — do not load" table in `docs/desktop/12-agent-tooling/skill-routing.md`.
3. Re-derive the declared estate from source, not from memory: `grep -n "^resource \|^module " infra/modules/platform.bicep` and `grep -n "^resource \|^module " infra/main.bicep`. Expect the 41 `resource` declarations of `platform.bicep` (Log Analytics at `:46` through the scheduled-query alert at `:617`) plus the resource group at `main.bicep:79` and the budget at `:114`. Every declared resource must end up with a register row.
4. Read the live estate. Azure MCP `subscription_list` to confirm you are on `e6076573-23a5-46a8-acef-7e22d264e5db`, then `group_resource_list` for `rg-pegasus-prod`. Save the raw JSON to the ticket with `append_scratch` — it is the evidence for the proof document. Done when the returned resource ids are captured verbatim.
5. Read each resource by type, list/show only: `storage` (both accounts, their containers and queues), `keyvault` (**secret names only, never values**), `sql` (server, database, firewall rules), `containerapps` (app and environment: revision, image digest, env-var *names*), `functionapp` (settings names including the nine `AzureWebJobs.<fn>.Disabled` gates), `monitor`, `applicationinsights`, `acr`, `role` (assignments scoped to the group). Record which command produced each fact in the register's last column.
6. Record the two facts that later decisions hang on: `allowBlobPublicAccess` on `pegtrans*` and `pegcustody*`, and the Log Analytics `workspaceCapping.dataIngestionStatus` plus `dailyQuotaGb`. **The 0.1 GB/day cap is not declared anywhere in `infra/modules/platform.bicep`** (`grep -n "workspaceCapping\|dailyQuotaGb" infra/modules/platform.bicep` returns nothing) — so it was set out of band and must be entered as a drift row, not as a Bicep-declared property. This feeds [[DSK-11-09]].
7. Fill the register table for every row: Resource · Type · Declared at (`file:line`) · Used by (code path) · Proposal §19 target position · Desktop-conversion impact · Deprovision candidate? · Read-only verification command. Use only the §19 vocabulary listed in the register file: *Retain*, *Retain, simplified*, *Consolidate into gateway*, *Retain or repurpose*, *Reassess after stabilization*, *Deprovision candidate*.
8. Diff Bicep against live. Anything present in Azure but not declared in `infra/` is a drift row ("present, undeclared") and anything declared but absent is a drift row ("declared, absent") — record both, remove nothing, create nothing. The Web container app and the 5xx alert are conditional on `webActivationApproved` (`platform.bicep:35`, `:354`, `:576`) and the Worker functions on `workerActivation == 'approved-live-worker'` (`:36`): an absence there is a gate state, not drift.
9. Confirm the "Declared absent" list in the register still holds (Front Door/CDN, SignalR, Service Bus/Event Hubs/Event Grid, Redis, API Management, slots/S1/multi-region/private networking, any dev/test/UAT/staging environment, Document Intelligence, Key Vault certificates, any feed resource). Any of these appearing live is drift and a §19.1 violation to report, not to fix in this ticket.
10. Update the "Used by" column with real code citations where the pre-filled value was approximate, checking against `src/Pegasus.Web/Program.cs:130-176` and the Worker composition root. Keep the *intended* tags recorded and **unapplied** — applying a tag is an Azure write and belongs to the writes catalogue in [[DSK-11-03]].
11. Run the documentation gates and record the output: `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`. Both must exit 0; the file stays under `docs/desktop/`, which is an allowed markdown root.
12. Perform the simplification pass over the branch diff (`AGENTS.md` step 4) — record `n/a — docs-only` if the diff is documentation only — then write the `proof` document as a `command-log` listing every read-only command run and hand the branch to `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] Every resource declared in `infra/main.bicep` and `infra/modules/platform.bicep` has exactly one register row with owner/use, §19 target position and a read-only verification command.
- [ ] Every resource returned by `group_resource_list` for `rg-pegasus-prod` maps to a register row or to an explicit drift row.
- [ ] Differences between the Bicep declaration and the live estate are recorded as drift, including the out-of-band Log Analytics daily cap.
- [ ] `allowBlobPublicAccess` for both storage accounts and `workspaceCapping.dataIngestionStatus` are recorded with the command that produced them.
- [ ] Every row's "Deprovision candidate?" answer is "No" or "Candidate — not before cutover"; no row proposes removal now.
- [ ] Zero Azure writes: no `create`, `update`, `delete`, tag application or role assignment appears in the command log.

## Verification

- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exits 0, no broken relative link reported.
- [ ] `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected: exits 0 (the register stays under `docs/desktop/`).
- [ ] `git diff --stat` on the branch — expected: only `docs/desktop/01-inventory-and-parity/azure-resource-register.md` (and, if the plan needed a correction, `docs/desktop/11-azure-disposition/README.md`) changed; no file under `src/`, `infra/`, `scripts/` or `.github/`.
- [ ] Azure MCP `group_resource_list` for `rg-pegasus-prod` re-run by the reviewer — expected: the same resource-id set as the attached evidence.

## Evidence tier

Tier 9 — Security/observability. The obligation here is that every claim about the estate is backed by a named read-only command whose output is attached, that no secret value is captured, and that the telemetry blind window is recorded rather than assumed away.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/azure-resource-register.md` — every row completed and verified; drift rows added; intended tags left unapplied.
- `docs/desktop/11-azure-disposition/README.md` § 2 Evidence base — only if the live read contradicts the recorded estate table; correct it with the command that proved the contradiction.

## Guardrails

- **Azure**: no write. Reads are free and need no per-target approval (`docs/runbook.md` § Live-operation approval matrix, row "Read Azure state"). Every write — including applying a tag — is a marked ⚠ Azure write requiring explicit approval for the exact target, and is mirrored in `docs/desktop/11-azure-disposition/README.md` § Conditional Azure writes. **Nothing is deprovisioned before cutover, observed non-use and rollback approval.**
- **Scope boundary**: this ticket may edit `docs/desktop/01-inventory-and-parity/azure-resource-register.md` and, if contradicted, `docs/desktop/11-azure-disposition/README.md`. It must not touch `infra/`, `src/`, `scripts/`, `.github/workflows/` or `docs/operations.md` (the cost note belongs to [[DSK-11-04]]).
- **Traps** (plan § 7): a write without approval is the single disqualifying failure of this area; out-of-band resources are invisible to `azd provision` and must be flagged, not adopted; telemetry blind spots (PLAT-034) mean App Insights cannot prove non-use — use gateway logs, action history and diagnostics bundles; stale current-state docs (`docs/operations.md:295` says "release 14" while its own release table is current) must be refreshed in the same task as any write.
- **Board placement**: this plan area seeds into `platform-operations` because the board-shape table in `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape assigns no area to plan 11; the seeding contract routes plans 10 and 11 to `platform-operations` (prefix `PLAT`).
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document (`n/a — docs-only` for documentation-only tickets).

## Outcome

_Filled at closeout._
