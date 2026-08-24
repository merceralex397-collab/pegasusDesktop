# Plan — FEAT-015: S15 Vehicle lookup and EVA handoff

**Diff estimate: ~17 files, ~1,950 lines** if `CaseVehicleViewModel` already
exists from [[FEAT-036]] (plan handle `DSK-07-10`); **~19 files, ~2,500 lines** if
it does not and this slice creates it to that ticket's pinned shape. Step 7
records which case applied and the estimate is restated then.

Derived from the `files` document, not asserted. `src/Pegasus.Contracts` vehicle
and EVA DTOs — 3 files, ~260 lines (lookup request and status, suggestion with
source and obtained-at, mileage observations with their three-way classification,
handoff revision); extensions to `CaseVehicleViewModel` and `CaseVehicleView.xaml`
(lookup-status refresh, EVA generate, EVA download, freshness header, five
provider states rendered distinctly) — 3 files, ~430 lines, plus ~550 if created
rather than extended; streamed-download wiring in
`src/Pegasus.Desktop.Infrastructure` — 1 file, ~70 lines; `/api/v1` gap-closing in
`src/Pegasus.Web` — 1 file, ~60 lines; `tests/Pegasus.Api.ContractTests` — 3
files, ~560 lines (the provider-taxonomy matrix over the replay adapter, the
no-key assertion, and the `EvaBundleContent` suite that diffs entry list, layout
and thirteen values against two samples); `tests/Pegasus.Desktop.ViewModelTests` —
2 files, ~330 lines; `tests/Pegasus.ArchitectureTests` — 1 file, ~50 lines;
documentation — 3 files, ~90 lines.

## Approach

Assert the EVA bundle's **content** — entry list, JSON layout and the thirteen
field values — as a first-class gate rather than proving only that generate and
download run, because `docs/desktop/05-implementation-and-migration/reuse-map.md:36`
marks `Eva/` REUSE and therefore obliges byte-identical output, and because a
slice that verifies only the two commands can sign off on a package EVA rejects.
That is not hypothetical: it is exactly what upstream hit on 2026-08-24 exporting
`ap.QDOS26015`. The alternative considered and rejected was asserting the
schema rather than the bytes — checking that thirteen keys exist and are typed
correctly. It would pass today with the wrong `Reference` value, a one-line
`Inspection Address`, a `Vehicle Model` missing its make and a mis-cased
`Mileage Unit`, which are the four defects upstream ENG-015 (board [[ENG-002]])
exists to fix. Diffing against the operator-supplied corpus in
`reference/eva_information/` catches all four and costs two 700-byte fixtures.
Everything else on this tab extends types [[FEAT-036]] owns and calls the Core
normalisation rule rather than copying it.

## Governing docs

The ticket carries two refs, and **both files exist today**
(`ls docs/frd` shows `frd-01`…`frd-12`), so this section is a genuine Meets
rather than a New-ADR placeholder. `docs_todo: true` is also set (confirmed in
`get_doc_gates FEAT-015`, which reports `governing-doc` satisfied at
`leave-backlog`).

**Meets — `docs/frd/frd-06-vehicle-and-engineering-evidence.md`.** Step 6 keeps
one registration normalisation rule, in Core, so vehicle identity is established
the same way whatever the client; step 7 renders each provider state distinctly so
a provider failure is never presented as a genuine not-found; step 8 makes
cached-lookup freshness visible without hovering; and step 11 evidences the whole
provider error taxonomy at route level through the replay adapter.

**Meets — `docs/frd/frd-07-eva-and-external-engineering-handoff.md`.** Step 9
implements generate and download as explicit commands carrying the reason Core
requires, and step 10 pins the handed-off bundle's entry list, JSON layout and
thirteen field values against the operator-supplied corpus — which is what makes
the handoff verifiable rather than assumed.

Neither FRD is modified by this ticket.

> **New ADR** — ADR-0107 (Box and DVLA/DVSA credentials stay behind the gateway;
> no long-lived provider secret in the package), authored by [[FND-005]] (plan
> handle `DSK-00-05`). **Consumed, not authored, by this ticket** — steps 11 and
> 13 produce the no-key and no-live-call evidence ADR-0107 will cite.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3; if the ADR lands
> differently this plan is revised before implementation.

> **New ADR** — ADR-0103 (gateway; never direct database access from
> workstations), authored by [[FND-005]]. Same condition.

Programme-level authorities that also bind today, for `kanmer-review` to check
against the diff:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal §12.3, §13.5 | Vehicle identity, lookups, mileage history, and the source and timestamp of external data, with provider secrets absent from the client | Steps 5–8, 11 |
| Proposal §16.2 | External provider resilience — failure classes distinguishable, not collapsed | Steps 4, 7 |
| `docs/desktop/05-implementation-and-migration/reuse-map.md:36` | `Eva/` is REUSE: the desktop ships byte-identical output | Step 10 |
| `docs/desktop/05-implementation-and-migration/reuse-map.md:42-48` | The desktop may reference `Pegasus.Core` for deterministic validation, never `Pegasus.Infrastructure`, EF Core, Azure SDKs, Box or Graph SDKs | Steps 6, 11 |
| `docs/desktop/06-ui-design/screen-specs.md:319-330` | Five provider states distinct from not-found; staff confirmation never overwritten by refresh; the four AutomationIds | Steps 7, 8 |
| `docs/design/README.md` § Voice | Permanent consequences visible without hover or colour alone — applied to cached-versus-fresh | Step 8 |
| `docs/engineering.md` § One Core owner | One normalisation rule, one EVA mapping, one view model per screen, one byte path | Steps 6, 7, 9, 10 |
| L-01 | The gateway holds the provider keys and the shared lookup cache | Steps 3, 11 |
| L-02 / ADR-0014 | Test/UAT uses the replay adapter; never a live provider and never an Azure test resource | Steps 11, 13 |
| L-04 | Subagent, skills and MCP named on the ticket | § Routing below |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 14 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`;
  `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi`
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

1. **Orient and take.** Read the plan row `DSK-05-15`,
   `docs/desktop/05-implementation-and-migration/vertical-slices.md:523-549`,
   `docs/desktop/06-ui-design/screen-specs.md:319-330`,
   `docs/frd/frd-06-vehicle-and-engineering-evidence.md` and
   `docs/frd/frd-07-eva-and-external-engineering-handoff.md`. Call
   `get_doc_gates FEAT-015`, then `take_ticket` with branch
   `task/dsk-05-15-vehicle-eva` and worktree
   `../pegasus-worktrees/dsk-05-15-vehicle-eva` from `origin/dev`.
2. **Read and record.** Read `src/Pegasus.Web/Pages/Cases/Vehicle.cshtml.cs` and
   `src/Pegasus.Web/Pages/Cases/Eva/Download.cshtml.cs` in full. Append to
   `research`: how the lookup request becomes a durable work item the Worker
   executes; what the accept command writes; the registration normalisation rule
   and where it lives in `src/Pegasus.Core/Vehicle/`; the mileage policy inputs;
   and the reason the EVA download requires — noting that
   `Eva/Download.cshtml.cs:21-28` also requires `expectedVersion` and an
   `editLeaseToken` and can return `Conflict` or `Refused`. **Record the SHA read.**
3. **Confirm the endpoints.** From [[FEAT-035]] (plan handle `DSK-07-09`) and the
   endpoint map: `POST /api/v1/cases/{id}/vehicle/lookups`,
   `POST /api/v1/cases/{id}/vehicle/suggestions/{sid}/accept`,
   `POST /api/v1/cases/{id}/eva-handoff` and the bundle read. Confirm the response
   carries the **cache lifetime** and the **provenance fields** (source and
   obtained-at) the screen must show, and settle the bundle read's shape with
   [[FEAT-035]] — it must carry `revision`, `expectedVersion`, `operationKey`,
   `reason` and `editLeaseToken`, which the endpoint map's `GET` row cannot.
4. **Confirm the error taxonomy.** From [[FEAT-045]] (plan handle `DSK-07-19`):
   `terminal` / `transient` / `unknown` alongside `not-found`, `invalid-request`,
   `not-authorized`, `rate-limited` and `unavailable`. **A provider failure must be
   distinguishable from a genuine not-found in the contract, not inferred by the
   client.**
5. **Contracts.** Add the vehicle and EVA DTOs to `src/Pegasus.Contracts`
   *(created by [[FND-029]], plan handle `DSK-02-04`)*, including the suggestion
   with its source and timestamp and the handoff revision identifier.
6. **Normalisation — call, do not copy.** Implement registration normalisation in
   the desktop by calling the **existing** Core rule from
   `src/Pegasus.Core/Vehicle/`; the boundary note at
   `docs/desktop/05-implementation-and-migration/reuse-map.md:42-48` permits a
   direct `Pegasus.Core` reference for deterministic validation. Do not write a
   second normalizer; the gateway re-checks on write.
7. **`CaseVehicleViewModel` — extend or create.** Check whether it already exists
   from [[FEAT-036]], which owns that type and its view. **If it does**, add the
   lookup-status refresh and the EVA handoff generate and download commands in
   place and change no existing member. **If it has not landed**, create it with
   exactly the members [[FEAT-036]] step 5 pins (`ObservableObject`,
   `[ObservableProperty]` partial properties, `[RelayCommand]`, and the shared Core
   normalisation rule reused rather than a second copy) and **record here which
   case applied**. Either way this slice's own surface is the same: request lookup,
   poll or refresh status, accept a suggestion showing source and obtained-at
   beside the value, and render each provider state distinctly using the shared
   vocabulary — **never one generic "failed"**. Never a second view model for the
   Vehicle tab.
8. **Freshness without hovering.** Show cached-lookup freshness explicitly using
   the header control from [[DUI-012]] (plan handle `DSK-06-12`), so an operator
   can tell a fresh answer from a cached one without hovering — the same rule
   `docs/design/README.md` applies to permanent consequences.
9. **EVA generate and download.** Implement both as explicit commands; the
   download is a **streamed** transfer reusing the service from [[FEAT-014]] (plan
   handle `DSK-05-14`) and carries the reason the Core download requires, plus the
   version and lease `Eva/Download.cshtml.cs:21-28` shows.
10. **Assert the bundle's CONTENT, not only that the commands run.** Generate a
    bundle on the local Test/UAT stack from the seeded case and add a test in
    `tests/Pegasus.Api.ContractTests` that pins two things against the
    operator-supplied corpus:
    **(a)** the archive's entry list — the thirteen-key JSON plus `Images/` and
    **nothing else** (the folder holds no `manifest.sha256` and no
    `provenance.json`) — and the JSON's layout: **two-space indentation** with the
    same key set and key order, diff clean against
    `reference/eva_information/AX_SP58WVO.json`;
    **(b)** the thirteen field values — `Work Provider`, `VRM`, `Vehicle Model`,
    `Claimant Name`, `Reference`, `Incident Date`, `Instruction Date`,
    `Inspection Date`, `Inspection Address`, `Accident Circumstances`,
    `VAT Status`, `Mileage`, `Mileage Unit` — matching the known-good samples
    `AX_SP58WVO.json` and `Final Format Example 02.json`, with **`Reference`
    carrying the work provider's claim number rather than our case reference**
    (`reference/eva_information/eva_information.md:31-45`), **`Inspection Address`
    carrying exactly six lines** (five `\n` separators in both samples), and
    **`Vehicle Model` carrying make and model**.
    Record the run in the proof. **If the assertion fails**, the fix belongs to
    **upstream ENG-014 (board [[ENG-001]])** for packaging and indentation and
    **upstream ENG-015 (board [[ENG-002]])** for the field values, in
    `src/Pegasus.Core/Eva/` and
    `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs`; both are already
    imported onto this board, sequenced upstream ENG-014 then upstream ENG-015 so
    the archive bytes change once. **Raise it there; do not write a second EVA
    mapping in the desktop or the gateway.**
11. **Contract tests over the replay adapter.** In
    `tests/Pegasus.Api.ContractTests` *(created by [[TEST-001]], plan handle
    `DSK-08-01`)*, using `DvlaDvsaReplayAdapter`
    (`src/Pegasus.Infrastructure/Vehicle/DvlaDvsaAdapters.cs:7`): success,
    not-found, each provider failure class, rate-limited, 401, 403, 409 stale
    version, replay of the same `operationKey`, and an assertion that **no provider
    key appears in any response**. Enable `Features:DesktopGateway` explicitly.
12. **View-model tests.** In `tests/Pegasus.Desktop.ViewModelTests` *(created by
    [[TEST-004]], plan handle `DSK-08-04`)*: normalisation delegating to Core, each
    provider state rendering distinctly, freshness display, accept updating the case
    version, and EVA generate-then-download.
13. **Test/UAT record.** Run the replay-adapter integration check on the local
    stack per `docs/desktop/08-testing/test-uat-stack.md` and record in the proof
    that **no live provider call was made**.
14. **Documentation, simplification pass, PR.** Update `parity-matrix.md` row
    `PAR-14`. **Do not edit `PAR-18`** — [[FND-018]] (plan handle `DSK-01-05`) owns
    that row and writes that EVA parity covers the bundle's CONTENT; this ticket
    supplies the evidence. Add the EVA handoff behaviour **inside the Vehicle tab
    section [[FEAT-036]] creates** in
    `docs/frd/frd-13-desktop-operator-experience.md`, citing FRD-06 and FRD-07 — a
    sub-heading under that section, **not a second vehicle section** (the file is
    created by [[DUI-013]], plan handle `DSK-06-13`; contribute the content there
    if it has not landed). Add the `DSK` rows to `docs/capabilities.md`. Run the
    simplification pass over this branch's diff, record it under a dated
    `## Simplification pass` heading below, then open the PR into `dev`.

## Verification

Evidence tiers from the body: **5** (Web/API/MCP caller), **7**
(Browser/accessibility).

- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build`
  — lookup, accept and EVA facts pass across the full provider error taxonomy with
  the replay adapter (tier 5: authorization, idempotency and deterministic
  external-failure translation).
- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~EvaBundleContent"`
  — the generated bundle's entry list and JSON layout diff clean against
  `reference/eva_information/AX_SP58WVO.json` and the thirteen field values match
  the known-good samples.
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`
  — normalisation, provider-state, freshness and EVA facts pass.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build`
  — the desktop references no provider adapter and no second normalizer exists.
- Test/UAT record in the proof — replay adapter used, no live provider call, no
  key in package or logs.
- Tier 7: keyboard, focus, semantic-label and **text-plus-colour** evidence for
  the provider states from a real run, captured with the [[TEST-006]] (plan handle
  `DSK-08-06`) harness and the `axe-windows` scan from [[TEST-009]] (plan handle
  `DSK-08-09`).

## Risks / open questions

- **The endpoint map shows the bundle read as a `GET`, but the current download
  requires a reason and an edit-lease token** (`src/Pegasus.Web/Pages/Cases/Eva/Download.cshtml.cs:21-28`).
  A `GET` cannot carry either. Mitigation: step 3 settles the shape with
  [[FEAT-035]] before binding; this slice consumes whatever shape carries all six
  parameters and does not invent one. Answered by: [[FEAT-035]].
- **A failing content assertion.** It is a finding, not a fix here. Packaging and
  indentation → **upstream ENG-014 (board [[ENG-001]])**; field values → **upstream
  ENG-015 (board [[ENG-002]])**. Both are already on the board and need finding by
  those board ids, not creating. Neither arrives by sync — ENG-014 is on the
  upstream branch `task/eng-014-drop-manifest-indent-json` against `dev`, outside
  [[FND-023]] (plan handle `DSK-01-10`)'s 32-commit `main` range, and ENG-015 has
  no upstream branch at all — so under D-001 both exist only because the fork board
  holds them.
- **Id collision in this area.** Board `ENG-001` is upstream ENG-014; board
  `ENG-002` is upstream ENG-015; **upstream `ENG-001` is a different ticket
  entirely** (a post-alpha external-supplier capability, dropped and never
  imported), and upstream ENG-008, ENG-009, ENG-011 and ENG-013 have no fork
  tickets at all. Always `upstream <ID> (board [[<board-id>]])`, never a bare
  `ENG-0nn`.
- **Two view models for the Vehicle tab.** [[FEAT-036]] owns
  `CaseVehicleViewModel` and `CaseVehicleView.xaml`. Mitigation: step 7 has an
  explicit extend-or-create branch and records which applied; if [[FEAT-036]] lands
  mid-slice, the created type is reconciled with its pinned shape before either
  merges.
- **A second normalizer.** A stop condition. Mitigation: step 6 calls the Core
  rule and the architecture test at step 14's verification asserts no second
  implementation exists.
- **Provider states collapsed into one "failed".** Mitigation: step 4 confirms the
  taxonomy is on the contract, step 7 renders each distinctly, and step 12 asserts
  it.
- **A live provider call in Test/UAT.** Out of bounds (L-02, ADR-0014).
  Mitigation: the replay adapter is the only provider in that stack and step 13
  records the evidence.
- **Scope creep from upstream.** upstream ENG-013 arrives via sync; upstream
  ENG-009 (Cazana valuation) stays backlog and must not be pulled in. Neither has a
  fork ticket, so neither may be written as a board wiki-link.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
