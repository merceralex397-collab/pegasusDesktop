# Checklist — REL-005

Derived from `plan`, one box per step, in plan order. Tick with
`set_ticket_doc(doc: "checklist")` as you complete them; append progress notes below
rather than rewriting.

- [ ] Read the area plan § 5 row `DSK-09-05` and § 2 and § 7, `.github/workflows/ci.yml` in full (234 lines) and `.github/actions/dotnet-build/action.yml`; run `get_doc_gates REL-005` and `take_ticket REL-005`
- [ ] Load `winui-packaging` and read its § CI/CD with GitHub Actions sample; record in the `plan` document whether `authoring-github-workflows` is vendored yet (`.agents/skills/` today holds only `pegasus-release` and `project/`)
- [ ] Run `grep -n "desktop" scripts/Get-CiChangeFlags.ps1` and `grep -n "outputs.desktop\|desktop:" .github/workflows/ci.yml`, and record in the `plan` document which change-flag branch applies
- [ ] **Preferred branch only**: gate the job on `needs.changes.outputs.desktop == 'true'` and change no classifier file
- [ ] **Fallback branch only**: extend `$buildPattern` at `scripts/Get-CiChangeFlags.ps1:11` with `|^eng/packaging/`, add one positive and one negative case to `scripts/Test-CiChangeFlags.ps1`, and run `pwsh ./scripts/Test-CiChangeFlags.ps1` (exit `0`)
- [ ] Run `grep -n '^  desktop-package:' .github/workflows/ci.yml` and record in the `plan` document whether `DSK-08-13` (board `TEST-013`) had already added the job
- [ ] Add (or extend in place) the single `desktop-package` job after `unit`, with `needs: changes`, the selected `if:` clause, `runs-on: windows-latest` and `timeout-minutes: 30`
- [ ] Write the job's leading comment covering all four points: why the lane exists, that it never signs for production, that it is the only packaging lane in the workflow, and which change flag gates it
- [ ] Add `desktop-build` to the job's `needs:` if `DSK-02-15` (board `FND-040`) has landed, and take the build from it rather than re-adding a second build-and-test lane
- [ ] Add job steps 1–2: `actions/checkout@v7` then `uses: ./.github/actions/dotnet-build`, with no inline SDK pin
- [ ] Add job step 3: the two `dotnet test` invocations chained with `&&` on one `run: >` line — or drop this step and record why, if `desktop-build` already proved them for the same commit
- [ ] Add job step 4a: `uses: microsoft/setup-WinAppCli@v0.1` then `winapp cert generate --manifest ./src/Pegasus.Desktop --if-exists skip --quiet`
- [ ] Add job step 4b: `winapp package ./src/Pegasus.Desktop/bin/x64/Release/<tfm>/ --cert ./devcert.pfx --self-contained --quiet`, resolving `<tfm>` with a glob that fails with a named message unless exactly one directory matches
- [ ] Confirm `winapp cert install` appears nowhere in the workflow (it writes to the machine Trusted Root store)
- [ ] Add job step 5: `pwsh ./eng/packaging/Test-TestAppInstaller.ps1` and `pwsh ./eng/packaging/Test-Package.ps1`, writing any skipped-scenario list into `$env:GITHUB_STEP_SUMMARY`
- [ ] Add job step 6: `actions/upload-artifact@v6` with `name: desktop-msix-unsigned`, `path: '**/*.msix'`, `if-no-files-found: error`, and a `path` glob broad enough for `DSK-09-16` to add the SBOM later
- [ ] Comment the artifact step to say the MSIX is dev-signed, not production-signed, and must never be published to a feed
- [ ] Confirm `git diff .github/workflows/ci.yml` shows only additions plus the `changes`-output reuse, and that the trigger block at `:3-6` is unchanged with no tag trigger added
- [ ] Open the PR and read the run: `desktop-package` green, nine pre-existing jobs green, artifact attached; record the run URL
- [ ] Record the observed `desktop-package` duration in minutes in the ticket proof, for `DSK-08-19` (board `TEST-019`)'s C-01 cost picture
- [ ] Verification run: `pwsh ./scripts/Test-CiChangeFlags.ps1` (exit `0`); `pwsh ./scripts/Get-CiChangeFlags.ps1 -ChangedPath eng/packaging/Test-AppInstaller.ps1` (the selected flag is `True`); `grep -c '^  desktop-package:' .github/workflows/ci.yml` (`1`); `grep -n "secrets\." .github/workflows/ci.yml` (no match introduced) — this box produces `proof`
- [ ] State in the proof that this ticket proves a green lane and an attached artifact only, and proves nothing about installation, update or signature-chain behaviour on a workstation
- [ ] Record the dated `## Simplification pass` in the `plan` document over this branch's own diff (not `n/a — docs-only`; this branch changes a workflow)

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
