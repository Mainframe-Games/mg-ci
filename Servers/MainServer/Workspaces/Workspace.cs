using MainServer.Configs;
using MainServer.VersionBumping;
using MainServer.VersionControls;
using Tomlyn;
using Tomlyn.Model;

namespace MainServer.Workspaces;

internal enum GameEngine
{
    Unity,
    Godot
}

internal enum VersionControlType
{
    Git,
    Plastic
}

internal class Workspace(string projectPath, ServerConfig serverConfig)
{
    public string ProjectPath => projectPath;
    public GameEngine Engine { get; private set; }
    public VersionControlType VersionControl { get; private set; }
    public string? Branch { get; init; } = "main";
    public string? GitUrl { get; init; }
    public string? SetLive { get; set; } = "beta";

    private Git GitProcess
    {
        get
        {
            var gitConfig = serverConfig.Git ?? throw new NullReferenceException();
            return new Git(
                projectPath,
                GitUrl!,
                Branch!,
                gitConfig.Username!,
                gitConfig.AccessToken!
            );
        }
    }

    public TomlTable GetProjectToml()
    {
        var projectTomlPath = Path.Combine(projectPath, ".ci", "project.toml");
        var toml = File.ReadAllText(projectTomlPath);
        return Toml.ToModel(toml);
    }

    #region Engine

    private static GameEngine GetEngine(string projectPath)
    {
        if (IsUnity(projectPath))
            return GameEngine.Unity;

        if (IsGodot(projectPath))
            return GameEngine.Godot;

        throw new Exception("Game engine not found in project directory.");
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
        var sceneFiles = root.GetFiles("*.godot", SearchOption.AllDirectories);
        return sceneFiles.Length > 0;
    }

    #endregion

    #region Version Control

    private static VersionControlType GetVersionControl(string projectPath)
    {
        if (IsGit(projectPath))
            return VersionControlType.Git;

        if (IsPlastic(projectPath))
            return VersionControlType.Plastic;

        throw new Exception("Version Control not found");
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
            case VersionControlType.Plastic:
                UpdatePlastic();
                break;

            case VersionControlType.Git:
                UpdateGit();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        Engine = GetEngine(projectPath);
        VersionControl = GetVersionControl(projectPath);
    }

    private void UpdatePlastic()
    {
        throw new NotImplementedException();
    }

    private void UpdateGit()
    {
        GitProcess.Update();
    }

    public string[] GetChangeLog()
    {
        var changelog = VersionControl switch
        {
            VersionControlType.Git => GitProcess.GetChangeLog(),
            VersionControlType.Plastic => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException()
        };

        Console.WriteLine("Changelog:");
        foreach (var log in changelog)
            Console.WriteLine($"  {log}");

        return changelog;
    }

    /// <summary>
    /// </summary>
    /// <returns>New Version</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public string VersionBump(bool standalone, bool android, bool ios)
    {
        return Engine switch
        {
            GameEngine.Unity => UnityVersionBump(standalone, android, ios),
            GameEngine.Godot => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private string UnityVersionBump(bool standalone, bool android, bool ios)
    {
        var unityVersionBump = new UnityVersionBump(ProjectPath, standalone, android, ios);
        var fullVersion = unityVersionBump.Run();

        // commit file
        var git = GitProcess;
        var sha = git.GetLatestCommitHash();
        git.Commit(
            $"_Build Version: {fullVersion} | sha: {sha}",
            [unityVersionBump.ProjectSettingsPath]
        );

        return fullVersion;
    }

    public void Tag(string tag)
    {
        switch (VersionControl)
        {
            case VersionControlType.Git:
                GitProcess.Tag(tag);
                break;
            case VersionControlType.Plastic:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
