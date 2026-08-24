# Plan — FEAT-031 Box broker endpoints

## Governing documents

This ticket is currently docs_todo: true. Its binding conversion decisions are the area plan and ADR-0107/0104 where applicable; link the final FRD/ADR path once the canonical documents are authored. Do not create a competing governing document in this task.

## Chosen approach

Define the gateway-only Box broker surface for list, metadata, bounded download/upload sessions, remove and confirm-evidence operations without placing provider credentials, reusable URLs or Box ids in the desktop.

## Steps

1. Inventory existing Box ports, Core use cases and custody/audit calls.
2. Add minimal api-v1 broker contracts/routes that translate to the existing policy owner.
3. Apply authorization, problem-details and no-secret/no-reusable-URL constraints.
4. Add contract tests for allowed, denied, expired and failure paths.

## Verification

- Focused gateway contract tests cover auth, response shape and failure details.
- Package/log scan design contains no provider secret or durable Box id.
- No direct desktop Box SDK call is introduced.

## Risks and dependencies

FEAT-033 owns the separate direct-transfer feasibility spike; provider credentials remain behind the gateway.

Implementation uses the named gateway/WinUI/test agents, records simplification, and receives independent review.
