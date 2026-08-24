---
id: FEAT-024
type: ticket
title: DSK-05-24 · Retire `CaseMutationPageModel` state machine for desktop paths
status: preparing
area: desktop-features
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:31:38.832Z'
labels:
  - desktop-conversion
  - plan-05
  - phase-4
  - tier-1
  - tier-7
groups:
  - EPIC-006
  - HZN-005
links: []
refs:
  - docs/frd/frd-01-case-identity-and-lifecycle.md
docs_todo: true
archived: false
created: '2026-08-24T08:02:19.044Z'
updated: '2026-08-24T21:31:38.832Z'
---

## What

Prove and enforce that the desktop edit path carries none of the web's mutation state machine: no `TempData` equivalent, no PRG, no retained-proposed-value budgets and no `RetainableFormFields` allow-list — desktop edit state is in-memory view-model state plus the server lease and version, and an architecture test keeps it that way.

## Why

`src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` (339 lines) retains proposed values in cookie `TempData` with `MaximumRetainedProposedCharacters = 8000`, `MaximumRetainedProposedValueCharacters = 2000` and a `RetainableFormFields` allow-list of about thirty names (`:36-80`). Those exist because HTTP POST-redirect-GET loses form state; the desktop has no such problem. The reuse map marks the type REPLACE — the web keeps it until cutover, the desktop must never grow an equivalent. Without an enforced rule, an agent implementing a later editing slice will reinvent a retention budget by analogy. Siblings: [[DSK-05-05]] and [[DSK-05-08]] establish the desktop edit and recovery model this ticket locks in; [[DSK-05-26]] deletes the web type after cutover.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-24`
- Plan detail: `docs/desktop/05-implementation-and-migration/reuse-map.md` § `Pegasus.Web — REPLACE pages, KEEP the host` (the `CaseMutationPageModel.cs` row); § 3 of `README.md` ("Characterization before moving any rule" — TempData-retained proposed values, PRG and antiforgery are deliberately **not** preserved)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `Case workspace` (dirty state and deliberate save)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 11.1 What may be cached locally, § 11.2 What should not become a local database initially, § 14.5 Case workspace
- Repository evidence: `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:36-80` (the two budgets and the `RetainableFormFields` allow-list), the 65 `RedirectToPage` calls across 27 page models and `TempData` use in 29 page models recorded in § 2 of the area plan; `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs` (520 lines, the reflection-based fact style to extend)
- Binding decisions: L-01 the server lease and version are the authority for edit safety; L-04 routing named on the ticket
- Depends on: `DSK-05-05` the desktop edit session with lease, version and dirty state; `DSK-05-08` the conflict-and-recovery pattern that replaces retained proposed values

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml` (independent review of the boundary)
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-code-review` (`.codex/skills/winui-code-review/SKILL.md`) → `run-tests` (dotnet/skills `98f84851`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; `chore` needs `plan` and `questions-resolved` to leave Preparing and `proof` to enter Done)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, the `reuse-map.md` `CaseMutationPageModel.cs` row and § 3 of the area plan. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-24-retire-mutation-state` and worktree `../pegasus-worktrees/dsk-05-24-retire-mutation-state` from `origin/dev`.
2. Read `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` in full and write, in the plan, a two-column table: each web mechanism (cookie `TempData` retention, the 8000/2000-character budgets, the `RetainableFormFields` allow-list, the PRG redirect, the antiforgery token, `TempData["CaseDetailsStatus"]`-style status passing) against the desktop equivalent that replaces it (view-model state, the navigation guard, the server lease and version, the bearer token, the `InfoBar` outcome). Record the SHA read.
3. Audit the merged desktop code for any equivalent that crept in: search `src/Pegasus.Desktop` and `src/Pegasus.Desktop.Infrastructure` for a retention character budget, a field allow-list, a redirect-style navigation after a save, or an outcome passed through navigation parameters instead of view-model state. Record every hit.
4. Remove each hit, replacing it with view-model state plus the server lease and version from [[DSK-05-05]] and the recovery pattern from [[DSK-05-08]]. Where an unsaved-draft need is genuine, it uses the encrypted local draft from [[DSK-02-06]] with an explicit, documented lifetime — not a character budget copied from the web.
5. Extend `tests/Pegasus.ArchitectureTests` with reflection-based facts, in the style of `DependencyDirectionTests.cs`, asserting that no type in `Pegasus.Desktop` or `Pegasus.Desktop.Infrastructure`: references an ASP.NET `TempData`/`ViewData` type; declares a member whose name matches a retained-proposed-value budget pattern; or declares a field allow-list constant of the `RetainableFormFields` shape. Each fact fails with a message naming the offending type and pointing at this ticket.
6. Add a view-model test in `tests/Pegasus.Desktop.ViewModelTests` proving the intended behaviour positively: after a failed save, the proposed values are still present in the view model (not re-fetched, not truncated), and after a deliberate discard they are gone.
7. Add a view-model test proving a save outcome is rendered from view-model state, not carried across a navigation — the desktop has no PRG, so navigating away and back re-queries rather than restoring a message.
8. Confirm the web is untouched: `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` and all 27 PRG page models keep working exactly as before. Deletion is [[DSK-05-26]]'s job after cutover, not this ticket's.
9. Record the rule in `docs/frd/frd-13-desktop-operator-experience.md` so a later slice author reads it before inventing a retention budget, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] No desktop type carries a `TempData`/`ViewData` reference, a retained-proposed-value budget or a `RetainableFormFields`-shaped allow-list.
- [ ] Architecture facts fail with an actionable message if one is added later.
- [ ] Desktop edit state lives in the view model plus the server lease and version; a genuine draft need uses the encrypted local draft with a documented lifetime.
- [ ] A failed save keeps the operator's proposed values in the view model, untruncated.
- [ ] A save outcome is rendered from view-model state, never carried across a navigation.
- [ ] `src/Pegasus.Web` behaviour is unchanged; nothing in the web mutation path was deleted.

## Verification

- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build` — expected: the new no-TempData, no-budget and no-allow-list facts pass, and existing dependency-direction facts stay green.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: the failed-save retention and no-PRG-outcome facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: existing web mutation tests are unchanged and green.
- [ ] Deliberate-regression check recorded in the proof — expected: temporarily adding a budget constant to a desktop type fails the new architecture fact with the actionable message; the change is reverted before commit.

## Evidence tier

Tier 1 — Static/build/architecture. Tier 7 — Browser/accessibility.
Tier 1 obliges compiling the approved projects and enforcing dependency direction and one policy owner — this proves consistency only, which is exactly what this rule needs; tier 7 obliges evidence from a real run that the edit and error behaviour the rule protects actually holds.

## Documentation changes

- `docs/frd/frd-13-desktop-operator-experience.md` — record the desktop edit-state rule and its rationale
- `docs/desktop/05-implementation-and-migration/reuse-map.md` — mark the `CaseMutationPageModel.cs` row as enforced for desktop paths

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `tests/Pegasus.ArchitectureTests` and `tests/Pegasus.Desktop.ViewModelTests`. Must not modify or delete `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` or any Razor page — the web keeps its state machine until [[DSK-05-26]].
- **Traps**: do not reproduce web mechanics by analogy — the budgets exist only because POST-redirect-GET loses form state; an architecture test that cannot name the offending type is not useful, so assert with actionable failure messages; local drafts are bounded and encrypted (proposal §11.1) and are not a substitute for the server lease.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
