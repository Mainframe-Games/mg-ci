using System.Diagnostics;
using System.Text;

namespace SteamDeployment;

public class SteamDeploy(
    string vdfPath,
    string password,
    string username,
    string description,
    string setLive = "beta"
)
{
    private static string SteamCmdPath
    {
        get
        {
            if (OperatingSystem.IsWindows())
                return Path.Combine("ContentBuilder", "builder", "steamcmd.exe");
            if (OperatingSystem.IsLinux())
                return Path.Combine("ContentBuilder", "builder_linux", "steamcmd.sh");
            if (OperatingSystem.IsMacOS())
                return Path.Combine("ContentBuilder", "builder_osx", "steamcmd.sh");

            throw new Exception("Operating system not supported");
        }
    }

    public void Deploy()
    {
        var vdfPath1 = Path.Combine(Environment.CurrentDirectory, vdfPath);
        SetVdfProperties(vdfPath1, ("Desc", description), ("SetLive", setLive));

        var args = new StringBuilder();
        args.Append("+login");
        args.Append($" {username}");
        args.Append($" {password}");
        args.Append($" +run_app_build \"{vdfPath1}\"");
        args.Append(" +quit");

        var process = Process.Start(SteamCmdPath, args.ToString());
        process.WaitForExit();

        var code = process.ExitCode;
        var output = process.StandardOutput.ReadToEnd();

        if (output.Contains("FAILED", StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Steam upload failed ({code}): {output}");
    }

    private static void SetVdfProperties(string vdfPath, params (string key, string value)[] values)
    {
        var vdfLines = File.ReadAllLines(vdfPath);

        foreach ((string key, string value) in values)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                continue;

            foreach (var line in vdfLines)
            {
                if (!line.Contains($"\"{key}\""))
                    continue;

                var index = Array.IndexOf(vdfLines, line);
                vdfLines[index] = $"\t\"{key}\" \"{value}\"";
            }
        }

        File.WriteAllText(vdfPath, string.Join("\n", vdfLines));
    }
}
