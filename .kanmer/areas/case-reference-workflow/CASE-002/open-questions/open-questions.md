# Open questions — CASE-002

- [ ] Confirm the accepted byte limits for the materialising anonymous upload path: per-file, per-request, and maximum file count. The proposal's 250 MB per file / 1 GB per request / 50 files values are not operator-confirmed in the live ticket, and the route currently materialises the upload in `MemoryStream` plus `byte[]` before custody. No value may be selected by assumption.
- [ ] Confirm whether the product should retain this materialising path or require a streaming change before accepting the proposed large limits. The current route copies `IFormFile` to memory before `IDocumentContentStore.StoreAsync`; Microsoft Learn's upload guidance distinguishes this buffering path from streaming for large uploads. The choice changes the implementation scope and the safe ceiling.

## Parked (explicitly deferred)

None.
