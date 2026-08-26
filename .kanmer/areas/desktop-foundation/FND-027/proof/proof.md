# Proof

## Result

FND-027's central package-management acceptance is satisfied. The repository now has one central package-version file for the seven Pegasus solution projects, locked restore enabled at the shared boundary, and the explicitly excluded WinForms evaluator keeps its own nested boundary as recorded in the plan.

## Acceptance evidence on merged main

Read-only checks against origin/main (fff7e14178f1be6e3d4f2fbc5a5401799ba69409) produced:

- Directory.Packages.props exists at the repository root and contains 36 PackageVersion entries, matching the measured distinct-package inventory.
- All seven expected solution-project lock files are present: src/Pegasus.Core, src/Pegasus.Infrastructure, src/Pegasus.Web, src/Pegasus.Worker, tests/Pegasus.ArchitectureTests, tests/Pegasus.Core.Tests, and tests/Pegasus.IntegrationTests.
- The project-file check found no PackageReference Version attributes under src or tests.
- RestorePackagesWithLockFile occurs once in Directory.Build.props; the three test-project copies were removed.
- The Playwright property chain and the CI cache dependency include are present; no desktop package was added by this ticket.
- The review-discovered scripts/email-eval-desktop boundary is recorded in the plan: it disables central management and locked restore locally without changing that independent evaluator.

The ticket plan records the required simplification pass, scope correction, generated lock review, and independent reviewer PASS. It also records the evaluator restore and Release test result (9/9) and the focused repository validation.

## CI, review, and merge evidence

- PR #17 (fnd-027-central-package-management to dev) merged at 2026-08-26T14:15:41Z with merge commit ec323b7f37870d5ab3476a077bb270cd3c4a5063.
- Repository-check run 32976021166, attempt 2, for PR #17 head 7dd5138d914126d104ea659ba2b71aff45ab91f9, completed successfully. Changes, documentation, local-development-scripts, reference-data, unit, all three SQL integration shards, integration coverage, and browser passed; infrastructure was correctly skipped.
- With literal MERGE AUTH GRANTED, the merged dev SHA was promoted by the documented atomic exact-SHA fast-forward. Verified afterward: origin/main and origin/dev both equal fff7e14178f1be6e3d4f2fbc5a5401799ba69409, which contains the FND-027 merge commit.
- No Azure write, deployment, upstream sync, or direct .kanmer edit was performed.

## Verification boundary

This proof establishes the package, lock, build, test, independent-review, merge, and main-history requirements. It does not claim production deployment; cloud/deployment work remains outside the current permitted scope.
