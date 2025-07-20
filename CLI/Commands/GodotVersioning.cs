using System.CommandLine;
using CLI.Utils;

namespace CLI.Commands;

public class GodotVersioning : ICommand
{
    public Command BuildCommand()
    {
        var command = new Command("godot-versioning");
        var projectPath = new Option<string>("--projectPath", "-p")
        {
            HelpName = "Path to project.godot"
        };
        command.Add(projectPath);
        
        // Set the handler directly
        command.SetAction(async (result, token) =>
        {
            try
            {
                var path = result.GetRequiredValue(projectPath);
                await Run(path);
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return 1;
            }
            
            return 0;
        });

        return command;
    }

    private static async Task Run(string path)
    {
        var fullPath = Path.GetFullPath(path);
        Log.WriteLine($"ProjectPath: {fullPath}");
        var dirInfo = new DirectoryInfo(fullPath);
        var files = dirInfo.GetFiles("*.godot", SearchOption.AllDirectories);
        
        if (files.Length == 0)
            throw new Exception("Could not find version in project.godot");
        
        foreach (var file in files)
        {
            var lines = await File.ReadAllLinesAsync(file.FullName);

            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains("config/version"))
                    continue;

                var verStr = lines[i].Split("=")[^1].Trim('"');
                var verSplit = verStr.Split(".");
                var buildNumInt = int.Parse(verSplit[^1]);
                buildNumInt++;
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