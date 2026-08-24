# Files — FEAT-030

Surveyed 2026-08-24 against fork `main` `191ddf33`. Paths marked *(created by …)*
do not exist today: `ls src` returns only `Pegasus.Core`, `Pegasus.Infrastructure`,
`Pegasus.Web`, `Pegasus.Worker`; `ls tests` only `Pegasus.ArchitectureTests`,
`Pegasus.Core.Tests`, `Pegasus.IntegrationTests`. There is no
`src/Pegasus.Desktop`, no `src/Pegasus.Desktop.Infrastructure`, no
`tests/Pegasus.Desktop.ViewModelTests`, no `tests/Pegasus.Desktop.UITests`, no
`src/Pegasus.Contracts`, no `openapi/`, no `eng/` and no `BuildAndRun.ps1`.
`ls docs/frd` returns FRD-01…FRD-12 only.

## Where the change lands

| Path | Why |
|---|---|
| `src/Pegasus.Desktop/` — `OperationsViewModel` *(project created by [[FND-030]], plan handle `DSK-02-05`)* | The screen's one view model: `ObservableObject` + `[RelayCommand]`, an explicit five-value load state, `ObtainedAtUtc` set **only** on success, and per-row retry/revoke enablement bound to the gateway's `canRetry` / `canRevoke`. No `SolidColorBrush`, `Visibility` or other UI type — `winui-code-review`'s MVVM checklist and the architecture test from [[FND-037]] (plan handle `DSK-02-12`) both fail on one. **This ticket owns the type**; if [[FEAT-020]] (plan handle `DSK-05-20`) landed first it created it under exactly these members — extend it in place. A second view model for this screen is a stop condition (the ticket's Guardrails). |
| `src/Pegasus.Desktop/` — `OperationsPage.xaml` and its code-behind | The external-work table, the upload-links table and the health rows, built from the [[DUI-007]] (plan handle `DSK-06-07`) data-table pattern rather than a bespoke grid. Four AutomationIds fixed by the spec, not chosen here. |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]], plan handle `DSK-02-06`)* | The typed calls into the Kiota client from [[GWY-005]] (plan handle `DSK-03-05`): intake status, external work, `GET /api/v1/operations`, and the four retry commands. The view model never touches `HttpClient` directly. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(scaffolded by [[TEST-004]] (plan handle `DSK-08-04`) per this ticket's body; [[FEAT-001]]'s files document names [[FND-038]] (plan handle `DSK-02-13`) — see the plan's Governing docs for the reconciliation)* | Freshness, enablement, refusal, disconnected and cancellation facts against a fake API client. This ticket writes tests into the project; it does not create it. |
| `tests/Pegasus.Desktop.UITests/` *(created by [[TEST-006]], plan handle `DSK-08-06`)* | A new `-Script operations` batch for `ui-tests.ps1`: table renders, retry disabled/enabled, reason dialog, keyboard-only traversal of both tables, and a screenshot of the disconnected state. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(skeleton created by [[FND-008]], plan handle `DSK-00-08`; sections adopted by [[DUI-013]], plan handle `DSK-06-13`)* | The Operations screen section. [[FEAT-020]] adds the retry and revoke command behaviour as a sub-heading **inside** it, per this ticket's Documentation changes. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md:72` | Row `PAR-27` moves `not inventoried` → `implemented`. Note it starts at **`not inventoried`** with an empty `Verification` cell, so that cell is filled here for the first time. [[FEAT-025]] (plan handle `DSK-05-25`) owns the maintenance pattern. |

## Context files

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Web/Pages/Operations/Index.cshtml.cs:41-45`, `:46`, `:67` | **The rule the whole screen turns on**, in the code's own words: "When this list was last read. Set only after the query returns, so a failed load never claims to be fresh (FRD-12)" — with the assignment at `:67`, after the await at `:64`. Reproduce the placement, not just the field. |
| `…/Operations/Index.cshtml.cs:47-49` | The pre-load value is an **empty** `RequestOperationsProjection` with `LimitReached: false`, not null. The desktop's initial state is "not started", never a rendered empty table that implies zero failures. |
| `…/Operations/Index.cshtml.cs:71-110` | The retry handler: `expectedAttemptCount` is an **`int`**, one `operationKey`, no reason. Its four approved sentences live at `:92-94` (replay vs first effect — "External work was already scheduled for retry." / "External work was scheduled for retry."), `:100-102` and `:104-106`. Reuse them; a native screen that writes new ones forks the operator vocabulary. |
| `…/Operations/Index.cshtml.cs:112-173` | The revoke handler, and the trap: it acquires a case edit lease with its **own** operation key (`:132-138`), calls revoke with a **second** key plus `expectedVersion`, `expectedCaseVersion`, `reason` and `lease.Token` (`:153`), then releases the lease in `ReleaseQuietlyAsync`. Two keys per revoke. Its four sentences are at `:128`, `:147` ("This link's case is open for editing by someone else. Try again in a few minutes."), `:157` and `:165`. |
| `…/Operations/Index.cshtml.cs:176-187` | `StateLabel` over the eight `RequestOperationState` members. `UnknownExternal` renders as "Unknown external" and the default arm **throws** (`:185`). A desktop `switch` with a `_ => "Unknown"` arm would silently absorb a new Core state — the refusal is the intended behaviour. |
| `…/Operations/Index.cshtml.cs:188-206` | `ReleaseQuietlyAsync` swallows `ArgumentException`, `InvalidOperationException` and `DbUpdateConcurrencyException`. A failed lease release is deliberately not surfaced; the desktop must not turn it into an operator-visible error. |
| `…/Operations/Index.cshtml.cs:218-235` | `PreserveReason` — on any revoke failure the typed reason (≤ 500 chars) is stashed and re-rendered so the operator does not retype it. The desktop's `ReasonDialog` must do the same on refusal. |
| `src/Pegasus.Core/Operations/RequestOperations.cs:32-58` | `RequestOperationProjection`'s twenty-four members plus `ActiveEditLease`. **`CanRetry` (`:51`) and `CanRevoke` (`:52`) are fields**, and `CaseEditLeaseState` / `CaseEditLeaseExpiresAtUtc` (`:54-55`) let the screen show a row whose case someone else already holds. Bind these; never infer eligibility from `AttemptCount`. |
| `src/Pegasus.Core/Operations/RequestOperations.cs:72-153` | `GetRequestOperations`: `PerformCasework` at `:87`, `MaximumItems = 100` at `:76`, and the two invariants that make the flags trustworthy — "Only versioned file requests may expose revocation" (`:142`) and "Only a versioned durable external-work failure may expose retry" (`:149`). |
| `src/Pegasus.Web/Presentation/OperatorLabels.cs` (685) | The one code→operator-vocabulary map. `"queue_poisoned"` → "Processing was attempted repeatedly without completing" (`:340`), and `OfficeTime` (`:412`) / `OfficeDate` (`:426`) resolving `Europe/London` (`:446`). Freshness times render through this. [[GWY-016]] (plan handle `DSK-03-16`) and [[FEAT-023]] (plan handle `DSK-05-23`) move it to `Pegasus.Contracts` — coordinate rather than writing a second office-time helper. |
| `docs/desktop/06-ui-design/screen-specs.md:390-398` | The screen's own spec: the external-work columns (kind, case, last failure, attempts, next action), "Retry (reasoned)", upload links with Revoke, "integration health rows … values and last-good times, **never secrets; absent when not composed**", and the four AutomationIds. Note the spec's "reasoned" retry against a Core command that takes no reason — see the plan's Approach. |
| `docs/desktop/06-ui-design/screen-specs.md:31-39` | The AutomationId convention `<Screen>.<Region>.<Element>[.<Key>]`. Coverage must be 100% and the ids are not this ticket's to choose. |
| `docs/desktop/06-ui-design/screen-specs.md:417-427` | The cross-cutting state contract with its **Desktop-specific** row — "disconnected (saves disabled, content visible)" is a named contract state, not an invention — and the empty-state rule: an absent section, a legitimate `0`, and "'No results' text appears only for a search the operator ran". A failed load is none of those three. |
| `docs/design/README.md:764-772` | The authority the spec restates. The query states this screen owes: loading, empty, current success, stale-with-last-good-time, partial, unavailable, failed/retry, unauthenticated, disabled, stale-role, denied. |
| `docs/design/README.md:170` | **The sharpest constraint on this particular screen**, because its subject matter is queue mechanics: "Do not expose Azure, OCR, AI, queue mechanics, extraction engines, deployment, adapter, lease/version, projection, ingress, or artifact terminology in operator copy. The word 'intake' never appears in operator-facing text." |
| `docs/design/README.md:412-420` | The banned-word list and the sentence that matters most: CI does **not** enforce it — the reviewer is the only gate. |
| `docs/frd/frd-12-operator-experience.md:95-99`, `:101-103` | The FRD this ticket's `refs` names: "`0`, loading, current, stale-with-last-good-time, partial, unavailable, and failed are distinct outcomes. A refresh never replaces a last-good value with a false zero…", and "Manual refresh reruns the same exact filtered query; it does not change policy or create a business transition." |
| `docs/desktop/06-ui-design/keyboard-and-accessibility.md:82`, `:88`, `:96` | "No information by colour alone: every chip carries text and glyph"; forced-colours brush mapping; "Permanent consequences visible without hover or colour". |
| `docs/desktop/06-ui-design/keyboard-and-accessibility.md:56-98`, `:99-114`, `:115-147` | The per-screen accessibility checklist enforced in review, the automated checks, and the ten recorded reviews per shipped screen. Step 12's scan is one of them, not all of them. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:37` | `GET /admin/health` — the source of the Box, DVLA/DVSA, feed and minimum-version health rows — is **`ManageWorkflowConfiguration`, phase 8**, while this screen is `PerformCasework`, phase 5. That mismatch is why those rows are absent here rather than blank. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md:115-116` | The two Operations rows the desktop calls: `GET /operations` and the retry/revoke commands, with their concurrency tokens. |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 "Contracts" | "desktop never hand-writes DTOs; Core records are **not** exposed directly". If a field the screen needs is missing, the fix is on the endpoint ticket, not a hand-written type here. |
| `docs/desktop/03-gateway-api-and-data/README.md` § 3 "Retry" | "Desktop retries only idempotent `GET`s (bounded, jittered); commands are never retried automatically." The refresh may retry; a retry or revoke command may not. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md:72` | `PAR-27`, status `not inventoried`, `Verification` column `to locate`. The row this ticket advances, and the empty cell it fills. |
| `docs/desktop/07-integrations/README.md` § 7 | Two trap rows that bind here: "Poison-queue visibility lost behind a friendly status" and "App Insights blind hours hide provider errors in production (PLAT-034)" — the second is the reason this screen exists at all. |

## Ripple effects

- **[[FEAT-020]] (plan handle `DSK-05-20`) extends this exact view model and
  page.** Whichever lands first creates them under the members named here; the
  other extends in place. A second view model for this screen is a stop
  condition.
- **[[FEAT-027]] (plan handle `DSK-07-01`) and [[FEAT-028]] (plan handle
  `DSK-07-02`) are this screen's contracts** — [[FEAT-028]] declares
  `blocks: [FEAT-030]` on the board. A field rename there is a breaking change
  to this screen; freeze the names when their contract tests go green.
- **[[GWY-013]] (plan handle `DSK-03-13`) publishes `GET /api/v1/operations`,**
  which supplies the upload-links half. A missing `canRevoke` or link field is
  raised there.
- **[[GWY-008]] (plan handle `DSK-03-08`) publishes the lease endpoints** revoke
  needs; [[PLAT-015]] (plan handle `DSK-10-15`) publishes the health endpoint
  the non-mailbox rows need.
- **Regenerating the Kiota client** (`eng/api/Generate-ApiClient.ps1`,
  [[GWY-005]], plan handle `DSK-03-05`) changes committed generated code; CI
  fails if a second regeneration is not a no-op, so run it twice and commit.
- **The architecture test from [[FND-037]] (plan handle `DSK-02-12`)** fails on a
  `src/Pegasus.Infrastructure` reference from the desktop and on any Pegasus UI
  hosted in a WebView. Both are named in this ticket's Guardrails.
- **`docs/frd/frd-13-desktop-operator-experience.md` gains a section** that
  [[FEAT-020]] then extends, and [[DUI-013]] (plan handle `DSK-06-13`) governs
  the section shape.
- **The parity matrix row and the accessibility report** are proof artefacts
  other tickets read: [[FEAT-025]] (plan handle `DSK-05-25`) for the matrix,
  [[DUI-015]] (plan handle `DSK-06-15`) for the scan.

## Out of scope

- **Adding or changing any `/api/v1` endpoint.** That is [[FEAT-027]] and
  [[FEAT-028]] (the ticket's Guardrails). If the screen needs a field the
  contract lacks, stop and raise it there.
- **`src/Pegasus.Infrastructure`** — the desktop must not reference it; the
  architecture test from [[FND-037]] enforces it.
- **Any WebView hosting Pegasus UI.** Forbidden by the same test and by
  proposal § 23.2; the one permitted isolated WebView2 is the report renderer
  ([[FEAT-040]], plan handle `DSK-07-14`).
- **`src/Pegasus.Web`, `src/Pegasus.Core`, `src/Pegasus.Infrastructure` and
  `src/Pegasus.Worker`** — every file. The Razor Operations page stays
  deployable until `PAR-27` reaches `UAT passed`.
- **Creating `tests/Pegasus.Desktop.ViewModelTests` or
  `tests/Pegasus.Desktop.UITests`.** Both are scaffolded by other tickets; this
  one writes into them.
- **A reason field on Retry.** `RetryExternalWorkCommand` has no `Reason`
  member (`RequestOperations.cs:157-161`); collecting one the gateway would
  discard is worse than not asking. Recorded as a decision in the plan.
- **The Box, DVLA/DVSA, update-feed and minimum-client-version health rows** at
  phase 5 — absent until [[PLAT-015]]'s endpoint exists, under the screen
  spec's own "absent when not composed" rule.
- **Any secret, token or raw provider payload on screen.** A health row shows a
  state and a last-good time, nothing more (ADR-0107).
- **Any Azure write.**
