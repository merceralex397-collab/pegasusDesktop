---
id: PLAT-012
type: ticket
title: >-
  DSK-10-12 · Performance review checklist wired into the reviewer agent and the
  PR template
status: backlog
area: platform-operations
assignee: ''
profile: chore
labels:
  - desktop-conversion
  - plan-10
  - phase-1
  - tier-1
groups:
  - EPIC-011
  - HZN-002
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:10:26.646Z'
updated: '2026-08-24T08:10:26.646Z'
---

## What

Turn proposal §15.2 into a concrete performance review checklist, wire it into the `pegasus-desktop-reviewer` agent instructions and the pull-request template, and prove it by reviewing the first three vertical slices against it.

## Why

Proposal §15.2 `:1076-1095` lists seventeen implementation practices — compiled bindings, virtualization, paging, lazy loading, decode-to-size, prompt disposal, off-UI-thread work, cancellation propagation, no synchronous waits, coalesced refresh, no duplicate event subscriptions, dispatcher-free view models, profile-before-preload, one `IHttpClientFactory` pipeline, JSON-only compression, no reflection-heavy mapping, bounded asynchronous logging. Every one of them is cheap at review time and expensive to retrofit; the plan's risk table names "memory growth from image/document views and event subscriptions" as the recurring failure. Operator-visible consequence: budgets from [[DSK-10-10]] are missed by accumulation, and each individual cause is too small to justify its own remediation ticket. Siblings: [[DSK-10-11]] (how to measure), [[DSK-10-13]] (the release report).

## Source of truth

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-12`
- Plan detail: same file § 1 (§15.2 "practices become a review checklist"), § 6 (routing), § 7
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 15.2 Implementation practices `:1076-1095`; § 20.6 Review protocol `:1416-1430`; § 25 Ticket structure `:1932-1954`
- Repository evidence:
  - `.codex/agents/pegasus-desktop-reviewer.toml` — the reviewer agent whose `developer_instructions` this checklist is added to
  - `.codex/skills/winui-code-review/SKILL.md` — the vendored performance checklist this one extends rather than replaces (win-dev-skills v0.5.0 `f1028dd5`)
  - `AGENTS.md` § Repository task workflow step 5 — review by an agent that did not implement
  - `docs/desktop/12-agent-tooling/skill-routing.md` § Work type routing, row "Performance work" — `winui-code-review` (performance checklist) with `pegasus-ui-verifier` measuring and `winui-dev` fixing
  - New: the desktop projects from `DSK-02-05`/`DSK-02-06`
- Binding decisions:
  - **L-04** — every ticket names its subagent, skills and MCP tools; the checklist is enforced through the reviewer agent, not through goodwill.
  - **ADR-0110** (to be authored) — agent-skill pinning and the invocation/review protocol.
- Depends on: `DSK-02-05` (desktop project scaffold), `DSK-02-06` (`Pegasus.Desktop.Infrastructure`, the single `IHttpClientFactory` pipeline the checklist asserts on).

## Routing

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-code-review` (`.codex/skills/winui-code-review/SKILL.md`; win-dev-skills v0.5.0 `f1028dd5`) → `analyzing-dotnet-performance` (dotnet/skills `98f84851`, plugin `dotnet-diag`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `get_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
- **Reviewer**: `pegasus-desktop-reviewer` reviews desktop PRs against this checklist; **this** ticket's own PR is reviewed by an agent that did not implement it (`AGENTS.md` § Repository task workflow step 5).

## Implementation steps

1. Orientation. Read the plan row, proposal `:1076-1095`, and the existing performance section of `.codex/skills/winui-code-review/SKILL.md` so the new checklist extends it rather than duplicating it. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.
2. Branch `task/dsk-10-12-performance-review-checklist` from `dev`.
3. Create `docs/desktop/10-security-observability-performance/performance-review-checklist.md`. Write one checklist item per §15.2 practice, each phrased as a question a reviewer can answer from the diff, with what "pass" looks like and the grep or file to check. For example: "Are all new bindings `x:Bind`? — search the diff for `{Binding` in `src/Pegasus.Desktop/**/*.xaml`; a `{Binding` needs a stated reason."
4. Include, verbatim as separate items: compiled XAML bindings; list and grid virtualization; server-side paging on every collection; lazy loading of case sections and large document metadata; decode images to display size; bounded thumbnail and reference-data caches; prompt disposal of streams and image sources; network, parsing, document and image work off the UI thread; cancellation tokens propagated; no synchronous waits on asynchronous code; coalesced refresh requests; no duplicate event subscriptions on navigation; view models independent of `DispatcherQueue`; startup profiled before any preloading is enabled; one shared `IHttpClientFactory` pipeline; compression for JSON only, not for already-compressed images or PDFs; no reflection-heavy mapping without a stated justification; asynchronous and bounded local log writing.
5. Add the budget cross-reference: each item names the §15.1 budget row it protects (for example virtualization and paging protect “First page of ordinary server results ≤ 1 second” and “List scrolling”; disposal and decode-to-size protect “Typical steady memory below 500 MB”; off-UI-thread work protects “Cached page navigation ≤ 200 ms”).
6. Add three items that are checkable mechanically and say how: an architecture test that `HttpClient` is never constructed directly outside the pipeline type; a test that no view model references `DispatcherQueue`; a grep gate for `.Result`/`.Wait()`/`GetAwaiter().GetResult()` in desktop projects. Where `DSK-02-12` already added a dependency-direction test, extend it rather than creating a second one.
7. Wire the checklist into `.codex/agents/pegasus-desktop-reviewer.toml`: add to its `developer_instructions` that every desktop PR is reviewed against `docs/desktop/10-security-observability-performance/performance-review-checklist.md` and that findings are recorded per item as pass / not-applicable / finding — never silently skipped. Keep the TOML fields to the documented set (`name`, `description`, `developer_instructions`, and the optional `model`, `model_reasoning_effort`, `sandbox_mode`, `mcp_servers`, `skills.config`).
8. Add the checklist reference to the pull-request template. If `.github/pull_request_template.md` does not exist, create it with the minimum the repository already expects — the simplification-pass reference (`AGENTS.md` step 4), the independent-review reference (step 5) and a line for desktop PRs pointing at this checklist. Do not invent additional process.
9. Prove it: run the checklist against the first three vertical slices (`DSK-05-01`, `DSK-05-02`, `DSK-05-03`) once they exist, and file the per-item results in each slice ticket's review record via `set_ticket_doc`. Any finding becomes its own `fix` ticket; do not fix code under this `chore`.
10. Run `pwsh ./scripts/Test-DocumentationLinks.ps1` and `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`; both must exit 0.
11. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to an agent that did not implement.

## Acceptance criteria

- [ ] Every one of the §15.2 practices is a separate checklist item with a pass definition and a way to check it from the diff.
- [ ] Each item names the §15.1 budget row it protects.
- [ ] At least three items are enforced mechanically (architecture test or grep gate) rather than by reading.
- [ ] `.codex/agents/pegasus-desktop-reviewer.toml` instructs the reviewer to use the checklist and to record pass / not-applicable / finding per item.
- [ ] The PR template points desktop PRs at the checklist.
- [ ] The first three slices have recorded per-item review results.

## Verification

- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exit 0.
- [ ] `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release` — expected: the new mechanical checks pass, and fail when a direct `HttpClient` construction is introduced in a temporary fixture.
- [ ] Kanmer `get_ticket_doc` on `DSK-05-01`, `DSK-05-02`, `DSK-05-03` — expected: each carries the per-item checklist result.

## Evidence tier

Tier 1 — Static/build/architecture. Here that obliges the mechanical items to be real compiled checks and the rest to be a recorded review outcome; it proves consistency only, so the measured evidence stays with [[DSK-10-13]].

## Documentation changes

- `docs/desktop/10-security-observability-performance/performance-review-checklist.md` — new file.
- `.github/pull_request_template.md` — desktop PR reference (created only if absent).
- `docs/engineering.md` — a one-line reference to the checklist from the review section, as the plan's documentation-changes list requires.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `docs/desktop/`, `.codex/agents/pegasus-desktop-reviewer.toml`, `.github/pull_request_template.md`, `docs/engineering.md`, and `tests/Pegasus.ArchitectureTests` for the mechanical checks. Must not change desktop application code — findings become separate tickets. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: a checklist nobody is instructed to run is decoration — the reviewer TOML edit is the load-bearing step; duplicating the vendored `winui-code-review` performance section creates two lists that drift, so extend and cross-reference; a checklist item with no pass definition produces reviewer disagreement rather than a finding; `winui-wpf-migration` and `winui-session-report` are on the do-not-load table and must not appear in the reviewer's skill list.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Outcome

_Filled at closeout._
