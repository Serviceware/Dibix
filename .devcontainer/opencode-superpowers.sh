#!/bin/bash
# Run OpenCode with Superpowers enabled for this session only.

set -euo pipefail

OPENCODE_CONFIG_CONTENT='{"plugin":["superpowers@git+https://github.com/obra/superpowers.git"]}' \
  opencode "$@"