Cloud environment settings for Dibix (claude.ai/code), next to the setup script .claude/cloud-environment-setup.sh
The lines below each heading are meant to be pasted as-is into the matching field.

Network access: Limited, with "Allow package managers" checked
(covers nuget.org, mcr.microsoft.com, Docker Hub and GitHub)

Allowed domains (.NET SDK downloads in the setup script):
builds.dotnet.microsoft.com

Environment variables (telemetry endpoints aren't allowed, so sending only adds latency):
DOTNET_CLI_TELEMETRY_OPTOUT=1
DOTNET_NOLOGO=1
DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

Network secrets (queuing builds through the Azure DevOps REST API; see AGENTS.md):
Secret type Basic ("Username and password") with any username (Azure DevOps ignores it) and an
Azure DevOps PAT as password, scope Build (Read & execute), for this host:
dev.azure.com