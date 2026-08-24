# Plan — FEAT-009: S9 Received items (intake detail, actions, bytes)

**Diff estimate: ~26 files, ~2,750 lines.**

Derived from the `files` document, file group by file group, not asserted:
`src/Pegasus.Contracts` received-item DTOs — 4 files, ~260 lines (detail,
evidence/suggestion records, draft record, nine request records);
`src/Pegasus.Desktop` view model and XAML — 4 files, ~950 lines (nine command
objects over a 613-line page model's worth of state, five tabs);
`src/Pegasus.Desktop.Infrastructure` streaming service — 2 files, ~220 lines;
`/api/v1` received group gap-closing in `src/Pegasus.Web` — 1 file, ~80 lines;
Core rule move plus the Razor re-point — 3 files, ~180 lines;
`tests/Pegasus.Core.Tests/Intake` characterization — 2 files, ~340 lines;
`tests/Pegasus.Api.ContractTests` (nine commands × the seven-case matrix, three
byte endpoints × four cases) — 3 files, ~430 lines;
`tests/Pegasus.Desktop.ViewModelTests` — 2 files, ~340 lines; documentation — 5
files, ~150 lines.

## Approach

Mirror the page model one-for-one — nine command objects over nine explicit
`/api/v1` routes — rather than introducing any desktop-side dispatch, because
`Details.cshtml.cs` is *already* one handler per action and proposal §10.2 forbids
a generic action endpoint. The alternative considered and rejected was a single
`ReceivedCommand(name, payload)` object routed by string, as
`src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:85` does for Triage: it would be
fewer lines here, and it is exactly the shape [[FEAT-011]] (plan handle
`DSK-05-11`) exists to remove. Bytes are streamed through one service in
`src/Pegasus.Desktop.Infrastructure` rather than per screen, because
[[FEAT-011]] step 9 and [[FEAT-012]] (plan handle `DSK-05-12`) step 8 both bind
to it by name. Two page-model rules — the link/reverse-link integrity checks and
the re-evaluation preconditions — move into `src/Pegasus.Core/Intake/` behind
characterization tests written first, per
`docs/desktop/05-implementation-and-migration/README.md:158-170`; the
re-evaluation characterization records upstream INTK-027 (board [[INTK-004]])'s
transient-staging failure as a known defect rather than encoding it as intent.

## Governing docs

The ticket carries `refs: ["docs/frd/frd-02-intake-and-source-identity.md"]` and
`docs_todo: true` (confirmed in `get_doc_gates FEAT-009`, which reports
`governing-doc` satisfied at `leave-backlog`).

**Meets — `docs/frd/frd-02-intake-and-source-identity.md`.** Steps 5–8 render the
receipt's source identity, classification evidence and typed draft without
altering any of it, and steps 3 and 9–10 pin that the link and reverse-link
integrity rules and the re-evaluation preconditions behave identically through
`/api/v1` and through the Razor page. The FRD is not modified by this ticket.

> **New ADR** — ADR-0103 (gateway; never direct database access from
> workstations), authored by [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3; if the ADR lands
> differently this plan is revised before implementation.

> **New ADR** — ADR-0106 (Graph intake worker stays central: unattended
> execution, protected credentials), authored by [[FND-005]].
> Same condition.

> **New ADR** — ADR-0101 (local-execution / cloud-authority split and the
> six-question cloud-justification test), authored by [[FND-005]].
> Same condition. The `research` document's Execution placement table is written
> in the form ADR-0101 will require.

`refs` names one FRD and no ADR, so the programme-level authorities that bind
today are tabulated here for `kanmer-review` to check against the diff:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal §10.2 (via `endpoint-map.md` Conventions) | Commands are explicit verbs, never a generic action endpoint | Steps 4, 6 |
| Proposal §13.4, §13.7 | Failed-intake review and retry with full source-to-case traceability | Steps 5–8, 11 |
| L-01 (`docs/desktop/README.md` § Locked decisions) | Gateway is `Pegasus.Web` evolved in place; it brokers the artifact store and the commands | Steps 4, 7 |
| L-02 | Verification on the local Test/UAT stack; never an Azure test resource | Step 11 |
| L-04 | Every ticket names its subagent, skills and MCP tools | § Routing below |
| `docs/desktop/05-implementation-and-migration/README.md:158-170` | Characterization before moving any rule; a duplicate business implementation is a stop condition | Step 3 |
| `docs/design/README.md:396-421` | Approved necessary copy only; `intake`, `artifact`, `durable`, `bytes` never reach the operator | Step 8 |
| `docs/desktop/06-ui-design/screen-specs.md:271-285` | Tabs, read-only draft, sections only when populated, three AutomationId families | Step 8 |
| `docs/engineering.md` § One Core owner | One implementation of the streaming download and of every moved rule | Steps 3, 7 |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over this branch's own diff before the PR | Step 12 |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`;
  `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`;
  `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `dotnet-webapi`
  (dotnet/skills `98f84851`) → `minimal-api-file-upload` (dotnet/skills
  `98f84851`, `plugins/dotnet-aspnetcore/skills/minimal-api-file-upload/SKILL.md`)
  → `winui-dev-workflow` (`.codex/skills/winui-dev-workflow/SKILL.md`) →
  `winui-design` (`.codex/skills/winui-design/SKILL.md`) → `code-testing-agent`
  (dotnet/skills `98f84851`) → `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`, `microsoft_code_sample_search`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates <id>` before every move; a move crosses at most one gated
  boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's twelve implementation steps in the same order and
with the same ownership. They add the *how*; they change no decision.

1. **Orient and take.** Read `docs/desktop/05-implementation-and-migration/README.md`
   § 5 row `DSK-05-09`, `vertical-slices.md:333-373`,
   `docs/desktop/06-ui-design/screen-specs.md:271-285` and
   `docs/design/README.md:396-421`. Read the four upstream carry-over lines under
   the ticket's Source of truth and note that `vertical-slices.md:369-373`'s
   "absorbs all four" claim is wrong for three. Call `get_doc_gates FEAT-009`,
   then `take_ticket` with branch `task/dsk-05-09-received-items` and worktree
   `../pegasus-worktrees/dsk-05-09-received-items` from `origin/dev`.
2. **Tabulate the handlers.** Read `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs`
   in full and append to `research` a table of the nine POST handlers — handler
   name and line, Core use case called, required `expectedVersion` /
   `operationKey` / `reason`, the operation-key length bound Core enforces, and
   the failure paths. Then read `Source.cshtml.cs` (78), `Asset.cshtml.cs` (80)
   and `Image.cshtml.cs` (79) and record how each validates and streams —
   `DownloadIntakeSource` recomputes SHA-256 at `DownloadIntakeSource.cs:40` and
   compares it in fixed time at `:43`, and each page sets a safe filename.
   **Record the SHA read** (`git rev-parse HEAD`), because upstream keeps fixing
   the web app and each slice records the revision it characterized
   (`README.md:257-260`).
3. **Characterize, then move.** Load `code-testing-agent`. Write facts in
   `tests/Pegasus.Core.Tests/Intake/` for (a) the link and reverse-link integrity
   checks and (b) the re-evaluation preconditions, asserting **current**
   behaviour, before touching either. Then move each rule into
   `src/Pegasus.Core/Intake/` and re-point `Details.cshtml.cs` at it. Two
   implementations of the same rule is a stop condition. For re-evaluation,
   include the transient-staging failure upstream INTK-027 (board [[INTK-004]])
   reports as a **named known defect** in the test's own comment, owned there; do
   not encode the broken behaviour as intended and do not fix it here —
   `src/Pegasus.Infrastructure` and `src/Pegasus.Worker` are out of bounds.
4. **Confirm the endpoints.** Against the generated client, confirm [[GWY-010]]
   (plan handle `DSK-03-10`) has landed `GET /api/v1/received/{id}`; the nine
   named commands — `retry-allocation`, `block`, `reevaluate`, `correct-draft`,
   `dismiss-suggestion`, `register-image-intake`, `case-lease/claim`, `link-case`,
   `reverse-case-link`; and
   `GET /api/v1/received/{id}/source|assets/{aid}|images/{iid}` with
   `Content-Length`, a weak `ETag`, range support,
   `X-Content-Type-Options: nosniff` and a safe filename. Note from
   `endpoint-map.md` that the last three commands carry the case
   `expectedVersion` **and** the `editLeaseToken`, and the other six only the
   receipt version. Load `minimal-api-file-upload` for the byte conventions. If a
   route is missing or folded, stop and raise it on [[GWY-010]].
5. **Contracts.** Add the received-item DTOs to `src/Pegasus.Contracts`
   *(created by [[FND-029]], plan handle `DSK-02-04`)*: detail, classification
   evidence, field suggestions **with provenance**, extracted-text availability,
   and the read-only typed draft. The draft record is read-only on this screen —
   it is editable only on the create screen, [[FEAT-004]] (plan handle
   `DSK-05-04`).
6. **View model.** Implement `ReceivedItemViewModel` in `src/Pegasus.Desktop`
   *(created by [[FND-030]], plan handle `DSK-02-05`)* with one command object per
   action, each carrying its own `operationKey` and the receipt
   `expectedVersion`, and each surfacing the shared conflict pattern from
   [[FEAT-008]] (plan handle `DSK-05-08`) on 409. `link-case`, `reverse-case-link`
   and `case-lease/claim` additionally acquire the case edit lease through the
   session [[FEAT-005]] (plan handle `DSK-05-05`) owns. No dispatcher string
   anywhere.
7. **Streaming.** Implement byte access in `src/Pegasus.Desktop.Infrastructure`
   *(created by [[FND-031]], plan handle `DSK-02-06`)* as a **streaming** download
   with progress and cancel — never buffer a whole source or image — writing to a
   per-user temporary path with restrictive ACLs and bounded retention as area 10
   specifies. This is the one implementation; [[FEAT-011]] and [[FEAT-012]] bind
   to it by name.
8. **Screen.** Build the XAML to `screen-specs.md:271-285`: identity head, tabs
   Evidence / Draft / Decision / Case / History, only populated sections rendered,
   an `AutomationId` on every control (`Received.Header.<Field>`,
   `Received.Tabs.<Tab>`, `Received.Actions.<Action>`). Operator vocabulary is
   "Received item"; the blocked and withheld states carry only the approved
   necessary copy verbatim — `Blocked — a reason is required.`
   (`docs/design/README.md:402`) and `No case or reference was created; review the
   missing or conflicting evidence.` (`:404`). Render every decision label
   through [[FEAT-023]] (plan handle `DSK-05-23`)'s single `OperatorLabels` list
   and **change no label text here**.
9. **Contract tests.** In `tests/Pegasus.Api.ContractTests` *(created by
   [[TEST-001]], plan handle `DSK-08-01`)*, apply the seven-case matrix from
   [[TEST-002]] (plan handle `DSK-08-02`) to each of the nine commands —
   success, 401, 403, 409 stale version, replay of the same `operationKey`
   returning the same result, bad-input problem, and the Core-specific failure —
   and four cases to each byte endpoint: 200 with `ETag` and no-sniff, range
   request, 404, 403. Enable `Features:DesktopGateway` explicitly, or a gated
   endpoint returns 404 and the test lies.
10. **View-model tests.** In `tests/Pegasus.Desktop.ViewModelTests` *(created by
    [[TEST-004]], plan handle `DSK-08-04`)*: `CanExecute` gating per command, the
    reason-required commands, streaming progress and cancellation, and the
    read-only draft rendering.
11. **Tier-8 corpus run, locally.** For the reviewed cohort used by
    `tests/Pegasus.IntegrationTests/MultiFormatIntakeWebTests.cs`, compare web and
    desktop outcomes for each of the nine actions. Corpus material and detailed
    evidence stay local and are never committed; only the pass/fail table reaches
    the proof (L-02: never an Azure test resource). A re-evaluate divergence
    traceable to upstream INTK-027 (board [[INTK-004]]) is recorded against that
    ticket, not fixed here.
12. **Documentation, simplification pass, PR.** Update `parity-matrix.md` rows
    `PAR-19` and `PAR-20`; correct `vertical-slices.md:369-373`'s "Absorbs
    upstream" line, coordinating with [[FND-022]] (plan handle `DSK-01-09`) so it
    changes once; add the received-items section to
    `docs/frd/frd-13-desktop-operator-experience.md` (created by [[DUI-013]],
    plan handle `DSK-06-13` — contribute the content there if it has not landed);
    add the `DSK` rows to `docs/capabilities.md`. Run the simplification pass over
    this branch's diff, record it under a dated `## Simplification pass` heading
    below, then open the PR into `dev`.

## Verification

Evidence tiers from the body: **2** (Core/domain), **5** (Web/API/MCP caller),
**7** (Browser/accessibility), **8** (Genuine corpus).

- `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build`
  — link/reverse-link integrity and re-evaluation characterization facts pass,
  with positive, contradictory, ambiguous and failure cases (tier 2). Output
  becomes proof.
- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build`
  — nine command matrices and three byte-endpoint facts pass (tier 5).
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`
  — command gating, streaming and draft facts pass.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"`
  — existing intake web tests stay green after the rule moves into Core.
- Tier 7: keyboard and semantic-label evidence from a real run of the screen,
  captured with the [[TEST-006]] (plan handle `DSK-08-06`) harness and the
  `axe-windows` scan from [[TEST-009]] (plan handle `DSK-08-09`).
- Tier 8: the corpus comparison table in the proof — desktop outcomes equal web
  outcomes across the reviewed cohort, with no corpus content committed.

## Risks / open questions

- **[[GWY-010]] may not have landed all nine commands.** Mitigation: step 4 is a
  hard gate — a missing or folded route stops the ticket and is raised on
  [[GWY-010]], never worked around client-side.
- **The re-evaluate action is broken today.** upstream INTK-027 (board
  [[INTK-004]]) owns the fix and is `backlog` upstream with no branch, so
  [[FND-023]] (plan handle `DSK-01-10`)'s pinned sync brings nothing. Mitigation:
  characterize the current behaviour and name the defect in the test; do not fix
  and do not work around. Answered by: [[INTK-004]].
- **The composition gate behind [[FEAT-011]] and [[FEAT-012]] is closed.**
  upstream INTK-033 (board [[INTK-007]]) is at `review` on the unmerged branch
  `task/intk-033-triage-from-intake` (`7b43ab17`), outside [[FND-023]]'s pinned
  range, so under D-001 it vanishes at the freeze unless its fork ticket carries
  it. Not this slice's work; recorded so nobody waits for a sync to deliver it.
  Answered by: [[INTK-007]].
- **Label text disagreement.** `Details.cshtml.cs:350-361` and
  `Message.cshtml.cs:1014-1023` disagree with `docs/design/README.md:535-546` on
  `OcrRequired` and `TechnicalFailure`. Scope boundary, not an open question:
  [[FEAT-023]] owns it as its one stated exception. This slice changes no label
  text.
- **Action count wording.** The acceptance line says "All ten actions"; the
  dispatcher, the endpoint map and the ticket's own What all enumerate **nine**
  named commands plus the detail read, which is the tenth handler the Why line
  counts. Default taken: deliver every action the body names, all nine, by name.
  Nothing named is dropped.
- **Tier-8 corpus availability.** If the reviewed cohort is not available on the
  local Test/UAT workstation, the tier-8 evidence cannot be produced and the
  ticket stops. Substituting synthetic material would make the evidence false;
  standing up an Azure test resource is forbidden by L-02 and ADR-0014.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading._
