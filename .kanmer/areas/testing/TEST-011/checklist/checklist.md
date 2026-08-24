# Checklist — TEST-011 Security test set

- [ ] Map each listed threat to its current gateway, desktop or packaging boundary.
- [ ] Add focused contract tests for auth/token/role/object access and malformed input.
- [ ] Add package/file-system checks for manifest/version/temp ACL/secret-log exposure.
- [ ] Run narrow tests and record any unverified environment-dependent check as such.
- [ ] Verify: Role/disabled/stale-token tests fail closed.
- [ ] Verify: Malformed upload/path tests cannot reach unsafe storage path.
- [ ] Verify: Package/log scan reports no credential or secret exposure.
- [ ] Record exact test command/output, simplification pass and independent review.
