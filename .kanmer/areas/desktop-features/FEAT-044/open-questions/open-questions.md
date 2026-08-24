# Open questions

**This spike is unfinished, and these boxes are what say so.** The `research` document is this
spike's output, and writing it satisfies the `enter-done` gate on its own — so a half-written
scaffold would make an unfinished spike closable. Each unticked item below corresponds to one
`NOT YET CAPTURED` block in `research`, and together they hold `enter-done` shut until the spike has
actually been done. That is the intended behaviour, not a defect.

For a `spike` these items gate **`enter-done` and nothing else** — the profile's only requirement
is `research` plus `questions-resolved` at that one boundary. They do not gate `leave-backlog`.

Tick an item only when the corresponding `NOT YET CAPTURED` block in `research` has been replaced by
the captured figures and their source.

---

- [ ] **Timebox set and recorded.** The body's step 1 requires a timebox to be set *before* starting
      and the amount actually spent to be recorded at step 11. Record both in `research` under a
      dated heading. This is the cheapest box here and the one most often skipped; an untimeboxed
      spike is how a two-day measurement becomes a two-week investigation.

- [ ] **A — Package-size delta for a `win-x64` self-contained MSIX, as a number in MB.**
      Run `dotnet publish ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -c Release -r win-x64 --self-contained true`
      twice — as built, and with a reference to the extracted vision project from **B** — and compare
      output directory sizes, recording ONNX Runtime and SkiaSharp **native** asset sizes separately
      from managed ones. **Blocked until `src/Pegasus.Desktop` exists** ([[FND-030]], plan handle
      `DSK-02-05`). Settles `A-07-18-2`, since
      `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:28` references only the **Linux**
      SkiaSharp native package today. The known floor is 13,036,223 bytes of models; ADR-0019's
      "tens of megabytes" is not a number a decision can be taken on.

- [ ] **B — Is the vision code separable from `Pegasus.Infrastructure`?** (`A-07-18-1`)
      Create a throwaway `Pegasus.Vision` library referencing only `Pegasus.Core`,
      `Microsoft.ML.OnnxRuntime` 1.20.1 and `SkiaSharp` 3.116.1; move the four files and four
      embedded model resources into it; run
      `dotnet build ./src/Pegasus.Vision/Pegasus.Vision.csproj -c Release` and record **every**
      compile error. **Delete the trial afterwards** — Guardrails forbid moving the engine and the
      deliverable is the finding. Record separately what `ImageIntakeAutomation` would still need
      server-side, because `DependencyInjection.cs:110-113` shows the surrounding automation is
      EF-backed even though the engine's own `using` set is not.

- [ ] **C — Per-image wall-clock time and peak working set on the baseline workstation.**
      (`A-07-18-3`) — **operator step.**
      Run
      `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "FullyQualifiedName~VrmRecognitionEngineTests"`
      as the smoke, then a bounded corpus run with `PEGASUS_VRM_EVAL_LIMIT` set.
      **The operator hands back:** machine specification (CPU model, core count, RAM), per-image
      wall-clock times, peak working set, and whether other work was running. **No image content
      leaves the machine.** If the baseline workstation is unavailable, this box stays unticked and
      the figures stay marked pending — the body's step 5 permits exactly that, and box **F** cannot
      be ticked without it.

- [ ] **D — Desktop-side accuracy parity against the accepted cohort and holdout.** (`A-07-18-4`)
      On a machine holding the immutable local corpus, run
      `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category=Corpus"`,
      with `PEGASUS_VRM_EVAL_HOLDOUT=1` **only** for the one-time holdout confirmation, exactly as
      `VrmRecognitionCorpusEvaluationTests.cs:10-20` prescribes. Compare against the accepted figures
      at `docs/operations.md:255`: 0.80 bar; cohort run `20260803-092906` over 2,818 images with
      **3.2%** genuine near-misses; holdout run `20260803-102921` over 705 images with **2.3%**.
      **Report figures only — commit nothing from `artifacts/vrm-recognition-eval/`**; it is ignored
      (`.gitignore:20-21`) and `docs/engineering.md` tier 8 keeps detailed corpus evidence local.
      If a move would change the model bytes, the runtime version or the pre-processing, ADR-0019's
      "new decision against the same cohort and gate, not a silent swap" applies and a **fresh
      evaluation** is required rather than a comparison — say which this is.

- [ ] **E — Fleet consequence against proposal § 15.1.** Would a desktop-side recognition run block
      the UI thread, breach a navigation budget, or make a document-heavy case unusable on the
      weakest supported machine? State the mitigation (background execution, queueing) or the
      disqualifier. **Depends on C** and cannot be answered before those figures exist.

- [ ] **F — The recommendation: keep / move / split, with its evidence.** **Depends on A–E.**
      It must also answer the body's step 9 explicitly rather than treating the question as
      all-or-nothing: automatic unattended scanning stays server-side while a *user-invoked* re-run
      or preview runs locally — **is that worth two implementations of one capability?**
      `AGENTS.md` § Simplicity rails treats a second business implementation as a stop condition, so
      a split needs a strong stated reason, not a convenience.
      Any non-keep answer must name the **new ADR in the reserved block ADR-0100…ADR-0110** that
      would supersede ADR-0019 — never a "next free" number — and the follow-up ticket that would do
      the work. **No engine moves without an accepted ADR.** No cloud AI may be introduced (proposal
      § 12.6; `azure-ai` is on this area's do-not-load list).

---

## Parked (explicitly deferred)

Nothing is parked. The items below are recorded only so a reader does not mistake them for open
questions.

- **ADR-0019 and its 2026-08-03 threshold acceptance are settled and are not reopened by this
  spike.** The operator selected the in-process route on 2026-08-03 and accepted the **0.80** bar
  with the `INT-28`/`INT-32` match rules (`docs/operations.md:255`). What is open here is a
  measurement nobody has taken, not a decision awaiting a second answer.
- **Suggestion-first is not in question.** ADR-0019's Decision fixes it: every suggested VRM
  requires an authorised staff confirmation before any record uses it, and the suggestion stays
  bound to its retained source image. No outcome of this spike changes that.
- **Automatic image-led and instruction-led matching (`INT-28` / `INT-32`) is separately gated** —
  ADR-0019 says so in as many words: "reading a plate is not associating a record". Out of scope.
- The desktop project and the desktop test project are created by [[FND-030]] (plan handle
  `DSK-02-05`) and [[TEST-004]] (plan handle `DSK-08-04`) / [[FND-038]] (plan handle `DSK-02-13`);
  box **A** waits on the first of those.

## Defaults taken rather than asked

- **No recommendation was pre-written.** The obvious temptation is to record "keep server-side" now,
  since three of the six cloud-justification answers already land server-side and the unattended
  trigger looks decisive. That would make the spike's conclusion identical to its premise and would
  reproduce exactly the circularity the ticket exists to break — the area plan's current
  "measured operational advantage: yes" is honest only once somebody has measured the alternative.
  The five determinable cloud-justification rows **are** answered in `research`; the sixth is marked
  `NOT YET CAPTURED` rather than guessed.
