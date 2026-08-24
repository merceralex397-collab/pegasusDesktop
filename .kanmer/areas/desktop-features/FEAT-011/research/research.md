# Research — FEAT-011: the Triage action matrix, counted, and the evidence surface Triage does not have

## Question

How many triage actions does `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs`
actually dispatch, what does each one require, and where does the plan text's
"thirteen" and `TriageMcpTools.cs`'s "ten" come from — since proposal §10.2
forbids replacing a dispatcher with a guess?

## Current behaviour

Read at fork `main` `191ddf33`. The implementer re-reads and records the SHA of
`Details.cshtml.cs` characterized (ticket Traps).

| Surface | `path:line` | What it does |
| --- | --- | --- |
| Triage list | `src/Pegasus.Web/Pages/Triage/Index.cshtml.cs` (449 lines) | `ITriageQueries.ListAsync` |
| Triage detail | `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:56` `OnGetAsync` | `ITriageQueries.GetAsync`; also the detail's **only** route to its own images — a link out to `/Intake/Details/{Origin.ReceiptId}` |
| Every action | `…/Details.cshtml.cs:85` `OnPostActionAsync` | One handler taking `actionName` and dispatching through a `switch` at `:114-210` |

Parity-matrix rows: **`PAR-23`** (list) and **`PAR-24`** (detail, which records
"dispatches 13 commands"), both `docs/desktop/01-inventory-and-parity/parity-matrix.md`,
both `not inventoried`. The matrix holds `PAR-01`…`PAR-46`
(`grep -c '^| PAR-' …/parity-matrix.md` → 46).

## Findings

### Facts

- **The dispatcher has exactly twelve `case` labels, measured.** Reading
  `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:114-210` on 2026-08-24 gives, in
  source order: `assign`, `unassign`, `await_information`, `record_finding`,
  `supersede_finding`, `link_response`, `unlink_response`, `complete`, `cancel`,
  `reopen`, `link_case`, `unlink_case`. The `default` branch (`:208-210`) throws
  `ArgumentException("The requested Triage action is not supported.")`.
- **`OnPostActionAsync`'s signature is the full parameter surface.**
  `Details.cshtml.cs:85-97` takes `Guid id`, `string actionName`,
  `long expectedVersion`, `string operationKey`, `string reason`,
  `RoadworthinessFinding? roadworthiness`, `AssessmentFinding? assessment`,
  `Guid? supersedesFindingId`, `string? responseCandidate`,
  `Guid? sentEvidenceId`, `Guid? caseId`. Twelve explicit routes must between them
  cover every one of these; no single route needs all of them.
- **Seven of the twelve take only the shared mutation request.**
  `TriageMutationRequest(id, expectedVersion, actor, operationKey, reason)` is
  built once at `Details.cshtml.cs:107-112` and passed unchanged by `unassign`,
  `await_information`, `complete`, `cancel` and `reopen`. `assign` builds its own
  request adding `staffId`; `record_finding` and `supersede_finding` add
  `roadworthiness`, `assessment` and (for supersede) `supersedesFindingId`;
  `link_response` parses a `responseCandidate` into a poll-outcome id and a
  sent-evidence id; `unlink_response` takes `sentEvidenceId`; `link_case` and
  `unlink_case` route through `ExecuteCaseAssociationAsync` with `caseId`.
- **`TriageMcpTools.cs` declares thirteen tools, of which exactly ten are
  mutations.** `grep -c "McpServerTool" src/Pegasus.Web/Mcp/TriageMcpTools.cs`
  → 14 (one is the class-level attribute count artefact; the tool declarations
  are the thirteen `Name = "pegasus_triage_*"` lines). Three are reads —
  `pegasus_triage_list` (`:37`), `pegasus_triage_get` (`:66`),
  `pegasus_triage_source_download` (`:81`) — and ten are mutations:
  `await_information` (`:98`), `record_finding` (`:103`), `supersede_finding`
  (`:108`), `response_link` (`:113`), `response_unlink` (`:118`), `complete`
  (`:123`), `cancel` (`:128`), `reopen` (`:133`), `case_link` (`:138`),
  `case_unlink` (`:143`).
- **The gap between ten and twelve is exactly `assign` and `unassign`.**
  Comparing the two lists above, the two dispatcher labels with no MCP tool are
  the assignment pair. That is a measured explanation for the discrepancy, not a
  resolution of it — the plan text says thirteen, `endpoint-map.md` lists twelve
  named routes with the note "verify the full set", and
  `parity-matrix.md` `PAR-24` says thirteen. **The open question is which number
  the board records, and it is opened rather than assumed (ticket step 2).**
- **`docs/desktop/05-implementation-and-migration/README.md:119-123`** is where
  the plan states the discrepancy in its own words: "The thirteen triage commands
  dispatched by … the remaining three are enumerated during S11 research, not
  [assumed]".
- **The endpoint map already names twelve routes.**
  `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Triage, Unidentified,
  Operations` lists `POST /triage/{id}/assign`, `/unassign`,
  `/await-information`, `/findings`, `/findings/{fid}/supersede`,
  `/responses/link`, `/responses/unlink`, `/complete`, `/cancel`, `/reopen`,
  `/case-link`, `/case-unlink` — with the parenthetical "verify the full set" and
  the note that `assign` becomes Engineer selection per upstream INTK-019.
- **`ITriageQueries` has two members only.**
  `src/Pegasus.Core/Triage/TriageContracts.cs:288-294` declares `ListAsync` and
  `GetAsync`. There is **no** `GetByOriginReceiptAsync` in the fork today; it
  arrives with upstream INTK-033 (board [[INTK-007]]).
- **`ICreateTriageFromIntake` exists at `TriageContracts.cs:138`** and its request
  `CreateTriageFromIntakeRequest` at `:79-84` takes a **normalized** vehicle
  registration. `src/Pegasus.Core/Intake/DurableIntake.cs:893`
  (`CreateTriageIfQualifyingAsync`) is its only caller today, reached from
  `ProcessQueuedIntake` (`DurableIntake.cs:418`, with the interface injected at
  `:423`).
- **`StaffAccessRight.PerformCasework` is at
  `src/Pegasus.Core/Identity/StaffAuthorization.cs:10`** — the right the
  evidence section is gated on. `src/Pegasus.Core/Triage/` holds
  `TriageContracts.cs`, `TriageLifecycle.cs`, `TriageQueryUseCases.cs` and
  `EmailEvidenceContracts.cs`.
- **Triage's evidence problem is real and visible in one line.**
  `src/Pegasus.Web/Pages/Triage/Details.cshtml:56` links out to
  `/Intake/Details/{Origin.ReceiptId}`; that link is the engineer's only route to
  the client's damage photographs. `docs/desktop/06-ui-design/screen-specs.md:287-296`
  § `Triage detail` lists evidence, reply evidence, findings, responses and the
  linked case as sections — **and no gallery**. This is upstream INTK-034.
- **`Triage` is a reserved business word.** `docs/engineering.md`
  § Capability organization settles its meaning; the design authority's
  banned-word list (`docs/design/README.md:412-421`) does not touch it, so it
  keeps its settled operator meaning in every string.
- **Existing test evidence exists but is not the action matrix.**
  `tests/Pegasus.IntegrationTests/` holds `TriageQueuesWebTests.cs`,
  `QdosTriageIntegrationTests.cs`, `QdosTriageCaseAssociationIntegrationTests.cs`
  and `QdosTriageReplayIntegrationTests.cs`; `tests/Pegasus.Core.Tests/Triage/`
  exists as a folder. `parity-matrix.md` `PAR-23` and `PAR-24` both record test
  evidence as "to locate", which is why step 3's characterization is new work
  rather than a re-run.
- **The projects this slice writes into do not exist yet.** `ls src` returns only
  `Pegasus.Core Pegasus.Infrastructure Pegasus.Web Pegasus.Worker`; `ls tests`
  only `Pegasus.ArchitectureTests Pegasus.Core.Tests Pegasus.IntegrationTests`.

### Assumptions

- **A-05-11-1 — the twelve dispatcher labels are the complete action set, and the
  plan text's "thirteen" is an over-count.** Evidence points that way (twelve
  measured labels; ten MCP mutations plus the assignment pair = twelve), but the
  ticket forbids assuming a number. Confirmed by: the open question being answered
  and recorded. Breaks if: a thirteenth action exists on a route the dispatcher
  does not carry — then the enumeration in step 2 finds it and the count changes.
- **A-05-11-2 — every precondition that governs which action is legal from which
  `TriageState` is reachable from `src/Pegasus.Core/Triage/TriageLifecycle.cs`
  rather than only from the page model.** Confirmed by: the characterization tests
  in step 3 passing against Core without a page-model shim. Breaks if: a
  precondition lives only in the page model — then it moves into
  `src/Pegasus.Core/Triage/` with the test written first, which is what step 3
  says to do.
- **A-05-11-3 — [[GWY-013]] (plan handle `DSK-03-13`) gives every enumerated
  action its own route and maps `TriageVersionConflictException` to a 409 carrying
  the current version.** Confirmed by: reading the generated client at step 4.
  Breaks if: a route is folded or the exception maps to a 500 — stop and raise it
  on [[GWY-013]] rather than translating client-side.
- **A-05-11-4 — the origin receipt's retained assets are readable through the
  byte endpoints [[FEAT-009]] (plan handle `DSK-05-09`) consumes, without a new
  custody record.** Confirmed by: the evidence section rendering images from
  `GET /api/v1/received/{id}/images/{iid}` with no write anywhere. Breaks if: the
  Triage detail cannot reach its origin receipt id — which is what
  `ITriageQueries.GetByOriginReceiptAsync` (upstream INTK-033, board
  [[INTK-007]]) exists to fix, and is [[GWY-013]] step 8's to resolve.

## Execution placement

The six-question test from `docs/desktop/00-governance-and-workflow/README.md`
§ 3, answered.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **yes** | Every action carries `expectedVersion` (`Details.cshtml.cs:88`) and `TriageVersionConflictException` exists because two staff can act on the same Triage. Lands in the gateway (L-01, ADR-0103). |
| Unattended execution — must it run with every desktop closed? | **yes** | Triage *creation* from a qualifying receipt runs in the Worker — `CreateTriageIfQualifyingAsync` at `src/Pegasus.Core/Intake/DurableIntake.cs:893`, reached from `ProcessQueuedIntake` at `:418`. Lands in the existing `src/Pegasus.Worker` (ADR-0106). The twelve staff actions in this slice are all operator-initiated and place nothing there. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | The artifact-store credential behind the origin receipt's retained assets and source download. Lands behind the gateway (ADR-0107); the desktop streams through `/api/v1` and holds no provider secret. |
| Public callback — must an external service call a stable public endpoint? | **no** | Nothing external calls into the triage surface. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes** | `StaffAuthorization.Require(…, StaffAccessRight.PerformCasework)` (`src/Pegasus.Core/Identity/StaffAuthorization.cs:10`), the action matrix's state preconditions, operation-key replay and the audit row per action must hold whatever the client is. Lands in the gateway. |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement in this repository supports rendering the triage surface or its evidence gallery centrally; §15.2 pushes decode-to-display-size onto the workstation. |

Conclusion: four "yes" answers place the twelve commands, the source broker and
the audit in the gateway (L-01), and the unattended creation path in the existing
Worker (ADR-0106) — where it already is, untouched by this slice. Command gating,
the reason dialogs and the evidence rendering belong in the desktop. No new Azure
resource; no Azure write.

## Implications

- **Twelve command objects, no dispatcher string anywhere.** The single
  `OnPostActionAsync` becomes twelve `[RelayCommand]` members with `CanExecute`
  derived from the loaded `TriageState` and the actor's rights. The desktop never
  sends an action name as data.
- **The parameter surface partitions cleanly.** Five actions need only the shared
  mutation request; one needs a staff id; two need finding payloads; two need
  response identifiers; two need a case id. That partition is the DTO design in
  step 5 — one request record per command, not one union record with eleven
  nullable fields.
- **The count is a gate, not a detail.** Because §10.2 forbids a generic action
  endpoint, "how many actions" is the same question as "how many routes", and
  [[GWY-013]] cannot be verified against a number nobody has agreed. Hence the
  blocking open question.
- **Assignment changes meaning, not just wording.** upstream INTK-019 replaces
  "Assign to me" with Engineer selection; the command takes the selected
  engineer's identity rather than implying the current user, so the request record
  for `assign` carries a staff id the operator chose.
- **One gallery for the whole application.** [[FEAT-016]] (plan handle
  `DSK-05-16`) owns the gallery and its viewer and names Triage in its adopter
  step. [[FEAT-016]] is phase 6 and this slice is phase 5, so step 10 has two
  legitimate cases and the plan records which applied. Either way this slice
  writes no second image renderer or thumbnail cache.
- **The evidence section is a read, not a retention.** The photographs come from
  the **origin receipt's** retained assets over the existing byte endpoints; a
  second retention under the Triage would duplicate custody and is a stop
  condition.
- **One streaming service.** The triage source download reuses [[FEAT-009]]'s
  streaming service by name, not by copy.

## Open questions

Two, both recorded in the `open-questions` document because the ticket body
instructs it and both must be answered before this ticket leaves Preparing.

1. **The action count.** Twelve measured `case` labels; ten MCP mutations plus
   `assign`/`unassign`; "thirteen" in the plan text
   (`docs/desktop/05-implementation-and-migration/README.md:119-123`,
   `vertical-slices.md` § S11) and in `parity-matrix.md` `PAR-24`. Ticket step 2
   says: "Record the actual count with evidence in `research`, note the
   discrepancy under the ticket's open questions, and get it resolved before
   leaving Preparing — do not assume a number."
2. **Whether a Triage evidence surface is wanted at all (operator).** Carried
   forward from upstream INTK-034 and restated in the ticket's Guardrails, with
   the FRD-03 answer to record. The ticket says in its own words: "an unticked
   open question blocks the Kanmer move, which is correct here."
