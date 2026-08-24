# Plan — REL-012: DSK-09-14 · Certificate renewal runbook R5 and emergency block runbook R6

**Diff estimate: ~2 files, ~120 lines.**
`docs/desktop/09-release-update-and-distribution/runbooks.md` gains ~100 lines across R5
(seven steps written for the single decided route, the compromise variant, the rehearsal
record and the "does not prove") and R6 (four steps, the measured elapsed time and its
limit); `docs/operations.md` gains ~5 lines — the certificate expiry calendar entry with a
90-day warning beside the thumbprint, subject and validity window. The two rehearsals are
**operator work on the Test/UAT machines and stack** and produce evidence, not diff.
`docs/engineering.md:201-207` § Plan sizing requires the estimate first.

## Approach

**Write one route, and rehearse it before it is needed rather than remembering it.** D-002
chose the self-managed certificate, so R5 has exactly one procedure — there is no route
comparison to write, and the option tables in
`signing-and-hosting-decision-matrix.md` are explicitly "history, not a menu". With a
~3-year validity, renewal fires roughly once per parliament, which is precisely why the
first execution must not be the one under time pressure: step 8 runs it end to end on the
Test/UAT machines with a throwaway replacement certificate, including the negative check
that the retired certificate no longer installs.

The alternative rejected for R5 was **writing the procedure and scheduling the rehearsal for
later**. It fails on the ticket's own terms: a runbook whose first execution is an emergency
is a document, not a procedure, and the estate carries the renewal burden precisely because
the paid routes that would have absorbed it were withdrawn.

For R6 the approach is to **measure, then claim**. R6 is the one procedure measured in
minutes, and step 11 records the elapsed time from decision to a defective client being
refused. A target that was never measured is worse than no number, because it will be
believed during an incident.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-012`). No existing PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by `DSK-09-01` (board `REL-001`). Its Consequences record
> D-002's accepted trade-offs, one of which is exactly this ticket: "a per-machine trust
> rollout, and a rehearsed renewal". R6 exercises its Decision clause (b) — the gateway gate
> is the fail-closed layer — from the emergency direction. This plan is written to the
> decisions as recorded in
> `docs/desktop/09-release-update-and-distribution/signing-and-hosting-decision-matrix.md`
> § D-002 "Chosen shape" and `runbooks.md` § R5 and § R6; if ADR-0105 lands differently,
> this plan is revised before implementation.

Existing documents this plan **meets**:

- **`AGENTS.md` § Safety rails** — refresh current-state documents in the same task.
  **Meets**: step 2's expiry calendar entry and step 6's post-renewal record both land in
  `docs/operations.md` in the task that makes them true.
- **`docs/desktop/09-release-update-and-distribution/README.md` § 3 "Emergency path"** — "A
  defective client is blocked by raising the minimum client version (admin setting, area 04)
  and, where needed, republishing the previous package; **there is no secret bypass**".
  **Meets**: R6 as written introduces no bypass, and the acceptance criterion says so.

Binding operator decisions, written to as settled:

- **D-002** (2026-08-23) — a **self-managed certificate**. R5 documents that route only.
  Delete no history from the decision matrix, but write **no alternative route** into the
  runbook: spikes `DSK-09-07` (Artifact Signing) and `DSK-09-09` (OV certificate) are
  withdrawn, and Key Vault `pegasusprodkv252ow37g` (`infra/modules/platform.bicep:85`) is not
  involved in certificate storage. Trust always reaches a machine before a package signed
  with that certificate does.
- **D-003** (2026-08-23) — R6 does **not** depend on the feed; the gateway gate is the fast
  lever, which matters because the feed is LAN/VPN-only.

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in the
plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) → `pegasus-release`
  (`.agents/skills/pegasus-release/SKILL.md`, verified present) for the runbook conventions
  and approval culture → `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`,
  verified present) for the signing commands.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for
  certificate-store and `certutil` behaviour questions.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates REL-012` before
  every move; a move crosses at most one gated boundary. `get_doc_gates` reports two gated
  boundaries: `leave-preparing` needs `plan` (this document), `enter-done` needs `proof`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's twelve implementation steps in the same order, with the same
ownership. Steps marked **Operator step** require elevated machine access.

1. **Orient and take.** Read `runbooks.md` § R5 and § R6 in full and
   `signing-and-hosting-decision-matrix.md` § D-002 "Chosen shape".
   `get_doc_gates REL-012`, then `take_ticket REL-012`. **D-002 is decided** — R5 documents
   the self-managed route only.
2. **R5 step 1 — the calendar entry.** Record the expiry in `docs/operations.md` with a
   **90-day warning**, beside the recorded thumbprint, subject and validity window handed
   over by `DSK-09-08` (board `REL-007`) step 13. Write the actual warning date, not "90 days
   before expiry" — a date is actionable, a rule is not. `DSK-09-18` (board `REL-016`) owns
   the surrounding desktop release table; add to it rather than creating a second place for
   certificate facts.
3. **R5 step 2 — issue the replacement with the same subject.** On the signing host, with the
   subject **equal to the manifest `Publisher` exactly**; changing it changes the package
   identity. Restrict the new `.pfx` to the publisher account
   (`icacls <path> /inheritance:r /grant "<publisher account>:(R)"`, the shape `DSK-09-08`
   used) and **keep the old one until step 6**.
4. **R5 steps 3–4 — export the public `.cer` only, then push trust before publishing
   anything signed with it.** Import into `Cert:\LocalMachine\TrustedPeople` on every
   workstation, scripted and elevated with
   `eng/packaging/Install-ProductionCertificateTrust.ps1`, or by Group Policy — whichever
   `DSK-09-08` (board `REL-007`) step 10 recorded as the estate mechanism. Reversing the
   order gives users `0x800B0109` and a failed update.
5. **R5 step 5 — verify on a machine that is not the publisher.**
   `certutil -verifystore TrustedPeople` must list **both** certificates during the overlap;
   then sign a test package with the new certificate and install it there.
6. **R5 steps 6–7 — switch over, then retire, then record.** Only after step 5 sign releases
   with the new certificate. Once **every** machine is confirmed, remove the old `.cer` from
   `TrustedPeople` — a decommissioned key should not keep vouching for packages. Record
   thumbprint, subject, validity window, rollout date and confirmed machines in
   `docs/operations.md`.
7. **R5's compromise variant, exactly as the plan states it.** Treat it as an emergency
   renewal: issue immediately, push the new trust, remove the old `.cer` **first** rather than
   last, re-sign the current release, and raise the gateway minimum version if a maliciously
   signed package could plausibly have been installed. State plainly that **there is no
   revocation list in a private-trust estate — removing trust is the revocation.**
8. **Operator step — rehearse R5 once, before go-live.** On the Test/UAT machines
   (`docs/desktop/08-testing/test-uat-stack.md` § Machine prerequisites: dedicated,
   rebuildable Windows 11 x64 VMs), issue a throwaway replacement certificate with the same
   subject; run the trust push; verify both certificates are listed; sign and install a test
   package; then remove the old `.cer` and confirm a package signed with the **retired**
   certificate no longer installs. Hand back every transcript. The first execution must not
   be the one under time pressure.
9. **R6 step 1 — the fast lever.** Raise the minimum client version above the defective
   version (R3 step 1, `DSK-09-12`, board `REL-010`) — or, when the fix is not yet built, to
   the **last good** version and publish the last good package as a rollback (R4,
   `DSK-09-13`, board `REL-011`) so clients can still work. Name the same admin control R3
   step 1 names; do not describe it twice in different words.
10. **R6 steps 2–4.** Confirm a defective client is refused — the
    `urn:pegasus:problem:client-unsupported` problem and the update-required screen
    (`docs/desktop/04-auth-session-update-and-startup/README.md:210`,
    `docs/desktop/06-ui-design/screen-specs.md:99-107`). Communicate to users. Collect
    diagnostics bundles (R10). Record in the release row and the security/action history.
    State R6's limit: it does **not** prove the defect's data impact — an audit query is
    needed.
11. **Operator step — rehearse R6 on the Test/UAT stack and time it.** Record the elapsed
    time from decision to a defective client being refused. **That number is the operational
    claim; do not write a target that was not measured.**
12. **Mark R5 and R6 as rehearsed** in `runbooks.md` with their dates, and record the dated
    `## Simplification pass` in this document. If the branch is documentation-only, the
    record is `n/a — docs-only`; check the actual diff before writing it.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture**, as the plan row assigns.
The obligation is **written, link-checked runbooks plus a recorded dry run**; the
workstation-level install evidence they reference belongs to `DSK-09-08` (board `REL-007`).
`proof` is the rehearsal transcripts as proof type `command-log`, plus the link-check output.

| Command / observation | Expected evidence |
| --- | --- |
| `certutil -verifystore TrustedPeople` during the rehearsal | **both** the old and new certificates listed during the overlap, then only the new one after step 6 |
| `Add-AppxPackage` of a package signed with the **retired** certificate, after removal | fails with `0x800B0109` — the negative that proves removal is the revocation |
| `signtool verify /pa /v` of a package signed with the replacement | valid chain **and** timestamp |
| Test/UAT stack R6 rehearsal | a defective client receives the `urn:pegasus:problem:client-unsupported` problem and the update-required screen; the **measured** elapsed time from decision to refusal is recorded |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exit code `0` |

Behaviours to read rather than infer: R5 as written names **one** route and no alternative;
neither runbook introduces a bypass that lets an unsupported client continue; and each
runbook carries its own "does not prove" clause.

## Risks / open questions

- **Risk — the renewal's first execution is the real one.** Mitigation: step 8's rehearsal is
  an acceptance criterion, not an optional extra, and includes the negative check.
- **Risk — the subject changes on re-issue.** It changes the package identity. Mitigation:
  step 3 states the rule, and the canonical string is the one `DSK-09-08` (board `REL-007`)
  pasted verbatim into its plan.
- **Risk — trust is pushed after the first signature.** Users get `0x800B0109` and a failed
  update. Mitigation: step 4 puts the push before the switch-over in both R5 and R7, and
  step 5's verification happens on a machine that is not the publisher.
- **Risk — the old `.cer` is left in place.** A decommissioned key keeps vouching for
  packages. Mitigation: step 6 removes it once every machine is confirmed, and the second
  verification command proves the removal took effect.
- **Risk — timestamping is assumed.** Certificate expiry without timestamping invalidates
  every installed package's signature path for **new** installs; existing timestamped
  installs keep working, which is why the runbook must say so explicitly rather than implying
  a fleet-wide outage. Mitigation: the third verification command reads the output for the
  timestamp line.
- **Risk — an unmeasured R6 target is written down and believed.** Mitigation: step 11
  records the measured elapsed time and nothing else.
- **Risk — `MERGE AUTH GRANTED` is extended to signing or publishing approvals.** It has one
  meaning. Mitigation: named in the ticket's Guardrails and restated here.
- **Open questions**: none. D-002 is decided and the estate mechanism was recorded by
  `DSK-09-08` (board `REL-007`) step 10; the admin control R6 uses is the one R3 names. **No
  `open-questions` document is created**, and no alternative signing route is re-opened.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. If the diff is
documentation only — `runbooks.md` and `docs/operations.md` — the record is
`n/a — docs-only`; confirm against the actual diff before writing it._
