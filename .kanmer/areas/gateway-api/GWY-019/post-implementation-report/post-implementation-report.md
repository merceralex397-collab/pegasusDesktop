# Post-implementation report — GWY-019

## Delivered

- Extracted the shared OpenIddict composition so Desktop can use `/connect/token` with Automation disabled while preserving Automation's client-credentials, authorization-code + PKCE, scopes, audience, lifetimes, and server-wide non-sliding refresh policy.
- Added the public `pegasus-desktop` registration with `pegasus.desktop` and `offline_access` permissions, password and refresh grants, idempotent reconciliation, and the existing registration kill-switch pattern.
- Added staff password and refresh handling using the existing Identity store, `CheckPasswordSignInAsync(..., lockoutOnFailure: false)`, role claims, security-stamp revalidation, rolling refreshes, and the 8-hour absolute-session claim cap.
- Replaced ephemeral token protection with Data Protection. Because the retained authorization-code flow still requires OpenIddict signing/encryption credentials, Development uses a user-scoped development certificate with subject `CN=Collision Engineers`; non-Development requires `OpenIddict:CertificatePath` and rejects a certificate whose subject does not exactly match. No certificate, private key, secret, Azure write, or deployment was added.
- Moved the shared original-issue claim owner to `DesktopSession` and added real request-pipeline access-token claim coverage.

## Validation

- `dotnet build tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore --nologo -v:minimal`: passed, 0 warnings, 0 errors.
- `dotnet test tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter "FullyQualifiedName~DesktopTokenIssuance|FullyQualifiedName~Automation" --logger "console;verbosity=minimal"`: passed 36/36, 0 failed, 0 skipped.
- `pwsh ./scripts/Test-MigrationGrants.ps1`: passed; 74 migration files checked.
- `git diff --check`: passed.
- `rg` confirms no `AddEphemeralEncryptionKey` or `AddEphemeralSigningKey` remains under `src/Pegasus.Web`.

## Boundaries and follow-up

- No application policy was duplicated and no `Mcp/*McpTools.cs`, Worker, infrastructure, cloud, or deployment files were changed.
- Production certificate issuance/trust rollout remains outside this ticket and is not claimed as completed; a non-Development host without its operator-provided certificate fails closed.
