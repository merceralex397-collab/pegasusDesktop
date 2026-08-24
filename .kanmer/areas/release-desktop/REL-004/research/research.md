# Research — REL-004: what a desktop release build must do, and what the gateway build already proves

## Question

What must `scripts/Build-DesktopRelease.ps1` do so that runbook R1 step 2 has a single
command to run, and which of its safety properties can be copied from the existing
gateway release script rather than invented?

## Current behaviour

**The repository builds a gateway release, not a desktop one.**
`scripts/Build-ReleaseArtifacts.ps1` (130 lines) is the whole of it, and it is worth
reading as the precedent rather than as a comparison:

- `:1-9` — parameter block: `-Version` validated against `^\d+\.\d+\.\d+-alpha\.\d+$`,
  `-SourceRevision` against `^[0-9a-f]{40}$`, `-MigrationRuntimeIdentifier` against
  `^(win|linux|osx)-(x64|arm64)$` with a comment explaining why the bundle follows the
  terminal while the packages do not.
- `:16-23` — the two guards this ticket copies: `git rev-parse HEAD` must equal
  `-SourceRevision`, and `git status --porcelain=v1 --untracked-files=all` must be empty,
  each with a sentence-shaped `throw`.
- `:25-33` — the output-root escape guard: the release root is resolved with
  `[IO.Path]::GetFullPath` and must `StartsWith` the allowed root plus a directory
  separator, or it throws `'The release output escaped artifacts/releases.'`
- `:45-71` — locked restore per project, then `dotnet publish -r linux-x64
  --self-contained false` for Web and Worker, an OCI container archive, and
  `dotnet ef migrations bundle --self-contained -r win-x64`.
- `:51-59` — a self-check the desktop script should imitate in spirit: the published Web
  binary is executed with `--diagnostics-version` and its reported version and source SHA
  are compared against the parameters. The build proves its own identity rather than
  asserting it.
- `:92-126` — per-artifact SHA-256 via `Get-FileHash`, an `[ordered]` manifest, and
  `ConvertTo-Json -Depth 8 | Set-Content -Encoding utf8NoBOM`; `:126` writes the manifest
  path as the script's only stdout result.
- `:128-130` — `finally { Pop-Location }`.

**No parity-matrix row covers this, and none should.**
`docs/desktop/01-inventory-and-parity/parity-matrix.md` runs `PAR-01`…`PAR-46` over Razor
page models — 46 rows, counted rather than copied
(`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46`,
verified 2026-08-24). Release tooling is not an observable web capability and has no row.
Building a client package is new desktop responsibility under proposal § 21, not parity
work. The closest existing repository mechanism — the thing that does this job for the
gateway today — is `scripts/Build-ReleaseArtifacts.ps1:1-130`, which is why the whole
**Current behaviour** section above is a reading of that script.

## Findings

- The gateway script's safety shape is reusable **verbatim in structure** and is the
  reason a desktop build can be trusted: exact-HEAD, clean-tree, escape-guarded output
  root, per-step `if ($LASTEXITCODE -ne 0) { throw '…' }`, one stdout result.
  — `scripts/Build-ReleaseArtifacts.ps1:11-33`, and the `throw` after each `dotnet`
  invocation at `:46`, `:48`, `:50`, `:66`, `:68`, `:71`.
- The packaging command is the single-step `winapp package … --cert` form, preferred over
  a separate `winapp sign` — `.codex/skills/winui-packaging/SKILL.md` § Key Rules
  ("**Prefer `winapp package --cert`** over separate `winapp sign` — one step instead of
  two"). The same section makes `--timestamp` "critical for production — without it,
  signatures expire with the cert", and `--self-contained` "bundles Windows App SDK
  runtime — larger but no runtime dependency".
- Self-contained packaging is what removes the `Dependencies` element from the
  `.appinstaller` — proposal § 7.1, recorded in
  `docs/desktop/09-release-update-and-distribution/README.md` § 3 ("Self-contained .NET
  and Windows App SDK in the MSIX, no `Dependencies` element") and enforced by validator
  check 7 ([[REL-003]], plan handle `DSK-09-03`).
- Runbook R1 steps 1–4 are the acceptance shape for this script:
  clean checkout of the tagged commit → `Build-DesktopRelease.ps1 -Channel pilot
  -Version <ver>` → sign with timestamp and `signtool verify /pa /v` → generate and
  validate the `.appinstaller`. — `docs/desktop/09-release-update-and-distribution/runbooks.md`
  § R1.
- **The script publishes nothing.** R1 separates step 4 (generate and validate) from step
  6 (publish), and publication needs a written approval phrase at step 5. Publishing is
  `eng/packaging/Publish-DesktopRelease.ps1`, owned by [[REL-008]] (plan handle
  `DSK-09-10`).
- `artifacts/` is git-ignored — `.gitignore:20-21` (`**/artifacts/` and `/artifacts/`),
  confirmed with `git check-ignore -v artifacts/devcert.cer` →
  `.gitignore:21:/artifacts/`. So `artifacts/desktop-releases/<ver>/` cannot be committed
  by accident, which matches where `Build-ReleaseArtifacts.ps1` puts its own output
  (`artifacts/releases/<Version>`).
- The script's home is contested in the plan set: area plan § 4 says `eng/packaging/`,
  the § 5 row says `scripts/`. The body resolves it in favour of `scripts/` and requires
  the plan text to be corrected in the same task.
- `dotnet list package --vulnerable` is the vulnerability command named by the area plan
  § 3. Its exit-code behaviour is the trap: it returns `0` even when it reports findings,
  which [[REL-014]] (plan handle `DSK-09-16`) records explicitly. This ticket's step 8
  therefore inspects the **text**, not the exit code.

### Facts

Verified by reading this repository on 2026-08-24 unless a URL and fetch date is given.

| Fact | Source |
| --- | --- |
| The gateway release script's guards, publish steps, manifest shape and single stdout result | `scripts/Build-ReleaseArtifacts.ps1:1-130` (line references above) |
| `artifacts/` is git-ignored, so the release root is safe from accidental commit | `.gitignore:20-21`; `git check-ignore -v artifacts/devcert.cer` |
| The repository has no MSIX, signing or App Installer step today | `ls eng` → nothing; `.github/workflows/ci.yml` has no publish/sign/deploy lane |
| `winapp package --cert` is preferred over `winapp sign`; `--timestamp` is critical; `--self-contained` bundles the Windows App SDK | `.codex/skills/winui-packaging/SKILL.md` § Key Rules |
| Releases run from an authorised Windows terminal | `docs/adr/0007-direct-terminal-azure-deployment.md`; `.agents/skills/pegasus-release/SKILL.md` § The estate ("Read-only Azure checks need no approval. **Every write needs explicit operator approval for the exact target**") |
| R1 steps 1–4 are the commands this script must satisfy; publication is a separate, approval-gated step | `docs/desktop/09-release-update-and-distribution/runbooks.md` § R1 |
| The desktop package is self-contained, so the `.appinstaller` carries no `Dependencies` element | area plan § 3; proposal § 7.1 |
| `dotnet list package --vulnerable` returns exit `0` even when it reports findings | recorded in the [[REL-014]] body § Guardrails |
| SDK pin is `10.0.302` with `rollForward: latestFeature` | `global.json` |
| The parity matrix holds 46 rows, `PAR-01`…`PAR-46`, and none covers release tooling | `grep -c '^\| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46` |

### Assumptions

- **A-09-5 — `winapp package` accepts a build-output directory and emits a `.msix` whose
  name the script can control.** The skill's examples pass a directory
  (`winapp package ./bin/x64/Release/ --cert ./devcert.pfx`) but do not show an output
  name flag.
  *Confirmed by*: running `winapp package --help` on the release terminal and recording
  the actual flag; if there is none, the script renames the produced file to
  `Pegasus_<Version>_x64.msix` after packaging.
  *Breaks if wrong*: the `.appinstaller`'s `MainPackage/@Uri` names a file that does not
  exist and validator check 5 fails. The rename fallback removes the risk entirely, so
  prefer it over waiting for an answer. Parked in `open-questions`.
- **A-09-6 — `signtool` is on the release terminal's `PATH`.** It ships with the Windows
  SDK, not with the .NET SDK, and nothing in this repository installs it.
  *Confirmed by*: `signtool /?` on the release terminal, recorded in the ticket proof.
  *Breaks if wrong*: step 7's verification cannot run and a signed package would be
  published unverified. Mitigation: fail fast with a named message when `-Sign` is passed
  and `signtool` is not resolvable, rather than skipping the check. Parked in
  `open-questions`.
- **A-09-7 — the MSIX is not bit-for-bit reproducible.** `Directory.Build.props:6` sets
  `<Deterministic>true</Deterministic>` for the managed compile, but the package is a
  ZIP-family container with timestamps, and a signature plus an RFC-3161 timestamp is
  non-deterministic by construction.
  *Confirmed by*: the body's step 12 — build twice unsigned from the same clean HEAD and
  compare the **content** hash list.
  *Breaks if wrong* (that is, if hashes do differ for content too): the release record's
  reproducibility claim is false. The body already requires recording the observed result
  including instability, rather than asserting reproducibility that was not observed.
  Record what happens; do not make the assertion the acceptance criterion. Parked in
  `open-questions`.
- **A-09-8 — the desktop project exists and restores under `--locked-mode`.**
  `src/Pegasus.Desktop/` is created by [[FND-030]] (plan handle `DSK-02-05`); `ls src/`
  today shows only `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`,
  `Pegasus.Worker`.
  *Confirmed by*: `dotnet restore ./src/Pegasus.Desktop/Pegasus.Desktop.csproj --locked-mode`
  exiting `0`.
  *Breaks if wrong*: nothing in this ticket can be executed end to end. The parameter
  block, guards and manifest/validator wiring can still be written and reviewed first.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered for the responsibility
this ticket places: *producing and self-verifying an immutable release artefact*.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **no** | A release artefact is produced once per version and never mutated; `artifacts/desktop-releases/<ver>/` is deleted and recreated per run (the shape `Build-ReleaseArtifacts.ps1:30-33` uses). |
| Unattended execution — must it run with every desktop closed? | **no** | ADR-0007 fixes releases to an attended authorised terminal, and R1 step 1 begins "on the authorised release terminal". The tag-triggered variant ([[REL-015]], plan handle `DSK-09-17`) is still gated on a human approval. |
| Protected credentials — long-lived secret that must not sit on workstations? | **no, for this script** | It takes `-CertificatePath` and never stores or embeds key material. Custody of the `.pfx` is D-002's decision and [[REL-007]]'s (plan handle `DSK-09-08`) work: it stays on the in-house signing host with a restricted ACL, and is explicitly **not** a GitHub secret. Note this is a placement answer, not an Azure answer — the responsibility lands on the in-house signing host. |
| Public callback — must an external service call a stable public endpoint? | **no** | Nothing calls the build. It reads the working tree and writes to `artifacts/`. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **no** | The invariants this script enforces (clean HEAD, exact revision, validator exit code, no `High`/`Critical` advisory) are build-time and local. The client-facing fail-closed rule is the gateway minimum-version gate, [[GWY-023]] (plan handle `DSK-04-06`). |
| Measured operational advantage — measured evidence central is materially better? | **no** | No measurement exists and none is claimed. A hosted build could not sign at all: D-002 confines the key to the signing host, which is why [[REL-015]] requires a self-hosted runner there. |

All six "no" → the release build belongs on the authorised terminal (or on a self-hosted
runner on the signing host), and **the desktop release path touches no Azure resource at
all** — D-002 withdrew the Artifact Signing and Key Vault routes, D-003 withdrew the blob
feed.

## Implications

- **Copy the gateway script's safety shape, do not re-derive it.** The exact-HEAD,
  clean-tree and escape-guard patterns already have production history; re-inventing them
  is how a release script quietly loses one.
- **Signing is opt-in and paired.** `-Sign` without both `-CertificatePath` and
  `-TimestampUrl` must fail immediately, before any build work, because a signed package
  without a timestamp is a latent estate-wide failure (every new install stops working the
  day the certificate expires).
- **Verify the signature before writing anything else.** Step 7's ordering matters: if
  `signtool verify /pa /v` does not report both a chain and a timestamp, no manifest,
  `.appinstaller` or hash list should exist to be mistaken for a releasable set.
- **The validator is a gate, not a report.** A non-zero exit from
  `eng/packaging/Test-AppInstaller.ps1` aborts the build ([[REL-003]] fixed its exit-code
  contract).
- **The SBOM generator is another ticket's decision — and the body still requires it be
  recorded as an open question here.** [[REL-014]] is the single owner of the generator
  choice, the vulnerability-gate contract and the suppression register, and its body says so
  explicitly. This ticket adds the `-SbomPath` pass-through and stops there. The body's
  step 8 nevertheless says in as many words to "record the open question in
  `open-questions/`", and [[REL-014]]'s own step 1 expects to read "the open question
  `DSK-09-04` left" — so the document exists, with the SBOM question **below
  `## Parked (explicitly deferred)`**: recorded and owned, not blocking.
- **Resolve the script's home in this ticket and fix the plan text.** Two paths in one
  plan set is exactly the drift § 7 of the area plan warns about; the body chooses
  `scripts/` and requires the § 4 correction in the same task.

## Open questions

**This ticket has an `open-questions` document, and every entry in it is parked.**

The earlier draft of this section said "No `open-questions` document is created", giving as
its reason that opening an item "would block every stage move on this ticket for a decision a
named sibling ticket already owns". The first half of that is false: an unticked `- [ ]` line
above `## Parked` blocks exactly `leave-preparing`, `enter-review` and `enter-done`, and never
`leave-backlog`. Verified 2026-08-24 with `get_doc_gates REL-004` — for a `feature`,
`questions-resolved` sits at three of the four boundaries and `leave-backlog` carries only
`governing-doc`.

The second half is sound and survives, so it decides the *shape* rather than the existence of
the document: a decision a named sibling ticket owns is a scope boundary, so the entries are
parked rather than unticked, and `questions-resolved` stays satisfied.

Recorded there:

- **The SBOM generator choice** — deferred to [[REL-014]]. This is the entry the ticket body
  instructs, and the one `REL-014` step 1 goes looking for.
- **A-09-5** (`winapp package` output-name flag), **A-09-6** (`signtool` on `PATH`) and
  **A-09-7** (MSIX determinism) — each settled by running one command on the release terminal
  during implementation, and each with a recorded fallback that does not require an answer
  first: unconditional rename, fail-fast `Get-Command signtool`, and measure-don't-assert.
- **The script's home** — not a question at all; this ticket resolves it in favour of
  `scripts/` and corrects the area plan § 4 in the same task.
