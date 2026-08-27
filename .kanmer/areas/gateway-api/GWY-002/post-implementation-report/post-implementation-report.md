# Post-implementation report — GWY-002

## Delivered

- Added the gated `/api/v1` native-desktop route-group composition boundary in the existing `Pegasus.Web` host.
- Added path-scoped correlation-id acceptance/generation/echo/logging and the named client-version extension filter.
- Added branch-scoped RFC 9457 problem handling for authorization, version, lease, operation, validation, cancellation, generic maintenance, and unmatched API 404 outcomes. Known Core-derived `InvalidOperationException` types are matched before the validation fallback.
- Added the Web → Contracts project reference and kept the architecture dependency assertion explicit.
- Kept the production group endpoint-free; later gateway tickets own authentication, authorization, and projections.
- Updated `docs/current-architecture.md` with the source-state gateway boundary and activation caveat.

## Final test strategy

The initial implementation at commit `6bf7a96c` used direct handler tests because the production group intentionally has no endpoint. Hilbert's independent review required host-level proof as well. Commit `63293de6` adds a test-only `IStartupFilter` middleware that throws only on test paths after the application pipeline, exercising the real path-scoped exception handler without adding a production route or hook. Direct handler tests remain branch-level coverage; WebApplicationFactory tests cover the real enabled/disabled composition, correlation, unmatched 404 problem response, and host-level exception handling.

## Validation at final head

- Exact head: `63293de6e44ffe048becb778e47ce9a0168c43e5`.
- `dotnet build Pegasus.slnx -c Release -nr:false` — passed; 0 warnings, 0 errors.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~DesktopGateway" -nr:false` — passed; 19 passed, 0 failed, 0 skipped.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj -c Release -nr:false` — passed; 958 passed, 16 skipped, 0 failed (974 total). Skips are the repository's expected absent local corpus cases.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj -c Release -nr:false` — passed; 110 passed, 0 failed, 0 skipped.
- Static checks — passed: exactly one `AddProblemDetails` registration; exactly one `Features:DesktopGateway` literal and one `/api/v1` literal, both in `DesktopGateway.cs`; `git diff --check` clean.

## Review and delivery handoff

Hilbert's independent review at the final head found no remaining implementation or plan-coverage defect; the prior evidence findings were remediated and the plan now records the final strategy and counts. PR #28 targets `dev` and is pushed to the configured `origin` remote. The next required action is to wait for every required exact-head CI job to finish green, then merge to `dev` and perform the post-merge main/proof closeout.
