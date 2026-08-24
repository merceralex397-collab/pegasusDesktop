# Checklist — REL-009

Derived from `plan`, one box per step, in plan order. Boxes marked **(operator)** are
performed by the operator; an agent prepares the command and the evidence template and
records the result. Tick with `set_ticket_doc(doc: "checklist")`; append progress notes
below rather than rewriting.

- [ ] Read `runbooks.md` § Conventions and § R1 in full, plus the area plan § 5 row `DSK-09-11` and § 3; run `get_doc_gates REL-009` and `take_ticket REL-009`
- [ ] **(operator)** Confirm the exact approval wording `FEED PUBLISH GRANTED pilot <ver>` / `FEED PUBLISH GRANTED prod <ver>`, and record it verbatim in `runbooks.md` § Conventions without extending `MERGE AUTH GRANTED`
- [ ] Record precondition 1 as met or not: the gateway release this package needs is live and recorded, or R8 confirmed no gateway change — checking the manifest's `minimumGatewayRelease` against the newest row of the gateway table at `docs/operations.md:311`
- [ ] Record precondition 2 as met or not: `git tag --list 'desktop/v*'` shows the tag and `git merge-base --is-ancestor <tag sha> origin/main` succeeds
- [ ] Record precondition 3 as met or not: CI green for that commit including the desktop lanes and packaging tests, with the run URL
- [ ] Record precondition 4 as met or not: signing host, certificate and `<feed>` path configured (`DSK-09-08` board `REL-007`, `DSK-09-10` board `REL-008`)
- [ ] Record precondition 5 as met or not: the Test/UAT rehearsal of install → update → rollback passed for **this** package, with the evidence linked
- [ ] R1 step 1 — clean checkout of the tagged commit on the authorised release terminal; `git status` clean; record the 40-character SHA
- [ ] R1 step 2 — run `pwsh ./scripts/Build-DesktopRelease.ps1 -Channel pilot -Version <ver> -SourceRevision <sha>` and record the SHA-256 of the `.msix` and the manifest path from stdout
- [ ] **(operator)** R1 step 3 — sign on the signing host **with a timestamp** (`--timestamp` or `/tr`) and hand back the output
- [ ] Run `signtool verify /pa /v <pkg>.msix` and confirm the output shows **both** the chain and a timestamp line, reading the output rather than the exit code
- [ ] R1 step 4 — generate `pilot/Pegasus.appinstaller` with `Version` = previous + 1 revision and `MainPackage Version=<ver>`, then run `pwsh ./eng/packaging/Test-AppInstaller.ps1`; a non-zero exit stops the release
- [ ] **(operator)** R1 step 5 — obtain `FEED PUBLISH GRANTED pilot <ver>` in writing and record it verbatim in the ticket
- [ ] R1 step 6 — publish with `eng/packaging/Publish-DesktopRelease.ps1` to `<feed>/pilot/`, package first and `.appinstaller` last, and confirm the previous package is still present afterwards
- [ ] R1 step 7 — run `pwsh ./eng/packaging/Test-FeedShare.ps1 -FeedRoot <feed> -Channel pilot` from a **non-publisher staff account** and record the output
- [ ] Correct R1 step 7's wording in `runbooks.md` from the `curl -I` / ranged-`GET` HTTP check to the SMB check, in the same task
- [ ] **(operator)** R1 step 8 — on the pilot workstation, launch Pegasus, confirm the App Installer prompt appears, take the update, and hand back a screenshot of the version screen plus the `Get-AppxPackage CollisionEngineers.Pegasus` transcript
- [ ] **(operator)** Take one document download and one case export **more than an hour after** the production gateway revision started, and record the revision start time and both request times — upstream `PLAT-039`'s outstanding renewal check
- [ ] Record that a failure in the >1-hour check is a **gateway** defect to raise as a separate ticket, not a pilot-release defect
- [ ] **(operator)** Obtain the combined approval text that names the gateway releases whose `OPS-10` execution it accepts (releases 1–3 plus any later gateway release this pilot ran against) as well as this desktop pilot, signed **once** by one approver, and record it verbatim beside the publish phrase (D-004)
- [ ] Confirm no second, separate `OPS-10` acceptance was sought and that upstream `TICK-001` was not treated as live work
- [ ] R1 step 9 — add the desktop release row (version, date, commit, package hash, signer, channel `pilot`, compatibility range) to `docs/operations.md` in the same task, coordinating with `DSK-09-18` (board `REL-016`) if the `### Desktop releases` table does not exist yet
- [ ] Add the one-line **D-004** row to `docs/desktop/README.md` § Locked decisions and open decisions in the existing `ID | Decision | Status | Owner plan` shape, in the same task
- [ ] Update `docs/current-architecture.md`'s deployment boundary paragraph **only if** the boundary changed; otherwise leave it untouched and say so in the proof
- [ ] Confirm `git diff --name-only` shows **no** change to `docs/capabilities.md`
- [ ] Verification run: `signtool verify /pa /v` (chain and timestamp); `Test-AppInstaller.ps1` (exit `0`); `Get-AppxPackage` on the pilot workstation (`Version` equals `<ver>`); `Test-FeedShare.ps1` as a staff user (exit `0`); the >1-hour download and export both succeed with three timestamps recorded; `grep -n 'D-004' docs/desktop/README.md` (exactly one row); `pwsh ./scripts/Test-DocumentationLinks.ps1` (exit `0`) — this box produces `proof`
- [ ] State R1's limits verbatim in substance in the proof: it does not prove production-ring behaviour on every workstation, does not prove telemetry (App Insights 0.1 GB/day cap, PLAT-034), and proves nothing about the gateway's own release
- [ ] Mark R1 **proven** in `runbooks.md` with its date, and record the dated `## Simplification pass` in the `plan` document

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
