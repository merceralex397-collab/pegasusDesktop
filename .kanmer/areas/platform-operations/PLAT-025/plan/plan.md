# Plan — PLAT-025

## Objective

Make "refresh the Azure resource register and the cloud-dependency records" a required step of every release: add it to `.agents/skills/pegasus-release/SKILL.md` § 11 (the existing current-state refresh) and to the desktop runbooks R2, R8 and R9 in `docs/desktop/09-release-update-and-distribution/runbooks.md`, then perform the first refresh so the rule is proven rather than merely written.

## Chosen approach

The register is only useful while it is current, and proposal §27 item 15 makes "runtime Azure dependencies match the approved cloud-boundary register" an acceptance criterion measured at the end of the programme, not once at the start. The release skill already carries the principle — "§ 11 Refresh the current-state docs — the release is not finished without this" — but names only `docs/current-architecture.md` and `docs/operations.md`. Operator-visible consequence: without this step the register silently rots and the Phase 10 deprovision decisions in [[DSK-11-08]] rest on stale evidence, which is exactly the failure `docs/open-decisions.md` § Azure ownership and retirement targets warns about ("dated names are not current identity proof").

Siblings: [[DSK-11-01]] creates the register this keeps current; [[DSK-11-02]] creates the dependency records; [[DSK-11-06]] is the first release that must follow the new step; [[DSK-11-08]] consumes the result.

## Governing docs

- No canonical PRD/FRD/ADR is linked yet. Keep `docs_todo: true`; do not link a planned decision until it exists on `origin/dev`.
- Use the ticket Source of truth and governing area plan until a real reference can be added.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml` (it owns the release route and the desktop runbooks)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `pegasus-release` (`.agents/skills/pegasus-release/SKILL.md`, the file being edited) → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** (`group_resource_list`, `containerapps`) for the first refresh only
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`; gates are `plan` + `questions-resolved` to leave `preparing`, `proof` + `questions-resolved` to enter `done`. Call `get_doc_gates <this ticket id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Ordered implementation

1. Orientation. Read `docs/desktop/11-azure-disposition/README.md` § 4 and § 8, `.agents/skills/pegasus-release/SKILL.md` § 11, and `docs/desktop/09-release-update-and-distribution/runbooks.md` R2, R8 and R9. Then `get_doc_gates <this ticket id>` and `take_ticket <this ticket id>`.
2. Draft the step text once and reuse it verbatim in all four places, so the rule cannot drift: *"Refresh the Azure resource register (`docs/desktop/01-inventory-and-parity/azure-resource-register.md`) and the cloud-dependency records: re-run the read-only verification procedure for any resource this release touched, update its row and the date, and record any Bicep-versus-live drift. Non-Azure dependencies — the UNC feed host (D-003) and the signing certificate expiry (D-002) — are checked in the same pass."*
3. Edit `.agents/skills/pegasus-release/SKILL.md` § 11: add the register and dependency-records bullet beside the existing `docs/current-architecture.md` and `docs/operations.md` bullets, keeping the section's "in the same task, before it merges" framing and its terse imperative style.
4. Add the same step to `docs/desktop/09-release-update-and-distribution/runbooks.md` § R2 Desktop production release (`:84`) as a numbered post-release step.
5. Add it to § R8 Gateway release coordination (`:272`), which is the runbook that fires whenever an Azure-visible gateway change ships — including [[DSK-11-06]].
6. Add the non-Azure half to § R9 Feed hosting operations (`:290`): the UNC feed host is a dependency of the desktop even though it is not an Azure resource, so the refresh records its availability and path, and R5's certificate expiry date.
7. Add the reciprocal pointer in `docs/desktop/11-azure-disposition/README.md` § 4, naming the release skill and the three runbooks as the enforcement points, so the register's own document says who keeps it current.
8. Perform the **first refresh** so the rule is proven, not just written: re-run the register's read-only verification procedure (Azure MCP `group_resource_list` for `rg-pegasus-prod`, then `containerapps` show for `pegasus-prod-web-252ow37gij`), update the affected rows and stamp the date. Attach the outputs with `append_scratch`.
9. Check for the drift the rule is meant to catch, and record whatever you find rather than fixing it here: `docs/operations.md:295` narrates "release 14" while its own release table at `:311-332` is current — the register file already flags this, so confirm the flag still matches reality.
10. Verify the wording is identical everywhere: `grep -rn "Refresh the Azure resource register" .agents/skills/pegasus-release/SKILL.md docs/desktop/09-release-update-and-distribution/runbooks.md docs/desktop/11-azure-disposition/README.md` — expect four hits with the same sentence.
11. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`; both must exit 0. Note that `.agents/` is excluded from the link checker's scan, so verify the skill file's links by reading them.
12. Simplification pass (`AGENTS.md` step 4, `n/a — docs-only`), write `proof` as a `command-log` with the first-refresh outputs, and hand to `pegasus-desktop-reviewer`, who reviews the skill and runbook diff.

## Verification

- [ ] `grep -rn "Refresh the Azure resource register" .agents/skills/pegasus-release/SKILL.md docs/desktop/09-release-update-and-distribution/runbooks.md docs/desktop/11-azure-disposition/README.md` — expected: four hits, identical sentence.
- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exits 0.
- [ ] `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected: exits 0.
- [ ] `git diff --stat` — expected: `.agents/skills/pegasus-release/SKILL.md`, `docs/desktop/09-release-update-and-distribution/runbooks.md`, `docs/desktop/11-azure-disposition/README.md` and `docs/desktop/01-inventory-and-parity/azure-resource-register.md`; nothing under `src/`, `infra/` or `scripts/`.
- [ ] Reviewer diff review recorded in the ticket — expected: the step reads as an obligation, not a suggestion.

## Risks and constraints

- **Azure**: no write. The first refresh uses read-only calls, which are permitted with no per-target approval (`docs/runbook.md` § Live-operation approval matrix). Any write encountered while refreshing is a marked ⚠ Azure write needing exact-target approval and belongs in `docs/desktop/11-azure-disposition/README.md` § Conditional Azure writes. **Nothing is deprovisioned before cutover, observed non-use and rollback approval.**
- **Scope boundary**: the release skill, the desktop runbooks, the area-11 plan and the register. Do not change the release *procedure* itself (image build, provisioning, worker deployment) and do not touch `src/`, `infra/` or `scripts/`.
- **Traps** (plan § 7): stale current-state docs are the specific failure this ticket exists to prevent — refresh in the same task as any write; dated names are not current identity proof, so the refresh re-reads rather than assumes; out-of-band configuration (such as the Log Analytics daily cap) will only ever be caught by this refresh, since `azd provision` cannot see it.
- **Board placement**: this plan area seeds into `platform-operations` because the board-shape table in `docs/desktop/00-governance-and-workflow/README.md` § Kanmer board shape assigns no area to plan 11.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document (`n/a — docs-only` for documentation-only tickets).

## Simplification pass

Before the PR, independently assess the branch diff for unnecessary abstractions, duplicated policy, and scope expansion; record findings and dispositions here.
