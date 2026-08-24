# Files — FND-005

Surveyed 2026-08-24 against the working tree at `origin/main` `191ddf3342…`.
Every path below was confirmed with `ls` or `sed -n`; the six ADR files are new
and are marked as such.

## Where the change lands

| Path | Why |
| --- | --- |
| `docs/adr/0100-native-winui3-desktop-client.md` | **New.** Native WinUI 3 client converted inside this fork. Must also carry the reserved-block restatement, the ADR-0009 deferral-clause supersession as a `## Context` sentence, the decided D-001 consequence and the "prior documents" sentence — the body is immutable once merged, so anything missing needs a whole new ADR. The single agreed path, also named by [[FND-026]] (plan handle `DSK-02-01`) |
| `docs/adr/0101-local-execution-cloud-authority-split.md` | **New.** Adopts the six-question cloud-justification test as the repository's placement rule; relates ADR-0002. Must state that ADR-0014 is not superseded |
| `docs/adr/0103-gateway-not-direct-database-access.md` | **New.** Workstations never connect to the database; the gateway is `Pegasus.Web` evolved in place (L-01); relates ADR-0002, ADR-0015. Must state that ADR-0014 is not superseded |
| `docs/adr/0104-online-required-no-offline-replication.md` | **New.** Online-required, bounded local cache, no replication. Also claimed by [[FND-026]] — one number, one file |
| `docs/adr/0105-msix-app-installer-and-minimum-version-gate.md` | **New.** Two-layer enforcement (App Installer `UpdateBlocksActivation` plus the gateway minimum-client-version gate that fails closed), the D-002 self-managed certificate and the D-003 UNC feed; relates ADR-0007. **Three claimants** — this ticket, [[REL-001]] (plan handle `DSK-09-01`) and [[FND-042]] (plan handle `DSK-04-01`) — all naming this one path |
| `docs/adr/0110-pin-agent-skills-and-invocation-protocol.md` | **New.** Skill pinning by revision, the vendored tree, the invocation/review protocol; relates `skills-lock.json`. Co-claimed by [[TOOL-008]] (plan handle `DSK-12-08`), whose whole subject is this ADR and which already carries a `plan` document — check before writing |
| `docs/adr/README.md` | 6 new rows in the `## Current architecture decisions (`status: accepted`)` table (heading `:16`, header `:18-19`), in ID order, three cells each: `ADR | Title | Related FRD`. Do not touch the `## Superseded and relocated` table at `:43-52` |
| `docs/desktop/00-governance-and-workflow/README.md` | One-line correction to the § 8 table row at `:422` (the `docs/adr/0100…0110-*.md` row), which still instructs an ADR-0009 `superseded_by` note. Step 6 resolves that into a `## Context` sentence in ADR-0100 with ADR-0009 untouched; the row must say the same or the plan and the tree disagree. **Nothing else in this file** |
| `AGENTS.md` | One-line correction at `:114-117` — the index-shape sentence naming `ID \| Title \| Status \| Superseded-by \| Owner capability`, which `docs/adr/README.md:18-19` contradicts. **This ticket owns that correction**; three sibling tickets cite it instead of editing. **Nothing else in this file** |

## Context files

What the implementer must read to avoid a trap, and what each one tells them.

| Path | What it tells the implementer |
| --- | --- |
| `AGENTS.md:77-118` § ADR conventions | The frontmatter block to copy verbatim in shape (`id`, `status`, `date`, `supersedes`, `superseded_by`, `related_capabilities`, `related_frd`, `tags`); that IDs are never renumbered or reused; that published bodies are immutable; and — at `:84-90` — the operator-confirmed reserved-block exception that makes ADR-0100…0110 legitimate instead of a numbering violation. Also the one sentence in the file that is **wrong** (`:115`) and that this ticket fixes |
| `docs/adr/README.md:1-14` | That "the current architecture is the set below with `status: accepted`" and that published bodies are immutable — the reason a forgotten sentence costs a new ADR rather than an edit |
| `docs/adr/README.md:16-41` | The exact accepted-table shape. Three cells. Writing a five-cell row here is the defect the `AGENTS.md` sentence invites |
| `docs/adr/0029-image-initiated-case-projection.md:1-30` | The **current** house form, and the closest model to copy: frontmatter, then `# ADR-00NN: <title>`, then `## Status` opening with "Accepted." plus the supersession sentence, then `## Context`. It is also the worked example of *full* supersession (`supersedes: [ADR-0013]`) — the form ADR-0100 must **not** use |
| `docs/adr/0009-adopt-pegasus-monorepo-workspaces.md:1-10,73-77` | The worked example of *partial* supersession: the deferral clause ADR-0100 supersedes is at `:73-74`, the "supersedes ADR-0002 **only where**…" sentence at `:76-77`, and the frontmatter at `:5` is `supersedes: []`. This is the precedent that settles step 6 |
| `docs/adr/0028-run-integrated-renderer-in-web-container-app.md` | 84 lines, `## Status · Context · Decision · Consequences · Options considered · Links` — the length and shape a conversion ADR should land near |
| `docs/adr/0015-host-web-on-container-apps-consumption.md` | 66 lines and **no** `## Status` heading — evidence that the older house form differs. Do not copy this one's heading set; copy 0028/0029 |
| `docs/adr/0014-local-to-production-deployment.md` | The decision ADR-0101 and ADR-0103 must explicitly **not** supersede: local and production only, no Azure dev/test/staging. 28 lines |
| Any `related_frd:` line in `docs/adr/*.md` | The values are lowercase stems — `[frd-08]`, `[frd-10, frd-11]`, `[frd-01, frd-02, frd-05, frd-06, frd-12]`. There is no `[FRD-11]` anywhere in the tree. Writing the display form is a silent house-style break a reviewer will catch |
| `docs/desktop/00-governance-and-workflow/README.md` § 3 | The ADR set table (one row per ADR-0100…0110 with context and relations) and the cloud-justification test table to paste verbatim into each `## Context`. This is the content brief |
| `docs/desktop/README.md` § Locked decisions | D-002 (self-managed certificate, `LocalMachine\TrustedPeople`), D-003 (in-house UNC share over SMB) and C-01 (repositories become private — GitHub Releases/Pages ruled out permanently). ADR-0105 records these; it does not re-argue them |
| `.github/workflows/ci.yml:71-87` | That the `documentation` job runs on **every** change set, on `windows-latest`, and that it is `Test-TestMarkdownPlacement.ps1` (`:84`) and `Test-DocumentationLinks.ps1` (`:87`) — the two commands step 11 runs locally |
| `scripts/Test-MarkdownPlacement.ps1:31` | The allowed-roots regex. `docs/adr/**.md` is allowed, so these six files pass; a note file anywhere else does not. Also that `-Base` and `-Head` are **mandatory** parameters (`:2-5`) — the CI wrapper `Test-TestMarkdownPlacement.ps1` takes none |
| [[REL-001]]'s `open-questions` document | Where the ADR-0105 ownership question is tracked as a blocking box, with the measured 2026-08-24 state (`ls docs/adr/0105*` → no such file; all three claimants in `backlog`, none taken). Read it before assuming this ticket authors ADR-0105 |
| [[TOOL-008]]'s `plan` document | What [[TOOL-008]] intends ADR-0110 to contain, so that "verify and extend in place" is a real comparison rather than a guess |

## Ripple effects

- **`docs/adr/README.md`** gains six rows; nothing else in the repository indexes
  ADRs, so this is the only index ripple. `scripts/Test-DocumentationLinks.ps1`
  resolves the six new relative links and fails the CI `documentation` job if a
  filename and its row disagree.
- **`docs_todo` on other tickets.** Step 12 clears it wherever a conversion
  ticket's governing ADR now exists. That flag is what satisfies the
  `governing-doc` gate at `leave-backlog` for every `feature` ticket
  (`get_doc_gates` → `feature.leave-backlog: [governing-doc]`), so clearing it
  without a real `link_doc` in the same action would *remove* a satisfied gate.
  Link first, clear second, and only for tickets whose governing ADR is among
  these six.
- **Sibling ADR tickets.** [[FND-006]] (ADR-0102, 0106, 0107, 0109), [[FND-007]]
  (ADR-0108), [[FND-026]], [[FND-042]], [[REL-001]] and [[TOOL-008]] all add rows
  to the same `docs/adr/README.md` table. Whoever merges second rebases; the
  table is line-oriented and conflicts are trivial but real.
- **[[FND-008]]** (plan handle `DSK-00-08`) writes FRD-13 citing ADR-0100,
  ADR-0104 and ADR-0105 by relative path; if a filename here changes after that
  ticket lands, its links break in the CI lane.
- **[[FND-010]] and [[FND-013]]** own text that must be *inside* ADR-0100 before
  it merges. If they are not coordinated into this PR, both are left needing a
  superseding ADR — which is the cost step 7 asks be accepted knowingly.
- **No test, no generated client, no OpenAPI snapshot.** This change touches no
  contract; `openapi/pegasus-v1.json` and the generated client are unaffected.
  Say so in the post-implementation report so a reviewer does not go looking.

## Out of scope

Recorded here so the reviewer sees each was a decision, not an oversight.

- **ADR-0102, ADR-0106, ADR-0107, ADR-0109** — [[FND-006]]'s four flow-derived
  ADRs. They need the area 01 flow records first; this ticket needs nothing.
- **ADR-0108** — [[FND-007]] authors it as `proposed`; [[FEAT-038]] (plan handle
  `DSK-07-12`) performs the acceptance flip. Not written here.
- **`docs/frd/`** — no FRD is authored or edited. FRD-13 is [[FND-008]].
- **`docs/capabilities.md`** — no `DSK` family rows, no allocation-summary
  recompute. [[FND-008]].
- **`docs/adr/0009-adopt-pegasus-monorepo-workspaces.md`** — **unchanged, body and
  frontmatter.** The deferral-clause supersession is recorded in ADR-0100's
  `## Context` instead. Editing ADR-0009's frontmatter would assert full
  supersession and would require `status: superseded` on it and its removal from
  the accepted table.
- **`docs/adr/0002`, `0007`, `0014`, `0015`** — related, cited, never edited.
- **Every other line of `AGENTS.md` and of
  `docs/desktop/00-governance-and-workflow/README.md`** — exactly one line each.
- **`src/`, `tests/`, `.github/workflows/`** — no code, no CI change.
- **Azure** — no write, and no read either; nothing in these ADRs needs a live
  call.
