using System.CommandLine;
using CLI.Utils;

namespace CLI.Commands;

public class GodotVersioning : Command
{
    private readonly Option<string> _projectPath = new("--projectPath", "-p")
    {
        HelpName = "Path to project.godot"
    };
    
    public GodotVersioning() : base("godot-versioning", "Increments the version in the project.godot file.")
    {
        Add(_projectPath);
        SetAction(async (result, token) =>
        {
            var path = result.GetRequiredValue(_projectPath);
            await Run(path, token);
        });
    }

    private static async Task Run(string path, CancellationToken token)
    {
        var fullPath = Path.GetFullPath(path);
        Log.WriteLine($"ProjectPath: {fullPath}");
        var dirInfo = new DirectoryInfo(fullPath);
        var files = dirInfo.GetFiles("*.godot", SearchOption.AllDirectories);
        
        if (files.Length == 0)
            throw new Exception("Could not find version in project.godot");
        
        foreach (var file in files)
        {
            var lines = await File.ReadAllLinesAsync(file.FullName, token);

            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains("config/version"))
                    continue;

                var verStr = lines[i].Split("=")[^1].Trim('"');
                var verSplit = verStr.Split(".");
                var buildNumInt = int.Parse(verSplit[^1]);
                buildNumInt++;
                
                verSplit[0] = DateTime.Now.Year.ToString();
                verSplit[1] = DateTime.Now.Month.ToString();
                verSplit[^1] = buildNumInt.ToString();
                
                var newVer = string.Join(".", verSplit);
                lines[i] = $"config/version=\"{newVer}\"";
                await FileWriter.WriteAllLinesAsync(file.FullName, lines);
                break;
            }
        }
    }

    public static string GetVersion(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        var files = dirInfo.GetFiles("*.godot", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file.FullName);

            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains("config/version"))
                    continue;

                var verStr = lines[i].Split("=")[^1].Trim('"');
                return verStr;
            }
        }
        
        throw new Exception("Could not find version in project.godot");
    }
}