using System.CommandLine;
using CliWrap;
using Command = System.CommandLine.Command;

namespace MG_CLI;

public class Commit : Command
{
    private readonly Option<string> _projectPath = new("--projectPath", "-p")
    {
        HelpName = "Path to Godot project"
    };

    public Commit() : base("commit", "Commit and tag the build")
    {
        Add(_projectPath);
        SetAction(Run);
    }
    
    private async Task<int> Run(ParseResult result, CancellationToken token)
    {
        var projectPath = result.GetRequiredValue(_projectPath);
        
        // stage files
        var res = await Cli.Wrap("git")
            .WithArguments("add .")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync(token);

        if (res.ExitCode != 0)
            return res.ExitCode;
        
        var version = GodotVersioning.GetVersion(projectPath);
        
        // commit with message
        res = await Cli.Wrap("git")
            .WithArguments($"commit -m \"_Build Version: {version}\"")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync(token);
        
        if (res.ExitCode != 0)
            return res.ExitCode;
        
        // tag commit
        res = await Cli.Wrap("git")
            .WithArguments($"tag v{version}")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync(token);
        
        if (res.ExitCode != 0)
            return res.ExitCode;
        
        // push
        res = await Cli.Wrap("git")
            .WithArguments("push origin main --tags")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync(token);
        
        return res.ExitCode;
    }
}