# Research — ENG-002: upstream:ENG-015 · Export the field values EVA expects

## Question

Correct four of the thirteen exported EVA values in the mapping layer: `Reference` carries the work provider's claim number instead of our case reference, `Inspection Address` is a six-line block instead of one line, `Vehicle Model` carries the make with the model on every branch, and the `Mileage Unit` casing (`Miles`/`Km`) is settled and recorded. The key set and key order do not change.

## Evidence examined

- **Upstream provenance** — upstream ticket `ENG-015`, upstream area `engineering-assessment`, upstream status `backlog`, upstream profile `fix`, upstream labels `qdos26015`, `eva`, `export`, `found-during-qa`, `operator-reported`; upstream links: none; upstream refs `docs/frd/frd-07-eva-and-external-engineering-handoff.md`. Read on **2026-08-24** from the read-only clone of `collisionengineers/pegasus` branch `kanmer-board` at clone commit **`a5b28111`**. No upstream branch, no upstream PR, no pipeline documents in the upstream ticket folder — so none are copied here.
- This ticket's profile on the fork board is **`feature`**, not upstream's `fix`: the six-line address block and the claim-number source are new mapping behaviour with a governing-document consequence, so `research`, `files`, `plan` and `checklist` are owed before implementation. Nothing else about the upstream ticket is reinterpreted.
- The 2026-08-23 carry-over register does **not** hold ENG-015: `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` § Triage table carries 109 rows and this ticket was opened 2026-08-24. It therefore has no triage disposition; [[DSK-01-09]] adds the row.

## Scope and constraints

The values are wrong in code the desktop **reuses unchanged**: `docs/desktop/05-implementation-and-migration/reuse-map.md:35` and `:65` mark `Eva/` (`EvaBundleSchema`, `CaseEvaMapping`) REUSE server-side into slice S15, and the mapping itself lives in `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs`, which no conversion ticket rewrites. Whatever the Razor app exports today, the native client exports identically — including the `ap.QDOS26015` package EVA refused on 2026-08-24.

**No seeded conversion ticket is permitted to fix it, and none asserts bundle content.** [[DSK-05-15]] (FEAT-015) is the only board ticket that touches EVA, and its scope boundary reads: "Must not change `src/Pegasus.Core/Eva/EvaBundleSchema.cs`, `CaseEvaMapping` or `EvaHandoffStore.cs` — this ticket asserts the bundle's content, upstream `ENG-014` and `ENG-015` fix it." Every other plan artefact describing the EVA handoff is plumbing-only: `vertical-slices.md` § S15 says "generate/download as explicit commands", `endpoint-map.md:72` gives the two routes, and parity row `PAR-18` (`parity-matrix.md:63`) records "EVA bundle download with reason; frozen revisions". Not one of them asserts that the archive EVA receives is one EVA will accept.

Unlike its sibling, nothing brings this in by sync: upstream ENG-015 is at `backlog` with **no branch and no assignee**, so there is no upstream commit to merge. Under **D-001** the fork becomes the single release source and upstream freezes after one more merge — this work exists only if the fork board holds it.

One of the four is already decided and needs no re-deciding: operator direction of 2026-08-24 is to emit the work provider's claim number in `Reference`. `reference/eva_information/eva_information.md:35` and `:39` are the authority — `Case/Po` is "our reference… manually created by an admin worker", `Claim no` is "'Their' ref - ie the work providers reference" — and all the known-good samples put the provider's reference in the JSON `Reference` key (`AX_SP58WVO.json:6` = `1070277`; `Final Format Example 02.json:6` = `SBL-B0492438`).

Operator-visible consequence: an engineer's hand-off is refused by EVA, or is accepted carrying the wrong reference and a model with no make, so the external record cannot be joined back to the work provider's own claim.

- Future owner follows the ticket’s stated project boundary and repository task workflow. Reuse existing Core policy/ports before adding any abstraction.

- **Azure**: no write, and no Azure read — this ticket has no cloud surface. Verification is the local stack and the retained corpus under **L-02**.
- **Scope boundary**: may touch `src/Pegasus.Infrastructure/Persistence/EvaHandoffStore.cs` (the mapping only), `src/Pegasus.Core/Eva/CaseEvaMapping.cs`, the EVA and QDOS test files, and `docs/frd/frd-07-eva-and-external-engineering-handoff.md`. Must **not** change `CaseEvaMapping.ImageBasedAssessment` or `src/Pegasus.Core/Address/Ext18InspectionAddressPolicy.cs:12` (both are comparison targets for the case's stored value), the thirteen keys or their order, `EvaBundleSchema`'s packaging (the imported `upstream:ENG-014` owns it), any Razor page model, or any desktop project.
- **Blocking**: this **unblocks** [[DSK-05-15]] (FEAT-015), whose thirteen-field acceptance criterion cannot pass while four values are wrong, and through it [[DSK-05-22]], [[DSK-05-25]], [[DSK-07-18]], [[DSK-08-08]] and [[DSK-08-16]]. It is **blocked by** the imported `upstream:ENG-014` (sequence ENG-014 then ENG-015 so the archive bytes change once) and follows [[DSK-01-10]]'s sync. [[DSK-01-09]] (FND-022) assigns its phase; do not invent one here.
- **Open questions carried from upstream** (recorded here, not answered in code): (a) **Mileage Unit casing and CRLF** — the samples show `Miles`/`Km` and CRLF but may be predecessor artefacts; step 8 settles it by an actual EVA import or the operator's answer. (b) **`Reference` fail-closed behaviour** — step 4's operator decision; this one is new to the fork and must not be skipped. (c) **Accident Circumstances** — whether damage-area text should feed the key when no circumstances prose exists (`QdosInstructionExtractionPolicy.cs:317`/`:340`) is a business rule and is **out of scope**; raise its own ticket. (d) **Instruction Date** — the bare `Date:` label absent from `QdosInstructionExtractionPolicy.cs:49-51` makes every QDOS case default to the receipt date; **out of scope**, likely an intake ticket. (e) **VAT Status** — confirm with the operator that it is meant to be staff-entered rather than derived; recording the answer is enough, no code follows.
- **Traps**: the fork is behind upstream, so every line number in the upstream body is upstream's — re-derive them. The `Reference` change makes a previously-always-present value optional; that is the one place this ticket can break generation for a real case. `NormalizeValue`'s `.Trim()` will silently eat the address padding unless the exemption is explicit. The samples are evidence of shape, not a specification — the casing question is settled by an import, not by copying.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Governing documents

- `docs/frd/frd-07-eva-and-external-engineering-handoff.md`

## Planning implication

Reuse the cited boundaries and revalidate the named sources against current `origin/dev` after the ticket is taken. Do not create a compatibility path, duplicate policy, or an unapproved external write.
