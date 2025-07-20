using System.CommandLine;
using CLI.Utils;
using CliWrap;
using Command = System.CommandLine.Command;

namespace CLI.Commands;

public class Commit : ICommand
{
    public Command BuildCommand()
    {
        var command = new Command("commit");
        
        var projectPath = new Option<string>("--projectPath", "-p")
        {
            HelpName = "Path to Godot project"
        };
        command.Add(projectPath);
        
        // Set the handler directly
        command.SetAction(async (result, token)
            =>
        {
            try
            {
                return await Run(result.GetRequiredValue(projectPath));
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return -1;
            }
        });
        
        
        return command;
    }

    private static async Task<int> Run(string projectPath)
    {
        // stage files
        var res = await Cli.Wrap("git")
            .WithArguments("add .")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync();

        if (res.ExitCode != 0)
            return res.ExitCode;
        
        var version = GodotVersioning.GetVersion(projectPath);
        
        // commit with message
        res = await Cli.Wrap("git")
            .WithArguments($"commit -m \"_Build Version: {version}\"")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync();
        
        if (res.ExitCode != 0)
            return res.ExitCode;
        
        // tag commit
        res = await Cli.Wrap("git")
            .WithArguments($"tag v{version}")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync();
        
        if (res.ExitCode != 0)
            return res.ExitCode;
        
        // push
        res = await Cli.Wrap("git")
            .WithArguments("push origin main --tags")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync();
        
        if (res.ExitCode != 0)
            return res.ExitCode;

        return 0;
    }
}