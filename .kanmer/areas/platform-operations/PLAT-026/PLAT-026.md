---
id: PLAT-026
type: ticket
title: 'DSK-11-08 · Post-cutover deprovision checklist, prepared and not executed'
status: backlog
area: platform-operations
assignee: ''
profile: chore
labels:
  - desktop-conversion
  - plan-11
  - phase-10
  - tier-1
groups:
  - EPIC-012
  - HZN-011
links: []
blocks:
  - FEAT-026
docs_todo: true
archived: false
created: '2026-08-24T08:33:11.197Z'
updated: '2026-08-24T08:51:46.023Z'
---

## What

Write proposal §19.2's nine deprovisioning steps as an executable checklist with a candidate list drawn from the register, an evidence field per step, an approval line per candidate, and a banner stating that it **must not be executed before the Phase 10 exit gate is met** — no resource is touched by this ticket.

## Why

Proposal §27 item 16 makes "no Azure resource has been removed before dependency, backup and rollback verification" an acceptance criterion, and §19.2 closes with the sentence this whole area rests on: "A service is not 'unused' merely because no developer remembers it." The checklist must exist *before* cutover, because writing it under time pressure after cutover is how a required resource gets deleted. Operator-visible consequence: with the checklist prepared, retiring the web-only alerts and the Playwright base image is a rehearsed procedure; without it, it is improvisation against production.

Siblings: [[DSK-11-01]] supplies the candidate rows; [[DSK-11-02]] supplies the per-capability `deprovision_candidate` answers; [[DSK-11-03]] supplies the approval template each candidate reuses; [[DSK-11-05]] supplies the health and cost evidence a candidate needs.

## Source of truth

- Plan row: `docs/desktop/11-azure-disposition/README.md` § 5 — `DSK-11-08`
- Plan detail: `docs/desktop/11-azure-disposition/README.md` § 5 "Deprovisioning checklist (after cutover only — §19.2)" — the nine steps and the entry conditions, copied verbatim; § 5 "Resource disposition register" for the candidates
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 19.2 Deprovisioning method after cutover (steps 1–9); § 24 Phase 10 exit gate; § 27 item 16
- Repository evidence:
  - `infra/modules/platform.bicep:576` `pegasus-prod-web-http5xx` and `:617` `pegasus-prod-application-exceptions` — the web-only monitoring candidates
  - `infra/modules/platform.bicep:354-478` the Web container app, `:436-445` the ADR-0028 comment explaining why cpu/memory are sized for in-process Chromium — the image-shrink candidate after native rendering parity (L-03/ADR-0108)
  - `docs/desktop/01-inventory-and-parity/azure-resource-register.md` — the "Deprovision candidate?" column
  - `docs/runbook.md` § Live-operation approval matrix, row "Deploy, restore, fail over, or retire" — explicit operation approval for the exact target, fresh inventory, rollback path, retained source data
  - `docs/open-decisions.md` § Azure ownership and retirement targets — the two decision questions that must be answered by name before any retirement
  - `docs/boundaries.md` — where the web front end is recorded as a post-cutover candidate
  - `docs/current-architecture.md:160-175` — why App Insights cannot prove non-use for most of the working day (PLAT-034)
- Binding decisions:
  - **L-03** — the gateway renderer is retained until golden-file parity passes; its removal supersedes ADR-0028 and therefore needs a new ADR, not just a checklist tick.
  - **D-002 / D-003** — nothing in the distribution path is an Azure resource, so no feed or signing resource ever appears as a candidate.
  - **L-02** — step 2's "confirm with the candidate disabled" is performed on the **local** Test/UAT stack; there is no Azure non-production environment (ADR-0014).
- Depends on: `DSK-11-02` — the per-capability records carry the `deprovision_candidate` answer each checklist row starts from.

## Routing

- **Subagent**: `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (read-only; it also drafts the approval text each candidate will need)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`) → `kanmer-docs` (`.grok/skills/kanmer-docs/SKILL.md`, only if the checklist becomes its own file)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** (`group_resource_list`, `monitor`, `containerapps`) only to confirm a candidate's exact resource id; Microsoft Learn (`microsoft_docs_search`) for what "disable or scale to zero" means per service
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`; gates are `plan` + `questions-resolved` to leave `preparing`, `proof` + `questions-resolved` to enter `done`. Call `get_doc_gates <this ticket id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read `docs/desktop/11-azure-disposition/README.md` § 5 "Deprovisioning checklist", proposal § 19.2 and § 24 Phase 10, and `docs/runbook.md` § Live-operation approval matrix. Then `get_doc_gates <this ticket id>` and `take_ticket <this ticket id>`.
2. Open the checklist with the entry conditions as a hard gate, copied from the plan: it must not start before the Phase 10 exit gate is met — no user requires the web UI, the dependency map matches the target, and the rollback window has expired with approval. Put that as a banner at the top of the document, not a footnote.
3. Copy the nine §19.2 steps **verbatim** as the checklist spine: (1) record traffic, dependencies and cost; (2) confirm the native client passes the full cloud-dependency test with the candidate disabled; (3) remove references in code, IaC, DNS, CI, secrets and monitoring; (4) back up data/configuration and document restoration; (5) disable or scale to zero before deleting where the service permits; (6) observe at least one normal business cycle; (7) obtain explicit approval for the exact target; (8) delete through infrastructure-as-code (`infra/`) or a recorded change; (9) verify no orphaned secrets, DNS, storage or billing items remain and refresh `docs/operations.md` and `docs/current-architecture.md` in the same task.
4. Give every step an **evidence field** that says what must be attached before the step can be ticked — for step 1 the read-only command output and its date; for step 2 the Test/UAT run log; for step 4 the backup location and the restore procedure; for step 6 the start and end dates of the observed cycle; for step 7 the pasted approval sentence.
5. Build the candidate list from the register's "Deprovision candidate?" column, and from nothing else. As of the plan it is: the web-only monitoring alerts (`pegasus-prod-web-http5xx`, `platform.bicep:576`), the web UI code and assets inside the Web image, and the Playwright base image with the cpu/memory reduction it allows (`platform.bicep:436-445`). Every other register row is "No".
6. Write the removal condition for each candidate in the register's own words — for the renderer, "only after all report types match and no unattended use remains", plus the note that removing it supersedes ADR-0028 and needs a new ADR under the reserved block ADR-0100…ADR-0110.
7. State explicitly what is **not** a candidate and why, so a future reader does not have to re-derive it: SQL, the Worker and its FC1 plan, Key Vault, both storage accounts, ACR, the Container Apps environment, the identities and role assignments, and the budget. Cite the cloud-dependency record that keeps each one.
8. Add the anti-pattern paragraph that §19.2 ends on, in the plan's own terms: telemetry cannot answer "is it still used?" during the capped Log Analytics window (PLAT-034), so use gateway logs, action history and the desktop diagnostics bundle — "a service is not unused merely because no developer remembers it".
9. Attach the approval line per candidate using the template held in `docs/desktop/11-azure-disposition/README.md`, pre-filled with subscription `e6076573-23a5-46a8-acef-7e22d264e5db`, tenant `858cf5b3-aa0a-47a6-9b40-4851fd0afa94` and resource group `rg-pegasus-prod`, leaving only the exact target, trigger and rollback to be completed at execution time.
10. Record where the checklist lives and link it: either as a section of `docs/desktop/11-azure-disposition/README.md` or as `docs/desktop/11-azure-disposition/deprovision-checklist.md` linked from § 4 — decide and record, as the plan does not say. Both are inside the allowed markdown root `docs/(prd|frd|adr|design|desktop)`.
11. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`; both must exit 0.
12. Simplification pass (`AGENTS.md` step 4, `n/a — docs-only`), write `proof`, and hand to `pegasus-desktop-reviewer`, whose review must confirm one thing above all: nothing in the branch executes anything.

## Acceptance criteria

- [ ] The checklist opens with the Phase 10 entry conditions and the words "do not execute before Phase 10 exit".
- [ ] All nine §19.2 steps are present verbatim, each with an evidence field naming what must be attached.
- [ ] The candidate list matches the register's "Deprovision candidate?" column exactly — no candidate is added on judgement.
- [ ] Every candidate has a removal condition and a pre-filled approval line; the renderer's row records that its removal supersedes ADR-0028 and needs a new ADR.
- [ ] The non-candidates are listed with the cloud-dependency record that keeps each one.
- [ ] The telemetry caveat (PLAT-034) is stated as a reason not to trust an absence of signal.
- [ ] No Azure resource is disabled, scaled or deleted by this ticket.

## Verification

- [ ] `grep -n "do not execute before Phase 10 exit" docs/desktop/11-azure-disposition/` — expected: the banner is present.
- [ ] Reviewer cross-check of the candidate list against `docs/desktop/01-inventory-and-parity/azure-resource-register.md` — expected: identical sets.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exits 0.
- [ ] `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected: exits 0.
- [ ] `git diff --stat` — expected: only files under `docs/desktop/11-azure-disposition/`; nothing under `infra/`, `src/` or `scripts/`.

## Evidence tier

Tier 1 — Static/build/architecture. The obligation is documentary: the checklist matches §19.2 step for step, its candidates match the register, and the documentation gates pass. The runtime evidence belongs to the future execution, which this ticket deliberately does not perform.

## Documentation changes

- `docs/desktop/11-azure-disposition/README.md` § Deprovisioning checklist, or a new `docs/desktop/11-azure-disposition/deprovision-checklist.md` linked from § 4.
- `docs/boundaries.md` — only if the web front end's post-cutover candidate status is not already recorded there; a one-line cross-reference, no change of position.

## Guardrails

- **Azure**: no write, and specifically no deletion, disable or scale-to-zero. Reads are free (`docs/runbook.md` § Live-operation approval matrix); retirement is the matrix's "Deploy, restore, fail over, or retire" row and needs explicit operation approval for the exact target, fresh inventory, a rollback path and retained source data. **Nothing is deprovisioned before cutover, observed non-use and rollback approval** — this ticket prepares the procedure and executes none of it.
- **Scope boundary**: documentation under `docs/desktop/11-azure-disposition/` plus at most a cross-reference line in `docs/boundaries.md`. Do not edit `infra/`, `src/`, monitoring definitions or CI.
- **Traps** (plan § 7): telemetry blind spots (PLAT-034) make "is it still used?" unanswerable from App Insights for most of the day; out-of-band resources will not be removed by `azd provision` and must be listed separately if any were found by [[DSK-11-01]]; stale current-state docs are refreshed in the same task as any change.
- **Board placement**: this plan area seeds into `platform-operations` because the board-shape table in `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape assigns no area to plan 11.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document (`n/a — docs-only` for documentation-only tickets).

## Outcome

_Filled at closeout._
