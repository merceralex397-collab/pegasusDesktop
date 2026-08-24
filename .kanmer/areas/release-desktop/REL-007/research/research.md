# Research — REL-007: issuing a self-managed signing certificate and reaching ten machines with its trust before any package

## Question

What exactly must be issued, exported, protected and pushed — and in what order — so that
the first signed Pegasus package installs on every workstation rather than failing with
`0x800B0109`?

## Current behaviour

**There is no signing of anything, anywhere in the repository.** Verified on 2026-08-24:

- `ls eng` returns nothing; there is no packaging folder.
- `.github/workflows/ci.yml` (234 lines, nine jobs) has no publish, sign or deploy lane
  and no protected job.
- `grep`-level reading of `scripts/` (21 scripts) finds no `signtool`, no `winapp`, no
  certificate handling. `scripts/Build-ReleaseArtifacts.ps1` produces an OCI image archive
  and an EF migration bundle; neither is signed.
- Key Vault `pegasusprodkv252ow37g` exists — `infra/modules/platform.bicep:85`
  (`resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01'`) — and holds **secrets
  only**. It has no certificate resource, and **this ticket does not touch it**: D-002
  withdrew the Key Vault certificate route.
- Releases run from an authorised Windows terminal —
  `docs/adr/0007-direct-terminal-azure-deployment.md`, and
  `.agents/skills/pegasus-release/SKILL.md` § The estate ("Read-only Azure checks need no
  approval. **Every write needs explicit operator approval for the exact target**").

**No parity-matrix row covers this.** `docs/desktop/01-inventory-and-parity/parity-matrix.md`
runs `PAR-01`…`PAR-46` over Razor page models; code signing is not an observable web
capability and has no row. Signing is new desktop responsibility under proposal § 9.1 and
§ 17.1.

## Findings

- **D-002 is decided and is not a comparison.** The decision matrix opens: "Both decisions
  are settled (2026-08-23): D-002 = option C, a self-managed certificate; D-003 = option C,
  a UNC file share… The option tables below are kept as the record of what was considered
  and why each choice was made — they are history, not a menu."
  `signing-and-hosting-decision-matrix.md` § D-002. The withdrawn spikes are named in the
  same file: `DSK-09-07` (Artifact Signing eligibility) and `DSK-09-09` (OV procurement)
  are **withdrawn**; this ticket "becomes the implementation ticket for the chosen route".
- **`TrustedPeople`, never `Trusted Root`.** The chosen shape's first bullet: the
  certificate is imported into `Cert:\LocalMachine\TrustedPeople` — "**not** into
  `Trusted Root Certification Authorities`, which the MSIX troubleshooting guide explicitly
  warns against because it would let that key vouch for anything the machine trusts". A
  private two-tier PKI was considered and rejected because it requires the root in Trusted
  Root, "a far broader grant than ten machines need".
- **The subject must equal the manifest `Publisher` exactly** — "same fields, same order,
  same spacing and case — or signing fails with `0x8007000B` (AppxPackagingOM Event ID
  150). The subject is therefore fixed once, before the first package is built, and never
  changed" — because changing it changes the package identity.
- **Ordering rule, from Microsoft's deployment guidance**, quoted in the same section:
  *"Certificate trust must reach devices before the app is installed"*. The expected
  failure when it does not is `0x800B0109` (`CERT_E_UNTRUSTEDROOT`).
- **Timestamping is mandatory** so packages already installed keep validating after the
  certificate expires — `signing-and-hosting-decision-matrix.md` § D-002 and
  `.codex/skills/winui-packaging/SKILL.md` § Key Rules ("`--timestamp` is critical for
  production — without it, signatures expire with the cert").
- **Validity ≈ 3 years**, "long enough that renewal is rare and short enough that a
  compromised key is not a decade-long liability".
- **There is no revocation list in a private-trust estate — removing trust is the
  revocation.** `runbooks.md` § R5, compromise variant.
- **Rollout mechanism is a choice with a factual precondition**: a scripted elevated
  `Import-Certificate` per machine, **or** Group Policy → Computer Configuration → Windows
  Settings → Security Settings → Public Key Policies → **Trusted People** "if the estate is
  domain-joined". Whether it is domain-joined "is a fact to establish, not assume" — the
  ticket body's step 10.
- **Sideloading needs no Developer Mode** on Windows 11 — the matrix's last chosen-shape
  bullet, and `docs/desktop/08-testing/test-uat-stack.md` § Machine prerequisites
  ("Developer Mode is **not** required to install a signed MSIX").
- **The signing host is the same machine that serves the D-003 share and would host a
  self-hosted CI runner.** `signing-and-hosting-decision-matrix.md` § How the decisions
  interact: "**One machine carries it all**… That concentration is the design's main
  operational risk — it is a single point of failure for publishing (not for running:
  installed clients keep working) and a single high-value target."
- **The `.pfx` is not a GitHub secret.** Constraint C-01 makes the repositories private and
  the natural signing host is in-house, "so the key never leaves the estate". This is why
  `DSK-09-17` (board `REL-015`) must run on a self-hosted runner.
- **`.gitignore` has no `*.pfx` rule today** — `git check-ignore -v devcert.pfx` exits `1`
  with no output; `.gitignore` (77 lines) covers `**/artifacts/` and `/artifacts/` at
  `:20-21` and a "Local secrets and environment state" block at `:4-13`, but no key
  pattern. `DSK-09-06` (board `REL-006`) step 4 adds it. Relevant here because the
  production `.pfx` must never be near the working tree at all.

### Facts

Verified by reading this repository on 2026-08-24 unless a URL and fetch date is given.

| Fact | Source |
| --- | --- |
| No certificate, signing tool or signing step exists in the repository | `ls eng`; `.github/workflows/ci.yml`; `ls scripts/` |
| Key Vault `pegasusprodkv252ow37g` holds secrets only and is **not** used by this ticket | `infra/modules/platform.bicep:85`; D-002 withdrew the Key Vault route |
| Releases run from an authorised Windows terminal; every Azure write needs exact-target approval; reads are free | `docs/adr/0007-direct-terminal-azure-deployment.md`; `.agents/skills/pegasus-release/SKILL.md` § The estate; `docs/runbook.md:776` § Live-operation approval matrix |
| D-002's chosen shape: `TrustedPeople` not `Trusted Root`; exact-subject rule and `0x8007000B`; mandatory timestamping; ~3-year validity; `0x800B0109` when trust is missing; trust-before-app ordering; scripted `Import-Certificate` or GPO Trusted People | `docs/desktop/09-release-update-and-distribution/signing-and-hosting-decision-matrix.md` § D-002 |
| R5 steps 2–7 (renewal) and R7 prerequisite 4 (first install) both put trust before the package | `docs/desktop/09-release-update-and-distribution/runbooks.md` |
| `winapp package --cert` preferred over `winapp sign`; `--timestamp` critical; Publisher must match `Identity.Publisher` | `.codex/skills/winui-packaging/SKILL.md` § Key Rules |
| The signing host, the share host and a future self-hosted runner are one machine, and that is the design's main operational risk | `signing-and-hosting-decision-matrix.md` § How the decisions interact |
| `.gitignore` carries no `*.pfx` rule today | `git check-ignore -v devcert.pfx` → exit `1`, no output; `.gitignore:1-77` |
| The estate is ten Windows 11 x64 workstations | area plan § 2 Assumptions |

### Assumptions

- **A-09-13 — the estate's domain-join status is unknown to the repository.** Nothing in
  `docs/` records whether the ten workstations are domain-joined, and Group Policy Trusted
  People exists only if they are.
  *Confirmed by*: the operator answering, or `(Get-WmiObject Win32_ComputerSystem).PartOfDomain`
  on one workstation.
  *Breaks if wrong*: a rollout plan built on GPO cannot execute and every machine needs a
  visit. Mitigation: the scripted `Import-Certificate` path works in **both** cases, so
  build that first and treat GPO as an optimisation. Step 10 records the answer; it does
  not assume one.
- **A-09-14 — `New-SelfSignedCertificate`'s documented flag set still produces a
  code-signing certificate MSIX accepts.** The shape recorded in the body is
  `-Type Custom -Subject "<publisher>" -KeyUsage DigitalSignature -FriendlyName "…"
  -CertStoreLocation "Cert:\LocalMachine\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")`
  plus `-NotAfter (Get-Date).AddYears(3)`.
  *Confirmed by*: the body's step 3 — `microsoft_docs_search` then `microsoft_docs_fetch`
  the resulting page **before running it**, and `signtool verify /pa /v` on a package
  signed with the result.
  *Breaks if wrong*: a certificate that signs but whose Enhanced Key Usage MSIX rejects,
  discovered only at install time on the estate. Mitigation: step 8's machine-A test
  install proves it before the rollout, not after.
- **A-09-15 — the timestamp service is reachable from the signing host.** Nothing in this
  repository names one; the skill's example uses `http://timestamp.digicert.com`.
  *Confirmed by*: a signed test package whose `signtool verify /pa /v` output contains a
  timestamp line.
  *Breaks if wrong*: signatures without a timestamp, which means every **new** install
  stops working the day the certificate expires while existing installs continue — a
  failure that hides for up to three years. Mitigation: step 9 checks the output for the
  timestamp line explicitly and fails on its absence.
- **A-09-16 — one certificate is enough for both channels.** The area plan's assumptions
  say "Package identity `CollisionEngineers.Pegasus`, one identity for both channels", and
  § 3 Channels records that a separate pilot identity was considered and rejected.
  *Confirmed by*: the pilot and prod `.appinstaller` files carrying the same
  `MainPackage/@Name` and `@Publisher`.
  *Breaks if wrong*: two certificates, two trust rollouts, two renewals. Nothing in the
  decided shape suggests it; do not introduce a second.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3, answered for the responsibility
this ticket places: *custody of the signing key and distribution of trust to the estate*.

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **no** | One certificate, one signing host, one publisher account. Workstations consume a public `.cer` and update nothing. |
| Unattended execution — must it run with every desktop closed? | **no** | Issuance, export, ACL restriction and the per-machine trust push are all attended operator actions (steps 4–6, 8, 10, 11). |
| Protected credentials — long-lived secret that must not sit on workstations? | **yes** | The `.pfx` is a ~3-year private key. D-002 places it **on the in-house signing host** with an ACL restricted to the publisher account — not in Azure Key Vault (that route was withdrawn), not as a GitHub secret (C-01 makes the repositories private and the key would leave the estate). A "yes" here means *not on a workstation*; it does not mean *in a cloud service*. |
| Public callback — must an external service call a stable public endpoint? | **no** | Nothing calls in. The only outbound call is to an RFC-3161 timestamp service at signing time. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **no** | A private-trust estate has **no revocation list**: removing the `.cer` from `TrustedPeople` on each machine is the revocation (`runbooks.md` § R5). Enforcement against an obsolete client is the gateway minimum-version gate, `DSK-04-06` (board `GWY-023`), not this ticket. |
| Measured operational advantage — measured evidence central is materially better? | **no** | The matrix records the opposite: with the feed private and LAN-only (D-003), SmartScreen reputation is irrelevant and no anonymous endpoint exposes packages, so "the advantages the paid routes buy… have no one to serve". |

One "yes" — protected credentials — and it lands on the **in-house signing host**, which
is not a cloud resource. **This ticket requires no Azure write at all**, and D-002
explicitly withdrew both Azure signing routes.

## Implications

- **Order everything around the trust-before-package rule.** Steps 7–8 exist to prove it,
  and step 11 exists to complete it before `DSK-09-11` (board `REL-009`) publishes the
  first signed package. Reversing the order gives users `0x800B0109` and a failed update.
- **Fix the subject once, in writing, before the key exists.** Step 2 pastes the canonical
  string into the plan. Everything downstream — the packaging Publisher check, validator
  check 4, the renewal in R5 — depends on it never changing.
- **The negative test is the deliverable.** Machine B's `0x800B0109` before trust and its
  success after is the only evidence that turns R5 and R7's ordering rule from a claim into
  a fact.
- **The scripted path works in both estates; GPO does not.** Build
  `Install-ProductionCertificateTrust.ps1` first (A-09-13) and treat GPO as an
  optimisation once the domain-join fact is established.
- **Timestamp verification must read the output, not the exit code** (A-09-15). A missing
  timestamp is a failure that hides for years.
- **Concentration risk is real and is out of this ticket's scope to fix.** The signing
  host, the share and a future runner are one machine; mitigations — restrictive ACLs, a
  backup of the certificate, a documented rebuild path — belong in R9 (`DSK-09-10`, board
  `REL-008`) and the security plan. Name it here; do not solve it here.

## Open questions

- **The estate's domain-join status** (A-09-13) determines whether the rollout mechanism is
  a scripted `Import-Certificate` or Group Policy Trusted People. It is answered by the
  operator during step 10 with one command, and it is **not blocking**: the scripted path
  works either way and is what steps 7–8 build and prove. It is recorded here and in the
  plan's Risks section rather than raised as a blocking item, so the board stays movable.
- No `open-questions` document is created. D-002 and D-003 are settled and must not be
  re-opened; the withdrawn spikes `DSK-09-07` and `DSK-09-09` must not be revived.
