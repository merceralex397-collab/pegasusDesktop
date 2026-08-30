# Proof — GWY-021

## Delivered change

The /api/v1 gateway bearer-authentication boundary is implemented and merged to main. The reconciled implementation head was 6db7511d9648e63a328af39e1c00348c8fa5d33e; PR #59 was independently reviewed and merged to dev as merge commit 0e7fa4237e151ac7fbbfea4b5b0c16c0ee7b170d.

The implementation evidence covers:

- valid desktop bearer claims reach Core through StaffActorFactory.TryCreate and expose the resulting ActionActor;
- the route group requires the desktop bearer policy and rejects cookie and Automation-token paths;
- account enabled state and security stamp are rechecked per request;
- absolute session expiry, disabled/missing account, unknown roles, and password-change-required outcomes fail closed with the documented problem types and correlation IDs;
- the password-change/session exemption remains an explicit seam for the not-yet-composed DSK-03-15 endpoint;
- the per-group access-right filter remains owned by GWY-003.

## Review and validation

- Fermat the 2nd independently reviewed the pre-reconciliation final implementation head 5cbe7033ad477895634fb8a8d769cc3943109b3c and returned PASS.
- Popper the 2nd independently reviewed the reconciled exact head 6db7511d9648e63a328af39e1c00348c8fa5d33e and returned PASS, including scope, documentation conflict resolution, merge ancestry, and simplification evidence.
- On the reconciled head: focused DesktopApiAuthenticationTests passed 7/7; the desktop-gateway architecture guard passed 1/1; locked restore passed; Release solution build passed with 0 warnings and 0 errors; git diff --check passed.
- PR #59 exact-head CI run 33320252751 matched 6db7511d9648e63a328af39e1c00348c8fa5d33e and passed all applicable jobs: changes, documentation, reference-data, local-development-scripts, browser, unit, SQL integration shards 1–3, and SQL integration coverage. Infrastructure was correctly skipped.
- Main push run 33320863853 matched 0e7fa4237e151ac7fbbfea4b5b0c16c0ee7b170d. Its initial SQL shard-2 attempt was cancelled at the workflow timeout without a reported assertion failure. The authorized failed-job rerun completed job 99285184607 successfully; the other applicable main jobs were successful and infrastructure was correctly skipped.
- The task branch is clean, and 6db7511d is an ancestor of merged main 0e7fa423.

No cloud, deployment, credential, destructive, or upstream write was performed.
