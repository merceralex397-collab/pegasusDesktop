# Plan — PLAT-019

## Objective

Turn `docs/desktop/01-inventory-and-parity/azure-resource-register.md` from a Bicep-derived draft into a verified register: every resource declared in `infra/main.bicep` and `infra/modules/platform.bicep`, and every resource actually present in `rg-pegasus-prod`, listed with owner/use, proposal §19 target position, deprovision-candidate answer and the exact read-only command that proved it — with every Bicep-versus-live difference recorded as drift.

## Chosen approach

Proposal §19 opens: "No Azure service is deprovisioned during conversion. The first action is to inventory and tag every current resource and identify which code path uses it." §27 item 15 makes "runtime Azure dependencies match the approved cloud-boundary register" an acceptance criterion of the whole conversion. Without a verified register the question "is this still used?" cannot be answered at cutover, and telemetry cannot answer it either — the Log Analytics workspace runs a 0.1 GB daily quota and the estate exhausts it within hours, so most of each UK working day is blind (`docs/current-architecture.md:160-175`, PLAT-034). Operator-visible consequence: an unverified register is the difference between a safe Phase 10 and deleting a resource the Worker still needs.

Siblings: [[DSK-01-08]] runs the same read-only verification from the area-01 side and owns the register file; [[DSK-11-02]] builds the capability records on top of this register; [[DSK-11-04]] prices it; [[DSK-11-05]] reads its health; [[DSK-11-08]] draws its deprovision-candidate list from it.

## Governing docs

- No canonical PRD/FRD/ADR is linked yet. Keep `docs_todo: true`; do not link a planned decision until it exists on `origin/dev`.
- Use the ticket Source of truth and governing area plan until a real reference can be added.

## Routing

- **Subagent**: `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (read-only sandbox; it refuses every Azure write by design and returns approval text instead)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `azure-resource-lookup` (`microsoft/azure-skills` `1a03acfb`) → `azure-resource-visualizer` (same pin, only if a resource-group diagram is asked for) → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** (`subscription_list`, `group_resource_list`, `storage`, `keyvault`, `sql`, `containerapps`, `functionapp`, `monitor`, `applicationinsights`, `acr`, `role`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. The `chore` gates are `plan` + `questions-resolved` to leave `preparing` and `proof` + `questions-resolved` to enter `done`; call `get_doc_gates <this ticket id>` before every move and cross at most one gated boundary per move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Ordered implementation

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

## Verification

- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exits 0, no broken relative link reported.
- [ ] `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected: exits 0 (the register stays under `docs/desktop/`).
- [ ] `git diff --stat` on the branch — expected: only `docs/desktop/01-inventory-and-parity/azure-resource-register.md` (and, if the plan needed a correction, `docs/desktop/11-azure-disposition/README.md`) changed; no file under `src/`, `infra/`, `scripts/` or `.github/`.
- [ ] Azure MCP `group_resource_list` for `rg-pegasus-prod` re-run by the reviewer — expected: the same resource-id set as the attached evidence.

## Risks and constraints

- **Azure**: no write. Reads are free and need no per-target approval (`docs/runbook.md` § Live-operation approval matrix, row "Read Azure state"). Every write — including applying a tag — is a marked ⚠ Azure write requiring explicit approval for the exact target, and is mirrored in `docs/desktop/11-azure-disposition/README.md` § Conditional Azure writes. **Nothing is deprovisioned before cutover, observed non-use and rollback approval.**
- **Scope boundary**: this ticket may edit `docs/desktop/01-inventory-and-parity/azure-resource-register.md` and, if contradicted, `docs/desktop/11-azure-disposition/README.md`. It must not touch `infra/`, `src/`, `scripts/`, `.github/workflows/` or `docs/operations.md` (the cost note belongs to [[DSK-11-04]]).
- **Traps** (plan § 7): a write without approval is the single disqualifying failure of this area; out-of-band resources are invisible to `azd provision` and must be flagged, not adopted; telemetry blind spots (PLAT-034) mean App Insights cannot prove non-use — use gateway logs, action history and diagnostics bundles; stale current-state docs (`docs/operations.md:295` says "release 14" while its own release table is current) must be refreshed in the same task as any write.
- **Board placement**: this plan area seeds into `platform-operations` because the board-shape table in `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape assigns no area to plan 11; the seeding contract routes plans 10 and 11 to `platform-operations` (prefix `PLAT`).
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document (`n/a — docs-only` for documentation-only tickets).

## Simplification pass

Before the PR, independently assess the branch diff for unnecessary abstractions, duplicated policy, and scope expansion; record findings and dispositions here.
