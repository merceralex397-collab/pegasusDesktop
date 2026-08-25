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
