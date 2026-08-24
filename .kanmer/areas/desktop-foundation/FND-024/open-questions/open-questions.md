# Open questions — FND-024

These boxes are the gate. For profile `spike` an unticked `- [ ]` line **above** the
`## Parked` heading blocks `enter-done` — and only `enter-done`; it never gates
`leave-backlog`. Verified with `get_doc_gates` (no id): `spike` resolves to
`enter-done: [research, questions-resolved]` and to nothing else.

This document exists because the `research` document is a **pre-work scaffold**: it is
the spike's output, so its mere existence would otherwise make an unmeasured spike
closable. Before these boxes were written, `get_doc_gates FND-024` reported
`enter-done` **passable: true** with `research` satisfied and no measurement taken.
The banner in `research` is prose; these boxes are the gate.

Every box corresponds to a `NOT YET CAPTURED` block in `research`. **Not one of them
can be answered from the repository** — each needs a measurement taken on the named
baseline workstation, against the local production-mimicking stack, in a **Release**
build. Tick a box only when the raw output is attached to the ticket and the figure is
written into
`docs/desktop/10-security-observability-performance/README.md` § 2.

- [ ] **U-1 · the baseline workstation.** **Operator step, and the hard gate on every
      box below.** The operator names the lowest-spec supported office workstation and
      hands back CPU model, physical cores, installed RAM, storage type, GPU, Windows 11
      edition and build, display resolution and scaling, and whether the machine is on
      the office LAN or VPN. **Nothing may be treated as a pass/fail budget until this
      line exists** (`docs/desktop/10-security-observability-performance/README.md`
      § 2 Assumptions). No repository command can answer it. If the operator cannot name
      the machine yet, add one line to `docs/open-decisions.md` naming the missing
      decision and stop — this ticket's own § Documentation changes already authorises
      exactly that.
- [ ] **U-2 · prerequisites on that machine.** `pwsh ./scripts/Invoke-Doctor.ps1`
      (verified present, `scripts/Invoke-Doctor.ps1`). Record every prerequisite as
      reported, plus the .NET SDK version, the LocalDB or SQL container in use, and the
      Azurite version, verbatim.
- [ ] **U-3 · the stack brought up, and which mode produced the numbers.**
      `pwsh ./scripts/Invoke-LocalDevelopment.ps1 -Action Start`, then `-Action Status`
      (expect manifest state `Running`) and `-Action Smoke` (expect exit 0). **State
      which mode.** `TestStack` does not exist in that script today —
      `grep -c 'TestStack' scripts/Invoke-LocalDevelopment.ps1` → **0** — so if
      [[TEST-017]] (plan handle `DSK-08-17`) has landed it by execution, use it and say
      so; if it has not, say that too, because the measurement is then against the plain
      local stack rather than the production-mimicking one.
- [ ] **U-4 · the data sizes, in tier-10 vocabulary, and how the database was seeded.**
      Per `docs/engineering.md:72-89`: number of cases, cases per stage, documents per
      case, retained mail rows, asset-metadata rows. Numbers come from the seeded local
      database and the seeding method is stated. **Never fabricate VRMs, names,
      addresses or emails** (`AGENTS.md` § Safety rails).
- [ ] **U-5 · the p50/p95 table, Release configuration only, ≥20 samples per
      workflow.** Never a single reading. The workflows: cold first request after start;
      warm repeat request; dashboard (`src/Pegasus.Web/Pages/Index.cshtml.cs`); case list
      page 1 **and** a deep page (`Pages/Cases/Index.cshtml.cs`); case detail
      (`Pages/Cases/Details.cshtml.cs`); inbox list (`Pages/Mail/Index.cshtml.cs`); inbox
      message (`Pages/Mail/Message.cshtml.cs`); an ordinary save on the case workspace.
      Every number names the command that produced it.
- [ ] **U-6 · report generation, with the first render separated.** Time
      `OnPostGenerateReportDraftAsync` on
      `src/Pegasus.Web/Pages/Cases/Assessment/Index.cshtml.cs`. The first render carries
      the Chromium startup cost and must be reported apart from subsequent renders:
      `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:19`
      serialises every render behind `SemaphoreSlim(1, 1)` and the browser is created
      lazily and cached (`:93`). One draft produces **two** PDFs — the assessment report
      and the fee note (`:74`, `:75`) — through that one gate, so say whether the figure
      is per draft or per PDF.
- [ ] **U-7 · process metrics.** Idle CPU, working set after start, working set after
      repeated navigation, and any sustained growth, captured with the
      `dotnet-trace-collect` and `analyzing-dotnet-performance` skills. Save trace
      artefacts under an **ignored local path** and reference them by name; commit no
      binaries.
- [ ] **U-8 · the two heaviest list surfaces at the real page sizes.** Operator-facing
      latency for the case list (`Pages/Cases/Index.cshtml.cs`) and the inbox
      (`Pages/Mail/Index.cshtml.cs`) at the page sizes the web app **actually** uses, so
      area 06 can size the virtualized desktop lists against a real number rather than a
      guess.
- [ ] **U-9 · the critical business fixtures, by path.** One line per fixture: path,
      what it exercises, which workflow it feeds. Drawn from `reference/`, whose
      top-level contents are verified as `EVA`, `README.md`, `cazana-api-spec.json`,
      `eva_information`, `rendererref1`, `reports`, `workproviders-and-repairers` — plus
      a **named, not copied** reference to any `corpus/` cohort used. `corpus/` is
      ignored and immutable.
- [ ] **U-10 · the record written back.**
      `docs/desktop/10-security-observability-performance/README.md` § 2 carries a dated
      "Baseline capture — web app" subsection with the workstation line, the data sizes,
      the p50/p95 table, the memory and CPU figures and the exact commands; the record
      states plainly that these are **web-app baselines, not desktop budgets**, and that
      [[PLAT-010]] (plan handle `DSK-10-10`) owns publishing the §15.1 budget table;
      `pwsh ./scripts/Test-DocumentationLinks.ps1` exits 0; and `git status --porcelain`
      shows **no** trace, dump or corpus artefact staged.
- [ ] **U-11 · the reviewer's re-run.** The reviewer re-runs one timed workflow from
      the recorded command, on the same machine, and the result falls inside the
      recorded p50–p95 band. A recorded number nobody can reproduce from the recorded
      command is not evidence.

## Parked (explicitly deferred)

Everything below this heading is **not** counted by the gate.

- [ ] Whether the baseline capture should be repeated after the first upstream sync
      ([[FND-023]], plan handle `DSK-01-10`) changes the web application under
      measurement. **Default taken:** no — this ticket records a dated baseline at the
      head it ran on, and a later re-capture is a new ticket, not a rewrite of this one.
      Reopened if the sync changes a timed page model, which
      `docs/desktop/01-inventory-and-parity/README.md` § 7 already flags as the general
      staleness risk for Phase 0 records.
- [ ] Whether the p50/p95 sample count should rise above 20 for the report-generation
      figure, whose first render is a single expensive event by construction. **Default
      taken:** keep ≥20 for every warm figure and report the first render as a single
      recorded value with its own line, since averaging one-off startup costs hides
      them. Reopened if the reviewer wants a distribution over repeated cold starts.

## Not open questions — settled, and not to be re-raised

- **Measuring against a real Azure environment.** Settled by **L-02** and **ADR-0014**:
  there is no Azure dev/test/staging environment, and production timings — if ever
  needed — come from the pilot ring under separate approval. Requesting an Azure test
  resource or load-testing production is out of bounds, not an open question.
- **Setting a release latency threshold here.** Tier 10 forbids inventing one. This
  ticket records observations; [[PLAT-010]] (plan handle `DSK-10-10`) publishes the
  budget table.
