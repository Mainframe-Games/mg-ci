using System.CommandLine;
using CLI.Utils;
using CliWrap;
using Spectre.Console;
using Command = System.CommandLine.Command;

namespace CLI.Commands;

public class GodotBuild : Command
{
    private readonly Option<string> _projectPath = new("--projectPath", "-p")
    {
        HelpName = "Path to Godot project"
    };

    private readonly Option<string> _godotVersion = new("--godotVersion", "-v")
    {
        HelpName = "Version of Godot Engine to use for build."
    };

    private readonly Option<string> _exportRelease = new("--export-release", "-r")
    {
        HelpName = "ExportRelease preset name set in export_presets.cfg"
    };
    
    private readonly Option<string> _exportDebug = new("--export-debug", "-d")
    {
        HelpName = "ExportDebug preset name set in export_presets.cfg"
    };
    
    public GodotBuild() : base("godot-build", "Builds a Godot project.")
    {
        Add(_projectPath);
        Add(_godotVersion);
        Add(_exportRelease);
        Add(_exportDebug);
        SetAction(Run);
    }

    private async Task<int> Run(ParseResult result, CancellationToken token)
    {
        var projectPath = result.GetRequiredValue(_projectPath);
        var godotVersion = result.GetRequiredValue(_godotVersion);
        var exportRelease = result.GetValue(_exportRelease);
        var exportDebug = result.GetValue(_exportDebug);
        
        // run a dotnet build to catch any compile errors first
        var res = await PrebuildAsync(projectPath, godotVersion);
        if (res != 0) return res;
        
        res = await BuildAsync(projectPath, godotVersion, exportRelease, exportDebug);
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
            // .WithArguments("build -c ExportRelease")
            .WithArguments("build")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync();
        
        if (res.ExitCode != 0)
            return res.ExitCode;

        // FIX: work around for bug https://github.com/firebelley/godot-export/issues/127
        var godotPath = GodotSetup.GetDefaultGodotPath(godotVersion);
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

    private static async Task<int> BuildAsync(string projectPath, string godotVersion, string? exportRelease, string? exportDebug)
    {
        // delete and create new build directory
        var buildPathRaw = GetExportPath(projectPath, exportRelease ?? exportDebug!);
        var buildPath = Path.GetFullPath(Path.Combine(projectPath, buildPathRaw));
        var buildDir = Path.GetDirectoryName(buildPath) ?? throw new NullReferenceException();
        DirectoryUtil.DeleteDirectoryExists(buildDir, true);
        
        string? export = null;
        if (exportRelease is not null)
            export = $"--export-release {exportRelease}";
        else if (exportDebug is not null)
            export = $"--export-debug {exportDebug}";
        
        // export project
        var godotPath = GodotSetup.GetDefaultGodotPath(godotVersion);
        var res = await Cli.Wrap(godotPath)
            .WithArguments($"--headless --import --path . {export}")
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