# Checklist — FEAT-039

One box per plan step, in plan order. Tick with `set_ticket_doc`; append progress notes below.

- [ ] Read the plan row `DSK-07-13` in `docs/desktop/07-integrations/README.md` § 5, that area's § 7 "Template duplication" trap, and the item group at `src/Pegasus.Infrastructure/Pegasus.Infrastructure.csproj:42-53`
- [ ] Call `get_doc_gates FEAT-039` and `take_ticket` on branch `task/dsk-07-13-shared-report-templates`
- [ ] Append to `research` the measured template inventory — six `.scriban` files plus `report.css`, of which `assessment_report.scriban`, `assessment_fee_note.scriban` and `report.css` are embedded today — with the `ls` that produced it
- [ ] Confirm [[FEAT-043]] has recorded the upstream TICK-216 desktop-exposure decision, and append to `research` which signature assets it authorises for embedding; if the record does not exist, stop and stay in Preparing
- [ ] Create the shared props file (`eng/build/ReportAssets.props` or the `directory-build-organization` recommendation, with the choice recorded) holding the five current items plus any signature the TICK-216 record adds
- [ ] Anchor every path in the props file to `$(MSBuildThisFileDirectory)` and keep the `logo_no_margin.png` item in its element form with the `<Link>Reports\Assets\brand\logo.png</Link>` child
- [ ] Parameterise the `LogicalName`s by an assembly-prefix property defaulting to `Pegasus.Infrastructure.Reports.Assets`, and cite `Directory.Build.props:9-18` as the precedent in a comment
- [ ] Import the props file from `Pegasus.Infrastructure.csproj` and delete the duplicated `ItemGroup` at `:42-53`
- [ ] List `Pegasus.Infrastructure`'s manifest resource names and confirm all five are character-for-character unchanged
- [ ] Import the same props file from `src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj` with the prefix property set to that assembly's namespace
- [ ] Confirm no asset file was copied into `src/` (`git status --porcelain -- src` shows only the two `.csproj` edits)
- [ ] Add the SHA-256 resource-hash test for every embedded report asset in `Pegasus.Infrastructure`, in `tests/Pegasus.Core.Tests` if it runs without a database, otherwise `tests/Pegasus.IntegrationTests`
- [ ] Add the cross-assembly equal-hash test in the desktop test project asserting each suffix's bytes in `Pegasus.Desktop.Infrastructure` equal the same suffix's bytes in `Pegasus.Infrastructure`
- [ ] Add the negative test asserting that none of upstream TICK-206's twelve legacy identifiers, nor one unknown identifier, resolves from `Pegasus.Desktop.Infrastructure`
- [ ] Add the no-CRLF assertion over the embedded `.scriban` and `.css` bytes, and state in the test that the PNGs are `binary` in `.gitattributes:20` and out of its scope
- [ ] Build both assemblies in Release and run both new test files
- [ ] Run the existing browser suite and confirm `AssessmentReportRendererTests` still renders all four outcomes
- [ ] Confirm no new CI job was added and the drift checks run inside the existing default and `browser` lanes plus the desktop lane from [[TEST-013]]
- [ ] Record in `docs/design/assets/report-renderer/README.md` (new) or `docs/design/README.md` that two assemblies embed the same source, which signature assets are embedded under the TICK-216 acceptance, and that a hash test enforces both — saying which location was chosen
- [ ] Add the embedded-template sharing step to `docs/current-architecture.md`
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document
- [ ] Verification run — the two Release builds, the default and browser integration filters, the desktop view-model project, `git status --porcelain -- docs/design` (expected empty), and the cited [[FEAT-043]] TICK-216 record — captured as `proof` at tier 1
- [ ] Open the PR into `dev`

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
