using System.CommandLine;
using CLI.Utils;
using CliWrap;
using Spectre.Console;
using Command = System.CommandLine.Command;

namespace CLI.Commands;

public class ItchioDeploy : ICommand
{
    public Command BuildCommand()
    {
        var command = new Command("itchio-deploy");
        
        var projectPath = new Option<string>("--projectPath", "-p")
        {
            HelpName = "Path to the Godot project"
        };
        command.Add(projectPath);
        
        var companyAndGame = new Option<string>("--companyAndGame", "-c")
        {
            HelpName = "COMPANY/GAME e.g. mainframegames/speed-golf-royale-prototype"
        };
        command.Add(companyAndGame);
        
        // Set the handler directly
        command.SetAction(async (result, token)
            =>
        {
            try
            {
                return await Run(
                    result.GetRequiredValue(projectPath), 
                    result.GetRequiredValue(companyAndGame)
                );
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return -1;
            }
        });
        
        return command;
    }

    private static async Task<int> Run(string projectPath, string companyAndGame)
    {
        var butlerPath = GetButlerPath();
        var version = GodotVersioning.GetVersion(projectPath);
        var buildPath = Path.Combine(projectPath, "builds", "windows");

        var res = await Cli.Wrap(butlerPath)
            .WithArguments($"push {buildPath} {companyAndGame}:windows --userversion {version}")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync();
        
        if (res.ExitCode != 0)
            return res.ExitCode;
        
        Log.WriteLine($"Itchio deploy successful! [{res.RunTime}]", Color.Green);
        
        return 0;
    }

    private static string GetButlerPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        if (OperatingSystem.IsWindows())
            return Path.Combine(home, "butler/butler.exe");

        throw new PlatformNotSupportedException();
    }
}