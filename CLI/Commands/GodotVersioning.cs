using System.CommandLine;

namespace CLI.Commands;

public class GodotVersioning : ICommand
{
    public Command BuildCommand()
    {
        var command = new Command("godot-versioning");
        var pathOption = new Option<string>("--path", "-p")
        {
            HelpName = "Path to project.godot"
        };
        command.Add(pathOption);
        
        // Set the handler directly
        command.SetAction(async (result, token) =>
        {
            try
            {
                var path = result.GetRequiredValue(pathOption);
                await Run(path);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return 1;
            }
            
            return 0;
        });

        return command;
    }

    private static async Task Run(string path)
    {
        if (!path.EndsWith("project.godot"))
            path = Path.Combine(path, "project.godot"); 
        
        var lines = await File.ReadAllLinesAsync(path);

        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("config/version"))
                continue;

            var verStr = lines[i].Split("=")[^1].Trim('"');
            var verSplit = verStr.Split(".");
            var buildNumInt = int.Parse(verSplit[^1]);
            buildNumInt++;
            verSplit[^1] = buildNumInt.ToString();
            var newLine = string.Join(".", verSplit);
            lines[i] = newLine;
            await File.WriteAllLinesAsync(path, lines);
            break;
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