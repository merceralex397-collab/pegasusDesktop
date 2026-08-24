# Plan — REL-016: DSK-09-18 · Desktop release table and compatibility range in `docs/operations.md`

**Diff estimate: ~2 files, ~35 lines.** `docs/operations.md` gains a `### Desktop releases`
subsection — a heading, a short authoritative-table paragraph, an eight-column table with one
real row, a compatibility-range sentence, a certificate-facts paragraph and the same-task
refresh rule (~30 lines); `docs/capabilities.md` gains **one changed line** — the trailing
clause of the `OPS-10` row at `:73`. A third file,
`docs/desktop/09-release-update-and-distribution/runbooks.md`, gains three one-line pointer
edits (R1 step 9, R2 step 7, R8 step 2), so the honest count is ~3 files if those are
included in the same commit. `docs/engineering.md:201-207` § Plan sizing requires the estimate
first.

## Approach

**Copy the gateway table's conventions rather than inventing new ones, and change exactly one
line of `docs/capabilities.md`.** `docs/operations.md:280-332` already carries an
authoritative release table with a settled house style: a heading, a short paragraph, then
rows newest-first with abbreviated identifiers (`05fe7a7f…`, `sha256:90b58000…`). Putting the
desktop table immediately after the gateway table under the same § Production environment
heading is what makes the compatibility join visible — the desktop row names **gateway release
numbers**, and the reader can see them one table up.

The alternative rejected was **a separate top-level section or a new file**. It would put the
two release trains in different places while their only interesting property is how they join,
and it would create a second place where release facts drift apart — the failure this
repository already demonstrates at `docs/operations.md:295`, where the narrative says
"release 14" against a table whose newest row is 20.

For the `docs/capabilities.md` half of D-004 the approach is **minimal and surgical**: change
the trailing acceptance clause and nothing else, so `git diff --numstat` shows one file and
one line. That constraint is not stylistic — a capability row is a registry entry, and a
diff that touches its id, horizon, target release or canonical owner is a different change
that needs different review.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-016`). No existing PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by `DSK-09-01` (board `REL-001`). Its Decision clause (e) —
> gateway first and backward compatible, desktop second, minimum client version raised last —
> is what the compatibility range column records, and its Decision clause (c) fixes the
> version shape the first column carries. This plan is written to the decisions as recorded in
> `docs/desktop/09-release-update-and-distribution/README.md` § 3 ("Desktop release manifest")
> and § 8; if ADR-0105 lands differently, this plan is revised before implementation.

Existing documents this plan **meets**:

- **`AGENTS.md` § Safety rails** — current-state documents are refreshed in the same task as
  the release. **Meets**: step 8 writes that rule into the section itself, so the obligation
  travels with the table rather than living only in `AGENTS.md`.
- **`docs/capabilities.md:73`**, capability `OPS-10`. **Meets, by amendment under D-004**:
  step 9 replaces "operator acceptance outstanding" with the statement that acceptance closes
  with the desktop pilot approval (`DSK-09-11`, board `REL-009`). This is an amendment the
  operator authorised on 2026-08-24, not a unilateral edit, and it is made **only once that
  approval record exists**.
- **`docs/adr/0014-local-to-production-deployment.md`** — the `OPS-10` row's canonical owner.
  Unchanged: this ticket touches the row's note, not its owner link.

Binding operator decisions, written to as settled:

- **D-002** (2026-08-23) — the signer column records the **self-managed certificate's** subject
  and thumbprint. There is no Azure signing identity to record.
- **D-003** (2026-08-23) — the channel column is `pilot` or `prod`, the UNC feed's two folders.
  Not a URL, not a container.
- **D-004** (2026-08-24) — `OPS-10`'s outstanding operator acceptance folds into the desktop
  pilot approval and does **not** close separately against the current web client. **This
  ticket owns the resulting one-line `docs/capabilities.md` change**; `DSK-09-11` (board
  `REL-009`) owns the approval record itself and the `docs/desktop/README.md` § Locked
  decisions entry. Upstream `TICK-001` stays dropped and no ticket is imported for it.

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in the
plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) → `pegasus-release`
  (`.agents/skills/pegasus-release/SKILL.md`, verified present) for the release-record
  conventions and the same-task refresh rule.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`).
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates REL-016` before
  every move; a move crosses at most one gated boundary. `get_doc_gates` reports two gated
  boundaries: `leave-preparing` needs `plan` (this document), `enter-done` needs `proof`.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's thirteen implementation steps in the same order, with the same
ownership and the same paths.

1. **Orient and take.** Read the area plan § 5 row `DSK-09-18`, § 3 "Desktop release
   manifest" and § 8, and `runbooks.md` § R1 step 9. `get_doc_gates REL-016`, then
   `take_ticket REL-016`.
2. **Read the existing table and copy its conventions.** `docs/operations.md:280-332`:
   a `## Production environment` heading, a short paragraph, then the release table at
   `:311-332` with columns `Release | Date | Source revision | Image digest | Web revision |
   Migration`, rows **newest-first**, and abbreviated identifiers in the house style
   (`05fe7a7f…`, `sha256:90b58000…`). The convention the paragraph carries is that **the table
   is authoritative**; copy it, do not invent a new one.
3. **Add a `### Desktop releases` subsection under § Production environment, after the gateway
   release table**, so the two are read together and the compatibility join is visible one
   table apart rather than one file apart.
4. **Give the table exactly these eight columns, in this order**: `Desktop version` | `Date` |
   `Source revision` | `Package SHA-256` | `Signer (subject · thumbprint)` | `Channel` |
   `Gateway compatibility (min–max tested)` | `Ring`. Every one comes from
   `desktop-release-manifest.json` (`DSK-09-02`, board `REL-002`) — `version`, `createdAtUtc`,
   `sourceCommit`, `packageSha256`, `signerSubject`+`signerThumbprint`, `channel`,
   `minimumGatewayRelease`+`maximumTestedGatewayRelease` — so a row can be filled
   mechanically from the release artefacts rather than transcribed by hand.
5. **Add the first row from the pilot release shipped by `DSK-09-11` (board `REL-009`)**,
   using the **real recorded values — never placeholders**. Abbreviate the revision and hash
   the way the gateway table does, so the columns stay readable.
6. **Write the compatibility-range sentence explicitly.** The range names **gateway release
   numbers** from the table above (for example "20–20", 20 being the newest gateway row at the
   time of writing, dated 2026-08-22), not commit SHAs and not capability ids. Record what
   happens when the range is exceeded: the minimum-version gate refuses the client
   (`DSK-04-06`, board `GWY-023`).
7. **Add the certificate-facts paragraph** from `DSK-09-08` (board `REL-007`) step 13:
   subject, thumbprint, validity window, and the **90-day expiry warning date** that R5 step 1
   requires. Write the date, not the rule — a date is actionable.
8. **State the refresh rule in the section itself**: this table is updated in the **same task**
   as each desktop release, per `AGENTS.md` § Safety rails, and **the table — not any
   surrounding prose — is authoritative**.
9. **Apply the D-004 half this ticket owns, in one line.** Change the note in the `OPS-10` row
   of `docs/capabilities.md:73` so it reads that **operator acceptance closes with the desktop
   pilot approval (`DSK-09-11`)**, replacing "operator acceptance outstanding". Keep the rest
   of the row **byte-identical**: capability id `OPS-10`, title "Production environment
   deployed directly from an authorised terminal", horizon `Now`, target release
   `0.1.0-alpha.1`, the `[ADR-0014](adr/0014-local-to-production-deployment.md)` link, and the
   clause "Executed for releases 1–3 ([operations — production environment](operations.md#production-environment)
   owns the evidence)" — so only the trailing acceptance clause changes. The decision was taken
   by the operator on 2026-08-24 and is **not re-opened here**: do not restate its reasoning in
   `docs/capabilities.md`. Make the edit **only once `DSK-09-11`'s pilot approval record
   exists**, and touch no other row of that file.
10. **Do not silently fix the existing gateway drift.** `docs/operations.md:295` reads "the
    estate currently serves **release 14**" against a table whose newest row is 20. It is out
    of this ticket's scope. Raise a `fix` ticket in the `delivery-repository` area (prefix
    `DELIV`, configured in the board's `data/board.yml`; it holds no tickets yet) naming the
    exact line, and reference that ticket from this document. `CHANGELOG.md`, which stopped at
    2026-08-03, is likewise not this ticket's to fix.
11. **Point the three runbook steps at the new subsection by its exact heading** — R1 step 9,
    R2 step 7 and R8 step 2 in
    `docs/desktop/09-release-update-and-distribution/runbooks.md` — so each runbook names
    where the row goes rather than saying "the operations table".
12. **Run the gates.** `pwsh ./scripts/Test-DocumentationLinks.ps1` and
    `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`, both exit `0`. The second name is correct:
    it is the script `.github/workflows/ci.yml:83` runs in the `documentation` job, and it
    exercises `scripts/Test-MarkdownPlacement.ps1`.
13. **Request review and record the pass.** Review by `pegasus-desktop-reviewer`, and the
    dated `## Simplification pass` in this document as `n/a — docs-only`.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture.** The obligation is a
link-checked, correctly placed documentation change **whose values reconcile against the
release manifest**; it proves nothing about the release itself, which `DSK-09-11` (board
`REL-009`) proved. `proof` is the gate output and the `grep`/`git diff` results as
`command-log`, plus the field-by-field reconciliation.

| Command / observation | Expected evidence |
| --- | --- |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exit code `0` |
| `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` | exit code `0` |
| `grep -n "### Desktop releases" docs/operations.md` | exactly one match |
| `grep -n "OPS-10" docs/capabilities.md` | one row whose note names the desktop pilot approval (`DSK-09-11`) and contains **no** "operator acceptance outstanding" |
| `git diff --numstat docs/capabilities.md` | one file, one line changed |
| Cross-check the first row against `desktop-release-manifest.json` from the pilot release | version, source revision, package SHA-256 and signer thumbprint match **field for field** |

Behaviour to read rather than infer: the first row carries **real values, not placeholders**;
the compatibility range names gateway release numbers rather than commits; and the certificate
paragraph carries an actual warning **date**.

## Risks / open questions

- **Risk — repeating the gateway table's drift.** `docs/operations.md:295` contradicts its own
  table and `CHANGELOG.md` stopped at 2026-08-03. Mitigation: step 8 puts the same-task refresh
  rule and the "the table is authoritative" sentence **inside the new section**, so it travels
  with the table.
- **Risk — the `docs/capabilities.md` edit sprawls.** A capability row is a registry entry;
  touching its id, horizon, target release or canonical owner is a different change needing
  different review. Mitigation: step 9's byte-identical rule and the `git diff --numstat`
  verification.
- **Risk — the `OPS-10` note is amended before the approval exists.** It would claim an
  acceptance that has not happened. Mitigation: step 9 makes the edit conditional on
  `DSK-09-11`'s (board `REL-009`) approval record existing; the ticket's `Depends on` says the
  same.
- **Risk — placeholders in the first row.** A table with `<ver>` in it is worse than no table,
  because it looks authoritative. Mitigation: step 5 and the sixth verification command, which
  reconciles field for field against the manifest.
- **Risk — the compatibility range is written as commit SHAs or capability ids.** Capability
  ids and release numbers are different namespaces. Mitigation: step 6 states the rule and
  gives a worked example.
- **Risk — fixing the pre-existing drift here.** It would make this diff unreviewable against
  its own scope. Mitigation: step 10 raises a separate `fix` ticket and references it.
- **Open questions**: none. D-004 is decided — the `OPS-10` note is amended once, pointing at
  `DSK-09-11`'s approval — and the table's shape is fixed by the area plan § 3 and the existing
  gateway table's conventions. **No `open-questions` document is created**, and in particular
  none for `OPS-10`, which the operator settled on 2026-08-24.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this branch's
own diff before the PR, recorded here under a dated heading. This branch is documentation-only,
so the expected record is `n/a — docs-only`._
