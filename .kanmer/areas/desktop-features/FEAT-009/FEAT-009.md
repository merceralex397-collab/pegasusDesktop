---
id: FEAT-009
type: ticket
title: 'DSK-05-09 · S9 Received items (intake detail, actions, bytes)'
status: preparing
area: desktop-features
assignee: ''
profile: feature
stageEntered:
  preparing: '2026-08-24T21:31:34.159Z'
labels:
  - desktop-conversion
  - plan-05
  - phase-5
  - tier-2
  - tier-5
  - tier-7
  - tier-8
groups:
  - EPIC-006
  - HZN-006
links: []
blocks:
  - FEAT-010
  - FEAT-011
  - FEAT-012
  - FEAT-013
  - FEAT-022
  - FEAT-025
  - TEST-016
refs:
  - docs/frd/frd-02-intake-and-source-identity.md
docs_todo: true
archived: false
created: '2026-08-24T07:49:10.235Z'
updated: '2026-08-25T00:29:23.253Z'
---

## What

Deliver the native Received item screen: classification evidence, field suggestions and extracted text, the ten explicit actions (retry allocation, block, re-evaluate, correct draft, claim case lease, link case, reverse case link, register vehicle images, dismiss suggestion) and streamed access to the source, assets and images.

## Why

Proposal §13.4 and §13.7 require failed-intake review and retry with full source-to-case traceability, which today lives in `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs` (613 lines, ten handlers at `:111`, `:157`, `:178`, `:192`, `:240`, `:274`, `:310`, `:513`, `:535` plus `OnGetAsync` at `:95`) with three byte pages — `Asset.cshtml.cs` (80), `Image.cshtml.cs` (79) and `Source.cshtml.cs` (78) — returning bytes through Core `DownloadIntakeSource`. The Phase 5 exit gate requires duplicate and failure paths to pass with no desktop holding Graph credentials. The operator word is "Received item" — `intake` is banned from operator copy. Siblings: [[DSK-05-05]] supplies the case lease session used by link/reverse-link, [[DSK-03-10]] the endpoints, [[DSK-05-10]] and [[DSK-05-11]] build on this surface.

## Source of truth

- Plan row: `docs/desktop/05-implementation-and-migration/README.md` § 5 — `DSK-05-09`
- Plan detail: `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S9 · Received items — intake detail, actions and bytes (DSK-05-09)`
- Endpoint map: `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Intake (received items), uploads, image intake`
- Screen spec: `docs/desktop/06-ui-design/screen-specs.md` § `§13.4 Intake` → `Received item (intake receipt detail)`
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 13.4 Intake, § 13.7 Documents and evidence, § 12.1 Microsoft Graph intake
- Upstream carry-over, one route each — **`vertical-slices.md` § S9 claims this slice absorbs all four and that is false for three of them; the fork board ids and the upstream ids also differ, so read this list rather than a bare id anywhere:**
  - **upstream INTK-001** (*honest queued upload status*) — absorbed, **no fork ticket**. Owned jointly by [[DSK-03-11]] (the `GET /uploads/{receiptId}/status` payload: `dueAtUtc`, the `retry_scheduled` state, the association-or-link `caseId`) and [[DSK-05-13]] (the operator surface and the waiting word). This slice supplies the Received item screen a completed receipt opens into and implements none of it. **Note the collision: the board's `INTK-001` is upstream INTK-002, an unrelated chore.**
  - **upstream INTK-004** (*reconcile intake decision labels and the Operations case-link claim with the code*) — absorbed, **no fork ticket, and not here**. Its label half is [[DSK-05-23]]'s, which folds `Intake/Details.cshtml.cs:349-360` and `Mail/Message.cshtml.cs:1019-1020` into the one `OperatorLabels` list and reconciles `OcrRequired` and `TechnicalFailure` against the binding `docs/design/README.md:541-542` table as its one stated exception. Its Operations half is [[DSK-05-20]]'s, which settles whether the received-intake row carries a real case link or the claim leaves `docs/current-architecture.md`. This slice renders labels through [[DSK-05-23]]'s list and changes no label text. **Note the collision: the board's `INTK-004` is upstream INTK-027, a different ticket entirely — see the next line.**
  - **upstream INTK-027 (board [[INTK-004]])** (*make policy re-evaluation work after transient staging cleanup*) — **imported as its own fork ticket and it does not arrive by sync**: it is `backlog` upstream with no branch, so [[DSK-01-10]]'s pinned range brings nothing. It is a live production defect on the `re-evaluate` action this screen exposes, and this slice's own scope boundary forbids the fix (`src/Pegasus.Infrastructure` and `src/Pegasus.Worker` are out of bounds), which is precisely why it has its own ticket. Do not fix it here and do not work around it; the re-evaluate command is wired to Core as it stands.
  - **upstream INTK-033 (board [[INTK-007]])** (*a triage-request email creates no Triage and no Unidentified item*) — **imported as its own fork ticket and it does not arrive by sync**: it is at `review` upstream on the unmerged branch `task/intk-033-triage-from-intake` (commit `7b43ab17`), outside [[DSK-01-10]]'s pinned range, so under D-001 it vanishes at the freeze unless the fork ticket carries it. It closes the composition gate behind [[DSK-05-11]] and [[DSK-05-12]] and brings `ITriageQueries.GetByOriginReceiptAsync`.
- Repository evidence: `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:95-560`, `src/Pegasus.Web/Pages/Intake/Asset.cshtml.cs`, `Image.cshtml.cs`, `Source.cshtml.cs`; `src/Pegasus.Core/Intake/IntakeQueryUseCases.cs:5` and `:43`, `src/Pegasus.Core/Intake/IntakeAllocation.cs:208`, `src/Pegasus.Core/Intake/DurableIntake.cs:1109` (`ILinkIntake`); `tests/Pegasus.IntegrationTests/QdosIntakeWebTests.cs`, `IntakeStablePersistenceTests.cs`, `MultiFormatIntakeWebTests.cs` (1,429 lines), `LocalIntakeAccessTests.cs`
- Binding decisions: L-01 the gateway brokers the artifact store and the commands; L-02 the genuine-corpus run is local only, never an Azure test resource; L-04 routing named on the ticket
- Depends on: `DSK-05-05` the case lease session reused by link and reverse-link; `DSK-03-10` the received-item detail, ten commands and three byte endpoints

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) → `minimal-api-file-upload` (dotnet/skills `98f84851`, `plugins/dotnet-aspnetcore/skills/minimal-api-file-upload/SKILL.md`) → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`, `microsoft_code_sample_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, `vertical-slices.md` § S9, the screen spec section and `docs/design/README.md` banned-words list (`intake`, `artifact`, `bytes`, `durable` all appear in the code but must never reach the operator). Read the four upstream carry-over lines under Source of truth and note that § S9's claim to absorb all four is wrong for three. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/dsk-05-09-received-items` and worktree `../pegasus-worktrees/dsk-05-09-received-items` from `origin/dev`.
2. Read `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs` in full and tabulate the ten handlers in `research`: handler name and line, the Core use case called, the required `expectedVersion` / `operationKey` / `reason`, the operation-key length bound Core enforces, and the failure paths. Read the three byte pages and record how each validates and streams (`DownloadIntakeSource`, SHA-256 validation, safe filename). Record the SHA read.
3. Identify any behaviour that lives only in the page model — in particular the link and reverse-link integrity checks and the re-evaluation preconditions listed as characterization gaps in `docs/desktop/05-implementation-and-migration/README.md` § 3. Load `code-testing-agent`, write characterization tests in `tests/Pegasus.Core.Tests` against current behaviour first, then move the rule into `src/Pegasus.Core/Intake/` and re-point the Razor page. A second implementation is a stop condition. Characterize the re-evaluation preconditions **as they behave today**, including the transient-staging failure upstream INTK-027 (board [[INTK-004]]) reports — record it as a known defect owned there, and do not encode the broken behaviour as intended.
4. Confirm the endpoints from [[DSK-03-10]]: `GET /api/v1/received/{id}`, the ten named commands, and `GET /api/v1/received/{id}/source|assets/{aid}|images/{iid}` with `Content-Length`, weak `ETag`, range support, `X-Content-Type-Options: nosniff` and a safe filename. Load `minimal-api-file-upload` for the byte-endpoint conventions.
5. Add the received-item DTOs to `src/Pegasus.Contracts`, including classification evidence, field suggestions with provenance, extracted-text availability, and the read-only typed draft (the draft is editable only on the create screen, [[DSK-05-04]]).
6. Implement `ReceivedItemViewModel` in `src/Pegasus.Desktop` with one command object per action, each carrying its own `operationKey` and the receipt `expectedVersion`, and each surfacing the shared conflict pattern from [[DSK-05-08]] on 409. The link and reverse-link commands additionally acquire the case edit lease through the session from [[DSK-05-05]].
7. Implement byte access in `src/Pegasus.Desktop.Infrastructure` as a **streaming** download with progress and cancel — never buffer a whole source or image in memory — writing to a per-user temporary path with restrictive ACLs and bounded retention as area 10 specifies.
8. Build the screen XAML: the operator vocabulary is "Received item"; blocked and withheld states carry only the approved necessary copy (`Blocked — a reason is required.`, `No case or reference was created; review the missing or conflicting evidence.`); only populated sections render; every control has an `AutomationId`. Render every decision label through [[DSK-05-23]]'s single `OperatorLabels` list and change no label text here (upstream INTK-004 is that ticket's).
9. Add contract tests in `tests/Pegasus.Api.ContractTests` for each of the ten commands (success, 401, 403, 409 stale version, replay returns the same result, Core failure path) and for each byte endpoint (200 with `ETag` and no-sniff, range request, 404, 403). Enable `Features:DesktopGateway` explicitly.
10. Add view-model tests in `tests/Pegasus.Desktop.ViewModelTests` for the ten commands' `CanExecute` gating, the reason-required commands, streaming progress and cancellation, and the read-only draft rendering.
11. Run the genuine-corpus comparison locally (tier 8): for the reviewed cohort used by `tests/Pegasus.IntegrationTests/MultiFormatIntakeWebTests.cs`, compare web and desktop outcomes for each of the ten actions. Corpus material and detailed evidence stay local and are never committed; record only the pass/fail table in the ticket proof. A re-evaluate divergence traceable to upstream INTK-027 (board [[INTK-004]]) is recorded against that ticket, not fixed here.
12. Update `docs/desktop/01-inventory-and-parity/parity-matrix.md` rows for the ten handlers and the three byte pages, add the received-items section to `docs/frd/frd-13-desktop-operator-experience.md`, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading, then open the PR into `dev`.

## Acceptance criteria

- [ ] All ten actions are available as explicit, audited commands with the receipt version and an operation key.
- [ ] Source, asset and image bytes stream with progress and cancel; nothing is fully buffered in memory.
- [ ] Outcomes equal the web for the fixture set, including blocked and withheld states.
- [ ] Blocked and withheld states carry the approved necessary copy only; the word `intake` never appears in operator copy.
- [ ] The typed draft is read-only on this screen.
- [ ] Link and reverse-link integrity rules live in `Pegasus.Core` with characterization tests.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build` — expected: link/reverse-link integrity and re-evaluation characterization facts pass.
- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build` — expected: ten command matrices and three byte-endpoint facts pass.
- [ ] `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build` — expected: command gating, streaming and draft facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"` — expected: existing intake web tests stay green after any rule moves into Core.
- [ ] Corpus comparison table in the ticket proof — expected: desktop outcomes equal web outcomes across the reviewed cohort, with no corpus content committed.

## Evidence tier

Tier 2 — Core/domain. Tier 5 — Web/API/MCP caller. Tier 7 — Browser/accessibility. Tier 8 — Genuine corpus.
Tier 2 obliges positive, contradictory, ambiguous and failure cases for the link, reverse-link and re-evaluation rules; tier 5 obliges route-level evidence per command and per byte endpoint; tier 7 obliges keyboard and semantic-label evidence from a real run; tier 8 obliges the immutable reviewed cohort run through the real caller, with detailed evidence kept local and untracked.

## Documentation changes

- `docs/desktop/01-inventory-and-parity/parity-matrix.md` — rows for the ten intake handlers and the three byte pages
- `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S9 · Received items` — correct the "Absorbs upstream" line: this slice absorbs neither upstream INTK-027 (imported as board [[INTK-004]]) nor upstream INTK-033 (imported as board [[INTK-007]]), and upstream INTK-004 is [[DSK-05-23]]'s and [[DSK-05-20]]'s, not this slice's. Coordinate with [[DSK-01-09]], which holds the carry-over join table, so the line is changed once.
- `docs/frd/frd-13-desktop-operator-experience.md` — received-items section
- `docs/capabilities.md` — `DSK` rows for received-item review and actions

## Guardrails

- **Azure**: no write. The artifact store is reached only through the gateway; no desktop code touches an Azure SDK.
- **Scope boundary**: may touch `src/Pegasus.Desktop`, `src/Pegasus.Desktop.Infrastructure`, `src/Pegasus.Contracts`, the `/api/v1` received group in `src/Pegasus.Web`, `src/Pegasus.Core/Intake/` only for rules moved in with a characterization test, and the test projects. Must not touch `src/Pegasus.Infrastructure` (readers stay central), `src/Pegasus.Worker`, or the Razor intake pages — which is exactly why upstream INTK-027 (board [[INTK-004]]) and upstream INTK-033 (board [[INTK-007]]) are their own tickets rather than work this slice may do.
- **Traps**: the desktop never parses source documents — readers stay server-side; binary endpoints must be streamed, not buffered; banned operator words include `intake`, `artifact`, `durable`, `bytes`; page-model rules that are business logic move into Core with a test first; `Features:DesktopGateway` must be enabled in tests. **The four upstream intake ids have four different routes, and `vertical-slices.md` § S9's "absorbs all four" claim is wrong for three** — read the Source-of-truth list before touching any of them: upstream INTK-001 is absorbed with no fork ticket and belongs to [[DSK-03-11]] and [[DSK-05-13]]; upstream INTK-004 is absorbed with no fork ticket and belongs to [[DSK-05-23]] and [[DSK-05-20]]; **upstream INTK-027 is imported as board [[INTK-004]]** and **upstream INTK-033 as board [[INTK-007]]**, and **neither arrives by upstream sync** — INTK-027 is `backlog` upstream with no branch at all, and INTK-033 is at `review` on the unmerged branch `task/intk-033-triage-from-intake`, outside [[DSK-01-10]]'s pinned range, so under D-001 both vanish at the freeze unless their fork tickets carry them. Waiting for a sync to deliver either is the mistake this trap exists to prevent, and duplicating either fix here is the other. **Upstream ids and fork board ids do not match**: the board's `INTK-001`…`INTK-007` are upstream INTK-002, INTK-003, INTK-026, INTK-027, INTK-031, INTK-032 and INTK-033 — never write a bare intake id, always `upstream <ID> (board <board-id>)` or "absorbed, no fork ticket". `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` is the register; [[DSK-01-09]] step 3 holds the join table.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
