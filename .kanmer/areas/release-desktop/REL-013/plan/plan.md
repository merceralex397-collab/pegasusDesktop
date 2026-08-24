# Plan — REL-013: DSK-09-15 · First-install onboarding guide R7 (operator one-pager)

**Diff estimate: ~2 files, ~90 lines.** One new page
`docs/desktop/09-release-update-and-distribution/first-install.md` (~80 lines: prerequisites,
the certificate-trust step, four install steps, the merged startup/blocked-state/diagnostics
sections, uninstall/reinstall and channel switch, the six-item operator skeleton, the
managed-device support line and the SmartScreen line), and ~8 lines in `runbooks.md` § R7 —
the link to the one-pager and the expected managed-device policy state, **and nothing else**.
`docs/engineering.md:201-207` § Plan sizing requires the estimate first; the ceiling here is
the deliverable's own definition, which is *one page*.

## Approach

**One page, at one path, in operator vocabulary, with the certificate-trust step first.**
The install has exactly one step that will otherwise fail for everyone: D-002's self-managed
certificate must be trusted on the machine **before** the package arrives, or the install
fails with `0x800B0109` — a code no operator can act on. So it is prerequisite number one on
the page, with the one actionable retry sentence beside it, rather than a footnote.

Two alternatives are rejected, and both are rejected by the ticket body rather than by this
plan. **A second guide** — in particular
`docs/desktop/04-auth-session-update-and-startup/first-run-guide.md` — is a stop condition:
`DSK-04-13` (board `FND-049`, "Contribute the startup, blocked-state and diagnostics content
to the workstation first-install guide") contributes into this page and authors nothing of
its own. Verified on 2026-08-24: `docs/desktop/04-auth-session-update-and-startup/` contains
only `README.md`, so no second guide exists yet and none may be created. **Re-deriving
`DSK-04-13`'s sections here** is the other rejected path: its content is merged **verbatim**,
because it is the ticket that reproduced the blocked states on a clean machine and verified
the refresh-token-on-uninstall behaviour.

The page explains nothing. `docs/design/README.md` is design authority for this repository —
"an operational, restrained, desktop-first internal case-management tool for a small office
of approximately eight users" — and a page that explains how updates work is a defect under
it, not a kindness.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-013`). No existing PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by `DSK-09-01` (board `REL-001`). This page is the operator
> face of its Consequences: trust before the package, updates on launch, a LAN/VPN-only feed.
> This plan is written to the decisions as recorded in
> `docs/desktop/09-release-update-and-distribution/runbooks.md` § R7 and
> `signing-and-hosting-decision-matrix.md` § D-002; if ADR-0105 lands differently, this plan
> is revised before implementation.

Existing documents this plan **meets**:

- **`docs/design/README.md`** — design authority, binding on every operator-facing text via
  `AGENTS.md` § Simplicity rails. **Meets**: the page carries operator vocabulary only, no
  how-it-works copy beyond the necessary steps, and the acceptance criterion is an
  independent walkthrough rather than the author's own read-through.
- **`AGENTS.md` § New Markdown placement** — any `.md` outside
  `docs/(prd|frd|adr|design|desktop)` fails the CI `documentation` job. **Meets**: the page is
  created under `docs/desktop/09-release-update-and-distribution/`, which
  `scripts/Test-MarkdownPlacement.ps1:31`'s allowed-roots regex admits, and the CI job at
  `.github/workflows/ci.yml:82-87` runs `Test-TestMarkdownPlacement.ps1` and
  `Test-DocumentationLinks.ps1` on every change set.

Binding operator decisions, written to as settled:

- **D-002** (2026-08-23) — the certificate-trust step is **always required** on every
  workstation, before the package.
- **D-003** (2026-08-23) — the install source is a **UNC path**, not a web download, and
  updates work on the office network or VPN only. Say that once, in operator terms, without
  explaining SMB.
- **C-01** — nothing in the guide may point at a GitHub download.

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in the
plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) → `kanmer-docs`
  (`.grok/skills/kanmer-docs/SKILL.md`, Kanmer 0.1.0).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates REL-013` before
  every move; a move crosses at most one gated boundary. `get_doc_gates` reports two gated
  boundaries: `leave-preparing` needs `plan` (this document), `enter-done` needs `proof`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5); the plan row names this review explicitly.

## Steps

These refine the body's thirteen implementation steps in the same order, with the same
ownership and the same paths. Step 12 is an **Operator step**.

1. **Orient and take.** Read `runbooks.md` § R7 **in full** — prerequisites 1–5, steps 1–4,
   uninstall/reinstall, switching channel and the one-page skeleton at the end of the section
   — then `docs/design/README.md` for the operator-copy rules.
   `get_doc_gates REL-013`, then `take_ticket REL-013`.
2. **Collect `DSK-04-13`'s contribution before writing.** The ownership split is decided and
   is not reopened: one first-install guide, owned here, at
   `docs/desktop/09-release-update-and-distribution/first-install.md`; `DSK-04-13` (board
   `FND-049`) contributes the startup sequence, the blocked states and the diagnostics
   location into it and authors no guide of its own. Two cases: the contribution is already
   merged into this page, or — the likely one, since `DSK-04-13` is phase 2 and this ticket
   is phase 9 — it is recorded in `DSK-04-13`'s plan document beside R7 for this ticket to
   merge **verbatim**. Record in this document which case applied. **A second guide at any
   path, or this page re-deriving what `DSK-04-13` contributes, is a stop condition.**
   Verified today: `docs/desktop/04-auth-session-update-and-startup/` contains only
   `README.md`, so no second guide exists yet.
3. **Create the page** at `docs/desktop/09-release-update-and-distribution/first-install.md`
   — inside an allowed Markdown root, so the CI `documentation` job passes. Not
   `docs/guides/`, not the repository root.
4. **Prerequisites, in operator vocabulary**, from R7's checklist: Windows 11 x64 (24H2
   recommended) with current updates, signed in as the person who will use Pegasus (per-user
   install, no administrator needed once the signing chain is trusted); Microsoft Edge
   WebView2 runtime present (default on Windows 11; check in Settings → Apps); network access
   to the Pegasus files location and to Pegasus itself. Keep it to the checklist — the
   existing platform prerequisites at `docs/runbook.md:19-75` § Supported platform are
   **extended, not restated**.
5. **Make the certificate-trust step prerequisite number one on the page**, not buried:
   import the public `.cer` into `Cert:\LocalMachine\TrustedPeople` (elevated, once per
   machine) or let Group Policy deliver it, then verify with
   `certutil -verifystore TrustedPeople`. Add the single operator-actionable sentence: **if
   the app was installed before this step it fails — do this step, then install again.** Do
   not print the error code as the headline; print the action.
6. **The four steps from R7**: open the channel's `.appinstaller` from the Pegasus files
   location and choose Install; sign in with the existing Pegasus account on first launch;
   check Settings/Diagnostics shows the version, channel and Pegasus address; record the
   workstation in the support register (user, machine, channel, date).
7. **Merge `DSK-04-13`'s contribution verbatim and unedited** as the page's startup,
   blocked-state and diagnostics sections: the three-sentence startup sequence in operator
   words (on launch the app checks for updates, then checks compatibility with Pegasus, then
   shows Login); each blocked state named **exactly** as
   `docs/desktop/06-ui-design/screen-specs.md` § *Update required / Blocked* names it —
   verified today at `screen-specs.md:99-107`: title "Update required", primary "Update now",
   secondary "Sign out", and the Blocked variant showing the operator sentence and "Sign out"
   only — with the one action the operator takes for each; and where the rolling logs live
   plus how to run the Export diagnostics command from `DSK-02-11` (board `FND-036`). Say what
   to do; do not explain the startup order, do not add a state the screen spec does not
   define, and **do not re-derive the sections** — if the contribution is missing, stop and
   raise it on `DSK-04-13` (board `FND-049`) rather than writing them here.
8. **Uninstall/reinstall and channel switch**, from R7: Settings → Apps → Pegasus →
   Uninstall, then repeat the install steps; switching channel is uninstall then install from
   the other channel's `.appinstaller`. Take the **verified** answer to R7's deferred
   refresh-token-on-uninstall item from `DSK-04-13` (board `FND-049`), which proves it on a
   real machine, rather than stating the intended behaviour.
9. **Close with the six-item skeleton exactly as the plan words it** — operator vocabulary
   only, no how-it-works copy: **Install · Sign in · Pegasus updates itself on launch · If
   you see "Update required", close and reopen · If you see "Cannot reach Pegasus", check the
   network and retry · Export diagnostics from Settings when asked by support.**
10. **The managed-device note as a support-facing line, not user copy**: `ms-appinstaller:`
    stays disabled (it is not needed) and App Installer auto-update must not be disabled by
    policy — support checks `Get-AppxPackageAutoUpdateSettings` after install. Record the
    expected policy state for the estate. In `runbooks.md` § R7 this ticket records **only**
    the link to the one-pager and that expected policy state; the R7 steps `DSK-04-13`'s
    clean-machine reproduction proved wrong, and its verified refresh-token answer, are
    `DSK-04-13`'s to record, so the two tickets never write the same R7 line.
11. **One honest expectation-setting line about SmartScreen**: a warning may appear on the
    first download of a new version; it is expected in an in-house estate and is not a signing
    failure.
12. **Operator step — the independent walkthrough.** A pilot user who did **not** write the
    page follows it end to end on a clean or reset workstation, and hands back where they
    hesitated. Fix the page against what actually confused them. **That feedback is the
    acceptance evidence, not the author's own read-through.**
13. **Run the gates and request review.** `pwsh ./scripts/Test-DocumentationLinks.ps1` and
    `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`, both exit `0`. Note the second name is
    correct: it is the script `.github/workflows/ci.yml:83` runs, and it exercises
    `scripts/Test-MarkdownPlacement.ps1`, whose allowed-roots regex at `:31` admits
    `docs/desktop/`. Request review by `pegasus-desktop-reviewer` and record the dated
    `## Simplification pass` (`n/a — docs-only`).

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture**, as the plan row assigns.
The obligation is a **placed, link-checked page plus the recorded independent walkthrough**;
install mechanics themselves are proven by `DSK-09-08` (board `REL-007`) and `DSK-09-11`
(board `REL-009`). `proof` is the gate output as `command-log` plus the walkthrough notes.

| Command / observation | Expected evidence |
| --- | --- |
| `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` | exit code `0` |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exit code `0` |
| `grep -n "ms-appinstaller:\|github.com" docs/desktop/09-release-update-and-distribution/first-install.md` | no match |
| `ls docs/desktop/04-auth-session-update-and-startup/first-run-guide.md` | no such file — verified absent today; and `grep -rln "first-install\|first-run guide" docs/desktop/` names exactly one guide page |
| Pilot-user walkthrough on a clean Windows 11 workstation | the app reaches the login screen **without the author's help**; the hesitation points are recorded and addressed |

Behaviours to read rather than infer: the page fits on one page; the certificate-trust step
appears **before** the install step; each blocked state's name matches
`docs/desktop/06-ui-design/screen-specs.md:99-107` word for word; and the startup,
blocked-state and diagnostics sections are `DSK-04-13`'s text, not a paraphrase of it.

## Risks / open questions

- **Risk — a second first-install guide.** A stop condition. Mitigation: step 2 records which
  case applied, and the fourth verification command asserts exactly one guide page exists in
  `docs/desktop/`.
- **Risk — `DSK-04-13`'s contribution is missing when this ticket is worked.** It is phase 2
  and this ticket is phase 9, so it should exist — but if it does not, **stop and raise it on
  `DSK-04-13` (board `FND-049`)** rather than writing the sections here. Re-deriving them is
  the other stop condition.
- **Risk — the page explains.** A page that "explains" is a defect under
  `docs/design/README.md`. Mitigation: the six-item skeleton at step 9 is the tone anchor, and
  the reviewer named in the plan row checks it.
- **Risk — the blocked-state names drift from the screen.** Mitigation: step 7 requires them
  to match `screen-specs.md:99-107` exactly, and that file is area 06's — the page follows the
  screen, never the other way round.
- **Risk — the error code becomes the headline.** `0x800B0109` is not actionable by an
  operator. Mitigation: step 5 prints the action ("do this step, then install again"), and
  leaves the code to the support-facing runbook.
- **Risk — a GitHub link or an `ms-appinstaller:` protocol link.** Both excluded — the first
  permanently by C-01, the second because the protocol has been disabled by default since
  December 2023. Mitigation: the third verification command.
- **Risk — the author validates their own page.** Mitigation: step 12's independent
  walkthrough is the acceptance evidence.
- **Open questions**: none. The ownership split is decided by the body and is not reopened;
  the refresh-token-on-uninstall answer comes from `DSK-04-13` (board `FND-049`), which proves
  it on a real machine, rather than being guessed here. **No `open-questions` document is
  created.**

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch is
documentation-only, so the expected record is `n/a — docs-only`._
