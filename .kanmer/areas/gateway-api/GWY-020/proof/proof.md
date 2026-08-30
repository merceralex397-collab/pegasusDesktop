# Verification proof

## Merged source

- Ticket: GWY-020
- PR: #58
- Reviewed head: 58ce5c09a5994e9ae292a28c25a304342f10a34e
- Merged into dev and promoted to main: d278de7ba0fd82e17f27e68e4658cd3e37ac7ccc
- Promotion: exact-SHA, non-force push; origin/main was an ancestor of origin/dev.

## Acceptance evidence

- /connect/token applies the StaffSignIn and global sign-in limiters to password grants.
- The Automation budget rejects the 121st request in the one-minute window with the `automation_rate_limited` reason and Retry-After.
- Browser-to-desktop requests consume the shared global limiter budget.
- Security-event coverage confirms rate limiting does not create a lockout event.
- The implementation is limited to the gateway limiter composition and its integration tests.

## Validation

- PR exact-head CI run 33316118553: success. Changes, documentation, local-development-scripts, reference-data, unit, browser, sql-integration (1), sql-integration (2), sql-integration (3), and sql-integration-coverage passed; infrastructure was skipped as expected.
- Main exact-SHA CI run 33316946735: success. The same required lanes passed; infrastructure was skipped as expected.
- Local locked restore: passed.
- Local Release build with shared compilation disabled: passed with 0 warnings and 0 errors.
- Focused DesktopTokenRateLimit tests: 4/4 passed.
- Combined DesktopTokenRateLimit, StaffSignInSecurity, and Automation tests: 41/41 passed.
- Independent reviewer Fermat the 2nd: PASS on exact reviewed head; no merge blocker.
- Simplification pass recorded in the ticket plan; no unapplied findings remain.

No deployment or cloud write was performed.
