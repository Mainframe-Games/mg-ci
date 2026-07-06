# Getting Started

This guide walks you from a clean machine to a full build-and-deploy pipeline.

## Prerequisites

- **[.NET 10.0 SDK](https://dotnet.microsoft.com/download)** — required to install and run the global tool.
- **A Godot project** using the Mono/.NET flavor of Godot (MG-CLI installs the `mono` engine builds and runs `dotnet build` as part of exporting).
- **`git`** on your `PATH` — used by the `commit` and `discord-hook` commands.
- Platform tools as needed: nothing extra to pre-install for Steam or itch.io — MG-CLI can install SteamCMD and Butler for you.

## Installation

Install from NuGet as a global tool:

```bash
dotnet tool install --global mg-cli
```

Update later with:

```bash
dotnet tool update --global mg-cli
```

Verify it is on your `PATH`:

```bash
mg-cli
```

Running `mg-cli` with no arguments prints the ASCII-art banner. Add `--help` to any command or command group to see its usage, e.g. `mg-cli godot --help` or `mg-cli godot build --help`.

## Your first build

The example below assumes your Godot project lives at `./game` and you want to build the export preset named `Windows`.

### 1. Install the Godot engine and export templates

```bash
mg-cli godot install 4.4.1
```

This downloads the Mono engine build and the matching export templates from the official Godot GitHub releases and installs them to the standard per-user location for your OS.

### 2. Bump the version (optional but recommended)

```bash
cd game
mg-cli godot version --bump
```

This updates `config/version` in `project.godot` to a fresh `YYYY.MM.BUILD` value. See [Versioning](versioning.md) for the details of the scheme.

### 3. Build an export preset

```bash
mg-cli godot build -p ./game -v 4.4.1 -r Windows
```

Before exporting, `godot build` runs a `dotnet build` to catch C# compile errors early, and imports the project if the `.godot` folder is missing. Build logs are written to `builds/Logs/<preset>.log`.

To choose presets interactively instead of naming one:

```bash
mg-cli godot build -p ./game -v 4.4.1 -i
```

## Deploying

Once you have a build, deploy it to any of the supported targets.

### Steam

```bash
# One-time: install SteamCMD
mg-cli steam setup

# Deploy using a VDF build script
mg-cli steam deploy -p ./game --vdf ./steam/app_build.vdf -u <user> -pw <password>
```

The current version is written into the VDF's `Desc` field automatically.

### itch.io

```bash
# One-time: install Butler (you'll be prompted to log in)
mg-cli itchio setup

# Push a build
mg-cli itchio deploy ./game/builds/Windows my-studio/my-game:windows -p ./game
```

### DigitalOcean (self-hosted server builds)

```bash
mg-cli digitalocean 203.0.113.10 ./deploy/master_server.service ./builds/master_server -n ./deploy/master_server.conf
```

## Tag the release and announce it

```bash
# Stage all, commit with the version, tag v<version>, push to origin/main
mg-cli commit ./game

# Post a Discord embed with a changelog generated from git tags
mg-cli discord-hook -p ./game -h <webhook-url> -s <steam-url> -l <logo-url>
```

## A complete pipeline

Chained together (in a shell, or as CI steps), a full pipeline reads naturally:

```bash
mg-cli godot install 4.4.1 \
  && mg-cli godot version --bump \
  && mg-cli godot build -p ./game -v 4.4.1 -r Windows \
  && mg-cli steam deploy -p ./game --vdf ./steam/app_build.vdf -u "$STEAM_USER" -pw "$STEAM_PASS" \
  && mg-cli commit ./game \
  && mg-cli discord-hook -p ./game -h "$DISCORD_HOOK" -s "$STEAM_URL" -l "$LOGO_URL"
```

Because each command returns a non-zero exit code on failure, the `&&` chain stops at the first problem — you will never deploy a build that failed to export.

Next: see the [Command Reference](commands.md) for every flag, or the [Architecture](architecture.md) to understand how the pieces are wired together.
