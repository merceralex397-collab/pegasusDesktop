---
id: PLAT-024
type: ticket
title: >-
  DSK-11-06 · ⚠ Azure write: enable Features:DesktopGateway on the Web Container
  App
status: preparing
area: platform-operations
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:21:16.450Z'
labels:
  - desktop-conversion
  - plan-11
  - phase-2
  - tier-12
  - azure-write
  - needs-operator
groups:
  - EPIC-012
  - HZN-003
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:30:10.950Z'
updated: '2026-08-24T21:21:16.450Z'
---

## What

Execute the conversion's one currently-authorised Azure write: add `Features__DesktopGateway` set to `true` to the Web Container App's environment array in `infra/modules/platform.bicep`, apply it through the existing `pegasus-release` provisioning route with exact-target approval recorded, prove the `/api/v1` group answers and the Razor Pages are unaffected, and refresh the current-state documents in the same task.

## Why

The `/api/v1` route group and the desktop token flow ship behind the composition gate `Features:DesktopGateway` (plan 03, [[DSK-03-02]]) so `main` stays releasable for the live web app throughout the conversion. Turning that gate on in production is the first — and, after D-002 and D-003, very nearly the only — Azure write the conversion needs (`docs/desktop/11-azure-disposition/README.md` § 2 Assumptions). Operator-visible consequence: until this lands, no desktop client can authenticate or check compatibility against production; if it lands wrongly, the live Razor Pages estate that ten operators use every day is the thing at risk.

Siblings: [[DSK-11-03]] holds the catalogue row and the approval sentence for this write; [[DSK-03-02]] creates the gated route group; [[DSK-04-06]] adds `GET /api/v1/client-compatibility`, the endpoint this write makes reachable; [[DSK-11-07]] adds the register-refresh step this release must follow.

## Source of truth

- Plan row: `docs/desktop/11-azure-disposition/README.md` § 5 — `DSK-11-06`
- Plan detail: `docs/desktop/11-azure-disposition/README.md` § 3 ("Feature flag `Features:DesktopGateway` is a Container App app setting change ⚠ when it is first enabled in production … mirroring how `Features:AutomationMcp` was enabled") and § 5 "Conditional Azure writes" row 1
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 19 Azure service disposition; § 24 Phase 2 — compatibility, update and authentication
- Repository evidence:
  - `infra/modules/platform.bicep:354-478` the Web container app; its env array at `:418-434`; **the precedent to copy is `{ name: 'Features__AutomationMcp', value: 'true' }` at `:429`** — a double underscore, because Container Apps env vars cannot carry `:`
  - `src/Pegasus.Web/Mcp/AutomationMcp.cs:12` — `public const string FeatureFlag = "Features:AutomationMcp";` shows the flag-constant pattern the desktop gate follows
  - `src/Pegasus.Web/Program.cs:112`, `:202`, `:640` — how `Features:*` values are read at composition time
  - `.agents/skills/pegasus-release/SKILL.md` § 5 Point the azd environment at *this* release (and its stale-environment trap), § 6 Provision, § 9 Smoke, § 11 Refresh the current-state docs
  - `scripts/Test-AzureDeploymentPlan.ps1` — the `-Mode PreProvision` gate run before provisioning
  - `scripts/Invoke-ProductionSmoke.ps1` — takes `-BaseUri`, `-ExpectedSourceRevision`, `-ExpectedVersion`, `-ResourceGroupName rg-pegasus-prod`, `-SubscriptionId e6076573-23a5-46a8-acef-7e22d264e5db`, `-ExpectedWorkerActivation`
  - `docs/runbook.md` § Live-operation approval matrix — "Change or use an Azure service" requires explicit approval for the exact target, fresh inventory, least-privilege identity
  - ADR-0007 direct authorised-terminal deployment; ADR-0026 is the precedent for enabling a capability by explicit deployment configuration
- Binding decisions:
  - **L-01** — the gateway is `Pegasus.Web` evolved in place; this write adds a setting to the existing Container App and creates no deployment unit.
  - **D-001** — at the first production gateway change the fork becomes the single release source; this ticket *is* that moment, so confirm the upstream freeze position before releasing.
  - **L-02** — there is no Azure test environment; the rehearsal happens on the local Test/UAT stack, not in a second Azure environment.
- Depends on: `DSK-03-01` and `DSK-03-02` — the contracts project and the gated `/api/v1` route group must exist and be off by default; `DSK-04-06` — the compatibility endpoint that proves the gate is on; `DSK-11-03` — the catalogue row and approval sentence; **operator approval** — the exact-target approval text must be recorded in this ticket before any command runs.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml` (it owns the release route; it must not change application code). Azure facts come from `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml`, which cannot perform the write.
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `pegasus-release` (`.agents/skills/pegasus-release/SKILL.md`) → `azure-validate` (`microsoft/azure-skills` `1a03acfb`, **what-if only, and only because a write is approved**) → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** (`containerapps`, `group_resource_list`, `monitor`) for fresh inventory before and after; Microsoft Learn (`microsoft_docs_search` for Container Apps environment-variable and revision semantics)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`; gates are `plan` + `questions-resolved` to leave `preparing`, `proof` + `questions-resolved` to enter `done`. Call `get_doc_gates <this ticket id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read `docs/desktop/11-azure-disposition/README.md` § 3 and § 5, `docs/runbook.md` § Live-operation approval matrix, and `.agents/skills/pegasus-release/SKILL.md` end to end. Then `get_doc_gates <this ticket id>` and `take_ticket <this ticket id>`.
2. **Operator step — approval before anything else.** Obtain and paste into this ticket the exact-target approval, using the template from the plan verbatim: *Request `change` of app setting `Features__DesktopGateway` on Container App `pegasus-prod-web-252ow37gij` in `rg-pegasus-prod` (subscription `e6076573-23a5-46a8-acef-7e22d264e5db`, tenant `858cf5b3-aa0a-47a6-9b40-4851fd0afa94`) because the Phase 2 gateway API is being enabled in production; Bicep change at `infra/modules/platform.bicep` container-app env block (`:418-434`); applied through `azd provision` on the authorised release terminal; rollback: set the value to `false` and re-provision; nothing else changes.* Evidence the operator hands back: the approval text with a date. **No command in the steps below runs before this exists in the ticket.**
3. Fresh inventory (the approval matrix requires it): Azure MCP `containerapps` show for `pegasus-prod-web-252ow37gij` — record the current revision name, image digest and the **names** of its environment variables. Confirm `Features__DesktopGateway` is absent today. Attach the output.
4. Rehearse locally first (L-02: there is no Azure test environment). On the Test/UAT stack from area 08, run the gateway with `Features:DesktopGateway` off and then on: off must give 404 on the `/api/v1` group, on must give a 200 from `GET /api/v1/client-compatibility`, and the Razor Pages must behave identically in both. Record both runs.
5. Edit `infra/modules/platform.bicep`: inside the Web container app's `env` array (`:418-434`), immediately after the `Features__AutomationMcp` entry at `:429`, add `{ name: 'Features__DesktopGateway', value: 'true' }`. Copy the surrounding style exactly — double underscore, string `'true'`. Change nothing else in the file. Done when `az bicep build --file infra/modules/platform.bicep` (or the CI infrastructure lane) succeeds.
6. Confirm the flag name matches the code constant that [[DSK-03-02]] introduced: `grep -rn "Features:DesktopGateway" src/` must find the constant, and the Bicep name must be that string with `:` replaced by `__`. A mismatch here silently leaves the gate off in production, which is the failure mode this step exists to catch.
7. Load `azure-validate` and run a **what-if only** preview of the provision against `rg-pegasus-prod`. Expected result: a single change — one added environment variable on `pegasus-prod-web-252ow37gij` — and nothing else. Any additional change in the what-if output stops the ticket and goes back to step 3.
8. Follow `.agents/skills/pegasus-release/SKILL.md` § 5: `azd env get-values | Select-String 'SECRET_URI|PEGASUS_WEB_|WORKER_ACTIVATION|AZURE_'`, confirm every `*_SECRET_URI` names `pegasusprodkv252ow37g`, `PEGASUS_WORKER_ACTIVATION` is exactly `approved-live-worker` and `AZURE_RESOURCE_GROUP` is `rg-pegasus-prod`, then set this release's `PEGASUS_WEB_IMAGE_DIGEST` and `PEGASUS_WEB_REVISION_SUFFIX`. The skill's trap applies: the azd environment is stale and is not authoritative.
9. Gate the provision: `pwsh ./scripts/Test-AzureDeploymentPlan.ps1 -Mode PreProvision -Environment pegasus-prod -ManifestPath artifacts/releases/<version>/release-manifest.json -WorkerActivation 'approved-live-worker' -ExpectedLiveWorkerActivation 'approved-live-worker'`. It must pass before provisioning.
10. **Operator step — apply the write.** On the authorised release terminal (ADR-0007), run `azd provision --no-prompt`. Evidence handed back: the provision output, the new revision name and the confirmation that the Function App settings were not altered (the skill warns a provision that fails on Web may still have succeeded for the Function App).
11. Prove it, twice over: `pwsh ./scripts/Invoke-ProductionSmoke.ps1 -BaseUri <production base uri> -ExpectedSourceRevision <40-char sha> -ExpectedVersion <version> -ResourceGroupName rg-pegasus-prod -SubscriptionId e6076573-23a5-46a8-acef-7e22d264e5db -ExpectedWorkerActivation approved-live-worker` must pass; `GET /api/v1/client-compatibility` must return 200 with `minimumVersion`, `currentVersion`, `channel`, `maintenanceMessage` and `validForSeconds`; and a signed-in Razor Pages workflow (dashboard, one case) must behave exactly as before. Attach all three.
12. Finish the release the way the skill requires — in the same task, before merge — by refreshing `docs/current-architecture.md` (the deployment boundary now includes `/api/v1`), `docs/operations.md` (the release row: number, date, source SHA, image digest, revision name, migrations, and what this release proved beyond smoke), and the register plus dependency records per [[DSK-11-07]]. Then simplification pass (`AGENTS.md` step 4), `proof` as a `command-log`, and review by `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] The exact-target approval text, dated, is recorded in the ticket before any Azure command was run.
- [ ] `infra/modules/platform.bicep` gains exactly one line: `{ name: 'Features__DesktopGateway', value: 'true' }` in the Web container app env array; no other infrastructure change is in the diff.
- [ ] The what-if preview showed one change and nothing else.
- [ ] `GET /api/v1/client-compatibility` answers 200 in production and the `/api/v1` group is reachable.
- [ ] The Razor Pages application is unaffected: production smoke passes and a signed-in workflow behaves as before.
- [ ] `docs/current-architecture.md` and `docs/operations.md` are refreshed in the same task as the write.
- [ ] The rollback is recorded and rehearsed on the local stack: value `false` plus re-provision restores 404 on `/api/v1`.

## Verification

- [ ] `pwsh ./scripts/Test-AzureDeploymentPlan.ps1 -Mode PreProvision -Environment pegasus-prod -ManifestPath artifacts/releases/<version>/release-manifest.json -WorkerActivation 'approved-live-worker' -ExpectedLiveWorkerActivation 'approved-live-worker'` — expected: passes with no plan violation.
- [ ] `pwsh ./scripts/Invoke-ProductionSmoke.ps1 -BaseUri <base uri> -ExpectedSourceRevision <sha> -ExpectedVersion <version> -ResourceGroupName rg-pegasus-prod -SubscriptionId e6076573-23a5-46a8-acef-7e22d264e5db -ExpectedWorkerActivation approved-live-worker` — expected: exits 0, all checks green.
- [ ] `curl -s -o /dev/null -w "%{http_code}" <base uri>/api/v1/client-compatibility` — expected: `200` (it was `404` before the write).
- [ ] Azure MCP `containerapps` show `pegasus-prod-web-252ow37gij` — expected: a new revision whose env-var names include `Features__DesktopGateway`, and no other env-var change.
- [ ] `git diff infra/` — expected: one added line in `infra/modules/platform.bicep`.

## Evidence tier

Tier 12 — Integrated workflow. The obligation is end-to-end proof against the real caller: the approved write applied through the real release route, the real production endpoint answering, the existing web workflow unbroken, and telemetry and documentation refreshed. Registration-only or mock evidence does not satisfy this tier.

## Documentation changes

- `docs/current-architecture.md` — the deployment boundary now includes the `/api/v1` gateway surface on the Web Container App.
- `docs/operations.md` § Production environment — the release row (number, date, source SHA, image digest, revision name, migrations) and what this release proved.
- `docs/desktop/11-azure-disposition/README.md` § 5 Conditional Azure writes — mark the row as executed, with its date, approval and rollback state.
- `docs/desktop/01-inventory-and-parity/azure-resource-register.md` — the Web container app row gains the new setting.

## Guardrails

- **⚠ Azure write**: app setting `Features__DesktopGateway` on Container App `pegasus-prod-web-252ow37gij` in `rg-pegasus-prod`. It needs explicit approval for that exact target with fresh inventory and a least-privilege identity (`docs/runbook.md` § Live-operation approval matrix), and it is mirrored as row 1 of `docs/desktop/11-azure-disposition/README.md` § Conditional Azure writes. Reads remain free. **Nothing is deprovisioned before cutover, observed non-use and rollback approval** — this ticket adds a setting and removes nothing.
- **Scope boundary**: `infra/modules/platform.bicep` (one env entry), `docs/current-architecture.md`, `docs/operations.md`, and the two plan-11/register documents. It must not change application code under `src/` (the gate itself is [[DSK-03-02]]'s), must not touch the Worker settings or any role assignment, and must not alter cpu/memory or the image.
- **Traps** (plan § 7 and the release skill): a write without approval is the disqualifying failure of this area; the azd environment is stale and is not authoritative — verify secret URIs and worker activation before provisioning; a provision that fails on Web may still have succeeded for the Function App, so re-read Worker settings; out-of-band changes are invisible to `azd provision`, so this must go through `infra/`; refresh the stale current-state docs in the same task (`docs/operations.md:295` still narrates "release 14" while its own release table is current).
- **Board placement**: this plan area seeds into `platform-operations` because the board-shape table in `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape assigns no area to plan 11.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
