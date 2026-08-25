# Research — PLAT-028

## Question

Determine which parts of the upstream PLAT-032 duplicate-route sweep still exist in the desktop fork, which are safe to remove or consolidate, and which are intentional boundaries or live contracts that must remain.

## Evidence checked

- `src/Pegasus.Infrastructure/DependencyInjection.cs` registers `IIntakeArtifactStore` for staged intake/quarantine and `IDocumentContentStore` for durable case-document content. Local and production registrations use distinct implementations and tests assert the service registrations. These are separate routes, not duplicate business ownership.
- `src/Pegasus.Core/Intake/InstructionEvidenceImages.cs` is the documented owner of the evidence-image selection rule. `src/Pegasus.Infrastructure/Persistence/EfIntakeReceiptStore.cs` implements `ICaseEvidenceImageQueries` and retains both case-document occurrence projection and the legacy receipt-asset fallback.
- `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` currently injects and calls `ICaseEvidenceImageQueries.ListForCaseAsync`. The current caller is real; deletion would be premature. The desktop endpoint map and DSK-03-07/DSK-03-10 planning do not yet establish a replacement owner for the fallback.
- `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs` has guarded and unguarded custody method pairs. The guarded methods perform the lease check immediately before remote mutation, while `LocalCaseCustody` uses the default guarded interface wrappers. The pairs are intentional and must remain.
- `src/Pegasus.Infrastructure/Intake/MimeKitPdfPigOpenXmlIntakeSourceReader.cs` and its `.DocMsg.cs` partial both compute inline-image classification. The predicates are duplicate policy in one partial class and can share one private helper without changing EML or DOC/MSG semantics.
- `src/Pegasus.Core/Intake/RetainedMailFolderMove.cs`, `src/Pegasus.Infrastructure/Persistence/EfRetainedMailFolderMoveStore.cs`, and `src/Pegasus.Web/Pages/Mail/Message.cshtml` show that all four expected concurrency fields on `RetainedMailFolderMoveResult` are used in uncertain-move recovery. The current plan's premise that only `Outcome` is read is stale; the fields must remain.
- The large Razor mail page named in the upstream roster is explicitly out of scope because the desktop conversion replaces it through DSK-05-10, DSK-03-12, and DSK-05-26. No Razor-page replacement is part of this ticket.

## Disposition

1. Keep the two content-store routes; they represent different lifecycle boundaries.
2. Keep `InstructionEvidenceImages`, `ICaseEvidenceImageQueries`, its registration, and its current fallback until an owning desktop contract explicitly replaces the live caller.
3. Keep the Box guarded/unguarded custody pairs and preserve the immediate `CustodyEffectLeaseGuard.RequireCurrentAsync` check before each remote mutation.
4. Consolidate only the duplicate inline-image predicate into a shared helper in the existing partial class. Preserve the explicit-attachment exclusion for EML and the existing DOC/MSG attachment signals.
5. Keep all `RetainedMailFolderMoveResult` recovery/concurrency fields; correct the plan premise rather than deleting a live recovery contract.
6. Do not touch `src/Pegasus.Web/Pages/`, Worker code, API routes/contracts, Azure, mailbox, Box, or unrelated cleanup.

## Conclusion

The only safe implementation change identified by this sweep is the behavior-preserving inline-image helper extraction. The remaining roster items are intentional boundaries or live callers and are documented as no-change findings.
