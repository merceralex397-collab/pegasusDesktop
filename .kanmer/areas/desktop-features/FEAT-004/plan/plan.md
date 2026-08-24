# Plan — FEAT-004: S4 Case create

**Diff estimate: ~22 files, ~2,600 lines.**

Derived from the files document: 7 characterization test files in
`tests/Pegasus.Core.Tests` (~640 lines total — the address matrix and
`EffectiveInspectionAddress` account for about half); 2–3 `src/Pegasus.Core`
files gaining the moved rules (~220); `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs`
re-pointed (~-120 removed, ~+40 added, net ~-80 on a 689-line file); 2
`Pegasus.Contracts` DTO files (~180); 1 `/api/v1` create endpoint file (~260,
most of it the three-write sequence and six problem translations); 3 desktop
files — `CaseCreateViewModel` (~320), `CaseCreatePage.xaml` (~300), code-behind
(~50); 1 `Pegasus.Desktop.Infrastructure` file (~80); 2 test files — ViewModel
(~300), contract (~280); ~2 regenerated Kiota files (~150, generated); 3
documentation edits. The 689-line page model is edited, not replaced, which is
why the net figure is smaller than the file count suggests.

## Approach

Do the characterization first and the desktop second. Seven rules that decide
business outcomes live only in `Create.cshtml.cs` (the `research` table); each
gets a test in `tests/Pegasus.Core.Tests` against **current** behaviour, then
moves into `src/Pegasus.Core/Cases/` or `src/Pegasus.Core/Address/`, and the
Razor page is re-pointed at it. Only then does the desktop consume them. The
rejected alternative was building the native screen against the gateway first
and moving the rules later: it produces two implementations of the address
matrix for the duration — the exact stop condition
`docs/desktop/05-implementation-and-migration/README.md` § 3 names — and it
gives the desktop no deterministic local validation to run, since the reuse-map
boundary note permits referencing `Pegasus.Core` and nothing else. The second
rejected alternative was orchestrating the three writes from the desktop: the
class remarks at `src/Pegasus.Web/Pages/Cases/Create.cshtml.cs:29-41` state that
the version chain must never be re-read and that `ExpectedReceiptVersion` must
not advance on a mid-sequence failure, and a client that owned that chain would
be a second implementation of the replay guard.

## Governing docs

### Meets — the ticket's `refs`

`refs`: `docs/frd/frd-01-case-identity-and-lifecycle.md`,
`docs/frd/frd-02-intake-and-source-identity.md`.

| Requirement | Where | Met by |
| --- | --- | --- |
| "Principal and reference are immutable after allocation. Wrong-principal work closes as `Created in error`… neither reference is reused" | `frd-01:34-38` (§ Product invariants restated) | Steps 4 and 10 — replay of the same `operationKey` returns the same result and never allocates a second reference; the create screen exposes no control that could change principal or reference after allocation |
| "Fail closed before case creation or normal Case/PO allocation when processing, limits, or principal identity are incomplete or ambiguous." | `AGENTS.md` § Product invariants, restated in `frd-01` and `frd-02` | Step 3 (`MissingIdentityCriticalFieldNames` moved with its test) and step 4 (the gateway's created / withheld / failed vocabulary) |
| "Case types" — an Audit is not created by hand | `frd-01:29-39` | Steps 3 and 8 — the Audit refusal moves into Core with a test, `CaseType.Audit` is **absent** from the desktop dropdown, and the gateway refuses it if the UI is bypassed |
| Source occurrence and dispatch identity are immutable and separate from the editable candidate projection | `frd-02` § Source occurrence and dispatch identity | Steps 5–6 — the desktop edits the typed draft only; the receipt is never mutated except through `IResolveIntake`'s `CorrectDraft` |
| Mandatory pre-case gates | `frd-02` § Mandatory pre-case gates | Step 3 (`DescribeRefusal`'s decision test moved with its test) and step 9 (the approved refusal sentence rendered in place) |
| Field provenance is shown beside the value | `frd-02`, and `docs/design/README.md:177` | Steps 5 and 8 — a provenance value per field on the wire; a glyph with a one-word tooltip on hover **and** keyboard focus |

### New ADR

The ticket carries `docs_todo: true` (confirmed in `get_doc_gates FEAT-004`).

> **New ADR** — ADR-0103 (gateway = evolved `Pegasus.Web`; never direct database
> access from a workstation) and ADR-0104 (online-required, bounded local cache
> only), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decisions as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 and to L-01 in
> `docs/desktop/README.md` § Locked decisions; if either ADR lands differently
> this plan is revised before implementation. **ADR-0104 bounds step 7: an
> unsaved create draft may be held encrypted locally under proposal §11.1, and
> is not offline replication.**

ADR-0100 records the deviation that `Pegasus.Core` is **not** split into
`Pegasus.Domain` and `Pegasus.Application`
(`docs/desktop/05-implementation-and-migration/README.md` § 3) — directly
relevant here, because every rule this ticket moves lands in `Pegasus.Core`
rather than in a new application layer. It is authored by [[FND-026]] (plan
handle `DSK-02-01`); see [[FND-026]]'s plan for the ownership reconciliation.

### Programme-level authorities that bind today

| Authority | Requirement | Met by |
| --- | --- | --- |
| `AGENTS.md` § Product invariants | Fail closed before allocation; principal and reference immutable; duplicate business implementation is a stop condition | Steps 3–4 and the Out-of-scope boundary |
| `docs/engineering.md` § One Core owner | One policy owner per rule | Step 3 |
| `docs/engineering.md` § Plan sizing | Diff estimate first, derived from the files document | First line |
| `docs/engineering.md` § Required evidence tiers | Tier 2 obliges positive, contradictory, ambiguous **and failure** cases before a rule moves; tier 8 evidence stays local and uncommitted | Steps 3 and 12 |
| Plan 05 § 3 | "Characterization before moving any rule", with create-screen draft-to-case mapping named as an S4 gap | Step 3 |
| Plan 05 § 7 | Page-model logic that is really business logic moves into Core with a test first; a second implementation is a stop condition | Step 3 |
| L-01 (`docs/desktop/README.md`) | The gateway allocates the reference | Step 4 |
| L-02 (same) | The genuine-corpus run is local only | Step 12 |
| L-04 (same) | Routing named on the ticket | § Routing below |
| `docs/design/README.md:177` | Closed provenance list; icon plus one-word tooltip on hover and keyboard focus; no source labels or policy keys in markup | Steps 5 and 8 |
| `docs/design/README.md:400-409` | Closed necessary-copy list, including the refusal sentence | Step 9 |
| `docs/design/README.md:422-430` | A field is a label and a control; no hint text, no "Required."/"Optional." | Step 8 |
| `docs/desktop/06-ui-design/screen-specs.md:28-30` | Deferred capabilities are absent, not disabled | Step 8 (Audit absent from the dropdown) |
| `docs/desktop/00-governance-and-workflow/README.md` § 3 | Six-question test answered with evidence | `research` § Execution placement |
| Proposal §22.1 | Characterization before refactoring | Step 3 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`;
  `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
  (characterization tests first).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `code-testing-agent`
  (dotnet/skills `98f84851`, `plugins/dotnet-test/skills/code-testing-agent/SKILL.md`)
  → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) →
  `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `dotnet-webapi`
  (dotnet/skills `98f84851`) → `run-tests` → `winui-code-review` at review.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`
  (call `get_doc_gates <id>` before every move; a move crosses at most one
  gated boundary).
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the ticket body's thirteen steps in the same order and with the
same ownership.

1. **Orient and take.** Read the plan row, `vertical-slices.md` § S4, and
   `docs/desktop/05-implementation-and-migration/README.md` § 3 (the
   characterization rule and its gap list). Then `get_doc_gates FEAT-004` and
   `take_ticket` with branch `task/dsk-05-04-case-create`, worktree
   `../pegasus-worktrees/dsk-05-04-case-create`, from `origin/dev`.
2. **Confirm the rule inventory.** The `research` document carries two tables —
   rules already in Core, and the seven that live only in the page model, each
   with file and line. Re-verify with
   `git diff --stat bbd1c549..HEAD -- src/Pegasus.Web/Pages/Cases/Create.cshtml.cs src/Pegasus.Core/Intake src/Pegasus.Core/Address src/Pegasus.Core/Cases`;
   if the upstream sync moved any of them, re-read and update `research` with
   the new SHA. The recorded SHA is `bbd1c549`.
3. **Characterize, then move — one rule at a time.** Load `code-testing-agent`.
   For each of the seven page-model rules, write the test in
   `tests/Pegasus.Core.Tests` **first**, against current behaviour, covering
   tier 2's four case kinds (positive, contradictory, ambiguous, failure); then
   move the rule into the owning `src/Pegasus.Core/Cases/` or
   `src/Pegasus.Core/Address/` use case; then re-point the Razor page at it and
   re-run `CaseCreateWebTests.cs` (918 lines) and `CaseAcceptanceReplayTests.cs`
   (467). Order by risk, highest first:
   1. `EffectiveInspectionAddress` (`Create.cshtml.cs:562-582`) — three branches,
      and the wrong order silently changes the created address;
   2. `ValidateAddressChoice` (`:503-546`) — four refusal outcomes;
   3. `DescribeRefusal` (`:584-601`) — the pre-case gate;
   4. `ValidateAuditCannotBeManuallyCreated` (`:548-559`);
   5. the reason bound (`:445-456`);
   6. the principal-code bound (`:457-467`);
   7. the suggested-vs-confirmed principal split (`:476-480`).
   **A second implementation is a stop condition** — if a rule ends up in both
   places, stop and consolidate (`docs/engineering.md` § One Core owner).
4. **Confirm and close the gateway contract** from [[GWY-008]] (plan handle
   `DSK-03-08`). Three checks:
   - `POST /api/v1/cases` carries the **whole three-write sequence**
     server-side — correction, address resolution, acceptance — with the version
     chain taken from each write's return and `ExpectedReceiptVersion` not
     advanced on a mid-sequence failure (assumption `A-05-12`). If it has not
     folded the sequence, **stop and raise it on [[GWY-008]]**;
   - the outcome vocabulary distinguishes **created / withheld / failed**
     exactly as `IntakeAllocationProjectionStatus` does
     (`Create.cshtml.cs:377-384`);
   - the **six** failure branches (`:391-424`) map to six distinct problem types
     (assumption `A-05-13`).
   Add a contract fact that replaying the same `operationKey` returns the same
   result rather than allocating a second reference.
5. **DTOs in `src/Pegasus.Contracts`** — the create request, the draft read, and
   a **provenance value per field** from the closed list at
   `docs/design/README.md:177`. The draft read must carry the extraction
   candidates (assumption `A-05-14`) or provenance cannot be shown without a
   desktop-side rule.
6. **`CaseCreateViewModel`** in `src/Pegasus.Desktop`: immediate field-level
   validation using the deterministic Core rules referenced directly from
   `Pegasus.Core` (permitted by the reuse-map boundary note); server validation
   surfaced next to the owning section; a deliberate Save; and **one stable
   `operationKey` generated per create attempt and reused on retry** — a new key
   only when the operator deliberately starts again.
7. **Unsaved state lives in the view model.** Where a local draft is justified
   (proposal §11.1) persist it **encrypted** through the credential/cache
   abstraction from [[FND-031]] (plan handle `DSK-02-06`) — never a `TempData`
   equivalent, never the `RetainableFormFields` allow-list, never the
   8 000 / 2 000-character budgets
   (`src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs:38-88`).
8. **The create XAML** on the form pattern from [[DUI-008]] (plan handle
   `DSK-06-08`): label and control only; no hint text, no "Required."/"Optional."
   prose; required state shown visually. A provenance glyph with its one-word
   tooltip beside each populated field per [[DUI-011]] (plan handle
   `DSK-06-11`), on hover **and** keyboard focus with a matching accessible
   name. Sections per `docs/desktop/06-ui-design/screen-specs.md:233-245`:
   Principal and instruction, Vehicle, Inspection address, Dates.
   `CaseType.Audit` is **absent** from the dropdown, not disabled. AutomationIds
   `CaseCreate.<Section>.<Field>`, `CaseCreate.Submit`.
9. **View-model tests** in `tests/Pegasus.Desktop.ViewModelTests` (project from
   [[FND-038]], plan handle `DSK-02-13`): field validation; dirty state; the
   deliberate-save gate; operation-key reuse on retry and a fresh key on a
   deliberate restart; and each of the three allocation outcomes rendered with
   the approved copy — the refusal uses exactly "No case or reference was
   created; review the missing or conflicting evidence."
   (`docs/design/README.md:404`) and keeps proposed values in memory.
10. **Contract tests** in `tests/Pegasus.Api.ContractTests`: create success;
    replay returning the same result; validation failure as a problem document;
    401; 403 without `PerformCasework`; and one fact per distinct failure branch
    from step 4. Enable `Features:DesktopGateway` explicitly.
11. **Fixture comparison.** For the QDOS fixture set used by
    `tests/Pegasus.IntegrationTests/QdosIntakeWebTests.cs` and
    `QdosAllocationRecoveryTests.cs`, create through the web page and through the
    desktop and confirm the allocation outcome and reference behaviour are
    identical. Record the table in `proof`. **This covers the draft path only** —
    the blank path has no web oracle and is proved by step 3's minimum-draft
    characterization plus step 10's contract facts.
12. **Operator step — UAT on the genuine corpus.** Run the case-create UAT
    script on the local Test/UAT stack (tier 8, local only; corpus material is
    never committed). The operator confirms the outcomes and signs the parity
    row; capture their sign-off text and date in `proof`.
13. **Documentation and PR.** Update
    `docs/desktop/01-inventory-and-parity/parity-matrix.md` row `PAR-09` (`:54`);
    add the create section to `docs/frd/frd-13-desktop-operator-experience.md`
    — **including the "from blank" path recorded as a new capability with no web
    predecessor** — and a `DSK` row to `docs/capabilities.md`. Run the
    simplification pass over the branch diff (`AGENTS.md` step 4), record it
    under a dated `## Simplification pass` heading here, then open the PR into
    `dev`.

## Verification

Evidence tiers from the body: **tier 2** (Core/domain), **tier 5**
(Web/API/MCP caller), **tier 7** (Browser/accessibility).

| Command | Expected | Evidence captured |
| --- | --- | --- |
| `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build` | The new draft-to-case characterization facts pass and the pre-existing Core facts stay green | Test summary — **tier 2 evidence**, covering positive, contradictory, ambiguous and failure cases per rule |
| `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` | Create, replay, validation-problem, 401, 403 and the six failure-branch facts pass | Test summary — **tier 5 evidence** |
| `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` | Validation, dirty-state, operation-key and outcome facts pass | Test summary |
| `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` | `CaseCreateWebTests`, `CaseAcceptanceReplayTests`, `InstructionDraftWebTests`, `ProviderInspectionModeAcceptanceTests` and the QDOS web tests all green **after** the rules move into Core | Test summary — the regression gate for step 3 |
| `dotnet build ./Pegasus.slnx --configuration Release --no-restore` | Succeeds under `TreatWarningsAsErrors=true` | Build log tail |
| Fixture comparison on the Test/UAT stack | Identical allocation outcome and reference behaviour, web vs desktop, per fixture | The table in `proof` |
| UAT record | Named operator sign-off with date for the create workflow | The sign-off text and date in `proof` — **tier 7/8 evidence**; corpus material itself stays local |

## Risks / open questions

- **Risk, and the one the ticket exists to manage: a rule ends up implemented
  twice.** Seven rules move. *Mitigation:* step 3 moves them **one at a time**,
  test first, with the Razor page re-pointed in the same commit; a second
  implementation is a stop condition, not a migration step
  (`docs/engineering.md` § One Core owner).
- **Risk: `EffectiveInspectionAddress` moves with the wrong branch order.** It
  picks between `AddressResolution.ResolvedValue`, `AddressSuggestion?.Value`
  and `Ext18InspectionAddressPolicy.ImageBasedAssessment` in a fixed order
  (`Create.cshtml.cs:562-582`), and the failure is silent — a case created with
  the wrong address. *Mitigation:* it is characterized **first**, with all three
  branches covered.
- **Risk: the gateway does not fold the three-write sequence.**
  `endpoint-map.md:53` shows one row; the web performs three writes.
  *Mitigation:* step 4 checks it, and the correct action is to **stop and raise
  it on [[GWY-008]]** (plan handle `DSK-03-08`) — a scope boundary a named
  sibling owns. Orchestrating from the desktop would own the version chain and
  the replay fingerprint, which `Create.cshtml.cs:29-41` forbids.
- **Risk: the six failure branches collapse into one problem type.**
  *Mitigation:* step 4's third check and one contract fact per branch at
  step 10.
- **Divergence from current behaviour, carried by the settled body: "create
  from blank".** No such path exists on the web — both handlers are
  receipt-scoped (`Create.cshtml.cs:216-220`, `:268-272`). The body requires it
  and the body outranks this plan, so it is built as **new capability**, sized
  in the estimate, proved by its own contract and characterization facts, and
  recorded in FRD-13 as having no web predecessor. It is deliberately **not**
  in the step-11 parity table, because there is nothing to compare it against.
  Raised to the reviewer here rather than resolved silently.
- **Risk: provenance computed on the desktop.** `ProvenanceWord`
  (`src/Pegasus.Web/Presentation/InstructionDraftFieldsView.cs:58-60`) needs the
  extraction candidates. *Mitigation:* step 5 puts a provenance value per field
  on the wire (assumption `A-05-14`); if the draft read does not carry it, raise
  it on [[GWY-008]] rather than inferring.
- **Risk: the received-item correction screen regresses.**
  `InstructionDraftFieldsView` has a second caller (`:9-22`). *Mitigation:*
  `InstructionDraftWebTests.cs` is in the regression command at step 3, and
  [[FEAT-009]] (plan handle `DSK-05-09`) owns that screen.
- **Scope boundary: editing an allocated case.** Save, lease and completeness
  are [[FEAT-005]] (plan handle `DSK-05-05`); the workflow commands are
  [[FEAT-006]] (plan handle `DSK-05-06`).
- **Not an open question: the operator decisions are settled.** D-002, D-003 and
  D-004 do not touch this ticket, which performs no Azure write.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
