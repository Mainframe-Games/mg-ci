using System.CommandLine;

namespace MG;

public class GodotVersioning : Command
{
    private readonly Option<string> _projectPath = new("--projectPath", "-p")
    {
        HelpName = "Path to project.godot"
    };
    
    public GodotVersioning() : base("godot-versioning", "Increments the version in the project.godot file.")
    {
        Add(_projectPath);
        SetAction(Run);
    }

    private async Task Run(ParseResult result, CancellationToken token)
    {
        var path = result.GetRequiredValue(_projectPath);
        var fullPath = Path.GetFullPath(path);
        Log.Print($"ProjectPath: {fullPath}");
        await SetVersion(fullPath, token);
    }

    private static async Task SetVersion(string fullPath, CancellationToken token)
    {
        var dirInfo = new DirectoryInfo(fullPath);
        var file = dirInfo
            .GetFiles("*.godot", SearchOption.AllDirectories)
            .First();
        
        var lines = await File.ReadAllLinesAsync(file.FullName, token);

        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("config/version"))
                continue;

            var verStr = lines[i].Split("=")[^1].Trim('"');
            var verSplit = verStr.Split(".");
            var buildNumInt = int.Parse(verSplit[^1]);
                
            verSplit[0] = DateTime.Now.ToString("yyyy");
            verSplit[1] = DateTime.Now.Month.ToString();
            verSplit[^1] = (++buildNumInt).ToString();
                
            var newVer = string.Join(".", verSplit);
            Log.Print($"New Version: {newVer}");
                
            lines[i] = $"config/version=\"{newVer}\"";
            await FileEx.WriteAllLinesAsync(file.FullName, lines);
            break;
        }
    }

    public static string GetVersion(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        var file = dirInfo
            .GetFiles("*.godot", SearchOption.AllDirectories)
            .First();
        
        var lines = File.ReadAllLines(file.FullName);

        var verStr = "";
        var verStrSuffix = "";

        foreach (var line in lines)
        {
            if (line.Contains("config/version"))
                verStr = line.Split("=")[^1].Trim('"');

            if (line.Contains("config/version_suffix"))
                verStrSuffix = line.Split("=")[^1].Trim('"');
        }
        
        return verStr + verStrSuffix;
    }
}