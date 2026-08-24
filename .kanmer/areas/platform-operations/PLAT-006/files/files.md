# File map — PLAT-006

## Change surface

- `docs/desktop/10-security-observability-performance/threat-register.md` — record the test names against the malformed-attachment row.
- `None.` elsewhere.

## Context files and evidence

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-06`
- Plan detail: same file § 2 (Facts — "Reader/resource limits enforced before Core"), § 4 (target state)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 17.3 `:1184-1198`; § 22.2 Security tests `:1608-1621`; § 12 Integration design `:681-772` for what the byte endpoints carry
- Repository evidence:
  - `src/Pegasus.Core/Intake/IntakeContracts.cs:7-57` — `MaximumContentLength` 10 MiB per file, `MaximumBatchFileCount` 20, `MultipartOverhead` 64 KiB, `MaximumBatchContentLength`, `MaximumMailboxContentLength` 750 MiB (mailbox envelope only — do not apply it to a staff upload)
  - `src/Pegasus.Web/Program.cs:527-533` — `FormOptions.MultipartBodyLengthLimit = IntakeEnvelopeLimits.MaximumBatchContentLength`
  - `src/Pegasus.Web/Pages/Upload.cshtml.cs` — the existing staff upload handler whose refusals the new endpoints must match
  - `tests/Pegasus.Core.Tests/Intake/IntakeEnvelopeLimitsTests.cs` — the existing limit tests
  - `tests/Pegasus.IntegrationTests/MultiFormatIntakeWebTests.cs` — the fixture set `DSK-03-10` reuses
  - `docs/current-architecture.md:222-236` — reader and resource limits enforced before Core
  - New: upload-session endpoints from `DSK-03-11`; byte endpoints from `DSK-03-10`
- Binding decisions:
  - **L-01** — endpoints live in `Pegasus.Web`; no new deployment unit.
  - **L-02** — fixtures and tests run on the local stack.
- Depends on: `DSK-03-10` (byte endpoints for asset/image/source), `DSK-03-11` (upload-session stage/status/group endpoints and case document upload).

## Ripple effects and acceptance

- [ ] Per-file, per-batch and per-request limits are asserted at the exact constants in `IntakeEnvelopeLimits`, with the boundary and boundary+1 cases both present.
- [ ] Each refusal is proved to happen before Core: no intake record, staged artifact or action-history entry is written.
- [ ] Every unsafe filename case is refused or normalised, and nothing is written outside the intended folder.
- [ ] Byte endpoints send `Content-Length`, `ETag` and `nosniff`, and serve the safe stored name.
- [ ] An interrupted upload leaves no receipt and no orphaned artifact.

## Deliberately out of scope

- **Azure**: no write. Blob interaction runs against Azurite in the local stack (L-02).
- **Scope boundary**: may add tests and fixtures under `tests/Pegasus.IntegrationTests`. Must not relax a limit to make a test pass, and must not edit `IntakeEnvelopeLimits` — a limit that is wrong is a separate decision. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: applying `MaximumMailboxContentLength` (750 MiB) to a staff upload is the exact confusion the constant's remarks warn about — a 16.69 MB instruction was once refused outright by the inverse mistake; corpus material must never be used as a fixture (`docs/runbook.md` § Corpus safety and evaluation); asserting only the HTTP status hides a refusal that happened *after* Core already wrote something.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.
