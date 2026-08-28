---
id: INTK-004
type: ticket
title: >-
  upstream:INTK-027 · Make policy re-evaluation work after transient staging
  cleanup
status: done
area: intake-processing
order: 100
assignee: codex-mcp-client
profile: fix
stageEntered:
  preparing: '2026-08-24T21:23:32.702Z'
  review: '2026-08-25T20:13:23.068Z'
  verifying: '2026-08-25T20:13:47.885Z'
  done: '2026-08-28T20:53:32.295Z'
labels:
  - defect
  - intake
  - reevaluation
  - live-found
  - upstream-carryover
  - upstream-INTK-027
  - gateway-worker-ticket
groups:
  - EPIC-014
links: []
blocks:
  - FEAT-009
  - GWY-010
refs:
  - docs/frd/frd-02-intake-and-source-identity.md
docs_todo: true
commits:
  - eff8c6678dc464cc4ca5c11426580266c5be7b41
  - 7656a65fff3e17d0c4bdada91acf72d5dc78b0b1
prs:
  - 'https://github.com/merceralex397-collab/pegasusDesktop/pull/11'
archived: false
created: '2026-08-24T11:47:12.109Z'
updated: '2026-08-28T20:55:22.762Z'
---

## What

Make "Re-evaluate with current policy" either work or refuse honestly. Re-evaluation of a completed receipt today queues work whose staged source blob has already been deleted by design, so it fails with `staged_artifact_integrity_failure` and strands the receipt in `blocked_intake` with `reevaluation_pending`. Either re-stage the source from the retained, hash-verified custody copy before dispatch, or refuse the command before any state change — with an operator-visible reason and no doomed queue entry.

## Why

This is a **confirmed live production defect affecting every processed receipt**: `transient-intake` holds zero `staging/` blobs, so the staged source the Worker needs on re-processing is gone for every receipt that has completed. The upstream ticket records it against receipt `48311398-C284-4000-BD38-15F4449CE05B` during release-16 live verification on 2026-08-21.

The desktop conversion inherits it whole and hands it to more operators. `docs/desktop/03-gateway-api-and-data/endpoint-map.md:87` publishes `POST /api/v1/received/{id}/reevaluate` as one of the six named intake commands, and [[DSK-05-09]] lists re-evaluate among the Received item screen's ten explicit actions. So the conversion turns a Razor button into a versioned, audited, contract-published command — over a use case that cannot succeed.

**The two owners of that command are both explicitly forbidden from fixing it.** [[DSK-03-10]]'s scope boundary reads "Must not touch `src/Pegasus.Core/Intake/**`, the Worker, or `src/Pegasus.Web/Pages/Intake/**`"; [[DSK-05-09]]'s reads "may touch … `src/Pegasus.Core/Intake/` **only for rules moved in with a characterization test** … Must not touch `src/Pegasus.Infrastructure` (readers stay central), `src/Pegasus.Worker`" — and the fix is in `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs`, which is out of bounds for both. [[DSK-05-09]]'s own traps say upstream INTK-027 is "absorbed or arrive[s] by upstream sync", but it is at `backlog` upstream with no branch, so no sync brings it, and under **D-001** upstream is frozen after the final merge. `docs/desktop/05-implementation-and-migration/reuse-map.md` § `Pegasus.Worker` is the accurate statement: Worker defects are carried over as Worker tickets, not desktop work.

## Source of truth

- Import decision: `coverage-decision.md` § Import list — the row for upstream `INTK-027` (this ticket; board `INTK-004`); § Plan gaps — "The 208-ticket set contains no owner for Worker and Core/Infrastructure intake defects… INTK-027 in particular is a confirmed live production defect affecting every processed receipt"
- Carry-over register: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:156` — the row for upstream `INTK-027`, quoted as it stands (its first cell is an upstream id): `INTK-027 | intake-processing | backlog | fix | defect, intake, reevaluation, live-found | … | gateway-worker-ticket | 07 | intake-processing`
- Governing document: `docs/frd/frd-02-intake-and-source-identity.md` (the upstream ticket's own `refs`)
- Endpoint the desktop publishes: `docs/desktop/03-gateway-api-and-data/endpoint-map.md:87` — `POST /received/{id}/reevaluate`, `receipt expectedVersion`, `operationKey`, `reason`
- Repository evidence (fork `main`, read 2026-08-24):
  - `src/Pegasus.Core/Intake/DurableIntake.cs:1084-1103` — `ReevaluateIntake.ExecuteAsync`, which validates the staff mutation and calls straight into the store
  - `src/Pegasus.Core/Intake/IntakeContracts.cs:807-813` — `ReevaluateIntakeRequest`; `:869-871` — `IIntakeMutationStore.ScheduleReevaluationAsync`; `:908-913` — `IReevaluateIntake`
  - `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs:223-263` — the actual defect: it resolves the latest `IntakeEvaluations.StagedReceiptId`, finds the `IntakeWorkItems` row, refuses only if the row is currently leased for processing, and then sets `State = "pending"` with `DueAtUtc = occurredAtUtc`. **It never checks that the staged source blob still exists.**
  - `src/Pegasus.Core/Intake/DurableIntake.cs:843-848` — `artifactStore.DeleteCompletedStagedAsync(...)` on successful processing, by design; `:975-990` — `ReconcileStagedArtifacts` deleting completed staged artefacts again
  - `src/Pegasus.Core/Intake/DurableIntake.cs:875-886` — `TerminalInputFailureCode` maps `IntakeArtifactIntegrityException` to `staged_artifact_integrity_failure`
  - `src/Pegasus.Web/Presentation/OperatorLabels.cs:332` — the operator label for `artifact_integrity_failure` / `staged_artifact_integrity_failure`
  - `src/Pegasus.Core/Intake/DownloadIntakeSource.cs:10-55` — the **durable, hash-verified** retained source: the receipt's `AssetRecords` entry with `IntakeAssetKind.Source` and `IntakeAssetDisposition.Source`, read through `IIntakeArtifactStore.ReadAsync` by its own `StorageKey` and checked against both `sourceAsset.ContentHash` and `receipt.SourceHash`. This is the copy the upstream Direction proposes re-staging from.
  - `src/Pegasus.Web/Program.cs:179` and `src/Pegasus.Worker/WorkerAzureClientFactory.cs:81` — the `transient-intake` container the staging blobs live in
  - `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:17`, `:178-190` — today's only caller, `OnPostReevaluateAsync`
- Binding decisions: **L-01** the gateway is `Pegasus.Web` evolved in place, so the desktop's re-evaluate command runs this same use case; **L-02** verification is the local production-mimicking stack with Azurite — no Azure test environment; **L-05** the fork board is the single work register; **D-001** the fork becomes the single release source and upstream is frozen, so this defect has no other route to a fix
- Depends on: `DSK-01-10` — the first one-way upstream sync (upstream `main` is ahead of the fork on intake paths, the same trap [[DSK-03-10]] records)

### Upstream ticket INTK-027 (verbatim)

Provenance — upstream area `intake-processing`; upstream status `backlog`; upstream profile `fix`; upstream labels `defect`, `intake`, `reevaluation`, `live-found`; upstream `refs` `docs/frd/frd-02-intake-and-source-identity.md`. Read from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at commit `a5b28111`, read date **2026-08-24**. Copied unedited.

````
# Why

Found during release-16 live verification (2026-08-21): "Re-evaluate with current policy" on `/Received/{id}` queues the receipt's `IntakeWorkItems` row back to pending, but the Worker's re-processing needs the staged source blob (`staging/{stagedReceiptId}/{hash}` in `transient-intake`) — and `DeleteCompletedStagedAsync` deletes that blob when processing completes, by design. Result: re-evaluation of any completed receipt fails after 2 attempts with `staged_artifact_integrity_failure`, and the receipt is left `blocked_intake` with `reevaluation_pending` → a cryptic failed state. Observed live on receipt `48311398-C284-4000-BD38-15F4449CE05B` (EREF24 shape); `transient-intake` holds 0 `staging/` blobs, so every processed receipt is affected.

The control's contract (versioned re-evaluation retained in permanent history) is sound; the source lifecycle contradicts it.

# Direction (for research)

Either re-stage the source from the retained custody/search copy of the original `.eml` before dispatch (the durable retained source exists and is hash-verified), or refuse the control honestly when no staged source exists (no doomed queue, no blocked_intake side effect). Fail-closed stays; the silent-degradation is the defect.

# How to verify

Re-evaluating a completed receipt either completes under the current policy versions (draft re-resolved, history appended) or is refused with an honest operator-visible reason before any state change; a receipt is never stranded in `blocked_intake` by a re-evaluation that cannot run.

# Outcome

(open)
````

## Routing

- **Subagent**: `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml`; tests by `pegasus-test-engineer` — `.codex/agents/pegasus-test-engineer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `optimizing-ef-core-queries` (dotnet/skills `98f84851`, `plugins/dotnet-data/skills/optimizing-ef-core-queries/SKILL.md`) → `code-testing-agent` (dotnet/skills `98f84851`) → `run-tests` (dotnet/skills `98f84851`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Azure MCP **read-only** `storage` to confirm the `transient-intake` container's `staging/` prefix is empty (a read; no approval needed); Microsoft Learn (`microsoft_docs_search`) only if a Blob SDK signature is in doubt
- **Kanmer pipeline** for profile `fix`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every move; `fix` needs `files`, `plan` and `questions-resolved` to leave Preparing, `post-implementation-report` to enter Review, `proof` to enter Done)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient. Read the verbatim upstream body above, `docs/frd/frd-02-intake-and-source-identity.md`, and `docs/desktop/03-gateway-api-and-data/endpoint-map.md:87`. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/upstream-intk-027-reevaluation-after-cleanup` and worktree `../pegasus-worktrees/upstream-intk-027-reevaluation-after-cleanup` from `origin/dev`.
2. Reproduce before repairing. In `files`, trace the full path: `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:178-190` → `ReevaluateIntake.ExecuteAsync` (`src/Pegasus.Core/Intake/DurableIntake.cs:1084-1103`) → `EfIntakeMutationStore.ScheduleReevaluationAsync` (`src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs:223-263`) → the Worker's re-processing → `DeleteCompletedStagedAsync` (`src/Pegasus.Core/Intake/DurableIntake.cs:843-848`). Write down the exact line at which the staged blob is assumed to exist.
3. **Verify the upstream body's attempt count against this tree rather than repeating it.** The upstream text says re-evaluation "fails after 2 attempts"; on the fork `IntakeArtifactIntegrityException` is a `TerminalInputFailureCode` (`src/Pegasus.Core/Intake/DurableIntake.cs:875-886`), which by its own comment fails on the first attempt under its own code. Record the observed count in the `plan` and use the observed one.
4. Confirm the retained durable source exists and is reachable: `src/Pegasus.Core/Intake/DownloadIntakeSource.cs:10-55` reads the receipt's `IntakeAssetKind.Source` / `IntakeAssetDisposition.Source` asset by its own `StorageKey` and validates it against both `sourceAsset.ContentHash` and `receipt.SourceHash`. Record in the `plan` that this, not the staging blob, is the hash-verified copy the upstream Direction refers to.
5. Take one of the two directions and record which, with its reason, in the `plan`. **(a) Re-stage**: before `ScheduleReevaluationAsync` requeues the work item, copy the retained source back to the staged storage key the Worker will read, preserving the hash so `IntakeArtifactIntegrityException` cannot fire for a legitimate re-evaluation. **(b) Refuse honestly**: check for the staged (or re-stageable) source *inside the same transaction*, before `workItem.State = "pending"`, and throw a named intake exception that becomes an operator-visible reason — no queue entry, no state change, no `blocked_intake` side effect. Fail-closed stays either way; the silent degradation is the defect.
6. Whichever direction is taken, add the check to `EfIntakeMutationStore.ScheduleReevaluationAsync` **before** the `workItem.State = "pending"` assignment at `:260`, inside the existing `ExecuteAsync` transaction, so a refusal leaves the receipt version and history untouched.
7. If direction (b) is chosen, give the refusal a named exception in `src/Pegasus.Core/Intake/IntakeContracts.cs` and a label in `src/Pegasus.Web/Presentation/OperatorLabels.cs` beside the existing `staged_artifact_integrity_failure` entry at `:332`. Use the approved necessary-copy style — an honest reason, no explanation of internals, and none of the banned operator words (`intake`, `artifact`, `staging`, `blob`).
8. **Re-expressed for the desktop world.** The upstream body describes a Razor button on `/Received/{id}`, which [[DSK-05-26]]'s cut list deletes. State the requirement against what replaces it and record it in the `plan`: `POST /api/v1/received/{id}/reevaluate` (endpoint-map `:87`) must return the refusal as a `validation`-shaped problem carrying the named reason, **not** a 200 followed by a silent `blocked_intake`; and [[DSK-05-09]]'s re-evaluate command must be able to disable itself or report the refusal without a second copy of the rule. Add that as a note to the `plan` for [[DSK-03-10]] and [[DSK-05-09]] to consume — do not edit those tickets.
9. Add tests: a completed receipt whose staged source is gone either completes under the current policy versions (draft re-resolved, history appended) or is refused before any state change; a receipt is never left in `blocked_intake` by a re-evaluation that could not run; a receipt currently leased for processing is still refused by the existing `:252-258` guard; and a replayed `operationKey` returns the same result.
10. Verify on the local stack under **L-02**: process a receipt to completion against Azurite so the staged blob is deleted, then re-evaluate it and observe the chosen behaviour end to end. Confirm the production symptom read-only if the operator makes it available — the `transient-intake` container's `staging/` prefix is empty, which is a **read** and needs no approval.
11. Update `docs/frd/frd-02-intake-and-source-identity.md` with the re-evaluation source rule, run the simplification pass over the branch diff, record it under a dated `## Simplification pass` heading in the ticket `plan`, then open the PR into `dev`.

## Acceptance criteria

- [ ] Re-evaluating a completed receipt either completes under the current policy versions with the draft re-resolved and history appended, or is refused with an honest operator-visible reason **before any state change**.
- [ ] No receipt is ever left in `blocked_intake` with `reevaluation_pending` by a re-evaluation that could not run.
- [ ] The check happens inside the existing `ScheduleReevaluationAsync` transaction, before `State = "pending"`; a refusal leaves the receipt version and its history untouched.
- [ ] `POST /api/v1/received/{id}/reevaluate` surfaces the refusal as a problem response carrying the named reason, not a 200 followed by a failed background state.
- [ ] Fail-closed behaviour is preserved: no path silently degrades, and `IntakeArtifactIntegrityException` still fires for a genuinely corrupt source.
- [ ] The refusal copy contains no banned operator word and no internal detail.

## Verification

- [ ] `dotnet build --configuration Release` — expected: clean.
- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release` — expected: the re-evaluation precondition facts pass.
- [ ] `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"` — expected: a completed receipt re-evaluated after staged cleanup reaches the chosen outcome, and no receipt reaches `blocked_intake` by that route.
- [ ] Local stack run (L-02) — expected: process to completion against Azurite, re-evaluate, observe either a successful re-resolution or an honest refusal with the receipt untouched; command log captured as `proof`.

## Evidence tier

Tier 2 — Core/domain. Tier 4 — LocalDB persistence. Tier 6 — Functions/Azurite caller.
Tier 2 obliges positive, contradictory and failure cases for the re-evaluation precondition; tier 4 obliges evidence that a refusal is atomic with no version bump or history row; tier 6 obliges the real Worker trigger against Azurite showing delete-after-completion followed by the chosen re-evaluation behaviour.

## Documentation changes

- `docs/frd/frd-02-intake-and-source-identity.md` — state what re-evaluation requires of the source and what happens when it is unavailable
- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` — annotate the upstream `INTK-027` row with this fork ticket id (`INTK-004`) and correct the claim, repeated in `vertical-slices.md` § S9 and `screen-specs.md:284`, that S9 absorbs upstream INTK-027
- `docs/operations.md` — record the live finding and its resolution beside the release-16 verification notes

## Guardrails

- **Azure**: no write. Reading the `transient-intake` container to confirm the empty `staging/` prefix is a read and is fully permitted with no per-target approval (`docs/runbook.md` § Live-operation approval matrix; mirrored in `docs/desktop/11-azure-disposition/README.md`). Re-staging a blob **in production** would be a write and is explicitly **not** part of this ticket — the code change is; any live remediation of already-stranded receipts is a separate approved operation.
- **Scope boundary**: may touch `src/Pegasus.Core/Intake/DurableIntake.cs`, `src/Pegasus.Core/Intake/IntakeContracts.cs`, `src/Pegasus.Infrastructure/Persistence/EfIntakeMutationStore.cs`, `src/Pegasus.Web/Presentation/OperatorLabels.cs`, the three test projects and the named documents. Must **not** touch `src/Pegasus.Web/Api/**` (that is [[DSK-03-10]]'s), any desktop project, or `src/Pegasus.Web/Pages/Intake/**` beyond reading it.
- **Unblocks / blocked by**: this ticket **blocks** [[DSK-05-09]] and [[DSK-03-10]] — they publish and render a command that cannot succeed today, and both are forbidden by their own scope boundaries from repairing it. It is **blocked by** [[DSK-01-10]], the first one-way upstream sync. [[DSK-05-23]] and [[DSK-03-16]] carry the operator label vocabulary any new refusal reason joins.
- **Traps**: **upstream ids and fork board ids do not match.** This ticket is board `INTK-004` and it is upstream INTK-027; upstream INTK-004 is a different ticket again — the received-intake Case-link and label defect absorbed into [[DSK-05-20]] and [[DSK-05-23]] — and it has **no fork ticket**, so never read a bare `INTK-004` as it. The join table is `HZN-001/board-conventions.md` § Upstream ids versus board ids: read it, never compute the mapping, and write `upstream <ID>`, or `upstream <ID> (board <board-id>)` where both are meant. The fix is in `Pegasus.Infrastructure`, which [[DSK-05-09]] may not touch and [[DSK-03-10]] may not touch — that is precisely why this ticket exists; do not let it drift into either. Do not weaken `IntakeArtifactIntegrityException`: a genuinely corrupt source must still fail closed. Do not delete or change `DeleteCompletedStagedAsync` — deleting the staged copy on completion is deliberate and the retained source is the durable one. `IntakeWorkItems` state strings are persisted values.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket `plan` document.

## Outcome

PR #11 (`https://github.com/merceralex397-collab/pegasusDesktop/pull/11`) merged the retained-source re-staging fix into `dev` on 2026-08-25 and it is included in `main` at `28ba13a4fcdb51270b24a48725d53b1de5bcae87`. Merged-main proof is recorded in `proof.md`; no deployment or external remediation was performed. Follow-up API and desktop consumer work remains with [[GWY-010]] and [[FEAT-009]].


## Operator scope amendment — 2026-08-25

The operator prohibits all upstream synchronization and external deployment activity during this refactor. This supersedes the imported upstream-sync dependency and any instruction to consult or merge an upstream remote.

- All implementation, tests, documentation, commits, and PRs stay in this repository and use the configured `pegasusDesktop` remote only.
- No upstream remote is added, fetched, compared, merged, or pushed.
- No cloud or deployment write is part of this ticket; local Test/UAT evidence is sufficient for this code change.
- The ticket's current acceptance is the in-repository re-evaluation fix and its local validation; the historical upstream record remains provenance only.
