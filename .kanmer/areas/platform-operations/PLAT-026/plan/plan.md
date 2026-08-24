# Plan — PLAT-026

## Objective

Write proposal §19.2's nine deprovisioning steps as an executable checklist with a candidate list drawn from the register, an evidence field per step, an approval line per candidate, and a banner stating that it **must not be executed before the Phase 10 exit gate is met** — no resource is touched by this ticket.

## Chosen approach

Proposal §27 item 16 makes "no Azure resource has been removed before dependency, backup and rollback verification" an acceptance criterion, and §19.2 closes with the sentence this whole area rests on: "A service is not 'unused' merely because no developer remembers it." The checklist must exist *before* cutover, because writing it under time pressure after cutover is how a required resource gets deleted. Operator-visible consequence: with the checklist prepared, retiring the web-only alerts and the Playwright base image is a rehearsed procedure; without it, it is improvisation against production.

Siblings: [[DSK-11-01]] supplies the candidate rows; [[DSK-11-02]] supplies the per-capability `deprovision_candidate` answers; [[DSK-11-03]] supplies the approval template each candidate reuses; [[DSK-11-05]] supplies the health and cost evidence a candidate needs.

## Governing docs

- No canonical PRD/FRD/ADR is linked yet. Keep `docs_todo: true`; do not link a planned decision until it exists on `origin/dev`.
- Use the ticket Source of truth and governing area plan until a real reference can be added.

## Routing

- **Subagent**: `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` (read-only; it also drafts the approval text each candidate will need)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`) → `kanmer-docs` (`.grok/skills/kanmer-docs/SKILL.md`, only if the checklist becomes its own file)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** (`group_resource_list`, `monitor`, `containerapps`) only to confirm a candidate's exact resource id; Microsoft Learn (`microsoft_docs_search`) for what "disable or scale to zero" means per service
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`; gates are `plan` + `questions-resolved` to leave `preparing`, `proof` + `questions-resolved` to enter `done`. Call `get_doc_gates <this ticket id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Ordered implementation

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

## Verification

- [ ] `grep -n "do not execute before Phase 10 exit" docs/desktop/11-azure-disposition/` — expected: the banner is present.
- [ ] Reviewer cross-check of the candidate list against `docs/desktop/01-inventory-and-parity/azure-resource-register.md` — expected: identical sets.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exits 0.
- [ ] `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected: exits 0.
- [ ] `git diff --stat` — expected: only files under `docs/desktop/11-azure-disposition/`; nothing under `infra/`, `src/` or `scripts/`.

## Risks and constraints

- **Azure**: no write, and specifically no deletion, disable or scale-to-zero. Reads are free (`docs/runbook.md` § Live-operation approval matrix); retirement is the matrix's "Deploy, restore, fail over, or retire" row and needs explicit operation approval for the exact target, fresh inventory, a rollback path and retained source data. **Nothing is deprovisioned before cutover, observed non-use and rollback approval** — this ticket prepares the procedure and executes none of it.
- **Scope boundary**: documentation under `docs/desktop/11-azure-disposition/` plus at most a cross-reference line in `docs/boundaries.md`. Do not edit `infra/`, `src/`, monitoring definitions or CI.
- **Traps** (plan § 7): telemetry blind spots (PLAT-034) make "is it still used?" unanswerable from App Insights for most of the day; out-of-band resources will not be removed by `azd provision` and must be listed separately if any were found by [[DSK-11-01]]; stale current-state docs are refreshed in the same task as any change.
- **Board placement**: this plan area seeds into `platform-operations` because the board-shape table in `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape assigns no area to plan 11.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document (`n/a — docs-only` for documentation-only tickets).

## Simplification pass

Before the PR, independently assess the branch diff for unnecessary abstractions, duplicated policy, and scope expansion; record findings and dispositions here.
