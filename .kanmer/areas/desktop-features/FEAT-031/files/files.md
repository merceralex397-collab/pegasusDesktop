# Files — FEAT-031 Box broker endpoints

## Owned implementation

| Path | Change | Evidence |
| --- | --- | --- |
| `src/Pegasus.Web/Api/BoxDocumentBrokerEndpoints.cs` | Versioned broker routes for list, metadata, streaming download, bounded upload sessions, logical removal, and third-party evidence confirmation. | Reuses existing Core use cases and custody/state ports; no provider policy or token/object detail in responses. |
| `src/Pegasus.Web/Api/DesktopGatewayAuthorizationEndpointFilter.cs` | API authentication filter for the gateway group. | Returns API problem details rather than cookie redirects; anonymous routes remain protected by the filter. |
| `src/Pegasus.Web/Api/DesktopGatewayExtensions.cs` | Registers the broker routes and shared gateway filters. | Uses the existing `/api/v1` gateway composition root. |
| `src/Pegasus.Web/Api/DesktopGatewayProblems.cs` | Problem-details mapping used by the broker routes. | Keeps the endpoint failure envelope in the gateway layer. |
| `src/Pegasus.Web/Api/CorrelationIdEndpointFilter.cs` | Shared correlation-filter registration adjustment. | Preserves the gateway correlation contract. |
| `src/Pegasus.Contracts/Responses/DocumentResponses.cs` | Caller-backed document mutation response contract. | Excludes Box token, URL, object ID, and fabricated replay fields. |
| `tests/Pegasus.IntegrationTests/BoxDocumentBrokerWebTests.cs` | Web contract tests for authorization, conditional responses, streaming/cache headers, upload limits, sessions, quota, expiry, concurrency, failures, and mutation semantics. | Uses the existing IntegrationTests project; no new test project or solution/CI lane. |
| `tests/Pegasus.IntegrationTests/BoxDocumentBrokerContractTests.cs` | Small contract-shape assertions. | Kept in the existing test project to avoid ownership overlap with GWY-004/TEST-001. |

## Explicitly not changed

- No standalone `tests/Pegasus.Api.ContractTests` scaffold: it was removed because that project is owned by other tickets and was not in the solution/CI.
- No export or evidence-gallery route: PLAT-041 current-fork O(1)+N implementation and measurement are not yet proven.
- No token-age implementation claim: PLAT-039 current-fork proof is not yet available.
- No upstream remote fetch/merge/push/synchronization, cloud write, deployment, credential change, corpus change, solution change, or CI workflow change.
