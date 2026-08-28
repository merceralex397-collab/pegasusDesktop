# Plan — PLAT-001

## Objective

Author `docs/desktop/10-security-observability-performance/threat-register.md`: a living table with one row for each of the nine proposal §17.3 threats, naming the control that answers it (with a repository citation, or the plan ticket that builds it), the ticket that tests it, and the residual risk. The same file restates the §17.2 non-goals and holds the secret/PII pattern list that [[DSK-10-03]] and [[DSK-10-09]] both consume.

## Chosen approach

The Phase 8 exit gate (proposal §24 `:1885-1890`) demands that the "security review has no unresolved high-risk item". Today nothing joins a threat to a control to a test, so the security tickets in this epic have no traceable parent and a reviewer has nothing to check off. The register is also where §17.2 non-goals (obfuscation, anti-debugging, anti-tamper beyond signing, hiding API routes, licensing, marketplace hardening, multi-tenant isolation) are recorded as refused, so the scope creep listed in the plan's traps table is answered by reference instead of re-argued. Operator-visible consequence: without it no release candidate can be declared reviewed. Siblings that fill the Test column: [[DSK-10-03]], [[DSK-10-04]], [[DSK-10-05]], [[DSK-10-06]], [[DSK-10-07]], [[DSK-10-08]], [[DSK-10-09]].

## Governing docs

- No canonical PRD/FRD/ADR is linked yet. `docs_todo: true` is intentionally retained: several desktop conversion decisions named by the ticket are planned canonical documents and must not be linked until they exist on `origin/dev`.
- Use the ticket's Source of truth and the owning desktop-area plan as the current planning authority; add a real governing-doc ref only through `link_doc` after the file exists.

## Routing

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
- **Skills**, loaded in this order: `pegasus-desktop` (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-code-review` (`.codex/skills/winui-code-review/SKILL.md`; win-dev-skills v0.5.0 `f1028dd5`, moves to `.agents/skills/vendor/winui/winui-code-review/` when DSK-12-02 lands) — use its security checklist section → `kanmer-plan` (`.grok/skills/kanmer-plan/SKILL.md`)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`, `append_scratch`, `move_item`). No Azure MCP call is needed.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (leave-preparing needs `plan` + `questions-resolved`; enter-done needs `proof` + `questions-resolved`; call `get_doc_gates <id>` before every move, and cross at most one gated boundary per move)
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement (`AGENTS.md` § Repository task workflow step 5)

## Ordered implementation

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

## Verification

- [ ] `pwsh ./scripts/Test-DocumentationLinks.ps1` — expected: exits 0, no broken link reported for the new file.
- [ ] `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — expected: exits 0 (the file sits under an allowed root).
- [ ] Reviewer record in the ticket's `post-implementation-report`: nine rows, nine controls, nine tests, reviewer name and date.

## Risks and constraints

- **Azure**: no write. No Azure MCP call is required; if one is used it must be a read-only tool (`group_resource_list`, `monitor`, `applicationinsights`).
- **Scope boundary**: documentation only. This ticket may not edit `src/`, `tests/`, `infra/`, `scripts/` or `.github/`. Board placement note: plan 00 § Kanmer board shape assigns no board area to plan 10, so this epic seeds into `platform-operations` (prefix `PLAT`) alongside plan 11.
- **Traps**: scope creep into obfuscation, anti-tamper and licensing — §17.2 non-goals are restated here precisely to refuse it; secrets leaking through logs or the diagnostics bundle — the pattern list is the shared answer; the plaintext verification account must appear as a live row until [[DSK-10-02]] ships.
- **Markdown placement**: any new `.md` outside `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job. Ticket-transient notes live in Kanmer, not the tree.
- **Simplification pass** (`AGENTS.md` step 4): required over this branch diff before the PR, recorded under a dated `## Simplification pass` heading in the ticket's `plan` document (`n/a — docs-only` here).

## Simplification pass

Before the PR, independently review the branch diff for reuse, unnecessary abstraction, duplicated policy, and scope expansion; record findings and dispositions here.

## Execution evidence — 2026-08-25

- Added `docs/desktop/10-security-observability-performance/threat-register.md` with the exact proposal §17.3 nine-threat order, one cited control and one test ticket per row, the verbatim §17.2 non-goals, the shared secret/PII pattern list, and D-002/D-003 certificate/feed custody rows.
- Updated the existing area README §8 to link the register; no other repository area or governance file changed.
- Structural audit: 9 threat rows, 9 rows with test references, required non-goal/pattern markers all present, and the exact table header present.
- `pwsh ./scripts/Test-DocumentationLinks.ps1` — passed; all 232 Markdown files checked.
- `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` — passed.
- `git diff --check` — passed.

## Simplification pass — 2026-08-25

- `n/a — docs-only` for implementation simplification: the ticket required one canonical register and one existing README link, so no code abstraction, helper, duplicate policy, or additional index was introduced.
- Reused the area README's existing Documentation changes section and cited existing controls/ticket owners rather than copying policy into new documents.
- Scope check: only the new register and the single README link are changed; no unresolved simplification finding remains.

## Current status

Documentation implementation and local validation are complete. Commit, independent review, PR/CI, merge to `dev`, proof on merged `main`, and Kanmer closeout remain.

## Review disposition — 2026-08-25

Independent `pegasus-desktop-reviewer` review returned `FAIL` on commit `337fba1e`. Findings and dispositions:

- High: feed-compromise row used the secret-scan ticket — corrected to `[[DSK-10-05]]`, the package/manifest tampering test; `DSK-10-03` remains the shared secret-scan owner.
- High: the register must not duplicate the committed bootstrap password. Replaced the path-only wording with an explicit scan-time source-value rule that reads the exact JSON value without copying the secret into documentation; the rule ends when [[DSK-10-02]] removes the account.
- Medium: attachment row now cites [[DSK-10-06]] for safe paths/content validation instead of attributing those claims to limits-only references.
- Medium: logging and provider rows now cite their owning tickets directly, removing generic area README line references.
- Medium: the README entry is now a real `[Threat register](threat-register.md)` link.
- The required `post-implementation-report` has been added with the review record and validation; a fresh independent review is still required before merge.

Post-fix validation: `Test-DocumentationLinks.ps1` passed with 233 files; `Test-TestMarkdownPlacement.ps1` passed; structural audit remains 9/9 threat rows, 9/9 test references, all required markers present; `git diff --check` passed.

## Review and delivery status — 2026-08-25

- Fresh independent `pegasus-desktop-reviewer` review of commit `79670d21` returned `PASS`: no unresolved findings; exact nine threats, seven non-goals, corrected citations, scan-time password handling, real README link, D-002/D-003 custody, post-report, and scope were verified.
- `gh pr create --base dev --head plat-001-threat-register` failed with the exact repository permission error: `pull request create failed: GraphQL: must be a collaborator (createPullRequest)`.
- Ticket remains `implementing`; no PR, CI, merge, merged-main proof, or Kanmer closeout is claimed. Next action is repository collaborator permission, then PR/CI/merge and proof on merged `main`.

## Delivery update — 2026-08-28

- The historical PR-creation permission failure is superseded: PR #35 is merged into `dev`; exact implementation head `b83f48296df1f1680563c9bbf0e0af6e70a7133b`, merge commit `76592d4666a41eeeddd4d993c135bd9a3bc56918`.
- Curie’s independent review of exact head `b83f4829` returned PASS with no unresolved findings. PR CI run `33134297925` completed success for all applicable documentation/reference-data/local-development-scripts/changes jobs; build/UI/SQL jobs were correctly skipped for the docs-only diff.
- The final exact main head containing this ticket and DUI-017 is `5f7b85a2a8fb32102b859cad559dec33a14872fd`; main push CI run `33134998958` completed success. Proof and Kanmer closeout are now pending.
