---
id: TEST-008
type: ticket
title: >-
  DSK-08-08 · UI scripts: document upload, vehicle lookup, report preview and
  finalize
status: preparing
area: testing
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:34:14.315Z'
labels:
  - desktop-conversion
  - plan-08
  - phase-7
  - tier-7
groups:
  - EPIC-009
  - HZN-008
links: []
docs_todo: true
archived: false
created: '2026-08-24T07:48:40.552Z'
updated: '2026-08-24T21:34:14.315Z'
---

## What

Add the three remaining critical-path `winapp ui` scripts: a document upload driven through the OS file picker, a vehicle lookup against the replay adapter, and a report preview through finalize that produces a PDF and registers it.

## Why

Proposal §22.2 names document upload, vehicle lookup and report preview/finalize in the UI suite, and §24 Phase 6 and Phase 7 exit gates depend on them: large and failed transfers recover safely, provider error states are distinct, and the finalized document and its audit are correct. These three are the paths where the UI, an external adapter and a file on disk meet — the API tests cannot see the file picker, and the view-model tests cannot see the produced PDF. Completes the suite started in [[DSK-08-07]] on the harness from [[DSK-08-06]].

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-08`
- Plan detail: `docs/desktop/08-testing/test-uat-stack.md` § "UAT scripts" rows 6, 7, 9 and § Components (Box custody via `LocalCaseCustody`; DVLA/DVSA replay adapter; desktop-side WebView2 rendering)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 22.2 "WinUI UI automation", § 12.2–12.5, § 24 Phases 6 and 7
- Repository evidence:
  - `.codex/skills/winui-ui-testing/SKILL.md` — file-picker driving through the same verbs with `-w <HWND>` against the Win32 dialog
  - `src/Pegasus.Infrastructure/Custody/LocalCaseCustody.cs` — the local custody adapter the stack composes in `DevelopmentOffline`
  - `src/Pegasus.Infrastructure/Vehicle/DvlaDvsaAdapters.cs` — the replay adapter the lookup script exercises
  - `tests/Pegasus.Desktop.UITests/ui-tests.ps1` — the harness and results contract
- Binding decisions:
  - L-03 — the report is rendered on the desktop through the isolated non-UI WebView2 HTML→PDF path; the script asserts the desktop-produced PDF, and golden-file comparison against the gateway renderer stays with [[DSK-08-18]].
  - L-02 — Box and DVLA/DVSA are replay/local adapters on the stack; the real providers are pilot-ring only.
- Depends on: `DSK-08-06` — the harness. `DSK-05-14` (S14 documents and custody), `DSK-05-15` (S15 vehicle lookup), `DSK-05-18` (S18 report generation, preview, finalise, send).

## Routing

- **Subagent**: `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`, `microsoft/win-dev-skills` v0.5.0 `f1028dd5`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-08`, `docs/desktop/08-testing/test-uat-stack.md` § "UAT scripts" rows 6, 7 and 9, and `docs/desktop/07-integrations/README.md` § 5 rows `DSK-07-06`, `DSK-07-10`, `DSK-07-16`. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. Start the stack (`pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start -Mode TestStack`) and confirm `-Action Status -Mode TestStack` is healthy. Prepare a deliberately generic test document under `artifacts/ui-tests/fixtures/` — never a file from `corpus/` and never fabricated domain material.
3. Load `pegasus-desktop`, then `winui-ui-testing`, and read its file-picker section. Add `tests/Pegasus.Desktop.UITests/scripts/07-document-upload.ps1`: open the case documents view, invoke the add-document command, then drive the OS file dialog by targeting its window with `winapp ui ... -w <HWND>` — the picker is a separate Win32 window, so the app PID alone will not find its elements. Type the fixture path into the dialog's edit and invoke Open.
4. In the same script assert the transfer queue: the item appears with a progress state, reaches a completed state within an explicit `wait-for` timeout, and the document then appears in the case document list with its name. Assert the custody row through the gateway (`GET /api/v1/cases/{id}/documents`) so the evidence is the persisted result, not only the UI.
5. Add a failure branch to the same script: cancel an in-flight transfer and assert the queue shows a cancelled state and no partial document is listed.
6. Add `scripts/08-vehicle-lookup.ps1`: enter a VRM that the replay adapter has a recorded response for, invoke lookup, `wait-for` the suggestion element, assert the displayed source and timestamp fields are present, accept the suggestion, and assert the case field now shows the accepted value. Then repeat with a VRM the adapter has no recording for and assert the provider error state is a distinct, named state — not the same message as a successful empty result.
7. Add `scripts/09-report-preview-finalize.ps1`: open the assessment, invoke generate draft, `wait-for` the preview surface, assert the preview is populated, invoke finalize, and assert the finalized report appears in the case with its registered identifier.
8. In the same script assert the produced file: resolve the PDF the desktop wrote, check it exists, is non-empty, and begins with the `%PDF-` header; record its byte length in the results JSON. Do not assert page content here — content parity is [[DSK-08-18]].
9. Register the three scripts in the `ui-tests.ps1` batch, keeping each independently runnable by PID.
10. Run the full batch twice on the Test/UAT workstation. Both runs must be green with identical pass counts; apply at most two fix-and-rerun cycles before escalating a flake as a finding.
11. File `artifacts/ui-tests/results.json`, the screenshots and the produced PDF path listing as ticket proof; the PDF itself stays local under `artifacts/` (ignored).
12. Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan document before opening the PR.

## Acceptance criteria

- [ ] The file picker is driven through `winapp ui` with `-w <HWND>`, not by bypassing the dialog.
- [ ] The upload script asserts both the transfer-queue states and the persisted custody row, and covers a cancelled transfer.
- [ ] The vehicle script distinguishes a successful lookup from a provider error state by name.
- [ ] The report script produces a real PDF file and asserts its existence, non-zero length and `%PDF-` header.
- [ ] Two consecutive full runs produce identical results.

## Verification

- [ ] `pwsh ./tests/Pegasus.Desktop.UITests/Invoke-UiSuite.ps1` — expected: exit 0, all nine scripts `PASS` in `results.json`.
- [ ] `Get-Item <finalized pdf path>` — expected: exists, `Length` greater than zero.
- [ ] `Invoke-RestMethod` on `GET /api/v1/cases/{id}/documents` with a staff token — expected: the uploaded document present with its custody state.

## Evidence tier

Tier 7 — Browser/accessibility, desktop reading. It obliges authenticated workflows through the real UI including the OS file dialog, with the persisted and produced artefacts checked rather than only the on-screen state.

## Documentation changes

- `tests/Pegasus.Desktop.UITests/README.md` — add the three scripts and their fixtures.
- `docs/desktop/08-testing/README.md` § 4 — mark the document, vehicle and report UI paths as scripted.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may create and edit `tests/Pegasus.Desktop.UITests/**` and the fixture folder under `artifacts/`. Must not change desktop or gateway production code, and must not call a real Box, DVLA or DVSA endpoint — the stack composes replay and local adapters.
- **Traps**: never fabricate domain emails, images, documents or data; `corpus/` is never copied into fixtures. UI tests mutate the installed package — dedicated workstation or runner only. `wait-for` instead of sleeps; two fix-and-rerun cycles maximum. The report content comparison belongs to [[DSK-08-18]]; asserting page content here would duplicate the golden-file lane.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
