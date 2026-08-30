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
