# Plan — TEST-011 Security test set

## Governing documents

This ticket remains docs_todo: true until the planned desktop governing documents are authored. The local Test/UAT and locked-decision material is binding now; do not create a competing product document in this task.

## Chosen approach

Build security tests for token lifecycle, disabled account, roles, direct-object access, malformed uploads, unsafe paths, tampered manifests, version spoofing, temp ACLs and secret/log scanning.

## Steps

1. Map each listed threat to its current gateway, desktop or packaging boundary.
2. Add focused contract tests for auth/token/role/object access and malformed input.
3. Add package/file-system checks for manifest/version/temp ACL/secret-log exposure.
4. Run narrow tests and record any unverified environment-dependent check as such.

## Verification

- Role/disabled/stale-token tests fail closed.
- Malformed upload/path tests cannot reach unsafe storage path.
- Package/log scan reports no credential or secret exposure.

## Risks

Security tests use synthetic technical fixtures only; no live credentials or unauthorized attack target.

Use the detected runner/framework and record exact command output when implementation begins. Complete a simplification pass and independent review before merge.
