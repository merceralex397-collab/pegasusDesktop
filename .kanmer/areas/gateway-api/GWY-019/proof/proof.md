# GWY-019 proof

## Merge and branch evidence

- PR #57, `DSK-04-02: add Desktop token session`, was independently reviewed and merged into `dev` at `f9fee74dc86903f10c2d522f8d3b09ec5dd3f410` on 2026-08-30.
- Before promotion, `origin/main` (`8ccbf8dab15d01bed8e58bf509a4a1c27851bdc2`) was verified as an ancestor of `origin/dev`.
- The exact merged `dev` SHA `f9fee74dc86903f10c2d522f8d3b09ec5dd3f410` was promoted non-force to `origin/main`; a subsequent `git ls-remote` verified `origin/main` at that SHA.
- No Azure/cloud write or deployment was performed.

## Review evidence

- Independent `pegasus-desktop-reviewer` review of clean implementation HEAD `59ae1ba2d14a4cb3ec4a68d70c1097e86ef1d16b` passed with no merge blockers.
- Review confirmed the combined Desktop/Automation composition, rolling Desktop refresh, Automation's 14-day cap, per-client and global rate-limit paths, Data Protection/certificate wiring, scope, and simplification dispositions.

## Validation evidence

- PR exact-head CI run [33310731756](https://github.com/merceralex397-collab/pegasusDesktop/actions/runs/33310731756) passed: changes, documentation, local-development-scripts, reference-data, unit, browser, all three SQL integration shards, and SQL integration coverage. The final PR head was `59ae1ba2d14a4cb3ec4a68d70c1097e86ef1d16b`.
- Post-merge `main` CI run [33312443481](https://github.com/merceralex397-collab/pegasusDesktop/actions/runs/33312443481) passed at merged SHA `f9fee74dc86903f10c2d522f8d3b09ec5dd3f410`, including unit, browser, all three SQL integration shards, coverage, changes, documentation, local-development-scripts, and reference-data.
- Local Web Release build passed with zero warnings and errors.
- Local integration validation passed: `DesktopTokenIssuance|Automation` filter, 42/42 passed, 0 failed, 0 skipped.
- Local migration grant validation passed: 74 migration files checked.
- `git diff --check` passed.
- The merged tree contains the Desktop token-session implementation, shared OpenIddict/Data Protection composition, Automation regression coverage, and current-architecture documentation.

## Acceptance mapping

- Password issuance through `/connect/token` returns Desktop access and refresh tokens; the integration tests assert staff subject/role/original-issue claims and the 10-minute access lifetime.
- Refresh rotation and the 8-hour absolute session cap are exercised by the integration tests.
- Combined Desktop/Automation tests pass, preserving Automation grants and its 14-day cap.
- The implementation uses the persisted Data Protection composition and contains no ephemeral OpenIddict key registration.
- The `pegasus-desktop` registration is public and has no client secret.
- This proof makes no claim of cloud deployment or production certificate/runtime proof; those are outside this ticket's authorized scope.

## Closeout

Verified on merged `main` after review, merge, exact-head CI, post-merge CI, and local acceptance validation.
