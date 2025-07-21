using System.CommandLine;
using CLI.Utils;

namespace CLI.Commands;

public class SteamCmdSetup : ICommand
{
    public Command BuildCommand()
    {
        var command = new Command("steamcmd-setup");
        command.SetAction(async (result, token) => await Run());
        return command;
    }

    private static async Task<int> Run()
    {
        const int sdkVersion = 160;
        var zipFileName = $"steamworks_sdk_{sdkVersion}.zip";
        var steamworksSdkUrl = $"https://partner.steamgames.com/downloads/{zipFileName}";

        var destinationPath = GetSteamCmdDestinationPath();
        var destinationPathTemp = $"{destinationPath}_temp";
        
        DirectoryUtil.DeleteDirectoryExists(destinationPath, true);
        DirectoryUtil.DeleteDirectoryExists(destinationPathTemp, true);

        await Web.DownloadFileWithProgressAsync(steamworksSdkUrl, destinationPath);
        await Zip.UnzipFileAsync($"{destinationPathTemp}/{zipFileName}", destinationPathTemp);

        DirectoryInfo tempDir = GetTempContentBuilderDirectory(destinationPathTemp);
        
        var files = tempDir.GetFiles("*.*", SearchOption.AllDirectories);
        foreach (var fileInfo in files)
            fileInfo.CopyTo($"{destinationPath}/{fileInfo.Name}", true);
        
        DirectoryUtil.DeleteDirectoryExists(destinationPathTemp, false);
        
        return 0;
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
            return Path.Combine("Applications", "steamcmd");
        
        throw new PlatformNotSupportedException($"Platform not supported. {Environment.OSVersion}");
    }

    public static string GetDefaultSteamCmdPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsWindows())
            return Path.Combine(home, "steamcmd", "steamcmd.exe");
        
        if (OperatingSystem.IsLinux())
            return Path.Combine(home, "steamcmd", "steamcmd.sh");

        throw new PlatformNotSupportedException($"Platform not supported. {Environment.OSVersion}");
    }
}