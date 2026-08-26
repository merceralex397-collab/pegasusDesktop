# Plan — REL-001: DSK-09-01 · Author ADR-0105: MSIX/App Installer distribution and the minimum-version gate

**Diff estimate: ~2 files, ~150 lines.** One new file
`docs/adr/0105-msix-app-installer-and-minimum-version-gate.md` (~145 lines: 10 lines
of frontmatter, Context, a five-clause Decision, Consequences, a six-row
cloud-justification table, Relates) and one added row in `docs/adr/README.md`.
`docs/engineering.md:201-207` § Plan sizing requires the estimate first; this one is
derived from the files this ticket may touch, which its Guardrails limit to `docs/adr/`.

## Approach

Write ADR-0105 as a **record of decisions already taken**, not as an evaluation. Every
clause the body's steps 4–8 name is already settled in
`docs/desktop/09-release-update-and-distribution/README.md` § 3 and in
`signing-and-hosting-decision-matrix.md` (D-002 and D-003, both decided 2026-08-23), so
the ADR's job is to move those sentences into the repository's durable decision log at
the one path all three claimant tickets name. The alternative considered and rejected
was **taking the next free number (ADR-0030)**, which `AGENTS.md` § ADR conventions
would normally require: it is rejected because the operator confirmed the reserved block
ADR-0100–ADR-0110 on 2026-08-23 precisely so a one-way sync from the still-active
upstream `collisionengineers/pegasus` ADR sequence (29 issued, active) cannot collide.
The second alternative — writing a **second** ADR-0105 owned by area 09 alongside one
written by area 00 or 04 — is rejected by the ticket's own Guardrails: one number, one
file, first-to-work authors and the other two extend in place.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`, as
`get_doc_gates REL-001` reports. There is therefore no existing PRD/FRD/ADR this plan
can claim to meet, and it must not pretend otherwise.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by [[REL-001]]; see [[REL-001]]'s plan for the
> ownership reconciliation — that is this document, and the reconciliation is the
> **Ownership** paragraph below plus the two boxes in this ticket's `open-questions`.
> This plan is written to the decisions as recorded in
> `docs/desktop/09-release-update-and-distribution/README.md` § 3 (two-layer
> enforcement, versioning, channels, order of deployment, known-good package, signing,
> feed hosting) and in `signing-and-hosting-decision-matrix.md` (D-002 self-managed
> certificate, D-003 UNC share, both 2026-08-23). If the ADR lands differently from
> those sections, this plan is revised before implementation.

Existing ADRs this one **relates to** and must not modify:

- **ADR-0007** (`docs/adr/0007-direct-terminal-azure-deployment.md`) — the gateway
  release route runs from an authorised Windows terminal. ADR-0105 leaves it untouched;
  step 8 records the relationship only.
- **ADR-0014** (`docs/adr/0014-local-to-production-deployment.md`) — local and
  production only, no Azure test environment. L-02 keeps it standing; ADR-0105 must not
  imply a test feed in Azure.
- **FRD-13** does not exist yet; `docs/frd/README.md` lists FRD-01…FRD-12. Step 8 adds a
  forward pointer only, phrased as a pointer to work [[FND-008]] (plan handle `DSK-00-08`)
  will do — never as a claim that FRD-13 exists.

**Ownership.** ADR-0105 has three claimants: this ticket,
[[FND-005]] (plan handle `DSK-00-05`, "Author ADR-0100, ADR-0101, ADR-0103, ADR-0104,
ADR-0105 and ADR-0110 in the reserved block") and
[[FND-042]] (plan handle `DSK-04-01`, "Author ADR-0102 … and ADR-0105"). The body's
reconciliation binds on **execution**: one filename, and the first of the three to be
worked authors it while the other two extend it in place. It does **not** settle who owns
the row — the body says in the same paragraph that this is "an ownership question for the
operator to settle before Phase 2, not something the first agent to start decides
silently", and that the change of shape is recorded in `open-questions/`. Both are now
unticked boxes in this ticket's `open-questions` document, which is what the body asked
for. Step 2 below still makes the board check mandatory before anything is created.

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in
the plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) → `kanmer-docs`
  (`.grok/skills/kanmer-docs/SKILL.md`, Kanmer 0.1.0).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `link_doc`, `move_item`); Microsoft Learn
  (`microsoft_docs_search`, `microsoft_docs_fetch`) for the App Installer
  update-settings claims quoted in the ADR.
- **Kanmer pipeline** for profile `chore`: `kanmer-plan` → `kanmer-execute` →
  `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call `get_doc_gates REL-001`
  before every move; a move crosses at most one gated boundary. `get_doc_gates` reports
  exactly two gated boundaries for this profile: **`leave-preparing` needs `plan` (this
  document) **and** `questions-resolved`**, and **`enter-done` needs `proof` **and**
  `questions-resolved`**. `leave-backlog` is not a gated boundary at all for a `chore` —
  verified 2026-08-24, it does not even appear in the boundary list.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's eleven implementation steps in the same order and with the same
ownership; they add the *how* the body leaves out.

1. **Orient and take.** Read
   `docs/desktop/09-release-update-and-distribution/README.md` § 3 and the § 5 row
   `DSK-09-01`, `signing-and-hosting-decision-matrix.md` in full, and proposal § 9 in
   `docs/desktop/Pegasus_Native_Desktop_Design_Proposal.md`. Then `get_doc_gates REL-001`
   and `take_ticket REL-001`. Read this ticket's `open-questions` document first — both of
   its boxes must be ticked before the ticket can leave Preparing, and one of them needs an
   operator.
2. **Resolve the three-way claim before creating anything.** Run Kanmer
   `search_items` for `ADR-0105`, and check `docs/adr/` on disk with
   `ls docs/adr/0105*`. Three outcomes, and the plan must record which applied:
   (a) no file and no other ticket has authored it → this ticket authors it, continue at
   step 3; (b) the file exists → this ticket becomes a **review** of that ADR against
   § 3 of the area 09 plan plus whatever area 09 still owes, and extends it in place;
   (c) another ticket is in `implementing` on it → stop and coordinate rather than race.
   Record the outcome in this document under a dated note **and in the second box of
   `open-questions`**, which is where the body requires the change of shape to be recorded.
   Measured 2026-08-24: outcome (a) — `ls docs/adr/0105*` returns nothing and all three
   claimants sit in `backlog`, untaken. Re-run it anyway; that is the whole point.
3. **Create the file at the single agreed path**,
   `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md`. Copy the frontmatter
   block shape from `docs/adr/0015-host-web-on-container-apps-consumption.md:1-10`,
   which is exactly:
   `id`, `status`, `date`, `supersedes`, `superseded_by`, `related_capabilities`,
   `related_frd`, `tags`. Fill it as `id: ADR-0105`, `status: accepted`, `date:` today,
   `supersedes: []`, `superseded_by: []`, `tags: [desktop, packaging, update]`. Confirm
   first that `AGENTS.md` § ADR conventions still carries the reserved-block sentence
   (it does today, at `AGENTS.md:80-88`: "the native-desktop conversion uses the reserved
   block ADR-0100–ADR-0110 instead of the next free number").
4. **§ Context** from proposal § 9.1: two compatible mechanisms — (a) the signed
   MSIX/App Installer launch update, (b) the gateway minimum-version gate with a
   pre-session `client-compatibility` endpoint and a client version on every
   authenticated request. State that nothing in `docs/adr/` records either today
   (ADR-0001…ADR-0029, 0017 never issued) and that proposal § 9 places substantially
   more logic in the client on the strength of the update path.
5. **§ Decision**, five clauses, each written as a decision and not as an option:
   (a) App Installer **2021 schema**
   (`http://schemas.microsoft.com/appx/appinstaller/2021`) with
   `OnLaunch HoursBetweenUpdateChecks="0" ShowPrompt="true" UpdateBlocksActivation="true"`,
   `AutomaticBackgroundTask` and `ForceUpdateFromAnyVersion` — the 2017/2 schema
   silently ignores the first three attributes;
   (b) the package check **fails open** when the feed is unreachable and the gateway gate
   **fails closed** after a short cached window — both layers are required;
   (c) package version `1.<minor>.<build>.0`, `build` = the CI run number, revision
   always `0`, distinct from the gateway's `<Version>0.1.0-alpha.1</Version>` at
   `Directory.Build.props:9`;
   (d) one package identity `CollisionEngineers.Pegasus` with two feeds, `pilot/` and
   `prod/`, and a ring change performed by reinstall;
   (e) gateway first and backward compatible, desktop second, minimum client version
   raised last.
6. **§ Consequences** recording D-002 and D-003 **by name and with the date
   2026-08-23**: signing is a self-managed certificate trusted per workstation in
   `LocalMachine\TrustedPeople` (never `Trusted Root`); the feed is a UNC file share
   served to App Installer over SMB; **the whole distribution path therefore touches no
   Azure resource and has no recurring cost**. Record the three accepted trade-offs in
   substance: a per-machine trust rollout, a renewal that must be rehearsed rather than
   remembered, and update checks that work on the office network or VPN only.
7. **§ Cloud-justification test** as a six-row table with the questions copied exactly
   from `docs/desktop/00-governance-and-workflow/README.md` § 3 (shared authority;
   unattended execution; protected credentials; public callback; central enforcement;
   measured operational advantage), each with a yes/no answer and evidence. The intended
   answers, which the ADR must state rather than leave to the reader: for the
   **minimum-version gate**, central enforcement is `yes` — a rule that must hold
   independently of the client, and a client whose policy overrides App Installer can
   bypass the package layer — which is what keeps the gate in the gateway; for the
   **feed**, all six are `no`, which is why it is an in-house share. "It is already in
   Azure", "the web app does it" and "it may scale later" are not answers.
8. **§ Relates**: ADR-0007 (gateway release route unchanged), ADR-0014 (two
   environments; no Azure test feed), and a forward pointer to FRD-13 *when*
   [[FND-008]] writes it — phrased so the link is not created before
   the file exists, or `scripts/Test-DocumentationLinks.ps1` fails.
9. **Index row** in `docs/adr/README.md`, in ADR-number order, matching the existing
   three-column shape `| [0105](0105-msix-app-installer-and-minimum-version-gate.md) |
   Signed MSIX / App Installer distribution with a gateway minimum-version gate | — |`
   under "Current architecture decisions (`status: accepted`)", after the 0029 row at
   `docs/adr/README.md:41`.
10. **Run the gates.** `pwsh ./scripts/Test-DocumentationLinks.ps1` and
    `pwsh ./scripts/Test-TestMarkdownPlacement.ps1`. Note the second name: the body
    writes `Test-TestMarkdownPlacement.ps1` and that is correct — it is the script
    `.github/workflows/ci.yml:83` runs in the `documentation` job, and it exercises
    `scripts/Test-MarkdownPlacement.ps1`, whose allowed-roots regex at
    `scripts/Test-MarkdownPlacement.ps1:31` already admits `docs/adr/`. Fix any broken
    relative link before opening the PR.
11. **Simplification pass.** Record it under a dated `## Simplification pass` heading in
    this document as `n/a — docs-only` (`AGENTS.md` § Repository task workflow step 4).

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture.** This ticket proves
documentation consistency only; it proves nothing about packaging behaviour. `proof` is
produced from the four commands below, run on the branch before the PR, with their
output pasted verbatim as proof type `command-log`.

| Command | Expected evidence |
| --- | --- |
| `pwsh ./scripts/Test-DocumentationLinks.ps1` | exit code `0`, no unresolved link reported |
| `pwsh ./scripts/Test-TestMarkdownPlacement.ps1` | exit code `0` — the new file is under `docs/adr/`, an allowed root (`scripts/Test-MarkdownPlacement.ps1:31`) |
| `ls docs/adr/0105*` | exactly one file, `0105-msix-app-installer-and-minimum-version-gate.md` |
| `git diff --name-only` | exactly `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md` and `docs/adr/README.md` |
| `get_doc_gates REL-001` | `questions-resolved` satisfied — both `open-questions` boxes ticked, with the operator's answer and the step-2 outcome recorded |

Behaviour to read rather than assume: open the rendered ADR and confirm each of the five
Decision clauses reads as a decision ("Pegasus signs…", "the package check fails open…")
and not as an option ("we could…"), and that the six-question table has six answers with
evidence in every row. That reading is the acceptance criterion the commands cannot check.

## Risks / open questions

- **Risk — a second ADR-0105 is created.** Two agents working area 00 and area 09 in
  parallel could both create the file. Mitigation: step 2 makes the `search_items` +
  `ls docs/adr/0105*` check mandatory *before* creation, and the outcome is recorded in
  this document and in the second `open-questions` box.
- **Open question, and it is open — which of the three claimants authors ADR-0105.**
  The body says it is for the operator to settle before Phase 2 and "not something the
  first agent to start decides silently", and it directs that the resulting change of
  shape be recorded in `open-questions/`. Both are now unticked boxes in this ticket's
  `open-questions` document.
  An earlier draft of this plan declined to open them, reasoning that "an unticked item
  would block every stage move". That reason was false and is withdrawn: an unticked box
  blocks `leave-preparing`, `enter-review` and `enter-done`, never `leave-backlog`, and for
  a `chore` the board declares only the first and the last of those three. Verified
  2026-08-24 with `get_doc_gates REL-001`: with `open-questions` present, `leave-preparing`
  is `passable: false` and `preparing` is still reachable. Blocking Preparing is the
  *intended* behaviour here — the body's whole point is that an agent must not start
  authoring before the operator has said who owns the row.
  The body's tie-break (first-to-be-worked authors, the other two extend in place) still
  governs execution once ownership is settled, and step 2 executes it.
- **Risk — an ADR number outside the reserved block.** `AGENTS.md` § ADR conventions
  still opens with "the next free number" and only then records the 2026-08-23 exception.
  Mitigation: step 3 requires reading `AGENTS.md:77-99` and confirming the reserved-block
  sentence before using 0105.
- **Risk — a forward link to FRD-13 breaks the link gate.** `docs/frd/README.md` lists
  FRD-01…FRD-12 only. Mitigation: step 8 writes the pointer as prose naming the ticket
  that will author FRD-13 ([[FND-008]]), with no relative link until the file exists.
- **Risk — ADR bodies are immutable once accepted.** If § 3 of the area 09 plan changes
  after this ADR is accepted, the correction is a new superseding ADR, not an edit. Noted
  here so the implementer does not "fix" the body later.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch is
expected to be documentation-only, so the expected record is `n/a — docs-only`._

## Reconciliation — 2026-08-26

The live ownership/file check resolved the remaining factual question:

- Kanmer search_items ADR-0105 found FND-005 done with the canonical ADR ref and FND-042 done with the same ref; no active authoring ticket was found.
- origin/dev and origin/main are both 36dccd8fa1c883c38977b6721d86b745c45c9a94, and git cat-file -e origin/dev:docs/adr/0105-msix-app-installer-and-minimum-version-gate.md succeeds.
- The canonical ADR is accepted and its index row already exists. It covers the two-layer enforcement, fail-open/fail-closed split, D-002, D-003, C-01, and the six-question table.
- The comparison against Area 09 §3 found four omitted release decisions: explicit 2021-schema/update attributes including AutomaticBackgroundTask; package version 1.<minor>.<build>.0 with CI run/build and revision 0; one identity with pilot/ and prod/ feeds and reinstall-based ring changes; and rollback's ForceUpdateFromAnyVersion="true".

Execution is therefore a review/reconciliation of the canonical file, never a second ADR-0105. The repository diff will be documentation-only and will not touch the ADR index, code, scripts, CI, operations, Azure, or upstream. The added text records the already-settled Area 09 decisions and does not claim package generation, deployment, or runtime proof. If the accepted-ADR rule is interpreted as forbidding even this explicitly owned clarification, stop before editing and record the governance conflict; do not invent a new ADR number.

## Simplification pass — 2026-08-26

n/a — docs-only. The change is limited to the canonical ADR clauses required by Area 09; no abstraction, helper, duplicate index row, or unrelated documentation is introduced.

## Implementation checkpoint — 2026-08-26

The canonical ADR now has an appended Area 09 release contract covering the explicit 2021 schema and update attributes, package version 1.<minor>.<build>.0, the single identity and pilot/prod ring model, rollback with ForceUpdateFromAnyVersion, the 2026-08-23 D-002/D-003 date, and the no-Azure/no-recurring-cost consequence. Validation passed:

- git diff --check
- git diff --name-only — only docs/adr/0105-msix-app-installer-and-minimum-version-gate.md
- pwsh -NoProfile -File ./scripts/Test-DocumentationLinks.ps1 — passed; all relative Markdown links resolve (235 files checked)
- pwsh -NoProfile -File ./scripts/Test-TestMarkdownPlacement.ps1 — passed
- Get-ChildItem docs/adr -Filter 0105* — exactly one file
- docs/adr/README.md — existing ADR-0105 row unchanged

## Review response — 2026-08-26

Independent review initially returned NEEDS CHANGES for the missing ADR-0014/FRD-13 Relates section and ambiguous “package manifest” wording. Commit 17c87e51 adds the required Relates section and names the App Installer file distinctly from Package.appxmanifest. The review scope remains one documentation file and no product decision or runtime claim was added.
