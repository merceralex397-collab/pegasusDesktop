# Research — REL-015: where a signing lane can run, given that the key may never be a secret

## Question

What shape can an automated sign-and-publish lane take when D-002 forbids the private key
from being a GitHub secret, and what must the lane refuse to do so that automating the pilot
half never widens into automating the production half?

## Current behaviour

**There is no signing lane, no tag trigger and no deployment from CI.** Verified on
2026-08-24:

- `.github/workflows/ci.yml:1-11` — the only workflow, named `repository-check`, triggered by
  `pull_request:` (all branches) and `push: branches: [main]`, with
  `permissions: contents: read`. **There is no `push: tags:` trigger anywhere.**
- Nine jobs, seven on `windows-latest`; none signs, publishes or deploys.
  `actions/upload-artifact@v6` is used at `:179`.
- `docs/runbook.md:903`: "`azd up` is not the release procedure. GitHub Actions/OIDC
  deployment is `Not planned`."
- The fork has **no tags** (`git tag` empty on 2026-08-23, recorded in
  `docs/desktop/00-governance-and-workflow/README.md` § 2). The convention that will exist is
  recorded at `:203-211`: tag releases on `main` as `gateway/r<N>` and `desktop/v<M.m.b>`
  (= MSIX version), with "CI builds an unsigned MSIX on every PR and builds + signs on `main`
  tags only; publishing to the production feed stays a runbook-controlled step (same culture
  as the `pegasus-release` skill); pilot-feed publishing to the decided UNC share (D-003) may
  be automated once D-002 settles how packages are signed."
  [[FND-009]] (plan handle `DSK-00-09`) owns recording that convention in
  `docs/engineering.md`.

**No parity-matrix row covers this, and none should.**
`docs/desktop/01-inventory-and-parity/parity-matrix.md` holds 46 rows, `PAR-01`…`PAR-46`, all
Razor page models — counted, not copied
(`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46`, verified
2026-08-24). A release automation lane is repository infrastructure; nothing here is parity
work. The closest existing repository mechanism is `.github/workflows/ci.yml` plus the manual
`pegasus-release` procedure, both read above.

## Findings

- **D-002 decides the runner, not just the certificate.** The matrix's chosen shape says the
  `.pfx` "stays on the signing host with an ACL limited to the publisher account. It is
  **not** stored as a GitHub secret: with the repositories going private (constraint C-01)
  the natural signing host is the same always-on machine that serves the share and hosts the
  self-hosted CI runner, so the key never leaves the estate."
  (`signing-and-hosting-decision-matrix.md` § D-002.) A hosted runner therefore cannot sign,
  and the lane must run on a self-hosted runner on that host.
- **The same host carries three roles, and that is the recorded risk.** § How the decisions
  interact: "**One machine carries it all**: the always-on host serves the share, runs the
  self-hosted CI runner that constraint C-01 makes attractive, and custodies the signing
  `.pfx`. That concentration is the design's main operational risk — it is a single point of
  failure for publishing (not for running: installed clients keep working) and a single
  high-value target."
- **The lane automates R1 steps 3–6 and must not touch R2.** Area plan § 3 Publication: "The
  pilot feed publish may be automated from CI once D-002 is decided and approved (the feed
  itself is settled: D-003, the UNC share); production feed publish stays a
  runbook-controlled terminal step with explicit operator approval." R2's own preconditions
  require R1 to have completed for the same `<ver>` and a soak period to have passed —
  conditions no workflow can evaluate.
- **The approval is two things, not one.** The body requires both a repository
  `environment:` requiring a reviewer **and** the recorded literal
  `FEED PUBLISH GRANTED pilot <ver>`: "the environment approval and the recorded phrase are
  both required — the phrase is the audit trail, the environment is the mechanism." The
  wording is confirmed by [[REL-009]] (plan handle `DSK-09-11`) step 2;
  `runbooks.md` § Conventions says `MERGE AUTH GRANTED` keeps its single meaning.
- **Adding a tag trigger to `ci.yml` would run all nine existing jobs on every tag.** That is
  why the body puts the lane in a new workflow, `.github/workflows/desktop-release.yml`, and
  requires the reasoning to be recorded so it is not re-litigated.
- **Tag provenance must be checked, not assumed.** A git tag can point at any commit,
  including one on an unmerged branch. `git merge-base --is-ancestor <tag sha> origin/main` is
  the check, and the same command already appears in the repository's vocabulary — area 00's
  branching flow uses `git merge-base --is-ancestor <fork-main> upstream/main` for the
  upstream sync.
- **Signing is verified before publishing, and both gates abort before anything reaches the
  feed** — R1 steps 3–4, and validator check 5 hashes the package the `.appinstaller` names.
- **Publish order is package first, `.appinstaller` last, previous package retained** — R9
  steps 1–2, implemented by `eng/packaging/Publish-DesktopRelease.ps1`
  ([[REL-008]], plan handle `DSK-09-10`).
- **`--timestamp` is mandatory** — `.codex/skills/winui-packaging/SKILL.md` § Key Rules:
  "without it, signatures expire with the cert".
- **The runner strategy belongs to another ticket.** [[TEST-019]] (plan handle `DSK-08-19`),
  "CI cost and runner plan for the private-repository era (C-01)", decides the runner's
  isolation and permissions; this lane respects that decision rather than making it.

### Facts

Verified by reading this repository on 2026-08-24 unless a URL and fetch date is given.

| Fact | Source |
| --- | --- |
| One workflow, triggered by `pull_request` and `push: branches: [main]`, `permissions: contents: read`; **no tag trigger**; nine jobs, none signing or publishing | `.github/workflows/ci.yml:1-11` and the job list to `:234` |
| `actions/upload-artifact@v6` is the version in use | `.github/workflows/ci.yml:179` |
| GitHub Actions/OIDC deployment is recorded as `Not planned` | `docs/runbook.md:903` |
| The fork has no tags; the convention is `gateway/r<N>` and `desktop/v<M.m.b>`, with signing on `main` tags only and production publish staying a runbook step | `docs/desktop/00-governance-and-workflow/README.md` § 2 and `:203-211`; [[FND-009]] owns recording it |
| The `.pfx` is not a GitHub secret; the signing host is the same always-on machine that serves the share and would host the self-hosted runner | `signing-and-hosting-decision-matrix.md` § D-002 and § How the decisions interact |
| Pilot publish may be automated; production publish stays a runbook-controlled terminal step with explicit operator approval | `docs/desktop/09-release-update-and-distribution/README.md` § 3 Publication |
| R1 steps 3–6 (sign, generate+validate, approve, publish) and R2's preconditions | `runbooks.md` § R1, § R2 |
| The approval phrase is proposed and confirmed by [[REL-009]]; `MERGE AUTH GRANTED` keeps one meaning | `runbooks.md` § Conventions |
| Publish order: package first, `.appinstaller` last, previous package retained | `runbooks.md` § R9 steps 1–2 |
| `--timestamp` is critical for production | `.codex/skills/winui-packaging/SKILL.md` § Key Rules |
| The composite build action pins SDK `10.0.x` with a locked restore of `./Pegasus.slnx` | `.github/actions/dotnet-build/action.yml` |
| The parity matrix holds 46 rows, `PAR-01`…`PAR-46`, and none covers release automation | `grep -c '^\| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → `46` |

### Assumptions

- **A-09-33 — a self-hosted runner exists, or is planned, on the signing host.** Nothing in
  this repository configures one; [[TEST-019]] (plan handle `DSK-08-19`) decides the runner
  strategy.
  *Confirmed by*: a runner registered with the labels the workflow's `runs-on` names.
  *Breaks if wrong*: the lane cannot sign. The body's fallback is explicit and must be
  followed rather than improvised: **land the workflow with the job `if: false` and a comment
  saying why**, never route signing through a hosted runner — that would require the `.pfx`
  as a secret, which D-002 forbids.
- **A-09-34 — the runner's labels are agreed.** The body proposes
  `runs-on: [self-hosted, windows, pegasus-signing]`.
  *Confirmed by*: the labels recorded by [[TEST-019]] or by the operator
  who registers the runner.
  *Breaks if wrong*: the job queues forever, which looks like a hang rather than a
  misconfiguration. Mitigation: record the agreed labels in the plan and add
  `timeout-minutes` so a mislabelled job fails rather than waits.
- **A-09-35 — the version can be derived from the tag deterministically.** The body's rule is
  `desktop/v1.2.345` → `1.2.345.0`.
  *Confirmed by*: a unit-style check in the workflow step that fails when the derived value
  does not match `^1\.\d+\.\d+\.0$`.
  *Breaks if wrong*: a package version that does not match the manifest, caught later by
  validator check 4 — but by then the build has run. Fail early instead.
- **A-09-36 — a GitHub `environment:` with a required reviewer is available on this
  repository's plan.** Environment protection rules are a repository feature, not a workflow
  one.
  *Confirmed by*: creating the environment and observing the approval prompt on the first
  real run.
  *Breaks if wrong*: the mechanism half of the two-part approval is missing and only the
  recorded phrase remains. Mitigation: if the environment is unavailable, record that fact and
  keep the job `if: false` rather than publishing on the phrase alone — the body makes both
  required.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered for the responsibility this
ticket places: *signing a production package and publishing it to the pilot feed*.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **no** | One publisher account writes the feed; the release identity is written once into the run summary and the release record. |
| Unattended execution — must it run with every desktop closed? | **yes, in the trigger sense** | The lane fires on a tag push and runs without a human at the keyboard — but only **after** an environment approval, so it is semi-attended. It runs on the in-house signing host's self-hosted runner, **not** a cloud service. The "yes" names where the responsibility lands, and that is the in-house always-on host (D-003's share host); it does not mean Azure. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | The signing `.pfx` is a ~3-year private key. D-002 forbids it as a GitHub secret and confines it to the signing host, which is precisely why this lane must run on a self-hosted runner there and why no `secrets.` reference may appear in the workflow. Again the "yes" places the responsibility on the in-house signing host, per D-002. |
| Public callback — must an external service call a stable public endpoint? | **no** | A self-hosted runner makes outbound long-poll connections to GitHub; nothing external needs an inbound endpoint into the estate. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **no** | The lane's own gates (tag provenance, `signtool verify`, the `.appinstaller` validator, the environment approval) are build-time controls. The client-facing fail-closed rule is the gateway minimum-version gate, [[GWY-023]] (plan handle `DSK-04-06`). |
| Measured operational advantage — measured evidence central is materially better? | **no** | None claimed. The automation's value is removing the most repeated manual step, not performance. |

Two "yes" answers — unattended trigger and protected credentials — and **both are satisfied
by the in-house signing host**. The lane authenticates to no cloud service and creates no
Azure resource; `docs/runbook.md:903` keeps GitHub Actions Azure deployment `Not planned`.

## Implications

- **A separate workflow file is the only sane trigger boundary.** `ci.yml`'s trigger block is
  `pull_request` + `push: branches: [main]`; adding `push: tags:` there would run all nine
  existing jobs on every tag, which is both wasteful under C-01's 2× Windows multiplier and
  confusing in the checks list.
- **Guard the tag before building.** A tag on an unmerged branch must never produce a signed
  package, and the check is one command.
- **Refuse `prod` explicitly, in code.** Automating pilot publication is safe because a pilot
  release is reversible and observed; automating production is not, and R2's preconditions —
  a completed R1 for the same `<ver>` and an elapsed soak period — are conditions no workflow
  can evaluate. An explicit guard means a future edit cannot quietly widen the lane.
- **Both approval halves, or neither.** The environment is the mechanism, the phrase is the
  audit trail. If the environment is unavailable (A-09-36), the correct action is to keep the
  job disabled and record why — not to publish on the phrase alone.
- **Dry run with the dev certificate first.** [[REL-006]] (plan handle `DSK-09-06`) produced
  one, and the Test/UAT stack has a share. A dry run exercises every step except the one that
  cannot be undone.
- **Record which steps are now automated, in R1 and R2.** A runbook that still describes a
  manual step a lane now performs is a runbook that will be executed twice.

## Open questions

**None opened as a blocking document — and not because opening one would be costly.**

The earlier draft of this section said "opening one would block every stage move for a
decision another ticket owns". The first half is false and is withdrawn: an unticked `- [ ]`
line above `## Parked` blocks exactly `leave-preparing`, `enter-review` and `enter-done`, and
never `leave-backlog`. This ticket is a `feature`, so `questions-resolved` sits at three of
its four boundaries and `leave-backlog` carries only `governing-doc`.

The second half is the real reason and it stands on the authoring contract's own rule: a
decision a **named sibling ticket** owns is a scope boundary, and it belongs here naming that
ticket rather than in an `open-questions` document. Nothing in this ticket's body instructs
that a question be recorded in `open-questions/`.

- **Does the self-hosted runner exist, and with which labels?** (A-09-33, A-09-34.)
  [[TEST-019]] (plan handle `DSK-08-19`) decides the runner strategy, its isolation and its
  permissions — a named sibling ticket, so this is a scope boundary. It is **not blocking**
  in any case: the body gives an explicit, recorded fallback — land the workflow with
  `if: false` and a comment saying why — so the work can be completed, reviewed and merged
  before the runner exists. What must **never** happen as a workaround is routing signing
  through a hosted runner, which would require the `.pfx` as a GitHub secret that D-002
  forbids.
- **Is a GitHub `environment:` with a required reviewer available?** (A-09-36.) Established by
  creating it — answerable by looking, not by asking. If it is not available, the job stays
  disabled and the fact is recorded; publishing on the recorded phrase alone is not a
  fallback, because the body makes both halves of the approval required.
- **Not open, and not to be re-opened**: the signing route (D-002) and the feed (D-003). No
  hosted-runner signing route may be invented, and no GitHub-hosted feed may be reintroduced.
