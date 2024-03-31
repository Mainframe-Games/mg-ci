using SharedLib;

namespace UnityBuilder;

/// <summary>
/// docs: https://docs.unity3d.com/hub/manual/HubCLI.html
/// </summary>
public class UnityHub
{
    public static void Download()
    {
        if (OperatingSystem.IsWindows())
        {
            DownloadWindows();
        }
        else if (OperatingSystem.IsLinux())
        {
            throw new NotImplementedException();
        }
        else if (OperatingSystem.IsMacOS())
        {
            throw new NotImplementedException();
        }
        else
        {
            throw new Exception("Unsupported Operating System");
        }
    }

    private static void DownloadWindows()
    {
        var (exitCode, output) = Cmd.Run("winget", "install -e --id Unity.UnityHub");

        if (exitCode != 0)
        {
            throw new Exception($"Failed to get Unity Hub. {output}");
        }

        Console.WriteLine("UnityHub installed successfully.");
        Console.WriteLine(output);
    }

    public static void Help()
    {
        var (exitCode, output) = Cmd.Run(
            "Unity Hub.exe",
            "-- --headless help",
            @"C:\Program Files\Unity Hub"
        );
        Console.WriteLine(output);
    }
}
