# Checklist — REL-010

Derived from `plan`, one box per step, in plan order. Boxes marked **(operator)** are
performed by the operator on a pilot workstation or against the production gateway. Tick
with `set_ticket_doc(doc: "checklist")`; append progress notes below rather than rewriting.

- [ ] Read `runbooks.md` § R3 and § R6 in full, the area plan § 5 row `DSK-09-12` and § 3 "Two-layer enforcement"; run `get_doc_gates REL-010` and `take_ticket REL-010`
- [ ] Record R3's preconditions with evidence: R2 done (or R1 done for the pilot-only rehearsal); all pilot users observed on `<ver>` or the grace period elapsed; the gateway supports `<ver>`
- [ ] Confirm no pilot user is known to be off-network before any minimum-version raise (R9 step 7)
- [ ] Read what `DSK-04-06` (board `GWY-023`) shipped and write the **actual** admin screen path or endpoint into `runbooks.md` § R3 step 1, replacing "the gateway administration surface"
- [ ] Stack rehearsal, **feed reachable**: install client N-1 from the local feed, raise the minimum to `<ver>`, relaunch, and record that App Installer prompts and updates
- [ ] Stack rehearsal, **feed unreachable**: repeat with the feed made unreachable by a recorded manoeuvre, and confirm the gateway's update-required screen appears with no work possible
- [ ] In the unreachable-feed run, assert that `GET /api/v1/client-compatibility` still answers, so an unreachable feed is not confused with an unreachable gateway
- [ ] Add the enforcement scenario to `eng/packaging/Test-Package.ps1` (`DSK-08-10`, board `TEST-010`) — extending that script, not creating a second one
- [ ] Make the scenario assert the update-required screen by `AutomationId` `Update.Required.Now` using `winui-ui-testing`
- [ ] Make the scenario assert that every `/api/v1` call from the old client returns `urn:pegasus:problem:client-unsupported` including its `minimumVersion` field
- [ ] Make the scenario restore the previous minimum version at the end so it is repeatable
- [ ] `microsoft_docs_search` for `Package CheckUpdateAvailabilityAsync Required Available` and record in R3 what `Required` means, plus the rule that the call is made on the package from `PackageManager.FindPackageForUser`, never `Package.Current`
- [ ] **(operator)** R3 step 2 — from a pilot machine still on the previous version, launch and hand back a screenshot plus the correlation id from the update-required screen
- [ ] Agree and record the named routine workflow for the positive case before running it (for example sign in, open a case, save an edit)
- [ ] **(operator)** R3 step 3 — confirm a current client on `<ver>` logs in and completes that named workflow, and hand back the transcript
- [ ] Record the minimum-version change (who, when, reason) in the desktop release row in `docs/operations.md`, in the same task
- [ ] Rehearse the rollback on the stack: lower the minimum to its previous value and confirm old clients are accepted again
- [ ] Record the **observed** recovery time from the rollback rehearsal as a measured number, not a target
- [ ] Extend R3's "does not prove" section to name the CSP/Group Policy override case, with `Get-AppxPackageAutoUpdateSettings` as the check and the rule that an override found is recorded rather than worked around
- [ ] Verification run: stack unreachable-feed scenario (screen, correlation id, `client-unsupported` on every `/api/v1` call, gateway still answering); stack reachable-feed scenario (App Installer updates, `Get-AppxPackage` moves to `<ver>`); pilot repeat with screenshot; current client completes the named workflow; `Get-AppxPackageAutoUpdateSettings` shows on-launch checks not overridden — this box produces `proof`
- [ ] Mark R3 **proven** in `runbooks.md` with its date, and record the dated `## Simplification pass` in the `plan` document (not `n/a — docs-only`; this branch extends a test script)

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
