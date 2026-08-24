# Plan — REL-014: DSK-09-16 · SBOM and vulnerability report per desktop release

**Diff estimate: ~5 files, ~250 lines.** One entry added to the **existing**
`.config/dotnet-tools.json` (~8 lines); `scripts/Build-DesktopRelease.ps1` extended (~40
lines: the generator invocation, the SBOM hash and manifest entry, and the swap from the
inline text check to the gate script); new `scripts/Test-DependencyVulnerabilities.ps1`
(~100 lines: parameter block, JSON parse, per-finding line, severity comparison, the
`-AcceptVulnerabilities` escape and the register read); new
`docs/desktop/10-security-observability-performance/dependency-audit.md` (~40 lines: the row
shape, the expiry rule and the single-file sentence); one SBOM step added to the
`desktop-package` lane in `.github/workflows/ci.yml` (~10 lines) **only if `DSK-08-14` has
not already added it**. `docs/engineering.md:201-207` § Plan sizing requires the estimate
first.

## Approach

**Own four things once, and make every other ticket consume them.** Three plans name the
desktop dependency-scan and SBOM controls — `DSK-08-14` (board `TEST-014`), `DSK-10-08`
(board `PLAT-008`) and this one — and the board settles the split by making this ticket the
single owner of the SBOM artefact and its generator choice, the vulnerability gate, the
gate's tool contract, and the suppression register. `DSK-08-14` owns only the CI
`dependency-scan` job that runs them and the SBOM-upload assertion; `DSK-10-08` wires the
same gate into `Directory.Build.props` and the threat register. A second copy of any of the
four is a stop condition.

The generator is chosen by a **recorded comparison**, not silently: step 2 compares
`Microsoft.Sbom.DotNetTool` (SPDX) and `CycloneDX.DotNet` on five stated criteria and writes
the result into the ticket research. That comparison is the only place on the board where
the choice is made on evidence, and it is what makes this ticket the owner rather than
merely the first arrival.

The rejected alternative for the gate was **trusting `dotnet list package --vulnerable`'s
exit code**. It returns `0` even when it reports findings, so a gate built on it is a no-op
that looks like a control — the worst possible outcome for a security check. The script
therefore parses `--format json` and compares severities itself. A second rejected
alternative was **a new Windows CI job for the scan**: `dotnet list package` needs no Windows
behaviour, and constraint C-01 makes a private-repository Windows minute cost twice a Linux
one, so the gate runs from the Linux `dependency-scan` job `DSK-08-14` owns.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-014`). No existing PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by `DSK-09-01` (board `REL-001`). Its Consequences record
> the self-contained package: .NET and the Windows App SDK ship **inside** the MSIX, which is
> exactly why `--include-transitive` is not optional here. This plan is written to the
> decisions as recorded in `docs/desktop/09-release-update-and-distribution/README.md` § 3
> ("SBOM and vulnerability report are produced per release … plus an SBOM generator chosen in
> DSK-09-16"; "Self-contained .NET and Windows App SDK in the MSIX, no `Dependencies`
> element"); if ADR-0105 lands differently, this plan is revised before implementation.

Existing documents this plan **meets**:

- **`AGENTS.md` § New Markdown placement** — any `.md` outside
  `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job. **Meets**: the
  suppression register is created at
  `docs/desktop/10-security-observability-performance/dependency-audit.md`, which
  `scripts/Test-MarkdownPlacement.ps1:31`'s allowed-roots regex admits. That folder contains
  only `README.md` today, so the file is new.

Binding constraints, written to as settled:

- **C-01** — private-repository Windows minutes bill at a 2× multiplier, so an SBOM step that
  adds minutes must be measured and justified against the cost plan in `DSK-08-19` (board
  `TEST-019`), and the vulnerability scan runs on the **Linux** lane `DSK-08-14` (board
  `TEST-014`) owns rather than in a new Windows job.
- **L-02** — no Azure scanning service is introduced; the report is produced locally and in
  CI.

Ownership contracts this plan **defines** and two siblings consume: the SBOM generator choice
and step; the gate script `scripts/Test-DependencyVulnerabilities.ps1` and its parameter
contract; the suppression register
`docs/desktop/10-security-observability-performance/dependency-audit.md`.

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in the
plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) →
  `authoring-github-workflows` (`dotnet/skills` `98f84851`,
  `.agents/skills/authoring-github-workflows/SKILL.md` once `DSK-12-02`, board `TOOL-002`,
  vendors it — it is **not** in `.agents/skills/` today, which holds only `pegasus-release`
  and `project/`).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`,
  `microsoft_docs_fetch`) for the SBOM tooling and `dotnet list package --vulnerable`
  documentation.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates REL-014` before
  every move; a move crosses at most one gated boundary. `get_doc_gates` reports two gated
  boundaries: `leave-preparing` needs `plan` (this document), `enter-done` needs `proof`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's twelve implementation steps in the same order, with the same
ownership and the same paths.

1. **Orient and take.** Read the area plan § 5 row `DSK-09-16` and § 3, then
   `scripts/Build-DesktopRelease.ps1` to see the `-SbomPath` hook and the inline
   vulnerability text check `DSK-09-04` (board `REL-004`) left. Read `DSK-08-14` (board
   `TEST-014`) and `DSK-10-08` (board `PLAT-008`) so the split this ticket ratifies is in
   front of you. `get_doc_gates REL-014`, then `take_ticket REL-014`.
2. **Decide and record the generator.** Compare at least `Microsoft.Sbom.DotNetTool` (SPDX
   output) and `CycloneDX.DotNet` on five criteria: dotnet-tool installability under a locked
   restore; output format; whether it covers a **self-contained publish's runtime files**
   (the decisive criterion here, since the MSIX ships .NET and the Windows App SDK);
   licence; and added CI minutes. Use `microsoft_docs_search` for
   `SBOM tool dotnet SPDX generate` and `microsoft_docs_fetch` the result, recording URLs and
   fetch dates. Write the comparison and the choice into the ticket's `research` scratch —
   **do not pick silently.** This is the only recorded comparison on the board and it is what
   makes this ticket the SBOM owner.
3. **Pin the tool the way the repository already pins tools.** A manifest already exists:
   `.config/dotnet-tools.json`, `"version": 1`, `"isRoot": true`, currently holding
   `dotnet-ef` `10.0.10` with `"rollForward": false`. Add the chosen generator as a second
   entry with an **exact** version and the same `rollForward: false` discipline, restored with
   `dotnet tool restore`. Do **not** create a second manifest — a tool restored without a
   pinned version turns a release build into a moving target.
4. **Extend `scripts/Build-DesktopRelease.ps1`** to run the generator over the desktop publish
   output and write `sbom.json` (or the tool's canonical name) into
   `artifacts/desktop-releases/<ver>/`, then add its SHA-256 to the hash list and an `sbom`
   entry to `desktop-release-manifest.json`. `artifacts/` is git-ignored
   (`.gitignore:20-21`), so the output cannot be committed by accident.
5. **Pin the one vulnerability-gate contract in one implementation.** Check whether
   `scripts/Test-DependencyVulnerabilities.ps1` already exists from `DSK-10-08` (board
   `PLAT-008`); if it does, extend it in place under this contract and **change no rule
   inside it**; if it does not, create it here. Record in this document which case applied.
   The contract, verbatim:
   `[CmdletBinding()] param([ValidateSet('Low','Moderate','High','Critical')][string] $FailAt = 'High', [string] $ProjectOrSolution = './Pegasus.slnx')`
   with the repository script header (`Set-StrictMode -Version Latest`,
   `$ErrorActionPreference = 'Stop'`, the shape every script in `scripts/` uses). It runs
   `dotnet list $ProjectOrSolution package --vulnerable --include-transitive --format json`,
   **parses the result rather than trusting the exit code**, prints one line per finding as
   `package@version severity advisory-url`, and exits `1` when any finding is at or above
   `-FailAt`. `Moderate` and `Low` are reported without failing. The release build calls it
   with `-ProjectOrSolution ./src/Pegasus.Desktop/Pegasus.Desktop.csproj`, writes the output
   to `vulnerability-report.txt` beside the SBOM, and fails the release on a non-zero exit.
   Provide an explicit `-AcceptVulnerabilities <reason>` escape that writes the triage text
   into the register named in step 6 **and** into the release manifest rather than silently
   continuing — an unrecorded waiver is the failure mode this gate exists to prevent. Never a
   second scanner.
6. **Pin the one suppression register**:
   `docs/desktop/10-security-observability-performance/dependency-audit.md`, one row per
   accepted finding — package, version, advisory id, why it is accepted, review date, and the
   ticket that will remove it. The script reads it and treats a listed advisory as non-fatal
   **until its review date passes**, then fails. The path sits in plan 10's folder because
   that is where it was first named and moving it would strand `DSK-10-08`'s threat-register
   row; ownership of its **contents** is here, and `DSK-10-08` and `DSK-08-14` add rows rather
   than creating a second file. Keep it under `docs/desktop/` — anywhere else fails the CI
   `documentation` job (`scripts/Test-MarkdownPlacement.ps1:31`). Verified today: that folder
   contains only `README.md`, so the file is new; check again at implementation time in case
   `DSK-10-08` landed first, and extend it in place if so.
7. **Record why the transitive flag matters, in the script header.** The package is
   **self-contained**, so runtime and Windows App SDK dependencies ship inside the MSIX;
   omitting `--include-transitive` under-reports the shipped surface. One sentence, at the
   point of use.
8. **Wire CI without adding a second step of anything.**
   - **SBOM**: check whether an SBOM step already exists in the `desktop-package` lane
     (`DSK-09-05`, board `REL-005`) from `DSK-08-14` (board `TEST-014`) —
     `grep -n "sbom" .github/workflows/ci.yml`. If it does, extend it in place under step 2's
     contract and add no second generator; if it has not landed, add the step here and upload
     the SBOM with `actions/upload-artifact@v6` **alongside the MSIX in the same artifact**
     (`desktop-msix-unsigned`), with the same `if-no-files-found: error` discipline the
     workflow already uses at `ci.yml:179`.
   - **Vulnerability gate**: it runs from the `dependency-scan` job `DSK-08-14` owns on
     `ubuntu-latest`. **Add no vulnerability step to `desktop-package` and create no job
     here** — `dotnet list package` needs no Windows behaviour and a Windows minute costs
     twice a Linux one (C-01).
   Record in this document which case applied on each half.
9. **Measure the added CI time.** Run the lane before and after, note the delta in minutes,
   and hand the number to `DSK-08-19` (board `TEST-019`). If the delta is material, **decide
   and record** whether the SBOM should run only on the tag lane (`DSK-09-17`, board
   `REL-015`) rather than every PR — do not leave it implied.
10. **Define the release-record shape.** Which fields of the SBOM and vulnerability report
    appear in the desktop release row (`DSK-09-18`, board `REL-016`) — at minimum the SBOM
    file hash and a clean/triaged flag.
11. **Prove it end to end.**
    `pwsh ./scripts/Build-DesktopRelease.ps1 -Channel pilot -Version 1.0.<run>.0 -SourceRevision $(git rev-parse HEAD)`
    on a clean tree, confirming the SBOM and the vulnerability report are produced, hashed and
    referenced from `desktop-release-manifest.json`.
12. **Simplification pass.** Record it under a dated `## Simplification pass` heading in this
    document (`AGENTS.md` § Repository task workflow step 4). This branch changes scripts, a
    tool manifest and CI, so `n/a — docs-only` does not apply.

## Verification

Evidence tier from the body: **Tier 9 — Security/observability.** The obligation is
dependency-scanning evidence per release: a produced SBOM, a scanned dependency graph
**including transitives**, and a failure (or a recorded triage) on a high-severity finding.
`proof` is the command output as `command-log` and `test-output`, plus the CI run URL and the
measured minute delta.

| Command / observation | Expected evidence |
| --- | --- |
| `dotnet tool restore` | exit `0` with the pinned generator version, restored from the existing `.config/dotnet-tools.json` |
| `pwsh ./scripts/Build-DesktopRelease.ps1 -Channel pilot -Version 1.0.1.0 -SourceRevision $(git rev-parse HEAD)` | `sbom.json` and `vulnerability-report.txt` present under `artifacts/desktop-releases/<ver>/`; both hashes appear in `desktop-release-manifest.json` |
| `pwsh ./scripts/Test-DependencyVulnerabilities.ps1 -FailAt High` on a clean tree | exit `0` and no finding printed |
| the same with a planted `High` advisory | exit `1` and the `package@version severity advisory-url` line printed |
| `dotnet list ./src/Pegasus.Desktop/Pegasus.Desktop.csproj package --vulnerable --include-transitive` | no `High`/`Critical` row, or a recorded triage with a reason in the register |
| `ls scripts/Test-DependencyVulnerabilities.ps1 docs/desktop/10-security-observability-performance/dependency-audit.md` | exactly one of each; and no second scanner or triage file anywhere in the tree |
| CI run of `desktop-package` | green, both artefacts attached, and the measured minute delta recorded in the ticket |

Behaviour to observe rather than infer: the planted-advisory case must exit `1` **because the
script parsed a finding**, not because `dotnet` failed — run `dotnet list package --vulnerable`
alone on the same tree and record that it exits `0`, so the proof shows why the exit code
could not have been the gate.

## Risks / open questions

- **Risk — a gate that trusts the exit code.** `dotnet list package --vulnerable` returns `0`
  even when it reports findings; such a gate is a no-op that looks like a control.
  Mitigation: step 5 parses `--format json`, and the verification records the bare command's
  exit `0` alongside the script's exit `1`.
- **Risk — a second scanner, generator or register.** A stop condition, and the reason this
  ticket exists as an owner rather than a contributor. Mitigation: steps 5, 6 and 8 are state
  checks whose outcome is recorded here, and the sixth verification command asserts exactly
  one of each.
- **Risk — an unrecorded waiver.** Worse than no gate. Mitigation: `-AcceptVulnerabilities`
  writes the triage into both the register and the release manifest, and the register's rows
  expire on a review date.
- **Risk — `--include-transitive` omitted.** The package is self-contained and ships its own
  runtime, so omitting it under-reports the shipped surface materially. Mitigation: it is in
  the contract at step 5 and explained in the script header at step 7.
- **Risk — an unpinned tool.** A release build becomes a moving target. Mitigation: step 3
  adds an exact version with `rollForward: false` to the existing manifest.
- **Risk — CI cost.** Private-repository Windows minutes bill at 2× (C-01). Mitigation: the
  scan stays on Linux in `DSK-08-14`'s job, and step 9 measures the SBOM step's delta rather
  than assuming it is free — with an explicit decision recorded if it is material.
- **Risk — the register lands outside an allowed Markdown root.** It would fail the CI
  `documentation` job. Mitigation: step 6 keeps it under `docs/desktop/`, which
  `scripts/Test-MarkdownPlacement.ps1:31` admits.
- **Open questions**: none that block. The generator choice is **this ticket's to make**, on
  the evidence step 2 gathers — it is a decision, not a question, and no `open-questions`
  document is created for it. The two state checks (whether `DSK-10-08` created the gate
  script, whether `DSK-08-14` added the SBOM step) are resolved by looking at the branch.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch changes
scripts, a tool manifest and CI, so `n/a — docs-only` does not apply._
