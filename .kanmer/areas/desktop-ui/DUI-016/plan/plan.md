# Plan — DUI-016 Ten recorded reviews

## Governing documents

The design authority owns the ten-review requirement; the area plan defines the evidence path. This chore records a reusable procedure, not a new accessibility standard.

## Steps

1. Convert the ten named manual review types into an evidence template at the existing artifacts/a11y/<release>/<Screen>.md convention.
2. Define reviewer identity, app build identity, exact command/report references and pass/fail/disposition fields.
3. Connect review 10 to DUI-015 AutomationId/Axe outputs without treating automated results as a replacement for manual review.
4. Run the procedure once on the current candidate, record gaps as tickets, and preserve evidence outside source control as specified.

## Verification

- [ ] One completed record demonstrates all ten review fields.
- [ ] Evidence identifies release, screen, reviewer and generated scan/recording paths.
- [ ] Manual keyboard and assistive-technology results are present separately from automation.

## Risks

Runs occur on the local Test/UAT workstation or pilot ring only; no Azure test environment is created.
