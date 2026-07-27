// .devcontainer/initialize.mjs
// Runs on the HOST before the container is created. Ensures the host paths bound
// read-write in devcontainer.json exist, so Docker binds the right kind of node at
// each path instead of silently creating a *directory* where a file is expected.
// Node (not a shell script) because the host may be Windows, macOS, or Linux;
// os.homedir() resolves ${localEnv:HOME}${localEnv:USERPROFILE}.
import fs from "node:fs";
import path from "node:path";
import os from "node:os";

const files = [
  [".claude", "CLAUDE.md"],
  [".config", "opencode", "AGENTS.md"],
];

const dirs = [
  [".claude", "agents"],
];

for (const rel of files) {
  const f = path.join(os.homedir(), ...rel);
  fs.mkdirSync(path.dirname(f), { recursive: true });
  if (!fs.existsSync(f)) fs.closeSync(fs.openSync(f, "w"));
}

for (const rel of dirs) {
  fs.mkdirSync(path.join(os.homedir(), ...rel), { recursive: true });
}