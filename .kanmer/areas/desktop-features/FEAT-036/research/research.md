# Research — FEAT-036 Desktop vehicle workflow

## Question

Implement VRM validation, lookup request, accept-suggestion and provenance presentation over FEAT-035 without assuming a direct-provider contract.

## Findings

- The desktop is native and gateway-backed; provider terms/contract check remains an explicit verification item.
- Shared FormField, ProblemInfoBar and ProvenanceGlyph are intended reuse points.
- Provider outcomes must remain distinct rather than a friendly collapsed status.

## Implication

Keep the desktop on the existing gateway/Core boundary; implement only a caller-backed contract or native client slice. No Azure write, direct provider access, secret or compatibility path is in scope.

## Dependencies

Requires FEAT-035; any provider-contract issue is recorded rather than guessed.
