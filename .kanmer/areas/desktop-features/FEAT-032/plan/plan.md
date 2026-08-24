# Plan — FEAT-032 Desktop document browser and transfer queue

## Governing documents

This ticket is currently docs_todo: true. Its binding conversion decisions are the area plan and ADR-0107/0104 where applicable; link the final FRD/ADR path once the canonical documents are authored. Do not create a competing governing document in this task.

## Chosen approach

Build the native document browser, transfer queue, preview pane and bounded temporary working cache over FEAT-031 broker contracts.

## Steps

1. Read document/custody screen specs and FEAT-031 contract before selecting view-model seams.
2. Implement browser/list/preview against gateway contracts and reuse shared UI controls.
3. Implement bounded temporary cache plus explicit queued/running/failed/cancelled states.
4. Add view-model/UI tests for large/failing transfers, cancellation and retry handoff.

## Verification

- No desktop provider secret, SDK or reusable provider URL appears.
- Queue tests prove failure/cancel state remains explicit.
- Bounded cache cannot become an offline authoritative store.

## Risks and dependencies

Requires FEAT-031 contracts; FEAT-033 is not assumed; human-only custody retry constraint remains binding.

Implementation uses the named gateway/WinUI/test agents, records simplification, and receives independent review.
