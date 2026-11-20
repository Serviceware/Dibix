#!/bin/bash
# .claude/cloud-environment-setup.sh
# Setup script for Claude Code cloud environments (claude.ai/code), the cloud counterpart of
# .devcontainer/. Runs as root on Ubuntu 24.04 before Claude starts. The resulting filesystem
# is cached, but only if setup finishes within about 5 minutes, and the cache is only rebuilt
# when the setup script (or the allowed hosts) change. That's why this script deliberately
# doesn't depend on the checkout: a NuGet restore or anything else derived from the repo
# contents would silently go stale in the cache.
#
# Required environment settings (network access, allowed domains, environment variables):
# see .claude/cloud-environment.md.

set -euo pipefail

# ─── SQL Server image for Testcontainers (shared/Tests/ContainerServices.cs) ───
# Pulled in the background, parallel to the SDK install, to stay within the cache budget.
# Not critical: Testcontainers pulls it on first use otherwise.
pull_sql_server_image() {
  if ! docker info >/dev/null 2>&1; then
    dockerd >/var/log/dockerd.log 2>&1 &
    for _ in $(seq 30); do
      docker info >/dev/null 2>&1 && break
      sleep 1
    done
  fi
  docker pull -q mcr.microsoft.com/mssql/server:latest
}
pull_sql_server_image &
pull_pid=$!

# ─── .NET 10 SDK ───
# Not pre-installed in cloud environments. global.json pins 10.0.403 with rollForward=latestMinor,
# which the latest 10.0 SDK satisfies. The apt feeds only carry the 10.0.1xx feature band.
curl -fsSL https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet --no-path
ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
# Lets apphost-based tools (ilspycmd) find the runtime without DOTNET_ROOT
mkdir -p /etc/dotnet
echo /usr/share/dotnet > /etc/dotnet/install_location
echo "✓ $(dotnet --version) .NET SDK installed"

# ─── ilspycmd (decompiler, for reverse-engineering third-party NuGet packages) ───
if dotnet tool install --tool-path /usr/local/dotnet/tools ilspycmd >/dev/null; then
  ln -sf /usr/local/dotnet/tools/ilspycmd /usr/local/bin/ilspycmd
  echo "✓ ilspycmd installed"
else
  echo "⚠ ilspycmd install failed"
fi

if wait "$pull_pid"; then
  echo "✓ SQL Server image pulled"
else
  echo "⚠ SQL Server image pull failed; Testcontainers will pull it on first use"
fi