using ServerShared.VersionControls;
using Tomlyn;
using Tomlyn.Model;

namespace ServerShared;

public enum GameEngine
{
    Unity,
    Godot
}

public enum VersionControlType
{
    Git, Plastic
}

public class Workspace(string projectPath)
{
    public string ProjectPath => projectPath;
    public GameEngine Engine { get; } = GetEngine(projectPath ?? throw new NullReferenceException());
    public VersionControlType VersionControl { get; } =
        GetVersionControl(projectPath ?? throw new NullReferenceException());
    public string? Branch { get; init; } = "main";
    public string? GitUrl { get; init; }

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
    }

    private void UpdatePlastic()
    {
        throw new NotImplementedException();
    }

    private void UpdateGit()
    {
        var git = new Git(projectPath, GitUrl!, Branch!, "", "");
        git.Update();
    }

    public string[] GetChangeLog()
    {
        switch (VersionControl)
        {
            case VersionControlType.Git:
                var git = new Git(projectPath, GitUrl!, Branch!, "", "");
                return git.GetChangeLog();
                
            case VersionControlType.Plastic:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// </summary>
    /// <returns>New Version</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public string VersionBump()
    {
        return Engine switch
        {
            GameEngine.Unity => UnityVersionBump(),
            GameEngine.Godot => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private string UnityVersionBump()
    {
        var standalone = true;
        var android = true;
        var ios = true;
        
        var unityVersionBump = new UnityVersionBump(
            ProjectPath,
            standalone,
            android,
            ios);
        
        var fullVersion = unityVersionBump.Run();
        
        // workspace.SaveBuildVersion(fullVersion);

        // commit file
        var git = new Git(projectPath, GitUrl!, Branch!, "", "");
        var sha = git.GetLatestCommitHash();
        git.Commit(
            $"_Build Version: {fullVersion} | sha: {sha}",
            [unityVersionBump.ProjectSettingsPath]
        );
        
        return fullVersion;
    }
}
