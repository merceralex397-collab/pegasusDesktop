# Checklist — REL-011

Derived from `plan`, one box per step, in plan order. Boxes marked **(operator)** are
performed by the operator on the feed or a pilot workstation. Tick with
`set_ticket_doc(doc: "checklist")`; append progress notes below rather than rewriting.

- [ ] Read `runbooks.md` § R4 in full plus `appinstaller-template.md` § Known behaviours and the area plan § 5 row `DSK-09-13`; run `get_doc_gates REL-011` and `take_ticket REL-011`
- [ ] `microsoft_docs_fetch` <https://learn.microsoft.com/windows/msix/app-installer/update-settings> and quote the `ForceUpdateFromAnyVersion` downgrade sentence into R4 with its URL and fetch date
- [ ] Confirm the feed holds at least two `Pegasus_*_x64.msix` files for the channel (`Get-ChildItem <feed>\<channel>`); if only one, stop and raise a defect against `DSK-09-10` (board `REL-008`)
- [ ] Confirm `Get-FileHash <feed>\<channel>\Pegasus_<prev-ver>_x64.msix` matches the `packageSha256` in that release's `desktop-release-manifest.json`
- [ ] **(operator)** Decide the scope (`pilot` or `prod`) and obtain `FEED PUBLISH GRANTED <channel> <prev-ver>` in the wording confirmed by `DSK-09-11` (board `REL-009`), without reusing `MERGE AUTH GRANTED`
- [ ] Rewrite R4 step 2 as the **first** operational action: read the current minimum client version from the admin setting and, if it was raised to the defective version, lower it before any publish
- [ ] Rewrite R4 step 3 with **concrete values** in one sentence — the `.appinstaller` `Version` increasing while `MainPackage Version` decreases — plus `ForceUpdateFromAnyVersion="true"` and `MainPackage Version=<prev-ver>`
- [ ] Run `pwsh ./eng/packaging/Test-AppInstaller.ps1 … -Rollback` on the rollback manifest and confirm exit `0`
- [ ] Run the same command **without** `-Rollback`, confirm a non-zero exit with the named downgrade failure, and record that refusal as evidence the guard exists
- [ ] Stack rehearsal: install `<ver>`, publish the rollback manifest for `<prev-ver>` with `Publish-Feed`, relaunch, and capture `Get-AppxPackage CollisionEngineers.Pegasus` **before and after** as two version strings
- [ ] **(operator)** Pilot rehearsal: launch after the rollback publish, confirm the downgrade is applied, and confirm the client signs in and completes a routine workflow against the gateway at the lowered minimum; hand back the transcript and a version screenshot
- [ ] Record whether the downgrade happened on relaunch or required the R4 step 5 fallback
- [ ] Prove the machine-level fallback once on a test machine: `Get-AppxPackage CollisionEngineers.Pegasus | Remove-AppxPackage`, then install from the channel `.appinstaller`
- [ ] Write R4 step 5 to include R7's fact that local preferences live in the package's `ApplicationData` and are removed with the package
- [ ] Record the rollback row (channel, `.appinstaller` version, package version, who approved, when) in `docs/operations.md` in the same task
- [ ] Open a `fix` ticket for the defect that caused the rollback, with the R10 diagnostics bundle attached
- [ ] State R4's "does not prove" verbatim in substance: it does not prove that data written by the defective version is correct — check audit/history for the window (area 10)
- [ ] Add the off-network consequence from R9 step 7 to R4: clients away from the LAN or VPN will not see the rollback until they return
- [ ] Add the downgrade scenario to `eng/packaging/Test-Package.ps1` (`DSK-08-10`, board `TEST-010`), extending that script rather than creating a second one
- [ ] Verification run: validator with `-Rollback` (exit `0`) and without it (non-zero, named failure); stack `Get-AppxPackage` moves from `<ver>` to `<prev-ver>`; pilot client logs in and completes a routine workflow; previous `.msix` hash matches its original manifest — this box produces `proof`
- [ ] Mark R4 **proven** in `runbooks.md` with its date, and record the dated `## Simplification pass` in the `plan` document (not `n/a — docs-only`; this branch extends a test script)

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
