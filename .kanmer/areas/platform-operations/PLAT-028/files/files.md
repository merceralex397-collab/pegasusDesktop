# Files — PLAT-028

## Files to inspect or change

| File | Role | Intended disposition |
| --- | --- | --- |
| `src/Pegasus.Infrastructure/Intake/MimeKitPdfPigOpenXmlIntakeSourceReader.cs` | EML/source-format image classification | Add the shared inline-image helper and call it from the existing EML path. |
| `src/Pegasus.Infrastructure/Intake/MimeKitPdfPigOpenXmlIntakeSourceReader.DocMsg.cs` | DOC/MSG image classification partial | Call the same helper, preserving current attachment behavior. |
| `tests/Pegasus.IntegrationTests/DocumentExtraction/` | Existing extraction characterization tests | Inspect and run the EML and DOC/MSG asset-classification coverage; change tests only if a verified gap is exposed. |
| `src/Pegasus.Infrastructure/DependencyInjection.cs` | Store and evidence-image composition | Read-only context; retain distinct registrations and `ICaseEvidenceImageQueries`. |
| `src/Pegasus.Infrastructure/Persistence/EfIntakeReceiptStore.cs` | Evidence-image query implementation | Read-only context; retain current and legacy fallback paths. |
| `src/Pegasus.Infrastructure/Custody/BoxCaseCustody.cs` | Guarded remote custody operations | Read-only context; retain guarded/unguarded pairs and immediate lease checks. |
| `src/Pegasus.Infrastructure/Custody/LocalCaseCustody.cs` | Local custody implementation | Read-only context; retain unguarded implementations and default guarded wrappers. |
| `src/Pegasus.Core/Intake/RetainedMailFolderMove.cs` | Move result contract | Read-only context; retain all recovery/concurrency fields. |
| `src/Pegasus.Infrastructure/Persistence/EfRetainedMailFolderMoveStore.cs` | Move result persistence mapping | Read-only context; retain mapping and concurrency hash inputs. |
| `src/Pegasus.Web/Pages/Cases/Details.cshtml.cs` and `src/Pegasus.Web/Pages/Mail/Message.cshtml` | Proof of live callers | Read-only evidence only; no Razor changes. |

## Ripple and ownership boundaries

- The helper is private to the existing `MimeKitPdfPigOpenXmlIntakeSourceReader` partial class; no new top-level abstraction or policy owner is introduced.
- Existing extraction tests remain the validation surface. No new test project, deployment unit, store, route, or compatibility path is allowed.
- PLAT-029 owns its separate local-profile document-content work; do not modify overlapping custody/content behavior for that ticket.
- INTK-002 owns broader intake duplication chores; this ticket is limited to the identified inline-image predicate.
- DSK-03-07/DSK-03-10/DSK-03-12 own future desktop contracts. This ticket does not preempt their API or UI decisions.
- The out-of-scope Razor page and all cloud/external resources remain untouched.

## Governing context

- `docs/desktop/03-gateway-api-and-data/endpoint-map.md` (documents, intake evidence, and retained-mail move record boundaries).
- `docs/desktop/03-gateway-api-and-data/README.md` (DSK-03-10/11/12 contract ownership).
- `docs/principal-rules-and-mappings/qdos.md` and `docs/current-architecture.md` (InstructionEvidenceImages ownership).
- `docs/desktop/05-implementation-and-migration/README.md` (replacement and sequencing boundaries).
