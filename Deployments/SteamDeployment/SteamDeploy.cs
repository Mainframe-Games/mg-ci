using System.Diagnostics;
using System.Text;

namespace SteamDeployment;

public class SteamDeploy(AppBuild appBuild, string password, string username)
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
        var vdf = appBuild.Build();
        var vdfPath = Path.Combine(appBuild.ContentRoot, "app_build.vdf");
        File.WriteAllText(vdfPath, vdf);

        var args = new StringBuilder();
        args.Append("+login");
        args.Append($" {username}");
        args.Append($" {password}");
        args.Append($" +run_app_build \"{vdfPath}\"");
        args.Append(" +quit");

        var process = Process.Start(SteamCmdPath, args.ToString());
        process.WaitForExit();

        var code = process.ExitCode;
        var output = process.StandardOutput.ReadToEnd();

        if (output.Contains("FAILED", StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Steam upload failed ({code}): {output}");
    }
}
