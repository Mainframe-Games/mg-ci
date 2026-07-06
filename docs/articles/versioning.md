# Versioning

MG-CLI deals with **two distinct versions**, and it is important not to confuse them:

1. **The game's version** — stored in `project.godot`, using a date-based `YYYY.MM.BUILD` scheme. This is what `godot version`, `commit`, `discord-hook`, `steam deploy`, and `itchio deploy` all read and stamp onto their outputs.
2. **The tool's own version** — the `<Version>` of the `mg-cli` NuGet package, using standard `MAJOR.MINOR.PATCH` [SemVer](https://semver.org/), bumped by `csproj-versioning` in the tool's own CI.

## The game version: `YYYY.MM.BUILD`

The game's version lives in the Godot project settings file under two keys:

```ini
config/version="2026.7.42"
config/version_suffix="-beta"   ; optional
```

- **`YYYY`** — the four-digit year, set from the current date on each bump.
- **`MM`** — the month (no leading zero), set from the current date on each bump.
- **`BUILD`** — a monotonically increasing build number, incremented by one on each bump.
- **`config/version_suffix`** — an optional suffix (e.g. `-beta`) that is preserved and appended when the version is read.

### Reading the version

```bash
cd your-godot-project
mg-cli godot version
# → 2026.7.42-beta   (version + suffix, if a suffix is set)
```

Internally, `GodotVersion.GetVersion(projectPath)` is a **static method** other commands call directly. It returns `config/version` concatenated with `config/version_suffix`. This is the single point through which the whole tool learns the game version — no command takes a version as an argument.

### Bumping the version

```bash
cd your-godot-project
mg-cli godot version --bump
```

On a bump, MG-CLI:

1. Locates the `.godot` settings file (recursive search from the current directory).
2. Finds the `config/version` line and splits it on `.`.
3. Sets the first segment to the current **year**, the second to the current **month**, and increments the **last** segment (the build number).
4. Writes the file back as **UTF-8 without a BOM** (via [`FileEx.WriteAllLinesAsync`](utilities.md#fileex--safe-writes-and-chmod)).

> [!NOTE]
> Because the year and month are always refreshed from the clock, a version like `2025.11.40` bumped in July 2026 becomes `2026.7.41`. The build number is the only strictly monotonic part; the date segments track when the bump happened.

### Where the game version flows

Once bumped, the version propagates automatically:

| Command | How it uses the version |
|---|---|
| `commit` | Commit message `_Build Version: <version>` and git tag `v<version>`. |
| `discord-hook` | Embed title `New Build Available! | <version>`. |
| `steam deploy` | Written into the VDF `Desc` field before upload. |
| `itchio deploy` | Passed to Butler as `--userversion <version>`. |

This is the core reason the tool feels cohesive: bump once, and the tag, the storefront metadata, and the announcement all agree.

## The tool version: `MAJOR.MINOR.PATCH`

The `mg-cli` package itself is versioned with standard SemVer in [`MG-CLI.csproj`](https://github.com/Mainframe-Games/mg-ci/blob/main/MG-CLI/MG-CLI.csproj):

```xml
<Version>1.0.26</Version>
```

The `csproj-versioning` command bumps the **patch** segment of a chosen property:

```bash
mg-cli csproj-versioning MG-CLI/MG-CLI.csproj Version
# 1.0.26 → 1.0.27
```

It loads the `.csproj` as XML, increments the third segment of the named property (default `AssemblyVersion`), and saves. The tool's [CI pipeline](cicd.md) runs this on every push to `main` so each publish gets a unique version — see [CI/CD Pipeline](cicd.md) for the full flow.

> [!TIP]
> `csproj-versioning` is general-purpose: it will bump any dotted `MAJOR.MINOR.PATCH` value in any `<PropertyGroup>` element you name. It is only *conventionally* tied to the tool's release process.

## Commit-message convention

The `commit` command writes messages prefixed with an underscore: `_Build Version: <version>`. This is deliberate — `discord-hook` **skips any commit whose message starts with `_`** when building its changelog, so the automated version-bump commits do not clutter the release notes players see. Keep your own meaningful commits free of a leading underscore and they will appear in the changelog.
