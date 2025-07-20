using System.CommandLine;
using CLI.Utils;
using CliWrap;
using Spectre.Console;
using Command = System.CommandLine.Command;

namespace CLI.Commands;

public class BuildGodot : ICommand
{
    public Command BuildCommand()
    {
        var command = new Command("build-godot");
        
        var projectPath = new Option<string>("--projectPath", "-p")
        {
            HelpName = "Path to Godot project"
        };
        command.Add(projectPath);
        
        var godotVersion = new Option<string>("--godotVersion", "-v")
        {
            HelpName = "Version of Godot Engine to use for build."
        };
        command.Add(godotVersion);
        
        var exportRelease = new Option<string>("--exportRelease", "-r")
        {
            HelpName = "ExportRelease preset name set in export_presets.cfg"
        };
        command.Add(exportRelease);
        
        // Set the handler directly
        command.SetAction(async (result, token) 
            => await Run(
                result.GetRequiredValue(projectPath),
                result.GetRequiredValue(godotVersion),
                result.GetRequiredValue(exportRelease)
                ));
        return command;
    }

    private static async Task<int> Run(string projectPath,
        string godotVersion,
        string exportRelease)
    {
        // run a dotnet build to catch any compile errors first
        var res = await PrebuildAsync(projectPath, godotVersion);
        if (res != 0) return res;
        
        res = await BuildAsync(projectPath, godotVersion, exportRelease);
        if (res != 0) return res;

        return 0;
    }

    /// <summary>
    /// Executes a pre-build process for the Godot project to detect and resolve
    /// potential compile-time errors while setting up the required Godot configurations.
    /// </summary>
    /// <param name="projectPath">The file path to the Godot project that requires preparation.</param>
    /// <param name="godotVersion">The version of the Godot engine being used for the project.</param>
    /// <returns>A task that returns an integer exit code, where 0 indicates success and any non-zero value indicates an error.</returns>
    private static async Task<int> PrebuildAsync(string projectPath, string godotVersion)
    {
        // run a dotnet build to catch any compile errors first
        var res = await Cli.Wrap("dotnet")
            .WithArguments("build -c ExportRelease")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync();
        
        if (res.ExitCode != 0)
            return res.ExitCode;

        // FIX: work around for bug https://github.com/firebelley/godot-export/issues/127
        var godotPath = SetupGodot.GetDefaultGodotPath(godotVersion);
        var dotGodotFolder = Path.Combine(projectPath, ".godot");
        if (!Directory.Exists(dotGodotFolder))
        {
            res = await Cli.Wrap(godotPath)
                .WithArguments("--headless --import")
                .WithWorkingDirectory(projectPath)
                .WithCustomPipes()
                .ExecuteAsync();
            return res.ExitCode;
        }
        
        return 0;
    }

    private static async Task<int> BuildAsync(string projectPath, string godotVersion, string exportRelease)
    {
        // delete and create new build directory
        var buildPathRaw = GetExportPath(projectPath, exportRelease);
        var buildPath = Path.GetFullPath(Path.Combine(projectPath, buildPathRaw));
        var buildDir = Path.GetDirectoryName(buildPath) ?? throw new NullReferenceException();
        if (Directory.Exists(buildDir))
            Directory.Delete(buildDir, true);
        Directory.CreateDirectory(buildDir);
        
        // export project
        var godotPath = SetupGodot.GetDefaultGodotPath(godotVersion);
        var res = await Cli.Wrap(godotPath)
            .WithArguments($"--headless --import --path . --export-release {exportRelease}")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync();
        
        if (res.IsSuccess)
            Log.WriteLine($"Build successful [{res.RunTime}]. {buildPath}", Color.Green);
        
        return res.ExitCode;
    }

    #region Helper Methods
    
    private static string GetExportPath(string projectPath, string exportRelease)
    {
        var exportPresetsCfg = GetExportPresetsCfg(projectPath);
        foreach (var line in exportPresetsCfg)
        {
            if (!line.Contains("export_path")) 
                continue;
            
            var exportPath = line.Split("=")[^1].Trim('"');
            return exportPath;
        }
        
        throw new Exception($"Export preset {exportRelease} not found in export_presets.cfg.");
    }

    private static string[] GetExportPresetsCfg(string projectPath)
    {
        var exportPresetsPath = Path.Combine(projectPath, "export_presets.cfg");
        
        if (File.Exists(exportPresetsPath))
            return File.ReadAllLines(exportPresetsPath);
        
        throw new Exception("No export presets found in project directory.");
    }
    
    #endregion
}