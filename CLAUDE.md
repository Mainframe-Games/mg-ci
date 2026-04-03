# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

MG-CLI is a .NET 10.0 global tool (`mg-cli`) for managing CI/CD pipelines for Godot game projects — building, versioning, deploying to Steam/itch.io/DigitalOcean, and posting Discord notifications.

## Commands

```bash
# Pack and install locally (primary dev workflow)
just pack
# Equivalent manual steps:
dotnet pack ./MG-CLI/MG-CLI.csproj -c Release
dotnet tool uninstall --global mg-cli || true
dotnet tool install --global mg-cli --add-source "./MG-CLI/nupkg" --no-cache

# Restore and build
dotnet restore
dotnet build --configuration Release --no-restore
```

There are no automated tests in this project.

## Architecture

**Entry point**: `MG-CLI/Program.cs` — registers all commands onto a `RootCommand` from `System.CommandLine`.

**Commands** (`MG-CLI/Commands/`): Each command is a subclass of `System.CommandLine.Command` with options/arguments registered in the constructor and a `Run()` async method as the handler. Key commands:
- `GodotBuild` — builds export presets; calls `GodotSetup.GetDefaultGodotPath()` and can invoke `GodotImport` as a pre-step
- `GodotVersion` — reads/bumps version in `project.godot` (YYYY.MM.BUILD scheme); other commands call `GetVersion()` statically
- `CsprojVersioning` — bumps patch in `.csproj` files (MAJOR.MINOR.PATCH); used by the CI workflow to auto-version the tool itself
- `DiscordHook` — builds a rich embed using `git log` between tags for changelog content
- `Commit` — stages all, commits with version string, creates git tag, pushes to `origin/main`
- `DigitalOcean` — deploys via SSH (scp + systemd restart)

**Utilities** (`MG-CLI/Utils/`):
- `Log.cs` — colored console output (Spectre.Console markup) + optional file logging; strip ANSI before writing to file
- `CliWrapExtensions.cs` — pipes `CliWrap` stdout/stderr through `Log` handlers
- `Web.cs` — `DownloadFileWithProgressAsync()` with Spectre progress bar
- `FileEx.cs` — async file writes (UTF-8 no BOM), Unix chmod helper

**Key dependencies**: `CliWrap` (process execution), `Spectre.Console` (UI), `System.CommandLine` (CLI framework), `Newtonsoft.Json` (Discord webhook payloads).

## CI/CD

`.github/workflows/publish.yml` triggers on push to `main` (skipped when commit message contains `[skip ci]`):
1. Runs `csproj-versioning` to bump patch version on `MG-CLI/MG-CLI.csproj`
2. Commits the bump and pushes a git tag
3. Packs and pushes the NuGet package to nuget.org (requires `NUGET_API_KEY` secret)
