using System.CommandLine;
using CliWrap;
using Spectre.Console;
using Command = System.CommandLine.Command;

namespace MG;

public class ItchioDeploy : Command
{
    private readonly Option<string> _projectPath = new("--projectPath", "-p")
    {
        HelpName = "Path to the Godot project"
    };

    private readonly Option<string> _companyAndGame = new("--companyAndGame", "-c")
    {
        HelpName = "COMPANY/GAME e.g. mainframegames/speed-golf-royale-prototype"
    };
    
    public ItchioDeploy() : base("itchio-deploy", "Deploys a game to itch.io")
    {
        Add(_projectPath);
        Add(_companyAndGame);
        SetAction(Run);
    }

    private async Task<int> Run(ParseResult result, CancellationToken token)
    {
        var projectPath = result.GetRequiredValue(_projectPath);
        var companyAndGame = result.GetRequiredValue(_companyAndGame);
        
        var butlerPath = GetButlerPath();
        var version = GodotVersioning.GetVersion(projectPath);
        var buildPath = Path.Combine(projectPath, "builds", "windows");

        var res = await Cli.Wrap(butlerPath)
            .WithArguments($"push {buildPath} {companyAndGame}:windows --userversion {version}")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync(token);
        
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