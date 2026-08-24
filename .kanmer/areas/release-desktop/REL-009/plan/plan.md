# Plan — REL-009: DSK-09-11 · Pilot-ring release runbook R1 and the first pilot release

**Diff estimate: ~4 files, ~70 lines.** `runbooks.md` gains ~35 lines across four changes
(§ Conventions approval phrase, R1 step 7 corrected to the SMB check, R1 evidence list
extended with the `OPS-10` closure and the >1-hour check, R1 marked proven);
`docs/desktop/README.md` gains **one** table row for D-004; `docs/operations.md` gains
**one** desktop release row; `docs/current-architecture.md` gains at most a sentence, and
only if the deployment boundary actually changed. The ticket's real output is **evidence**
— ten operator artefacts listed in the files document — not diff.
`docs/engineering.md:201-207` § Plan sizing requires the estimate first, and an honest
estimate for an execution ticket says exactly this.

## Approach

**Execute R1 once, end to end, recording each step's evidence as it happens, and correct
the runbook while running it.** The runbook is a draft: § Conventions says the approval
phrase is *proposed* and must be confirmed before first use, and R1 step 7 still describes
an HTTP header check that D-003's SMB feed cannot answer. Correcting those while executing
is the only moment the corrections are cheap and provably right — the alternative, running
the draft as written and fixing it afterwards, produces a proof that quotes a command
nobody should run again.

The alternative rejected for the approval was **reusing `MERGE AUTH GRANTED`**: it has one
meaning (the `dev` → `main` promotion) in a culture that depends on that phrase being
unambiguous, and extending it would make every future audit of a promotion ambiguous. A
distinct phrase per channel and version costs one sentence and keeps both audits clean.

D-004 is applied as written: one approver signs **once**, and that signature accepts both
the desktop pilot and capability `OPS-10`'s outstanding operator acceptance. Seeking a
second sign-off would re-open a decision the operator took on 2026-08-24.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-009`). No existing PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by `DSK-09-01` (board `REL-001`). This run is the first
> exercise of its whole decision: 2021-schema `.appinstaller`, self-managed certificate
> (D-002), UNC feed (D-003), gateway-first order of deployment. This plan is written to the
> decisions as recorded in `docs/desktop/09-release-update-and-distribution/README.md` § 3
> and `runbooks.md` § R1; if ADR-0105 lands differently, this plan is revised before
> implementation.

Existing documents this plan **meets**:

- **ADR-0007** (`docs/adr/0007-direct-terminal-azure-deployment.md`) — deployment from an
  authorised terminal. **Meets**: R1 steps 1–2 run on the authorised release terminal from
  a clean checkout of the tagged commit; nothing is unattended.
- **ADR-0014** (`docs/adr/0014-local-to-production-deployment.md`) — local and production
  only. **Meets**: precondition 5 is a **local Test/UAT** rehearsal, and the pilot ring runs
  on the **production** gateway; no third environment is created.
- **`AGENTS.md` § Safety rails** — refresh current-state documents in the same task.
  **Meets**: steps 13's `docs/operations.md` row and `docs/desktop/README.md` D-004 row are
  written in this task, not deferred.
- **`docs/capabilities.md:73`** — capability `OPS-10`. **Meets, by producing the approval
  record it will point at**: step 12's signed text closes its outstanding operator
  acceptance under D-004. The row edit itself is `DSK-09-18`'s (board `REL-016`) and is
  **not** made here.

Binding operator decisions, written to as settled:

- **D-002** (2026-08-23) — sign with the self-managed certificate on the signing host,
  always timestamped.
- **D-003** (2026-08-23) — publish by file copy to the UNC share; there is no MIME,
  `Content-Length` or byte-range configuration to verify over SMB.
- **D-004** (2026-08-24) — `OPS-10`'s outstanding operator acceptance **folds into this
  desktop pilot approval** and does not close separately against the current web client;
  upstream `TICK-001` stays dropped and no ticket is imported for it.
- **L-02** — no Azure test environment; the Test/UAT stack rehearses install → update →
  rollback before the pilot.
- **C-01** — the feed is LAN/VPN-only by design.

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in
the plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) → `pegasus-release`
  (`.agents/skills/pegasus-release/SKILL.md`, verified present) → `winui-packaging`
  (`.codex/skills/winui-packaging/SKILL.md`, `microsoft/win-dev-skills` v0.5.0 `f1028dd5`,
  verified present).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`) — all evidence lands as ticket proof (`command-log`,
  `test-output`, `visual`); Microsoft Learn (`microsoft_docs_search`) for any App Installer
  behaviour question that arises mid-release.
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates REL-009` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's fifteen implementation steps in the same order, with the same
ownership. Steps marked **Operator step** are performed by the operator; an agent prepares
the command and the evidence template and records the result.

1. **Orient and take.** Read `runbooks.md` § Conventions and § R1 **in full**, then the
   area plan § 5 row `DSK-09-11` and § 3. `get_doc_gates REL-009`, then
   `take_ticket REL-009`.
2. **Operator step — confirm the approval phrase before first use.** The plan *proposes*
   `FEED PUBLISH GRANTED pilot <ver>` and `FEED PUBLISH GRANTED prod <ver>`; § Conventions
   says the implementing agent must confirm the wording. `MERGE AUTH GRANTED` keeps its
   single meaning (the `dev` → `main` promotion) and is **not** extended. Record the
   confirmed wording verbatim in `runbooks.md` § Conventions.
3. **Record all five preconditions as met or not, each with its evidence**, before any
   build: (1) the gateway release this package needs is live and recorded in
   `docs/operations.md` — or R8 confirmed no gateway change is needed; check the manifest's
   `minimumGatewayRelease` against the newest row of the gateway table at
   `docs/operations.md:311` (release 20, 2026-08-22, at the time of writing); (2) `main`
   carries the commit and the `desktop/v<ver>` tag exists on it — `git tag --list 'desktop/v*'`
   and `git merge-base --is-ancestor <tag sha> origin/main`; (3) CI is green for that commit
   including the desktop lanes and packaging tests — record the run URL; (4) D-002 and D-003
   are in place — signing host, certificate and `<feed>` path configured, per `DSK-09-08`
   (board `REL-007`) and `DSK-09-10` (board `REL-008`); (5) the Test/UAT rehearsal of
   install → update → rollback passed **for this package**, with the evidence linked. A
   precondition recorded as "assumed" is a precondition not met.
4. **R1 step 1 — clean checkout on the authorised release terminal.** Check out the tagged
   commit, confirm `git status` is clean, and record the 40-character SHA.
5. **R1 step 2 — build.**
   `pwsh ./scripts/Build-DesktopRelease.ps1 -Channel pilot -Version <ver> -SourceRevision <sha>`
   (locked restore, x64 Release build, `winapp package`, manifest, SBOM, hashes). Record the
   SHA-256 of the `.msix`; the script's only stdout line is the manifest path, so capture it.
6. **R1 step 3 — Operator step: sign, always with a timestamp.** On the signing host,
   `winapp package --cert` or `signtool sign /fd SHA256 /f`, with `--timestamp` or `/tr`.
   Verify with `signtool verify /pa /v <pkg>.msix` and confirm the output shows **both** the
   chain and the timestamp — reading the output, not the exit code. Hand back the
   verification output verbatim.
7. **R1 step 4 — generate and validate the `.appinstaller`.** `Version` = previous + 1
   revision, `MainPackage Version=<ver>`; then
   `pwsh ./eng/packaging/Test-AppInstaller.ps1` (schema, `Uri`, monotonic version, hash vs
   manifest). **A non-zero exit stops the release** — the validator is a gate, not a report.
8. **R1 step 5 — Operator step: obtain the approval in writing.**
   `FEED PUBLISH GRANTED pilot <ver>` in the wording confirmed at step 2. Record it verbatim
   in the ticket.
9. **R1 step 6 — publish.** `eng/packaging/Publish-DesktopRelease.ps1` (`DSK-09-10`, board
   `REL-008`): `.msix`, `.appinstaller`, `desktop-release-manifest.json` and the SBOM to
   `<feed>/pilot/`, **package first and `.appinstaller` last**, keeping the previous package
   in place. Retaining it is what makes rollback (`DSK-09-13`, board `REL-011`) possible at
   all.
10. **R1 step 7 — verify the feed from a workstation network position, by the SMB check.**
    Under D-003 this is R9 step 4, **not** the `curl -I` and ranged `GET` the runbook's
    original wording describes: from a non-publisher staff account run
    `pwsh ./eng/packaging/Test-FeedShare.ps1 -FeedRoot <feed> -Channel pilot` and confirm
    path, manifest `Version`/`Uri`, package hash and read-only ACL. **Correct R1 step 7's
    wording in `runbooks.md` in the same task** so the runbook stops describing an HTTP
    check against a share.
11. **R1 step 8 — Operator step: the pilot workstation.** Launch Pegasus; the App Installer
    prompt appears; take the update; confirm the version in Settings/Diagnostics; confirm
    the gateway accepts the version. Hand back a screenshot of the version screen and the
    `Get-AppxPackage CollisionEngineers.Pegasus` transcript.
    Then take the evidence the pilot is the **first** place that can produce it: **one
    document download and one case export taken more than an hour after the production
    gateway's current revision started**, both succeeding. That is **upstream `PLAT-039`**'s
    outstanding renewal check — its own `proof.md` records the deployed export running at
    roughly 15:00Z against a revision that started at 14:35Z, inside the first hour, so it
    proves the token renewal did not break the Box read but not that it renews. Record the
    revision start time and both request times. A failure here is a **gateway defect to
    raise separately**, not a pilot-release defect. (Id namespace: `PLAT-039` is an
    **upstream** id — there is no board `PLAT-039`; the board's `platform-operations` area
    runs `PLAT-001`…`PLAT-029`.)
12. **Operator step — close `OPS-10` with this pilot approval (D-004).** The approval record
    must say so in words: it names the gateway releases whose `OPS-10` execution it accepts
    — releases 1–3 plus any later gateway release this pilot ran against, taken from the
    table at `docs/operations.md:311-332` — as well as this desktop pilot, and **the
    approver signs once, for both**. Record the signed text verbatim in the ticket proof
    beside the `FEED PUBLISH GRANTED pilot <ver>` phrase. **Do not seek a second, separate
    `OPS-10` acceptance**, and do not treat upstream `TICK-001` as live work: both re-open a
    settled decision. The matching `docs/capabilities.md` `OPS-10` row change belongs to
    `DSK-09-18` (board `REL-016`) and is **not** made here.
13. **R1 step 9 — record the release row, in the same task.** Version, date, commit, package
    hash, signer, channel `pilot`, compatibility range — into `docs/operations.md`, per
    `AGENTS.md` § Safety rails. The `### Desktop releases` table itself is created by
    `DSK-09-18` (board `REL-016`); if it does not exist yet, coordinate rather than inventing
    a second table. In the **same task**, add the one-line **D-004** row to
    `docs/desktop/README.md` § Locked decisions and open decisions, in the shape the
    L-01…D-003 rows use (`ID | Decision | Status | Owner plan`): `OPS-10` operator acceptance
    folds into the desktop pilot approval, decided 2026-08-24, owner plan 09.
14. **Attach the full evidence set to `proof`** and state the runbook's own limits inside it:
    build log, hashes, `signtool verify` output, validator output, feed-check output, version
    screenshot, the >1-hour download and export results with all three timestamps, the signed
    pilot-and-`OPS-10` approval text, and the operations row. Then, verbatim in substance:
    R1 does **not** prove production-ring behaviour on every workstation, does **not** prove
    telemetry (App Insights 0.1 GB/day cap, PLAT-034), and proves **nothing** about the
    gateway's own release.
15. **Mark R1 proven** in `runbooks.md` with its date, and record the dated
    `## Simplification pass` in this document.

## Verification

Evidence tier from the body: **Tier 12 — Integrated workflow.** The obligation is end-to-end
evidence through the real caller: a real signed package, published to the real feed,
installed by a real pilot user, accepted by the production gateway, with the operations row
written. Registration or mock-only paths do not satisfy this tier. `proof` combines
`command-log`, `test-output` and `visual` evidence.

| Command / observation | Expected evidence |
| --- | --- |
| `signtool verify /pa /v .\Pegasus_<ver>_x64.msix` | `Successfully verified`, a chain to the self-managed certificate, **and** a timestamp line |
| `pwsh ./eng/packaging/Test-AppInstaller.ps1 -AppInstallerPath <feed>\pilot\Pegasus.appinstaller -Channel pilot -ManifestPath <manifest>` | exit `0` |
| `Get-AppxPackage CollisionEngineers.Pegasus` on the pilot workstation | `Version` equals `<ver>` |
| `pwsh ./eng/packaging/Test-FeedShare.ps1 -FeedRoot <feed> -Channel pilot`, run as a staff user | exit `0` |
| Pilot-workstation document download and case export, taken more than an hour after the recorded gateway revision start | both succeed; revision start time and both request times recorded in the proof |
| `grep -n 'D-004' docs/desktop/README.md` | exactly one row, in the § Locked decisions and open decisions table |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` after the operations row | exit `0` |

Behaviours to observe rather than infer, and to state in the proof: the App Installer prompt
**appeared** on launch (not that it should have); the previous package is still on the feed
after publication; the approval text names the gateway releases it accepts for `OPS-10`; and
`git diff --name-only` shows no change to `docs/capabilities.md`.

## Risks / open questions

- **Risk — the package layer is mistaken for enforcement.** It **fails open** when the feed
  is unreachable; only the gateway minimum-version gate closes that door, and R1 raises
  nothing. `DSK-09-12` (board `REL-010`) owns R3. Mitigation: stated in the proof's limits.
- **Risk — a pilot user is off-network when the minimum version is raised.** They would be
  locked out until they return. Mitigation: R1 raises no minimum version, and R9 step 7's
  rule is carried into `DSK-09-12`.
- **Risk — telemetry hides the evidence.** App Insights' 0.1 GB/day cap (PLAT-034) can hide
  update and blocked-client telemetry for most of the day. Mitigation: rely on the
  diagnostics bundle and feed-side evidence, not only on telemetry — and say so in the proof.
- **Risk — a second `OPS-10` sign-off is sought.** D-004 is decided: one approval closes
  both. Mitigation: step 12 states it explicitly, and the acceptance criterion names "signed
  **once** by one approver for both".
- **Risk — release documentation drift.** `docs/operations.md:295` already contradicts its
  own table and `CHANGELOG.md` stopped at 2026-08-03. Mitigation: step 13 writes the desktop
  row in the **same task**; the pre-existing gateway drift is out of scope and is raised
  separately by `DSK-09-18` (board `REL-016`).
- **Risk — R1 step 7 is run as written.** `curl -I` against a UNC share is meaningless.
  Mitigation: step 10 corrects the runbook while executing the real check.
- **Risk — a gateway change turns out to be needed.** That is the existing `pegasus-release`
  procedure with its own approval, and must **not** be folded into this ticket. Mitigation:
  precondition 1 is checked before any build.
- **Open questions**: none. D-004 settles `OPS-10`; the approval phrase is a wording
  confirmation the operator gives at step 2, not a design question; and the remaining
  unknowns (pilot users, tag existence, network reachability) are facts established by a
  command or by the operator at execution time. **No `open-questions` document is created**,
  and in particular none for `OPS-10`, which the operator has already decided.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch is
expected to be documentation-only (four documentation edits), so the expected record is
`n/a — docs-only`; confirm against the actual diff before writing it._
