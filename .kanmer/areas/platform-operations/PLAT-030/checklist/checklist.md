# PLAT-030 acceptance checklist

- [x] SQL Server `Up` grants Web UPDATE on `dbo.ApprovedSentPollOutcomes`.
- [x] `Up` validates the managed Web runtime role and is a no-op for non-SQL providers.
- [x] `Down` revokes exactly Web UPDATE and is a non-SQL no-op.
- [x] The existing `Invoke-AzureDatabaseBootstrap.ps1` permission matrix accounts for the new grant-carrying migration.
- [x] No duplicate EvaHandoffDownloadOperations grant or unrelated file was added.
- [x] Release build passes with 0 warnings/errors.
- [x] Full architecture suite passes: 111/111.
- [x] `dotnet ef migrations list` recognizes the new migration.
- [x] `Test-MigrationGrants.ps1` passes: 72 migration files.
- [x] `Test-AzureDeploymentPlan.ps1 -Mode Local` passes.
- [x] `git diff --check` passes.
- [ ] PLAT-018 focused coverage passes after its parser correction and this migration is present.
- [ ] Independent review passes on the exact PR head.
- [ ] PR is merged to `dev`; proof and Kanmer closeout are completed.
- [x] No cloud, deployment execution, credential, corpus, or upstream operation occurred.
