# Post-implementation report — GWY-003

## Result

Implemented the reusable per-group `StaffAccessRight` boundary for the desktop gateway. `StaffActorAccessor` is the single Web claims-to-actor call site beside the existing StaffPageModel call, rejects the Automation audience and non-staff actors before any right is consulted, records denial events through the existing security-event writer with the request correlation, and exposes the resolved staff actor through `DesktopGateway.ActorItemKey`. `RequireStaffRightFilter` delegates authorization to Core's existing `StaffAuthorization` owner. The existing GWY-021 account/session resolver remains first in the `/api/v1` group and now records account-disabled, invalid-stamp, and absolute-expiry token denials. No Azure, deployment, upstream, credential, schema, Worker, or cloud write was made.

## Files changed

- `src/Pegasus.Web/Api/DesktopGateway.cs`
- `src/Pegasus.Web/Api/DesktopGatewayExtensions.cs`
- `src/Pegasus.Web/Api/StaffActorAccessor.cs`
- `src/Pegasus.Web/Api/RequireStaffRightFilter.cs`
- `src/Pegasus.Web/Desktop/DesktopActorResolver.cs`
- `tests/Pegasus.IntegrationTests/IntakeWebTestSupport.cs`
- `tests/Pegasus.IntegrationTests/DesktopGatewayAuthorizationTests.cs`

## Validation

- `git diff --check` — passed.
- `dotnet restore ./Pegasus.slnx --locked-mode` — passed; all projects up to date.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false` — passed with 0 warnings and 0 errors.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~DesktopGatewayAuthorizationTests" --logger "console;verbosity=minimal" -nr:false -p:UseSharedCompilation=false` — passed 27/27 with 0 skipped. The matrix covers all twelve rights; the `ExecuteSystemWork` and `SubmitRequestUpload` permanent-refusal facts each exercise Administrator, Engineer, and User.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` — passed 1,075, failed 0, skipped 16, total 1,091. The 16 skips are existing local-corpus-gated tests; no GWY-003 test skipped.
- `rg -n "StaffActorFactory\\.TryCreate" src/Pegasus.Web` — exactly two call sites: `Pages/StaffPageModel.cs` and `Api/StaffActorAccessor.cs`.
- `rg -n 'pegasus-automation-mcp' src/Pegasus.Web -g '*.cs'` — exactly one literal, the `AutomationMcp.Audience` constant.
- The focused disabled-account and Automation-audience facts assert `Denied` token security events with the same correlation id returned in the problem response.

## Simplification pass — 2026-08-30

- **Reuse:** retained Core `StaffActorFactory`/`StaffAuthorization`, the existing OpenIddict claims, gateway correlation helper, problem mappings, security-event writer, and `IntakeWebApplicationFactory`.
- **Simplification:** added one accessor and one parameterized endpoint filter; no second parser, policy engine, cache, compatibility path, alternate limiter, or production test endpoint. `StaffAuthorization.Require` remains the single right decision and exception construction point.
- **Efficiency:** kept the required uncached per-request account/security-stamp read, resolved the actor once, and passed it through request items to the right filter without re-querying or re-resolving.
- **Altitude:** Web owns transport/session/actor-boundary enforcement; Core owns business authorization and state preconditions; integration tests exercise the composed gateway group and persisted denial evidence.
- **Disposition:** no unapplied behaviour-preserving simplification finding remains. The claim-shape deviation is intentional and documented in the plan: OpenIddict `Claims.Subject`/`Claims.Role` are authoritative for GWY-021 bearer tokens, with `ClaimTypes.NameIdentifier` fallback and role-claim union for supported test/cookie-shaped principals; Core's factory is still called exactly once.

## Review state

Cicero the 2nd independently reviewed the pre-final diff and confirmed the production design, scope, correlation/audit behavior, and simplification lenses. The review found one test evidence defect: the permanent-refusal rows did not explicitly exercise Engineer. That defect was fixed by keeping the 27-fact count and making both permanent-refusal facts issue and assert refusals for Administrator, Engineer, and User. A fresh independent review of the committed exact head is required before PR merge.

## Delivery state

The branch still requires commit/push, PR targeting `dev`, exact-head independent review, green applicable CI, merge to `dev`, exact-SHA non-force promotion to `main`, main CI, proof, and Kanmer closeout. Cloud/deployment/upstream writes remain out of scope.
