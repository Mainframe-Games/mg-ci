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
    public string? Name { get; set; }
    public string? Location { get; set; }
    public ProjectSettings Settings { get; set; } = new();
    public Prebuild Prebuild { get; set; } = new();
    public BuildTargets BuildTargets { get; set; } = new();
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
        Console.WriteLine($"Loading project: {proj.Location}");
        return proj;
    }

    public void Save()
    {
        var toml = Toml.FromModel(this, new TomlModelOptions { IgnoreMissingProperties = true, });
        File.WriteAllText(Path.Combine(Location!, ".ci", "project.toml"), toml);
        Console.WriteLine($"Saved project: {Location}");
    }
}

public class ProjectSettings
{
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
    public string? Url { get; set; }

    /// <summary>
    /// Thumbnail image for page
    /// </summary>
    public string? Thumbnail { get; set; }

    /// <summary>
    /// Plastic: Changeset ID of last successful build
    /// <para></para>
    /// Git: Sha of last successful build
    /// </summary>
    public string? LastSuccessfulBuild { get; set; }
}

public class Prebuild { }

public class BuildTargets { }

public class Deployment { }

public class HookItemTemplate
{
    public string? Title { get; set; } = "Captain Hook";
    public string? Url { get; set; } = "htts://";
    public bool IsErrorChannel { get; set; }
}
