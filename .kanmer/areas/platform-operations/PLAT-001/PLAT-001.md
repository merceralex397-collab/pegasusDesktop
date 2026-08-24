---
id: PLAT-001
type: ticket
title: DSK-10-01 · Threat → control → test register for the desktop conversion
status: preparing
area: platform-operations
assignee: ''
profile: chore
stageEntered:
  preparing: '2026-08-24T21:21:13.874Z'
labels:
  - desktop-conversion
  - plan-10
  - phase-8
  - tier-9
groups:
  - EPIC-011
  - HZN-009
links: []
docs_todo: true
archived: false
created: '2026-08-24T08:05:04.668Z'
updated: '2026-08-24T21:21:13.874Z'
---

## What

Author `docs/desktop/10-security-observability-performance/threat-register.md`: a living table with one row for each of the nine proposal §17.3 threats, naming the control that answers it (with a repository citation, or the plan ticket that builds it), the ticket that tests it, and the residual risk. The same file restates the §17.2 non-goals and holds the secret/PII pattern list that [[DSK-10-03]] and [[DSK-10-09]] both consume.

## Why

The Phase 8 exit gate (proposal §24 `:1885-1890`) demands that the "security review has no unresolved high-risk item". Today nothing joins a threat to a control to a test, so the security tickets in this epic have no traceable parent and a reviewer has nothing to check off. The register is also where §17.2 non-goals (obfuscation, anti-debugging, anti-tamper beyond signing, hiding API routes, licensing, marketplace hardening, multi-tenant isolation) are recorded as refused, so the scope creep listed in the plan's traps table is answered by reference instead of re-argued. Operator-visible consequence: without it no release candidate can be declared reviewed. Siblings that fill the Test column: [[DSK-10-03]], [[DSK-10-04]], [[DSK-10-05]], [[DSK-10-06]], [[DSK-10-07]], [[DSK-10-08]], [[DSK-10-09]].

## Source of truth

- Plan row: `docs/desktop/10-security-observability-performance/README.md` § 5 — `DSK-10-01`
- Plan detail: same file § 2 (Facts — the controls that already exist), § 3 (decisions and deviations), § 4 (target state and exit gate), § 7 (risks and traps)
- Proposal: `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md` § 17 Security and privacy — §17.1 required controls `:1153-1172`, §17.2 controls intentionally not prioritized `:1174-1182`, §17.3 threat model focus `:1184-1198`; § 22.2 Security tests `:1608-1621`
- Repository evidence:
  - `src/Pegasus.Web/Program.cs:158-171` — managed identity only (`DefaultAzureCredential` with every other source excluded)
  - `src/Pegasus.Web/Program.cs:172-176` — Data Protection keys in blob `authentication-ring/keys.xml`
  - `src/Pegasus.Web/Program.cs:262-327`, `src/Pegasus.Web/Pages/Account/SignIn.cshtml.cs:63` — Identity with rate limiting instead of lockout (`lockoutOnFailure: false`, ADR-0013)
  - `src/Pegasus.Web/Program.cs:368-457`, `:353` — `__Host-Pegasus` cookie, 2 h idle / 8 h absolute, `SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero`
  - `src/Pegasus.Web/Program.cs:517-522` — fallback policy `RequireAuthenticatedUser()`
  - `src/Pegasus.Web/Program.cs:758-764` — HSTS, CSP `default-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'`, `nosniff`
  - `src/Pegasus.Core/Identity/IdentityContracts.cs:98-137` — `SecurityEvent`, `ActionHistoryEntry`, `ISecurityEventWriter`, `IActionHistoryWriter`
  - `src/Pegasus.Core/Identity/StaffAuthorization.cs:1-40` — the `StaffAccessRight` boundary that fails closed
  - `src/Pegasus.Core/Intake/IntakeContracts.cs:7-57` — `IntakeEnvelopeLimits` (10 MiB per file, 20 files, 64 KiB multipart overhead)
  - `src/Pegasus.Web/appsettings.json:8-14` — the committed bootstrap verification account ([[DSK-10-02]])
  - `scripts/Test-MigrationGrants.ps1:1-60` and `src/Pegasus.Infrastructure/Persistence/Migrations/20260729199000_RuntimeRoleReconciliation.cs:1-20` — least-privilege runtime roles ([[DSK-10-18]])
  - `docs/current-architecture.md:160-183` — PLAT-034 (telemetry blind window) and PLAT-035 (ungranted writes)
- Binding decisions:
  - **L-02** — Test/UAT is a local production-mimicking stack; ADR-0014 stands, so no row may propose an Azure test resource.
  - **D-002** (2026-08-23) — production signing uses a self-managed certificate trusted per workstation in `LocalMachine\TrustedPeople`; the private key is an asset this register must carry a row for.
  - **D-003** (2026-08-23) — the update feed is an in-house UNC share over SMB; the "compromised update package/feed" row is about SMB ACLs and signature validation, not public HTTPS.
  - **ADR-0109** (to be authored) — desktop diagnostics bundle plus the existing Application Insights; no new telemetry fleet.
- Depends on: None.

## Routing

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-code-review` (`.codex/skills/winui-code-review/SKILL.md`; win-dev-skills v0.5.0 `f1028dd5`, moves to `.agents/skills/vendor/winui/winui-code-review/` when DSK-12-02 lands) — use its security checklist section → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`). No Azure MCP call is needed.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (leave-preparing needs `plan` + `questions-resolved`; enter-done needs `proof` + `questions-resolved`; call `get_doc_gates <id>` before every move, and cross at most one gated boundary per move)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Implementation steps

1. Orientation. Read the plan row and §§ 2, 3, 4 and 7 of `docs/desktop/10-security-observability-performance/README.md`, then proposal `:1149-1198` and `:1608-1621`. Call Kanmer `get_doc_gates` with this ticket's board id, then `take_ticket`. Confirm the gates printed match the `chore` profile above; `board.yml` is never the authority.
2. Create the branch from `dev` as `task/dsk-10-01-threat-register` (`docs/engineering.md` § Branches and delivery). Never branch from `main`.
3. Create `docs/desktop/10-security-observability-performance/threat-register.md` with an H1, a one-paragraph scope note, and a table whose header is exactly: `| # | Threat (§17.3) | Control | Where the control lives | Test | Residual risk / owner |`.
4. Add exactly these nine threat rows, in proposal order: lost or shared workstation session; leaked service credential; accidental over-permission; malicious or malformed attachment; duplicate or conflicting data writes; compromised update package/feed; sensitive information in logs or temp files; third-party provider outage; administrator error.
5. Fill the `Control` and `Where the control lives` columns from the Source of truth citations above — an existing control cites `path:line`; a planned control cites the plan ticket that builds it (for example the DPAPI refresh-token store is `DSK-04-07`, the signed package and trusted manifest are `DSK-09-08`/`DSK-09-03`, the concurrency token is `DSK-03-08`). Never write a control with no citation.
6. Fill the `Test` column with the ticket handle that exercises it: tokens/session → [[DSK-10-04]]; authorization and direct-object → [[DSK-10-05]]; malformed upload and unsafe path → [[DSK-10-06]]; temp files and cache ACLs → [[DSK-10-07]]; package/config/log secret scan → [[DSK-10-03]]; dependency vulnerabilities → [[DSK-10-08]]; log redaction → [[DSK-10-09]]; provider outage taxonomy → [[DSK-10-17]]; administrator error (audit trail) → [[DSK-10-15]] and `DSK-03-15`. A row with no test is a defect: raise it as an open question rather than leaving the cell blank.
7. Add a `## Not prioritised (§17.2)` section listing the seven non-goals verbatim from proposal `:1176-1182`, with the sentence that a ticket proposing any of them is out of scope without a new accepted decision.
8. Add a `## Secret and PII pattern list` section holding the regular expressions the scanners use — at minimum: `Server=tcp:`/`Initial Catalog=` SQL connection strings, `https://*.vault.azure.net/secrets/`, `InstrumentationKey=`/`APPLICATIONINSIGHTS_CONNECTION_STRING`, `Bearer eyJ` JWTs, `client_secret`, `AccountKey=`, `-----BEGIN * PRIVATE KEY-----`, and the literal password string in `src/Pegasus.Web/appsettings.json:12`. State that [[DSK-10-03]] and [[DSK-10-09]] read this section and that a change here must be reflected in both.
9. Add a `## Certificate and key custody` row set for D-002: signing key location on the signing host, its ACL, that it is never a GitHub secret, and the pointer to runbook R5 (`DSK-09-14`) for renewal and the compromise variant.
10. Link the new file from `docs/desktop/10-security-observability-performance/README.md` § 8 (Documentation changes) so the documentation-link test can reach it, and from `docs/desktop/README.md` only if that index already lists per-area detail files — do not add a second index.
11. Run `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` and `pwsh ./scripts/Test-DocumentationLinks.ps1`. Both must exit 0. The new file is inside `docs/desktop/`, which the placement gate allows; anywhere else fails the CI `documentation` job.
12. Record `## Simplification pass` in the ticket's `plan` document as `n/a — docs-only` with today's date, then open the PR into `dev` and request review from `pegasus-desktop-reviewer`.

## Acceptance criteria

- [ ] Every one of the nine §17.3 threats has a row, and every row names a control with a `path:line` citation or a plan ticket handle.
- [ ] Every row names a test ticket handle; no cell reads "TBD".
- [ ] The §17.2 non-goals are listed verbatim with the out-of-scope sentence.
- [ ] The secret/PII pattern list exists in this file and is referenced by name from [[DSK-10-03]] and [[DSK-10-09]] when those tickets are planned.
- [ ] The certificate/key custody rows reflect D-002 (self-managed certificate, in-house key) and D-003 (UNC/SMB feed), not a cloud signing service.
- [ ] `pegasus-desktop-reviewer` has reviewed the file and recorded no unresolved high-risk gap.

## Verification

- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exits 0, no broken link reported for the new file.
- [ ] `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected: exits 0 (the file sits under an allowed root).
- [ ] Reviewer record in the ticket's `post-implementation-report`: nine rows, nine controls, nine tests, reviewer name and date.

## Evidence tier

Tier 9 — Security/observability (`docs/engineering.md` § Required evidence tiers). Here that obliges a documented control-to-test mapping rather than executed tests: the register is the index the tier-9 tickets in this epic are graded against, and it must cite real code or a real ticket for every claim.

## Documentation changes

- `docs/desktop/10-security-observability-performance/threat-register.md` — new file (the register itself).
- `docs/desktop/10-security-observability-performance/README.md` § 8 — add the register to the documentation-changes list.

## Guardrails

- **Azure**: no write. No Azure MCP call is required; if one is used it must be a read-only tool (`group_resource_list`, `monitor`, `applicationinsights`).
- **Scope boundary**: documentation only. This ticket may not edit `src/`, `tests/`, `infra/`, `scripts/` or `.github/`. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`) alongside plan 11.
- **Traps**: scope creep into obfuscation, anti-tamper and licensing — §17.2 non-goals are restated here precisely to refuse it; secrets leaking through logs or the diagnostics bundle — the pattern list is the shared answer; the plaintext verification account must appear as a live row until [[DSK-10-02]] ships.
- **Markdown placement**: any new `.md` outside `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job. Ticket-transient notes live in Kanmer, not the tree.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document (`n/a — docs-only` here).

## Outcome

_Filled at closeout._
