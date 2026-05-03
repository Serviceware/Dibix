# Plan: `dibix artifact` — Consumer Version Management & Local Deploy

## Background

The PowerShell script `D:\Serviceware\Common\Scripts\dibix-nuget.ps1` is a developer-local tool
that packages Dibix and deploys it to the local NuGet cache so that consumer repos (e.g. Helpline)
can test unreleased builds without pushing to a feed. It also has remote-feed modes used at
release time (`push` to the internal Azure DevOps feed, `unlist` from nuget.org + DockerHub).

**The goal is to delete `dibix-nuget.ps1` once every mode is reachable from `dibix artifact`.**
The dev-loop modes (`clear` / `reset` / `deploy`) are already ported. The remote modes
(`push` / `unlist`) are still pending — they were initially deferred but stay on the critical
path for retiring the script.

---

## Status snapshot (2026-05-06)

| PowerShell mode | CLI sub-command | Status |
|---|---|---|
| `clear` | `dibix artifact clear` | ✅ Done |
| `reset [Name]` | `dibix artifact reset [package-name]` | ✅ Done — also reverts consumer changes via libgit2 |
| `deploy [Name]` | `dibix artifact deploy [package-name] [-c Debug\|Release]` | ✅ Done |
| `push [Name]` | `dibix artifact push [package-name]` | ✅ Done — `PushNuGetPackageCommand`; `--source` (required, no default) + `--api-key` (optional) both backed by env vars |
| `unlist <Version>` | `dibix artifact unlist <version>` | ❌ Pending — required to retire the script |

Only `unlist` (nuget.org + DockerHub) remains in PowerShell; closing it unblocks
deletion of `dibix-nuget.ps1`. The items below are deviations from the original
plan and gaps worth closing.

---

## Actual implementation (file map)

Note: paths in the original plan (`Tools/...`) were superseded — the actual layout is
`Commands/...` for command classes and `Utilities/...` for helpers.

| File | Role |
|---|---|
| `src/Dibix.Sdk.Cli/Program.cs` | Wires `ArtifactCommand` into `RootCommand` |
| `src/Dibix.Sdk.Cli/Commands/Artifact/ArtifactCommand.cs` | `ArtifactCommand` parent — declares the shared `--consumer-directory` option (long-form only, env-var fallback) and registers `clear`, `reset`, `deploy`, `push` |
| `src/Dibix.Sdk.Cli/Commands/Artifact/ClearNuGetPackagesCommand.cs` | `clear` — deletes `~/.nuget/packages/dibix*` |
| `src/Dibix.Sdk.Cli/Commands/Artifact/ResetNuGetPackagesCommand.cs` | `reset [package-name]` — removes versions from cache **and** reverts the consumer's `.props` / `global.json` via LibGit2Sharp |
| `src/Dibix.Sdk.Cli/Commands/Artifact/DeployNuGetPackageCommand.cs` | `deploy [package-name] [-c Debug\|Release]` — full pack/expand/sync workflow |
| `src/Dibix.Sdk.Cli/Commands/Artifact/PushNuGetPackageCommand.cs` | `push [package-name]` — `dotnet nuget push` to internal ADO feed with API key + optional source override |
| `src/Dibix.Sdk.Cli/Commands/Artifact/ConsumerPackageCommand.cs` | Base class for subcommands that need a consumer; resolves `--consumer-directory` and constructs `ConsumerPackageManager` |
| `src/Dibix.Sdk.Cli/Commands/EnvironmentVariableOption.cs` | Option with env-var fallback (`CollectValue`) |
| `src/Dibix.Sdk.Cli/Commands/Root/SetConsumerDirectoryCommand.cs` | `dibix set consumer-directory <path>` — persists `DIBIX_CONSUMER_DIRECTORY` |
| `src/Dibix.Sdk.Cli/Commands/Root/SetNuGetPackageFeedApiKeyCommand.cs` | `dibix set nuget-api-key <key>` — persists `DIBIX_NUGET_API_KEY` |
| `src/Dibix.Sdk.Cli/Commands/Root/SetNuGetPackageFeedSourceCommand.cs` | `dibix set nuget-feed-source <url>` — persists `DIBIX_NUGET_FEED_SOURCE` |
| `src/Dibix.Sdk.Cli/Utilities/ConsumerPackageManager.cs` | GET via `dotnet msbuild -getItem:PackageVersion`; SET via `XDocument` walk through `<Import>` chain; `global.json` SDK side-write; `RevertPackageVersionChanges` for `reset` |
| `src/Dibix.Sdk.Cli/Utilities/PackageUtility.cs` | Package list, `GetLocalDibixVersion` (nbgv), `RestoreNuGetPackages` (dotnet restore on `Dibix.sln`), `CreateNuGetPackage` (dotnet pack), `RemovePackageFromNuGetPackageCache`, `DeployPackageToNuGetPackageCache`, `IsSdk` |
| `src/Dibix.Sdk.Cli/Utilities/KnownDirectory.cs` | `PackageCacheDirectory`, `DibixRootDirectory` |
| `src/Dibix.Sdk.Cli/Utilities/NuGetPackageExpander.cs` | In-process equivalent of `nuget add … -expand` (no nuget.exe dependency) |
| `src/Dibix.Sdk.Cli/Utilities/ProcessUtility.cs` | `Execute`, `Capture`, throws `ProcessExecutionException` on non-zero exit |
| `src/Dibix.Sdk.Cli/Utilities/EnvironmentVariableName.cs` | Constants: `ConsumerDirectory`, `NuGetApiKey`, `NuGetFeedSource` (+ pending: `NuGetPublicApiKey`, `DockerHubUsername`, `DockerHubPat`) |

---

## Deviations from the original plan

These are intentional changes that ended up looking different from the plan; flagged so
future readers don't get confused when comparing the doc to the code.

1. **`KnownDirectory.DibixRootDirectory` replaced `DibixSourceDirectoryResolver`.**
   The original plan called for env-var (`DIBIX_SOURCE_DIR`) + upward walk for `Dibix.sln`.
   Actual implementation is a single line: `Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."))`.
   This works for in-repo dev builds (binary at `src/Dibix.Sdk.Cli/bin/{Configuration}/{tfm}/`)
   but will resolve to garbage if the CLI is ever installed/published outside the source tree.
   See "Open gaps" below.

2. **`NuGetPackageExpander` replaced the `nuget add … -expand` shell call.**
   Removes the `nuget.exe` prerequisite. Mirrors the v3 global-cache layout (lowercase id +
   version dir, `.nupkg` copy, `.sha512` marker, `.nupkg.metadata`).

3. **`reset` reverts consumer files via libgit2.**
   The PS script only deleted from cache. The CLI also does
   `Repository.RevertFile(propsFile)` (and `global.json` if SDK), so a `deploy`+`reset`
   round-trip leaves the consumer clean. Adds a `LibGit2Sharp` runtime dependency.

4. **SET writes both indirect and literal `<PackageVersion>` forms.**
   - Indirect: `Version="$(DibixSdkVersion)"` → walks `<Import>` chain, finds defining
     `.props`, updates `<DibixSdkVersion>`.
   - Literal: `Version="1.7.53"` → updates the attribute on `Directory.Packages.props` in
     place. (The plan only described the indirect form.)

5. **XML write strategy is `XDocument` + `XmlWriter`, not `XmlDocument.Save`.**
   `LoadPreservingWhitespace` + `SavePreservingXmlDeclaration` preserves the XML
   declaration / encoding while keeping the API async-friendly.

6. **`ConsumerPackageCommand` base class.**
   Centralises the `--consumer-directory` collection + `ConsumerPackageManager`
   construction so each subcommand doesn't repeat the wiring.

7. **`EnvironmentVariableOption` is long-form only.** All three env-var-backed options
   (`--consumer-directory`, `--http-host-directory`, `--worker-host-directory`) drop their
   short aliases. Reasoning: `-h` would collide with `--help`, and dropping it only for
   that one option would create asymmetry between the two host-directory siblings.
   Treating env-var options as a class with no short aliases keeps the help table
   uniform; the env-var fallback covers the day-to-day case anyway. `--configuration`
   on `deploy` keeps its `-c` short alias since it has no sibling and no collision.

8. **Top-level command is `artifact`, not `nuget`** — class and file are both
   `ArtifactCommand`.

---

## Open gaps (PS feature → CLI status)

| PS feature | CLI status | Notes |
|---|---|---|
| `Restore-Packages` (runs `dotnet restore` before `dotnet pack`) | ✅ **Ported** | `PackageUtility.RestoreNuGetPackages()` runs once at the top of `deploy` against `Dibix.sln`; per-package `dotnet pack` keeps `--no-restore`. |
| Consumer-directory CWD fallback (`Test-Path Directory.Packages.props`) | **Not ported** | Adopted approach is `--consumer-directory` option + `DIBIX_CONSUMER_DIRECTORY` env var only. |
| Hardcoded `../../Helpline` fallback | **Not ported (intentional)** | Replaced by the env-var mechanism. |
| `push` mode | ✅ **Done** | `PushNuGetPackageCommand` — `dotnet nuget push` to internal ADO feed; credentials via `DIBIX_NUGET_API_KEY` / `--api-key`; feed source overridable via `DIBIX_NUGET_FEED_SOURCE` / `--source`. |
| `unlist` mode | **Pending** | Removes a version from nuget.org + DockerHub. See "Unlist Workflow" below. |

### Smaller gaps worth considering

- **`KnownDirectory.DibixRootDirectory` is fragile.** The relative-path traversal only
  works for the dev build layout. If we ever ship the CLI as an installed tool, this
  breaks silently. Recommend reinstating the plan's two-step resolver: (1) `DIBIX_SOURCE_DIR`
  env var, (2) walk up looking for `Dibix.sln`. Throw with a clear error if neither
  resolves.

- **`DeployNuGetPackageCommand` always calls `SetPackageVersionGlobalJson` for SDK
  packages**, even when the version already matches. Harmless (idempotent re-write) but
  inconsistent with `SetPackageVersionMSBuild`, which is gated on a version diff. Tighten
  to only write when the value differs, or move both calls under the `if (localDibixVersion
  != consumerPackageVersion)` block.

- **No `dibix artifact get-version` / `set-version` standalone subcommands.** The plan
  proposed these as scriptable verbs that expose the manager's GET/SET. Not strictly
  required for parity with the PS script, but useful for debugging or for non-deploy
  scripted workflows. **Decision needed:** ship without them, or add now.

- **No tests.** `tests/Dibix.Sdk.Cli.Tests/` doesn't exist; nothing covers
  `ConsumerPackageManager` (XML walk, glob expansion, JSON write), `NuGetPackageExpander`
  (cache layout), or the deploy/reset workflows. Worth at least unit tests for the
  XML walk against a fixture consumer tree, since that is the most failure-prone code.

---

## What's left to implement

The end state is a deleted `dibix-nuget.ps1`. To get there, in suggested order:

1. **Credentials infrastructure for `unlist`.** The three remaining secrets need env-var
   constants and `dibix set` subcommands (same pattern used for `push`). See "Credentials" below.
   - `DIBIX_NUGET_PUBLIC_API_KEY` — nuget.org (unlist)
   - `DIBIX_DOCKERHUB_USERNAME`, `DIBIX_DOCKERHUB_PAT` — DockerHub (unlist)

2. **`dibix artifact unlist <version>`.** Wraps `dotnet nuget delete` against nuget.org
   + a small DockerHub HTTP client (JWT login + `DELETE /v2/repositories/.../tags/{ver}/`).
   The PS script also runs `docker rmi` — decide whether to keep that local cleanup. See
   "Unlist Workflow" below.

3. **Delete `D:\Serviceware\Common\Scripts\dibix-nuget.ps1`** once steps 1–2 are merged
   and verified, plus any references in developer docs / wiki / muscle-memory aliases.
   This is the goal the rest of the plan exists to enable.

4. **Harden Dibix-root resolution.** Replace `KnownDirectory.DibixRootDirectory` with the
   originally-planned `DIBIX_SOURCE_DIR` env-var + `Dibix.sln`-walk fallback. Throw a
   clear error if both fail. Low effort, prevents a footgun for any future installed-CLI
   scenario.

5. **Tighten SDK version sync in `deploy`.** Move the
   `SetPackageVersionGlobalJson` call inside the `localDibixVersion !=
   consumerPackageVersion` branch (or guard it with its own diff check) so we don't
   needlessly rewrite `global.json` on every deploy.

6. **(Optional) Add `dibix artifact get-version <package>` and
   `dibix artifact set-version <package> <version>`** — thin wrappers over
   `ConsumerPackageManager.GetPackageVersion` / `SetPackageVersionMSBuild` (+
   `SetPackageVersionGlobalJson` when `IsSdk`). Useful for one-off scripted workflows.
   Skip if there's no concrete need.

7. **(Optional) Tests for `ConsumerPackageManager`.** Fixture-based: a `Directory.Packages.props`
   with both literal and `$(...)` forms, an `<Import>` chain with at least one glob, and
   a `global.json`. Exercise GET, SET, and the revert path. Same for
   `NuGetPackageExpander.Expand` against a real `.nupkg` to lock in the v3 cache layout.

---

## Credentials

The PS script hard-codes four secrets at lines 126–130 of `dibix-nuget.ps1`:

```
$NugetApiKeyPublic   = ''   # nuget.org (unlist)
$NugetApiKeyPrivate  = ''   # internal ADO feed (push)
$DockerHubUsername   = ''
$DockerHubPAT        = ''
```

These cannot move into the CLI as-is (committed source). Adopt the existing
`EnvironmentVariableOption` + `dibix set <name>` pattern that already covers
`DIBIX_CONSUMER_DIRECTORY`:

| Env var | Used by | Persisted via | Status |
|---|---|---|---|
| `DIBIX_NUGET_API_KEY` | `push` | `dibix set nuget-api-key <key>` | ✅ Done |
| `DIBIX_NUGET_FEED_SOURCE` | `push` | `dibix set nuget-feed-source <url>` | ✅ Done (optional override) |
| `DIBIX_NUGET_PUBLIC_API_KEY` | `unlist` | `dibix set nuget-public-api-key <key>` | ❌ Pending |
| `DIBIX_DOCKERHUB_USERNAME` | `unlist` | `dibix set dockerhub-username <user>` | ❌ Pending |
| `DIBIX_DOCKERHUB_PAT` | `unlist` | `dibix set dockerhub-pat <pat>` | ❌ Pending |

Implementation: extend `EnvironmentVariableName` with three more constants
(`NuGetPublicApiKey`, `DockerHubUsername`, `DockerHubPat`) and add three
`SetEnvironmentVariableCommand` subclasses under `Commands/Root/`, mirroring the
existing `SetNuGetPackageFeedApiKeyCommand`. Each command is small (~10 lines).

**Side action — rotate the leaked keys.** All four values are in the script, which is
checked into `D:\Serviceware\Common\Scripts`. Whoever owns those credentials should rotate
them after the CLI is wired up; the CLI port is the natural moment to do it.

---

## Push Workflow (as implemented)

`PushNuGetPackageCommand : ValidatableActionCommand` (no `ConsumerPackageCommand` base —
push doesn't touch a consumer):

1. Resolves API key from `--api-key` option (with `DIBIX_NUGET_API_KEY` env-var fallback
   via `EnvironmentVariableOption`). Fails fast if missing.
2. Argument: `package-name` (optional; omit = all packages).
3. `string version = await PackageUtility.GetLocalDibixVersion()`.
4. For each package: locates `src/{projectName}/bin/Release/{packageName}.{version}.nupkg`.
   If the file doesn't exist, throws — caller is expected to have run `deploy` (or `dotnet pack
   -c Release`) first.
5. `ProcessUtility.Execute("dotnet", $"nuget push \"{nupkgPath}\" --source {feedUrl} --api-key {key}")`.

Feed URL is provided via `--source` / `DIBIX_NUGET_PACKAGE_FEED_SOURCE` env var
(persisted via `dibix set nuget-feed-source <url>`). The option is **required** —
no default constant, no consumer-specific knowledge in the binary.

Wire-up in `ArtifactCommand`:
```csharp
Add(new PushNuGetPackageCommand());
```

---

## Unlist Workflow (planned)

`UnlistArtifactsCommand : ValidatableActionCommand`. Argument: `version` (required).
Iterates the same `PackageUtility.NuGetPackageNames` array plus a new
`PackageUtility.ApplicationNames = ["dibix-http-host", "dibix-worker-host"]`.

**Per package:**
1. `dotnet nuget delete --source https://nuget.org --api-key {DIBIX_NUGET_PUBLIC_API_KEY} --non-interactive {packageName} {version}` via `ProcessUtility.Execute`.
2. `PackageUtility.RemovePackageFromNuGetPackageCache(packageName, version)` (already exists).

**Per application (DockerHub):**
1. New helper `DockerHubClient` (`Utilities/DockerHubClient.cs`):
   - `Task<string> AuthenticateAsync(string username, string pat)` — `POST https://hub.docker.com/v2/users/login/` with JSON body, returns the JWT.
   - `Task DeleteTagAsync(string organization, string repository, string version, string token)` — `DELETE https://hub.docker.com/v2/repositories/{org}/{repo}/tags/{version}/` with `Authorization: JWT {token}`.
   - Single shared `HttpClient`; throw on non-2xx.
2. `ProcessUtility.Execute("docker", $"rmi {servicewareit}/{appName}:{version} --force")` — **decide whether to keep this**. Argument for keeping: cleans local Docker cache so a re-pull picks up the next build. Argument for dropping: most devs don't have those images locally; failure (image-not-found) needs to be tolerated. Recommendation: keep, but make `docker rmi` non-fatal (catch `ProcessExecutionException`, log, continue).

Constants to add to `PackageUtility` (or a new `DockerConstants`):
- `DockerOrganizationName = "servicewareit"`
- `ApplicationNames = ["dibix-http-host", "dibix-worker-host"]`

Wire into `ArtifactCommand`:
```csharp
Add(new UnlistArtifactsCommand());
```

**Confirmation prompt.** Unlist is destructive on remote services. Add an interactive
`Are you sure you want to unlist version {version}? [y/N]` prompt unless `--yes` /
`--force` is passed. Match this to the user instructions in `AGENTS.md` (irreversible
remote operations should not run unattended). Skip the prompt only when stdin is
redirected and `--yes` is set.

---

## Architecture / How Consumer Version Discovery Works

### GET — Use MSBuild evaluation directly

`dotnet msbuild <Directory.Packages.props> -getItem:PackageVersion -nologo` evaluates the entire
import chain and returns a JSON structure like:

```json
{
  "Items": {
    "PackageVersion": [
      { "Identity": "Dibix.Sdk", "Version": "1.7.53", "DefiningProjectFullPath": "...", ... },
      ...
    ]
  }
}
```

The `Version` field is **fully evaluated** — MSBuild resolves all property references
(e.g. `$(DibixSdkVersion)`) automatically. We parse the JSON and filter to entries whose
`Identity` starts with `Dibix` (JSONPath `$.Items.PackageVersion[?(@.Identity =~ /^Dibix/)]`).

> **Why not `-getProperty:DibixSdkVersion`?**
> We don't know the property name upfront, and it varies per package. `-getItem:PackageVersion`
> gives us all packages in one call.

> **Note on `DefiningProjectFullPath`:** This metadata points to the file where the
> `<PackageVersion>` _item_ is declared (always `Directory.Packages.props`), **not** the file
> where the version _property_ is defined. It is therefore NOT useful for SET.

### SET — Two strategies, picked by inspecting the `Version` attribute

Implemented in `ConsumerPackageManager.SetPackageVersionMSBuild`. After loading
`Directory.Packages.props` and locating
`/Project/ItemGroup/PackageVersion[@Include='{packageName}']/@Version`:

**Literal form** — `Version="1.7.53"`: update the attribute on the loaded document and save it.

**Indirect form** — `Version="$(PropName)"` (matched by `^\$\((?<PropertyName>[^)]+)\)$`):
walk the `<Import>` chain starting at `Directory.Packages.props`, recursing through each
`<Import Project="...">`. Only `$(MSBuildThisFileDirectory)` is substituted (the directory
of the file currently being parsed). Glob patterns in the last path segment (`*` or `?`)
are expanded with `Directory.EnumerateFiles`. A `HashSet<string>` of full paths guards
against import cycles. The first file containing
`/Project/PropertyGroup/{propertyName}` wins; its element value is updated and the document
saved.

```
Directory.Packages.props
  imports → build/MSBuild/dependencies.props
    imports (glob) → build/MSBuild/dependencies/dibix-sdk.props
      <PropertyGroup>
        <DibixSdkVersion>1.7.53</DibixSdkVersion>    ← SET writes here
      </PropertyGroup>
```

`LoadPreservingWhitespace` (XmlReader → `XDocument.LoadAsync` with `LoadOptions.PreserveWhitespace`)
plus `SavePreservingXmlDeclaration` (XmlWriter, `OmitXmlDeclaration` mirroring the source)
keep the file's declaration and formatting intact across the round-trip.

### `global.json` side-write

Implemented in `ConsumerPackageManager.SetPackageVersionGlobalJson`:
- `JObject.LoadAsync` from `{consumerRoot}/global.json`
- Walk to `msbuild-sdks.{packageName}` and overwrite
- `JsonTextWriter` with `Formatting.Indented`

Throws if either `msbuild-sdks` or the package entry is missing — consistent with the PS
script which assumed those keys exist.

This handles the `Dibix.Sdk` dual-use pattern (both `PackageReference` and MSBuild SDK)
generically, but the *call site* (`DeployNuGetPackageCommand`) gates on
`PackageUtility.IsSdk(packageName)`, which is a hard `== "Dibix.Sdk"` check — so other
hypothetical SDK packages would not be detected. Fine for now since `Dibix.Sdk` is the
only SDK.

### Step-by-step example for `Dibix.Sdk` in Helpline

```
GET: dotnet msbuild Directory.Packages.props -getItem:PackageVersion
     → JSON → Items.PackageVersion[?Identity=="Dibix.Sdk"].Version → "1.7.53"

SET: Parse Directory.Packages.props
     → <PackageVersion Include="Dibix.Sdk" Version="$(DibixSdkVersion)" />
     → property name = "DibixSdkVersion"
     Walk imports:
       Directory.Packages.props
         → build/MSBuild/dependencies.props
           → build/MSBuild/dependencies/dibix-sdk.props   ← found <DibixSdkVersion>
               Update Value "1.7.53" → "1.7.54"
               Save preserving XML declaration
     Update global.json: msbuild-sdks.Dibix.Sdk → "1.7.54"
```

---

## Consumer Directory Resolution

Resolved via `EnvironmentVariableOption.CollectValue` on `ConsumerPackageCommand`:

1. `--consumer-directory <path>` (alias `-c`) on the parent `ArtifactCommand`, inherited by subcommands
2. `DIBIX_CONSUMER_DIRECTORY` **user-scoped** env var on Windows (`EnvironmentVariableTarget.User`),
   process-scoped fallback on Linux

Persist the env var via:
```
dibix set consumer-directory <path>
```

If neither is provided, `Validate` throws `CommandLineValidationException` and the command
exits with code 1.

> **CWD fallback from the PS script is NOT implemented** (PS used CWD if it contained a
> `Directory.Packages.props`). The two-step option+env-var pattern is the adopted
> approach; revisit only if developers complain.

---

## Dibix Source Directory Resolution

**Currently:** `KnownDirectory.DibixRootDirectory` =
`Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."))`.

This assumes the binary is at `src/Dibix.Sdk.Cli/bin/{Configuration}/{tfm}/` and walks up
five directories. Works for `dotnet run` and `dotnet build` against the source tree;
breaks for installed/published builds.

**Recommended (gap #1 above):**
1. `DIBIX_SOURCE_DIR` environment variable
2. Walk upward from `AppContext.BaseDirectory` looking for `Dibix.sln`
3. Throw with instructions to set `DIBIX_SOURCE_DIR` if neither resolves

---

## Deploy Workflow (as implemented)

`DeployNuGetPackageCommand.Execute`:

1. **Get local Dibix version** — `PackageUtility.GetLocalDibixVersion()` →
   `nbgv get-version --project {DibixRootDirectory} --variable NuGetPackageVersion`
2. **Restore (once, solution-wide)** — `PackageUtility.RestoreNuGetPackages()` →
   `dotnet restore Dibix.sln --verbosity quiet --nologo`. Runs once before the per-package
   loop so each `dotnet pack` can keep `--no-restore`.

Then for each package:

3. **Pack** — `PackageUtility.CreateNuGetPackage(packageName, version, configuration)` →
   `dotnet pack {projectPath} --verbosity quiet --nologo --no-restore -p:PackageVersionOverride={version} -p:Configuration={configuration}`.
   `Dibix.Sdk` maps to `src/Dibix.Sdk.Cli/`; all others map to `src/{PackageName}/`.
4. **Bust cache** — delete `~/.nuget/packages/{packageName}/{version}/` if present
5. **Expand to cache** — `NuGetPackageExpander.Expand(...)` (in-process, no `nuget.exe`)
6. **Get consumer version** — `ConsumerPackageManager.GetPackageVersion(packageName)`
7. **Sync consumer if different** —
   `ConsumerPackageManager.SetPackageVersionMSBuild(packageName, version, ct)`
8. **Sync `global.json` if SDK** —
   `ConsumerPackageManager.SetPackageVersionGlobalJson(packageName, version)`
   (currently unconditional — see open gap #2)

---

## Reset Workflow (per package, as implemented)

`ResetNuGetPackagesCommand.Execute`:

1. **Get consumer version** — `ConsumerPackageManager.GetPackageVersion(packageName)`
2. **Bust cache** — `PackageUtility.RemovePackageFromNuGetPackageCache(packageName, version)`
3. **Revert consumer changes via libgit2** —
   `ConsumerPackageManager.RevertPackageVersionChanges(packageName, isSdk, ct)`:
   resolves the same target file the deploy SET would have written to (via the same
   `<Import>` walk) and runs `Repository.RevertFile` on it; if `isSdk`, also reverts
   `global.json`.

This is **richer than the PS script's `reset`**, which only deleted from the cache. Means
a `deploy` + `reset` round-trip leaves the consumer in the state it was in before the deploy.

---

## Process Execution

`ProcessUtility` (`src/Dibix.Sdk.Cli/Utilities/ProcessUtility.cs`):

- `Execute(string fileName, string arguments, string? workingDirectory)` — fire-and-forget;
  throws `ProcessExecutionException` on non-zero exit code
- `Capture(string fileName, string arguments, string? workingDirectory) → Task<string>` —
  captures stdout; async; throws `ProcessExecutionException` on non-zero exit code

`ProcessExecutionException` carries `fileName`, `arguments`, `exitCode`, `stdout`, `stderr`.

`ValidatableActionCommand.ExecuteCore` catches `ProcessExecutionException` and prints to stderr,
returning exit code 1.

---

## Decisions

| # | Topic | Decision |
|---|---|---|
| 1 | `dotnet restore` before `dotnet pack` | **Ported** — `PackageUtility.RestoreNuGetPackages()` runs once on `Dibix.sln` at the start of `deploy`; per-package pack keeps `--no-restore` so the restore isn't repeated. |
| 2 | `Dibix.Sdk` in `global.json` | Handled via `msbuild-sdks` scan; call site gates on `IsSdk` (currently `== "Dibix.Sdk"`). |
| 3 | `nuget.exe` availability | **Not required** — `NuGetPackageExpander` does the expand in-process. |
| 4 | `nbgv` availability | Documented as prerequisite (`dotnet tool install -g nbgv`). |
| 5 | Cross-platform | `DIBIX_CONSUMER_DIRECTORY` uses user-scope on Windows, process-scope on Linux. |
| 6 | GET uses `dotnet msbuild` | Requires .NET SDK in PATH; `-getItem` available since .NET 7 SDK. |
| 7 | MSBuild JSON output | Parsed with `Newtonsoft.Json` (existing dep), not `System.Text.Json`. |
| 8 | CWD fallback for consumer dir | **Not implemented** — option + env-var only. |
| 9 | Top-level command name | `dibix artifact` (file `NuGetCommand.cs` should be renamed). |
| 10 | Dibix-root resolution | Currently a relative-path constant. **Should** become env-var + sln-walk (gap #1). |
| 11 | `reset` reverts consumer files | **Yes**, via LibGit2Sharp — adds a runtime dependency, but eliminates manual cleanup. |
| 12 | XML write strategy | `XDocument` + `XmlWriter`, with `LoadOptions.PreserveWhitespace` and matching `OmitXmlDeclaration` on save. |
| 13 | `push` / `unlist` modes | **`push` done** (`PushNuGetPackageCommand`). **`unlist` still pending** — required to delete `dibix-nuget.ps1`. See "What's left" + workflow sections. |
| 14 | Credentials in source | **Forbidden.** Use env vars + `dibix set` persistence. Rotate the four keys currently embedded in the PS script when migrating. |
| 15 | DockerHub HTTP client | **Custom, in-process.** No new SDK dependency — small REST surface (login + DELETE tag). |
| 16 | `docker rmi` after unlist | **Keep, but tolerate failure.** Image may not exist locally on every dev's machine. |
| 17 | Unlist confirmation prompt | **Required** — destructive remote action. Bypass via `--yes`. |
| 18 | End state | `dibix-nuget.ps1` deleted; remove references from any docs/aliases. |