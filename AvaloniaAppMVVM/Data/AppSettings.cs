using System.Runtime.Serialization;
using Tomlyn;

namespace AvaloniaAppMVVM.Data;

public class AppSettings
{
    /// <summary>
    /// Last project loaded location.
    /// </summary>
    public string? LastProjectLocation { get; set; }

    /// <summary>
    /// All projects loaded.
    /// </summary>
    public List<string?> LoadedProjectPaths { get; set; } = [];

    [IgnoreDataMember]
    public static AppSettings Singleton { get; private set; } = new();

    public void Save()
    {
        var toml = Toml.FromModel(this);
        File.WriteAllText("settings.toml", toml);
        Console.WriteLine("Saved settings");
    }

    public static AppSettings Load()
    {
        if (!File.Exists("settings.toml"))
            return Singleton;

        var toml = File.ReadAllText("settings.toml");
        Singleton = Toml.ToModel<AppSettings>(toml);
        return Singleton;
    }
}
