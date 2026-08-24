# Research — FND-007: ADR-0108, isolated WebView2 report rendering

## Question

What must ADR-0108 decide now, what must it explicitly leave to the Phase 7
spike, and how does it stand as a written decision without either breaching the
proposal's "no WebView shell" constraint or contradicting ADR-0025 and ADR-0028,
which keep the integrated renderer running in the Web Container App?

## Current behaviour

Report rendering is server-side today. Measured 2026-08-24 from the working tree
at `origin/main` `191ddf3342…`, corroborated by flow record 6
(`docs/desktop/01-inventory-and-parity/flow-records.md:362-433`):

- **Entry point.** `Pages/Cases/Assessment/Index.cshtml.cs`
  `OnPostGenerateReportDraftAsync`.
- **Core contract.** `IAssessmentReportRenderer` in
  `src/Pegasus.Core/Reports/AssessmentReportRendering.cs` (**312 lines**), with
  the projection at `Reports/AssessmentReportProjection.cs` (362 lines).
- **Implementation.**
  `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs`
  (**326 lines**): `AssessmentReportSnapshot` → Scriban templates → HTML →
  Playwright Chromium `PdfAsync` → PDFsharp post-processing →
  `*_assessment.pdf` and `*_fee_note.pdf`. Serialised by a `SemaphoreSlim(1,1)`;
  the browser is lazily created and cached; `IAsyncDisposable`; registered as a
  singleton by `AddPegasusReportRendering()`
  (`src/Pegasus.Infrastructure/DependencyInjection.cs:446`).
- **Templates.** `ls docs/design/assets/report-renderer/templates/` returns
  **six `.scriban` files** — `advert_evidence_pack`, `assessment_fee_note`,
  `assessment_report`, `expert_report`, `fee_note`, `market_valuation_evidence`
  — plus `report.css`. (Flow record 6 at `:388-392` says "seven `.scriban`
  files"; measured, it is six plus the stylesheet. A small correction for
  [[FND-020]] (plan handle `DSK-01-07`), which owns that record — not for this
  ticket to edit.)
- **Tests.** `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs`
  and `AssessmentReportDraftWebTests.cs` — the baseline [[FEAT-041]] (plan handle
  `DSK-07-15`) reuses for golden-file fixtures.
- **Pins.** `Directory.Build.props` `PlaywrightVersion 1.61.0` matched to
  `Pegasus.Web.csproj` `ContainerBaseImage
  mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` (ADR-0028); the Container
  App runs cpu 1.0 / 2 Gi for in-process Chromium.

**No parity-matrix row covers ADR-0108, and none should.**
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds **46** rows
(`grep -c '^| PAR-'` → 46), each keyed to a Razor page model under
`src/Pegasus.Web/Pages/` (`parity-matrix.md:36-38`); one of those rows will
cover the assessment page that *triggers* rendering, but the rendering decision
itself is an ADR, not a screen. The closest existing repository mechanism this
ticket must not break is the ADR index (`docs/adr/README.md:16-41`) and the CI
`documentation` job (`.github/workflows/ci.yml:71-87`).

## Findings

- **The proposal both forbids and permits WebView2, and the permission is
  conditional on this ADR existing.** § 2.1 locked constraint at
  `Pegasus_Native_Desktop_Design_Proposal.md:60`: "It must not be a
  WebView/WebView2 shell around the current application." § 23.2 at `:1715`: "An
  isolated WebView2 use for a third-party login consent page or a specific
  document preview is not automatically a web wrapper, **but it requires an ADR
  and must not host Pegasus UI**." Without ADR-0108 on record, the first desktop
  renderer commit reads as a violation of a locked constraint.
- **§ 23.2's release gate is where the constraint is actually enforced**
  (`:1701-1713`): "no WebView renders the legacy Pegasus application", "no
  required workflow launches the legacy site". ADR-0108 must be written so a
  reviewer can check the renderer against those lines.
- **ADR-0025 and ADR-0028 are accepted and keep the gateway renderer running.**
  ADR-0028 at `:15-16` records "Accepted on 2026-08-19. This decision refines
  ADR-0015 and ADR-0025; it supersedes neither", and its `## Decision` puts the
  integrated renderer in process inside the existing Pegasus Web Container App.
  ADR-0108 must therefore state the retention explicitly or the two decisions
  look contradictory — and ADR-0028's own Status sentence is the exact
  precedent for how to word that.
- **The host is genuinely undecided, and the plan says so.**
  `docs/desktop/07-integrations/README.md:255` is the risk row: "a WinUI
  `WebView2` control needs a XAML root; a zero-size collapsed control **may**
  still initialise, but behaviour must be proven (DSK-07-14 spike);
  `CoreWebView2Controller` on a hidden HWND is the fallback host. Spike first;
  record the chosen host in ADR-0108; keep the renderer behind
  `IAssessmentReportRenderer` so the host can change." That is why the ADR
  merges `proposed`.
- **The `docs/adr/README.md` accepted table has no status column**
  (`:18-19`, `ADR | Title | Related FRD`), so a `proposed` ADR has no honest row
  there — adding one would assert it as current architecture. `AGENTS.md:114-117`
  describes a five-column index that would have had one; the real file
  contradicts it and **the file wins**. Correcting that sentence is [[FND-005]]'s
  (plan handle `DSK-00-05`), not this ticket's.
- **`docs/adr/0108-desktop-webview2-report-rendering.md` does not exist**
  (`ls docs/adr/0108*` → no such file) and **both claimants name that exact
  path**: this ticket's body, and [[FEAT-038]] (plan handle `DSK-07-12`) at its
  own steps 2, 3 and `## Guardrails`. [[FEAT-038]]'s Guardrails state the split
  in the same words this ticket's step 10 uses — [[FND-007]] authors and merges
  it `proposed` in Phase 0; [[FEAT-038]] performs the frontmatter-only
  acceptance flip and adds the index row in that same PR. There is no ownership
  question outstanding.
- **The house form to copy is ADR-0028/ADR-0029**, which open at `## Status`
  (`0028:13`, `0029:13`); the older ADR-0014/0015/0025 do not. Every
  `related_frd:` value in `docs/adr/*.md` is a lowercase stem — `[frd-11]`, not
  `[FRD-11]`.
- **Flow record 6 carries four open questions**, Q6.1–Q6.4 at `:414-426`:
  which templates are in desktop scope (upstream TICK-206), print-to-PDF fidelity
  differences between WebView2 and Playwright's `PdfAsync`, WebView2 runtime
  presence on the ten workstations, and PDFsharp behaviour on WebView2 output.
  Q6.2 and Q6.4 are exactly what the Phase 7 spike measures — they are what
  ADR-0108 defers, not what it must answer.
- **The Microsoft Learn references the plan already fetched**
  (`docs/desktop/07-integrations/README.md:112-114`): the WebView2 print-to-PDF
  how-to and the `CoreWebView2.PrintToPdfAsync` / `PrintToPdfStreamAsync` /
  `CoreWebView2PrintSettings` reference. Step 2 re-fetches them so the ADR's API
  claims carry a current URL and fetch date rather than a remembered one.
- **The Phase 0 placement of this ticket is deliberate.** Plan 00 § 4 Target
  state makes ADR-0100…ADR-0110 part of the Phase 0 governance exit gate and
  allows ADR-0108 to stand `proposed` until the Phase 7 spike — which is why the
  ticket is grouped `HZN-001` while its acceptance flip waits on [[FEAT-040]]
  (plan handle `DSK-07-14`) and [[FEAT-041]].

### Facts

| Fact | Source |
| --- | --- |
| "No WebView2 shell" is a locked constraint | proposal `:60` |
| Isolated WebView2 permitted, requires an ADR, must not host Pegasus UI | proposal `:1715` |
| § 23.2 release-gate lines | proposal `:1701-1713` |
| ADR-0028 refines ADR-0015/0025 and supersedes neither | `docs/adr/0028-*.md:15-16` |
| Integrated renderer runs in the Web Container App | `docs/adr/0028-*.md` § Decision |
| Core contract `IAssessmentReportRenderer`, 312 lines | `src/Pegasus.Core/Reports/AssessmentReportRendering.cs` |
| Playwright implementation, 326 lines, `SemaphoreSlim(1,1)` | `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs`; flow record 6 `:373-383` |
| Registered singleton | `src/Pegasus.Infrastructure/DependencyInjection.cs:446` |
| Six `.scriban` templates plus `report.css` | `ls docs/design/assets/report-renderer/templates/` |
| Golden-file baseline tests exist | `tests/Pegasus.IntegrationTests/Reports/` |
| Off-screen host undecided; spike first; record in ADR-0108 | `docs/desktop/07-integrations/README.md:255` |
| Accepted index table has no status column | `docs/adr/README.md:18-19` |
| No ADR-0108 file yet | `ls docs/adr/0108*` |
| Both claimants name the same path and the same split | this ticket's body; [[FEAT-038]] body steps 2–3 and Guardrails |
| Q6.1–Q6.4 | `flow-records.md:414-426` |
| Learn references already identified | `docs/desktop/07-integrations/README.md:112-114` |

### Assumptions

- **A-00-10 — a collapsed, zero-size WinUI `WebView2` control initialises well
  enough to print.** *Confirmed by:* the [[FEAT-040]] spike, which is the whole
  reason this ADR merges `proposed`. *Breaks if:* it does not, in which case the
  `CoreWebView2Controller`-on-a-hidden-HWND fallback becomes the recorded host —
  which is why the decision must be written so the host is a *recorded*
  parameter and the renderer stays behind `IAssessmentReportRenderer`.
- **A-00-11 — the WebView2 Evergreen runtime is present on every target
  workstation.** *Confirmed by:* Q6.3, which is [[FND-020]]'s to close.
  *Breaks if:* it is absent or pinned to an old version on some machines, which
  turns "runtime missing" from an edge case into the reversal condition step 7
  must name.
- **A-00-12 — golden-file parity within documented tolerances is achievable.**
  *Confirmed by:* [[FEAT-041]]'s fixture run. *Breaks if:* Q6.2 finds a fidelity
  gap that cannot be closed, in which case ADR-0108 never reaches `accepted` and
  the gateway renderer stays the path in use — an outcome the ADR must survive
  rather than a failure of this ticket.

## Execution placement

**This ticket places no responsibility anywhere: it authors one document.** The
one placement it assumes is that ADR-0108 lives in this repository under
`docs/adr/`. But the *decision* it records does move a responsibility, and the
six-question table is what the ADR body must carry — so these are the answers,
for report rendering, that step 4 writes into `## Context`:

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **no** for rendering; **yes** for the result | The render is a pure function of `AssessmentReportSnapshot` (`PlaywrightAssessmentReportRenderer.cs`); the *finalised PDF* is registered into custody centrally, which [[FEAT-042]] (plan handle `DSK-07-16`) owns |
| Unattended execution — must it run with every desktop closed? | **no** | Rendering today is triggered by an operator action, `OnPostGenerateReportDraftAsync`. Nothing renders on a timer. If a template later needs unattended generation (upstream DOCS-001), that path stays server-side — Q6.1 |
| Protected credentials — long-lived secret that must not sit on workstations? | **no** | The renderer consumes a snapshot plus templates and brand assets embedded from `docs/design/assets/report-renderer/templates/`; it holds no provider secret |
| Public callback — must an external service call a stable public endpoint? | **no** | Local HTML → local PDF; no external service is involved |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes**, for readiness and finality only | Report readiness, accepted inputs, immutable identity and hash, correction and approval "remain governed by FRD-11 and `Pegasus.Core`" (`docs/adr/0028-*.md` § Context). The desktop renders; it does not decide that a report may be produced |
| Measured operational advantage — measured evidence central is materially better? | **no** | The measured cost is the other way: Chromium startup on first render, one render at a time behind a `SemaphoreSlim(1,1)`, and a Container App sized cpu 1.0 / 2 Gi to carry in-process Chromium (flow record 6 § Failure modes; ADR-0028 § Context) |

The rendering *step* therefore belongs on the desktop; the *authority* over
whether a report may be produced and what counts as final stays central under
FRD-11. That split is the whole content of ADR-0108's decision, and it is why
the ADR relates ADR-0025 and ADR-0028 rather than superseding them.

## Implications

1. **Write the decision so it separates "decided now" from "recorded later".**
   Decided now: rendering moves to `Pegasus.Desktop.Infrastructure` behind
   `IAssessmentReportRenderer`, using the shared Scriban templates, in an
   isolated non-UI single-flight WebView2. Recorded later: which off-screen host
   (`docs/desktop/07-integrations/README.md:255`). An ADR that pretends the host
   is settled would be wrong on the day it merged.
2. **The retention clause must be a gate, not a sentiment.** "The gateway
   renderer stays until the golden-file parity tests of [[FEAT-041]] pass on
   approved fixtures, and no required report may depend on the web renderer
   after that unless amended by a superseding ADR" — phrased so
   `kanmer-review` can check it against a diff.
3. **Add no index row at this merge.** The accepted table would assert ADR-0108
   as current architecture. Discoverability while `proposed` comes from
   `docs/desktop/00-governance-and-workflow/README.md` § 3's ADR set table and
   from this ticket.
4. **Cite Learn with a fetch date.** The two API claims the ADR makes —
   `PrintToPdfStreamAsync` and `CoreWebView2Controller` hosting on a window
   handle — are the kind that go stale. Record URL and date in `## Links`.
5. **A `proposed` ADR is not settled authority.** Until acceptance the gateway
   renderer remains the path in use, and no other ticket may cite ADR-0108 as
   binding. Say so in `## Status`.

## Open questions

- **Q6.2 and Q6.4** (print-to-PDF fidelity against Playwright's `PdfAsync`;
  PDFsharp behaviour on WebView2 output) — deliberately *deferred into* the ADR
  rather than answered by it. They are what `status: proposed` means here.
- **Q6.3** (WebView2 runtime presence on the ten workstations) — owned by
  [[FND-020]]; feeds the reversal condition in step 7.
- **Q6.1** (which templates are in desktop scope, upstream TICK-206) — an
  upstream decision; write it `upstream TICK-206`, never bare, since a bare
  `TICK-<nnn>` would read as a fork board id.
- **Which off-screen host is used** — owned by [[FEAT-040]]'s spike, a scope
  boundary rather than an open question; the ADR is written to receive the
  answer.
- **Not open, and not to be reopened:** L-03 (report rendering moves to the
  desktop through an isolated non-UI WebView2 path, gateway renderer retained
  until parity); L-02 (parity evidence is produced on the local Test/UAT stack);
  the reserved ADR block (operator, 2026-08-23); and the ADR-0108 authorship
  split with [[FEAT-038]], which both bodies state identically.
