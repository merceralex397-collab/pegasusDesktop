---
id: FEAT-008
type: ticket
title: 'DSK-05-08 · S8 Concurrency UX (conflict, lease lost, replay)'
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-05
  - phase-4
  - tier-5
  - tier-7
  - tier-12
  - needs-operator
groups:
  - EPIC-006
  - HZN-005
links: []
blocks:
  - FEAT-022
  - FEAT-024
  - FEAT-025
  - TEST-016
refs:
  - docs/frd/frd-01-case-identity-and-lifecycle.md
docs_todo: true
archived: false
created: '2026-08-24T07:49:10.219Z'
updated: '2026-08-24T12:33:53.569Z'
---

## What

Build the one conflict-and-recovery pattern every desktop editor reuses: when another user changed the record, the edit lease was lost, or a command was replayed, the operator is told what happened and can reload, compare and deliberately reapply — and an uncertain outcome is resolved by re-query, never by a blind retry.

## Why

Proposal §10.4 and §16.1 require detected concurrency and an operation model where uncertain outcomes are resolved deterministically. The web surfaces `CaseVersionConflictException` and `CaseOperationConflictException` through page errors and retained proposed values (`src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:36-80`) — a mechanism the desktop does not have and must not imitate. Without one shared pattern each slice would invent its own conflict copy, which the design authority forbids. The Phase 4 exit gate requires the two-user conflict test to pass and no silent overwrite. Upstream KANMER-005 records the case this pattern must render correctly and cannot itself prove: an Automation Actor held the lease, staff took it, and the actor's later release was rejected. Proving that exclusion is [[DSK-03-08]]'s job, not this ticket's — this ticket renders whichever outcome its two named facts establish. Siblings: [[DSK-05-05]] surfaces the raw states, [[DSK-06-10]] owns the problem presentation control, [[DSK-03-08]] owns the lease commands and their cross-actor evidence, and every later editing slice consumes this pattern.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-08`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S8 · Concurrency UX (DSK-05-08)` and § `Common to every slice` (concurrency and idempotency)
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Cases` (409 problems carrying the current version; lease conflict carrying the holder)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `Cross-cutting state contract`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 10.4 Concurrency, § 16.1 Operation model, § 14.8 Notifications and errors
- Upstream carry-over: **upstream KANMER-005** *Enforce exclusive editing leases between staff and Automation Actors* — **absorbed, not imported: there is no fork ticket for it**, and **[[DSK-03-08]] is its single owner**. That ticket's step 12 adds the two facts and its acceptance criterion states the rule; this ticket consumes both and asserts neither for itself. The two facts, restated verbatim from [[DSK-03-08]] step 12 so they cannot drift: **fact one — Automation holds, staff competes**: with an Automation Actor holding an unexpired lease, a staff `POST /cases/{id}/lease/claim` returns the `lease-conflict` problem, the retained holder is unchanged (`CaseWorkflowEntity.EditLeaseHolder` still the Automation subject id and `EditLeaseExpiresAtUtc` unmoved), a staff `PUT /cases/{id}` is refused at the write boundary, and the Automation Actor can still save and then release afterwards; **fact two — the mirror**: staff holds, an Automation Actor competes, with the same four assertions reversed. Its acceptance criterion is *"A competing claim never replaces an unexpired lease holder, in either actor direction — Automation holding against a staff claimant and staff holding against an Automation claimant — and the holder can still save and release after the rejected claim."*
- Repository evidence: `src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:125` (`CaseVersionConflictException`), `:322-334` (replay semantics documented on `ILeaseCaseForEdit`), `src/Pegasus.Core/Workflow/CaseEditAuthority.cs`, `:75-81` (`CaseEditAuthorityHolder` and `CaseEditAuthorityHolder.Automation` — the Automation Actor is a first-class holder and is disclosed as itself, which is what the lease-lost path must name), `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` (154 lines — the existing Core-exception-to-transport-error map the gateway problem types are ported from), `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:36-80`
- Binding decisions: L-01 the gateway returns typed problems, the desktop implements the recovery experience; L-02 the two-user scenario runs on the local Test/UAT stack; L-04 routing named on the ticket
- Depends on: `DSK-05-05` the lease, version and save session that produces these states

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` (problem types); `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`; `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S8 and § `Common to every slice`, and `docs/desktop/06-ui-design/screen-specs.md` § `Cross-cutting state contract`. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-08-concurrency-ux` and worktree `../pegasus-worktrees/dsk-05-08-concurrency-ux` from `origin/dev`.
2. Enumerate the problem types in `research` by reading `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` and the Core exceptions it maps: version conflict (must carry the current version), lease conflict (must carry the holder's display name via `src/Pegasus.Core/Actors/ActorDisplayNames.cs`), operation conflict, and replayed. Record the SHA read.
3. Confirm with [[DSK-03-02]] that the `/api/v1` problem-details mapping emits exactly those four types with the payload fields above, and that a replayed `operationKey` returns the original outcome rather than a new one. Add the missing fields to the gateway mapping where they are absent — the desktop cannot invent a version it was not given.
4. Add the problem DTOs to `src/Pegasus.Contracts` as a discriminated set keyed by problem type, so a view model matches on the type rather than parsing prose.
5. Implement `ConflictRecoveryService` in `src/Pegasus.Desktop.Infrastructure`: given a failed command and its problem, it re-queries the affected record, produces a field-level comparison of the operator's proposed values against the current server values, and returns a reapply plan the operator confirms. It never resends the original body unchanged.
6. Implement the reusable `ConflictRecoveryView` in `src/Pegasus.Desktop`: an `InfoBar` explaining what happened in settled operator vocabulary (the words `lease`, `projection`, `caller` and `correlation identifier` are banned from operator copy), a compare pane listing only the fields that differ, and explicit Reload, Keep mine and Cancel actions. A `ContentDialog` is used only where the decision genuinely interrupts.
7. Define the retry rule in code and in the FRD section: an idempotent command may be retried with the **same** `operationKey`; a non-idempotent command is never retried without the operator deciding to issue a fresh `operationKey`; an uncertain outcome after a timeout is resolved by re-querying the record, never by resending.
8. Implement the lease-lost path: the editor becomes read-only immediately, the current holder is named, and the operator is offered re-claim (which re-queries first) rather than a silent re-acquire. **Check first that [[DSK-03-08]]'s two cross-actor lease facts (its step 12) and its acceptance criterion "A competing claim never replaces an unexpired lease holder, in either actor direction" have landed and pass** — they are restated under Source of truth and are the only evidence on the board that the exclusion upstream KANMER-005 reports is closed. If they have landed and pass, build the lease-lost path to that behaviour: a rejected claim leaves the existing holder in place, so this screen shows *the holder is unchanged and the operator did not take the lease*, and it names an Automation Actor holder as itself through `CaseEditAuthorityHolder` (`CaseEditAuthority.cs:75-81`) in settled operator vocabulary. If either fact fails, the exclusion is live and unfixed: **stop and raise it on [[DSK-03-08]]** — do not model a takeover in this pattern, do not add a client-side guard, and do not claim parity around it.
9. Implement the replayed path: when the gateway reports a replay, show the original outcome and do not present it as a new success.
10. Add contract tests in `tests/Pegasus.Api.ContractTests` for each of the four problem types: shape, status code, and the presence of the current version or holder. Enable `Features:DesktopGateway` explicitly.
11. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` covering each problem type mapped to its state, the comparison producing only differing fields, refusal to retry a non-idempotent command without a fresh key, re-query on timeout, and the lease-conflict state naming an Automation Actor holder correctly without the operator's own identity being substituted.
12. **Operator step** — run the scripted two-user UAT scenario on the local Test/UAT stack: two operators edit the same case; the second is told about the conflict, compares, reapplies deliberately, and no value is lost. Capture the operator's sign-off text and date in the ticket proof, plus screenshots of each of the four states.
13. Add the conflict section to `docs/frd/frd-13-desktop-operator-experience.md`, note the pattern in `docs/desktop/01-inventory-and-parity/parity-matrix.md` as the shared recovery behaviour, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] One conflict-and-recovery pattern exists and is the only one any editor uses.
- [ ] A version conflict shows the current version and a field-level comparison limited to differing fields.
- [ ] A lost lease immediately makes the editor read-only and names the holder, including an Automation Actor holder named as itself; a rejected re-claim leaves the existing holder in place and the screen says so, matching [[DSK-03-08]]'s two cross-actor facts rather than asserting the exclusion here.
- [ ] A replayed command shows the original outcome and is not reported as a new success.
- [ ] A non-idempotent command is never retried without an operator decision; an uncertain outcome is resolved by re-query.
- [ ] No banned operator word appears in any conflict message.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: the four problem-type facts pass with the required payload fields.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: state mapping, comparison, retry-rule, re-query and Automation-holder-naming facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGatewayCaseCommandTests"` — expected: [[DSK-03-08]]'s two cross-actor lease facts pass, since this pattern is built to their outcome; a failure here is a blocker raised on that ticket, not worked around in this one.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -Script concurrency` — expected: the scripted two-user conflict passes against the gateway fixture without sleeps.
- [ ] UAT record in the ticket proof — expected: named operator sign-off with date plus screenshots of the four states.

## Evidence tier

Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility. Tier 12 — Integrated workflow.
Tier 5 obliges observable evidence that each problem type is produced by the real endpoint with correct exception translation; tier 7 obliges the two-session editing, error-behaviour and accessible validation-summary evidence; tier 12 obliges the end-to-end two-user run through Core and SQL with safe replay, not a mocked failure.

## Documentation changes

- `docs/frd/frd-13-desktop-operator-experience.md` — conflict and recovery section
- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — note recording the shared recovery pattern
- `docs/capabilities.md` — `DSK` row for concurrency recovery

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` problem-details mapping in `src/Pegasus.Web` and the test projects. Must not modify `CaseMutationPageModel.cs` or any Razor page. `src/Pegasus.Core/Workflow/CaseEditAuthority.cs` and `src/Pegasus.Infrastructure/Persistence/EfCaseWorkflowStore.cs` are **[[DSK-03-08]]'s** under its named conditional exception; this ticket reads them and changes neither.
- **Traps**: do not reproduce retained proposed values — the desktop compares against a fresh server read instead; banned words include `lease`, so operator copy must use settled vocabulary; a blind retry of a non-idempotent command is a defect, not a convenience; `Features:DesktopGateway` must be enabled in tests. **Upstream KANMER-005 (exclusive leases between staff and Automation Actors) is owned by [[DSK-03-08]], not absorbed here**: its step 12 adds the two named cross-actor facts — Automation-holds/staff-competes and staff-holds/Automation-competes, each asserting the retained holder is unchanged, the competing write refused, and the holder still able to save and release — and its acceptance criterion is "A competing claim never replaces an unexpired lease holder, in either actor direction". Those facts are the evidence; this ticket renders their outcome and asserts nothing of its own. Reading the trap as "confirm it is implemented on the endpoint" is what this amendment replaced: that was a verification obligation pointing at nothing named. If a fact fails, the correct action is to block on [[DSK-03-08]] under its named Core scope exception — never a client-side workaround, a modelled takeover, or a parity claim over an unproved exclusion. **Upstream ids and fork board ids do not match**: upstream KANMER-005 has no fork ticket at all, so never write it as a board wiki-link.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
