# Command Reference

This page documents every MG-CLI command: its purpose, arguments, options, and the behavior it drives. Commands are grouped exactly as they appear on the command line.

> **Conventions used below**
> - `-p`/`--projectPath` always points at a Godot project directory (the folder containing `project.godot`, or an ancestor of it — the tool searches recursively for the `.godot` settings file).
> - The **version** is never passed as an argument; commands read `config/version` from `project.godot`. See [Versioning](versioning.md).
> - All commands return a process **exit code** — `0` on success, non-zero on failure — so they compose safely in scripts.

---

## Godot commands (`mg-cli godot …`)

The `godot` group bundles everything that touches the engine or the project itself.

### `godot install`

Install the Godot engine and export templates for a specific version.

```bash
mg-cli godot install <version>
```

| Argument | Required | Description |
|---|---|---|
| `version` | Yes | Godot version to install (e.g. `4.4.1`). |

**Behavior**

- Downloads the **Mono** engine build and the matching **export templates** (`.tpz`) from the official [`godot-builds`](https://github.com/godotengine/godot-builds) GitHub releases, using a per-OS asset name:
  - Windows → `Godot_v<ver>-stable_mono_win64.zip`, installed under `%USERPROFILE%\Godot`.
  - Linux → `Godot_v<ver>-stable_mono_linux_x86_64.zip`, installed under `~/.local/share/godot/engine`.
  - macOS → `Godot_v<ver>-stable_mono_macos.universal.zip`, installed under `~/Applications`.
- Extracts export templates to the platform's standard `export_templates/<ver>.stable.mono` directory and flattens the archive's inner `templates/` folder.
- On **Linux**, additionally creates a desktop entry at `~/.local/share/applications/godot.desktop`, downloads the Godot icon, and `chmod +x`'s the engine.
- On **macOS**, `chmod +x`'s the engine binary inside `Godot_mono.app`.
- Downloads render a [progress bar](utilities.md#web--downloads-with-a-progress-bar).

> [!TIP]
> A matching static helper, `GodotInstall.GetDefaultGodotPath(version)`, is used by `godot build` and `godot import` to locate the installed engine — so once you have installed a version, the build commands find it automatically.

---

### `godot import`

Run the Godot headless import process for a project. Import must succeed before an export will work; this materializes the `.godot` cache and imported assets.

```bash
mg-cli godot import <project-path> <godot-version>
```

| Argument | Required | Description |
|---|---|---|
| `project-path` | Yes | Path to the Godot project directory. |
| `godot-version` | Yes | Godot version to use. |

**Behavior**

- Resolves the engine via `GetDefaultGodotPath` and runs `godot --headless --import` in the project directory.
- Captures warnings/errors to `godot-import.log` (minimum level `Warning`) for the duration of the run.
- `godot build` calls this automatically when it detects a missing `.godot` folder, so you rarely need to run it by hand.

---

### `godot build`

Build (export) a Godot project for one or more export presets. This is the workhorse command.

```bash
# Release build of a named preset
mg-cli godot build -p <project-path> -v <godot-version> -r <preset-name>

# Debug build of a named preset
mg-cli godot build -p <project-path> -v <godot-version> -d <preset-name>

# Interactive — pick presets from a checklist
mg-cli godot build -p <project-path> -v <godot-version> -i
```

| Option | Alias | Required | Description |
|---|---|---|---|
| `--projectPath` | `-p` | Yes | Path to the Godot project. |
| `--godotVersion` | `-v` | Yes | Godot version to build with. |
| `--export-release` | `-r` | No | Export preset name to build in **release** mode. |
| `--export-debug` | `-d` | No | Export preset name to build in **debug** mode. |
| `--interactive` | `-i` | No | Select one or more presets from a checklist prompt, then choose debug/release. |

**Behavior**

1. **Preset selection.** In interactive mode, presets are read from `export_presets.cfg` and shown in a Spectre multi-select prompt, followed by a "Build in Debug mode?" confirmation. Otherwise, the single preset named by `-r` or `-d` is used (presence of `-d` implies debug).
2. **Pre-build.** Runs `dotnet build` in the project directory to surface C# compile errors *before* attempting an export. A non-zero result aborts the whole command.
3. **Import guard.** If the `.godot` directory does not exist, `godot import` is run first (a workaround for [firebelley/godot-export#127](https://github.com/firebelley/godot-export/issues/127)).
4. **Per-preset export.** For each selected preset:
   - The output path is read from `export_presets.cfg` (`export_path=`), and that directory is deleted and recreated.
   - A per-preset log file `builds/Logs/<preset>.log` is opened.
   - The engine runs headless: `godot --headless --import --path . --export-release <preset>` (or `--export-debug`).
   - **macOS post-build:** if the preset is named `Mac`, the exported `.zip` is unzipped to a `.app`, the inner binary is `chmod +x`'d, the quarantine attribute is removed (`xattr -dr com.apple.quarantine`), and the app is ad-hoc code-signed (`codesign -s - --force --deep`).
5. **Exit code.** Returns the exit code of the last export; failures per-preset are logged as errors.

---

### `godot version`

Read or bump the game's version in `project.godot`.

```bash
# Print the current version (and suffix, if any)
mg-cli godot version

# Bump the build number and update year/month
mg-cli godot version --bump
```

| Option | Alias | Required | Description |
|---|---|---|---|
| `--bump` | `-b` | No | Bump the version instead of printing it. |

**Behavior**

- Runs against the **current working directory** — `cd` into your Godot project first.
- Reads `config/version` (and appends `config/version_suffix` if present) from the `.godot` settings file.
- With `--bump`, rewrites `config/version` using the `YYYY.MM.BUILD` scheme: the year and month are set from the current date and the build number is incremented. The file is written back as UTF-8 without a BOM.

See [Versioning](versioning.md) for the full scheme and rationale.

---

### `godot settings`

Get or set an arbitrary key in the `project.godot` settings file.

```bash
# Read a value
mg-cli godot settings --get config/name

# Write a value
mg-cli godot settings --set 'config/name="My Game"'
```

| Option | Alias | Required | Description |
|---|---|---|---|
| `--get` | `-g` | No | Print the value of the given key. |
| `--set` | `-s` | No | Set a key using `key=value` syntax. |

**Behavior**

- Runs against the current working directory and locates the `.godot` settings file.
- `--set` takes a `key=value` string, finds the matching line, and replaces its value (writing UTF-8 without a BOM). `--get` prints the current value, or an error if the key is absent.
- You can pass both in one invocation; the `--set` is applied first, then `--get` reads.

---

## Steam commands (`mg-cli steam …`)

### `steam setup`

Download and install SteamCMD (the SteamPipe content builder) from the Steamworks SDK.

```bash
mg-cli steam setup
```

**Behavior**

- Downloads the Steamworks SDK zip (SDK version `163`) from `partner.steamgames.com`, extracts it, and copies the platform-appropriate `ContentBuilder/builder*` contents into `~/steamcmd` (or `~/Applications/steamcmd` on macOS).
- On macOS, `chmod +x`'s `steamcmd`/`steamcmd.sh` and runs it once to bootstrap.
- Exposes `GetDefaultSteamCmdPath()` for the deploy command to locate the installed binary.

> [!NOTE]
> The Steamworks SDK download requires network access to Valve's partner site. This is a one-time setup per machine.

---

### `steam deploy`

Deploy a build to Steam via SteamCMD and a VDF app-build script.

```bash
mg-cli steam deploy -p <project-path> --vdf <path-to-vdf> -u <username> -pw <password> [--preview]
```

| Option | Alias | Required | Description |
|---|---|---|---|
| `--projectPath` | `-p` | Yes | Path to the Godot project. |
| `--vdf` | — | Yes | Path to the SteamPipe `.vdf` build script (relative to the project path). |
| `--username` | `-u` | Yes | Steamworks username. |
| `--password` | `-pw` | Yes | Steamworks password. |
| `--preview` | `-pv` | No | Mark the VDF build as a preview (`Preview` → `1`). |

**Behavior**

- Reads the version from `project.godot` and writes it into the VDF's `Desc` field (and toggles `Preview` based on `--preview`) before uploading.
- Runs SteamCMD: `steamcmd +login <user> <pass> +run_app_build <vdf> +quit`, with the console's standard input piped through so interactive prompts (e.g. Steam Guard) work.
- Returns SteamCMD's exit code.

> [!WARNING]
> Credentials are passed on the command line. In CI, provide them via masked secrets/environment variables, never hard-coded.

---

## itch.io commands (`mg-cli itchio …`)

### `itchio setup`

Download and install [Butler](https://itch.io/docs/butler/), itch.io's command-line upload tool.

```bash
# Install Butler (prompts an interactive login afterward)
mg-cli itchio setup

# Print the installed Butler version instead of installing
mg-cli itchio setup -v
```

| Option | Alias | Required | Description |
|---|---|---|---|
| `--version` | `-v` | No | Print the installed Butler version instead of installing. |

**Behavior**

- Downloads the latest Butler build for the current OS from `broth.itch.zone`, extracts it to `~/butler`, and `chmod +x`'s the binary.
- After installing, runs `butler login` so you can authenticate interactively. Your credentials are stored by Butler itself for subsequent deploys.

---

### `itchio deploy`

Push a build directory to an itch.io page/channel using Butler.

```bash
mg-cli itchio deploy <build-path> <company/game:platform> -p <project-path>
```

| Argument | Required | Description |
|---|---|---|
| `build-path` | Yes | Path to the build directory to upload. |
| `company/game:platform` | Yes | The itch.io target, e.g. `my-studio/my-game:windows`. |

| Option | Alias | Required | Description |
|---|---|---|---|
| `--projectPath` | `-p` | Yes | Path to the Godot project (used to read the version). |

**Behavior**

- Reads the version from `project.godot` and runs `butler push <build-path> <company/game:platform> --userversion <version>`, so the itch.io channel is tagged with your game's version.
- Returns Butler's exit code.

---

## Release commands

### `commit`

Stage all changes, commit with the current build version, create a git tag, and push to `origin/main`.

```bash
mg-cli commit [project-path]
```

| Argument | Required | Description |
|---|---|---|
| `project-path` | No | Path to the Godot project; defaults to the current directory. |

**Behavior**, in order (each step aborts on a non-zero git exit code):

1. `git add .`
2. Read the version from `project.godot`.
3. `git commit -m "_Build Version: <version>"` — note the leading underscore, which `discord-hook` uses to filter this commit out of changelogs.
4. `git tag v<version>`
5. `git push origin main --tags`

---

### `discord-hook`

Post a rich Discord embed announcing a new build, including a changelog generated from git tags.

```bash
mg-cli discord-hook -p <project-path> -h <webhook-url> -s <steam-url> -l <logo-url> [--noChangeLog]
```

| Option | Alias | Required | Description |
|---|---|---|---|
| `--projectPath` | `-p` | Yes | Path to the Godot project. |
| `--hookUrl` | `-h` | Yes | Discord webhook URL to POST to. |
| `--steamUrl` | `-s` | Yes | Steam store URL — becomes the embed's clickable title link. |
| `--logoUrl` | `-l` | Yes | URL of a logo/capsule image — becomes the embed thumbnail. |
| `--noChangeLog` | — | No | Omit the changelog section. |

**Behavior**

- Lists git tags sorted by creation date (`git tag --sort=-creatordate`).
- Unless `--noChangeLog` is set, collects commits between the previous tag and `HEAD` (`git log <prevTag>..HEAD --oneline`), strips the SHA prefixes, and **skips commits whose message starts with `_`** (i.e. the build-version commits made by `commit`).
- Builds a green (`color: 65280`) embed titled `New Build Available! | <version>`, with the changelog as a bulleted list in the description, the Steam URL as the title link, and the logo as the thumbnail — then POSTs it to the webhook as JSON.
- Returns non-zero (the HTTP status code) if Discord rejects the request.

---

## Versioning command

### `csproj-versioning`

Bump a `MAJOR.MINOR.PATCH` version inside a `.csproj` file. This is what the tool's own [CI pipeline](cicd.md) uses to auto-version each NuGet release — it is not tied to Godot at all.

```bash
mg-cli csproj-versioning <path-to-csproj> [property-name]
```

| Argument | Required | Default | Description |
|---|---|---|---|
| `path` | Yes | — | Path to the `.csproj` file. |
| `propertyName` | No | `AssemblyVersion` | The XML element under `<PropertyGroup>` to update. |

**Behavior**

- Loads the `.csproj` as XML, reads the named property, increments the third (`PATCH`) segment, and writes the file back.
- On any error, logs it and returns a non-zero exit code (the exception's `HResult`).

> [!NOTE]
> The GitHub Actions workflow calls this with `Version` as the property name to bump `<Version>` in `MG-CLI.csproj`. See [CI/CD Pipeline](cicd.md).

---

## Server deploy command

### `digitalocean`

Deploy a build to a DigitalOcean droplet over SSH, managing a systemd service and (optionally) an nginx config. Intended for headless server builds such as a game's master/matchmaking server.

```bash
mg-cli digitalocean <ip-address> <service-file-path> <build-path> [-n <nginx-config>]
```

| Argument | Required | Description |
|---|---|---|
| `ip-address` | Yes | IP address of the droplet. Connects as `root@<ip>`. |
| `service-file-path` | Yes | Path to the systemd `.service` file to deploy. |
| `build-path` | Yes | Local build directory to copy to the server. |

| Option | Alias | Required | Description |
|---|---|---|---|
| `--nginx-config` | `-n` | No | Path to an nginx `.conf` file to deploy and reload. |

**Behavior**

- Reads `WorkingDirectory` and `ExecStart` from the `.service` file to learn where to deploy and which binary to make executable.
- **Stop & clean:** `systemctl stop <service>` (tolerating a not-running service) and `rm -rf <WorkingDirectory>` on the server.
- **Service file:** `scp`'s the `.service` file to `/etc/systemd/system/` (validates the `.service` extension).
- **nginx (optional):** `scp`'s the `.conf` to `/etc/nginx/conf.d/` (validates the `.conf` extension) and runs `nginx -s reload`.
- **Build files:** recursively `scp`'s the build directory to the server home.
- **Restart:** `chmod +x` the `ExecStart` binary, then `systemctl daemon-reload && enable && start` the service.
- Errors during any step are logged and returned as the command's exit code.

> [!WARNING]
> This command assumes password-less SSH access as `root@<ip>` (i.e. your SSH key is already authorized on the droplet). It runs privileged operations (`systemctl`, writing to `/etc`) — point it only at servers you control.

---

For how these commands share state and conventions, see [Architecture](architecture.md); for the helper layer they build on, see [Utilities](utilities.md).
