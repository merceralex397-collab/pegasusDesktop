# Approved prototype-to-page screen map

This is a reference artefact, not a design authority. The binding design rules
remain in [`docs/design/README.md`](../README.md); this map preserves the join
between each approved Claude Design prototype and the Razor page it restated so
the native screen specifications can be checked against the same source.

## Capture provenance

- Source used: the read-only fallback from upstream Kanmer `PLAT-001`'s
  `files/files.md`, because DesignSync project access was not available in this
  session. That document records that its table was taken from the design
  project's own `github.md` and verified against the tree.
- Claude Design project: `710bb42f-84ed-4d82-b216-7c5d60fb5aef`, **Pegasus
  Design**.
- `github.md` source values: `repo: collisionengineers/pegasus`; last sync
  `2026-08-16`.
- Source snapshot: `collisionengineers/pegasus` `origin/kanmer-board` at
  `a5b28111`; capture date: `2026-08-25`.
- The source record covers 21 screen prototypes plus one screen-local
  scaffolding/search row. The desktop join has fifteen `— replaces
  \`Pages/…\`` headings in `screen-specs.md`; the earlier plan-gap note's count
  of eighteen is corrected here rather than inferred.

## Prototype map

The first two columns preserve the source map's prototype names and Razor paths.
The screen-specification column names the replacement heading and line. “Remote
only” means the prototype source bytes are not in this repository; related
reference assets already present in the tree are listed in the final column and
hashed in [Asset hashes](#asset-hashes).

| Prototype | Razor page(s) it was drawn from | screen-specs.md heading | Assets in tree | Notes |
| --- | --- | --- | --- | --- |
| `Dashboard.html` | `Pages/Index.cshtml` | `Dashboard — replaces Pages/Index.cshtml.cs, Presentation/RailCountsPageFilter.cs` (line 129) | Remote only | The prototype source is not mirrored. |
| `Inbox.html` | `Pages/Mail/Index.cshtml` | `Inbox — replaces Pages/Mail/Index.cshtml.cs (list, preview) and Pages/Mail/Message.cshtml.cs (detail)` (line 248) | Remote only; related Inbox mockup bundle is in tree | The related bundle is not claimed as the original prototype source. |
| `InboxMessage.html` | `Pages/Mail/Message.cshtml` | `Inbox — replaces Pages/Mail/Index.cshtml.cs (list, preview) and Pages/Mail/Message.cshtml.cs (detail)` (line 248) | Remote only; related Inbox mockup bundle is in tree | The bundle covers the approved message-page redesign. |
| `Upload.html` | `Pages/Upload.cshtml` | `Upload — replaces Pages/Upload.cshtml.cs, UploadStatus, UploadGroupStatus` (line 309) | Remote only | — |
| `UploadLink.html` | `Pages/Uploads/Request.cshtml` | **No desktop screen** | Remote only | The external token-bound upload surface remains web; it has no desktop replacement heading. |
| `Queues.html` | `Pages/Triage/Index.cshtml` | `Queues (pre-engineer work) — replaces Pages/Triage/Index, Pages/Unidentified/Index, Pages/ImageIntake/Index, and the case-stage queues` (line 148) | Remote only | — |
| `Cases.html` | `Pages/Cases/Index.cshtml` | `Cases list and search — replaces Pages/Cases/Index.cshtml.cs, Pages/Search/Index.cshtml.cs (UI-07)` (line 163) | Remote only | — |
| — | `Pages/Search/Index.cshtml` | `Cases list and search — replaces Pages/Cases/Index.cshtml.cs, Pages/Search/Index.cshtml.cs (UI-07)` (line 163) | No prototype bytes | No prototype: the backing query shares the Cases screen; no separate approved prototype was listed. |
| `Case.html` | `Pages/Cases/Details.cshtml` + `Pages/Cases/Shared/_CaseSummary.cshtml`, `_CaseDocuments.cshtml`, `_CaseHistory.cshtml`, `_CaseWorkflow.cshtml` | `Case workspace — replaces Pages/Cases/Details.cshtml.cs and its handler families ...` (line 178) | Remote only | The heading's complete handler list is authoritative in `screen-specs.md`. |
| `Assessment.html` | `Pages/Cases/Assessment/Index.cshtml` | `Case workspace — replaces Pages/Cases/Details.cshtml.cs and its handler families ...` (line 178) | Remote only | Assessment is a workspace tab, not a separate replacement heading. |
| `CreateCase.html` | `Pages/Cases/Create.cshtml` | `Case create — replaces Pages/Cases/Create.cshtml.cs` (line 233) | Remote only | — |
| `Operations.html` | `Pages/Operations/Index.cshtml` | `Operations — replaces Pages/Operations/Index.cshtml.cs` (line 390) | Remote only | — |
| `Administration.html` | `Pages/Administration/Index.cshtml` | `Administration — replaces Pages/Administration/** (16 page models)` (line 401) | Remote only | — |
| `AdminAccounts.html` | `Pages/Administration/Accounts/Index.cshtml` | `Administration — replaces Pages/Administration/** (16 page models)` (line 401) | Remote only | — |
| `AdminRoles.html` | `Pages/Administration/Roles/Index.cshtml` | `Administration — replaces Pages/Administration/** (16 page models)` (line 401) | Remote only | — |
| `AdminAccess.html` | `Pages/Administration/Access/Index.cshtml` | `Administration — replaces Pages/Administration/** (16 page models)` (line 401) | Remote only | — |
| `AdminOrganizations.html` | `Pages/Administration/Organizations/Index.cshtml` | `Administration — replaces Pages/Administration/** (16 page models)` (line 401) | Remote only | — |
| `AdminPrincipals.html` | `Pages/Administration/Principals/Index.cshtml` | `Administration — replaces Pages/Administration/** (16 page models)` (line 401) | Remote only | — |
| `AdminConfiguration.html` | `Pages/Administration/Configuration.cshtml` | `Administration — replaces Pages/Administration/** (16 page models)` (line 401) | Remote only | — |
| `AdminMailboxes.html` | `Pages/Administration/Mailboxes.cshtml` | `Administration — replaces Pages/Administration/** (16 page models)` (line 401) | Remote only | — |
| `AdminAutomation.html` | `Pages/Administration/Automation/Index.cshtml` + `Activity.cshtml` | `Administration — replaces Pages/Administration/** (16 page models)` (line 401) | Remote only | — |
| `ChangePassword.html` | `Pages/Account/PasswordChange.cshtml` | `Change password — replaces Pages/Account/PasswordChange.cshtml.cs` (line 108) | Remote only | Auth shell is already represented by the retained web layout. |

## Screen-specification headings with no prototype

These headings are explicitly retained in `screen-specs.md` but have no row in
the 21-prototype source map: **Shell** (line 41), **Sign in** (line 85), **Update
required / Blocked** (line 99), **Diagnostics and settings** (line 116),
**Received item** (line 271), **Triage detail** (line 287), and **Unidentified
and Vehicle images** (line 298). They are recorded as “no prototype”, not
silently assigned to a nearby screen.

## Asset hashes

The original prototype bytes are remote only. These are the related reference
files already in the repository, recorded by SHA-256 rather than substituted
for the source prototypes.

| In-tree related asset | SHA-256 |
| --- | --- |
| `docs/design/references/mockups/candidate-a-operations-first.png` | `87A7122B1F10029233F8FFEDA0983442B111214CED18A9B93EB59C90E70FC51F` |
| `docs/design/references/mockups/candidate-b-worklist-first.png` | `BAF857949310F9840B8EBBE237F77E7B43532D6B18B549E0597849C070DA8D07` |
| `docs/design/references/mockups/candidate-c-case-first.png` | `FED2BA79C803DCD8D8DF70B652BD01F3D733F08BD0C7A1A10120FD1655BF7893` |
| `docs/design/references/mockups/inbox-message-page/canvas.json` | `CF46F2049A6EDAD05672EA1CED33C0CD5F7C49E127AC40791B03CEDC574F2682` |
| `docs/design/references/mockups/inbox-message-page/Case.dc.html` | `E797611F3E48324EB97B51B0DB6C7F7257DA45C06DF8B43EEE0F31CB03A3C314` |
| `docs/design/references/mockups/inbox-message-page/CaseLinked.dc.html` | `4278E44BA79B9C5711DDCBB3D8E9F6332C6853724E14B0F6767DCF1015721C7B` |
| `docs/design/references/mockups/inbox-message-page/Correcting.dc.html` | `48594AD87A8EEB7B03E8B126A504344AFF6DF1E0BD9352DA6C9F05CB030E081F` |
| `docs/design/references/mockups/inbox-message-page/Dialogs.dc.html` | `CA9D00D7FCA603090A4F17DB527C3F2F5724848D92958241BCF2939D336D158C` |
| `docs/design/references/mockups/inbox-message-page/Filed.dc.html` | `11A80BABD73D0A7AC03031B6373678E45CCBE7C1EA4D5BBD0E591CDF8F82F4D0` |
| `docs/design/references/mockups/inbox-message-page/FolderStates.dc.html` | `E1BC447A8C15720CCFF06B0C2DA5A9AE88B5176A4062C5B24CC1EFA404F58D79` |
| `docs/design/references/mockups/inbox-message-page/Main.dc.html` | `CD822A574D0078395D2B4867D1607A437CDCAD8B58637CC6F2E361CDE1E1CCEF` |
| `docs/design/references/mockups/inbox-message-page/Moving.dc.html` | `F3CBA1DC348A6B954CE1E74E2811DE998136A9DAC9FBF9C584EC6D69C400AAAA` |
| `docs/design/references/mockups/inbox-message-page/preview/Case.html` | `364DB95726C96FB1201DA503CDEC7B28F5B0CDAC3B5B9701D4DFDAF79D550920` |
| `docs/design/references/mockups/inbox-message-page/preview/CaseLinked.html` | `9DC172999C5D06F5EC80F8EE734110348134A1CE8489D0F8D6C6889291289C01` |
| `docs/design/references/mockups/inbox-message-page/preview/Correcting.html` | `52DDCDC7852AC2E012DE734009F630242C8B6A26D79D5A98836024BB1FAEC44A` |
| `docs/design/references/mockups/inbox-message-page/preview/Dialogs.html` | `CF2880172E911BC09523BEBC8A522817E6475846EDDD694FADD5A029DB9AB5BE` |
| `docs/design/references/mockups/inbox-message-page/preview/Filed.html` | `3A525E6F4B4D648BF75BA47DBAECC12239531AA319F5BBC861EEF895B3B69454` |
| `docs/design/references/mockups/inbox-message-page/preview/FolderStates.html` | `2E33E6698E4E981ACB123CAE99160B2E9FC33FB358EACD0BA3DB9E8D7C80BE03` |
| `docs/design/references/mockups/inbox-message-page/preview/Main.html` | `697366079D8A282E80DB7DE5CC2B48229AB41DACC73ADDFE29A67818C677CBE0` |
| `docs/design/references/mockups/inbox-message-page/preview/Moving.html` | `F1BF4E1A1AAAE72637B47AA34579AE22477C8ABC3DA7937E34C8865FC62ECBC9` |
