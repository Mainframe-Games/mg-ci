using System.CommandLine;
using CliWrap;
using Spectre.Console;
using Command = System.CommandLine.Command;

namespace MG;

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
    
    private readonly Option<bool> _interactive = new("--interactive", "-i")
    {
        HelpName = "Enable interactive mode"
    };
    
    public GodotBuild() : base("godot-build", "Builds a Godot project.")
    {
        Add(_projectPath);
        Add(_godotVersion);
        Add(_exportRelease);
        Add(_exportDebug);
        Add(_interactive);
        SetAction(Run);
    }

    private async Task<int> Run(ParseResult result, CancellationToken token)
    {
        var projectPath = result.GetRequiredValue(_projectPath);
        var godotVersion = result.GetRequiredValue(_godotVersion);
        var isInteractive = result.GetValue(_interactive);

        var templates = new List<string>();
        var isDebug = false;
        
        if (isInteractive)
        {
            templates = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Choose targets:")
                    .InstructionsText(
                        "[grey](Press [blue]<space>[/] to toggle, " + 
                        "[green]<enter>[/] to accept)[/]")
                    .AddChoices(GetExportPresets(projectPath)));
            
            isDebug = await AnsiConsole.ConfirmAsync("Build in Debug mode?", false, token);
            
        }
        else
        {
            var exportRelease = result.GetValue(_exportRelease);
            var exportDebug = result.GetValue(_exportDebug);
            isDebug = !string.IsNullOrEmpty(exportDebug);
            templates.Add(isDebug ? exportDebug! : exportRelease!);
        }

        // run a dotnet build to catch any compile errors first
        var res = await PrebuildAsync(projectPath, godotVersion);
        if (res != 0) return res;
        
        foreach (var template in templates)
        {
            var exportType = isDebug ? $"--export-debug {template}" : $"--export-release {template}";
            res = await BuildAsync(projectPath, godotVersion, exportType);
            if (res != 0)
                Log.Print($"Build failed for '{template}' [{res}].", Color.Red);
        }

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
            .WithArguments("build")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync();
        
        if (res.ExitCode != 0)
            return res.ExitCode;

        // FIX: work around for bug https://github.com/firebelley/godot-export/issues/127
        var dotGodotFolder = Path.Combine(projectPath, ".godot");
        if (!Directory.Exists(dotGodotFolder))
        {
            var godotPath = GodotSetup.GetDefaultGodotPath(godotVersion);
            res = await Cli.Wrap(godotPath)
                .WithArguments("--headless --import")
                .WithWorkingDirectory(projectPath)
                .WithCustomPipes()
                .ExecuteAsync();
            return res.ExitCode;
        }
        
        return 0;
    }

    private static async Task<int> BuildAsync(string projectPath, string godotVersion, string? exportType)
    {
        // delete and create new build directory
        var template = exportType?.Split(' ')[^1].Trim() ?? throw new NullReferenceException("No export preset specified.");
        var buildPathRaw = GetExportPath(projectPath, template);
        var buildPath = Path.GetFullPath(Path.Combine(projectPath, buildPathRaw));
        var buildDir = Path.GetDirectoryName(buildPath) ?? throw new NullReferenceException();
        DirectoryUtil.DeleteDirectoryExists(buildDir, true);
        
        Log.CreateLogFile($"builds/Logs/{template}.log");
        
        // export project
        var godotPath = GodotSetup.GetDefaultGodotPath(godotVersion);
        var res = await Cli.Wrap(godotPath)
            .WithArguments($"--headless --import --path . {exportType}")
            .WithWorkingDirectory(projectPath)
            .WithCustomPipes()
            .ExecuteAsync();
        
        if (res.IsSuccess)
            Log.Print($"Build successful [{res.RunTime}]. {buildPath}", Color.Green);
        
        // mac builds need to be unzipped
        if (template == "Mac")
        {
            var extractFolder = buildPath.Replace(".zip", "");
            await Zip.UnzipFileAsync(buildPath, extractFolder + "/..");
            Log.Print($"Deleting zip file... {buildPath}");
            File.Delete(buildPath);
        }

        Log.StopLoggingToFile();
        return res.ExitCode;
    }

    #region Helper Methods

    private static IEnumerable<string> GetExportPresets(string projectPath)
    {
        var exportPresetsCfg = GetExportPresetsCfg(projectPath);
        foreach (var line in exportPresetsCfg)
        {
            if (!line.StartsWith("name=")) 
                continue;
            
            var presetName = GetLineValue(line);
            yield return presetName;
        }
    }
    
    private static string GetExportPath(string projectPath, string exportRelease)
    {
        var exportPresetsCfg = GetExportPresetsCfg(projectPath);
        var foundPreset = false;
        
        foreach (var line in exportPresetsCfg)
        {
            if (line.StartsWith("name=") && GetLineValue(line) == exportRelease)
            {
                foundPreset = true;
                continue;
            }
            
            if(!foundPreset)
                continue;
            
            if (!line.StartsWith("export_path=")) 
                continue;
            
            var exportPath = GetLineValue(line);
            return exportPath;
        }

        throw new Exception($"Export preset {exportRelease} not found in export_presets.cfg.");
    }
    
    private static string GetLineValue(in string line)
        => line.Split("=")[^1].Trim('"');

    private static string[] GetExportPresetsCfg(string projectPath)
    {
        var exportPresetsPath = Path.Combine(projectPath, "export_presets.cfg");
        return File.Exists(exportPresetsPath)
            ? File.ReadAllLines(exportPresetsPath)
            : throw new Exception("No export presets found in project directory.");
    }
    
    #endregion
}