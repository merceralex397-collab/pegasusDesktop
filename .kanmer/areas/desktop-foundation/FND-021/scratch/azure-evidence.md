### Raw Azure MCP output — subscription_list — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"subscriptions":[{"subscriptionId":"e6076573-23a5-46a8-acef-7e22d264e5db","displayName":"Azure subscription 1","state":"Enabled","tenantId":"858cf5b3-aa0a-47a6-9b40-4851fd0afa94","isDefault":true}]},"duration":0}
```

### Raw Azure MCP output — group_list — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"groups":[{"name":"DefaultResourceGroup-SUK","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/DefaultResourceGroup-SUK","location":"uksouth"},{"name":"VisualStudioOnline-24D3DE18145149ECA713A2C21F0A74B1","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/VisualStudioOnline-24D3DE18145149ECA713A2C21F0A74B1","location":"uksouth"},{"name":"VisualStudioOnline-C54F94A5C4C841719773D424E581EAE4","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/VisualStudioOnline-C54F94A5C4C841719773D424E581EAE4","location":"uksouth"},{"name":"rg-pegasus-prod","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod","location":"uksouth"},{"name":"rg-vehicledb-dist","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-vehicledb-dist","location":"uksouth"}]},"duration":0}
```

### Raw Azure MCP output — group_resource_list — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"resources":[{"name":"pegasus-prod-worker-id-252ow37gij","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.ManagedIdentity/userAssignedIdentities/pegasus-prod-worker-id-252ow37gij","type":"Microsoft.ManagedIdentity/userAssignedIdentities","location":"uksouth"},{"name":"pegasus-prod-aca-env-252ow37gij","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.App/managedEnvironments/pegasus-prod-aca-env-252ow37gij","type":"Microsoft.App/managedEnvironments","location":"uksouth"},{"name":"pegasus-prod-logs-252ow37gij","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.OperationalInsights/workspaces/pegasus-prod-logs-252ow37gij","type":"Microsoft.OperationalInsights/workspaces","location":"uksouth"},{"name":"pegasus-prod-sql-252ow37gij","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Sql/servers/pegasus-prod-sql-252ow37gij","type":"Microsoft.Sql/servers","location":"uksouth"},{"name":"pegasus-prod-worker-plan-252ow37gij","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Web/serverFarms/pegasus-prod-worker-plan-252ow37gij","type":"Microsoft.Web/serverFarms","location":"uksouth"},{"name":"pegtrans252ow37gij","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegtrans252ow37gij","type":"Microsoft.Storage/storageAccounts","location":"uksouth"},{"name":"pegcustody252ow37gij","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegcustody252ow37gij","type":"Microsoft.Storage/storageAccounts","location":"uksouth"},{"name":"pegasus-prod-operations","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Insights/actiongroups/pegasus-prod-operations","type":"Microsoft.Insights/actiongroups","location":"global"},{"name":"pegasus-prod-web-id-252ow37gij","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.ManagedIdentity/userAssignedIdentities/pegasus-prod-web-id-252ow37gij","type":"Microsoft.ManagedIdentity/userAssignedIdentities","location":"uksouth"},{"name":"pegasusprodacr252ow37gij","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.ContainerRegistry/registries/pegasusprodacr252ow37gij","type":"Microsoft.ContainerRegistry/registries","location":"uksouth"},{"name":"pegasusprodkv252ow37g","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g","type":"Microsoft.KeyVault/vaults","location":"uksouth"},{"name":"pegasus-prod-appi-252ow37gij","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Insights/components/pegasus-prod-appi-252ow37gij","type":"Microsoft.Insights/components","location":"uksouth"},{"name":"pegasus-prod-application-exceptions","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Insights/scheduledqueryrules/pegasus-prod-application-exceptions","type":"Microsoft.Insights/scheduledqueryrules","location":"uksouth"},{"name":"pegasus-prod-sql-252ow37gij/master","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Sql/servers/pegasus-prod-sql-252ow37gij/databases/master","type":"Microsoft.Sql/servers/databases","location":"uksouth"},{"name":"pegasus-prod-sql-252ow37gij/pegasus","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Sql/servers/pegasus-prod-sql-252ow37gij/databases/pegasus","type":"Microsoft.Sql/servers/databases","location":"uksouth"},{"name":"pegasus-prod-worker-252ow37gij","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Web/sites/pegasus-prod-worker-252ow37gij","type":"Microsoft.Web/sites","location":"uksouth"},{"name":"pegasus-prod-web-252ow37gij","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.App/containerApps/pegasus-prod-web-252ow37gij","type":"Microsoft.App/containerApps","location":"uksouth"},{"name":"pegasus-prod-web-http5xx","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Insights/metricalerts/pegasus-prod-web-http5xx","type":"Microsoft.Insights/metricalerts","location":"global"}]},"duration":0}
```

### Raw Azure MCP output — storage_account_get pegtrans252ow37gij — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"accounts":[{"name":"pegtrans252ow37gij","location":"uksouth","kind":"StorageV2","skuName":"Standard_LRS","skuTier":"Standard","provisioningState":"Succeeded","creationTime":"2026-08-01T20:44:46.899+00:00","allowBlobPublicAccess":false,"enableHttpsTrafficOnly":true}]},"duration":0}
```

### Raw Azure MCP output — storage_account_get pegcustody252ow37gij — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"accounts":[{"name":"pegcustody252ow37gij","location":"uksouth","kind":"StorageV2","skuName":"Standard_LRS","skuTier":"Standard","provisioningState":"Succeeded","creationTime":"2026-08-01T20:44:46.93+00:00","allowBlobPublicAccess":false,"enableHttpsTrafficOnly":true}]},"duration":0}
```

### Raw Azure MCP output — storage_blob_container_get transport — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"containers":[{"name":"app-package","lastModified":"2026-08-25T01:12:34+00:00","eTag":"\u00220x8DF0245F1B7E4E5\u0022","leaseStatus":"Unlocked","leaseState":"Available","hasImmutabilityPolicy":false,"hasLegalHold":false},{"name":"azure-webjobs-hosts","lastModified":"2026-08-01T22:16:06+00:00","eTag":"\u00220x8DEF01A7BB6B154\u0022","leaseStatus":"Unlocked","leaseState":"Available","hasImmutabilityPolicy":false,"hasLegalHold":false},{"name":"azure-webjobs-secrets","lastModified":"2026-08-01T22:38:06+00:00","eTag":"\u00220x8DEF01D8E73A9F7\u0022","leaseStatus":"Unlocked","leaseState":"Available","hasImmutabilityPolicy":false,"hasLegalHold":false}]},"duration":0}
```

### Raw Azure MCP output — storage_blob_container_get custody — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"containers":[{"name":"authentication-ring","lastModified":"2026-08-25T01:12:40+00:00","eTag":"\u00220x8DF0245F56E18C5\u0022","leaseStatus":"Unlocked","leaseState":"Available","hasImmutabilityPolicy":false,"hasLegalHold":false},{"name":"box-links","lastModified":"2026-08-25T01:12:39+00:00","eTag":"\u00220x8DF0245F51E7546\u0022","leaseStatus":"Unlocked","leaseState":"Available","hasImmutabilityPolicy":false,"hasLegalHold":false},{"name":"transient-intake","lastModified":"2026-08-25T01:12:39+00:00","eTag":"\u00220x8DF0245F51557CA\u0022","leaseStatus":"Unlocked","leaseState":"Available","hasImmutabilityPolicy":false,"hasLegalHold":false}]},"duration":0}
```

### Raw Azure MCP output — keyvault_secret_get names-only attempt — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
Operation cancelled by user (decline).
```

### Raw Azure MCP output — keyvault_certificate_get — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"certificates":[]},"duration":0}
```

### Raw Azure MCP output — sql_server_get — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":[{"name":"pegasus-prod-sql-252ow37gij","fullyQualifiedDomainName":"pegasus-prod-sql-252ow37gij.database.windows.net","location":"uksouth","resourceGroup":"rg-pegasus-prod","subscription":"e6076573-23a5-46a8-acef-7e22d264e5db","administratorLogin":"CloudSAc8b269b0","version":"12.0","state":"Ready","publicNetworkAccess":"Enabled","tags":{"app":"pegasus","environment":"prod","managedBy":"azd-bicep","release":"0.1.0-alpha.1"}}],"duration":0}
```

### Raw Azure MCP output — sql_server_firewall-rule_list — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"firewallRules":[{"name":"AllowAzureServices","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Sql/servers/pegasus-prod-sql-252ow37gij/firewallRules/AllowAzureServices","type":"Microsoft.Sql/servers/firewallRules","startIpAddress":"0.0.0.0","endIpAddress":"0.0.0.0"},{"name":"codex-current-machine-20260812","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Sql/servers/pegasus-prod-sql-252ow37gij/firewallRules/codex-current-machine-20260812","type":"Microsoft.Sql/servers/firewallRules","startIpAddress":"82.8.225.120","endIpAddress":"82.8.225.120"},{"name":"dev-machine-1","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Sql/servers/pegasus-prod-sql-252ow37gij/firewallRules/dev-machine-1","type":"Microsoft.Sql/servers/firewallRules","startIpAddress":"82.10.246.160","endIpAddress":"82.10.246.160"}]},"duration":0}
```

### Raw Azure MCP output — sql_db_get — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"databases":[{"name":"pegasus","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Sql/servers/pegasus-prod-sql-252ow37gij/databases/pegasus","type":"Microsoft.Sql/servers/databases","location":"uksouth","sku":{"name":"Standard","tier":"Standard","capacity":10},"status":"Online","collation":"SQL_Latin1_General_CP1_CI_AS","creationDate":"2026-08-01T20:46:49.587+00:00","maxSizeBytes":268435456000,"serviceLevelObjective":"S0","edition":"Standard","earliestRestoreDate":"2026-08-18T03:04:32.1468804+00:00","readScale":"Disabled","zoneRedundant":false},{"name":"master","id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Sql/servers/pegasus-prod-sql-252ow37gij/databases/master","type":"Microsoft.Sql/servers/databases","location":"uksouth","sku":{"name":"GP_SYSTEM","tier":"System","capacity":4,"family":"SYSTEM"},"status":"Online","collation":"SQL_Latin1_General_CP1_CI_AS","creationDate":"2026-08-01T20:45:36.19+00:00","maxSizeBytes":107374182400,"serviceLevelObjective":"GP_SYSTEM_4","edition":"GP_SYSTEM","readScale":"Disabled","zoneRedundant":false}]},"duration":0}
```

### Raw Azure MCP output — containerapps_list — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"containerApps":[{"name":"pegasus-prod-web-252ow37gij","location":"uksouth","resourceGroup":"rg-pegasus-prod","managedEnvironmentId":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.App/managedEnvironments/pegasus-prod-aca-env-252ow37gij","provisioningState":"Succeeded"}],"areResultsTruncated":false},"duration":0}
```

### Raw Azure MCP output — functionapp_get — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"functionApps":[{"name":"pegasus-prod-worker-252ow37gij","resourceGroupName":"rg-pegasus-prod","location":"uksouth","appServicePlanName":"pegasus-prod-worker-plan-252ow37gij","status":"Running","defaultHostName":"pegasus-prod-worker-252ow37gij.azurewebsites.net","tags":{"app":"pegasus","environment":"prod","managedBy":"azd-bicep","release":"0.1.0-alpha.1","azd-service-name":"worker"}}]},"duration":0}
```

### Raw Azure MCP output — monitor_workspace_list — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"workspaces":[{"name":"pegasus-prod-logs-252ow37gij","customerId":"0e4342c1-73ea-48d8-8571-8bca88991b21"}]},"duration":0}
```

### Raw Azure MCP output — applicationinsights_recommendation_list — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","duration":0}
```

### Raw Azure MCP output — acr_registry_list — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"registries":[{"name":"pegasusprodacr252ow37gij","location":"uksouth","loginServer":"pegasusprodacr252ow37gij.azurecr.io","skuName":"Basic","skuTier":"Basic"}],"areResultsTruncated":false},"duration":0}
```

### Raw Azure MCP output — role_assignment_list — 2026-08-25

Command was read-only; no Azure mutation was requested.

```json
{"status":200,"message":"Success","results":{"assignments":[{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegtrans252ow37gij/queueServices/default/queues/intake-work/providers/Microsoft.Authorization/RoleAssignments/76c401ce-f8a3-582c-b37b-f91bd373cd68","name":"76c401ce-f8a3-582c-b37b-f91bd373cd68","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/69a216fc-b8fb-44d8-bc22-1f3c2cd27a39","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegtrans252ow37gij/queueServices/default/queues/intake-work","principalId":"f3b032cc-7591-4ea8-bd68-d165578c576f","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegcustody252ow37gij/blobServices/default/containers/transient-intake/providers/Microsoft.Authorization/RoleAssignments/4e927216-b064-4161-a1ae-3f7ed5b86687","name":"4e927216-b064-4161-a1ae-3f7ed5b86687","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/ba92f5b4-2d11-453d-a403-e96b0029c9fe","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegcustody252ow37gij/blobServices/default/containers/transient-intake","principalId":"06b65d89-b4dd-4e64-927e-0f154b4f9427","principalType":"User"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/providers/Microsoft.Authorization/RoleAssignments/b5e742fb-f8f6-45d8-9f08-b8409420c6eb","name":"b5e742fb-f8f6-45d8-9f08-b8409420c6eb","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/4633458b-17de-408a-b874-0445c86b69e6","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g","principalId":"4f4d9606-3634-4c21-a1ee-3238351cfc69","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegtrans252ow37gij/providers/Microsoft.Authorization/RoleAssignments/f50c8915-e647-4a1d-8eb4-1e762e5854b8","name":"f50c8915-e647-4a1d-8eb4-1e762e5854b8","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/974c5e8b-45b9-4653-ba55-5f855dd0fb88","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegtrans252ow37gij","principalId":"06b65d89-b4dd-4e64-927e-0f154b4f9427","principalType":"User"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/providers/Microsoft.Authorization/RoleAssignments/aa912c7e-bcb2-4a68-b6d3-fea392b2ddbf","name":"aa912c7e-bcb2-4a68-b6d3-fea392b2ddbf","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/b86a8fe4-44ce-4948-aee5-eccb2c155cd7","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g","principalId":"06b65d89-b4dd-4e64-927e-0f154b4f9427","principalType":"User"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/automation-mcp-client-secret/providers/Microsoft.Authorization/RoleAssignments/2545b2da-075c-4841-8dc7-dd53f5ff5426","name":"2545b2da-075c-4841-8dc7-dd53f5ff5426","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/4633458b-17de-408a-b874-0445c86b69e6","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/automation-mcp-client-secret","principalId":"f3b032cc-7591-4ea8-bd68-d165578c576f","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/dvsa-api-key/providers/Microsoft.Authorization/RoleAssignments/a609ab81-5e8b-42da-b52c-9ee3b3a293a2","name":"a609ab81-5e8b-42da-b52c-9ee3b3a293a2","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/4633458b-17de-408a-b874-0445c86b69e6","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/dvsa-api-key","principalId":"4f4d9606-3634-4c21-a1ee-3238351cfc69","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/box-client-secret/providers/Microsoft.Authorization/RoleAssignments/0f7491ba-3aea-42eb-84e6-9128f845e0fb","name":"0f7491ba-3aea-42eb-84e6-9128f845e0fb","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/4633458b-17de-408a-b874-0445c86b69e6","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/box-client-secret","principalId":"f3b032cc-7591-4ea8-bd68-d165578c576f","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/box-config-json/providers/Microsoft.Authorization/RoleAssignments/d28bc17b-3b2d-48a8-903d-b100bd5cc50d","name":"d28bc17b-3b2d-48a8-903d-b100bd5cc50d","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/4633458b-17de-408a-b874-0445c86b69e6","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/box-config-json","principalId":"f3b032cc-7591-4ea8-bd68-d165578c576f","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/dvsa-client-secret/providers/Microsoft.Authorization/RoleAssignments/87806fcf-80ba-42c9-bc5a-33ac07b84f7b","name":"87806fcf-80ba-42c9-bc5a-33ac07b84f7b","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/4633458b-17de-408a-b874-0445c86b69e6","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/dvsa-client-secret","principalId":"4f4d9606-3634-4c21-a1ee-3238351cfc69","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/dvsa-client-id/providers/Microsoft.Authorization/RoleAssignments/fdc5632c-43d8-405c-964f-dafa8aab7727","name":"fdc5632c-43d8-405c-964f-dafa8aab7727","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/4633458b-17de-408a-b874-0445c86b69e6","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/dvsa-client-id","principalId":"4f4d9606-3634-4c21-a1ee-3238351cfc69","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/dvla-api-key/providers/Microsoft.Authorization/RoleAssignments/6ccf81c7-de93-499e-9014-ce0f9d155d7c","name":"6ccf81c7-de93-499e-9014-ce0f9d155d7c","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/4633458b-17de-408a-b874-0445c86b69e6","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/dvla-api-key","principalId":"4f4d9606-3634-4c21-a1ee-3238351cfc69","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/box-client-secret/providers/Microsoft.Authorization/RoleAssignments/b564ca8b-f5c3-4a2c-af49-20d72d90c5a8","name":"b564ca8b-f5c3-4a2c-af49-20d72d90c5a8","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/4633458b-17de-408a-b874-0445c86b69e6","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/box-client-secret","principalId":"4f4d9606-3634-4c21-a1ee-3238351cfc69","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/box-config-json/providers/Microsoft.Authorization/RoleAssignments/af21f046-1b52-4df6-acf3-91834df909fc","name":"af21f046-1b52-4df6-acf3-91834df909fc","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/4633458b-17de-408a-b874-0445c86b69e6","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/secrets/box-config-json","principalId":"4f4d9606-3634-4c21-a1ee-3238351cfc69","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g/providers/Microsoft.Authorization/RoleAssignments/97d23517-f35d-438f-bfb9-7890e5fbd617","name":"97d23517-f35d-438f-bfb9-7890e5fbd617","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/21090545-7ca7-4776-b22c-e363652d74d2","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.KeyVault/vaults/pegasusprodkv252ow37g","principalId":"06b65d89-b4dd-4e64-927e-0f154b4f9427","principalType":"User"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegcustody252ow37gij/blobServices/default/containers/box-links/providers/Microsoft.Authorization/RoleAssignments/01e813bb-daed-5ea8-a161-1933fdb23525","name":"01e813bb-daed-5ea8-a161-1933fdb23525","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/ba92f5b4-2d11-453d-a403-e96b0029c9fe","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegcustody252ow37gij/blobServices/default/containers/box-links","principalId":"f3b032cc-7591-4ea8-bd68-d165578c576f","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegcustody252ow37gij/blobServices/default/containers/transient-intake/providers/Microsoft.Authorization/RoleAssignments/6f6f9fbe-fd94-5d61-8a7a-0daaa9bff0cb","name":"6f6f9fbe-fd94-5d61-8a7a-0daaa9bff0cb","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/b7e6dc6d-f1e8-4753-8033-0f276bb0955b","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegcustody252ow37gij/blobServices/default/containers/transient-intake","principalId":"4f4d9606-3634-4c21-a1ee-3238351cfc69","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegtrans252ow37gij/providers/Microsoft.Authorization/RoleAssignments/c90358ce-2823-58bb-823d-64215876693f","name":"c90358ce-2823-58bb-823d-64215876693f","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/974c5e8b-45b9-4653-ba55-5f855dd0fb88","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegtrans252ow37gij","principalId":"4f4d9606-3634-4c21-a1ee-3238351cfc69","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegtrans252ow37gij/providers/Microsoft.Authorization/RoleAssignments/28c14b9e-9512-5c09-9640-e0be19e4a852","name":"28c14b9e-9512-5c09-9640-e0be19e4a852","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegtrans252ow37gij","principalId":"4f4d9606-3634-4c21-a1ee-3238351cfc69","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegcustody252ow37gij/blobServices/default/containers/authentication-ring/providers/Microsoft.Authorization/RoleAssignments/f9486296-1ac8-5382-a841-388f05b82d4a","name":"f9486296-1ac8-5382-a841-388f05b82d4a","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/ba92f5b4-2d11-453d-a403-e96b0029c9fe","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegcustody252ow37gij/blobServices/default/containers/authentication-ring","principalId":"f3b032cc-7591-4ea8-bd68-d165578c576f","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegtrans252ow37gij/providers/Microsoft.Authorization/RoleAssignments/fd704884-3d68-52d1-8f81-ae1d13bd9dff","name":"fd704884-3d68-52d1-8f81-ae1d13bd9dff","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/b7e6dc6d-f1e8-4753-8033-0f276bb0955b","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegtrans252ow37gij","principalId":"4f4d9606-3634-4c21-a1ee-3238351cfc69","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegcustody252ow37gij/blobServices/default/containers/box-links/providers/Microsoft.Authorization/RoleAssignments/bdc4e0b2-1bac-58d4-8999-bbe69f5e9e4d","name":"bdc4e0b2-1bac-58d4-8999-bbe69f5e9e4d","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/ba92f5b4-2d11-453d-a403-e96b0029c9fe","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegcustody252ow37gij/blobServices/default/containers/box-links","principalId":"4f4d9606-3634-4c21-a1ee-3238351cfc69","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.ContainerRegistry/registries/pegasusprodacr252ow37gij/providers/Microsoft.Authorization/RoleAssignments/ebd71c7c-3410-5bc8-9b4a-9d79b0c53734","name":"ebd71c7c-3410-5bc8-9b4a-9d79b0c53734","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/7f951dda-4ed3-4680-a7ca-43fe172d538d","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.ContainerRegistry/registries/pegasusprodacr252ow37gij","principalId":"f3b032cc-7591-4ea8-bd68-d165578c576f","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegcustody252ow37gij/blobServices/default/containers/transient-intake/providers/Microsoft.Authorization/RoleAssignments/8dad1c97-5634-5037-83c0-2184d5dc571b","name":"8dad1c97-5634-5037-83c0-2184d5dc571b","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/b7e6dc6d-f1e8-4753-8033-0f276bb0955b","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Storage/storageAccounts/pegcustody252ow37gij/blobServices/default/containers/transient-intake","principalId":"f3b032cc-7591-4ea8-bd68-d165578c576f","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Insights/components/pegasus-prod-appi-252ow37gij/providers/Microsoft.Authorization/RoleAssignments/5a9eda7a-5a74-522b-9b80-ec1139b38c3e","name":"5a9eda7a-5a74-522b-9b80-ec1139b38c3e","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/3913510d-42f4-4e42-8a64-420c390055eb","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Insights/components/pegasus-prod-appi-252ow37gij","principalId":"4f4d9606-3634-4c21-a1ee-3238351cfc69","principalType":"ServicePrincipal"},{"id":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Insights/components/pegasus-prod-appi-252ow37gij/providers/Microsoft.Authorization/RoleAssignments/26f2d4d9-2763-585a-ba06-09dcfd61ab25","name":"26f2d4d9-2763-585a-ba06-09dcfd61ab25","roleDefinitionId":"/providers/Microsoft.Authorization/RoleDefinitions/3913510d-42f4-4e42-8a64-420c390055eb","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourcegroups/rg-pegasus-prod/providers/Microsoft.Insights/components/pegasus-prod-appi-252ow37gij","principalId":"f3b032cc-7591-4ea8-bd68-d165578c576f","principalType":"ServicePrincipal"}],"areResultsTruncated":false},"duration":0}
```

### Supplemental read-only identity evidence — 2026-08-25

Azure MCP subscription_list returned the pinned subscription as enabled/default and group_list returned rg-pegasus-prod in uksouth. The Azure CLI was used only as a supplemental read-only check because the exposed Azure MCP role router returns assignments but not the caller's resolved role name:

```json
{
  "id": "e6076573-23a5-46a8-acef-7e22d264e5db",
  "tenantId": "858cf5b3-aa0a-47a6-9b40-4851fd0afa94",
  "userType": "user"
}
[
  {"principalType":"User","role":"Owner","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db"},
  {"principalType":"User","role":"Owner","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db"},
  {"principalType":"User","role":"Foundry User","scope":"/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db"}
]
```

This is authenticated and read-capable, but it is not a reader-level least-privilege session. No Azure write-capable command was issued. FND-021 must not claim the reader-role criterion is satisfied; smallest clearing action is rerun the audit under an identity with Reader access scoped to the pinned subscription/resource group (without changing this Owner session).

### Supplemental read-only Azure CLI output — 2026-08-25

The Azure MCP routers do not expose storage queue listing, Container App show/revision, Function App settings-name, Log Analytics workspace show, Application Insights component show, or budget commands in their advertised child schemas. The following Azure CLI commands were run as supplemental read-only reads only; values were filtered to omit secrets and setting values.

```text
--- az storage queue list (names only) ---
WARNING: Command group 'storage queue' is in preview and under development. Reference and support levels: https://aka.ms/CLI_refstatus
[
  "external-work",
  "external-work-poison",
  "intake-work",
  "intake-work-poison"
]
--- az containerapp show (safe fields; env names only) ---
{
  "activeRevisionsMode": "Single",
  "envNames": [
    "APPLICATIONINSIGHTS_CONNECTION_STRING",
    "APPLICATIONINSIGHTS_AUTHENTICATION_STRING",
    "APPLICATIONINSIGHTS_ENABLEADAPTIVESAMPLING",
    "ASPNETCORE_ENVIRONMENT",
    "ASPNETCORE_HTTP_PORTS",
    "Runtime__Profile",
    "ConnectionStrings__Pegasus",
    "KEY_VAULT_URI",
    "TransportStorage__AccountName",
    "CustodyStorage__AccountName",
    "CustodyStorage__ServiceUri",
    "AZURE_CLIENT_ID",
    "AzureIdentity__WebClientId",
    "Graph__BaseUri",
    "Box__BaseUri",
    "Box__UploadUri",
    "Box__RootFolderId",
    "Box__ConfigJson",
    "Box__ClientSecret",
    "Eva__AcceptedMapping__Key",
    "Eva__AcceptedMapping__Version",
    "Eva__AcceptedMapping__EvidenceReference",
    "Features__AutomationMcp",
    "AutomationMcp__ClientId",
    "AutomationMcp__ClientSecret",
    "AutomationMcp__PublicOrigin",
    "AutomationMcp__RedirectUris"
  ],
  "environmentId": "/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.App/managedEnvironments/pegasus-prod-aca-env-252ow37gij",
  "image": "pegasusprodacr252ow37gij.azurecr.io/pegasus/web@sha256:08f5f605b511f3a8d16a6702a071aa72e1403281b0b8289ddaae46601c86f105",
  "ingress": {
    "additionalPortMappings": null,
    "allowInsecure": false,
    "clientCertificateMode": null,
    "corsPolicy": null,
    "customDomains": null,
    "exposedPort": 0,
    "external": true,
    "fqdn": "pegasus-prod-web-252ow37gij.ashymushroom-676209e5.uksouth.azurecontainerapps.io",
    "ipSecurityRestrictions": null,
    "stickySessions": null,
    "targetPort": 8080,
    "traffic": [
      {
        "latestRevision": true,
        "weight": 100
      }
    ],
    "transport": "Auto"
  },
  "name": "pegasus-prod-web-252ow37gij",
  "provisioningState": "Succeeded",
  "revisionSuffix": "7e9465b00603"
}
--- az containerapp revision list ---
[
  {
    "active": true,
    "created": "2026-08-25T01:13:07+00:00",
    "healthState": "Healthy",
    "name": "pegasus-prod-web-252ow37gij--7e9465b00603",
    "replicas": 1
  }
]
--- az functionapp appsetting names only ---
[
  "Runtime__Profile",
  "AzureWebJobsStorage__accountName",
  "AzureWebJobsStorage__credential",
  "AzureWebJobsStorage__clientId",
  "AzureIdentity__WorkerClientId",
  "IntakeStorage__ServiceUri",
  "IntakeQueue__ServiceUri",
  "ExternalWorkQueue__ServiceUri",
  "PendingWorkDispatchSchedule",
  "IntakeStagedArtifactReconciliationSchedule",
  "ApprovedInboxPollSchedule",
  "SentEvidencePollSchedule",
  "DueWorkSweepSchedule",
  "AzureWebJobs.PendingWorkDispatchFunction.Disabled",
  "AzureWebJobs.IntakeWorkFunction.Disabled",
  "AzureWebJobs.IntakePoisonFunction.Disabled",
  "AzureWebJobs.StagedArtifactReconciliationFunction.Disabled",
  "AzureWebJobs.InboxPollFunction.Disabled",
  "AzureWebJobs.SentEvidencePollFunction.Disabled",
  "AzureWebJobs.DueWorkSweepFunction.Disabled",
  "AzureWebJobs.ExternalWorkFunction.Disabled",
  "AzureWebJobs.ExternalPoisonFunction.Disabled",
  "APPLICATIONINSIGHTS_CONNECTION_STRING",
  "APPLICATIONINSIGHTS_AUTHENTICATION_STRING",
  "APPLICATIONINSIGHTS_ENABLEADAPTIVESAMPLING",
  "ConnectionStrings__Pegasus",
  "KEY_VAULT_URI",
  "TransportStorage__AccountName",
  "CustodyStorage__AccountName",
  "Graph__BaseUri",
  "Graph__MailboxId",
  "Graph__MailboxAddress",
  "Graph__InboxFolderId",
  "Graph__SentFolderId",
  "Box__BaseUri",
  "Box__UploadUri",
  "Box__RootFolderId",
  "Dvla__BaseUri",
  "Dvsa__BaseUri",
  "Dvsa__TokenUri",
  "Dvsa__Scope",
  "Box__ConfigJson",
  "Box__ClientSecret",
  "Dvla__ApiKey",
  "Dvsa__ClientId",
  "Dvsa__ClientSecret",
  "Dvsa__ApiKey"
]
--- az monitor workspace show ---
{
  "name": "pegasus-prod-logs-252ow37gij",
  "publicNetworkAccessForIngestion": "Enabled",
  "publicNetworkAccessForQuery": "Enabled",
  "retentionInDays": 31,
  "sku": "PerGB2018",
  "workspaceCapping": {
    "dailyQuotaGb": 0.1,
    "dataIngestionStatus": "RespectQuota",
    "quotaNextResetTime": "2026-08-25T03:00:00Z"
  }
}
--- az Application Insights resource show ---
{
  "disableLocalAuth": true,
  "kind": "web",
  "name": "pegasus-prod-appi-252ow37gij",
  "type": "microsoft.insights/components",
  "workspaceResourceId": "/subscriptions/e6076573-23a5-46a8-acef-7e22d264e5db/resourceGroups/rg-pegasus-prod/providers/Microsoft.OperationalInsights/workspaces/pegasus-prod-logs-252ow37gij"
}
--- az acr show ---
{
  "adminUserEnabled": false,
  "loginServer": "pegasusprodacr252ow37gij.azurecr.io",
  "name": "pegasusprodacr252ow37gij",
  "publicNetworkAccess": "Enabled",
  "sku": "Basic"
}
--- az consumption budget list (safe fields) ---
} was unexpected at this time.
C:\Users\PC\Documents\GitHub\pegasusDesktop\.worktrees\fnd-021>  "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\\..\python.exe" -IBm azure.cli consumption budget list --subscription e6076573-23a5-46a8-acef-7e22d264e5db --query [?name=='pegasus-prod-monthly'].{name:name,amount:amount,currentSpend:currentSpend,timeGrain:timeGrain,timePeriod:timePeriod,notificationNames:keys(notifications)} -o json

```

### Supplemental budget read-only output — 2026-08-25

Command: az consumption budget list --subscription e6076573-23a5-46a8-acef-7e22d264e5db --query "[?name=='pegasus-prod-monthly'].{name:name,amount:amount,currentSpend:currentSpend,timeGrain:timeGrain,timePeriod:timePeriod}" -o json

```text
WARNING: This command is in preview and under development. Reference and support levels: https://aka.ms/CLI_refstatus
[
  {
    "amount": "75.0",
    "currentSpend": {
      "amount": "29.50478827580924",
      "unit": "GBP"
    },
    "name": "pegasus-prod-monthly",
    "timeGrain": "Monthly",
    "timePeriod": {
      "endDate": "2036-08-01T00:00:00Z",
      "startDate": "2026-08-01T00:00:00Z"
    }
  }
]

```

### Independent pegasus-azure-auditor result — 2026-08-25

The bounded auditor made no file, Kanmer, or Azure mutations and exposed no secret values. It independently confirmed the pinned subscription/tenant/RG, all declared resources, both storage accounts with public access disabled, the four queues, the healthy single Web revision, nine Worker Disabled setting names, 0.1 GB/day workspace cap, App Insights workspace-based/DisableLocalAuth/90-day retention, ACR Basic/admin disabled, SQL S0/250 GB, and budget amount 75 GBP.

Independent drift findings agree with the local evidence: transport has azure-webjobs-hosts and azure-webjobs-secrets support containers; SQL has codex-current-machine-20260812 and dev-machine-1 firewall rules; the live descendant-scope RBAC read has 26 assignments including Key Vault grants, user/duplicate grants, Key Vault Secrets Officer, and Azure Service Bus Data Sender; ACA diagnostic settings read empty despite the declared Bicep resource; SQL master is platform-managed and omitted from the application register; App Insights retention is 90 days.

The auditor's cost-usage read did not prove a usable spend/forecast amount. The separate read-only budget list proves currentSpend 29.50478827580924 GBP against amount 75.0 GBP; no forecast amount is asserted.

Tool limitations recorded: Azure MCP keyvault secret-name call returned exactly "Operation cancelled by user (decline)"; no secret value was requested. The installed Azure MCP child schemas lack queue listing, Container App detail/revision, Function App setting-name, workspace-show, App Insights component-show, and budget commands, so safe-field Azure CLI reads supplemented those surfaces. The audit identity is over-privileged Owner rather than reader-only; a reader-scoped rerun remains required for the least-privilege criterion.

### Supplemental Key Vault names-only evidence — 2026-08-25

Azure MCP keyvault_secret_get returned "Operation cancelled by user (decline)" before any value was requested. A safe read-only Azure CLI fallback listed names only:

```json
[
  "automation-mcp-client-secret",
  "box-client-secret",
  "box-config-json",
  "dvla-api-key",
  "dvsa-api-key",
  "dvsa-client-id",
  "dvsa-client-secret"
]
[]
```

The first array is secret names; the second is certificate names. No secret values were queried or recorded.

### Azure MCP pricing read — 2026-08-25

Read-only exact-SKU queries for Standard_LRS Storage and S0 SQL in uksouth both returned `prices: []`. No retail estimate is asserted. The budget read remains the authoritative live cost context available here: amount 75.0 GBP, currentSpend 29.50478827580924 GBP; no forecast amount was exposed. No change is proposed.

### Deferred tagging approval text — 2026-08-25

No tag was applied. Any future write must enumerate the exact resource IDs from the attached group inventory and be separately approved:

> Request change of tags on the explicitly enumerated resource IDs in subscription e6076573-23a5-46a8-acef-7e22d264e5db, resource group rg-pegasus-prod: add or merge desktop-conversion=phase0-inventory, owner=<approved owner>, and codepath=<verified path:line>; because this is Phase 0 inventory ownership metadata and not a permission or runtime change; applied only through the approved IaC/release route after the exact-target approval; rollback by removing only those three added tag keys/values and restoring the prior tag set; approver: the operator named in the exact-target approval. Nothing else changes.

This text is recorded for a future write ticket. It is not authorization and no Azure write occurred.

### Cost forecast endpoint attempt — 2026-08-25

A read-only Azure Cost Management forecast query was attempted at the pinned subscription scope. The first request returned HTTP 415 Unsupported Media Type; the JSON-header retry returned HTTP 400 because the CLI body was serialized as an invalid Usage value. No resource or budget state was changed. Together with the budget read and Azure MCP pricing empty results, this does not prove a forecast amount; U-9 remains open.
