# Plan — FEAT-035 DVLA/DVSA gateway endpoints

## Governing documents

This ticket is currently docs_todo: true. Its binding conversion decisions are the area plan and ADR-0107/0104 where applicable; link the final FRD/ADR path once the canonical documents are authored. Do not create a competing governing document in this task.

## Chosen approach

Expose request lookup, accept suggestion, status, cache lifetime and provenance through the gateway while retaining provider credentials and policy server-side.

## Steps

1. Inventory existing vehicle provider port, cache and provenance owner.
2. Design minimal request/status/accept contracts around the existing use cases.
3. Implement gateway routes with auth/problem mapping and no secret/provider-internal projection.
4. Test validation, known response, unavailable/timeout and provenance states using replay.

## Verification

- Contract tests cover role denial, invalid VRM, provider failure and accepted suggestion.
- No provider credential/token appears in desktop contract or logs.
- Cache/provenance semantics are asserted by the single Core owner.

## Risks and dependencies

FEAT-036 consumes these contracts; direct desktop provider calls are forbidden.

Implementation uses the named gateway/WinUI/test agents, records simplification, and receives independent review.

## Current-head coordination decision (2026-08-29)

origin/dev contains the shared desktop gateway composition and Core vehicle workflow but no vehicle route group. FEAT-035 therefore owns the first /api/v1 vehicle route group in Pegasus.Web/Api; it will invoke the existing Core ports and be structured so the later assessment routes in [[GWY-014]] extend the same group rather than registering a second vehicle group. The desktop workflow remains downstream in [[FEAT-036]].

The current operator boundary prohibits cloud writes/deployment and upstream synchronization. Tests will use the existing DevelopmentOffline profile and replay adapter; a live Key Vault names-only check is not required to implement this ticket and will not be represented as completed evidence unless actually run.
