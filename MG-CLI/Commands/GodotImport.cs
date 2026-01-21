using System.CommandLine;
using CliWrap;
using Command = System.CommandLine.Command;

namespace MG_CLI;

public class GodotImport : Command
{
    private readonly Argument<string> _projectPath = new("project-path")
    {
        HelpName = "Path to Godot project"
    };
    
    private readonly Argument<string> _godotVersion = new("godot-version")
    {
        HelpName = "Version of Godot to use"
    };
    
    public GodotImport() : base("godot-import", "Runs import process for Godot")
    {
        Add(_projectPath);
        Add(_godotVersion);
        SetAction(Run);
    }

    private async Task<int> Run(ParseResult result, CancellationToken token)
    {
        Log.CreateLogFile("godot-import.log", LogLevel.Warning);
        var godotVersion = result.GetRequiredValue(_godotVersion);
        var projectPath = result.GetRequiredValue(_projectPath);
        var exitCode = await Import(godotVersion, projectPath, token);
        Log.StopLoggingToFile();
        return exitCode;
    }

    public static async Task<int> Import(string godotVersion, string projectPath, CancellationToken token)
    {
        var godotPath = GodotSetup.GetDefaultGodotPath(godotVersion);
        var res = await Cli
            .Wrap(godotPath)
            .WithArguments("--headless --import")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync(token);
        Log.Success($"Completed godot import: {res.RunTime.TotalSeconds}s");
        return res.ExitCode;
    }
}