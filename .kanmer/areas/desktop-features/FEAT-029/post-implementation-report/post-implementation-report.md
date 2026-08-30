# Post-implementation report — FEAT-029

## Scope delivered

The existing `Pegasus.Web` Desktop Gateway now exposes the retained-mail API contract in one `/api/v1/mail` group: retained list, Deleted Items search, preview, detail, link/unlink prepare and confirmation, classification correction, and recommended-folder move. The handlers call the existing Core use cases and ports; no business policy or provider implementation was added. Response DTOs contain Pegasus-owned projections only.

The Kiota generator, pinned tool and generated client remain the explicit ownership of [[GWY-005]]. Its documented prerequisites are [[FND-031]] and [[GWY-004]]. This ticket regenerated the OpenAPI input and did not create a second client tree or script.

## Acceptance evidence

- Authorization: every implemented mail route is behind the desktop gateway composition, `RequireAuthorization`, and `PerformCasework`; dedicated tests cover gateway-off, unauthenticated and wrong-role responses.
- Concurrency: link/unlink prepare and confirmation carry receipt/case versions, lease token, operation key and reason; classification carries the expected classification version; folder move carries classification/recommendation/mailbox versions and bare-GUID key validation.
- Provider behavior: detail projects Core's complete folder recommendation; unavailable composition has `CanMove=false` and no suggested move; available composition exposes the move capability. Move success/failure/uncertain messages remain distinct.
- Deleted Items: the route uses `SearchDeletedMail`, preserves its 100-item cap and state/truncation projection, and has no mutation route.
- Security: contract tests inspect detail and deleted responses for credential/raw-provider leakage; no Graph token, mailbox secret, connection string or raw provider JSON is projected.
- Parity: `DesktopGatewayMailTests.ApiLinkConfirmHasTheSameCoreAssociationEffectAsTheRazorHandler` runs equivalent SQL-backed link-confirm flows through Razor and the API and compares association, event type, reason and before/expected/after intake and case versions.

## Commands and results

- `dotnet restore ./Pegasus.slnx --nologo` — passed; restored six missing clean-worktree project assets.
- `dotnet build src/Pegasus.Web/Pegasus.Web.csproj --configuration Release --no-restore -nr:false --nologo` — passed, 0 warnings, 0 errors.
- `pwsh ./eng/api/Export-OpenApiDocument.ps1` — passed after rebuilding the Web host.
- `dotnet test tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --no-restore -nr:false --nologo` — passed 62/62.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore -nr:false --filter "FullyQualifiedName~MailWorkspaceWebTests" --nologo` — passed 39/39.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore -nr:false --filter "FullyQualifiedName~DesktopGatewayMailTests" --nologo` — passed 1/1.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false --nologo` — passed, 0 warnings, 0 errors, after the solution restore.
- `git diff --stat origin/dev -- src/Pegasus.Worker src/Pegasus.Infrastructure/Email src/Pegasus.Web/Pages/Mail` — empty output; guarded scope remains untouched.
- The exact required broad command is currently running; its aggregate result will be appended before the ticket enters Review.

## Review handoff

The dated simplification pass is recorded in `plan`. The only intentional non-local deliverable is the GWY-005 generated-client handoff; the API snapshot and behavior are locally complete. No PR, merge, proof or Done transition has been claimed yet.

## Final verification update — 2026-08-30

The exact broad command `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser" --nologo` passed with 974 passed, 2 skipped, 0 failed, 976 total in 12m52s. The focused API contract suite passed 62/62, the existing `MailWorkspaceWebTests` passed 39/39, and `DesktopGatewayMailTests` passed 1/1. The full Release solution build passed with 0 warnings and 0 errors. The guarded scope command returned empty output. The OpenAPI snapshot test passed as part of the API contract suite.

The ticket is implementation/review-ready. It is not yet merged, verified on main, or closed. Kiota client generation remains the documented [[GWY-005]] deliverable; no duplicate generated tree was created here.

## Review correction update — 2026-08-30

Independent review found and the branch corrected two contract defects: the retained-list route no longer advertises two competing 200 schemas (Deleted Items is only `/mail/deleted`), and all four conditional mail GETs now declare 304. After correction: Web Release build passed with 0 warnings/0 errors; OpenAPI export passed; API contract tests passed 62/62; `DesktopGatewayMailTests` passed 1/1; `MailWorkspaceWebTests` passed 39/39. The exact broad verification command was reopened because these source changes occurred after its prior run and is being rerun now.

## Final-source verification — 2026-08-30

After the independent review fixes, the exact broad command was rerun against the final source: `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --filter "Category!=Corpus&Category!=Browser" --nologo` — passed 974, skipped 2, failed 0, total 976, duration 13m28s. Final focused checks remained green: API contract suite 62/62, `DesktopGatewayMailTests` 1/1, `MailWorkspaceWebTests` 39/39; full Release solution build passed with 0 warnings and 0 errors; OpenAPI export/snapshot passed; guarded-scope diff remained empty.

## Final review correction validation — 2026-08-30

The final independent review identified two P2 contract issues: missing conditional-read OpenAPI metadata for the four mail GET routes and nullable `OtherName`/`OtherReasoning` being incorrectly required in the classification request schema. Both were corrected. The shared operation transformer now adds `If-None-Match`, 304 and ETag metadata for `ListMail`, `SearchDeletedMail`, `PreviewMail`, and `GetMail`; the classification request constructor now defaults the two nullable details after its required fields. OpenAPI was regenerated and inspected to confirm the four mail GETs advertise the conditional contract and the classification schema requires only `expectedClassificationVersion`, `classificationKey`, `reason`, and `operationKey`. Post-correction checks: API mail contracts 15/15, `MailWorkspaceWebTests` 39/39, `DesktopGatewayMailTests` 1/1, Web Release build 0 warnings/0 errors, and the exact broad integration command 974 passed, 2 skipped, 0 failed, 976 total in 12m28s. A fresh final independent review is required after this correction before PR creation.

## Final review correction validation — 2026-08-30

The final independent review's only remaining P1 was the stale OpenAPI snapshot caused by adding current association versions to the read contract. Regenerated openapi/pegasus-v1.json with pwsh ./eng/api/Export-OpenApiDocument.ps1. The corrected read responses expose IntakeVersion and the current associated CaseVersion from the existing receipt/case-workflow records; no provider or credential data is exposed.

Validation against the final source: full API contract suite passed 62/62; MailWorkspaceWebTests passed 39/39; DesktopGatewayMailTests passed 1/1; full Release solution build passed with 0 warnings and 0 errors; exact command dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore -nr:false --filter "Category!=Corpus&Category!=Browser" --nologo passed 974, skipped 2, failed 0, total 976 in 12m45s. The guarded scope diff remained empty. The independent reviewer rechecked the corrected diff and reported no remaining design or behavior issue.
