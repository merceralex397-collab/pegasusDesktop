# Plan — FEAT-034 Box conflict and version handling

## Governing documents

This ticket is currently docs_todo: true. Its binding conversion decisions are the area plan and ADR-0107/0104 where applicable; link the final FRD/ADR path once the canonical documents are authored. Do not create a competing governing document in this task.

## Chosen approach

Detect a newer canonical document version before overwrite and present a safe, explicit conflict path through the gateway.

## Steps

1. Locate existing document version/custody policy and establish expected concurrency contract.
2. Expose a typed conflict result from the gateway without provider implementation detail.
3. Present the conflict with explicit reload/return path; never auto-overwrite.
4. Test stale upload/update, correct version and user cancellation paths.

## Verification

- Stale version test receives an explicit conflict rather than overwrite.
- Desktop tests prove no automatic retry/overwrite occurs.
- Audit/custody behaviour remains in the existing Core owner.

## Risks and dependencies

Builds on FEAT-031 broker operations and the shared problem/dialog components.

Implementation uses the named gateway/WinUI/test agents, records simplification, and receives independent review.
