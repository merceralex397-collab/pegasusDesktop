# Plan — DUI-004 Authenticated shell

## Governing documents

This ticket currently remains `docs_todo: true`; its existing source material is the authoritative design documentation and area-06 plan. Do not create or link a speculative canonical document here. When FRD-13 exists, link the relevant stable section before the ticket leaves Preparing; current related references, where present, stay unchanged.

## Chosen approach

How can the shell reproduce the authority's navigation, counts and status context while keeping data behind the gateway? The chosen implementation is a narrow native WinUI 3 shared component/surface, extending the existing desktop project and gateway contracts rather than adding a second framework or policy owner.

## Steps

1. Read screen-specs Shell and the existing foundation shell before selecting extension points.
2. Implement the authority route order and fixed rail/title/status layout with ThemeResources and AutomationIds.
3. Bind counts to the gateway rail-counts query and represent loading, stale and unavailable states explicitly.
4. Consume the FND-033 environment-badge decision when available; do not invent a local label.
5. Exercise keyboard rail navigation, count refresh and constrained-width layout.

## Verification

- UI automation confirms route order and selection semantics.
- Gateway-backed count states never present unavailable/stale data as `0`.
- Keyboard and 200%/High Contrast screenshots pass.

## Risks and dependencies

DSK-02-08 and DSK-02-10 provide shell/lifecycle foundations; FND-033 owns the outstanding environment-badge wording.

The implementation worktree must record its simplification pass and independent desktop review before merge.
