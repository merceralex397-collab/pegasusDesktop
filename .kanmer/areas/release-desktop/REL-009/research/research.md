# Research — REL-009: what R1 must prove, and what one pilot approval has to close

## Question

What must be true before the first signed Pegasus package reaches a real pilot user, what
evidence does each R1 step produce, and what exactly does the pilot approval record have to
say so that capability `OPS-10` closes with it under D-004?

## Current behaviour

**The gateway has a mature release procedure; the desktop has none.**

- `.agents/skills/pegasus-release/SKILL.md` is the eleven-step gateway route, executed
  against the live estate: preflight → exact-SHA atomic fast-forward of `dev` to `main`
  requiring the literal `MERGE AUTH GRANTED` → `scripts/Build-ReleaseArtifacts.ps1` →
  `oras cp` of the OCI image to ACR → azd inputs → `azd provision --no-prompt` → Worker by
  `config-zip` → `efbundle` migrations → `scripts/Invoke-ProductionSmoke.ps1` →
  behavioural verification → refresh `docs/current-architecture.md` and
  `docs/operations.md` **in the same task**. Its § The estate states the approval culture
  this ticket inherits: "Read-only Azure checks need no approval. **Every write needs
  explicit operator approval for the exact target**, and the `main` update additionally
  needs the words `MERGE AUTH GRANTED` immediately before the push."
- `docs/operations.md:280-332` § Production environment holds the authoritative gateway
  release table. Its newest row is **release 20, 2026-08-22**, source revision `05fe7a7f…`,
  image digest `sha256:90b58000…`, revision `pegasus-prod-web-252ow37gij--05fe7a7f2d86`,
  migration `20260822044425_GrantWorkerCaseDocuments`. The columns are `Release | Date |
  Source revision | Image digest | Web revision | Migration`, and abbreviated hashes are the
  house style.
- **The narrative contradicts the table.** `docs/operations.md:295` still reads "the estate
  currently serves **release 14**" while the table's newest row is 20. `CHANGELOG.md`
  stopped at 2026-08-03. Neither is current, and the desktop table must not repeat the
  pattern.
- `docs/capabilities.md:73` is the `OPS-10` row: "Production environment deployed directly
  from an authorised terminal | Now | 0.1.0-alpha.1 | [ADR-0014](adr/0014-local-to-production-deployment.md)
  | Executed for releases 1–3 ([operations — production environment](operations.md#production-environment)
  owns the evidence); **operator acceptance outstanding.**" The final clause is what D-004
  replaces — and the edit belongs to `DSK-09-18` (board `REL-016`), not here.

**No parity-matrix row covers this.** Releasing a desktop package is not an observable web
capability; `PAR-01`…`PAR-46` are Razor page models. R1 is new operational responsibility
under proposal § 24 Phase 9.

## Findings

- **R1 has five preconditions and nine steps**, and the preconditions are the part most
  likely to be skipped: (1) the gateway release the package needs is live and recorded in
  `docs/operations.md` — or R8 confirmed no gateway change; (2) `main` carries the commit
  and the `desktop/v<ver>` tag exists on it; (3) CI is green for that commit including the
  desktop lanes and packaging tests; (4) D-002 and D-003 are in place — signing host,
  certificate and feed configured; (5) the **Test/UAT rehearsal of install → update →
  rollback passed for this package**, evidence linked. — `runbooks.md` § R1.
- **The approval phrase is proposed, not confirmed.** `runbooks.md` § Conventions: "this
  plan **proposes** the literal phrase `FEED PUBLISH GRANTED prod <ver>`; for pilot,
  `FEED PUBLISH GRANTED pilot <ver>` … The implementing agent must confirm the phrase with
  the operator before first use." And: "The existing literal phrase `MERGE AUTH GRANTED`
  keeps its single meaning (the `dev` → `main` promotion)" — it must not be extended to
  publishing.
- **R1 step 7's wording is stale.** It describes verifying the feed with `curl -I` and a
  ranged `GET` — an HTTP check. Under D-003 the feed is SMB and those requirements do not
  exist; the real check is R9 step 4 (`Test-Path`, `Select-Xml`, `Get-FileHash`, `Get-Acl`)
  via `eng/packaging/Test-FeedShare.ps1`. The body requires R1 step 7 to be corrected in the
  same task.
- **D-004 (2026-08-24) folds `OPS-10` into this approval.** From the operator decisions:
  the outstanding acceptance of `OPS-10` "**folds into the desktop pilot approval**. It does
  **not** close separately against the current web client." Upstream `TICK-001` stays
  **dropped** and no ticket is imported for it. `REL-009` gains an acceptance criterion:
  the approval record explicitly closes `OPS-10`'s outstanding acceptance, names the
  releases it covers, and **the approver signs once for both**.
- **D-004 also gives this ticket a one-line documentation change**: record D-004 in
  `docs/desktop/README.md` § Locked decisions as a row in the shape the L-01…D-003 rows
  already use. That file's table columns are `| ID | Decision | Status | Owner plan |`.
- **The `docs/capabilities.md` half of D-004 is `DSK-09-18`'s** (board `REL-016`), not this
  ticket's. This ticket must not edit `docs/capabilities.md`.
- **Upstream `PLAT-039`'s renewal check is genuinely outstanding**, and the pilot is the
  first place it can be settled. The body records why: that ticket's own `proof.md` shows
  the deployed export running at roughly 15:00Z against a revision that started at 14:35Z —
  inside the first hour — so it proves the token renewal did not break the Box read, but not
  that it renews. The pilot check is one document download and one case export taken **more
  than an hour after** the production gateway revision started, with the revision start time
  and both request times recorded. Note the id namespace: `PLAT-039` here is an **upstream**
  id and is written `upstream PLAT-039`; there is no board `PLAT-039` (the board's
  `platform-operations` area runs `PLAT-001`…`PLAT-029`), and board `PLAT-028`/`PLAT-029`
  map to upstream `PLAT-032`/`PLAT-038` per `HZN-001` / `board-conventions.md`.
- **The package check fails open; the gateway gate fails closed.** R1 raises no minimum
  version — that is R3, owned by `DSK-09-12` (board `REL-010`).
- **App Insights' 0.1 GB/day cap (PLAT-034) can hide update and blocked-client telemetry
  for most of the day** — area plan § 7. Rely on the diagnostics bundle and feed-side
  evidence, not only on telemetry.
- **`AGENTS.md` § Safety rails** requires current-state documents to be refreshed in the
  **same task** as the release. That is why R1 step 9 and the `docs/desktop/README.md`
  D-004 row are inside this ticket rather than deferred.

### Facts

Verified by reading this repository on 2026-08-24.

| Fact | Source |
| --- | --- |
| The gateway release route, its eleven steps and the `MERGE AUTH GRANTED` rule | `.agents/skills/pegasus-release/SKILL.md` |
| The gateway release table's columns, house style and newest row (20, 2026-08-22) | `docs/operations.md:311-332` |
| The narrative at `docs/operations.md:295` says "release 14" against a table whose newest row is 20 | `docs/operations.md:295` |
| The `OPS-10` row's exact current text, ending "operator acceptance outstanding." | `docs/capabilities.md:73` |
| R1's five preconditions, nine steps, evidence list, rollback and "does not prove" | `docs/desktop/09-release-update-and-distribution/runbooks.md` § R1 |
| The approval phrase is proposed and must be confirmed before first use; `MERGE AUTH GRANTED` keeps one meaning | same file, § Conventions |
| R1 step 7 still describes `curl -I` and a ranged `GET`, which do not apply over SMB | same file, § R1 step 7; `appinstaller-template.md` § Hosting requirements |
| R9's publish order and non-publisher verification | same file, § R9 steps 1–4 |
| `docs/desktop/README.md` § Locked decisions table columns are `ID | Decision | Status | Owner plan` | `docs/desktop/README.md` |
| Read-only Azure reads are permitted with no per-target approval; every write needs exact-target approval | `docs/runbook.md:776-781` |
| There is no board `PLAT-039`; the board's `platform-operations` area runs `PLAT-001`…`PLAT-029` | `ls .worktrees/kanmer/.kanmer/areas/platform-operations` |
| The upstream-to-board join table, and the rule that a bare `<PREFIX>-<nnn>` is a fork board id | group document `HZN-001` / `board-conventions.md` |

### Assumptions

- **A-09-21 — the pilot ring is one or two internal users, already identified.** Area plan
  § 3: "Pilot ring is one or two internal users on the production gateway".
  *Confirmed by*: the operator naming them before step 11.
  *Breaks if wrong*: there is no one to update, and R1 step 8 cannot produce evidence.
  Not an agent's decision.
- **A-09-22 — no gateway change is needed for this package.** R1 precondition 1 allows
  either "the gateway release the package needs is live and recorded" or "R8 confirmed no
  gateway change".
  *Confirmed by*: reading the desktop release manifest's `minimumGatewayRelease` against the
  newest row of the gateway table at `docs/operations.md:311`.
  *Breaks if wrong*: a gateway release is needed first, and that is the existing
  `pegasus-release` procedure with its own approval — **not** something to fold into this
  ticket.
- **A-09-23 — the `desktop/v<ver>` tag convention is settled.**
  `docs/desktop/00-governance-and-workflow/README.md` § Recommended branching flow item 4
  names tags `gateway/r<N>` and `desktop/v<M.m.b>`; the fork has **no tags** today
  (`git tag` was empty on 2026-08-23, per area 00 § 2).
  *Confirmed by*: `git tag --list 'desktop/v*'` on the release terminal.
  *Breaks if wrong*: precondition 2 cannot be met. `DSK-00-09` (board `FND-009`) owns the
  tag convention.
- **A-09-24 — the pilot workstation can reach both the feed and the production gateway.**
  D-003's accepted trade-off is that update checks work on the office network or VPN only.
  *Confirmed by*: `Test-Path \\<host>\<share>\pilot` and a successful sign-in from the
  workstation.
  *Breaks if wrong*: the launch check fails open and the user runs the old version — which
  is the designed behaviour, not a defect, but it means the pilot proves nothing that day.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3. This ticket **executes** placements
already decided rather than making new ones, so each answer names where the decision lives.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **no** | A release is produced once and published once. The shared record is the `docs/operations.md` desktop release row — a documentation write, not runtime state. |
| Unattended execution — must it run with every desktop closed? | **no** | R1 is an attended runbook on the authorised release terminal (ADR-0007), with an operator approval between generation and publication. |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes, at step 6 only** | The signing `.pfx`, which stays on the in-house signing host under a restricted ACL (D-002, executed by `DSK-09-08`, board `REL-007`). No credential reaches a workstation; only the public `.cer` does. |
| Public callback — must an external service call a stable public endpoint? | **no** | Nothing calls in. The pilot workstation polls the share over SMB and calls the gateway outbound. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes — but not by this ticket** | The fail-closed layer is the gateway minimum-version gate, `DSK-04-06` (board `GWY-023`), raised by R3 in `DSK-09-12` (board `REL-010`). **R1 raises nothing**, and the package layer it exercises fails open. |
| Measured operational advantage — measured evidence central is materially better? | **no** | None claimed. The pilot exists precisely because L-02 forbids an Azure test environment, so the production gateway with one or two users is the only place real-Azure behaviour can be observed. |

Two "yes" answers, both already placed: the key on the in-house signing host (D-002), and
enforcement in the gateway (ADR-0103/ADR-0105, owned elsewhere). **This ticket makes no new
placement and requires no Azure write.**

## Implications

- **Preconditions are evidence, not a formality.** Step 3 records each of the five as met or
  not with its evidence; a pilot run whose Test/UAT rehearsal never happened is not an R1
  execution.
- **Confirm the approval phrase before using it.** § Conventions says it is proposed. Get it
  in writing, record it in § Conventions, and do not reuse `MERGE AUTH GRANTED`.
- **Correct R1 step 7 while running it.** Leaving `curl -I` in a runbook for an SMB feed
  guarantees that the next operator either runs a meaningless command or invents their own
  check.
- **One signature, two acceptances.** D-004 means the approval text must *say* it accepts
  both the desktop pilot and `OPS-10`, and must name the gateway releases it covers —
  releases 1–3 plus any later gateway release this pilot ran against, taken from the table
  at `docs/operations.md:311-332`. Asking for a second `OPS-10` sign-off re-opens a settled
  decision.
- **Take the upstream `PLAT-039` evidence while a real user is on a real workstation.** The
  >1-hour download and export is cheap during the pilot and expensive to arrange later, and
  a failure there is a **gateway** defect to raise separately, not a pilot-release defect.
- **Refresh the documents in the same task.** `AGENTS.md` § Safety rails, and the standing
  example of what happens otherwise is `docs/operations.md:295`.
- **Do not touch `docs/capabilities.md`.** The `OPS-10` row edit is `DSK-09-18`'s (board
  `REL-016`).

## Open questions

- **None.** The two things that look like questions are settled: D-004 decides that one
  approval closes both the desktop pilot and `OPS-10` and that upstream `TICK-001` stays
  dropped; and the approval phrase is a wording confirmation the operator gives at step 2,
  not an open design question. A-09-21 through A-09-24 are all confirmed by a command or an
  operator naming a fact at execution time. **No `open-questions` document is created** —
  and in particular none is created for `OPS-10`, which the operator has already decided.
