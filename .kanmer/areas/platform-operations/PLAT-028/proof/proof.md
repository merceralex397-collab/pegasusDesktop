# Proof — PLAT-028

## Result

PLAT-028's behavior-preserving inline-image consolidation is merged and present on the configured remote's `main`. No deletion candidate was removed without a live caller/contract disposition, and the out-of-scope Razor page model was untouched.

## Merged delivery evidence

- PR: [#2](https://github.com/merceralex397-collab/pegasusDesktop/pull/2)
- PR head: `9f582036c5d304bfeea441ffb30415f71274c699`
- Merge commit: `bbc2d8f5b10815d1744ad510c6508de975958eff`
- Exact GitHub Actions run: `32879509769`; all applicable repository-check lanes passed. The infrastructure lane was skipped by its path filter.
- Read-only remote check on 2026-08-26:
  - `origin/main = 3b1737de2a27f84aa1bea03bf2c34d41d5a8006a`
  - `origin/dev = 3b1737de2a27f84aa1bea03bf2c34d41d5a8006a`
  - `git merge-base --is-ancestor 9f582036c5d304bfeea441ffb30415f71274c699 origin/main` passed.
  - `git show origin/main:src/Pegasus.Infrastructure/Intake/MimeKitPdfPigOpenXmlIntakeSourceReader.cs` contains the shared `IsInlineImage` helper and EML call; the `.DocMsg.cs` file calls the same helper.

## Validation evidence

The exact branch validation recorded before merge passed:

- `dotnet restore ./Pegasus.slnx --locked-mode`
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore` — 0 warnings, 0 errors
- Core tests — 916/916 passed
- Integration tests — 920 passed, 18 skipped, 0 failed, 938 total
- Architecture tests — 99/99 passed
- `pwsh ./scripts/Test-PegasusPlatform.ps1`
- `git diff --check`
- Focused EML/DOC/MSG inline-image contract check — 2/2 passed
- `git diff --stat origin/dev...HEAD` and name-only checks showed the two intended intake-reader partials only; no `src/Pegasus.Web/Pages/` file was changed.

The earlier broad filtered hosted invocation was canceled and is not claimed as a pass.

## Review and boundaries

- Independent review was completed by an agent other than the implementer; findings were addressed in the ticket plan and before merge.
- The simplification pass is recorded in the plan, including the direct-pass substitution because the code-simplifier agent was unavailable.
- No Azure/cloud/deployment, mailbox, Box, upstream, credential, or destructive write was performed.
- The current main checkout had a pre-existing user-owned modification in `tests/Pegasus.IntegrationTests/VehicleWorkflowTerminalTests.cs`; it was preserved and not included in this ticket.

## Closeout conclusion

Acceptance criteria and required review, CI, merge, ancestry, and merged-source checks are satisfied for PLAT-028.
