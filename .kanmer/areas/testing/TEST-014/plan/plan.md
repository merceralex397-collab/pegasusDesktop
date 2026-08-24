# Plan — TEST-014 Vulnerability and SBOM step

## Governing documents

This is a repository hygiene chore. It consumes existing package management and CI conventions; it does not create a separate security policy.

## Steps

1. Inspect current package management, lock-file and CI output conventions.
2. Add the dotnet list package vulnerable include-transitive check using locked restore inputs.
3. Decide whether Syft output is needed only after confirming existing tooling/license constraints; keep it optional rather than adding an unneeded dependency.
4. Store report output as CI evidence and make actionable vulnerabilities fail or surface according to the documented policy.

## Verification

- [ ] The vulnerability command runs against the solution after locked restore.
- [ ] Transitive packages are included.
- [ ] Optional SBOM path is absent unless its real consumer is approved.
