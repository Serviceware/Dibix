# .devcontainer/Dockerfile
# Sandboxed image for running Claude Code / OpenCode autonomously: the .NET 10
# SDK plus Node.js (for the agents), the OpenCode CLI, and the outbound-firewall
# tooling used to lock down network egress.

ARG NODE_VERSION=24

# .NET 10 SDK (Ubuntu 24.04 "noble"). global.json pins 10.0.302 with
# rollForward=latestMinor, and the :10.0 tag always carries the latest GA
# 10.0.x SDK, so the pin resolves without an extra install step.
FROM mcr.microsoft.com/dotnet/sdk:10.0

ARG NODE_VERSION=24
ARG OPENCODE_VERSION=1.18.4
# UID/GID 1001: the base image (Ubuntu 24.04) already occupies 1000 with its
# stock 'ubuntu' user and 1654 with the .NET 'app' user, so 1001 avoids both
# without deleting base accounts. Exact value is irrelevant on a Windows host.
ARG USERNAME=developer
ARG USER_UID=1001
ARG USER_GID=$USER_UID

# System packages
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
 && apt-get install -y --no-install-recommends \
      # Core dev tools
      git \
      curl \
      sudo \
      ca-certificates \
      gnupg \
      unzip \
      # Search tools (the agents use these heavily)
      ripgrep \
      fd-find \
      tree \
      # Shell
      zsh \
      # Networking (for the outbound firewall)
      iptables \
      iproute2 \
      dnsutils \
      ipset \
 && rm -rf /var/lib/apt/lists/*

# ─── Node.js (for Claude Code and OpenCode) ───
RUN curl -fsSL https://deb.nodesource.com/setup_${NODE_VERSION}.x | bash - \
 && apt-get install -y nodejs \
 && rm -rf /var/lib/apt/lists/*

# ─── OpenCode (SST terminal AI agent) ───
RUN npm install -g opencode-ai@${OPENCODE_VERSION}

# Opt out of .NET telemetry / first-run experience — the outbound firewall blocks
# those endpoints anyway, so leaving them on only adds startup latency and noise.
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# ─── Create non-root user ───
# The extra sudoers entries beyond init-firewall.sh are required by the
# docker-outside-of-docker feature, whose postStart hook forwards the host
# Docker socket as the (non-root) remote user.
RUN groupadd --gid $USER_GID $USERNAME \
 && useradd -s /bin/zsh --uid $USER_UID --gid $USER_GID -m $USERNAME \
 && { \
      echo "$USERNAME ALL=(root) NOPASSWD: /bin/bash /workspace/.devcontainer/init-firewall.sh"; \
      echo "$USERNAME ALL=(root) NOPASSWD: /usr/bin/tee -a /tmp/vscr-docker-from-docker.log"; \
      echo "$USERNAME ALL=(root) NOPASSWD: /usr/bin/tee /tmp/vscr-docker-from-docker.pid"; \
      echo "$USERNAME ALL=(root) NOPASSWD: /usr/bin/rm -rf /var/run/docker.sock"; \
      echo "$USERNAME ALL=(root) NOPASSWD: /usr/bin/socat *"; \
      echo "$USERNAME ALL=(root) NOPASSWD: /usr/sbin/groupmod --gid [0-9]* docker"; \
    } > /etc/sudoers.d/developer-devcontainer \
 && chmod 0440 /etc/sudoers.d/developer-devcontainer \
 && visudo -cf /etc/sudoers.d/developer-devcontainer

# ─── Directories ───
RUN mkdir -p /home/$USERNAME/.claude \
 && mkdir -p /home/$USERNAME/.config/opencode \
 && mkdir -p /home/$USERNAME/.config/gh \
 && mkdir -p /home/$USERNAME/.local/share/opencode \
 && mkdir -p /commandhistory \
 && touch /commandhistory/.zsh_history \
 && chown -R $USERNAME:$USERNAME /home/$USERNAME \
 && chown -R $USERNAME:$USERNAME /commandhistory

# ─── Shell history persistence ───
RUN echo 'export HISTFILE=/commandhistory/.zsh_history' >> /home/$USERNAME/.zshrc \
 && echo 'export HISTSIZE=10000' >> /home/$USERNAME/.zshrc \
 && echo 'export SAVEHIST=10000' >> /home/$USERNAME/.zshrc

# ─── Workspace ───
# Match the bind-mount target in devcontainer.json so `devcontainer exec` and
# interactive shells start in the populated workspace, not an empty dir.
WORKDIR /workspace

USER $USERNAME

# Verify toolchain
RUN dotnet --version && node --version && opencode --version

ENV SHELL=/bin/zsh
CMD ["zsh"]