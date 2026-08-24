---
id: TEST-014
type: ticket
title: >-
  DSK-08-14 · Vulnerability and SBOM step (`dotnet list package --vulnerable
  --include-transitive`, optional Syft SBOM)
status: backlog
area: testing
assignee: ''
profile: chore
labels:
  - desktop-conversion
  - plan-08
  - phase-8
  - tier-9
groups:
  - EPIC-009
  - HZN-009
links: []
blocks:
  - TEST-019
docs_todo: true
archived: false
created: '2026-08-24T07:53:33.944Z'
updated: '2026-08-24T11:08:29.160Z'
---

## What

Add the CI `dependency-scan` job that runs the dependency-vulnerability gate on every change, and extend the desktop package lane so a software bill of materials is attached to the desktop package artifact. This ticket owns the **job**; the gate's tool contract, its suppression register and the SBOM generator are owned by [[DSK-09-16]] and are consumed here, never rebuilt.

## Why

Proposal §21.2 stage 12 requires an SBOM and a vulnerability report before signing, and §22.2 lists dependency scanning among the security tests. The repository has neither, and no Dependabot. A desktop package is distributed to workstations and installs code outside the browser sandbox, so a transitive vulnerability in it is a different class of problem from one in a server image that is rebuilt on every release. The SBOM is also what makes "what version of what shipped in release N" answerable after the fact. Extends the lanes from [[DSK-08-13]]; feeds the cost analysis in [[DSK-08-19]]; consumes [[DSK-09-16]]'s gate contract, register and SBOM step, and shares the script with [[DSK-10-08]].

## Source of truth

- Plan row: `docs/desktop/08-testing/README.md` § 5 — `DSK-08-14`
- Plan detail: `docs/desktop/08-testing/README.md` § 4 (target state row "Security") and § 7 (CI minutes)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 21.2 stage 12, § 22.2 "Security tests"
- Repository evidence:
  - `.github/workflows/ci.yml` — the `repository-check` workflow and the artifact-upload pattern (`actions/upload-artifact@v6`, `if-no-files-found: error`)
  - `.github/actions/dotnet-build/action.yml` — locked restore; `dotnet list package --vulnerable` needs a restored graph, so the step runs after it
  - `Pegasus.slnx` and every `packages.lock.json` — the pinned graph the scan reads
  - `docs/engineering.md` § Required evidence tiers, tier 9 — dependency and dynamic scanning
- Binding decisions:
  - C-01 — the scan runs on the cheapest lane that can host it; `dotnet list package` needs no Windows-specific behaviour, so it belongs on `ubuntu-latest` unless a desktop-only package makes that impossible.
  - D-002 — the SBOM accompanies the package artifact; it is not a signing input and carries no key material.
- Depends on: `DSK-08-13` — the desktop lanes the step attaches to. `DSK-09-16` — owns the SBOM generator choice, the SBOM step, the vulnerability-gate tool contract (`scripts/Test-DependencyVulnerabilities.ps1`) and the suppression register (`docs/desktop/10-security-observability-performance/dependency-audit.md`); this ticket runs them from CI and builds none of them a second time. `DSK-10-08` — wires the same gate into `Directory.Build.props` and the threat register and is invoked from the job this ticket adds.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `authoring-github-workflows` (`dotnet/skills`, `.agents/skills/authoring-github-workflows/SKILL.md`)
- **MCP**: Microsoft Learn (`microsoft_docs_search`) for the current `dotnet list package --vulnerable` behaviour and exit-code semantics; Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` → `kanmer-closeout`. `leave-preparing` requires `plan` and `questions-resolved`; `enter-done` requires `proof` and `questions-resolved`. Call `get_doc_gates <id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-14`, `docs/desktop/09-release-update-and-distribution/README.md` § 5 row `DSK-09-16` and `docs/desktop/10-security-observability-performance/README.md` § 5 row `DSK-10-08`. The three-way split is **already ratified and is not reopened here**: [[DSK-09-16]] owns the SBOM generator choice, the SBOM step, the vulnerability-gate tool contract and the suppression register; [[DSK-10-08]] wires the same gate into `Directory.Build.props` and the threat register; this ticket owns the `dependency-scan` job that runs them and the assertion that the SBOM ships with the MSIX. Copy that split into the plan document as the recorded decision. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. Load `pegasus-desktop`, then `authoring-github-workflows`. Confirm with `microsoft_docs_search` whether `dotnet list package --vulnerable --include-transitive` sets a non-zero exit code on findings in SDK 10.0.302, and record which behaviour you observed. [[DSK-09-16]]'s contract already requires the script to parse the output rather than trust the exit code; if what you observe contradicts that contract, raise it to [[DSK-09-16]] and do not write a second parser here.
3. Check whether `scripts/Test-DependencyVulnerabilities.ps1` and its triage file `docs/desktop/10-security-observability-performance/dependency-audit.md` already exist from [[DSK-09-16]] (which owns both) or [[DSK-10-08]] (which shares them). If they do, add only the `dependency-scan` job that invokes the script and change no rule inside it. If they have not landed, create them with exactly the shape [[DSK-09-16]] steps 5 and 6 pin — the `-FailAt` severity parameter defaulting to `High` with `-ProjectOrSolution` defaulting to `./Pegasus.slnx`, `dotnet list … package --vulnerable --include-transitive --format json` parsed rather than trusted, one line per finding as `package@version severity advisory-url`, exit `1` at or above `-FailAt`, Moderate and Low reported without failing, and a triage file whose entries expire at their review date and then fail. Record in the plan document which case applied. Never a second scanner and never a second suppression register.
4. Add the `dependency-scan` job to `.github/workflows/ci.yml` on `ubuntu-latest`, `needs: changes`, gated on the build flag: `dotnet restore ./Pegasus.slnx --locked-mode`, then `pwsh ./scripts/Test-DependencyVulnerabilities.ps1 -FailAt High`, tee-ing the script's output to `artifacts/security/vulnerable-packages.json`. Add one comment in the workflow naming [[DSK-09-16]] as the owner of the severity rule and the register, so the next reader does not look for the rule in the job. The job must not be a dependency of any other job.
5. Upload `artifacts/security/vulnerable-packages.json` with `actions/upload-artifact@v6` and `if-no-files-found: error`.
6. Check whether an SBOM step already exists in the `desktop-package` job from [[DSK-09-16]], which owns the SBOM generator choice, the tool manifest pin and the SBOM step. If it does, add only the assertion that the SBOM is uploaded in the same artifact as the `.msix` and named for the package version, and change no existing step. If it has not landed, add it under [[DSK-09-16]] step 2's contract — the generator chosen by recorded comparison and pinned to an exact version in a tool manifest — and record in the plan document which case applied. Never a second SBOM generator.
7. Record in the workflow which runner each new step uses. Keep the dependency scan on `ubuntu-latest` — it does not need Windows, and under C-01 a Windows minute costs twice a Linux one. [[DSK-10-08]] explicitly refuses to add a new Windows job for this; the job added here is the one it invokes.
8. Open a PR and confirm the job runs, produces both artifacts and does not extend the critical path.
9. Prove the gate: temporarily pin a package version with a known High advisory in a scratch branch, confirm the job fails naming the package and advisory, then revert. Record both runs.
10. Add the lane to `docs/operations.md`, pointing at [[DSK-09-16]]'s register rather than describing a second one, and note the SBOM location in `docs/desktop/09-release-update-and-distribution/README.md` release evidence.
11. Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan document before opening the PR.

## Acceptance criteria

- [ ] A `dependency-scan` job exists on `ubuntu-latest`, is not on the critical path, and fails on a High or Critical advisory anywhere in the transitive graph, naming the package and advisory.
- [ ] Moderate and Low findings are reported without failing.
- [ ] An SBOM is produced for the desktop package and uploaded in the same artifact as the `.msix`, named for the package version.
- [ ] Exactly one vulnerability script and one suppression register exist in the tree, both under [[DSK-09-16]]'s contract, and the plan document records whether this ticket found them or created them.
- [ ] Exactly one SBOM generator exists, and the plan document records whether this ticket found the SBOM step or added it.
- [ ] No severity rule, suppression rule or generator choice is restated inside the job — the job invokes and asserts, it does not redefine.

## Verification

- [ ] `pwsh ./scripts/Test-DependencyVulnerabilities.ps1 -FailAt High` locally — expected: same finding set as the job reports, and the same exit code.
- [ ] Scratch branch pinning a known-vulnerable package — expected: the job fails naming the package and advisory; after revert, green.
- [ ] The workflow run artifacts — expected: `vulnerable-packages.json` and the SBOM file present, both non-empty, the SBOM in the same artifact as the `.msix`.
- [ ] `ls scripts/Test-DependencyVulnerabilities.ps1 docs/desktop/10-security-observability-performance/dependency-audit.md` — expected: exactly one of each, and no second scanner or triage file anywhere in the tree.

## Evidence tier

Tier 9 — Security/observability. It obliges dependency scanning through the real restored graph with a recorded, expiring exception path; it does not prove runtime security behaviour, which is [[DSK-08-11]].

## Documentation changes

- `docs/operations.md` — the `dependency-scan` lane and the SBOM artifact, linking to [[DSK-09-16]]'s suppression register rather than describing a second one.
- `docs/desktop/09-release-update-and-distribution/README.md` — note the SBOM as release evidence.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may edit `.github/workflows/ci.yml` and the two documentation files named above, and may create `scripts/Test-DependencyVulnerabilities.ps1` and `docs/desktop/10-security-observability-performance/dependency-audit.md` **only** in the case step 3 names where [[DSK-09-16]] has not landed, and then only to that ticket's pinned shape. Must not create a second suppression register, a second scanner or a second SBOM generator, must not change a rule inside [[DSK-09-16]]'s script or register, and must not upgrade a package to clear a finding — an upgrade is its own reviewed change, and major Windows App SDK or UI toolkit upgrades are never taken automatically (proposal §21.1).
- **Traps**: three plans name the desktop dependency-scan and SBOM controls ([[DSK-09-16]], [[DSK-10-08]], this one) — exactly one SBOM generator, one vulnerability script and one suppression register may exist: [[DSK-09-16]] owns the SBOM, the vulnerability script's contract and the triage file, [[DSK-10-08]] wires the same gate into `Directory.Build.props` and the threat register, and this ticket owns the `dependency-scan` job that runs them; a second copy of any of them is a stop condition. `dotnet list package --vulnerable` may exit 0 with findings — the script parses rather than trusting the exit code, and re-implementing that parsing in the job would be the second scanner. Every added Windows minute bills at 2× once private (C-01), so keep this lane on Linux.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
