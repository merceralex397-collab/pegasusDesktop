---
id: FEAT-036
type: ticket
title: >-
  DSK-07-10 · Desktop vehicle workflow: VRM validation, request, accept,
  provenance display and the provider-contract check
status: backlog
area: desktop-features
assignee: ''
profile: feature
labels:
  - desktop-conversion
  - plan-07
  - phase-6
  - tier-7
  - needs-operator
groups:
  - EPIC-008
  - HZN-007
links: []
refs:
  - docs/frd/frd-06-vehicle-and-engineering-evidence.md
docs_todo: true
archived: false
created: '2026-08-24T08:24:13.928Z'
updated: '2026-08-24T08:24:13.928Z'
---

## What

Build the Vehicle tab in `src/Pegasus.Desktop`: registration normalisation and validation before the call, Request lookup and Accept suggestion commands, and a display that shows each value's source and timestamp with provider states (`stale`, `partial`, `not found`, `throttled`, `unavailable`, `failed`) kept visibly distinct — plus a recorded check of the DVLA/DVSA provider contract confirming that no direct desktop call is permitted.

## Why

Proposal § 13.5 requires vehicle registration data, lookups, mileage history, and "source and timestamp of external data"; § 16.2 requires the desktop to show when data is cached and when it was obtained, and that a failed external lookup must not corrupt the case. § 12.3 permits a direct desktop provider call **only** if the API is explicitly designed for public/native clients and needs no privileged secret, and says that must be proven from the provider contract rather than assumed — this area's § 2 records it as an assumption and names this ticket as the check. Siblings: [[DSK-07-09]] supplies the endpoints and the provenance fields; [[DSK-05-15]] is the case-workspace slice that hosts this tab.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-10`
- Plan context: `docs/desktop/07-integrations/README.md` § 2 Assumptions (the DVLA/DVSA provider-contract assumption), § 4 Target state (third bullet)
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.5 Vehicle and inspection information — Case workspace › Vehicle tab` (AutomationIds `Case.Vehicle.Lookup`, `Case.Vehicle.Suggestion.Accept.<Key>`, `Case.Vehicle.Address.Mode`, `Case.Vehicle.Eva.Generate`)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.3 DVLA/DVSA, § 13.5 Vehicle and inspection information, § 16.2 External provider resilience
- Repository evidence: `src/Pegasus.Core/Vehicle/LookupContracts.cs:20-37` (the single normalisation rule the client must reuse, not re-implement), `:56-80` (`VehicleLookupResult` provenance fields and `SourceAge`); `src/Pegasus.Core/Vehicle/VehicleWorkflow.cs:55-130` (`ConfirmedVehicleField<T>`, `ConfirmedVehicleEvidence`, `VehicleLookupAvailability`); `src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs:87` (`OnPostGenerateEvaHandoffAsync`); `docs/current-architecture.md:514`
- Binding decisions: L-01 — every value arrives through `/api/v1`. **ADR-0107** — no DVLA/DVSA key in the desktop package; the desktop asks the gateway. L-04 — routing named on this ticket.
- Depends on: `DSK-07-09` the vehicle endpoints and provenance contract; `DSK-06-13` the adopted screen specs; `DSK-06-11` the provenance glyph; `DSK-06-06` the `StatusChip` control

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; verification by `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`) → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for WinUI input validation and `AutomationProperties` naming)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, the Vehicle screen spec, `docs/frd/frd-06-vehicle-and-engineering-evidence.md`, and the contracts published by [[DSK-07-09]]. Call `get_doc_gates <this ticket id>`, then `take_ticket` on branch `task/dsk-07-10-vehicle-workflow`.
2. **Operator step** — the provider-contract check. The operator reads the current DVLA Vehicle Enquiry Service and DVSA MOT History API terms and hands back, in writing: whether either API is designed for public/native clients, whether either can be called without a privileged secret, and the date and document version read. Record the answer verbatim in the ticket's `research` document. Until it is recorded, the assumption in this area's § 2 stands and the desktop calls only the gateway.
3. If — and only if — the operator's answer says a direct native call is permitted without a privileged secret, do **not** implement it here: raise a follow-up ticket in area 07 and record why. Placement stays as ADR-0107 has it for this ticket.
4. Regenerate the API client with `pwsh ./eng/api/Generate-ApiClient.ps1` and confirm the vehicle contracts carry `outcome`, `provider`, `providerVersion`, `retrievedAtUtc`, `sourceObservedAtUtc` and `failure`.
5. Add `CaseVehicleViewModel` to `src/Pegasus.Desktop` using `ObservableObject`, `[ObservableProperty]` partial properties and `[RelayCommand]`. Reuse the shared normalisation rule rather than writing a second one: uppercase, strip whitespace, ASCII letters and digits only, maximum 20 characters — the same rule `VehicleLookupRequest` enforces at `LookupContracts.cs:22-33`. If the shared rule is not yet reachable from the desktop, take it from `src/Pegasus.Contracts` as [[DSK-05-23]] relocates shared vocabulary; do not copy the regex into the view model.
6. Build `CaseVehicleView.xaml` from the screen spec: the VRM field with inline validation placed per [[DSK-06-08]], make/model/colour/year rows each carrying a provenance glyph ([[DSK-06-11]]) and a source-and-age chip, MOT and mileage observations classified supplied/external/estimated, suggestion rows with an Accept command keyed `Case.Vehicle.Suggestion.Accept.<Key>`, and the `Case.Vehicle.Lookup` command.
7. Render the seven provider outcomes as distinct, named states using `StatusChip` from [[DSK-06-06]] — `current`, `stale`, `partial`, `not found`, `throttled`, `unavailable`, `failed`. Text plus colour, never colour alone (`docs/desktop/06-ui-design/keyboard-and-accessibility.md`). "Not found" and "unavailable" must never share a presentation.
8. Show cache honesty: every externally sourced value displays where it came from and when it was obtained, computed from `retrievedAtUtc` / `sourceObservedAtUtc`. A stale value stays visible and labelled rather than being hidden or silently refreshed.
9. Protect staff confirmation: an accepted suggestion is never overwritten by a later refresh (`VehicleWorkflow.cs` confirmation rules). A refresh that would change a confirmed field surfaces it as a suggestion, not as a change.
10. Make failure non-destructive: a failed lookup leaves the case data untouched and shows the failure sentence with a copyable Reference through the shared problem presentation from [[DSK-06-10]]. Assert it — proposal § 16.2 makes "a failed external lookup must not corrupt the case" a rule, not a preference.
11. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests`: normalisation of mixed-case and spaced input; rejection of an over-long or non-alphanumeric registration before any call; each of the seven outcomes producing a distinct state; a failed lookup leaving prior values intact; a confirmed field surviving a refresh; the request command disabled when `requestsEnabled` is false, with the mode named.
12. Build and launch with `.\BuildAndRun.ps1` from the `winui-dev-workflow` skill (async mode, capture the PID) and run a `winapp ui` batch script per `winui-ui-testing` covering: type a lowercase registration and confirm it normalises; request a lookup; accept a suggestion by keyboard; assert the `unavailable` and `not found` states differ in text; capture screenshots of each state.
13. Run the accessibility scan from [[DSK-06-15]] over the tab and attach the report. Then run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the plan document, and open the PR into `dev`.

## Acceptance criteria

- [ ] Registration input is normalised and validated once, using the shared Core rule rather than a second copy.
- [ ] All seven provider outcomes render as distinct named states with text as well as colour.
- [ ] Every externally sourced value shows its source and the time it was obtained; stale values stay visible and labelled.
- [ ] A failed lookup leaves case data unchanged and shows an operator sentence with a copyable Reference.
- [ ] A staff-confirmed value is never overwritten by a refresh.
- [ ] The DVLA/DVSA provider-contract check is recorded in writing with its date; no direct provider call is made from the desktop.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` — expected: normalisation, seven-state, non-destructive-failure and confirmation-protection facts pass.
- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/ui-tests.ps1 -AppPid <pid> -Script vehicle` — expected: request, accept, keyboard traversal and distinct-state assertions pass; screenshots attached.
- [ ] `grep -rn "driver-vehicle-licensing\|history.mot.api\|x-api-key" src/Pegasus.Desktop src/Pegasus.Desktop.Infrastructure` — expected: no matches.
- [ ] `get_ticket_doc <this ticket id> research` — expected: the provider-contract check with its date and source is present.

## Evidence tier

Tier 7 — Browser/accessibility (desktop equivalent: real authenticated workflow, keyboard, focus and error behaviour, semantic labels, text-plus-colour states).
Tier 7 obliges a real run against the gateway with the keyboard walk recorded; an automated scan does not replace it.

## Documentation changes

- `docs/frd/frd-13-desktop-operator-experience.md` — Vehicle tab section
- `docs/frd/frd-06-vehicle-and-engineering-evidence.md` — desktop provenance-display clause
- `docs/desktop/07-integrations/README.md` § 2 Assumptions — the DVLA/DVSA assumption is replaced by the recorded check result

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `tests/Pegasus.Desktop.ViewModelTests`, `tests/Pegasus.Desktop.UITests`. Must not add endpoints (that is [[DSK-07-09]]), must not reference `src/Pegasus.Infrastructure/Vehicle/`, must not contain a provider base URI or key.
- **Traps**: ADR-0107 — no provider key in the package; a second registration-normalisation rule in the client is duplication under `AGENTS.md` § Simplicity rails; provider failure must stay distinguishable from "not found"; the release-15 automatic sweep means a case may already hold lookup evidence the operator did not request — show its provenance rather than presenting it as staff-entered; operator copy rules apply (`docs/design/README.md`).
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
