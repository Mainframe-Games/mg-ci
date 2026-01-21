using System.CommandLine;
using CliWrap;
using Spectre.Console;
using Command = System.CommandLine.Command;

namespace MG_CLI;

public class SteamDeploy : Command
{
    private readonly Option<string> _projectPath = new("--projectPath", "-p")
    {
        HelpName = "Path to Godot project"
    };

    private readonly Option<string> _vdfPath = new("--vdf")
    {
        HelpName = "Path to the .vdf file"
    };

    private readonly Option<string> _steamAccount = new("--username", "-u")
    {
        HelpName = "Steamworks username"
    };

    private readonly Option<string> _steamPassword = new("--password", "-pw")
    {
        HelpName = "Steamworks password"
    };
    
    private readonly Option<bool> _preview = new("--preview", "-pv")
    {
        HelpName = "Marks vdf file as Preview 1"
    };
    
    public SteamDeploy() : base("steam-deploy", "Deploy to Steam")
    {
        Add(_projectPath);
        Add(_vdfPath);
        Add(_steamAccount);
        Add(_steamPassword);
        Add(_preview);
        SetAction(Run);
    }

    private async Task<int> Run(ParseResult result, CancellationToken token)
    {
        var projectPath = result.GetRequiredValue(_projectPath);
        var vdf = result.GetRequiredValue(_vdfPath);
        var steamUsername = result.GetRequiredValue(_steamAccount);
        var steamPassword = result.GetRequiredValue(_steamPassword);
        var preview = result.GetValue(_preview);
        
        var projectPathFull = Path.GetFullPath(projectPath);
        Log.Print($"ProjectPath: {projectPathFull}");
        
        var version = GodotVersioning.GetVersion(projectPathFull);
        var vdfFullPath = Path.Combine(projectPathFull, vdf);
        await UpdateVdfDescription(vdfFullPath, version, preview);
        
        var steamCmdPath = SteamCmdSetup.GetDefaultSteamCmdPath();
        Log.Print($"SteamCmdPath: {steamCmdPath}");
        Log.Print($"Vdf: {vdfFullPath}");
        var res = await Cli.Wrap(steamCmdPath)
            .WithArguments($"+login {steamUsername} {steamPassword} +run_app_build {vdfFullPath} +quit")
            .WithWorkingDirectory(projectPathFull)
            .WithCustomPipes()
            .WithStandardInputPipe(PipeSource.FromStream(Console.OpenStandardInput()))
            .ExecuteAsync(token);
        
        if (res.ExitCode != 0)
            return res.ExitCode;
        
        Log.Success($"Steam deploy successful! [{res.RunTime}]");
        
        return 0;
    }

    private static async Task UpdateVdfDescription(string vdfPath, string version, bool preview)
    {
        if (!File.Exists(vdfPath))
            throw new FileNotFoundException($"File doesn't exist: {vdfPath}");
        
        var lines = await File.ReadAllLinesAsync(vdfPath);
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("Desc"))
                lines[i] = $"\t\"Desc\" \"{version}\"";

            if (lines[i].Contains("Preview"))
                lines[i] = $"\t\"Preview\" \"{(preview ? "1" : "0")}\"";
        }
        
        await FileEx.WriteAllLinesAsync(vdfPath, lines);
    }
}