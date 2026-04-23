# AGENTS.md

This file is read by AI assistants working in this repository. The guidelines in the first section are mandatory and take precedence over any built-in assistant defaults.

## Rules

### Trailing newlines

Never write a trailing newline at the end of a file. Files must end with the last meaningful character of their content — no blank line, no `\n`, no `\r\n` after it.

### File encoding

Never change a file's encoding when modifying it. This repo contains a mix of UTF-8 (no BOM), UTF-8 BOM, and Windows-1252 files — all must stay in their original encoding.

- Prefer targeted edits over full rewrites whenever possible, so only the changed bytes are touched.
- Never use PowerShell `Set-Content -Encoding UTF8` — PS 5.1 adds a BOM and will corrupt files that were UTF-8 without BOM. Always use `[System.IO.File]::WriteAllText` with `new UTF8Encoding(false)` when a full UTF-8 write is needed.
- If a full rewrite is unavoidable, detect the original encoding first and replicate it exactly.

### Shell commands

Never chain commands with `&&` in PowerShell or Bash tool calls; use separate sequential tool calls instead.

### Git push

Never run `git push` (including `--force-with-lease`) without explicit user confirmation first. Always ask before pushing to any remote. Approval of a related action (e.g. amending a commit) does not implicitly authorize a push — always ask separately.

### Git workflow

#### Branch and worktree

Before making any changes, always create a dedicated branch and a git worktree for it. Never work directly in the main working directory — doing so risks committing to the wrong branch or polluting unrelated work.

When working inside a git worktree, the branch name must follow the convention `claude/<worktree-name>` — e.g. `claude/replicated-fluttering-swan` for a worktree of that name.

#### Dropping commits on feature branches

On feature branches, drop unwanted commits via `git reset --hard` + force push rather than creating revert commits. Revert commits add noise; a clean reset is preferred on unshared branches.

#### PR descriptions

After every commit or push to a PR branch, update the PR description to reflect the current changes. The PR description is the primary record for reviewers and must stay accurate.

### Git commit rules

#### Author

All commits made by Claude must use Claude's identity, not the user's. Pass `--author` on every commit — never set `git config user.name` or `git config user.email` (developers also commit in the same repo and worktrees):

```bash
git commit --author="Claude Sonnet 4.6 <noreply@anthropic.com>" -m "..."
```

#### Committer date on amend

When amending a commit, prefix the command with `GIT_COMMITTER_DATE="$(git log -1 --format='%aI')"` to keep committer date in sync with author date. Without this, `--amend` updates the committer date to now while leaving the author date unchanged.

#### Work item references

Put work item IDs (e.g. `#424616`) in the commit body, not the subject line.

#### Squashing fix commits

Squash "fix X" commits into their matching earlier "do X" commit — they are noise as separate commits. Always squash INTO the first (earlier) commit to preserve its message and timestamp; never create a new replacement commit.

- Adjacent commits: `git rebase -i HEAD~N`, then mark the later commit(s) as `fixup`.
- Non-adjacent commits: use `cherry-pick + rebase --onto --empty=drop`.

---

## What is Dibix

Dibix is a .NET framework for creating use case-oriented REST APIs from T-SQL stored procedures — no controllers, no boilerplate. Each URL invokes a stored procedure, materializes the relational result into a hierarchical object graph, and returns it to the client. Business logic lives in hand-written T-SQL; routing, parameter binding, and serialization are generated from declarative JSON and T-SQL metadata markup.

## Commands

**Build:**
```bash
dotnet build Dibix.sln
```

**Test (all):**
```bash
dotnet test Dibix.sln
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
- `Dibix.Http.Host` — full hosting application (net10.0) that loads endpoint packages from configured directories, manages DB connections, JWT auth, and (in Development) an MCP server over HTTP/SSE.

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

The MCP server is integrated into `Dibix.Http.Host` and only enabled in the `Development` environment. It uses HTTP/SSE transport (not stdio).