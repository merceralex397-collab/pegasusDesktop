# Files — REL-007

Surveyed on 2026-08-24 against the fork at branch `task/desktop-plan-segmentation`.
Most of this ticket is **operator work on machines**, which produces evidence rather than
diff. The repository change is one script.

## Where the change lands

| Path | Why |
|---|---|
| `eng/packaging/Install-ProductionCertificateTrust.ps1` | **New**, and the only file this ticket adds. Elevated; takes `-CertificatePath` and `-ExpectedThumbprint`; runs `Import-Certificate -FilePath <cer> -CertStoreLocation Cert:\LocalMachine\TrustedPeople`, then `certutil -verifystore TrustedPeople`, and fails unless the expected thumbprint is listed. Breaks if the store is changed to `Trusted Root Certification Authorities`: that grant lets the key vouch for anything the machine trusts and is forbidden by D-002. `eng/` does not exist today (`ls eng` returns nothing); `eng/packaging/` is created by `DSK-09-02` (board `REL-002`) or `DSK-09-06` (board `REL-006`), whichever lands first. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R5 and § R7 | **Edited.** R5 and R7 gain the recorded estate mechanism (scripted `Import-Certificate` or GPO Trusted People) and the confirmed machine list. `DSK-09-14` (board `REL-012`) and `DSK-09-15` (board `REL-013`) finalise those runbooks; this ticket contributes the facts they need. |

**Operator artefacts that are not repository files** and must be captured in the ticket
proof instead: the certificate thumbprint, subject, `NotBefore`/`NotAfter` and signing-host
name (step 4); the `icacls` output for the `.pfx` (step 5); the public `.cer` (step 6);
machine A's and machine B's install transcripts (step 8); the per-machine
`certutil -verifystore TrustedPeople` confirmations (step 11).

## Context files

Read these before issuing anything. Each carries a rule whose violation is discovered on
ten machines rather than in review.

| Path | What it tells the implementer |
|---|---|
| `docs/desktop/09-release-update-and-distribution/signing-and-hosting-decision-matrix.md` § D-002 | **The specification for this ticket.** Its "Decision" and "Chosen shape" bullets fix: one self-signed certificate and no private CA; `Cert:\LocalMachine\TrustedPeople` and **never** `Trusted Root`; only the public `.cer` reaches a workstation while the `.pfx` stays on the signing host under an ACL limited to the publisher account and is **not** a GitHub secret; the subject must equal the manifest `Publisher` exactly or signing fails with `0x8007000B` (AppxPackagingOM Event ID 150); timestamping is mandatory; validity ≈ 3 years; the expected failure when trust is missing is `0x800B0109`; and the rollout is a scripted elevated `Import-Certificate` **or** GPO Trusted People if the estate is domain-joined. It also records that the option tables are "history, not a menu". |
| `docs/desktop/09-release-update-and-distribution/signing-and-hosting-decision-matrix.md` § How the decisions interact | "**One machine carries it all**" — the signing host also serves the D-003 share and would host the self-hosted CI runner. That concentration is "the design's main operational risk… a single point of failure for publishing (not for running: installed clients keep working) and a single high-value target". Name it in the write-up; its mitigations belong to R9 and the security plan, not here. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R5 | The renewal procedure this ticket must make executable: same subject on re-issue, overlap window, **push trust before publishing anything signed with the new certificate**, verify on a machine that is not the publisher, remove the old `.cer` only after every machine is confirmed, and the compromise variant that reverses the removal order. Its closing line is the one to carry into the write-up: "There is no revocation list in a private-trust estate — removing trust is the revocation." |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R7 prerequisite 4 | The first-install view of the same rule: import the public `.cer` into `Cert:\LocalMachine\TrustedPeople` (elevated, once per machine) or let Group Policy deliver it, verify with `certutil -verifystore TrustedPeople`; "If the package arrives first the install fails with `0x800B0109`; the fix is this step, then retry." |
| `.codex/skills/winui-packaging/SKILL.md` | § Key Rules: `--timestamp` is "critical for production — without it, signatures expire with the cert"; the certificate subject must match `Identity.Publisher`; `winapp package --cert` is preferred over a separate `winapp sign`. § End-to-End Workflow step 3 warns implicitly about the wrong store: `winapp cert install` "Adds cert to machine Trusted Root store" — the command this ticket must not use. |
| `docs/adr/0007-direct-terminal-azure-deployment.md` | Why release operations are attended and terminal-based, which is the same posture this ticket takes for issuance and rollout. |
| `.agents/skills/pegasus-release/SKILL.md` § The estate | "Read-only Azure checks need no approval. **Every write needs explicit operator approval for the exact target**", and `MERGE AUTH GRANTED` has exactly one meaning. This ticket needs no Azure write at all, and must not extend that phrase. |
| `infra/modules/platform.bicep:85` | Key Vault `pegasusprodkv252ow37g` — secrets only, no certificate resource. Recorded so a reader can see it was considered and is deliberately **not** used: D-002 withdrew the Key Vault certificate route. |
| `docs/desktop/08-testing/test-uat-stack.md` § Machine prerequisites | The test machines are dedicated rebuildable Windows 11 x64 VMs, already required to carry "a development signing certificate trusted in `Cert:\LocalMachine\TrustedPeople` — the same store and mechanism the production certificate uses (D-002)". Machines A and B in step 8 come from here. It also records that Developer Mode is **not** required to install a signed MSIX. |
| `eng/packaging/Install-DevCertificateTrust.ps1` (created by `DSK-09-06`, board `REL-006`) | The rehearsal of this ticket's script, on disposable material. Read it first: this ticket's production script is the same shape with an added `-ExpectedThumbprint` assertion, and diverging from it means two trust scripts that behave differently. |
| `src/Pegasus.Desktop/Package.appxmanifest` (created by `DSK-02-05`, board `FND-030`) | `Identity/@Publisher` — the canonical subject string. Read it, paste it into the plan verbatim, and never change it: changing it changes the package identity. |

## Ripple effects

- **`DSK-09-11` (board `REL-009`)** cannot publish the first signed package until trust has
  reached every machine. This ticket's step 11 per-machine confirmations are that ticket's
  precondition 4 evidence.
- **`DSK-09-14` (board `REL-012`)** owns R5 and R6 and consumes step 12's renewal and
  revocation write-up plus the recorded estate mechanism. Hand it the write-up rather than
  leaving it to re-derive one.
- **`DSK-09-15` (board `REL-013`)** puts the trust step first on the operator one-pager;
  the exact command and the retry instruction come from here.
- **`DSK-09-17` (board `REL-015`)** requires the `.pfx` to be on the signing host and
  **not** a GitHub secret, which is why its lane must run on a self-hosted runner there.
  The host name and publisher account recorded here are that ticket's `runs-on` label
  input.
- **`DSK-09-18` (board `REL-016`)** puts the certificate subject, thumbprint, validity
  window and the 90-day expiry warning into `docs/operations.md`. Step 13's recorded facts
  are its source.
- **`DSK-09-03` (board `REL-003`)** validator check 4 compares
  `MainPackage/@Publisher` against the manifest's `signerSubject`; the string fixed at step
  2 is what makes that check meaningful.
- **`DSK-09-02` (board `REL-002`)** manifest fields `signerSubject` and `signerThumbprint`
  are filled from this certificate.
- **No OpenAPI, generated-client, test or build ripple.** No endpoint, no contract, no
  package reference, no project file changes.

## Out of scope

Recorded so the reviewer sees these were decisions, not oversights. Each is a Guardrail in
the ticket body.

- **Azure, entirely.** D-002 withdrew the Artifact Signing and Key Vault certificate routes;
  Key Vault `pegasusprodkv252ow37g` (`infra/modules/platform.bicep:85`) is not touched.
  Read-only Azure calls are permitted but not needed.
- **Reviving the withdrawn spikes.** `DSK-09-07` (Artifact Signing eligibility) and
  `DSK-09-09` (OV certificate procurement) are withdrawn. Do not write a comparison, a
  fallback or a "if D-002 is revisited" branch.
- **A private two-tier PKI.** Considered and rejected in the matrix: it requires the root
  in `Trusted Root`, a far broader grant than ten machines need.
- **`Trusted Root Certification Authorities`.** Forbidden by D-002. `winapp cert install`
  writes there and must not be used for the production certificate.
- **A second certificate for the pilot channel.** One package identity serves both channels
  (area plan § 3 Channels); a separate pilot identity was considered and rejected.
- **`infra/`, `src/Pegasus.Web`, the gateway release route.** Untouched.
- **Fixing the signing host's concentration risk.** Named in the write-up; its mitigations
  (restrictive ACLs beyond the `.pfx`, share backup, documented rebuild path) belong to R9
  in `DSK-09-10` (board `REL-008`) and the security plan.
- **Performing the operator actions.** Certificate issuance, key export, ACL setting and
  the per-machine rollout require elevated access to the signing host and access to each
  workstation. An agent prepares the scripts and the evidence template; it does not perform
  them.
