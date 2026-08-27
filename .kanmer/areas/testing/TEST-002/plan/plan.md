# Plan — TEST-002 Authorization and failure-path test template

## Objective

Create a reusable contract-test harness for every command endpoint under `/api/v1`. Each explicit table row will supply the route, method, required `StaffAccessRight`, valid and invalid request bodies, and the concurrency/idempotency flags needed by the five failure-path theories. The harness must enumerate the real host's endpoint data source and fail closed when an endpoint is added without a row.

## Verified starting state — 2026-08-27

- `TEST-001` is done and owns the existing `Pegasus.Api.ContractTests` project and `ContractTestWebApplicationFactory`.
- The merged `origin/dev` head `ae2ce74a8eea31232203971415fe6b652c89ea84` contains the `/api/v1` group and OpenAPI surface, but no POST, PUT, PATCH, or DELETE command endpoint yet.
- `GWY-003` and `GWY-021` are not merged, so this ticket must not invent bearer authentication, endpoint authorization metadata, command handlers, database setup, or duplicate Core policy.
- Therefore the initial literal command table is intentionally empty. The normal-host guard must pass with zero commands, while the throwaway probe test must prove that a newly mapped command is reported until its explicit row is added. Future endpoint tickets add their rows and their endpoint-specific effect/setup assertions within this harness.

## Ordered implementation

1. Add `CommandEndpointCatalogue` under `tests/Pegasus.Api.ContractTests/CommandCoverage/`. Resolve `EndpointDataSource` from the existing factory service provider, select `POST`, `PUT`, `PATCH`, and `DELETE` route endpoints under `/api/v1`, and expose stable route/method identities plus the endpoint's declared access-right metadata when it exists. Do not enumerate Razor or MCP endpoints.
2. Add a literal `CommandCoverageTable` with the row contract: route pattern, method, required right, a concrete request path/body factory, invalid body, and version/operation-key flags. Keep it empty for the current merged host; do not fabricate command endpoints or request data.
3. Add a symmetric guard fact: every catalogued command has exactly one table row, and every table row has exactly one catalogued command. Failure text includes the route and method.
4. Add the five data-driven theory classes. They use the existing `ContractTestWebApplicationFactory` and row-supplied request/effect probes; they do not re-implement Core rules. Unauthenticated requests expect 401 and only the Bearer challenge; wrong-right and stale-version cases assert the persisted effect snapshot is unchanged; invalid requests expect the mapped `PegasusProblem` 400 contract; operation-key replay expects identical response and one effect.
5. Add a test-only derived factory that maps `POST /api/v1/__probe` and assert the guard reports that route, then remove the probe factory from the normal test path.
6. Update `docs/desktop/08-testing/README.md` §4 to state that the template exists and future area-03 command tickets must add literal rows.
7. Run the detected .NET/xUnit contract suite, then the locked restore/Release build and the exact throwaway-probe red/green check. Run a simplification pass over the branch diff and record the disposition here before review.

## Scope and non-goals

- Owned files: `tests/Pegasus.Api.ContractTests/CommandCoverage/**`, the five TEST-002 theory files under `tests/Pegasus.Api.ContractTests/**`, and the one named sentence/table update in `docs/desktop/08-testing/README.md`.
- No endpoint implementation, bearer-token pipeline, Core policy, database migration, CI lane, cloud/Azure write, upstream sync, `corpus/`, or unrelated documentation.
- The current zero-command table is evidence about the merged host, not a claim that future command coverage is complete. The guard is the enforcement point for later endpoint rows.

## Verification and exit conditions

- Normal host catalogue and guard pass with zero command endpoints.
- Throwaway `POST /api/v1/__probe` causes the guard to fail naming `/api/v1/__probe`; removing the probe restores green.
- The contract project builds with warnings as errors and the five theories are wired over the applicable literal rows.
- Simplification pass and independent review are recorded before the PR.

## Simplification pass — 2026-08-27

- Reused the existing `ContractTestWebApplicationFactory`, xUnit project, and shared ASP.NET problem contract; no second host, auth implementation, business-policy fixture, or new dependency was introduced.
- Kept one endpoint catalogue and one literal row type. The five required theories are thin consumers of row-supplied request/effect delegates instead of repeating endpoint-specific setup.
- The current row set remains empty because the verified host has no command endpoints. xUnit requires at least one data item for a theory, so a private placeholder row is used only to keep each future-row theory discoverable; each theory exits before creating a request, and the placeholder cannot satisfy the coverage guard because it is not in `Rows`.
- The probe is an in-memory `RouteEndpointBuilder` rather than a derived web host, eliminating static-assets and application-startup machinery while still exercising the real catalogue/guard code path.
- No behaviour-changing simplification was identified. Reflection-based access-right discovery is limited to test-side endpoint metadata and avoids inventing or duplicating the not-yet-merged GWY-003 metadata type; it will be exercised by the guard when that production metadata exists.
