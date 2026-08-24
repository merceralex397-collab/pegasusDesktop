# EPIC-012 · Area 11 — Azure disposition

Read this once before working any `DSK-11-xx` ticket. It carries what binds every
ticket in the epic; the ticket body carries the work.

## What this area delivers

The Azure estate's position during and after the conversion: a verified resource
register, a cloud-dependency record per capability, the complete catalogue of Azure
writes the conversion may ever need (each with approval text and rollback), a cost
and health baseline, the per-release refresh rule that keeps all of it current, and
a post-cutover deprovision checklist that is prepared and deliberately not executed.
The area owns no deployment route: the gateway ships through the existing
`pegasus-release` procedure, and desktop packaging belongs to area 09.

**The rule of the area, in one line:** read freely, write only with exact-target
approval, deprovision nothing before cutover, observed non-use and rollback approval.

## Proposal coverage

- §4 and §4.1 — the cloud-justification test and placement table, instantiated as
  Appendix B cloud-dependency records.
- §19 — the service disposition table, instantiated against the real estate (which
  is smaller than the proposal's generic list).
- §19.1 — "do not add by default", enforced as a check on every Azure touch.
- §19.2 — the deprovisioning method, as a prepared post-cutover checklist.
- §24 Phase 10 — cloud rationalisation, preparation only.
- §27 items 15 and 16 — the two acceptance criteria this area is measured by.
- Appendix B — the cloud dependency record shape.

## Decisions, assumptions and deviations that bind every ticket

- **L-01** the gateway is `Pegasus.Web` evolved in place; no new deployment unit.
- **L-02** Test/UAT is the local production-mimicking stack; ADR-0014 stands, so
  there is no Azure dev/test/staging and asking for one is out of bounds.
- **L-03** the gateway renderer is retained until golden-file parity passes; its
  later removal supersedes ADR-0028 and needs a new ADR (reserved block ADR-0100…0110).
- **D-002** (2026-08-23) signing uses a self-managed in-house certificate — no Azure
  signing service, and Key Vault keeps holding secrets only.
- **D-003** (2026-08-23) the update feed is an in-house UNC share over SMB — **no
  Azure resource hosts the feed**; every feed-related write is withdrawn.
- **C-01** (2026-08-23) the repositories become private; anonymous public hosting is
  ruled out permanently, and private Windows CI minutes bill at 2×.
- **Assumption now proven false in the drafts**: the `update-feed` dependency record
  still says "D-003 open". It is decided; correct it rather than copying it forward.
- **Deviation from proposal §19**: resources the proposal lists that Pegasus does not
  have (Front Door, SignalR, Service Bus, Redis, slots) are recorded as
  "does not exist — do not add", never as deprovision candidates.
- Minimum client version is **database-backed**, not config-backed, so raising it is
  an administrative action and not an Azure write.
- Only one Azure write is currently authorised by the plan: `Features:DesktopGateway`
  on the Web Container App (`DSK-11-06`). Anything else is a plan change.

## Exit gate and what proves it

Runtime Azure dependencies equal the approved register, and no resource has been
removed. Proved by: the Test/UAT stack running with only the documented dependencies;
the pilot ring's gateway logs and telemetry; the register and dependency records
refreshed at the most recent release; and the deprovision checklist existing,
signed off and unexecuted.

## Routing for this area

- Subagents: `pegasus-azure-auditor` (`.codex/agents/pegasus-azure-auditor.toml`) for
  all read-only inventory, cost, health and approval-text work — it is read-only by
  sandbox and refuses writes; `pegasus-release-packager`
  (`.codex/agents/pegasus-release-packager.toml`) executes approved writes through the
  release route; `pegasus-desktop-reviewer` reviews every PR in the epic.
- Skills (load `pegasus-desktop`, `.agents/skills/project/pegasus-desktop/SKILL.md`,
  first, always): `azure-resource-lookup`, `azure-resource-visualizer`, `azure-cost`,
  `azure-compliance`, `azure-diagnostics`, `appinsights-instrumentation` — all from
  `microsoft/azure-skills` `1a03acfb`; `azure-validate` **only** when a write is
  already approved, and what-if only; `pegasus-release`
  (`.agents/skills/pegasus-release/SKILL.md`) for anything that provisions; Kanmer
  skills from `.grok/skills/<name>/SKILL.md`.
- **Never load** `azure-deploy`, `azure-prepare`, `azure-app-onboard`,
  `azure-enterprise-infra-planner` (do-not-load table,
  `docs/desktop/12-agent-tooling/skill-routing.md`): they provision, and the only
  deployment route is `pegasus-release`.
- MCP: Azure MCP read-only (`subscription_list`, `group_resource_list`, `storage`,
  `keyvault` names only, `sql`, `containerapps`, `functionapp`, `monitor`,
  `applicationinsights`, `acr`, `role`, `pricing`, `advisor`, `resourcehealth`);
  Microsoft Learn (`microsoft_docs_search`, `microsoft_docs_fetch`); Kanmer
  (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`,
  `move_item`).

## Traps (plan §7)

- **A write without approval is the single disqualifying failure of this area.**
- **Out-of-band resources** are invisible to `azd provision`. The Log Analytics
  0.1 GB/day cap is exactly this: it appears nowhere in `infra/`. All writes go
  through Bicep and the release route.
- **Telemetry blind spots** (PLAT-034): the workspace cap exhausts within hours, so
  most of each UK working day returns empty and the two Sev1 alert rules cannot fire.
  Never conclude "unused" from App Insights — use gateway logs, action history and
  desktop diagnostics bundles. A service is not unused merely because no developer
  remembers it (§19.2).
- **Stale current-state docs**: `docs/operations.md:295` still narrates "release 14"
  while its own release table is current. Refresh in the same task as any write.
- **Runtime-role grants** (PLAT-035) travel with migrations, not Azure writes, but a
  missing grant has shipped three times — every new gateway table needs its `Grant*`
  migration and the expected-matrix update in `scripts/Invoke-AzureDatabaseBootstrap.ps1`.
- **Dated names are not current identity proof** (`docs/open-decisions.md` § Azure
  ownership and retirement targets): re-verify every exact target before using it.

## Read these before starting any ticket in this epic

1. `docs/desktop/11-azure-disposition/README.md` — the area plan, in full.
2. `docs/desktop/01-inventory-and-parity/azure-resource-register.md` — the register.
3. `docs/runbook.md` § Live-operation approval matrix — reads free, writes approved.
4. `docs/desktop/README.md` § Locked decisions and open decisions — L-01…L-05,
   D-001…D-003, C-01.
5. `docs/desktop/00-governance-and-workflow/README.md` — ADR block, board shape,
   ticket template, phase map.
6. `docs/desktop/12-agent-tooling/skill-routing.md` — exact skill names, pinned
   revisions, and the do-not-load table.
7. `infra/main.bicep` and `infra/modules/platform.bicep` — the declared estate.
8. `docs/operations.md` § Production environment and `docs/current-architecture.md`
   — the as-built state and the telemetry gap.
9. `.agents/skills/pegasus-release/SKILL.md` — the only route to production.
10. `AGENTS.md` — task workflow, simplification pass, independent review,
    markdown placement.

## Board note

Plan area 11 seeds into board area `platform-operations` (prefix `PLAT`): the
board-shape table in plan 00 assigns no area to plan 11, and the seeding contract
routes plans 10 and 11 there. Tickets `DSK-11-01`…`DSK-11-09` are `PLAT-019`…`PLAT-027`.
