---
id: PLAT-021
type: ticket
title: >-
  DSK-11-03 · Conditional Azure writes catalogue with approval templates and
  rollback
status: preparing
area: platform-operations
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:21:16.112Z'
labels:
  - desktop-conversion
  - plan-11
  - phase-2
  - tier-1
groups:
  - EPIC-012
  - HZN-003
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:26:57.634Z'
updated: '2026-08-24T21:21:16.112Z'
---

## What

Make `docs/desktop/11-azure-disposition/README.md` § "Conditional Azure writes (complete list, all ⚠)" the single mirror of every Azure write the conversion may ever need: each row carrying its trigger, exact target, Bicep location, filled approval text and rollback — with the withdrawn rows kept struck through and dated, and a cross-check proving no ⚠ anywhere in `docs/desktop/` is missing from it.

## Why

The rule of this area is "read freely, write only with exact-target approval", and the plan states that the complete list of writes is in this table — "anything not listed is a plan change". `docs/runbook.md` § Live-operation approval matrix requires explicit approval for the exact target of every Azure mutation, and `docs/open-decisions.md` § Azure ownership and retirement targets requires fresh inventory with resolved resource ids before any mutation. Operator-visible consequence: without one catalogue, an agent asks for approval in free text against a stale target — the precise failure the matrix exists to prevent.

Siblings: [[DSK-11-06]] executes the one write this catalogue currently authorises; [[DSK-09-10]] withdrew the feed-hosting writes by choosing a UNC share; [[DSK-10-16]] owns the alert-rule and quota write follow-ups this table mirrors; [[DSK-11-08]] reuses the approval template for deprovisioning.

## Source of truth

- Plan row: `docs/desktop/11-azure-disposition/README.md` § 5 — `DSK-11-03`
- Plan detail: `docs/desktop/11-azure-disposition/README.md` § 5 "Conditional Azure writes (complete list, all ⚠)" (the table to complete) and the approval-text template immediately below it
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 19 Azure service disposition and § 19.1 "Do not add by default"
- Repository evidence:
  - `docs/runbook.md` § Live-operation approval matrix — the row "Change or use an Azure service (write/mutation/cost)" requires "Explicit approval for the exact target, fresh inventory, least-privilege identity"
  - `docs/open-decisions.md` § Azure ownership and retirement targets — "dated names are not current identity proof"
  - `infra/modules/platform.bicep:354-478` the Web container app (its env array at `:418-434`, with `Features__AutomationMcp` at `:429` as the precedent), `:46` Log Analytics, `:617-689` the scheduled-query alert after which a new rule would be added
  - `infra/main.bicep:71` `rg-pegasus-prod`; `.azure/deployment-plan.md:24-27` subscription `e6076573-23a5-46a8-acef-7e22d264e5db`, tenant `858cf5b3-aa0a-47a6-9b40-4851fd0afa94`
  - `.agents/skills/pegasus-release/SKILL.md` § 5–6 — the only route by which a Bicep change reaches production (`azd env set`, `Test-AzureDeploymentPlan.ps1 -Mode PreProvision`, `azd provision --no-prompt`)
  - `docs/desktop/04-auth-session-update-and-startup/README.md:175-186` — the minimum-version config fallback that is ⚠ only if ever changed in production
  - `docs/desktop/09-release-update-and-distribution/README.md:247` — the conditional writes of the release area
  - `docs/desktop/10-security-observability-performance/README.md:123`, `:174` — the cap change and the blocked-client alert rule
- Binding decisions:
  - **D-002** — self-managed certificate: the Artifact Signing and Key Vault certificate-import rows stay **withdrawn**, struck through and dated 2026-08-23.
  - **D-003** — in-house UNC share: the update-feed container, its public-access setting and its publisher RBAC stay **withdrawn**, struck through and dated 2026-08-23.
  - **C-01** — private repositories rule out anonymous HTTPS hosting permanently, which is *why* those rows are withdrawn; record the reason, not just the strike-through.
  - **L-02** — no Azure test environment exists (ADR-0014); a request for one is out of bounds and must not be catalogued as a candidate write.
- Depends on: `DSK-09-10` — standing up the decided UNC feed fixes which distribution writes are withdrawn; `DSK-04-06` — the minimum-version design fixes whether the config fallback is ever written in production (the plan row cites `DSK-04-05`, the token-revocation ticket, which carries no Azure write; treat that as an off-by-one).

## Routing

- **Subagent**: `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (drafts the approval text; refuses writes) with `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml` (confirms the route each write would take)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `pegasus-release` (`.agents/skills/pegasus-release/SKILL.md`, for the provisioning route and its traps) → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** (`group_resource_list`, `containerapps`, `monitor`) only to confirm an exact target id; Microsoft Learn (`microsoft_docs_search`) for service semantics of a proposed write
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`; gates are `plan` + `questions-resolved` to leave `preparing`, `proof` + `questions-resolved` to enter `done`. Call `get_doc_gates <this ticket id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read `docs/desktop/11-azure-disposition/README.md` § 3 and § 5, `docs/runbook.md` § Live-operation approval matrix, and `.agents/skills/pegasus-release/SKILL.md` § 5–6. Then `get_doc_gates <this ticket id>` and `take_ticket <this ticket id>`.
2. Enumerate every warning marker in the plan set: `grep -rn "⚠" docs/desktop/ > <scratch>/azure-writes-sweep.txt`. Attach the sweep with `append_scratch`. Expect hits in areas 04, 09, 10 and 11; classify each hit as (a) a real conditional Azure write, (b) a reference to the marker itself, or (c) a withdrawn write.
3. For every class (a) hit not already in the catalogue, add a row with all six columns: Write · Trigger · Exact target · Bicep location · Approval · Rollback. Do not paraphrase a target — write the resource name exactly as the register records it (for example `pegasus-prod-web-252ow37gij`, `pegasus-prod-logs-<suffix>` resolved to its real suffix).
4. Fill the approval text for each row from the template held in the plan, which is used verbatim, one request per write:
   > Request `<create | change | assign>` of `<exact resource/setting>` in `rg-pegasus-prod` (subscription `e6076573-23a5-46a8-acef-7e22d264e5db`, tenant `858cf5b3-aa0a-47a6-9b40-4851fd0afa94`) because `<trigger>`; Bicep change at `<file:section>`; applied through `<route>`; rollback `<steps>`; nothing else changes.
5. Set the route of every row to the existing release route and nothing else: a Bicep edit under `infra/`, then `azd provision --no-prompt` performed by `pegasus-release-packager` following `.agents/skills/pegasus-release/SKILL.md`. A write applied outside `infra/` is invisible to `azd provision` and must be recorded as forbidden (plan § 7, "out-of-band resources").
6. Write a concrete rollback for every row — the inverse change and the command that applies it, not "revert". For `Features:DesktopGateway` that is: set `Features__DesktopGateway` to `false` in `infra/modules/platform.bicep` and re-provision; for an alert rule, delete the rule through `infra/`; for the Log Analytics cap, restore the previous `dailyQuotaGb`.
7. Keep the withdrawn rows in place, struck through, with the withdrawal date and reason: the update-feed container with anonymous read plus publisher RBAC (withdrawn 2026-08-23, D-003 chose an in-house UNC share), the Artifact Signing account and certificate profile, and the Key Vault certificate import plus signer RBAC (both withdrawn 2026-08-23, D-002 chose a self-managed certificate). Deleting them would lose the record that they were considered and rejected.
8. Add the writes this area has that are easy to forget: applying the **intended register tags** (`desktop-conversion=phase0-inventory`, `owner=<name>`, `codepath=<file>`) is a write and belongs in the table as a row that is not currently approved.
9. Prove the catalogue is complete: re-run the sweep from step 2 and check every class (a) and (c) hit against the table, then record the mapping in the ticket. Any ⚠ in another plan that has no row here is either added or reported as a plan change.
10. Add one sentence to `docs/desktop/11-azure-disposition/README.md` § 3 stating that this table is the single mirror and that a write not listed is a plan change requiring a new decision.
11. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`; both must exit 0.
12. Simplification pass (`AGENTS.md` step 4, `n/a — docs-only`), then `proof` (`command-log` with the sweep output) and review by `pegasus-desktop-reviewer`, who re-runs the grep and checks the mapping.

## Acceptance criteria

- [ ] Every ⚠ conditional Azure write in `docs/desktop/` areas 04, 09, 10 and 11 has exactly one row in the catalogue.
- [ ] Each row has trigger, exact target (real resource name), Bicep `file:line` location, a fully filled approval sentence and a concrete rollback command.
- [ ] Withdrawn rows are retained struck through with their 2026-08-23 date and the deciding constraint (D-002, D-003, C-01).
- [ ] Every row's route is a Bicep change applied through the `pegasus-release` procedure; no row proposes a portal or ad-hoc `az` mutation.
- [ ] Applying the register's intended tags appears as an unapproved write row.
- [ ] The README states that a write not in this table is a plan change.

## Verification

- [ ] `grep -rn "⚠" docs/desktop/ | wc -l` re-run by the reviewer, mapped row by row against the catalogue — expected: no unmapped conditional write.
- [ ] `grep -n "e6076573-23a5-46a8-acef-7e22d264e5db" docs/desktop/11-azure-disposition/README.md` — expected: the subscription id appears in the approval template and in every filled approval sentence.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exits 0.
- [ ] `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected: exits 0.
- [ ] `git diff --stat` — expected: only `docs/desktop/11-azure-disposition/README.md` changed.

## Evidence tier

Tier 1 — Static/build/architecture. The obligation is a documentary completeness proof: the grep sweep maps one-to-one onto the catalogue rows, and the documentation gates pass. No Azure state is touched.

## Documentation changes

- `docs/desktop/11-azure-disposition/README.md` § 5 "Conditional Azure writes" — rows completed, approval text filled, rollbacks written, withdrawn rows retained; § 3 gains the "single mirror" sentence.

## Guardrails

- **Azure**: no write. This ticket *catalogues* writes; it performs none. Reads are free and need no per-target approval (`docs/runbook.md` § Live-operation approval matrix); every write in the catalogue needs explicit approval for its exact target before anyone executes it. **Nothing is deprovisioned before cutover, observed non-use and rollback approval.**
- **Scope boundary**: `docs/desktop/11-azure-disposition/README.md` only. Do not edit `infra/` (a Bicep change belongs to the ticket that owns the write), `src/`, or the area 04/09/10 plans — a missing ⚠ there is reported, not fixed here.
- **Traps** (plan § 7): a write without approval is the disqualifying failure of this area; out-of-band resources are invisible to `azd provision`; dated names are not current identity proof, so every exact target is re-verified against the register before the approval text is used.
- **Board placement**: this plan area seeds into `platform-operations` because the board-shape table in `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape assigns no area to plan 11.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document (`n/a — docs-only` for documentation-only tickets).

## Outcome

_Filled at closeout._
