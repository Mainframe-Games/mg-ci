using System.Runtime.Serialization;
using Tomlyn;

namespace AvaloniaAppMVVM.Data;

public enum VersionControlType
{
    Git,
    Plastic,
}

public enum GameEngineType
{
    Unity,
    Godot,
}

/// <summary>
/// A container for each workspace
/// </summary>
public class Project
{
    [IgnoreDataMember]
    public string? Location { get; set; } = "C:\\";

    public ProjectSettings Settings { get; set; } = new();
    public Prebuild Prebuild { get; set; } = new();
    public List<UnityBuildTarget> BuildTargets { get; set; } = new();
    public Deployment Deployment { get; set; } = new();
    public List<HookItemTemplate> Hooks { get; set; } = [];

    public static Project Load(string? location)
    {
        if (string.IsNullOrEmpty(location))
            return null!;

        var toml = File.ReadAllText(Path.Combine(location, ".ci", "project.toml"));
        var proj = Toml.ToModel<Project>(
            toml,
            options: new TomlModelOptions { IgnoreMissingProperties = true }
        );
        proj.Location = location;
        Console.WriteLine($"Loading project: {proj.Location}");
        return proj;
    }

    public void Save()
    {
        var toml = Toml.FromModel(this, new TomlModelOptions { IgnoreMissingProperties = true, });
        File.WriteAllText(Path.Combine(Location!, ".ci", "project.toml"), toml);
        Console.WriteLine($"Saved project: {Location}\n{toml}");
    }
}

public class ProjectSettings
{
    public string? ProjectName { get; set; } = "Project";

    /// <summary>
    /// The version control system used for the project
    /// </summary>
    public VersionControlType VersionControl { get; set; }

    /// <summary>
    /// The game engine used for the project
    /// </summary>
    public GameEngineType GameEngine { get; set; }

    /// <summary>
    /// URL to game page
    /// </summary>
    public string? StoreUrl { get; set; } = "https://";

    /// <summary>
    /// Thumbnail image for page
    /// </summary>
    public string? StoreThumbnailUrl { get; set; } = "https://";

    /// <summary>
    /// Plastic: Changeset ID of last successful build
    /// <para></para>
    /// Git: Sha of last successful build
    /// </summary>
    public string? LastSuccessfulBuild { get; set; } = "0";
}

public class Prebuild
{
    public bool BuildNumberStandalone { get; set; }
    public bool BuildNumberIphone { get; set; }
    public bool AndroidVersionCode { get; set; }
}

public class Deployment
{
    public List<string> SteamVdfs { get; set; } = [];
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
