# CI/CD Pipeline

MG-CLI is not only a tool *for* CI/CD — it also has its own CI/CD that publishes each release to NuGet automatically. This page documents that self-publishing pipeline, and then discusses how to use MG-CLI inside *your* game's pipeline.

## The tool's own pipeline

The workflow lives at [`.github/workflows/publish.yml`](https://github.com/Mainframe-Games/mg-ci/blob/main/.github/workflows/publish.yml) and runs on **every push to `main`**, except when the commit message contains `[skip ci]`:

```yaml
on:
  push:
    branches: [main]

jobs:
  publish:
    if: "!contains(github.event.head_commit.message, '[skip ci]')"
```

### What it does, step by step

1. **Checkout & setup .NET 10.**
2. **Restore** dependencies (`dotnet restore`).
3. **Bump the version** by running the tool on itself:
   ```bash
   dotnet run --project MG-CLI/MG-CLI.csproj csproj-versioning MG-CLI/MG-CLI.csproj Version
   ```
   This increments the patch segment of `<Version>` in `MG-CLI.csproj` (see [Versioning](versioning.md#the-tool-version-majorminorpatch)).
4. **Read the new version** back out of the `.csproj` into a step output.
5. **Commit & tag** the bump as `github-actions[bot]`:
   ```bash
   git commit -m "chore: bump version to <ver> [skip ci]"
   git tag "v<ver>"
   git push origin main --tags
   ```
   The `[skip ci]` marker prevents this bot commit from re-triggering the workflow — an infinite loop guard.
6. **Build** in Release (`dotnet build --configuration Release --no-restore`).
7. **Pack** the NuGet package with the computed version (`dotnet pack … -p:Version=<ver>`).
8. **Push to NuGet** (`dotnet nuget push … --skip-duplicate`) using the `NUGET_API_KEY` secret.

### The `[skip ci]` loop guard

This pattern is worth calling out because it is easy to get wrong. The workflow itself pushes a commit (the version bump). Without a guard, that push would trigger the workflow again, which would bump again, and so on. The `[skip ci]` string in the bot's commit message, combined with the `if:` condition on the job, breaks the cycle. You can see the guard commits in the repo history as `chore: bump version to X.Y.Z [skip ci]`.

### Required configuration

To run this pipeline in a fork or your own repo:

- Add a repository secret named **`NUGET_API_KEY`** (Settings → Secrets and variables → Actions) with a valid nuget.org API key.
- Ensure the workflow has `contents: write` permission (already declared in the workflow) so it can push the bump commit and tag.

## Using MG-CLI in *your* game's pipeline

MG-CLI's commands are designed to be dropped into CI steps. Because each returns a meaningful exit code, a failing build or deploy fails the pipeline.

A GitHub Actions sketch for a Godot game might look like:

```yaml
jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with: { fetch-depth: 0 }   # full history + tags, so discord-hook can diff

      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: "10.0.x" }

      - name: Install MG-CLI
        run: dotnet tool install --global mg-cli

      - name: Install Godot
        run: mg-cli godot install 4.4.1

      - name: Bump version
        run: (cd game && mg-cli godot version --bump)

      - name: Build
        run: mg-cli godot build -p ./game -v 4.4.1 -r Linux

      - name: Deploy to Steam
        run: mg-cli steam deploy -p ./game --vdf ./steam/app_build.vdf -u "${{ secrets.STEAM_USER }}" -pw "${{ secrets.STEAM_PASS }}"

      - name: Tag & push
        run: mg-cli commit ./game

      - name: Announce on Discord
        run: mg-cli discord-hook -p ./game -h "${{ secrets.DISCORD_HOOK }}" -s "$STEAM_URL" -l "$LOGO_URL"
```

### Tips for CI use

- **Fetch full history and tags** (`fetch-depth: 0`). `discord-hook` diffs between the two most recent tags to build a changelog, and `commit` pushes tags — both need the tags present.
- **Pass credentials as secrets**, never inline. `steam deploy` takes username/password on the command line; source them from masked CI secrets/environment variables.
- **Watch for interactive prompts.** `itchio setup` runs `butler login` interactively and `steam deploy` pipes stdin through for Steam Guard — plan authentication (persisted Butler creds, Steam Guard handling) so unattended runs do not block.
- **Cache the Godot install** where possible; `godot install` re-downloads the engine and templates each time otherwise.
- **Rely on exit codes.** Keep steps separate (as above) or chain with `&&` so a failure stops the pipeline before it deploys a bad build.

See [Getting Started](getting-started.md) for the equivalent commands run locally, and the [Command Reference](commands.md) for every flag.
