# Plan — REL-004: DSK-09-04 · `scripts/Build-DesktopRelease.ps1` — the release-terminal desktop build

**Diff estimate: ~2 files, ~230 lines.** One new script
`scripts/Build-DesktopRelease.ps1` (~225 lines: ~25 for the parameter block and pairing
guard, ~25 for the HEAD/clean-tree/escape guards copied in structure from
`scripts/Build-ReleaseArtifacts.ps1:11-33`, ~30 restore and build, ~30 package, ~25
signature verification, ~20 vulnerability report, ~25 manifest and `.appinstaller`
wiring, ~25 hashes, log and stdout result, plus comments) and a one-line path correction
in `docs/desktop/09-release-update-and-distribution/README.md` § 4.
`docs/engineering.md:201-207` § Plan sizing requires the estimate first.

## Approach

**Copy the gateway release script's safety shape verbatim in structure, then do the
desktop-specific work between the guards.** `scripts/Build-ReleaseArtifacts.ps1` has
production history across twenty releases; its exact-HEAD check, clean-tree check,
output-root escape guard, per-step `if ($LASTEXITCODE -ne 0) { throw '…' }` habit and
single-stdout-result convention are the properties that make a release artefact
trustworthy, and re-deriving them is how a release script quietly loses one. The
alternative rejected was **extending `Build-ReleaseArtifacts.ps1` with a `-Desktop`
switch**: its `-Version` parameter is validated against `^\d+\.\d+\.\d+-alpha\.\d+$`,
which a four-part MSIX version can never satisfy, and its whole body is `linux-x64`
publish plus an EF migration bundle — two unrelated release trains in one script, each
able to break the other. A second alternative, **putting the script under
`eng/packaging/`**, is rejected in favour of the area plan § 5 row: `scripts/` is where
the repository's other release entry point lives and where an operator will look; step 11
corrects § 4 rather than leaving both paths in the plan set.

Signing is **opt-in and paired**, and the signature is verified before any other artefact
is written — so a failed verification leaves no set of files that could be mistaken for a
releasable release.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-004`). No existing PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by [[REL-001]] (plan handle `DSK-09-01`); see
> [[REL-001]]'s plan for the ownership reconciliation — ADR-0105 has three claimants
> (`REL-001`, `FND-005`, `FND-042`). This plan implements
> its Decision clause (c) — package version `1.<minor>.<build>.0` — through the `-Version`
> pattern in step 3, and its Consequences — signing with the self-managed certificate,
> always timestamped — through steps 6 and 7. This plan is written to the decisions as
> recorded in `docs/desktop/09-release-update-and-distribution/README.md` § 3; if ADR-0105
> lands differently, this plan is revised before implementation.

Existing ADRs this plan **meets** rather than introduces:

- **ADR-0007** (`docs/adr/0007-direct-terminal-azure-deployment.md`) — releases run from
  an authorised Windows terminal. **Meets**: steps 1–3 make the script a terminal command
  with clean-HEAD and exact-revision preconditions; there is no unattended path, and the
  later tag lane ([[REL-015]], plan handle `DSK-09-17`) is still gated on a human approval.

Binding operator decisions, written to as decisions and never as options:

- **D-002** (2026-08-23) — signing uses the **in-house self-managed certificate** on the
  signing host. The `.pfx` never leaves it and is never a GitHub secret. This script takes
  a `-CertificatePath` and never stores key material.
- **D-003** (2026-08-23) — the feed is an **in-house UNC file share**. This script stages
  output for copy; it publishes nothing. There is no blob upload, no HTTP host, no MIME
  configuration.
- **C-01** — the repositories become private; no artefact may depend on anonymous GitHub
  download, and GitHub Releases and GitHub Pages are permanently ruled out.

Contracts this plan **consumes**: the thirteen manifest fields and generator parameters
fixed by [[REL-002]] (plan handle `DSK-09-02`), and the `.appinstaller` generator plus
eight-check validator and its exit-code contract fixed by
[[REL-003]] (plan handle `DSK-09-03`).

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in
the plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) →
  `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`,
  `microsoft/win-dev-skills` v0.5.0 `f1028dd5`, verified present — the path moves to
  `.agents/skills/vendor/windows/winui-packaging/` once [[TOOL-002]] lands) →
  `pegasus-release` (`.agents/skills/pegasus-release/SKILL.md`, verified present, for the
  release-terminal conventions and traps table).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`,
  `microsoft_docs_fetch`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates REL-004` before every move; a move crosses at most one gated boundary.
  `get_doc_gates` reports `leave-backlog` (`governing-doc`, already satisfied by
  `docs_todo: true`), `leave-preparing` (`research`, `files`, `plan`, `checklist`,
  **`questions-resolved`**), `enter-review` (`post-implementation-report`,
  **`questions-resolved`**) and `enter-done` (`proof`, **`questions-resolved`**).
  Note that `questions-resolved` appears at three of those four boundaries and **never at
  `leave-backlog`** — verified 2026-08-24.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's thirteen implementation steps in the same order, with the same
ownership and the same file paths.

1. **Orient and take.** Read the area plan § 5 row `DSK-09-04`, `runbooks.md` § R1 steps
   1–4, and area plan § 3. `get_doc_gates REL-004`, then `take_ticket REL-004`. Read this
   ticket's `open-questions` document — every entry in it is parked, so none of them blocks
   a move, but each names something not to re-decide here.
2. **Read the precedent end to end.** `scripts/Build-ReleaseArtifacts.ps1`, all 130 lines.
   Reuse its safety shape verbatim in structure: `Set-StrictMode -Version Latest`,
   `$ErrorActionPreference = 'Stop'`, `$repositoryRoot = Split-Path -Parent $PSScriptRoot`,
   `Push-Location $repositoryRoot` / `finally { Pop-Location }`, the exact-HEAD check
   (`:16-19`), the clean-tree check (`:20-23`) and the output-root escape guard
   (`:25-33`).
3. **Create `scripts/Build-DesktopRelease.ps1`** with parameters `-Channel`
   (`ValidateSet 'pilot','prod'`), `-Version` (`ValidatePattern '^1\.\d+\.\d+\.0$'`),
   `-SourceRevision` (`ValidatePattern '^[0-9a-f]{40}$'`), `-Sign` (switch),
   `-CertificatePath`, `-TimestampUrl`, `-FeedRoot`, and — added now so [[REL-014]]
   (plan handle `DSK-09-16`) can extend additively — `-SbomPath`. **Fail immediately** if
   `-Sign` is passed without both `-CertificatePath` and `-TimestampUrl`, with a message that
   names all three parameters; this check runs before any build work, because a signed
   package without a timestamp is a latent estate-wide failure. Also fail fast when
   `-Sign` is passed and `signtool` is not resolvable on `PATH`
   (`Get-Command signtool -ErrorAction SilentlyContinue`), rather than skipping step 7's
   verification.
4. **Set the release root** to `artifacts/desktop-releases/<Version>` under the repository
   root, apply the same `[IO.Path]::GetFullPath` + `StartsWith` escape guard as
   `Build-ReleaseArtifacts.ps1:25-29`, then delete and recreate it. `artifacts/` is
   git-ignored (`.gitignore:20-21`), so nothing written here can be committed by accident.
5. **Restore and build.** `dotnet restore ./src/Pegasus.Desktop/Pegasus.Desktop.csproj
   --locked-mode`, then `dotnet build ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -c
   Release -p:Platform=x64 -p:DesktopPackageVersion=<Version>
   -p:ContinuousIntegrationBuild=true --no-restore`. Throw on any non-zero
   `$LASTEXITCODE`, one `throw` per step with its own sentence, following the habit at
   `Build-ReleaseArtifacts.ps1:46-71`. Note that `Directory.Build.props:8` sets
   `TreatWarningsAsErrors`, so a warning is a build failure here — that is intended.
6. **Package with the WinApp CLI, self-contained** per proposal § 7.1 so the package
   carries .NET and the Windows App SDK and the `.appinstaller` needs no `Dependencies`
   element (validator check 7 enforces it). Unsigned by default:
   `winapp package <build-output-dir> --self-contained --quiet`. With `-Sign`, use the
   single-step form `winui-packaging` § Key Rules prefers:
   `winapp package <build-output-dir> --cert <CertificatePath> --self-contained
   --timestamp <TimestampUrl> --quiet`. Name the output `Pegasus_<Version>_x64.msix`; if
   `winapp package` has no output-name flag (run `winapp package --help` and record the
   answer), rename the produced file rather than accepting whatever name it chose — the
   `.appinstaller`'s `MainPackage/@Uri` names this file and validator check 5 hashes it.
7. **Verify the signature before anything else is written.**
   `signtool verify /pa /v <releaseRoot>/Pegasus_<Version>_x64.msix`, and throw unless the
   output reports a valid chain **and** a timestamp — check for both, not just exit `0`.
   Extract the signer subject and thumbprint from that output and carry them into the
   manifest at step 9. Ordering matters: a failed verification must leave no manifest, no
   `.appinstaller` and no hash list.
8. **Write the vulnerability report, and record the SBOM question rather than answering it.**
   `dotnet list ./src/Pegasus.Desktop/Pegasus.Desktop.csproj package --vulnerable
   --include-transitive > <releaseRoot>/vulnerability-report.txt`, and throw if the text
   contains `Critical` or `High`. Inspect the **text**, never the exit code: `dotnet list
   package --vulnerable` returns `0` even when it reports findings, and a gate that trusts
   the exit code is a no-op. `--include-transitive` is essential because the package is
   self-contained and ships its own runtime.

   Leave the SBOM **generator choice** to [[REL-014]], which the board makes the single
   owner of the generator, the gate contract and the suppression register: add the
   `-SbomPath` pass-through (step 3) and **record the open question in `open-questions/`**,
   as the ticket body directs in as many words. That document now exists, with the SBOM
   question recorded **below `## Parked (explicitly deferred)`** — recorded, owned, and not
   blocking, because the body tells this ticket to add the hook and proceed. `REL-014`'s own
   step 1 says it will read "the `-SbomPath` hook and the open question `DSK-09-04` left", so
   leaving nothing there would have broken a sibling's first step.

   The earlier draft of this step said not to create the document at all, on the ground that
   "an unticked item would block every stage move". That reason is false and is withdrawn: an
   unticked box blocks `leave-preparing`, `enter-review` and `enter-done`, and never
   `leave-backlog`. The sound half of the reasoning — that a decision a named sibling ticket
   owns is a scope boundary rather than an open question — is why the entry is parked instead
   of unticked. Verified 2026-08-24 with `get_doc_gates REL-004`: with a parked-only
   `open-questions`, `questions-resolved` remains satisfied at all three boundaries.
9. **Emit the manifest and the `.appinstaller`.** Call
   `eng/packaging/New-DesktopReleaseManifest.ps1` ([[REL-002]]) with the
   version, channel, source revision, package path, signer subject and thumbprint from
   step 7, and the gateway compatibility range; then
   `eng/packaging/New-AppInstaller.ps1` and `eng/packaging/Test-AppInstaller.ps1`
   ([[REL-003]]) for the requested channel. **Throw on a non-zero
   validator exit** — the validator is a gate, not a report.
10. **Write the evidence files and the single result.** A `build-log.txt`, and a hash list
    from `Get-FileHash -Algorithm SHA256` over the `.msix`, the `.appinstaller` and the
    manifest, into `$releaseRoot`. Then `Write-Output` the manifest path as the script's
    **only** stdout result, mirroring `Build-ReleaseArtifacts.ps1:126` — so a caller can
    capture it directly.
11. **Resolve the script's home and correct the plan.** The area plan § 4 says
    `eng/packaging/`, the § 5 row says `scripts/`. Follow the row
    (`scripts/Build-DesktopRelease.ps1`, beside `Build-ReleaseArtifacts.ps1`) and correct
    `docs/desktop/09-release-update-and-distribution/README.md` § 4 in the same task. Run
    `grep -rn "eng/packaging/Build-DesktopRelease" docs/` before the PR and fix every hit,
    not only § 4.
12. **Measure determinism; do not assert it.** Run the unsigned build twice from the same
    clean HEAD and compare the **content** hash list. `Directory.Build.props:6` sets
    `<Deterministic>true</Deterministic>` for the managed compile, but an MSIX is a
    ZIP-family container and a signature plus an RFC-3161 timestamp is non-deterministic by
    construction. Record the **observed** result — including any instability — in the
    ticket proof. Do not write a reproducibility claim that was not measured.
13. **Simplification pass.** Record it under a dated `## Simplification pass` heading in
    this document (`AGENTS.md` § Repository task workflow step 4). This branch adds a
    script, so `n/a — docs-only` does not apply.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture.** The obligation is a
green build and a produced, validated artefact set on the release terminal. Install and
update behaviour is proven by [[TEST-010]] (plan handle `DSK-08-10`) and by runbook R1 in
[[REL-009]] (plan handle `DSK-09-11`), and this ticket must not claim it. `proof` is the
captured console output of the four cases below as proof type `command-log`, plus the
two-run hash comparison from step 12.

| Command | Expected evidence |
| --- | --- |
| `pwsh ./scripts/Build-DesktopRelease.ps1 -Channel pilot -Version 1.0.<run>.0 -SourceRevision $(git rev-parse HEAD)` on a clean tree | exit `0`; the manifest path printed as the only stdout line; all five artefacts present under `artifacts/desktop-releases/<ver>/` — `Pegasus_<ver>_x64.msix`, `Pegasus.appinstaller`, `desktop-release-manifest.json`, `vulnerability-report.txt`, the hash list |
| the same command after `New-Item ./dirty.txt` | non-zero exit with the clean-tree message (the `Build-ReleaseArtifacts.ps1:22` equivalent, `'Release artifacts require a clean exact source revision.'`) |
| `pwsh ./scripts/Build-DesktopRelease.ps1 -Channel pilot -Version 1.0.1.0 -SourceRevision <sha> -Sign` with no `-CertificatePath` | fails immediately, before any build work, with the message naming `-Sign`, `-CertificatePath` and `-TimestampUrl` |
| `Get-Content ./artifacts/desktop-releases/<ver>/vulnerability-report.txt` | no `High` or `Critical` row, or a triage recorded in the ticket with the advisory id and the reason |

Behaviours to observe rather than infer, and to state in the proof:
the `.appinstaller` produced has **no** `Dependencies` element (`Select-Xml` it);
`signtool verify /pa /v` reported both a chain **and** a timestamp for the signed case, or
the signed case was not run and the proof says so; the two unsigned runs' content hashes
were compared and the observed result — stable or not — is recorded; and
`git diff --name-only` shows only `scripts/Build-DesktopRelease.ps1` and the
`docs/desktop/09-.../README.md` § 4 correction.

## Risks / open questions

- **Risk — a signed package without a timestamp reaches the feed.** Every installed
  package's signature path stops validating for new installs the day the certificate
  expires. Mitigation: step 3's parameter pairing fails before any work, and step 7
  requires the timestamp line in `signtool verify` output, not merely exit `0`.
- **Risk — `signtool` is not on the release terminal's `PATH`.** It ships with the Windows
  SDK, not the .NET SDK, and nothing here installs it. Mitigation: step 3's fail-fast
  `Get-Command signtool` check when `-Sign` is passed; never skip the verification.
  Parked as A-09-6 in `open-questions`.
- **Risk — `winapp package` names the output file itself.** Mitigation: step 6's rename
  fallback, applied unconditionally rather than conditionally on a flag that may not
  exist. Parked as A-09-5 in `open-questions`.
- **Risk — the reproducibility claim outruns the measurement.** Mitigation: step 12
  records the observed result including instability; the acceptance criterion is the
  measurement, not a stable hash. Parked as A-09-7 in `open-questions`.
- **Risk — the script grows past a reviewable size.** The body's sizing concern: build,
  package, sign, manifest, validate, vulnerability report is a lot for one ticket. If the
  diff outgrows review, split the SBOM/vulnerability half into [[REL-014]] rather than
  inventing a new handle.
- **Risk — the desktop project does not exist yet.** `src/Pegasus.Desktop/` is created by
  [[FND-030]] (plan handle `DSK-02-05`). Mitigation: the parameter block, guards, escape
  guard and wiring can be written and reviewed first; only steps 5–10's execution needs the
  project.
- **Open questions — recorded, and none of them blocking.** This ticket now has an
  `open-questions` document, because its body's step 8 instructs one in as many words and
  [[REL-014]]'s own step 1 expects to find it. Every entry sits below
  `## Parked (explicitly deferred)`: the **SBOM generator choice** (owned by `REL-014`),
  `winapp package`'s output flag (A-09-5), `signtool` availability (A-09-6) and MSIX
  determinism (A-09-7). The last three are each settled by running one command on the
  release terminal and each has a recorded fallback that does not require an answer first.
  Verified 2026-08-24: with the document parked-only, `questions-resolved` is satisfied at
  `leave-preparing`, `enter-review` and `enter-done` — so recording the questions costs this
  ticket nothing, and the earlier reason for not recording them ("would block every stage
  move") was false in both its halves.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch adds a
script, so `n/a — docs-only` does not apply._
