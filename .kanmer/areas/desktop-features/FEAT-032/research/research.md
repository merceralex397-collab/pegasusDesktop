# Research — FEAT-032 Desktop document browser and transfer queue

## Question

Build the native document browser, transfer queue, preview pane and bounded temporary working cache over FEAT-031 broker contracts.

## Findings

- L-01/L-02 require native UI backed by the local Test/UAT gateway; no direct Box credentials or Azure test environment.
- ADR-0104 allows only bounded temporary cache, not an offline replica.
- Shared DataTable, StatusChip and PageHeader controls should be reused once they land.

## Implication

Keep the desktop on the existing gateway/Core boundary; implement only a caller-backed contract or native client slice. No Azure write, direct provider access, secret or compatibility path is in scope.

## Dependencies

Requires FEAT-031 contracts; FEAT-033 is not assumed; human-only custody retry constraint remains binding.
