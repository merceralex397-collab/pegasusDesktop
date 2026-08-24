# Files — FEAT-009

Surveyed 2026-08-24 against fork `main` `191ddf33`. Paths marked *(created by …)*
do not exist in the working tree today — `ls src` returns only `Pegasus.Core`,
`Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`, and `ls tests` only
`Pegasus.ArchitectureTests`, `Pegasus.Core.Tests`, `Pegasus.IntegrationTests`.

## Where the change lands

| Path | Why |
|---|---|
| `src/Pegasus.Contracts/` *(created by [[FND-029]], plan handle `DSK-02-04`)* | New received-item DTOs: receipt detail, classification evidence, field suggestions with provenance, extracted-text availability, the read-only typed draft, and one request record per command. Breaks the generated client and every contract test if a member is renamed after [[GWY-010]] has shipped against it. |
| `src/Pegasus.Desktop/` *(created by [[FND-030]], plan handle `DSK-02-05`)* | `ReceivedItemViewModel` with one command object per action, and the screen XAML (tabs Evidence / Draft / Decision / Case / History per `docs/desktop/06-ui-design/screen-specs.md:271-285`). Every control carries an `AutomationId` or the [[DUI-015]] coverage audit fails. |
| `src/Pegasus.Desktop.Infrastructure/` *(created by [[FND-031]], plan handle `DSK-02-06`)* | The **streaming** download service for source, asset and image bytes — progress, cancel, per-user temporary path with restrictive ACLs and bounded retention. [[FEAT-011]] step 9 and [[FEAT-012]] step 8 both reuse this exact service; a screen-private helper here breaks both. |
| `src/Pegasus.Web/` — the `/api/v1` received group only | Only where [[GWY-010]] (plan handle `DSK-03-10`) left a gap this slice must close to consume its own contract. The group lives behind `Features:DesktopGateway` ([[GWY-002]], plan handle `DSK-03-02`). |
| `src/Pegasus.Core/Intake/` | **Only** for a rule moved in from the page model with a characterization test written first: the link and reverse-link integrity checks and the re-evaluation preconditions. A duplicate business implementation is a stop condition (`docs/engineering.md` § One Core owner). |
| `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs` | Re-pointed at the moved Core rule and nothing else. The page stays live until cutover (`docs/desktop/05-implementation-and-migration/README.md:150-152`). |
| `tests/Pegasus.Core.Tests/Intake/` | Characterization facts for the link, reverse-link and re-evaluation rules — written **before** the rule moves. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]], plan handle `DSK-08-01`)* | Nine command matrices and three byte-endpoint facts, using the seven-case template from [[TEST-002]] (plan handle `DSK-08-02`). |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[TEST-004]], plan handle `DSK-08-04`)* | `CanExecute` gating per command, reason-required commands, streaming progress and cancellation, read-only draft rendering. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | Rows `PAR-19` (the nine handlers and the detail read) and `PAR-20` (the three byte pages) advance from `inventoried`. |
| `docs/desktop/05-implementation-and-migration/vertical-slices.md` § `S9` | Correct the "Absorbs upstream" line at `:369-373` — see Ripple effects. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(created by [[DUI-013]], plan handle `DSK-06-13`)* | Received-items section. The file does not exist today (`ls docs/frd` shows `frd-01`…`frd-12` only). |
| `docs/capabilities.md` | `DSK` rows for received-item review and actions. |

## Context files

| Path | What it tells the implementer |
|---|---|
| `src/Pegasus.Web/Pages/Intake/Details.cshtml.cs:95-560` | The nine POST handlers and their exact parameter sets. `:350-361` holds a `DecisionLabel` switch that is a **second copy** of `src/Pegasus.Web/Pages/Mail/Message.cshtml.cs:1014-1023` — do not add a third; [[FEAT-023]] (plan handle `DSK-05-23`) folds both into one list. |
| `src/Pegasus.Core/Intake/DurableIntake.cs:1106-1121` | `LinkIntake`'s `: ILinkIntake` line is `:1109`, and its first act is `IntakeCommandValidation.RequireStaffMutation(receiptId, expectedIntakeVersion, actor, operationKey, …)`. Receipt version, actor and operation key are Core preconditions — the desktop must send all three or Core throws, not the endpoint. |
| `src/Pegasus.Core/Intake/DownloadIntakeSource.cs:40-43` | The source read recomputes SHA-256 and compares it in fixed time. A byte endpoint that streams around this use case silently drops the integrity check — which is why the gateway keeps calling it and the desktop never reads storage directly. |
| `src/Pegasus.Core/Intake/IntakeQueryUseCases.cs:5,16-32,43,53` | `ListIntake` and `GetIntake`, both gated on `StaffAccessRight.PerformCasework`. `ListIntake` bounds page to `1…10_000` and page size to `1…100` **in Core** — the desktop list must not request outside those bounds or it gets `ArgumentOutOfRangeException`, not an empty page. |
| `src/Pegasus.Core/Intake/IntakeAllocation.cs:199-208` | `AllocateIntake`'s own summary says it is "the one Core owner for initial allocation, durable failure and reasoned staff retry. Completed-work replay never calls this use case." The retry-allocation command must therefore not be reused as a replay path. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs:10` | `StaffAccessRight.PerformCasework` — the right every command and byte read on this screen checks. An actor without it must see the section absent, not an error. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Intake (received items), uploads, image intake` | The nine commands are split across two rows precisely because three of them (`case-lease/claim`, `link-case`, `reverse-case-link`) additionally carry the case `expectedVersion` **and** the `editLeaseToken`. Sending only the receipt version on those three is the mistake the split exists to prevent. |
| `docs/desktop/06-ui-design/screen-specs.md:271-285` | The tab set, the read-only-draft rule ("editable only on Case create", i.e. [[FEAT-004]], plan handle `DSK-05-04`), the "sections only when populated" rule, and the three AutomationId families. |
| `docs/design/README.md:396-421` | The approved necessary copy this screen may use verbatim (`:402`, `:404`) and the banned-word list (`:412-421`), which includes `intake`, `artifact`, `durable` and `bytes`. The document says in its own words that **nothing in CI enforces this** — it is a merge rule the reviewer applies. |
| `docs/design/README.md:535-546` | The binding `IntakeDecision` → operator-label table. It disagrees with both page-model copies on `OcrRequired` and `TechnicalFailure`. Reconciling that is [[FEAT-023]]'s single stated exception — this slice changes no label text. |
| `docs/desktop/05-implementation-and-migration/README.md:158-170` | The characterization-gap list, which names "intake draft correction and link/unlink integrity checks (S9, S10)" as gaps to close **before** the slice that moves them. It also lists what is deliberately *not* preserved: TempData, PRG, antiforgery, the `IAsyncPageFilter` rail-count injection. |
| `tests/Pegasus.IntegrationTests/MultiFormatIntakeWebTests.cs` (1,429 lines) | The reviewed cohort and the fixture shapes the tier-8 comparison runs against. Read it before inventing a corpus harness — there is one. |
| `tests/Pegasus.IntegrationTests/LocalIntakeAccessTests.cs` (184 lines) | Route-denial facts for the byte pages. The `/api/v1` byte endpoints must refuse the same actors, or the desktop widens access the web never granted. |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | The upstream register. Read with the join table in group document `HZN-001` / `board-conventions.md`: the board's `INTK-001`…`INTK-007` are upstream INTK-002, INTK-003, INTK-026, INTK-027, INTK-031, INTK-032 and INTK-033. Never a bare intake id. |

## Ripple effects

- **OpenAPI and the generated client.** Adding received-item DTOs to
  `src/Pegasus.Contracts` changes `openapi/pegasus-v1.json` and the generated
  client that [[GWY-010]] and the contract tests bind to. A DTO renamed after
  [[GWY-010]] merges breaks its tests, not only this slice's.
- **`tests/Pegasus.IntegrationTests`** — `QdosIntakeWebTests.cs`,
  `IntakeStablePersistenceTests.cs`, `MultiFormatIntakeWebTests.cs` and
  `LocalIntakeAccessTests.cs` all exercise the Razor path. Moving a rule into
  `src/Pegasus.Core/Intake/` and re-pointing `Details.cshtml.cs` must leave every
  one of them green; that is the ticket's fourth verification command.
- **`tests/Pegasus.ArchitectureTests`** — [[FND-037]] (plan handle `DSK-02-12`)
  extends `DependencyDirectionTests` for the desktop boundaries. A desktop
  reference to `src/Pegasus.Infrastructure` or an Azure SDK fails there.
- **Sibling slices that consume this one.** The streaming service is reused by
  [[FEAT-011]] step 9 and [[FEAT-012]] step 8; the received-item surface is the
  screen a completed upload receipt opens into for [[FEAT-013]] (plan handle
  `DSK-05-13`); the gallery adopters step of [[FEAT-016]] (plan handle
  `DSK-05-16`) replaces whatever image rendering this screen ships with.
- **`vertical-slices.md` § S9's "Absorbs upstream" line (`:369-373`)** claims all
  four upstream intake ids. Three of the four are wrong: upstream INTK-004 is
  [[FEAT-023]]'s and [[FEAT-020]] (plan handle `DSK-05-20`)'s; upstream INTK-027
  is board [[INTK-004]]; upstream INTK-033 is board [[INTK-007]]. The correction
  is coordinated with [[FND-022]] (plan handle `DSK-01-09`), which holds the
  carry-over join table, so the line changes once.
- **`docs/frd/frd-13-desktop-operator-experience.md` does not exist yet** — it is
  authored by [[DUI-013]]. If it has not landed, this slice contributes its
  section content to that ticket rather than creating a rival file.

## Out of scope

Recorded so the reviewer sees each was a decision, not an oversight.

- **`src/Pegasus.Infrastructure`** — readers stay central. The desktop never
  parses a source document.
- **`src/Pegasus.Worker`** — the queued-intake path (`DurableIntake.cs:418`,
  `:893`) is untouched.
- **The Razor intake pages beyond re-pointing a moved rule.** They stay
  deployable until their parity rows reach `UAT passed`.
- **upstream INTK-027 (board [[INTK-004]])** — the transient-staging
  re-evaluation defect. Characterized as it behaves today and recorded as a known
  defect owned there; not fixed and not worked around.
- **upstream INTK-033 (board [[INTK-007]])** — the stranded triage-request
  e-mail. Its own ticket; it brings `ITriageQueries.GetByOriginReceiptAsync`,
  which does not exist in the fork today (`src/Pegasus.Core/Triage/TriageContracts.cs:288-294`
  carries only `ListAsync` and `GetAsync`).
- **upstream INTK-001** — the honest queued upload status. Absorbed with **no
  fork ticket**, owned jointly by [[GWY-011]] (plan handle `DSK-03-11`) for the
  payload and [[FEAT-013]] for the operator surface.
- **upstream INTK-004** — the decision-label and Operations-claim
  reconciliation. Absorbed with **no fork ticket**; its label half is
  [[FEAT-023]]'s and its Operations half is [[FEAT-020]]'s.
- **Any Azure write.** The artifact store is reached only through the gateway;
  no desktop code references an Azure SDK.
- **A second image renderer or thumbnail cache.** [[FEAT-016]] owns the one
  gallery and viewer control.
