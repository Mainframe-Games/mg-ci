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
}
