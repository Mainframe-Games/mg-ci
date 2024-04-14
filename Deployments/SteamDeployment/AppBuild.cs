using System.Text;

namespace SteamDeployment;

public class AppBuild
{
    public string AppID { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public string ContentRoot { get; set; } = string.Empty;
    public string BuildOutput { get; set; } = string.Empty;
    public string Preview { get; set; } = "1";
    public string SetLive { get; set; } = "beta";
    public Dictionary<string, Depot> Depots { get; set; } = new();

    public string Build()
    {
        var str = new StringBuilder();
        str.AppendLine("\"AppBuild\"");
        str.AppendLine("{");
        str.AppendLine($"\t\"AppID\" \"{AppID}\"");
        str.AppendLine($"\t\"Desc\" \"{Desc}\"");
        str.AppendLine($"\t\"ContentRoot\" \"{ContentRoot}\"");
        str.AppendLine($"\t\"BuildOutput\" \"{BuildOutput}\"");
        str.AppendLine($"\t\"Preview\" \"{Preview}\"");
        str.AppendLine($"\t\"SetLive\" \"{SetLive}\"");
        str.AppendLine();

        str.AppendLine("\t\"Depots\"");
        str.AppendLine("\t{");
        foreach (var (depotId, depot) in Depots)
        {
            str.AppendLine($"\t\t\"{depotId}\"");
            str.AppendLine("\t\t{");
            str.AppendLine($"\t\t\t\"LocalPath\" \"{depot.FileMapping.LocalPath}\"");
            str.AppendLine($"\t\t\t\"DepotPath\" \"{depot.FileMapping.DepotPath}\"");
            str.AppendLine($"\t\t\t\"recursive\" \"{depot.FileMapping.Recursive}\"");
            str.AppendLine("\t\t}");
        }

        str.AppendLine("\t}");
        str.AppendLine("}");
        var outStr = str.ToString();
        return outStr;
    }
}

public class Depot
{
    public FileMapping FileMapping { get; set; } = new();
}

public class FileMapping
{
    public string LocalPath { get; set; } = string.Empty;
    public string DepotPath { get; set; } = ".";
    public string Recursive { get; set; } = "1";
}
