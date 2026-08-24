# Research — REL-011: how a downgrade actually happens, and the two rules that make it fail

## Question

What sequence returns a channel to its previous known-good package, and which of its steps
are counter-intuitive enough that they will be got wrong under pressure if they are not
proven first?

## Current behaviour

**There is no rollback because there is nothing to roll back.** The web application's
"rollback" is a gateway redeploy of a previous image, covered by
`.agents/skills/pegasus-release/SKILL.md` and `docs/runbook.md`; nothing in the repository
distributes or downgrades a client. Verified on 2026-08-24: `ls eng` returns nothing,
`.github/workflows/ci.yml` has no packaging lane, and `scripts/` holds no publish step.

**No parity-matrix row covers this.** `docs/desktop/01-inventory-and-parity/parity-matrix.md`
runs `PAR-01`…`PAR-46` over Razor page models; downgrading an installed client is not an
observable web capability. Rollback is new operational responsibility under proposal § 9.3
and § 24 Phase 9.

## Findings

- **Two rules invert against intuition, and both are load-bearing.**
  1. The `.appinstaller` `Version` must go **up** while the `MainPackage Version` goes
     **down**. App Installer only reacts to a higher manifest version, so a rollback
     published with a lower manifest version is silently ignored —
     `appinstaller-template.md` § Template rules ("`Version` of the `.appinstaller` must
     increase on every publish, **including a rollback publish**") and `runbooks.md` § R4
     step 3.
  2. The gateway minimum client version must be lowered **first**, before the rollback is
     published — R4 step 2. "A rollback published while the gateway still rejects the older
     version leaves every workstation blocked", because the client would downgrade
     successfully and then be refused by the gate.
- **Downgrade needs `ForceUpdateFromAnyVersion`.** Without it App Installer moves only to
  higher package versions — `appinstaller-template.md` § Known behaviours. The template
  already carries `<ForceUpdateFromAnyVersion>true</ForceUpdateFromAnyVersion>` with the
  comment "Allow the feed to move a workstation to a LOWER version (rollback)".
- **The validator has a mode for exactly this.**
  `appinstaller-template.md` § Validator outline check 8: "(Rollback mode)
  `MainPackage/@Version` lower than the previous is allowed only when the invocation passes
  `-Rollback` and `ForceUpdateFromAnyVersion` is `true`." So the same command that would
  reject an accidental downgrade accepts a deliberate one, and the difference is one switch
  a human typed.
- **Nothing is rebuilt or re-signed.** R4 step 3 publishes "the previous signed `.msix`
  (already on the feed)". The area plan § 3 records why it is there: "**Known-good previous
  package** is retained on the feed for every channel and rollback republishes it with a
  higher `.appinstaller` `Version` and `ForceUpdateFromAnyVersion="true"`." R9 step 2's
  never-overwrite rule is what guarantees it still exists.
- **There is a machine-level fallback, and it must be proven before it is needed.** R4 step
  5: "If App Installer cannot downgrade on a particular machine, run R7's uninstall/reinstall
  steps for that machine" — `Get-AppxPackage CollisionEngineers.Pegasus | Remove-AppxPackage`,
  then install from the channel `.appinstaller`. R7 also records that "local preferences live
  in the package's `ApplicationData` and are removed with the package".
- **The approval phrase applies to a rollback too** — R4 step 1,
  `FEED PUBLISH GRANTED <channel> <prev-ver>`, in the wording `DSK-09-11` (board `REL-009`)
  confirms with the operator. `MERGE AUTH GRANTED` keeps its single meaning.
- **The stack can rehearse it.** `docs/desktop/08-testing/test-uat-stack.md:84`:
  `Publish-Feed` "Copies a freshly packaged `.msix` and the `.appinstaller` for the
  `teststack` channel into the feed folder, bumping the version; used by the packaging tests
  to simulate mandatory updates **and rollbacks**".
- **Off-network clients will not see the rollback** until they return to the LAN or VPN —
  R9 step 7, a direct consequence of D-003's LAN-only feed.
- **R4 proves nothing about data.** Its "does not prove" is specific: "data written by the
  defective version is correct — check audit/history for the window (area 10)."

### Facts

Verified by reading this repository on 2026-08-24.

| Fact | Source |
| --- | --- |
| The `.appinstaller` `Version` must increase on every publish, including a rollback publish | `docs/desktop/09-release-update-and-distribution/appinstaller-template.md` § Template rules |
| Downgrade requires `ForceUpdateFromAnyVersion`; the template already sets it `true` with an explanatory comment | same file, § Template and § Known behaviours |
| Validator check 8 — a lower `MainPackage/@Version` passes only with `-Rollback` **and** `ForceUpdateFromAnyVersion` true | same file, § Validator outline |
| R4's six steps, including lower-the-minimum-first at step 2 and the uninstall/reinstall fallback at step 5, and its "does not prove" | `docs/desktop/09-release-update-and-distribution/runbooks.md` § R4 |
| The known-good previous package is retained per channel; rollback republishes it | `docs/desktop/09-release-update-and-distribution/README.md` § 3 |
| Never overwrite a published `.msix`; only `Pegasus.appinstaller` is replaced in place | `runbooks.md` § R9 step 2 |
| The approval phrase `FEED PUBLISH GRANTED <channel> <ver>` is proposed and must be confirmed before first use | `runbooks.md` § Conventions |
| `Publish-Feed` bumps the version and is used by the packaging tests to simulate rollbacks | `docs/desktop/08-testing/test-uat-stack.md:84` |
| R7's uninstall/reinstall path and the fact that local preferences are removed with the package | `runbooks.md` § R7 |
| Off-network clients do not see the rollback until they return | `runbooks.md` § R9 step 7 |
| The gateway gate's problem type and header, needed to confirm a downgraded client is accepted | `docs/desktop/04-auth-session-update-and-startup/README.md:175-179`, `:210` |

### Assumptions

- **A-09-29 — a previous package exists on the feed when the rollback is exercised.** R4 has
  nothing to publish otherwise.
  *Confirmed by*: `Get-ChildItem <feed>\<channel>` listing at least two `Pegasus_*_x64.msix`
  files before the rehearsal.
  *Breaks if wrong*: the only recovery is the uninstall/reinstall path of R4 step 5, on every
  machine. Mitigation: check the listing as a precondition rather than discovering it mid-run.
- **A-09-30 — the previous package's hash still matches its own release manifest.** R9 step
  2 forbids overwriting, so it should.
  *Confirmed by*: `Get-FileHash <feed>\<channel>\Pegasus_<prev-ver>_x64.msix` against the
  `packageSha256` recorded in that release's `desktop-release-manifest.json`.
  *Breaks if wrong*: the feed holds a package nobody can attest to, and the rollback ships an
  unverified artefact. This is the fifth verification command for exactly that reason.
- **A-09-31 — App Installer applies the downgrade on relaunch rather than needing a manual
  action.** The `.appinstaller` carries `OnLaunch HoursBetweenUpdateChecks="0"
  ShowPrompt="true" UpdateBlocksActivation="true"` plus `ForceUpdateFromAnyVersion`.
  *Confirmed by*: `Get-AppxPackage CollisionEngineers.Pegasus` before and after relaunch on
  the stack.
  *Breaks if wrong*: the machine-level fallback (R4 step 5) becomes the primary path, which
  is why step 8 proves that path once regardless.
- **A-09-32 — the minimum client version had actually been raised.** R4 step 2 is conditional
  ("if the minimum client version was raised to the defective version").
  *Confirmed by*: reading the current minimum from the admin setting before deciding.
  *Breaks if wrong*: an unnecessary lowering, which is harmless, or — the dangerous direction
  — skipping the check when it *was* raised, which blocks every downgraded client.
  Mitigation: make step 4 read the current value rather than remembering it.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered for the responsibility this
ticket exercises: *deciding and executing a return to the previous known-good package*.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **no** | The `.appinstaller` is written by one publisher account and read by every workstation; the rollback republishes an artefact that already exists. R9 step 3's ACL is exactly this shape. |
| Unattended execution — must it run with every desktop closed? | **no** | The decision, the approval and the publish are attended operator actions (R4 steps 1–3). The **feed's** availability is a separate requirement, already placed on the always-on in-house host by D-003 and owned by `DSK-09-10` (board `REL-008`). |
| Protected credentials — long-lived secret that must not sit on workstations? | **no** | Rollback republishes the **already-signed** package; no signing occurs, so the `.pfx` is not touched at all. |
| Public callback — must an external service call a stable public endpoint? | **no** | Clients poll the share over SMB; nothing calls in. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **yes, and not here** | Lowering the minimum client version is central enforcement, and it is the gateway's — `DSK-04-06` (board `GWY-023`), exercised by R3 in `DSK-09-12` (board `REL-010`). R4 **depends** on it at step 2 but does not own it. |
| Measured operational advantage — measured evidence central is materially better? | **no** | None claimed. |

One "yes", already placed in the gateway by ADR-0103/ADR-0105 and owned by another ticket.
**This ticket makes no new placement and requires no Azure write.**

## Implications

- **Order is the whole procedure.** Lower the minimum first, then publish. Getting it
  backwards leaves every workstation blocked — a worse state than the defect being rolled
  back from.
- **The `.appinstaller` version goes up while the package version goes down.** Write it into
  the runbook as two explicit values in one sentence, not as a general principle, because
  under pressure a general principle gets read as "decrement everything".
- **`-Rollback` is the only intentional path.** Validator check 8 refuses the downgrade
  without it, so the switch is the audit trail that someone meant it. Run the validator
  **without** `-Rollback` too, and record the refusal — that negative is what proves the
  guard exists.
- **Nothing is rebuilt.** The rollback ships the same signed bytes, which is why the hash
  check against the original manifest is a real assertion and not a formality (A-09-30).
- **Prove the machine-level fallback once, in advance.** R4 step 5 is the path used when App
  Installer will not downgrade on a particular machine; discovering it for the first time
  during an incident is the failure this ticket exists to prevent.
- **Rehearse twice: stack, then pilot.** L-02 gives no third environment, and the pilot is
  production. The stack's `Publish-Feed` verb exists for this.
- **Say what it does not prove.** Data written by the defective version is not made correct
  by rolling back; that is an audit/history question for area 10.

## Open questions

- **None that block.** The approval phrase is confirmed by `DSK-09-11` (board `REL-009`)
  step 2; the previous package's presence and hash are preconditions checked by command; and
  whether the minimum was raised is read from the admin setting rather than remembered. The
  documentation confirmation of `ForceUpdateFromAnyVersion` semantics (step 2 of the plan) is
  a fetch, not a question. **No `open-questions` document is created.**
