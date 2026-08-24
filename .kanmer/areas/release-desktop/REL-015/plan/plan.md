# Plan — REL-015: DSK-09-17 · Tag-triggered sign and publish lane to the pilot feed

**Diff estimate: ~3 files, ~120 lines.** One new workflow
`.github/workflows/desktop-release.yml` (~95 lines: trigger and permissions, the provenance
guard, the version-derivation check, the composite build step, the signed build, the two
verification gates, the publish call with its `prod` guard, the artifact upload and the job
summary); ~15 lines added to `docs/desktop/09-release-update-and-distribution/runbooks.md`
across R1 and R2; ~3 lines added to `docs/engineering.md` § Branches and delivery,
coordinated with `DSK-00-09` (board `FND-009`).
`docs/engineering.md:201-207` § Plan sizing requires the estimate first.

## Approach

**Automate the repeated half, keep the irreversible half deliberate, and put both rules in
code.** Pilot publication is repeated, reversible and observed, so automating R1 steps 3–6
removes real friction; production publication is none of those things, and R2's preconditions
— R1 completed for the same `<ver>` and an elapsed soak period — are conditions no workflow
can evaluate. So the lane refuses `-Channel prod` with an explicit guard rather than merely
not using it, because a guard survives a future edit and a convention does not.

The runner is decided by D-002, not by preference: the `.pfx` is not a GitHub secret and
never leaves the signing host, so a hosted runner **cannot sign at all**. The alternative
rejected is therefore not "hosted vs self-hosted" but "self-hosted or nothing", and the
recorded fallback when the runner does not yet exist is to land the workflow with the job
`if: false` and a comment — never to route signing through a hosted runner.

A **new workflow file** is chosen over extending `ci.yml`: its trigger block is
`pull_request` + `push: branches: [main]`, and adding `push: tags:` there would run all nine
existing jobs on every tag — wasteful under C-01's 2× private-repository Windows multiplier
and confusing in the checks list. The reasoning is recorded here so it is not re-litigated.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-015`). No existing PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by `DSK-09-01` (board `REL-001`). Its Consequences record
> D-002 (self-managed certificate confined to the signing host) and D-003 (UNC feed), which
> together decide this lane's runner and its publish target. This plan is written to the
> decisions as recorded in `docs/desktop/09-release-update-and-distribution/README.md` § 3
> "Signing" and "Publication" and `signing-and-hosting-decision-matrix.md` § D-002; if
> ADR-0105 lands differently, this plan is revised before implementation.

Existing documents this plan **meets**:

- **`docs/desktop/00-governance-and-workflow/README.md:203-211`** — the branching flow's item
  4: "CI builds an unsigned MSIX on every PR and builds + signs on `main` tags only;
  publishing to the production feed stays a runbook-controlled step … pilot-feed publishing to
  the decided UNC share (D-003) may be automated once D-002 settles how packages are signed."
  **Meets**: steps 2–8 are that sentence's execution, and step 3's provenance guard is what
  makes "on `main` tags only" true rather than nominal.
- **`docs/runbook.md:903`** — "GitHub Actions/OIDC deployment is `Not planned`." **Meets**:
  the lane authenticates to no cloud service and creates no Azure resource; nothing in it is
  a deployment path.

Binding operator decisions and constraints, written to as settled:

- **D-002** (2026-08-23) — the `.pfx` never leaves the signing host and is **not** a GitHub
  secret, so the lane runs on a self-hosted runner there, not on `windows-latest`.
- **D-003** (2026-08-23) — the publish target is the **UNC share**, reachable from the signing
  host by file copy.
- **C-01** — the repositories become private; a self-hosted runner on the share host is the
  cost-driven shape, decided by `DSK-08-19` (board `TEST-019`).

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
  vendors it — it is **not** in `.agents/skills/` today) → `winui-packaging`
  (`.codex/skills/winui-packaging/SKILL.md`, `microsoft/win-dev-skills` v0.5.0 `f1028dd5`,
  verified present).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`,
  `microsoft_docs_fetch`) for signing and App Installer questions; Azure MCP read-only only
  if an inventory record is wanted — this lane touches no Azure resource.
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates REL-015` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's thirteen implementation steps in the same order, with the same
ownership and the same paths. Step 11 is an **Operator step**.

1. **Orient and take.** Read the area plan § 5 row `DSK-09-17`, § 3 "Publication",
   `runbooks.md` § R1 steps 3–6 and § R2, and `.github/workflows/ci.yml` in full.
   `get_doc_gates REL-015`, then `take_ticket REL-015`.
2. **Decide and record the workflow file.** `ci.yml`'s trigger block is `pull_request` +
   `push: branches: [main]` (`:3-6`); adding `push: tags:` there would run all nine existing
   jobs on every tag. Create a **new** workflow `.github/workflows/desktop-release.yml` with
   `on: push: tags: ['desktop/v*']` and `permissions: contents: read` (copying `ci.yml:8-9`
   rather than widening it), and record the reasoning in this document so the choice is not
   re-litigated.
3. **Guard the tag's provenance in the first job step.** Resolve the tag's commit and fail
   unless it is contained in `main`:
   `git merge-base --is-ancestor <tag sha> origin/main`. A tag can point at any commit,
   including one on an unmerged branch, and **a tag on an unmerged branch must not produce a
   signed package.** Check out with enough history for the command to work
   (`fetch-depth: 0`, as `ci.yml:21-22` does for the history guard).
4. **Run on the signing host's self-hosted runner.**
   `runs-on: [self-hosted, windows, pegasus-signing]` — taking the labels from `DSK-08-19`
   (board `TEST-019`) rather than inventing them, and adding `timeout-minutes` so a
   mislabelled job fails rather than queueing forever. D-002 forbids the `.pfx` being a
   GitHub secret, so a hosted runner cannot sign. **If the self-hosted runner is not yet
   available, land the workflow with the job `if: false` and an explicit comment** rather than
   inventing a hosted-signing route; record that this is what was done.
5. **Add the two-part approval gate.** Put the signing/publishing job behind a repository
   `environment:` that requires a reviewer, **and** require the operator to record the literal
   `FEED PUBLISH GRANTED pilot <ver>` — in the wording `DSK-09-11` (board `REL-009`) step 2
   confirmed — in the ticket before approving. Both are required: **the phrase is the audit
   trail, the environment is the mechanism.** If the environment feature is unavailable on
   this repository, keep the job `if: false` and record that fact rather than publishing on
   the phrase alone.
6. **Job steps, in order.** `actions/checkout@v7` at the tag; `uses: ./.github/actions/dotnet-build`
   (so the SDK pin and NuGet cache key match every other lane — never a second inline pin);
   then
   `pwsh ./scripts/Build-DesktopRelease.ps1 -Channel pilot -Version <version from the tag>
   -SourceRevision <tag sha> -Sign -CertificatePath <host path> -TimestampUrl <timestamp
   service>`. Derive `<version>` from the tag name (`desktop/v1.2.345` → `1.2.345.0`) and
   **fail immediately** if the derived value does not match `^1\.\d+\.\d+\.0$` — before the
   build, not after it.
7. **Verify before publishing, twice.** `signtool verify /pa /v` on the produced `.msix` must
   report a valid chain **and** a timestamp — read the output for the timestamp line, do not
   accept exit `0` alone — and `pwsh ./eng/packaging/Test-AppInstaller.ps1` must exit `0`.
   Either failure aborts the job **before anything reaches the feed**.
8. **Publish, and refuse `prod` in code.**
   `pwsh ./eng/packaging/Publish-DesktopRelease.ps1 -Channel pilot` — package first,
   `.appinstaller` last, previous package retained. Add an **explicit guard that refuses
   `-Channel prod` in this workflow**, so a future edit cannot quietly widen it. Automating
   pilot publication is safe because it is reversible and observed; R2's preconditions are
   conditions no workflow can evaluate.
9. **Record the release identity.** Upload the signed package, manifest, SBOM and hashes with
   `actions/upload-artifact@v6` (matching `ci.yml:179`), and write version, source commit,
   package SHA-256, signer thumbprint and compatibility range into the job summary
   (`>> $env:GITHUB_STEP_SUMMARY`), satisfying proposal § 21.2 item 15.
10. **Dry run first, with a development certificate to a local feed.** Point
    `-CertificatePath` at the development certificate from `DSK-09-06` (board `REL-006`) and
    `-FeedRoot` at the Test/UAT stack's share, and run the whole lane. **Only after that
    passes** should the real certificate and the real pilot feed be used. Record both runs.
11. **Operator step — the first real run.** Push `desktop/v<ver>` on `main`, record
    `FEED PUBLISH GRANTED pilot <ver>`, approve the environment, and confirm the pilot feed
    received the files. Hand back the run URL and the feed listing.
12. **Update both runbooks in this task.** In `runbooks.md` § R1, note which steps this lane
    now performs automatically and which remain manual — otherwise the next operator executes
    a step the lane already performed. In § R2, state plainly that **production publication is
    never automated**: it stays the terminal step with `FEED PUBLISH GRANTED prod <ver>`. Add
    the one sentence to `docs/engineering.md` § Branches and delivery saying the
    `desktop/v<M.m.b>` tag now triggers this lane, coordinating with `DSK-00-09` (board
    `FND-009`), which owns the tag convention there.
13. **Simplification pass.** Record it under a dated `## Simplification pass` heading in this
    document (`AGENTS.md` § Repository task workflow step 4). This branch adds a workflow, so
    `n/a — docs-only` does not apply.

## Verification

Evidence tier from the body: **Tier 9 — Security/observability**, as the plan row assigns.
The obligation is evidence that the signing path is **confined to a protected job on a
controlled host**, that approvals are enforced before publication, and that no signing
material is exposed to CI. `proof` is the two run URLs, the job summary, the feed listing and
the four `grep`/push checks, as `command-log`.

| Command / observation | Expected evidence |
| --- | --- |
| Dry run with the dev certificate to the Test/UAT feed | lane green; package signed; validator exit `0`; files present on the stack share **in the correct order** (package before manifest) |
| Real run on `desktop/v<ver>` | environment approval requested; lane green after approval; pilot feed carries the new `.msix`, `.appinstaller` and manifest, **with the previous package retained** |
| `git push` of a `desktop/v*` tag pointing at a commit not in `main` | the lane fails at the provenance check **before building** |
| `grep -n "secrets\." .github/workflows/desktop-release.yml` | no certificate or password secret referenced |
| Push to `main` without a tag | `desktop-release.yml` does not run; `ci.yml` behaves exactly as before |

Behaviours to observe rather than infer, and to state in the proof: whether the self-hosted
runner existed (and if not, that the job was landed `if: false` with a comment); whether the
`environment:` approval prompt actually appeared; and that the `prod` guard was exercised —
run the publish step's guard once with `-Channel prod` in the dry run and record the refusal.

## Risks / open questions

- **Risk — the `.pfx` becomes a GitHub secret.** D-002 forbids it, and C-01's private
  repositories make the estate the only safe place for it. Mitigation: the fourth verification
  command, and step 4's rule that the fallback is a disabled job, never a hosted-signing
  route.
- **Risk — a tag on an unmerged branch produces a signed package.** Mitigation: step 3's
  provenance guard runs before anything else, with `fetch-depth: 0` so the command can resolve.
- **Risk — the lane quietly widens to `prod`.** Mitigation: step 8's explicit guard, exercised
  once in the dry run so its refusal is evidenced rather than assumed.
- **Risk — only one half of the approval is enforced.** Mitigation: step 5 requires both, and
  records the fact if the `environment:` feature is unavailable rather than falling back to
  the phrase alone.
- **Risk — a signature without a timestamp.** It hides for up to three years, then breaks
  every new install. Mitigation: step 7 reads the output for the timestamp line.
- **Risk — a second SDK pin drifts the NuGet cache key.** Mitigation: step 6 uses the
  composite action.
- **Risk — duplicated Windows minutes.** C-01's 2× multiplier. Mitigation: the tag lane must
  not duplicate the PR lane's work beyond what signing requires; record the measured duration
  for `DSK-08-19` (board `TEST-019`).
- **Risk — the signing host is a single point of failure and a high-value target.** Its
  isolation and permissions are `DSK-08-19`'s to specify and this lane's to respect; do not
  loosen them here.
- **Open questions**: none that block. Whether the self-hosted runner exists, what its labels
  are, and whether the `environment:` feature is available are each established by looking,
  and each has a recorded fallback that leaves the workflow reviewable and mergeable. **No
  `open-questions` document is created** — opening one would block every stage move for a
  runner decision `DSK-08-19` (board `TEST-019`) owns.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch adds a
workflow, so `n/a — docs-only` does not apply._
