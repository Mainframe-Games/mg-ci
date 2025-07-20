using System.CommandLine;
using CLI.Utils;

namespace CLI.Commands;

public class GodotSetup : ICommand
{
    public Command BuildCommand()
    {
        var command = new Command("godot-setup");
        
        // Add options or subcommands
        var option = new Option<string>("--version", "-v");
        command.Add(option);

        // Set the handler directly
        command.SetAction(async (result, token) 
            => await Run(result.GetRequiredValue(option)));

        return command;
    }

    private static async Task<int> Run(string godotVersion)
    {
        string coreUrl =
            $"https://github.com/godotengine/godot-builds/releases/download/{godotVersion}-stable/";
        
        Console.WriteLine($"[Godot Setup] Godot version: {godotVersion}");
        Console.WriteLine($"[Godot Setup] Core Url: {coreUrl}");

        string engineZip;
        string engineDir;
        string exportTemplatesPath;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        if (OperatingSystem.IsWindows())
        {
            engineZip = $"Godot_v{godotVersion}-stable_mono_win64.zip";
            engineDir = $"{home}/Godot";
            exportTemplatesPath = $"{home}/AppData/Roaming/Godot/export_templates/{godotVersion}.stable.mono";
        }
        else if (OperatingSystem.IsLinux())
        {
            engineZip = $"Godot_v{godotVersion}-stable_mono_linux_x86_64.zip";
            engineDir = $"{home}/.local/share/godot/engine";
            exportTemplatesPath = $"{home}/.local/share/godot/export_templates/{godotVersion}.stable.mono";
        }
        else
        {
            throw new Exception($"Platform not supported: {Environment.OSVersion}");
        }
        
        var engineUrl = $"{coreUrl}/{engineZip}";
        
        var exportTemplateTpz = $"Godot_v{godotVersion}-stable_mono_export_templates.tpz";
        var exportTemplatesUrl = $"{coreUrl}/{exportTemplateTpz}";
        
        Console.WriteLine($"[Godot Setup] EngineDir: {engineDir}");
        Console.WriteLine($"[Godot Setup] ExportTemplatesUrl: {exportTemplatesUrl}");
        Console.WriteLine($"[Godot Setup] ExportTemplatesPath: {exportTemplatesPath}");
        
        // create directory
        DirectoryUtil.DeleteDirectoryExists(engineDir, true);
        
        // download engine
        // Console.WriteLine($"[Godot Setup] Downloading engine... {engineUrl} -> {engineDir}/{engineZip}");
        await Web.DownloadFileWithProgressAsync(engineUrl, $"{engineDir}/{engineZip}");
        
        // download export templates
        // Console.WriteLine($"[Godot Setup] Downloading export templates... {exportTemplatesUrl} -> {engineDir}/{exportTemplateTpz}");
        await Web.DownloadFileWithProgressAsync(exportTemplatesUrl, $"{engineDir}/{exportTemplateTpz}");
        
        // unzip engine
        await Zip.UnzipFileAsync($"{engineDir}/{engineZip}", engineDir);
        File.Delete($"{engineDir}/{engineZip}");
        
        // unzip templates
        DirectoryUtil.DeleteDirectoryExists(exportTemplatesPath, true);
        await Zip.UnzipFileAsync($"{engineDir}/{exportTemplateTpz}", exportTemplatesPath);
        
        // clean up export templates unzip
        var templates = new DirectoryInfo($"{exportTemplatesPath}/templates");
        foreach (var templateFile in templates.GetFiles())
            templateFile.MoveTo($"{exportTemplatesPath}/{templateFile.Name}");
        templates.Delete(true);
        File.Delete($"{engineDir}/{exportTemplateTpz}");
        
        Console.WriteLine($"[GODOT SETUP] Godot Engine {godotVersion} setup completed successfully.");
        
        return 0;
    }

    /// <summary>
    /// Retrieves the default file path for the Godot engine based on the user's operating system.
    /// The path is configured to be suitable for the platform the application is executed on.
    /// </summary>
    /// <returns>A string specifying the default path where the Godot engine is expected to be located.</returns>
    public static string GetDefaultGodotPath(string godotVersion)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        if (OperatingSystem.IsWindows())
        {
            var engineDir = Path.Combine(home, "Godot");
            var dir = new DirectoryInfo(engineDir);
            var exes = dir.GetFiles("*.exe", SearchOption.AllDirectories);
            foreach (var exe in exes)
            {
                var fileInfo = new FileInfo(exe.FullName);
                if (fileInfo.Name.StartsWith($"Godot_v{godotVersion}-stable_mono_win64"))
                    return exe.FullName;
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            var engineDir = Path.Combine(home, ".local/share/godot/engine");
            var dir = new DirectoryInfo(engineDir);
            var exes = dir.GetFiles("*.x86_64", SearchOption.AllDirectories);
            foreach (var exe in exes)
            {
                var fileInfo = new FileInfo(exe.FullName);
                if (fileInfo.Name.StartsWith($"Godot_v{godotVersion}-stable_mono_linux"))
                    return exe.FullName;
            }
        }
        else
        {
            throw new Exception($"Platform not supported: {Environment.OSVersion}");
        }

        throw new Exception("Could not find Godot executable.");
    }
}