# FND-031 post-implementation report

## Implementation

Commit c39ea6f0246b4ef664f1b96bfe2a0bf7abc9eac0 adds Pegasus.Desktop.Infrastructure with the HTTP request handler and GET-only retry handler, DPAPI credential store, bounded in-memory snapshot cache, rolling redacted diagnostics writer, package-version provider, project/package registration, solution/project wiring, dependency-direction assertion, and current-architecture update.

The project has exactly the permitted Pegasus.Core and Pegasus.Contracts project references. No Azure, database, Graph, Box, server, durable-cache, generated-client, token-flow, diagnostics-bundle, or test-scaffold work was added.

## Validation

- dotnet restore .\src\Pegasus.Desktop.Infrastructure\Pegasus.Desktop.Infrastructure.csproj -r win-x64 --force-evaluate — passed.
- dotnet build .\Pegasus.slnx --configuration Release --no-restore -nr:false -p:UseSharedCompilation=false -p:BuildInParallel=false -p:NodeReuse=false --verbosity minimal — passed with 0 warnings and 0 errors.
- dotnet test .\tests\Pegasus.ArchitectureTests\Pegasus.ArchitectureTests.csproj --configuration Release --no-build --no-restore --verbosity minimal — passed 121/121.
- Forbidden-reference scan over src/Pegasus.Desktop.Infrastructure — no matches.
- git diff --check — passed.

## Acceptance status

The implementation satisfies the project-boundary, request-header, GET-only retry, DPAPI, bounded-cache, diagnostics-bound, and forbidden-reference requirements by inspection and build. The required credential-store and handler unit tests are not yet present because tests/Pegasus.Desktop.ViewModelTests does not exist; EPIC-003 assigns that scaffold to FND-038, while the board dependency currently places FND-038 after FND-031. This sequencing conflict is recorded in the plan. Those tests and merged-main proof remain open; FND-031 is not eligible for Done.

An independent desktop reviewer is reviewing the exact commit to determine whether this implementation can merge as the prerequisite for FND-038 without representing FND-031 as complete.

No cloud, deployment, credential, mailbox, Box, upstream, or other external write was performed.

## Exact-head and merge update — 2026-08-29

The implementation branch's exact reviewed head was `26aae2fa5a072e3518d93db0afdd8c241dd3a4bd` (the earlier `c39ea6f` remains the initial implementation commit). Exact-head CI run `33261673009` passed all required repository lanes. PR #42 merged that exact head into `dev` as `89fcfa20cb570845dbb1ad9b2f3c45fdd83723e4`.

This remains a prerequisite-only merge. The FND-031-specific credential-store, header/correlation, retry asymmetry, and isolation tests are not yet present; [[FND-038]] owns adding them to the shared test project. Merged-main proof is also not yet available, so this ticket is not eligible for Done.

## Redaction remediation — 2026-08-29

Commit 627d3f613234a75203f1c7115ea590a2a176b199 corrected two findings from the prior redaction review: sensitive fields now redact a complete bearer value, and pipe-delimited context is preserved. Fresh independent review passed the exact commit. Infrastructure build passed with 0 warnings/errors; existing ViewModelTests passed 6/6; direct redaction and retention smoke passed; git diff --check passed. The shared test fixtures remain owned by FND-038 and are not duplicated here. FND-031 remains incomplete pending those tests and merged-main proof.

## Merge checkpoint — 2026-08-29

The reviewed follow-up PR #43 merged to `dev` as `52a1741cfa6544dfdad2632b5192a162c2430a2f` from exact head `627d3f613234a75203f1c7115ea590a2a176b199`. Exact-head CI run `33265617566` completed successfully across all required active lanes, including SQL integration shards 1–3 and aggregate SQL coverage; the infrastructure lane was intentionally skipped. The follow-up remains a prerequisite correction, not full ticket delivery.

## Follow-up correction — 2026-08-29

A narrow registration correction was added at `bec8d1bcd4465078e2ea3fab9a9188081118d00c`: missing gateway-address failure is now deferred from registration to named HttpClient creation so the host's `ValidateOnStart()` path can report invalid configuration at start. The infrastructure Release build passed with zero warnings/errors. Independent review and PR are still pending; no completion claim is made.
