# Files — TEST-002 Authorization and failure-path test template

| File or area | Change | Evidence / reuse |
| --- | --- | --- |
| `tests/Pegasus.Api.ContractTests/CommandCoverage/CommandEndpointCatalogue.cs` | Derive command route/method identities from the host `EndpointDataSource`; read future access-right metadata when present. | Uses ASP.NET routing metadata and the existing factory service provider; no second endpoint list. |
| `tests/Pegasus.Api.ContractTests/CommandCoverage/CommandCoverageTable.cs` | Define the literal per-command row and symmetric endpoint/table guard. | Empty on the current merged host because the live inventory has zero command endpoints; future command tickets add rows. |
| `tests/Pegasus.Api.ContractTests/CommandCoverage/CommandCoverageTestSupport.cs` | Share request construction, problem-details, bearer-challenge, response equality, and effect-snapshot assertions. | Reuses `ContractTestWebApplicationFactory`; does not duplicate Core policy. |
| `tests/Pegasus.Api.ContractTests/CommandCoverage/CommandCoverageGuardTests.cs` | Verify the normal host and a test-only unlisted `POST /api/v1/__probe`. | The probe is an in-memory `RouteEndpointBuilder`; it never changes product routing. |
| `tests/Pegasus.Api.ContractTests/{UnauthenticatedCommandTests,WrongRightCommandTests,StaleVersionCommandTests,InvalidRequestCommandTests,IdempotentReplayCommandTests}.cs` | Provide five shared theories over future literal rows. | The theories early-exit only for the documented zero-command table; once a row exists it must provide real request/effect factories. |
| `docs/desktop/08-testing/README.md` | Document TEST-002 ownership and the future row-extension rule. | One named testing-programme sentence; no new product requirement. |

## Out of scope

No endpoint implementation, bearer-token pipeline, Core policy, database migration, CI lane, cloud/Azure write, upstream sync, `corpus/`, or unrelated documentation.
