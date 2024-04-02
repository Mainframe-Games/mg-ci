using LibGit2Sharp;
using SharedLib;

namespace OffloadServer.Utils;

internal class Workspace(string projectPath)
{
    public string ProjectPath => projectPath;
    public string? Engine { get; } = GetEngine(projectPath ?? throw new NullReferenceException());
    public string? VersionControl { get; } =
        GetVersionControl(projectPath ?? throw new NullReferenceException());
    public string? Branch { get; init; } = "main";
    public string? GitUrl { get; init; }

    #region Engine

    private static string GetEngine(string projectPath)
    {
        if (IsUnity(projectPath))
            return "Unity";

        if (IsGodot(projectPath))
            return "Godot";

        return "Unknown";
    }

    private static bool IsUnity(string projectPath)
    {
        var root = new DirectoryInfo(projectPath);
        var sceneFiles = root.GetFiles("*.unity", SearchOption.AllDirectories);
        return sceneFiles.Length > 0;
    }

    private static bool IsGodot(string projectPath)
    {
        var root = new DirectoryInfo(projectPath);
        var sceneFiles = root.GetFiles("*.tscn", SearchOption.AllDirectories);
        return sceneFiles.Length > 0;
    }

    #endregion

    #region Version Control

    private static string GetVersionControl(string projectPath)
    {
        if (IsPlastic(projectPath))
            return "Plastic";

        if (IsGit(projectPath))
            return "Git";

        return "Unknown";
    }

    private static bool IsGit(string projectPath)
    {
        var root = new DirectoryInfo(projectPath);
        var gitDir = new DirectoryInfo(Path.Combine(root.FullName, ".git"));
        return gitDir.Exists;
    }

    private static bool IsPlastic(string projectPath)
    {
        var root = new DirectoryInfo(projectPath);
        var plasticDir = new DirectoryInfo(Path.Combine(root.FullName, ".plastic"));
        return plasticDir.Exists;
    }

    #endregion

    public void Update()
    {
        switch (VersionControl)
        {
            case "Plastic":
                UpdatePlastic();
                break;

            case "Git":
                UpdateGit();
                break;
        }
    }

    private void UpdatePlastic()
    {
        throw new NotImplementedException();
    }

    private void UpdateGit()
    {
        if (!Repository.IsValid(projectPath))
        {
            Repository.Clone(
                GitUrl,
                projectPath,
                new CloneOptions { RecurseSubmodules = true, BranchName = Branch }
            );

            // init LFS
            Cmd.Run("git", "lfs install", projectPath);
        }

        // clear
        using var repo = new Repository(projectPath);
        repo.Reset(ResetMode.Hard, repo.Head.Tip);

        // switch branch
        Cmd.Run("git", $"switch {Branch}", projectPath);

        // pull
        Commands.Pull(
            repo,
            new Signature("build-bot", "email@build.bot", DateTimeOffset.Now),
            new PullOptions
            {
                FetchOptions = new FetchOptions
                {
                    CredentialsProvider = (url, fromUrl, types) =>
                    {
                        return types switch
                        {
                            SupportedCredentialTypes.UsernamePassword
                                => new UsernamePasswordCredentials
                                {
                                    Username = "build-bot",
                                    Password = "password"
                                },
                            SupportedCredentialTypes.Default => new DefaultCredentials(),
                            _ => throw new ArgumentOutOfRangeException(nameof(types), types, null)
                        };
                    }
                }
            }
        );
        Cmd.Run("git", "lfs pull", projectPath);
    }
}
