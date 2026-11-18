#!/bin/bash
# .devcontainer/start-sshd.sh
# postStartCommand: starts the opt-in SSH server (sshd_config) when the host set DIBIX_SSHD=1.
# Runs as developer, no sudo. Does nothing otherwise, so the container listens on nothing.
set -euo pipefail

[ "${DIBIX_SSHD:-}" = "1" ] || exit 0

STATE="$HOME/.local/share/sshd"   # volume: keeps the host key stable across rebuilds
CONFIG=/workspace/.devcontainer/sshd_config

if [ ! -f "$STATE/ssh_host_ed25519_key" ]; then
  ssh-keygen -q -t ed25519 -N '' -C dibix-devcontainer -f "$STATE/ssh_host_ed25519_key"
fi

# Copy, not use in place: the bind keeps the host's owner and mode (root:root 0777 on Docker
# Desktop), which sshd's StrictModes rejects. CRLF stripped for keys files written on Windows.
tr -d '\r' < /etc/ssh/host_authorized_keys > "$STATE/authorized_keys"
chmod 600 "$STATE/authorized_keys"
if ! grep -q '^[^#]' "$STATE/authorized_keys"; then
  echo "  ⚠ sshd: the host's ~/.ssh/authorized_keys is empty — no key can log in"
fi

if [ -f "$STATE/sshd.pid" ] && kill "$(cat "$STATE/sshd.pid")" 2>/dev/null; then
  sleep 1
fi
/usr/sbin/sshd -f "$CONFIG" -E "$STATE/sshd.log"
echo "  ✓ sshd listening on port 2222, published on the host (log: $STATE/sshd.log)"