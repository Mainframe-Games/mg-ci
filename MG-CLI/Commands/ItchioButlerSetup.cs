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
    public ItchioButlerSetup() : base("itchio-setup", "Deploys a game to itch.io")
    {
        SetAction(Run);
    }

    private static async Task<int> Run(ParseResult result, CancellationToken token)
    {
        await DownloadButler();

        var butlerPath = GetButlerPath();
        var res = await Cli
            .Wrap(butlerPath)
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
}