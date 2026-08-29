# Post-implementation report — FND-032

## Exact implementation

- Reviewed implementation head: 704996c7d41c9c59de8a75ef7f2b5a84a9ccff9c.
- The desktop host, embedded channel configuration, lifecycle disposal, options registration, API client registration, credential-store registration, bounded cache registration, and diagnostics provider are implemented in the ticket-owned production source.
- The FND-038-owned test source was intentionally not added or duplicated in this ticket.

## Validation completed

- Locked restore: passed.
- Full Release solution build: passed with 0 warnings and 0 errors.
- Existing ViewModelTests: passed 6/6.
- Pilot-channel build/resource inspection: passed for the selected resource shape.
- Configuration scan: no secrets or tokens were found in the desktop configuration files.
- Local BuildAndRun launch: responsive launch and structured diagnostics log with a session identifier were observed.
- Diff check: passed.

## Independent review and unresolved acceptance (historical — 2026-08-29)

Zeno independently reviewed the earlier implementation head `704996c7d41c9c59de8a75ef7f2b5a84a9ccff9c`. That review recorded the then-current local gateway/feed placeholders, the pre-PR-43 redaction weakness, and the missing FND-038 evidence. Those findings are retained as dated history; they are not the current state.

## Current status

The pilot and production `Gateway:BaseAddress` entries now use the observed read-only production ingress recorded below, and the shared FND-031 redaction correction is merged to `dev` through PR #43. A separate FND-031 follow-up at `bec8d1bcd4465078e2ea3fab9a9188081118d00c` defers invalid gateway-address failure until named-client creation so `ValidateOnStart()` can report it at host start; that follow-up remains under review. FND-038 still owns the missing host/options/log/rotation evidence. The exact pilot/production UNC feed host/share is still not established by repository authority, so release configuration remains blocked.

This report is evidence of the current state only. It does not assert merge, deployment, runtime acceptance, or Done.

## Follow-up read-only endpoint check — 2026-08-29

Read-only Container App inspection returned the current production gateway ingress hostname: https://pegasus-prod-web-252ow37gij.ashymushroom-676209e5.uksouth.azurecontainerapps.io/. The pilot and production Gateway:BaseAddress entries were corrected in the ticket branch to this observed value. No Azure write or deployment was performed. The D-003 UNC feed host/share is not present in repository authority; pilot and production feed URIs remain placeholders and block release acceptance.

## Dependency revalidation — 2026-08-29

After PR #43 merged to `dev`, `origin/dev` was merged into `task/desktop-host` as `925e98724554c1ba7528492e6a3136f44c8b0416`. Locked solution restore and targeted Release builds for Infrastructure and Desktop passed with zero warnings/errors. The branch was pushed to `origin/task/desktop-host`. Remaining acceptance blockers are the exact pilot/production feed host/share and FND-038-owned host/log/validation test evidence; no cloud or deployment operation was performed.

## Independent review disposition — 2026-08-29

Boole's exact-head review of `925e98724554c1ba7528492e6a3136f44c8b0416` is BLOCKED. The review found the missing gateway-address failure occurred during registration rather than host start, stale report wording, absent FND-038 behavior evidence, an unresolved unpackaged store-root fallback discrepancy, and unreleased UNC feed placeholders. The registration timing correction is isolated to FND-031's owned follow-up commit `bec8d1bc`, which is pending independent review. No FND-032 delivery or Done claim is made.

## Fallback reconciliation — 2026-08-29

The plan now explicitly records the supported local/unpackaged behavior: when `ApplicationData.Current.LocalFolder` is unavailable, the host uses a per-process OS-temp directory for its diagnostics writer and DPAPI store; packaged launches use the app-local folder. This is not release storage. The behavior still requires an explicit FND-038 test; until that test passes, FND-032 remains incomplete.

## Post-PR-45 revalidation — 2026-08-29

PR #45 merged the gateway registration timing correction to `dev`; the owned host branch now contains it at head `f62407a30955d6ee2e1e1ee192c6e76d867a998c`. Locked solution restore, full Release solution build, and targeted Desktop/Infrastructure Release builds passed with 0 warnings/errors. The pilot-channel assembly inspection showed base plus the selected pilot resource, with no production resource included. FND-038 behavior tests and exact release UNC feed authority remain outstanding; no release or deployment claim is made.
