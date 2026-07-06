# Introduction

## What is MG-CLI?

MG-CLI is a command-line tool distributed as the .NET global tool `mg-cli`. Its single purpose is to make **shipping a Godot game repeatable and automatable** — whether you run the commands by hand on your own machine or wire them into a CI runner such as GitHub Actions.

Godot's release story involves a lot of moving parts: you need the right engine version and export templates installed, the project's assets must be imported before an export will succeed, the version string in `project.godot` needs to be maintained, each target platform must be exported from its preset, and then the resulting binaries have to reach players through Steam, itch.io, or a self-hosted server. On top of that, teams usually want a git tag per release and a changelog posted somewhere visible.

MG-CLI collapses all of that into a set of small, composable sub-commands with a shared philosophy: **each command wraps one external tool or one well-defined file operation**, produces consistent colored output, and returns a meaningful exit code so it can be chained safely in a script.

## Design goals

- **Scriptable first.** Every command is non-interactive by default (with a couple of opt-in interactive modes) and returns process exit codes that a CI system can branch on. Failures short-circuit.
- **Cross-platform.** Windows, Linux, and macOS are first-class. Platform-specific behavior — install paths, executable names, code-signing — is handled internally so the same command line works everywhere.
- **Thin wrappers, not reimplementations.** MG-CLI shells out to the real tools (the Godot editor, SteamCMD, Butler, `git`, `ssh`) rather than reimplementing their logic. This keeps the tool small and keeps behavior faithful to the underlying tooling.
- **One version, one source of truth.** The game's version lives in `project.godot` under `config/version`. Almost every command reads it from there, so the version stays consistent across builds, tags, storefront metadata, and Discord announcements.
- **Consistent UX.** All output flows through a single [logging layer](utilities.md#log--colored-logging-with-optional-file-capture) built on [Spectre.Console](https://spectreconsole.net/), including progress bars for downloads and unzips.

## Who is it for?

- **Solo developers and small studios** shipping Godot games who want a reproducible release process without hand-maintaining a pile of shell scripts.
- **CI pipelines** that build and publish Godot games automatically on push or on tag.

## The command families

MG-CLI groups its commands into families. Some are grouped under a parent verb (e.g. `godot`, `steam`, `itchio`); others are top-level.

| Family | Commands | Purpose |
|---|---|---|
| **Godot** (`godot …`) | `install`, `import`, `build`, `version`, `settings` | Manage the engine and the project — install Godot, import assets, export builds, read/bump the version, and read/write project settings. |
| **Steam** (`steam …`) | `setup`, `deploy` | Install SteamCMD and push builds to Steam. |
| **itch.io** (`itchio …`) | `setup`, `deploy` | Install Butler and push builds to itch.io. |
| **Git / release** | `commit`, `discord-hook` | Tag & push a release, and announce it on Discord with a changelog. |
| **Versioning** | `csproj-versioning` | Bump a `MAJOR.MINOR.PATCH` version in a `.csproj` (used by the tool's own CI). |
| **Server deploy** | `digitalocean` | Deploy a build to a DigitalOcean droplet over SSH with systemd + nginx. |

See the [Command Reference](commands.md) for full details on every command, argument, and option.

## How it fits together

The commands are intentionally decoupled — each does one job — but they share conventions that let them compose:

1. **The project path** is passed to most commands (`-p`/`--projectPath` or a positional argument) and points at the Godot project directory.
2. **The version** is derived from that project's `project.godot` on demand, so you never pass a version string around manually. Bump it once with `godot version --bump` and every downstream command picks up the new value.
3. **Exit codes propagate.** If `godot build` fails, it returns the failing process's exit code, so a `&&`-chained pipeline or a CI step stops before it tries to deploy a broken build.

Continue to [Getting Started](getting-started.md) to install the tool and run a first command, or jump to the [Architecture](architecture.md) for a look under the hood.
