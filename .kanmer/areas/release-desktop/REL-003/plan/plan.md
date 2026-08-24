# Plan — REL-003: DSK-09-03 · `.appinstaller` templates (pilot/prod) and validator `eng/packaging/Test-AppInstaller.ps1`

**Diff estimate: ~15 files, ~620 lines.** Derived from the files document:
`Pegasus.appinstaller.template.xml` (~40 lines, copied verbatim);
`New-AppInstaller.ps1` (~70 lines); `Test-AppInstaller.ps1` (~230 lines — eight checks,
one function each, plus a namespace-aware reader and a result printer);
`Test-TestAppInstaller.ps1` (~120 lines, ten expectations); ten fixture XML files plus
two paired fixture manifests (~160 lines total, each fixture being the valid document
with one attribute changed). `docs/engineering.md:201-207` § Plan sizing requires the
estimate first.

## Approach

**One template, one substitution script, one validator with eight independent check
functions.** The template is copied verbatim from
`docs/desktop/09-release-update-and-distribution/appinstaller-template.md` § Template
rather than composed, because that page's inline comments carry the two rules the file
otherwise loses: `UpdateBlocksActivation` requires `ShowPrompt`, and the absence of a
`Dependencies` element is a consequence of the self-contained package (proposal § 7.1).
The rejected alternative was **two hand-maintained channel templates**: the only
per-channel difference is the `Uri` and an independent `.appinstaller` `Version` counter,
so two files would drift on every schema change and neither would be obviously
authoritative. A second rejected alternative was **building the XML from a PowerShell
object model** — it produces a file no one can diff against the documented template, and
the documented template is the thing the reviewer checks against.

Each validator check is its own function returning a named result, and the script prints
a pass/fail list and exits non-zero if any failed. That shape is chosen over
`throw`-on-first-failure because a release engineer wants to see *all* the violations in
one run, and because `Test-TestAppInstaller.ps1` must assert that a fixture produces
**exactly** its own named failure and no other.

## Governing docs

The ticket's `refs` list is **empty** and its frontmatter carries `docs_todo: true`
(`get_doc_gates REL-003`). No existing PRD/FRD/ADR is claimed to be met.

> **New ADR** — ADR-0105 (signed MSIX / App Installer distribution with a gateway
> minimum-version gate), authored by [[REL-001]] (plan handle `DSK-09-01`); see
> [[REL-001]]'s plan for the ownership reconciliation — ADR-0105 has three claimants
> (`REL-001`, `FND-005`, `FND-042`). This plan implements
> its Decision clauses (a) — the 2021 schema with
> `OnLaunch HoursBetweenUpdateChecks="0" ShowPrompt="true" UpdateBlocksActivation="true"`,
> `AutomaticBackgroundTask` and `ForceUpdateFromAnyVersion` — and (d) — one package
> identity `CollisionEngineers.Pegasus` with two feeds, `pilot/` and `prod/`. Steps 3 and
> 5 below are what satisfies them. This plan is written to the decisions as recorded in
> `docs/desktop/09-release-update-and-distribution/README.md` § 3 and
> `appinstaller-template.md`; if ADR-0105 lands with different attribute values, this
> plan is revised before implementation.

Binding operator decisions, written to as decisions and never as options:

- **D-003** (2026-08-23) — the feed is an **in-house UNC file share** over SMB. The `Uri`
  values are therefore `\\<host>\<share>\<channel>\…` paths. Check 2 compares against
  that form. There is no HTTP variant to support and no MIME / `Content-Length` /
  byte-range check to write; constraint **C-01** rules out GitHub Releases and GitHub
  Pages permanently and they must not be reintroduced as a fallback.
- **D-002** (2026-08-23) — signing is a **self-managed certificate**. Check 4 compares
  `MainPackage/@Publisher` against the `signerSubject` field of
  `desktop-release-manifest.json`, which records that certificate's subject.

Contract this plan **consumes**: the thirteen manifest fields fixed by
[[REL-002]] (plan handle `DSK-09-02`), in particular `version`, `packageSha256`,
`signerSubject`, `appInstallerVersion` and `channel`.

## Routing

Copied from the ticket body's `## Routing` block, which
`docs/desktop/00-governance-and-workflow/README.md` § Ticket template makes mandatory in
the plan document.

- **Subagent**: `pegasus-release-packager` — `.codex/agents/pegasus-release-packager.toml`
  (verified present).
- **Skills**, loaded in this order: `pegasus-desktop`
  (`.agents/skills/project/pegasus-desktop/SKILL.md`, verified present) →
  `winui-packaging` (`.codex/skills/winui-packaging/SKILL.md`, vendored from
  `microsoft/win-dev-skills` v0.5.0 `f1028dd5`, verified present; the path moves to
  `.agents/skills/vendor/windows/winui-packaging/` once
  [[TOOL-002]] (plan handle `DSK-12-02`) lands).
- **MCP**: Kanmer (`get_status`, `get_doc_gates`, `take_ticket`, `set_ticket_doc`,
  `append_scratch`, `move_item`); Microsoft Learn (`microsoft_docs_search`,
  `microsoft_docs_fetch`) for the App Installer update-settings and schema pages.
- **Kanmer pipeline** for profile `feature`: `kanmer-research` → `kanmer-plan` →
  `kanmer-execute` → `kanmer-review` → `kanmer-verify` → `kanmer-closeout`. Call
  `get_doc_gates REL-003` before every move; a move crosses at most one gated boundary.
  `get_doc_gates` reports four gated boundaries: `leave-backlog` needs `governing-doc`
  (already satisfied by `docs_todo: true`), `leave-preparing` needs `research`, `files`,
  `plan`, `checklist` **and `questions-resolved`**, `enter-review` needs
  `post-implementation-report` **and `questions-resolved`**, `enter-done` needs
  `proof` **and `questions-resolved`**. `questions-resolved` sits at three of the four
  and **never at `leave-backlog`**.
- **Reviewer**: `pegasus-desktop-reviewer` — an agent that did not implement
  (`AGENTS.md` § Repository task workflow step 5).

## Steps

These refine the body's eleven implementation steps in the same order, same ownership,
same paths.

1. **Orient and take.** Read `appinstaller-template.md` **in full** (it is the whole
   ticket), then the area plan § 5 row `DSK-09-03` and § 7 traps.
   `get_doc_gates REL-003`, then `take_ticket REL-003`.
2. **Confirm the attribute names against official documentation before writing the
   template.** Load `winui-packaging`. `microsoft_docs_search` for
   `App Installer update settings OnLaunch HoursBetweenUpdateChecks UpdateBlocksActivation`,
   then `microsoft_docs_fetch` on
   <https://learn.microsoft.com/windows/msix/app-installer/update-settings>. Confirm two
   things and record the fetch date in the ticket scratch: the attribute spellings, and
   that `UpdateBlocksActivation` requires `ShowPrompt="true"`.
3. **Create `eng/packaging/Pegasus.appinstaller.template.xml` by copying the body's
   fenced XML byte for byte**, comments included. `eng/` does not exist yet — this ticket
   creates `eng/packaging/`. Do not reformat, do not re-indent, do not drop the
   `UpdateUris` block that is deliberately commented out.
4. **Create `eng/packaging/New-AppInstaller.ps1`.** Header:
   `[CmdletBinding()]`, `Set-StrictMode -Version Latest`,
   `$ErrorActionPreference = 'Stop'` — the convention every script in `scripts/` follows
   (`scripts/Get-CiChangeFlags.ps1:8-9`). It reads `desktop-release-manifest.json`, takes
   `-Channel` and `-FeedRoot`, and substitutes the five placeholders. Two rules the
   substitution must enforce itself: the output must contain **no remaining `<…>`
   placeholder** (fail with a named message if one survives), and `<feed>/<channel>`
   must produce a UNC path of the form `\\<host>\<share>\<channel>` under D-003 — with no
   trailing separator and no drive letter.
5. **Create `eng/packaging/Test-AppInstaller.ps1`** with parameters
   `-AppInstallerPath`, `-Channel` (`ValidateSet 'pilot','prod'`), `-ManifestPath`,
   `-PreviousAppInstallerPath` and a `-Rollback` switch. Read the document **namespace
   aware** — this is the correctness prerequisite, not a detail:
   `Select-Xml -Path $AppInstallerPath -Namespace @{ ai = 'http://schemas.microsoft.com/appx/appinstaller/2021' } -XPath '/ai:AppInstaller'`.
   A bare `/AppInstaller` XPath matches nothing on a namespaced document and every check
   would silently pass on an empty node set. Then implement the eight checks from
   `appinstaller-template.md` § Validator outline, one function each, each with its own
   named failure message:
   1. namespace is `http://schemas.microsoft.com/appx/appinstaller/2021`;
   2. `Uri` equals `<feed>/<channel>/Pegasus.appinstaller` for the channel;
   3. `Version` is strictly greater than the last published version for the channel
      (four-part numeric compare);
   4. `MainPackage/@Name` is `CollisionEngineers.Pegasus`, `@ProcessorArchitecture` is
      `x64`, `@Version` equals the manifest `version`, `@Publisher` equals the manifest
      `signerSubject`;
   5. `MainPackage/@Uri` resolves to a file whose SHA-256 equals the manifest
      `packageSha256`;
   6. `UpdateSettings/OnLaunch` has `HoursBetweenUpdateChecks="0"`, `ShowPrompt="true"`,
      `UpdateBlocksActivation="true"`; `ForceUpdateFromAnyVersion` is `true`;
      `AutomaticBackgroundTask` present;
   7. no `Dependencies` element unless the manifest says framework-dependent;
   8. rollback mode — a `MainPackage/@Version` lower than the previous is allowed only
      when `-Rollback` is passed **and** `ForceUpdateFromAnyVersion` is `true`.
   Two behaviours the outline leaves implicit and this plan fixes: check 3 **passes with
   a recorded note** when `-PreviousAppInstallerPath` is absent, because a first publish
   on a channel is the base case and not a violation; and check 5 resolves
   `MainPackage/@Uri` **relative to the `.appinstaller`'s own directory** when the target
   is a local file, so the validator runs at build time before any feed exists.
6. **Make the version compare real.** Cast both sides with `[version]`, not string
   compare — `1.0.9.0` must sort below `1.0.10.0`. Apply the same cast in checks 3, 4
   and 8.
7. **Print a pass/fail list to stdout and exit non-zero on any failure.** Collect results
   rather than throwing on the first, so a release engineer sees every violation in one
   run and so `Test-TestAppInstaller.ps1` can assert that a fixture produced *exactly*
   its own named failure. `exit 1` at the end; both consumers gate on the exit code.
8. **Create `eng/packaging/fixtures/appinstaller/`** with one fixture per failure —
   `schema-2017.xml`, `wrong-uri.xml`, `version-not-monotonic.xml`,
   `publisher-mismatch.xml`, `hash-mismatch.xml`, `missing-showprompt.xml`,
   `unexpected-dependencies.xml`, `downgrade-without-rollback.xml` — plus `valid-pilot.xml`
   and `valid-prod.xml`. Build each failing fixture as the valid document with **one**
   attribute changed, so the test proves the check fires on that attribute alone. Each
   fixture is paired with a fixture `desktop-release-manifest.json`; the Publisher and
   hash come from the paired manifest, **never** from a literal inside the validator, so
   the fixtures stay valid when [[REL-007]] (plan handle `DSK-09-08`) fixes the real
   subject.
9. **Create `eng/packaging/Test-TestAppInstaller.ps1`** in the shape of
   `scripts/Test-CiChangeFlags.ps1:9-30`: a local `Assert-Failure`/`Assert-Pass` helper
   that `throw`s a message beginning with the case name, then a flat list of ten cases.
   Each failing case asserts a non-zero exit **and** the presence of its own named
   message; each valid case asserts exit `0`.
10. **Run it**: `pwsh ./eng/packaging/Test-TestAppInstaller.ps1`, expected exit code `0`
    with ten reported expectations met.
11. **Simplification pass.** Record it under a dated `## Simplification pass` heading in
    this document (`AGENTS.md` § Repository task workflow step 4). This branch adds
    scripts and fixtures, so `n/a — docs-only` does not apply.

## Verification

Evidence tier from the body: **Tier 1 — Static/build/architecture.** The obligation is
fixture-driven proof that the validator's failures fire. App Installer's real behaviour is
proven later by the packaging suite [[TEST-010]] (plan handle `DSK-08-10`) against the
local feed, and this ticket must not claim it. `proof` is the captured stdout and exit
codes of the three commands below, as proof type `test-output` for the first and
`command-log` for the others.

| Command | Expected evidence |
| --- | --- |
| `pwsh ./eng/packaging/Test-TestAppInstaller.ps1` | exit `0`; every fixture reports its expected named failure and both valid fixtures pass; ten expectations printed |
| `pwsh ./eng/packaging/Test-AppInstaller.ps1 -AppInstallerPath ./eng/packaging/fixtures/appinstaller/schema-2017.xml -Channel pilot -ManifestPath ./eng/packaging/fixtures/appinstaller/valid-pilot.manifest.json` | non-zero exit; the printed failure names the 2021-namespace check and no other check reports a false positive |
| `Select-Xml -Path ./eng/packaging/Pegasus.appinstaller.template.xml -XPath '/*'` | the root element namespace is `http://schemas.microsoft.com/appx/appinstaller/2021` |

Behaviour to read, not infer: diff the created template against the fenced block in
`docs/desktop/09-release-update-and-distribution/appinstaller-template.md` § Template and
confirm it is identical apart from nothing at all — comments included. That diff is the
acceptance criterion no command checks.

Note for whoever runs CI on this branch: `scripts/Get-CiChangeFlags.ps1:11`'s
`$buildPattern` does **not** match `^eng/`, so these tests run in **no** CI lane until
[[REL-005]] (plan handle `DSK-09-05`) step 3 resolves the change-flag question. Run them
locally and say so in the proof rather than pointing at a green CI run that never executed
them.

## Risks / open questions

- **Risk — a namespace-blind XPath makes every check pass.** The single worst failure mode
  here: the validator reports "pass" on files it never read. Mitigation: step 5 fixes the
  namespace-prefixed reader first, and the `schema-2017.xml` fixture is the proof that the
  reader actually reads — it must fail, and if it passes the reader is wrong.
- **Risk — a string version compare.** `1.0.10.0` would sort below `1.0.9.0` and a
  non-monotonic publish would pass. Mitigation: step 6 and the
  `version-not-monotonic.xml` fixture, whose values must straddle a ten boundary
  (for example previous `1.0.10.0`, candidate `1.0.9.0`) so a string compare is caught.
- **Risk — check 5 cannot resolve a UNC path at build time.** Assumption A-09-1 in the
  research document. Mitigation: step 5's relative-resolution rule; the fixture set
  exercises the local-file path only, and R9 step 4 ([[REL-008]], plan handle `DSK-09-10`)
  owns the published-file check.
- **Risk — the fixtures encode a Publisher that never matches reality.** The subject is
  fixed by [[REL-007]]. Mitigation: step 8's rule that the Publisher
  comes from the paired fixture manifest, never a literal in the validator.
- **Risk — someone adds an HTTP header check.** MIME, `Content-Length` and byte ranges are
  HTTP-only and do not exist over SMB (D-003); such a check would make the validator
  unrunnable against the real feed. Mitigation: it is a Guardrail in the body and an
  explicit Out-of-scope entry in the files document.
- **Open questions: none opened, and the reason is not cost.** An earlier draft of this
  section said "an unticked item would block every stage move for values another ticket is
  already scheduled to supply". The first half is false and is withdrawn: an unticked
  `- [ ]` line above `## Parked` blocks exactly `leave-preparing`, `enter-review` and
  `enter-done`, and never `leave-backlog`. For this `feature` that is three of the four
  gated boundaries, and blocking Preparing would have been affordable if there were a real
  question.

  The second half is the real reason and it stands on the authoring contract's own rule:
  the concrete Publisher string and the concrete UNC root are **inputs supplied by named
  sibling tickets** — [[REL-007]] and [[REL-008]] — which makes them scope boundaries
  recorded here rather than `open-questions` entries. And they do not gate this ticket at
  all: every fixture is paired with its own fixture manifest, so the validator is fully
  testable before either value exists. Nothing in this ticket's body instructs that a
  question be recorded in `open-questions/`.

## Simplification pass

_Not yet run. `AGENTS.md` § Repository task workflow step 4 requires a pass over this
branch's own diff before the PR, recorded here under a dated heading. This branch adds
scripts, a template and fixtures, so `n/a — docs-only` does not apply._
