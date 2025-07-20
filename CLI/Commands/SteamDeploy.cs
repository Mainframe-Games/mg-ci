using System.CommandLine;
using System.Text.RegularExpressions;
using CLI.Utils;

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
            => await Run(
                result.GetRequiredValue(projectPath),
                result.GetRequiredValue(vdfPath),
                result.GetRequiredValue(steamAccount),
                result.GetRequiredValue(steamPassword)
            ));
        return command;
    }

    private static async Task<int> Run(string projectPath, string vdf, string steamUsername, string steamPassword)
    {
        var version = GodotVersioning.GetVersion(projectPath);
        var vdfFullPath = Path.Combine(projectPath, vdf);
        UpdateVdfDescription(vdfFullPath, version);
        
        var steamCmdPath = SteamSetup.GetDefaultSteamCmdPath();
        var res = await CliWrap.Cli.Wrap(steamCmdPath)
            .WithArguments($"+login {steamUsername} {steamPassword} +run_app_build {vdf} quit")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync();
        
        if (res.ExitCode != 0)
            return res.ExitCode;
        
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