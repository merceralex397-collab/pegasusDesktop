# Plan — REL-002: DSK-09-02 · Desktop versioning scheme and desktop-release-manifest.json generator

**Diff estimate: ~4 files, ~240 lines.** New `src/Pegasus.Desktop/Directory.Build.props`
(~18 lines); an MSBuild target added to `src/Pegasus.Desktop/Pegasus.Desktop.csproj`
(~25 lines); new `eng/packaging/New-DesktopReleaseManifest.ps1` (~95 lines, sized against
`scripts/Build-ReleaseArtifacts.ps1:92-126`, which builds an equivalent ordered manifest
in ~35 lines plus parameter block); new `eng/packaging/Test-NewDesktopReleaseManifest.ps1`
(~100 lines, sized against `scripts/Test-CiChangeFlags.ps1`'s assert-helper-plus-cases
shape). `docs/engineering.md:201-207` § Plan sizing requires the estimate first.

## Approach

Keep the two version identities physically separate rather than logically separate. The
gateway's identity is `<Version>0.1.0-alpha.1</Version>` at `Directory.Build.props:9`,
consumed by `azure.yaml:3`, `package.json:3` and — critically —
`scripts/Build-ReleaseArtifacts.ps1:3`, whose `-Version` parameter is validated against
`^\d+\.\d+\.\d+-alpha\.\d+$`. A four-part MSIX version can never satisfy that pattern, so
the two must not share a property. The chosen approach is a **desktop-only
`Directory.Build.props` under `src/Pegasus.Desktop/`** that imports the repository-root
file with `$([MSBuild]::GetPathOfFileAbove(...))` and adds one new property,
`DesktopPackageVersion`, plus a build target that stamps it into the staged
`Package.appxmanifest`. The alternative rejected was **adding a second property to the
root `Directory.Build.props`**: it would put a desktop-only concern in the file every
gateway project imports, and the root file's own comment block (lines 10-18, the
`PlaywrightVersion` note) shows the repository reserves that file for properties with
cross-cutting reach. A second alternative — deriving the package version from
`<Version>` by transformation — was rejected because the two version lines move for
different reasons: the gateway version tracks the product release train, the package
version must increase on **every** build Windows is asked to install over.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-002`). No existing PRD/FRD/ADR is claimed.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by [[REL-001]] (plan handle `DSK-09-01`); see
> [[REL-001]]'s plan for the ownership reconciliation — ADR-0105 has three claimants
> (`REL-001`, `FND-005`, `FND-042`). Decision clause (c) of that ADR fixes the package
> version as `1.<minor>.<build>.0` with `build` = the CI run number and revision always
> `0`. This plan is written to that decision as recorded
> in `docs/desktop/09-release-update-and-distribution/README.md` § 3 ("Versioning" and
> "Desktop release manifest"); if ADR-0105 lands with a different scheme, this plan is
> revised before implementation.

Binding operator decisions this plan is written to, and must not re-argue:

- **D-002** (2026-08-23) — production signing is a **self-managed certificate**. The
  manifest's `signerSubject` and `signerThumbprint` fields therefore record that
  certificate, issued by [[REL-007]] (plan handle `DSK-09-08`). There is no Azure signing
  service and no Key Vault certificate to record.
- **D-003** (2026-08-23) — the feed is an **in-house UNC file share** over SMB. The
  manifest's `channel` field is therefore one of the share's two folders, `pilot` or
  `prod`, and never a URL host.

Downstream contracts this document is the source of truth for, because two sibling
tickets read it: [[REL-003]] (plan handle `DSK-09-03`) validates a generated
`.appinstaller` against `desktop-release-manifest.json`, and
[[REL-004]] (plan handle `DSK-09-04`) calls the generator. The thirteen field names and
their order in step 6 are the contract.

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in
the plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) →
  `directory-build-organization` (`dotnet/skills` `98f84851`, plugin `dotnet-msbuild`) →
  `binlog-failure-analysis` (`dotnet/skills` `98f84851`, plugin `dotnet-msbuild`) when an
  MSBuild target misfires.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`,
  `microsoft_docs_fetch`).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates REL-002`
  before every move; a move crosses at most one gated boundary. `get_doc_gates` reports
  exactly two gated boundaries: **`leave-preparing` needs `plan` (this document) **and**
  `questions-resolved`**, and **`enter-done` needs `proof` **and** `questions-resolved`**.
  `leave-backlog` is not a gated boundary for a `chore` at all.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's eleven implementation steps in the same order, with the same
ownership and the same file paths.

1. **Orient and take.** Read
   `docs/desktop/09-release-update-and-distribution/README.md` § 3 subsections
   "Versioning" and "Desktop release manifest", then `appinstaller-template.md`
   § Validator outline (checks 3, 4 and 5 are the fields the validator reads back from
   the manifest). `get_doc_gates REL-002`, then `take_ticket REL-002`. Read this ticket's
   `open-questions` document — its one unticked box must be answered before the ticket can
   leave Preparing, and it bears directly on step 8.
2. **Read, do not edit, the root build properties.** Load
   `directory-build-organization` and read `Directory.Build.props` (19 lines). Confirm
   by reading that `<Version>0.1.0-alpha.1</Version>` is line 9 and leave it alone: it is
   the gateway's identity and `scripts/Build-ReleaseArtifacts.ps1:3` validates it against
   `^\d+\.\d+\.\d+-alpha\.\d+$`.
3. **Add the desktop-only property file** at `src/Pegasus.Desktop/Directory.Build.props`
   (the project is created by [[FND-030]], plan handle `DSK-02-05`). It must import the
   parent explicitly, because MSBuild stops at the first `Directory.Build.props` it finds:
   `<Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />`.
   Then define
   `<DesktopPackageVersion Condition="'$(DesktopPackageVersion)' == ''">1.0.0.0</DesktopPackageVersion>`
   so a plain `dotnet build` works and `-p:DesktopPackageVersion=` overrides it.
   Note the inheritance the import preserves and that the desktop project therefore also
   gets `TreatWarningsAsErrors`, `Deterministic` and `AnalysisLevel` from the root file.
4. **Add the stamping target** `StampDesktopPackageVersion` in
   `src/Pegasus.Desktop/Pegasus.Desktop.csproj`, with
   `BeforeTargets="_CreateMsixRecipe;GenerateAppxManifest"`, rewriting `Identity/@Version`
   in the **staged** `Package.appxmanifest` to `$(DesktopPackageVersion)`. Validate the
   value inside the target against `^1\.\d+\.\d+\.0$` and `<Error Text="…">` with a named
   message when it does not match — the failure must name the property and the required
   shape, not just "invalid". Before writing the regex, confirm the four-part rule with
   `microsoft_docs_search` for `MSIX Package.appxmanifest Identity Version four part` and
   `microsoft_docs_fetch` on the page it returns; record the URL and the fetch date in
   the ticket's `research` scratch, since this ticket owes no `research` document.
5. **Create the generator** `eng/packaging/New-DesktopReleaseManifest.ps1`. `eng/` does
   not exist in the repository today (`ls eng` returns nothing) — this ticket creates it.
   Model the parameter block on `scripts/Build-ReleaseArtifacts.ps1:1-9`: `[CmdletBinding()]`,
   `[Parameter(Mandatory)]` with `[ValidatePattern]`/`[ValidateSet]` attributes, and a
   comment above any parameter whose value is not self-evident. Parameters:
   `-Version` (`ValidatePattern '^1\.\d+\.\d+\.0$'`), `-Channel`
   (`ValidateSet 'pilot','prod'`), `-SourceRevision` (`ValidatePattern '^[0-9a-f]{40}$'`),
   `-PackagePath`, `-AppInstallerVersion`, `-SignerSubject`, `-SignerThumbprint`,
   `-MinimumGatewayRelease`, `-MaximumTestedGatewayRelease`, `-WindowsAppSdkVersion`,
   `-BuildRun`, `-OutputPath`. Open with
   `Set-StrictMode -Version Latest` and `$ErrorActionPreference = 'Stop'`, as every
   script in `scripts/` does.
6. **Emit exactly thirteen fields, in this order**, as an `[ordered]` hashtable
   serialised with `ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath
   -Encoding utf8NoBOM` — the same call shape as
   `scripts/Build-ReleaseArtifacts.ps1:124`:
   `schemaVersion` (literal `1`), `version`, `sourceCommit`, `packageSha256` (from
   `(Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash`),
   `appInstallerVersion`, `channel`, `signerSubject`, `signerThumbprint`,
   `minimumGatewayRelease`, `maximumTestedGatewayRelease`, `windowsAppSdkVersion`,
   `buildRun`, `createdAtUtc` (`[DateTimeOffset]::UtcNow.ToString('O')`). This list is
   the contract `REL-003` and `REL-004` read; do not reorder or rename it later without
   changing both.
7. **Create the generator's test** `eng/packaging/Test-NewDesktopReleaseManifest.ps1`,
   following `scripts/Test-CiChangeFlags.ps1`'s shape: a local `Assert-…` function that
   `throw`s with a case name, then a flat list of cases. Cases: a valid invocation
   produces every one of the thirteen fields in order; `-Version 2.0.0.0` is rejected;
   `-Version 1.0.0.1` is rejected (revision must be `0`); `packageSha256` equals
   `Get-FileHash` of the input file; the emitted file round-trips through
   `Get-Content -Raw | ConvertFrom-Json` and its first byte is not `0xEF` (no BOM).
   Exit non-zero on any failure — `throw` under `$ErrorActionPreference = 'Stop'` does
   that.
8. **Record where `build` comes from outside CI.** The plan states `build` is the CI run
   number, re-based if the CI provider changes. The default this plan takes, and which the
   script header comment must state in one sentence: *the release terminal passes the run
   number of the CI run that built the tagged commit; where no CI run exists (a local
   rehearsal build) it passes a value higher than the last published `build` for that
   channel, taken from the desktop release row in `docs/operations.md`.*
   `-Version` is mandatory on both this generator and `Build-DesktopRelease.ps1`
   ([[REL-004]]), so there is no path that needs a value invented at runtime.

   **The body's condition on this step is met, so an `open-questions/` entry is owed and
   has been created.** Body step 8 says to "add an `open-questions/` entry if the release
   terminal needs a manual value" — and the second clause of the default above is exactly
   a manual value. The entry records the gap that clause leaves: verified 2026-08-24,
   `docs/operations.md` carries the **gateway** release table but **no desktop release
   table**, and that table is authored by [[REL-016]] (plan handle `DSK-09-18`), a phase-9
   row, while this ticket is phase-1. So between here and `REL-016` the default names a
   source that does not exist. Answer the box by naming the authoritative source that holds
   in the meantime — the highest `version` among the manifests already published to that
   channel's folder on the D-003 share is the obvious candidate — and write it into both
   this step and the generator's header comment.

   The earlier draft of this step declined to create the entry, reasoning that "an unticked
   item would block every stage move". That is false and is withdrawn: an unticked box
   blocks `leave-preparing`, `enter-review` and `enter-done`, never `leave-backlog`, and a
   `chore` carries only the first and last of the three.
9. **Prove the generator locally** with the body's exact invocation, pointing
   `-PackagePath` at any real file (`./global.json` works, it is 7 lines) so the hash is
   computable before a real `.msix` exists:
   `pwsh ./eng/packaging/New-DesktopReleaseManifest.ps1 -Version 1.0.1234.0 -Channel pilot
   -SourceRevision <40-hex> -PackagePath ./global.json -AppInstallerVersion 1.0.0.0
   -SignerSubject 'CN=Collision Engineers' -SignerThumbprint 0000
   -MinimumGatewayRelease 20 -MaximumTestedGatewayRelease 20 -WindowsAppSdkVersion 2.4.0
   -BuildRun 1234 -OutputPath ./artifacts/desktop-releases/1.0.1234.0/desktop-release-manifest.json`.
   `artifacts/` is git-ignored (`.gitignore:20-21`), so the output cannot be committed by
   accident.
10. **Run the test**: `pwsh ./eng/packaging/Test-NewDesktopReleaseManifest.ps1`, expected
    exit code `0`.
11. **Simplification pass.** Record it under a dated `## Simplification pass` heading in
    this document (`AGENTS.md` § Repository task workflow step 4). This branch changes
    build files and scripts, so `n/a — docs-only` does **not** apply; run the pass.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture.** The obligation is a
compiling build, a passing script test, and no drift in the gateway's own version
property; it proves nothing about installing or updating a package. `proof` is produced
from the commands below as proof type `command-log`, with the two build cases
captured as a matched pair (success and named failure).

| Command | Expected evidence |
| --- | --- |
| `pwsh ./eng/packaging/Test-NewDesktopReleaseManifest.ps1` | exit code `0`; every assertion reported as passed |
| `dotnet build ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -c Release -p:Platform=x64 -p:DesktopPackageVersion=1.0.1234.0` | build succeeds; the staged `Package.appxmanifest` shows `Version="1.0.1234.0"` — read the staged file, not the source one |
| `dotnet build ./src/Pegasus.Desktop/Pegasus.Desktop.csproj -c Release -p:Platform=x64 -p:DesktopPackageVersion=2.0.0.0` | build **fails** with the named version-shape message from step 4 |
| `dotnet restore ./Pegasus.slnx --locked-mode` | exit code `0` — no package graph change; the same command `.github/actions/dotnet-build/action.yml` runs in every CI lane |
| `get_doc_gates REL-002` | `questions-resolved` satisfied — the `build`-source box ticked with its answer recorded in step 8 and in the script header |

Behaviour to observe rather than infer: open the emitted `desktop-release-manifest.json`
and confirm the thirteen keys appear in the order step 6 fixes, and that
`git diff Directory.Build.props` is empty.

## Risks / open questions

- **Risk — the two version identities are conflated.** A four-part version fed to
  `scripts/Build-ReleaseArtifacts.ps1 -Version` fails its `ValidatePattern` immediately,
  which is the safe direction; the dangerous direction is editing
  `Directory.Build.props:9`. Mitigation: step 2 makes reading-not-editing explicit, and
  the fourth verification command plus `git diff Directory.Build.props` catch it.
- **Risk — the target name `_CreateMsixRecipe` is internal and may move.** It is an
  MSBuild implementation detail of the Windows App SDK packaging targets, not a
  documented contract. Mitigation: `BeforeTargets` lists **two** targets so the stamp
  still runs if one is renamed, and the verification reads the staged manifest rather
  than trusting the target fired. If neither target exists in the pinned Windows App SDK,
  `binlog-failure-analysis` over a `-bl` build names the real one — record the answer in
  this document rather than guessing a third name.
- **Risk — the desktop project does not exist yet.** `src/Pegasus.Desktop/` is created by
  [[FND-030]] and `ls src/` today shows only `Pegasus.Core`,
  `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`. Steps 3, 4 and the second
  and third verification commands cannot run before it lands. Mitigation: steps 5–7 and
  9–10 (the generator and its test) are independent of the project and can be completed
  and proven first; sequence the work that way rather than blocking the whole ticket.
- **Risk — the Linux publish of Web and Worker breaks.** Desktop projects are excluded
  from the Linux solution filter by [[FND-028]] (plan handle `DSK-02-03`). This ticket
  adds no project reference, so the risk is only that `Pegasus.slnx` is edited; the fourth
  verification command is the check.
- **Open question, and it is open — the `build` component outside CI.** Recorded as the
  one unticked box in this ticket's `open-questions` document, because the ticket body's
  step 8 instructs it and the "manual value" condition it names is met. Who answers it:
  the release owner. The default is taken and stated in step 8; what the box asks is where
  the number is read from until [[REL-016]] adds the desktop release table to
  `docs/operations.md`. This is not a formality — MSIX refuses a package whose four-part
  version is not higher than the installed one, and `ValidatePattern` checks the *shape*
  of the version, never its *ordering*.
- **Naming concern carried from the body.** The area plan § 4 says
  `Build-DesktopRelease.ps1` lives under `eng/packaging/` while its § 5 row says
  `scripts/`. This ticket places only the manifest generator and its test under
  `eng/packaging/` and takes no position on the script's home; [[REL-004]] resolves it and
  corrects the plan text. Parked in `open-questions` as a scope boundary owned by a named
  sibling, not opened as a question.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch touches
build files and scripts, so `n/a — docs-only` does not apply._
