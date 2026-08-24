---
id: AUTO-002
type: ticket
title: 'upstream:AUTO-008 · Measure and reduce durable intake processing latency'
status: backlog
area: automation-integrations
assignee: ''
profile: spike
labels:
  - performance
  - intake
  - provider-api
  - research
  - upstream-carryover
  - upstream-AUTO-008
  - gateway-worker-ticket
groups:
  - EPIC-014
links: []
refs:
  - docs/frd/frd-09-provider-and-intermediary-routes.md
docs_todo: true
archived: false
created: '2026-08-24T11:41:25.722Z'
updated: '2026-08-24T11:41:25.722Z'
---

## What

A timeboxed spike that measures the durable intake path segment by segment on the local production-mimicking stack — durable receipt, dispatch wait, queue-trigger claim, inner `ProcessIntake`, Case allocation and terminal tail — reports median, p95 and worst case with the healthy path separated from the retry ladder, and recommends the smallest justified change as a **separate** implementation ticket. No application code, schedule, configuration or cloud resource is changed by this ticket.

## Why

The desktop inherits this latency whole and measures none of it.

`docs/desktop/05-implementation-and-migration/reuse-map.md` marks `src/Pegasus.Worker` REUSE unchanged, and [[DSK-07-01]] states plainly "No Worker code is written or changed". So every second the Worker spends before processing starts is a second the desktop operator waits, and no seeded ticket is even permitted to look at it, let alone shorten it.

What the code actually does today:

- `ReceiveIntake` (`src/Pegasus.Core/Intake/DurableIntake.cs:247`) retains staging bytes and writes a Pending work item; the upload returns after durability, **before** processing.
- Pending work is **not** enqueued inline. `PendingWorkDispatchFunction` is a `[TimerTrigger("%PendingWorkDispatchSchedule%", RunOnStartup = false)]` (`src/Pegasus.Worker/IntakeFunctions.cs:13-15`) driving `DispatchPendingIntakeWork` (`DurableIntake.cs:359`), so schedule alignment adds a wait before the message exists at all.
- The queue trigger `[QueueTrigger("intake-work", Connection = "AzureWebJobsStorage")]` (`IntakeFunctions.cs:35`) then calls `ProcessQueuedIntake` (`DurableIntake.cs:418`), which hashes staging bytes, stores durable bytes, parses/extracts/classifies, persists receipt and evaluation, deletes staging, associates or allocates a Case, runs image automation and synchronises Unidentified work — all sequential, all currently timed as one lump.
- The only existing measurement is the Activity tag `intake.duration_ms` (`src/Pegasus.Core/Intake/ProcessIntake.cs:786`, `:795`, from `ActivitySource("Pegasus.Core.Intake")` at `:22`), and it covers **only** the inner retained-intake evaluation — not the receipt-to-dispatch wait and not the allocation tail.
- The retry ladder is 30 s, 2 min, 10 min, 30 min, 2 h (`DurableIntake.cs:434-440`). Averaging any of that into a healthy-path number would make the result meaningless.

What the operator sees: [[DSK-05-13]] renders upload status as Received / Processing / Complete / Failed with a **fixed two-second poll** (`docs/desktop/06-ui-design/screen-specs.md:314`). A receipt sitting in the dispatch wait shows as a spinner that ticks every two seconds and changes nothing — which an operator reads as "the desktop is slow after upload". Whether that wait is 5 seconds or 15 is currently unknown, and the fork tree and the upstream research disagree about it (see step 2).

No seeded ticket measures it, and each of the three that come closest says so itself:

- [[DSK-10-10]]'s own trap: "the desktop does not exist yet at Phase 0, so this ticket measures the **web** baseline and publishes the desktop targets" — its ten §15.1 budget rows are launch, navigation, list, save, memory and thumbnails; none is a server-side intake segment.
- [[DSK-01-11]] times web page workflows (dashboard, case list, case detail, inbox, save, report generation) on the baseline workstation.
- [[DSK-08-15]] scripts the nine proposal §22.2 scenarios and times gateway responses under ten concurrent clients; no scenario touches the durable intake path.

The coverage decision records that a grep of all 208 seeded ticket bodies for `PendingWorkDispatch`, "durable intake" and "dispatch schedule" returns nothing. It also requires this spike to stay **separate** from [[DSK-10-10]], because folding a Worker-pipeline measurement into the desktop budget table corrupts both.

## Source of truth

- Import decision: `coverage-decision.md` § Import list — AUTO-008; § Plan gaps — "The 208-ticket set contains no owner for Worker and Core/Infrastructure intake defects…"
- Carry-over register row: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:84` — disposition `gateway-worker-ticket`, plan area "10 (performance baseline)", fork area `automation-integrations`
- Governing document: `docs/frd/frd-09-provider-and-intermediary-routes.md` (the provider submission contract whose expectation of prompt completion this spike tests)
- Repository evidence:
  - `src/Pegasus.Core/Intake/DurableIntake.cs:247` `ReceiveIntake`, `:359` `DispatchPendingIntakeWork`, `:418` `ProcessQueuedIntake`, `:434-440` `RetryDelays`, `:927` `ReconcilePoisonedIntakeWork`, `:935` `ReconcileStagedArtifacts`
  - `src/Pegasus.Worker/IntakeFunctions.cs:13-15` timer dispatch, `:35` `intake-work` queue trigger, `:52` `intake-work-poison`, `:77` staged-artifact reconciliation timer
  - `src/Pegasus.Worker/local.settings.example.json` — the checked-in local example values, including `PendingWorkDispatchSchedule` and `IntakeStagedArtifactReconciliationSchedule`
  - `src/Pegasus.Core/Intake/ProcessIntake.cs:22`, `:786`, `:795` — the `intake.duration_ms` Activity tag and what it does and does not cover
  - `src/Pegasus.Core/Custody/ExternalWorkProcessing.cs:117` `DispatchPendingWork` — the shared timer dispatches two durable outboxes; optimising intake must not starve external work
  - `src/Pegasus.Worker/AzureQueueIntakeWorkQueue.cs` — the actual enqueue boundary
  - `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs` — the durable state transitions and timestamps available for segment timing
  - `src/Pegasus.Web/Pages/Upload.cshtml.cs` — today's submission entry point, which returns after durability
  - `scripts/Invoke-LocalDevelopment.ps1`, `scripts/Invoke-Doctor.ps1` — the local stack lifecycle and prerequisite report
  - `docs/engineering.md` § Required evidence tiers, tier 10; `docs/runbook.md` § Corpus safety and evaluation, § Local setup and run, § Live-operation approval matrix
  - `docs/desktop/06-ui-design/screen-specs.md:314` — the four-state upload status and the fixed two-second poll
- Binding decisions: **L-02** Test/UAT is a local production-mimicking stack and ADR-0014 stands — there is no Azure dev/test/staging, so this spike may not request one and may not load-test production; **L-04** routing is named on this ticket; **L-05** the fork board is the single work register; **D-001** upstream is frozen after one more sync, so nobody upstream will do this work; **C-01** the estate is ten users, so any recommendation is priced against that, not against a hypothetical scale-out.
- Depends on: [[DSK-08-17]] — the Test/UAT stack lifecycle, if it has landed, gives a more production-like run than the plain local profile; the spike may proceed on the plain local stack and must say which it used.

### Upstream ticket AUTO-008 (verbatim)

Provenance — read 2026-08-24 from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at commit `a5b28111`:

- Upstream area: `automation-integrations`
- Upstream status: `preparing` (entered 2026-08-21T14:20:04.663Z)
- Upstream profile: `spike`
- Upstream labels: `performance`, `intake`, `provider-api`, `research`
- Upstream groups: `HZN-002` — an **upstream** horizon id; it is unrelated to this board's `HZN-002` (Phase 1), and no horizon is set on this ticket
- Upstream links: `TICK-058`
- Upstream refs: `docs/frd/frd-09-provider-and-intermediary-routes.md`

The body below is copied exactly and is not edited or paraphrased.

```markdown
## What

Measure end-to-end durable intake latency and separate queue wait from actual processing cost.

## Why

Provider submissions are expected to complete quickly, but the current Worker dispatch schedule may add up to 15 seconds before processing begins. Architecture changes must be based on measured latency rather than a separate provider-facing Processing feature.

## Approach

- Measure durable receipt, dispatch wait, processing, allocation, and terminal persistence independently.
- Compare representative current fixtures and approved predecessor evidence when available.
- Recommend the smallest measured improvement and file separate implementation tickets for any change.

## Verification

- [ ] Median, p95, and worst-case timings identify queue wait versus processing cost.
- [ ] Recommendations cite evidence and preserve durable replay and failure semantics.

## Outcome
```

The upstream `research`, `files`, `plan`, `checklist` and `open-questions` documents are copied onto this ticket verbatim as well; read them with `get_ticket_doc` before measuring. The copied `open-questions` document carries two **unticked** parked items — the predecessor comparison and production measurement — and they stay unticked until an operator decision retires them.

## Routing

- **Subagent**: `pegasus-ui-verifier` — `.codex/agents/pegasus-ui-verifier.toml` (the measuring agent for performance work, `docs/desktop/12-agent-tooling/skill-routing.md` § work-type routing, Performance work row); `pegasus-azure-auditor` — `.codex/agents/pegasus-azure-auditor.toml` for the read-only Application Insights and Function App configuration reads; `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `analyzing-dotnet-performance` (dotnet/skills `98f84851`, plugin `dotnet-diag`) → `dotnet-trace-collect` (same pin) → `appinsights-instrumentation` (microsoft/azure-skills `1a03acfb`, read-only guidance only)
- **Do not load**: `configuring-opentelemetry-dotnet` — `docs/desktop/12-agent-tooling/skill-routing.md` § Not applicable to this conversion (the estate uses the App Insights SDK; no collector fleet).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `get_ticket_doc`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** `applicationinsights`, `monitor`, `functionapp`; Microsoft Learn (`microsoft_docs_search` for Azure Queue Storage trigger polling and `maxPollingInterval` semantics)
- **Kanmer pipeline** for profile `spike`: `kanmer-research` → `kanmer-verify` → `kanmer-closeout`; only `enter-done` is gated (`research`, `questions-resolved`) — call `get_doc_gates <this ticket id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read this body, then the upstream `research`, `files`, `plan`, `checklist` and `open-questions` documents on this ticket via `get_ticket_doc`. Read `docs/engineering.md` § Required evidence tiers (tier 10), `docs/runbook.md` § Local setup and run and § Corpus safety and evaluation, and `docs/desktop/10-security-observability-performance/README.md` §§ 2–4. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/auto-008-durable-intake-latency` from `origin/dev`.
2. **Resolve the schedule fact before measuring anything — two sources disagree.** The upstream `research` document records the checked-in local example as `*/15 * * * * *` and its 2026-08-21 Azure refresh records the **live** Worker setting as `*/15 * * * * *`. The fork tree today carries `"PendingWorkDispatchSchedule": "*/5 * * * * *"` in `src/Pegasus.Worker/local.settings.example.json`. Re-read both at execution time — `cat src/Pegasus.Worker/local.settings.example.json` for the local value and an Azure MCP read-only `functionapp` configuration read for the deployed value — and record both in `research` with their read dates. Do not carry either number forward as fact without re-reading it; the upstream ticket's headline "up to 15 seconds" is a hypothesis, not a finding.
3. **Re-express the measurement environment for the desktop era.** The upstream `plan` step 2 assumes production and "approved predecessor" evidence may be available. L-02 removes the Azure dev/test/staging environment and makes the local stack the only verification environment; ADR-0014 stands. So: the measured run happens on `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start` (or [[DSK-08-17]]'s `TestStack` mode if it has landed — say which), and Azure evidence is limited to **read-only aggregate** telemetry. Record that constraint in `research` and leave the upstream's two parked questions parked. The upstream requirement is preserved; only the environment it runs in is re-expressed.
4. Define the segment boundaries and a disclosure-safe correlation key, then confirm each boundary already has a durable timestamp in `src/Pegasus.Infrastructure/Persistence/EfIntakeWorkStore.cs` or must be derived: (a) durable acceptance in `ReceiveIntake`; (b) dispatch claim/enqueue in `DispatchPendingIntakeWork`; (c) queue-trigger claim entering `ProcessQueuedIntake`; (d) inner evaluation, already covered by `intake.duration_ms`; (e) Case association/allocation; (f) terminal persistence and staging delete. Where a boundary has no timestamp, record it as a focused telemetry follow-up rather than adding instrumentation in this spike.
5. Run representative fixtures repeatedly on the local stack. Fixtures come from tracked `reference/` material or the ignored, immutable `corpus/` referenced by name — never copied into the repository, never fabricated (`docs/runbook.md` § Corpus safety and evaluation, `AGENTS.md` § Safety rails). Record sample size, fixture identity, machine and stack mode alongside every number.
6. Report median, p95 and worst case **per segment**, with the healthy path and the retry ladder reported separately. A run that hit the 30 s–2 h `RetryDelays` sequence is a different population and is never averaged into the healthy figure. A single sample is not a measurement.
7. Measure Azure Queue Storage trigger polling as its own segment rather than attributing it to processing code. Use `microsoft_docs_search` for the queue-trigger exponential idle polling and `maxPollingInterval` semantics before scripting it, and record the documented behaviour beside the observed one.
8. **Translate the result into the operator-visible consequence — a desktop re-expression the upstream ticket could not make.** For each segment, state what [[DSK-05-13]]'s upload-status screen shows while it elapses, and what an honest waiting state and poll interval would be given the measured dispatch wait (`screen-specs.md:314` currently fixes the poll at two seconds and lists no waiting state). Hand that number to [[DSK-05-13]] and, as an input only, to [[DSK-10-10]]. Do **not** edit either ticket.
9. Compare remedies in evidence order and stop at the smallest that the measurement justifies: (i) shorten the existing `PendingWorkDispatchSchedule`; (ii) wake the existing dispatcher on write while retaining the SQL outbox and its reconciliation; (iii) only then consider a different messaging service. Check every candidate against `src/Pegasus.Core/Custody/ExternalWorkProcessing.cs:117` — the shared timer dispatches two durable outboxes sequentially, so speeding intake must not starve external work — and against the poison and staged-artifact reconciliation paths (`DurableIntake.cs:927`, `:935`).
10. Record the recommendation with its expected gain, its failure and replay impact, and the evidence line that supports it. **File any code, schedule or configuration change as a separate implementation ticket in `intake-processing` or `platform-operations`** — this ticket changes nothing.
11. Write the full result into this ticket's `research` document with `set_ticket_doc` (sample size, fixtures, environment, percentile method, per-segment timings, healthy/retry separation, recommendation). Tick the `open-questions` items the measurement genuinely resolves, leave the two parked items unticked, run the Verification checks, then `get_doc_gates` and move to Done.

## Acceptance criteria

- [ ] The upstream criterion, unchanged: median, p95 and worst-case timings identify queue wait versus processing cost, per segment.
- [ ] The upstream criterion, unchanged: every recommendation cites its evidence and preserves durable replay and failure semantics.
- [ ] Both schedule values — the checked-in local example and the deployed Worker setting — are re-read at execution time and recorded with their read dates; neither the upstream "15 seconds" nor the fork's `*/5` is carried forward unverified.
- [ ] The healthy path and the `RetryDelays` ladder (30 s, 2 min, 10 min, 30 min, 2 h) are reported as separate populations.
- [ ] Queue-trigger polling is measured as its own segment, not attributed to processing code.
- [ ] The report states sample size, fixtures by name, machine and stack mode, and states what a local measurement cannot prove (Azure SQL latency, Container App behaviour, real provider round-trips).
- [ ] The operator-visible translation for [[DSK-05-13]]'s upload status is recorded, and handed over as an input rather than written into another ticket.
- [ ] No application code, schedule, configuration or cloud resource was changed; every recommended change is a separate filed ticket.
- [ ] No corpus material was copied into the repository and no domain data was fabricated.

## Verification

- [ ] `pwsh ./scripts/Invoke-Doctor.ps1` — expected: every prerequisite reported present; output attached.
- [ ] `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start` then `-Action Status` — expected: manifest state `Running`.
- [ ] `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Smoke` — expected: exit code 0 before any timing run is trusted.
- [ ] `cat src/Pegasus.Worker/local.settings.example.json` — expected: the `PendingWorkDispatchSchedule` value recorded in `research` matches the tree at the read date.
- [ ] `git status --porcelain` — expected: empty; the spike commits no code, no schedule change and no trace, dump or corpus artefact.
- [ ] `pegasus-desktop-reviewer` re-reads the `research` document — expected: every number names the command and sample that produced it, and no recommendation is stated without an evidence line.

## Evidence tier

Tier 10 — Performance/concurrency. Tier 6 — Functions/Azurite caller.
Tier 10 obliges the measurement to be sized against the recorded shape (eight concurrent operators, 2,000 cases per month, 2–20+ files per case, the one-file 10 MiB limit and the 10 MiB-plus-64-KiB multipart envelope) and forbids inventing a release latency threshold without an explicit decision — this spike records observations and sets no threshold. Tier 6 obliges the timings to come from the actual timer and queue triggers with Blob staging and identifier-only messages, not from an in-process shortcut that calls `ProcessQueuedIntake` directly (the existing integration tests do exactly that and therefore represent no queue latency at all).

## Documentation changes

`None.` The spike's output is this ticket's `research` document. The operator-visible translation is handed to [[DSK-05-13]] and [[DSK-10-10]] as an input; any documentation change belongs to the implementation ticket this spike recommends, not to this one — and `docs/desktop/10-security-observability-performance/README.md` § 2 is [[DSK-01-11]]'s baseline subsection and must not be overwritten here.

## Guardrails

- **Azure**: no write. Read-only `applicationinsights`, `monitor` and `functionapp` reads only. The Application Insights workspace is capped at 0.1 GB/day, so working-hour queries frequently return empty — absence of data is never evidence of no traffic (upstream `PLAT-034`). Production load testing is out of bounds; production observation, if it is ever wanted, is a pilot-ring activity under separate operator approval (`docs/runbook.md` § Live-operation approval matrix).
- **Scope boundary**: this ticket changes **no** file under `src/`, `tests/`, `scripts/`, `.github/` or `docs/`. It may write this ticket's own pipeline documents and local artefacts under the ignored `artifacts/` path, referenced by name. It must not change `PendingWorkDispatchSchedule` anywhere, must not add Service Bus, Event Grid or another Function, and must not bypass the SQL outbox.
- **Blocks**: [[DSK-05-13]] — its upload-status screen cannot honestly choose a waiting state or a poll interval while the dispatch wait is unmeasured; a fixed two-second poll against an unknown multi-second wait is the dishonesty this spike exists to price. [[DSK-10-13]] — the release-candidate performance regression report gates on a budget set that today has no row for the one operator-visible latency the desktop inherits unchanged, so a dispatch-cadence regression would ship through the gate unseen.
- **Deliberately separate from [[DSK-10-10]]**: the coverage decision requires it. [[DSK-10-10]] measures the **web** baseline and publishes the desktop §15.1 budget table by its own trap; this spike measures the server-side Worker path. Folding either into the other corrupts both — hand numbers across, do not merge the tickets.
- **Traps**: `corpus/` is ignored and immutable and must never be a performance fixture, and domain data must never be fabricated; `docs/engineering.md` tier 10 forbids inventing a release latency threshold without an explicit decision; retry latency must never be averaged into the healthy path; the existing integration tests invoke `ProcessQueuedIntake` immediately and therefore represent no timer or queue latency — do not present them as end-to-end evidence; the upstream body's `HZN-002` is an **upstream** horizon id, not this board's HZN-002 (Phase 1), and no horizon is set here because the carry-over phase is assigned by [[DSK-01-09]]; there is no tracked TypeScript intake runtime, so a predecessor comparison stays parked rather than being reconstructed from recollection.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — no code change`, recorded under a dated `## Simplification pass` heading in this ticket's `plan` document.

## Outcome

_Filled at closeout._
