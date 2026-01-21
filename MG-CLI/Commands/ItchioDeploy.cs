using System.CommandLine;
using System.Diagnostics;
using CliWrap;
using Spectre.Console;
using Command = System.CommandLine.Command;

namespace MG_CLI;

public class ItchioDeploy : Command
{
    private readonly Option<string> _projectPath = new("--projectPath", "-p")
    {
        HelpName = "Path to the Godot project"
    };
    
    private readonly Argument<string> _buildPath = new("build-path")
    {
        HelpName = "Path to the build directory"
    };

    private readonly Argument<string> _companyGamePlatform = new("company-game-platform")
    {
        HelpName = "e.g. <company>/<game-name>:<platform>"
    };
    
    public ItchioDeploy() : base("itchio-deploy", "Deploys a game to itch.io")
    {
        Add(_projectPath);
        Add(_buildPath);
        Add(_companyGamePlatform);
        SetAction(Run);
    }

    private async Task<int> Run(ParseResult result, CancellationToken token)
    {
        var projectPath = result.GetRequiredValue(_projectPath);
        var buildPath = result.GetRequiredValue(_buildPath);
        var companyAndGame = result.GetRequiredValue(_companyGamePlatform);
        
        var butlerPath = ItchioButlerSetup.GetButlerPath();
        var version = GodotVersioning.GetVersion(projectPath);

        var res = await Cli
            .Wrap(butlerPath)
            .WithArguments($"push {buildPath} {companyAndGame} --userversion {version}")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync(token);
        
        if (res.ExitCode != 0)
            return res.ExitCode;
        
        Log.Print($"Itchio deploy successful! [{res.RunTime}]", Color.Green);
        return 0;
    }
}