# Research — INTK-005: upstream:INTK-031 · Identify the third-party engineer behind an audit's original report

## Question

A timeboxed, evidence-first survey: label the local corpus of audit instructions by the **engineering firm that issued the original report**, record per issuer the layout tells that identify it, the facts its reports carry and where they sit, and — above all — **where and in what words that issuer states Repairable versus Total Loss**. The deliverable is the ticket's `research` document plus a proposed issuer registry and its rules; corpus content is never committed.

## Evidence examined

- Import decision: `coverage-decision.md` § Import list — the row for upstream `INTK-031` (this ticket; board `INTK-005`); § Plan gaps — "Three server-side domain requirements have no register at all: `unchanged-backlog` is only safe for rows that have a `docs/capabilities.md` row, and these have none"
- Carry-over register: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:157` — the row for upstream `INTK-031`, quoted as it stands (its first cell is an upstream id): `INTK-031 | intake-processing | backlog | feature | extraction, audits, corpus | … | unchanged-backlog | — | intake-processing`
- Repository evidence (fork `main`, read 2026-08-24):
  - `src/Pegasus.Core/Intake/DirectProviders/Qdos/QdosMailClassificationPolicy.cs:180-215` — `EvaluateStandaloneAuditReport`: two distinct document attachments required, exactly one bearing the audit-notification title, and exactly one of the others stating one outcome; `:223-229` — `ContainsRepairable` / `ContainsTotalLoss` with their negation guards. This is the single grammar the survey must replace with per-issuer selection.
  - `src/Pegasus.Core/Intake/Classification/MailClassificationContracts.cs:240` — the classification record carrying `AuditAssessment`
  - `src/Pegasus.Core/Cases/CaseContracts.cs:37-41` — `enum AuditAssessment { Repairable, TotalLoss }`; `:93-108` — `AuditIdentity.Create`, the `a.` / `ap.` prefix allocation
  - `src/Pegasus.Core/Intake/InstructionFieldExtraction.cs:11` — `InstructionFieldEngine` and its `FieldDefinition` record (`:13`); `:384` `IsUkRegistration`, `:400` `NormalizeRegistration` — the shared extraction route the upstream Approach says to reuse
  - `src/Pegasus.Core/Intake/DirectProviders/Qdos/QdosInstructionExtractionPolicy.cs:29-36` — the field definitions; `:383`, `:396` — `SubjectFactLines`. The registry must **not** live in this folder; it is not QDOS's.
  - `.gitignore:2` — `/corpus/` is ignored, and the directory is **absent from this checkout**, so the operator must supply it on the workstation before the survey can start
  - `tests/Pegasus.Core.Tests/Intake/Qdos/` — where per-issuer extraction facts would land
- Binding decisions: **L-02** the local production-mimicking stack is the only verification environment and corpus material stays local and untracked; **L-05** the fork board is the single work register; **D-001** upstream is frozen after the final sync, so this has no other route
- Depends on: `DSK-01-10` — the first one-way upstream sync, so the survey is taken against the extraction code the fork will actually carry
- Sibling: the imported `upstream:INTK-032` owns what happens when no issuer matches. This spike must finish first; it defines the abstention this ticket only names.
- Upstream links `INTK-028`, `INTK-032`, `CASE-014`: upstream INTK-028 is a closed upstream extraction fix cited as the shape of the recurring failure and has **no fork ticket**; upstream CASE-014 is the reference-prefix ticket and has **no fork ticket** either — neither is recreated on the fork board, and both are recorded here for provenance. The third, upstream INTK-032, **is** imported: it is board [[INTK-006]], the sibling named above.

## Scope and constraints

The desktop conversion needs this because the fact it establishes is immutable once used. `src/Pegasus.Core/Cases/CaseContracts.cs:93-108` (`AuditIdentity.Create`) turns `AuditAssessment.Repairable` into the prefix `a.` and `AuditAssessment.TotalLoss` into `ap.`, and a case reference cannot be corrected after allocation. Today that assessment is read by a **single grammar over every report**: `QdosMailClassificationPolicy.EvaluateStandaloneAuditReport` (`src/Pegasus.Core/Intake/DirectProviders/Qdos/QdosMailClassificationPolicy.cs:180-215`) groups the attachments, applies `ContainsRepairable` / `ContainsTotalLoss` regex literals (`:223-229`) to whichever attachment is not the instruction, and returns `null` when it cannot get exactly one outcome — which the caller reads as "not a standalone audit" rather than "this report could not be read". A firm that writes the outcome differently is therefore indistinguishable from a message that is not an audit at all.

No board ticket touches any of this. Searches across the 208 seeded bodies for `extraction`, `issuer` and `instruction draft` return nothing; [[DSK-05-09]] renders "classification evidence, field suggestions and extracted text" and [[DSK-03-10]] projects them, but neither owns how they were produced, and both are barred from `src/Pegasus.Core/Intake/**` or from the readers that live in `Pegasus.Infrastructure`. The carry-over disposition is `unchanged-backlog`, which `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` § Disposition categories justifies because "their capability rows stay in `docs/capabilities.md`" — and there is **no** `docs/capabilities.md` row for upstream INTK-031, no `capability`, `post-alpha` or `blocked` label on it, and an operator direction dated 2026-08-22 behind it. That restriction on `unchanged-backlog` is written into § Disposition categories by [[DSK-01-09]] step 15(e), which is its single owner; this ticket cites it and does not write it. Under **L-05** the fork board is the single work register, so leaving it in a table would silently drop a live operator-directed requirement.

It is filed as a **spike** because its deliverable is the labelled evidence base and the proposed registry, not the extractor change: the upstream Approach is explicit that the registry must sit beside the shared extraction code and not under `Intake/DirectProviders/Qdos/`, and where exactly it sits cannot be settled before the survey exists.

- Future owner: Core/Infrastructure intake work with focused Core and integration tests. The local Test/UAT stack is the verification environment; no production intake or Azure write is authorized.

- **Azure**: no write. No Azure resource is involved; the corpus is local.
- **Scope boundary**: this is a survey. It may **read** anything under `src/Pegasus.Core/Intake/`, `src/Pegasus.Infrastructure/Intake/` and the operator-supplied `corpus/`, and it may write the ticket's `research` and `open-questions` documents plus the two documentation targets named under § Documentation changes — the new `docs/capabilities.md` row and the upstream `INTK-031` row annotation. It must **not** edit § Disposition categories of `upstream-kanmer-carryover.md` ([[DSK-01-09]] step 15(e) owns that sentence), change extraction behaviour, add the registry, or touch any desktop project — those are the follow-on tickets this spike proposes.
- **Unblocks / blocked by**: this spike **blocks no seeded board ticket** — stated deliberately rather than left blank: [[DSK-05-09]] and [[DSK-03-10]] render and project extracted facts but neither asserts anything about how they were produced, and adding an issuer to provenance later is an additive contract change. What it does block is the imported `upstream:INTK-032`, whose fail-closed rule cannot be designed before the abstention contract exists. It is **blocked by** [[DSK-01-10]], the first one-way upstream sync, and by the operator supplying the corpus (step 2).
- **Traps**: the § Disposition categories sentence restricting `unchanged-backlog` to rows that have a `docs/capabilities.md` row has exactly one owner, [[DSK-01-09]] step 15(e) — this ticket cites it and never writes it; `corpus/` is immutable and untracked — committing content or an excerpt is a defect, and `docs/engineering.md` tier 1 requires the repository to prevent tracked corpus material. Do not key the registry by principal; the upstream body is explicit that it is keyed by firm and that another principal may forward the same firm's report. Do not build a second extractor. Do not design upstream INTK-032's (board [[INTK-006]]) operator-visible state here. Audit + inspection is **out of scope**; audits only. **Upstream ids and fork board ids do not match**: this ticket is board `INTK-005` and it is upstream INTK-031; upstream INTK-005 has **no fork ticket** and is not on this board at all. Read the join table in `HZN-001/board-conventions.md` § Upstream ids versus board ids; never compute the mapping.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — research-only`, unless a prototype test is written, in which case it is required over that diff and recorded under a dated `## Simplification pass` heading.

## Governing documents

- No canonical document is linked yet. Retain the ticket's existing `docs_todo` state; do not invent or link a proposed desktop ADR.

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.

## Corpus availability and method — 2026-08-25

The operator-supplied corpus is present at the local ignored path `C:/Users/PC/Documents/GitHub/pegasusDesktop/corpus/corpus`. It remains untracked and immutable; no corpus file, excerpt, claimant data, or generated output was added to the repository.

Read-only inventory:

- 2,567 files total: 271 `.eml`, 690 `.pdf`, 68 `.docx`, and 203 directories.
- All 271 EML headers and attachment names were scanned. Fifteen messages were audit/assessment candidates by subject or attachment vocabulary. Thirteen had report/instruction attachments; two were header-only candidates and had no attached report to attribute.
- A focused text extraction scan covered 283 PDFs under `cereference/audits`, `cereference/totalLoss`, `cereference/reports`, and `qdosmapping`. PyMuPDF extraction was used read-only. The cohort is a report-candidate survey, not a claim that every corpus document is an audit.
- Twenty legacy `.doc` attachments, image-only material, and PDFs without extractable text were not assigned an issuer from this pass. This is a coverage limitation, not permission to guess.

Principal/source provenance available from the EML headers and paths:

- QDOS-assist-forwarded candidates: 7 (sender domains/paths observed as `qdosassist.co.uk` or `qdosmapping`).
- Collision Engineers desk candidates: 5 (sender `collisionengineers.co.uk`).
- Connexus sender candidate: 1 (`connexus.co.uk`).
- Vehicle Resolutions sender candidate: 1.
- One additional total-loss subject from an `als.example.co.uk` sender had no attachments and cannot be attributed.
- These are observed corpus provenance labels, not product-level principal decisions. The firm registry must key on the report issuer, not the forwarding principal.

## Proposed issuer evidence

The following table records only extracted layout/outcome evidence and aggregate counts. `Repairable parts` is treated as a component label unless accompanied by the report-level status/title; it is not independently used as the outcome.

| Proposed firm key | Evidence cohort | Issuer/layout tells | Report-level outcome wording/location |
|---|---:|---|---|
| `collision-engineers` (internal control, excluded from third-party registry) | 142 PDFs: 105 total-loss, 18 repairable, 19 other/unclear | Collision Engineers letterhead/footer and `engineers@collisionengineers.co.uk`; observed in audit/report candidates | Report titles/status blocks use `TOTAL LOSS REPORT`, `REPAIRABLE REPORT`, and `T/Loss`; outcome is taken from the report-level title/status, not a parts list |
| `connexus-vehicle-assessors` | 27 PDFs, all repairable | `Engineer Repairable Report` title and explicit `Connexus Vehicle Assessors` issuer line | `Repairable` in the report title and repairable section |
| `exclusive-vehicle-assessors` | 63 PDFs, all repairable | `REPAIRABLE REPORT` title and explicit `Exclusive Vehicle Assessors` issuer line | `Repairable` in the report title; repairable-parts section is supporting evidence |
| `laird-assessors` | 6 PDFs, all total-loss style | `Total Loss Damage Assessment Report`, Laird footer/domain and social handle | Status block states `Total Loss`; supporting prose recommends total-loss treatment |
| `northern-assessors` | 1 PDF, total-loss style | `Northern Assessors` issuer line | `TOTAL LOSS REPORT - Cat N` title; the unrelated `Repairable parts` detail does not override the status |
| `sprint-assessors` | 1 directly attributable PDF sample | `sprintassessors@btinternet.com`, `Consulting Engineers`, `Automotive Claims Assessors` | `Vehicle Status: REPAIRABLE` |
| isolated/unknown candidates | 43 of the 283-PDF cohort were not assigned to the five recurring external firms above or the internal control | Atkinson, AMBER Vehicle Assessors, NorthEast Assessors, and a legal-evidence reference to Stephenson's appeared only in isolated material | No repeatable issuer/outcome grammar established; must abstain pending more legible evidence |

The repeated external-firm cohort is therefore 98 reports (27 Connexus + 63 Exclusive + 6 Laird + 1 Northern + 1 Sprint), with 43 remaining isolated/unassigned and 142 internal Collision Engineers reports excluded from the proposed third-party registry. Counts are heuristic text-extraction counts and require a later image/`.doc` review before they can become production selection rules.

## Registry and extraction proposal

The proposed registry is keyed by engineering firm and should sit beside the shared extraction code, for example `src/Pegasus.Core/Intake/EngineeringFirmReportRegistry.cs` with focused per-firm descriptors beside it. It must not live under `src/Pegasus.Core/Intake/DirectProviders/Qdos/`, because QDOS is a forwarding principal and not the report issuer. Issuer selection should precede the existing shared `InstructionFieldExtraction` grammar; it must not create a second extractor or duplicate field normalization.

Each descriptor should contain: firm key, positive issuer anchors, report-layout anchors, fact locations, outcome anchors for Repairable and Total Loss, precedence when a detail section conflicts with a report-level status, and an evidence reference to the reviewed local cohort. The registry remains a follow-on implementation; this spike creates no code or registry.

The abstention contract for [[INTK-006]] is:

1. No issuer matched: return no issuer attribution and preserve today's extraction behavior; do not fail intake solely because the issuer is unknown.
2. Issuer matched but the outcome is absent, contradictory, or only inferable from a non-authoritative detail: return an explicit outcome-unreadable/abstained result, not `Repairable` or `TotalLoss`.
3. Unknown issuer must never default to either case-reference prefix. The existing undifferentiated `null` is insufficient evidence for the later immutable reference allocation.

The desktop consequence is data-only: once implemented, issuer identity and outcome provenance must travel with extracted facts to [[DSK-05-09]] through [[DSK-03-10]] and onward to [[DSK-05-04]]. Adding that provenance is an additive contract change requiring the [[DSK-03-04]] OpenAPI snapshot and [[DSK-03-05]] generated client to be regenerated. This ticket does not make that change.

## Follow-on work and unresolved evidence

- Registry implementation: add the firm-keyed descriptor owner beside shared extraction and preserve unknown-issuer degradation.
- Per-issuer extraction rules: implement and test at least Connexus and Exclusive repairable layouts, Laird/Northern total-loss status layouts, and Sprint's vehicle-status wording; include Collision Engineers only as an explicit internal-control case if product scope later requires it.
- Provenance contract: carry issuer, outcome, and evidence location through Core/gateway/desktop contracts without client-side inference.
- [[INTK-006]]: decide the operator-facing state for matched-but-unreadable and no-issuer cases; this ticket intentionally does not make that product decision.
- Corpus completeness remains unresolved for legacy `.doc`, image-only material, and the 43 isolated/unassigned PDF candidates. A later read-only corpus pass is required before treating the proposed counts as exhaustive. No product behavior should be implemented from those unresolved cases.
- `docs/capabilities.md` has no existing upstream INTK-031 row in this checkout and the carry-over row is the live board provenance. No repository documentation was edited by this research-only pass.
- Simplification pass: n/a — research-only; no prototype or product code was written.
