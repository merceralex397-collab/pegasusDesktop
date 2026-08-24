# Open questions — FND-024

These boxes are the gate. For profile `spike` an unticked `- [ ]` line **above** the
`## Parked` heading blocks `enter-done` — and only `enter-done`; it never gates
`leave-backlog`. Verified with `get_doc_gates` (no id): `spike` resolves to
`enter-done: [research, questions-resolved]` and nothing else.

Every box corresponds to a `NOT YET CAPTURED` block in the `research` document. No
measurement was taken to write these documents and **none may be invented**. Tick a
box only when the number is written into
`docs/desktop/10-security-observability-performance/README.md` § 2 **and** the command
that produced it is recorded beside it.

- [ ] **U-1 · the baseline workstation, named by the operator.** **Operator step, and
      the hard gate on every other box.** Hand back: CPU model, physical cores,
      installed RAM, storage type, GPU, Windows 11 edition and build, display resolution
      and scaling, and whether the machine is on the office LAN or VPN. Nothing may be
      treated as a pass/fail budget until this line exists
      (`docs/desktop/10-security-observability-performance/README.md` § 2 Assumptions).
      If the operator cannot name it yet, add one line to `docs/open-decisions.md` and
      tick this box against that line.
- [ ] **U-2 · prerequisites confirmed on that machine.**
      `pwsh ./scripts/Invoke-Doctor.ps1` — note it defaults to `-Profile Offline`
      (research F-4), which is the profile L-02 requires; do not pass `-Profile Cloud`.
      Unblocked by: every prerequisite present, plus the .NET SDK version, the LocalDB
      or SQL container in use, and the Azurite version, recorded verbatim.
- [ ] **U-3 · the stack up, and the mode stated.**
      `-Action Start`, `-Action Status` (expect manifest state `Running`),
      `-Action Smoke` (expect exit 0). **Say which mode produced the numbers:**
      `grep -rn "TestStack" scripts/` returns nothing today, so [[TEST-017]] (plan
      handle `DSK-08-17`) has not landed its `TestStack` mode and the run will be a
      plain `Start` unless it lands first.
- [ ] **U-4 · the data sizes, in tier-10 vocabulary, with the seeding method.**
      Cases, cases per stage, documents per case, retained mail rows, asset-metadata
      rows (`docs/engineering.md:72-89`). Numbers come from the seeded local database.
      **Never fabricate VRMs, names, addresses or emails**; `corpus/` is ignored and
      immutable (`.gitignore:1-2`).
- [ ] **U-5 · the p50/p95 table over ≥20 samples per workflow, release configuration
      only.** Cold first request, warm repeat, dashboard, case list page 1 and a deep
      page, case detail, inbox list, inbox message, ordinary save. A single reading is
      not a p95. Every number names its command.
- [ ] **U-6 · report generation, first render reported separately.** The renderer
      serialises behind `SemaphoreSlim(1,1)`
      (`src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:19`)
      and creates the browser lazily (`:93`), and one draft produces two PDFs (`:74`,
      `:75`). Unblocked by: first render apart from subsequent renders, and a statement
      of whether the figure is per draft or per PDF.
- [ ] **U-7 · process metrics captured with `dotnet-trace-collect` and
      `analyzing-dotnet-performance`.** Idle CPU, working set after start, working set
      after repeated navigation, sustained growth. Trace artefacts stay under an ignored
      local path and are referenced by name; commit no binaries.
- [ ] **U-8 · the two heaviest list surfaces timed at the page sizes the web app
      actually uses** — case list (`src/Pegasus.Web/Pages/Cases/Index.cshtml.cs`) and
      inbox list (`src/Pegasus.Web/Pages/Mail/Index.cshtml.cs`) — so area 06 can size the
      virtualized desktop lists against a real number rather than a guess.
- [ ] **U-9 · the critical business fixtures listed by path.** One line per fixture:
      path, what it exercises, which workflow it feeds. Sources are `reference/`
      (`EVA`, `eva_information`, `rendererref1`, `reports`,
      `workproviders-and-repairers`, `cazana-api-spec.json`) and a **named, not copied**
      `corpus/` cohort where one was used.
- [ ] **U-10 · the record written back and clean.**
      `docs/desktop/10-security-observability-performance/README.md` § 2 carries a dated
      "Baseline capture — web app" subsection with the workstation, data sizes,
      p50/p95 table, memory and CPU, and the exact commands; it states plainly that
      these are **web-app baselines, not desktop budgets**, and that [[PLAT-010]] (plan
      handle `DSK-10-10`) publishes the §15.1 budget table.
      `pwsh ./scripts/Test-DocumentationLinks.ps1` exits 0 and
      `git status --porcelain` shows no trace, dump or corpus artefact staged.
- [ ] **U-11 · the reviewer's re-run lands inside the recorded band.** The reviewer
      re-runs one timed workflow from the recorded command on the same machine and gets
      a result within the recorded p50–p95. A number nobody can reproduce from the
      recorded command is not evidence.

## Parked (explicitly deferred)

Everything below this heading is **not** counted by the gate.

- [ ] Whether to re-take the whole capture once [[TEST-017]] (plan handle `DSK-08-17`)
      lands the `TestStack` mode. Safe to defer: the ticket already permits either mode
      provided the record says which one produced the numbers, and a baseline taken in
      `Start` mode is still a valid comparison baseline for [[TEST-015]] (plan handle
      `DSK-08-15`) as long as that ticket runs in the same mode. Reopened if the two
      modes turn out to differ materially in stack composition.
- [ ] Whether the case-list and inbox page sizes should themselves be recorded as
      configuration facts in the parity matrix rather than only in the baseline record.
      Deferred because the matrix has no column for it and adding one is [[FND-014]]'s
      (plan handle `DSK-01-01`) skeleton work. Reopened if area 06 needs the page size
      as a design input rather than as a measurement note.

**Not an open question.** Whether to measure against a real Azure environment is
settled: L-02 and ADR-0014 mean no Azure dev/test/staging exists, and production
timings — if ever needed — come from the pilot ring under separate approval. Do not
raise it.
