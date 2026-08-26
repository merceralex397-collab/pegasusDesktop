# Checklist

- [x] Name the triage-request category once; classification and operational routing use the shared constant
- [x] A classified triage request is `NeedsSorting`, never `CaseCreated`
- [x] One `AcceptedTriageMatch` evidence entry derived from the classification
- [x] `IIntakeTriageMatcher`, `NoAcceptedIntakeTriageMatcher`, `IntakeTriageMatch` deleted
- [x] DI registration and extraction-policy parameter removed; policy version 5 → 6
- [x] Subject registration rule; vehicle rule stops swallowing the label
- [x] Triage created when a registration is known
- [x] Unidentified registered when it is not, and not when a Triage was created
- [x] Core tests: decision, evidence, both branches, both subject spacings, ambiguity
- [x] Existing downstream integration suites retain their valid contract fixtures; new QdosTriageIntegrationTests cover the real default route (scope deviation documented below)
- [x] Production composition test pins the active route
- [x] `docs/open-decisions.md` triage-matcher paragraph closed
- [x] FRD-03, FRD-09, `qdos.md`, and `capabilities.md` checked; capabilities owner remains TRI-02 and required updates were applied elsewhere
- [x] Release build green
- [x] Core tests green
- [x] Local non-Corpus/non-Browser integration tests green; exact-head CI is a PR gate
- [x] Simplification pass over the branch diff, recorded in the plan
- [x] Exact-head GitHub Actions run 32992629383 passed for commit `c25099f92681db991a0003146991b676d1c8b82b`; all repository-check jobs, including browser, three SQL shards, coverage, unit, infrastructure, documentation, changes, reference-data, and local-development-scripts, passed
- [x] PR into `dev`, independent review, merge — PR #21 merged at `36dccd8fa1c883c38977b6721d86b745c45c9a94` after Gibbs PASS and exact-head CI 32992629383
- [x] Proof on merged `main` — merged-main commit `36dccd8fa1c883c38977b6721d86b745c45c9a94` verified; `proof.md` written

## Progress notes

**2026-08-24** — Implemented on `task/intk-033-triage-from-intake`, commit `7b43ab17`.

Scope deviation from the plan, recorded rather than quietly taken: the plan said
to move four integration suites off the `AcceptedTriageMatchPolicy` stub. I did
not. Those suites test the downstream contract — replay safety, multi-match
fail-closed, case association — and that contract is unchanged, so the stub is
still a valid way to reach it. Rewriting them would have been a large diff for
no added proof. Instead the existing `QdosTriageIntegrationTests` class now
drives the real default classification path end to end and proves both branches
of the operator's rule.

Also changed and worth a reviewer's eye:
`ProcessIntakeTests.ClassificationIsRecordedOnlyAndNeverChangesTheIntakeDecision`
pinned "a classification never changes the decision" using a **triage** message.
That invariant was already qualified before this branch — the standalone-Audit
rule at `ProcessIntake.cs:189` has downgraded `CaseCreated` on a classification
fact for some time — and FRD-01 only forbids classification mutating *Case*
state, which this does not do. The test now uses an automatic reply, where the
invariant genuinely holds, and the triage exception is its own named test.

Corpus reading narrowed fault 3: the body-phrase template carries a labelled
`Registration:` line and was already extracted correctly. Only the subject
template needed work.

**2026-08-26 execution amendment** — upstream sync/re-check is removed as a prerequisite. Work is performed only in the PegasusDesktop repository from `origin/dev`; upstream material is retained as read-only provenance. No upstream, cloud, mailbox, Box, credential, deployment, or external write is permitted.

**2026-08-26 validation** — `dotnet restore` passed; `dotnet build --configuration Release` passed with 0 warnings and 0 errors; full Core tests passed 935/935; targeted SQL integration passed 19/19; full non-Corpus/non-Browser integration passed 886/886 with 2 expected skips. Final simplification changes were revalidated by a final 119/119 focused Core pass and 19/19 targeted SQL integration pass.

**2026-08-26 exact-head CI** — Added the repository's existing `repository-check` workflow_dispatch trigger so the exact PR head could be validated after pull_request event registration failed to create a run. Manual run 32992629383 passed at commit `c25099f92681db991a0003146991b676d1c8b82b`; this does not substitute for independent review or merged-main proof.


**2026-08-26 closeout evidence** — The final PR head passed exact-head CI and independent review, merged to `dev`, was promoted to `main`, and was revalidated from the merged-main commit.
