using System.CommandLine;

namespace MG_CLI;

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

    private static FileInfo GetProjectSettingsFile(string fullPath)
    {
        var dirInfo = new DirectoryInfo(fullPath);
        var file = dirInfo
            .GetFiles("*.godot", SearchOption.AllDirectories)
            .First();
        return file;
    }

    private static async Task SetVersion(string fullPath, CancellationToken token)
    {
        var file = GetProjectSettingsFile(fullPath);
        var lines = await File.ReadAllLinesAsync(file.FullName, token);

        for (var i = 0; i < lines.Length; i++)
        {
            var key = lines[i].Split("=")[0].Trim('"');
            var value = lines[i].Split("=")[^1].Trim('"');
            
            if (key != "config/version")
                continue;

            var verSplit = value.Split(".");
            var buildNumInt = int.Parse(verSplit[^1]);
            
            verSplit[0] = DateTime.Now.ToString("yyyy");
            verSplit[1] = DateTime.Now.Month.ToString();
            verSplit[^1] = (++buildNumInt).ToString();
                
            var newVer = string.Join(".", verSplit);
            Log.Print($"New Version: {newVer}");
            
            lines[i] = $"{key}=\"{newVer}\"";
        }
        
        await FileEx.WriteAllLinesAsync(file.FullName, lines);
    }

    public static string GetVersion(string path)
    {
        var file = GetProjectSettingsFile(path);
        var lines = File.ReadAllLines(file.FullName);

        var verStr = "";
        var verStrSuffix = "";

        foreach (var line in lines)
        {
            var key = line.Split("=")[0].Trim('"');
            var value = line.Split("=")[^1].Trim('"');
            
            if (key == "config/version")
                verStr = value;

            if (key == "config/version_suffix")
                verStrSuffix = value;
        }
        
        return verStr + verStrSuffix;
    }
}