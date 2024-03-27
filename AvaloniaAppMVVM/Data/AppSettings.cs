using System.Runtime.Serialization;
using Tomlyn;

namespace AvaloniaAppMVVM.Data;

public class AppSettings
{
    [IgnoreDataMember]
    public static AppSettings Singleton { get; private set; } = new();
    
    /// <summary>
    /// Last project loaded location.
    /// </summary>
    public string? LastProjectLocation { get; set; }

    /// <summary>
    /// All projects loaded.
    /// </summary>
    public List<string?> LoadedProjectPaths { get; set; } = [];
    
    public string? ServerIp { get; set; } = "localhost";
    public ushort ServerPort { get; set; } = 8080;

    public void Save()
    {
        var toml = Toml.FromModel(this);
        File.WriteAllText("settings.toml", toml);
        Console.WriteLine("Saved settings: settings.toml");
        Console.WriteLine(toml);
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
