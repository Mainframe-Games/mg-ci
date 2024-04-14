internal class AppBuild
{
    public string AppID { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public string ContentRoot { get; set; } = string.Empty;
    public string BuildOutput { get; set; } = string.Empty;
    public string Preview { get; set; } = "1";
    public string SetLive { get; set; } = "beta";
    public Dictionary<string, Depot> Depots { get; set; } = new();
}

internal class Depot
{
    public FileMapping FileMapping { get; set; } = new();
}

internal class FileMapping
{
    public string LocalPath { get; set; } = string.Empty;
    public string DepotPath { get; set; } = ".";
    public string Recursive { get; set; } = "1";
}
