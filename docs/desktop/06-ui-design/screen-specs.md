# Screen specifications — by capability group

One block per screen, grouped by the proposal's capability groups
(§13.1–13.10). Each block names the page models it replaces (from the
inventory in [01 · parity matrix](../01-inventory-and-parity/parity-matrix.md)),
the gateway endpoints it consumes (named in
[03 · endpoint map](../03-gateway-api-and-data/endpoint-map.md)), its states
from the authority's complete UI state contract
(`docs/design/README.md:764-772`), its commands, keyboard and AutomationIds.
Slice tickets in [05 · vertical slices](../05-implementation-and-migration/vertical-slices.md)
point here; FRD-13 adopts these blocks as its sections.

Rules that bind every block (authority `:160-180`, `:396-445`):

- One title per screen, no lede or subtitle; guidance only as an approved
  consequence sentence beside the control it concerns.
- A field is a label and a control. No hint text, no "Required."/"Optional.",
  no format guidance.
- No how-it-works copy, no worked examples, no introductory sentences.
- Only populated, relevant sections render; read-only view has no empty-state
  panels; edit-only sections render only in edit context.
- Filters are dropdowns (`ComboBox`), tables sort newest first, headers are
  sort controls.
- Every state value and date passes through the shared operator-label map
  (Europe/London); banned words never reach the operator.
- State is never colour alone; one primary action per view region.
- Deferred capabilities are absent, not disabled; an action the record will
  offer once a condition is met stays visible and disabled with the condition
  named on the control ("Available in Review").

## AutomationId convention

`<Screen>.<Region>.<Element>[.<Key>]`, PascalCase segments, stable across
releases, unique per window. Examples: `Shell.Rail.Cases`, `Shell.Status.Connection`,
`Cases.List.Table`, `Cases.List.Filter.Stage`, `Case.Header.Status`,
`Case.Actions.EnterEdit`, `Case.Tabs.Documents`, `Dialog.Reason.Text`,
`Dialog.Reason.Confirm`, `Dialog.Reason.Cancel`. Row-level elements append
the record key: `Cases.List.Row.576059`. Every interactive control has one;
`pegasus-ui-verifier`'s coverage audit must report 100%.

## Shell (§14.2) — replaces `Pages/Shared/_Layout.cshtml`, `_LayoutAuth.cshtml`

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ [logo] Pegasus   [ENV badge]                 Connection ● Sync ● v1.2.3  [User ▾] │  title bar (drag region)
├────────────┬────────────────────────────────────────────────────────────────┤
│ Dashboard  │  Screen title                         [Primary action]          │
│ Inbox  (n) │  ───────────────────────────────────────────────────────────   │
│ Upload     │  content region (max 1280, centred, 24px gutters)              │
│ Queues (n) │                                                                │
│ Cases      │                                                                │
│ Operations │                                                                │
│ Administr. │  (admin only — absent otherwise)                               │
├────────────┴────────────────────────────────────────────────────────────────┤
│ Status: Connected · Last synced 14:02 · Pilot channel · Update available    │
└─────────────────────────────────────────────────────────────────────────────┘
```

- `NavigationView` left, `PaneDisplayMode=Left`, `OpenPaneLength=236`,
  `IsPaneToggleButtonVisible=False` (the authority's rail never hides), items
  in the approved order; `Administration` is present only for the
  Administrator role (derived from the role matrix and server authorisation);
  `Inbox` is present only when the capability is composed.
- Current item: weight change plus the 2px Collision-red left marker (the
  NavigationView selection indicator restyled), never colour alone.
- Rail counts come from the dashboard rail-counts query; absent when the query
  has not returned; never a shell-level `0`.
- Title bar: logo (checksummed asset), environment badge (non-production
  only: "Pilot", "Test/UAT", "Development"), connection glyph + word, version
  and channel, user menu (Change password, Sign out, Diagnostics).
- Status bar: connection state, last sync time (Europe/London), background
  transfer summary (opens the transfer pane), update availability.
- Connectivity state: "Disconnected — reconnecting" in the status bar; saves
  disabled; existing content visible (proposal §11.3).
- States: authenticated; unauthenticated (login screen replaces the shell);
  update-required and blocked (full-window screens, no rail); disabled
  account; stale role (re-login prompt).
- Keyboard: rail access keys (Alt+D, Alt+I, Alt+U, Alt+Q, Alt+C, Alt+O,
  Alt+A), `Ctrl+K` → Cases search, F5 refresh current screen.
- AutomationIds: `Shell.Rail.<Route>`, `Shell.Title.Environment`,
  `Shell.Title.User`, `Shell.Status.Connection`, `Shell.Status.Update`.

## §13.1 Access and session

### Sign in — replaces `Pages/Account/SignIn.cshtml.cs`

- Navless frame with the logo and the company name (not the product; `_LayoutAuth`
  convention). Fields: User name, Password (label + control only), Sign in
  (primary). No "forgot password" (not a current capability).
- States: idle; signing in (button disabled, thin progress); invalid
  credentials (generic failure sentence, field focus returns to User name);
  rate limited (problem `sign_in_rate_limited` → "Try again in a minute");
  account disabled; password change required → Change password screen;
  server unreachable → connectivity sentence, not an invalid-credentials
  message; client unsupported → Update required screen.
- AutomationIds: `SignIn.UserName`, `SignIn.Password`, `SignIn.Submit`,
  `SignIn.Problem`.

### Update required / Blocked — new (proposal §9)

- Full-window, no rail. Title "Update required"; the current and minimum
  versions as values; one primary action "Update now" (opens the App
  Installer update); secondary "Sign out". Blocked (account disabled or
  compatibility fail-closed) shows the operator sentence and "Sign out" only.
- AutomationIds: `Update.Required.Now`, `Update.Required.SignOut`,
  `Blocked.Reason`.

### Change password — replaces `Pages/Account/PasswordChange.cshtml.cs`

- Fields: Current password, New password, Confirm new password; Save
  (primary). Validation messages attach to the field; minimum length is shown
  only as a validation outcome, never as hint text.
- AutomationIds: `Password.Current`, `Password.New`, `Password.Confirm`,
  `Password.Save`.

### Diagnostics and settings — new (proposal §11.1, §18.1)

- Route: user menu → Diagnostics. Sections render only when populated: About
  (version, channel, package identity, Windows version, gateway address);
  Preferences (theme follows system / light / dark, grid column layouts per
  table, window position restore — local only); Diagnostics (Export
  diagnostics bundle — primary; Open logs folder); Developer (gallery page;
  non-production only).
- AutomationIds: `Settings.Theme`, `Settings.ExportDiagnostics`,
  `Settings.OpenLogs`, `Settings.Gallery`.

## §13.2 Dashboard and work queues

### Dashboard — replaces `Pages/Index.cshtml.cs`, `Presentation/RailCountsPageFilter.cs`

- Landing route. Metric tiles, each an exact link to its filtered queue:
  active cases Not ready | Review | Held; e-mail activity Received today |
  Unidentified | Blocked; New cases today; Sent to Engineer today/week;
  Reports sent today/week (authority `:463-471`). Each tile shows its value
  or its unavailable state, its last-good time and current refresh state
  (loading, current, stale, partial, unavailable, failed); `0` is a current
  result only.
- Recent cases (local convenience list, not search authority) renders only
  when there are entries.
- Integration failures needing attention: a row per failed external work
  item linking to Operations; absent when none.
- Commands: Refresh (reruns the same queries, start/completion feedback,
  keeps last-good data). No charts.
- Keyboard: tiles are buttons in tab order; Enter opens the queue.
- AutomationIds: `Dashboard.Tile.<Metric>`, `Dashboard.Refresh`,
  `Dashboard.Recent.Row.<Ref>`.

### Queues (pre-engineer work) — replaces `Pages/Triage/Index`, `Pages/Unidentified/Index`, `Pages/ImageIntake/Index`, and the case-stage queues

- One screen with a stage selector (ComboBox: Not ready, Review, Held,
  Triage, Unidentified, Vehicle images) and a data table per selection;
  filters as dropdowns (principal, age, assigned); newest first; columns per
  selection (reference/plate, principal, received/instruction date, state
  chip, due by/overdue chip, assigned, latest activity summary).
- Double-click/Enter opens the record (case workspace, triage detail,
  unidentified detail, image-intake detail).
- States per query contract; selection remembered locally.
- AutomationIds: `Queues.Stage`, `Queues.Table`, `Queues.Filter.<Name>`,
  `Queues.Row.<Key>`.

## §13.3 Case lifecycle

### Cases list and search — replaces `Pages/Cases/Index.cshtml.cs`, `Pages/Search/Index.cshtml.cs` (UI-07)

- Search box (exact Case/PO, registration, principal text) with `Ctrl+K`
  focus; filter dropdowns (stage, type Inspection/Audit/Inspection + Audit,
  principal, engineer, date range via two date pickers); table newest first:
  Case/PO (reference plate), Registration (VRM plate), Principal, Type, Stage
  chip, Due by/overdue chip, Updated. Column chooser persisted locally.
  Paging: server-paged with accessible current-page context and
  keyboard-operable next/previous.
- Commands: New case (primary, `Ctrl+N`), Refresh. Multi-select only if a
  bulk action is approved (none in scope).
- Keyboard: arrows move rows, Enter opens, `Ctrl+F`/`Ctrl+K` focus search.
- AutomationIds: `Cases.Search`, `Cases.List.Table`, `Cases.List.Filter.<Name>`,
  `Cases.List.Row.<Ref>`, `Cases.New`.

### Case workspace — replaces `Pages/Cases/Details.cshtml.cs` and its handler families (`Workflow`, `Tasks`, `Closure`, `Custody`, `Vehicle`, `Assessment/Index`, `Documents/*`, `Eva/Download`), `CaseMutationPageModel.cs`

```text
┌ Case/PO 576059 │ EJ17 NBZ │ Principal │ Inspection │ Stage: Review │ Due by 12/09/2026 │ [lock] Editing: you (renew 14:20) ┐
│ [Enter edit mode] [Hold] [Assign engineer] [Record finding ▾] [More ▾]                                     │
├ Overview │ Vehicle │ Assessment │ Documents │ Communications │ Tasks │ Reports │ History ──────────────────────────┤
│ tab content: only populated sections; edit-only sections in edit mode only                                 │
└────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

- One container: identity header (read-only Case/PO, principal,
  registration, type and secondary Audit identity, workflow state chip, Due
  by/overdue, EVA proxy limitation), action bar (one primary = the next
  permitted action; others default), tabs. Identity, state, actions and main
  content reachable without scrolling at 1280×800.
- Lease/conflict: `Enter edit mode` acquires the lease; header shows holder
  and expiry; renew and `Leave editing`; lease loss or stale version disables
  every mutation, preserves proposed values in memory for comparison and never
  overwrites the newer record; reload/compare/reacquire are the only recovery
  actions; no forced takeover (authority `:622`, `:719-723`).
- Tabs (each renders only populated sections): Overview (parties, key dates,
  inspection address with provider-determined mode, next action, due/chaser
  panel); Vehicle (registration, DVLA/DVSA/MOT observations with
  source/version/age chips, suggestion-first VRM, lookup request); Assessment
  (separate Roadworthiness and Assessment findings, damage, estimate import,
  specification acceptance, reconciliation — the largest sub-slice, see 05
  S17); Documents (Box folder list, transfer queue pane, upload drop target,
  download/export, lock state, version conflict rows); Communications (linked
  e-mails, sent evidence, draft/queued/sent/failed distinction); Tasks (tasks,
  reminders, manual chase, report-evidence links); Reports (generated
  versions, preview, finalise/send, custody and sent evidence separate);
  History (read-only permanent action history: actor, time, outcome; no
  bodies, no telemetry noise).
- Lifecycle actions use only the named Core actions (Hold, Release hold,
  Return to review, Assign engineer, Start work, Record engineer finding,
  Create linked replacement, Record report approval, Close with named
  outcome, Reopen with reason, Archive); never a generic Close; every
  reasoned action through the `ReasonDialog` contract; `Created in error`
  shows both references and no reopen control.
- Dirty state: header shows "Unsaved changes" as a chip; `Ctrl+S` saves;
  navigation away warns via `ReasonDialog`-shaped confirmation ("Discard
  changes" verb / Cancel).
- Validation: field-level immediately; server validation problems attach to
  the section they concern (InfoBar in the tab) with the Reference row.
- States: loading (section-level thin progress), denied, not found (styled
  not-found screen), lease held by another (read-only + holder), stale
  version, dependency unavailable (Box/DVLA), idempotent replay result.
- Keyboard: `Ctrl+1..8` tabs, `Ctrl+S` save, `Esc` closes flyouts/panes,
  `Ctrl+W` closes the case view back to the list.
- AutomationIds: `Case.Header.<Field>`, `Case.Actions.<Action>`,
  `Case.Tabs.<Tab>`, `Case.<Tab>.<Section>.<Element>`, `Case.Lease.Enter`,
  `Case.Lease.Renew`, `Case.Lease.Leave`.
- Upstream carry-over absorbed: CASE-012, UICASE-001, CASE-020, CASE-021,
  CASE-022 (make the public upload link findable — Documents tab command).

### Case create — replaces `Pages/Cases/Create.cshtml.cs`

- Reached from Cases (`Ctrl+N`) or from a received item/triage record with
  the typed draft values pre-filled as candidates with provenance glyphs;
  a keyed value becomes a staff-sourced candidate (current behaviour). Fields
  grouped in sections: Principal and instruction, Vehicle, Inspection address
  (provider-determined mode autofills `Image Based Assessment` or requires a
  physical location), Dates. Create (primary) and Cancel.
- Refusal (no case/reference created) shows the approved sentence "No case or
  reference was created; review the missing or conflicting evidence." and
  keeps proposed values in memory.
- AutomationIds: `CaseCreate.<Section>.<Field>`, `CaseCreate.Submit`.

## §13.4 Intake

### Inbox — replaces `Pages/Mail/Index.cshtml.cs` (list, preview) and `Pages/Mail/Message.cshtml.cs` (detail)

- Mail list: mailbox and folder scope as dropdowns, optional search, table
  newest first (received, from, subject, attachments n, classification chip,
  filed-to case). Preview on selection (body text only, inert; no remote
  content). Manual refresh with freshness.
- Message page (worked example: the approved `/Inbox/{id}` mockup): one
  container with head band (subject wraps, never truncates), state accent,
  four tabs Message · Attachments n · Thread · Case; Message tab is a split
  with the letter in the main column and the Decision card in the right
  column (Classification, Destination, Filed to, Folder, Decided + *Correct
  classification*, *Move to folder*; rows/actions only when populated and
  available; no Open case button — Filed to is the link); Case tab: search,
  candidates, confirm target; unlink shows the approved consequence
  "Unlinking this email cancels case <reference>." in its dialog.
- Deleted-items search (capped 100 newest) as a scope option.
- AutomationIds: `Inbox.Scope.Mailbox`, `Inbox.Scope.Folder`, `Inbox.Table`,
  `Inbox.Row.<Id>`, `Message.Tabs.<Tab>`, `Message.Decision.Correct`,
  `Message.Decision.Move`, `Message.Case.Search`, `Message.Case.Link`,
  `Message.Case.Unlink`.
- Upstream carry-over absorbed: AUTO-003 (expose completed workspace actions
  to Automation — gateway side), MAIL-008 label maps.

### Received item (intake receipt detail) — replaces `Pages/Intake/Details.cshtml.cs`, `Asset`, `Image`, `Source`

- Identity head (receipt reference, source, received time, state chip);
  tabs Evidence (page-labelled extracted text, images, attachments as
  separate occurrences with download), Draft (typed instruction values with
  provenance — read-only here; editable only on Case create), Decision
  (classification, allocation outcome, failure details), Case (link/unlink
  with lease), History. Actions as named commands: Retry allocation, Block
  (reason), Re-evaluate, Correct draft, Register vehicle images, Dismiss
  suggestion. Operator vocabulary: the word "intake" never appears
  (Received item, E-mail activity, Blocked).
- AutomationIds: `Received.Header.<Field>`, `Received.Tabs.<Tab>`,
  `Received.Actions.<Action>`.
- Upstream carry-over covered: INTK-001, INTK-027 (fork implementation [[INTK-004]]),
  INTK-033 (gateway side),
  INTK-019 (engineer selection replaces "Assign to me" in Triage).

### Triage detail — replaces `Pages/Triage/Details.cshtml.cs` (13 actions)

- One container; identity (registration, source, assigned engineer, state
  chip); sections only when populated (evidence, reply evidence, findings with
  superseded versions, responses, linked case); thirteen named actions as
  explicit commands in the action bar/overflow (await information, record
  finding, supersede finding, link/unlink response, complete, cancel, reopen,
  link/unlink case, assign engineer…), each through `ReasonDialog` where a
  reason is required; never a generic Close.
- AutomationIds: `Triage.Header.<Field>`, `Triage.Actions.<Action>`.

### Unidentified and Vehicle images — replaces `Pages/Unidentified/*`, `Pages/ImageIntake/*`

- Unidentified detail: `U<n>` reference, canonical reason, open/resolved
  history, group members with source download; Resolve (reasoned).
- Vehicle images detail: Image reference plate, VRM suggestions
  (source-image/confirmed/no-result distinction), preserved group evidence,
  merge history, registration-matched eligible cases while unassociated,
  Close (reasoned).
- AutomationIds: `Unidentified.Resolve`, `VehicleImages.Suggestions`,
  `VehicleImages.Close`.

### Upload — replaces `Pages/Upload.cshtml.cs`, `UploadStatus`, `UploadGroupStatus`

- Drop target plus file picker (one file ≤ 10 MiB, allowed extensions from
  the current limits; limits surface only as a validation outcome), Submit
  (primary); status view: Received, Processing, Complete, Failed with
  polling every two seconds and manual refresh; completion links to the case
  or retained receipt; group registration/attach for vehicle-image groups.
- AutomationIds: `Upload.Drop`, `Upload.Pick`, `Upload.Submit`,
  `Upload.Status.State`, `Upload.Group.Register`.

## §13.5 Vehicle and inspection information — Case workspace › Vehicle tab

- Registration (VRM plate), make/model/colour/year from lookup with
  source/version/age chips; MOT/mileage observations classified
  supplied/external/estimated; suggestion rows with Accept (staff
  confirmation never overwritten by refresh); Request lookup command with
  provider state distinct from "not found" (unknown, stale, partial,
  unavailable, failed); inspection address with provider-determined mode and
  reasoned per-case override; engineer allocation shown from EVA proxy with
  its limitation; Generate EVA handoff (once-per-case proxy) and Download.
- AutomationIds: `Case.Vehicle.Lookup`, `Case.Vehicle.Suggestion.Accept.<Key>`,
  `Case.Vehicle.Address.Mode`, `Case.Vehicle.Eva.Generate`.

## §13.6 Parties and reference data — Administration › Organizations, Principals

- Organizations list/edit (name, addresses, contacts) and Principals
  create/replace (inspection mode setting; replacement keeps history; a
  replaced principal is read-only with its successor link). Administration
  entry is a set of cards, one accessible control per card.
- AutomationIds: `Admin.Organizations.Table`, `Admin.Organizations.Create`,
  `Admin.Principals.Create`, `Admin.Principals.Replace`.
- Upstream carry-over absorbed: PLAT-028 (redesign with provider API controls
  — the provider API itself stays out of scope).

## §13.7 Documents and evidence — Case workspace › Documents tab

- Native folder/file list for the case's Box folder (name, type, size MB one
  decimal, source, uploader, time, lock/version state); drag-and-drop upload
  and file picker into a transfer queue pane (progress, cancel, retry, failed
  rows kept); preview pane for supported images/PDF (image decode to display
  size; PDF via the isolated report/preview path — never a WebView hosting
  app UI); Open externally (explicit command); Download, Export (archive);
  Remove (logical, reasoned); Confirm third-party vehicle evidence; Create /
  Revoke public upload link (findable — CASE-022); clear distinction between
  the local working copy and the canonical Box copy; no hidden overwrite;
  conflicting versions shown as rows with the newer one named.
- Evidence gallery (instruction photographs) reads document records with
  paging and download (DOCS-011/012, CASE-011 gallery viewer reused across
  image-bearing screens).
- AutomationIds: `Case.Documents.Table`, `Case.Documents.Upload`,
  `Case.Documents.Queue`, `Case.Documents.Preview`, `Case.Documents.OpenExternally`,
  `Case.Documents.UploadLink.Create`.

## §13.8 Communications — Case workspace › Communications tab and Inbox

- Linked source e-mails and attachments; outbound actions only where the
  current capability exists (send report/fee note through the gateway with
  idempotency); explicit draft / queued / sent / failed chips; exact Outlook
  Sent evidence with separate discovery, link and sent times; correlation to
  the case and actor.
- AutomationIds: `Case.Communications.Table`, `Case.Communications.Send`.

## §13.9 Assessment, valuation and reporting — Case workspace › Assessment and Reports tabs

- Assessment: damage entry grid, estimate import (file → parsed lines with
  original vs assessed comparison), specification acceptance (reasoned),
  reconciliation; professional findings Roadworthiness and Assessment as
  separate controls with accepted/superseded versions; values stored
  unconfirmed until engineer confirmation (automation parity).
- Reports: Generate report draft (local WebView2 render, L-03; progress in
  status bar; cancel), Preview (PDF viewer in-app; the preview surface is a
  document viewer, not Pegasus UI in a WebView), Finalise/Send (reasoned;
  idempotent), list of issued versions with custody and sent evidence shown
  separately; each version binds only to its exact approved artifact identity
  and hash, while a correction leaves predecessor Sent evidence visible. An
  explicit reasoned unlink/relink retains the evidence and ordered association
  history; legacy rows without exact version proof render as `Unresolved`.
  Regeneration rules are surfaced as enabled/disabled named conditions.
- AutomationIds: `Case.Assessment.Damage.Grid`, `Case.Assessment.Import`,
  `Case.Assessment.AcceptSpecification`, `Case.Reports.Generate`,
  `Case.Reports.Preview`, `Case.Reports.Send`.

## §13.10 Administration and operations

### Operations — replaces `Pages/Operations/Index.cshtml.cs`

- Retryable external work (table: kind, case, last failure, attempts, next
  action) with Retry (reasoned), active public upload links with Revoke;
  integration health rows (Graph last successful cycle per mailbox, Box,
  DVLA/DVSA, update feed, minimum client version) — values and last-good
  times, never secrets; absent when not composed.
- AutomationIds: `Operations.External.Table`, `Operations.External.Retry`,
  `Operations.Links.Revoke`, `Operations.Health.<Dependency>`.
- Upstream carry-over absorbed: PLAT-023.

### Administration — replaces `Pages/Administration/**` (16 page models)

- Entry cards (Accounts, Roles, Access review, Organizations, Principals,
  Configuration, Mailboxes, Mail categories, Automation). Each screen is a
  table plus a create/edit form following the field pattern; disable/enable
  and role assignment through `ReasonDialog` where the current behaviour
  requires a reason; access review as a table with review action; automation
  (enable, connector redirect URIs, channel token rotate/clear, activity
  log); mailboxes (update, resolve folders); workflow configuration; mail
  categories.
- AutomationIds: `Admin.<Area>.Table`, `Admin.<Area>.Create`, `Admin.<Area>.Edit`,
  `Admin.<Area>.Action.<Name>`.
- Upstream carry-over absorbed: PLAT-025, PLAT-026, PLAT-027, AUTO-006,
  AUTO-007, PLAT-029 (information architecture — resolved by the rail order
  and this grouping).

## Cross-cutting state contract (authority `:764-772`)

| Scope | States every screen must render distinctly |
| --- | --- |
| Queries | loading; empty (`0` or absent section); current; stale with last-good time; partial; unavailable; failed/retry; unauthenticated; disabled; stale role; denied |
| Mutations | validation; confirmation; success; denied; stale version; lease lost; dependency unavailable; idempotent/replayed result; conflict and recovery |
| Desktop-specific | disconnected (saves disabled, content visible); update required; client unsupported; compatibility cached/expired; transfer queued/running/failed/cancelled; draft recovered after abnormal exit |

Empty-state rule: a read-only section with nothing recorded and no available
action is absent; a query that legitimately returned zero shows `0` in its
count position. "No results" text appears only for a search the operator ran.
