namespace AvaloniaAppMVVM.Data;

public class AppBuild
{
    public string AppID { get; set; } = string.Empty;
    public List<Depot> Depots { get; set; } = [];
}

public class Depot
{
    public string Id { get; set; } = string.Empty;
    public string BuildTargetName { get; set; } = string.Empty;
}
