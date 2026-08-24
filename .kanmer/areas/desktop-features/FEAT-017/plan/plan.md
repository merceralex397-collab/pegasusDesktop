# Plan — FEAT-017: S17 Assessment workbench

**Diff estimate: ~34 files, ~2,600 lines**, across three PRs — S17a ~9 files / ~550 lines,
S17b ~16 files / ~1,500 lines, S17c ~9 files / ~550 lines. Derived from the files document:
5 contracts DTO files, 6 desktop view-model/XAML files, 3 desktop-infrastructure client files,
5 gateway endpoint/handler files, ~4 files touched in `src/Pegasus.Core/Assessment/` for the nine
moved rules, 1 re-pointed Razor page model, 9 test files (4 Core characterization, 3 contract,
2 view-model), plus 3 documentation files. The 1,500-line S17b share carries the upload session,
the FRD-06 acceptance aggregate and the six rules moved out of `OnPostImportEstimateAsync`.

## Approach

Rebuild the workbench from the **Core use cases and the handlers**, not from the page — because
`src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs:16-27` records that the section forms are
unbound design markup, so there is no bound web behaviour to translate. Rules that today live in
the page model move into `src/Pegasus.Core/Assessment/` behind a characterization test first, the
Razor page is re-pointed at them, and the desktop then runs the same deterministic
`AssessmentPolicy` locally through a direct `Pegasus.Core` reference for immediate feedback while
the gateway re-checks every figure inside the write transaction.

Rejected: **translating the page model into a view model**. It would have carried the `TempData`
and PRG machinery ([[FEAT-024]] exists to stop exactly that), duplicated the nine page-model rules
into a second implementation, and — because the forms are unbound — reproduced markup with no
behaviour behind it. Also rejected: **rendering the whole workbench server-side and displaying it**,
which fails the placement test's desktop default for interaction and immediate validation.

## Governing docs

The ticket's `refs` are `docs/frd/frd-06-vehicle-and-engineering-evidence.md` and
`docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md`. Both exist in the repository.

| Ref | Requirement | Meets |
| --- | --- | --- |
| FRD-06 § `Canonical repair specifications` (`:182-205`) | One immutable, versioned current accepted specification per case; imported material stays a **draft** until an authorised Engineer accepts source, mapping, ordered lines and calculation basis | Steps 7–9 (draft/accept split), Step 11 (Engineer 403 fact) |
| FRD-06 § `Canonical repair specifications` (`:199-201`) | Corrections create a new reasoned version; accepted rows are never edited in place; a case with no unambiguous accepted version fails closed | Step 9 (reason required on a correcting import), Step 11 (409 stale-version fact) |
| FRD-06 § `Conservative MOT mileage estimation` (`:206-215`) | Preserved seam: raw observations, normalized units, model/rule version, estimate/range and staff disposition stay distinct source-labelled identities; a range is never defaulted into the case | Step 12 (prefill shows provenance and obtained-at; a prefilled value is never presented as keyed by the operator) |
| FRD-11 § `Report correction, finality, and post-report work` (`:130-166`) | Deterministic template and payload versioning; preserved document/source provenance; authorised human review before issue | Step 8 (the accepted specification is the report's input and is Engineer-accepted before any report exists) — the report itself is [[FEAT-018]] |

`docs_todo: true` on this ticket, confirmed in `get_doc_gates FEAT-017` (the `governing-doc`
requirement at `leave-backlog` already reads `satisfied: true`).

> **New ADR** — ADR-0101 (local-execution / cloud-authority split and the six-question
> cloud-justification test), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table, `:150-166`); if the ADR
> lands differently this plan is revised before implementation. ADR-0103 (gateway, never direct
> database access from workstations) also governs and is authored by the same ticket. The reserved
> block ADR-0100…ADR-0110 spans several areas; where an ADR has more than one claimant the plan
> naming it says so rather than asserting a single author.

Programme-level authorities that bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 13.9 | Data entry, deterministic calculation and repair/valuation information move across intact | Steps 7–9 |
| Proposal § 4.1 / governance § 3 | Six-question placement test answered per capability, "yes" naming *where*, not "Azure" | `research` § Execution placement |
| Plan 05 § 7 ("The two giants") | `Assessment/Index.cshtml.cs` (740 lines) ships as three PRs, never one | Steps 2, 7, 8, 9, 15 |
| Plan 05 § 3 ("Characterization before moving any rule") | A rule found only in a page model moves into Core with a test first; a second implementation is a stop condition | Steps 5–6 |
| `docs/engineering.md` § One Core owner | Migrate or delete the replaced code, registrations, tests and documentation in the same slice | Steps 6, 15 |
| `docs/engineering.md` § Required evidence tiers (2, 5, 7) | Tier 2 obliges positive, contradictory, ambiguous and failure cases before a rule moves; tier 5 obliges route-level evidence per command; tier 7 obliges keyboard/focus/label evidence from a real run | Steps 5, 11, 12 |
| L-01 | Gateway is `Pegasus.Web` evolved in place; the authoritative write and the estimate parse stay there | Steps 6, 8 |
| L-02 | Fixture comparison and UAT run on the local Test/UAT stack, never an Azure environment | Steps 13, 14 |
| L-04 | Every ticket names its subagent, skills and MCP tools | § Routing below |
| Operator decision, 2026-08-24 (Send to AI) | AI-09 is a recorded exclusion with a reactivation condition; no ticket reopens it and no question is filed for it | Step 3 (both AI handlers excluded from characterization); § Risks |
| `docs/design/README.md:412-445` | Banned operator words and the four hard rules; merge rules with no CI enforcement | Step 12, and the reviewer pass |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires in the plan document.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` —
  `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` —
  `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent` (dotnet/skills
  `98f84851`, `plugins/dotnet-test/skills/code-testing-agent/SKILL.md`) → `dotnet-webapi`
  (dotnet/skills `98f84851`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`)
  → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `run-tests` → `winui-code-review`
  at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute`
  → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every
  move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

These refine the ticket body's fourteen implementation steps — same order, same ownership, same
file paths — adding the *how* the body leaves out. Ticket step numbers are given in brackets.

1. **[body 1] Orient and take.** Read the plan row, `vertical-slices.md` § S17, the screen spec
   assessment section, FRD-06 and FRD-11. Call `get_doc_gates FEAT-017`, then `take_ticket` with
   branch `task/dsk-05-17a-assessment-damage` and worktree
   `../pegasus-worktrees/dsk-05-17a-assessment-damage` from `origin/dev`.
2. **[body 2] Record the split in this plan before writing code.** S17a record damage; S17b
   estimate import and specification acceptance; S17c reconcile. One branch, commit series and PR
   each, into `dev`, in that order, with a checkpoint after each. Append the branch names for S17b
   and S17c here when S17a merges.
3. **[body 3] Tabulate the handlers.** Read `Index.cshtml.cs` in full. In `research`, tabulate the
   four **in-scope** handlers (`:184`, `:246`, `:330`, `:476`) with their Core calls and their
   required `expectedVersion` / `operationKey` / `editLeaseToken`. Explicitly exclude `:583` and
   `:628`: both are Send-to-AI surfaces (`ISendCaseToAi` at `:593`, `IReconcileAiWorkRequest` at
   `:639`) and `reuse-map.md:38` puts `AiWork/` out of parity scope. Record the prefill path from
   `AssessmentVehiclePrefillWebTests.cs`. Record the SHA read.
4. **[body 3, refinement] Confirm the fixture set.** Enumerate the fixtures behind
   `AssessmentDamageAndCopyWebTests.cs` and `AssessmentEstimateImportWebTests.cs` and write the
   list into the plan — this list, not a vague "approved fixture set", is what step 13 compares.
5. **[body 4] Characterize before moving.** Load `code-testing-agent`. Write facts in
   `tests/Pegasus.Core.Tests` for each of the nine page-model rules **at current behaviour**:
   Engineer-only import (`Index.cshtml.cs:341`), the 10 MiB ceiling (`:45`, enforced `:351`),
   PDF-only (`:356`), existing-draft refusal (`:382-387`), accepted-specification-needs-a-reason
   (`:388-394`), the `estimate-import:{operationKey}` artifact identity (`:397`), Engineer-only
   acceptance (`:494`), the `repairerVatRegistered` tri-state validation (`:504`), and the
   `draft.SpecificationId != specificationId` staleness check (`:509-514`). Tier 2 obliges a
   positive, a contradictory, an ambiguous and a failure case for each.
6. **[body 4] Move them, then re-point.** Move each characterized rule into
   `src/Pegasus.Core/Assessment/` and re-point `Index.cshtml.cs` at the moved rule in the same
   commit. Run the four existing assessment web tests after each move; an edited assertion there
   means the move changed behaviour — stop and investigate. Check the MCP surface
   (`src/Pegasus.Web/Mcp/`) for the same use cases: moving a rule into Core changes what MCP
   enforces too, which is intended and must be verified rather than assumed.
7. **[body 5–6] Confirm the endpoints and add the DTOs.** Against [[GWY-014]] (plan handle
   `DSK-03-14`) and `endpoint-map.md` § `Cases`: `GET /api/v1/cases/{id}/assessment` (ETag +
   `version`), `POST …/assessment/damage`, `POST …/assessment/estimate-import` (upload session),
   `POST …/assessment/specification/accept` (**Engineer**), `POST …/assessment/reconcile`. Add the
   DTOs to `src/Pegasus.Contracts` with `decimal` money and measurement fields — no lossy rounding
   on the wire. Regenerate `openapi/pegasus-v1.json` and the generated client in this change.
8. **[body 7] S17a — damage.** Implement `AssessmentDamageViewModel` in `src/Pegasus.Desktop`,
   running the deterministic `AssessmentPolicy` calculations locally through the direct
   `Pegasus.Core` reference the `reuse-map.md` boundary note permits. The authoritative figure is
   always the one returned by the save response; the local figure is feedback, never the record.
   Reproduce the write shape at `Index.cshtml.cs:213-228` — lease, then save with version, actor,
   operation key, reason and lease token. **Open the S17a PR here.**
9. **[body 8] S17b — import and accept.** Branch `task/dsk-05-17b-assessment-estimate`. Estimate
   import is an upload session reusing the transfer service from [[FEAT-014]] (plan handle
   `DSK-05-14`); the desktop never parses the PDF. Show the imported lines as a **draft** and offer
   acceptance to an Engineer. FRD-06 `:190-195` binds: the draft is not the specification until an
   authorised Engineer accepts the exact source, mapping, ordered lines and calculation basis. The
   Engineer gate is enforced server-side and only reflected in the UI. **Open the S17b PR here.**
10. **[body 9] S17c — reconcile.** Branch `task/dsk-05-17c-assessment-reconcile`. Specify the
    command from the endpoint-map row and [[GWY-014]]'s merged contract, **not** from
    `OnPostReconcileAsync` (`:628`), which reconciles a Send-to-AI work request. Carry the reason
    dialog from [[DUI-009]] (plan handle `DSK-06-09`) where Core requires a reason, and surface the
    shared conflict pattern from [[FEAT-008]] (plan handle `DSK-05-08`) on 409. If [[GWY-014]] has
    not defined the command, S17c stays in Preparing rather than inventing one. **Open the S17c PR
    here.**
11. **[body 10] Prefill with provenance.** Take mileage and its source from the accepted lookup
    evidence produced by [[FEAT-015]] (plan handle `DSK-05-15`), showing the provenance glyph and
    the obtained-at value beside the figure. FRD-06 `:214` keeps the seam distinct: a prefilled
    value is never presented as keyed by the operator, and a range is never defaulted in.
12. **[body 11] Contract tests.** In `tests/Pegasus.Api.ContractTests`, per command: success, 401,
    403 (including a non-Engineer attempting acceptance), 409 stale version, replay of the same
    `operationKey` returning the original outcome, and a malformed estimate rejected with a problem
    rather than a partial import. Assert an action-history record for each mutation — FRD-04 `:29`
    makes the history write part of the business transaction. Enable `Features:DesktopGateway`
    explicitly; a gated-off endpoint returns 404 and would pass a naive negative test.
13. **[body 12] View-model tests.** In `tests/Pegasus.Desktop.ViewModelTests`: local calculation
    equals the server response, dirty state, Engineer gating, prefill provenance, reconcile.
14. **[body 13] Fixture comparison.** For every fixture enumerated at step 4, the desktop figure
    must equal the web figure. Record the table in the ticket proof — figure for figure, with the
    fixture name in each row.
15. **[body 14] Operator step, documentation, simplification, PRs.** UAT by a qualified Engineer on
    the local Test/UAT stack across damage, import, accept and reconcile; capture the sign-off text
    and date in the proof. Update `parity-matrix.md` row `PAR-15` (assessment portion only), add the
    assessment section to `docs/frd/frd-13-desktop-operator-experience.md` citing FRD-06 and FRD-11,
    add the `DSK` rows to `docs/capabilities.md`, run the simplification pass over **each** sub-slice
    diff under a dated `## Simplification pass` heading, and open the PRs in S17a → S17b → S17c
    order.

## Verification

Evidence tiers from the body: **tier 2** (Core/domain), **tier 5** (Web/API/MCP caller),
**tier 7** (Browser/accessibility).

- `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build`
  — the nine characterization facts pass and the existing `AssessmentPolicy` facts stay green.
- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build`
  — damage, import, accept and reconcile facts pass, including the non-Engineer 403 and the
  `operationKey` replay.
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`
  — calculation, gating and prefill facts pass.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"`
  — the four existing assessment web tests stay green after every rule move.
- The **fixture comparison table** and the **Engineer UAT record** in the ticket proof: figure-for-
  figure equality with the web per named fixture, and a named Engineer's sign-off text with a date.

The evidence that becomes `proof`: the four test outputs (test-output tier), the fixture table, and
the Engineer sign-off.

## Risks / open questions

- **The four in-scope handlers are the only characterization source; two handlers on the same page
  are not.** Mitigation: step 3 excludes `:583` and `:628` explicitly, and the files document
  records why. An agent who characterizes them would build a Send-to-AI surface the operator has
  excluded.
- **"Reconcile" has no web implementation.** Owner: [[GWY-014]] (plan handle `DSK-03-14`), which
  publishes `POST /cases/{id}/assessment/reconcile`. This is a scope boundary, not an open
  question on this ticket. If [[GWY-014]] has not defined the command when S17b merges, S17c waits.
- **No report-send path exists in the web application** (`grep -rn "OnPostSend" src/Pegasus.Web/Pages/`
  returns one hit, the AI one). This affects [[FEAT-018]], not this ticket, and is recorded there;
  noted here because the two slices share row `PAR-15` and the same file.
- **A rule moved into Core changes the MCP surface too.** Mitigation: step 6 checks
  `src/Pegasus.Web/Mcp/` for each moved use case. That is the intended consequence of one policy
  owner, but it must be observed rather than assumed.
- **Money precision.** Mitigation: `decimal` throughout the DTOs (step 7); FRD-06 requires the raw
  calculation basis and totals to be retained, so a formatted string on the wire is a defect.
- **The three-PR rule can be eroded under time pressure.** Mitigation: each sub-slice has its own
  branch name recorded at step 2 and its own simplification pass at step 15; the reviewer refuses a
  combined PR.
- **Send to AI** — a recorded exclusion with a reactivation condition (`docs/capabilities.md:269`),
  settled by the operator on 2026-08-24. Not an open question; no `open-questions` document is
  created for it on any ticket.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading. This ticket ships three branches, so it
records three dated headings — one per sub-slice._
