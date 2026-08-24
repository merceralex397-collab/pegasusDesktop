# Open questions — REL-004 (plan handle `DSK-09-04`): the desktop release build

**Why this document exists.** The ticket body instructs it, unconditionally. Step 8 reads:
*"Leave the SBOM **generator choice** to [[DSK-09-16]]: add a `-SbomPath` pass-through
parameter now, **record the open question in `open-questions/`**, and do not invent a
generator here."* And the receiving ticket expects to find it:
[[REL-014]] (plan handle `DSK-09-16`) step 1 says *"read `scripts/Build-DesktopRelease.ps1`
to see the `-SbomPath` hook and **the open question [[DSK-09-04]] left**"*. The body outranks
the author.

The earlier plan and research declined to create this, reasoning that "an unticked item would
block every stage move on this ticket for a decision a named sibling ticket already owns". The
first half is false — an unticked `- [ ]` line above `## Parked` blocks exactly
`leave-preparing`, `enter-review` and `enter-done`, and never `leave-backlog`. The second half
is sound, and it is why every entry below is **parked** rather than unticked: the body tells
this ticket to add the pass-through and proceed, so nothing here should stop it. A parked
entry records the question without blocking, which is exactly the shape the authoring
contract § 7 provides for a deferrable question.

**Nothing in this document is unticked, so `questions-resolved` stays satisfied and this
ticket's gates are unaffected.** That is deliberate. If a later reader needs one of these to
block, promote it above the `## Parked` heading — do not delete it.

## Parked (explicitly deferred)

- **Which SBOM generator produces the desktop release SBOM, and what its output contract
  is.** *Deferred to [[REL-014]] (plan handle `DSK-09-16`), which the board makes the single
  owner of the generator choice, the SBOM step, the vulnerability-gate tool contract and the
  suppression register.* This is the question body step 8 directs be recorded here.

  What this ticket does instead, and where the seam is: `scripts/Build-DesktopRelease.ps1`
  takes a `-SbomPath` parameter now (plan step 3) so `REL-014` can extend additively rather
  than reopening this script's parameter block. This ticket **invents no generator** and takes
  no position on CycloneDX versus SPDX, on which tool emits it, or on where the suppression
  register lives.

  Reason for deferring rather than blocking: the body says to add the hook and proceed, and
  [[REL-014]] is a phase-2 sibling that can land in parallel. Blocking `leave-preparing` here
  would serialise two contemporaneous tickets for a decision this one is forbidden from
  making.

  Answered when: `REL-014` records the generator choice and the gate contract. At that point
  the only change here is what the caller passes to `-SbomPath`.

  Note the adjacent thing that is **not** deferred and is this ticket's own: the
  **vulnerability report**. Plan step 8 writes it with
  `dotnet list ./src/Pegasus.Desktop/Pegasus.Desktop.csproj package --vulnerable
  --include-transitive` and throws on `Critical` or `High` **found in the text**, never on the
  exit code — `dotnet list package --vulnerable` returns `0` even when it reports findings, so
  a gate that trusts the exit code is a no-op.

- **Does `winapp package` accept an output-file name, or must the produced `.msix` be renamed?**
  (Assumption **A-09-5**.) Parked because it is answered by running `winapp package --help` on
  the release terminal during implementation, and because the fallback removes the risk
  entirely: plan step 6 renames the produced file to `Pegasus_<Version>_x64.msix`
  **unconditionally**, rather than conditionally on a flag that may not exist. The
  `.appinstaller`'s `MainPackage/@Uri` names that file and validator check 5 hashes it.

- **Is `signtool` on the release terminal's `PATH`?** (Assumption **A-09-6**.) It ships with
  the Windows SDK, not the .NET SDK, and nothing in this repository installs it. Parked
  because it is answered by looking, and because the recorded behaviour is fail-fast rather
  than degrade: plan step 3 runs `Get-Command signtool -ErrorAction SilentlyContinue` when
  `-Sign` is passed and throws with a named message if it is missing. **Never skip step 7's
  verification** — a signed package published unverified is the failure this guard exists for.

- **Is the MSIX bit-for-bit reproducible?** (Assumption **A-09-7**.) Parked because the body
  already converts it from a question into a measurement: plan step 12 builds twice unsigned
  from the same clean HEAD and compares the **content** hash list, and records the observed
  result *including instability*. `Directory.Build.props:6` sets
  `<Deterministic>true</Deterministic>` for the managed compile, but an MSIX is a ZIP-family
  container and a signature plus an RFC-3161 timestamp is non-deterministic by construction.
  The acceptance criterion is the measurement, not a stable hash — do not write a
  reproducibility claim that was not measured.

- **Where does `Build-DesktopRelease.ps1` live — `scripts/` or `eng/packaging/`?** Not open:
  this ticket **resolves** it. The area plan § 4 says `eng/packaging/` and its § 5 row says
  `scripts/`; the body chooses `scripts/`, beside `Build-ReleaseArtifacts.ps1`, and plan
  step 11 corrects § 4 in the same task and greps `docs/` for every other hit. Recorded here
  only so a reader who meets the contradiction elsewhere knows it was decided, not missed.

- **D-002 (self-managed certificate), D-003 (in-house UNC share) and C-01 (private
  repositories).** Not open and not to be re-opened — D-002 and D-003 were decided by the
  operator on 2026-08-23. This script takes a `-CertificatePath` and never stores key
  material; it stages output for copy and publishes nothing; and no artefact may depend on an
  anonymous GitHub download, so GitHub Releases and GitHub Pages are permanently ruled out.
