# Checklist — REL-008

Derived from `plan`, one box per step, in plan order. Boxes marked **(operator)** require
administrative access to the in-house host or the directory service; an agent writes and
tests the scripts and records the evidence. Tick with
`set_ticket_doc(doc: "checklist")`; append progress notes below rather than rewriting.

- [ ] Read the area plan § 5 row `DSK-09-10`, `signing-and-hosting-decision-matrix.md` § D-003 in full, `runbooks.md` § R9 and `appinstaller-template.md` § Hosting requirements; run `get_doc_gates REL-008` and `take_ticket REL-008`
- [ ] `microsoft_docs_fetch` <https://learn.microsoft.com/windows/msix/app-installer/app-installer-file-overview>, quote the "https, http and smb" sentence into the ticket scratch with its fetch date
- [ ] **(operator)** Agree the permanent UNC root as a DFS namespace or a CNAME'd host — never a raw machine name, never a mapped drive letter — and hand it back
- [ ] Record the agreed UNC root in the `plan` document as permanent; if neither DFS nor a CNAME could be created, record that in `runbooks.md` § R9 as a recognised debt with the reinstall cost stated
- [ ] **(operator)** Create `\\<host>\<share>\prod\` and `\\<host>\<share>\pilot\` and hand back a `Get-ChildItem` listing
- [ ] **(operator)** Set the ACLs — staff group read + execute, publisher account modify, no other writer, inherited broad grants removed — and hand back `Get-Acl \\<host>\<share>\prod | Format-List`
- [ ] Record the staff group name and publisher account name in `runbooks.md` § R9 so the next reader grants the same objects rather than individuals
- [ ] Create `eng/packaging/Test-FeedShare.ps1` with the repository script header and parameters `-FeedRoot` and `-Channel`, plus a header comment requiring it to run as an ordinary staff user from a non-publisher workstation
- [ ] Implement the four `Test-FeedShare.ps1` checks with named failures: `Test-Path` on the channel path; `Select-Xml -XPath /*` for the expected `Version` and `Uri`; `Get-FileHash` against the manifest's `packageSha256`; `Get-Acl` proving the staff group has no write permission — exiting non-zero on any failure
- [ ] Create `eng/packaging/Publish-DesktopRelease.ps1` copying in R9 order: `robocopy … Pegasus_<ver>_x64.msix /Z /R:2 /W:5` first, then `desktop-release-manifest.json`, then `Pegasus.appinstaller` last
- [ ] Add the never-overwrite guard to `Publish-DesktopRelease.ps1`: refuse to replace an existing `.msix` with a named failure, while allowing `Pegasus.appinstaller` to be replaced in place
- [ ] Add a post-copy re-hash of the published `.msix` against the staging copy, so a truncated `robocopy` is caught before the manifest is published
- [ ] `microsoft_docs_fetch` <https://learn.microsoft.com/uwp/schemas/appinstallerschema/element-s4-updateuri> and establish whether a second UNC path is accepted as an `UpdateUris` fallback
- [ ] Write that finding into `runbooks.md` § R9 step 6 as a stated fact with URL and fetch date — including, if the answer is negative, that the feed has **no** fallback and share availability is the single point of failure for updates
- [ ] **(operator)** Confirm the share is covered by the host's backup, record the restore path, and hand back the backup job name and its last successful run
- [ ] Rehearse the client side on the Test/UAT stack: publish with `Publish-DesktopRelease.ps1`, install from `\\<stack host>\<share>\teststack\Pegasus.appinstaller`, publish a higher version, relaunch and confirm the update is detected
- [ ] Record the L-02 rehearsal result — `Get-AppxPackage CollisionEngineers.Pegasus` before and after — as the gate that must pass before the pilot
- [ ] Run Azure MCP `group_resource_list` for `rg-pegasus-prod` read-only before and after, and attach the identical inventories to the proof as evidence that nothing was created
- [ ] Correct the stale § 5 row wording for `DSK-09-10` in `docs/desktop/09-release-update-and-distribution/README.md` (it still names `allowBlobPublicAccess`, RBAC and MIME/Range verification from the withdrawn Azure options)
- [ ] Run `grep -rn "allowBlobPublicAccess\|desktop-releases container" docs/desktop/` and fix any other blob-era wording in area 09
- [ ] Mirror the "no ⚠ Azure write required" statement in `docs/desktop/11-azure-disposition/README.md`
- [ ] Confirm the `.appinstaller` `Uri` on the share is **byte-identical** to the path clients install from, by string comparison rather than by eye
- [ ] Verification run: `pwsh ./eng/packaging/Test-FeedShare.ps1 -FeedRoot \\<host>\<share> -Channel pilot` as a staff user (exit `0`); `Get-Acl \\<host>\<share>\prod | Format-List` (publisher `Modify`, staff `ReadAndExecute`, no `Everyone`); Test/UAT install-then-update rehearsal; identical before/after Azure inventories — this box produces `proof`
- [ ] Record the dated `## Simplification pass` in the `plan` document over this branch's own diff (not `n/a — docs-only`; this branch adds scripts)

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
