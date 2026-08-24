# Plan — FEAT-040: Desktop report renderer — Scriban + isolated WebView2 `PrintToPdfStreamAsync` + PDFsharp post-processing

## 2026-08-25 correction — documented invisible host

Microsoft Learn documents `HWND_MESSAGE` as the valid parent for an invisible `CoreWebView2Controller` on Windows 8 and later; the WebView will never become visible. The fixed design is `CoreWebView2Environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE)`. This supersedes every earlier collapsed-XAML/hidden-HWND host-selection instruction below. Phase 7 validates packaged-app initialisation, PDF output and no-window behaviour; it does not select a host.


**Diff estimate: ~8 files, ~760 lines.**

Derived from the files document: the renderer itself ~430 lines (the gateway equivalent is 326 —
`wc -l src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` → 326 — plus the
fixed controller lifetime, margin-unit conversion and tuple resolution), the authorised-identity
resolver ~70, the desktop csproj ~6, the DI registration ~20, the view-model test file ~220
(placeholder, cancellation, provenance, concurrency and six negative tuple cases), the
architecture-test extension ~35, and ~30 across the three documentation files. No route, no
migration, no Azure write.

## Approach

Reproduce `PlaywrightAssessmentReportRenderer` structure for structure behind the existing
`IAssessmentReportRenderer` seam, swapping only the two lines that actually differ — how the HTML
reaches a browser (`SetContentAsync` → `NavigateToString` on the isolated `CoreWebView2`) and how
the PDF comes back (`page.PdfAsync(PagePdfOptions)` → `PrintToPdfStreamAsync(CoreWebView2PrintSettings)`).
The rejected alternative was to write a cleaner renderer from the Scriban templates directly — a
smaller, tidier file that nobody could compare against the baseline. It is rejected because
[[FEAT-041]] (plan handle `DSK-07-15`) has to be able to attribute a golden-file failure, and every
gratuitous divergence in context building, placeholder rejection or page setup turns a fixture
failure into an investigation. Fidelity beats elegance here. The existing `IAssessmentReportRenderer` is the renderer boundary;
with the documented host fixed, no separate `OffScreenWebViewHost` type is warranted.

## Governing docs

`refs`: `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md`.

| Ref | Requirement | Meets |
| --- | --- | --- |
| FRD-11 | Report content, correction and finality; what a rendered report must contain and that nothing may be substituted | Steps 4–5 reproduce the composition and fail closed on identity; step 8's provenance passes `GenerateAssessmentReportDraft`'s re-hash. Finality itself stays FRD-11's and [[FEAT-042]]'s — this ticket adds no finality concept |

The ticket carries **`docs_todo: true`**:

> **New ADR** — ADR-0108 (isolated, non-UI WebView2 HTML→PDF rendering; never-UI rule; gateway
> renderer retained until golden-file parity), authored by [[FND-007]] (plan handle `DSK-00-07`);
> ADR-0108 has two claimants, so see [[FND-007]]'s plan for the ownership reconciliation —
> [[FEAT-038]] (plan handle `DSK-07-12`) owns the acceptance flip, while this ticket records
> packaged-app evidence for the documented `HWND_MESSAGE` host.
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (the ADR-0108 row) and in
> `docs/desktop/README.md` § Locked decisions (L-03); if the ADR lands differently this plan is
> revised before implementation.

`refs` carries no ADR, so the programme-level authorities that bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| L-03 / ADR-0108 (as recorded in 00 § 3) | Isolated non-UI WebView2; gateway renderer retained until golden-file parity | Steps 3, 10, 11 |
| Proposal § 23.2 | The isolated-WebView2 exception: off-screen, one purpose, never hosts Pegasus UI | Step 11 |
| Proposal § 12.5 | Documents, PDFs and reports rendered from the governed templates | Step 4 |
| Proposal § 27 item 6 | A report can be produced and previewed without the web application | Steps 3–8; preview itself is [[FEAT-042]] |
| L-01 | The canonical store stays behind the gateway | Nothing here stores anything |
| L-02 | All evidence is produced locally | Verification; step 13's operator run is on the baseline workstation |
| Upstream `TICK-216`, accepted 2026-08-19 | Exact supplied wording, named qualifications and three signatures, **provided** name, qualification and signature match as one tuple; missing or mismatched fails closed; absent content is never invented; human approval still required before issue | Step 5 and its six negative tests |
| WebView2 documentation (fetched 2026-08-23; re-fetched in step 2) | One print operation per WebView at a time; `PrintToPdfStreamAsync` returns a rewound stream | Steps 6–7 |
| `docs/current-architecture.md:86-90` | Unknown outcomes remain unknown | Step 9's named runtime-missing failure, which is not silently a success or a crash |
| `Directory.Build.props:8` | `TreatWarningsAsErrors` | Verification's Release builds |
| `HZN-001` / `board-conventions.md` § Upstream ids versus board ids | Upstream ids never written bare | Every `upstream TICK-216` citation here |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`, then `pegasus-desktop-reviewer` —
  `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `microsoft-code-reference` (Microsoft Learn
  plugin) → `winui-code-review` (`.codex/skills/winui-code-review/SKILL.md`, the `WUI4xxx` interop
  rules for WebView2 initialisation)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_fetch` on
  <https://learn.microsoft.com/microsoft-edge/webview2/how-to/print> and the `CoreWebView2`
  reference; `microsoft_code_sample_search` for `PrintToPdfStreamAsync` and
  `CoreWebView2Environment.CreateAsync`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute`
  → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every
  move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refines the body's fourteen steps in the same order.

1. **Orient and take.** Read the plan row (`docs/desktop/07-integrations/README.md` § 5,
   `DSK-07-14`), the proposed ADR-0108 from [[FND-007]], that area's § 2 WebView2 facts and § 7 trap rows, the
   [[FEAT-043]] (plan handle `DSK-07-17`) upstream `TICK-216` record, and
   `src/Pegasus.Infrastructure/Reports/PlaywrightAssessmentReportRenderer.cs` **end to end** (326
   lines). Call `get_doc_gates FEAT-040`, then `take_ticket` on branch
   `task/dsk-07-14-desktop-renderer`.
2. **Validate the documented controller first and record the evidence in `research`.** Re-fetch
   the controller and print documentation, then create
   `CoreWebView2Controller` with
   `CoreWebView2Environment.CreateCoreWebView2ControllerAsync(HWND_MESSAGE)`. Render a trivial
   HTML document to PDF twice and repeat the check from the packaged desktop app. Record the
   observed runtime version (`A-07-14-4`), non-empty output, no-window evidence and the
   `CoreWebView2PrintSettings` margin-unit shape (`A-07-14-2`). This validates integration;
   it does not choose a host.
3. **Create `WebView2AssessmentReportRenderer` in `src/Pegasus.Desktop.Infrastructure`**
   implementing `Pegasus.Core.Reports.IAssessmentReportRenderer`. The renderer owns the fixed
   controller lifetime directly; do not introduce an `OffScreenWebViewHost` abstraction.
4. **Reproduce the composition, not a reinterpretation of it.** Build the two `ScriptObject`
   contexts exactly as `PlaywrightAssessmentReportRenderer` does from one `AssessmentReportSnapshot`
   (`:23-80` and `CommonContext` from `:140`), parse the templates from the embedded resources
   [[FEAT-039]] (plan handle `DSK-07-13`) shares, and keep **both** halves of the placeholder
   rejection: `template.HasErrors` → throw, and `html.Contains("{{", StringComparison.Ordinal) ||
   html.Contains('«')` → `ReportRenderRejectedException` (`:105-114`). Reuse that exception type;
   `AssessmentReportRendering.cs` already uses it sixteen times and `:312` declares it.
5. **Resolve the engineer identity as one authorised tuple and fail closed.** Upstream `TICK-216`'s
   accepted contract — recorded by [[FEAT-043]] — authorises the exact `reference/rendererref1/`
   wording, its named qualifications and the governed engineer signatures (Andy Patterson, Ed
   Mawdsley, Neil O'Reilly) **only** where the selected engineer's name, qualification and signature
   match as one tuple. Resolve exactly one such tuple and fail closed on missing, unknown,
   mismatched or substituted values: no silent omission, no fallback signature, no caller-supplied
   or custom signature path, no invented wording or qualification. Core owns the authorisation
   decision; this renderer maps the accepted tuple to the byte-identified assets [[FEAT-039]]
   embeds and chooses no identity of its own. Six negative tests: missing name, missing
   qualification, missing signature, unknown key, a signature paired with another engineer's name,
   and an arbitrary substitution.
6. **Match the print settings one for one via `CoreWebView2PrintSettings`:** A4 page size,
   backgrounds printed, header and footer displayed with the same footer template and an empty
   header (`"<span></span>"`, `PlaywrightAssessmentReportRenderer.cs:124`), and margins top 8 mm,
   right 12 mm, bottom 22 mm, left 12 mm (`:126`). Convert to the units the settings type expects
   and **record the conversion in a comment**. Call `PrintToPdfStreamAsync`, which returns a rewound
   stream, and read it fully.
7. **Serialise renders with `SemaphoreSlim(1, 1)`**, taken before any host work exactly as the
   gateway renderer does at `:19`/`:26`. The documentation permits one print operation per WebView
   at a time; a parallel render throws. Test: two concurrent `RenderAsync` calls both succeed and
   neither corrupts the other's output.
8. **Post-process with PDFsharp as the gateway does.** `PdfReader.Open(new MemoryStream(pdf),
   PdfDocumentOpenMode.Import)` for the page count (`:133`), then `RenderedReportArtifact` with the
   suggested file name, the bytes, the page count, `Convert.ToHexStringLower(SHA256.HashData(pdf))`,
   `AssessmentReportContract.TemplateVersion`, and an engine-version string naming WebView2 and its
   **runtime** version. `GenerateAssessmentReportDraft` re-hashes and rejects a mismatch
   (`AssessmentReportRendering.cs:291-307`), so hash the exact bytes returned.
9. **Handle a missing or outdated WebView2 runtime as a named failure.** Detect it at composition —
   the user-facing prompt belongs to the area 04 startup check ([[FND-045]], plan handle
   `DSK-04-09`) — and return or throw a **distinct** failure the caller maps to "render unavailable
   — use the gateway renderer", logging the runtime version found. Name the install step from the
   startup check rather than inventing one. Not an exception dump, and not a silent success.
10. **Register in the desktop host's DI so the gateway path remains available.** Composition selects
    the desktop renderer when the runtime is present and the parity flag allows it; the gateway
    `POST /api/v1/cases/{id}/reports/draft` remains the fallback until [[FEAT-041]] signs off.
    **Record the flag name in this document** under a dated heading — [[FEAT-042]] (plan handle
    `DSK-07-16`) step 9 consumes it after parity; record it once in the renderer plan or the finalise ticket,
    whichever lands first.
11. **Prove the never-UI rule mechanically.** The WebView2 is never navigated to an http/https
    Pegasus URL, hosts no application XAML, and is created off-screen. Extend [[FND-037]]'s (plan
    handle `DSK-02-12`) architecture test so the only permitted `WebView2` usage in the solution is
    this renderer type, and run `winui-code-review`'s `WUI4xxx` checks for uninitialised-WebView2
    defects.
12. **Unit and adapter tests in the desktop test project:** an unresolved placeholder is rejected; a
    cancelled render throws `OperationCanceledException` and leaves no partial artifact; the
    returned page count matches PDFsharp's; the SHA-256 matches the returned bytes; two concurrent
    renders serialise; and every step-5 negative case fails closed rather than rendering.
13. **Operator step.** Run one real render of each of the four `AssessmentReportOutcome` values
    (`TotalLoss`, `Repairable`, `CashInLieu`, `ContractRepair` — the same four
    `tests/Pegasus.IntegrationTests/Reports/AssessmentReportRendererTests.cs` uses) on the baseline
    Windows 11 workstation, **from the packaged app**, and hand back: the four PDFs, the WebView2
    runtime version the app reports, the wall-clock render time for each, and confirmation that no
    window appeared. Include one render per authorised engineer identity where a fixture exists, and
    confirm each artifact carries that person's matching name, qualification and signature. Attach
    to the ticket proof; [[FEAT-041]] compares them with the gateway fixtures.
14. **Simplification pass and PR.** Run the pass over this branch's diff, record it under a dated
    `## Simplification pass` heading below, and open the PR into `dev`.

## Verification

Evidence tier from the body: **Tier 3 — Parser/adapter contracts**
(`docs/engineering.md` § Required evidence tiers item 3: corruption, expansion/resource limits,
cancellation, path/integrity safety, stable contract codes and deterministic external failures).
`proof` is the captured output of:

- `dotnet build ./src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj -c Release`
  — expected: succeeds under `TreatWarningsAsErrors` (`Directory.Build.props:8`).
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release`
  — expected: placeholder-rejection, cancellation, provenance, concurrency and **all six**
  engineer-tuple fail-closed facts pass, named individually.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release`
  — expected: the WebView2 single-permitted-usage fact passes, and still fails when a second
  reference is introduced (check that by introducing one locally and reverting).
- **Operator render record** attached to the proof — four PDFs, the runtime version, per-render
  wall-clock timings, "no window appeared", and each artifact carrying the selected engineer's
  matching name, qualification and signature.

Behaviour to observe: no window, taskbar entry or focus change during a render on the packaged app;
a second render started while one is in flight waits rather than throwing.

## Risks / open questions

- **Risk — the documented `HWND_MESSAGE` controller fails to initialise or print from the packaged
  app.** Mitigation: step 2 runs before renderer work and records the runtime and PDF evidence. Keep
  the gateway renderer through parity; report a failure against ADR-0108's reversal condition rather
  than introducing a collapsed-XAML or arbitrary-hidden-HWND alternative.
- **Risk — one print operation per WebView; parallel renders throw.** Mitigation: the
  `SemaphoreSlim(1, 1)` at step 7 plus the concurrency test at step 12.
- **Risk — margin-unit conversion drift.** Mitigation: the conversion is recorded in a comment
  (step 6) and [[FEAT-041]]'s documented position tolerance absorbs representable-precision
  differences; a *changed* margin is not an acceptable fix for a failing fixture.
- **Risk — Chromium drift.** The WebView2 runtime updates itself while `Directory.Build.props:18`
  pins Playwright to 1.61.0. Mitigation: pixel equality is explicitly not the target;
  [[FEAT-041]] sets tolerances.
- **Risk — a professional-attribution defect.** A valid signature paired with another engineer's
  name is the defect upstream `TICK-216` fails closed on, and moving the render to the client is
  where it would be easiest to introduce unnoticed. Mitigation: six negative tests in every
  direction, and the renderer choosing no identity of its own.
- **Scope boundary, not an open question** — which engineer identities are authorised is
  [[FEAT-043]]'s recorded disposition of an already-accepted upstream contract; which signature
  assets are embedded is [[FEAT-039]]'s; the parity fixtures are [[FEAT-041]]'s; storage and
  preview are [[FEAT-042]]'s; ADR-0108 is authored by [[FND-007]]; only its later acceptance flip is [[FEAT-038]]'s.
- **No open question is opened.** The host is settled by the documented `HWND_MESSAGE` API; margin
  units and packaged-app integration are implementation facts, and step 13 is an operator action
  producing proof, not a decision anyone is waiting on.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
