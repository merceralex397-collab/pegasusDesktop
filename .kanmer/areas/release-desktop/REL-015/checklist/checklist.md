# Checklist — REL-015

Derived from `plan`, one box per step, in plan order. Boxes marked **(operator)** are
performed by the operator. Tick with `set_ticket_doc(doc: "checklist")`; append progress
notes below rather than rewriting.

- [ ] Read the area plan § 5 row `DSK-09-17` and § 3 "Publication", `runbooks.md` § R1 steps 3–6 and § R2, and `.github/workflows/ci.yml` in full; run `get_doc_gates REL-015` and `take_ticket REL-015`
- [ ] Create `.github/workflows/desktop-release.yml` with `on: push: tags: ['desktop/v*']` and `permissions: contents: read`, and record in the `plan` document why the lane is a new workflow rather than a tag trigger on `ci.yml`
- [ ] Add the provenance guard as the first job step: resolve the tag's commit and fail unless `git merge-base --is-ancestor <tag sha> origin/main` succeeds, checking out with `fetch-depth: 0`
- [ ] Set `runs-on: [self-hosted, windows, pegasus-signing]` using labels taken from `DSK-08-19` (board `TEST-019`), and add `timeout-minutes` so a mislabelled job fails rather than queueing forever
- [ ] If the self-hosted runner does not exist yet, land the job `if: false` with an explicit comment, and record that in the `plan` document — never route signing through a hosted runner
- [ ] Put the signing/publishing job behind a repository `environment:` requiring a reviewer
- [ ] Require the recorded literal `FEED PUBLISH GRANTED pilot <ver>` (in `DSK-09-11`'s confirmed wording) in the ticket before the environment is approved, and record that both halves are required
- [ ] If the `environment:` feature is unavailable on this repository, keep the job `if: false` and record that fact rather than publishing on the phrase alone
- [ ] Add job steps `actions/checkout@v7` at the tag and `uses: ./.github/actions/dotnet-build`, with no inline SDK pin
- [ ] Derive `<version>` from the tag name (`desktop/v1.2.345` → `1.2.345.0`) and fail immediately if it does not match `^1\.\d+\.\d+\.0$`, before the build
- [ ] Add the signed build step: `pwsh ./scripts/Build-DesktopRelease.ps1 -Channel pilot -Version <version> -SourceRevision <tag sha> -Sign -CertificatePath <host path> -TimestampUrl <timestamp service>`
- [ ] Add `signtool verify /pa /v` and confirm it aborts the job unless the output reports **both** a valid chain and a timestamp line
- [ ] Add `pwsh ./eng/packaging/Test-AppInstaller.ps1` and confirm a non-zero exit aborts the job before anything reaches the feed
- [ ] Add the publish step `pwsh ./eng/packaging/Publish-DesktopRelease.ps1 -Channel pilot`, package first and `.appinstaller` last, previous package retained
- [ ] Add an explicit guard that refuses `-Channel prod` in this workflow, and exercise it once in the dry run so the refusal is evidenced
- [ ] Add `actions/upload-artifact@v6` for the signed package, manifest, SBOM and hashes, matching the version at `ci.yml:179`
- [ ] Write version, source commit, package SHA-256, signer thumbprint and compatibility range into the job summary via `$env:GITHUB_STEP_SUMMARY`
- [ ] Dry run the whole lane with the development certificate from `DSK-09-06` (board `REL-006`) against the Test/UAT stack share, and record the run
- [ ] **(operator)** First real run: push `desktop/v<ver>` on `main`, record the approval phrase, approve the environment, and hand back the run URL and the feed listing
- [ ] Update `runbooks.md` § R1 with a note on which steps this lane now performs automatically and which remain manual
- [ ] Update `runbooks.md` § R2 to state plainly that production publication is **never** automated and stays the terminal step with `FEED PUBLISH GRANTED prod <ver>`
- [ ] Add the one sentence to `docs/engineering.md` § Branches and delivery saying the `desktop/v<M.m.b>` tag now triggers `desktop-release.yml`, coordinated with `DSK-00-09` (board `FND-009`)
- [ ] Verification run: dry run green with the dev certificate and files in the correct order on the stack share; real run requests approval and publishes with the previous package retained; a tag not contained in `main` fails at the provenance check before building; `grep -n "secrets\." .github/workflows/desktop-release.yml` shows no certificate or password secret; a push to `main` without a tag does not run the new workflow and `ci.yml` behaves as before — this box produces `proof`
- [ ] Record in the proof whether the self-hosted runner existed, whether the environment approval prompt appeared, and the lane's measured duration for `DSK-08-19` (board `TEST-019`)
- [ ] Record the dated `## Simplification pass` in the `plan` document over this branch's own diff (not `n/a — docs-only`; this branch adds a workflow)

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
