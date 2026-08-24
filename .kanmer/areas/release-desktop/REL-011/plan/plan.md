# Plan — REL-011: DSK-09-13 · Rollback runbook R4 and its downgrade test

**Diff estimate: ~3 files, ~100 lines.** `runbooks.md` § R4 gains ~30 lines (step 2 promoted
to the first operational action, the quoted `ForceUpdateFromAnyVersion` sentence, the proven
uninstall/reinstall fallback, the proven marker and the verbatim "does not prove");
`eng/packaging/Test-Package.ps1` gains one downgrade scenario of ~60 lines;
`docs/operations.md` gains one rollback row of ~2 lines.
`docs/engineering.md:201-207` § Plan sizing requires the estimate first.

## Approach

**Rehearse the two counter-intuitive rules until they are boring, then write them into the
runbook as concrete values rather than as principles.** Rollback is the one procedure that
is only ever run under pressure, and it inverts twice: the `.appinstaller` `Version` goes
**up** while the `MainPackage Version` goes **down**, and the gateway minimum client version
must be lowered **before** the rollback is published rather than after. Either one missed
leaves the estate worse off than the defect — a silent no-op in the first case, every
workstation blocked in the second. So the runbook states them as two explicit values in one
sentence, and the rehearsal runs twice: on the Test/UAT stack, then on the pilot ring.

The alternative rejected was **publishing a new package with a decremented version number**.
It is superficially simpler, but it burns a version, produces an artefact nobody has ever
installed, and — since it would be built from the previous source — introduces a new hash
and a new signature the estate has no evidence for. Republishing the **same signed bytes**
that are already on the feed is both faster and more provable, and R9 step 2's
never-overwrite rule exists to guarantee those bytes are still there.

The validator's `-Rollback` switch is treated as the audit trail rather than a convenience:
the same command that refuses an accidental downgrade accepts a deliberate one, and the
difference is one switch a human typed. Step 5 therefore runs the validator **both** ways and
records the refusal.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-011`). No existing PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by `DSK-09-01` (board `REL-001`). Its Decision clause (a)
> includes `ForceUpdateFromAnyVersion`, which exists **only** for this procedure, and its
> Consequences record the retained known-good package. This plan is written to the decisions
> as recorded in `docs/desktop/09-release-update-and-distribution/README.md` § 3 ("Known-good
> previous package") and `appinstaller-template.md` § Known behaviours; if ADR-0105 lands
> differently, this plan is revised before implementation.

Existing documents this plan **meets**:

- **`docs/desktop/09-release-update-and-distribution/appinstaller-template.md` § Validator
  outline check 8.** **Meets**: step 5 makes `-Rollback` the only path by which a lower
  `MainPackage/@Version` passes, and the second verification command captures the refusal
  without it.
- **`AGENTS.md` § Safety rails** — refresh current-state documents in the same task.
  **Meets**: step 9's rollback row in `docs/operations.md` is written in this task.

Binding operator decisions:

- **D-003** (2026-08-23) — rollback is a **file copy to the UNC share**; the previous `.msix`
  is already there and is never overwritten. Off-network clients will not see the rollback
  until they return to the LAN or VPN, which is expected and must be stated.
- **L-02** — rehearse on the Test/UAT stack, then rehearse on the pilot ring; there is no
  third environment.

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in the
plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) → `winui-packaging`
  (`.codex/skills/winui-packaging/SKILL.md`, `microsoft/win-dev-skills` v0.5.0 `f1028dd5`,
  verified present).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`,
  `microsoft_docs_fetch`) for `ForceUpdateFromAnyVersion` semantics.
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates REL-011` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's twelve implementation steps in the same order, with the same
ownership. Steps marked **Operator step** are performed by the operator on the feed or a
pilot workstation.

1. **Orient and take.** Read `runbooks.md` § R4 in full plus `appinstaller-template.md`
   § Known behaviours, and the area plan § 5 row `DSK-09-13`. `get_doc_gates REL-011`, then
   `take_ticket REL-011`.
2. **Confirm the downgrade rule from official documentation and quote it.**
   `microsoft_docs_search` for `App Installer ForceUpdateFromAnyVersion downgrade`, then
   `microsoft_docs_fetch`
   <https://learn.microsoft.com/windows/msix/app-installer/update-settings>. Quote the
   sentence into R4 with its URL and fetch date, so the rule is evidenced rather than
   asserted by this plan set alone.
3. **Write R4 step 1 against the real approval.** Decide the scope (`pilot` or `prod`) and
   obtain `FEED PUBLISH GRANTED <channel> <prev-ver>` in the wording `DSK-09-11` (board
   `REL-009`) step 2 confirmed with the operator. Do not reuse `MERGE AUTH GRANTED`.
4. **Write R4 step 2 as the first operational action, ahead of any publish.** Read the
   **current** minimum client version from the admin setting rather than remembering whether
   it was raised; if it was raised to the defective version, lower it first (the R3 rollback
   path, `DSK-09-12`, board `REL-010`) so downgraded clients are accepted. A rollback
   published while the gateway still rejects the older version leaves every workstation
   blocked — a worse state than the defect.
5. **Write R4 step 3 with concrete values, not principles.** Publish the previous signed
   `.msix` — already on the feed, **never rebuilt**, same hash — under a **new**
   `.appinstaller` `Version` **higher** than the defective one, with
   `ForceUpdateFromAnyVersion="true"` and `MainPackage Version=<prev-ver>`. Write both numbers
   into the runbook sentence (for example: manifest `Version` `1.0.0.4` → `1.0.0.5` while
   `MainPackage Version` `1.2.346.0` → `1.2.345.0`), because under pressure a general
   principle is read as "decrement everything". Validate with
   `pwsh ./eng/packaging/Test-AppInstaller.ps1 … -Rollback` before publishing, and **also run
   it without `-Rollback` and record the refusal** — that negative is the evidence the guard
   exists.
6. **Rehearse the whole sequence on the Test/UAT stack (L-02)** using `Publish-Feed`
   (`test-uat-stack.md:84`): install `<ver>`, publish the rollback manifest for `<prev-ver>`,
   relaunch, and confirm App Installer applies the downgrade. Record `Get-AppxPackage
   CollisionEngineers.Pegasus` **before and after** — the two version strings are the
   evidence, not a description of them.
7. **Operator step — rehearse on a pilot workstation.** Launch after the rollback publish,
   confirm App Installer applies the downgrade, and confirm the client works against the
   gateway at the lowered minimum version (sign in and complete a routine workflow). Hand
   back the transcript and a screenshot of the version screen.
8. **Write R4 step 5 as the machine-level fallback, and prove it once.** If App Installer
   cannot downgrade on a particular machine, run R7's uninstall/reinstall steps for that
   machine: `Get-AppxPackage CollisionEngineers.Pegasus | Remove-AppxPackage`, then install
   from the channel `.appinstaller`. Prove that path once on a test machine so it is not
   first attempted during an incident, and note R7's fact that local preferences live in the
   package's `ApplicationData` and are removed with the package.
9. **Write R4 step 6 and record the row.** Channel, `.appinstaller` version, package version,
   who approved, when — into the desktop release table in `docs/operations.md` in the same
   task. Open a `fix` ticket for the defect with the diagnostics bundle from R10 attached.
10. **State R4's limits verbatim in substance in its "does not prove" section**: it does not
    prove that data written by the defective version is correct — check audit/history for the
    window (area 10). Add the off-network consequence from R9 step 7: clients away from the
    LAN or VPN will not see the rollback until they return.
11. **Add the rollback scenario to the packaging suite** — one scenario in
    `eng/packaging/Test-Package.ps1` (`DSK-08-10`, board `TEST-010`), extending that script
    rather than creating a second one, so a regression in the downgrade path is caught in CI
    rather than during an incident.
12. **Mark R4 proven** in `runbooks.md` with its date, and record the dated
    `## Simplification pass` in this document.

## Verification

Evidence tier from the body: **Tier 7** — the workstation evidence tier the plan row assigns.
Proof is an **observed downgrade on a real machine**, on the stack and again on the pilot
ring, with before/after package versions — not a validator exit code alone. `proof` combines
`command-log`, `test-output` and `visual`.

| Command / observation | Expected evidence |
| --- | --- |
| `pwsh ./eng/packaging/Test-AppInstaller.ps1 -AppInstallerPath <rollback manifest> -Channel pilot -ManifestPath <prev manifest> -Rollback` | exit `0` |
| the same command **without** `-Rollback` | non-zero exit and the named downgrade failure — the guard proven, not assumed |
| Test/UAT stack: `Get-AppxPackage CollisionEngineers.Pegasus` before and after relaunch | version moves from `<ver>` to `<prev-ver>`; both strings captured |
| Pilot workstation after rollback | the client logs in and completes a routine workflow against the gateway at the lowered minimum |
| `Get-FileHash <feed>\<channel>\Pegasus_<prev-ver>_x64.msix` | identical to the `packageSha256` recorded in that release's `desktop-release-manifest.json` — proving nothing was rebuilt or overwritten |
| `Get-ChildItem <feed>\<channel>` before the rehearsal | at least two `Pegasus_*_x64.msix` files present; if only one, R9's never-overwrite rule has been broken and that is a defect to raise |

Behaviours to observe rather than infer: that the downgrade happened **on relaunch** without
manual intervention (or, if it did not, that the R4 step 5 fallback was what moved the
machine — record which); and that the minimum version was lowered **before** the publish, with
the ordering visible in the timestamps.

## Risks / open questions

- **Risk — the `.appinstaller` `Version` is decremented along with the package version.** App
  Installer only reacts to a higher manifest version, so the rollback silently does nothing.
  Mitigation: step 5 writes both concrete numbers into the runbook, and validator check 3
  (strictly increasing manifest version) fails the file before it is published.
- **Risk — `ForceUpdateFromAnyVersion` is missing.** App Installer then moves only to higher
  package versions and the rollback silently does nothing. Mitigation: it is in the template
  by default, validator check 6 requires it, and check 8 requires it for the `-Rollback` path.
- **Risk — the rollback is published while the gateway still rejects the older version.**
  Every workstation is then blocked. Mitigation: step 4 makes lowering the minimum the first
  operational action and requires reading the **current** value rather than remembering it.
- **Risk — the previous `.msix` is not on the feed.** Mitigation: the sixth verification
  command checks the listing as a precondition; a single `.msix` means R9's never-overwrite
  rule was broken and is a defect against `DSK-09-10` (board `REL-008`).
- **Risk — the previous package's hash no longer matches its manifest.** Mitigation: the
  fifth verification command; a mismatch means the feed holds an artefact nobody can attest
  to, and the rollback must stop.
- **Risk — App Installer will not downgrade on a particular machine.** Mitigation: step 8
  proves the uninstall/reinstall fallback once, in advance.
- **Risk — off-network clients do not see the rollback.** Expected under D-003's LAN-only
  feed. Mitigation: stated in R4 step 10 rather than discovered.
- **Open questions**: none. The approval phrase is confirmed by `DSK-09-11` (board `REL-009`);
  the feed contents and hashes are preconditions checked by command; and whether the minimum
  was raised is read from the admin setting. **No `open-questions` document is created.**

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch extends a
test script as well as documentation, so `n/a — docs-only` does not apply._
