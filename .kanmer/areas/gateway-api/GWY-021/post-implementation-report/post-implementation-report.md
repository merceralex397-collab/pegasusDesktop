# Post-implementation report — GWY-021

## Result

Implemented desktop bearer authentication for the `/api/v1` gateway. The route group requires the OpenIddict validation scheme, the `pegasus.desktop` scope, and a non-empty staff subject. `DesktopActorResolver` uses `StaffActorFactory.TryCreate`, re-checks the Identity user enabled state and security stamp on every request, enforces the absolute session lifetime, returns the documented account-disabled/password-change/not-authorized problems, and exposes the resolved `ActionActor` to existing endpoint authorization. Automation tokens and cookie authentication do not satisfy the desktop bearer policy. The per-group access-right filter remains owned by GWY-003.

## Commits

- `481d29c84d27efbdd78f0e23b13ad6ce2cc2a1d8` — bearer authentication implementation and tests.
- `20cd20bc29d035e1bb7689fdd71ce71394191cb` — planned `docs/current-architecture.md` authentication-boundary snapshot update.

## Validation

- `git diff --check` — passed.
- `dotnet restore ./Pegasus.slnx --locked-mode` — passed.
- `dotnet build ./Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false` — passed with 0 warnings and 0 errors.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~DesktopApiAuthenticationTests"` — passed 7/7.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~EveryDesktopGatewayEndpointInheritsTheDesktopApiPolicy"` — passed 1/1.

## Simplification pass

The implementation reuses the existing OpenIddict validation scheme, Core `StaffActorFactory`/`StaffAuthorization`, `UserManager`, API problem model, and group route composition. It adds one resolver for the required per-request account boundary and one architecture guard for the group policy. No cache, parallel actor factory, cookie behavior change, Worker change, cloud write, or compatibility path was introduced.

## Review state

Independent review of the final implementation/documentation head is pending. Proof and merge state must not be asserted until that review passes.

## Final correction and parent validation — 2026-08-30

- Final branch head: `5cbe7033ad477895634fb8a8d769cc3943109b3c`. It contains the implementation head `481d29c84d27efbdd78f0e23b13ad6ce2cc2a1d8`, docs snapshot `20cd20bc29d035e1bb7689fdd71ce71394191cb`, and the production-route/structural-guard correction `5cbe7033ad477895634fb8a8d769cc3943109b3c`.
- Parent rerun on this exact head: focused production-route authentication tests passed 7/7; structural architecture guard passed 1/1; `git diff --check` passed. The implementation head was already parent-validated with locked restore and a full Release build at 0 warnings/0 errors.
- The tests now exercise the real `/api/v1/mail` production route. The future DSK-03-15 password-change endpoint is not yet composed, so its exemption is recorded as a seam rather than falsely claimed as runtime-tested.
- Independent review of this final head is still pending; proof and merge state are not asserted.

## Independent review — 2026-08-30

Fermat the 2nd reviewed final exact head `5cbe7033ad477895634fb8a8d769cc3943109b3c` and returned PASS. The reviewer confirmed the production `/api/v1/mail` route evidence, bearer policy and per-request account checks, rejection/correlation behavior, structural guard, scope, and the intentional DSK-03-15 seam. No remaining merge blocker was identified.

## Reconciled exact-head validation — 2026-08-30

The branch was reconciled with `origin/dev` in its own worktree; the documentation conflict was resolved by retaining both the bearer-authentication snapshot and the current token-throttle statement. Merge commit: `6db7511d`.

- `dotnet restore ./Pegasus.slnx --locked-mode` — passed.
- `dotnet test ./tests/Pegasus.IntegrationTests/Pegasus.IntegrationTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~DesktopApiAuthenticationTests" --logger "console;verbosity=minimal" -nr:false -p:UseSharedCompilation=false` — passed 7/7 in 1m44s.
- `dotnet test ./tests/Pegasus.ArchitectureTests/Pegasus.ArchitectureTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~EveryDesktopGatewayEndpointInheritsTheDesktopApiPolicy" --logger "console;verbosity=minimal" -nr:false -p:UseSharedCompilation=false` — passed 1/1.

The final solution build, independent review of the reconciled exact head, and exact-head PR CI remain required. No proof or merge is claimed yet.
