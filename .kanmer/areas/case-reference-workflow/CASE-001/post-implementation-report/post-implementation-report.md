# Post-implementation report — CASE-001

## Summary

The automatic allocation path now observes image completeness from the receipt's retained evidence assets through the existing `InstructionEvidenceImages` owner. Image-free automatic cases are persisted as `NotReady` with their existing scheduled chase, while attached photographs still allow `Review`; inline images, undersized embedded images, and letterhead-shaped banners remain excluded. The live upstream check showed the fix had not arrived, so this fork carries the scoped implementation.

## Changes

| File | Change | Why |
|---|---|---|
| `src/Pegasus.Core/Intake/IntakeAllocation.cs` | Removed the asserted `AutomaticCompleteness` constant and built `CaseCompleteness` at the existing automatic-allocation call site using `InstructionEvidenceImages.Select(receipt.AssetRecords)` | Make `ImagesComplete` truthful without a second image rule or extra query |
| `tests/Pegasus.Core.Tests/Intake/AllocateDefinitiveIntakeTests.cs` | Added real-path assertions for no photographs, attached photographs, inline body images, under-floor embedded images, and letterhead banners | Prove the changed caller wiring and the required positive/negative cases |
| `tests/Pegasus.IntegrationTests/QdosAllocationRecoveryTests.cs` | Added the LocalDB NotReady/chase assertion, extended the existing receipt fixture with optional assets, and corrected the pending replay fixture's expected observed completeness | Prove persisted lifecycle state and keep idempotent replay material consistent with the new command |

## Governing docs

This implements the case readiness behavior owned by `docs/frd/frd-01-case-identity-and-lifecycle.md` and the operator truth that Not ready means missing material, usually images or instructions, while Ready means ready to enter EVA. No governing document was changed and no new design decision was introduced. The corrected flag remains in Core and reaches desktop consumers only through their existing gateway paths; no desktop UI or duplicate policy was added.

## Risks / follow-ups

- A later receipt carrying photographs does not recompute the allocation-time completeness flag; that existing grouped image-intake behavior remains intentionally outside this diff and is covered by the grouped-intake tests.
- The due-work reason remains the existing aggregate `Details are incomplete`; naming missing images is a separate follow-up.
- The stale screen-spec claim that CASE-021 is absorbed is owned by [[DSK-03-07]] coordinated with [[DSK-06-13]], not this ticket.
- No Azure, mailbox, Box, deployment, or release write was performed.

## Verification hand-off

After merge, `kanmer-verify` should run on merged `main` (or the repository's merged delivery SHA):

- `dotnet restore ./Pegasus.slnx`
- `dotnet build ./Pegasus.slnx --configuration Release`
- `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release`
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser"`
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release`
- confirm the merged diff remains limited to the three scoped files and no runtime/deployment proof is inferred from local tests.

## Independent review correction and revalidation — 2026-08-25

The independent review identified that the earlier report overstated the grouped-intake coverage: the existing grouped tests did not exercise a later photograph arriving after an instruction case was allocated. That gap is now closed by `QdosAllocationRecoveryTests.PhotographsArrivingAfterAllocationDoNotRewriteAllocationCompleteness`.

The new LocalDB fact allocates an image-free instruction, verifies `NotReady` and `ImagesComplete=false`, processes a later photograph through the existing upload/Worker automation path, verifies the image receipt is registered and associated with the case, and verifies the case remains image-incomplete and staff-unconfirmed. The inaccurate production comment about the former four fields was also corrected.

Validation after the correction:

- focused later-receipt integration fact: 1/1 passed;
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore`: passed, 0 warnings/errors;
- `dotnet test ./tests/Pegasus.Core.Tests/Pegasus.Core.Tests.csproj --configuration Release --no-build`: 921/921 passed;
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-build`: 99/99 passed;
- full non-corpus/non-browser integration: 873 passed, 3 skipped, 876 total;
- `git diff --check`: passed.

The original risk remains intentional: later photographs do not rewrite allocation-time completeness; staff confirmation is the existing route out. No Azure, mailbox, Box, deployment, or release write was performed.

## Final independent review and PR handoff — 2026-08-25

Halley's independent re-review of `995bf671` passed after the later-receipt test and historical comment correction. The ticket files map was reconciled to document the simplification move into `AllocateDefinitiveIntakeTests.cs`; the CASE-013 policy guard remains unchanged.

The branch is pushed, but `gh pr create --base dev --head case-001-observed-images` returned exactly `pull request create failed: GraphQL: must be a collaborator (createPullRequest)`. Therefore there is no PR, CI result, merge, proof, or Kanmer closeout evidence yet.

## Test-evidence strengthening — 2026-08-25

Independent test review required two missing assertions. Commit `d0604850` changes only the mapped test files:

- `AllocateDefinitiveIntakeTests.cs`: the existing real-path helper proves the unchanged instruction and staff-confirmation fields alongside observed image completeness.
- `QdosAllocationRecoveryTests.cs`: after a later image receipt, the existing workflow query proves due work remains scheduled and retains a next-chase time.

Fresh evidence: Release build passed with 0 warnings/errors; focused Core 12/12 and focused LocalDB 1/1 passed; CI-equivalent integration shards passed 876 enumerated tests exactly once (873 passed, 3 skipped) with three TRX files; partition verification passed; architecture tests passed 99/99; full Core passed 921/921 on its idle rerun. A concurrent-load Core attempt had two unrelated regex timeouts and is retained in the plan as honest timing-sensitive evidence.

## Independent review — 2026-08-25

Independent reviewer: Chandrasekhar. Verdict: implementation scope, Core ownership, planned behavior, local test evidence, and simplification disposition all pass. The reviewer independently reran Core 921/921, architecture 99/99, focused CASE-001 integration 2/2, and non-browser/non-corpus integration 873 passed / 3 skipped / 876 total.

Merge remains blocked: PR [#4](https://github.com/merceralex397-collab/pegasusDesktop/pull/4) has no status checks and GitHub reports zero registered Actions workflows. The repository rule requires green CI. Required next action: restore/register CI with owner/admin authority, run it against `d0604850fe0726a8debf955db810d7231866286f`, then attach the green result before merge. No proof or closeout is claimed.

## Durable legacy replay correction — 2026-08-25

Independent review found that durable automatic attempts created before observed image completeness could retain ImagesComplete: true; replay after this change computes false and would otherwise fail the existing operation-hash conflict guard. The correction is limited to the existing persistence store and recovery tests:

- pending legacy attempts are aligned to the current observed completeness/hash before normal acceptance resumes;
- failed legacy attempts replay their recorded failed state as suppressed;
- every other persisted command field and actor/operation identity must match;
- staff retry semantics and unrelated operation conflicts remain unchanged.

Validation: Release Web build passed with 0 warnings/errors; Core 921/921 passed; QdosAllocationRecoveryTests 20/20 passed; git diff --check passed. No cloud, mailbox, Box, deployment, or upstream write was performed. Independent review of this amendment is still required before merge.
