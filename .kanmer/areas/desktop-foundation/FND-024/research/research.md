# Research — FND-024: baseline performance and critical business fixtures

> **STATUS — INCOMPLETE. Do not move this ticket to Done yet.**

This document is the spike's **output**. `get_doc_gates FND-024` resolves profile
`spike` to one gated boundary — `enter-done` needs `research` and
`questions-resolved` — so its existence is what would let the ticket close. It is a
pre-work scaffold: everything under **Facts** was verified by a read-only command
quoted beside it, and **every measured number this ticket owes is a literal
`NOT YET CAPTURED` block**, because no measurement was taken and none may be invented.
`open-questions` carries one unticked `- [ ]` box per uncaptured item.

## Question

What does the **web application** actually cost on the lowest-spec supported office
workstation, running against the local production-mimicking stack — cold and warm page
timings, list paging, an ordinary save, report generation, CPU and memory — and which
files under `reference/` are the critical business fixtures? Proposal §15.1 gives
provisional desktop budgets and then says "Baseline hardware and data sizes must be
recorded. Adjustments require evidence, not convenience." Without this capture, "the
native app feels faster" is an opinion and a release candidate can regress with
nothing able to prove it.

## Current behaviour

The six surfaces to time are all Razor page models, and each has a parity row:

| Surface | Page model (`path`, measured lines) | Parity row |
| --- | --- | --- |
| Dashboard | `src/Pegasus.Web/Pages/Index.cshtml.cs` (43) — `OnGetAsync` | `PAR-05` |
| Case list | `src/Pegasus.Web/Pages/Cases/Index.cshtml.cs` (261) — `OnGetAsync` | `PAR-07` |
| Case workspace | `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` (654) — `OnGetAsync`, `OnPostSaveAsync` | `PAR-08` |
| Inbox list | `src/Pegasus.Web/Pages/Mail/Index.cshtml.cs` (428) — `OnGetAsync`, `OnGetPreviewAsync` | `PAR-21` |
| Inbox message | `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs` (1,025) — `OnGetAsync` and seven `OnPost*` handlers | `PAR-22` |
| Report draft | `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs` (740) — `OnPostGenerateReportDraftAsync` | `PAR-15` |

Line counts are measured (`wc -l`), not copied. The matrix holds `PAR-01`…`PAR-46`
(`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46`), so
every surface this ticket times is already inventoried; the ticket adds the *cost* of
each, which the matrix has no column for and does not need one.

The stack the measurement runs against is `scripts/Invoke-LocalDevelopment.ps1`
(`-Action Start|Status|Smoke|Stop|Reset`) with `scripts/Invoke-Doctor.ps1` as the
prerequisite report, documented at `docs/runbook.md:581` § Local setup and run and
`:619` § Status and smoke.

## Findings

- The declared dependency has **not** landed: `grep -rn "TestStack" scripts/` returns
  nothing, so `Invoke-LocalDevelopment.ps1` has no `TestStack` mode yet (F-2). The
  ticket already anticipates this — "If `DSK-08-17`'s `TestStack` mode has landed, use
  it instead and say which mode produced the numbers" — so the measurement is runnable
  today in `Start`/`Status`/`Smoke` mode, and the mode used must be stated.
- Report generation has a structural cost the measurement must separate: the renderer
  serialises every render behind one gate and creates the browser lazily (F-3). A
  single averaged "report generation" number would hide both.
- `Invoke-Doctor.ps1` takes a `-Profile` of `Offline` or `Cloud` and defaults to
  `Offline` (F-4), so the bare invocation in the ticket's Verification block runs the
  offline profile — which is the correct one, and worth saying so the implementer does
  not reach for `-Profile Cloud`.
- The whole measurement is blocked on one thing nobody in this repository can supply:
  the operator naming the baseline workstation (U-1). Nothing may be treated as a
  pass/fail budget until that line exists.

### Facts

Each fact carries the command that produced it. Run in
`C:\Users\PC\Documents\GitHub\pegasusDesktop` on 2026-08-24 at `bbd1c549`.

- **F-1 — the local stack script has five actions and defaults to `Status`.**
  `sed -n '1,15p' scripts/Invoke-LocalDevelopment.ps1` →
  `[ValidateSet('Start', 'Status', 'Smoke', 'Stop', 'Reset')]` at `:3`,
  `[string]$Action = 'Status'` at `:4`, `[int]$StartupTimeoutSeconds = 120` at `:12`.
  The 120-second startup timeout is itself a number the cold-launch measurement must
  not collide with.
- **F-2 — the `TestStack` mode does not exist yet.**
  `grep -rn "TestStack" scripts/` returns **no output**. [[TEST-017]] (plan handle
  `DSK-08-17`) owns building it. The ticket's stated dependency is therefore unmet
  today, and `list_items` reports this ticket `blocked: true`.
- **F-3 — report rendering is serialised behind one gate with a lazily created,
  cached browser.**
  `grep -n "SemaphoreSlim\|LaunchAsync\|PdfAsync" src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs`
  → `:19 private readonly SemaphoreSlim gate = new(1, 1);`,
  `:93 browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true })`,
  `:120 return await page.PdfAsync(new PagePdfOptions`. A single draft produces **two**
  PDFs — `:74` `assessment_report.scriban` and `:75` `assessment_fee_note.scriban` —
  so "one report generation" is two renders through one gate. First render carries the
  Chromium startup cost; subsequent renders do not. Report them separately, as the
  ticket requires.
- **F-4 — `Invoke-Doctor.ps1` defaults to the offline profile.**
  `sed -n '1,12p' scripts/Invoke-Doctor.ps1` →
  `[ValidateSet('Offline', 'Cloud')] [string]$Profile = 'Offline'`. The Verification
  block's bare `pwsh ./scripts/Invoke-Doctor.ps1` therefore runs `Offline`, which is
  the profile L-02 requires. Do not pass `-Profile Cloud`.
- **F-5 — the tier-10 capacity envelope is exactly what
  `docs/engineering.md:72-89` states.** Tier 10 reads: "eight concurrent operators,
  2,000 cases per month, 2–20+ files per case, the one-file 10 MiB limit and 10
  MiB-plus-64-KiB multipart envelope, burst/soak behavior, and 48,000–480,000+ annual
  asset-metadata shapes. **Do not invent a release latency threshold without an
  explicit decision.**" That last sentence is why this ticket records observations and
  sets no threshold.
- **F-6 — the fixture sources exist and are exactly seven entries.**
  `ls reference/` → `EVA`, `README.md`, `cazana-api-spec.json`, `eva_information`,
  `rendererref1`, `reports`, `workproviders-and-repairers`. `corpus/` is ignored and
  immutable: `.gitignore:1-2` reads "# Genuine local evaluation corpus. Never commit
  operational emails or case files." then `/corpus/`. Fixtures come from `reference/`
  by path, or from a **named** `corpus/` cohort — never copied into the repository, and
  never fabricated.
- **F-7 — the six surfaces and their measured sizes.** `wc -l` over the six page
  models gives 43, 261, 654, 428, 1,025 and 740 lines respectively (table above). The
  two heaviest list surfaces the ticket singles out for area 06 are the case list
  (`Cases/Index.cshtml.cs`, 261) and the inbox list (`Mail/Index.cshtml.cs`, 428).
- **F-8 — the published baseline table has a different owner.** `search_items` for
  `DSK-10-10` resolves it to [[PLAT-010]] — "Performance baseline: record the
  lowest-spec workstation, the data sizes, the web timings and the budget table",
  area `platform-operations`, profile `chore`, and this ticket's `blocks` list names
  it. So this ticket produces the numbers and [[PLAT-010]] publishes the §15.1 budget
  table from them. The desktop re-run of the same workflows is [[TEST-015]] (plan
  handle `DSK-08-15`).

### Assumptions

- **A-01-13 — the local production-mimicking stack is representative enough for a
  comparison baseline.** Confirmed by recording the data sizes in tier-10 vocabulary
  and stating the seeding method, so a later reader can judge representativeness for
  themselves. Breaks if the local database is seeded far below the tier-10 envelope —
  in which case the numbers are still a baseline, but the record must say what they are
  a baseline *of*.
- **A-01-14 — release-configuration builds are available on the baseline
  workstation.** Proposal §15.3 requires release builds and production-like data.
  Confirmed by `pwsh ./scripts/Invoke-Doctor.ps1` reporting every prerequisite present.
  Breaks the whole capture if the workstation cannot build Release: a debug-build
  number is worse than no number, because it will be quoted later as if it were real.
- **A-01-15 — twenty samples per workflow is enough for a stable p95.** Taken as the
  ticket's own floor ("at least 20 samples"). Confirmed by the spread: if p95 and p50
  are far apart on twenty samples, take more and say how many. Breaks quietly if a
  single reading is reported as a p95.
- **A-01-16 — no Azure measurement is needed or permitted.** L-02 and ADR-0014 mean
  there is no Azure dev/test/staging environment, so a load test cannot be run there;
  production timings, if ever needed, come from the pilot ring under separate approval.
  Confirmed by ADR-0014 standing. This is a hard boundary, not an assumption to test.

## Execution placement

This ticket produces measurements and places no product responsibility. The six-question
test is answered once, for the **responsibility of taking the baseline measurement**,
because "should this be measured in the cloud?" is a real placement question that L-02
and ADR-0014 have already answered and that this section makes checkable.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority | **No** | One run, one recorder, one written record; no two actors update the same state. |
| Unattended execution | **No** | The capture is operator-supervised on a named workstation, once. Nothing needs to run with every desktop closed. |
| Protected credentials | **No** | The stack is local: `scripts/Invoke-LocalDevelopment.ps1` (F-1) with Azurite, LocalDB or a SQL container, and the DevelopmentOffline replay adapters. No production secret and no provider key is involved. |
| Public callback | **No** | Nothing external calls in. |
| Central enforcement | **No** | The measurement enforces nothing; it observes. |
| Measured operational advantage | **No** | And this row deserves the honesty it asks for: this is the ticket that *creates* the programme's first measurements, so it has none of its own with which to argue that a central placement is materially better. |

**All six "no" — the responsibility belongs on the workstation**, which is exactly what
L-02 and ADR-0014 already require. Asking for an Azure test environment, or load-testing
production, is out of bounds. The one placement this ticket assumes is that the
production-mimicking stack stays local.

## NOT YET CAPTURED

No measurement was taken to write this document, and none may be invented. Each block
names what must produce the number and the question it answers. Each has a matching
unticked box in `open-questions`.

### NOT YET CAPTURED — U-1 · the baseline workstation

```
<Operator step — no repository command can answer this.>
```

**Operator step, and the hard gate on everything below.** The operator identifies the
lowest-spec supported office workstation and hands back: CPU model, physical cores,
installed RAM, storage type, GPU, Windows 11 edition and build, display resolution and
scaling, and whether the machine is on the office LAN or VPN. **Nothing may be treated
as a pass/fail budget until this line exists**
(`docs/desktop/10-security-observability-performance/README.md` § 2 Assumptions). If
the operator cannot name it yet, add one line to `docs/open-decisions.md`.

### NOT YET CAPTURED — U-2 · prerequisites on that machine

```
pwsh ./scripts/Invoke-Doctor.ps1          # defaults to -Profile Offline (F-4)
```

Must answer: every prerequisite reported present, plus the .NET SDK version, the
LocalDB or SQL container in use, and the Azurite version, recorded verbatim.

### NOT YET CAPTURED — U-3 · the stack brought up, and which mode

```
pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start
pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Status
pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Smoke
```

Must answer: manifest state `Running`, smoke check passing, and **which mode produced
the numbers**. `TestStack` does not exist today (F-2); if [[TEST-017]] (plan handle
`DSK-08-17`) has landed it by execution, use it and say so.

### NOT YET CAPTURED — U-4 · the data sizes, in tier-10 vocabulary

```
<queries against the seeded local database>
```

Must answer, per `docs/engineering.md:72-89`: number of cases, cases per stage,
documents per case, retained mail rows, asset-metadata rows — **and how the database
was seeded**. Numbers come from the seeded local database. Never fabricate VRMs,
names, addresses or emails (`AGENTS.md` § Safety rails; F-6).

### NOT YET CAPTURED — U-5 · the p50/p95 table, release configuration only

```
<measurement script, Release build, ≥20 samples per workflow>
```

Must answer, per workflow, **p50 and p95 over at least 20 samples** — never a single
reading: cold first request after start, warm repeat request, dashboard
(`Pages/Index.cshtml.cs`), case list page 1 **and** a deep page
(`Cases/Index.cshtml.cs`), case detail (`Cases/Details.cshtml.cs`), inbox list
(`Mail/Index.cshtml.cs`), inbox message (`Mail/Message.cshtml.cs`), and an ordinary
save on the case workspace (`Cases/Details.cshtml.cs` `OnPostSaveAsync`). Every number
names the command that produced it.

### NOT YET CAPTURED — U-6 · report generation, first render separated

```
<time OnPostGenerateReportDraftAsync on src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs>
```

Must answer: the **first** render (carrying the Chromium startup cost) reported apart
from subsequent renders, because
`src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:19`
serialises every render behind `SemaphoreSlim(1,1)` and `:93` creates and caches the
browser lazily. Note that one draft produces two PDFs (`:74`, `:75`) through that one
gate — say whether the figure is per draft or per PDF.

### NOT YET CAPTURED — U-7 · process metrics

```
dotnet-trace collect ...        (via the dotnet-trace-collect skill)
<analyzing-dotnet-performance skill over the trace>
```

Must answer: idle CPU, working set after start, working set after repeated navigation,
and any sustained growth. Save trace artefacts under an ignored local path and
reference them **by name**; commit no binaries.

### NOT YET CAPTURED — U-8 · the two heaviest list surfaces at the real page sizes

```
<time Cases/Index.cshtml.cs and Mail/Index.cshtml.cs at the page sizes the web app uses>
```

Must answer: operator-facing latency for the case list and the inbox at the page sizes
actually configured, so area 06 can size the virtualized desktop lists against a real
number rather than a guess.

### NOT YET CAPTURED — U-9 · the critical business fixtures

```
ls -R reference/
```

Must answer: one line per fixture — path, what it exercises, which workflow it feeds —
drawn from `reference/` (`EVA`, `eva_information`, `rendererref1`, `reports`,
`workproviders-and-repairers`, `cazana-api-spec.json`; F-6), plus a **named, not
copied** reference to any `corpus/` cohort used. `corpus/` is ignored and immutable.

### NOT YET CAPTURED — U-10 · the record written back

```
pwsh ./scripts/Test-DocumentationLinks.ps1
git status --porcelain
```

Must answer: that
`docs/desktop/10-security-observability-performance/README.md` § 2 carries a dated
"Baseline capture — web app" subsection with the workstation line, the data sizes, the
p50/p95 table, the memory and CPU figures and the exact commands; that the record says
plainly these are **web-app baselines, not desktop budgets**, and that [[PLAT-010]]
(plan handle `DSK-10-10`) owns publishing the §15.1 budget table; that the link check
exits 0; and that `git status --porcelain` shows **no** trace, dump or corpus artefact
staged.

### NOT YET CAPTURED — U-11 · the reviewer's re-run

```
<reviewer re-runs one timed workflow from the recorded command, on the same machine>
```

Must answer: that the result falls inside the recorded p50–p95 band. A recorded number
nobody can reproduce from the recorded command is not evidence.

## Implications

1. **U-1 gates everything.** Without the named workstation the numbers are
   uninterpretable, and proposal §15.1's budgets have nothing to be judged against.
   Get the operator line first, or park the ticket with a `docs/open-decisions.md`
   entry.
2. **Report generation must be split** (F-3). One averaged number hides both the
   Chromium startup cost and the serialisation gate, and both are exactly what the
   desktop's isolated WebView2 path (L-03, ADR-0108) will change.
3. **Release builds only** (proposal §15.3, A-01-14). A debug number will be quoted
   later as if it were real.
4. **State the stack mode** (F-2). `TestStack` does not exist today; if the numbers
   come from `-Action Start` rather than a `TestStack` run, say so, because
   [[TEST-015]] (plan handle `DSK-08-15`) will compare against them.
5. **Record observations, set no threshold.** `docs/engineering.md` tier 10 forbids
   inventing a release latency threshold without an explicit decision (F-5). This
   ticket does not set budgets; [[PLAT-010]] (plan handle `DSK-10-10`) publishes them.
6. **Change no application code to make a number better**, add no benchmark project,
   and commit no trace, dump or corpus artefact. The editable files are
   `docs/desktop/10-security-observability-performance/README.md` and
   `docs/open-decisions.md`.
7. **Telemetry is not a substitute.** App Insights is capped at 0.1 GB/day (upstream
   `PLAT-034`), so an empty query proves nothing; and a local full-privilege run proves
   nothing about deployed permissions (upstream `PLAT-035`, carried here by
   [[PLAT-018]] (plan handle `DSK-10-18`)) — do not present these numbers as production
   evidence.
8. **This ticket blocks [[PLAT-010]].** A partial capture stalls the published budget
   table; complete the table rather than sampling a few surfaces.

## Open questions

The eleven uncaptured items are this spike's subject and are tracked as boxes in
`open-questions`. One thing that looks like an open question is not:

- Whether to measure against a real Azure environment. **Settled**: L-02 and ADR-0014
  mean there is no Azure dev/test/staging environment, and production timings — if ever
  needed — come from the pilot ring under separate approval. Not an open question, and
  not to be raised as one.
