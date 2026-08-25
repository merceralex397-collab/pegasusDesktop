# Files

| Path | Change |
| --- | --- |
| `src/Pegasus.Core/Intake/IntakeAllocation.cs` | Delete the `AutomaticCompleteness` constant (`:224-228`); build the record at the call site (`:269`) with `ImagesComplete` observed from the receipt's own assets. Keep the CASE-013 warning in the doc comment and say which half is now observed. |
| `tests/Pegasus.Core.Tests/Intake/AllocateDefinitiveIntakeTests.cs` | Caller-wiring facts drive `AttemptAutomaticAsync`: no photographs → incomplete; one photograph → complete; inline/undersized embedded images and a letterhead banner → incomplete. Reuses this class's existing receipt and acceptance helpers. |
| `tests/Pegasus.IntegrationTests/QdosAllocationRecoveryTests.cs` | End-to-end LocalDB assertions that an automatic allocation from a receipt with no photographs lands in `NotReady` with its chase scheduled, not `Review`, and that a later photograph receipt associates without rewriting allocation-time completeness. |

**Reused, not written:** `InstructionEvidenceImages.Select` — already the single Core
owner of "which retained assets are this case's photographs", already what custody
uses to decide which assets become `DocumentSemanticRole.Image` documents, which is
exactly the population the export then counts. Asking it at allocation makes Review
agree with export by construction rather than by a second rule.

The original upstream map named `AutomaticCaseReadinessTests.cs` for the caller-wiring
facts. The simplification pass moved those facts to
`AllocateDefinitiveIntakeTests.cs`, where the real allocation caller and existing
test helpers live; the pure CASE-013 policy guards remain in
`AutomaticCaseReadinessTests.cs` unchanged.

**No document change.** FRD-01 and `operator-notes.md` already state the behaviour;
this makes the code match them.

## Deliberately not in this diff

`EfQueuedCustodyProcessor.cs:309-312` files every `image/*` attachment as
`DocumentSemanticRole.Image` using `IsImage` alone, without `IsPhotographShaped`.
So a letterhead banner becomes export-eligible while the gallery excludes it. After
this fix a receipt carrying only a banner would be `ImagesComplete: false` yet still
export — a narrower disagreement in the opposite direction. Real, but folding it in
widens the diff and risks the export side. Filed separately.
