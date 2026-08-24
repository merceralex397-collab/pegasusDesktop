# Plan — GWY-018: independent contract and authorization gap review across every `/api/v1` command endpoint

**Diff estimate: 0 files, 0 lines of repository change.** This chore is read-only by design —
the Guardrails say so in terms ("this ticket changes **no** production code and no test code.
Its only writes are Kanmer tickets and its own ticket documents"). The estimate is zero because
the work product is a review, and the inventory below is what makes that zero credible rather
than asserted.

### Inventory — the surface under audit, measured 2026-08-24

| What | Measured value | How it was measured |
| --- | --- | --- |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` | 154 lines; 73 projected endpoint rows across 7 sections, plus 5 "stays web-only" rows | `wc -l`; per-section row count with `awk` over `^\| ` excluding header/separator rows: Session/compat/diagnostics 8, Dashboard 2, Cases 30, Intake/uploads/image 10, Mail 7, Triage/Unidentified/Operations 5, Administration/audit 11 |
| Method + route tokens in that map | 122 total — 49 `GET`, 62 `POST`, 8 `PUT`, 2 `DELETE`, 1 `GET/POST` (the web-only `Uploads/{token}` row) | ``grep -oE '`~?(GET\|POST\|PUT\|DELETE\|PATCH)[^`]*`' docs/desktop/03-gateway-api-and-data/endpoint-map.md`` piped through `awk '{print $1}' \| sort \| uniq -c` |
| **Mutating routes to audit against the seven-case matrix** | **72** (62 `POST` + 8 `PUT` + 2 `DELETE`) | Same command. A row may carry more than one route, so this exceeds any per-row count |
| Read routes to audit for paging/filter/sort/newest-first | 49 `GET` | Same command |
| `StaffAccessRight` values every route must map onto | 12, enum at `src/Pegasus.Core/Identity/StaffAuthorization.cs:9-20`, single fail-closed `switch` at `:33-57` with `_ => false` at `:56` | Read the file |
| Existing integration-test corpus the audit reads | 116 `.cs` files in `tests/Pegasus.IntegrationTests/` | `ls tests/Pegasus.IntegrationTests/*.cs \| wc -l` |
| `scripts/Test-MigrationGrants.ps1` | 99 lines; invoked by CI at `.github/workflows/ci.yml:58-60` | `wc -l`; `grep -n` in the workflow |
| The TempData anti-pattern to check for | `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs` — 339 lines, chunk budgets `MaximumRetainedProposedCharacters = 8000` at `:38` and `MaximumRetainedProposedValueCharacters = 2000` at `:39` | `wc -l`; `grep -n` |
| The dispatcher-string anti-pattern to check for | `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs` — 496 lines; one `OnPostActionAsync` at `:85` whose `switch (actionName)` at `:114` carries **12** string cases (`:116`–`:204`) | `wc -l`; `grep -n 'switch (actionName)'`; `grep -c '^\s*case "'` |

### Inventory — what this ticket produces instead of a diff

| Artefact | Where | Approximate size |
| --- | --- | --- |
| Route inventory taken from the running host (step 2) | `set_ticket_doc GWY-018 scratch-route-inventory` — the `scratch` folder is gate-exempt (`get_doc_gates` → `gateExemptFolders: ["reference","scratch","assets"]`) | ~72 mutating + ~49 read lines |
| `test-gap-analysis` report (step 6) | `set_ticket_doc GWY-018 scratch-test-gap-analysis` | skill output, verbatim |
| `assertion-quality` report (step 7) | `set_ticket_doc GWY-018 scratch-assertion-quality` | skill output, verbatim |
| Review summary and coverage table (step 11) | `set_ticket_doc GWY-018 proof` | ~120 lines |
| One Kanmer ticket per gap (step 10) | `create_item`, area `gateway-api`, group `EPIC-004` | unknown until the audit runs; zero is a valid result |

**Repository files changed: none.** If a repository file needs editing, the audit has stopped
being independent — the fix belongs in a filed ticket, per the Guardrails.

## Approach

Audit **from the running host, then against the map, then against the tests** — in that order,
and never from a source-code reading alone. The order is what makes the audit trustworthy: an
inventory built from `EndpointDataSource.Endpoints` on a booted
`WebApplicationFactory<Program>` finds routes that exist but were never documented, which a
reading of `endpoint-map.md` can never do; a reading of the map finds documented routes that
were never built, which the running host can never do. Only both directions together answer
the exit-gate question ("every command endpoint has the seven-case matrix").

The alternative considered and rejected was **running `test-gap-analysis` first and treating its
report as the audit**. It was rejected because the skill measures coverage of the tests that
exist against the code that exists — it cannot see a route that has no test *class* at all, and
it cannot see that a route's declared `StaffAccessRight` disagrees with the map. The skill is
step 6 for a reason: it deepens an inventory that steps 2–5 have already made complete.

The second choice worth naming: **file every gap, including trivial ones.** The Guardrails
require it, and the reason is structural — an independent reviewer that edits the code under
review is no longer independent (`AGENTS.md` § Repository task workflow step 5), and L-05 says
gaps become Kanmer tickets, not a to-do list in a document.

## Governing docs

The ticket's `refs` is empty and it carries `docs_todo: true`, so no repository document
governs it yet.

> **New ADR** — ADR-0103 (gateway, never direct database access from workstations), authored by
> [[FND-005]] (plan handle `DSK-00-05`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR table) and
> `docs/desktop/03-gateway-api-and-data/README.md` § 3; if the ADR lands differently this plan
> is revised before implementation. ADR-0103 is the authority this review checks the endpoints
> against: every command must reach the database through a Core use case behind the gateway,
> never around it.

Because `refs` is empty, the programme-level authorities that bind today, each with the step
that satisfies it:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Plan 03 § 4 exit gate, bullet 3 | Every command endpoint has tests for authorized success, unauthorized, version conflict, lease conflict, operation-key replay, validation failure and the problem-details shape | Step 4 |
| Plan 03 § 4 exit gate, bullet 4 | Every list endpoint has paging/filter/sort contract tests and a newest-first default test | Step 5 |
| Plan 03 § 4 exit gate, bullet 5 | `Features:DesktopGateway=false` leaves no `/api/v1` route (404 test) | Step 8 |
| Plan 03 § 4 exit gate, bullet 6 | A contract test runs the previous snapshot against the current server for the supported client range | Step 8 |
| Plan 03 § 7 trap *Two policy engines* | Any rule that appears in an endpoint filter is a defect; API and MCP must both call Core use cases | Step 9 |
| Plan 03 § 7 trap *TempData semantics* | Do not port `CaseMutationPageModel`'s proposed-values/lease chaining (`:38-39` chunk budgets) | Step 9 |
| Plan 03 § 7 trap *Runtime-role grants* | Any new table or write path needs a `Grant*` migration; failure class has shipped three times (PLAT-035) | Steps 9, 12 |
| Endpoint map § Conventions (`endpoint-map.md:20-21`) | **Auth right** is the `StaffAccessRight` the endpoint filter checks; `PerformCasework` implies `AccessStaffApplication` | Step 3 |
| Proposal § 20.6 Review protocol | Independent review protocol for agent work | Steps 1, 10 |
| Proposal § 22.2 Test pyramid, § 23.1 Required conversion evidence | The evidence a converted capability owes | Steps 4–7, 11 |
| L-04 (`docs/desktop/README.md`) | Every ticket names its subagent, skills and MCP tools | Step 3 extends to checking the endpoint tickets did |
| L-05 (same) | The board is the record; gaps become tickets | Step 10 |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Step 1's self-check, and the reviewer line in *Routing* |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass over the branch diff | `n/a — docs-only`, recorded below |
| `docs/desktop/00-governance-and-workflow/README.md` § 7 | Ticket-transient documents live in Kanmer, never as a new `.md` in the tree | The *what this ticket produces* inventory above |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
  (read-only sandbox; must not be the agent that implemented any endpoint ticket)
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `test-gap-analysis` (`dotnet/skills`
  `98f84851`, plugin `dotnet-test`) → `assertion-quality` (`dotnet/skills` `98f84851`, plugin
  `dotnet-test`) → `microsoft-code-reference` (Microsoft Learn plugin)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `create_item`, `move_item`); Microsoft Learn (`microsoft_docs_search`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` →
  `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move)
- **Reviewer**: this ticket *is* the independent review; its own PR is reviewed by a different
  agent again (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's implementation steps: same order, same ownership, same paths.

1. **Orient and prove independence.** Read `docs/desktop/03-gateway-api-and-data/README.md`
   § 4 and § 7 in full and `docs/desktop/03-gateway-api-and-data/endpoint-map.md` end to end
   (154 lines). Confirm you implemented none of [[GWY-006]] (plan handle `DSK-03-06`) through
   [[GWY-015]] (`DSK-03-15`) — check `get_activity` and the merged PR authorship, not memory.
   If you did, hand the ticket to another agent rather than proceeding. Then
   `get_doc_gates GWY-018` and `take_ticket`.
2. **Build the route inventory from the running server.** Boot
   `WebApplicationFactory<Program>` with `Features:DesktopGateway=true` — the same
   `factory.WithWebHostBuilder(...)` + `builder.UseSetting(...)` shape as
   `tests/Pegasus.IntegrationTests/AutomationMcpTestSupport.cs:32-42` — resolve
   `EndpointDataSource` from `factory.Services` and enumerate `Endpoints`, filtered to routes
   whose pattern starts `/api/v1`. Record method, pattern, and the endpoint metadata that names
   the required `StaffAccessRight`. Save it with
   `set_ticket_doc GWY-018 scratch-route-inventory`. Expect roughly 72 mutating and 49 read
   routes (inventory above) — a materially different count is itself the first finding.
3. **Cross-check both directions against the map.** Every one of the 73 projected rows in
   `endpoint-map.md` must have a route in the inventory; every route in the inventory must have
   a row. For each match, compare the route's declared right against the row's **Auth right**
   column, remembering the convention at `endpoint-map.md:20-21` that `PerformCasework` implies
   `AccessStaffApplication`, and that the right must be one of the 12 values at
   `src/Pegasus.Core/Identity/StaffAuthorization.cs:9-20`. **An extra undocumented route is as
   much a finding as a missing one.** While here, confirm each endpoint ticket's plan carries a
   `## Routing` block (L-04); a missing block is a finding against that ticket.
4. **Seven cases per mutating route.** For each of the ~72 mutating routes, read the test files
   and verify all seven cases exist *and actually assert*: authorized success; unauthorized
   (wrong role); version conflict returning the **current** version
   (`src/Pegasus.Core/Workflow/CaseWorkflowContracts.cs:125`); lease conflict (`:135`) and
   lease expiry (`:143`) where the route's **Concurrency token** column says `editLeaseToken`;
   operation-key replay returning the same result (`:151`); validation failure; and the
   problem-details shape — a `urn:pegasus:problem:<slug>` `type` plus a `correlationId`. Record
   one row per route in the coverage table.
5. **Paging, filter, sort per read route.** For each of the ~49 read routes, verify the paging,
   filter and sort tests exist, that the newest-first default is asserted, and that the sort
   **whitelist is actually enforced** — a rejected out-of-whitelist `sort` value, not merely a
   200 on an accepted one. A fact that only asserts a status code is not coverage; record it as
   a gap.
6. **Run `test-gap-analysis`.** Scope it to the `DesktopGateway*` classes in
   `tests/Pegasus.IntegrationTests` (116 `.cs` files today) and the contract classes in
   `tests/Pegasus.Api.ContractTests`. Attach the report verbatim with
   `set_ticket_doc GWY-018 scratch-test-gap-analysis`. Reconcile its findings against the
   coverage table from steps 4–5: anything the skill found that the table missed is a defect in
   the audit, not only in the code.
7. **Run `assertion-quality`.** Over the same classes. Record every weak assertion —
   status-code-only facts, assertions on a message string where an exception type is available,
   facts with no arrange-time precondition. Attach with
   `set_ticket_doc GWY-018 scratch-assertion-quality`.
8. **Re-confirm the two structural gate facts.** (a) `Features:DesktopGateway=false` leaves no
   `/api/v1` route — the 404 fact from [[GWY-002]] (plan handle `DSK-03-02`); run it, do not
   trust its name. (b) The OpenAPI snapshot test from [[GWY-004]] (`DSK-03-04`) and the
   previous-snapshot compat-range test from [[GWY-017]] (`DSK-03-17`) both pass. If
   [[GWY-017]] created `openapi/pegasus-v1.previous.json` as a copy of the current snapshot
   rather than a genuinely older contract, say so in the summary — a green compat test over a
   copied snapshot proves nothing.
9. **Read the code for the three cross-cutting traps.** (a) *Two policy engines*: no business
   rule lives in an endpoint filter; every command endpoint reaches Core the way the MCP tools
   do (reference projection: `src/Pegasus.Web/Mcp/*McpTools.cs`, error translation
   `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:19-69`). (b) *TempData semantics*: no endpoint
   reproduces `src/Pegasus.Web/Pages/Cases/CaseMutationPageModel.cs`'s proposed-value/lease
   chaining — the tell is a retained-value budget like `:38` `= 8000` or `:39` `= 2000` appearing
   in `src/Pegasus.Web/Api/`. (c) *Dispatcher strings*: no endpoint reproduces the
   `switch (actionName)` at `src/Pegasus.Web/Pages/Triage/Details.cshtml.cs:114` with its 12
   string cases (`:116`–`:204`); [[GWY-013]] (plan handle `DSK-03-13`) owed 12 explicit named
   commands, and a single generic action endpoint is a finding. (d) *Runtime-role grants*: run
   `pwsh ./scripts/Test-MigrationGrants.ps1` (99 lines; CI runs it at
   `.github/workflows/ci.yml:58-60`) and confirm exit code 0.
10. **File one ticket per gap.** `create_item` in area `gateway-api`, group `EPIC-004`, profile
    `fix` for a defect or `feature` for missing coverage. Each ticket names the exact route, the
    exact missing case, and the test file it belongs in. **Do not fix any gap in this ticket** —
    including trivial ones. Zero tickets is a valid outcome and must be stated as such rather
    than left implicit.
11. **Write the review summary into `proof`.** `set_ticket_doc GWY-018 proof` carrying: the
    route inventory count against the 72/49 expectation; the number of mutating routes audited;
    the seven-case coverage table, one row per route; the list of filed ticket ids (or the
    explicit statement that none were needed); and the independence attestation from step 1.
12. **Final command log.** Run
    `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGateway"`
    and `pwsh ./scripts/Test-MigrationGrants.ps1` one final time and record the output as the
    `command-log` half of `proof`. Confirm the test count matches the coverage table — a
    mismatch means the table counted a fact that does not run.

## Verification

Evidence tier from the body: **tier 5** — Web/API/MCP caller. It obliges the audit to be taken
from routes on a running host, with authorization, idempotency and exception translation
observable, not from a source-code reading alone. Step 2 is what discharges that obligation;
an audit assembled from `endpoint-map.md` alone would fail the tier.

Commands, exactly as the ticket's Verification block names them:

```
dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release \
  --filter "FullyQualifiedName~DesktopGateway"
```
Expected: the whole gateway suite passes, **and** its reported test count matches the coverage
table in the review summary. The count match is the real assertion — a passing suite that runs
fewer facts than the table claims is the failure this ticket exists to catch.

```
pwsh ./scripts/Test-MigrationGrants.ps1
```
Expected: exit code 0, no missing runtime-role grant.

Which output becomes `proof`: **command-log** — both commands above, plus the route-inventory
count from step 2 and the seven-case coverage table. The `test-gap-analysis` and
`assertion-quality` reports are attached as scratch documents and referenced from the summary,
not pasted into `proof`.

## Risks / open questions

- **The reviewer may be the implementer.** The single largest failure mode: an agent that wrote
  an endpoint ticket cannot audit it. Mitigation: step 1 checks `get_activity` and PR
  authorship rather than relying on recall, and hands the ticket over if it finds itself.
  *Answered by*: the executing agent, before `take_ticket`.
- **The `/api/v1` surface may be incomplete when this runs.** The ticket depends on
  [[GWY-008]] (plan handle `DSK-03-08`) through [[GWY-015]]. *Scope boundary owned by those
  tickets.* Mitigation: step 2's inventory count against the measured 72/49 expectation makes
  an incomplete surface visible immediately rather than producing a falsely clean audit.
- **`openapi/pegasus-v1.previous.json` may be a copy of the current snapshot.** *Scope boundary
  owned by [[GWY-017]] (plan handle `DSK-03-17`) and [[GWY-004]] (`DSK-03-04`).* Mitigation:
  step 8 says so in the summary rather than reporting a green test as a compatibility
  guarantee.
- **Endpoint metadata may not expose the required `StaffAccessRight` at inventory time.** Step 3
  compares a declared right against the map; if [[GWY-003]] (plan handle `DSK-03-03`) implemented
  the filter without discoverable metadata, the comparison has to fall back to reading the
  registration source, which weakens the tier-5 claim. *Scope boundary owned by [[GWY-003]].*
  Mitigation: record the fallback explicitly in the summary and file it as a finding, because a
  right that cannot be enumerated from the running host cannot be audited at scale.
- **A gap may be trivial enough to tempt a fix.** Mitigation: the Guardrails forbid it outright
  and step 10 repeats the rule. File it.
- **The audit may find a rule living in an endpoint filter** (step 9a). That is a defect by
  plan 03 § 7's own words, not a judgement call; file it as `fix`.

## Simplification pass

_n/a — docs-only. This ticket changes no repository file (`AGENTS.md` § Repository task
workflow step 4 records `n/a — docs-only` for a documentation-only branch), and the Guardrails
state the same rule for this ticket in terms._
