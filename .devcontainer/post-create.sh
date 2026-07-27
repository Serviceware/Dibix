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
mkdir -p "$HOME/.config/opencode"
cat > "$HOME/.config/opencode/opencode.json" <<'JSON'
{
  "$schema": "https://opencode.ai/config.json",
  "permission": {
    "edit": "allow",
    "bash": "allow",
    "webfetch": "allow"
  }
}
JSON
echo "  ✓ OpenCode allows edit/bash/webfetch without asking"

echo ""
echo "=== Setup Complete ==="
echo ""
echo "Usage (autonomous by default — the container is the sandbox):"
echo "  claude              # Claude TUI (starts in bypassPermissions mode)"
echo "  claude -p \"<x>\"     # Claude one-shot, non-interactive"
echo "  opencode            # OpenCode TUI (permissions pre-allowed)"
echo "  opencode run \"<x>\"  # OpenCode one-shot, non-interactive"
echo ""
echo "Build commands:"
echo "  dotnet build Dibix.slnx                                            # Build the solution"
echo "  dotnet test  Dibix.slnx                                            # Run all tests"
echo "  dotnet test  tests/Dibix.Sdk.Tests/Dibix.Sdk.Tests.csproj          # Single test project"
echo ""
echo "  # Testcontainers-backed suites (SQL Server via the host Docker daemon / DooD):"
echo "  dotnet test  tests/Dibix.Dapper.Tests/Dibix.Dapper.Tests.csproj"
echo "  dotnet test  tests/Dibix.Http.Host.Tests/Dibix.Http.Host.Tests.csproj"