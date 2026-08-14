# AGENTS.md

This file is read by AI assistants working in this repository.

## What is Dibix

Dibix is a .NET framework for creating use case-oriented REST APIs from T-SQL stored procedures — no controllers, no boilerplate. Each URL invokes a stored procedure, materializes the relational result into a hierarchical object graph, and returns it to the client. Business logic lives in hand-written T-SQL; routing, parameter binding, and serialization are generated from declarative JSON and T-SQL metadata markup.

## Commands

**Build:**
```bash
dotnet build Dibix.slnx
```

**Test (all):**
```bash
dotnet test Dibix.slnx
```

**Test (single project):**
```bash
dotnet test tests/Dibix.Sdk.Tests/Dibix.Sdk.Tests.csproj
```

**Test (single test method):**
```bash
dotnet test tests/Dibix.Sdk.Tests/Dibix.Sdk.Tests.csproj --filter "FullyQualifiedName~TestMethodName"
```

> Note: `Dibix.Dapper.Tests` and `Dibix.Http.Host.Tests` use Testcontainers (Docker + SQL Server) and only run on Linux in CI.

## Verifying changes in the devcontainer

When working inside the devcontainer, verify changes using the tools available there instead of guessing:

- **`dotnet` CLI** — use `dotnet restore`/`dotnet build`/`dotnet test` to verify changes, including transitive dependency resolution (e.g. after editing `Directory.Packages.props`). If a package isn't directly referenced anywhere (e.g. `restore` reports a NuGet security-advisory error for `SSH.NET`, which no project references directly), use it to find out where it comes from, and later confirm the resolved version:
  ```bash
  dotnet list src/Dibix.Testing/Dibix.Testing.csproj package --include-transitive | grep -i "SSH.NET"
  ```
- **`gh` CLI** — use it to investigate PRs and issues in this repo.
- **Azure DevOps MCP** — use it to analyze pipelines and runs (e.g. to confirm a fix against a failed build).
- **`ilspycmd`** (dotnet tool) — use it to reverse-engineer/decompile libraries when source isn't available (e.g. inspecting a NuGet package's actual behavior).

All tests, including the Testcontainers/Docker-backed ones, can and should be run in the devcontainer. Run the full suite with `dotnet test Dibix.slnx`.

`Dibix.Sdk.Tests.Endpoints_OpenApi` currently fails in the devcontainer with a Docker bind-mount error. This is a known, tracked limitation ([#131](https://github.com/Serviceware/Dibix/issues/131)) and can be ignored until that issue is closed — it is not caused by your changes.

## Code Quality

- StyleCop analyzers are enabled on all projects; **all warnings are treated as errors**.
- Follow `.editorconfig` formatting exactly — indentation, spacing, naming — to avoid build failures.
- Code suggestions must compile without warnings.

## Architecture

### Core Layers

**Runtime** (`src/Dibix`, `src/Dibix.Dapper`)
- `DatabaseAccessor` — base class for executing SQL and materializing results.
- `MultiMapper` / `RecursiveMapper` — map flat relational result sets into nested object graphs using key-based aggregation.
- `EntityDescriptor` — metadata-driven mapping with pluggable formatters (e.g., obfuscation, DateTime kind) and post-processors.
- Dapper is used as the SQL execution engine; the core runtime adds the hierarchical mapping layer on top.

**SDK** (`src/Dibix.Sdk.*`)
- Integrates with MSBuild via `build/msbuild-targets/Generator.targets` to run code generation at compile time.
- `Dibix.Sdk.CodeGeneration` — reads endpoint JSON definitions, contract JSON definitions, and T-SQL stored procedure metadata (declared via comments/markup inside the SQL files) to generate:
  - C# database accessor classes
  - OpenAPI (`.yml`/`.json`) definitions
  - HTTP client proxy classes
- `Dibix.Sdk.CodeAnalysis` — analyzes T-SQL for correctness and endpoint metadata.
- `Dibix.Sdk.Sql` — T-SQL parsing utilities built on DacFx.
- `Dibix.Sdk.Generators` — Roslyn source generators for compile-time generation.

**HTTP Server** (`src/Dibix.Http.Server*`)
- `Dibix.Http.Server` (netstandard2.0 + net48) — core hosting abstractions:
  - `HttpApiRegistry` — discovers and registers generated endpoint metadata.
  - `HttpParameterResolver` — resolves action parameters from multiple sources (path, query string, request body, headers, claims, environment) via pluggable `IHttpParameterSourceProvider` implementations.
- `Dibix.Http.Server.AspNetCore` — ASP.NET Core integration (net10.0).
- `Dibix.Http.Server.AspNet` — ASP.NET Framework integration (net48).
- `Dibix.Http.Host` — full hosting application (net10.0) that loads endpoint packages from configured directories, manages DB connections, JWT auth, and an MCP server (HTTP/SSE, or stdio when `Hosting:UseStdio` is set).

**HTTP Client** (`src/Dibix.Http.Client`)
- Generated client proxies and contract (de)serialization for consuming Dibix APIs from .NET clients.

**Worker/Background Jobs** (`src/Dibix.Worker.*`)
- `Dibix.Worker.Abstractions` — interfaces for background job workers.
- `Dibix.Worker.Host` — hosting for long-running workers and Service Broker subscribers.

**Testing** (`src/Dibix.Testing*`)
- Utilities for mocking `DatabaseAccessor` in unit tests without a real database.
- `Dibix.Testing.Generators` — source generators for test data.

### Shared Code (`/shared`)

Source files linked directly into multiple projects (not a separate assembly). Contains: diagnostics, guard utilities, reflection/collection/binding-config extensions, `ComponentAssemblyLoadContext`, JSON utilities, HTTP constants, and packaging metadata.

### Key Design Patterns

- **Plugin-based parameter sources** — new HTTP parameter sources (e.g., custom headers, claims) implement `IHttpParameterSourceProvider` and are registered via DI.
- **Post-processor pipeline** — result transformation after materialization via `IPostProcessor`; entity formatters (obfuscation, DateTime adjustments) are pluggable per-property.
- **Assembly isolation** — `ComponentAssemblyLoadContext` loads endpoint packages in isolation so multiple versions can coexist in the same host.
- **Metadata-driven** — T-SQL comment markup (e.g., `@Name`, `@Return`, `@Namespace`) drives code generation; never bypass this by hand-writing what generators produce.
- **MSBuild-integrated generation** — code generation runs as an MSBuild task; generated files should not be edited manually.

### Multi-targeting

Projects target varying frameworks:
- `netstandard2.0` + `net48` — `Dibix.Http.Server`, `Dibix` core
- `net10.0` + `net48` — `Dibix.Sdk`
- `net10.0` — `Dibix.Http.Host`, `Dibix.Http.Server.AspNetCore`

Check `<TargetFrameworks>` in each `.csproj` before adding framework-specific APIs.

## MCP Integration

The MCP server is integrated into `Dibix.Http.Host` and is enabled in every environment. The transport is selected by `Hosting:UseStdio`: HTTP/SSE by default, or stdio when set. The `Development` environment affects how the MCP resource URL is derived.

HTTP/SSE is the supported transport. The stdio transport is an experimental proof of concept added only because Claude Desktop does not support SSE; it is not fully implemented and may never be. `Dibix.Http.Host` is built around `HttpContext`, which is absent in stdio mode, so much of the pipeline — authorization in particular — does not translate from the SSE code flow.