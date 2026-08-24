# Plan — TOOL-002 (plan handle `DSK-12-02`): Vendor the pinned skills and promote `skills.lock.json` with real hashes

**Diff estimate: ~5 hand-authored files, ~730 hand-written lines — plus ~60–90 vendored
files (~12,000–18,000 lines) copied verbatim by the sync script and never typed.**

Derived from the surface area below. Hand-authored: `eng/skills/sync-skills.ps1` (~180
lines), `eng/skills/verify-skills.ps1` (~90), `eng/skills/skills.lock.json` (382 lines,
copied from the 382-line draft with 35 `computedHash` values substituted), and three
documentation edits — two sentence-level ones in
`docs/desktop/12-agent-tooling/README.md` § 3 and
`docs/desktop/12-agent-tooling/skill-routing.md` § Pinned sources, plus a ~25-line
presentational split of `skill-routing.md` § "Not applicable to this conversion (do not
load)" into its two real categories (body **Documentation changes**). Vendored: 35 skill
folders — 19 from `dotnet/skills`, 8 from `win-dev-skills`, 8 from `azure-skills`. The
measured shape of the existing `.codex/skills/` tree is 18 files for 8 WinUI skills (2.25
files per skill), so ~80 files is the honest projection; the binary decision in step 5 moves
that by 8 MiB, not by file count.

## Approach

Write the sync as a **lockfile-driven copy from a pinned commit**, not as a submodule and
not as a package restore. The lockfile is already fully drafted at
`docs/desktop/12-agent-tooling/skills.lock.draft.json` (382 lines, `version: 2`, 3 sources,
35 entries), so the ticket's real work is the two PowerShell scripts and the two recorded
decisions, not the data. The alternative considered and rejected was **git submodules** for
the three upstream repositories: a submodule pins a commit correctly but drags in whole
repositories (`dotnet/skills` carries 106 `SKILL.md` across 16 plugins, of which this
conversion wants 19), gives no per-file hash to verify, cannot express a per-skill
`reason`/`owner`/`reviewedOn` audit trail, and needs `git submodule update` on every clone
and every CI job — a live cost under C-01. A second rejected alternative was **fetching at
execution time**, which proposal §20.2 forbids in as many words: "Do not let every agent
clone the latest upstream skill at execution time."

The sync therefore does exactly three things per lockfile entry: fetch the source
repository at its pinned commit into a temporary directory, copy the skill's whole folder
to its `destination`, and write the SHA-256 back into `computedHash`. `verify-skills.ps1`
recomputes and compares, and nothing else — it is the piece CI will run
([[TOOL-003]], plan handle `DSK-12-03`), so it must stay small and Linux-clean.

## Governing docs

The ticket carries `refs: []` and **`docs_todo: true`**, which is the normal state on this
board — the conversion's own decision records do not exist yet.

> **New ADR** — ADR-0110 (agent-skill pinning, the lockfile and vendored revisions, and the
> invocation/review protocol), authored by [[TOOL-008]] (plan handle `DSK-12-08`), filename
> `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md`. This plan is written to the
> decision as recorded in `docs/desktop/12-agent-tooling/README.md` § 3 ("Agents never fetch
> a moving `main`; a skill update is a reviewed PR that bumps the commit in the lockfile and
> re-runs the sync script") and in the reserved ADR block at
> `docs/desktop/00-governance-and-workflow/README.md` § 3. If ADR-0110 lands differently
> this plan is revised before implementation. **Do not claim this ticket "meets ADR-0110":
> the ADR does not exist yet, and [[TOOL-008]] explicitly depends on this ticket shipping
> first so the ADR describes a real file rather than a wish.**

Programme-level authorities this plan does meet, and which step satisfies each:

| Authority | Requirement | Met by |
| --- | --- | --- |
| Proposal §20.2 (pinning and vendoring) | Skills are vendored at a pinned commit; no execution-time fetch | Steps 3–4, 6 |
| `docs/desktop/12-agent-tooling/README.md` § 3 | Lockfile at `eng/skills/skills.lock.json` records source, commit, skill path, destination, hash, date reviewed, owner, reason | Steps 2, 9 |
| `docs/desktop/12-agent-tooling/README.md` § 4 (exit gate) | `verify-skills.ps1` green locally | Steps 4, 8 |
| `docs/desktop/12-agent-tooling/skill-routing.md` § Not applicable | Nothing on the never-vendored list is vendored; the five reference-only skills stay vendored and unloaded | Step 10 |
| L-04 (locked, `docs/desktop/README.md`) | Every ticket names its subagent, skills and MCP tools — verifiably | The lockfile is what makes the names resolvable |
| C-01 (2026-08-23) | Repository weight and CI minutes are live costs | Step 5 (binary payload is a recorded decision, not a default) |
| `AGENTS.md` § New Markdown placement | No `.md` outside `docs/(prd\|frd\|adr\|design\|desktop)` and the other allowed roots | Step 12; `eng/` is **not** an allowed root |

## Routing

Copied from the ticket body's `## Routing` block.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (`sandbox_mode = "workspace-write"`, `model_reasoning_effort = "high"`).
- **Skills**, in load order:
  1. `pegasus-desktop` — `.agents/skills/project/pegasus-desktop/SKILL.md` (project skill,
     tracked, always first)
  2. `directory-build-organization` — `dotnet/skills` `98f84851`,
     `plugins/dotnet-msbuild/skills/directory-build-organization/SKILL.md`
  3. `authoring-github-workflows` — `dotnet/skills` `98f84851`,
     `.agents/skills/authoring-github-workflows/SKILL.md`
  4. `kanmer-plan`, `kanmer-execute` — `.grok/skills/<name>/SKILL.md` (Kanmer 0.1.0)

  **Bootstrap order matters and is not a formality:** until this ticket lands there is no
  `.agents/skills/vendor/dotnet/` to read entries 2 and 3 from, so read them from the
  upstream repository at the pinned commit. Do not "load" them from a path this ticket has
  not created yet.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) only for
  PowerShell API facts such as `Get-FileHash` semantics. No Azure MCP.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Gates confirmed by
  `get_doc_gates TOOL-002` on 2026-08-24: `leave-preparing` needs `plan` +
  `questions-resolved`; `enter-done` needs `proof` + `questions-resolved`. There is **no**
  `research`, `files` or `checklist` gate on a `chore`. Call `get_doc_gates TOOL-002`
  before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's 13 implementation steps in the same order and with the same
ownership. Where a body step says "decide and record", the plan gives the recommended
default and its reason; the implementer still records the final choice.

1. **Orientation.** Read `EPIC-013/context.md` (`get_group_doc EPIC-013 context.md`) once
   — it carries the traps for the whole epic — then the plan sections named in the body's
   **Source of truth**. `get_doc_gates TOOL-002`, then `take_ticket`. Read
   [[TOOL-001]]'s (`DSK-12-01`) research verdict paragraph before writing any destination
   path: if it says Codex does not scan `.agents/skills`, stop and re-plan rather than
   syncing into a directory nothing reads.
2. **Confirm the draft's shape rather than retyping it.** `docs/desktop/12-agent-tooling/skills.lock.draft.json`
   is 382 lines. Verified 2026-08-24: `version: 2`; `policy.vendorRoot = ".agents/skills/vendor/"`;
   three sources — `dotnet/skills` `98f848512e9ee4877e399a0ae367bb5e4a193144` (2026-08-21),
   `microsoft/win-dev-skills` `f1028dd5bb19af59df400cb4a2ab867e40a40a4a` (v0.5.0,
   2026-07-22), `microsoft/azure-skills` `1a03acfb9ac1a1a05518bf7420d4618cc41847be`
   (2026-08-21); **35 entries split 19 `dotnet-skills` / 8 `win-dev-skills` /
   8 `azure-skills`** — the same split the body's **Source of truth** now states. Copy the
   file to `eng/skills/skills.lock.json` and change only the `computedHash` values and the
   `generatedBy` string. Never substitute a branch name for a commit SHA.
3. **Write `eng/skills/sync-skills.ps1`.** Per lockfile `source`, once (not once per
   entry — 35 clones of three repositories is the naive shape and it is slow):
   - `git clone --filter=blob:none --no-checkout <repository> <tmp>/<source>` then
     `git -C <tmp>/<source> checkout <commit> -- <paths>`.
   - **`skillPath` names a file, not a directory.** Every one of the 35 entries ends in
     `SKILL.md` (verified 2026-08-24). The body's step 3 requires copying "the whole skill
     folder — not only `SKILL.md`", so the script must take `Split-Path -Parent` of
     `skillPath` and check out that directory. Getting this wrong silently drops
     `winui-search.exe`, the analyzer DLL and every `references/` file, and the verifier
     will still pass because it only hashes what the lockfile lists.
   - Copy the folder to `destination`, compute SHA-256 for every copied file, write the
     per-skill `computedHash` back.
   - Provide `-Verify` to compute and compare without copying.
   - Delete files at a `destination` that the source no longer has, or the tree accumulates
     removed skills and step 7's idempotency check still passes.
4. **Write `eng/skills/verify-skills.ps1`.** Recompute hashes for every `destination` in
   `eng/skills/skills.lock.json`; exit non-zero naming each drifted or missing path; print
   the number of skills verified on success (the body's Verification expects that line).
   It must run under `pwsh` on `ubuntu-latest`, because [[TOOL-003]] puts it in the
   `changes` job (`.github/workflows/ci.yml:12-15`, `runs-on: ubuntu-latest`,
   `timeout-minutes: 5`). Concretely: forward slashes, `Join-Path`, no `Get-Acl`, no
   `Get-AppxPackage`, no registry, no `-Path` comparisons that assume case-insensitivity,
   and read files as bytes so CRLF/LF normalisation cannot change a hash. Match the calling
   style of its neighbours — `.github/workflows/ci.yml:55-60` invokes
   `./scripts/Test-TestShard.ps1` and `./scripts/Test-MigrationGrants.ps1` with
   `shell: pwsh`.
5. **Decide and record the non-Markdown payload.** Measured 2026-08-24 in the existing
   `.codex/skills/` tree: `winui-design/winui-search.exe` 7,911,936 B;
   `winui-dev-workflow/analyzer/Microsoft.WindowsAppSDK.Analyzers.dll` 49,664 B;
   `winui-session-report/Analyze-Session.ps1` 45,966 B. Total tree 7.9 MiB, of which the
   `.exe` is 98%.
   **Recommended default: commit and hash them.** Reasons: (a) the whole point of §20.2 is
   reproducibility, and an on-demand fetch reintroduces the execution-time network
   dependency the ADR is about to forbid; (b) a fetch-on-demand path has to work on
   `ubuntu-latest` inside a 5-minute job, and a Windows-only `.exe` fetched on Linux is
   pure cost; (c) 7.9 MiB is a one-time clone cost, not a per-CI-run cost. The counter —
   C-01 makes repository weight real — is why this must be *written down with its number*
   rather than defaulted silently. If the implementer chooses fetch-on-demand instead,
   record how `verify-skills.ps1` treats an absent binary (it must not silently pass).
   Record the choice in the lockfile `policy` block and under a dated heading here.
6. **Run the sync and bound the blast radius.** `pwsh ./eng/skills/sync-skills.ps1`, then
   `git status --porcelain` must show changes **only** under `.agents/skills/vendor/**` and
   `eng/skills/**` — nothing under `src/`, `tests/`, `scripts/`, `.codex/` or `docs/`.
7. **Prove idempotency.** Run the sync a second time; `git status --porcelain` must be
   clean afterwards. The two things that break this in practice are line-ending
   normalisation on copy and a timestamp or `generatedBy` date written into the lockfile on
   every run — do neither.
8. **Prove the verifier bites.** Append one character to a vendored `SKILL.md`, run
   `pwsh ./eng/skills/verify-skills.ps1`, confirm non-zero exit **and that the log names
   that exact path**; `git checkout --` the file and confirm exit 0.
9. **No `TBD` may survive.** `grep -c '"computedHash"' eng/skills/skills.lock.json` → `35`;
   `grep -c 'TBD' eng/skills/skills.lock.json` → `0`.
10. **Check the never-vendored group, not the whole do-not-load table.** The body's step 10
    now states this distinction in full and gives the authoritative PowerShell block; this
    plan adds only the on-disk form of the same check and the reason it matters.

    `docs/desktop/12-agent-tooling/skill-routing.md` § "Not applicable to this conversion
    (do not load)" (`skill-routing.md:56-70`) is a **loading** rule, not a vendoring rule.
    Verified against the draft lockfile on 2026-08-24: five of its entries are in the
    lockfile **on purpose** — `winui-wpf-migration`, `winui-session-report`,
    `dotnet-aot-compat`, `configuring-opentelemetry-dotnet` and `create-custom-agent`, whose
    own `reason` fields say "reference only", "user-invoked only", "deferred until startup
    is profiled". A check phrased against the whole table is false by exactly five and stops
    the ticket for no reason. Run the body's block and expect `0`, `0`, `5`, `35`.

    The on-disk complement, which also catches a sync that wrote somewhere unexpected:
    `ls .agents/skills/vendor/azure/` must be exactly the eight lockfile entries
    (`azure-resource-lookup`, `azure-resource-visualizer`, `azure-cost`,
    `azure-diagnostics`, `azure-compliance`, `azure-validate`, `azure-storage`,
    `appinsights-instrumentation`) with no `azure-deploy`, `azure-prepare`,
    `azure-app-onboard*`, `azure-cloud-migrate`, `azure-enterprise-infra-planner`,
    `python-appservice-deploy`, `entra-*`, `azure-kubernetes`, `airunway-aks-setup`,
    `azure-aigateway`, `microsoft-foundry`, `azure-ai`, `azure-messaging`, `azure-kusto`,
    `azure-upgrade`, `azure-reliability` or `azure-quotas`; and
    `ls .agents/skills/vendor/dotnet/` must hold 19 entries with no skill from the
    `dotnet-maui`, `dotnet-blazor`, `dotnet-template-engine`, `dotnet-test-migration`,
    `dotnet11`, `dotnet-ai` or `dotnet-advanced` plugin families. Record the counts
    (19 / 8 / 8 = 35) alongside the body's four numbers.

    `EPIC-013/context.md` already resolves the one live contradiction the routing matrix
    creates: `create-custom-agent` appears both in the area-12 routing row as "(reference
    only)" and on the do-not-load table. It is vendored, and **the do-not-load table wins —
    never load it.** The body's **Documentation changes** entry splitting that table into
    its two categories is what stops this recurring; it is presentational and moves no row
    between categories.
11. **Decide and record the fate of the root `skills-lock.json`.** It is a different file:
    `version: 1`, four `mattpocock/skills` entries (`domain-modeling`, `grill-me`,
    `grill-with-docs`, `grilling`) with real `computedHash` values and **no skill bodies in
    the tree** (verified 2026-08-24). **Recommended default: leave it exactly as it is**,
    and say so in one sentence — it belongs to a different tool's convention, folding it
    into a `version: 2` schema would either invent destinations for skills that are not
    vendored or add four more unresolved names, and deleting it is out of scope and
    unrequested. Plan § 3 permits either. Do not silently delete it.
12. **Run the two documentation gates the new tree can break.**
    `pwsh ./scripts/Test-DocumentationLinks.ps1` — expect
    `All relative Markdown links resolve (<n> files checked).` Note that its exclusion
    regex at `scripts/Test-DocumentationLinks.ps1:14` is
    `^(node_modules|corpus|artifacts|\.git|\.claude|\.agents|\.codex|\.kanmer)/`, so the
    vendored Markdown is **not** link-checked — a broken relative link inside a vendored
    skill will not be caught, which is fine because vendored content is not edited.
    `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base with dev> -Head HEAD` —
    expect `Markdown placement passed for <base>..<head>.` The allowed-root regex at
    `scripts/Test-MarkdownPlacement.ps1:31` is
    `^((docs/(prd|frd|adr|design|desktop))|workspaces/document-extraction|\.agents/skills|\.design-sync|\.grok|\.stitch|design/planning-and-old-designs)/.+\.md$`.
    `.agents/skills` is allowed — so the 35 vendored `SKILL.md` files pass, and so does the
    `skill-routing.md` edit under `docs/desktop/`. **`eng/` is not** — so no `.md` may be
    added under `eng/skills/`, not even a README. The procedure text belongs in
    `docs/runbook.md` and is [[TOOL-010]]'s (`DSK-12-10`) work.
13. **Record the Appendix C evidence** in the post-implementation report: skills consulted
    with their pinned SHAs; the commands run verbatim; both sync runs; the drift test's red
    and green output; the step 5 binary decision with its byte count; the step 11 decision;
    and from step 10 both the body's four counts (`0`, `0`, `5`, `35`) and the three
    per-source directory counts (19 / 8 / 8).

## Verification

Evidence tier **1 — Static/build/architecture**, as the body states. It proves the vendored
tree matches the lockfile hashes and that the verifier fails on drift; it proves nothing
about whether any agent ever read a skill. `proof` is a `command-log`.

Run and capture verbatim:

1. `pwsh ./eng/skills/verify-skills.ps1` → exit 0, plus the line naming the number of
   skills verified (expect 35).
2. `pwsh ./eng/skills/sync-skills.ps1; git status --porcelain` → empty output (idempotency).
3. Drift test: append one byte to `.agents/skills/vendor/windows/winui-design/SKILL.md`,
   `pwsh ./eng/skills/verify-skills.ps1` → non-zero exit with that path named; then
   `git checkout -- <path>` and re-run → exit 0. **Both halves are the evidence** — a green
   run alone does not prove a gate exists.
4. `grep -c 'TBD' eng/skills/skills.lock.json` → `0`;
   `grep -c '"computedHash"' eng/skills/skills.lock.json` → `35`.
5. The body's step-10 PowerShell block → `0` never-vendored names, `0` skills from the seven
   excluded plugin families, `5` reference-only skills present, `35` entries in total.
6. `ls .agents/skills/vendor/windows/ | wc -l` → `8`;
   `ls .agents/skills/vendor/azure/ | wc -l` → `8`;
   `ls .agents/skills/vendor/dotnet/ | wc -l` → `19`.
7. `pwsh ./scripts/Test-MarkdownPlacement.ps1 -Base <merge-base> -Head HEAD` →
   `Markdown placement passed for <base>..<head>.`
8. `pwsh ./scripts/Test-DocumentationLinks.ps1` →
   `All relative Markdown links resolve (<n> files checked).`

## Risks / open questions

| Risk | Mitigation |
| --- | --- |
| The upstream repositories may not actually carry the binaries. `winui-search.exe` (7.9 MiB) exists in the local `.codex/skills/` copy, but whether `microsoft/win-dev-skills` ships it in-tree at `f1028dd5` — as opposed to a release asset or Git LFS — is **unverified**. If it is LFS, `--filter=blob:none` plus a plain checkout yields a pointer file and the hash is wrong. | Step 3: after the first checkout, list the copied files and compare against the local `.codex/skills/` tree (18 files across the 8 WinUI skills, sizes recorded in the body). If a file is an LFS pointer (starts `version https://git-lfs`), that is the finding — record it and take the fetch-on-demand branch of step 5. |
| A check written against the whole do-not-load table stops the ticket on five entries that belong in the lockfile. | Body step 10 and plan step 10 both state the two categories and give the exact expected counts; the body's **Documentation changes** entry splits the table so the ambiguity does not survive this ticket. |
| A rename or removal upstream breaks routing names, and the hash verifier cannot see it. | Out of scope here (pins do not move in this ticket); it is exactly what [[TOOL-010]]'s rename rule exists for. Named so the reviewer sees it was a decision. |
| Line-ending normalisation makes the sync non-idempotent on Windows. | Step 7 is the detector. Copy bytes, do not round-trip text; check `.gitattributes` before assuming. |
| The 5-minute `changes` job budget. | Not this ticket's gate, but time the verifier locally and record the number so [[TOOL-003]] step 5 has it. |
| Deleting a stale `destination` could delete something else if a `destination` is wrong. | Constrain deletion to paths under `policy.vendorRoot` and refuse anything outside it. |

Open questions: **none opened as a blocking `open-questions` document**, and not because
opening one would be costly — an unticked box would block `leave-preparing`, `enter-review`
and `enter-done` (never `leave-backlog`), which is a perfectly acceptable price for a real
question. There simply is no real question here. The two "decide and record" items (binary
payload, root `skills-lock.json`) both have a recommended default with its reason in steps 5
and 11, and the authoring contract says to take a trivial default and say you took it. The
one genuine unknown (LFS vs in-tree binaries) is answered by a read-only check inside step 3,
not by asking anyone. Nothing in this ticket's body instructs that a question be recorded in
`open-questions/`.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch is **not**
docs-only — it adds two PowerShell scripts and a lockfile — so `n/a — docs-only` is not
available; the four lenses (reuse, simplification, efficiency, altitude) must be applied to
`sync-skills.ps1` and `verify-skills.ps1` and their dispositions recorded._
