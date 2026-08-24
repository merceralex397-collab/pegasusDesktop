# Plan — REL-006: DSK-09-06 · Development certificate pipeline and trust on Test/UAT machines

**Diff estimate: ~2 files, ~70 lines.** One new script
`eng/packaging/Install-DevCertificateTrust.ps1` (~60 lines: parameter block, elevation
check, `Import-Certificate`, `certutil -verifystore TrustedPeople` thumbprint assertion,
named failures) and ~4 added lines in `.gitignore`. The rest of the ticket is **operator
work on machines**, which produces evidence rather than diff — steps 7, 9 and 10 below.
`docs/engineering.md:201-207` § Plan sizing requires the estimate first.

## Approach

**Rehearse the production trust mechanism with a throwaway key.** The development
certificate is not a different route from the production one — it is the same route with
disposable material, and that is the whole value of this ticket. D-002 fixes the
production shape as a self-managed certificate imported into
`Cert:\LocalMachine\TrustedPeople`, so this ticket imports the dev certificate into the
**same store** by the **same command**, and proves the ordering rule on a machine that has
not had the trust step. The alternative rejected is the one the vendored skill offers
first: `winapp cert install ./devcert.pfx`, which
`.codex/skills/winui-packaging/SKILL.md` § End-to-End Workflow step 3 states plainly
"Adds cert to machine Trusted Root store. Persists across reboots." A Trusted Root grant
lets that key vouch for anything the machine trusts — a materially broader grant than ten
workstations need, warned against by the MSIX troubleshooting guide, and forbidden by
D-002. Using `Import-Certificate` into `TrustedPeople` instead costs one script and buys a
rehearsal that transfers directly to [[REL-007]] (plan handle `DSK-09-08`).

The negative test is the point of the ticket, not a nicety: step 10 reproduces
`0x800B0109` on an untrusted machine and then fixes it, which is the only evidence that
the trust-before-package ordering in runbooks R5 and R7 is real rather than folklore.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-006`). No existing PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by [[REL-001]] (plan handle `DSK-09-01`); see
> [[REL-001]]'s plan for the ownership reconciliation — ADR-0105 has three claimants
> (`REL-001`, `FND-005`, `FND-042`). Its Consequences
> record D-002: signing is a self-managed certificate trusted per workstation in
> `LocalMachine\TrustedPeople`, never `Trusted Root`. This ticket rehearses exactly that
> consequence with a throwaway key. This plan is written to the decision as recorded in
> `docs/desktop/09-release-update-and-distribution/signing-and-hosting-decision-matrix.md`
> § D-002 "Chosen shape"; if ADR-0105 lands differently, this plan is revised before
> implementation.

Existing ADRs this plan relates to:

- **ADR-0014** (`docs/adr/0014-local-to-production-deployment.md`) — local and production
  only, no Azure dev/test/staging. **Meets** it by using the local Test/UAT stack machines
  (L-02) rather than provisioning anything; there is no Azure test machine to install onto
  and none is created.

Binding operator decisions:

- **D-002** (2026-08-23) — the production route is a **self-managed certificate** trusted
  in `LocalMachine\TrustedPeople`. The development route rehearses the same mechanism with
  a throwaway key. It is decided; do not write, plan or rehearse an Azure Artifact Signing
  or OV-certificate alternative — spikes `DSK-09-07` and `DSK-09-09` were withdrawn.
- **L-02** — Test/UAT is the local production-mimicking stack.

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in
the plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) →
  `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`,
  `microsoft/win-dev-skills` v0.5.0 `f1028dd5`, verified present — the path moves to
  `.agents/skills/vendor/windows/winui-packaging/` once [[TOOL-002]] lands).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`,
  `microsoft_docs_fetch`).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates REL-006`
  before every move; a move crosses at most one gated boundary. `get_doc_gates` reports
  exactly two gated boundaries: **`leave-preparing` needs `plan` (this document) **and**
  `questions-resolved`**, and **`enter-done` needs `proof` **and** `questions-resolved`**.
  `leave-backlog` is not a gated boundary for a `chore`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's twelve implementation steps in the same order, with the same
ownership. Steps marked **Operator step** are performed by the operator on a machine; an
agent prepares the command and the evidence template and records the result.

1. **Orient and take.** Read the area plan § 5 row `DSK-09-06`,
   `signing-and-hosting-decision-matrix.md` § D-002 "Chosen shape" (all seven bullets),
   and `runbooks.md` § R7 prerequisite 4. `get_doc_gates REL-006`, then
   `take_ticket REL-006`.
2. **Read the skill's tables in full** — `winui-packaging` § Quick Reference and its
   Troubleshooting table. Note before doing anything that the Quick Reference's
   "Trust certificate (admin) — `winapp cert install ./devcert.pfx`" row is the one this
   ticket does **not** follow.
3. **Fix the dev certificate subject to `Package.appxmanifest`'s `Identity/@Publisher`**
   — the stable placeholder CN established by [[FND-030]] (plan handle `DSK-02-05`).
   Generate with
   `winapp cert generate --manifest ./src/Pegasus.Desktop --if-exists skip` so the subject
   is auto-matched; `--manifest` is documented in the skill's step 2 as the flag that
   auto-matches `Publisher`, and a mismatch produces the `0x8007000B` packaging failure.
   Record the resulting subject string verbatim in this document — [[REL-007]]
   needs the same string for the production certificate.
4. **Git-ignore the private key — it is not covered today.** Verified on 2026-08-24:
   `git check-ignore -v devcert.pfx` exits `1` with no output, and `.gitignore` (77 lines)
   contains no `*.pfx` rule; only `**/artifacts/` and `/artifacts/` at `:20-21` would
   cover a key placed under `artifacts/`. So this step is a **real edit**, not a check:
   add `*.pfx` and `devcert.pfx` under the existing "Local secrets and environment state"
   block (`.gitignore:4-13`, which already holds `secrets.json`, `.env*` and
   `**/local.settings.json`). Then confirm with `git check-ignore -v devcert.pfx`, which
   must report the new rule.
5. **Export the public certificate only.**
   `Export-Certificate -Cert (Get-PfxCertificate ./devcert.pfx) -FilePath ./artifacts/devcert.cer -Type CERT`.
   Two things the command hides: `Get-PfxCertificate` **prompts** for the password on a
   protected `.pfx` unless PowerShell 7's `-Password` parameter is supplied, and the skill
   records the WinApp CLI's default PFX password as `password` (overridable with
   `--password`) — so pass it explicitly rather than letting an interactive prompt hang a
   scripted run. `artifacts/` is git-ignored (`.gitignore:20-21`, verified with
   `git check-ignore -v artifacts/devcert.cer` → `.gitignore:21:/artifacts/`), so the
   `.cer` cannot be committed by accident either. The `.pfx` never leaves the build
   machine.
6. **Write `eng/packaging/Install-DevCertificateTrust.ps1`.** `eng/` does not exist yet
   (`ls eng` returns nothing); this ticket may create `eng/packaging/` if [[REL-002]]
   (plan handle `DSK-09-02`) has not. Repository script header:
   `[CmdletBinding()]`, `Set-StrictMode -Version Latest`,
   `$ErrorActionPreference = 'Stop'`. Parameters `-CertificatePath` and
   `-ExpectedThumbprint`. Body: assert the session is elevated and `throw` a sentence
   naming the requirement if not (the shape `scripts/Test-PegasusPlatform.ps1:7-9` uses for
   its Windows precondition); run
   `Import-Certificate -FilePath <cer> -CertStoreLocation Cert:\LocalMachine\TrustedPeople`;
   then `certutil -verifystore TrustedPeople` and `throw` unless the expected thumbprint
   appears in the output. Use **`TrustedPeople`, never
   `Trusted Root Certification Authorities`** — and put that reason in a comment at the
   import line, where the next reader will be tempted to change it.
7. **Operator step — trust the machine before any package reaches it.** On the Test/UAT
   workstation VM (`docs/desktop/08-testing/test-uat-stack.md` § Machine prerequisites: a
   dedicated Windows 11 x64 VM, "not a developer's machine that holds a pilot install"),
   run the trust script elevated and hand back the `certutil -verifystore TrustedPeople`
   output showing the thumbprint. Trust goes on the machine **before** any package is
   copied to it.
8. **Build and package a dev-signed MSIX.**
   `winapp package ./src/Pegasus.Desktop/bin/x64/Release/<tfm>/ --cert ./devcert.pfx --self-contained` —
   the single-step `package --cert` form the skill's § Key Rules prefers over a separate
   `winapp sign`. Resolve `<tfm>` from the actual build output rather than hard-coding it.
9. **Operator step — install, launch and uninstall on the clean machine.**
   `Add-AppxPackage .\Pegasus_<ver>_x64.msix`; confirm with
   `Get-AppxPackage CollisionEngineers.Pegasus`; launch the app; then
   `Get-AppxPackage CollisionEngineers.Pegasus | Remove-AppxPackage` and confirm it is
   gone. Hand back the console transcript, not a summary of it.
10. **Operator step — prove the negative, on a second machine that has not been
    trusted.** Attempt `Add-AppxPackage` **first** and record the exact failure; expected
    `0x800B0109` (`CERT_E_UNTRUSTEDROOT`). Then run the trust script and retry
    successfully. Hand back both transcripts. This is the evidence that the ordering rule
    in R5 and R7 is real, and it is the single most valuable artefact this ticket
    produces.
11. **Write up the development certificate's renewal and revocation shape** in this
    document: it is a throwaway, regenerated freely, with no estate impact, and the
    production route is [[REL-007]] and is **not** interchangeable with
    it. State that explicitly so a later reader does not reuse the dev material.
12. **Simplification pass.** Record it under a dated `## Simplification pass` heading in
    this document (`AGENTS.md` § Repository task workflow step 4). If the diff is only the
    trust script and `.gitignore`, say exactly that rather than writing `n/a — docs-only`,
    which is false for this branch.

## Verification

Evidence tier from the body: **Tier 7**, applied here as the workstation-level evidence
tier the plan row assigns — the proof is an observed install, launch and uninstall on a
real clean Windows 11 machine, **not a script's return code**. `proof` is the operator
transcripts from steps 7, 9 and 10 as proof type `command-log`, plus the two local
`git check-ignore` results.

| Command / observation | Expected evidence |
| --- | --- |
| `certutil -verifystore TrustedPeople` on the test machine | the development certificate thumbprint is listed |
| `Add-AppxPackage .\Pegasus_<ver>_x64.msix` then `Get-AppxPackage CollisionEngineers.Pegasus` | install succeeds; the reported `Version` matches the build |
| `Get-AppxPackage CollisionEngineers.Pegasus | Remove-AppxPackage` | exit `0` and the package no longer listed |
| `git check-ignore -v devcert.pfx` | a matching `.gitignore` rule is reported — note it exits `1` with no output **before** step 4, which is why step 4 is an edit |
| `Add-AppxPackage` on the untrusted machine B, before the trust step | fails with `0x800B0109`; the retry after the trust step succeeds |

Behaviour to observe rather than infer: the app **launches** on the trusted machine, not
merely installs; and the untrusted machine's failure code is read from the console, not
assumed.

## Risks / open questions

- **Risk — `winapp cert install` is used out of habit.** It writes to the machine Trusted
  Root store and persists across reboots. Mitigation: step 6's comment at the import line,
  and the rule that it may only be used on a throwaway VM with the fact recorded.
- **Risk — a development certificate reaches a production workstation.** Mitigation: step
  11's explicit statement that the two routes are not interchangeable, and the fact that
  only `artifacts/devcert.cer` — a git-ignored path — ever holds the public dev
  certificate.
- **Risk — the `.pfx` is committed.** Verified real today: `.gitignore` has no `*.pfx`
  rule. Mitigation: step 4 is an edit, not a check, and the fourth verification command is
  the guard.
- **Risk — a scripted export hangs on a password prompt.** `Get-PfxCertificate` prompts
  interactively on a protected `.pfx`. Mitigation: step 5 passes the password explicitly.
- **Risk — the Test/UAT machine is a developer's own machine.**
  `docs/desktop/08-testing/test-uat-stack.md` § Machine prerequisites requires a dedicated,
  rebuildable VM "that will install and uninstall the package many times". Mitigation:
  named in step 7 and in the ticket's Guardrails.
- **Plan-set defect, recorded not opened — the dependency the plan row names.** The § 5
  row's `Depends on` is [[FND-035]] (plan handle `DSK-02-10`, "Single instance per Windows
  user: `AppInstance.FindOrRegisterForKey` and activation redirection"), which does not
  gate certificate work; the substantive prerequisite is
  [[FND-039]] (plan handle `DSK-02-14`, "Dev-certificate MSIX build and the
  install/uninstall packaging script"), because steps 8–10 need a packaged MSIX. **Who
  answers it**: the plan owner / operator. It is recorded here rather than silently
  re-pointed, as the body requires.

  **No `open-questions` document is created for it**, and the reason is not the one given
  in an earlier draft of this plan. That draft said "an unticked item would block every
  stage move", which is false: an unticked `- [ ]` line above `## Parked` blocks exactly
  `leave-preparing`, `enter-review` and `enter-done`, never `leave-backlog`, and a `chore`
  carries only the first and the last of those three. Blocking Preparing would have been
  affordable.

  The real reason stands on its own: **the answer changes no work in this ticket.** Steps
  3–6 (subject, `.gitignore`, export, trust script) can be completed and reviewed before any
  MSIX exists, and steps 8–10 need `FND-039`'s packaged MSIX whichever label the plan row
  carries. The substantive dependency is a **named sibling ticket**, which the authoring
  contract keeps in this section as a scope boundary rather than in an `open-questions`
  document; and the label itself is a correction to the plan set, not a decision anyone has
  to take before this ticket can proceed. Nothing in this ticket's body instructs that a
  question be recorded in `open-questions/`.
- **Open question, answered by default and recorded as taken** — which trust mechanism the
  Test/UAT machines used. The body's `## Documentation changes` asks for it to be recorded in
  `runbooks.md` § R7 so [[REL-007]] can compare it against the estate
  rollout. Default taken: the scripted elevated `Import-Certificate` of step 6, because the
  Test/UAT machines are standalone VMs and Group Policy Trusted People needs a
  domain-joined estate — a fact [[REL-007]] step 10 must establish rather than assume.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch adds a
script and edits `.gitignore`, so `n/a — docs-only` does not apply._
