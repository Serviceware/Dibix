// .devcontainer/initialize.mjs
// Runs on the HOST before the container is created. Ensures the host paths bound
// read-write in devcontainer.json exist, so Docker binds the right kind of node at
// each path instead of silently creating a *directory* where a file is expected.
// Node (not a shell script) because the host may be Windows, macOS, or Linux;
// os.homedir() resolves ${localEnv:HOME}${localEnv:USERPROFILE}.
import fs from "node:fs";
import path from "node:path";
import os from "node:os";

// Native Windows herdr keeps its files in %APPDATA%\herdr, not ~/.config/herdr, and
// devcontainer.json can't pick a per-OS path. Junction ~/.config/herdr to it (no admin
// rights needed), so the herdr binds share the host herdr's files.
if (process.platform === "win32" && process.env.APPDATA) {
  const link = path.join(os.homedir(), ".config", "herdr");
  const target = path.join(process.env.APPDATA, "herdr");
  if (!fs.existsSync(link)) {
    fs.mkdirSync(target, { recursive: true });
    fs.mkdirSync(path.dirname(link), { recursive: true });
    fs.symlinkSync(target, link, "junction");
  } else if (!fs.lstatSync(link).isSymbolicLink()) {
    console.warn(`${link} is a folder, not a link to ${target}; the container won't see your herdr config`);
  }
}

const files = [
  [".claude", "CLAUDE.md"],
  [".claude", "statusline-command.sh"],
  [".config", "opencode", "AGENTS.md"],
  [".config", "herdr", "config.toml"],
];

for (const rel of files) {
  const f = path.join(os.homedir(), ...rel);
  fs.mkdirSync(path.dirname(f), { recursive: true });
  if (!fs.existsSync(f)) fs.closeSync(fs.openSync(f, "w"));
}

// herdr's plugin registry must be a JSON array; an empty file would not parse.
const herdrPlugins = path.join(os.homedir(), ".config", "herdr", "plugins.json");
if (!fs.existsSync(herdrPlugins)) fs.writeFileSync(herdrPlugins, "[]");

const dirs = [[".config", "herdr", "plugins", "config"]];
for (const rel of dirs) fs.mkdirSync(path.join(os.homedir(), ...rel), { recursive: true });