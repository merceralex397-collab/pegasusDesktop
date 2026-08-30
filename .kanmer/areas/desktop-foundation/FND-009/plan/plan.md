# Plan — FND-009: Record the `gateway/r<N>` and `desktop/v<M.m.b>` release-tag convention

**Diff estimate: ~3 files, ~28 lines** — or ~4 files, ~38 lines if step 7 mirrors the duplicate skill copy rather than deferring it to [[TOOL-004]] (plan handle `DSK-12-04`).

Derived from the measured inventory below, not asserted. The range is the one
real choice in the ticket, and step 7 records which branch was taken.

## Measured file-and-line inventory

`chore` owes no `files` document, so the surface area is measured here. Read on
2026-08-24 from the working tree at `origin/main` `191ddf3342…`.

| Path | Measured today | What this ticket changes |
| --- | --- | --- |
| `docs/engineering.md` | 236 lines; § Branches and delivery is `:10-52` (heading `:10`, next heading `## Markdown convention` at `:54`). Hard-wrapped near 78 columns; compact table delimiter `\| --- \|` (§ Markdown convention `:54-62`) | +1 bullet stating both tag forms and the CI consequence — ~8 lines at that wrap |
| `.agents/skills/pegasus-release/SKILL.md` | 275 lines; `## 2. Promote `dev` to `main` — exact-SHA atomic fast-forward` at `:62`, its read-back fenced block at `:66-72`, and `## 3. Build immutable artifacts` at `:78` | +1 tag step inserted between `:77` and `:78`, with a fenced command block — ~10 lines |
| `.codex/skills/pegasus-release/SKILL.md` | **Exists, tracked, and byte-identical** to the `.agents` copy (`diff -q` reports no difference; both 275 lines) | either the same ~10 lines, or a recorded hand-off to [[TOOL-004]] — step 7 decides and records |
| `docs/operations.md` | 920 lines; `## Production environment` at `:280`; the highest entry is **Release 20** at `:336` (2026-08-22, source `05fe7a7f`) | +1 line beside the next release's entry, at the next release — ~1 line |
| Tags on the fork | `git tag` returns **nothing**; `git tag \| wc -l` → 0 | the first `gateway/r<N>` is genuinely new |
| `.github/workflows/ci.yml` | — | **not touched**; [[REL-005]] (plan handle `DSK-09-05`) and [[REL-015]] (plan handle `DSK-09-17`) own it |

## Approach

Record the convention in `docs/engineering.md` — the working rule that
`docs/index.md` § Authority already names for "what engineering guidance
applies" — and mirror only the *executable* half into the release skill, placed
immediately after the promotion read-back so the tag can never precede the thing
it names. The rejected alternative was recording it in
`docs/desktop/00-governance-and-workflow/README.md` alone: that file is
programme planning, and `AGENTS.md` § New Markdown placement is explicit that
`docs/desktop/` "holds programme planning only". A convention that CI and a
release actor must follow has to live in a canonical file, which is why step 8
marks the plan as recorded rather than making the plan the authority.

The tag is deliberately *not* part of the promotion transaction. `docs/engineering.md:16-33`
makes promotion an atomic exact-SHA push with a read-back; adding a tag push into
that sequence would turn one transaction into two and create a state where `main`
moved but the tag did not. So the skill's tag step comes **after** the read-back
that proves `origin/main` equals the promoted SHA, and the convention says so.

## Governing docs

`refs` is empty and `docs_todo: true` — confirm with `get_doc_gates FND-009`
before moving. No repository ADR governs this work today.

> **New ADR** — ADR-0105 (signed MSIX/App Installer distribution plus the
> gateway minimum-version gate) is the decision this convention serves: the
> gate's minimum client version has to be traceable to the gateway commit that
> shipped it. **ADR-0105 has three claimants** — [[FND-005]] (plan handle
> `DSK-00-05`), [[REL-001]] (plan handle `DSK-09-01`) and [[FND-042]] (plan
> handle `DSK-04-01`) — so it is `authored by whichever of the three is worked
> first; see [[FND-005]]'s plan for the ownership reconciliation`, with the
> operator's answer tracked as an unticked box on [[REL-001]]'s
> `open-questions` document.
> This plan is written to the convention as recorded in
> `docs/desktop/00-governance-and-workflow/README.md` § 3 "Recommended branching
> flow" item 4 and to D-002/D-003 as recorded in `docs/desktop/README.md`
> § Locked decisions; if ADR-0105 lands differently this plan is revised before
> implementation.

Because `refs` is empty, the authorities that bind today are these:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `docs/engineering.md:10-15` § Branches and delivery | `main` is the active deployment and the sole revision eligible for an authorised release | Step 3 (tags on `main` only) |
| `docs/engineering.md:16-33` | Promotion is an exact-SHA atomic fast-forward with a read-back, authorised by `MERGE AUTH GRANTED` | Steps 3, 6 (the tag follows the read-back and is not part of the transaction) |
| `docs/engineering.md:54-62` § Markdown convention | Hard wrap near 78 columns; compact table delimiter | Step 5 |
| Plan 00 § 3 item 4 | `gateway/r<N>` where N is the release number in `docs/operations.md` § Production environment; `desktop/v<M.m.b>` = the MSIX version; unsigned MSIX on every PR, signed on `main` tags only; publishing stays runbook-controlled | Steps 3–4 |
| Proposal § 21 Build, CI and release; § 9 Forced updates | The release route and the compatibility range the tags make traceable | Steps 3–4 |
| D-001 (2026-08-23) | After the fork becomes the single release source, these tags are the only release marks that exist | Step 3 |
| D-002 / D-003 (2026-08-23) | The desktop signs in-house and publishes to a UNC share, so a `desktop/v*` tag never triggers a public release | Step 4 |
| C-01 (2026-08-23) | Private repositories bill Windows runner minutes at 2×, so tag-triggered lanes stay narrow | Step 4 (the convention records the constraint; the lane itself is [[REL-015]]'s) |
| `scripts/Test-MarkdownPlacement.ps1:31,58-71` | New Markdown only under the allowed roots — **and the gate only inspects `A`, `C` and `R` change kinds**, so modifying an existing file anywhere is never a placement violation | Steps 6–7 |
| `.github/workflows/ci.yml:71-87` | The `documentation` job runs on every change set | Step 9 |

## Routing

Copied from the ticket body's `## Routing` block, as
`docs/desktop/00-governance-and-workflow/README.md` § "Ticket template" requires
of the plan document specifically.

- **Subagent**: `pegasus-release-packager` —
  `.codex/agents/pegasus-release-packager.toml` (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`) → `pegasus-release`
  (`.agents/skills/pegasus-release/SKILL.md`) → `kanmer-plan`
  (`.grok/skills/kanmer-plan/SKILL.md`).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`,
  `set_ticket_doc`, `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-tickets` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates FND-009` before every move; a move crosses at most one gated
  boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's twelve implementation steps; order, ownership and paths
are the body's.

1. **Orient.** Read `docs/engineering.md:1-52`,
   `.agents/skills/pegasus-release/SKILL.md` (275 lines; the promotion section is
   `## 2.` at `:62-77`), `docs/operations.md` § Production environment (`:280`),
   and plan 00 § 3 item 4. Call `get_doc_gates FND-009`, then `take_ticket`.
   Confirm [[FND-001]] (plan handle `DSK-00-01`) has landed — tags are applied on
   `main`, which is only meaningful once `dev` exists and the promotion route
   works.
2. **Establish the next release number, and record it.** Read the highest
   `Release <N>` entry in `docs/operations.md` § Production environment.
   **Measured 2026-08-24: Release 20 at `:336`** (2026-08-22, source `05fe7a7f`),
   so `N+1` is **21** today. **Re-read this at execution**: [[FND-002]] (plan
   handle `DSK-00-02`) merges upstream releases 21–24 into this file, after which
   `N+1` becomes 25. Write the value you actually read, with its date, into this
   plan. The convention has to name a real next value, not a placeholder.
3. **Add the bullet to `docs/engineering.md` § Branches and delivery**, stating
   all four rules exactly: tags are applied **on `main` only**, after the
   exact-SHA promotion and after its read-back; `gateway/r<N>` where N is the
   release number recorded in `docs/operations.md` § Production environment;
   `desktop/v<M.m.b>` equal to the MSIX package version in
   `Package.appxmanifest`; and **tags are never moved or deleted once pushed**.
4. **In the same bullet, record the CI consequence** from plan 00 § 3 item 4: CI
   builds an **unsigned** MSIX on every PR and builds and signs on `main` tags
   only; publishing to the production feed stays a runbook-controlled step. Do
   **not** write the workflow — [[REL-005]] and [[REL-015]] own
   `.github/workflows/ci.yml`. Note the C-01 constraint (private repositories
   bill Windows runner minutes at 2×) as the reason tag lanes stay narrow, but
   leave the lane's shape to [[REL-015]].
5. **Match the file's house style.** `docs/engineering.md` is hard-wrapped near
   78 columns with the compact table delimiter `| --- |` (§ Markdown convention,
   `:54-62`). Keep the surrounding prose intact; this is one bullet added to an
   existing list, not a restructure.
6. **Mirror the executable half into `.agents/skills/pegasus-release/SKILL.md`.**
   Insert the tag step between the end of `## 2. Promote `dev` to `main`` (`:77`)
   and `## 3. Build immutable artifacts` (`:78`), so it sits immediately after
   the read-back block at `:66-72` that requires `git rev-parse origin/main
   origin/dev` to equal `$SHA`. Literal commands:
   ```
   git tag -a gateway/r<N> <promoted-sha> -m "Gateway release <N>"
   git push origin gateway/r<N>
   ```
   plus the rule that the tag is pushed **only after** the read-back of
   `origin/main` equals the promoted SHA. The tag is not part of the promotion
   transaction and must never be folded into that `git push --atomic`.
7. **Handle the duplicate skill copy, and record which branch you took.**
   Measured 2026-08-24: `.codex/skills/pegasus-release/SKILL.md` **exists, is
   tracked, and is byte-identical** to the `.agents` copy (`diff -q` reports no
   difference; both 275 lines). Two divergent copies of the release route is the
   failure this step prevents, so either:
   **(a)** apply the same edit there — safe, because
   `scripts/Test-MarkdownPlacement.ps1` inspects only `A`, `C` and `R` change
   kinds (`:58-61`) and a modification to an existing tracked file is never a
   placement violation, even though `.codex/skills` is **not** in the allowed
   roots regex at `:31`; or
   **(b)** record that [[TOOL-004]] (plan handle `DSK-12-04`, "Remove the
   duplicate skill copies under `.codex/skills/` so there is one list") will
   carry it, and note here that the two copies are knowingly divergent until
   that ticket lands.
   Write the choice and its reason into this plan; an unrecorded choice is the
   defect.
8. **Do not touch `docs/desktop/00-governance-and-workflow/README.md` § 3 item
   4** except to mark the convention as recorded. The plan is not the authority
   once `docs/engineering.md` holds it.
9. **Run the gate** the CI `documentation` job runs at
   `.github/workflows/ci.yml:87`:
   ```
   pwsh ./scripts/Test-DocumentationLinks.ps1
   ```
   Exits 0. Open the PR against `dev` and merge after the independent review.
10. **Operator step — apply the first tag.** At the next production gateway
    release, the release actor applies the tag on the promoted `main` SHA with
    the commands in step 6 and hands back the output of
    `git tag --list 'gateway/*'` and `git ls-remote --tags origin`.
11. **Record the applied tag in `docs/operations.md`** beside that release's
    entry, so the number→SHA join is readable from the current-state document.
12. **Write the proof** as a `command-log`: the `git tag --list 'gateway/*'`
    output plus the diff of the documentation edits.

## Verification

Evidence tier 1 — Static/build/architecture (`docs/engineering.md:72-90`), as
the body states: documentation and a tag reference. The release itself is
evidenced by the release ticket that applies the tag.

| Command | Expected |
| --- | --- |
| `git tag --list 'gateway/*'` | the first tag after the next release; **empty before it** — record at closeout which state applies. Measured 2026-08-24: `git tag` returns nothing at all |
| `grep -n 'gateway/r' docs/engineering.md .agents/skills/pegasus-release/SKILL.md` | a hit in both files |
| `grep -n 'desktop/v' docs/engineering.md` | the second tag form present |
| `grep -n 'gateway/r' .codex/skills/pegasus-release/SKILL.md` | a hit under branch (a); **no hit and a recorded hand-off to [[TOOL-004]]** under branch (b) |
| `diff .agents/skills/pegasus-release/SKILL.md .codex/skills/pegasus-release/SKILL.md` | no output under branch (a) — the two copies stay identical |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exits 0 |
| `git diff --stat -- .github/workflows/ci.yml` | empty — the workflow is [[REL-005]]'s and [[REL-015]]'s |

Plus, recorded in the plan and the proof: the release number actually read in
step 2, with its date.

## Risks / open questions

- **Naming a placeholder release number.** `N+1` is 21 today and becomes 25 once
  [[FND-002]] merges upstream releases 21–24 into `docs/operations.md`.
  Mitigation: step 2 re-reads at execution and records the value with its date.
- **Folding the tag push into the promotion transaction.** Would convert one
  atomic operation into two and create a `main`-moved-but-untagged state.
  Mitigation: step 6 places the tag step after the read-back and says so
  explicitly.
- **The two skill copies diverging.** Measured identical today; step 7 forces an
  explicit choice and records it.
- **A tag is not a promotion.** The exact-SHA push with `MERGE AUTH GRANTED`
  still governs `main` (`docs/engineering.md:16-33`); a tag records what was
  promoted and confers nothing.
- **Placement-gate confusion around `.codex/skills`.** It is not in the
  allowed-roots regex (`scripts/Test-MarkdownPlacement.ps1:31`), which reads as a
  blocker until you notice the gate only inspects `A`/`C`/`R` change kinds
  (`:58-61`). Editing an existing tracked file there is safe; **adding a new
  `.md` there would not be.**
- **`docs/engineering.md` § Branches and delivery edit collision** with
  [[FND-002]], which adds the one-way `upstream` sync sentence to the same
  section. A scope boundary owned by a named ticket, not a question: coordinate
  the two edits, or rebase whichever lands second.
- **Scope boundaries owned by named tickets, not questions:** `ci.yml`
  ([[REL-005]], [[REL-015]]); the version-generation script and the release
  manifest ([[REL-002]], plan handle `DSK-09-02`); removing the duplicate skill
  tree ([[TOOL-004]]); ADR-0105 authorship ([[FND-005]], [[REL-001]],
  [[FND-042]] — tracked on [[REL-001]]'s `open-questions`).
- **Not open, and not to be reopened:** D-001, D-002, D-003 and C-01, all
  recorded by the operator on 2026-08-23.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over
this branch's own diff before the PR, recorded here under a dated heading.
Expected outcome: `n/a — docs-only`._

## Implementation checkpoint — 2026-08-26

- The highest recorded production gateway release is `Release 20` in `docs/operations.md`; the next release number for the first tag is therefore `21`.
- Added the immutable tag convention and CI consequence to `docs/engineering.md` § Branches and delivery.
- Added the post-promotion `gateway/r<N>` and `desktop/v<M.m.b>` tag commands to both byte-identical release skill copies (`.agents/skills/pegasus-release/SKILL.md` and `.codex/skills/pegasus-release/SKILL.md`). The duplicate was synchronized rather than left divergent; its later removal remains [[TOOL-004]]'s ownership.
- `git tag --list 'gateway/*' 'desktop/*'` is empty. No production gateway release occurred during this task, so the first `gateway/r21` tag and its `docs/operations.md` release-row record are intentionally pending the next authorized production release. They are not claimed as complete here.

## Simplification pass — 2026-08-26

The diff was reviewed for reuse, duplication, scope, efficiency and altitude. The documentation change adds one compact engineering rule; the release skill copies are kept byte-identical to avoid two routes diverging; no workflow, packaging, deployment, feed, Azure, or version-generation change was added. No further behavior-preserving simplification was identified.

## Independent review and merge — 2026-08-26

Faraday (`pegasus-desktop-reviewer`) independently reviewed PR #24 at exact head `322e18bda664f3b800c9614a64513a4be6b11e34` and returned **PASS** for the static portion. The review verified the two tag mappings, main-only/post-promotion ordering, immutability rule, synchronized skill copies, three-file scope, and no cloud/deployment/upstream activity. It confirmed merge was appropriate while the release-time tag remains pending.

- PR #24 merged into `dev` at `f26b5b01d509ad21d9db58bca9fb00afe77c384a` on 2026-08-26.
- Exact-head repository-check run `33009752135`: applicable `changes`, `documentation`, `local-development-scripts`, and `reference-data` jobs passed; code/infrastructure lanes were path-skipped.
- `gateway/r21` and the corresponding `docs/operations.md` entry are not claimed. FND-009 must remain open until the next authorized production release applies the tag and records the release evidence.

## Retrospective review and live disposition — 2026-08-29

Independent reviewer Peirce reviewed the exact merged ticket head `322e18bda664f3b800c9614a64513a4be6b11e34` and merge commit `f26b5b01d509ad21d9db58bca9fb00afe77c384a`. PR #24's repository-check run `33009752135` matched the exact head and passed the documentation, changes, local-development-scripts and reference-data lanes; the .NET, SQL and browser lanes were correctly skipped for this documentation-only diff.

The review is retrospective: there is no evidence of an independent review before PR #24 merged. The two release skill copies are byte-identical and carry the tag route. The canonical engineering paragraph records tags after exact-SHA promotion but does not explicitly say after the promotion read-back or preserve the plan's C-01/2x Windows-runner cost rationale; this needs an in-repo remediation PR.

The first `gateway/r<N>` tag is not present locally or on `origin`, and `docs/operations.md` still ends at Release 20. That tag must be applied by the release actor to the next authorized promoted `main` SHA and recorded beside the release. The current no-release/no-cloud constraint means this acceptance criterion is intentionally pending; no tag or production proof is fabricated.

## In-repo remediation — 2026-08-30

- Re-read the current branch-delivery paragraph and release skill route on the branch cut from `origin/dev` at `8aa8f211d34f9b476c5231eff60fce071104b4e3`.
- Added the explicit post-promotion `origin/main` read-back requirement and the C-01 private-runner 2× cost rationale to `docs/engineering.md` in commit `5d8be6841043c095b5fc7a2bc27127dbfa47a2e6`.
- `pwsh ./scripts/Test-DocumentationLinks.ps1` passed (238 files); `git diff --check` passed; `.agents/skills/pegasus-release/SKILL.md` and `.codex/skills/pegasus-release/SKILL.md` remain byte-identical.
- This does not claim the first `gateway/r<N>` tag. The tag and its `docs/operations.md` record remain a release-time handback, prohibited under the current no-release/cloud constraint.
