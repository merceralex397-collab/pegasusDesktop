# Research — FEAT-045: One provider error taxonomy for the gateway and the desktop

## Question

What provider-outcome vocabularies already exist in this repository, what does each already
express, and what exactly is left to add — so that the catalogue this ticket ships is a **projection
of what Core already decides** rather than a new vocabulary sitting beside four old ones?

## Current behaviour

The web application has no single provider error vocabulary, and it does not need one: a Razor page
renders a Core outcome directly into its own markup, so the outcome never has to survive a wire
format. The vocabulary problem is created by the gateway, not inherited from the web app.

What **does** exist today is a repository-wide **disposition rule** and four Core outcome enums that
each encode part of the answer.

- `docs/current-architecture.md:85-90` — an engineering invariant, not a suggestion: "External
  clients and catch paths distinguish `terminal`, `transient`, and `unknown`; terminal outcomes stop
  retries, **unknown outcomes remain unknown**, and metrics count successful effects rather than
  attempts." The same bullet list forbids horizontal `Common`/`Helpers`/`Utilities` packages and
  `V2`/`New`/`Manager` names — so the catalogue must be a named domain concept, not a utility bag.
- `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs` is the only place in the tree that currently
  translates Core refusals into a caller-safe vocabulary. Its summary (`:7-16`) states the rule the
  `/api/v1` mapping is ported from: domain exceptions "carry deliberately safe messages … and pass
  through; anything unexpected collapses to a generic failure so no infrastructure detail crosses
  the boundary", and the three edit-guard refusals "name which guard refused and the current case
  version … no token or other holder material crosses the boundary with them". It maps
  `StaffAuthorizationException`, `CaseEditLeaseExpiredException`, `CaseEditLeaseConflictException`
  and `CaseVersionConflictException` explicitly (`:30-53`), passes `ArgumentException` /
  `InvalidOperationException` / `InvalidDataException` messages through (`:54-60`), rethrows
  `OperationCanceledException`, and collapses everything else to "The automation action failed."
  (`:62-66`).

**Parity-matrix row.** None, and none should. The matrix's **46** rows
(`grep -c '^| PAR-' docs/desktop/01-inventory-and-parity/parity-matrix.md` → 46) are all keyed to
page models under `src/Pegasus.Web/Pages/**`, and this ticket ships a contracts catalogue plus a
problem-details mapping — there is no Razor page whose behaviour it carries. The closest existing
repository mechanism is the pair described above: the invariant at
`docs/current-architecture.md:85-90` and its one implementation at
`src/Pegasus.Web/Mcp/AutomationMcpErrors.cs`. `PAR-46`
(`docs/desktop/01-inventory-and-parity/parity-matrix.md:91`) records that MCP surface as "the
reference projection for `/api/v1` shapes (area 03)" and notes `/mcp` is unchanged — which is the
row that explains *why* `AutomationMcpErrors.cs` is the precedent, not a row this ticket advances.

## Findings

- **The existing problem-type catalogue already holds four of the five slugs this ticket needs.**
  `docs/desktop/03-gateway-api-and-data/README.md:167` specifies RFC 9457 via `AddProblemDetails`
  with stable `type` URIs `urn:pegasus:problem:<slug>` and **thirteen** slugs: `validation`,
  `not-authorized`, `version-conflict`, `lease-conflict`, `lease-expired`, `operation-conflict`,
  `client-unsupported`, `password-change-required`, `account-disabled`, `provider-unavailable`,
  `not-found`, `rate-limited`, `maintenance`. It also fixes two body rules — "Body never carries
  payload dumps; `correlationId` always present" — and names itself a "Port of
  `AutomationMcpErrors.cs`". **`not-found`, `not-authorized`, `rate-limited` and
  `provider-unavailable` are already there; only `invalid-request` is new.** The body's step 3 is
  right, and the ticket is smaller than its title suggests.
- **None of it exists in code yet.** `grep -rn "urn:pegasus:problem" src/ tests/` returns
  **nothing**. The list is specification, owned by [[GWY-001]] (plan handle `DSK-03-01`), and
  `src/Pegasus.Contracts` does not exist (`ls src/` → `Pegasus.Core`, `Pegasus.Infrastructure`,
  `Pegasus.Web`, `Pegasus.Worker`). This ticket **extends a catalogue that a sibling creates**; it
  does not bootstrap one.
- **The four Core enums, measured.** Every count below was read, not copied:
  - `src/Pegasus.Core/Vehicle/LookupContracts.cs:3-12` — `VehicleLookupOutcome`, **seven** values:
    `Current`, `Stale`, `Partial`, `NotFound`, `Throttled`, `Unavailable`, `Failed`. This is the
    richest of the four and the one that most needs faithful mapping.
  - same file `:53-56` — `VehicleLookupFailure(string Code, bool Retryable, TimeSpan? RetryAfter)`.
    **The three extension members this ticket standardises already exist here as Core fields**:
    `Code` → `providerCode`, `Retryable` → `retryable`, `RetryAfter` → `retryAfterSeconds`. The
    catalogue is a projection, exactly as the body says.
  - `src/Pegasus.Core/Operations/EmailOperations.cs:12-18` — `EmailOperationState`, **four** values:
    `Pending`, `Succeeded`, `Failed`, `Unknown`. **The ticket body's step 2 says "five values"; the
    measured count is four.** The line reference `:12-18` in the body is exactly right — only the
    count is off. [[FEAT-037]] (plan handle `DSK-07-11`)'s plan step 4 independently reached the
    same measurement, writing "Core has four states, not five". Write the measured count.
  - `src/Pegasus.Core/Intake/RetainedMail.cs:344-349` — `MailFreshnessState`, **three** values:
    `Current`, `Stale`, `Unavailable`. The record `MailPollHealth` at `:361-365` carries
    `LastFailureCode`, and the summary just above it (`:355-359`) draws the line this ticket must
    respect: "Raw facts only: turning them into a freshness state is **policy** and belongs to
    `GetRetainedMailFreshness`."
  - `src/Pegasus.Core/Custody/CustodyContracts.cs:297-305` — `RetryCaseCustodyOutcome`, **five**
    values: `Pending`, `Replay`, `Conflict`, `Refused`, `NotFound`.
- **Three of the four enums already carry an "uncertain" member, and they do not agree on its
  name.** `EmailOperationState.Unknown`, `VehicleLookupOutcome.Failed` (no further information) and
  `MailFreshnessState.Unavailable` each express a different flavour of "we do not know". This is
  precisely why the disposition triple has to be a **separate axis** from the slug rather than a
  fourth value inside it — a single flat enum would have to choose one of those three names and
  would lose the other two distinctions.
- **`Stale` and `Partial` have no slug and are not errors.** `VehicleLookupOutcome.Stale` and
  `Partial` are *successful* reads with caveats; mapping either to a problem type would turn a
  usable answer into a failure. The catalogue must cover only the failure subset, and the mapping
  table must say so explicitly rather than leaving a reader to infer it.
- **The accessibility rule this ticket must satisfy is already written and testable.**
  `docs/desktop/06-ui-design/keyboard-and-accessibility.md:82` — "No information by colour alone:
  every chip carries text and glyph; table…", and `:155` requires the reviewer to check "one
  vocabulary, one primary action, no colour-only state" in the ticket plan. The file is 155 lines.
- **A new `.md` under `src/` would fail CI — which constrains where the mapping table lives.**
  `scripts/Test-MarkdownPlacement.ps1:29-32` allows only
  `^((docs/(prd|frd|adr|design|desktop))|workspaces/document-extraction|\.agents/skills|\.design-sync|\.grok|\.stitch|design/planning-and-old-designs)/.+\.md$`,
  and it inspects added, copied and renamed files (`A`, `C`, `R`). The body's step 6 says to "record
  the mapping table in the contracts project's own documentation" — that must mean **XML doc
  comments on the catalogue type**, not a new markdown file beside it. (One `.md` does exist under
  `src/` — `src/Pegasus.Web/wwwroot/images/marks/README.md` — but it is pre-existing and therefore
  never inspected by the validator.)
- **`docs/frd/frd-13-desktop-operator-experience.md` does not exist.** `ls docs/frd/` shows only
  `frd-12-operator-experience.md` (131 lines) in that range. The body's Documentation changes offers
  frd-12 "or" frd-13, so the "or" resolves to **frd-12** unless a named sibling authors frd-13 first.
- **The consuming and rendering siblings are named and countable.** Consumers: [[FEAT-027]] (plan
  handle `DSK-07-01`), [[FEAT-028]] (`DSK-07-02`), [[FEAT-029]] (`DSK-07-03`), [[FEAT-031]]
  (`DSK-07-05`), [[FEAT-035]] (`DSK-07-09`), [[FEAT-037]] (`DSK-07-11`). Renderers: [[FEAT-030]]
  (`DSK-07-04`), [[FEAT-036]] (`DSK-07-10`). This ticket also **blocks** [[GWY-014]] (plan handle
  `DSK-03-14`), the vehicle-lookup and assessment endpoint group — which is the strongest argument
  for landing the catalogue before those endpoints rather than after.

### Facts

Verified at fork `main` on 2026-08-24, with the command that produced each.

| Fact | Source |
| --- | --- |
| The disposition rule is an engineering invariant: terminal stops retries, unknown remains unknown, metrics count effects not attempts | `docs/current-architecture.md:85-90` |
| Thirteen problem slugs specified; `correlationId` always present; no payload dumps; "Port of `AutomationMcpErrors.cs`" | `docs/desktop/03-gateway-api-and-data/README.md:167` |
| Four of the five needed slugs already exist; only `invalid-request` is new | same line |
| **No `urn:pegasus:problem` string exists in code** | `grep -rn "urn:pegasus:problem" src/ tests/` → no match |
| `src/Pegasus.Contracts` does not exist yet | `ls src/` → Core, Infrastructure, Web, Worker |
| `VehicleLookupOutcome` — **seven** values | `src/Pegasus.Core/Vehicle/LookupContracts.cs:3-12` |
| `VehicleLookupFailure(Code, Retryable, RetryAfter)` | same file `:53-56` |
| `EmailOperationState` — **four** values (`Pending`, `Succeeded`, `Failed`, `Unknown`) | `src/Pegasus.Core/Operations/EmailOperations.cs:12-18` |
| `MailFreshnessState` — **three** values | `src/Pegasus.Core/Intake/RetainedMail.cs:344-349` |
| `MailPollHealth.LastFailureCode`; "Raw facts only … policy … belongs to `GetRetainedMailFreshness`" | same file `:355-365` |
| `RetryCaseCustodyOutcome` — **five** values | `src/Pegasus.Core/Custody/CustodyContracts.cs:297-305` |
| `AutomationMcpErrors` maps four domain exceptions, passes three through, rethrows cancellation, collapses the rest | `src/Pegasus.Web/Mcp/AutomationMcpErrors.cs:7-16`, `:30-66` |
| "No information by colour alone: every chip carries text and glyph" | `docs/desktop/06-ui-design/keyboard-and-accessibility.md:82`; reviewer check at `:155`; file is 155 lines |
| Markdown placement allow-list regex; checks only `A`/`C`/`R` | `scripts/Test-MarkdownPlacement.ps1:29-32`, `:59-61` |
| `frd-13-desktop-operator-experience.md` does not exist; `frd-12-operator-experience.md` is 131 lines | `ls docs/frd/`; `wc -l` |
| Parity matrix holds 46 `PAR-` rows; `PAR-46` records `/mcp` as the reference projection | `grep -c '^| PAR-' …/parity-matrix.md` → 46; row at `:91` |
| This ticket blocks `GWY-014` | `get_item FEAT-045` → `"blocks": ["GWY-014"]` |

### Assumptions

- **`A-07-19-1` — every provider-touching failure in area 07 maps onto exactly one of the five
  slugs.** Twenty-one Core outcome values across four enums must land on five slugs plus a
  disposition, and the non-error values (`Current`, `Stale`, `Partial`, `Succeeded`, `Replay`) must
  land on none. *Confirmed by*: the mapping table built in plan step 2 and the per-endpoint
  application in step 6. *Breaks if wrong*: a sixth slug is needed, which is a change to
  [[GWY-001]]'s single list and must be coordinated, never added locally.
- **`A-07-19-2` — `retryAfterSeconds` is available whenever `rate-limited` is emitted.**
  `VehicleLookupFailure.RetryAfter` is `TimeSpan?` — nullable — so the provider may not supply one.
  *Confirmed by*: reading the DVLA/DVSA adapter's population of that field. *Breaks if wrong*: the
  member is optional in the contract and the `Retry-After` header is omitted rather than guessed;
  a fabricated retry window is worse than none.
- **`A-07-19-3` — an architecture test can detect a second provider-error enum outside
  `Pegasus.Contracts`.** `tests/Pegasus.ArchitectureTests` exists and [[FND-037]] (plan handle
  `DSK-02-12`) extends it for desktop boundaries. *Confirmed by*: writing the test and introducing a
  violation locally to watch it fail. *Breaks if wrong*: the one-list rule is enforced by review
  only, which is weaker and must be recorded as such.
- **`A-07-19-4` — the desktop slug-to-state mapping can live in `Pegasus.Desktop.Infrastructure`
  and be consumed by every screen.** *Confirmed by*: the view-model tests in plan step 11. *Breaks
  if wrong*: the mapping drifts per screen, which is the duplication the ticket exists to prevent,
  one layer further down.
- **`A-07-19-5` — the OpenAPI snapshot test fails on an unreviewed contract change.** [[GWY-004]]
  (plan handle `DSK-03-04`) owns that behaviour. *Confirmed by*: plan step 10's deliberate
  unreviewed change. *Breaks if wrong*: the snapshot is a build artefact rather than reviewed
  evidence, which the Guardrails explicitly reject.

## Execution placement

The six-question cloud-justification test from
`docs/desktop/00-governance-and-workflow/README.md` § 3 (`:166-178`), answered for the
responsibility this ticket places: **defining and enforcing the provider error vocabulary.**

| Question | Answer | Evidence |
| --- | --- | --- |
| Shared authority — must several users see and update the same state? | **No** | The catalogue is compile-time constants in a shared assembly, changed through pull request. No runtime state, shared or otherwise. |
| Unattended execution — must it run with every desktop closed? | **No** | It is a vocabulary, not a process. The Worker paths that *produce* these outcomes already run unattended and are untouched by this ticket — Guardrails: "Must not change Core enums or Core refusal semantics". |
| Protected credentials — long-lived secret that must not sit on workstations? | **No — and inverted into a prohibition** | No credential is involved, and ADR-0107 makes the absence enforceable: "a problem detail names a provider state, never a provider credential, a raw provider payload or a URL." Step 9's assertion that no body contains `x-api-key`, `Authorization`, `client_secret`, `box.com` or a bearer token is that rule made testable. |
| Public callback — must an external service call a stable public endpoint? | **No** | Nothing external reads these problem types. They are emitted to one authenticated desktop client. |
| Central enforcement — revocation, permissions, audit, invariant independent of the client? | **Yes** | Two invariants must hold regardless of the client: `unknown` may never be presented as success (`docs/current-architecture.md:85-90`), and the HTTP status paired with each slug must be the same at every endpoint. Both are enforced **at the gateway** — the existing `Pegasus.Web` Container App under L-01, no new deployment unit — and asserted in contract tests. A client-side mapping is a rendering convenience, never the enforcement. |
| Measured operational advantage — measured evidence central is materially better? | **No** | No measurement is claimed and none is needed; question 5 already places the enforcement. "It is already in Azure" is not an answer and is not being given. |

One "yes", and it places the enforcement with the **gateway that already exists** (L-01: the
catalogue ships in `Pegasus.Contracts` beside it, consumed by both hosts). **Nothing here requires
an Azure write** — the area plan's § 3 records that this whole area needs none, and the ticket's own
Guardrails say "Azure: no write."

## Implications

1. **The ticket is a projection exercise, not a design exercise.** Twenty-one Core outcome values
   across four enums, plus a disposition rule already written as an invariant, plus thirteen slugs
   already specified. Only `invalid-request` and the four extension member names are genuinely new.
   The plan's first real work is the inventory table, and the body is right to order it before any
   code.
2. **Disposition must be a second axis, not a fourth slug.** Three enums already carry three
   differently-named "uncertain" members; collapsing them into the slug loses distinctions Core
   deliberately makes.
3. **The non-error values are the trap.** `Current`, `Stale`, `Partial`, `Succeeded` and `Replay`
   must map to **no** slug. `Stale` in particular looks like a degraded state and is not one — it is
   a successful read with a caveat, and turning it into a problem would make a usable answer look
   like a failure.
4. **`Failed` must not become `not-found`.** The body's step 7 names this pair, and
   `VehicleLookupOutcome` carries both `NotFound` and `Failed` as distinct members — so the
   conflation would erase a distinction Core already draws. `Failed` with no further information is
   `provider-unavailable` with `disposition: unknown`.
5. **The extension member names already exist as Core field names.** `Code`, `Retryable` and
   `RetryAfter` on `VehicleLookupFailure` map one-for-one onto `providerCode`, `retryable` and
   `retryAfterSeconds`. Naming them anything else would create a second vocabulary at the very
   moment of consolidating the first.
6. **The mapping table cannot be a new `.md` under `src/`.** The placement validator's allow-list
   forbids it. XML doc comments on the catalogue type are the compliant home, and they have the
   advantage of travelling with the type into the generated OpenAPI descriptions.
7. **Landing before [[GWY-014]] matters.** This ticket blocks it, and retrofitting a vocabulary
   across endpoints that already shipped their own strings is the expensive version of this work.
8. **`MailPollHealth`'s summary draws the line to respect.** Raw facts belong to the read model;
   turning them into a state is policy owned by `GetRetainedMailFreshness`. The catalogue projects
   the policy's output, and must not start classifying raw failure codes itself.

## Open questions

None. The body instructs none, every value in the inventory is readable from the repository, and the
two judgement calls the ticket contains are settled by existing authorities rather than by anyone's
opinion: the slug set is fixed by
`docs/desktop/03-gateway-api-and-data/README.md:167` plus the one addition the body names, and the
disposition semantics are fixed by `docs/current-architecture.md:85-90`.

Two defaults were taken rather than asked, and both are recorded in the plan: the mapping table
lives in XML doc comments on the catalogue type (because the placement validator forbids a new `.md`
under `src/`), and the operator-experience documentation change goes to
`docs/frd/frd-12-operator-experience.md` (because `frd-13-desktop-operator-experience.md` does not
exist and the body offers the two as alternatives). No `open-questions` document is created.

**One body inaccuracy is recorded rather than propagated**: step 2 describes `EmailOperationState`
as having five values; it has **four** (`src/Pegasus.Core/Operations/EmailOperations.cs:12-18`).
The body's line reference is correct and its instruction — inventory every Core enum that expresses
a provider outcome — is followed exactly. Only the count is written as measured.
