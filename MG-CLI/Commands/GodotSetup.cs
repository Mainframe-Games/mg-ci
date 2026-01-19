using System.CommandLine;

namespace MG_CLI;

public class GodotSetup : Command
{
    private readonly Option<string> _option = new ("--version", "-v")
    {
        HelpName = "The version of Godot to install"
    };
    
    public GodotSetup() : base("godot-setup", "Installs the Godot engine and export templates.")
    {
        Add(_option);
        SetAction(Run);
    }

    private async Task<int> Run(ParseResult result, CancellationToken token)
    {
        var godotVersion = result.GetRequiredValue(_option);
        var coreUrl =
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
        else if (OperatingSystem.IsMacOS())
        {
            engineZip = $"Godot_v{godotVersion}-stable_mono_macos.universal.zip";
            engineDir = $"{home}/Applications";
            exportTemplatesPath = $"{home}/Library/Application Support/Godot/export_templates/{godotVersion}.stable.mono";
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
        if (OperatingSystem.IsMacOS())
        {
            DirectoryUtil.DeleteDirectoryExists(engineDir + "/Godot_mono.app", false);
        }
        else
        {
            DirectoryUtil.DeleteDirectoryExists(engineDir, true);
        }
        
        // download engine
        await Web.DownloadFileWithProgressAsync(engineUrl, $"{engineDir}/{engineZip}");
        
        // download export templates
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

        if (OperatingSystem.IsLinux())
        {
            await CreateDesktopEntryLinux(godotVersion);
            var enginePath = $"{engineDir}/Godot_v{godotVersion}-stable_mono_linux_x86_64/Godot_v{godotVersion}-stable_mono_linux.x86_64";
            FileEx.Chmod(enginePath);
        }
        else if (OperatingSystem.IsMacOS())
        {
            var enginePath = $"{engineDir}/Godot_mono.app/Contents/MacOS/Godot";
            FileEx.Chmod(enginePath);
        }
        
        Console.WriteLine($"[GODOT SETUP] Godot Engine {godotVersion} setup completed successfully.");
        
        return 0;
    }
    
    private static async Task CreateDesktopEntryLinux(string godotVersion)
    {
        if (!OperatingSystem.IsLinux())
            return;
        
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        string desktopContents =
            $"""
            [Desktop Entry]
            Version=4.1.1
            Name=Godot Engine
            Comment=Godot Engine Mono
            Exec=/bin/sh -c "{home}/.local/share/godot/engine/Godot_v{godotVersion}-stable_mono_linux_x86_64/Godot_v{godotVersion}-stable_mono_linux.x86_64"
            Icon={home}/.local/share/godot/engine/icon_color.svg
            Terminal=false
            Type=Application
            Categories=Development
            Name[en_NZ]=Godot Engine
            """;
        
        var desktopEntryPath = $"{home}/.local/share/applications/godot.desktop";
        await File.WriteAllTextAsync(desktopEntryPath, desktopContents);
        Console.WriteLine($"[GODOT SETUP] Created desktop entry {desktopEntryPath}\n{desktopContents}");
        FileEx.Chmod(desktopEntryPath);
        
        // download icon
        var iconPath = $"{home}/.local/share/godot/engine/icon_color.svg";
        await Web.DownloadFileWithProgressAsync("https://godotengine.org/assets/press/icon_color.svg", iconPath);
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
        else if (OperatingSystem.IsMacOS())
        {
            var engineDir = Path.Combine(home, "Applications/Godot_mono.app/Contents/MacOS/Godot");
            var fileInfo = new FileInfo(engineDir);
            if (fileInfo.Exists)
                return fileInfo.FullName;
            throw new FileNotFoundException($"Could not find Godot executable: {engineDir}");
        }
        else
        {
            throw new PlatformNotSupportedException($"Platform not supported: {Environment.OSVersion}");
        }

        throw new Exception("Could not find Godot executable.");
    }
}