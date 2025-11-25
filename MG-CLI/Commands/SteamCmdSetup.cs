using System.CommandLine;
using CliWrap;
using CliWrap.Buffered;
using Spectre.Console;
using Command = System.CommandLine.Command;

namespace MG;

public class SteamCmdSetup : Command
{
    public SteamCmdSetup() : base("steamcmd-setup", "Setup SteamCMD")
    {
        SetAction(Run);
    }

    private static async Task Run(ParseResult result, CancellationToken token)
    {
        const int sdkVersion = 163;
        var zipFileName = $"steamworks_sdk_{sdkVersion}.zip";
        var steamworksSdkUrl = $"https://partner.steamgames.com/downloads/{zipFileName}";

        var destinationPath = GetSteamCmdDestinationPath();
        var destinationPathTemp = $"{destinationPath}_temp";
        
        DirectoryUtil.DeleteDirectoryExists(destinationPath, true);
        DirectoryUtil.DeleteDirectoryExists(destinationPathTemp, true);

        var zipPathTemp = Path.Combine(destinationPathTemp, zipFileName);
        await Web.DownloadFileWithProgressAsync(steamworksSdkUrl, zipPathTemp);
        await Zip.UnzipFileAsync(zipPathTemp, destinationPathTemp);

        var tempDir = GetTempContentBuilderDirectory(destinationPathTemp);
        
        var files = tempDir.GetFiles("*.*", SearchOption.AllDirectories);
        foreach (var fileInfo in files)
            fileInfo.CopyTo($"{destinationPath}/{fileInfo.Name}", true);
        
        DirectoryUtil.DeleteDirectoryExists(destinationPathTemp, false);
        
        // set permissions
        if (OperatingSystem.IsMacOS())
        {
            FileEx.SetExecutablePermissionsUnix($"{destinationPath}/steamcmd");
            FileEx.SetExecutablePermissionsUnix($"{destinationPath}/steamcmd.sh");
           
            await Cli
                .Wrap($"{destinationPath}/steamcmd.sh")
                .WithCustomPipes()
                .ExecuteBufferedAsync(cancellationToken: token);
        }
        
        Log.WriteLine("SteamCMD setup complete");
    }

    private static DirectoryInfo GetTempContentBuilderDirectory(string destinationPathTemp)
    {
        if (OperatingSystem.IsWindows())
            return new DirectoryInfo($"{destinationPathTemp}/sdk/tools/ContentBuilder/builder");
        if (OperatingSystem.IsLinux())
            return new DirectoryInfo($"{destinationPathTemp}/sdk/tools/ContentBuilder/builder_linux");
        if (OperatingSystem.IsMacOS())
            return new DirectoryInfo($"{destinationPathTemp}/sdk/tools/ContentBuilder/builder_osx");
        
        throw new PlatformNotSupportedException($"Platform not supported. {Environment.OSVersion}");
    }

    private static string GetSteamCmdDestinationPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            return Path.Combine(home, "steamcmd");
        
        if (OperatingSystem.IsMacOS())
            return Path.Combine(home, "Applications", "steamcmd");
        
        throw new PlatformNotSupportedException($"Platform not supported. {Environment.OSVersion}");
    }

    public static string GetDefaultSteamCmdPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsWindows())
            return Path.Combine(home, "steamcmd", "steamcmd.exe");
        
        if (OperatingSystem.IsLinux())
            return Path.Combine(home, "steamcmd", "steamcmd.sh");
        
        if (OperatingSystem.IsMacOS())
            return Path.Combine(home, "Applications", "steamcmd", "steamcmd.sh");

        throw new PlatformNotSupportedException($"Platform not supported. {Environment.OSVersion}");
    }
}