# Checklist — FEAT-031 Box broker endpoints

- [ ] Inventory existing Box ports, Core use cases and custody/audit calls.
- [ ] Add minimal api-v1 broker contracts/routes that translate to the existing policy owner.
- [ ] Apply authorization, problem-details and no-secret/no-reusable-URL constraints.
- [ ] Add contract tests for allowed, denied, expired and failure paths.
- [ ] Verify: Focused gateway contract tests cover auth, response shape and failure details.
- [ ] Verify: Package/log scan design contains no provider secret or durable Box id.
- [ ] Verify: No direct desktop Box SDK call is introduced.
- [ ] Record simplification and independent review evidence.
