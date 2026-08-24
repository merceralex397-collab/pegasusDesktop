# Checklist — PLAT-015

## Implementation

- [ ] 1. Orientation. Read the plan row, proposal `:1229-1241`, and `src/Pegasus.Web/Program.cs:523-524` and `:939-954` so the new endpoint complements the probes rather than replacing them. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.

- [ ] 2. Branch `task/dsk-10-15-admin-health-surface` from `dev`.

- [ ] 3. Define the contract in `src/Pegasus.Contracts` (created by `DSK-02-04`/`DSK-03-01`): `AdminHealthResponse { ObtainedAtUtc, MinimumClientVersion, CurrentGatewayVersion, Dependencies: [ { Name, State, ObtainedAtUtc, Detail, LastSuccessAtUtc? } ] }` where `State` is a closed enum — `healthy`, `degraded`, `unavailable`, `unknown`. Every dependency entry carries its own `ObtainedAtUtc` so a cached answer is visibly cached (proposal §16.2 `:1135`).

- [ ] 4. Enumerate the dependency rows exactly as §18.3 lists them: gateway reachable (trivially true if the response was produced); database reachable (reuse `DatabaseReadinessHealthCheck`); Worker last successful cycle **per function** (from the intake-status data of `DSK-07-01`, covering each `AzureWebJobs.*` function the estate runs); Box connectivity; DVLA state; DVSA state; update-feed reachability; current minimum client version.

- [ ] 5. Implement the endpoint in the `/api/v1` administration route group behind the `Administrator` policy and the `StaffAccessRight` filter from `DSK-03-03`. Return `200` with the payload for an administrator, the standard `not-authorized` problem for a non-administrator, `401` when unauthenticated. Never `AllowAnonymous`.

- [ ] 6. Bound every probe: each dependency check runs with its own short timeout and returns `unknown` with the timeout recorded rather than hanging the response; the whole endpoint returns within a fixed budget. Use `microsoft_docs_search` for the health-check timeout pattern rather than inventing one. A health endpoint that blocks on a dead provider is itself an outage.

- [ ] 7. Make provider checks cheap and safe: prefer a cached last-known outcome from the provider adapters over issuing a live third-party call on every request, and state the cache age in `ObtainedAtUtc`. A live call per request would multiply provider load and could trip rate limits (proposal §16.2).

- [ ] 8. Implement update-feed reachability as a test of the D-003 UNC path: can the gateway (or the desktop, whichever the plan for `DSK-09-10` established) stat the `.appinstaller` on the share, and what version does it advertise. Report the path in a redacted form — the share name, not credentials.

- [ ] 9. Add the secrets test: a contract test that serializes the response for a fully populated fixture and asserts it contains no connection string, no `vault.azure.net` URI, no client id or secret, no mailbox address and no file path beyond the redacted feed name. Reuse the pattern list from [[DSK-10-01]].

- [ ] 10. Add authorization tests: administrator → 200; staff without the administration right → the documented problem; automation token → refused; unauthenticated → 401. Each refusal writes the expected security/action-history record.

- [ ] 11. Build the desktop surface on the Operations/Settings screen (`DSK-05-20`): one row per dependency showing name, state, `ObtainedAtUtc` and detail, with `AutomationProperties.AutomationId` on every row so `winapp ui` can assert it. Follow `docs/design/README.md` operator-copy rules — state words are operator vocabulary ("Unavailable", "Last checked 12:04"), not protocol jargon. Show state with text plus colour, never colour alone.

- [ ] 12. Write the `winapp ui` script that opens the screen, waits for the rows and asserts every dependency has a state and an obtained-at value; file it with the UI suite from `DSK-08-06`.

- [ ] 13. Run `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release` and the UI script against the Test/UAT stack. Both green.

- [ ] 14. Update `docs/current-architecture.md` with the health surface as a retained fact and add a `DSK` capability row for the admin health view once the `DSK` family exists (`DSK-00-08`).

- [ ] 15. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `dotnet test ./tests/Pegasus.Api.ContractTests/Pegasus.Api.ContractTests.csproj --configuration Release --filter "FullyQualifiedName~AdminHealth"` — expected: contract, authorization and no-secrets facts pass.
- [ ] `winapp ui` health script against the Test/UAT stack — expected: every dependency row present with a non-empty state and obtained-at.
- [ ] Manual check with a provider stopped on the local stack — expected: that row reports `unavailable` or `unknown` within the endpoint budget and the rest of the response is still returned.

## Progress notes

Record factual progress here.
