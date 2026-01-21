using System.CommandLine;
using System.Diagnostics;
using CliWrap;
using Spectre.Console;
using Command = System.CommandLine.Command;

namespace MG_CLI;

/// <summary>
/// https://itch.io/docs/butler/installing.html
/// </summary>
public class ItchioButlerSetup : Command
{
    private static readonly Option<bool> _version = new("--version", "-v")
    {
        HelpName = "The version of Butler that is installed"
    };
    
    public ItchioButlerSetup() : base("itchio-setup", "Downloads and installed Butler")
    {
        Add(_version);
        SetAction(Run);
    }

    private static async Task<int> Run(ParseResult result, CancellationToken token)
    {
        var version = result.GetValue(_version);
        if (version)
            return await PrintVersion(token);
        
        await DownloadButler();

        var butlerPath = GetButlerPath();
        var dir = Path.GetDirectoryName(butlerPath)!;
        var res = await Cli
            .Wrap(butlerPath)
            .WithWorkingDirectory(dir)
            .WithArguments("login")
            .WithCustomPipes()
            .ExecuteAsync(token);
        
        return res.ExitCode;
    }

    private static async Task DownloadButler()
    {
        var ms = Stopwatch.StartNew();

        // Path to install butler
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = Path.Combine(home, "butler");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        else
            DirectoryUtil.DeleteDirectoryExists(path, true);

        // Get URL
        string url;
        if (OperatingSystem.IsWindows())
            url = "https://broth.itch.zone/butler/windows-amd64/LATEST/archive/default";
        else if (OperatingSystem.IsLinux())
            url = "https://broth.itch.zone/butler/linux-amd64/LATEST/archive/default";
        else if (OperatingSystem.IsMacOS())
            url = "https://broth.itch.zone/butler/darwin-amd64/LATEST/archive/default";
        else
            throw new PlatformNotSupportedException();

        // Download and unzip
        var zipFilePath = $"{path}/butler.zip";
        await Web.DownloadFileWithProgressAsync(url, zipFilePath);
        await Zip.UnzipFileAsync(zipFilePath, path);
        File.Delete(zipFilePath);
        Log.Print($"Itchio butler downloaded successfully! [{ms.ElapsedMilliseconds}ms]", Color.Green);

        // chmod
        var butlerPath = GetButlerPath();
        FileEx.Chmod(butlerPath);
    }

    public static string GetButlerPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsWindows())
            return Path.Combine(home, "butler/butler.exe");
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            return Path.Combine(home, "butler/butler");
        
        return string.Empty;
    }

    private static async Task<int> PrintVersion(CancellationToken token)
    {
        var butlerPath = GetButlerPath();
        var res = await Cli
            .Wrap(butlerPath)
            .WithArguments("version")
            .WithCustomPipes()
            .ExecuteAsync(token);
        return res.ExitCode;
    }
}