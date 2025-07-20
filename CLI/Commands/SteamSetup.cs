namespace CLI.Commands;

public class SteamSetup
{
    public static string GetDefaultSteamCmdPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsWindows())
            return Path.Combine(home, "steamcmd", "steamcmd.exe");
        
        if (OperatingSystem.IsLinux())
            return Path.Combine(home, "steamcmd", "steamcmd.sh");

        throw new Exception("Could not find steamcmd.");
    }
}