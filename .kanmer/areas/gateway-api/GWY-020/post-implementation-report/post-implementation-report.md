# Post-implementation report — GWY-020

## Result

Desktop password grants at `POST /connect/token` now use the existing staff sign-in protection: the shared global 100-per-minute budget plus the per-client 10-per-minute partition, while Automation grants retain the 120-per-minute partition. Request-form inspection preserves the body for OpenIddict. Desktop rejections return `429` with `Retry-After: 60` and record `sign_in_rate_limited`; Automation rejections remain `automation_rate_limited`. Identity lockout is not introduced. The planned current-architecture snapshot line was added.

## Commits

- `16a9b4892381edd2ca902365b3cefc52f03c41db` — initial buffering and rate-limit coverage.
- `14059f0e1b60c0c52b926fe9c97e558addc5eeb1` — strengthened Automation and browser-to-desktop global-budget coverage plus architecture snapshot.
- `58ce5c09a5994e9ae292a28c25a304342f10a34e` — honest antiforgery-harness assertion for the browser request after the global permit is consumed.

## Validation

- `git diff --check` — passed.
- `dotnet restore ./Pegasus.slnx --locked-mode` — passed.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false` — passed with 0 warnings and 0 errors.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~DesktopTokenRateLimit"` — passed 4/4.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~DesktopTokenRateLimit|FullyQualifiedName~StaffSignInSecurity|FullyQualifiedName~Automation"` — passed 41/41.

## Simplification pass

The implementation reuses the existing global limiter, endpoint policy, `OnRejected` callback, marker, security-event writer, and token composition. The only production change beyond the existing path was request buffering; the remaining changes are bounded acceptance tests and one current-state documentation line. No new service, configuration, cache, compatibility path, Worker change, cloud write, or speculative time-control mechanism was introduced. The wall-clock replenishment case was not simulated and is not claimed.

## Review state

The first independent review found two evidence gaps; both were addressed in the final head. Fresh independent review of `58ce5c09a5994e9ae292a28c25a304342f10a34e` is pending. Proof and merge state must not be asserted until that review passes.
