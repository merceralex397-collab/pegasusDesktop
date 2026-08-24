# Research — FEAT-035 DVLA/DVSA gateway endpoints

## Question

Expose request lookup, accept suggestion, status, cache lifetime and provenance through the gateway while retaining provider credentials and policy server-side.

## Findings

- ADR-0107 locks DVLA/DVSA credentials behind the gateway.
- The desktop receives normalised response/provenance and problem types, never provider secret or raw provider state.
- Provider cache lifetime belongs to the authoritative server policy.

## Implication

Keep the desktop on the existing gateway/Core boundary; implement only a caller-backed contract or native client slice. No Azure write, direct provider access, secret or compatibility path is in scope.

## Dependencies

FEAT-036 consumes these contracts; direct desktop provider calls are forbidden.
