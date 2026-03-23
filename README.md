# MG-CLI

[![NuGet](https://img.shields.io/nuget/v/mg-cli)](https://www.nuget.org/packages/mg-cli)

A .NET command-line tool for managing builds, versioning, and deployments of Godot projects. Handles the full CI/CD pipeline — from building export presets, to deploying on Steam and itch.io, to sending Discord notifications.

## Installation

```bash
dotnet tool install --global mg-cli
```

## Commands

| Command | Description |
|---|---|
| `godot-setup` | Install Godot engine and export templates |
| `godot-import` | Run the Godot import process |
| `godot-build` | Build a Godot project for one or more export presets |
| `godot-version` | Get or bump the version in `project.godot` |
| `csproj-versioning` | Bump the version in a `.csproj` file |
| `commit` | Commit and tag the current build |
| `discord-hook` | Send a Discord webhook with build info and changelog |
| `steamcmd-setup` | Install SteamCMD from the Steamworks SDK |
| `steam-deploy` | Deploy a build to Steam |
| `itchio-setup` | Install Butler (itch.io CLI) |
| `itchio-deploy` | Deploy a build to itch.io |
| `digitalocean` | Deploy a build to a DigitalOcean droplet |
| `test` | Print the MG-CLI banner |

---

### Godot Setup

Install the Godot engine and export templates for a specific version. Supports Windows, Linux, and macOS.

```bash
mg-cli godot-setup --version <godot-version>
```

| Option | Alias | Required | Description |
|---|---|---|---|
| `--version` | `-v` | Yes | Godot version to install (e.g. `4.4.1`) |

Downloads the engine and export templates from the official Godot GitHub releases, extracts them to the standard platform location, and sets executable permissions.

---

### Godot Import

Run the Godot headless import process for a project.

```bash
mg-cli godot-import <project-path> <godot-version>
```

| Argument | Required | Description |
|---|---|---|
| `project-path` | Yes | Path to the Godot project directory |
| `godot-version` | Yes | Godot version to use |

---

### Godot Build

Build a Godot project for one or more export presets. Supports both interactive selection and explicit preset names.

```bash
# Release build
mg-cli godot-build -p <project-path> -v <godot-version> -r <preset-name>

# Debug build
mg-cli godot-build -p <project-path> -v <godot-version> -d <preset-name>

# Interactive mode — select presets from a list
mg-cli godot-build -p <project-path> -v <godot-version> -i
```

| Option | Alias | Required | Description |
|---|---|---|---|
| `--projectPath` | `-p` | Yes | Path to the Godot project |
| `--godotVersion` | `-v` | Yes | Godot version |
| `--export-release` | `-r` | No | Export preset name (release) |
| `--export-debug` | `-d` | No | Export preset name (debug) |
| `--interactive` | `-i` | No | Select presets interactively |

Before building, the command runs `dotnet build` to catch C# compilation errors and ensures the `.godot` directory exists (running import if needed). Build logs are written to `builds/Logs/<preset>.log`. On macOS, the resulting `.app` bundle is automatically unsigned and de-quarantined.

---

### Godot Version

Get or bump the version in a Godot `project.godot` file. Uses a `YYYY.MM.BUILD` scheme — year and month are set automatically, and the build number is incremented. Run from the Godot project directory.

```bash
# Print the current version
mg-cli godot-version

# Bump the version
mg-cli godot-version --bump
```

| Option | Alias | Required | Description |
|---|---|---|---|
| `--bump` | `-b` | No | Bump the version number instead of printing it |

---

### Csproj Versioning

Bump the patch version in a `.csproj` file. Increments the third segment of a `MAJOR.MINOR.PATCH` version string.

```bash
mg-cli csproj-versioning <path-to-csproj> [property-name]
```

| Argument | Required | Default | Description |
|---|---|---|---|
| `path` | Yes | — | Path to the `.csproj` file |
| `propertyName` | No | `AssemblyVersion` | The XML element to update |

---

### Commit

Stage all changes, commit with the current build version, create a git tag, and push to `origin/main`.

```bash
mg-cli commit -p <project-path>
```

| Option | Alias | Required | Description |
|---|---|---|---|
| `--projectPath` | `-p` | Yes | Path to the Godot project |

Commit message format: `_Build Version: <version>`
Tag format: `v<version>`

---

### Discord Hook

Send a Discord webhook embed with the latest build version and a changelog generated from git commits since the previous tag.

```bash
mg-cli discord-hook -p <project-path> -h <webhook-url> -s <steam-url> -l <logo-url>
```

| Option | Alias | Required | Description |
|---|---|---|---|
| `--projectPath` | `-p` | Yes | Path to the Godot project |
| `--hookUrl` | `-h` | Yes | Discord webhook URL |
| `--steamUrl` | `-s` | Yes | Steam store page URL |
| `--logoUrl` | `-l` | Yes | URL to a logo/thumbnail image |
| `--noChangeLog` | — | No | Skip the changelog in the embed |

---

### SteamCMD Setup

Download and install SteamCMD from the Steamworks SDK.

```bash
mg-cli steamcmd-setup
```

Installs the content builder to `~/steamcmd`. No arguments required.

---

### Steam Deploy

Deploy a build to Steam using SteamCMD and a VDF build configuration file.

```bash
mg-cli steam-deploy -p <project-path> --vdf <path-to-vdf> -u <username> -pw <password>
```

| Option | Alias | Required | Description |
|---|---|---|---|
| `--projectPath` | `-p` | Yes | Path to the Godot project |
| `--vdf` | — | Yes | Path to the `.vdf` build config |
| `--username` | `-u` | Yes | Steamworks username |
| `--password` | `-pw` | Yes | Steamworks password |
| `--preview` | `-pv` | No | Mark the build as a preview |

The VDF file's `Desc` field is automatically set to the current version.

---

### Itch.io Butler Setup

Download and install [Butler](https://itch.io/docs/butler/), the itch.io command-line deployment tool.

```bash
# Install Butler
mg-cli itchio-setup

# Check installed version
mg-cli itchio-setup -v
```

| Option | Alias | Required | Description |
|---|---|---|---|
| `--version` | `-v` | No | Print the installed Butler version instead of installing |

After installation you will be prompted to log in interactively.

---

### Itch.io Deploy

Push a build to itch.io using Butler.

```bash
mg-cli itchio-deploy <build-path> <company/game:platform> -p <project-path>
```

| Argument | Required | Description |
|---|---|---|
| `build-path` | Yes | Path to the build directory |
| `company/game:platform` | Yes | itch.io target (e.g. `my-studio/my-game:windows`) |

| Option | Alias | Required | Description |
|---|---|---|---|
| `--projectPath` | `-p` | Yes | Path to the Godot project |

---

### DigitalOcean Deploy

Deploy a build to a DigitalOcean droplet over SSH. Copies a systemd service file, an optional nginx config, and build files to the server, then restarts the service.

```bash
mg-cli digitalocean <ip-address> <service-file-path> <build-path> [-n <nginx-config>]
```

| Argument | Required | Description |
|---|---|---|
| `ip-address` | Yes | IP address of the DigitalOcean droplet |
| `service-file-path` | Yes | Path to the systemd `.service` file to deploy |
| `build-path` | Yes | Path to the local build directory to copy to the server |

| Option | Alias | Required | Description |
|---|---|---|---|
| `--nginx-config` | `-n` | No | Path to an nginx `.conf` file to deploy |

The command reads `WorkingDirectory` and `ExecStart` from the service file to determine where to deploy and what to make executable. It stops the existing service, cleans the remote directory, copies all files via `scp`, and restarts the service with `systemctl`.

---

## CI/CD

The repository includes a [GitHub Actions workflow](.github/workflows/publish.yml) that runs on every push to `main`:

1. Bumps the package version
2. Commits the version change and creates a git tag
3. Builds, packs, and publishes to NuGet

To set up publishing, add a `NUGET_API_KEY` secret to your repository (Settings → Secrets and variables → Actions).

## License

[MIT](LICENSE.md)
