# Plan — FND-049: startup, blocked-state and diagnostics content for the workstation first-install guide

**Diff estimate: ~1 file, ~14 lines** (Case B, the likely case — see the
inventory). Case A is ~2 files, ~60 lines.

The number is small because this ticket **authors no guide**. Its output is
three things: a contribution recorded for [[REL-013]] (plan handle
`DSK-09-15`) to merge, a verified answer to a question `runbooks.md` § R7
defers, and a clean-machine reproduction by someone other than the author. Only
the last two touch the repository.

## Measured file-and-line inventory

Profile `chore` owes no `research` and no `files` document, so this plan
carries the surface-area burden alone —
`.grok/skills/kanmer-plan/assets/plan-template.md`'s "written FROM the ticket's
`research` and `files` documents" precondition does not apply to `chore`. Every
row was measured against the fork working tree on 2026-08-24 with `wc -l`,
`sed -n`, `ls` and `grep -n`.

**Case resolution, measured today:**
`ls docs/desktop/09-release-update-and-distribution/` returns exactly
`README.md`, `appinstaller-template.md`, `runbooks.md`,
`signing-and-hosting-decision-matrix.md` — **`first-install.md` does not
exist**. `find docs -name "first-install*" -o -name "first-run*"` returns
nothing, and `ls docs/desktop/04-auth-session-update-and-startup/` returns only
`README.md`. So **Case B applies as of 2026-08-24**; re-check at execution,
because [[REL-013]] may have landed by then.

### Case B — [[REL-013]] has not landed (measured today, and the body's likely case)

| Path | Measured now | Change | Lines |
| --- | --- | --- | --- |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` | **352 lines.** § R7 runs `:221-271`; prerequisites `:227-243` (the certificate-trust item is `:234-240`); steps `:245-255`; the uninstall/reinstall paragraph `:257-261`, whose **`:260-261` reads "credential store cleanup is part of uninstall behaviour *to verify in area 04*"** — that clause is this ticket's item; channel switch `:263-264`; the one-page skeleton `:266-270`. § R8 begins `:272`. | **Edit.** Replace the deferred clause at `:260-261` with the verified answer from step 6, and correct whichever of `:245-255` the clean-machine reproduction proved wrong. Nothing else. | +14 |
| *(this plan document)* | — | The startup, blocked-state and diagnostics contribution is recorded **here**, under the dated heading step 2 requires, as the text [[REL-013]] merges **verbatim**. Not a repository file, so it is not in the diff. | 0 |

**Sum: 1 file, ~14 lines.**

### Case A — [[REL-013]] has landed

| Path | Change | Lines |
| --- | --- | --- |
| `docs/desktop/09-release-update-and-distribution/first-install.md` | **Edit, in place.** Add the startup, blocked-state and diagnostics sections. Change no existing section; restate none of [[REL-013]]'s prerequisites, certificate-trust step, install steps, uninstall/channel-switch paragraphs or six-item skeleton. | +45 |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` | As Case B | +14 |

**Sum: 2 files, ~59 lines.**

### Measured and deliberately not touched

| Path | Measured now | Why not |
| --- | --- | --- |
| `docs/desktop/04-auth-session-update-and-startup/first-run-guide.md` | **Does not exist** (`ls` returns only `README.md` in that folder) | A second first-install guide at any path is a **stop condition** (body Traps). This ticket must leave that `ls` result unchanged. |
| `docs/runbook.md` § Supported platform | Section `:19-38` in a 1254-line file | Extended, not restated (body § Source of truth). The `winapp`/Developer Mode paragraph is [[FND-039]]'s (plan handle `DSK-02-14`). |
| `src/`, `tests/`, `scripts/`, `.github/` | — | Guardrails: documentation only. A product defect found by the reproduction becomes a separate `fix` ticket. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R7 one-pager link and managed-device policy state (`:266-270` and `:241-243`) | — | [[REL-013]] step 10 records exactly those two, "so the two tickets never write the same R7 line". |

## Approach

**Contribute into one page, and let a stranger prove it.** The content is
three short sections — the startup sequence in operator words, each blocked
state named exactly as the screen spec names it with the one action for each,
and where the diagnostics live — written once and merged into the single
first-install guide [[REL-013]] owns. The acceptance evidence is not the text;
it is a person who did not write it reaching the login screen on a clean
Windows 11 machine from a state with no certificate trusted and no package
installed, with every place they had to guess recorded and fixed.

The alternative rejected is **writing this ticket's own guide** — the obvious
move for a phase-2 ticket whose owner-page is phase 9, and the one earlier
drafts of this ticket took, naming
`docs/desktop/04-auth-session-update-and-startup/first-run-guide.md`. It is
rejected by the body in as many words: two first-install guides at two paths is
worse than one late one, because the second one is the one an operator finds
and the first one is the one that gets maintained. Recording the contribution
in this plan document instead costs one merge step in [[REL-013]] and
guarantees the repository never holds two. [[REL-013]]'s own plan step 2
already expects it there — "it is recorded in `DSK-04-13`'s plan document
beside R7 for this ticket to merge **verbatim**" — so the handshake exists on
both sides.

The second alternative rejected is **documenting the intended
refresh-token-on-uninstall behaviour** rather than verifying it. `runbooks.md`
`:260-261` currently asserts the token is removed *and* flags it "to verify in
area 04" in the same sentence. Copying the assertion forward would close the
flag without answering it; step 6 verifies it on a real machine and, if the
token survives, records the true behaviour and raises a `fix` ticket.

## Governing docs

The ticket's `refs` list is **empty** and `get_doc_gates FND-049` reports
`docs_todo: true`. No existing PRD or FRD is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a
> gateway minimum-version gate), authored by [[REL-001]] (plan handle
> `DSK-09-01`); [[FND-005]] (plan handle `DSK-00-05`) and [[FND-042]] (plan
> handle `DSK-04-01`) also claim ADR-0105 — see [[REL-001]]'s plan for the
> ownership reconciliation. This contribution is the operator face of its
> consequences: trust before the package, updates on launch, a LAN/VPN-only
> feed.
> This plan is written to the decisions as recorded in
> `docs/desktop/09-release-update-and-distribution/runbooks.md` § R7 and
> `signing-and-hosting-decision-matrix.md` § D-002; if ADR-0105 lands
> differently this plan is revised before implementation.

Existing repository documents this plan **meets**:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `docs/design/README.md:433-438` § No explanatory copy | "**No how-it-works copy.** A page never describes its own mechanics, workings, derivations, or what will happen when a button is pressed… no introductory sentences under headings." | Steps 3–5 — three sentences for startup, one action per blocked state, one location for diagnostics; nothing explains how any of it works |
| `docs/design/README.md:398` § Voice, labels and necessary copy | "Guidance is appropriate only when an operator must understand a consequence" | Step 5's one consequence sentence; no others |
| `docs/design/README.md:412-421` | The banned-words list for operator-facing copy — `intake`, `bounded`, `durable`, `correlation identifier`, `artifact`, `caller`, `bytes` and the rest — a review rule, **not** an automated check | Step 3's wording, and the reviewer named in Routing. Because nothing in CI enforces it, the review is the enforcement |
| `AGENTS.md` § New Markdown placement | Any `.md` outside `docs/(prd\|frd\|adr\|design\|desktop)` fails the CI `documentation` job | The measured "not touched" row: **no new Markdown file is created at all**, which is the strongest possible form of compliance |

### Programme-level authorities that bind today

| Authority | Requirement | Met by |
| --- | --- | --- |
| **D-002** (2026-08-23) | Self-managed certificate trusted per workstation in `Cert:\LocalMachine\TrustedPeople`, **never** Trusted Root; trust always precedes the package | Steps 7–8; the Traps entry forbidding a Trusted Root instruction. Recording the trust step itself is [[REL-013]]'s, not this ticket's |
| **D-003** (2026-08-23) | The feed is an in-house UNC share over SMB; updates need the office network or VPN | Step 4's "Cannot reach Pegasus" action, stated without explaining SMB |
| **C-01** (2026-08-23) | The repositories become private; there is no public download page | Step 10's `grep` for `github.com` and `ms-appinstaller:`; the Traps entry |
| Proposal § 9.3 Operational controls | Interrupted updates, invalid signatures and unavailable feeds are tested and understood operationally | Steps 4, 7 and 8 |
| Proposal § 17.1 Required controls | Code-signing certificate protection and renewal | Cited, not implemented here — [[REL-007]] (plan handle `DSK-09-08`) rolls trust to the estate and [[REL-012]] (plan handle `DSK-09-14`) owns renewal R5 |
| `runbooks.md:221-270` § R7 | The five prerequisites, four steps, uninstall/reinstall and channel-switch paragraphs, and the six-item operator skeleton | Read in full at step 1; **restated nowhere** — [[REL-013]] carries them |
| `runbooks.md:260-261` | "credential store cleanup is part of uninstall behaviour **to verify in area 04**" | Step 6 — this ticket is area 04, and this is the deferred item |
| `runbooks.md:333-352` § R10 | Where a diagnostics bundle comes from and what it contains; and that `%LOCALAPPDATA%\Packages\<pfn>\LocalState\logs` is the manual fallback when the app cannot start | Step 5 names the location and the command and **does not** describe the bundle's internals — R10 already does, for support |
| `docs/desktop/06-ui-design/screen-specs.md:99-106` § Update required / Blocked | Title "Update required"; current and minimum versions as values; primary "Update now"; secondary "Sign out"; **Blocked** (account disabled or compatibility fail-closed) shows the operator sentence and "Sign out" only | Step 4 — each state named exactly, one action each, **no invented state** |
| `docs/desktop/04-auth-session-update-and-startup/README.md:191-198` § 3 decision 7 | The startup sequence: App Installer `OnLaunch` → `CheckUpdateAvailabilityAsync` → `Required`/`Available` handling → compatibility gate → WebView2 presence → session restore or native login → shell | Step 3, compressed to three operator sentences; the internal order is **not** described |
| `docs/engineering.md:78` (tier 7) | "authenticated workflows … keyboard, focus and error behavior… Automated axe results do not replace manual keyboard or assistive-technology review" | Step 8's independent reproduction is the evidence; a written contribution with no reproduction does not satisfy the tier |
| `docs/engineering.md:201-203` § Plan sizing | A plan states its diff estimate first, from a real number | The first line and the measured inventory above |
| `AGENTS.md` § Repository task workflow step 4 | Simplification pass recorded under a dated heading; `n/a — docs-only` for a documentation-only branch | Step 10 and the `## Simplification pass` heading below |
| `AGENTS.md` § Repository task workflow step 5 | Review by an agent that did not implement | Routing → Reviewer |

## Routing

Copied from the ticket body's `## Routing` block; required in the plan document
by `docs/desktop/00-governance-and-workflow/README.md` § Ticket template.

- **Subagent**: `pegasus-release-packager` —
  `.codex/agents/pegasus-release-packager.toml`
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `winui-packaging`
  (`.codex/skills/winui-packaging/SKILL.md`, `microsoft/win-dev-skills` v0.5.0
  `f1028dd5`) → `microsoft-docs` (Microsoft Learn plugin)
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`, `microsoft_docs_fetch`) for App Installer install
  behaviour and certificate-store guidance
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout` (call
  `get_doc_gates FND-049` before every move; a move crosses at most one gated
  boundary). `chore` owes `plan` at `leave-preparing` and `proof` at
  `enter-done`; no `research`, `files` or `checklist`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5)

## Steps

These refine the ticket body's ten implementation steps in the same order, with
the same ownership and the same file paths.

1. **Orient and take.** Read `runbooks.md` § R7 **in full** — `:221-271`:
   prerequisites `:227-243`, steps `:245-255`, uninstall/reinstall `:257-261`,
   channel switch `:263-264`, the operator skeleton `:266-270` — then
   `docs/desktop/04-auth-session-update-and-startup/README.md:191-198` § 3
   decision 7, then `docs/design/README.md:396-445` for the copy rules that
   bind every word written here. Call `get_doc_gates FND-049`, then
   `take_ticket`; branch `task/<slug>` from `origin/dev` with a worktree under
   `../pegasus-worktrees/<slug>`. Load `pegasus-desktop`, then
   `winui-packaging`.
2. **Resolve the case before writing anything.** Check whether
   `docs/desktop/09-release-update-and-distribution/first-install.md` exists.
   Measured 2026-08-24: it does **not**, so Case B applies — write the startup,
   blocked-state and diagnostics content **into this plan document**, under a
   dated heading, as the contribution [[REL-013]] (plan handle `DSK-09-15`)
   merges verbatim, and link it from the R7 section. If it does exist, add the
   three sections into that page in place, change no existing section, and
   restate none of [[REL-013]]'s prerequisites, certificate-trust step, install
   steps, uninstall/channel-switch paragraphs or six-item skeleton. **Record
   which case applied.** Either way, create **no** second guide at any path —
   in particular not
   `docs/desktop/04-auth-session-update-and-startup/first-run-guide.md`, which
   earlier drafts of this ticket named and which today does not exist.
3. **Write the startup section — three sentences.** On launch the app checks
   for updates, then checks compatibility with Pegasus, then shows Login. That
   is the whole section. `docs/desktop/04-auth-session-update-and-startup/README.md:191-198`
   lists seven internal stages; **compress, do not enumerate** — a sentence
   that explains how the system works is a defect under
   `docs/design/README.md:433-438`. Check the wording against the banned-words
   list at `:412-421` before moving on; nothing in CI catches it.
4. **Write the blocked states from the screen spec, exactly.**
   `docs/desktop/06-ui-design/screen-specs.md:99-106` defines two and only two:
   **"Update required"** — the operator's action is close and reopen (the
   screen's own primary is "Update now"); and **Blocked** (account disabled, or
   the compatibility gate fail-closed) — the operator's action is "Sign out"
   and contact support, because the screen offers nothing else. Name each state
   in the screen's words, give exactly one action each, and **add no state the
   screen spec does not define**. These are the two the six-item skeleton's
   "Update required" and "Cannot reach Pegasus" lines point at.
5. **Write the diagnostics section — a location and a command.** Where the
   rolling logs live under the packaged app's `ApplicationData`
   (`runbooks.md:348-350` gives the manual fallback path
   `%LOCALAPPDATA%\Packages\<pfn>\LocalState\logs`), and how to run the
   "Export diagnostics" command from [[FND-036]] (plan handle `DSK-02-11`) when
   support asks for a bundle. **Do not describe the bundle's internals** —
   `runbooks.md:333-352` § R10 already lists them, for support rather than for
   an operator.
6. **Answer R7's deferred area-04 item.** `runbooks.md:260-261` asserts the
   refresh token is removed on uninstall *and* flags it "to verify in area 04"
   in the same sentence. Verify it on a real machine: uninstall, then confirm
   the DPAPI credential store from [[FND-031]] (plan handle `DSK-02-06`) and
   [[FND-043]] (plan handle `DSK-04-07`) is gone with the package's
   `ApplicationData`. Hand the **verified** answer to [[REL-013]] for its
   uninstall/reinstall paragraph. **If the token survives uninstall, that is a
   product defect**: raise a separate `fix` ticket, record the true behaviour
   in R7, and do not document the intended one.
7. **Verify the commands the content relies on, on the clean machine.**
   `Get-AppxPackage CollisionEngineers.Pegasus` lists the version;
   `Get-AppxPackageAutoUpdateSettings` shows on-launch checks; the app's
   Settings/Diagnostics screen shows version, channel and gateway URL. These
   are R7 `:251-253`'s own verification lines — hand any correction to
   [[REL-013]]; **do not add a second verification section beside the owner's**.
8. **Operator step — the independent reproduction.** Hand the material —
   [[REL-013]]'s page if it exists, otherwise this plan document's contribution
   set beside R7 — to someone who did **not** write it, and have them reproduce
   the whole install on a clean Windows 11 VM **from a state with no
   certificate trusted and no package installed**. Starting from an untrusted
   machine is the point: it is the only way the `0x800B0109` path is exercised,
   and that path is why D-002 makes the trust step mandatory. Evidence back: a
   screenshot of each numbered step, `Get-AppxPackage` output,
   `certutil -verifystore TrustedPeople` output, and a written note of every
   place they had to guess. **Every guess is a defect to fix before the ticket
   closes.**
9. **Record the corrections in `runbooks.md` § R7** — the R7 steps the
   reproduction proved wrong (within `:245-255`), and the refresh-token answer
   from step 6 replacing the deferred clause at `:260-261`. Record the
   one-pager link and the managed-device policy state **only** in [[REL-013]];
   its plan step 10 claims exactly those two lines, so writing them here would
   give R7 two authors for one sentence.
10. **Run the gates, record the pass, open the PR.**
    `pwsh ./scripts/Test-DocumentationLinks.ps1` (declared `param()` at
    `Test-DocumentationLinks.ps1:8-9` — it takes no arguments) and
    `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD`.
    **Pass `-Base` and `-Head`**: `Test-MarkdownPlacement.ps1:3-4` declares
    both `[Parameter(Mandatory)]` and the script diffs that revision range, so
    a bare invocation prompts or fails and checks nothing — a recorded bare
    invocation is a failed verification, not a passed one. Use `origin/dev`, or
    the merge-base with `dev` if this branch has diverged. (For reference, CI's
    `documentation` job at `.github/workflows/ci.yml:82-87` runs
    `./scripts/Test-TestMarkdownPlacement.ps1` — the regression test *of* the
    placement script — plus `./scripts/Test-DocumentationLinks.ps1`; the
    two-argument form above is the direct check the body requires and is not a
    substitute for CI's.) Record the simplification pass as
    **`n/a — docs-only`** under a dated `## Simplification pass` heading in this
    document, then open the PR into `dev`.

## Verification

Evidence tier from the body: **Tier 7 — Browser/accessibility**
(`docs/engineering.md:78`). The body is explicit: "A written contribution with
no reproduction does not satisfy this tier." Proof types: `visual` (a
screenshot per numbered step) and `command-log`.

| Command / observation | Expected | Becomes evidence as |
| --- | --- | --- |
| `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base origin/dev -Head HEAD` | exit `0` and `Markdown placement passed for <base>..<head>.` (`Test-MarkdownPlacement.ps1:81`) | `proof` (command-log) |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exit `0`, no broken-link lines | `proof` (command-log) |
| `ls docs/desktop/04-auth-session-update-and-startup/first-run-guide.md` | **no such file** | `proof` (command-log) |
| `grep -rln "first-install\|first-run guide" docs/desktop/` | names exactly one guide page, `docs/desktop/09-release-update-and-distribution/first-install.md` (or, under Case B, names no guide page at all and the contribution is in this plan document) | `proof` (command-log) |
| `certutil -verifystore TrustedPeople` on the clean VM after the trust step | the Pegasus signing certificate is listed | `proof` (command-log) |
| `Get-AppxPackage CollisionEngineers.Pegasus` on the clean VM after the install | the installed version is reported | `proof` (command-log) |
| The uninstall check from step 6 | the DPAPI credential store and the package `ApplicationData` are both gone — **or** the true behaviour, with a `fix` ticket raised | `proof` (command-log), and the R7 edit |
| The independent reproduction | every numbered step completed by the second person with **no undocumented decision**; a screenshot per step | `proof` (visual) plus the written guess list |

Behaviours to read rather than infer: each blocked state's name matches
`screen-specs.md:99-106` word for word; the startup section is three sentences
and describes no mechanism; the diagnostics section names a location and a
command and no internals; and the R7 diff touches only `:245-255` and
`:260-261`.

## Risks / open questions

- **Risk — a second first-install guide.** The body's Traps make it a stop
  condition, and the temptation is real: this ticket is phase 2 and the owning
  page is phase 9. Mitigation: step 2 resolves the case and records it; the
  third and fourth Verification rows assert the absence directly; and
  [[REL-013]]'s own plan step 2 expects the contribution in this document, so
  the handshake exists on both sides.
- **Risk — the contribution is written as explanation.** A guide that explains
  the architecture is a defect under `docs/design/README.md:433-438`.
  Mitigation: step 3 caps the startup section at three sentences, step 5 caps
  diagnostics at a location and a command, and the reviewer named in Routing
  checks against `:412-421`. The banned-words rule is a **review** rule — CI
  enforces nothing here, and claiming otherwise would be exactly the false
  assurance `:418-421` warns about.
- **Risk — the intended refresh-token behaviour is documented instead of the
  real one.** `runbooks.md:260-261` states it and defers it in one sentence,
  which makes copying it forward easy and wrong. Mitigation: step 6 verifies on
  a real machine and raises a `fix` ticket if the token survives.
- **Risk — the reproduction starts from a machine that already trusts the
  certificate.** Then the `0x800B0109` path is never exercised and the guide's
  most important prerequisite is untested. Mitigation: step 8 requires a clean
  VM with **no certificate trusted and no package installed**.
- **Risk — a bare `Test-MarkdownPlacement.ps1` is recorded as a pass.** Both
  parameters are `[Parameter(Mandatory)]` (`:3-4`), so a bare call prompts or
  fails and checks nothing. Mitigation: step 10 and the first Verification row
  both carry the two-argument form and the expected output string.
- **Risk — a `Trusted Root` instruction, a GitHub link, or an
  `ms-appinstaller:` link reaches operator copy.** D-002 specifies
  `TrustedPeople`; C-01 makes the repositories private; the protocol has been
  disabled by default since December 2023. Mitigation: none of the three is
  written here at all — the trust step is [[REL-013]]'s — and the reviewer
  checks the contribution for them.
- **Scope boundary, not an open question — the first-install page itself, its
  prerequisites, its trust step, its install steps and its operator skeleton.**
  Owned by [[REL-013]]. The one-pager link and the managed-device policy state
  in R7 are also [[REL-013]]'s (its plan step 10).
- **Scope boundary, not an open question — the startup sequence and the
  blocked screens themselves.** Owned by [[FND-045]] (plan handle `DSK-04-09`)
  and area 06. This ticket describes them in operator words and defines none of
  them; if a state changes, the screen spec changes first and the contribution
  follows.
- **Operator dependency, not an open question.** Step 8 needs a dedicated clean
  Windows 11 VM and a second person; the ticket carries the `needs-operator`
  label for exactly that. D-002, D-003 and C-01 were all settled on 2026-08-23
  and `docs/desktop/README.md` records that **no open decisions remain**.
- **Open questions**: none. No `open-questions` document is created — the
  ticket body does not instruct one, and every unknown above is a scope
  boundary owned by a named sibling ticket, which
  `docs/desktop/00-governance-and-workflow/README.md` § 3 makes a boundary
  rather than a question.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass
over this branch's own diff before the PR, recorded here under a dated heading.
This branch changes Markdown only, so the expected record is
`n/a — docs-only`._
