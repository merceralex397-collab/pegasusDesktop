# Plan — REL-010: DSK-09-12 · Mandatory-update runbook R3 and its enforcement test

**Diff estimate: ~3 files, ~110 lines.** `runbooks.md` § R3 gains ~35 lines (the named admin
control, the `CheckUpdateAvailabilityAsync` semantics, the CSP/Group Policy caveat, the
proven marker); `eng/packaging/Test-Package.ps1` gains one scenario of ~65 lines (set the
minimum, launch the old client, assert the screen by `AutomationId`, assert the API
refusal, restore the minimum); `docs/operations.md` gains ~3 lines inside the existing
desktop release row. `docs/engineering.md:201-207` § Plan sizing requires the estimate
first.

## Approach

**Test both network conditions, and assert the refusal at both levels.** The two enforcement
layers fail in opposite directions by design — the package check fails open when the feed is
unreachable, the gateway gate fails closed — so a single test with a reachable feed proves
only that App Installer updates, which is the layer that cannot be relied on. The rehearsal
therefore runs twice: feed reachable (App Installer should update) and feed unreachable
(the gateway's update-required screen must appear and no work must be possible), asserting
in the second case that the **gateway** is still reachable so an unreachable-feed test is
never mistaken for an unreachable-gateway test.

The rejected alternative was **proving it once on the pilot ring only**. L-02 forbids an
Azure test environment, so the pilot is the only real-Azure surface — but it is also
production, with real users, and a first attempt there cannot be repeated or made to fail
safely. The Test/UAT stack already carries the scenario (`test-uat-stack.md:131`, scenario
12: stack with local feed, **then** pilot with real feed), and this plan follows that
order.

Every assertion names a literal already fixed elsewhere: the problem type
`urn:pegasus:problem:client-unsupported`, the header `X-Pegasus-Client-Version`, the
endpoint `GET /api/v1/client-compatibility`, and the AutomationId `Update.Required.Now`.
Nothing here invents a contract.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-010`). No existing PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by `DSK-09-01` (board `REL-001`). This ticket proves its
> Decision clause (b): the package check **fails open** when the feed is unreachable and the
> gateway gate **fails closed** after a short cached window, and both layers are required.
> This plan is written to that decision as recorded in
> `docs/desktop/09-release-update-and-distribution/README.md` § 3 "Two-layer enforcement";
> if ADR-0105 lands differently, this plan is revised before implementation.

Existing documents this plan **meets**:

- **`docs/desktop/04-auth-session-update-and-startup/README.md:220`** — area 04's exit-gate
  row "Obsolete package is blocked and updates". **Meets**: step 5's scripted scenario is
  the gateway half ("old `X-Pegasus-Client-Version` → `client-unsupported`") and step 4's
  reachable-feed rehearsal is the packaging half.
- **`docs/desktop/06-ui-design/screen-specs.md:99-107`** — the Update required / Blocked
  screen. **Meets, by asserting against it rather than changing it**: the scripted scenario
  locates `Update.Required.Now` and the manual confirmation reads the current and minimum
  versions as values.
- **`AGENTS.md` § Safety rails** — refresh current-state documents in the same task.
  **Meets**: step 9 records the minimum-version change in `docs/operations.md` in this task.

Binding operator decisions:

- **L-02** — the rehearsal happens on the local Test/UAT stack and the confirmation on the
  pilot ring, because there is no Azure test environment.
- **D-003** — when the feed is unreachable the package check is skipped and the app
  launches; the gateway gate is the **only** fail-closed layer. The feed is LAN/VPN-only by
  design, so off-network users are a live operational consideration, not a hypothetical.

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in
the plan document.

- **Subagents**: `pegasus-release-packager`
  (`.codex/agents/pegasus-release-packager.toml`, verified present);
  `pegasus-test-engineer` (`.codex/agents/pegasus-test-engineer.toml`, verified present)
  for the scripted enforcement test.
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) →
  `winui-ui-testing` (`.codex/skills/winui-ui-testing/SKILL.md`,
  `microsoft/win-dev-skills` v0.5.0 `f1028dd5`, verified present) → `winui-packaging`
  (`.codex/skills/winui-packaging/SKILL.md`, verified present) for the package-side
  behaviour.
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`) for
  `Package.CheckUpdateAvailabilityAsync` `Required` versus `Available` semantics.
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates REL-010` before every move; a move crosses at most one gated boundary.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's twelve implementation steps in the same order, with the same
ownership. Steps marked **Operator step** are performed by the operator on a pilot
workstation or against the production gateway.

1. **Orient and take.** Read `runbooks.md` § R3 and § R6 in full, the area plan § 5 row
   `DSK-09-12`, and area plan § 3 "Two-layer enforcement". `get_doc_gates REL-010`, then
   `take_ticket REL-010`.
2. **Record R3's preconditions**: R2 done — or, for the pilot-only rehearsal, R1 done
   (`DSK-09-11`, board `REL-009`); all pilot users observed on `<ver>` or the grace period
   elapsed; the gateway supports `<ver>`. Record each with its evidence, not as assumed.
3. **Name the real admin control in R3 step 1.** Read what `DSK-04-06` (board `GWY-023`)
   shipped — a **DB-backed Administrator setting with an audit entry**, changed with a
   reason, plus a config bootstrap fallback
   (`docs/desktop/04-auth-session-update-and-startup/README.md:237`) — and write the actual
   screen path or endpoint into R3 step 1, replacing "the gateway administration surface".
   A runbook that says "the admin surface" is a runbook someone will guess at during an
   incident.
4. **Rehearse on the Test/UAT stack first (L-02), in both network conditions.** Install
   version N-1 from the stack's local feed; raise the minimum client version to `<ver>` in
   the stack gateway; relaunch, and record which layer fires:
   - **feed reachable** → App Installer should prompt and update (the package layer);
   - **feed unreachable** → the launch check is skipped and the gateway's update-required
     screen must appear, with **no work possible**.
   Make the feed unreachable by a controlled manoeuvre — the stack's own failure-injection
   precedent is at `test-uat-stack.md` § Lifecycle — and **assert in the same run that
   `GET /api/v1/client-compatibility` still answers**, so the two failure modes are not
   conflated. Record the exact manoeuvre used.
5. **Script the enforcement check as a scenario in `eng/packaging/Test-Package.ps1`**
   (`DSK-08-10`, board `TEST-010`) — extending that script, not creating a second one. The
   scenario: set the minimum version; launch the old client; assert the update-required
   screen by `AutomationId` `Update.Required.Now` using `winui-ui-testing`; assert that
   **every** `/api/v1` call from that client returns
   `urn:pegasus:problem:client-unsupported` with its `minimumVersion` field
   (`docs/desktop/04-auth-session-update-and-startup/README.md:210`); then restore the
   minimum so the scenario is repeatable. Assert both the screen **and** the API refusal — a
   screen alone could be cosmetic over a working API.
6. **Record the `CheckUpdateAvailabilityAsync` semantics in the runbook.**
   `microsoft_docs_search` for `Package CheckUpdateAvailabilityAsync Required Available` and
   write into R3 what `Required` means — that the `.appinstaller` policy blocks activation —
   alongside the note that the call must be made on the package from
   `PackageManager.FindPackageForUser`, never `Package.Current`, which fails with access
   denied. `DSK-04-09` (board `FND-045`) owns that code; R3 records the semantics so the two
   layers are not confused in the write-up.
7. **Operator step — R3 step 2 on a pilot machine still on the previous version.** Launch.
   Expect App Installer to update if the feed is reachable; if the feed check is bypassed or
   unreachable, expect the gateway's update-required screen **with the correlation id**, and
   confirm no work is possible. Hand back a screenshot and the correlation id.
8. **Operator step — R3 step 3, the positive case.** Confirm a current client logs in
   normally **and completes a named routine workflow** — agree the workflow before the run
   (sign in, open a case, save an edit) so the evidence is comparable between runs rather
   than "it looked fine". Hand back the transcript.
9. **R3 step 4 — record the minimum-version change** (who, when, reason) in the desktop
   release row in `docs/operations.md`, in the same task per `AGENTS.md` § Safety rails. The
   table is `DSK-09-18`'s (board `REL-016`) and the first row is `DSK-09-11`'s (board
   `REL-009`); add to that row rather than creating a table.
10. **Write and rehearse the rollback path.** Lowering the minimum version to its previous
    value through the same admin setting restores the old clients. Prove it on the Test/UAT
    stack and **record the observed recovery time** — a measured number, not a target. This
    is the path `DSK-09-13` (board `REL-011`) R4 step 2 depends on.
11. **State R3's limits explicitly in its "does not prove" section.** It does not prove App
    Installer behaviour on a machine whose policy overrides update settings: CSP (Intune)
    settings override PowerShell and `.appinstaller` settings, which override an embedded
    `.appinstaller` (`appinstaller-template.md` § Known behaviours). R7 records the expected
    policy state and `Get-AppxPackageAutoUpdateSettings` is the check. An override found is
    **recorded**, not worked around.
12. **Mark R3 proven** in `runbooks.md` with its date, and record the dated
    `## Simplification pass` in this document.

## Verification

Evidence tier from the body: **Tier 7** — the workstation/UI evidence tier the plan row
assigns. Proof is observed application behaviour on a real machine (screens, AutomationIds,
refusal responses), captured on the stack **and repeated on the pilot ring**. Automated
assertions do not replace the manual pilot confirmation. `proof` combines `test-output`,
`visual` and `command-log`.

| Command / observation | Expected evidence |
| --- | --- |
| Test/UAT stack: set minimum to `<ver>`, launch client N-1 with the **feed unreachable** | update-required screen shown; correlation id visible; every `/api/v1` call returns `urn:pegasus:problem:client-unsupported`; `GET /api/v1/client-compatibility` still answers, proving the gateway was reachable |
| Test/UAT stack: same, with the **feed reachable** | App Installer prompts and updates; `Get-AppxPackage CollisionEngineers.Pegasus` moves to `<ver>` |
| Pilot ring: the unreachable-feed scenario on a real workstation | same outcome; screenshot captured; correlation id recorded |
| Current client on `<ver>` | login succeeds and the named routine workflow completes |
| `Get-AppxPackageAutoUpdateSettings` on the test machine | on-launch checks enabled and not overridden by policy; **any override is recorded rather than worked around** |
| Lowering the minimum to its previous value | old clients accepted again; the observed recovery time is recorded |

Behaviours to observe rather than infer: which of the two layers actually fired in each run,
and that "no work is possible" means an actual `/api/v1` refusal, not merely a screen.

## Risks / open questions

- **Risk — testing with a reachable feed proves the wrong layer.** The package check fails
  open, so with the feed up the gateway gate never fires. Mitigation: step 4 runs both
  conditions and asserts gateway reachability in the unreachable-feed case.
- **Risk — asserting the screen without the API refusal.** A cosmetic screen over a working
  API would pass. Mitigation: step 5 asserts both.
- **Risk — `Package.Current.CheckUpdateAvailabilityAsync` access denied.** The client must
  call it on the package from `PackageManager.FindPackageForUser`. `DSK-04-09` (board
  `FND-045`) owns that code; this ticket records the semantics rather than fixing them.
- **Risk — Group Policy or CSP overrides App Installer settings on managed devices.** R3
  cannot prove that case. Mitigation: step 11 names it in "does not prove" and points at R7
  and `Get-AppxPackageAutoUpdateSettings`.
- **Risk — a pilot user is off-network when the minimum is raised.** They are locked out of
  work until they return, because D-003's feed is LAN/VPN-only. Mitigation: R9 step 7's rule
  is restated in R3's preconditions; confirm no pilot user is away before raising.
- **Risk — the positive case degrades to "it looked fine".** Mitigation: step 8 names the
  routine workflow before the run.
- **Risk — telemetry is trusted as evidence.** App Insights' 0.1 GB/day cap (PLAT-034) can
  hide blocked-client counts for most of the day. Mitigation: the evidence is screens and
  API responses.
- **Open questions**: none. Every literal this ticket asserts against
  (`urn:pegasus:problem:client-unsupported`, `X-Pegasus-Client-Version`,
  `GET /api/v1/client-compatibility`, `Update.Required.Now`) is already specified in areas 04
  and 06 and is read at implementation time. If a defect is found in the endpoint or
  middleware, raise a `fix` ticket against `DSK-04-06` (board `GWY-023`) rather than patching
  from here. **No `open-questions` document is created.**

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch extends a
test script as well as documentation, so `n/a — docs-only` does not apply._
