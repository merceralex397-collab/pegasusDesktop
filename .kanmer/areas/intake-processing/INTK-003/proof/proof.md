# Proof

## Result

Canonical case mileage is implemented at the Core write boundary. Documented kilometres are converted once to whole canonical miles with factor `0.6213711922` and `MidpointRounding.AwayFromZero`; the typed kilometre reading is retained as provenance; missing units mean miles; and existing persisted cases are not backfilled or transformed.

## Acceptance evidence

- `CaseDataPolicy.Normalize` is the single conversion owner and reuses the existing `VehicleMileagePolicy` vocabulary and rounding policy.
- `CaseVehicleData.OriginalMileageKilometres` and the existing EAV field `vehicle_mileage_kilometres` retain typed provenance. The generated migration updates only the EAV field-name check constraint; it does not transform existing cases.
- The existing case DTO, Web case-details, MCP save path, and case summary carry/display the canonical value and compact marker without a second client-side conversion.
- Missing units are treated as miles; unknown or numeric nonblank units fail closed.
- The FRD-06 requirement and `docs/capabilities.md` register the rule. EVA field names and bundle ownership remain unchanged.
- No new route, data backfill, read-time conversion, Azure write, cloud/deployment operation, or upstream operation was introduced.

## Validation

- Focused Core/vehicle-workflow tests — 36/36 passed.
- Full `Pegasus.Core.Tests` — 927/927 passed.
- Case-data persistence tests — 5/5 passed.
- Vehicle-workflow kilometre-correction persistence test — 1/1 passed.
- Release build — passed with 0 warnings and 0 errors.
- `pwsh ./scripts/Test-MigrationGrants.ps1` — 66 migrations checked; passed.
- Focused integration and recovery validation — passed, including the existing recovery suite.
- `git diff --check` — passed with only line-ending normalization warnings.

## Review, CI, and merged-main evidence

- PR #12 final reviewed head `13ba7b41775ee83c1399eb84c17e008aa13d7a67` merged into configured `dev` as `38a7816ed2c6b91e77c46472844ce92499cfb3a5`.
- Independent review by Parfit passed the corrected implementation; the later commit corrected only the owned migration-list expectation.
- Exact-head repository-check run `32900431792` passed after the authorized failed-job rerun on the unchanged reviewed head; SQL integration shard 3 and coverage completed successfully, and the other applicable lanes were green or correctly skipped.
- After the authorized non-force exact-SHA promotion, read-back was `origin/main=origin/dev=3b1737de2a27f84aa1bea03bf2c34d41d5a8006a`.
- `git merge-base --is-ancestor 13ba7b41775ee83c1399eb84c17e008aa13d7a67 origin/main` succeeds.
- Read-only inspection of `origin/main` confirms `VehicleMileagePolicy` is used by the case normalization path, `OriginalMileageKilometres` is present, and FRD-06 contains the canonical-mile/provenance rule.

No upstream, cloud, deployment, credential, mailbox/Box mutation, or direct `.kanmer` edit was performed.
