using SharedLib;

namespace UnityBuilder;

/// <summary>
/// docs: https://docs.unity3d.com/hub/manual/HubCLI.html
/// </summary>
public static class UnityHub
{
    private static string Path
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return @"C:\Program Files\Unity Hub\Unity Hub.exe";

            if (OperatingSystem.IsMacOS())
                return "/Applications/Unity Hub.app/Contents/MacOS/Unity Hub";

            if (OperatingSystem.IsLinux())
                return "/usr/bin/unityhub";

            throw new Exception("Unsupported Operating System");
        }
    }

    #region Install

    public static void Install()
    {
        if (OperatingSystem.IsWindows())
        {
            InstallWindows();
        }
        else if (OperatingSystem.IsMacOS())
        {
            InstallMac();
        }
        else if (OperatingSystem.IsLinux())
        {
            InstallLinux();
        }
        else
        {
            throw new Exception("Unsupported Operating System");
        }
    }

    private static void InstallWindows()
    {
        var (exitCode, output) = Cmd.Run("winget", "install -e --id Unity.UnityHub");

        if (exitCode != 0)
        {
            throw new Exception($"Failed to get Unity Hub. {output}");
        }

        Console.WriteLine("UnityHub installed successfully.");
        Console.WriteLine(output);
    }

    private static void InstallMac()
    {
        var (exitCode, output) = Cmd.Run("brew", "install --cask unity-hub");

        if (exitCode != 0)
        {
            throw new Exception($"Failed to get Unity Hub. {output}");
        }

        Console.WriteLine("UnityHub installed successfully.");
        Console.WriteLine(output);
    }

    /// <summary>
    /// docs: https://docs.unity3d.com/hub/manual/InstallHub.html
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private static void InstallLinux()
    {
        Cmd.Run(
            "wget",
            "-qO - https://hub.unity3d.com/linux/keys/public | gpg --dearmor | sudo tee /usr/share/keyrings/Unity_Technologies_ApS.gpg > /dev/null"
        );

        Cmd.Run(
            "sudo",
            "sh -c 'echo \"deb [signed-by=/usr/share/keyrings/Unity_Technologies_ApS.gpg] https://hub.unity3d.com/linux/repos/deb stable main\" > /etc/apt/sources.list.d/unityhub.list'"
        );

        Cmd.Run("sudo", "apt update");
        Cmd.Run("sudo", "apt-get install unityhub");

        Console.WriteLine("UnityHub installed successfully.");
    }

    #endregion

    public static void Help()
    {
        var (exitCode, output) = Cmd.Run(Path, "-- --headless help");
        Console.WriteLine(output);
    }
}
