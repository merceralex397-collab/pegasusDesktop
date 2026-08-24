---
id: TOOL-006
type: ticket
title: DSK-12-06 · Wire the Azure MCP server for the read-only auditor agent
status: backlog
area: agent-tooling
assignee: ''
profile: chore
labels:
  - desktop-conversion
  - plan-12
  - phase-0
  - tier-1
  - needs-operator
groups:
  - EPIC-013
  - HZN-001
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:07:44.168Z'
updated: '2026-08-24T08:07:44.168Z'
---

## What

Add the azure-skills MCP server entry to `.codex/config.toml` — disabled first, then enabled once the command is verified on the workstation — and prove `pegasus-azure-auditor` can list `rg-pegasus-prod` read-only without any write tool being available or used.

## Why

Plan area 11 and every Azure question in the conversion (`docs/desktop/11-azure-disposition/README.md` § 5, rows DSK-11-01 through DSK-11-09) route to `pegasus-azure-auditor` with Azure MCP read tools. That agent exists as `.codex/agents/pegasus-azure-auditor.toml` but has no Azure MCP server wired, so today it cannot answer a single register question and the Azure inventory work is blocked. `docs/desktop/12-agent-tooling/README.md` § 2 records the working command as an **assumption** — "the Azure MCP server command in azure-skills `.mcp.json` works unchanged on the Windows workstations (verified in DSK-12-06 before enabling)" — which is exactly what this ticket converts to a fact.

## Source of truth

- Plan row: `docs/desktop/12-agent-tooling/README.md` § 5 — `DSK-12-06`
- Plan detail: `docs/desktop/12-agent-tooling/subagents.md` § `.codex/config.toml` additions — the commented placeholder and the instruction to "copy the server entry from microsoft/azure-skills `.mcp.json` at the pinned commit `1a03acfb` and keep it disabled until DSK-12-06 records the command"
- Plan detail: `docs/desktop/12-agent-tooling/skill-routing.md` § Work type routing, row "Azure inventory / cost / health (read-only)" — the allowed tool list; and § Not applicable — do not load
- Plan detail: `docs/desktop/12-agent-tooling/README.md` § 7 ("The Azure MCP entry, if enabled, must stay limited to read tools by instruction; there is no per-tool permission in the TOML")
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 19 Azure service disposition, § 20.4 Skill routing by work type
- Repository evidence:
  - `.codex/agents/pegasus-azure-auditor.toml` — `sandbox_mode = "read-only"`, `model_reasoning_effort = "medium"`; its `developer_instructions` name the allowed read tools and the target: subscription `e6076573-23a5-46a8-acef-7e22d264e5db`, tenant `858cf5b3-aa0a-47a6-9b40-4851fd0afa94`, resource group `rg-pegasus-prod` (uksouth)
  - `docs/operations.md:287-289` — the same production target, independently recorded
  - `.codex/config.toml:1-13` — two MCP servers today (`mcp_microsoftdocs`, `kanmer`); no `azure` entry
  - `docs/runbook.md:776` — `## Live-operation approval matrix`, the exact-target approval rule every write is bound by
  - `docs/desktop/11-azure-disposition/README.md:345` — the read-only Azure MCP tool list this ticket must match: `group_resource_list`, `storage`, `keyvault`, `monitor`, `applicationinsights`, `sql`, `containerapps`, `functionapp`, `pricing`, `advisor`, `resourcehealth`, `role`, `subscription_list`
- Binding decisions:
  - **Azure rule** — reads are free; every write is ⚠, needs exact-target approval (`docs/runbook.md` § Live-operation approval matrix) and is mirrored in plan 11. Nothing is deprovisioned before cutover, observed use and rollback approval.
  - **L-02** — ADR-0014 stands: there is no Azure dev/test/staging. This ticket reads production and creates nothing.
  - **L-04** — every ticket names its subagent, skills and MCP tools.
- Depends on: `DSK-12-01` — the recorded Codex build and its honoured config fields decide whether an MCP entry can be scoped per agent or only per session.

## Routing

- **Subagent**: `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (strictly read-only; it refuses writes by design and returns approval text instead)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `azure-resource-lookup` (`.agents/skills/vendor/azure/azure-resource-lookup/`, from `microsoft/azure-skills` `1a03acfb`) → `kanmer-plan`, `kanmer-execute` (`.grok/skills/<name>/SKILL.md`). Do **not** load `azure-deploy`, `azure-prepare`, `azure-app-onboard`, `azure-app-onboard-prereq`, `azure-cloud-migrate`, `azure-enterprise-infra-planner` or `python-appservice-deploy` — all are on the do-not-load table in `skill-routing.md`; and do not run `azure-validate` in any mode that changes state.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** (`subscription_list`, `group_list`, `group_resource_list`); Microsoft Learn (`microsoft_docs_search`) for any Azure CLI or MCP fact.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates: `leave-preparing` needs `plan` + `questions-resolved`; `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates <this ticket's board id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row and the plan sections named under **Source of truth**, plus `docs/desktop/11-azure-disposition/README.md` § 6 routing and `docs/runbook.md` § Live-operation approval matrix. Then `get_doc_gates <this ticket's board id>` and `take_ticket`.
2. Get the server entry from the authoritative place, not from memory: read `.mcp.json` in `microsoft/azure-skills` at the pinned commit `1a03acfb9ac1a1a05518bf7420d4618cc41847be` and copy its Azure server block verbatim. The block printed in `subagents.md` is an explicitly marked placeholder — the pinned file wins if they differ.
3. Add the entry to `.codex/config.toml` **disabled**, replacing the commented placeholder, in the same shape as the two existing servers:

   ```toml
   [mcp_servers.azure]
   command = "<from the pinned .mcp.json>"
   args = [ "<from the pinned .mcp.json>" ]
   enabled = false
   ```

   **Decide and record** whether a moving reference such as `@azure/mcp@latest` is acceptable here. The whole point of §20.2 is that agent tooling is pinned; if the upstream entry uses `@latest`, either pin the version or write down why a moving MCP package is tolerated where a moving skill is not.
4. **Operator step** — authenticate the workstation: `az login --tenant 858cf5b3-aa0a-47a6-9b40-4851fd0afa94`, then `az account set --subscription e6076573-23a5-46a8-acef-7e22d264e5db`, then `az account show`. Hand back the `az account show` output with the subscription id and tenant id visible. The operator must confirm the account used is a read-capable identity, not an owner credential reserved for release work.
5. Flip `enabled = true`, restart Codex, and record that the `azure` server appears in the session's MCP list. Commit the enable only after the probe in step 6 succeeds; if it fails, leave the entry disabled and record why.
6. Delegate a single read-only probe to `pegasus-azure-auditor`: "list every resource in `rg-pegasus-prod` with its type, using `group_resource_list`; do not call any other tool." Capture its full output into `append_scratch`. Expected: the resources declared in `infra/modules/platform.bicep` — the Container App, the SQL server and database, the storage accounts, the Key Vault, the Application Insights instance — with any live/Bicep difference flagged by the agent.
7. Record the tool list actually exercised, and assert the negative: no create, update, delete, role assignment, setting change, deployment, scale or restart tool was called. Copy the exact tool names into the proof; "no writes happened" without the list is not evidence.
8. Re-read `.codex/agents/pegasus-azure-auditor.toml` and confirm its allowed-tool paragraph still matches the read-only list in `docs/desktop/11-azure-disposition/README.md:345` and the `skill-routing.md` row. There is **no per-tool permission in the TOML** (plan § 7), so this text plus the approval matrix is the entire guardrail — any divergence is a real hole, not a wording nit.
9. Confirm the do-not-load skills were not vendored and are not reachable: `ls .agents/skills/vendor/azure/` must contain only `azure-resource-lookup`, `azure-resource-visualizer`, `azure-cost`, `azure-diagnostics`, `azure-compliance`, `azure-validate`, `azure-storage`, `appinsights-instrumentation` — the eight entries the lockfile names — and nothing from the do-not-load table.
10. Write the guardrail sentence into the plan document and the post-implementation report: the Azure MCP entry exists for read-only inventory, health and cost; any write requires exact-target approval text (target resource id, exact change, rollback, approver) produced by the auditor and approved per `docs/runbook.md` § Live-operation approval matrix before any other agent acts.
11. Record the Appendix C evidence: the pinned `.mcp.json` source, the config diff, the `az account show` output, the `group_resource_list` output, and the tool list from step 7.

## Acceptance criteria

- [ ] `.codex/config.toml` carries an `[mcp_servers.azure]` entry copied from azure-skills `.mcp.json` at commit `1a03acfb9ac1a1a05518bf7420d4618cc41847be`.
- [ ] The entry was added disabled and only enabled after the read-only probe succeeded; the decision about a moving package reference is recorded.
- [ ] `pegasus-azure-auditor` returned a `group_resource_list` listing for `rg-pegasus-prod` (uksouth, subscription `e6076573-23a5-46a8-acef-7e22d264e5db`).
- [ ] The exact tool names used are recorded, and no write tool appears among them.
- [ ] `.agents/skills/vendor/azure/` contains only the eight vendored azure skills; no do-not-load skill is present.
- [ ] The read-only guardrail sentence, naming the approval matrix, is in the plan document.

## Verification

- [ ] `grep -n 'mcp_servers.azure' -A 4 .codex/config.toml` — expected: the server entry with the command and args from the pinned file.
- [ ] `python -c "import tomllib, sys; tomllib.load(open(sys.argv[1], 'rb'))" .codex/config.toml` — expected: exit 0, no output.
- [ ] `az account show` (operator) — expected: `"id": "e6076573-23a5-46a8-acef-7e22d264e5db"` and `"tenantId": "858cf5b3-aa0a-47a6-9b40-4851fd0afa94"`.
- [ ] The recorded `group_resource_list` output for `rg-pegasus-prod` — expected: a resource list consistent with `infra/modules/platform.bicep`, differences flagged.
- [ ] `ls .agents/skills/vendor/azure/` — expected: exactly the eight skill folders named in `eng/skills/skills.lock.json`.

## Evidence tier

Tier 1 — Static/build/architecture. It obliges recorded configuration and read-only tool output; note that the plan's own Azure rows in area 11 are tier 9, so this ticket proves the wiring only and makes no claim about the estate itself.

## Documentation changes

- `docs/desktop/12-agent-tooling/README.md` § 2 Assumptions — the Azure MCP command assumption becomes a recorded fact with the verified command and date.
- `docs/desktop/12-agent-tooling/subagents.md` § `.codex/config.toml` additions — replace the commented placeholder with the verified entry and its date.

## Guardrails

- **Azure**: **no write.** Reads only — `subscription_list`, `group_list`, `group_resource_list` and the other read tools named in `docs/desktop/11-azure-disposition/README.md:345`. `keyvault` may list and show metadata but **never** secret values. Any write requires exact-target approval per `docs/runbook.md` § Live-operation approval matrix, mirrored in `docs/desktop/11-azure-disposition/README.md`; nothing is deprovisioned before cutover, observed use and rollback approval.
- **Scope boundary**: may edit `.codex/config.toml` and the two `docs/desktop/12-agent-tooling/` documents. Must not edit `infra/`, `azure.yaml`, `docs/operations.md`, `src/` or `tests/`, and must not run `scripts/Invoke-AzureDatabaseBootstrap.ps1`, `scripts/Invoke-ProductionSmoke.ps1` or any deployment script.
- **Traps**: there is no per-tool permission in the agent TOML, so a read-only guarantee is text plus discipline — record the tools actually used. `azure-validate` has modes that change state; only its inspection use is permitted, and only when a write has already been approved. A moving MCP package reference contradicts §20.2 pinning — step 3 forces that to be a recorded decision rather than a default.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
