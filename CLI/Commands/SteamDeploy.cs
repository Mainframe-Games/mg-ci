using System.CommandLine;
using System.Text.RegularExpressions;
using CLI.Utils;
using Spectre.Console;

namespace CLI.Commands;

public partial class SteamDeploy : ICommand
{
    [GeneratedRegex("""(?<="Desc"\s*")[\d.]+(?=")""")]
    private static partial Regex DescRegex();
    
    public Command BuildCommand()
    {
        var command = new Command("steam-deploy");
        
        var projectPath = new Option<string>("--projectPath", "-p")
        {
            HelpName = "Path to Godot project"
        };
        command.Add(projectPath);
        
        var vdfPath = new Option<string>("--vdf")
        {
            HelpName = "Path to the .vdf file"
        };
        command.Add(vdfPath);
        
        var steamAccount = new Option<string>("--username", "-u")
        {
            HelpName = "Steamworks username"
        };
        command.Add(steamAccount);
        var steamPassword = new Option<string>("--password", "-pw")
        {
            HelpName = "Steamworks password"
        };
        command.Add(steamPassword);
        
        // Set the handler directly
        command.SetAction(async (result, token)
            =>
        {
            try
            {
                return await Run(
                    result.GetRequiredValue(projectPath),
                    result.GetRequiredValue(vdfPath),
                    result.GetRequiredValue(steamAccount),
                    result.GetRequiredValue(steamPassword)
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

    private static async Task<int> Run(string projectPath, string vdf, string steamUsername, string steamPassword)
    {
        var projectPathFull = Path.GetFullPath(projectPath);
        Log.WriteLine($"ProjectPath: {projectPathFull}");
        
        var version = GodotVersioning.GetVersion(projectPathFull);
        var vdfFullPath = Path.Combine(projectPathFull, vdf);
        UpdateVdfDescription(vdfFullPath, version);
        
        var steamCmdPath = SteamSetup.GetDefaultSteamCmdPath();
        Log.WriteLine($"SteamCmdPath: {steamCmdPath}");
        Log.WriteLine($"Vdf: {vdfFullPath}");
        var res = await CliWrap.Cli.Wrap(steamCmdPath)
            .WithArguments($"+login {steamUsername} {steamPassword} +run_app_build {vdfFullPath} +quit")
            .WithWorkingDirectory(projectPathFull)
            .WithCustomPipes()
            .ExecuteAsync();
        
        if (res.ExitCode != 0)
            return res.ExitCode;
        
        Log.WriteLine($"Steam deploy successful! [{res.RunTime}]", Color.Green);
        
        return 0;
    }

    private static void UpdateVdfDescription(string vdfPath, string version)
    {
        if (!File.Exists(vdfPath))
            throw new FileNotFoundException($"File doesn't exist: {vdfPath}");
        
        var lines = File.ReadAllLines(vdfPath);
        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("Desc"))
                continue;
            
            // Using regex pattern that matches the version number between quotes
            var result = DescRegex().Replace(lines[i], version);
            
            lines[i] = result;
            File.WriteAllLines(vdfPath, lines);
            break;
        }
    }
}