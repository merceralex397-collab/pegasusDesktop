# Checklist — FND-031

One box per plan step, in plan order. Each is independently tickable: it names the file or command
whose completion makes the box true.

- [ ] Read `src/Pegasus.Contracts/PegasusHeaders.cs` and `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:23-39`; note that `:33` forbids `System.Net.Http` for **Core** and that this list is not reusable for the desktop. Run `get_doc_gates FND-031`; `take_ticket` on branch `task/desktop-infrastructure` from `origin/dev`.
- [ ] Create `src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj`: `Microsoft.NET.Sdk`, `net10.0-windows10.0.26100.0`, `TargetPlatformMinVersion 10.0.22000.0`, `<Platforms>x64</Platforms>`, `ImplicitUsings`, `Nullable`, and exactly two `ProjectReference` entries (`Pegasus.Core`, `Pegasus.Contracts`).
- [ ] Add `System.Security.Cryptography.ProtectedData` (**version ≥ 9.0.4**, matching the transitive resolution at `src/Pegasus.Infrastructure/packages.lock.json:887-891`) and `Microsoft.Extensions.Http` to `Directory.Packages.props`, and reference both from the new csproj without version literals.
- [ ] Create the folders `Api/`, `Authentication/`, `Caching/`, `Diagnostics/`, `Windows/` — and confirm `Documents/` was **not** created (no caller until area 05).
- [ ] Write `Api/PegasusRequestHandler.cs`: a `DelegatingHandler` setting `PegasusHeaders.ClientVersion` from an injected `IClientVersionProvider` and `PegasusHeaders.CorrelationId` to a fresh `Guid` only when the caller supplied none, exposing the correlation id to the logger scope, using the Contracts constants and no string literals.
- [ ] Write the `Package.Current.Id.Version` implementation of `IClientVersionProvider` under `Windows/`, so the handler is testable without package identity.
- [ ] Write `Api/GatewayOptions.cs` and `Api/PegasusHttpClientRegistration.cs` with `AddPegasusApiClient(this IServiceCollection, Action<GatewayOptions>)` calling `AddHttpClient("pegasus")`, setting `BaseAddress` from options and adding the handler.
- [ ] Scope the bounded jittered retry to idempotent `GET` requests only, with a code comment citing `docs/desktop/03-gateway-api-and-data/README.md:173`; confirm no policy is attached to the named client as a whole.
- [ ] Write `Authentication/IDesktopCredentialStore.cs` (`Save`, `TryRead`, `Clear`) and `Authentication/DpapiCredentialStore.cs` using `ProtectedData.Protect`/`Unprotect` at `DataProtectionScope.CurrentUser`, one file per key under a constructor-injected `storeRoot`; confirm the access token is never written.
- [ ] Write `Caching/BoundedSnapshotCache.cs` with an explicit entry-count cap and per-entry expiry, in-memory only — no file-backed store and no SQLite.
- [ ] Write `Diagnostics/IDiagnosticsWriter.cs` and its rolling-file implementation with a total-size cap, a retention count and a redaction hook that strips bearer tokens, refresh tokens and password fields before a line is written; confirm the bundle is **not** built here.
- [ ] Add `<Project Path="src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj" />` to `Pegasus.slnx`, and confirm it is **not** added to the server entry point from [[FND-028]] (plan handle `DSK-02-03`).
- [ ] Extend the ordinal expected array at `tests/Pegasus.ArchitectureTests/DependencyDirectionTests.cs:137-149`, placing the new path immediately after `src/Pegasus.Desktop/Pegasus.Desktop.csproj`.
- [ ] Add a `ProjectReference` from `src/Pegasus.Desktop/Pegasus.Desktop.csproj` to this project.
- [ ] Run the boundary inspection `grep -rn 'EntityFrameworkCore\|Azure\.\|Box\.\|Microsoft.Graph\|Microsoft.AspNetCore\|Pegasus.Infrastructure' src/Pegasus.Desktop.Infrastructure` and record in the proof that it is an inspection, with [[FND-037]] (plan handle `DSK-02-12`) owning the enforcing test and needing a **new** desktop prefix list.
- [ ] Record in the plan the sequencing note: `tests/Pegasus.Desktop.ViewModelTests` does not exist, only that project can host `ProtectedData`/`ApplicationData` tests, and [[FND-038]] (plan handle `DSK-02-13`) must land first — no test scaffold is duplicated here.
- [ ] Land the credential-store and handler unit tests in `tests/Pegasus.Desktop.ViewModelTests` once it exists.
- [ ] Run `dotnet restore ./src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj -r win-x64 --force-evaluate` and commit the generated lock file.
- [ ] Add the project and its two permitted references to `docs/current-architecture.md` § Components and dependency direction (`:55`).
- [ ] Run the simplification pass over this branch's diff and record it under a dated `## Simplification pass` heading in the plan document.
- [ ] Verification run (this box produces `proof`): `dotnet restore ./Pegasus.slnx --locked-mode`; `dotnet build ./Pegasus.slnx --configuration Release` (exit 0, `0 Warning(s)`); `dotnet test ./tests/Pegasus.Desktop.ViewModelTests/Pegasus.Desktop.ViewModelTests.csproj --configuration Release` covering the credential-store round-trip, missing key, corrupted blob, store isolation, both header cases and the `GET`-retried / `POST`-not-retried pair; the boundary `grep` (no matches); `grep -c 'ProjectReference' src/Pegasus.Desktop.Infrastructure/Pegasus.Desktop.Infrastructure.csproj` (exactly `2`); and `git diff --stat src/Pegasus.Infrastructure/packages.lock.json src/Pegasus.Web/packages.lock.json` (no change). Capture every output as tier-2 evidence.

## Progress notes

(append with `set_ticket_doc(doc: "checklist", append: true)`)
