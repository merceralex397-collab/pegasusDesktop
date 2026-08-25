# Desktop conversion threat register

This register joins the nine proposal §17.3 threats to an existing or planned
control, its authoritative location, the ticket that tests it, and the residual
risk or owner. It is a planning and review index; it does not claim that a
planned control or test is already delivered.

| # | Threat (§17.3) | Control | Where the control lives | Test | Residual risk / owner |
| --- | --- | --- | --- | --- | --- |
| 1 | Lost or shared workstation session | Short-lived, revocable session with idle and absolute limits; account revocation is checked on each request. | `src/Pegasus.Web/Program.cs:353,368-457`; `DSK-04-08` | [[DSK-10-04]] | A shared Windows account remains an operator-control risk; owner: workstation operations. |
| 2 | Leaked service credential | The desktop carries no production database, provider, or Azure secret; server access uses managed identity and the desktop refresh token is DPAPI-protected. | `src/Pegasus.Web/Program.cs:158-176`; `DSK-04-07` | [[DSK-10-04]] | Server-side secret rotation and certificate custody remain operational responsibilities; owner: release operations. |
| 3 | Accidental over-permission | Staff access rights fail closed and server-side authorization is required for every protected operation. | `src/Pegasus.Core/Identity/StaffAuthorization.cs:1-40`; `src/Pegasus.Web/Program.cs:517-522` | [[DSK-10-05]] | A missing grant on a newly added write remains possible until the runtime-role check lands; owner: [[DSK-10-18]]. |
| 4 | Malicious or malformed attachment | Reader and envelope limits run before Core, including file size, file count, multipart overhead, safe paths, and content validation. | `src/Pegasus.Core/Intake/IntakeContracts.cs:7-57`; `docs/current-architecture.md:222-236` | [[DSK-10-06]] | Novel parser defects and provider-specific content remain residual risk; owner: intake and security test maintainers. |
| 5 | Duplicate or conflicting data writes | Commands carry expected versions and operation keys; the gateway records sensitive actions and rejects stale or replayed mutations. | `DSK-03-08`; `src/Pegasus.Core/Identity/IdentityContracts.cs:98-137` | [[DSK-10-05]] | Correctness depends on every new command adopting the shared contract; owner: gateway implementation. |
| 6 | Compromised update package/feed | Signed MSIX packages and a trusted release manifest are required; the in-house UNC/SMB feed is protected by its ACLs rather than public hosting. | `DSK-09-03`; `DSK-09-08`; `D-003` in `docs/desktop/README.md` | [[DSK-10-03]] | Signing-key loss or feed compromise requires the renewal and compromise procedure; owner: [[DSK-09-14]]. |
| 7 | Sensitive information in logs/temp files | Logs are structured, redacted, bounded, and correlation-based; local cache and temporary-file controls exclude routine PII, document content, and tokens. | `DSK-10-09`; `docs/desktop/10-security-observability-performance/README.md:2` | [[DSK-10-07]], [[DSK-10-09]] | Redaction is only as complete as the fields added by later features; owner: desktop diagnostics maintainers. |
| 8 | Third-party provider outage | Provider calls expose explicit unavailable/transient/terminal outcomes and the desktop operation model preserves a recoverable uncertain state. | `DSK-10-17`; `docs/desktop/10-security-observability-performance/README.md:3` | [[DSK-10-17]] | Provider recovery time and external availability remain outside Pegasus control; owner: provider integration owners. |
| 9 | Administrator error | Sensitive operations have durable action history and security records; the administrator health surface reports dependency state without secrets. | `src/Pegasus.Core/Identity/IdentityContracts.cs:98-137`; `DSK-10-15`; `DSK-03-15` | [[DSK-10-15]], `DSK-03-15` | Human approval and exact-target change controls remain required; owner: operations. |

## Not prioritised (§17.2)

The following controls are intentionally not prioritised:

- Code obfuscation;
- anti-debugging;
- anti-tamper logic beyond package signing;
- hiding API routes;
- licensing enforcement;
- public marketplace hardening;
- multi-tenant isolation.

A ticket proposing one of these controls is out of scope without a new accepted
technical or product decision.

## Secret and PII pattern list

The package, desktop configuration, exported diagnostics bundle, and routine
logs must be scanned for these patterns. The list is shared by [[DSK-10-03]]
and [[DSK-10-09]]; a change here must be reflected in both.

| Pattern | What it catches |
| --- | --- |
| `Server=tcp:` or `Initial Catalog=` | SQL connection strings |
| `https://*.vault.azure.net/secrets/` | Key Vault secret URIs |
| `InstrumentationKey=` or `APPLICATIONINSIGHTS_CONNECTION_STRING` | Telemetry connection material |
| `Bearer eyJ` | JWT bearer tokens |
| `client_secret` | OAuth client secret fields |
| `AccountKey=` | Storage account keys |
| `-----BEGIN .* PRIVATE KEY-----` | Private-key material |
| The literal password value at `src/Pegasus.Web/appsettings.json:12` | The committed bootstrap verification account, until [[DSK-10-02]] removes it |

The scanner treats these as case-sensitive source patterns unless the owning
test explicitly documents a case-insensitive rule. It must report the path and
line without copying matched secret or case content into logs.

## Certificate and key custody

| Asset | Required control | Authority / owner |
| --- | --- | --- |
| Production signing certificate and private key | Self-managed certificate; private key stays on the signing host under a restricted ACL and is never stored as a GitHub secret. | D-002; `DSK-09-08`; renewal and compromise procedure [[DSK-09-14]] |
| App Installer update feed and manifest | In-house UNC share over SMB with controlled ACLs; package and manifest signatures are validated before installation. | D-003; `DSK-09-03`; feed operations [[DSK-09-10]] |

Loss or compromise of the signing key is an incident: follow the R5 renewal
and compromise variant, block the affected release path, and record the exact
operator-approved recovery action.
