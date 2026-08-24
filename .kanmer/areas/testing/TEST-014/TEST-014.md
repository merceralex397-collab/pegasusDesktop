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
updated: '2026-08-24T08:51:17.781Z'
---

## What

Add a CI step that fails on a known-vulnerable package anywhere in the dependency graph, and attach a software bill of materials to the desktop package artifact.

## Why

Proposal §21.2 stage 12 requires an SBOM and a vulnerability report before signing, and §22.2 lists dependency scanning among the security tests. The repository has neither, and no Dependabot. A desktop package is distributed to workstations and installs code outside the browser sandbox, so a transitive vulnerability in it is a different class of problem from one in a server image that is rebuilt on every release. The SBOM is also what makes "what version of what shipped in release N" answerable after the fact. Extends the lanes from [[DSK-08-13]]; feeds the cost analysis in [[DSK-08-19]] and coordinates with [[DSK-09-16]] and [[DSK-10-08]].

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
- Depends on: `DSK-08-13` — the desktop lanes the step attaches to.

## Routing

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `authoring-github-workflows` (`dotnet/skills`, `.agents/skills/authoring-github-workflows/SKILL.md`)
- **MCP**: Microsoft Learn (`microsoft_docs_search`) for the current `dotnet list package --vulnerable` behaviour and exit-code semantics; Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`)
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-verify` → `kanmer-closeout`. `leave-preparing` requires `plan` and `questions-resolved`; `enter-done` requires `proof` and `questions-resolved`. Call `get_doc_gates <id>` before every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Read `docs/desktop/08-testing/README.md` § 5 row `DSK-08-14`, `docs/desktop/09-release-update-and-distribution/README.md` § 5 row `DSK-09-16` and `docs/desktop/10-security-observability-performance/README.md` § 5 row `DSK-10-08`. Agree which of the three owns the CI step and which consume its output; record the split. Call `get_doc_gates` on this ticket id, then `take_ticket`, and work in the ticket's own worktree and branch.
2. Load `pegasus-desktop`, then `authoring-github-workflows`. Confirm with `microsoft_docs_search` whether `dotnet list package --vulnerable --include-transitive` sets a non-zero exit code on findings in SDK 10.0.302; if it does not, the step must parse the output and fail explicitly. Record which behaviour you observed.
3. Add a `dependency-scan` job to `.github/workflows/ci.yml`, `needs: changes`, gated on the build flag. Run `dotnet restore ./Pegasus.slnx --locked-mode` then `dotnet list package --vulnerable --include-transitive --format json`, writing the output to `artifacts/security/vulnerable-packages.json`.
4. Make the step fail when any package is reported at severity High or Critical, and warn (without failing) at Moderate and Low, printing package, version, severity and advisory URL. State that rule in one comment in the workflow so the next reader does not have to infer it.
5. Upload `artifacts/security/vulnerable-packages.json` with `actions/upload-artifact@v6` and `if-no-files-found: error`.
6. Add an SBOM step to the `desktop-package` job: produce a CycloneDX or SPDX document for the packaged output (Syft is the plan's suggestion; if a .NET-native generator is already available in the toolchain, prefer it and say why). Name the file after the package version and upload it alongside the `.msix` in the same artifact.
7. Record in the workflow which runner each new step uses. Keep the dependency scan on `ubuntu-latest` — it does not need Windows, and under C-01 a Windows minute costs twice a Linux one.
8. Add a documented suppression path for an accepted advisory: a single file listing package, version, advisory id, reason and review date, read by the failing step. An unreviewed suppression must expire — the step fails when a suppression's review date has passed.
9. Open a PR and confirm the job runs, produces both artifacts and does not extend the critical path (it must not be a dependency of any other job).
10. Prove the gate: temporarily pin a package version with a known High advisory in a scratch branch, confirm the step fails naming it, then revert. Record both runs.
11. Add the lane and the suppression file to `docs/operations.md`, and note the SBOM location in `docs/desktop/09-release-update-and-distribution/README.md` release evidence.
12. Run the simplification pass over the branch diff and record it under a dated `## Simplification pass` heading in the plan document before opening the PR.

## Acceptance criteria

- [ ] The lane fails on a High or Critical advisory anywhere in the transitive graph, naming the package and advisory.
- [ ] Moderate and Low findings are reported without failing.
- [ ] An SBOM is produced for the desktop package and uploaded with it.
- [ ] Suppressions are explicit, reasoned and expire on a review date.
- [ ] The scan runs on `ubuntu-latest` and is not on the critical path.

## Verification

- [ ] `dotnet list package --vulnerable --include-transitive` locally — expected: same finding set as the lane reports.
- [ ] Scratch branch pinning a known-vulnerable package — expected: the lane fails naming the package and advisory; after revert, green.
- [ ] The workflow run artifacts — expected: `vulnerable-packages.json` and the SBOM file present, both non-empty.

## Evidence tier

Tier 9 — Security/observability. It obliges dependency scanning through the real restored graph with a recorded, expiring exception path; it does not prove runtime security behaviour, which is [[DSK-08-11]].

## Documentation changes

- `docs/operations.md` — the dependency-scan lane, the SBOM artifact and the suppression file.
- `docs/desktop/09-release-update-and-distribution/README.md` — note the SBOM as release evidence.

## Guardrails

- **Azure**: no write.
- **Scope boundary**: may edit `.github/workflows/ci.yml` and add the suppression file and documentation. Must not upgrade a package to clear a finding in this ticket — an upgrade is its own reviewed change, and major Windows App SDK or UI toolkit upgrades are never taken automatically (proposal §21.1).
- **Traps**: `dotnet list package --vulnerable` may exit 0 with findings — verify and parse rather than trusting the exit code. Every added Windows minute bills at 2× once private (C-01), so keep this lane on Linux. Three plans name a dependency-scanning step ([[DSK-09-16]], [[DSK-10-08]], this one) — exactly one lane may exist.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the plan document.

## Outcome

_Filled at closeout._
