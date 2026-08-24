# Plan — FEAT-045 Provider error taxonomy

## Governing documents

This ticket is currently docs_todo: true. Its binding conversion decisions are the area plan and ADR-0107/0104 where applicable; link the final FRD/ADR path once the canonical documents are authored. Do not create a competing governing document in this task.

## Chosen approach

Create one contract-level vocabulary for terminal, transient and unknown provider failures and five provider problem types, consumed by gateway and desktop.

## Steps

1. Inventory current provider problem mappings and locate any duplicated classification.
2. Define the smallest shared taxonomy and five problem types in contracts.
3. Replace divergent gateway/desktop mapping with the shared values.
4. Add table-driven contract/presentation tests including unknown and transient paths.

## Verification

- Search shows one classification owner.
- Tests cover terminal, transient and unknown classifications and each problem type.
- Unknown remains visible and is not silently retried.

## Risks and dependencies

DUI-010 consumes the type-to-copy mapping; gateway/provider work owns transport translation.

Implementation uses the named gateway/WinUI/test agents, records simplification, and receives independent review.
