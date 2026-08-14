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
#   - builds.dotnet.microsoft.com / aka.ms: .NET reference packs / workloads on restore
#   - marketplace.visualstudio.com + vscode.*: VS Code Server extensions
# Note: api.nuget.org (NuGet restore, public feed in NuGet.config) is handled
# separately below via CIDR, not this dig loop — see the block after GitHub.
for domain in \
    "api.anthropic.com" \
    "statsig.anthropic.com" \
    "claude.ai" \
    "console.anthropic.com" \
    "api.openai.com" \
    "auth.openai.com" \
    "chatgpt.com" \
    "sentry.io" \
    "statsig.com" \
    "opencode.ai" \
    "models.dev" \
    "registry.npmjs.org" \
    "aka.ms" \
    "builds.dotnet.microsoft.com" \
    "marketplace.visualstudio.com" \
    "vscode.blob.core.windows.net" \
    "update.code.visualstudio.com" \
    "login.microsoftonline.com" \
    "management.azure.com" \
    "dev.azure.com" \
    "vssps.dev.azure.com"; do
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

# api.nuget.org (NuGet restore, public feed in NuGet.config) is fronted by Azure
# Traffic Manager + Azure Front Door (api.nuget.org -> nugetapiprod.trafficmanager.net
# -> apiprod-mscdn.afd.azureedge.net -> mr-afd-azuredge.tm-azurefd.net) — a large
# anycast CDN with a huge, rotating pool of edge IPs, the same short-TTL problem as
# GitHub above but with far more addresses. Allow Azure Front Door's published
# frontend ranges (service tag "AzureFrontDoor.Frontend", IPv4 subset) instead of dig.
# Source: https://www.microsoft.com/en-us/download/details.aspx?id=56519
# (Azure IP Ranges and Service Tags – Public Cloud; snapshot 2026-07-27,
# changeNumber 411 — Microsoft publishes a new file weekly).
echo "Adding 123 NuGet (Azure Front Door) ranges..."
for cidr in \
    "4.145.22.160/29" \
    "4.147.44.8/29" \
    "4.173.102.138/31" \
    "4.173.103.144/29" \
    "4.188.10.28/30" \
    "4.188.12.24/29" \
    "4.191.92.24/29" \
    "4.199.29.134/31" \
    "4.199.29.184/29" \
    "4.208.127.240/29" \
    "4.216.8.160/29" \
    "4.223.184.160/30" \
    "4.232.98.112/29" \
    "13.73.248.8/29" \
    "13.80.194.200/29" \
    "13.105.221.0/24" \
    "13.107.208.0/24" \
    "13.107.213.0/24" \
    "13.107.224.0/24" \
    "13.107.226.0/24" \
    "13.107.231.0/24" \
    "13.107.234.0/23" \
    "13.107.237.0/24" \
    "13.107.238.0/23" \
    "13.107.246.0/24" \
    "13.107.253.0/24" \
    "20.15.221.160/29" \
    "20.17.125.72/29" \
    "20.21.37.32/29" \
    "20.36.120.96/29" \
    "20.37.64.96/29" \
    "20.37.156.112/29" \
    "20.37.192.88/29" \
    "20.37.224.96/29" \
    "20.38.84.64/29" \
    "20.38.136.96/29" \
    "20.39.11.0/29" \
    "20.41.4.80/29" \
    "20.41.64.112/29" \
    "20.41.192.96/29" \
    "20.42.4.112/29" \
    "20.42.129.144/29" \
    "20.42.224.96/29" \
    "20.43.41.128/29" \
    "20.43.64.88/29" \
    "20.43.128.104/29" \
    "20.45.112.96/29" \
    "20.45.192.96/29" \
    "20.51.7.32/29" \
    "20.52.95.240/29" \
    "20.59.82.180/30" \
    "20.72.18.240/29" \
    "20.97.39.120/29" \
    "20.113.254.80/29" \
    "20.119.28.40/29" \
    "20.150.160.72/29" \
    "20.189.106.72/29" \
    "20.192.161.96/29" \
    "20.192.225.40/29" \
    "20.197.145.0/29" \
    "20.197.145.8/31" \
    "20.210.70.68/30" \
    "20.215.4.200/29" \
    "20.217.44.200/29" \
    "40.67.48.96/29" \
    "40.74.30.64/29" \
    "40.80.56.96/29" \
    "40.80.168.96/29" \
    "40.80.184.112/29" \
    "40.82.248.72/29" \
    "40.89.16.96/29" \
    "40.90.64.0/22" \
    "40.90.68.0/24" \
    "40.90.70.0/23" \
    "48.192.88.240/30" \
    "48.195.102.234/31" \
    "48.195.103.72/29" \
    "48.199.205.88/30" \
    "48.204.185.120/29" \
    "48.223.80.232/29" \
    "51.12.41.0/29" \
    "51.12.193.0/29" \
    "51.53.28.216/29" \
    "51.57.122.168/29" \
    "51.104.24.88/29" \
    "51.105.80.96/29" \
    "51.105.88.96/29" \
    "51.107.48.96/29" \
    "51.107.144.96/29" \
    "51.120.40.96/29" \
    "51.120.224.96/29" \
    "51.137.160.88/29" \
    "52.136.48.96/29" \
    "52.140.104.96/29" \
    "52.150.136.112/29" \
    "52.228.80.112/29" \
    "57.166.0.112/29" \
    "57.175.44.132/31" \
    "57.175.48.144/29" \
    "68.210.172.152/29" \
    "68.221.92.24/29" \
    "74.144.32.230/31" \
    "74.144.33.0/29" \
    "102.133.56.80/29" \
    "102.133.216.80/29" \
    "104.212.67.0/24" \
    "104.212.68.0/24" \
    "150.171.1.16/28" \
    "150.171.22.0/23" \
    "150.171.26.0/24" \
    "150.171.84.0/22" \
    "150.171.88.0/23" \
    "150.171.109.0/24" \
    "150.171.110.0/23" \
    "150.171.112.0/24" \
    "158.23.108.48/29" \
    "172.186.128.134/31" \
    "172.186.128.152/29" \
    "172.192.205.92/31" \
    "172.192.208.96/29" \
    "172.204.165.104/29" \
    "191.233.9.112/29" \
    "191.235.224.88/29"; do
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

# api.openai.com, auth.openai.com, chatgpt.com, and the Microsoft/Entra + Azure DevOps
# endpoints (login.microsoftonline.com, management.azure.com, dev.azure.com,
# vssps.dev.azure.com) all sit behind CDNs/traffic managers with short DNS TTLs
# (observed 16-300s for the Microsoft ones, ~6s for OpenAI's Cloudflare fronting) — far more
# volatile than GitHub's ~45s, so the CIDR-range trick above isn't a good fit for them: these
# ranges are shared across unrelated tenants, which would make the allowlist far too broad.
# The Microsoft/Azure DevOps domains are also resolved eagerly in the loop above so the ipset
# is already populated before the firewall drops packets; this background loop keeps them fresh
# for the life of the container. ipset only grows (stale entries are harmless leftovers);
# iptables rules above already ACCEPT anything in the set, so no further rule changes are
# needed once entries land.
CDN_DNS_REFRESH_PID_FILE="/var/run/cdn-dns-refresh.pid"
if [ -f "$CDN_DNS_REFRESH_PID_FILE" ]; then
    old_pid=$(cat "$CDN_DNS_REFRESH_PID_FILE" 2>/dev/null || true)
    if [ -n "$old_pid" ] && kill -0 "$old_pid" 2>/dev/null; then
        echo "Stopping previous CDN DNS refresh loop (pid $old_pid)"
        kill "$old_pid" 2>/dev/null || true
    fi
fi

(
    while true; do
        sleep 10
        for domain in "api.openai.com" "auth.openai.com" "chatgpt.com" \
            "login.microsoftonline.com" "management.azure.com" "dev.azure.com" \
            "vssps.dev.azure.com"; do
            ips=$(dig +short A "$domain" 2>/dev/null | grep -E '^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$' || true)
            for ip in $ips; do
                ipset add allowed-domains "$ip" 2>/dev/null || true
            done
        done
    done
) </dev/null >/var/log/cdn-dns-refresh.log 2>&1 &
disown
echo $! > "$CDN_DNS_REFRESH_PID_FILE"
echo "Started background CDN DNS refresh loop (pid $!, every 10s) — OpenAI + Microsoft Entra/Azure DevOps"

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

if ! curl --connect-timeout 5 -o /dev/null -s -w '%{http_code}' https://api.openai.com/v1/models | grep -qE '^(200|401)$'; then
    echo "WARNING: Unable to reach https://api.openai.com — OpenCode's OpenAI provider may not function"
else
    echo "Firewall verification passed — able to reach https://api.openai.com"
fi