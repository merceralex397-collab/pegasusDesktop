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

## Dependency finding — 2026-08-27

- The current target `origin/dev` contains the shared `Pegasus.Contracts` project and the existing `PegasusProblemTypes`, but it does not contain `src/Pegasus.Desktop.Infrastructure`, `tests/Pegasus.Desktop.ViewModelTests`, or the area-07 provider endpoint implementations that this ticket's acceptance requires.
- The linked endpoint tickets [[FEAT-027]], [[FEAT-029]], [[FEAT-031]], [[FEAT-035]] and [[GWY-014]] remain unimplemented/preparing; their routes are not present in the target composition. Creating their endpoints, desktop infrastructure, or test projects here would violate this ticket's explicit out-of-scope guardrails and the one-ticket ownership rule.
- Therefore this ticket cannot truthfully reach review/done from the current target. The smallest unblock is delivery of the provider endpoint contracts/routes and the desktop infrastructure/test owners, after which this ticket can add the catalogue and apply it to real callers. No partial catalogue is being claimed as completion.
