## Upstream ids versus board ids — read this before writing any id

The 19 imported carry-over tickets kept their **upstream** id in the title
(`upstream:<ID> · …`) but were allocated a **fork board** id from their area
prefix. The two numbering spaces overlap and **do not correspond**. Across both
boards 45 ids exist on both sides; the `PLAT` namespace alone puts 29 board ids
in collision.

**The rule, and it is absolute:**

> A bare `<PREFIX>-<nnn>` anywhere in a ticket body, document or checklist is a
> **fork board id**. An upstream id is **never** written bare — it is always
> written `upstream <ID>`, and where both are meant, `upstream <ID> (board
> [[<board-id>]])`.

This is not pedantry. `FND-022` step 7 once read "**Drop `CASE-001`**", meaning
upstream CASE-001 (moot with the Razor front end). Board `CASE-001` is a live
imported production defect that blocks four tickets, and `FND-022` is the ticket
authorised to act on the batch. A bare id in the wrong namespace is how the
board deletes real work.

### The join table — the 19 imports

| Board id | Upstream id | Title |
| --- | --- | --- |
| `AUTO-001` | upstream `AUTO-003` | Expose the completed email-workspace actions through the Automation Actor |
| `AUTO-002` | upstream `AUTO-008` | Measure and reduce durable intake processing latency |
| `CASE-001` | upstream `CASE-021` | Refuse Review for a case with no images |
| `CASE-002` | upstream `CASE-022` | Deliver public upload links (INT-31) to the accepted limits |
| `DOCS-001` | upstream `DOCS-001` | Trigger report generation from complete accepted assessments |
| `DOCS-002` | upstream `TICK-018` | DOC-02 — Store source emails, documents, correspondence and reports in Box |
| `DOCS-003` | upstream `TICK-208` | Preserve final Sent evidence through post-report correction |
| `DUI-017` | upstream `DELIV-006` | Capture the Claude Design screen map in the repository |
| `ENG-001` | upstream `ENG-014` | Stop producing the invented manifest and provenance files |
| `ENG-002` | upstream `ENG-015` | Export the field values EVA expects |
| `INTK-001` | upstream `INTK-002` | Intake duplication chores |
| `INTK-002` | upstream `INTK-003` | Recover dispatched intake work whose queue message never arrives |
| `INTK-003` | upstream `INTK-026` | Normalize kilometre case mileage to canonical miles |
| `INTK-004` | upstream `INTK-027` | Make policy re-evaluation work after transient staging cleanup |
| `INTK-005` | upstream `INTK-031` | Identify the third-party engineer behind an audit's original report |
| `INTK-006` | upstream `INTK-032` | Fall back safely when a third-party report format cannot be read |
| `INTK-007` | upstream `INTK-033` | A triage-request email creates no Triage and no Unidentified item |
| `PLAT-028` | upstream `PLAT-032` | Simplification and duplicate-route sweep across the codebase |
| `PLAT-029` | upstream `PLAT-038` | Serve intake-retained document content in the local profile |

**`DOCS-001` is the trap in this table.** Board and upstream ids happen to be
identical, so it reads as unambiguous when it is only coincidence. Write it
`upstream DOCS-001 (board [[DOCS-001]])` like every other row.

Note also that the seven `INTK` rows are shifted by one against their upstream
numbers for the first four, and not shifted at all after that — there is no
formula. Read the table; never compute the mapping.

### Where a bare upstream id is still correct

Inside an imported ticket's `### Upstream ticket <ID> (verbatim)` block. That
text is a quotation of the upstream body and is copied unedited — its ids are
upstream ids by definition, and the heading above it says so. Never "fix" ids
inside a verbatim block.
