# Files — FEAT-019: S19 Administration

Surveyed at `bbd1c549` (2026-08-24). Paths marked *(created by …)* do not exist yet — `ls src/`
returns only `Pegasus.Core`, `Pegasus.Infrastructure`, `Pegasus.Web`, `Pegasus.Worker`.

## Where the change lands

| Path | Why |
| --- | --- |
| `src/Pegasus.Contracts/` *(created by [[FND-029]] (plan handle `DSK-02-04`))* | One DTO group per screen, each carrying its resource's version so a stale write is a 409 rather than a silent overwrite. The channel-token rotate response is the exception: it carries the token exactly once and the DTO must not be persisted or logged by the client. |
| `src/Pegasus.Desktop/` *(created by [[FND-030]] (plan handle `DSK-02-05`))* | One view model per screen, on the data-table pattern from [[DUI-007]] (plan handle `DSK-06-07`) and the form pattern from [[DUI-008]] (plan handle `DSK-06-08`). Accounts, roles and access review consolidate into one **Administration › People** area (upstream PLAT-027, absorbed by this slice). |
| `src/Pegasus.Desktop/` — the Activity screen | Built from `Automation/Activity.cshtml.cs:23`'s read, but **not** from its view: `Automation/Activity.cshtml:67` renders a raw `AggregateId`, which the desktop resolves to the Case/PO reference or omits. |
| `src/Pegasus.Web/` — the `/api/v1` administration group | The routes from `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Administration and audit`, each with its own named route and its own `StaffAccessRight`. Behind `Features:DesktopGateway`. |
| `tests/Pegasus.Api.ContractTests/` *(created by [[TEST-001]] (plan handle `DSK-08-01`))* | The authorization matrix per **endpoint**: 200 with the correct right, 403 `not-authorized` with any other right **and for the Automation Actor**, 401 without a token, 409 on a stale version, `operationKey` replay, and an assertion that each sensitive mutation produced an audit record. |
| `tests/Pegasus.Desktop.ViewModelTests/` *(created by [[FND-038]] (plan handle `DSK-02-13`))* | Per-screen load, validation, reason-required commands, and the token non-retention fact. |
| `docs/desktop/01-inventory-and-parity/parity-matrix.md` | The administration rows for the ten in-scope page models. |
| `docs/frd/frd-13-desktop-operator-experience.md` *(created by area 00)* | The administration section, citing FRD-04. |
| `docs/capabilities.md` | `DSK` rows per administration capability. |

## Context files

| Path | What it tells the implementer |
| --- | --- |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs:7-20` | The twelve `StaffAccessRight` values — the complete vocabulary. Seven of them gate this slice; `ManageOrganizationsAndPrincipals` gates [[FEAT-007]]'s screens instead. |
| `src/Pegasus.Core/Identity/StaffAuthorization.cs:29-57` | The single fail-closed switch. The eight management rights resolve to `actor.Kind == ActorKind.Staff && actor.IsInRole(StaffRole.Administrator)` (`:52`); `PerformCasework` is the **only** right the Automation Actor holds (`:44-45`); unknown combinations return `false` (`:56`). The comment at `:38-42` states the ADR-0011 reasoning. Re-implementing any of this in the desktop creates a second policy owner and is a stop condition. |
| `src/Pegasus.Web/Pages/Administration/AdministrationPageModel.cs` | 7 lines. It is a marker base, not the authorization implementation — do not look for the matrix here. |
| `src/Pegasus.Web/Pages/Administration/Automation/Index.cshtml.cs:168-206` | The channel-token rotate and clear commands: what the rotate returns, and whether the value is retrievable afterwards. Read before implementing the one-time reveal. |
| `src/Pegasus.Web/Pages/Administration/Automation/Activity.cshtml:64-69` | What the current Activity table renders: `OperatorLabels.Humanise(record.EventKind)` (`:64`), `Model.SubjectLabel(record.SubjectId)` (`:65`), `OperatorLabels.Humanise(record.Outcome)` (`:66`) — and then a **raw `AggregateId`** in the Target column at `:67`. Upstream PLAT-015 names that column; do not reproduce it. |
| `src/Pegasus.Web/Pages/Administration/Mailboxes.cshtml.cs:167` | `OnPostResolveFoldersAsync` — the Graph read that must stay server-side. `reuse-map.md`'s `Email/` row records that Graph credentials never reach the desktop (ADR-0106); the desktop calls the gateway endpoint. |
| `docs/frd/frd-04-parties-accounts-and-access.md:13-26` | The staff role access matrix: `Engineer` and `User` "must not access" accounts, roles, access review, principals, workflow configuration, the mailbox allowlist or authentication-client administration. It also records that no person or email address is hard-coded into authorization. |
| `docs/frd/frd-04-parties-accounts-and-access.md:27-35` | § `Permanent action history` — "A history write is part of the mutable business transaction; a failed write cannot leave an unrecorded successful mutation. History is append-only." This is why the audit assertion belongs **inside** the contract test. Sign-ins and authentication failures go to the security log, not the action history. |
| `docs/desktop/03-gateway-api-and-data/endpoint-map.md` § `Administration and audit` | The authoritative routes and rights, and two details easy to miss: `POST /admin/accounts/{id}/disable` **requires a `reason`** and results in "disabled → tokens revoked"; the mailboxes row notes the resolver is a Web-only Graph read. |
| `docs/desktop/05-implementation-and-migration/vertical-slices.md` § Common to every slice | "The desktop hides or disables commands for usability only" — the reason [[FND-046]]'s role-aware shell is a convenience and the gateway 403 is the control. |
| `docs/design/README.md:398-409` | The closed list of approved necessary copy. A destructive action's consequence sentence must come from this list or from an individually approved addition — it is not written freehand. |
| `docs/design/README.md:412-445` | Banned operator words and the four hard rules. Relevant here because administration screens are the most tempting place to explain a setting; "no how-it-works copy" applies. |
| `docs/capabilities.md:269` | The AI-09 row: implemented behind `Features:SendToAi`, DevelopmentOffline evidence runs only, "production activation needs a separate non-preview transport decision". The Send-to-AI *toggle* is carried across as a setting; the capability is not reopened. |
| `docs/desktop/01-inventory-and-parity/upstream-kanmer-carryover.md` | Check before duplicating upstream PLAT-025, PLAT-026, PLAT-027, AUTO-006, AUTO-007 or PR-026 — all absorbed here. Note the namespace trap: the board's own `PLAT-025`, `PLAT-026` and `PLAT-027` are `DSK-11-07`, `DSK-11-08` and `DSK-11-09`, entirely different tickets; the `HZN-001` group document `board-conventions.md` § `Upstream ids versus board ids` holds the join table. |

## Ripple effects

- **`openapi/pegasus-v1.json` and the generated client** — roughly twenty new routes across the
  administration group; regenerated in this change.
- **`tests/Pegasus.IntegrationTests`** — the existing administration web tests must stay green; the
  Razor pages are untouched and remain deployable until cutover.
- **[[FND-046]] (plan handle `DSK-04-10`)'s role-aware shell** gains an Administration entry per
  right; a right added or renamed here changes that navigation.
- **[[GWY-022]] (plan handle `DSK-04-05`)** owns refresh-token revocation, which
  `POST /admin/accounts/{id}/disable` triggers; the disable command's user-visible consequence
  depends on that behaviour being in place.
- **[[FEAT-021]] (plan handle `DSK-05-21`)** is the operator-side counterpart of account
  disablement — the exact disabled-account message the desktop shows. The two must agree.
- **[[FEAT-022]] (plan handle `DSK-05-22`)** sweeps these screens for the PLAT-015 identifier rules;
  building the Activity Target column correctly here avoids a finding there.
- **`docs/capabilities.md`, `frd-13`, the parity matrix** — updated in the same slice
  (`docs/engineering.md` § One Core owner).

## Out of scope

- `src/Pegasus.Web/Pages/Administration/Organizations/*` and `Principals/*` — five page models
  (126 + 146 + 31 + 137 + 199 lines) owned by [[FEAT-007]] (plan handle `DSK-05-07`), gated by
  `ManageOrganizationsAndPrincipals`.
- The Razor administration pages themselves — not modified; the web keeps them until cutover, and
  their removal is [[FEAT-026]]'s (plan handle `DSK-05-26`).
- `src/Pegasus.Infrastructure/Email/` — mailbox folder resolution stays a gateway-side Graph read.
- Any Send-to-AI capability work beyond carrying across the existing toggle — a recorded exclusion
  (`docs/capabilities.md:269`, `reuse-map.md:38`).
- Re-implementing the rights matrix anywhere in `src/Pegasus.Desktop*` — a stop condition.
- Refresh-token revocation mechanics — [[GWY-022]].
- Azure: no write.
