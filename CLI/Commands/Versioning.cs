using System.CommandLine;

namespace CLI.Commands;

public class Versioning : ICommand
{
    public Command BuildCommand()
    {
        var command = new Command("versioning");
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
}