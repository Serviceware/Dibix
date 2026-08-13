#!/bin/bash
# .devcontainer/post-create.sh
# Runs once after container creation. Configures both agents to run autonomously
# BY DEFAULT — no launch flags needed. The isolated container is the security
# boundary, so plain `claude` / `opencode` behave as if invoked with
# --dangerously-skip-permissions / --auto:
#   - Claude: settings.json sets defaultMode=bypassPermissions and suppresses the
#     one-time confirmation; .claude.json pre-accepts onboarding + workspace trust.
#   - OpenCode: opencode.json allows edit/bash/webfetch without asking.

set -euo pipefail

echo "=== Dibix — Agent Workspace Post-Create Setup ==="

# ─── 1. Claude: onboarding + workspace trust, and bypass mode by default ───
# .claude.json clears the first-launch dialogs ("Do you trust the files in this
# folder?", onboarding). settings.json (user scope) makes bypassPermissions the
# default mode and skips its confirmation prompt, so plain `claude` is autonomous.
if [ ! -f "$HOME/.claude.json" ] && [ ! -f "$HOME/.claude/.claude.json" ]; then
  cat > "$HOME/.claude.json" <<'JSON'
{
  "hasCompletedOnboarding": true,
  "bypassPermissionsModeAccepted": true,
  "projects": {
    "/workspace": {
      "hasTrustDialogAccepted": true,
      "hasCompletedProjectOnboarding": true
    }
  }
}
JSON
  echo "  ✓ Created .claude.json (onboarding + workspace trust)"
fi

mkdir -p "$HOME/.claude"
cat > "$HOME/.claude/settings.json" <<'JSON'
{
  "$schema": "https://json.schemastore.org/claude-code-settings.json",
  "permissions": {
    "defaultMode": "bypassPermissions"
  },
  "skipDangerousModePermissionPrompt": true
}
JSON
echo "  ✓ Claude defaults to bypassPermissions mode"

# ─── 2. OpenCode: allow all permissions by default (equivalent to --auto) ───
# ~/.config/opencode is ephemeral (only AGENTS.md is bind-mounted, as a separate
# file), so write the config fresh each create.
# opencode-openai-residency: overrides the outgoing User-Agent/originator headers to match
# OpenAI's codex_cli_rs client-identity gate. See https://github.com/DusKing1/opencode-openai-residency.
mkdir -p "$HOME/.config/opencode"
cat > "$HOME/.config/opencode/opencode.json" <<'JSON'
{
  "$schema": "https://opencode.ai/config.json",
  "permission": {
    "edit": "allow",
    "bash": "allow",
    "webfetch": "allow"
  },
  "plugin": ["opencode-openai-residency"],
  "provider": {
    "openai": {
      "options": {
        "ua_override": true
      }
    }
  }
}
JSON
echo "  ✓ OpenCode allows edit/bash/webfetch without asking"
echo "  ✓ OpenCode OpenAI residency plugin (ua_override) configured"

# ─── 2a. Enable Testcontainers container reuse ───
# Not just a speed-up: without it, Testcontainers registers containers with Ryuk
# and the reaper is unreachable in this docker-outside-of-docker setup —
# "Can not connect to Ryuk at host.docker.internal:<port>: Connection refused" —
# which fails every Testcontainers test before it starts. Reusable containers skip
# that registration. (Dibix.Dapper.Tests, Dibix.Http.Host.Tests.)
TESTCONTAINERS_PROPS="$HOME/.testcontainers.properties"
if ! grep -qs '^testcontainers.reuse.enable=true' "$TESTCONTAINERS_PROPS"; then
  printf 'testcontainers.reuse.enable=true\n' >> "$TESTCONTAINERS_PROPS"
  echo "  ✓ Enabled Testcontainers container reuse"
else
  echo "  ✓ Testcontainers container reuse already enabled"
fi

# ─── 2b. Install Superpowers for Claude Code but leave it disabled ───
# Its skills omit disable-model-invocation: true and its SessionStart hook injects a
# bootstrap that enforces planning and review, so normal autonomous sessions must not
# load it. Use .devcontainer/claude-superpowers.sh to enable it for one session.
echo "  → Installing disabled Superpowers plugin for Claude Code..."
claude plugin marketplace add anthropics/claude-plugins-official \
  2>/dev/null || echo "  ⚠ Plugin marketplace add failed (may already be registered)"

claude plugin install superpowers@claude-plugins-official \
  2>/dev/null || echo "  ⚠ Plugin install failed (may already be installed)"

claude plugin disable superpowers@claude-plugins-official --scope user \
  2>/dev/null || echo "  ⚠ Could not leave Claude Superpowers disabled by default"

# ─── 2c. Register Azure DevOps MCP server for both agents ───
# Used here only for pipelines (build/release.yml, project serviceware/Dibix). Auth is
# az-login only (azcli mode) — no PAT support, so nothing is baked into the registration.
claude mcp remove azure-devops --scope user 2>/dev/null || true
claude mcp add azure-devops \
  --transport stdio \
  --scope user \
  -- npx -y "@azure-devops/mcp@2.9.0" serviceware -a azcli -d core build \
  && echo "  ✓ Claude: azure-devops registered (run 'az login' — see reminder below)" \
  || echo "  ⚠ Claude: azure-devops registration failed"

if opencode mcp add azure-devops \
  -- npx -y "@azure-devops/mcp@2.9.0" serviceware -a azcli -d core build </dev/null; then
  echo "  ✓ OpenCode: azure-devops registered (run 'az login' — see reminder below)"
else
  echo "  ⚠ OpenCode: azure-devops registration failed"
fi

# ─── 3. First-time provisioning notice: gh / claude / opencode login ───
# postCreateCommand's stdio isn't a keyboard-connected terminal (it's a log/
# progress stream in VS Code and most other devcontainer front-ends), so we
# can't block here waiting for `read` input — it would hang forever with no
# way to answer. Instead, just report what's still missing; the commands
# themselves need to be run afterwards from a real interactive shell anyway
# (device-code / OAuth flows need a terminal you can type into).
echo ""
echo "=== First-time provisioning ==="

if gh auth status >/dev/null 2>&1; then
  echo "  ✓ gh already authenticated"
else
  echo "  ! gh is not authenticated — run 'gh auth login' in a terminal"
fi

if claude auth status >/dev/null 2>&1; then
  echo "  ✓ claude already authenticated"
else
  echo "  ! claude is not authenticated — run 'claude auth login' in a terminal"
fi

# OpenCode supports many providers (Models.dev), so we don't push a specific
# one — just flag that at least one needs configuring.
OPENCODE_AUTH_FILE="$HOME/.local/share/opencode/auth.json"
if [ -s "$OPENCODE_AUTH_FILE" ] && [ "$(cat "$OPENCODE_AUTH_FILE")" != "{}" ]; then
  echo "  ✓ opencode already has a provider configured"
else
  echo "  ! opencode has no provider configured — run 'opencode auth login' in a terminal"
fi

# ─── 4. Remind about the one-time az login the azure-devops MCP server needs ───
# The azure-devops MCP server registered in section 2c is az-login only (azcli mode, no
# PAT support). Plain `az login` works fine in this container despite the missing browser
# — it detects that and falls back to a device code on its own, and that flow never binds
# a local listener, so there is no loopback/port-forwarding problem either, just a code to
# enter on any device with a browser.
# --tenant/--skip-subscription-discovery skip the "No subscriptions found" tenant/
# subscription picker: the Serviceware SE tenant has no ARM subscriptions at all, so plain
# `az login` stops at an interactive picker that can't be answered from this reminder.
# Older az CLI: use --allow-no-subscriptions instead and press Enter at the single-entry picker.
echo ""
echo "  → azure-devops MCP server (pipelines for serviceware/Dibix) needs a one-time login:"
echo "      az login --tenant 8b6310db-d77e-45e7-a076-56d5aea129bd --skip-subscription-discovery"
echo "    Start a new claude/opencode session afterwards so the MCP server picks it up."

echo ""
echo "=== Setup Complete ==="
echo ""
echo "Usage (autonomous by default — the container is the sandbox):"
echo "  claude              # Claude TUI (starts in bypassPermissions mode)"
echo "  claude -p \"<x>\"     # Claude one-shot, non-interactive"
echo "  opencode            # OpenCode TUI (permissions pre-allowed)"
echo "  opencode run \"<x>\"  # OpenCode one-shot, non-interactive"
echo ""
echo "Superpowers (plan-driven workflow, enabled per-session so it can't hijack normal sessions):"
echo "  .devcontainer/claude-superpowers.sh    # Claude with Superpowers"
echo "  .devcontainer/opencode-superpowers.sh  # OpenCode with Superpowers"
echo ""
echo "Build commands:"
echo "  dotnet build Dibix.slnx                                            # Build the solution"
echo "  dotnet test  Dibix.slnx                                            # Run all tests"
echo "  dotnet test  tests/Dibix.Sdk.Tests/Dibix.Sdk.Tests.csproj          # Single test project"
echo ""
echo "  # Testcontainers-backed suites (SQL Server via the host Docker daemon / DooD):"
echo "  dotnet test  tests/Dibix.Dapper.Tests/Dibix.Dapper.Tests.csproj"
echo "  dotnet test  tests/Dibix.Http.Host.Tests/Dibix.Http.Host.Tests.csproj"