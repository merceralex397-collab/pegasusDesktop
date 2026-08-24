# Plan — FEAT-036 Desktop vehicle workflow

## Governing documents

This ticket is currently docs_todo: true. Its binding conversion decisions are the area plan and ADR-0107/0104 where applicable; link the final FRD/ADR path once the canonical documents are authored. Do not create a competing governing document in this task.

## Chosen approach

Implement VRM validation, lookup request, accept-suggestion and provenance presentation over FEAT-035 without assuming a direct-provider contract.

## Steps

1. Read FEAT-035 contract and design screen spec; identify existing validation use case.
2. Implement input, request, result, explicit provider state and accept flow through the generated client.
3. Reuse shared field/problem/provenance controls instead of local variants.
4. Test invalid input, provider timeout/unavailable, accepted result, accessibility and keyboard path.

## Verification

- Tests prove no request is sent for invalid input.
- Provider failure is explicit and provenance is exposed to UIA.
- No direct provider traffic or credential is packaged.

## Risks and dependencies

Requires FEAT-035; any provider-contract issue is recorded rather than guessed.

Implementation uses the named gateway/WinUI/test agents, records simplification, and receives independent review.
