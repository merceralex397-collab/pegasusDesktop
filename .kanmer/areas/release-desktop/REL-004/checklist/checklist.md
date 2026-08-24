# Checklist — REL-004

Derived from `plan`, one box per step, in plan order. Tick with
`set_ticket_doc(doc: "checklist")` as you complete them; append progress notes below
rather than rewriting.

- [ ] Read the area plan § 5 row `DSK-09-04`, `runbooks.md` § R1 steps 1–4 and area plan § 3; run `get_doc_gates REL-004` and `take_ticket REL-004`
- [ ] Read this ticket's `open-questions` document and confirm every entry is below `## Parked (explicitly deferred)` — none of them blocks a move, and none of them is to be re-decided here
- [ ] Read `scripts/Build-ReleaseArtifacts.ps1` end to end (130 lines) and note the five patterns to reuse: exact-HEAD guard `:16-19`, clean-tree guard `:20-23`, output-root escape guard `:25-33`, per-step `throw` on `$LASTEXITCODE`, single stdout result `:126`
- [ ] Create `scripts/Build-DesktopRelease.ps1` with the repository header (`Set-StrictMode -Version Latest`, `$ErrorActionPreference = 'Stop'`, `Push-Location $repositoryRoot` / `finally { Pop-Location }`)
- [ ] Add the parameter block: `-Channel` (`ValidateSet 'pilot','prod'`), `-Version` (`ValidatePattern '^1\.\d+\.\d+\.0$'`), `-SourceRevision` (`ValidatePattern '^[0-9a-f]{40}$'`), `-Sign`, `-CertificatePath`, `-TimestampUrl`, `-FeedRoot`, `-SbomPath`
- [ ] Fail immediately, before any build work, when `-Sign` is passed without both `-CertificatePath` and `-TimestampUrl`, with a message naming all three parameters
- [ ] Fail fast when `-Sign` is passed and `Get-Command signtool` does not resolve, rather than skipping the signature verification
- [ ] Add the exact-HEAD guard (`git rev-parse HEAD` must equal `-SourceRevision`) and the clean-tree guard (`git status --porcelain=v1 --untracked-files=all` must be empty), each throwing a named sentence
- [ ] Set `$releaseRoot = artifacts/desktop-releases/<Version>`, apply the `[IO.Path]::GetFullPath` + `StartsWith` escape guard, then delete and recreate it
- [ ] Add locked restore and x64 Release build of `src/Pegasus.Desktop/Pegasus.Desktop.csproj` with `-p:DesktopPackageVersion=<Version> -p:ContinuousIntegrationBuild=true --no-restore`, throwing on non-zero `$LASTEXITCODE`
- [ ] Add the unsigned packaging call `winapp package <build-output-dir> --self-contained --quiet`
- [ ] Add the signed packaging call `winapp package <build-output-dir> --cert <CertificatePath> --self-contained --timestamp <TimestampUrl> --quiet`, used only when `-Sign` is passed
- [ ] Run `winapp package --help`, record whether an output-name flag exists, and ensure the produced file is named `Pegasus_<Version>_x64.msix` — renaming it if there is no flag
- [ ] Add `signtool verify /pa /v <releaseRoot>/Pegasus_<Version>_x64.msix` after signing, throwing unless the output reports **both** a valid chain and a timestamp, and extract the signer subject and thumbprint from that output
- [ ] Confirm by ordering that a failed signature verification leaves no manifest, no `.appinstaller` and no hash list on disk
- [ ] Add the vulnerability report `dotnet list ./src/Pegasus.Desktop/Pegasus.Desktop.csproj package --vulnerable --include-transitive > <releaseRoot>/vulnerability-report.txt`, throwing when the **text** contains `Critical` or `High` (never trusting the exit code, which is `0` even with findings)
- [ ] Confirm the SBOM generator question is recorded in `open-questions/` below `## Parked (explicitly deferred)`, naming [[REL-014]] (plan handle `DSK-09-16`) as its owner and `-SbomPath` as the pass-through — the ticket body's step 8 instructs this document in as many words, and `REL-014` step 1 reads it. Record the same in the `plan`. **Do not delete it and do not invent a generator here.**
- [ ] Call `eng/packaging/New-DesktopReleaseManifest.ps1` with version, channel, source revision, package path, signer subject, signer thumbprint and the gateway compatibility range
- [ ] Call `eng/packaging/New-AppInstaller.ps1` then `eng/packaging/Test-AppInstaller.ps1` for the requested channel, throwing on a non-zero validator exit
- [ ] Write `build-log.txt` and a `Get-FileHash -Algorithm SHA256` hash list over the `.msix`, the `.appinstaller` and the manifest into `$releaseRoot`
- [ ] Make the manifest path the script's only `Write-Output` result, mirroring `scripts/Build-ReleaseArtifacts.ps1:126`
- [ ] Correct `docs/desktop/09-release-update-and-distribution/README.md` § 4 from `eng/packaging/Build-DesktopRelease.ps1` to `scripts/Build-DesktopRelease.ps1`
- [ ] Run `grep -rn "eng/packaging/Build-DesktopRelease" docs/` and fix every remaining hit, not only § 4
- [ ] Run the unsigned build twice from the same clean HEAD, compare the content hash lists, and record the **observed** result — including instability — in the proof without asserting reproducibility
- [ ] Verification run: clean-tree build exits `0` with five artefacts and the manifest path on stdout; the dirty-tree run fails with the clean-tree message; `-Sign` without `-CertificatePath` fails immediately with the pairing message; `vulnerability-report.txt` shows no `High`/`Critical` row or a recorded triage — this box produces `proof`
- [ ] Confirm in the proof that the generated `.appinstaller` has no `Dependencies` element (`Select-Xml`), and that `git diff --name-only` shows only the new script and the § 4 correction
- [ ] Record the dated `## Simplification pass` in the `plan` document over this branch's own diff (not `n/a — docs-only`; this branch adds a script)

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
