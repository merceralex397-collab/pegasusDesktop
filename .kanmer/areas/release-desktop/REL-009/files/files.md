# Files — REL-009

Surveyed on 2026-08-24 against the fork at branch `task/desktop-plan-segmentation`. Most of
this ticket is an **executed runbook**, so its main output is evidence rather than diff; the
repository changes are four documentation edits.

## Where the change lands

| Path | Why |
|---|---|
| `docs/desktop/09-release-update-and-distribution/runbooks.md` | **Edited, four changes.** § Conventions records the operator-confirmed approval phrase (it is currently *proposed*). § R1 step 7 is corrected from the HTTP check (`curl -I`, ranged `GET`) to the SMB check — `eng/packaging/Test-FeedShare.ps1` — because D-003's feed has no headers to inspect. R1's evidence list gains the `OPS-10` closure and the >1-hour download/export check. R1 is marked **proven** with its date. Breaks if step 7 is left as written: the next operator either runs a meaningless command or invents their own check. |
| `docs/desktop/README.md` § Locked decisions and open decisions | **Edited, one row.** The one-line **D-004** entry, in the same shape as the existing L-01…D-003 rows (columns `ID | Decision | Status | Owner plan`): `OPS-10` operator acceptance folds into the desktop pilot approval, decided 2026-08-24, owner plan 09. Required by the operator decision itself. |
| `docs/operations.md` | **Edited, one row added.** The first desktop release row — version, date, commit, package hash, signer, channel `pilot`, compatibility range — written **in the same task** per `AGENTS.md` § Safety rails. The `### Desktop releases` table itself is created by `DSK-09-18` (board `REL-016`); if it does not exist yet, coordinate rather than inventing a second table. |
| `docs/current-architecture.md` | **Edited only if the deployment boundary changed** with this release — the paragraph gains the desktop package and feed. If it did not change, leave it untouched and say so in the proof. |

**Operator artefacts that are not repository files** and must be captured in the ticket
proof: the tagged commit SHA (step 4); the build log and `.msix` SHA-256 (step 5);
`signtool verify /pa /v` output (step 6); validator output (step 7); the verbatim
`FEED PUBLISH GRANTED pilot <ver>` text (step 8); the feed listing after publication
(step 9); `Test-FeedShare.ps1` output (step 10); the pilot workstation's version screenshot
and `Get-AppxPackage` transcript, plus the >1-hour download and export results with three
timestamps (step 11); and the signed combined pilot-and-`OPS-10` approval text (step 12).

## Context files

Read these before starting the run. Each carries a rule or a value the run depends on.

| Path | What it tells the implementer |
|---|---|
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § Conventions | The approval phrase is **proposed, not confirmed**: "The implementing agent must confirm the phrase with the operator before first use." It also fixes that `MERGE AUTH GRANTED` keeps its single meaning — the `dev` → `main` promotion — and must not be extended to publishing. |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R1 | The five preconditions, nine steps, evidence list, rollback and — the part that keeps the proof honest — "Does not prove: production-ring behaviour on every workstation; telemetry (App Insights quota, PLAT-034); anything about the gateway's own release." |
| `docs/desktop/09-release-update-and-distribution/runbooks.md` § R9 | The publish procedure this run uses: package **first**, `.appinstaller` **last**, previous package retained, and step 4's verification from a non-publisher staff account. |
| `docs/desktop/09-release-update-and-distribution/README.md` § 3 | "Order of deployment" (gateway first and backward compatible, desktop second, minimum client version raised **last** — R1 raises nothing); "Pilot ring" (one or two internal users on the production gateway, because L-02 forbids an Azure test environment); "Publication" (pilot publish may be automated once D-002 is decided; production stays a runbook-controlled terminal step). |
| `.agents/skills/pegasus-release/SKILL.md` | The gateway release procedure this must **not** change, and the approval culture the new phrase mirrors. § The estate: "Read-only Azure checks need no approval. **Every write needs explicit operator approval for the exact target**, and the `main` update additionally needs the words `MERGE AUTH GRANTED`". Also its habit of refreshing `docs/current-architecture.md` and `docs/operations.md` in the same task. |
| `docs/operations.md:280-332` | The gateway release table: its columns (`Release | Date | Source revision | Image digest | Web revision | Migration`), its abbreviated-hash house style (`05fe7a7f…`, `sha256:90b58000…`), and its newest row — **release 20, 2026-08-22**. The compatibility range recorded for the desktop row names **gateway release numbers** from this table. |
| `docs/operations.md:295` | "the estate currently serves **release 14**" — a narrative line that contradicts its own table. This is the drift the desktop table must not repeat, and it is **not** fixed here (`DSK-09-18`, board `REL-016`, raises it as a separate `fix` ticket). |
| `docs/capabilities.md:73` | The `OPS-10` row and its exact trailing clause "operator acceptance outstanding." — the text D-004 replaces. **This ticket must not edit this file**; the change is `DSK-09-18`'s (board `REL-016`). Read it so the approval text you draft points at the right capability and the right releases. |
| `docs/runbook.md:776-781` | The live-operation approval matrix: reads are permitted with no per-target approval; every change needs explicit approval for the exact target. This ticket makes no Azure write; the production gateway is only read from. |
| `docs/desktop/README.md` § Locked decisions and open decisions | The table D-004's row joins, and the row shape to copy (`ID | Decision | Status | Owner plan`), with L-01…D-003 as the worked examples. |
| `scripts/Build-DesktopRelease.ps1` (created by `DSK-09-04`, board `REL-004`) | The command R1 step 2 runs, its parameters, and its single stdout result (the manifest path). |
| `eng/packaging/Test-AppInstaller.ps1` (created by `DSK-09-03`, board `REL-003`) | The eight-check validator R1 step 4 runs; a non-zero exit stops the release. |
| `eng/packaging/Publish-DesktopRelease.ps1` and `Test-FeedShare.ps1` (created by `DSK-09-10`, board `REL-008`) | The publish and verification scripts R1 steps 6 and 7 use; the second must be run **as an ordinary staff user from a non-publisher workstation** or its ACL check proves nothing. |
| group document `HZN-001` / `board-conventions.md` | The id rule this ticket must not get wrong: a bare `<PREFIX>-<nnn>` is a **fork board id**; an upstream id is always written `upstream <ID>`. `PLAT-039` in the body is an **upstream** id — there is no board `PLAT-039` (the board's `platform-operations` area runs `PLAT-001`…`PLAT-029`) — and board `PLAT-028`/`PLAT-029` map to upstream `PLAT-032`/`PLAT-038`. |
| `docs/desktop/08-testing/test-uat-stack.md` | The stack that must have rehearsed install → update → rollback for **this** package before the pilot (R1 precondition 5, and L-02's reason). |

## Ripple effects

- **`DSK-09-12` (board `REL-010`)** cannot run R3 until a pilot release exists; this run is
  its precondition and supplies the `<ver>` the minimum version is raised to.
- **`DSK-09-13` (board `REL-011`)** needs a published pilot release **and a previous package
  retained on the feed** before a rollback can be exercised — so R9's never-overwrite rule
  must hold through this run.
- **`DSK-09-18` (board `REL-016`)** creates the `### Desktop releases` table this run's row
  joins, and owns the `docs/capabilities.md` `OPS-10` edit that points at this approval.
  Sequence matters: it must not make that edit before this approval record exists.
- **`DSK-09-15` (board `REL-013`)** writes the operator one-pager against **this** published
  pilot release and is used by these pilot users.
- **`DSK-09-17` (board `REL-015`)** takes the operator-confirmed approval phrase from step 2
  and enforces it alongside a GitHub environment approval.
- **Upstream `PLAT-039`** gains its outstanding renewal evidence from step 11. A failure
  there is a **gateway** defect raised as a separate ticket, not a pilot-release defect.
- **`docs/current-architecture.md`** gains the desktop package and feed in its deployment
  boundary paragraph once this release ships — but only if the boundary actually changed.
- **No OpenAPI, generated-client or build ripple.** No endpoint, no contract, no package
  reference, no project file changes.

## Out of scope

Recorded so the reviewer sees these were decisions, not oversights. Each is a Guardrail in
the ticket body.

- **`docs/capabilities.md`.** The `OPS-10` row change is `DSK-09-18`'s (board `REL-016`).
  This ticket produces the approval record it will point at, and edits nothing in that file.
- **Raising the gateway minimum client version.** R3 owns it (`DSK-09-12`, board `REL-010`).
  R1 raises nothing, and the package layer it exercises **fails open**.
- **Any Azure write.** The desktop release path touches no Azure resource; the production
  gateway is only read from. If a gateway change turns out to be needed, that is the
  existing `pegasus-release` procedure and a separate approval — do not fold it into this
  ticket.
- **`.agents/skills/pegasus-release/SKILL.md` and any gateway release step.** Unchanged.
- **A second `OPS-10` acceptance.** D-004 is decided: one approver signs once for both.
  Seeking a separate sign-off, or treating upstream `TICK-001` as live work, re-opens a
  settled decision.
- **Fixing `docs/operations.md:295`.** The pre-existing gateway narrative drift is out of
  scope; `DSK-09-18` (board `REL-016`) raises it as a separate `fix` ticket in the
  `delivery-repository` area.
- **Production-ring publication.** This run publishes to `pilot/` only; `prod/` is R2.
