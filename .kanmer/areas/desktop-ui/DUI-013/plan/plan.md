# Plan — DUI-013 Screen specifications adopted into FRD-13

## Governing documents

This ticket implements FRD-13 once FND-008 has authored it. The area-06 screen specification remains programme planning and docs/design/README.md remains design authority. Do not create a separate feature specification.

## Steps

1. Wait for FND-008 FRD-13 skeleton and stable heading/anchor convention; use kanmer-docs in the ticket worktree for canonical-document changes.
2. Reconcile each screen-spec block against its cited design-authority rule, keeping cross-cutting state and AutomationId contracts once rather than copying them per screen.
3. Check the DSK-03-07 and DSK-05-16 corrections named in the ticket body before adopting their blocks.
4. Add stable FRD-13 sections/anchors and cross-reference FRD-12 without restating design authority.
5. Link each plan-05 slice to its governing FRD-13 section, validate documentation links/placement, and record DUI-017 as the screen-map dependency.

## Verification

- [ ] Documentation link and placement checks pass.
- [ ] Every slice has one correct FRD-13 reference; no duplicate behaviour owner exists.
- [ ] The screen map from DUI-017 is linked as reference evidence, not authority.

## Risks

FND-008 and DUI-017 are prerequisites. This ticket must use a dedicated worktree and not overwrite shared canonical-document changes.
