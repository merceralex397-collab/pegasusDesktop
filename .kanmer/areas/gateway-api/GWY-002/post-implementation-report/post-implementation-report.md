# Post-implementation report — GWY-002

## Delivered

- Added the gated `/api/v1` native-desktop route-group composition boundary in the existing `Pegasus.Web` host.
- Added shared correlation-id acceptance/generation/echo/logging and the named client-version extension filter.
- Added branch-scoped RFC 9457 problem handling for authorization, version, lease, operation, validation, cancellation, and generic maintenance outcomes. Known Core-derived `InvalidOperationException` types are matched before the validation fallback.
- Added the Web → Contracts project reference and kept the architecture dependency assertion explicit.
- Kept the group endpoint-free; later gateway tickets own authentication, authorization, and projections.
- Updated `docs/current-architecture.md` with the source-state gateway boundary and activation caveat.

## Test strategy

The plan's test-only startup-filter endpoint was not used. Because this ticket intentionally maps an empty production group, adding a test route would require a production-facing hook or alter composition. The documented fallback is implemented: direct handler tests cover every mapping branch, and WebApplicationFactory tests cover the real enabled/disabled composition and machine-surface behavior.

## Validation

- Commit: `6bf7a96c` (`Add desktop gateway composition boundary`).
- `dotnet build Pegasus.slnx -c Release -nr:false` — passed; 0 warnings, 0 errors.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGateway" -nr:false` — passed; 12 passed, 0 failed, 0 skipped.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release -nr:false` — passed; 950 passed, 16 skipped, 0 failed (966 total). Skips are the repository's expected absent local corpus cases.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj -c Release -nr:false` — passed; 110 passed, 0 failed, 0 skipped.
- Static checks — passed: exactly one `AddProblemDetails` registration; exactly one `Features:DesktopGateway` literal and one `/api/v1` literal, both in `DesktopGateway.cs`; `git diff --check` clean.

## Review and delivery handoff

The branch is pushed to the configured `origin` remote as `task/desktop-gateway-group`. The next required action is opening the PR to `dev`, obtaining independent review from an agent that did not implement the ticket, and waiting for exact-head CI before merging.
