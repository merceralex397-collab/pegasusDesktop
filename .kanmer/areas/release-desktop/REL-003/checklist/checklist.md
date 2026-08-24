# Checklist — REL-003

Derived from `plan`, one box per step, in plan order. Tick with
`set_ticket_doc(doc: "checklist")` as you complete them; append progress notes below
rather than rewriting.

- [ ] Read `docs/desktop/09-release-update-and-distribution/appinstaller-template.md` in full, plus the area plan § 5 row `DSK-09-03` and § 7 traps; run `get_doc_gates REL-003` and `take_ticket REL-003`
- [ ] Load `winui-packaging`, then `microsoft_docs_fetch` <https://learn.microsoft.com/windows/msix/app-installer/update-settings> and record in the ticket scratch the confirmed attribute spellings and the `UpdateBlocksActivation` requires-`ShowPrompt` rule, with the fetch date
- [ ] Create `eng/packaging/Pegasus.appinstaller.template.xml` by copying the ticket body's fenced XML byte for byte, comments and the commented-out `UpdateUris` block included
- [ ] Create `eng/packaging/New-AppInstaller.ps1` with the repository script header (`Set-StrictMode -Version Latest`, `$ErrorActionPreference = 'Stop'`), substituting the five placeholders from `desktop-release-manifest.json`
- [ ] Make `New-AppInstaller.ps1` fail with a named message if any `<…>` placeholder survives substitution, and assert the `<feed>/<channel>` result is a `\\<host>\<share>\<channel>` UNC path with no trailing separator and no drive letter
- [ ] Create `eng/packaging/Test-AppInstaller.ps1` with parameters `-AppInstallerPath`, `-Channel`, `-ManifestPath`, `-PreviousAppInstallerPath`, `-Rollback`, reading the document with a namespace-prefixed XPath bound to `http://schemas.microsoft.com/appx/appinstaller/2021`
- [ ] Implement validator check 1 (namespace is the 2021 schema) with its own named failure message
- [ ] Implement check 2 (`Uri` equals `<feed>/<channel>/Pegasus.appinstaller` for the channel) with its own named failure message
- [ ] Implement check 3 (`.appinstaller` `Version` strictly greater than the previous published version, four-part `[version]` compare) and make it pass with a recorded note when `-PreviousAppInstallerPath` is absent
- [ ] Implement check 4 (`MainPackage/@Name`, `@ProcessorArchitecture`, `@Version` vs manifest `version`, `@Publisher` vs manifest `signerSubject`) with its own named failure message
- [ ] Implement check 5 (`MainPackage/@Uri` target SHA-256 equals manifest `packageSha256`), resolving a local target relative to the `.appinstaller`'s own directory
- [ ] Implement check 6 (`OnLaunch` has `HoursBetweenUpdateChecks="0"`, `ShowPrompt="true"`, `UpdateBlocksActivation="true"`; `ForceUpdateFromAnyVersion` true; `AutomaticBackgroundTask` present)
- [ ] Implement check 7 (no `Dependencies` element unless the manifest says framework-dependent)
- [ ] Implement check 8 (a lower `MainPackage/@Version` passes only with `-Rollback` **and** `ForceUpdateFromAnyVersion="true"`)
- [ ] Use a `[version]` cast on both sides of every version comparison in checks 3, 4 and 8 — no string compares
- [ ] Make the validator collect all results, print a pass/fail list to stdout, and `exit 1` if any check failed
- [ ] Create the eight failing fixtures in `eng/packaging/fixtures/appinstaller/` (`schema-2017.xml`, `wrong-uri.xml`, `version-not-monotonic.xml`, `publisher-mismatch.xml`, `hash-mismatch.xml`, `missing-showprompt.xml`, `unexpected-dependencies.xml`, `downgrade-without-rollback.xml`), each the valid document with exactly one attribute changed
- [ ] Create `valid-pilot.xml` and `valid-prod.xml` plus their paired fixture `desktop-release-manifest.json` files, so Publisher and hash come from the manifest and never from a literal in the validator
- [ ] Set `version-not-monotonic.xml`'s values to straddle a ten boundary (for example previous `1.0.10.0`, candidate `1.0.9.0`) so a string compare would be caught
- [ ] Create `eng/packaging/Test-TestAppInstaller.ps1` in the shape of `scripts/Test-CiChangeFlags.ps1:9-30`, asserting each fixture produces exactly its own named failure and both valid fixtures exit `0`
- [ ] Diff the created template against `appinstaller-template.md` § Template and confirm they are identical, comments included
- [ ] Verification run: `pwsh ./eng/packaging/Test-TestAppInstaller.ps1` (exit `0`, ten expectations met); `pwsh ./eng/packaging/Test-AppInstaller.ps1 -AppInstallerPath ./eng/packaging/fixtures/appinstaller/schema-2017.xml -Channel pilot -ManifestPath <paired fixture manifest>` (non-zero, names the namespace check); `Select-Xml -Path ./eng/packaging/Pegasus.appinstaller.template.xml -XPath '/*'` (root namespace is the 2021 schema) — this box produces `proof`
- [ ] State in the proof that these tests ran **locally**, because `scripts/Get-CiChangeFlags.ps1:11` does not match `^eng/` and no CI lane executes them until `DSK-09-05` (board `REL-005`) lands
- [ ] Record the dated `## Simplification pass` in the `plan` document over this branch's own diff (not `n/a — docs-only`; this branch adds scripts and fixtures)

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
