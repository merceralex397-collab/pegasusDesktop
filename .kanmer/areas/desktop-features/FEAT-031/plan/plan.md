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

## Operator boundary amendment — 2026-08-27

The operator's repository boundary supersedes inherited upstream-sync wording: no upstream Pegasus fetch, merge, push, or synchronization is permitted; the configured `pegasusDesktop` remote is the only remote; all work must be performed in this repository. Cloud writes, deployments, credentials, and external-environment changes are also out of scope until the full refactor is complete. This ticket therefore does not depend on `DSK-01-10` or any upstream commit.

A read-only current-fork check on 2026-08-27 found only the configured `origin` remote. The current source contains the Box custody implementation and historical PLAT-039/PLAT-041 references in the desktop planning documents, but no imported upstream proof may be treated as current evidence. Before closeout, verify the token-renewal and Box call-budget requirements from the current fork's code and local test harness. If either cannot be proven without an upstream change, record the exact missing in-repository capability and keep the affected acceptance scope blocked; do not sync or claim completion.

The export/evidence-gallery precondition is consequently amended: those endpoints remain unexposed unless the current fork itself contains the required call-budget implementation and a local measurement proves it. The brokered metadata, content, upload-session, logical-removal, and third-party-evidence routes may proceed only within the current repository's existing Core/ports and their own evidence.
