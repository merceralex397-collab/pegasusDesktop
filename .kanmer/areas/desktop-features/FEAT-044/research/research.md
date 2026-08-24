# Research — FEAT-044: Should ONNX VRM/image preprocessing move to the desktop?

> **STATUS — INCOMPLETE. Do not move this ticket to Done yet.**

This document is the spike's **output**, and writing it satisfies this ticket's only gate
(`enter-done`: `research` plus `questions-resolved`). It is therefore opened deliberately
unfinished. Everything under **Facts** is verified from the repository at fork `main` on
2026-08-24 and needs no further work. Everything marked `NOT YET CAPTURED` requires a measurement
or an operator run that a pre-work author cannot perform, and each has a matching unticked item in
this ticket's `open-questions` document. **Those unticked items are what actually holds the gate
shut** — this banner is prose; the gate reads document existence.

**No recommendation is stated below.** Stating one before the measurements exist would be the
failure this spike was created to prevent: the current placement's "measured operational
advantage" answer is only honest while somebody has actually measured the alternative.

## Question

Should the in-process ONNX vehicle-registration recognition engine move from
`src/Pegasus.Infrastructure/Vision/` to the desktop — and if the answer is anything but "keep",
what would an accepted ADR have to say, given that the engine runs **unattended** today?

## Current behaviour

The engine is in-process, server-side, and reached from the unattended intake path.

- `src/Pegasus.Infrastructure/Vision/OnnxVrmRecognitionEngine.cs` (263 lines) is the ADR-0019
  engine. Its own summary (`:7-14`) states the contract: "vendored hash-verified ONNX plate
  detection and recognition, bytes in and a result out. It performs no I/O beyond the supplied
  bytes, never uploads an image anywhere, and fails toward abstention: an unusable model set is
  `Unavailable`, an undecodable or failing image is `TechnicalFailure`, and anything unreadable is
  `NoReadableResult` rather than a guess."
- The port is Core's: `src/Pegasus.Core/ImageIntake/VrmRecognition.cs:45` declares
  `IVrmRecognitionEngine`. The composition root binds it at
  `src/Pegasus.Infrastructure/DependencyInjection.cs:114` —
  `services.TryAddSingleton<IVrmRecognitionEngine, OnnxVrmRecognitionEngine>()` — with
  `IImageIntakeAutomation` registered scoped on the next line (`:115`).
- **It runs unattended.** `ImageIntakeAutomation` is invoked from
  `src/Pegasus.Core/Intake/DurableIntake.cs:508` (replay) and `:626` (first pass), which is the
  durable intake path driven by the Worker's queue trigger. `docs/current-architecture.md:262`
  states it plainly: "Ordinary images are retained review evidence; they are scanned by the
  in-process ONNX VRM engine (ADR-0019) and are **never sent to an external OCR or vision
  service**." This is the fact that dominates the whole question — see *Execution placement*.

**Parity-matrix row.** The engine itself is not a page-model capability, so no row covers it
directly — the matrix's 46 rows are all keyed to page models under `src/Pegasus.Web/Pages/**`
(`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → 46). The row that
covers the **operator-visible surface the engine feeds** is **`PAR-26`**
(`docs/desktop/01-inventory-and-parity/parity-matrix.md:71`) — "13.4 Intake (images)", FRD-06,
entry points `ImageIntake/Index.cshtml.cs` (85) and `ImageIntake/Details.cshtml.cs` (89), whose
API/data column names "`IImageIntakeQueries`; Image Intake Reference; **VRM suggestions**;
registration-matched candidates", native target "Vehicle images workspace", status
**`not inventoried`**. `PAR-19` (`:64`) also touches it through
`OnPostDismissSuggestionAsync`. Neither row's status changes as a result of this spike — it
recommends, it does not implement.

## Findings

- **The engine's own code has no server-side entanglement.** Every `using` across the four files:
  `OnnxVrmRecognitionEngine.cs:1-3` — `Microsoft.ML.OnnxRuntime`, `Pegasus.Core.ImageIntake`,
  `SkiaSharp`; `PlateDetector.cs:1-3` — `Microsoft.ML.OnnxRuntime`,
  `Microsoft.ML.OnnxRuntime.Tensors`, `SkiaSharp`; `PlateRecognizer.cs:1-3` — the same three;
  `VisionModels.cs:1-3` — `System.Reflection`, `System.Security.Cryptography`, `System.Text.Json`.
  **No EF Core, no Graph, no Box, no Azure Storage anywhere in the vision folder.** This is the
  single most encouraging finding for separability and it is verifiable in one command.
- **The surrounding automation *is* entangled, and that distinction matters.** The four
  registrations either side of the engine at `DependencyInjection.cs:110-115` are
  `EfImageIntakeOriginResolver`, `EfImageIntakeCaseCandidates`, `RegisterImageIntake` and
  `EfImageVrmSuggestionStore` — all EF-backed. So "the engine is separable" and "image intake
  automation is separable" are different claims, and only the first is supported. A desktop-side
  engine would still have to hand its result back to a server-side automation that owns the
  suggestion store and the case candidates.
- **The model set is 13,036,223 bytes and hash-pinned.**
  `src/Pegasus.Infrastructure/Vision/Models/`: `yolo-v9-t-384-license-plates-end2end.onnx`
  **7,771,218** bytes, `cct_s_v2_global.onnx` **5,262,230** bytes,
  `cct_s_v2_global_plate_config.yaml` 1,725, `vision-models-manifest.json` 1,050. All four are
  embedded with explicit `LogicalName`s at
  `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:34-41`. `VisionModels.cs:7-11` records
  why: "embedded bytes verified against the hash-pinned manifest before any session is created. A
  hash mismatch makes the engine `Unavailable`; **nothing is ever downloaded at runtime**." A
  desktop package cannot fetch these lazily without breaking that rule.
- **The native dependency set is the unmeasured part, and there is a Windows-specific wrinkle.**
  `Pegasus.Infrastructure.csproj:21` `Microsoft.ML.OnnxRuntime` 1.20.1; `:27` `SkiaSharp` 3.116.1;
  `:28` **`SkiaSharp.NativeAssets.Linux.NoDependencies` 3.116.1**. The only explicitly referenced
  native-asset package today is the **Linux** one — because the gateway runs in a Linux container.
  A `win-x64` self-contained desktop package pulls a different native set, so the package-size
  delta cannot be inferred from the current build output and must be measured by publishing.
- **ADR-0019 already priced the footprint qualitatively and left the quantitative question open.**
  Its Consequences say the runtime and vendored bytes "add tens of megabytes to the build". Its
  Decision fixes the boundary this spike must respect: "`Pegasus.Core` owns the port; the ONNX
  execution lives in `Pegasus.Infrastructure`" and "**No image leaves the application, no external
  credential exists, and no new deployment unit is created**". Its final Consequences paragraph is
  the governing sentence for this ticket: "**A future engine change — an external adapter, a
  retrained detector, or a replacement recogniser — is a new decision against the same cohort and
  gate, not a silent swap.**" A placement move is exactly such a decision.
- **The accuracy bar is accepted, numeric, and reproducible only on a machine holding the corpus.**
  `docs/operations.md:255` records the 2026-08-03 acceptance at the **0.80** confidence bar with the
  `INT-28`/`INT-32` match rules: full-cohort run `20260803-092906` over **2,818** cohort images —
  315 suggestions, **3.2%** genuine near-misses, 13.7% correctly read third-party registrations,
  **zero** technical failures; one-time holdout run `20260803-102921` over **705** untouched images
  — 88 suggestions at 12.5%, 2 genuine near-misses at **2.3%**, 14 third-party, zero technical
  failures. Those are the numbers a desktop-side run must be compared against.
- **The evaluation harness already exists and is designed for exactly this comparison.**
  `tests/Pegasus.IntegrationTests/VrmRecognitionCorpusEvaluationTests.cs` (319 lines) carries
  `[SkippableCorpusFact]` and `[Trait("Category", "Corpus")]`. Its summary (`:10-20`) records the
  protocol: it reads the ignored immutable corpus, evaluates candidate thresholds
  `[0.5, 0.6, 0.7, 0.8, 0.9]` (`:24`), splits deterministically **80% cohort / 20% holdout by
  relative-path hash** (`:37-43`), leaves the holdout untouched unless `PEGASUS_VRM_EVAL_HOLDOUT=1`,
  bounds a run with `PEGASUS_VRM_EVAL_LIMIT`, writes its report under
  `artifacts/vrm-recognition-eval/`, and states that "a bounded run is never presented as the full
  cohort". `artifacts/` is ignored (`.gitignore:20-21`), so **figures are reported and no corpus
  file is committed** — which is `docs/engineering.md` tier 8 satisfied by construction.
- **`FakeVrmRecognitionEngine.cs` (36 lines) exists**, so the port is already fake-able and a
  desktop-side implementation would not be the first alternative to the ONNX one.

### Facts

Every row verified on 2026-08-24 at fork `main`, with the command that produced it.

| Fact | Command | Value |
| --- | --- | --- |
| Vision source size | `wc -l src/Pegasus.Infrastructure/Vision/*.cs` | `OnnxVrmRecognitionEngine.cs` **263**, `PlateDetector.cs` **137**, `PlateRecognizer.cs` **88**, `VisionModels.cs` **131**; **619** total |
| Model file sizes | `ls -la src/Pegasus.Infrastructure/Vision/Models/` | `yolo-v9-t-384-license-plates-end2end.onnx` **7,771,218** B; `cct_s_v2_global.onnx` **5,262,230** B; `cct_s_v2_global_plate_config.yaml` **1,725** B; `vision-models-manifest.json` **1,050** B; **13,036,223 B total (≈ 12.43 MiB)** |
| Vision `using` set — no EF Core, Graph, Box or Azure Storage | `head -20 …/OnnxVrmRecognitionEngine.cs`, `head -12` on the other three | `Microsoft.ML.OnnxRuntime`(`.Tensors`), `SkiaSharp`, `Pegasus.Core.ImageIntake`, `System.Reflection`, `System.Security.Cryptography`, `System.Text.Json` — nothing else |
| Package references | `grep -n -i "onnx\|SkiaSharp" src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj` | `Microsoft.ML.OnnxRuntime` **1.20.1** (`:21`); `SkiaSharp` **3.116.1** (`:27`); `SkiaSharp.NativeAssets.Linux.NoDependencies` **3.116.1** (`:28`) — **Linux natives only** |
| Models embedded with explicit logical names | same file `:34-41` | four `EmbeddedResource` entries under `Pegasus.Infrastructure.Vision.Models.*` |
| Core owns the port | `grep -rn "IVrmRecognitionEngine" src/` | interface at `src/Pegasus.Core/ImageIntake/VrmRecognition.cs:45`; consumer `ImageIntakeAutomation.cs:46` |
| Composition binding | `sed -n '110,118p' src/Pegasus.Infrastructure/DependencyInjection.cs` | `TryAddSingleton<IVrmRecognitionEngine, OnnxVrmRecognitionEngine>()` at `:114`; `IImageIntakeAutomation` scoped at `:115` |
| Surrounding automation is EF-backed | same lines `:110-113` | `EfImageIntakeOriginResolver`, `EfImageIntakeCaseCandidates`, `RegisterImageIntake`, `EfImageVrmSuggestionStore` |
| **The engine runs unattended** | `grep -rn "ImageIntakeAutomation" src/` | invoked from `src/Pegasus.Core/Intake/DurableIntake.cs:508` and `:626` — the Worker-driven durable intake path |
| No image leaves the application | `docs/current-architecture.md:262`; ADR-0019 Consequences | "never sent to an external OCR or vision service" |
| Implementation ≠ acceptance | `docs/current-architecture.md:147-150` | "Implementation is not live-caller acceptance." |
| Accepted threshold and cohort numbers | `docs/operations.md:255` | 0.80 bar; cohort `20260803-092906` 2,818 images / 315 suggestions / 3.2% near-miss / 0 technical failures; holdout `20260803-102921` 705 images / 88 suggestions / 2.3% near-miss / 0 technical failures |
| ADR-0019 status and boundary | `cat docs/adr/0019-in-process-onnx-vrm-recognition.md` | `status: accepted`, `date: 2026-08-03`, `related_frd: [frd-06]`; "a new decision against the same cohort and gate, not a silent swap" |
| Evaluation harness shape | `sed -n '1,45p' tests/Pegasus.IntegrationTests/VrmRecognitionCorpusEvaluationTests.cs` | 319 lines; `[SkippableCorpusFact]`, `Category=Corpus`; thresholds `[0.5,0.6,0.7,0.8,0.9]`; deterministic 80/20 split; `PEGASUS_VRM_EVAL_HOLDOUT`, `PEGASUS_VRM_EVAL_LIMIT`; report under `artifacts/vrm-recognition-eval/` |
| Corpus and reports stay untracked | `grep -n -i "corpus\|artifacts" .gitignore` | `/corpus/` (`:1-2`), `**/artifacts/` and `/artifacts/` (`:20-21`) |
| Other test assets | `wc -l tests/Pegasus.IntegrationTests/*Vrm*.cs` | `VrmRecognitionEngineTests.cs` **133**, `FakeVrmRecognitionEngine.cs` **36** |
| Desktop projects do not exist yet | `ls src/`, `ls tests/` | `src/` holds only Core, Infrastructure, Web, Worker; `tests/` only ArchitectureTests, Core.Tests, IntegrationTests |

### Assumptions

- **`A-07-18-1` — the engine is separable into a project the desktop could reference without
  dragging EF Core, Graph, Box or Azure Storage.** Strongly supported by the `using` set above, but
  not proved: separability is decided by what compiles, not by what is imported. *Confirmed by*:
  the extraction trial in `NOT YET CAPTURED — B`. *Breaks if wrong*: a "move" recommendation
  becomes far more expensive than the model bytes suggest, and the split option in step 9 becomes
  the only viable non-keep answer.
- **`A-07-18-2` — a `win-x64` self-contained package needs different SkiaSharp native assets from
  the ones referenced today.** `:28` references only the Linux package. *Confirmed by*: the publish
  in `NOT YET CAPTURED — A`. *Breaks if wrong*: the package delta is smaller than budgeted, which
  only makes a move easier.
- **`A-07-18-3` — the baseline workstation can run the two ONNX sessions on CPU without a GPU
  execution provider.** The gateway does so today in a container. *Confirmed by*: the measurement
  in `NOT YET CAPTURED — C`. *Breaks if wrong*: the placement question is settled against a move
  immediately, on hardware grounds.
- **`A-07-18-4` — the same corpus, cohort split and 0.80 bar can be applied to a desktop-side run.**
  The harness splits deterministically by relative-path hash, so the split is reproducible on any
  machine holding the same corpus. *Confirmed by*: `NOT YET CAPTURED — D`. *Breaks if wrong*: there
  is no comparable accuracy figure and no move can be recommended at all.
- **`A-07-18-5` — moving the engine does not change the abstention semantics.** `Unavailable` /
  `TechnicalFailure` / `NoReadableResult` (`OnnxVrmRecognitionEngine.cs:7-14`) are the port's
  contract, not the host's. *Confirmed by*: reading `VrmRecognition.cs:45` against any desktop-side
  implementation. *Breaks if wrong*: a missing runtime on a workstation would surface as a guess
  rather than an abstention, which ADR-0019 forbids.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3 (`:166-178`), for **automatic
vehicle-registration recognition from intake images**. Five rows are answerable from the repository
today; the sixth is the whole point of this spike and is honestly left uncaptured.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **Yes** | A VRM suggestion is case state every operator sees and any authorised operator may confirm or dismiss — `Intake/Details.cshtml.cs` `OnPostDismissSuggestionAsync` (`parity-matrix.md:64`), stored through `EfImageVrmSuggestionStore` (`DependencyInjection.cs:113`). The **suggestion store** therefore stays central regardless of where the bytes are crunched. Lands with the gateway and its existing SQL store. |
| Unattended execution — must it run with every desktop closed? | **Yes — and this is decisive** | The engine is reached from `DurableIntake.cs:508` and `:626`, the Worker-driven durable intake path. Image-only intake is scanned automatically as it arrives. Lands with the **existing Worker** — an always-on host that already exists, not a new one. A desktop-only engine would **silently drop automatic scanning**, which is the trap the ticket body names in step 8. |
| Protected credentials — long-lived secret that must not sit on workstations? | **No** | There is no credential of any kind. ADR-0019: "no external credential exists"; `VisionModels.cs:7-11`: "nothing is ever downloaded at runtime". The models are hash-pinned embedded bytes. |
| Public callback — must an external service call a stable public endpoint? | **No** | Nothing external is involved in either direction. `docs/current-architecture.md:262`: images are "never sent to an external OCR or vision service". |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes** | Suggestion-first is the settled product boundary: ADR-0019's Decision states "every suggested VRM requires an authorised staff confirmation before any record uses it, and the suggestion stays bound to its retained source image", and the accepted **0.80** bar with the `INT-28`/`INT-32` match rules (`docs/operations.md:255`) must hold no matter what produced the candidate. A client that could set its own threshold would be a different capability. Lands in **Core**, enforced server-side. |
| Measured operational advantage — measured evidence that central is materially better? | **`NOT YET CAPTURED`** | This is the question the spike exists to answer, and answering it from the current placement would be circular. The area plan's § 3 currently records "yes" — "measured operational advantage today yes (accepted engine in place)" — and that answer is only honest while nobody has measured the alternative. See `NOT YET CAPTURED — A` through `D`. |

**Reading so far, stated carefully:** three "yes" answers already place the *suggestion store*, the
*unattended trigger* and the *threshold enforcement* server-side, on hosts that already exist and
**not** in any new Azure resource. What remains genuinely open is only whether the *pixel-crunching
step* is better performed on the workstation — which is the split option the body's step 9 requires
be stated explicitly rather than assumed away. **No conclusion is drawn here.**

---

## `NOT YET CAPTURED` — A. Package-size delta for a `win-x64` self-contained MSIX

**Question the output must answer:** what does adding the ONNX runtime, SkiaSharp Windows native
assets and 13,036,223 bytes of models do to the desktop package size, as a single number in MB?

**Exact command**, once `src/Pegasus.Desktop` exists ([[FND-030]], plan handle `DSK-02-05`) — this
measurement cannot be taken before that project lands:

```
dotnet publish ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -c Release -r win-x64 --self-contained true
```

taken twice — once as built, once with a project reference to the extracted vision project from
**B** — and the two output directory sizes compared. Record the ONNX Runtime and SkiaSharp **native**
asset sizes separately from the managed ones, because assumption `A-07-18-2` says the Windows native
set differs from the Linux package referenced at `Pegasus.Infrastructure.csproj:28`.

**Why it matters:** proposal § 7.1 defers Native AOT until startup is profiled, and C-01 makes a
larger package a recurring CI cost on private-repository Windows runners billed at 2×. "Tens of
megabytes" (ADR-0019 Consequences) is not a number a decision can be taken on.

## `NOT YET CAPTURED` — B. Separability of the vision code (`A-07-18-1`)

**Question the output must answer:** can `Vision/` become a project the desktop references without
dragging EF Core, Graph, Box or Azure Storage — and if not, exactly what blocks it?

**Exact procedure:** create a throwaway `Pegasus.Vision` class library referencing only
`Pegasus.Core`, `Microsoft.ML.OnnxRuntime` 1.20.1 and `SkiaSharp` 3.116.1; move the four files and
the four embedded model resources into it; build:

```
dotnet build ./src/Pegasus.Vision/Pegasus.Vision.csproj -c Release
```

Record every compile error. **Delete the trial afterwards** — Guardrails forbid moving the engine,
and the deliverable is the finding, not the project. Record separately what `ImageIntakeAutomation`
would still need from the server side, since `DependencyInjection.cs:110-113` shows the surrounding
automation is EF-backed even though the engine is not.

## `NOT YET CAPTURED` — C. Per-image cost on the baseline workstation (`A-07-18-3`) — **operator step**

**Question the output must answer:** how long does one image take, and what is the peak working set,
on the weakest supported Windows 11 workstation?

**Exact command:** run the existing engine locally over a small representative set —
`dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~VrmRecognitionEngineTests"`
as the smoke, then a bounded corpus run with `PEGASUS_VRM_EVAL_LIMIT` set — and record wall-clock
time and peak working set **per image**.

**Operator hands back**, per the body's step 6: machine specification (CPU model, core count, RAM),
per-image wall-clock times, peak working set, and whether other work was running. **No image content
leaves the machine.** If the baseline workstation is unavailable to the agent, the figures stay
marked pending here and the recommendation is not written.

## `NOT YET CAPTURED` — D. Desktop-side accuracy parity against the accepted holdout (`A-07-18-4`)

**Question the output must answer:** does a desktop-side run reproduce the accepted numbers — 3.2%
genuine near-misses on the 2,818-image cohort and 2.3% on the 705-image holdout at the 0.80 bar
(`docs/operations.md:255`)?

**Exact command**, on a machine holding the immutable local corpus:

```
dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category=Corpus"
```

with `PEGASUS_VRM_EVAL_HOLDOUT=1` **only** for the one-time holdout confirmation, exactly as the
harness's own summary prescribes (`VrmRecognitionCorpusEvaluationTests.cs:10-20`). Report the
figures from `artifacts/vrm-recognition-eval/`; **commit nothing from that directory** — it is
ignored (`.gitignore:20-21`) and `docs/engineering.md` tier 8 keeps detailed corpus evidence local.

**Note on method:** the deterministic 80/20 split by relative-path hash (`:37-43`) is what makes a
desktop-side run comparable at all. If a move would change the model bytes, the ONNX Runtime
version, or the pre-processing, ADR-0019's "new decision against the same cohort and gate, not a
silent swap" applies and a fresh evaluation is required rather than a comparison.

## `NOT YET CAPTURED` — E. Fleet consequence against proposal § 15.1

**Question the output must answer:** would a desktop-side recognition run block the UI thread,
breach a navigation budget, or make a document-heavy case unusable on the weakest supported machine?
State the mitigation (background execution, queueing) or the disqualifier.

**Depends on C.** Cannot be answered before the per-image figures exist.

## `NOT YET CAPTURED` — F. The recommendation itself

**Question the output must answer:** **keep server-side**, **move**, or **split** — with the
evidence behind it, and, if it is not "keep", the new ADR in the reserved block
ADR-0100…ADR-0110 that would supersede ADR-0019 plus the follow-up ticket that would do the work.

**Depends on A–E.** It also requires the body's step 9 answered explicitly rather than treated as
all-or-nothing: automatic unattended scanning stays server-side while a *user-invoked* re-run or
preview runs locally — is that worth **two implementations of one capability**? `AGENTS.md`
§ Simplicity rails treats a second business implementation as a stop condition, so a split needs a
strong, stated reason, not a convenience.

**Constraint on any non-keep answer**, from ADR-0019's Decision and this ticket's Traps: no engine
moves without an accepted ADR; ADR-0019 currently holds the placement and would need superseding by
a **new** ADR in the reserved block, never a "next free" number; no cloud AI may be introduced
(proposal § 12.6, and `azure-ai` is on this area's do-not-load list).

---

## Implications

Provisional, and explicitly subject to A–F.

1. **The engine's code is the easy part; the trigger is the hard part.** The `using` set says the
   engine could move. `DurableIntake.cs:508`/`:626` says something must still scan automatically
   while every desktop is closed. Any honest recommendation has to say what happens to unattended
   scanning, and "the desktop does it when it is open" is not an answer.
2. **The suggestion store, the 0.80 bar and the confirmation requirement stay central whatever
   happens.** Three of the six questions answer "yes" independently of where the pixels are
   crunched, so the maximum possible scope of a move is the recognition step alone.
3. **The split option is the only shape a move could plausibly take** — and it costs two
   implementations of one capability, which `AGENTS.md` § Simplicity rails treats as a stop
   condition. The body's step 9 is right to demand it be stated rather than drifted into.
4. **13,036,223 bytes is a floor, not the delta.** The native asset sets are unmeasured and the
   Windows ones are not the ones referenced today (`A-07-18-2`).
5. **The evaluation protocol is already fit for purpose.** The harness's deterministic split,
   threshold sweep, bounded-run honesty and ignored output directory mean the accuracy comparison
   needs a machine and a run, not new test infrastructure.
6. **The measurements are cheap relative to the decision.** Two publishes, one throwaway build and
   one operator run settle a placement question that ADR-0019 explicitly left as "a new decision".

## Open questions

Six, recorded as unticked items in this ticket's `open-questions` document — one per
`NOT YET CAPTURED` block above, plus the timebox the body's step 1 requires be set and recorded.
Each blocks `enter-done`, which for a `spike` is the only gated boundary and exactly the intended
behaviour: this document must not be allowed to close an unfinished spike.

Nothing here is an operator *decision* awaiting an answer that has already been given. ADR-0019 and
its 2026-08-03 threshold acceptance are settled and are **not** reopened by this spike; what is open
is a measurement nobody has taken.
