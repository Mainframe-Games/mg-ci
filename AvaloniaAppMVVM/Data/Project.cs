using System.Runtime.Serialization;
using AvaloniaAppMVVM.Utils;
using Tomlyn;

namespace AvaloniaAppMVVM.Data;

public enum VersionControlType
{
    Git,
    Plastic,
}

/// <summary>
/// A container for each workspace
/// </summary>
public class Project
{
    [IgnoreDataMember]
    public string? Location { get; set; }

    public string? Guid { get; set; } = System.Guid.NewGuid().ToString();
    public ProjectSettings Settings { get; set; } = new();
    public Prebuild Prebuild { get; set; } = new();
    public List<UnityBuildTarget> BuildTargets { get; set; } = [];
    public Deployment Deployment { get; set; } = new();
    public List<HookItemTemplate> Hooks { get; set; } = [];

    [IgnoreDataMember]
    private static readonly Dictionary<string, Project> _projectsMap = new();

    public static Project? Load(string? location)
    {
        if (string.IsNullOrEmpty(location))
            return null!;

        // return cached project if exists
        if (_projectsMap.TryGetValue(location, out var cachedProj))
        {
            Console.WriteLine($"Loading cached project: {cachedProj.Location}");
            return cachedProj;
        }

        var projectToml = Path.Combine(location, ".ci", "project.toml");
        if (!File.Exists(projectToml))
            return null;

        var toml = File.ReadAllText(projectToml);
        Project proj;
        try
        {
            proj = Toml.ToModel<Project>(
                toml,
                options: new TomlModelOptions { IgnoreMissingProperties = true },
                sourcePath: projectToml
            );
            proj.Location = location;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }

        _projectsMap.Add(location, proj);

        Console.WriteLine($"Loading project: {proj.Location}");
        return proj;
    }

    public void Save()
    {
        if (!Directory.Exists(Location))
        {
            Console.WriteLine($"Project location does not exist: {Location}");
            return;
        }

        var toml = Toml.FromModel(this, new TomlModelOptions { IgnoreMissingProperties = true, });
        var projTomlPath = Path.Combine(Location!, ".ci", "project.toml");
        File.WriteAllText(projTomlPath, toml);
        // Console.WriteLine($"Saved project: {projTomlPath}");
    }
}

public class ProjectSettings
{
    public string? ProjectName { get; set; }

    /// <summary>
    /// The version control system used for the project
    /// </summary>
    public VersionControlType VersionControl { get; set; }

    public string? GitRepositoryUrl { get; set; }
    public string? GitRepositorySubPath { get; set; }

    public string? PlasticWorkspaceName { get; set; }

    /// <summary>
    /// URL to game page
    /// </summary>
    public string? StoreUrl { get; set; } = "https://";

    /// <summary>
    /// Thumbnail image for page
    /// </summary>
    public string? StoreThumbnailUrl { get; set; } = "https://";
}

public class Prebuild
{
    public bool BuildNumberStandalone { get; set; } = true;
    public bool BuildNumberIphone { get; set; }
    public bool AndroidVersionCode { get; set; }
}

public class Deployment
{
    public List<AppBuild> SteamAppBuilds { get; set; } = [];
    public bool AppleStore { get; set; }
    public bool GoogleStore { get; set; }
    public bool Clanforge { get; set; }
    public bool AwsS3 { get; set; }
}

public class HookItemTemplate
{
    public string? Title { get; set; } = "Captain Hook";
    public string? Url { get; set; } = "htts://";
    public bool IsErrorChannel { get; set; }
}
