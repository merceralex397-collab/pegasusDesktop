# Plan — TOOL-008 (plan handle `DSK-12-08`): Author ADR-0110 — agent-skill pinning and the invocation protocol

**Diff estimate: ~2 files, ~150 lines.** One new ADR
(`docs/adr/0110-pin-agent-skills-and-invocation-protocol.md`, ~140 lines — comparable to
`docs/adr/0025-integrate-renderer-and-extractor-into-the-application.md`, whose body runs
Context `:22` → Links `:105`) plus one index row in `docs/adr/README.md`. `docs/index.md` is
almost certainly `None.` — its row at `docs/index.md:28` already points at the desktop plan
set — but verify rather than assume, as the body says.

## Approach

**Check for an existing ADR-0110 before writing one.** Board [[FND-005]] (plan handle
`DSK-00-05`, "Author ADR-0100, ADR-0101, ADR-0103, ADR-0104, ADR-0105 **and ADR-0110** in the
reserved block", profile `feature`) claims the same number. Confirmed on the board
2026-08-24. One filename, one rule: whichever ticket is worked first authors
`docs/adr/0110-pin-agent-skills-and-invocation-protocol.md`; the other **verifies and
extends it in place**, never a second file for the same number. That interlock, not the
prose, is the main design decision in this ticket.

The second choice is to **assemble the ADR from existing text rather than rewrite it**. The
invocation protocol (seven steps), the review protocol (§20.6's eight verification points),
the three pinned commits and the six-question table all already exist in settled wording;
`AGENTS.md` § ADR conventions makes published ADR bodies immutable, so the wording chosen
here is permanent and a paraphrase now becomes a second, divergent version forever. Rejected
alternative: recording the pinning rule only in `docs/desktop/12-agent-tooling/README.md`.
`AGENTS.md` § Documentation model puts durable technical decisions in an ADR, and
`docs/desktop/` is explicitly programme planning, not authority — a lockfile whose only
justification lives in a plan has nothing a bump can be in breach of.

## Governing docs

The ticket carries `refs: []` and **`docs_todo: true`** — and this ticket is the one that
fixes that for the whole area.

> **New ADR** — ADR-0110 (agent-skill pinning, the lockfile and vendored revisions, and the
> invocation/review protocol), authored **by this ticket** (or by board [[FND-005]], plan
> handle `DSK-00-05`, whichever is worked first), filename
> `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md`. Reserved in the ADR-0100…
> ADR-0110 block recorded at `docs/desktop/00-governance-and-workflow/README.md` § 3 and in
> `AGENTS.md` § ADR conventions. Step 12 links it to this ticket with `link_doc`, which is
> what clears `docs_todo`.

Programme-level authorities this plan meets:

| Authority | Requirement | Met by |
| --- | --- | --- |
| `AGENTS.md` § ADR conventions | Stable IDs, YAML frontmatter, one decision per ADR, supersede-don't-renumber, published bodies immutable | Steps 3–4, 10 |
| `docs/adr/README.md:9-14` | Frontmatter carries `id`, `status`, `date`, `supersedes`, `superseded_by`, `related_capabilities`, `related_frd`, `tags`; the accepted set is the current architecture | Step 4 |
| Proposal Appendix A | Status / Context / Current evidence / Options / Cloud-justification test / Decision / Consequences / Verification / Reversal-deprovision condition | Steps 5–10 |
| Proposal §20 in full | The decision this ADR records | Steps 6–7 |
| ADR block rule | Never "next free number"; upstream keeps issuing ADRs and the one-way sync would collide | Step 2, and Consequences (b) in step 9 |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-desktop-reviewer` — `.codex/agents/pegasus-desktop-reviewer.toml`
  (`sandbox_mode = "read-only"`, `model_reasoning_effort = "high"`). It checks the ADR
  against the shipped lockfile and the conventions; it cannot write, so the owner transcribes.
- **Skills**, in load order:
  1. `pegasus-desktop` — `.agents/skills/project/pegasus-desktop/SKILL.md`
  2. `kanmer-docs` — `.grok/skills/kanmer-docs/SKILL.md`
  3. `kanmer-plan`, `kanmer-execute` — `.grok/skills/<name>/SKILL.md`
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `link_doc`, `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates confirmed by
  `get_doc_gates TOOL-008`: `leave-preparing` needs `plan` + `questions-resolved`;
  `enter-done` needs `proof` + `questions-resolved`. Call `get_doc_gates TOOL-008` before
  every move.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

Refines the body's 13 steps in the same order.

1. **Orientation.** Read `EPIC-013/context.md`, then the plan sections in the body's
   **Source of truth**. `get_doc_gates TOOL-008`, then `take_ticket`. Confirm
   [[TOOL-002]] (`DSK-12-02`) landed: `eng/skills/skills.lock.json` must exist with real
   hashes, or this ADR describes a file that does not exist.
2. **Check before writing.** `ls docs/adr/0110-*.md`. If a file exists, board [[FND-005]]
   has already authored it — this ticket then **verifies and completes** that file (steps
   5–10) and creates nothing new. Record which path was taken in the first line of the
   implementation notes.
3. **Read the conventions and the shape reference.** `AGENTS.md` § ADR conventions;
   `docs/adr/README.md:1-20`; then `docs/adr/0025-integrate-renderer-and-extractor-into-the-application.md`
   as the shape reference — its body headings are `# ADR-0025: <title>` (`:11`),
   `## Context` (`:22`), `## Decision` (`:52`), `## Consequences` (`:69`),
   `## Options considered` (`:97`), `## Links` (`:105`).
4. **Write the frontmatter.** Exactly the key set the existing ADRs use — the precedent is
   `docs/adr/0026-enable-automation-mcp-by-explicit-deployment-configuration.md:1-9`
   (`id: ADR-0026` at `:2`, `superseded_by: []` at `:6`), and
   `docs/adr/0025-…:1-10` matches:

   ```yaml
   ---
   id: ADR-0110
   status: accepted
   date: <today>
   supersedes: []
   superseded_by: []
   related_capabilities: []
   related_frd: []
   tags: [agent-tooling, desktop-conversion]
   ---
   ```

   The `id` carries the `ADR-` prefix; empty lists are `[]`, **never `null`**. Do not invent
   a field. `related_capabilities` is empty because no `FAMILY-NN` capability in
   `docs/capabilities.md` covers developer tooling; say so in Consequences rather than
   inventing one.
5. **Body sections — take the union of the repository precedent and proposal Appendix A**,
   keeping the repository's heading names where they overlap (`## Options considered`, not
   "Options"), and adding Appendix A's extra sections. Order:
   `## Context` → `## Current evidence` → `## Options considered` →
   `## Cloud-justification test` → `## Decision` → `## Consequences` → `## Verification` →
   `## Reversal / deprovision condition` → `## Links`.
   - **Context**: proposal §20.1–20.2 — skills are playbooks, and mutable instructions make
     code review and reproduction unreliable.
   - **Current evidence**: `eng/skills/skills.lock.json` (35 entries, 19 `dotnet/skills` +
     8 `win-dev-skills` + 8 `azure-skills`), the vendored destinations
     `.agents/skills/vendor/{dotnet,windows,azure}/`, the project skill at
     `.agents/skills/project/pegasus-desktop/SKILL.md`, and the CI verifier step in the
     `changes` job of `.github/workflows/ci.yml` ([[TOOL-003]], `DSK-12-03`).
   - **Options considered**: fetch at execution time / vendor unpinned / vendor pinned by
     commit — with why each of the first two was rejected.
6. **Decision.** Agents load skills only from the vendored destinations at the pinned
   commits, never from a moving branch. Record the three pins **verbatim** — they are the
   contract and a truncated SHA is not one:
   - `dotnet/skills` `98f848512e9ee4877e399a0ae367bb5e4a193144`
   - `microsoft/win-dev-skills` `f1028dd5bb19af59df400cb4a2ab867e40a40a4a` (v0.5.0)
   - `microsoft/azure-skills` `1a03acfb9ac1a1a05518bf7420d4618cc41847be`

   Record that a skill update is a **reviewed PR** that bumps the commit and re-runs the
   sync — the procedure itself is [[TOOL-010]]'s (`DSK-12-10`) work and the ADR points at
   `docs/runbook.md` for it rather than restating it.
7. **Include the two protocols, not a rewrite of them.** The invocation protocol is the
   seven numbered steps of `docs/desktop/12-agent-tooling/README.md` § 6 (also present as
   § Invocation protocol in the project skill — reconcile so there is one wording). The
   review protocol is §20.6's list: the reviewer loads the skills independently and verifies
   dependency boundaries, XAML/native implementation, async and UI-thread safety,
   accessibility, package and update implications, API and data compatibility, test
   evidence, and cloud-placement justification.
8. **Answer the six-question cloud-justification table.** Copy the table verbatim from
   `docs/desktop/00-governance-and-workflow/README.md:169-179` — its six rows are Shared
   authority, Unattended execution, Protected credentials, Public callback, Central
   enforcement, Measured operational advantage — and fill **every** row with yes/no plus
   evidence. All six are **no** for agent tooling: the skills are text files on a developer
   workstation, read by a local toolchain, with no shared state, no unattended run, no
   secret, no inbound callback, no client-independent enforcement and no measured advantage
   to centralising. So the responsibility sits on the workstation and no Azure resource is
   involved. Six blank rows is not an answer, and "it is already in Azure" / "the web app
   does it" / "it may scale later" are not answers.
9. **Consequences must record two deviations honestly.**
   (a) The routing document lives at `docs/desktop/12-agent-tooling/skill-routing.md`, not
   the proposal's `docs/agent/skill-routing.md`, because
   `scripts/Test-MarkdownPlacement.ps1:31` allows only
   `^((docs/(prd|frd|adr|design|desktop))|workspaces/document-extraction|\.agents/skills|\.design-sync|\.grok|\.stitch|design/planning-and-old-designs)/.+\.md$`
   and `docs/agent/` would fail the CI `documentation` job
   (`.github/workflows/ci.yml:71-87`, `windows-latest`).
   (b) The ADR uses the reserved ADR-0100…ADR-0110 block rather than the next free number,
   to avoid collision with upstream's still-active ADR series (upstream `main` was 32
   commits ahead on 2026-08-23).
   Add a third consequence worth recording: `eng/` is **not** an allowed Markdown root, so
   no `README.md` accompanies the lockfile and the operational text lives in
   `docs/runbook.md`.
10. **Verification and Reversal sections.** Verification is
    `pwsh ./eng/skills/verify-skills.ps1` green locally and in the CI `changes` job.
    Reversal is a **superseding ADR, never an edit** — `docs/adr/README.md:11-14` makes
    published bodies immutable and IDs never renumbered or reused. There is no deprovision
    condition because no cloud resource is involved; say that in one line rather than
    dropping the heading.
11. **Add the index row** to `docs/adr/README.md` under
    "## Current architecture decisions (`status: accepted`)", **in ADR-number order**,
    matching the existing three-column row format
    (`| ADR | Title | Related FRD |`), for example:
    `| [0110](0110-pin-agent-skills-and-invocation-protocol.md) | Pin agent skills and the invocation protocol | — |`.
    The table is currently ADR-0001…ADR-0029 with 0017 never issued, so 0110 sorts last.
12. **`link_doc`** the ADR to this ticket so the governing-doc reference is real, then clear
    `docs_todo`.
13. **Run the documentation gates.** `pwsh ./scripts/Test-DocumentationLinks.ps1` → expect
    `All relative Markdown links resolve (<n> files checked).`
    `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base with dev> -Head HEAD` →
    expect `Markdown placement passed for <base>..<head>.` (`docs/adr/` is an allowed root.)

## Verification

Evidence tier **1 — Static/build/architecture**, as the body states — and the body's caveat
is the important half: **an ADR proves a decision was recorded, never that it is enforced.**
The enforcement evidence is [[TOOL-003]]'s CI run. `proof` is a `command-log`.

1. `ls docs/adr/0110-*.md` → exactly one file.
2. `head -12 docs/adr/0110-*.md` → YAML frontmatter with `id: ADR-0110`, `status: accepted`,
   `date`, `supersedes: []`, `superseded_by: []`, `related_capabilities`, `related_frd`,
   `tags`.
3. `grep -c '98f848512e9ee4877e399a0ae367bb5e4a193144\|f1028dd5bb19af59df400cb4a2ab867e40a40a4a\|1a03acfb9ac1a1a05518bf7420d4618cc41847be' docs/adr/0110-*.md`
   → `3`.
4. `grep -n '0110' docs/adr/README.md` → one index row in the accepted table.
5. `pwsh ./scripts/Test-DocumentationLinks.ps1` →
   `All relative Markdown links resolve (<n> files checked).`
6. `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base> -Head HEAD` →
   `Markdown placement passed for <base>..<head>.`

## Risks / open questions

| Risk | Mitigation |
| --- | --- |
| **Two agents author two ADR-0110s.** Board [[FND-005]] claims the same number and is a `feature` (so it also owes research, files, plan and checklist before it can leave Preparing — it is the slower of the two). | Step 2 is the interlock: `ls docs/adr/0110-*.md` first, and one filename only. If the file exists, verify and extend in place. |
| Taking "the next free number" and colliding with upstream's series. | Step 4 pins `ADR-0110`; the ADR block rule is restated in Consequences (b) and in `EPIC-013/context.md`. |
| Writing the ADR before the lockfile exists, so it describes a wish. | Step 1 requires `eng/skills/skills.lock.json` present with real hashes; [[TOOL-002]] is a hard dependency. |
| Paraphrasing the two protocols creates a second, divergent version — permanently, because bodies are immutable. | Step 7 copies them; step 7 also requires reconciling the project skill's wording so there is one. |
| Editing another ADR body to "fix" something. | Forbidden: published bodies are immutable and a changed decision is a new superseding ADR (`docs/adr/README.md:11-14`). Scope boundary allows only `docs/adr/0110-*.md`, `docs/adr/README.md` and possibly `docs/index.md`. |
| An ADR outside the allowed Markdown roots. | `docs/adr/` is allowed; step 13's placement gate confirms. |

Open questions: **none.** The one genuine unknown — who authors the file first — is resolved
by a rule rather than a question (step 2), so no `open-questions` document is created.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR. **This branch is documentation-only**, so the expected
record is `n/a — docs-only` under a dated heading — write the date, do not omit the heading._
