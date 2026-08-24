# Plan — FEAT-011: S11 Triage list, detail and actions

**Diff estimate: ~24 files, ~3,100 lines.**

Derived from the `files` document, not asserted. `src/Pegasus.Contracts` triage
DTOs — 4 files, ~380 lines (list item, detail with evidence/reply-evidence/
response candidates, finding payloads, and twelve request records of which five
reuse the shared mutation shape); `src/Pegasus.Desktop` list and detail view
models plus XAML — 5 files, ~1,050 lines (twelve `[RelayCommand]` members with
per-state `CanExecute`, plus the detail's six sections);
`src/Pegasus.Core/Triage/` rule moves plus the Razor re-point — 3 files,
~170 lines; `tests/Pegasus.Core.Tests/Triage/` action-matrix characterization —
2 files, ~480 lines (twelve actions × legal-state, reason-required and
payload-required facts); `tests/Pegasus.Api.ContractTests` — 3 files, ~620 lines
(twelve actions × the seven-case matrix from [[TEST-002]]);
`tests/Pegasus.Desktop.ViewModelTests` — 2 files, ~330 lines; documentation — 5
files, ~70 lines. **The count itself is the open question below; if it resolves
to thirteen, add roughly one contract-test file's worth (~55 lines) and one
request record.**

## Approach

Replace the string dispatcher with one command object and one route per
enumerated action, and *enumerate before designing* — because proposal §10.2
forbids a generic action endpoint and three sources disagree about how many
actions there are (twelve measured `case` labels at
`src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:114-210`; ten mutations in
`src/Pegasus.Web/Mcp/TriageMcpTools.cs`; "thirteen" in
`docs/desktop/05-implementation-and-migration/README.md:119-123` and
`parity-matrix.md` `PAR-24`). The alternative considered and rejected was
carrying the dispatcher across as a single `POST /triage/{id}/actions` with an
action name in the body: it is fewer routes and fewer records, and it is exactly
the shape §10.2 bans and this slice exists to remove. The request records
partition on the evidence rather than on symmetry — `Details.cshtml.cs:107-112`
builds one `TriageMutationRequest` reused unchanged by five of the twelve, so
five commands share that shape and seven carry their own, instead of one union
record with eleven nullable fields.

## Governing docs

The ticket carries `refs: ["docs/frd/frd-03-triage.md"]` and `docs_todo: true`
(confirmed in `get_doc_gates FEAT-011`, which reports `governing-doc` satisfied
at `leave-backlog`).

**Meets — `docs/frd/frd-03-triage.md`.** Steps 3 and 11 pin that every action's
legality by `TriageState`, its reason requirement and its payload requirement are
identical through `/api/v1` and through the Razor page, so the triage lifecycle
FRD-03 governs is preserved rather than re-implemented.

**Modifies — `docs/frd/frd-03-triage.md`, conditionally.** *Only* if the
carried-forward upstream INTK-034 operator question is answered **yes**: record
the Triage evidence surface as required behaviour. That answer is open question 2
below and is the operator's to give; nothing is written to FRD-03 until it is.

> **New ADR** — ADR-0103 (gateway; never direct database access from
> workstations), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3; if the ADR lands
> differently this plan is revised before implementation.

> **New ADR** — ADR-0107 (Box and DVLA/DVSA credentials stay behind the gateway;
> no long-lived provider secret in the package), authored by [[FND-005]]. Same
> condition — it is the authority for the source download and the evidence bytes
> being brokered rather than fetched.

`refs` names one FRD and no ADR, so the programme-level authorities that bind
today are tabulated for `kanmer-review`:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal §10.2 | No generic action endpoint; one named command per action | Steps 2, 4, 7 |
| Proposal §13.4 | The triage flow natively, with evidence | Steps 6, 7, 10 |
| `docs/desktop/05-implementation-and-migration/README.md:119-123` | The remaining commands are enumerated during S11 research, not assumed | Step 2 + open question 1 |
| `docs/desktop/05-implementation-and-migration/README.md:158-170` | Characterization before moving any rule; a duplicate implementation is a stop condition | Step 3 |
| `docs/desktop/06-ui-design/screen-specs.md:287-296` | One container, sections only when populated, `ReasonDialog` where Core requires a reason, never a generic Close | Steps 7, 10 |
| `docs/engineering.md` § Capability organization | `Triage` keeps its settled business meaning in every operator string | Steps 7, 10 |
| `docs/engineering.md` § One Core owner | One gallery, one viewer, one streaming service, one implementation per rule | Steps 9, 10 |
| L-01 | Gateway owns the commands and the audit | Steps 4, 11 |
| L-02 | Verification on the local Test/UAT stack | Step 13 |
| L-04 | Subagent, skills and MCP named on the ticket | § Routing below |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 14 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`;
  `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent`
  (dotnet/skills `98f84851`,
  `plugins/dotnet-test/skills/code-testing-agent/SKILL.md`) → `dotnet-webapi`
  (dotnet/skills `98f84851`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`) → `run-tests` → `winui-code-review` at
  review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates <id>` before every move; a move crosses at most one gated
  boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's fourteen implementation steps in the same order
and with the same ownership.

1. **Orient and take.** Read the plan row `DSK-05-11`,
   `docs/desktop/05-implementation-and-migration/vertical-slices.md:408-433`,
   `docs/desktop/06-ui-design/screen-specs.md:287-296` and
   `docs/frd/frd-03-triage.md`. Call `get_doc_gates FEAT-011`, then `take_ticket`
   with branch `task/dsk-05-11-triage` and worktree
   `../pegasus-worktrees/dsk-05-11-triage` from `origin/dev`.
2. **Enumerate the action matrix and resolve the open question.** Read
   `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:114-210` and list every `case`
   label with the Core command it calls, its required parameters
   (`expectedVersion`, `operationKey`, `reason`, `roadworthiness`, `assessment`,
   `supersedesFindingId`, `responseCandidate`, `sentEvidenceId`, `caseId`) and its
   failure paths. The `research` document records twelve measured labels, ten MCP
   mutations and a plan text saying thirteen; **do not assume a number.** Resolve
   open question 1 below before leaving Preparing, and update `parity-matrix.md`
   `PAR-24`'s "dispatches 13 commands" text to the agreed count at step 14.
3. **Characterize, then move.** Load `code-testing-agent`. Write facts in
   `tests/Pegasus.Core.Tests/Triage/` for the action matrix — which action is
   legal from which `TriageState`, which require a reason, which require a finding
   payload — **before** any rule moves. Where a precondition lives only in the
   page model, move it into `src/Pegasus.Core/Triage/` and re-point the Razor
   page. A second implementation is a stop condition.
4. **Confirm the routes.** With [[GWY-013]] (plan handle `DSK-03-13`), confirm
   every enumerated action has its **own** route — for example
   `POST /api/v1/triage/{id}/await-information`, `…/findings`,
   `…/findings/{fid}/supersede`, `…/responses/link`, `…/responses/unlink`,
   `…/complete`, `…/cancel`, `…/reopen`, `…/case-link`, `…/case-unlink`, plus the
   assignment routes — each carrying the triage `expectedVersion` and an
   `operationKey`, and that `TriageVersionConflictException` maps to a 409 problem
   carrying the current version. A folded route or a 500 is a stop-and-raise on
   [[GWY-013]], not a client-side translation.
5. **Contracts.** Add the triage DTOs to `src/Pegasus.Contracts` *(created by
   [[FND-029]], plan handle `DSK-02-04`)*: list item; detail including the
   evidence images from the origin receipt, the reply evidence and the response
   candidates; finding payloads; and one request record per command. Five commands
   reuse the shared mutation shape (`Details.cshtml.cs:107-112`); note that
   `link_response` takes a **pair** — poll-outcome id and sent-evidence id parsed
   from `responseCandidate` at `Details.cshtml.cs:156-170` — not the raw string.
6. **List.** Implement `TriageListViewModel` over
   `GET /api/v1/triage?page&state` using the data-table pattern from [[DUI-007]]
   (plan handle `DSK-06-07`) with state as a dropdown filter, newest first.
7. **Detail.** Implement `TriageDetailViewModel` with one command object per
   action — **no dispatcher string anywhere in the desktop** — each with
   `CanExecute` derived from the loaded state and the actor's rights, a reason
   dialog from [[DUI-009]] (plan handle `DSK-06-09`) where Core requires a reason,
   and the shared conflict pattern from [[FEAT-008]] (plan handle `DSK-05-08`) on
   409. Never a generic Close.
8. **Engineer selection.** Replace "Assign to me" with an Engineer selection per
   upstream INTK-019, which this slice absorbs; the assignment command takes the
   **selected** engineer's identity rather than implying the current user.
9. **Source download.** Implement `GET /api/v1/triage/{id}/source` as a streamed
   transfer with progress and cancel, using the same streaming service as
   [[FEAT-009]] (plan handle `DSK-05-09`) — one implementation, not a copy.
10. **Evidence surface (upstream INTK-034).** Check whether the shared gallery
    and viewer control from [[FEAT-016]] (plan handle `DSK-05-16`) has landed.
    **If it has**, bind the Triage detail's evidence section to it and render
    nothing of your own. **If it has not** — [[FEAT-016]] is phase 6 and this
    slice is phase 5 — add the evidence section, its `Triage.Evidence.*`
    AutomationIds and its view-model shape here and leave the rendering to
    [[FEAT-016]]'s adopter step. **Record here which case applied.** Never a
    second image renderer or thumbnail cache — that is a stop condition. Either
    way: the photographs are read from the **origin receipt's** retained assets
    over the existing byte endpoints and are not retained a second time under the
    Triage, so no new custody record is written; and a reader without
    `StaffAccessRight.PerformCasework`
    (`src/Pegasus.Core/Identity/StaffAuthorization.cs:10`) sees the page with the
    evidence section **absent** rather than an error. Today the engineer reaches
    these images only by navigating out to the originating e-mail
    (`src/Pegasus.Web/Pages/Triage/Details.cshtml:56`), which is the defect
    INTK-034 records. **This step does not ship until open question 2 is
    answered.**
11. **Contract tests.** In `tests/Pegasus.Api.ContractTests` *(created by
    [[TEST-001]], plan handle `DSK-08-01`)*, cover every enumerated action with the
    seven-case matrix from [[TEST-002]] (plan handle `DSK-08-02`): success, 401,
    403, 409 stale version, 400 bad-input problem, replay of the same
    `operationKey`, and the Core-specific failure. Enable `Features:DesktopGateway`
    explicitly.
12. **View-model tests.** In `tests/Pegasus.Desktop.ViewModelTests` *(created by
    [[TEST-004]], plan handle `DSK-08-04`)*: `CanExecute` per state,
    reason-required commands, finding and supersede payload validation, response
    link/unlink candidate selection, and the evidence section being **absent** —
    not errored — for an actor without `PerformCasework`.
13. **Operator step.** Run the triage UAT script covering the full enumerated
    action set on the local Test/UAT stack
    (`docs/desktop/08-testing/test-uat-stack.md`), confirming each outcome and its
    audit row, including a Triage opened from an intake message showing its
    vehicle photographs without navigating to the e-mail. Capture the operator's
    sign-off text and date in the proof.
14. **Documentation, simplification pass, PR.** Update `parity-matrix.md` rows
    `PAR-23` and `PAR-24` (including `PAR-24`'s command count); add the
    evidence-images section and the `Triage.Evidence.*` AutomationId to
    `screen-specs.md:287-296` — **this ticket's block, and only this block**; write
    `docs/frd/frd-03-triage.md` **only if** open question 2 is answered yes; add
    the triage section to `docs/frd/frd-13-desktop-operator-experience.md` citing
    FRD-03 (created by [[DUI-013]], plan handle `DSK-06-13` — contribute the
    content there if it has not landed); add the `DSK` rows to
    `docs/capabilities.md`. Run the simplification pass over this branch's diff,
    record it under a dated `## Simplification pass` heading below, then open the
    PR into `dev`.

## Verification

Evidence tiers from the body: **2** (Core/domain), **5** (Web/API/MCP caller),
**7** (Browser/accessibility).

- `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build`
  — triage action-matrix characterization facts pass with positive,
  contradictory, ambiguous and failure cases for the lifecycle (tier 2).
- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build`
  — the seven-case matrix passes for every enumerated action, including
  idempotency and exception translation (tier 5).
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`
  — state gating, reason, payload and evidence-section-absent facts pass.
- Tier 7: keyboard, focus and error-behaviour evidence from a real run, captured
  with the [[TEST-006]] (plan handle `DSK-08-06`) harness and the `axe-windows`
  scan from [[TEST-009]] (plan handle `DSK-08-09`).
- UAT record in the proof — named operator sign-off with date across the full
  action set, including a Triage opened from an intake message showing its
  vehicle photographs without navigating to the e-mail.

## Risks / open questions

Two questions genuinely block this ticket and are recorded in the
`open-questions` document, where an unticked `- [ ]` line holds
`leave-preparing`, `enter-review` and `enter-done` — which is the intended
behaviour here and is what the ticket body instructs.

- **Open question 1 — the action count.** Twelve measured `case` labels; ten MCP
  mutations plus `assign`/`unassign`; "thirteen" in the plan text and in
  `PAR-24`. Ticket step 2 forbids assuming a number. Answered by: the operator or
  the plan owner, against the enumeration this ticket produces.
- **Open question 2 — whether a Triage evidence surface is wanted at all
  (operator).** Carried forward from upstream INTK-034, with the FRD-03 answer to
  record. Step 10 does not ship until it is answered. Answered by: the operator.
- **Risk: [[GWY-013]] folds two actions into one route.** Mitigation: step 4 is a
  hard gate; a folded route is raised there and never translated client-side.
- **Risk: a second image renderer appears.** [[FEAT-016]] owns the one gallery
  and viewer and names Triage in its adopter step. Mitigation: step 10 records
  which of its two cases applied, and the ticket's Guardrails make a second
  renderer a stop condition. Owner: [[FEAT-016]].
- **Risk: a second custody record under the Triage.** Surfacing the receipt's
  existing assets duplicates no custody; retaining the images again would.
  Mitigation: the evidence section is read-only over the origin receipt's byte
  endpoints, and step 10 writes nothing.
- **Scope boundary, not a question: `ITriageQueries.GetByOriginReceiptAsync`.**
  It does not exist in the fork (`src/Pegasus.Core/Triage/TriageContracts.cs:288-294`);
  it arrives with upstream INTK-033 (board [[INTK-007]]) and its resolution after
  [[FND-023]] (plan handle `DSK-01-10`)'s sync is [[GWY-013]] step 8's.
- **Risk: parity drift.** Record the SHA of `Details.cshtml.cs` characterized
  (ticket Traps), because upstream keeps fixing the web app.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
