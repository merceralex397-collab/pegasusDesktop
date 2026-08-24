# Research — REL-005: the existing CI workflow, and where exactly one desktop packaging lane fits

## Question

What shape must the `desktop-package` job take so it proves MSIX generation on every
relevant pull request without disturbing the nine existing jobs, and which of the three
tickets that name an overlapping desktop lane owns which piece?

## Current behaviour

**One workflow, nine jobs, no publish/sign/deploy lane.**
`.github/workflows/ci.yml` is 234 lines and is called `repository-check`:

- `:3-6` — triggers are `pull_request:` (all branches) and `push: branches: [main]`.
  **There is no tag trigger today**, which is why [[REL-015]] (plan handle `DSK-09-17`) puts
  its lane in a separate workflow rather than extending this one.
- `:8-9` — `permissions: contents: read` at workflow level.
- `:12` `changes` — `runs-on: ubuntu-latest`, `timeout-minutes: 5`, with a leading comment
  saying it is path detection only and therefore not Linux-development evidence. It
  outputs exactly two values, `build` (`:17`) and `infrastructure` (`:18`), computed at
  `:33-53` by calling `./scripts/Get-CiChangeFlags.ps1`. It also runs the shard, migration
  grant and Local-mode Azure plan checks at `:55-69`.
- `:71` `documentation` — `windows-latest`, with a comment explaining why it stays on
  Windows (`Test-Path` is case-insensitive there and case-sensitive on Linux, so moving
  it would quietly change the rule). Runs
  `./scripts/Test-TestMarkdownPlacement.ps1` (`:83`) and
  `./scripts/Test-DocumentationLinks.ps1` (`:86`). This is "the one lane every change set
  runs" — it has no `if:` gate.
- `:89` `local-development-scripts`, `:100` `reference-data`, `:115` `infrastructure` —
  all `windows-latest`.
- `:131` `unit` — the shape to copy: `needs: changes`,
  `if: needs.changes.outputs.build == 'true'`, `runs-on: windows-latest`,
  `timeout-minutes: 20`, steps `actions/checkout@v7` then
  `uses: ./.github/actions/dotnet-build` then a `Test` step. Its `run: >` block at
  `:143-148` chains two `dotnet test` invocations with `&&` and carries the comment
  explaining why: "pwsh reports only the last command's exit code, so a failing first
  project would otherwise pass the step".
- `:149` `sql-integration` (three shards, `windows-latest`), which uploads with
  `actions/upload-artifact@v6` at `:179`.
- `:185` `sql-integration-coverage` — `runs-on: ubuntu-latest`; the cross-shard check.
- `:207` `browser` — `windows-latest`, Playwright.

Seven of the nine run on `windows-latest`; only `changes` and `sql-integration-coverage`
are on `ubuntu-latest`. That ratio is what makes constraint C-01's 2× private-repository
Windows multiplier a real cost rather than a theoretical one.

`.github/actions/dotnet-build/action.yml` is the composite action every build lane uses:
`actions/setup-dotnet@v6` pinned to `10.0.x` with `cache: true` keyed on `global.json`,
`src/**/packages.lock.json` and `tests/**/packages.lock.json`, then
`dotnet restore ./Pegasus.slnx --locked-mode` and
`dotnet build ./Pegasus.slnx --configuration Release --no-restore`. Its own description
says sharing one definition "keeps the cache key from drifting between lanes" — which is
why step 5 of this ticket must use it rather than pinning the SDK inline.

**No parity-matrix row covers this, and none should.** CI is repository infrastructure, not
an observable web capability; `docs/desktop/01-inventory-and-parity/parity-matrix.md` holds
46 rows, `PAR-01`…`PAR-46`, all Razor page models — counted, not copied
(`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46`, verified
2026-08-24). The closest existing repository mechanism is `.github/workflows/ci.yml` itself,
read above.

## Findings

- **The change classifier emits only two flags today.**
  `scripts/Get-CiChangeFlags.ps1:23-26` returns a `[pscustomobject]` with exactly `Build`
  and `Infrastructure`. `$buildPattern` at `:11` is
  `^(src|tests)/|^Pegasus\.slnx$|\.csproj$|\.props$|\.targets$|packages\.lock\.json$|^global\.json$|^nuget\.config$|^scripts/(Invoke-TestShard|Test-(MainBranchHistory|TestShard)|Get-CiChangeFlags)\.ps1$|^\.github/workflows/ci\.yml$|^\.github/actions/`.
  It does **not** match `^eng/`. A change to `eng/packaging/Test-AppInstaller.ps1` today
  therefore sets no flag and triggers no lane.
- **Adding a third flag is not a one-line change.**
  `scripts/Test-CiChangeFlags.ps1:9-21`'s `Assert-Flags` helper takes exactly
  `-Build` and `-Infrastructure` as mandatory `[bool]`s and compares both; `:23-30` are
  eight existing cases. A `Desktop` output means changing the helper signature and every
  existing call. That cost is a concrete argument for the body's preferred branch:
  gate on [[TEST-013]]'s (plan handle `DSK-08-13`) `desktop` flag when it has landed and
  change nothing.
- **Three tickets name an overlapping desktop lane**, and the body settles the split on
  the evidence: [[FND-040]] (plan handle `DSK-02-15`, "CI lane `desktop-build` on
  `windows-latest`: locked restore, x64 Release build, desktop tests") proves build and
  tests and **packages nothing**; this ticket owns `desktop-package`; [[TEST-013]]
  ("`ci.yml` lanes: `desktop-build`, `desktop-package`, `desktop-ui-smoke`,
  `packaging-tests`") **owns the `desktop` change flag and the `changes` job output that
  carries it** and extends this one job with `desktop-ui-smoke` and `packaging-tests`.
- **The CI runner has no production certificate, by decision.** D-002 confines the `.pfx`
  to the signing host and forbids it as a GitHub secret; the area plan § 7 records "CI
  runner has no production certificate: signing only in the protected tag job or the
  release terminal; PR builds use the dev cert."
- **`winapp cert install` writes to the machine Trusted Root store** —
  `.codex/skills/winui-packaging/SKILL.md` § End-to-End Workflow step 3: "Adds cert to
  machine Trusted Root store. Persists across reboots." On a shared runner that is a
  materially broader grant than packaging needs, and the body forbids it. The
  `winapp cert generate --manifest` + `winapp package --cert` pair is enough: packaging
  does not require the certificate to be trusted, only installing does.
- **`--if-exists skip` keeps re-runs from failing** on an existing certificate — the
  skill's § CI/CD sample uses `winapp cert generate --if-exists skip --quiet`.
- **`--manifest` auto-matches the certificate subject to `Package.appxmanifest`'s
  `Publisher`** — the skill's step 2 — which is the fix for the Publisher-mismatch
  packaging failure (`0x8007000B`).
- **The artifact name is a cross-ticket literal.** [[TEST-013]]'s `desktop-ui-smoke`
  downloads the MSIX by exactly the name `desktop-msix-unsigned`, so renaming it breaks a
  sibling lane silently.
- **`actions/upload-artifact@v6` is the version already in use** at `ci.yml:179`; matching
  it avoids a second major version in one workflow.

### Facts

Verified by reading this repository on 2026-08-24.

| Fact | Source |
| --- | --- |
| Nine jobs, seven on `windows-latest`; triggers `pull_request` + `push: branches: [main]`; `permissions: contents: read`; no publish/sign/deploy lane and no tag trigger | `.github/workflows/ci.yml:1-234` |
| The `unit` job's shape and the `&&`-chaining comment | `.github/workflows/ci.yml:131-148` |
| `actions/upload-artifact@v6` already in use | `.github/workflows/ci.yml:179` |
| The composite action's SDK pin, cache key and locked restore, and its stated purpose | `.github/actions/dotnet-build/action.yml` |
| `$buildPattern` does not match `^eng/`; the classifier emits only `Build` and `Infrastructure` | `scripts/Get-CiChangeFlags.ps1:11`, `:23-26` |
| `Assert-Flags` takes exactly `-Build` and `-Infrastructure`; eight existing cases | `scripts/Test-CiChangeFlags.ps1:9-30` |
| `winapp cert install` writes to machine Trusted Root; `--manifest` auto-matches Publisher; `--if-exists skip --quiet` in the CI sample; `setup-WinAppCli@v0.1` | `.codex/skills/winui-packaging/SKILL.md` |
| GitHub Actions Azure deployment is recorded as `Not planned` | `docs/runbook.md:903` |
| C-01: private-repository Windows runners bill at a 2× multiplier; the cost plan is [[TEST-019]] (plan handle `DSK-08-19`) | `docs/desktop/README.md` § Constraints recorded after planning began |
| The desktop test project this lane runs, `tests/Pegasus.Desktop.ViewModelTests`, does not exist yet | `ls tests/` → `Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests` only |
| The parity matrix holds 46 rows, `PAR-01`…`PAR-46`, and none covers CI | `grep -c '^\| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46` |

### Assumptions

- **A-09-9 — `microsoft/setup-WinAppCli@v0.1` is usable from this repository's runners.**
  It is the action the vendored skill's CI sample uses, but nothing in this repository has
  ever run it.
  *Confirmed by*: the first `desktop-package` run on a PR completing the
  `setup-WinAppCli` step.
  *Breaks if wrong*: the lane cannot package at all. Fallback recorded rather than
  invented: `winget install Microsoft.WinAppCLI` on the runner, which
  `docs/desktop/08-testing/test-uat-stack.md` § Machine prerequisites already names as
  the install route for the Test/UAT machine.
- **A-09-10 — `Test-Package.ps1` ([[TEST-010]], plan handle `DSK-08-10`) can run at least
  partially on a hosted runner.** Some of its scenarios install a package, which needs a
  trusted certificate on the machine — and the body forbids `winapp cert install` on a
  shared runner.
  *Confirmed by*: running it and reading which scenarios it skips.
  *Breaks if wrong*: the lane either fails or silently drops coverage. The body already
  fixes the behaviour: run only the scenarios that do not require installation and
  **record which are skipped in the job summary** — do not silently drop them.
- **A-09-11 — the desktop build output path contains a single target framework
  directory.** The packaging step needs
  `./src/Pegasus.Desktop/bin/x64/Release/<tfm>/`, and `<tfm>` is fixed by
  [[FND-030]] (plan handle `DSK-02-05`).
  *Confirmed by*: reading `src/Pegasus.Desktop/Pegasus.Desktop.csproj`'s
  `TargetFramework` once it exists.
  *Breaks if wrong*: a hard-coded `<tfm>` breaks on the next Windows App SDK bump.
  Mitigation: resolve the directory with a glob and fail with a named message if it does
  not match exactly one directory.
- **A-09-12 — [[TEST-013]] has not landed when this ticket is worked.**
  Both the phase labels (this ticket is `phase-2`, `TEST-013` is a testing-area lane) and
  the body's conditional wording assume it may go either way.
  *Confirmed by*: `grep -n "desktop-package\|outputs.desktop" .github/workflows/ci.yml`
  and `grep -n "desktop" scripts/Get-CiChangeFlags.ps1` at implementation time.
  *Breaks if wrong*: a second `desktop-package` job or a second pattern for the same
  paths — both stop conditions. Steps 3 and 4 make the check mandatory and require the
  outcome to be recorded.

## Execution placement

**This ticket places no product responsibility anywhere — it is CI work**, so the
six-question cloud-justification test of
`docs/desktop/00-governance-and-workflow/README.md` § 3 does not apply to it, and the
heading is kept rather than dropped to say so. This is the case the authoring contract
carves out explicitly: *a PR lane using a generated development certificate places nothing*.
That is exactly this lane — the certificate is generated on the runner by
`winapp cert generate`, is never trusted or installed, and no `secrets.` reference may
appear in the workflow. The artefact it uploads (`desktop-msix-unsigned`) is an unsigned
intermediate consumed by a sibling lane inside the same PR run, not a published release; the
tag lane that *does* place a real credential responsibility is [[REL-015]], and it answers
all six there.

The one placement decision this lane touches is already made and is not this ticket's to
re-open: the production signing key stays on the in-house signing host (**D-002**).

## Implications

- **Check before adding, twice.** Steps 3 and 4 are state checks against
  `scripts/Get-CiChangeFlags.ps1` and `.github/workflows/ci.yml` on the branch, not
  questions for anyone. Their outcome is recorded in the plan document, which is what
  makes the three-way lane overlap auditable rather than accidental.
- **Prefer gating on [[TEST-013]]'s flag.** Beyond the ownership rule, the classifier's
  test helper makes a third output a multi-file change (`Test-CiChangeFlags.ps1:9-30`), so
  the fallback branch is genuinely more expensive as well as less correct.
- **Use the composite action; never re-pin the SDK inline.** The action exists precisely
  to keep the cache key from drifting, and a second pin would be the drift it prevents.
- **Chain `dotnet test` calls with `&&` on one `run: >` line.** `ci.yml:143-148` records
  the reason in a comment: pwsh reports only the last command's exit code.
- **Never `winapp cert install` on a hosted runner.** Packaging needs the certificate, not
  its trust; trust is only needed to *install*, and installing on a shared runner is what
  the body forbids.
- **The artifact name `desktop-msix-unsigned` is a contract**, consumed by [[TEST-013]]'s
  `desktop-ui-smoke`. One name, one artifact.
- **Cost is a first-class constraint here.** C-01 makes a duplicate Windows lane bill
  twice; if [[FND-040]]'s `desktop-build` lane has landed, take its result through
  `needs:` and drop the duplicate test step rather than paying for the same minutes.

## Open questions

**None opened — and the reason is not that opening one would be expensive.**

The earlier draft of this section said "an unticked item would block every stage move". That
is false and is withdrawn: an unticked `- [ ]` line above `## Parked` blocks exactly
`leave-preparing`, `enter-review` and `enter-done`, and never `leave-backlog`. This ticket is
a `feature`, so `questions-resolved` sits at three of its four boundaries and `leave-backlog`
carries only `governing-doc`. Blocking Preparing would have been affordable if there were a
real question.

There is not, and the sound half of the earlier reasoning is why: **every branch this ticket
has is resolved by looking at the branch, by the implementer, in a step the ticket already
owns.** Specifically —

- whether [[TEST-013]]'s `desktop` flag has landed, whether a `desktop-package` job already
  exists, and whether [[FND-040]]'s `desktop-build` lane is available to take `needs:` from
  (A-09-12) — all three are the state checks in steps 3 and 4, and their outcome is recorded
  in the plan;
- `A-09-9` (is `setup-WinAppCli` usable?), `A-09-10` (can `Test-Package.ps1` run on a hosted
  runner?) and `A-09-11` (is there exactly one `<tfm>` directory?) are each settled by the
  first CI run, and each has a recorded fallback that does not need an answer first — a
  `winget install` route, a run-what-you-can-and-report-the-skips rule, and a glob that fails
  with a named message rather than a hard-coded framework moniker.

Nothing in this ticket's body instructs that a question be recorded in `open-questions/`, and
the two decisions that are genuinely not this ticket's — the `desktop` change flag
([[TEST-013]]) and the Windows-runner cost plan ([[TEST-019]]) — are scope boundaries owned by
named sibling tickets, which the authoring contract keeps in this section rather than in an
`open-questions` document.
