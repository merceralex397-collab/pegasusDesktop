---
id: INTK-005
type: ticket
title: >-
  upstream:INTK-031 · Identify the third-party engineer behind an audit's
  original report
status: backlog
area: intake-processing
assignee: ''
profile: spike
labels:
  - extraction
  - audits
  - corpus
  - upstream-carryover
  - upstream-INTK-031
  - needs-operator
groups:
  - EPIC-014
links: []
docs_todo: true
archived: false
created: '2026-08-24T11:50:33.921Z'
updated: '2026-08-24T12:33:14.267Z'
---

## What

A timeboxed, evidence-first survey: label the local corpus of audit instructions by the **engineering firm that issued the original report**, record per issuer the layout tells that identify it, the facts its reports carry and where they sit, and — above all — **where and in what words that issuer states Repairable versus Total Loss**. The deliverable is the ticket's `research` document plus a proposed issuer registry and its rules; corpus content is never committed.

## Why

The desktop conversion needs this because the fact it establishes is immutable once used. `src/Pegasus.Core/Cases/CaseContracts.cs:93-108` (`AuditIdentity.Create`) turns `AuditAssessment.Repairable` into the prefix `a.` and `AuditAssessment.TotalLoss` into `ap.`, and a case reference cannot be corrected after allocation. Today that assessment is read by a **single grammar over every report**: `QdosMailClassificationPolicy.EvaluateStandaloneAuditReport` (`src/Pegasus.Core/Intake/DirectProviders/Qdos/QdosMailClassificationPolicy.cs:180-215`) groups the attachments, applies `ContainsRepairable` / `ContainsTotalLoss` regex literals (`:223-229`) to whichever attachment is not the instruction, and returns `null` when it cannot get exactly one outcome — which the caller reads as "not a standalone audit" rather than "this report could not be read". A firm that writes the outcome differently is therefore indistinguishable from a message that is not an audit at all.

No board ticket touches any of this. Searches across the 208 seeded bodies for `extraction`, `issuer` and `instruction draft` return nothing; [[DSK-05-09]] renders "classification evidence, field suggestions and extracted text" and [[DSK-03-10]] projects them, but neither owns how they were produced, and both are barred from `src/Pegasus.Core/Intake/**` or from the readers that live in `Pegasus.Infrastructure`. The carry-over disposition is `unchanged-backlog`, which `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` § Disposition categories justifies because "their capability rows stay in `docs/capabilities.md`" — and there is **no** `docs/capabilities.md` row for INTK-031, no `capability`, `post-alpha` or `blocked` label on it, and an operator direction dated 2026-08-22 behind it. That restriction on `unchanged-backlog` is written into § Disposition categories by [[DSK-01-09]] step 15(e), which is its single owner; this ticket cites it and does not write it. Under **L-05** the fork board is the single work register, so leaving it in a table would silently drop a live operator-directed requirement.

It is filed as a **spike** because its deliverable is the labelled evidence base and the proposed registry, not the extractor change: the upstream Approach is explicit that the registry must sit beside the shared extraction code and not under `Intake/DirectProviders/Qdos/`, and where exactly it sits cannot be settled before the survey exists.

## Source of truth

- Import decision: `coverage-decision.md` § Import list — row `INTK-031`; § Plan gaps — "Three server-side domain requirements have no register at all: `unchanged-backlog` is only safe for rows that have a `docs/capabilities.md` row, and these have none"
- Carry-over register: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:157` — `INTK-031 | intake-processing | backlog | feature | extraction, audits, corpus | … | unchanged-backlog | — | intake-processing`
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
- Upstream links `INTK-028`, `INTK-032`, `CASE-014`: `INTK-028` is a closed upstream extraction fix cited as the shape of the recurring failure; `CASE-014` is the reference-prefix ticket. Neither is recreated on the fork board — recorded here for provenance.

### Upstream ticket INTK-031 (verbatim)

Provenance — upstream area `intake-processing`; upstream status `backlog`; upstream profile `feature`; upstream labels `extraction`, `audits`, `corpus`; upstream links `INTK-028`, `INTK-032`, `CASE-014`; upstream `docs_todo` true. Read from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at commit `a5b28111`, read date **2026-08-24**. Copied unedited. Note that the fork profile is `spike` where upstream is `feature`; the requirement is unchanged, the gate is not — see § Why.

````
## What

Recognise which third-party engineering firm issued the original report that
arrives with an **audit** instruction, and select the extraction method for that
document from what the issuer is known to produce — rather than running one
grammar over every report and hoping the labels line up.

Scoped to **audits only**. Audit + inspection is out of scope for this ticket.

**The corpus and the registry are keyed by engineering firm, not by principal.**
QDOS is the principal we have seen audits from first, but another principal may
send an audit carrying a report from the same firm, and the same firm's layout
must be recognised either way. Nothing here belongs under a principal's
direct-provider namespace.

## The report's outcome is a required fact, not an optional one

Operator direction, 2026-08-22:

> "Audits are either a. or ap. depending on whether the original report said it
> was Repairable or Total Loss."

So the extractor must read, per issuer, **whether the report declares the vehicle
Repairable or a Total Loss** — and confirm it, not infer it. That fact decides the
case's own reference prefix ([[CASE-014]]), and a reference is immutable once
allocated, so a wrong or guessed reading cannot be corrected afterwards.

This raises the bar for this ticket in two ways:

- the per-issuer record must include **where and how each firm states the
  outcome**, and the vocabulary each uses for it — firms will not all write
  "Total Loss";
- abstention matters more than coverage. A report whose outcome cannot be read
  must say so rather than defaulting to either prefix. What happens then is
  [[INTK-032]]'s subject.

## Why

An audit instruction arrives with an original report written by a different
engineering firm each time — in practice the same few firms, but with no
consistent file naming, and each with its own report layout. [[INTK-028]] fixed
one such layout by de-anchoring the `Speedo:` rule after the deployed grammar
missed a multi-column line; that fix is correct but it is the general shape of a
recurring failure. A rule tuned to one firm's layout silently reads nothing, or
reads the wrong column, on another firm's.

Today nothing records **which** firm's document is in front of the extractor, so
there is no way to say which method applies, no way to see that a firm's layout
has changed, and no way to measure extraction quality per issuer. Building that
labelled evidence base is the prerequisite for every later per-issuer rule.
Keying it by firm also means each principal that starts sending audits inherits
whatever firms are already recognised, instead of restarting the survey.

## Approach

- Survey the local corpus for audit instructions across **every** principal that
  sends them, take the non-instruction document attachment as the original
  report, and label each by issuing firm — from the report's own content
  (letterhead, footer, issuer block), never from the file name and never from
  which principal forwarded it.
- Record, per issuer: the layout tells that identify it, which facts its reports
  carry (vehicle, registration, speedo, make/model, colour, VIN), where they sit,
  **and how that issuer states Repairable versus Total Loss**.
- Turn that into an issuer identification step in the intake extraction route
  that names the issuer on the extracted facts' provenance, and abstains rather
  than guessing when no issuer matches.
- Unknown issuers must degrade to today's behaviour, not fail the intake.
- Reuse the existing extraction route (`InstructionFieldExtraction` and the
  report grammar [[INTK-028]] corrected) — this adds issuer selection ahead of
  the grammar, it does not become a second extractor. The issuer registry lives
  beside the shared extraction code, not under
  `Intake/DirectProviders/Qdos/`, because it is not QDOS's.

**Constraint:** `corpus/` is local, gitignored and immutable. The labelling work
happens against it in place; the committed artefact is the issuer registry and
its rules, never corpus content or excerpts of it.

## Verification

- [ ] Corpus survey recorded in the ticket's research: audit instructions found,
      issuers identified, count per issuer, which principal each arrived via,
      and which reports could not be attributed.
- [ ] The survey records, per issuer, how Repairable and Total Loss are stated,
      including the wording each firm uses.
- [ ] Extraction tests cover at least two distinct issuers' real layouts, plus
      an unattributable report that still extracts what it can.
- [ ] The same issuer is recognised identically regardless of which principal
      sent the audit.
- [ ] Extracted report facts carry the identified issuer in their provenance.
- [ ] The Repairable/Total Loss outcome is extracted with its issuer and its
      location cited, and a report that does not state it clearly **abstains**
      rather than choosing a default.
- [ ] A report from an unknown issuer produces no issuer attribution and no
      regression against current extraction.

## Outcome
````

## Routing

- **Subagent**: `pegasus-parity-researcher` — `.codex/agents/pegasus-parity-researcher.toml` (the survey and the registry proposal); `pegasus-gateway-dev` — `.codex/agents/pegasus-gateway-dev.toml` if a prototype extraction fact is written
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `kanmer-research` (`.grok/skills/kanmer-research/SKILL.md`) → `test-gap-analysis` (dotnet/skills `98f84851`, `plugins/dotnet-test/skills/test-gap-analysis/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `spike`: `kanmer-research` → `kanmer-verify` → `kanmer-closeout` (the only gate is `enter-done`: `research` plus `questions-resolved`; call `get_doc_gates <id>` before every move)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orient. Read the verbatim upstream body above and `coverage-decision.md` § Import list row `INTK-031`. Call `get_doc_gates <this ticket id>`, then `take_ticket` with branch `task/upstream-intk-031-issuer-identification` and worktree `../pegasus-worktrees/upstream-intk-031-issuer-identification` from `origin/dev`.
2. **Operator step.** `/corpus/` is gitignored (`.gitignore:2`) and is **not present in this checkout** — confirmed 2026-08-24. The operator must place the immutable audit corpus on the workstation and confirm the path before the survey starts, and must confirm that the survey may read it in place. Evidence handed back: the corpus root path and a count of audit instructions available. Nothing from it is ever committed.
3. Read the current single grammar in full before surveying anything: `QdosMailClassificationPolicy.EvaluateStandaloneAuditReport` (`src/Pegasus.Core/Intake/DirectProviders/Qdos/QdosMailClassificationPolicy.cs:180-215`) and `ContainsRepairable` / `ContainsTotalLoss` (`:223-229`). Record in `research` the exact conditions under which it returns `null`, and the fact that the caller cannot tell "not an audit" from "outcome unreadable".
4. Survey the corpus across **every** principal that sends audits, not only QDOS. For each audit instruction take the non-instruction document attachment as the original report and label it by issuing firm from the report's own content — letterhead, footer, issuer block — never from the file name and never from which principal forwarded it.
5. Record in `research`, per issuer: the layout tells that identify it; which facts its reports carry (vehicle, registration, speedo, make/model, colour, VIN) and where they sit; **and where and in what words that issuer states Repairable versus Total Loss**. This last column is the one the reference prefix depends on and is the reason the bar is higher here than for the other facts.
6. Record the counts the upstream Verification asks for: audit instructions found, issuers identified, count per issuer, which principal each arrived via, and which reports could not be attributed. Report the aggregate figures only — no corpus content, no excerpts.
7. Propose the issuer registry's **shape and location**, with the reason. The upstream Approach is binding on one point: it lives beside the shared extraction code (`src/Pegasus.Core/Intake/InstructionFieldExtraction.cs` and its neighbours) and **not** under `src/Pegasus.Core/Intake/DirectProviders/Qdos/`, because it is keyed by engineering firm rather than by principal. Show how issuer selection sits *ahead* of the existing grammar rather than becoming a second extractor — `AGENTS.md` § Simplicity rails treats a second owner of one question as a stop condition.
8. Specify the abstention contract precisely enough for the imported `upstream:INTK-032` to build on: what "no issuer matched" produces, what "issuer matched but outcome unreadable" produces, and how those differ from today's `null`. Do **not** design the operator-visible outcome here — that is INTK-032's, and its operator has deferred it.
9. Specify the unknown-issuer rule: an unrecognised issuer degrades to today's behaviour and must not fail the intake. Name the regression fact that would prove it.
10. **Re-expressed for the desktop world.** The upstream body assumes the extracted facts surface on the Razor Received-item page that [[DSK-05-26]]'s cut list deletes. State the same requirement against the surfaces that replace it and record it in `research`: the identified issuer travels on the extracted facts' **provenance**, so it reaches [[DSK-05-09]] and [[DSK-05-04]] through [[DSK-03-10]]'s detail payload as data — not as a second client-side inference — and adding it is an additive contract change that [[DSK-03-04]]'s OpenAPI snapshot and [[DSK-03-05]]'s generated client must be regenerated for. Name that consequence; do not make the change here.
11. Propose the follow-on tickets the survey justifies (the registry itself, the per-issuer rules, the provenance field) with a one-line scope each, and record any question the survey could not answer as an `open-questions` entry rather than guessing it.
12. Make the two documentation changes this ticket owns and no others. (a) Add the `docs/capabilities.md` row named under § Documentation changes. (b) Annotate row `INTK-031` at `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:157` with this fork ticket id. **Check first, and change nothing else in that file**: [[DSK-01-09]] owns the § Disposition categories sentence — its step 15(e) reads "add a sentence to § Disposition categories restricting `unchanged-backlog` to rows that **have** a `docs/capabilities.md` row", made once in the same edit as its four other changes to this document. Read § Disposition categories before touching the file; if that sentence is already there, cite it and write nothing; if it is not there, still write nothing — record in `research` that [[DSK-01-09]] has not run yet and leave the sentence to it. Then write the `research` document and move to Done through `kanmer-verify`. There is no branch diff to simplify unless a prototype test was written; if one was, run the simplification pass over it and record it under a dated `## Simplification pass` heading.

## Acceptance criteria

- [ ] `research` records the corpus survey: audit instructions found, issuers identified, count per issuer, which principal each arrived via, and which reports could not be attributed — as aggregate figures, with no corpus content or excerpts committed.
- [ ] `research` records, per issuer, how Repairable and Total Loss are stated, including the wording each firm uses.
- [ ] The proposed registry is keyed by engineering firm, sits beside the shared extraction code and **not** under `Intake/DirectProviders/Qdos/`, and adds issuer selection ahead of the existing grammar rather than a second extractor.
- [ ] The abstention contract is specified precisely enough for the imported `upstream:INTK-032` to build on, and distinguishes "no issuer matched" from "issuer matched, outcome unreadable" from today's undifferentiated `null`.
- [ ] The unknown-issuer rule is specified as degrade-to-current-behaviour, never fail-the-intake, with the regression fact named.
- [ ] The desktop consequence is recorded: the issuer reaches [[DSK-05-09]] as provenance data through [[DSK-03-10]], and the OpenAPI snapshot and generated client must be regenerated when it is added.
- [ ] No corpus content, excerpt or file is added to the repository.
- [ ] The only edit this ticket makes to `upstream-kanmer-carryover.md` is the row `INTK-031` annotation. The § Disposition categories sentence restricting `unchanged-backlog` to rows that **have** a `docs/capabilities.md` row is **cited, not written** — [[DSK-01-09]] step 15(e) is its single owner.

## Verification

- [ ] `git status --porcelain` — expected: no file under `corpus/` staged or tracked; `.gitignore:2` still ignores it.
- [ ] `research` document review by `pegasus-desktop-reviewer` — expected: every per-issuer claim cites where in the corpus it was observed, by count and location, without reproducing content.
- [ ] `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --filter "FullyQualifiedName~Qdos"` — expected: green; run only if a prototype extraction fact was added, to prove no regression against current extraction.

## Evidence tier

Tier 8 — Genuine corpus. Tier 3 — Parser/adapter contracts.
Tier 8 obliges the immutable reviewed cohort read through the real material with field-level accuracy, conflicts and unattributable cases reported, and detailed evidence kept local and untracked. Tier 3 obliges the proposed contract to state its deterministic failure behaviour — here, abstention — rather than a best guess.

## Documentation changes

- `docs/capabilities.md` — add the row this requirement has never had, so `unchanged-backlog` is no longer the only register (its absence is why this ticket exists)
- `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md:157` — annotate row `INTK-031` with this fork ticket id. **The § Disposition categories sentence is not written here**: the sentence restricting `unchanged-backlog` to rows that **have** a `docs/capabilities.md` row is owned by [[DSK-01-09]] step 15(e), which makes it once in a single edit to this document. This ticket cites that sentence as the reason it exists (§ Why) and must never write a second copy — two tickets writing one sentence produces a duplicate or a conflict
- `docs/principal-rules-and-mappings/` — only if the survey proposes a per-issuer record there; decide and record, do not assume

## Guardrails

- **Azure**: no write. No Azure resource is involved; the corpus is local.
- **Scope boundary**: this is a survey. It may **read** anything under `src/Pegasus.Core/Intake/`, `src/Pegasus.Infrastructure/Intake/` and the operator-supplied `corpus/`, and it may write the ticket's `research` and `open-questions` documents plus the two documentation targets named under § Documentation changes — the new `docs/capabilities.md` row and the `INTK-031` row annotation. It must **not** edit § Disposition categories of `upstream-kanmer-carryover.md` ([[DSK-01-09]] step 15(e) owns that sentence), change extraction behaviour, add the registry, or touch any desktop project — those are the follow-on tickets this spike proposes.
- **Unblocks / blocked by**: this spike **blocks no seeded board ticket** — stated deliberately rather than left blank: [[DSK-05-09]] and [[DSK-03-10]] render and project extracted facts but neither asserts anything about how they were produced, and adding an issuer to provenance later is an additive contract change. What it does block is the imported `upstream:INTK-032`, whose fail-closed rule cannot be designed before the abstention contract exists. It is **blocked by** [[DSK-01-10]], the first one-way upstream sync, and by the operator supplying the corpus (step 2).
- **Traps**: the § Disposition categories sentence restricting `unchanged-backlog` to rows that have a `docs/capabilities.md` row has exactly one owner, [[DSK-01-09]] step 15(e) — this ticket cites it and never writes it; `corpus/` is immutable and untracked — committing content or an excerpt is a defect, and `docs/engineering.md` tier 1 requires the repository to prevent tracked corpus material. Do not key the registry by principal; the upstream body is explicit that it is keyed by firm and that another principal may forward the same firm's report. Do not build a second extractor. Do not design INTK-032's operator-visible state here. Audit + inspection is **out of scope**; audits only.
- **Simplification pass** (`AGENTS.md` step 4): `n/a — research-only`, unless a prototype test is written, in which case it is required over that diff and recorded under a dated `## Simplification pass` heading.

## Outcome

_Filled at closeout._
