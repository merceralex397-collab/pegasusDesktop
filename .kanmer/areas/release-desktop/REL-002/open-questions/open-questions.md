# Open questions — REL-002 (plan handle `DSK-09-02`): the `build` component outside CI

**Why this document exists.** The ticket body instructs it. Step 8 reads: *"Decide and record
where the `build` component comes from outside CI … Write that sentence into the script's
header comment **and add an `open-questions/` entry if the release terminal needs a manual
value.**"* The condition is met — this plan's own default has the release terminal supplying
a manually-determined number on any build that did not come from CI — so the entry is owed.
The body outranks the author.

The earlier plan declined to open this, reasoning that "an unticked item would block every
stage move for a question this default answers". The first half is false: an unticked `- [ ]`
line above `## Parked` blocks exactly `leave-preparing`, `enter-review` and `enter-done`, and
never `leave-backlog`; for profile `chore` the board declares only `leave-preparing` and
`enter-done`. The second half is half true — the default answers *what* to pass, but not
*where the number is read from*, and that is what the box below asks.

Blocking `leave-preparing` is the right place for it, because this ticket freezes the
thirteen-field manifest contract that [[REL-003]] (plan handle `DSK-09-03`) validates against
and [[REL-004]] (plan handle `DSK-09-04`) calls. `buildRun` is one of those thirteen fields.

## Unresolved

- [ ] **Where does a non-CI build read its `build` number from, before `docs/operations.md`
      has a desktop release table?**

      The default this plan takes is recorded in step 8 and must go into the generator's
      header comment: *the release terminal passes the run number of the CI run that built
      the tagged commit; where no CI run exists (a local rehearsal build) it passes a value
      higher than the last published `build` for that channel, taken from the desktop release
      row in `docs/operations.md`.*

      The gap is in the second clause. Verified 2026-08-24: `docs/operations.md` exists and
      carries the **gateway** release table (rows through release 14), but **no desktop
      release table** — that table and the compatibility range are authored by
      [[REL-016]] (plan handle `DSK-09-18`), which is a phase-9 row. This ticket is phase-1.
      So for the whole span between this ticket and `REL-016` the default names a source that
      does not exist.

      This matters beyond tidiness: MSIX will refuse to install a package whose four-part
      version is not higher than the installed one, so a rehearsal build that guesses low is
      silently un-installable over the current package, and `-Version` is `[Parameter(Mandatory)]`
      with `ValidatePattern '^1\.\d+\.\d+\.0$'` — the shape is checked, the *ordering* is not.

      Answer it by naming the authoritative source that holds until `REL-016` lands, and
      record the answer in this plan's step 8 and in the generator's header comment. The two
      candidates, either of which is acceptable if written down:
      - the highest `version` among the `desktop-release-manifest.json` files already
        published to that channel's folder on the D-003 UNC share — authoritative, and it
        exists from the first pilot release; or
      - a `build` floor recorded per channel in this area's own documentation until
        `docs/operations.md` gains the table.

      Who answers it: the release owner. Tick when the chosen source is named in step 8, in
      the script header comment, and in this box with its date.

## Parked (explicitly deferred)

- **Which of `eng/packaging/` or `scripts/` is `Build-DesktopRelease.ps1`'s home.** Parked,
  not open: the area plan § 4 and § 5 disagree, and [[REL-004]] resolves it and corrects the
  plan text. This ticket places only the manifest generator and its test under
  `eng/packaging/` and takes no position. A decision a named sibling ticket owns is a scope
  boundary, not an open question.

- **Whether ADR-0105 fixes a different version scheme.** Parked: ADR-0105 is authored by
  [[REL-001]] (plan handle `DSK-09-01`) and its decision clause (c) is already recorded in
  `docs/desktop/09-release-update-and-distribution/README.md` § 3 as `1.<minor>.<build>.0`,
  revision always `0`. This plan is written to that recorded decision and is revised if the
  ADR lands differently — a named-ticket dependency, not a question.

- **The real name of the Windows App SDK packaging target the stamp hooks.**
  `_CreateMsixRecipe` is an MSBuild implementation detail. Parked because it is answerable by
  looking rather than by asking: `BeforeTargets` lists two targets, the verification reads the
  **staged** `Package.appxmanifest` rather than trusting the target fired, and
  `binlog-failure-analysis` over a `-bl` build names the real one if neither exists. Record
  the answer in the plan; do not guess a third name.

- **D-002 (self-managed certificate) and D-003 (in-house UNC share).** Not open and not to be
  re-opened — both decided by the operator on 2026-08-23. `signerSubject` /
  `signerThumbprint` record that certificate, and `channel` is one of the share's two folders,
  `pilot` or `prod`, never a URL host.
