# Checklist — PLAT-003

## Implementation

- [ ] 1. Orientation. Read the plan row, the threat register section `## Secret and PII pattern list` created by [[DSK-10-01]], and the CI desktop lane added by `DSK-09-05` in `.github/workflows/ci.yml`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`.

- [ ] 2. Branch `task/dsk-10-03-package-secret-scan` from `dev`.

- [ ] 3. Create `eng/packaging/Test-PackageSecrets.ps1` with `[CmdletBinding()] param([Parameter(Mandatory)][string] $PackagePath, [string[]] $AdditionalPath, [switch] $ExpectMatch)`, `Set-StrictMode -Version Latest` and `$ErrorActionPreference = 'Stop'` — the same header shape every script in `scripts/` uses (see `scripts/Test-MigrationGrants.ps1:1-6`).

- [ ] 4. In that script, expand the package: use `makeappx unpack /p <PackagePath> /d <temp>` (search `microsoft_docs_search` for `makeappx unpack` if the switch names need confirming) and fall back to `Expand-Archive` only when `makeappx` is absent. Fail with a named error when neither is available — never silently scan nothing.

- [ ] 5. Define the pattern set as a single ordered hashtable in the script, copied verbatim from the threat register's pattern list: SQL connection strings (`Server=tcp:`, `Initial Catalog=`), `https://[a-z0-9-]+\.vault\.azure\.net/secrets/`, `InstrumentationKey=`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `AccountKey=`, `client_secret`, `-----BEGIN [A-Z ]*PRIVATE KEY-----`, `Bearer eyJ[A-Za-z0-9_-]+\.`, and the literal `Pegasus-UI-Verify`. Add a comment naming the register as the source of truth so the two never drift.

- [ ] 6. Scan every text-like file in the unpacked payload plus each `-AdditionalPath` (desktop `appsettings*.json`, the `.appinstaller`, and a diagnostics bundle produced by [[DSK-10-09]]). Skip binary payloads by extension, and report each hit as `path:line:pattern-name` without echoing the matched value. Exit 1 on any hit, 0 otherwise.

- [ ] 7. Add the negative test: `eng/packaging/Test-PackageSecrets.Tests.ps1` (or an xunit shim in `tests/Pegasus.Packaging.Tests`, whichever `DSK-08-10` established) that plants a fixture file containing a fake connection string, runs the scanner with `-ExpectMatch`, asserts exit code 1 and the reported pattern name, and then asserts a clean fixture exits 0. The planted secret must be an obvious fake (for example `Server=tcp:example.invalid;Password=NOT-A-REAL-SECRET`) and must live only in the fixture folder.

- [ ] 8. Wire the scanner into `.github/workflows/ci.yml` as a step of the existing desktop packaging job created by `DSK-09-05` — not as a new job — using `shell: pwsh` and `run: ./eng/packaging/Test-PackageSecrets.ps1 -PackagePath <the artifact path that job already produced>`. Adding a second Windows job would double the 2× private-runner cost (C-01).

- [ ] 9. Run the scanner locally against a dev-signed MSIX from `DSK-02-14`: expect exit 0 and a printed count of files scanned. Then re-run with the planted fixture: expect exit 1 and the pattern name in the output.

- [ ] 10. Remove the planted secret from the working tree before the PR and confirm with `git status --porcelain` that only the intended files remain. State in the post-implementation report that the fixture value is fake and was never a live credential.

- [ ] 11. Document the step in `docs/desktop/10-security-observability-performance/README.md` § 8 and record in the threat register that the "sensitive information in logs/temp files" and "leaked service credential" rows now have an automated test.

- [ ] 12. Record `## Simplification pass` with today's date over the branch diff in the ticket's `plan` document, open the PR into `dev`, and hand review to `pegasus-desktop-reviewer`.

## Verification

- [ ] `pwsh ./eng/packaging/Test-PackageSecrets.ps1 -PackagePath <dev MSIX>` — expected: exit code 0 and a scanned-file count.
- [ ] `pwsh ./eng/packaging/Test-PackageSecrets.ps1 -PackagePath <fixture with planted secret> -ExpectMatch` — expected: exit code 1 and the pattern name printed.
- [ ] CI run of `repository-check` on the PR — expected: the desktop packaging job green with the new step visible in its log.

## Progress notes

Record only factual progress here; unresolved decisions remain in `open-questions`.
