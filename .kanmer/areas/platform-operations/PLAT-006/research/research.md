# Research — PLAT-006

## Question

Prove that the `/api/v1` upload-session and byte endpoints enforce `IntakeEnvelopeLimits` and the reader resource limits **before** anything reaches Core, that path traversal and unsafe filenames cannot escape their folder, and that content sniffing is refused.

## Findings

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

## Implications for this ticket

Proposal §17.3 names "malicious or malformed attachment" as a meaningful threat and §22.2 `:1615-1616` lists malformed uploads and unsafe file paths as security tests. The web Upload path already enforces its bounds (`src/Pegasus.Core/Intake/IntakeContracts.cs:7-57` and the `FormOptions` limit at `src/Pegasus.Web/Program.cs:527-533`), but the desktop moves uploading onto new gateway endpoints (`DSK-03-10`, `DSK-03-11`) where those bounds have to be re-established rather than inherited. `docs/current-architecture.md:222-236` records that reader and resource limits are enforced before Core; that ordering is the property under test. Operator-visible consequence: an oversized or hostile attachment either crashes the gateway or lands as a case document with a filename that escapes its folder. Siblings: [[DSK-10-05]], [[DSK-10-07]], [[DSK-10-01]].

## Boundaries and assumptions

- **Azure**: no write. Blob interaction runs against Azurite in the local stack (L-02).
- **Scope boundary**: may add tests and fixtures under `tests/Pegasus.IntegrationTests`. Must not relax a limit to make a test pass, and must not edit `IntakeEnvelopeLimits` — a limit that is wrong is a separate decision. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`).
- **Traps**: applying `MaximumMailboxContentLength` (750 MiB) to a staff upload is the exact confusion the constant's remarks warn about — a 16.69 MB instruction was once refused outright by the inverse mistake; corpus material must never be used as a fixture (`docs/runbook.md` § Corpus safety and evaluation); asserting only the HTTP status hides a refusal that happened *after* Core already wrote something.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document.

## Research conclusion

The ticket evidence identifies the target, routing and verification. It does not create or link a planned canonical governing document; `docs_todo` remains accurate until one exists.
