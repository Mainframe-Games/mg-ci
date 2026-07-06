# Development

This page is for contributors working on MG-CLI itself: how to build and install it locally, how the project is laid out, and how to add a new command.

## Prerequisites

- **[.NET 10.0 SDK](https://dotnet.microsoft.com/download)**.
- Optionally **[`just`](https://github.com/casey/just)** for the one-line dev workflow.

## Building and installing locally

The primary development loop is "pack and install as a global tool," so you can run the `mg-cli` command exactly as an end user would.

Using `just`:

```bash
just pack
```

Or the equivalent manual steps:

```bash
dotnet pack ./MG-CLI/MG-CLI.csproj -c Release
dotnet tool uninstall --global mg-cli || true
dotnet tool install --global mg-cli --add-source "./MG-CLI/nupkg" --no-cache
```

The `--add-source` points at the local `nupkg` output and `--no-cache` ensures you pick up the freshly-packed build rather than a cached one.

For a plain compile without packaging:

```bash
dotnet restore
dotnet build --configuration Release --no-restore
```

To run without installing:

```bash
dotnet run --project MG-CLI/MG-CLI.csproj -- godot version --bump
```

(Everything after `--` is passed to the tool.)

> [!NOTE]
> There are **no automated tests** in this project. Verify changes by packing, installing, and running the affected commands against a real Godot project.

## Project layout

```text
MG-CLI/
├── Program.cs                 composition root — registers top-level commands
├── MG-CLI.csproj              packaging + dependencies (targets net10.0)
├── Commands/
│   ├── Git/Commit.cs
│   ├── Godot/                 GodotCommands + Import/Build/Version/Install/ProjectSettings
│   ├── Hooks/DiscordHook.cs
│   ├── Itchio/                ItchioCommands + ButlerSetup/Deploy
│   ├── Steam/                 SteamCommands + CmdSetup/Deploy
│   └── Versioning/CsprojVersioning.cs
├── DigitalOcean/DigitalOcean.cs
└── Utils/                     Log, CliWrapExtensions, Web, Zip, FileEx, DirectoryUtil, GodotUtils, StringExtensions
```

See [Architecture](architecture.md) for how these fit together and [Utilities](utilities.md) for the helper layer.

## Coding conventions

- **One class per command**, subclassing `System.CommandLine.Command`. Follow the existing pattern exactly (see below).
- **Alias the CLI `Command` type** when a file also uses CliWrap:
  ```csharp
  using Command = System.CommandLine.Command;
  ```
- **Shell out through CliWrap with `.WithCustomPipes()`** so subprocess output flows through the shared [logging layer](utilities.md#log--colored-logging-with-optional-file-capture). Thread the handler's `CancellationToken` into `ExecuteAsync`.
- **Check exit codes and short-circuit.** Return the failing subprocess's exit code so the command composes safely in scripts.
- **Read the version, never accept it.** Use `GodotVersion.GetVersion(projectPath)` rather than adding a version argument.
- **Write settings files via `FileEx.WriteAllLinesAsync`** (UTF-8, no BOM).
- **Branch on `OperatingSystem.Is*()`** for platform-specific paths/behavior, and throw `PlatformNotSupportedException` for the fallthrough.
- **Nullable reference types and implicit usings are enabled** — honor the existing style.

## Adding a command

Adding a command is mechanical. Suppose you want `mg-cli godot rename`:

1. **Create the class** in the appropriate folder (`Commands/Godot/GodotRename.cs`):

   ```csharp
   using System.CommandLine;

   namespace MG_CLI;

   public class GodotRename : Command
   {
       private readonly Option<string> _name = new("--name", "-n")
       {
           HelpName = "The new project name"
       };

       public GodotRename() : base("rename", "Renames the Godot project.")
       {
           Add(_name);
           SetAction(Run);
       }

       private async Task<int> Run(ParseResult result, CancellationToken token)
       {
           var name = result.GetRequiredValue(_name);
           // ... do the work, return an exit code ...
           return 0;
       }
   }
   ```

2. **Register it.**
   - For a command in an existing family, add it to that container's constructor — e.g. `GodotCommands` calls `Add(new GodotRename());`.
   - For a new top-level command, add it to the `RootCommand` initializer in [`Program.cs`](https://github.com/Mainframe-Games/mg-ci/blob/main/MG-CLI/Program.cs).

3. **Pack, install, and try it:**

   ```bash
   just pack
   mg-cli godot rename --help
   ```

That's the whole process — no registration tables or DI wiring beyond the two touch points above.

## Releasing

Releases are automated: pushing to `main` triggers the [publish workflow](cicd.md), which bumps the patch version, tags it, and pushes the package to NuGet. You generally do **not** bump `<Version>` by hand — let CI do it. Include `[skip ci]` in a commit message if you need to push to `main` without triggering a release.

See [CI/CD Pipeline](cicd.md) for the full workflow and required secrets.
