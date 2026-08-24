# Plan — REL-007: DSK-09-08 · Issue the production self-managed certificate and roll trust to the estate

**Diff estimate: ~2 files, ~90 lines.** One new script
`eng/packaging/Install-ProductionCertificateTrust.ps1` (~70 lines: parameter block,
elevation check, `Import-Certificate`, `certutil -verifystore TrustedPeople` thumbprint
assertion, named failures, and the comment that pins the store choice) and ~20 lines added
to `docs/desktop/09-release-update-and-distribution/runbooks.md` § R5 and § R7 recording
the estate mechanism and the confirmed machine list. Everything else this ticket delivers
is **operator evidence**, not diff: a certificate, an ACL, a public `.cer`, two install
transcripts and ten per-machine confirmations. `docs/engineering.md:201-207` § Plan sizing
requires the estimate first.

## Approach

**Push trust before the package, prove that the ordering matters, then roll it out.** The
sequence is not a preference: Microsoft's deployment guidance, quoted in
`signing-and-hosting-decision-matrix.md` § D-002, says certificate trust must reach devices
before the app is installed, and the failure when it does not — `0x800B0109`
(`CERT_E_UNTRUSTEDROOT`) — is a code no operator can act on. So step 8 deliberately does it
**wrong on machine B first**, records the failure, then fixes it. That transcript is the
most valuable artefact this ticket produces, because it converts the ordering rule in R5
and R7 from a claim into an observation.

Two alternatives are already rejected by the operator and are **not re-argued here**:
Azure Artifact Signing (spike `DSK-09-07`, **withdrawn**) and a purchased OV certificate in
Key Vault (spike `DSK-09-09`, **withdrawn**). A third, a private two-tier PKI, is rejected
in the matrix itself because it requires the root in `Trusted Root Certification
Authorities` — a far broader grant than ten machines need. The route is a single
self-signed certificate in `LocalMachine\TrustedPeople`, and this plan writes only that.

The trust script is built for the **scripted** rollout first, because it works whether or
not the estate is domain-joined; Group Policy Trusted People is an optimisation that
depends on a fact step 10 must establish rather than assume.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-007`). No existing PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by `DSK-09-01` (board `REL-001`). Its **Consequences**
> section records D-002 by name and date: signing is a self-managed certificate trusted per
> workstation in `LocalMachine\TrustedPeople` (never `Trusted Root`), with the accepted
> trade-offs of a per-machine trust rollout and a rehearsed renewal. This ticket is the
> execution of that consequence. This plan is written to the decision as recorded in
> `docs/desktop/09-release-update-and-distribution/signing-and-hosting-decision-matrix.md`
> § D-002 "Decision" and "Chosen shape"; if ADR-0105 lands differently, this plan is
> revised before implementation.

Existing ADRs this plan relates to:

- **ADR-0007** (`docs/adr/0007-direct-terminal-azure-deployment.md`) — releases run from an
  authorised Windows terminal. **Meets**: issuance, export and rollout are attended
  operator actions on the signing host and on each workstation; there is no unattended
  path.

Binding operator decisions and constraints, written to as settled:

- **D-002** (2026-08-23) — a **self-managed certificate**; the `.pfx` never leaves the
  signing host and is not a GitHub secret; `LocalMachine\TrustedPeople`, never
  `Trusted Root`; subject equals the manifest `Publisher` exactly; timestamping mandatory;
  ~3-year validity.
- **D-003** (2026-08-23) — the signing host is the **same always-on in-house machine that
  serves the UNC share**. The certificate never lives on the share.
- **C-01** — the repositories become private, so a GitHub-secret-based signing route is
  excluded and the key stays in the estate.

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in
the plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) →
  `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`,
  `microsoft/win-dev-skills` v0.5.0 `f1028dd5`, verified present).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`,
  `microsoft_docs_fetch`, `microsoft_code_sample_search`).
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates REL-007` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's fourteen implementation steps in the same order, with the same
ownership. Steps marked **Operator step** require elevated access to the signing host or
access to a workstation; **an agent prepares the command and the evidence template and
records the result — it does not perform them.**

1. **Orient and take.** Read the area plan § 5 row `DSK-09-08`,
   `signing-and-hosting-decision-matrix.md` § D-002 in full, and `runbooks.md` § R5 and
   § R7. `get_doc_gates REL-007`, then `take_ticket REL-007`. **D-002 is decided** — write
   and execute the self-managed route only.
2. **Fix the subject string once and for all.** Read `Identity/@Publisher` from
   `src/Pegasus.Desktop/Package.appxmanifest` (created by `DSK-02-05`, board `FND-030`) and
   paste it into this document verbatim as the canonical string, in a fenced block so
   whitespace survives. It must equal the certificate subject **exactly — same fields, same
   order, same spacing and case** — or signing fails with `0x8007000B` (AppxPackagingOM
   Event ID 150). It is never changed afterwards, because changing it changes the package
   identity. Cross-check it against the value `DSK-09-06` (board `REL-006`) recorded for
   the development certificate: they must be the same string.
3. **Verify the issuance command against official documentation before running it.**
   `microsoft_docs_search` for
   `create a certificate for package signing New-SelfSignedCertificate code signing MSIX`,
   then `microsoft_docs_fetch` the resulting page and record its URL and fetch date here.
   The documented shape is
   `New-SelfSignedCertificate -Type Custom -Subject "<publisher>" -KeyUsage DigitalSignature
   -FriendlyName "Pegasus desktop code signing" -CertStoreLocation "Cert:\LocalMachine\My"
   -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")`, plus
   `-NotAfter (Get-Date).AddYears(3)` for the ~3-year validity D-002 fixes. The two OIDs
   are code-signing EKU and an empty Basic Constraints extension; confirm both from the
   page rather than trusting this plan. **Do not run it from memory.**
4. **Operator step — issue on the signing host.** The always-on in-house machine that also
   serves the D-003 share. Run the verified command elevated. Hand back: thumbprint,
   subject, `NotBefore`, `NotAfter`, and the machine name. The private key is generated on
   that host and nowhere else.
5. **Operator step — export and protect the key.**
   `Export-PfxCertificate -Cert Cert:\LocalMachine\My\<thumbprint> -FilePath <secure path>\PegasusCodeSigning.pfx -Password (Read-Host -AsSecureString)`,
   then restrict the ACL:
   `icacls <path> /inheritance:r /grant "<publisher account>:(R)"`. Hand back the `icacls`
   output. The `.pfx` never leaves this host, is never committed, and is **never** stored as
   a GitHub secret. Choose a `<secure path>` **outside any git working tree** — note that
   `.gitignore` carries no `*.pfx` rule today (`git check-ignore -v devcert.pfx` exits `1`
   with no output; `DSK-09-06` adds one), so proximity to a repository is a real hazard, not
   a theoretical one.
6. **Operator step — export the public certificate only.**
   `Export-Certificate -Cert Cert:\LocalMachine\My\<thumbprint> -FilePath .\PegasusCodeSigning.cer -Type CERT`.
   Only this `.cer` reaches a workstation.
7. **Write `eng/packaging/Install-ProductionCertificateTrust.ps1`.** Repository script
   header: `[CmdletBinding()]`, `Set-StrictMode -Version Latest`,
   `$ErrorActionPreference = 'Stop'`. Parameters `-CertificatePath` and
   `-ExpectedThumbprint` (both mandatory). Assert the session is elevated and `throw` a
   sentence naming the requirement if not. Run
   `Import-Certificate -FilePath <cer> -CertStoreLocation Cert:\LocalMachine\TrustedPeople`,
   then `certutil -verifystore TrustedPeople`, and fail unless `-ExpectedThumbprint` appears
   in the output. Put the store rationale in a comment **at the import line**, where the
   next reader will be tempted to change it: `TrustedPeople`, never
   `Trusted Root Certification Authorities`, because a root grant would let that key vouch
   for anything the machine trusts. Start from `eng/packaging/Install-DevCertificateTrust.ps1`
   (`DSK-09-06`, board `REL-006`) so the two scripts do not diverge in behaviour.
8. **Operator step — prove the ordering on two machines.**
   *Machine A*: run the trust script; confirm `certutil -verifystore TrustedPeople` lists
   the thumbprint; sign a test package
   (`winapp package <dir> --cert PegasusCodeSigning.pfx --self-contained --timestamp <timestamp url>`)
   and install it.
   *Machine B*: attempt the install **first**, record the exact failure (expected
   `0x800B0109`), then run the trust script and retry successfully.
   Hand back both transcripts verbatim. Machine B's failure is the evidence that R5 and R7
   must push trust before publishing.
9. **Verify every signature is timestamped.** `signtool verify /pa /v <pkg>.msix` must
   report **both** a valid chain and a timestamp — read the output for the timestamp line;
   do not accept exit `0` alone. Without `--timestamp`/`/tr`, installed packages stop
   validating for **new** installs the day the certificate expires, and that failure hides
   for up to three years.
10. **Operator step — decide and record the estate mechanism.** Establish, do not assume,
    whether the machines are domain-joined —
    `(Get-CimInstance Win32_ComputerSystem).PartOfDomain` on one workstation, or the
    operator's answer. Then choose (a) the scripted elevated `Import-Certificate` per
    machine, or (b) Group Policy → Computer Configuration → Windows Settings → Security
    Settings → Public Key Policies → **Trusted People**. Record which applies, why, and the
    machine list in this document. GPO is available only in case (a)'s absence.
11. **Operator step — roll trust to the remaining workstations** with the chosen mechanism
    and hand back a per-machine confirmation: machine name, date, and the
    `certutil -verifystore TrustedPeople` thumbprint line. Trust reaches every machine
    **before** `DSK-09-11` (board `REL-009`) publishes the first signed package — that is
    R1 precondition 4.
12. **Write up the renewal and revocation rehearsal R5 will formalise**: same subject on
    re-issue; overlap window; new trust pushed before the first signature with the new key;
    old `.cer` removed only after every machine is confirmed; and the fact that **there is
    no revocation list in a private-trust estate — removing trust is the revocation**. Hand
    the write-up to `DSK-09-14` (board `REL-012`), which owns R5 and R6, rather than leaving
    it to re-derive one. Name the concentration risk in the write-up — the signing host also
    serves the share and would host the CI runner — and point its mitigations at R9
    (`DSK-09-10`, board `REL-008`) and the security plan rather than solving them here.
13. **Record the certificate facts for the release record**: thumbprint, subject, validity
    window, signing host, rollout date, confirmed machines. `DSK-09-18` (board `REL-016`)
    puts them in `docs/operations.md` with a 90-day expiry warning.
14. **Simplification pass.** Record it under a dated `## Simplification pass` heading in
    this document (`AGENTS.md` § Repository task workflow step 4). This branch adds a script
    and edits a runbook, so `n/a — docs-only` does not apply.

## Verification

Evidence tier from the body: **Tier 7** — the workstation-level evidence tier the plan row
assigns. Proof is observed install behaviour on real machines with and without trust, plus
per-machine trust confirmations — **not a script's return code**. `proof` is the operator
transcripts as proof type `command-log`.

| Command / observation | Expected evidence |
| --- | --- |
| `certutil -verifystore TrustedPeople` on each workstation | the production thumbprint listed, once per machine, with machine name and date |
| `signtool verify /pa /v .\Pegasus_<ver>_x64.msix` | `Successfully verified`, a chain terminating at the self-managed certificate, **and** a timestamp line |
| `Add-AppxPackage .\Pegasus_<ver>_x64.msix` on a trusted machine | succeeds; `Get-AppxPackage CollisionEngineers.Pegasus` reports the version |
| `Add-AppxPackage .\Pegasus_<ver>_x64.msix` on an untrusted machine (machine B, before trust) | fails with `0x800B0109`; the retry after the trust script succeeds |
| `icacls <pfx path>` on the signing host | only the publisher account has read access; inheritance removed |

Behaviours to observe rather than infer, and to state in the proof: the certificate
`NotAfter` is approximately three years out; the subject string in the certificate is
byte-identical to `Identity/@Publisher`; and the estate mechanism chosen at step 10 was
based on an **established** domain-join fact, with the command or the operator's answer
recorded.

## Risks / open questions

- **Risk — a package signed with the new certificate reaches a machine before its trust
  does.** The result is `0x800B0109` and a user who cannot work. Mitigation: step 11
  completes before `DSK-09-11` (board `REL-009`) publishes, and step 8 makes the failure
  mode observed rather than theoretical.
- **Risk — a subject mismatch.** Packaging fails with `0x8007000B`. Mitigation: step 2
  pastes the canonical string verbatim and cross-checks it against the development
  certificate's subject.
- **Risk — a signature without a timestamp.** Hides for up to three years, then breaks
  every **new** install. Mitigation: step 9 reads the output for the timestamp line rather
  than trusting the exit code.
- **Risk — `Trusted Root` instead of `TrustedPeople`.** A materially broader grant,
  forbidden by D-002. Mitigation: step 7's comment sits at the import line, and
  `winapp cert install` — which writes to Trusted Root — is excluded from this route.
- **Risk — the signing host is a single point of failure and a single high-value target.**
  It carries the key, the share and a future runner. Mitigation: restrictive ACLs (step 5),
  a certificate backup and a documented rebuild path, all named in step 12 and owned by R9
  (`DSK-09-10`, board `REL-008`) and the security plan. This ticket names it; it does not
  solve it.
- **Risk — SmartScreen warns on the first download of a new package hash.** That is
  reputation, not a signature failure. Mitigation: it is stated in R7 and in the operator
  one-pager (`DSK-09-15`, board `REL-013`); do not treat it as a defect here.
- **Open question — is the estate domain-joined?** It decides whether the rollout mechanism
  is scripted `Import-Certificate` or GPO Trusted People. **Who answers it**: the operator,
  during step 10, from one command. It is **not blocking**: the scripted path works either
  way and is what steps 7–8 build and prove, so the ticket can be planned, scripted and
  rehearsed before the answer arrives. No `open-questions` document is created.
- **Not open, and not to be re-opened**: the signing route. D-002 is decided; spikes
  `DSK-09-07` (Artifact Signing) and `DSK-09-09` (OV certificate) are withdrawn; Key Vault
  `pegasusprodkv252ow37g` (`infra/modules/platform.bicep:85`) is not touched.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch adds a
script and edits `runbooks.md`, so `n/a — docs-only` does not apply._
