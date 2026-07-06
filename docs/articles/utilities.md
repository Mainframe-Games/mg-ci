# Utilities

The `MG-CLI/Utils/` folder holds the shared, mostly-static helper layer that every command builds on. These types have no command-line surface of their own; they exist to keep the commands small, consistent, and cross-platform. This page explains what each one does and why it exists.

## `Log` — colored logging with optional file capture

[`Log`](https://github.com/Mainframe-Games/mg-ci/blob/main/MG-CLI/Utils/Log.cs) is the single output channel for the whole tool. It wraps [Spectre.Console](https://spectreconsole.net/) to write severity-colored text and, optionally, tee that same output to a log file.

**Severity API** (each writes a color-coded line and forwards to the file sink at a mapped level):

| Method | Color | File log level |
|---|---|---|
| `Log.Print(msg)` | White | Debug |
| `Log.Success(msg)` | Green | Info |
| `Log.PrintWarning(msg)` | Yellow | Warning |
| `Log.PrintError(msg)` | Red | Error |
| `Log.Logo()` | Cyan Figlet banner | — |

**File sink.** A command can begin capturing output to a file:

```csharp
Log.CreateLogFile("builds/Logs/Windows.log", LogLevel.Debug); // minimum level to record
// ... work; all Log.* calls and piped subprocess output are teed to the file ...
Log.StopLoggingToFile();
```

Key behaviors:

- The `LogLevel` enum (`Debug < Info < Warning < Error`) sets the **minimum** level written to the file; console output is unaffected.
- The stream is opened with `AutoFlush` so a crash still leaves a usable log.
- **ANSI escape codes are stripped** before writing to disk (via a compiled `GeneratedRegex`), so log files are clean plain text even though the console is colored.

`godot build` uses per-preset log files (`builds/Logs/<preset>.log`); `godot import` captures to `godot-import.log` at `Warning` level.

## `CliWrapExtensions` — consistent process I/O

[`CliWrapExtensions`](https://github.com/Mainframe-Games/mg-ci/blob/main/MG-CLI/Utils/CliWrapExtensions.cs) makes every subprocess log the same way. It defines two shared [CliWrap](https://github.com/Tyrrrz/CliWrap) `PipeTarget`s and one extension method:

```csharp
command
    .WithStandardOutputPipe(StdOutPipe)   // → Log.Print
    .WithStandardErrorPipe(StdErrorPipe); // → Log.PrintWarning if line starts with "warning", else Log.PrintError
```

`WithCustomPipes()` bundles both, and is called on nearly every `Cli.Wrap(...)` in the codebase:

```csharp
await Cli.Wrap("git").WithArguments("add .").WithCustomPipes().ExecuteAsync(token);
```

The payoff: subprocess output is colored and, when a file sink is active, captured — without each command wiring up piping itself.

## `Web` — downloads with a progress bar

[`Web.DownloadFileWithProgressAsync(url, destinationPath)`](https://github.com/Mainframe-Games/mg-ci/blob/main/MG-CLI/Utils/Web.cs) streams an HTTP download to disk while rendering a Spectre progress bar.

- Uses `HttpCompletionOption.ResponseHeadersRead` and an 8 KB buffer to stream large files without buffering them entirely in memory.
- Drives the progress bar from the `Content-Length` header; if the server does not report a length, it retries the download.
- Used by every "setup/install" command: `godot install`, `steam setup`, `itchio setup`.

## `Zip` — unzip with a progress bar

[`Zip.UnzipFileAsync(zipPath, extractPath)`](https://github.com/Mainframe-Games/mg-ci/blob/main/MG-CLI/Utils/Zip.cs) extracts a `.zip` archive with a progress bar driven by cumulative entry size.

- Recreates the directory structure, skips directory entries, and copies each file with a configurable buffer (default ~80 KB).
- Used to unpack the Godot engine and export templates, the Steamworks SDK, Butler, and macOS `.app` bundles produced by `godot build`.

## `FileEx` — safe writes and chmod

[`FileEx`](https://github.com/Mainframe-Games/mg-ci/blob/main/MG-CLI/Utils/FileEx.cs) centralizes two file concerns:

- **`WriteAllLinesAsync(path, lines)`** — writes UTF-8 **without a BOM**. This matters for the config files MG-CLI edits (`project.godot`, `.vdf`): a stray BOM can break tools that parse them. Every command that rewrites a settings file goes through this.
- **`Chmod(filePath)`** — a `chmod +x` helper that is a **no-op on Windows** and shells out on Linux/macOS. Used after downloading engine/tool binaries so they are executable.

## `DirectoryUtil` — delete/recreate helpers

[`DirectoryUtil`](https://github.com/Mainframe-Games/mg-ci/blob/main/MG-CLI/Utils/DirectoryUtil.cs) provides two small guards used throughout the install and build flows:

- `DeleteDirectoryExists(path, recreate)` — delete a directory if present, optionally recreating it empty (used to clean output/temp dirs before writing to them).
- `CreateDirectoryIfNotExists(path)` — idempotent create.

## `GodotUtils` — locate the settings file

[`GodotUtils.GetProjectSettingsFile(fullPath)`](https://github.com/Mainframe-Games/mg-ci/blob/main/MG-CLI/Utils/GodotUtils.cs) finds the first `*.godot` settings file at or below a given path (recursive search, first match). This is why the project-path arguments are forgiving: you can point at the project root and the version/settings commands still find `project.godot`.

## `StringExtensions` — `ToSnakeCase()`

[`StringExtensions.ToSnakeCase()`](https://github.com/Mainframe-Games/mg-ci/blob/main/MG-CLI/Utils/StringExtensions.cs) converts a string to `snake_case` (splitting camelCase boundaries, normalizing separators, collapsing repeats), falling back to `"anim"` for empty input. A small utility kept in the shared layer.

---

Together these utilities encode the tool's cross-cutting conventions — consistent logging, progress feedback, BOM-free writes, and platform-safe permissions — so the command classes can stay focused on orchestration. See [Architecture](architecture.md#cross-cutting-concerns) for how they tie into the command pattern.
