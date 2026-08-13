#!/bin/bash
# Run Claude Code with Superpowers enabled for this session only.

set -euo pipefail

PLUGIN="superpowers@claude-plugins-official"

claude plugin enable "$PLUGIN" --scope user
disable_plugin() {
  claude plugin disable "$PLUGIN" --scope user >/dev/null 2>&1 || true
}
trap disable_plugin EXIT INT TERM

claude "$@"