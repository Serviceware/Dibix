#!/bin/bash
set -euo pipefail
IFS=$'\n\t'

# Fix Docker socket group so the developer user (member of 'docker') can connect.
# docker-outside-of-docker creates /var/run/docker-host.sock owned by root:root;
# we need the group permission to actually apply for Testcontainers.
if [ -S /var/run/docker-host.sock ]; then
    chmod 666 /var/run/docker-host.sock && echo "Docker socket permissions set to 666" \
        || echo "WARNING: Could not chmod docker socket — Testcontainers may not work"
fi

# 1. Extract Docker DNS destination BEFORE any flushing
DNS_DEST=$(iptables-save -t nat 2>/dev/null \
    | grep "127\.0\.0\.11.*DNAT" \
    | grep -oP '(?<=--to-destination )\S+' \
    | head -1 || true)

# Flush existing rules
iptables -F
iptables -X
iptables -t nat -F
iptables -t nat -X
iptables -t mangle -F
iptables -t mangle -X
ipset destroy allowed-domains 2>/dev/null || true

# 2. Restore Docker DNS
if [ -n "$DNS_DEST" ]; then
    echo "Restoring Docker DNS rules (destination: $DNS_DEST)..."
    iptables -t nat -N DOCKER_OUTPUT 2>/dev/null || true
    iptables -t nat -A DOCKER_OUTPUT -d 127.0.0.11/32 -p udp --dport 53 -j DNAT --to-destination "$DNS_DEST"
    iptables -t nat -A DOCKER_OUTPUT -d 127.0.0.11/32 -p tcp --dport 53 -j DNAT --to-destination "$DNS_DEST"
else
    echo "No Docker DNS rules to restore"
fi

# Allow DNS and localhost before any restrictions
iptables -A OUTPUT -p udp --dport 53 -j ACCEPT
iptables -A INPUT  -p udp --sport 53 -j ACCEPT
iptables -A INPUT  -i lo -j ACCEPT
iptables -A OUTPUT -o lo -j ACCEPT

# Create ipset with CIDR support
ipset create allowed-domains hash:net

# Resolve and add allowed domains
#   - Anthropic / OpenCode telemetry + model metadata: the agents themselves
#   - claude.ai + console.anthropic.com: the OAuth login flow for `claude` (and
#     OpenCode's Anthropic provider) — not needed for API calls, only for
#     first-time / re-authentication
#   - sentry.io + statsig.*: agent crash reporting / feature-flag telemetry (optional, non-functional)
#   - registry.npmjs.org: OpenCode/Claude Code plugin + npx installs
#   - api.nuget.org: NuGet restore (public feed in NuGet.config) + NuGetAudit
#   - builds.dotnet.microsoft.com / aka.ms: .NET reference packs / workloads on restore
#   - marketplace.visualstudio.com + vscode.*: VS Code Server extensions
for domain in \
    "api.anthropic.com" \
    "statsig.anthropic.com" \
    "claude.ai" \
    "console.anthropic.com" \
    "sentry.io" \
    "statsig.com" \
    "opencode.ai" \
    "models.dev" \
    "registry.npmjs.org" \
    "api.nuget.org" \
    "aka.ms" \
    "builds.dotnet.microsoft.com" \
    "marketplace.visualstudio.com" \
    "vscode.blob.core.windows.net" \
    "update.code.visualstudio.com"; do
    echo "Resolving $domain..."
    ips=$(dig +noall +answer A "$domain" | awk '$4 == "A" {print $5}')
    if [ -z "$ips" ]; then
        echo "WARNING: Failed to resolve $domain — skipping"
        continue
    fi

    while read -r ip; do
        if [[ ! "$ip" =~ ^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$ ]]; then
            echo "ERROR: Invalid IP from DNS for $domain: $ip"
            exit 1
        fi
        echo "Adding $ip for $domain"
        ipset add allowed-domains "$ip" 2>/dev/null || true
    done < <(echo "$ips")
done

# GitHub (github.com, api.github.com, codeload, raw) sits behind a global load
# balancer with a short DNS TTL, so resolving to a single IP goes stale within
# minutes. Dibix's git remote is github.com/Serviceware/Dibix, so git clone/pull/
# push needs these. Allow GitHub's published owned CIDR ranges instead.
# Source: https://api.github.com/meta (git/web/api live in these).
for cidr in \
    "140.82.112.0/20" \
    "143.55.64.0/20" \
    "185.199.108.0/22" \
    "192.30.252.0/22"; do
    echo "Adding GitHub range $cidr"
    ipset add allowed-domains "$cidr" 2>/dev/null || true
done

# Get host IP from default route
HOST_IP=$(ip route | grep default | cut -d" " -f3)
if [ -z "$HOST_IP" ]; then
    echo "ERROR: Failed to detect host IP"
    exit 1
fi

echo "Host gateway IP: $HOST_IP"
iptables -A INPUT  -s "$HOST_IP" -j ACCEPT
iptables -A OUTPUT -d "$HOST_IP" -j ACCEPT

# Allow traffic to host.docker.internal (may differ from the default gateway on OrbStack).
# Required for Testcontainers: containers started via DooD expose ports on the Docker host,
# not on localhost inside the devcontainer.
DOCKER_HOST_IP=$(getent ahostsv4 host.docker.internal 2>/dev/null | awk '{print $1; exit}' || true)
if [ -n "$DOCKER_HOST_IP" ] && [ "$DOCKER_HOST_IP" != "$HOST_IP" ]; then
    echo "Docker host IP (host.docker.internal): $DOCKER_HOST_IP"
    iptables -A INPUT  -s "$DOCKER_HOST_IP" -j ACCEPT
    iptables -A OUTPUT -d "$DOCKER_HOST_IP" -j ACCEPT
elif [ -z "$DOCKER_HOST_IP" ]; then
    DOCKER_HOST_ADDRESS=$(getent hosts host.docker.internal 2>/dev/null | awk '{print $1; exit}' || true)
    if [ -n "$DOCKER_HOST_ADDRESS" ]; then
        echo "Skipping non-IPv4 Docker host address for iptables: $DOCKER_HOST_ADDRESS"
    fi
fi

# Set default policies to DROP
iptables -P INPUT   DROP
iptables -P FORWARD DROP
iptables -P OUTPUT  DROP

# Allow established connections
iptables -A INPUT  -m state --state ESTABLISHED,RELATED -j ACCEPT
iptables -A OUTPUT -m state --state ESTABLISHED,RELATED -j ACCEPT

# Allow outbound traffic to whitelisted domains
iptables -A OUTPUT -m set --match-set allowed-domains dst -j ACCEPT

# Reject everything else with immediate feedback
iptables -A OUTPUT -j REJECT --reject-with icmp-admin-prohibited

echo "Firewall configuration complete"
echo "Verifying firewall rules..."

if curl --connect-timeout 5 https://example.com >/dev/null 2>&1; then
    echo "ERROR: Firewall verification failed — was able to reach https://example.com"
    exit 1
else
    echo "Firewall verification passed — unable to reach https://example.com as expected"
fi

if ! curl --connect-timeout 5 https://api.anthropic.com/health >/dev/null 2>&1; then
    echo "WARNING: Unable to reach https://api.anthropic.com — Claude Code may not function"
else
    echo "Firewall verification passed — able to reach https://api.anthropic.com"
fi