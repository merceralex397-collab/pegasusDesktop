# Plan — FEAT-018: S18 Report generation, preview, finalise, send

**Diff estimate: ~26 files, ~2,000 lines.** Derived from the files document: 4 contracts DTO files
(~200), 4 desktop view-model/XAML files (~500), 3 desktop-infrastructure client/wiring files
(~250), 5 gateway endpoint files including the dual-mode draft endpoint and the flag (~450),
7 test files — 3 contract, 2 view-model, 1 golden-file integration, 1 architecture (~500), and
3 documentation files (~100). No new renderer is written here; [[FEAT-040]] (plan handle
`DSK-07-14`) owns that type and is a dependency, not a line in this estimate.

## Approach

Fetch the **projection** from the gateway, render it on the desktop through the injected
`IAssessmentReportRenderer` that [[FEAT-040]] implements over
`CoreWebView2.PrintToPdfStreamAsync`, preview the result, and make Finalise and Send two separate
deliberate commands — Finalise uploading the canonical PDF through the transfer service and
registering it, Send carrying a stable idempotency key the gateway authorises and executes. A
single named flag selects both the renderer and the draft endpoint's response mode, so the gateway
Playwright renderer stays selectable until golden-file parity is signed off.

Rejected: **rendering on the gateway and streaming bytes to the desktop for preview**. It is what
happens today (`Index.cshtml.cs:319` returns `File(pdf, …)`), it keeps Chromium and the ADR-0028
CPU/memory uplift in the Container App, and it fails the placement test's *measured operational
advantage* row in the direction L-03 already settled. Also rejected: **writing a second renderer
inside this slice** — the ticket forbids it, and two `IAssessmentReportRenderer` implementations in
`Pegasus.Desktop*` would be a stop condition under `docs/engineering.md` § One Core owner.

## Governing docs

The ticket's `refs` is `docs/frd/frd-11-reports-correspondence-and-reviewed-proposals.md`, which
exists in the repository.

| Ref | Requirement | Meets |
| --- | --- | --- |
| FRD-11 § `Report correction, finality, and post-report work` (`:130-134`) | An issued report has an immutable artifact/version identity and hash; a correction creates a new reasoned version and retains every earlier artifact; never a silent overwrite | Step 9 (Finalise registers once), Step 12 (contract fact: a finalised report refuses a silent overwrite) |
| FRD-11 (`:138-144`) | The report-sent business event **is** the approved-mailbox Sent-item evidence per FRD-08 § `Outbound correspondence evidence`; a draft, queue result or staff assertion proves neither sending nor receipt | Step 10 (send status re-query resolves from the evidence, not from the command's own return), Step 12 (replay fact) |
| FRD-11 § `Targeted sending and reviewed AI proposals` (`:167-173`) | A targeted report-send transaction is idempotent and records approved destinations, immutable artifact/version, Box filing, exact send evidence, completion outcome and partial-failure recovery | Steps 9–10, Step 12 |
| FRD-11 (`:157-165`, requirements list) | Deterministic template and payload versioning; preserved document/source provenance; immutable issued artifact identity and hash; accessible presentation of status, validation and failure "without implying an unproved external delivery" | Steps 6–7 (the renderer identity and `TemplateVersion = "rendererref1-v1"` are carried), Step 6 (the `NotReady` reasons render as rows, not prose) |

`docs_todo: true`, confirmed in `get_doc_gates FEAT-018` — the `governing-doc` requirement at
`leave-backlog` reads `satisfied: true`.

> **New ADR** — ADR-0108 (report rendering in the desktop through an isolated, non-UI WebView2
> HTML→PDF path; gateway renderer retained until golden-file parity), authored by [[FEAT-038]]
> (plan handle `DSK-07-12`).
> This plan is written to the decision as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 (ADR set table, `:163`) and locked
> decision L-03 in `docs/desktop/README.md`; if the ADR lands differently this plan is revised
> before implementation. **This ticket edits no ADR** — it supplies the renderer-selection flag
> name, its default and the parity outcome to [[FEAT-038]] while ADR-0108 still reads
> `status: proposed`. ADR-0100 and ADR-0103 also bind and are authored by [[FND-005]] (plan handle
> `DSK-00-05`).

Programme-level authorities that bind today:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal § 12.5, § 13.9 | Report generation is interactive and local; canonical storage, audit and sending stay central | Steps 6, 9, 10 |
| Proposal § 23.2 (Native verification) / Phase 7 exit gate | Approved fixtures match; no required report depends on the web renderer unless explicitly retained | Steps 7, 11 |
| Proposal § 14.5 | Long-running work shows progress and remains cancellable | Step 6 |
| L-03 | Isolated non-UI WebView2 HTML→PDF path; gateway renderer retained until golden-file parity | Steps 7, 11 |
| L-01 | Gateway registers the canonical copy and audits the send | Steps 9, 10 |
| L-02 | Verification and the performance run happen on the local Test/UAT workstation | Step 13 |
| L-04 | Routing named on the ticket | § Routing |
| `docs/engineering.md` § Required evidence tiers (2, 5, 7, 10) | Tier 10 obliges a **measured** generation time on baseline hardware, not an asserted one | Step 13 |
| `docs/design/README.md:432-445` | Only populated sections render; a page never describes its own mechanics | Step 6 |
| Operator decision, 2026-08-24 (Send to AI) | AI-09 is a recorded exclusion with a reactivation condition | Step 2's exclusion; § Risks |
| ADR-0028 / plan 11 | The Container App CPU/memory uplift reversal is an ⚠ Azure change owned elsewhere | § Risks (out of scope here) |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template requires in the plan.

- **Subagent**: `winui-dev` — `.codex/agents/winui-dev.toml`; `pegasus-gateway-dev` —
  `.codex/agents/pegasus-gateway-dev.toml`; `pegasus-test-engineer` —
  `.codex/agents/pegasus-test-engineer.toml`; `pegasus-desktop-reviewer` —
  `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `microsoft-code-reference` (Microsoft Learn
  plugin — verify the WebView2 print-to-PDF API before writing it) → `winui-dev-workflow`
  (`.codex/skills/winui-dev-workflow/SKILL.md`) → `winui-design`
  (`.codex/skills/winui-design/SKILL.md`) → `dotnet-webapi` (dotnet/skills `98f84851`) →
  `run-tests` → `winui-code-review` at review
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`,
  `microsoft_code_sample_search`, `microsoft_docs_fetch`)
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` → `kanmer-execute`
  → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call `get_doc_gates <id>` before every
  move; a move crosses at most one gated boundary)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md`
  § Repository task workflow step 5)

## Steps

Refining the ticket body's fourteen steps — same order, same ownership, same paths. Body step
numbers in brackets.

1. **[body 1] Orient and take.** Read the plan row, `vertical-slices.md` § S18, the `reuse-map.md`
   `Reports/` row, ADR-0108 as authored by [[FEAT-038]], and FRD-11 § `Report correction, finality,
   and post-report work` (`:130-166`) for the finality and regeneration rules. Call
   `get_doc_gates FEAT-018`, then `take_ticket` with branch `task/dsk-05-18-reports` and worktree
   `../pegasus-worktrees/dsk-05-18-reports` from `origin/dev`.
2. **[body 2] Record the pipeline contract.** Read `AssessmentReportProjection.cs`,
   `AssessmentReportRendering.cs` and `PlaywrightAssessmentReportRenderer.cs`. In `research`, record
   the projection contract, the two template names actually embedded
   (`Pegasus.Infrastructure.csproj:42-47` — `assessment_report.scriban`,
   `assessment_fee_note.scriban`, `report.css`; the other four `.scriban` files in the folder are
   not reachable), and the post-processing steps to reproduce: PDFsharp page count, lowercase-hex
   SHA-256, `TemplateVersion = "rendererref1-v1"`, the renderer identity string, and the
   unresolved-placeholder guard at `:114-117`. Record the SHA read. **Also record that
   `OnPostSendAsync` (`:583`) is a Send-to-AI handler and is not the send this ticket implements.**
3. **[body 3] Confirm the dependencies before writing anything.** [[FEAT-040]] (plan handle
   `DSK-07-14`) must have landed an `IAssessmentReportRenderer` implementation in
   `src/Pegasus.Desktop.Infrastructure`, and [[FEAT-039]] (plan handle `DSK-07-13`) must embed the
   templates from one source with a hash check. If either is missing the ticket **stays in
   Preparing** — do not write a second renderer here.
4. **[body 4] Re-verify the WebView2 API.** This research already settled it against Microsoft
   Learn on 2026-08-24: `CoreWebView2.PrintToPdfStreamAsync(CoreWebView2PrintSettings)` returning
   `Task<System.IO.Stream>`, rewound to the start of the PDF data — package
   `Microsoft.Web.WebView2`, namespace `Microsoft.Web.WebView2.Core`. The file-path variant
   `PrintToPdfAsync(string, CoreWebView2PrintSettings)` exists but would write an unencrypted report
   to disk. Re-verify with `microsoft_code_sample_search` / `microsoft_docs_fetch` at
   implementation time and record the confirmed signature here, together with the documented
   constraint that **only one printing operation may be in progress per WebView** — the desktop
   renderer needs a serialising gate like `PlaywrightAssessmentReportRenderer.cs:19`.
5. **[body 5] Confirm the endpoints and settle the dual response mode.** With [[GWY-014]] (plan
   handle `DSK-03-14`) and [[FEAT-042]] (plan handle `DSK-07-16`):
   `POST /api/v1/cases/{id}/reports/draft` returns the **projection** for local rendering, or the
   gateway-rendered bytes while the flag selects the retained renderer;
   `POST /api/v1/cases/{id}/reports` registers the finalised PDF;
   `GET /api/v1/cases/{id}/reports/{rid}/content` serves it back;
   `POST /api/v1/cases/{id}/assessment/send` carries the idempotency key and audits the provider
   message id. Note that today the web draft handler returns **bytes** (`Index.cshtml.cs:319`), so
   the projection mode is new, not a rename.
6. **[body 6] Implement `ReportViewModel`.** Fetch the projection; render locally through the
   injected renderer; show a preview; offer **Finalise** and **Send** as separate deliberate
   commands. Long rendering shows progress and stays cancellable (proposal §14.5). Render a
   `NotReady` result as rows built from `Requirement` / `WhyOutstanding` (`Index.cshtml.cs:313-317`)
   — no composed explanation, no how-it-works copy (`docs/design/README.md:432-445`).
7. **[body 7] Implement the renderer-selection flag.** One name, one default, recorded here, and
   it selects both the renderer and the draft endpoint's response mode. Hand the name and default
   to [[FEAT-038]] for ADR-0108's Consequences **before** the acceptance flip. This ticket makes no
   edit to ADR-0108.
8. **[body 8] Implement the WebView2-absent path.** When the runtime is missing, show the guided
   message from [[FND-045]]'s (plan handle `DSK-04-09`) startup check and fall back to the gateway
   renderer rather than failing the workflow.
9. **[body 9] Implement Finalise.** Upload the rendered PDF through the transfer service from
   [[FEAT-014]] (plan handle `DSK-05-14`) and register it with `POST /api/v1/cases/{id}/reports`,
   so the canonical copy is stored once and its registration is audited. Regeneration follows
   FRD-11 `:130-134`: a finalised report is never silently replaced; a correction is a new reasoned
   version that retains every earlier artifact.
10. **[body 10] Implement Send.** Generate a stable idempotency key **once per user-initiated
    send** and reuse it on retry. An uncertain outcome is resolved by re-querying the send status,
    never by resending. Per FRD-11 `:138-144`, the send is proved by the approved-mailbox Sent-item
    evidence (FRD-08 § `Outbound correspondence evidence`, `frd-08-…md:328`) — not by the command's
    own return, a queue result or a staff assertion. The desktop confirms; the gateway authorises
    and executes, because Graph credentials never reach the workstation (ADR-0106).
11. **[body 11] Run the golden-file suite.** From [[FEAT-041]] (plan handle `DSK-07-15`), for every
    approved fixture, compare WebView2 output against Playwright output on text, values, page count
    and key element positions within the documented tolerances. Scope: the **two** live templates.
    A failure blocks the parity claim, not the ticket's honesty — record the diff.
12. **[body 12] Contract tests.** In `tests/Pegasus.Api.ContractTests`, for draft, register,
    content and send: success, 401, 403, 409 stale version, replay of the send idempotency key
    returning the original outcome, and a finalised report refusing a silent overwrite. Enable
    `Features:DesktopGateway` explicitly.
13. **[body 13] Operator step.** Measure report generation on the baseline Test/UAT workstation
    against the target in `docs/desktop/10-security-observability-performance/README.md`, and have
    the operator confirm the final document and its audit trail are correct. Record the figures,
    the workstation specification and the sign-off in the ticket proof. Tier 10 requires a measured
    figure, not an asserted one.
14. **[body 14] Documentation, simplification, PR.** Update `parity-matrix.md` row `PAR-15` (report
    portion only), cross-reference FRD-11 from `docs/frd/frd-13-desktop-operator-experience.md`, add
    the `DSK` rows to `docs/capabilities.md`, run the simplification pass over the branch diff under
    a dated `## Simplification pass` heading, then open the PR into `dev`.

## Verification

Evidence tiers from the body: **2**, **5**, **7**, **10**.

- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --filter "Category!=Corpus&Category!=Browser"`
  — golden-file report facts pass alongside the existing `tests/Pegasus.IntegrationTests/Reports/`
  tests, which stay green because the gateway renderer is retained.
- `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-build`
  — draft, register, content and send facts including idempotent replay and the refused silent
  overwrite.
- `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release --no-build`
  — preview, finalise, send, cancellation and WebView2-absent facts.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build`
  — the no-WebView-hosting-Pegasus-UI fact passes and the desktop holds no second renderer.
- **Performance and operator records in the proof** — a measured generation time on the named
  baseline workstation, within the target, and the operator's confirmation that the final document
  and its audit trail are correct.

Evidence that becomes `proof`: the four test outputs, the golden-file comparison report (including
any recorded tolerance), the measured performance figures with the workstation specification, and
the operator sign-off.

## Risks / open questions

- **There is no report-send baseline.** `grep -rn "OnPostSend" src/Pegasus.Web/Pages/` returns one
  hit and it is the Send-to-AI handler. Mitigation: every send requirement in step 10 is traced to
  an FRD-11 or FRD-08 line rather than to a handler, and the acceptance evidence is the Sent-item
  record, not the command's return. This is recorded rather than smoothed over because a reviewer
  looking for the "web send path" will not find one.
- **`OnPostSendAsync` (`:583`) and `OnPostReconcileAsync` (`:628`) are AI-09 surfaces.** Send to AI
  is a **recorded exclusion with a reactivation condition** (`docs/capabilities.md:269`), settled by
  the operator on 2026-08-24 — not an open question, and no `open-questions` document is created
  for it on any ticket.
- **ADR-0108's content** — owned by [[FEAT-038]] (plan handle `DSK-07-12`). Scope boundary, not an
  open question. Answer arrives when that ticket accepts the ADR; this plan is revised if it lands
  differently.
- **The renderer and the golden-file suite are other tickets' work** — [[FEAT-040]] and
  [[FEAT-041]]. Mitigation: step 3 is a hard precondition; the ticket stays in Preparing rather
  than growing a second renderer.
- **Golden-file parity may not be achievable within tolerance.** Mitigation: the flag keeps the
  gateway renderer selectable; a failure is recorded as a diff and the parity claim is withheld.
  The ticket can still merge honestly with the flag defaulted to the retained renderer.
- **WebView2 serialises printing.** Documented: only one printing operation per WebView at a time;
  a concurrent call throws. Mitigation: the desktop renderer carries a gate like
  `PlaywrightAssessmentReportRenderer.cs:19`, and the view model's progress/cancel path must not
  start a second render.
- **Only two templates are live** (`Pegasus.Infrastructure.csproj:42-47`). Mitigation: the parity
  claim is scoped to those two and says so; four `.scriban` files in the folder are not reachable
  from `IAssessmentReportRenderer` today.
- **Retiring the Playwright renderer is not this ticket's.** The `AddPegasusReportRendering`
  removal and the ADR-0028 Container App uplift reversal are an ⚠ Azure change owned by plan 11
  ([[PLAT-026]], plan handle `DSK-11-08`); the pin and browser-lane removal are [[FEAT-026]]'s and
  are coupled to this ticket's parity outcome, not to its schedule.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's own
diff before the PR, recorded here under a dated heading._
