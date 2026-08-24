# Research — FND-007: ADR-0108, the one sanctioned WebView2 in the conversion

## Scope correction — 2026-08-25

FND-007 is complete once its own Phase 0 proposed-ADR PR is merged and verified. The Phase 7 evidence below explains why ADR-0108 is proposed; it is owned by [[FEAT-040]] and [[FEAT-041]] and later consumed by [[FEAT-038]], not by FND-007 closeout.

## 2026-08-25 correction — documented invisible host

Microsoft Learn documents `HWND_MESSAGE` as the valid parent for an invisible `CoreWebView2Controller` on Windows 8 and later; the WebView will never become visible. The fixed design is `CoreWebView2Environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE)`. This supersedes every earlier collapsed-XAML/hidden-HWND host-selection instruction below. Phase 7 validates packaged-app initialisation, PDF output and no-window behaviour; it does not select a host. This user-directed correction also adds `docs/desktop/00-governance-and-workflow/README.md` and `docs/desktop/07-integrations/README.md` to FND-007's docs-only scope.


## Question

What must ADR-0108 assert so that a WebView2 in a "no WebView shell" programme
is lawful rather than a breach; what is verifiably true today about report
rendering; why must the ADR merge `proposed` rather than `accepted`; and what
does the index gate mean for a `proposed` ADR?

## Current behaviour

Report rendering today is a **gateway** capability, and the parity matrix does
cover it.

- **The parity row is `PAR-15`** (`docs/desktop/01-inventory-and-parity/parity-matrix.md:60`),
  "13.9 Assessment and reporting", owner FRD-11 and FRD-06, entry point
  `Cases/Assessment/Index.cshtml.cs` (740 lines) with
  `OnPostGenerateReportDraftAsync` among its handlers. The row already records
  "report draft via `IAssessmentReportRenderer` (Playwright)" and, in its target
  column, "Assessment tab + report preview/finalise (Phase 7; rendering local via
  WebView2 per L-03)". Its recorded test evidence is
  `tests/Pegasus.IntegrationTests/Reports/AssessmentReportDraftWebTests.cs` and
  `AssessmentReportRendererTests.cs` — both files exist. Status: `inventoried`.
  (The matrix holds `PAR-01`…`PAR-46`; `grep -c '^| PAR-' …` → **46**,
  2026-08-24.)
- **The port** is `IAssessmentReportRenderer`,
  `src/Pegasus.Core/Reports/AssessmentReportRendering.cs:284`, consumed by the
  use case `GenerateAssessmentReportDraft` at `:291`.
- **The one implementation** is
  `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs:13`
  (326 lines), registered as a singleton at
  `src/Pegasus.Infrastructure/DependencyInjection.cs:448`. It uses `Scriban`
  (`:8-9`), drives Chromium through `Microsoft.Playwright` (`:5`, `Playwright.CreateAsync()`
  at `:92`), post-processes with `PdfSharp.Pdf.IO` (`:7`), and stamps a producer
  string naming the Playwright assembly version and Chromium at `:140`.
- **Where it runs is already an accepted decision.** ADR-0028 (`:39-45`) puts the
  integrated renderer in process inside the existing Pegasus Web Container App,
  and ADR-0025 requires it to be integrated behind a `Pegasus.Core`-owned port
  rather than deployed as a separate unit. Neither is superseded by ADR-0108;
  ADR-0108 relates to them and adds a second implementation of the same port on
  a different host.

## Findings

### Facts

Read on **2026-08-24** at `origin/main` `191ddf3342…`, each with its source.

- **F1 — the proposal's own exception is the whole legal basis, and it is one
  sentence.** `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md:1715`
  (§ 23.2 Native verification, heading at `:1701`): "An isolated WebView2 use for
  a third-party login consent page or a specific document preview is not
  automatically a web wrapper, but it **requires an ADR and must not host Pegasus
  UI**." The locked constraint it excepts is at `:1351` — "no WebView shell" —
  inside § 2.1 Locked constraints (heading at `:54`). ADR-0108 must quote both,
  or the first renderer commit reads as a violation of a locked constraint.
- **F2 — Microsoft Learn fixes the invisible host.**
  `docs/desktop/07-integrations/README.md:120-125` records the direct Learn
  reference: `HWND_MESSAGE` is a valid `ParentWindow` for an invisible
  `CoreWebView2Controller` on Windows 8+; it will never become visible. Phase 7
  validates that controller in the packaged app, not a collapsed-XAML alternative.
  `status: proposed` remains honest because packaged integration and parity are
  still future evidence.
- **F3 — the work breakdown assigns the acceptance flip to another ticket.**
  `docs/desktop/07-integrations/README.md:227` — row `DSK-07-12`, profile
  `chore`, "ADR-0108 isolated WebView2 HTML→PDF rendering (scope, never-UI rule,
  fallback, parity gate)", acceptance "**Accepted ADR** with the §23.2 statement
  and reversal condition". `DSK-07-14` (`:229`) is the renderer plus packaged-controller validation; `DSK-07-15` (`:230`) is the golden-file parity suite.
  Board ids, resolved with `search_items` (read, never computed):
  `DSK-07-12` → [[FEAT-038]], `DSK-07-13` → [[FEAT-039]], `DSK-07-14` →
  [[FEAT-040]], `DSK-07-15` → [[FEAT-041]].
- **F4 — the index has no status column, so a `proposed` row would be a false
  claim.** `docs/adr/README.md:16` is
  `## Current architecture decisions (`status: accepted`)` and `:18` is
  `| ADR | Title | Related FRD |` — three cells. `:11-12` states plainly that
  "the **current architecture is the set below with `status: accepted`**". A row
  for a `proposed` ADR would assert it as current architecture. The separate
  `## Superseded and relocated` table at `:43-52` is not a home for it either.
  The body's step 8 is therefore correct and is not merely stylistic.
- **F5 — `AGENTS.md:114-116` describes an index this repository does not have**
  (`ID | Title | Status | Superseded-by | Owner capability`), which would appear
  to give a `proposed` ADR a row. The file wins; `grep -n 'Owner capability'
  AGENTS.md` → exactly one match, at `:115`. **Correcting that sentence belongs
  to [[FND-005]]** (plan handle `DSK-00-05`) and must not be done here.
- **F6 — no ADR-01xx file exists yet.** `ls docs/adr/010*` →
  `No such file or directory`; the tree holds `0001`…`0029` with `0017` never
  issued (`docs/adr/README.md:57-58`). ADR-0108 is the only number in the
  reserved block this ticket touches, and it has **no** co-claimant that also
  *authors* it — [[FEAT-038]] only flips its frontmatter later.
- **F7 — the house frontmatter form for `related_frd` is a lowercase file stem.**
  `grep -h '^related_frd:' docs/adr/*.md | sort | uniq -c` over all 28 ADRs
  returns only `[]` and lowercase stems — `[frd-11]`, `[frd-10, frd-11]`,
  `[frd-02, frd-05, frd-11]` and so on. `grep -l '^related_frd: \[FRD'
  docs/adr/*.md` returns **no file**. The ticket body's step 3 writes
  `related_frd: [FRD-11]`; see *Open questions*.
- **F8 — the newest ADR opens with `## Status`, the older ones do not.**
  `docs/adr/0029-image-initiated-case-projection.md` has
  `## Status` `:13`, `## Context` `:19`, `## Decision` `:27`, `## Consequences`
  `:45`, `## Links` `:54`. `docs/adr/0015-…` has no `## Status` at all
  (`## Context` `:16`, `## Decision` `:28`, `## Consequences` `:53`).
  `AGENTS.md:107-110` requires Status first "so a body-only read is never
  mistaken for current when it is superseded" — which matters more for a
  `proposed` ADR than for any other. ADR-0028 shows the form to copy: a
  `## Status` section that states the date and the relation ("This decision
  refines ADR-0015 and ADR-0025; it supersedes neither", `:14-16`).
- **F9 — the templates are already a single governed source.**
  `docs/design/assets/report-renderer/templates/` holds seven files —
  `assessment_report.scriban`, `assessment_fee_note.scriban`,
  `expert_report.scriban`, `fee_note.scriban`, `advert_evidence_pack.scriban`,
  `market_valuation_evidence.scriban`, `report.css`. ADR-0025's Context (`:30-36`)
  records that CollisionRenderer "already embeds the canonical design assets from
  this repository's design tree" and that "its templates are Pegasus product
  behaviour and must co-version with the FRDs and Core policy that feed them".
  The desktop renderer must consume that same source — [[FEAT-039]]
  (`DSK-07-13`) does the embedding, hash-checked.
- **F10 — the golden-file baseline exists and is Playwright-pinned.**
  `tests/Pegasus.IntegrationTests/Reports/` holds
  `AssessmentReportDraftWebTests.cs` and `AssessmentReportRendererTests.cs`.
  `docs/desktop/07-integrations/README.md:258` records the drift risk that makes
  the parity gate tolerant rather than exact: "WebView2 runtime updates itself;
  Playwright is pinned to 1.61.0", mitigated by "Tolerant comparisons (text,
  values, page count, positions within tolerance)… **not pixel equality**".
- **F11 — the runtime-absence failure mode is already specified.**
  `docs/desktop/07-integrations/README.md:257`: "WebView2 runtime missing or
  outdated on a workstation → Startup check (04) with a named install step;
  **gateway render fallback** until fixed", and `:229`'s acceptance requires
  "runtime-missing → named failure and gateway fallback". This is the reason the
  retention clause is a real mechanism and not a courtesy.
- **F12 — this ticket is Phase 0 work despite depending on Phase 7.** It carries
  `HZN-001` and plan 00 § 4 Target state makes ADR-0100…ADR-0110 part of the
  Phase 0 governance exit gate, explicitly allowing "ADR-0108 may be `proposed`
  until Phase 7 packaged-controller validation and parity". No FND-007 step waits on Phase 7; later acceptance is solely [[FEAT-038]]'s work.

### Assumptions

- **A-00-7-1 — the documented `HWND_MESSAGE` controller will produce a PDF in Pegasus.**
  The host itself is supported by Microsoft Learn; the product integration remains
  unverified because no desktop project exists yet. [[FEAT-040]] validates
  initialization, PDF output and no-window behaviour from the packaged app. If it
  fails, the gateway remains and ADR-0108's reversal condition fires; no second host
  is trialled.
- **A-00-7-2 — `PrintToPdfStreamAsync` is the right API and it is current.**
  `docs/desktop/07-integrations/README.md:112-115` cites the Microsoft Learn
  print how-to and the `CoreWebView2` WinRT reference, and `:229` names
  `PrintToPdfStreamAsync`. *Confirmed by:* step 2's `microsoft_docs_search` /
  `microsoft_docs_fetch`, with the URLs and fetch date recorded in `## Links`.
  *Breaks if:* the API is renamed, deprecated, or unavailable in the pinned
  Windows App SDK — the ADR would then name the wrong mechanism, and a published
  body is immutable.
- **A-00-7-3 — [[FEAT-038]] performs the acceptance flip, not this ticket.** This is a successor ownership boundary: after the packaged-controller and parity evidence exists, FEAT-038 updates only ADR-0108 frontmatter and the index. Its absence does not block FND-007 proof or closeout.
- **A-00-7-4 — the WebView2 runtime is present on every target Windows 11
  workstation.** `docs/desktop/07-integrations/README.md:125` records this as an
  assumption, not a fact. *Confirmed by:* the area 04 startup check.
  *Breaks if:* a fleet machine lacks it — mitigated in the ADR by the named
  failure and gateway fallback (F11), which is why that clause must be in the
  Decision rather than left to the implementation.
- **A-00-7-5 — golden-file parity is achievable within tolerance** between a
  self-updating WebView2 Chromium and a pinned Playwright Chromium (F10).
  *Confirmed by:* [[FEAT-041]]'s suite passing on approved fixtures.
  *Breaks if:* divergence cannot be closed — which is exactly the reversal
  condition step 7 must write down in advance.

## Execution placement

This is the ticket's own six-question answer for the responsibility ADR-0108
places: **producing the rendered report document**.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **no** | A render is a pure transformation of one approved snapshot into one document. The shared, authoritative artefact is the *stored* report record and its Box custody, which stays behind the gateway ([[FEAT-042]], `DSK-07-16`, "Final document stored once; regeneration audited") |
| Unattended execution — must it run with every desktop closed? | **no** | Rendering is initiated by an operator finalising an assessment; `OnPostGenerateReportDraftAsync` in `Cases/Assessment/Index.cshtml.cs` is a request handler today, not a timer. No scheduled render exists |
| Protected credentials — long-lived secret that must not sit on workstations? | **no** | The renderer needs the case snapshot and the Scriban templates; the templates are governed repository assets (F9) shipped inside the package, not secrets. Box custody credentials stay behind the gateway under ADR-0107 |
| Public callback — must an external service call a stable public endpoint? | **no** | Nothing external calls the renderer |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes — on the gateway** | FRD-11's report readiness, immutable identity and hash, correction, approval and finality rules are `Pegasus.Core` policy, and ADR-0028 `:33-36` states plainly that those "remain governed by FRD-11 and `Pegasus.Core` rather than by this ADR". The desktop may **produce** the bytes; only the gateway may **register** a final report. That split is what keeps this a rendering decision rather than an authority decision |
| Measured operational advantage — measured evidence central is materially better? | **no** | Rather the reverse: ADR-0028 `:22-27` records that central rendering forced a pinned Chromium build, native Linux dependencies, fonts and writable temporary space into the Web container image. Moving the render to a machine that already has a Chromium engine removes that from the deployment unit. But no measurement has been taken yet — record it as "no, and not yet measured", not as a claim |

**Conclusion: the render belongs on the desktop; the report record does not.**
One "yes", and it names the gateway. The ADR must say both halves — an ADR that
says only "rendering moves to the desktop" invites a later ticket to let the
desktop register the report too.

**Nothing here is placed in Azure by this ticket, and nothing is removed from
it.** ADR-0025 and ADR-0028 keep the gateway renderer in the Web Container App
until the parity gate passes; ADR-0108 adds a second implementation of an
existing port and deprovisions nothing.

## Implications

- **Write `status: proposed` and mean it.** The host is documented, but packaged
  integration and parity are not yet evidenced. The ADR body names `HWND_MESSAGE`
  now; the later acceptance change is frontmatter and index only.
- **The §23.2 quotation is the ADR's spine** (F1). Quote `:1715` and `:1351`
  verbatim in `## Context`, then state the two constraints that keep the
  exception intact: the control never hosts Pegasus UI, and it is never visible.
  A reviewer who cannot find those two sentences should reject the ADR.
- **Add no index row at first merge** (F4), and do not let `AGENTS.md:115` (F5)
  argue otherwise. The discoverability answer while it is `proposed` is plan 00
  § 3's ADR set table and this ticket.
- **Say what stays central, not only what moves** — the Execution-placement
  "yes" row. ADR-0025 and ADR-0028 are *related*, not superseded; ADR-0028's own
  Consequences (`:57-60`) already require "measured evidence… and a new accepted
  ADR" before a renderer moves host, which is exactly what ADR-0108 plus the
  parity gate provide.
- **The retention clause must be a gate with an owner**, not a sentiment:
  the gateway renderer stays until [[FEAT-041]]'s golden-file suite passes on
  approved fixtures, and after that no required report may depend on the web
  renderer unless a superseding ADR says so.
- **The reversal condition must be written before the evidence exists**, which is
  the only time it can be written honestly: WebView2 runtime absence across the
  fleet (F11, A-00-7-4), or a golden-file divergence that cannot be closed within
  tolerance (F10, A-00-7-5).
- **Cite Microsoft Learn with a fetch date** (A-00-7-2). An immutable body that
  names a renamed API ages badly, and the ADR's own `## Links` is the only place
  the claim can be checked later.
- **This ticket's own diff is three docs files.** ADR-0108 and the two source plans
  are corrected together; no code, index row, ADR-0025/0028 change, or `src/` change
  is included. The renderer remains [[FEAT-040]]'s.

## Open questions

- **`related_frd: [FRD-11]` versus the measured house form `[frd-11]`.** The
  ticket body's step 3 writes the uppercase display form; all 28 existing ADRs
  use lowercase file stems and none uses the uppercase form (F7). The body is
  settled and outranks this document, so the plan follows it and **flags the
  discrepancy for the reviewer at the point of writing** rather than diverging
  silently. It is a one-token frontmatter value, it blocks nothing, and it is
  raised here rather than opened as a blocking question.
- **Nothing else is open.** Packaged-controller validation is owned by [[FEAT-040]];
  the acceptance flip by [[FEAT-038]]; parity fixtures by [[FEAT-041]]; and the
  `AGENTS.md` index sentence by [[FND-005]]. Each is a scope boundary with a named
  owner and none gates this ticket's `leave-preparing`.
