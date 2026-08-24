---
id: FEAT-044
type: ticket
title: >-
  DSK-07-18 · Spike: should ONNX VRM/image preprocessing move to the desktop?
  Engine size, accuracy parity, fleet CPU
status: preparing
area: desktop-features
assignee: ''
profile: spike
stageEntered:
  preparing: '2026-08-24T21:31:46.726Z'
labels:
  - desktop-conversion
  - plan-07
  - phase-6
  - tier-2
groups:
  - EPIC-008
  - HZN-007
links: []
refs:
  - docs/adr/0019-in-process-onnx-vrm-recognition.md
docs_todo: true
archived: false
created: '2026-08-24T08:30:09.694Z'
updated: '2026-08-24T21:31:46.726Z'
---

## What

A written recommendation on whether the in-process ONNX vehicle-registration recognition engine should move from `Pegasus.Infrastructure` to the desktop, covering the engine's dependency and model footprint, accuracy parity, CPU behaviour on the baseline fleet, and what an accepted ADR would have to say. No engine is moved by this ticket.

## Why

Proposal § 12.6 defaults user-invoked preprocessing to the desktop "when it can run reliably on the Windows 11 fleet", but this area's § 3 records a deliberate deviation: the ONNX engine already lives in `Pegasus.Infrastructure` with embedded models under an accepted evaluation (ADR-0019), so under the six-question test its "measured operational advantage" answer is currently yes and placement stays server-side. That answer is only honest while nobody has measured the alternative — which is what this spike does. It also matters for package size and startup: proposal § 7.1 defers Native AOT until startup is profiled, and adding ~13 MB of models plus the ONNX runtime to an MSIX is a decision, not a detail.

## Source of truth

- Plan row: `docs/desktop/07-integrations/README.md` § 5 — `DSK-07-18`
- Plan context: `docs/desktop/07-integrations/README.md` § 3 (Deviation: OCR/ONNX), § 1 (the §12.6 row)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 12.6 OCR, image analysis and future AI, § 4 cloud-justification test, § 15.1 performance budgets, § 7.1 runtime
- Repository evidence: `src/Pegasus.Infrastructure/Vision/OnnxVrmRecognitionEngine.cs` (263 lines), `PlateDetector.cs` (137), `PlateRecognizer.cs` (88), `VisionModels.cs` (131); `src/Pegasus.Infrastructure/Vision/Models/` — `yolo-v9-t-384-license-plates-end2end.onnx` (7,771,218 bytes), `cct_s_v2_global.onnx` (5,262,230 bytes), `cct_s_v2_global_plate_config.yaml`, `vision-models-manifest.json`, all embedded with explicit `LogicalName`s in `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj`; `Microsoft.ML.OnnxRuntime` 1.20.1 and `SkiaSharp` 3.116.1 in the same csproj; `tests/Pegasus.IntegrationTests/VrmRecognitionEngineTests.cs`, `VrmRecognitionCorpusEvaluationTests.cs`, `FakeVrmRecognitionEngine.cs`; `docs/adr/0019-in-process-onnx-vrm-recognition.md`; `docs/current-architecture.md:149-150,263`
- Binding decisions: the deviation recorded in this area's § 3 — placement stays server-side until a spike says otherwise, and **no engine move without an accepted ADR**. L-02 — measurement happens on the local stack and the baseline workstation, never on an Azure test resource. C-01 — a bigger package costs CI minutes on private-repository Windows runners at 2×.
- Depends on: `DSK-05-14` the documents slice, `DSK-05-15` the vehicle slice and `DSK-05-16` the images and gallery slice — the Phase 6 desktop surfaces whose image handling the recommendation must account for

## Routing

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `microsoft-code-reference` (Microsoft Learn plugin) → `kanmer-research` (`.grok/skills/kanmer-research/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search` for `Microsoft.ML.OnnxRuntime` packaging and execution-provider guidance on Windows x64)
- **Kanmer pipeline** for profile `spike`: `kanmer-research` → `kanmer-verify` → `kanmer-closeout` (the only gate is `enter-done`: `research` plus `questions-resolved`; call `get_doc_gates <id>` before every move)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient: read the plan row, this area's § 3 OCR/ONNX deviation, proposal § 12.6, and `docs/adr/0019-in-process-onnx-vrm-recognition.md` in full — the accepted evaluation behind the current placement. Call `get_doc_gates <this ticket id>`, then `take_ticket`. Set and record a timebox before starting.
2. Establish the footprint from the repository, not from memory: record the two model sizes (7,771,218 and 5,262,230 bytes — about 13 MB combined), the `Microsoft.ML.OnnxRuntime` 1.20.1 and `SkiaSharp` 3.116.1 dependencies, and the native asset sets each would add to a `win-x64` self-contained MSIX. State the estimated package-size delta as a number.
3. Map the code boundary: list what `OnnxVrmRecognitionEngine`, `PlateDetector`, `PlateRecognizer` and `VisionModels` depend on inside `Pegasus.Infrastructure`, and say whether the engine is separable into a project the desktop could reference without dragging EF Core, Graph, Box or Azure Storage with it. If it is not separable today, say what it would take.
4. Establish the accuracy baseline: read `tests/Pegasus.IntegrationTests/VrmRecognitionCorpusEvaluationTests.cs` and record what accuracy the current engine is held to, on what cohort, and how a desktop-side run would be evaluated against the same holdout. Note that detailed corpus evidence stays local and ignored (`docs/engineering.md` tier 8) — the spike reports figures, not corpus files.
5. Measure, do not estimate, single-image recognition cost on the baseline workstation: run the existing engine locally over a small representative set and record wall-clock time and peak working set per image. If the baseline workstation is unavailable to the agent, record it as an operator step and mark the figures pending.
6. **Operator step** — if measurement needs the baseline hardware, the operator runs the harness and hands back: machine specification, per-image wall-clock times, peak working set, and whether other work was running. No image content leaves the machine.
7. Assess the fleet consequence against proposal § 15.1: would a desktop-side recognition run block the UI thread, breach a navigation budget, or make a document-heavy case unusable on the weakest supported machine? State the mitigation (background execution, queueing) or the disqualifier.
8. Answer the six-question cloud-justification test from `docs/desktop/00-governance-and-workflow/README.md` § 3 for VRM recognition, with evidence per row. Pay particular attention to question 2 — the engine currently runs unattended, scanning image-only intake automatically (`docs/current-architecture.md:263`), which a desktop-only engine could not do while every desktop is closed.
9. State the split option explicitly rather than treating this as all-or-nothing: automatic, unattended scanning stays server-side; a *user-invoked* re-run or preview could run locally. Say whether the split is worth two implementations of one capability — `AGENTS.md` § Simplicity rails treats a second business implementation as a stop condition, so a split needs a strong reason.
10. Write the recommendation with one of three outcomes and the evidence behind it: **keep server-side** (the current placement, with the measured advantage now recorded rather than assumed); **move**, which requires a new ADR in the reserved block and a follow-up ticket; or **split**, which requires the same ADR plus an explicit answer to step 9. No engine may move without an accepted ADR.
11. Record every unresolved question in the ticket's `open-questions` document, confirm `git status` shows no production change, and close the spike with the timebox actually spent.

## Acceptance criteria

- [ ] Package-size delta, dependency set and native asset impact are stated as numbers.
- [ ] The separability of the vision code from `Pegasus.Infrastructure` is assessed concretely.
- [ ] Accuracy parity method is defined against the existing corpus evaluation, with no corpus material committed.
- [ ] Per-image time and memory on baseline hardware are measured or explicitly marked pending an operator run.
- [ ] The six cloud-justification answers are recorded, including the unattended-execution consequence.
- [ ] The recommendation is one of keep / move / split, with the ADR and follow-up ticket named if it is not "keep".
- [ ] No engine, model or project reference is moved by this ticket.

## Verification

- [ ] `get_ticket_doc <this ticket id> research` — expected: footprint numbers, separability assessment, accuracy method, measurements, six answers and a single named recommendation.
- [ ] `git status --porcelain` — expected: no production source, model or project file modified.
- [ ] `git diff --stat origin/dev -- src tests` — expected: empty output.

## Evidence tier

Tier 2 — Core/domain.
Tier 2 obliges reasoning about the domain behaviour itself — positive, contradictory, ambiguous and failure cases for recognition — which here means an evidence-based placement judgement rather than a preference.

## Documentation changes

- None. The recommendation lives in the ticket's `research` document; a decision to move becomes a new ADR in the reserved block plus its own ticket.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: this ticket writes **no** production code and moves no model. It may read anything under `src/` and may run existing tests and a local measurement harness.
- **Traps**: no engine move without an accepted ADR — ADR-0019 currently holds the placement and would need superseding by a new ADR in the reserved block ADR-0100…ADR-0110, never a "next free" number; the engine runs unattended today, so a desktop-only engine silently drops automatic scanning; no cloud AI may be introduced (proposal § 12.6) and `azure-ai` is on this area's do-not-load list; corpus material stays local and ignored (`docs/engineering.md` tier 8); a split placement means two implementations of one capability and needs justifying against `AGENTS.md` § Simplicity rails.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — docs-only`.

## Outcome

_Filled at closeout._
